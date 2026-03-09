using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppContext _dbContext;

    public DocumentRepository(AppContext dbContext)
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
}
