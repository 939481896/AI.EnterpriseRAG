using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Agent;

/// <summary>
/// Agent编排引擎接口（ReAct/Plan-Solve）
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// 执行Agent任务（流式返回）
    /// </summary>
    /// <param name="userInput">用户输入</param>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="maxIterations">最大迭代次数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行步骤流</returns>
    IAsyncEnumerable<AgentStepEvent> ExecuteAsync(
        string userInput,
        string userId,
        string tenantId = "default",
        int maxIterations = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话历史
    /// </summary>
    Task<AgentSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Agent步骤事件（用于流式传输）
/// </summary>
public class AgentStepEvent
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    public AgentEventType EventType { get; set; }

    /// <summary>
    /// 步骤索引
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// 思考内容
    /// </summary>
    public string? Thought { get; set; }

    /// <summary>
    /// 工具调用信息
    /// </summary>
    public ToolCallInfo? ToolCall { get; set; }

    /// <summary>
    /// 工具执行结果
    /// </summary>
    public string? Observation { get; set; }

    /// <summary>
    /// 最终答案
    /// </summary>
    public string? FinalAnswer { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 工具调用信息
/// </summary>
public class ToolCallInfo
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// Agent事件类型
/// </summary>
public enum AgentEventType
{
    /// <summary>
    /// 会话开始
    /// </summary>
    SessionStarted,

    /// <summary>
    /// 意图识别
    /// </summary>
    IntentRecognized,

    /// <summary>
    /// 思考过程
    /// </summary>
    Thinking,

    /// <summary>
    /// 工具调用
    /// </summary>
    ToolCalling,

    /// <summary>
    /// 观察结果
    /// </summary>
    Observation,

    /// <summary>
    /// 最终答案
    /// </summary>
    FinalAnswer,

    /// <summary>
    /// 错误
    /// </summary>
    Error,

    /// <summary>
    /// 会话完成
    /// </summary>
    SessionCompleted
}
