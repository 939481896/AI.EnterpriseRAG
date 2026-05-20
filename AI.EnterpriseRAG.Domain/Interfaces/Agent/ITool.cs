using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Agent;

/// <summary>
/// 工具接口（所有Agent工具的基类）
/// </summary>
public interface ITool
{
    /// <summary>
    /// 工具唯一名称（用于LLM识别）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 工具描述（告诉LLM何时使用）
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 工具参数Schema（JSON Schema格式）
    /// </summary>
    string ParametersSchema { get; }

    /// <summary>
    /// 工具分类（rag/data/system/external）
    /// </summary>
    string Category { get; }

    /// <summary>
    /// 是否需要鉴权
    /// </summary>
    bool RequiresAuth { get; }

    /// <summary>
    /// 执行工具
    /// </summary>
    /// <param name="arguments">工具参数（JSON反序列化后的字典）</param>
    /// <param name="context">执行上下文（包含UserId、TenantId等）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>工具执行结果</returns>
    Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具执行上下文
/// </summary>
public class ToolExecutionContext
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public string TenantId { get; set; } = "default";

    /// <summary>
    /// 会话ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// 额外上下文数据
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// 工具执行结果
/// </summary>
public class ToolResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 结果数据（JSON序列化字符串）
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static ToolResult Success(string data, long durationMs = 0)
        => new() { IsSuccess = true, Data = data, DurationMs = durationMs };

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static ToolResult Failure(string errorMessage)
        => new() { IsSuccess = false, ErrorMessage = errorMessage };
}
