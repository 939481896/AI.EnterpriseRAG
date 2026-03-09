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
}