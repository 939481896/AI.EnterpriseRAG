using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// Hybrid Search Service: Combines Vector + BM25 for better accuracy
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Perform hybrid search (Vector + BM25 + RRF Fusion)
    /// </summary>
    /// <param name="query">Query text (for BM25)</param>
    /// <param name="collectionName">Vector collection name</param>
    /// <param name="queryVector">Query embedding (for vector search)</param>
    /// <param name="filter">Permission/tenant filters</param>
    /// <param name="topK">Final result count</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Fused results ranked by RRF score</returns>
    Task<List<DocumentChunk>> SearchAsync(
        string query,
        string collectionName,
        float[] queryVector,
        Dictionary<string, object>? filter,
        int topK,
        CancellationToken ct);
}
