namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// Agent会话实体（记录智能体执行全链路）
/// </summary>
public class AgentSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID（多租户隔离）
    /// </summary>
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// 用户原始意图
    /// </summary>
    public string UserIntent { get; set; } = string.Empty;

    /// <summary>
    /// 识别的意图类型（rag_search/data_analysis/troubleshooting）
    /// </summary>
    public string IntentType { get; set; } = string.Empty;

    /// <summary>
    /// 执行计划（JSON存储Plan步骤）
    /// </summary>
    public string ExecutionPlan { get; set; } = string.Empty;

    /// <summary>
    /// 执行状态（running/completed/failed）
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Running;

    /// <summary>
    /// 最终答案
    /// </summary>
    public string FinalAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 执行步骤集合
    /// </summary>
    public virtual ICollection<AgentStep> Steps { get; set; } = new List<AgentStep>();

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 总耗时（秒）
    /// </summary>
    public decimal TotalCostSeconds { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Agent执行步骤（ReAct链路追踪）
/// </summary>
public class AgentStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属会话ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 步骤类型（thought/action/observation/final_answer）
    /// </summary>
    public string StepType { get; set; } = string.Empty;

    /// <summary>
    /// 步骤序号
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// 思考内容（LLM推理过程）
    /// </summary>
    public string? Thought { get; set; }

    /// <summary>
    /// 工具名称
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// 工具参数（JSON）
    /// </summary>
    public string? ToolArguments { get; set; }

    /// <summary>
    /// 工具执行结果（Observation）
    /// </summary>
    public string? ToolResult { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联会话
    /// </summary>
    public virtual AgentSession Session { get; set; } = null!;
}

/// <summary>
/// Agent执行状态枚举
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// 运行中
    /// </summary>
    Running = 0,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 执行失败
    /// </summary>
    Failed = 2,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout = 3,

    /// <summary>
    /// 用户取消
    /// </summary>
    Cancelled = 4
}
