using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Repositories;

/// <summary>
/// 文档仓储接口（企业级仓储模式）
/// </summary>
public interface IDocumentRepository
{
    Task<Document> GetByIdAsync(Guid id);
    Task AddAsync(Document document);
    Task UpdateAsync(Document document);
    Task AddChunkAsync(DocumentChunk chunk);
    Task<List<DocumentChunk>> GetChunksByIdsAsync(List<Guid> ids);

    /// <summary>
    /// 根据文件哈希查询文档（用于重复检测）
    /// </summary>
    /// <param name="fileHash">文件MD5哈希</param>
    /// <param name="uploadedBy">上传者（可选，用于多用户隔离）</param>
    /// <param name="tenantId">租户ID（可选，用于多租户隔离）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已存在的文档，如果不存在返回null</returns>
    Task<Document?> GetByFileHashAsync(
        string fileHash, 
        string? uploadedBy = null, 
        string? tenantId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文档的所有分块
    /// </summary>
    Task DeleteChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据状态查询文档（用于恢复未完成的文档）
    /// </summary>
    Task<IEnumerable<Document>> GetByStatusAsync(
        Domain.Enums.DocumentStatus status, 
        CancellationToken cancellationToken = default);
}