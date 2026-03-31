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
/// 问答用例实现（企业级核心RAG逻辑）
/// 优化点：Prompt增强、相似度过滤、上下文截断、重复初始化优化、异常分级处理、可观测性增强
/// </summary>
public class ChatUseCase : IChatUseCase
{
    private readonly ILlmService _llmService;
    private readonly IVectorStore _vectorStore;
    private readonly IChatConversationRepository _chatRepo;
    private readonly ILogger<ChatUseCase> _logger;
    private readonly IPermissionService _permissionService;
    private readonly IRerankService _rerankService;

    // 缓存向量库CollectionID，避免重复初始化（线程安全）
    private string _cachedCollectionId;
    private readonly object _collectionLock = new object();

    // 从常量类读取核心配置（避免硬编码）
    private readonly float _minSimilarityThreshold = LLMConstants.MIN_SEARCH_SIMILARITY;
    private readonly int _maxContextToken = LLMConstants.MAX_CONTEXT_TOKEN;

    public ChatUseCase(
        ILlmService llmService,
        IVectorStore vectorStore,
        IChatConversationRepository chatRepo,
        IPermissionService permissionService,
        IRerankService rerankService,
        ILogger<ChatUseCase> logger)
    {
        _llmService = llmService;
        _vectorStore = vectorStore;
        _chatRepo = chatRepo;
        _permissionService = permissionService;
        _rerankService= rerankService;
        _logger = logger;
    }

    public async Task<(string Answer, List<string> References, decimal CostSeconds)> ChatAsync(
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
            _logger.LogInformation("用户{UserId}发起问答：{Question}", userId, question);

            // ==============================================
            // 1. 多租户路由：获取用户专属向量集合
            // ==============================================
            var collectionName = await _permissionService.GetUserCollectionNameAsync(userId, cancellationToken);

            // ==============================================
            // 2. 权限过滤：获取用户可访问的文档ID
            // ==============================================
            var allowedDocIds = await _permissionService.GetUserAllowedDocumentIdsAsync(userId, cancellationToken);
            if (!allowedDocIds.Any())
            {
                _logger.LogWarning("用户{UserId}无文档权限", userId);
                return ("您暂无文档访问权限", validReferences, (decimal)stopwatch.Elapsed.TotalSeconds);
            }

            // 3. 生成问题向量
            var queryVector = await _llmService.EmbeddingAsync(question, cancellationToken);
            if (queryVector == null || queryVector.Length == 0)
                throw new BusinessException(500, LLMConstants.VECTOR_GENERATE_FAILED_MSG);

            // ==============================================
            // 4. 带权限过滤的向量检索
            // ==============================================
            var filter = new Dictionary<string, object>
            {
                ["document_id"] = allowedDocIds
            };

            var matchedChunks = await _vectorStore.SearchAsync(
                collectionName,
                queryVector,
                topK: 20, // 先检索20条
                filter: filter,
                cancellationToken: cancellationToken);

            var validChunks = FilterValidChunks(matchedChunks);
            if (!validChunks.Any())
            {
                _logger.LogWarning("用户{UserId}未检索到有效内容", userId);
                return ("知识库中未找到相关答案", validReferences, (decimal)stopwatch.Elapsed.TotalSeconds);
            }

            // ==============================================
            // 5. Rerank 重排（企业级精度）
            // ==============================================
            validChunks = await _rerankService.RerankAsync(question, validChunks, take: 3, cancellationToken);

            // ==============================================
            // 6. 文本清洗
            // ==============================================
            //var cleanedTexts = validChunks.Select(c => _textCleaner.Clean(c.Content)).ToList();
            var clearedTExts = validChunks;
            // ==============================================
            // 7. 生成溯源引用（文件名+页码）
            // ==============================================
            //validReferences = validChunks.Select(c =>$"文档：{c.DocumentId} 页码： 相似度：{c.Similarity:F2}").ToList();
            validReferences = validChunks.Select(c => c.Content).ToList();

            avgSimilarity = (float)validChunks.Average(c => c.Similarity);

            // 8. 构建上下文
            var context = BuildAndTruncateContext(validReferences, out contextTokenCount);
            var prompt = BuildRagPrompt(context, question);
            promptTokenCount = TokenCounter.EstimateTokenCount(prompt);
            ValidatePromptTokenCount(promptTokenCount);

            // 9. 调用大模型
            var answer = await _llmService.ChatAsync(prompt, cancellationToken);

            // 10. 记录与返回
            stopwatch.Stop();
            var costSeconds = (decimal)stopwatch.Elapsed.TotalSeconds;

            await RecordChatConversation(
                userId, question, answer, validReferences, costSeconds,
                true, avgSimilarity, contextTokenCount, promptTokenCount);

            _logger.LogInformation("用户{UserId} 完成，耗时：{Time:F2}s", userId, costSeconds);
            return (answer, validReferences, costSeconds);
        }
        catch (BusinessException ex)
        {
            stopwatch.Stop();
            await RecordChatConversation(userId, question, $"异常：{ex.Message}", new List<string>(), (decimal)stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogWarning(ex, "用户{UserId}业务异常", userId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RecordChatConversation(userId, question, "系统错误", new List<string>(), (decimal)stopwatch.Elapsed.TotalSeconds, false);
            _logger.LogError(ex, "用户{UserId}系统异常", userId);
            throw new BusinessException(500, "服务异常，请稍后重试");
        }
    }

    public async Task<(string Answer, List<string> References, decimal CostSeconds)> OldChatAsync(string userId, string question, CancellationToken cancellationToken = default)
    {
        // 初始化性能计时器
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 可观测性指标（用于日志和存储）
        float? avgSimilarity = null;
        int contextTokenCount = 0;
        int promptTokenCount = 0;
        List<string> validReferences = new List<string>();

        try
        {
            // 1. 企业级参数校验（空值+空白字符校验）
            ValidateInput(userId, question);
            question = question.Trim();
            _logger.LogInformation("用户{UserId}发起问答请求，问题：{Question}", userId, question);

            // 2. 向量库初始化（避免重复初始化）
            var collectionID = await GetOrInitVectorCollection(cancellationToken);

            // 3. 生成问题向量（异常信息细化）
            var queryVector = await _llmService.EmbeddingAsync(question, cancellationToken);
            if (queryVector == null || queryVector.Length == 0)
            {
                throw new BusinessException(500, LLMConstants.VECTOR_GENERATE_FAILED_MSG);
            }

            // 4. 向量检索（过滤低相似度结果）
            var matchedChunks = await _vectorStore.SearchAsync(collectionID, queryVector, LLMConstants.DEFAULT_SEARCH_TOP_K,null, cancellationToken);
            var validChunks = FilterValidChunks(matchedChunks);

            // 记录有效检索结果和相似度
            validReferences = validChunks.Select(c => c.Content).ToList();
            if (validChunks.Any())
            {
                avgSimilarity = (float)validChunks.Average(chunk => chunk.Similarity);
                _logger.LogInformation("用户{UserId}检索到有效分块数：{ValidCount}，平均相似度：{AvgSimilarity:F2}",
                    userId, validChunks.Count, avgSimilarity);
            }
            else
            {
                _logger.LogWarning("用户{UserId}未检索到有效分块（相似度低于{Threshold}）", userId, _minSimilarityThreshold);
            }

            // 5. 构建上下文（超长截断+空值兜底）
            var context = BuildAndTruncateContext(validReferences, out contextTokenCount);

            // 6. 构建企业级Prompt（核心优化：使用常量模板）
            var prompt = BuildRagPrompt(context, question);
            promptTokenCount = TokenCounter.EstimateTokenCount(prompt);

            // 7. Prompt Token校验（避免超出LLM窗口）
            ValidatePromptTokenCount(promptTokenCount);

            // 8. 调用大模型生成回答
            var answer = await _llmService.ChatAsync(prompt, cancellationToken);
            _logger.LogDebug("用户{UserId}LLM回答生成完成，Prompt Token数：{PromptTokenCount}", userId, promptTokenCount);

            // 9. 记录成功对话（增强：补充可观测性指标）
            stopwatch.Stop();
            var costSeconds = (decimal)stopwatch.Elapsed.TotalSeconds;
            await RecordChatConversation(
                userId, question, answer, validReferences, costSeconds,
                isSuccess: true,
                avgSimilarity: avgSimilarity,
                contextTokenCount: contextTokenCount,
                promptTokenCount: promptTokenCount);

            _logger.LogInformation("用户{UserId}问答流程完成，耗时：{CostSeconds:F2}秒", userId, costSeconds);
            return (answer, validReferences, costSeconds);
        }
        catch (BusinessException ex)
        {
            // 业务异常：直接记录并抛出（保留原始错误信息）
            stopwatch.Stop();
            var costSeconds = (decimal)stopwatch.Elapsed.TotalSeconds;
            await RecordChatConversation(
                userId, question, $"问答失败：{ex.Message}", new List<string>(), costSeconds,
                isSuccess: false);

            _logger.LogWarning("用户{UserId}问答业务异常：{Message}", userId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            // 系统异常：包装后抛出（避免暴露敏感信息）
            stopwatch.Stop();
            var costSeconds = (decimal)stopwatch.Elapsed.TotalSeconds;
            var errorMsg = "系统内部错误，请稍后重试";
            await RecordChatConversation(
                userId, question, $"问答失败：{errorMsg}", new List<string>(), costSeconds,
                isSuccess: false);

            _logger.LogError(ex, "用户{UserId}问答系统异常", userId);
            throw new BusinessException(500, $"问答失败：{errorMsg}");
        }
    }

    #region 私有核心辅助方法
    /// <summary>
    /// 输入参数校验（统一封装）
    /// </summary>
    private void ValidateInput(string userId, string question)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId))
        {
            throw new BusinessException(400, "用户ID不能为空");
        }

        if (string.IsNullOrEmpty(question) || string.IsNullOrWhiteSpace(question))
        {
            throw new BusinessException(400, "问题不能为空");
        }
    }

    /// <summary>
    /// 获取或初始化向量库Collection（双重检查锁，避免重复初始化）
    /// </summary>
    private async Task<string> GetOrInitVectorCollection(CancellationToken cancellationToken)
    {
        // 第一重检查（无锁）
        if (!string.IsNullOrEmpty(_cachedCollectionId))
        {
            return _cachedCollectionId;
        }

        // 加锁后第二重检查（线程安全）
        lock (_collectionLock)
        {
            if (!string.IsNullOrEmpty(_cachedCollectionId))
            {
                return _cachedCollectionId;
            }
        }

        // 初始化并缓存
        var collectionId = await _vectorStore.InitAsync(cancellationToken);
        lock (_collectionLock)
        {
            _cachedCollectionId = collectionId;
        }

        _logger.LogInformation("向量库Collection初始化完成，ID：{CollectionId}", collectionId);
        return collectionId;
    }

    /// <summary>
    /// 过滤有效分块（仅保留相似度≥阈值的结果）
    /// </summary>
    private List<DocumentChunk> FilterValidChunks(List<DocumentChunk> matchedChunks)
    {
        if (matchedChunks == null || !matchedChunks.Any())
        {
            return new List<DocumentChunk>();
        }

        // 过滤低相似度分块 + 按相似度降序排序
        return matchedChunks
            .Where(chunk => chunk.Similarity >= _minSimilarityThreshold)
            .OrderByDescending(chunk => chunk.Similarity)
            .ToList();
    }

    /// <summary>
    /// 构建并截断上下文（避免超长）
    /// </summary>
    private string BuildAndTruncateContext(List<string> references, out int finalTokenCount)
    {
        var contextBuilder = new StringBuilder();
        finalTokenCount = 0;

        if (references == null || !references.Any())
        {
            return string.Empty;
        }

        // 逐段拼接，超出Token阈值则停止
        foreach (var content in references)
        {
            var tempContext = contextBuilder.Append(content + "\n\n").ToString();
            var tempTokenCount = TokenCounter.EstimateTokenCount(tempContext);

            if (tempTokenCount > _maxContextToken)
            {
                _logger.LogWarning("上下文超长，已截断至{MaxToken}Token（当前{TempToken}Token）",
                    _maxContextToken, tempTokenCount);
                break;
            }

            contextBuilder.Append(content + "\n\n");
            finalTokenCount = tempTokenCount;
        }

        return contextBuilder.ToString().Trim();
    }

    /// <summary>
    /// 构建RAG Prompt（适配空上下文+使用常量模板）
    /// </summary>
    private string BuildRagPrompt(string context, string question)
    {
        // 空上下文：使用兜底模板
        if (string.IsNullOrWhiteSpace(context))
        {
            return string.Format(LLMConstants.RAG_PROMPT_EMPTY_CONTEXT_TEMPLATE, question);
        }

        // 非空上下文：使用增强版模板
        return string.Format(LLMConstants.RAG_PROMPT_ENHANCED_TEMPLATE, context, question);
    }

    /// <summary>
    /// 校验Prompt Token数（避免超出LLM窗口）
    /// </summary>
    private void ValidatePromptTokenCount(int tokenCount)
    {
        if (tokenCount > LLMConstants.MAX_PROMPT_TOKEN)
        {
            throw new BusinessException(400,
                $"{LLMConstants.PROMPT_EXCEED_TOKEN_MSG}（当前Token数：{tokenCount}，最大允许：{LLMConstants.MAX_PROMPT_TOKEN}）");
        }
    }

    /// <summary>
    /// 记录对话记录（统一封装）
    /// </summary>
    private async Task RecordChatConversation(
        string userId,
        string question,
        string answer,
        List<string> references,
        decimal costSeconds,
        bool isSuccess,
        float? avgSimilarity = null,
        int contextTokenCount = 0,
        int promptTokenCount = 0)
    {
        var conversation = new ChatConversation
        {
            UserId = userId,
            Question = question,
            Answer = answer,
            ReferenceContexts = JsonSerializer.Serialize(references),
            CostSeconds = costSeconds,
            IsSuccess = isSuccess,
            // 新增可观测性字段（需确保ChatConversation实体已扩展）
            SearchSimilarity = avgSimilarity,
            ContextTokenCount = contextTokenCount,
            PromptTokenCount = promptTokenCount
        };

        await _chatRepo.AddAsync(conversation);
        _logger.LogDebug("用户{UserId}对话记录已保存，是否成功：{IsSuccess}", userId, isSuccess);
    }
    #endregion
}