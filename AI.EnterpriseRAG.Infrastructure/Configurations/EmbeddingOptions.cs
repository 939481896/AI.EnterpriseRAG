// ============================================
// 多Embedding模型配置类
// ============================================

namespace AI.EnterpriseRAG.Infrastructure.Configurations;

/// <summary>
/// Embedding模型配置
/// </summary>
public class EmbeddingOptions
{
    /// <summary>
    /// 默认Provider
    /// </summary>
    public string DefaultProvider { get; set; } = "nomic";
    
    /// <summary>
    /// 所有可用的Provider
    /// </summary>
    public Dictionary<string, EmbeddingProviderConfig> Providers { get; set; } = new();
}

/// <summary>
/// 单个Provider的配置
/// </summary>
public class EmbeddingProviderConfig
{
    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// 向量维度
    /// </summary>
    public int Dimension { get; set; }
    
    /// <summary>
    /// API地址
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API密钥（可选）
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// 最大Token数
    /// </summary>
    public int MaxTokens { get; set; } = 8192;
    
    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// 提供商类型（ollama/openai/azure等）
    /// </summary>
    public string ProviderType { get; set; } = "ollama";
}

/// <summary>
/// Qdrant Collection配置（支持多Collection）
/// </summary>
public class QdrantCollectionConfig
{
    public string Name { get; set; } = string.Empty;
    public int VectorSize { get; set; }
    public string EmbeddingProvider { get; set; } = string.Empty;
    public string DistanceMetric { get; set; } = "Cosine";
}

/// <summary>
/// 扩展的Qdrant配置
/// </summary>
public class QdrantConfigOptionsExtended : QdrantConfigOptions
{
    /// <summary>
    /// 多Collection配置
    /// </summary>
    public Dictionary<string, QdrantCollectionConfig> Collections { get; set; } = new();
}
