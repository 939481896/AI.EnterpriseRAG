using AI.EnterpriseRAG.Core.Constants;
using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Utils;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AI.EnterpriseRAG.Application.UseCases;

/// <summary>
/// V1.0 Enhanced ChatUseCase with Brain-Like Capabilities
/// Features: HyDE, Multi-Query, Self-Reflection, Citations
/// </summary>
public partial class ChatUseCase : IChatUseCase
{
    /// <summary>
    /// V1.0: Brain-Like RAG with HyDE + Multi-Query + Self-Reflection
    /// </summary>
    public async Task<(string Answer, List<string> References, decimal CostSeconds)> ChatV1Async(
        string userId,
        string question,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        float? avgSimilarity = null;
        int contextTokenCount = 0;
        int promptTokenCount = 0;
        List<string> validReferences = new List<string>();

        try
        {
            ValidateInput(userId, question);
            question = question.Trim();
            _logger.LogInformation("🧠 V1.0 RAG | User: {UserId} | Question: {Question}", userId, question);

            // ==============================================
            // 1. Multi-Tenant Routing
            // ==============================================
            var collectionName = await _permissionService.GetUserCollectionNameAsync(userId, cancellationToken);
            
            // ==============================================
            // 2. Permission Filtering
            // ==============================================
            var allowedDocIds = await _permissionService.GetUserAllowedDocumentIdsAsync(userId, cancellationToken);
            if (!allowedDocIds.Any())
            {
                _logger.LogWarning("User {UserId} has no document permissions", userId);
                return ("您暂无文档访问权限", validReferences, (decimal)stopwatch.Elapsed.TotalSeconds);
            }

            // ==============================================
            // 3. 🚀 HyDE: Query Rewriting (20-30% Accuracy Boost)
            // ==============================================
            string queryText;
            if (_queryRewriting != null)
            {
                try
                {
                    var hypotheticalDoc = await _queryRewriting.GenerateHypotheticalDocumentAsync(
                        question, cancellationToken);
                    queryText = hypotheticalDoc;
                    _logger.LogInformation("✅ HyDE rewriting applied");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "HyDE failed, using original query");
                    queryText = question;
                }
            }
            else
            {
                queryText = question;
            }
            
            var queryVector = await _llmService.EmbeddingAsync(queryText, cancellationToken);
            if (queryVector == null || queryVector.Length == 0)
                throw new BusinessException(500, LLMConstants.VECTOR_GENERATE_FAILED_MSG);

            // ==============================================
            // 4. 🔍 Multi-Query Fusion (Optional Enhancement)
            // ==============================================
            var allResults = new List<List<DocumentChunk>>();

            if (_queryRewriting != null && _multiQueryConfig?.Enabled == true)
            {
                try
                {
                    var queryCount = _multiQueryConfig?.QueryCount ?? 2;
                    var similarQueries = await _queryRewriting.GenerateSimilarQueriesAsync(
                        question, count: queryCount, cancellationToken);

                    foreach (var sq in similarQueries.Take(queryCount))
                    {
                        var sqVector = await _llmService.EmbeddingAsync(sq, cancellationToken);
                        var sqResults = await SearchWithPermissions(
                            collectionName, sqVector, allowedDocIds, cancellationToken, sq);
                        allResults.Add(sqResults);
                    }

                    _logger.LogInformation("✅ Multi-query: {Count} queries", similarQueries.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Multi-query failed");
                }
            }

            // Main query search (with hybrid search if available)
            var mainResults = await SearchWithPermissions(
                collectionName, queryVector, allowedDocIds, cancellationToken, question);
            allResults.Insert(0, mainResults);

            // Fuse results if multiple queries
            var fusedChunks = allResults.Count > 1 
                ? FuseMultipleResults(allResults) 
                : mainResults;
            
            var validChunks = FilterValidChunks(fusedChunks);
            if (!validChunks.Any())
            {
                _logger.LogWarning("User {UserId} found no valid content", userId);
                return ("知识库中未找到相关答案", validReferences, (decimal)stopwatch.Elapsed.TotalSeconds);
            }

            // ==============================================
            // 5. Rerank
            // ==============================================
            try
            {
                var rerankTopK = _ragConfig.RerankTopK;
                validChunks = await _rerankService.RerankAsync(question, validChunks, take: rerankTopK, cancellationToken);
                _logger.LogInformation("✅ Rerank complete | Final: {Count}", validChunks.Count);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "⚠️ Rerank unavailable, using top-{TopK}", _ragConfig.RerankTopK);
                validChunks = validChunks.Take(_ragConfig.RerankTopK).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Rerank error");
                validChunks = validChunks.Take(_ragConfig.RerankTopK).ToList();
            }

            // ==============================================
            // 6. 📝 Build Context with Citations & Memory
            // ==============================================
            validReferences = validChunks.Select(c => c.Content).ToList();
            avgSimilarity = (float)validChunks.Average(c => c.Similarity);

            string prompt;
            Guid? sessionId = null;

            // Check if memory service is available and user wants conversation context
            if (_memory != null && _memoryConfig?.Enabled == true)
            {
                try
                {
                    // Get or create session (for now, auto-create per user)
                    var sessions = await _memory.GetUserSessionsAsync(userId, limit: 1, cancellationToken);
                    if (sessions.Any())
                    {
                        sessionId = sessions.First().Id;
                    }

                    // Add user question to memory
                    await _memory.AddMessageAsync(
                        userId, sessionId, "user", question, 
                        null, 0, true, cancellationToken);

                    // Build context-aware prompt with conversation history
                    var maxHistoryTokens = _memoryConfig?.MaxHistoryTokens ?? 1000;
                    prompt = await _memory.BuildContextAwarePromptAsync(
                        sessionId ?? Guid.NewGuid(), question, validChunks, 
                        maxHistoryTokens: maxHistoryTokens, cancellationToken);

                    promptTokenCount = TokenCounter.EstimateTokenCount(prompt);
                    _logger.LogInformation("💭 Using conversation memory | Session: {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Memory service failed, falling back to standard prompt");
                    var context = BuildAndTruncateContext(validReferences, out contextTokenCount);
                    prompt = BuildRagPromptWithCitations(context, question);
                    promptTokenCount = TokenCounter.EstimateTokenCount(prompt);
                }
            }
            else
            {
                // Fallback: Standard prompt without memory
                var context = BuildAndTruncateContext(validReferences, out contextTokenCount);
                prompt = BuildRagPromptWithCitations(context, question);
                promptTokenCount = TokenCounter.EstimateTokenCount(prompt);
            }

            ValidatePromptTokenCount(promptTokenCount);

            // ==============================================
            // 7. Generate Answer
            // ==============================================
            var answer = await _llmService.ChatAsync(prompt, cancellationToken);

            // ==============================================
            // 8. 🪞 Self-Reflection & Correction
            // ==============================================
            if (_selfReflection != null)
            {
                try
                {
                    answer = await _selfReflection.SelfCorrectAsync(
                        question, answer, validChunks, cancellationToken);
                    _logger.LogInformation("✅ Self-reflection applied");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Self-reflection failed, using original answer");
                }
            }

            // ==============================================
            // 9. Record & Return
            // ==============================================
            stopwatch.Stop();
            var costSeconds = (decimal)stopwatch.Elapsed.TotalSeconds;

            // Save to memory if available
            if (_memory != null && sessionId.HasValue)
            {
                try
                {
                    await _memory.AddMessageAsync(
                        userId, sessionId, "assistant", answer, 
                        JsonSerializer.Serialize(validChunks.Select(c => c.Id)), 
                        costSeconds, true, cancellationToken);
                    _logger.LogDebug("💾 Saved to conversation memory");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save to memory");
                }
            }

            await RecordChatConversation(
                userId, question, answer, validReferences, costSeconds,
                true, avgSimilarity, contextTokenCount, promptTokenCount);

            _logger.LogInformation("✅ V1.0 RAG complete | Time: {Time:F2}s | Chunks: {Count}", 
                costSeconds, validChunks.Count);

            return (answer, validReferences, costSeconds);
        }
        catch (BusinessException ex)
        {
            stopwatch.Stop();
            await RecordChatConversation(userId, question, $"Exception: {ex.Message}", 
                new List<string>(), (decimal)stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogWarning(ex, "User {UserId} business exception", userId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordChatConversation(userId, question, "System error", 
                new List<string>(), (decimal)stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogError(ex, "User {UserId} system exception", userId);
            throw new BusinessException(500, "Service exception, please try again later");
        }
    }

    #region V1.0 Helper Methods

    private async Task<List<DocumentChunk>> SearchWithPermissions(
        string collectionName,
        float[] queryVector,
        List<string> allowedDocIds,
        CancellationToken ct,
        string? queryText = null)
    {
        var filter = new Dictionary<string, object>
        {
            ["document_id"] = allowedDocIds
        };

        // 🔍 Use Hybrid Search if available (Vector + BM25)
        if (_hybridSearch != null && !string.IsNullOrEmpty(queryText))
        {
            try
            {
                var topK = _ragConfig.RetrievalTopK;
                _logger.LogInformation("🔬 Using Hybrid Search (Vector + BM25) | TopK: {TopK}", topK);
                return await _hybridSearch.SearchAsync(
                    query: queryText,
                    collectionName: collectionName,
                    queryVector: queryVector,
                    filter: filter,
                    topK: topK,
                    ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Hybrid search failed, falling back to vector search");
            }
        }

        // Fallback: Standard vector search
        var fallbackTopK = _ragConfig.RetrievalTopK;
        return await _vectorStore.SearchAsync(
            collectionName, queryVector, topK: fallbackTopK, filter: filter, cancellationToken: ct);
    }

    private List<DocumentChunk> FuseMultipleResults(List<List<DocumentChunk>> allResults)
    {
        var scoreMap = new Dictionary<Guid, double>();
        
        // Reciprocal Rank Fusion
        foreach (var resultList in allResults)
        {
            for (int i = 0; i < resultList.Count; i++)
            {
                var chunkId = resultList[i].Id;
                scoreMap[chunkId] = scoreMap.GetValueOrDefault(chunkId) + 1.0 / (60 + i + 1);
            }
        }
        
        // Merge and rank
        return allResults
            .SelectMany(r => r)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .OrderByDescending(c => scoreMap.GetValueOrDefault(c.Id))
            .ToList();
    }

    private string BuildRagPromptWithCitations(string context, string question)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return string.Format(LLMConstants.RAG_PROMPT_EMPTY_CONTEXT_TEMPLATE, question);
        }

        // Number each paragraph for citation
        var paragraphs = context.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var numberedContext = string.Join("\n\n", 
            paragraphs.Select((p, i) => $"[{i+1}] {p}"));

        return $@"You are a helpful AI assistant. Use the following context to answer the question.

Context (with source numbers):
{numberedContext}

Question: {question}

Instructions:
1. Answer ONLY using information from the context above
2. Cite sources using [1], [2], etc. after each claim
3. If the context doesn't contain enough information, say so
4. Be concise and accurate

Answer:";
    }

    #endregion
}
