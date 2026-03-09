namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// 大模型服务接口（企业级多模型抽象）
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// 模型名称
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// 生成回答
    /// </summary>
    /// <param name="prompt">提示词</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回答内容</returns>
    Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成文本向量
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>向量数组</returns>
    Task<float[]> EmbeddingAsync(string text, CancellationToken cancellationToken = default);
}