namespace AI.EnterpriseRAG.Infrastructure.Configurations;

/// <summary>
/// 向量库配置选项
/// </summary>
public class VectorStoreOptions
{
    /// <summary>
    /// 默认向量库类型
    /// </summary>
    public string DefaultType { get; set; } = "Chroma";

    /// <summary>
    /// Chroma配置
    /// </summary>
    public ChromaOptions Chroma { get; set; } = new();

    public QdrantConfigOptions Qdrant { get; set; } = new();

    public UnstructuredOptions Unstructured { get; set; }

}

public class QdrantConfigOptions
{
    public string? BaseUrl { get; set; }
    public int? Timeout { get; set; }
    public string? CollectionName { get; set; }
    public int? VectorSize { get; set; }
    public string? DistanceMetric { get; set; }
    public int? RetryCount { get; set; }
    public int? RetryDelayMilliseconds { get; set; }
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


// 配置
public class UnstructuredOptions
{
    public string ApiUrl { get; set; } = "http://localhost:8000";
    public string ApiKey { get; set; } = "your_secure_api_key_here";
}