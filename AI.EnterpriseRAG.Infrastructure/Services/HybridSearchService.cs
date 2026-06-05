using AI.EnterpriseRAG.Core.Configuration;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AI.EnterpriseRAG.Infrastructure.Services;

/// <summary>
/// Hybrid Search: Vector + BM25 with RRF (Reciprocal Rank Fusion)
/// Combines semantic understanding with exact keyword matching
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    private readonly IVectorStore _vectorStore;
    private readonly AppEnterpriseAiContext _context;
    private readonly ILogger<HybridSearchService> _logger;
    private readonly HybridSearchOptions _options;

    public HybridSearchService(
        IVectorStore vectorStore,
        AppEnterpriseAiContext context,
        ILogger<HybridSearchService> logger,
        IOptions<HybridSearchOptions> options)
    {
        _vectorStore = vectorStore;
        _context = context;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<List<DocumentChunk>> SearchAsync(
        string query,
        string collectionName,
        float[] queryVector,
        Dictionary<string, object>? filter,
        int topK,
        CancellationToken ct)
    {
        _logger.LogInformation("🔍 Hybrid Search | Query: {Query} | TopK: {TopK}", query, topK);

        // 1. Vector Search (Semantic similarity)
        var vectorResults = await _vectorStore.SearchAsync(
            collectionName, queryVector, topK * 3, filter, ct);
        _logger.LogDebug("📊 Vector results: {Count}", vectorResults.Count);

        // 2. BM25 Search (Keyword matching)
        var bm25Results = await BM25SearchAsync(query, filter, topK * 3, ct);
        _logger.LogDebug("📊 BM25 results: {Count}", bm25Results.Count);

        // 3. Reciprocal Rank Fusion
        var fusedResults = ReciprocalRankFusion(vectorResults, bm25Results, topK);

        _logger.LogInformation("✅ Hybrid fusion complete | Final: {Count}", fusedResults.Count);
        return fusedResults;
    }

    /// <summary>
    /// BM25 Keyword Search Implementation (Simplified)
    /// Uses SQL LIKE for basic matching, can upgrade to MySQL FULLTEXT or Lucene.NET
    /// </summary>
    private async Task<List<DocumentChunk>> BM25SearchAsync(
        string query,
        Dictionary<string, object>? filter,
        int topK,
        CancellationToken ct)
    {
        try
        {
            // Tokenize query (simple Chinese/English tokenization)
            var queryTerms = TokenizeQuery(query);
            if (!queryTerms.Any())
            {
                _logger.LogWarning("No valid query terms for BM25");
                return new List<DocumentChunk>();
            }

            _logger.LogDebug("BM25 query terms: {Terms}", string.Join(", ", queryTerms.Take(10)));

            // Get candidate chunks (apply document-level filters if provided)
            var chunksQuery = _context.DocumentChunks.AsNoTracking().AsQueryable();

            if (filter != null && filter.ContainsKey("document_id"))
            {
                var docIds = ExtractDocumentIds(filter["document_id"]);
                if (docIds.Any())
                {
                    chunksQuery = chunksQuery.Where(c => docIds.Contains(c.DocumentId));
                    _logger.LogDebug("BM25 filtering by {Count} documents", docIds.Count);
                }
            }

            // 🔧 FIX: Fetch chunks that match at least ONE query term (Chinese character matching)
            // Build SQL LIKE clauses for Chinese characters
            var candidates = new List<DocumentChunk>();

            // For Chinese text, check if chunk contains any of the query characters
            var chineseTerms = queryTerms.Where(t => t.Length == 1 && t[0] >= 0x4e00 && t[0] <= 0x9fa5).ToList();

            if (chineseTerms.Any())
            {
                // Fetch chunks containing Chinese terms
                foreach (var term in chineseTerms.Take(5)) // Limit to first 5 terms for performance
                {
                    var matchingChunks = await chunksQuery
                        .Where(c => c.Content.Contains(term))
                        .Take(_options.Bm25MaxCandidates / 5)
                        .ToListAsync(ct);

                    candidates.AddRange(matchingChunks);

                    if (candidates.Count >= _options.Bm25MaxCandidates)
                        break;
                }

                // Deduplicate
                candidates = candidates.GroupBy(c => c.Id).Select(g => g.First()).ToList();

                _logger.LogDebug("BM25 found {Count} candidate chunks (from {Terms} Chinese terms)",
                    candidates.Count, chineseTerms.Count);
            }
            else
            {
                // Fallback: fetch recent chunks
                candidates = await chunksQuery
                    .OrderByDescending(c => c.CreateTime)
                    .Take(_options.Bm25MaxCandidates)
                    .ToListAsync(ct);

                _logger.LogDebug("BM25 fallback: fetched {Count} recent chunks", candidates.Count);
            }

            if (!candidates.Any())
            {
                _logger.LogDebug("BM25: No candidates found");
                return new List<DocumentChunk>();
            }

            // Calculate BM25 scores
            var avgDocLength = candidates.Average(c => c.Content.Length);
            var results = new List<(DocumentChunk Chunk, double Score)>();

            foreach (var chunk in candidates)
            {
                var score = CalculateBM25Score(chunk.Content, queryTerms, avgDocLength, candidates.Count);
                if (score > 0)
                {
                    results.Add((chunk, score));
                }
            }

            // Return top-K by BM25 score
            var topResults = results
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .Select(r =>
                {
                    r.Chunk.Similarity = (float)r.Score;  // Store BM25 score temporarily
                    return r.Chunk;
                })
                .ToList();

            _logger.LogDebug("BM25 matched {Count} chunks | Avg score: {AvgScore:F4}",
                topResults.Count, topResults.Any() ? topResults.Average(c => c.Similarity) : 0);

            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BM25 search failed, returning empty");
            return new List<DocumentChunk>();
        }
    }

    /// <summary>
    /// Calculate BM25 score for a document
    /// Formula: IDF(term) * (TF * (k1 + 1)) / (TF + k1 * (1 - b + b * (docLen / avgDocLen)))
    /// </summary>
    private double CalculateBM25Score(
        string document,
        List<string> queryTerms,
        double avgDocLength,
        int totalDocs)
    {
        var k1 = _options.Bm25K1;
        var b = _options.Bm25B;

        var docLength = document.Length;
        var docTerms = TokenizeQuery(document);
        var termFrequency = docTerms.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());

        double score = 0;

        foreach (var term in queryTerms)
        {
            if (!termFrequency.ContainsKey(term))
                continue;

            var tf = termFrequency[term];

            // IDF calculation (simplified, assumes 10% of docs contain term)
            var docFreq = Math.Max(1, totalDocs / 10.0);
            var idf = Math.Log((totalDocs - docFreq + 0.5) / (docFreq + 0.5) + 1.0);

            // BM25 formula
            var numerator = tf * (k1 + 1);
            var denominator = tf + k1 * (1 - b + b * (docLength / avgDocLength));

            score += idf * (numerator / denominator);
        }

        return score;
    }

    /// <summary>
    /// Simple tokenization (Chinese + English)
    /// Production: Use jieba for Chinese, NLTK for English
    /// </summary>
    private List<string> TokenizeQuery(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var terms = new List<string>();

        // Chinese character segmentation (每个汉字单独成词)
        var chineseChars = text.Where(c => c >= 0x4e00 && c <= 0x9fa5).Select(c => c.ToString());
        terms.AddRange(chineseChars);

        // Remove punctuation for English/mixed text
        var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"[^\w\s\u4e00-\u9fa5]", " ");

        // Split by whitespace for English words
        var words = cleaned
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length >= _options.MinTokenLength);

        terms.AddRange(words);

        var result = terms.Distinct().ToList();

        _logger.LogDebug("Tokenized '{Text}' → {Count} terms: {Sample}",
            text.Length > 20 ? text.Substring(0, 20) + "..." : text,
            result.Count,
            string.Join(", ", result.Take(5)));

        return result;
    }

    /// <summary>
    /// Reciprocal Rank Fusion: Merge vector + BM25 results
    /// RRF Score = Σ 1 / (k + rank)
    /// ⚠️ IMPORTANT: Preserves original similarity scores (0-1) for downstream filtering
    /// </summary>
    private List<DocumentChunk> ReciprocalRankFusion(
        List<DocumentChunk> vectorResults,
        List<DocumentChunk> bm25Results,
        int topK)
    {
        var k = _options.RrfK;
        var scoreMap = new Dictionary<Guid, double>();
        var originalScores = new Dictionary<Guid, float>(); // 🆕 Preserve original scores

        // Score from vector search
        for (int i = 0; i < vectorResults.Count; i++)
        {
            var chunkId = vectorResults[i].Id;
            scoreMap[chunkId] = scoreMap.GetValueOrDefault(chunkId) + 1.0 / (k + i + 1);

            // 🆕 Keep original vector similarity (0-1 range)
            if (!originalScores.ContainsKey(chunkId))
            {
                originalScores[chunkId] = vectorResults[i].Similarity;
            }
        }

        // Score from BM25
        for (int i = 0; i < bm25Results.Count; i++)
        {
            var chunkId = bm25Results[i].Id;
            scoreMap[chunkId] = scoreMap.GetValueOrDefault(chunkId) + 1.0 / (k + i + 1);

            // 🆕 Keep BM25 score if chunk not in vector results
            if (!originalScores.ContainsKey(chunkId))
            {
                // Normalize BM25 score to 0-1 range (BM25 typically 0-10)
                originalScores[chunkId] = Math.Min(bm25Results[i].Similarity / 10.0f, 1.0f);
            }
        }

        // Merge and rank by RRF score
        var allChunks = vectorResults.Concat(bm25Results)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        var fusedResults = allChunks
            .OrderByDescending(c => scoreMap.GetValueOrDefault(c.Id))
            .Take(topK)
            .ToList();

        // 🆕 CRITICAL FIX: Preserve original similarity scores (not RRF scores!)
        // This ensures downstream similarity filtering (e.g., threshold 0.2) works correctly
        foreach (var chunk in fusedResults)
        {
            if (originalScores.TryGetValue(chunk.Id, out var originalScore))
            {
                chunk.Similarity = originalScore; // Keep 0-1 range for filtering
            }
        }

        _logger.LogDebug("RRF fusion: {Count} results | Score range: {Min:F4}-{Max:F4}",
            fusedResults.Count,
            fusedResults.Any() ? fusedResults.Min(c => c.Similarity) : 0,
            fusedResults.Any() ? fusedResults.Max(c => c.Similarity) : 0);

        return fusedResults;
    }

    /// <summary>
    /// Extract document IDs from filter value
    /// </summary>
    private List<Guid> ExtractDocumentIds(object filterValue)
    {
        var ids = new List<Guid>();

        if (filterValue is List<Guid> guidList)
        {
            return guidList;
        }
        else if (filterValue is List<string> stringList)
        {
            foreach (var str in stringList)
            {
                if (Guid.TryParse(str, out var guid))
                    ids.Add(guid);
            }
        }
        else if (filterValue is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item is Guid guid)
                    ids.Add(guid);
                else if (Guid.TryParse(item?.ToString(), out var parsed))
                    ids.Add(parsed);
            }
        }

        return ids;
    }
}

