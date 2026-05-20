using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// Agent智能体控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IAgentOrchestrator agentOrchestrator,
        ILogger<AgentController> logger)
    {
        _agentOrchestrator = agentOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// 执行Agent任务（流式响应SSE）
    /// </summary>
    [HttpPost("execute")]
    public async Task ExecuteAgent(
        [FromBody] AgentExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var tenantId = User.FindFirstValue("tenant_id") ?? "default";

        _logger.LogInformation(
            "用户 {UserId} 启动Agent任务: {Input}",
            userId,
            request.Input);

        // 设置SSE响应头
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");

        try
        {
            await foreach (var stepEvent in _agentOrchestrator.ExecuteAsync(
                request.Input,
                userId,
                tenantId,
                request.MaxIterations ?? 10,
                cancellationToken))
            {
                // 转换为SSE格式
                var sseData = FormatSseEvent(stepEvent);
                await Response.WriteAsync(sseData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                // 短暂延迟确保客户端能接收
                await Task.Delay(50, cancellationToken);
            }

            // 发送结束标记
            await Response.WriteAsync("event: done\ndata: {\"status\":\"completed\"}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent执行异常");
            var errorData = $"event: error\ndata: {{\"message\":\"{ex.Message}\"}}\n\n";
            await Response.WriteAsync(errorData, cancellationToken);
        }
    }

    /// <summary>
    /// 执行Agent任务（普通JSON响应）
    /// </summary>
    [HttpPost("execute-sync")]
    public async Task<IActionResult> ExecuteAgentSync(
        [FromBody] AgentExecuteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var tenantId = User.FindFirstValue("tenant_id") ?? "default";

        var steps = new List<AgentStepEvent>();
        string? finalAnswer = null;
        Guid? sessionId = null;

        try
        {
            await foreach (var stepEvent in _agentOrchestrator.ExecuteAsync(
                request.Input,
                userId,
                tenantId,
                request.MaxIterations ?? 10,
                cancellationToken))
            {
                steps.Add(stepEvent);
                sessionId = stepEvent.SessionId;

                if (stepEvent.EventType == AgentEventType.FinalAnswer)
                {
                    finalAnswer = stepEvent.FinalAnswer;
                }
            }

            return Ok(new AgentExecuteResponse
            {
                SessionId = sessionId ?? Guid.Empty,
                FinalAnswer = finalAnswer ?? "未生成答案",
                Steps = steps.Select(s => new AgentStepDto
                {
                    StepIndex = s.StepIndex,
                    EventType = s.EventType.ToString(),
                    Thought = s.Thought,
                    ToolName = s.ToolCall?.ToolName,
                    ToolArguments = s.ToolCall?.Arguments,
                    Observation = s.Observation,
                    Timestamp = s.Timestamp
                }).ToList(),
                TotalSteps = steps.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent执行失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取会话历史
    /// </summary>
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _agentOrchestrator.GetSessionAsync(sessionId, cancellationToken);

        if (session == null)
            return NotFound(new { message = "会话不存在" });

        return Ok(new
        {
            session_id = session.Id,
            user_id = session.UserId,
            intent = session.UserIntent,
            intent_type = session.IntentType,
            status = session.Status.ToString(),
            final_answer = session.FinalAnswer,
            start_time = session.StartTime,
            end_time = session.EndTime,
            total_cost_seconds = session.TotalCostSeconds,
            steps = session.Steps.Select(s => new
            {
                step_index = s.StepIndex,
                step_type = s.StepType,
                tool_name = s.ToolName,
                tool_arguments = s.ToolArguments,
                tool_result = s.ToolResult,
                is_success = s.IsSuccess,
                duration_ms = s.DurationMs
            }).ToList()
        });
    }

    private string FormatSseEvent(AgentStepEvent stepEvent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"event: {stepEvent.EventType}");

        var data = new
        {
            session_id = stepEvent.SessionId,
            step_index = stepEvent.StepIndex,
            thought = stepEvent.Thought,
            tool_call = stepEvent.ToolCall,
            observation = stepEvent.Observation,
            final_answer = stepEvent.FinalAnswer,
            error_message = stepEvent.ErrorMessage,
            timestamp = stepEvent.Timestamp
        };

        sb.AppendLine($"data: {System.Text.Json.JsonSerializer.Serialize(data)}");
        sb.AppendLine();

        return sb.ToString();
    }
}

/// <summary>
/// Agent执行请求
/// </summary>
public class AgentExecuteRequest
{
    /// <summary>
    /// 用户输入
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// 最大迭代次数
    /// </summary>
    public int? MaxIterations { get; set; } = 10;
}

/// <summary>
/// Agent执行响应
/// </summary>
public class AgentExecuteResponse
{
    public Guid SessionId { get; set; }
    public string FinalAnswer { get; set; } = string.Empty;
    public List<AgentStepDto> Steps { get; set; } = new();
    public int TotalSteps { get; set; }
}

public class AgentStepDto
{
    public int StepIndex { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Thought { get; set; }
    public string? ToolName { get; set; }
    public Dictionary<string, object>? ToolArguments { get; set; }
    public string? Observation { get; set; }
    public DateTime Timestamp { get; set; }
}
