using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent;

/// <summary>
/// ReAct Agent编排引擎实现
/// </summary>
public class ReactAgentOrchestrator : IAgentOrchestrator
{
    private readonly ILlmService _llmService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IIntentRecognitionService _intentService;
    private readonly ILogger<ReactAgentOrchestrator> _logger;

    // 存储会话的临时内存（生产环境应用Redis或数据库）
    private readonly Dictionary<Guid, AgentSession> _sessions = new();

    public ReactAgentOrchestrator(
        ILlmService llmService,
        IToolRegistry toolRegistry,
        IIntentRecognitionService intentService,
        ILogger<ReactAgentOrchestrator> logger)
    {
        _llmService = llmService;
        _toolRegistry = toolRegistry;
        _intentService = intentService;
        _logger = logger;
    }

    public async IAsyncEnumerable<AgentStepEvent> ExecuteAsync(
        string userInput,
        string userId,
        string tenantId = "default",
        int maxIterations = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();
        var session = new AgentSession
        {
            Id = sessionId,
            UserId = userId,
            TenantId = tenantId,
            UserIntent = userInput,
            StartTime = DateTime.UtcNow,
            Status = AgentStatus.Running
        };

        _sessions[sessionId] = session;

        // 发送会话开始事件
        yield return new AgentStepEvent
        {
            SessionId = sessionId,
            EventType = AgentEventType.SessionStarted,
            Timestamp = DateTime.UtcNow
        };

        // 使用内部方法处理，捕获异常并作为事件返回
        AgentStepEvent? errorEvent = null;
        await foreach (var stepEvent in ExecuteInternalAsync(session, userInput, userId, tenantId, maxIterations, cancellationToken)
            .ConfigureAwait(false)
            .WithCancellation(cancellationToken))
        {
            // 如果已经有错误，停止迭代
            if (errorEvent != null)
                break;

            // 检查是否是错误事件
            if (stepEvent.EventType == AgentEventType.Error)
            {
                errorEvent = stepEvent;
            }

            yield return stepEvent;
        }

        // 处理意外异常（ExecuteInternalAsync外部）
        if (session.Status == AgentStatus.Running)
        {
            session.Status = AgentStatus.Failed;
            session.EndTime = DateTime.UtcNow;
        }
    }

    private async IAsyncEnumerable<AgentStepEvent> ExecuteInternalAsync(
        AgentSession session,
        string userInput,
        string userId,
        string tenantId,
        int maxIterations,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sessionId = session.Id;

        // 1. 意图识别
        var intent = await _intentService.RecognizeAsync(userInput, null, cancellationToken);
        session.IntentType = intent.Type.ToString();

        yield return new AgentStepEvent
        {
            SessionId = sessionId,
            EventType = AgentEventType.IntentRecognized,
            Thought = $"识别意图: {intent.Type} (置信度: {intent.Confidence:F2})",
            Timestamp = DateTime.UtcNow
        };

        // 2. ReAct循环
        var context = new AgentContext
        {
            UserInput = userInput,
            Intent = intent,
            History = new List<string>()
        };

        int iteration = 0;
        while (iteration < maxIterations)
        {
            iteration++;
            var stepIndex = iteration;

            // 2.1 Thought - LLM思考下一步
            var thoughtPrompt = BuildReActPrompt(context, _toolRegistry.GenerateToolsPrompt());
            var thoughtResponse = await _llmService.ChatAsync(thoughtPrompt, cancellationToken);

            yield return new AgentStepEvent
            {
                SessionId = sessionId,
                EventType = AgentEventType.Thinking,
                StepIndex = stepIndex,
                Thought = thoughtResponse,
                Timestamp = DateTime.UtcNow
            };

            // 2.2 解析Action或FinalAnswer
            var actionInfo = ParseActionFromThought(thoughtResponse);

            if (actionInfo.IsFinalAnswer)
            {
                // 找到最终答案
                session.FinalAnswer = actionInfo.FinalAnswer;
                session.Status = AgentStatus.Completed;
                session.EndTime = DateTime.UtcNow;
                session.TotalCostSeconds = (decimal)(session.EndTime.Value - session.StartTime).TotalSeconds;

                yield return new AgentStepEvent
                {
                    SessionId = sessionId,
                    EventType = AgentEventType.FinalAnswer,
                    StepIndex = stepIndex,
                    FinalAnswer = actionInfo.FinalAnswer,
                    Timestamp = DateTime.UtcNow
                };

                break;
            }

            if (actionInfo.ToolName == null)
            {
                // LLM没有给出明确的工具调用，可能需要重新提示
                context.History.Add($"Iteration {iteration}: LLM未给出明确工具调用，请重新思考");
                continue;
            }

            // 2.3 Action - 执行工具
            var tool = _toolRegistry.GetTool(actionInfo.ToolName);
            if (tool == null)
            {
                context.History.Add($"错误: 工具 '{actionInfo.ToolName}' 不存在");
                continue;
            }

            yield return new AgentStepEvent
            {
                SessionId = sessionId,
                EventType = AgentEventType.ToolCalling,
                StepIndex = stepIndex,
                ToolCall = new ToolCallInfo
                {
                    ToolName = actionInfo.ToolName,
                    Arguments = actionInfo.Arguments
                },
                Timestamp = DateTime.UtcNow
            };

            var toolContext = new ToolExecutionContext
            {
                UserId = userId,
                TenantId = tenantId,
                SessionId = sessionId
            };

            var toolResult = await tool.ExecuteAsync(actionInfo.Arguments, toolContext, cancellationToken);

            // 2.4 Observation - 记录工具执行结果
            var observation = toolResult.IsSuccess
                ? $"工具执行成功: {toolResult.Data}"
                : $"工具执行失败: {toolResult.ErrorMessage}";

            context.History.Add($"Action: {actionInfo.ToolName}({JsonSerializer.Serialize(actionInfo.Arguments)})");
            context.History.Add($"Observation: {observation}");

            yield return new AgentStepEvent
            {
                SessionId = sessionId,
                EventType = AgentEventType.Observation,
                StepIndex = stepIndex,
                Observation = observation,
                Timestamp = DateTime.UtcNow
            };

            // 保存步骤到会话
            session.Steps.Add(new AgentStep
            {
                SessionId = sessionId,
                StepType = "action",
                StepIndex = stepIndex,
                Thought = thoughtResponse,
                ToolName = actionInfo.ToolName,
                ToolArguments = JsonSerializer.Serialize(actionInfo.Arguments),
                ToolResult = observation,
                IsSuccess = toolResult.IsSuccess,
                DurationMs = toolResult.DurationMs
            });
        }

        // 如果达到最大迭代次数仍未完成
        if (iteration >= maxIterations && session.Status == AgentStatus.Running)
        {
            session.Status = AgentStatus.Timeout;
            session.FinalAnswer = "任务执行超时，请简化问题或增加迭代次数";
            session.EndTime = DateTime.UtcNow;

            yield return new AgentStepEvent
            {
                SessionId = sessionId,
                EventType = AgentEventType.Error,
                ErrorMessage = "达到最大迭代次数",
                Timestamp = DateTime.UtcNow
            };
        }

        // 发送会话完成事件
        yield return new AgentStepEvent
        {
            SessionId = sessionId,
            EventType = AgentEventType.SessionCompleted,
            FinalAnswer = session.FinalAnswer,
            Timestamp = DateTime.UtcNow
        };
    }

    public Task<AgentSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    private string BuildReActPrompt(AgentContext context, string toolsPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是一个企业级AI智能体，能够使用工具完成复杂任务。请按照以下格式思考和行动：");
        sb.AppendLine();
        sb.AppendLine("## ReAct格式");
        sb.AppendLine("Thought: 分析当前情况，思考下一步");
        sb.AppendLine("Action: tool_name");
        sb.AppendLine("Action Input: {\"param1\": \"value1\"}");
        sb.AppendLine("或者");
        sb.AppendLine("Final Answer: 给出最终答案");
        sb.AppendLine();
        sb.AppendLine("## 可用工具");
        sb.AppendLine(toolsPrompt);
        sb.AppendLine();
        sb.AppendLine($"## 用户问题");
        sb.AppendLine(context.UserInput);
        sb.AppendLine();

        if (context.History.Any())
        {
            sb.AppendLine("## 执行历史");
            foreach (var item in context.History)
            {
                sb.AppendLine(item);
            }
            sb.AppendLine();
        }

        sb.AppendLine("请严格按照格式输出你的思考和行动：");

        return sb.ToString();
    }

    private ActionParseResult ParseActionFromThought(string thought)
    {
        // 检查是否是Final Answer
        var finalAnswerMatch = Regex.Match(thought, @"Final Answer:\s*(.+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (finalAnswerMatch.Success)
        {
            return new ActionParseResult
            {
                IsFinalAnswer = true,
                FinalAnswer = finalAnswerMatch.Groups[1].Value.Trim()
            };
        }

        // 解析Action
        var actionMatch = Regex.Match(thought, @"Action:\s*(\w+)", RegexOptions.IgnoreCase);
        var actionInputMatch = Regex.Match(thought, @"Action Input:\s*(\{.+?\})", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (actionMatch.Success)
        {
            var toolName = actionMatch.Groups[1].Value.Trim();
            var arguments = new Dictionary<string, object>();

            if (actionInputMatch.Success)
            {
                try
                {
                    var json = actionInputMatch.Groups[1].Value;
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    if (parsed != null)
                    {
                        foreach (var kvp in parsed)
                        {
                            arguments[kvp.Key] = kvp.Value.ValueKind switch
                            {
                                JsonValueKind.String => kvp.Value.GetString()!,
                                JsonValueKind.Number => kvp.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => kvp.Value.GetRawText()
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "解析Action Input失败: {Input}", actionInputMatch.Groups[1].Value);
                }
            }

            return new ActionParseResult
            {
                ToolName = toolName,
                Arguments = arguments
            };
        }

        return new ActionParseResult();
    }

    private class AgentContext
    {
        public string UserInput { get; set; } = "";
        public IntentResult Intent { get; set; } = null!;
        public List<string> History { get; set; } = new();
    }

    private class ActionParseResult
    {
        public bool IsFinalAnswer { get; set; }
        public string FinalAnswer { get; set; } = "";
        public string? ToolName { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new();
    }
}
