using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppEnterpriseAiContext _dbContext;

    public DocumentRepository(AppEnterpriseAiContext dbContext)
    {  _dbContext = dbContext; }
    public async Task AddAsync(Document document)
    {
        await _dbContext.Documents.AddAsync(document);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddChunkAsync(DocumentChunk chunk)
    {
        await _dbContext.DocumentChunks.AddAsync(chunk);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Document> GetByIdAsync(Guid id)
    {
        return await _dbContext.Documents.Include(document => document.Chunks).FirstOrDefaultAsync(document => document.Id == id)
            ?? throw new KeyNotFoundException($"文档{id}不存在");
    }

    public async Task<List<DocumentChunk>> GetChunksByIdsAsync(List<Guid> ids)
    {
        return await _dbContext.DocumentChunks.Where(c=>ids.Contains(c.Id)).ToListAsync();
    }

    public async Task UpdateAsync(Document document)
    {
        _dbContext.Documents.Update(document);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 根据文件哈希查询文档（用于重复检测）
    /// </summary>
    public async Task<Document?> GetByFileHashAsync(
        string fileHash, 
        string? uploadedBy = null, 
        string? tenantId = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Documents
            .Where(d => d.FileHash == fileHash);

        // 如果指定了上传者，只查询该用户的文档
        if (!string.IsNullOrEmpty(uploadedBy))
        {
            query = query.Where(d => d.UploadedBy == uploadedBy);
        }

        // 如果指定了租户，只查询该租户的文档
        if (!string.IsNullOrEmpty(tenantId))
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 删除文档的所有分块
    /// </summary>
    public async Task DeleteChunksByDocumentIdAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default)
    {
        var chunks = await _dbContext.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        _dbContext.DocumentChunks.RemoveRange(chunks);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 根据状态查询文档（用于恢复未完成的文档）
    /// </summary>
    public async Task<IEnumerable<Document>> GetByStatusAsync(
        DocumentStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .Where(d => d.Status == status)
            .OrderBy(d => d.CreateTime)
            .ToListAsync(cancellationToken);
    }
}
