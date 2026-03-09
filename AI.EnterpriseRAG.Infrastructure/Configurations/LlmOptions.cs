namespace AI.EnterpriseRAG.Infrastructure.Configurations;

/// <summary>
/// 大模型配置选项（企业级配置化）
/// </summary>
public class LlmOptions
{
    /// <summary>
    /// 默认模型
    /// </summary>
    public string DefaultModel { get; set; } = "ollama";

    /// <summary>
    /// Ollama配置
    /// </summary>
    public OllamaOptions Ollama { get; set; } = new();

    /// <summary>
    /// 通义千问配置
    /// </summary>
    public TongyiOptions Tongyi { get; set; } = new();
}

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "qwen3:8b";
    public string EmbeddingModelName { get; set; } = "nomic-embed-text";
}

public class TongyiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "qwen-turbo";
}