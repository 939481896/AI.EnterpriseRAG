using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// 向量库接口（企业级多向量库抽象）
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// 初始化向量库
    /// </summary>
    /// <returns></returns>
    Task<string> InitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入分块向量
    /// </summary>
    /// <param name="chunk">文档分块</param>
    /// <param name="vector">向量数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task InsertAsync(DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default);

    /// <summary>
    /// 相似性检索
    /// </summary>
    /// <param name="queryVector">查询向量</param>
    /// <param name="topK">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的分块</returns>
    Task<List<DocumentChunk>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据文档ID删除对应向量（单文档）
    /// </summary>
    /// <param name="documentId">文档ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除指定文档ID的向量
    /// </summary>
    /// <param name="documentIds">文档ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BatchDeleteByDocumentIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清空整个向量库（谨慎使用）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除过期向量（需DocumentChunk包含CreateTime字段）
    /// </summary>
    /// <param name="expireTime">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteExpiredAsync(DateTime expireTime, CancellationToken cancellationToken = default);
}