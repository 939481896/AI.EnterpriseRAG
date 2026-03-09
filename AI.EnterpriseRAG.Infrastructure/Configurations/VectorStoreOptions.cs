namespace AI.EnterpriseRAG.Infrastructure.Configurations;

/// <summary>
/// 向量库配置选项
/// </summary>
public class VectorStoreOptions
{
    /// <summary>
    /// 默认向量库类型
    /// </summary>
    public string DefaultType { get; set; } = "chroma";

    /// <summary>
    /// Chroma配置
    /// </summary>
    public ChromaOptions Chroma { get; set; } = new();
}

public class ChromaOptions
{
    public string? BaseUrl { get; set; }
    public int? Timeout { get; set; }
    public string? Tenant { get; set; }
    public string? Database { get; set; }
    public string? CollectionId { get; set; }
    public string? CollectionName { get; set; }
    public int? RetryCount { get; set; }
    public int? RetryDelayMilliseconds { get; set; }
}