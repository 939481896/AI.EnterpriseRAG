using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AI.EnterpriseRAG.Infrastructure.Services;

/// <summary>
/// HyDE: Hypothetical Document Embeddings
/// Improves retrieval accuracy by 20-30%
/// ✅ Now uses centralized PromptService
/// </summary>
public class QueryRewritingService : IQueryRewritingService
{
    private readonly ILlmService _llm;
    private readonly IPromptService _promptService;
    private readonly ILogger<QueryRewritingService> _logger;

    public QueryRewritingService(
        ILlmService llm,
        IPromptService promptService,
        ILogger<QueryRewritingService> logger)
    {
        _llm = llm;
        _promptService = promptService;
        _logger = logger;
    }

    /// <summary>
    /// HyDE: Generate hypothetical document that would answer the question
    /// </summary>
    public async Task<string> GenerateHypotheticalDocumentAsync(
        string query,
        CancellationToken ct)
    {
        // ✅ Use centralized prompt
        var prompt = _promptService.GetHydePrompt(query, maxLength: 500);

        var hypotheticalDoc = await _llm.ChatAsync(prompt, ct);

        _logger.LogDebug("HyDE query rewriting: {Query} → {HypoDoc}", 
            query, hypotheticalDoc.Substring(0, Math.Min(100, hypotheticalDoc.Length)));

        return hypotheticalDoc;
    }

    /// <summary>
    /// Multi-Query: Generate similar questions for better recall
    /// </summary>
    public async Task<List<string>> GenerateSimilarQueriesAsync(
        string originalQuery,
        int count,
        CancellationToken ct)
    {
        // ✅ Use centralized prompt
        var prompt = _promptService.GetMultiQueryPrompt(originalQuery, count);

        var response = await _llm.ChatAsync(prompt, ct);
        var queries = ParseQueries(response);

        queries.Insert(0, originalQuery); // Include original

        _logger.LogDebug("Multi-query expansion: {Count} queries generated", queries.Count);

        return queries;
    }
    
    /// <summary>
    /// Query Decomposition: Break complex questions into sub-questions
    /// </summary>
    public async Task<List<string>> DecomposeQueryAsync(
        string complexQuery,
        CancellationToken ct)
    {
        var prompt = $@"Break down this complex question into simpler sub-questions:

Question: {complexQuery}

Sub-questions:
1.";

        var response = await _llm.ChatAsync(prompt, ct);
        var subQueries = ParseQueries(response);
        
        _logger.LogDebug("Query decomposition: {Original} → {Count} sub-queries", 
            complexQuery, subQueries.Count);
        
        return subQueries;
    }
    
    private List<string> ParseQueries(string response)
    {
        return response
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line))
            .Select(line => System.Text.RegularExpressions.Regex.Replace(line, @"^\d+\.\s*", ""))
            .Where(line => !string.IsNullOrEmpty(line))
            .ToList();
    }
}
