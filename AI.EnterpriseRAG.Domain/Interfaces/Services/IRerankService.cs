using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

public interface IRerankService
{
    Task<List<DocumentChunk>> RerankAsync(string query, List<DocumentChunk> chunks, int take = 3, CancellationToken ct = default);
}