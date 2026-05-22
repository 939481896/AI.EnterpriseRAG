namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 应用日志实体（结构化日志存储）
/// </summary>
public class ApplicationLog
{
    public long Id { get; set; }

    /// <summary>
    /// 日志级别（Debug/Information/Warning/Error/Critical）
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 消息模板
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// TraceId（请求追踪ID）
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// SpanId（操作ID）
    /// </summary>
    public string? SpanId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// 属性（JSON格式）
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 来源（类名+方法名）
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// 线程ID
    /// </summary>
    public int? ThreadId { get; set; }

    /// <summary>
    /// 机器名
    /// </summary>
    public string? MachineName { get; set; }
}

/// <summary>
/// 请求日志实体（API请求审计）
/// </summary>
public class RequestLog
{
    public long Id { get; set; }

    /// <summary>
    /// TraceId
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 请求方法
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// 查询字符串
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// 请求体（POST/PUT）
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>
    /// 响应体
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Referer
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// 请求开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 请求结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
