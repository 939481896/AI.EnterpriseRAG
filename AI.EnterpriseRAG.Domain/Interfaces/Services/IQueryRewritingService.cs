namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// Query rewriting service for improving retrieval accuracy
/// </summary>
public interface IQueryRewritingService
{
    /// <summary>
    /// HyDE: Generate hypothetical document that would answer the question
    /// Improves retrieval accuracy by 20-30%
    /// </summary>
    Task<string> GenerateHypotheticalDocumentAsync(
        string query, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Multi-Query: Generate similar questions for better recall
    /// </summary>
    Task<List<string>> GenerateSimilarQueriesAsync(
        string originalQuery, 
        int count = 3, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Query Decomposition: Break complex questions into sub-questions
    /// </summary>
    Task<List<string>> DecomposeQueryAsync(
        string complexQuery, 
        CancellationToken ct = default);
}
