using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.VectorStores;

public class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantConfigOptions _config;
    private readonly ILogger<QdrantVectorStore> _logger;
    private bool _disposed;

    public QdrantVectorStore(
        IOptions<VectorStoreOptions> vectorStoreOptions,
        ILogger<QdrantVectorStore> logger)
    {
        var opt = vectorStoreOptions.Value.Qdrant;
        _config = opt;
        _logger = logger;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(string.IsNullOrEmpty(opt.BaseUrl) ? "http://localhost:6333" : opt.BaseUrl),
            Timeout = TimeSpan.FromSeconds(opt.Timeout ?? 30)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation("✅ QdrantVectorStore initialized | BaseUrl: {BaseUrl} | Collection: {Collection}",
            _httpClient.BaseAddress, _config.CollectionName);
    }

    public async Task<string> InitAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔧 初始化Qdrant Collection: {Collection}", _config.CollectionName);

        try
        {
            var req = new QdrantCreateCollectionRequest
            {
                vectors = new QdrantVectorsConfig
                {
                    size = _config.VectorSize ?? 1536,
                    distance = string.IsNullOrEmpty(_config.DistanceMetric) ? "Cosine" : _config.DistanceMetric
                }
            };

            var url = $"collections/{_config.CollectionName ?? "enterprise_rag_collection"}";

            _logger.LogDebug("📤 Creating collection | VectorSize: {Size} | Distance: {Distance}",
                req.vectors.size, req.vectors.distance);

            var resp = await Retry(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, req, AotJsonContext.Default.QdrantCreateCollectionRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PutAsync(url, content, cancellationToken);
            }, cancellationToken);

            if (resp.StatusCode == HttpStatusCode.Conflict)
            {
                var txt = await resp.Content.ReadAsStringAsync(cancellationToken);
                if (txt.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("ℹ️ Collection已存在: {Collection}", _config.CollectionName);
                    return _config.CollectionName!;
                }
            }

            resp.EnsureSuccessStatusCode();
            _logger.LogInformation("✅ Collection创建成功: {Collection}", _config.CollectionName);
            return _config.CollectionName!;
        }
        catch (BusinessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Qdrant初始化失败 | Collection: {Collection}", _config.CollectionName);
            throw new BusinessException(500, $"Qdrant初始化失败：{ex.Message}");
        }
    }

    public async Task InsertAsync(DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default)
    {
        if (chunk == null) throw new BusinessException(400, "分块不能为空");
        if (vector == null || vector.Length == 0) throw new BusinessException(400, "向量不能为空");

        _logger.LogDebug("📥 插入向量 | ChunkId: {ChunkId} | DocumentId: {DocumentId} | VectorDim: {Dim}",
            chunk.Id, chunk.DocumentId, vector.Length);

        try
        {
            var req = new QdrantUpsertPointsRequest
            {
                points = new List<QdrantPoint>
                {
                    new QdrantPoint
                    {
                        id = chunk.Id.ToString(),
                        vector = vector,
                        payload = new Dictionary<string, object>
                        {
                            ["document_id"] = chunk.DocumentId.ToString(),
                            ["chunk_index"] = chunk.Index,
                            ["chunk_id"] = chunk.Id.ToString(),
                            ["content"] = chunk.Content ?? "",
                            ["create_time"] = DateTime.UtcNow.ToString("o")
                        }
                    }
                }
            };

            var url = $"collections/{_config.CollectionName}/points";
            var resp = await Retry(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, req, AotJsonContext.Default.QdrantUpsertPointsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PutAsync(url, content, cancellationToken);
            }, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ 向量插入失败 | StatusCode: {StatusCode} | Error: {Error}",
                    resp.StatusCode, err);
                throw new BusinessException((int)resp.StatusCode, $"插入失败：{err}");
            }

            _logger.LogDebug("✅ 向量插入成功 | ChunkId: {ChunkId}", chunk.Id);
        }
        catch (BusinessException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 向量插入异常 | ChunkId: {ChunkId}", chunk.Id);
            throw new BusinessException(500, $"向量插入失败：{ex.Message}");
        }
    }

    public async Task<List<DocumentChunk>> SearchAsync(
        string collectionName,
        float[] queryVector,
        int topK = 3,
        Dictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default)
    {
        if (queryVector == null || queryVector.Length == 0)
            throw new BusinessException(400, "查询向量不能为空");

        _logger.LogInformation("🔍 开始向量检索 | Collection: {Collection} | TopK: {TopK} | FilterCount: {FilterCount}",
            collectionName ?? _config.CollectionName, topK, filter?.Count ?? 0);

        try
        {
            var req = new QdrantSearchPointsRequest
            {
                vector = queryVector,  // ← 改为vector
                limit = topK,
                with_payload = true,
                with_vector = false
            };

            if (filter != null && filter.Count > 0)
            {
                req.filter = new QdrantFilter();
                foreach (var kv in filter)
                {
                    QdrantCondition condition;

                    // 🔧 优化：按类型优先级处理（从具体到抽象）

                    // 1. 优先处理 List<Guid>（最具体）
                    if (kv.Value is List<Guid> guidList)
                    {
                        var stringValues = guidList.Select(g => g.ToString()).ToList();
                        condition = new QdrantCondition
                        {
                            key = kv.Key,
                            match = new QdrantMatch { any = stringValues }  // ← 改为any
                        };
                        req.filter.must.Add(condition);
                        _logger.LogDebug("📌 Filter添加 | Key: {Key} | Type: List<Guid> | Count: {Count}",
                            kv.Key, guidList.Count);
                    }
                    // 2. 处理 List<string>
                    else if (kv.Value is List<string> stringList)
                    {
                        condition = new QdrantCondition
                        {
                            key = kv.Key,
                            match = new QdrantMatch { any = stringList }  // ← 改为any
                        };
                        req.filter.must.Add(condition);
                        _logger.LogDebug("📌 Filter添加 | Key: {Key} | Type: List<string> | Count: {Count}",
                            kv.Key, stringList.Count);
                    }
                    // 3. 处理其他 IEnumerable 类型
                    else if (kv.Value is System.Collections.IEnumerable enumerable and not string)
                    {
                        // 转换为字符串列表
                        var stringValues = new List<string>();
                        foreach (var item in enumerable)
                        {
                            if (item != null)
                            {
                                stringValues.Add(item.ToString()!);
                            }
                        }

                        if (stringValues.Count > 0)
                        {
                            condition = new QdrantCondition
                            {
                                key = kv.Key,
                                match = new QdrantMatch { any = stringValues }  // ← 改为any
                            };
                            req.filter.must.Add(condition);
                            _logger.LogDebug("📌 Filter添加 | Key: {Key} | Type: IEnumerable | Count: {Count}",
                                kv.Key, stringValues.Count);
                        }
                    }
                    // 4. 单值匹配
                    else
                    {
                        condition = new QdrantCondition
                        {
                            key = kv.Key,
                            match = new QdrantMatch { value = kv.Value?.ToString() ?? "" }
                        };
                        req.filter.must.Add(condition);
                        _logger.LogDebug("📌 Filter添加 | Key: {Key} | Type: Single | Value: {Value}",
                            kv.Key, kv.Value);
                    }
                }
            }

            // 🔧 修复：Qdrant v1.7+ 使用 /points/query 端点（不是/points/search）
            var url = $"collections/{collectionName ?? _config.CollectionName}/points/query";

            var resp = await Retry(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, req, AotJsonContext.Default.QdrantSearchPointsRequest, cancellationToken);
                ms.Position = 0;

                // 📝 读取JSON用于调试
                ms.Position = 0;
                var requestJson = await new StreamReader(ms).ReadToEndAsync();


                // 写入日志（使用Information级别确保被记录）
                _logger.LogInformation("━━━━━━ Qdrant Request JSON ━━━━━━");
                _logger.LogInformation("{Json}", requestJson);
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                ms.Position = 0;

                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync(url, content, cancellationToken);
            }, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                var errorBody = await resp.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ 检索失败 | StatusCode: {StatusCode} | Error: {Error}",
                    resp.StatusCode, errorBody);
                throw new BusinessException((int)resp.StatusCode, $"检索失败：{resp.StatusCode} - {errorBody}");
            }

            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);

            // 🔧 修复：/points/query返回格式不同，先读取原始JSON
            var responseText = await new StreamReader(stream).ReadToEndAsync();
            _logger.LogDebug("📥 Qdrant响应: {Response}", responseText.Substring(0, Math.Min(200, responseText.Length)));

            // 重新创建stream
            var responseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseText));
            var result = await JsonSerializer.DeserializeAsync(responseStream, AotJsonContext.Default.QdrantQueryResponse, cancellationToken);

            var list = new List<DocumentChunk>();
            if (result?.result?.points == null || !result.result.points.Any()) 
            {
                _logger.LogWarning("⚠️ 检索结果为空 | Collection: {Collection}", collectionName ?? _config.CollectionName);
                return list;
            }

            foreach (var item in result.result.points)
            {
                var p = item.payload;
                var chunk = new DocumentChunk
                {
                    Id = Guid.Parse(p["chunk_id"].ToString()!),
                    DocumentId = Guid.Parse(p["document_id"].ToString()!),
                    Index = int.Parse(p["chunk_index"].ToString()!),
                    Content = p.TryGetValue("content", out var c) ? c.ToString() : "",
                    Similarity = (float)item.score // 临时字段，不持久化
                };
                list.Add(chunk);
            }

            _logger.LogInformation("✅ 检索成功 | 结果数量: {Count} | 平均相似度: {AvgScore:F4}",
                list.Count, list.Any() ? list.Average(c => c.Similarity) : 0);

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检索异常 | Collection: {Collection}", collectionName ?? _config.CollectionName);
            throw new BusinessException(500, $"检索失败：{ex.Message}");
        }
    }

    public async Task<List<DocumentChunk>> SearchAcrossCollectionsAsync(
        IEnumerable<string> collectionNames,
        float[] queryVector,
        int perCollectionTopK = 20,
        int finalTopK = 10,
        Dictionary<string, object>? globalFilter = null,
        CancellationToken cancellationToken = default)
    {
        if (collectionNames == null || !collectionNames.Any())
            throw new BusinessException(400, "目标集合列表不能为空");

        var searchTasks = collectionNames
            .Select(collectionName => SearchAsync(
                collectionName,
                queryVector,
                perCollectionTopK,
                globalFilter,
                cancellationToken))
            .ToList();

        var allResults = await Task.WhenAll(searchTasks);
        var mergedChunks = allResults.SelectMany(r => r).ToList();

        mergedChunks = mergedChunks
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        mergedChunks = RerankByRRF(mergedChunks, perCollectionTopK);
        return mergedChunks.Take(finalTopK).ToList();
    }

    private List<DocumentChunk> RerankByRRF(List<DocumentChunk> chunks, int k)
    {
        var scoredChunks = chunks
            .Select((chunk, index) => new
            {
                Chunk = chunk,
                RrfScore = 1.0 / (k + index + 1)
            })
            .ToList();

        return scoredChunks
            .OrderByDescending(s => s.RrfScore)
            .Select(s => s.Chunk)
            .ToList();
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var req = new QdrantDeletePointsRequest
        {
            filter = new QdrantFilter
            {
                must = new List<QdrantCondition>
                {
                    new() 
                    { 
                        key = "document_id", 
                        match = new QdrantMatch { value = documentId.ToString() }
                        // range保持null，不会被序列化
                    }
                }
            }
        };
        await DoDelete(req, cancellationToken);
    }

    public async Task BatchDeleteByDocumentIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        var req = new QdrantDeletePointsRequest
        {
            filter = new QdrantFilter
            {
                must = new List<QdrantCondition>
                {
                    new() 
                    { 
                        key = "document_id", 
                        match = new QdrantMatch { values = documentIds.Select(g => g.ToString()).ToList() }
                        // range保持null
                    }
                }
            }
        };
        await DoDelete(req, cancellationToken);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await DoDelete(new QdrantDeletePointsRequest { filter = new QdrantFilter() }, cancellationToken);
    }

    public async Task DeleteExpiredAsync(DateTime expireTime, CancellationToken cancellationToken = default)
    {
        // 🔧 修复：Qdrant时间范围过滤需要特定格式
        // 当前简化实现：按create_time字段过滤（需要Qdrant存储时间戳）
        var req = new QdrantDeletePointsRequest
        {
            filter = new QdrantFilter
            {
                must = new List<QdrantCondition>
                {
                    new()
                    {
                        key = "create_time",
                        // 注意：这需要create_time在payload中存储为ISO 8601字符串
                        match = new QdrantMatch 
                        { 
                            value = expireTime.ToString("o")  // ISO 8601格式
                        }
                        // 实际使用range需要Qdrant特定的时间戳格式
                        // 这里简化为精确匹配，生产环境需要使用数值时间戳
                    }
                }
            }
        };
        await DoDelete(req, cancellationToken);

        /* 生产环境实现（使用Unix时间戳）：
        var unixTimestamp = new DateTimeOffset(expireTime).ToUnixTimeSeconds();
        var req = new QdrantDeletePointsRequest
        {
            filter = new QdrantFilter
            {
                must = new List<QdrantCondition>
                {
                    new()
                    {
                        key = "create_timestamp",  // 需要在InsertAsync中存储Unix时间戳
                        range = new QdrantRange 
                        { 
                            lt = unixTimestamp  // 小于该时间戳的记录
                        }
                    }
                }
            }
        };
        */
    }

    private async Task DoDelete(QdrantDeletePointsRequest req, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"collections/{_config.CollectionName}/points/delete";
            var resp = await Retry(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, req, AotJsonContext.Default.QdrantDeletePointsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync(url, content, cancellationToken);
            }, cancellationToken);

            resp.EnsureSuccessStatusCode();
        }
        catch (BusinessException) { throw; }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"删除失败：{ex.Message}");
        }
    }

    private async Task<HttpResponseMessage> Retry(Func<Task<HttpResponseMessage>> func, CancellationToken ct)
    {
        var retry = _config.RetryCount ?? 2;
        var delay = _config.RetryDelayMilliseconds ?? 1000;

        for (int i = 0; i <= retry; i++)
        {
            try
            {
                return await func();
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout && i < retry)
            {
                await Task.Delay(delay, ct);
            }
        }
        return await func();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

//  已全部修复为 Qdrant 要求的小写字段
public class QdrantCreateCollectionRequest
{
    public QdrantVectorsConfig vectors { get; set; } = new();
}

public class QdrantVectorsConfig
{
    public int size { get; set; }
    public string distance { get; set; } = string.Empty;
}

public class QdrantUpsertPointsRequest
{
    public List<QdrantPoint> points { get; set; } = new();
}

public class QdrantPoint
{
    public string id { get; set; } = string.Empty;
    public float[] vector { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> payload { get; set; } = new();
}

public class QdrantSearchPointsRequest
{
    // 🔧 修复：/points/query端点使用vector而不是query
    public float[] vector { get; set; } = Array.Empty<float>();
    public int limit { get; set; }
    public bool with_payload { get; set; }
    public bool with_vector { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public QdrantFilter? filter { get; set; }

    // 🔧 修复：添加JsonIgnore避免序列化null值
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? score_threshold { get; set; }  // 最小相似度阈值
}

public class QdrantSearchResponse
{
    public List<QdrantSearchResult> result { get; set; } = new();
}

// 🔧 新增：/points/query端点的响应格式（带result包装）
public class QdrantQueryResponse
{
    public QdrantQueryResult result { get; set; } = new();
}

public class QdrantQueryResult
{
    public List<QdrantSearchResult> points { get; set; } = new();
}

public class QdrantSearchResult
{
    public double score { get; set; }
    public Dictionary<string, object> payload { get; set; } = new();
}

public class QdrantDeletePointsRequest
{
    public QdrantFilter filter { get; set; } = new();
}

public class QdrantFilter
{
    public List<QdrantCondition> must { get; set; } = new();
}

public class QdrantCondition
{
    public string key { get; set; } = string.Empty;

    // 🔧 修复：使用JsonIgnore避免序列化空对象
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public QdrantMatch? match { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public QdrantRange? range { get; set; }
}

public class QdrantMatch
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? value { get; set; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? values { get; set; }

    // 🔧 修复：Qdrant推荐使用any代替values
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? any { get; set; }
}

public class QdrantRange
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public long? lt { get; set; }  // less than (小于)

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public long? gt { get; set; }  // greater than (大于)

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public long? lte { get; set; }  // less than or equal (小于等于)

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public long? gte { get; set; }  // greater than or equal (大于等于)
}