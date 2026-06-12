using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// Agent controller
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class AgentController : BaseApiController
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
    /// Execute Agent task (SSE streaming response)
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    public async Task ExecuteAgent(
        [FromBody] AgentExecuteRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = GetCurrentUser();
        if (user == null || !user.IsAuthenticated)
        {
            _logger.LogWarning("User not authenticated");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsync($"event: error\ndata: {{\"message\":\"{MessageResources.Agent.UserNotAuthenticated}\"}}\n\n", cancellationToken);
            return;
        }

        var userId = user.UserId;
        var tenantId = user.TenantId;

        _logger.LogInformation(
            "User {UserId} started Agent task: {Input}",
            userId,
            request.Input);

        // Set SSE response headers
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
                // Convert to SSE format
                var sseData = FormatSseEvent(stepEvent);
                await Response.WriteAsync(sseData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                // Short delay to ensure client receives data
                await Task.Delay(50, cancellationToken);
            }

            // Send completion marker
            await Response.WriteAsync($"event: done\ndata: {{\"status\":\"completed\",\"message\":\"{MessageResources.Agent.ExecutionCompleted}\"}}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution error");
            var errorData = $"event: error\ndata: {{\"message\":\"{MessageResources.Agent.ExecutionFailed}: {ex.Message}\"}}\n\n";
            await Response.WriteAsync(errorData, cancellationToken);
        }
    }

    /// <summary>
    /// Execute Agent task (synchronous JSON response)
    /// </summary>
    [HttpPost("execute-sync")]
    [ProducesResponseType(typeof(Result<AgentExecuteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteAgentSync(
        [FromBody] AgentExecuteRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = GetCurrentUser();
        if (user == null || !user.IsAuthenticated)
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail(MessageResources.Agent.UserNotAuthenticated));
        }

        var userId = user.UserId;
        var tenantId = user.TenantId;

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

            var response = new AgentExecuteResponseDto
            {
                SessionId = sessionId ?? Guid.Empty,
                FinalAnswer = finalAnswer ?? "No answer generated",
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
            };

            return Ok(Result<AgentExecuteResponseDto>.SuccessResult(response, MessageResources.Agent.ExecutionCompleted));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent execution failed");
            return StatusCode(500, Result.Fail($"{MessageResources.Agent.ExecutionFailed}: {ex.Message}", 500));
        }
    }

    /// <summary>
    /// Get session history
    /// </summary>
    [HttpGet("session/{sessionId}")]
    [ProducesResponseType(typeof(Result<AgentSessionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _agentOrchestrator.GetSessionAsync(sessionId, cancellationToken);

            if (session == null)
                return NotFound(Result.Fail(MessageResources.Agent.SessionNotFound, 404));

            var response = new AgentSessionResponseDto
            {
                SessionId = session.Id,
                UserId = session.UserId,
                Intent = session.UserIntent,
                IntentType = session.IntentType,
                Status = session.Status.ToString(),
                FinalAnswer = session.FinalAnswer,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                TotalCostSeconds = session.TotalCostSeconds,
                Steps = session.Steps.Select(s => new AgentSessionStepDto
                {
                    StepIndex = s.StepIndex,
                    StepType = s.StepType,
                    ToolName = s.ToolName,
                    ToolArguments = s.ToolArguments,
                    ToolResult = s.ToolResult,
                    IsSuccess = s.IsSuccess,
                    DurationMs = s.DurationMs
                }).ToList()
            };

            return Ok(Result<AgentSessionResponseDto>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve session {SessionId}", sessionId);
            return StatusCode(500, Result.Fail($"Failed to retrieve session: {ex.Message}", 500));
        }
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
