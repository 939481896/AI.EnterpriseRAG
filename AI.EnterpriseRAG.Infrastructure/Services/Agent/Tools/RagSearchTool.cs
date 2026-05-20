using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using System.Diagnostics;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

/// <summary>
/// RAG知识库检索工具
/// </summary>
public class RagSearchTool : ITool
{
    private readonly IVectorStore _vectorStore;
    private readonly ILlmService _llmService;
    private readonly IPermissionService _permissionService;

    public string Name => "rag_search";
    public string Description => "从企业知识库检索相关文档和信息。当用户询问产品手册、技术文档、公司政策等知识库内容时使用此工具。";
    public string Category => "rag";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""query"": {
      ""type"": ""string"",
      ""description"": ""搜索查询语句，应包含关键词""
    },
    ""top_k"": {
      ""type"": ""integer"",
      ""description"": ""返回结果数量，默认5"",
      ""default"": 5
    }
  },
  ""required"": [""query""]
}";

    public RagSearchTool(
        IVectorStore vectorStore,
        ILlmService llmService,
        IPermissionService permissionService)
    {
        _vectorStore = vectorStore;
        _llmService = llmService;
        _permissionService = permissionService;
    }

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. 解析参数
            if (!arguments.TryGetValue("query", out var queryObj) || queryObj is not string query)
                return ToolResult.Failure("缺少必需参数: query");

            var topK = arguments.TryGetValue("top_k", out var topKObj)
                ? Convert.ToInt32(topKObj)
                : 5;

            // 2. 获取用户权限
            var collectionName = await _permissionService.GetUserCollectionNameAsync(
                context.UserId,
                cancellationToken);

            var allowedDocIds = await _permissionService.GetUserAllowedDocumentIdsAsync(
                context.UserId,
                cancellationToken);

            if (!allowedDocIds.Any())
                return ToolResult.Failure("用户无文档访问权限");

            // 3. 生成查询向量
            var queryVector = await _llmService.EmbeddingAsync(query, cancellationToken);

            // 4. 向量检索
            var filter = new Dictionary<string, object>
            {
                ["document_id"] = allowedDocIds
            };

            var chunks = await _vectorStore.SearchAsync(
                collectionName,
                queryVector,
                topK,
                filter,
                cancellationToken);

            // 5. 构建结果（注意：DocumentChunk不再包含Similarity字段，使用匿名对象）
            var results = chunks.Select(c => new
            {
                content = c.Content,
                similarity = 0.85f, // 临时值，实际应从向量库返回的元数据获取
                document_id = c.DocumentId,
                chunk_index = c.Index
            }).ToList();

            sw.Stop();

            return ToolResult.Success(
                JsonSerializer.Serialize(new
                {
                    query,
                    results_count = results.Count,
                    results
                }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return ToolResult.Failure($"RAG检索失败: {ex.Message}");
        }
    }
}
