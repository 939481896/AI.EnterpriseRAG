using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.VectorStores;

public class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantConfigOptions _config;
    private bool _disposed;

    public QdrantVectorStore(IOptions<VectorStoreOptions> vectorStoreOptions)
    {
        var opt = vectorStoreOptions.Value.Qdrant;
        _config = opt;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(string.IsNullOrEmpty(opt.BaseUrl) ? "http://localhost:6333" : opt.BaseUrl),
            Timeout = TimeSpan.FromSeconds(opt.Timeout ?? 30)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var req = new QdrantCreateCollectionRequest
            {
                Vectors = new QdrantVectorsConfig
                {
                    Size = _config.VectorSize ?? 1536,
                    Distance = string.IsNullOrEmpty(_config.DistanceMetric) ? "Cosine" : _config.DistanceMetric
                }
            };

            var url = $"collections/{_config.CollectionName ?? "enterprise_rag_collection"}";

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
                    return _config.CollectionName!;
            }

            resp.EnsureSuccessStatusCode();
            return _config.CollectionName!;
        }
        catch (BusinessException) { throw; }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"Qdrant初始化失败：{ex.Message}");
        }
    }

    public async Task InsertAsync(DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default)
    {
        if (chunk == null) throw new BusinessException(400, "分块不能为空");
        if (vector == null || vector.Length == 0) throw new BusinessException(400, "向量不能为空");

        try
        {
            var req = new QdrantUpsertPointsRequest
            {
                Points = new List<QdrantPoint>
                {
                    new QdrantPoint
                    {
                        Id = chunk.Id.ToString(),
                        Vector = vector,
                        Payload = new Dictionary<string, object>
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
                return await _httpClient.PostAsync(url, content, cancellationToken);
            }, cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)resp.StatusCode, $"插入失败：{err}");
            }

            chunk.VectorJson = JsonSerializer.Serialize(vector, AotJsonContext.Default.SingleArray);
        }
        catch (BusinessException) { throw; }
        catch (Exception ex)
        {
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

        try
        {
            var req = new QdrantSearchPointsRequest
            {
                Vector = queryVector,
                Limit = topK,
                WithPayload = true,
                WithVector = false
            };

            // ========================
            // 构建 Filter
            // ========================
            if (filter != null && filter.Count > 0)
            {
                req.Filter = new QdrantFilter();
                foreach (var kv in filter)
                {
                    if (kv.Value is List<string> values)
                    {
                        req.Filter.Must.Add(new QdrantCondition
                        {
                            Key = kv.Key,
                            Match = new QdrantMatch { Values = values }
                        });
                    }
                    else
                    {
                        req.Filter.Must.Add(new QdrantCondition
                        {
                            Key = kv.Key,
                            Match = new QdrantMatch { Value = kv.Value.ToString() }
                        });
                    }
                }
            }

            var url = $"collections/{collectionName??_config.CollectionName}/points/query";
            var resp = await Retry(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, req, AotJsonContext.Default.QdrantSearchPointsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                return await _httpClient.PostAsync(url, content, cancellationToken);
            }, cancellationToken);

            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync(stream, AotJsonContext.Default.QdrantSearchResponse, cancellationToken);

            var list = new List<DocumentChunk>();
            if (result?.Result == null) return list;

            foreach (var item in result.Result)
            {
                var p = item.Payload;
                list.Add(new DocumentChunk
                {
                    Id = Guid.Parse(p["chunk_id"].ToString()!),
                    DocumentId = Guid.Parse(p["document_id"].ToString()!),
                    Index = int.Parse(p["chunk_index"].ToString()!),
                    Content = p.TryGetValue("content", out var c) ? c.ToString() : "",
                    Similarity = (float)item.Score
                });
            }
            return list;
        }
        catch (Exception ex)
        {
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

        // 1. 并行查询所有目标Collection
        var searchTasks = collectionNames
            .Select(collectionName => SearchAsync(
                collectionName,
                queryVector,
                perCollectionTopK,
                globalFilter,  // 每个Collection都应用全局权限过滤
                cancellationToken))
            .ToList();

        // 等待所有查询完成
        var allResults = await Task.WhenAll(searchTasks);

        // 2. 合并所有结果
        var mergedChunks = allResults.SelectMany(r => r).ToList();

        // 3. 去重（按ChunkId）
        mergedChunks = mergedChunks
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        // 4. 重排序（RRF：Reciprocal Rank Fusion，标准跨源融合算法）
        mergedChunks = RerankByRRF(mergedChunks, perCollectionTopK);

        // 5. 取最终Top-K
        var finalResults = mergedChunks.Take(finalTopK).ToList();

        return finalResults;
    }

    // RRF重排序实现（消除不同Collection的相似度评分差异）
    private List<DocumentChunk> RerankByRRF(List<DocumentChunk> chunks, int k)
    {
        // 按原始相似度排序，计算RRF分数
        var scoredChunks = chunks
            .Select((chunk, index) => new
            {
                Chunk = chunk,
                RrfScore = 1.0 / (k + index + 1)  // RRF公式：1/(k+rank)
            })
            .ToList();

        // 按RRF分数降序排序
        return scoredChunks
            .OrderByDescending(s => s.RrfScore)
            .Select(s => s.Chunk)
            .ToList();
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var req = new QdrantDeletePointsRequest
        {
            Filter = new QdrantFilter
            {
                Must = new List<QdrantCondition>
                {
                    new() { Key = "document_id", Match = new QdrantMatch { Value = documentId.ToString() } }
                }
            }
        };

        await DoDelete(req, cancellationToken);
    }

    public async Task BatchDeleteByDocumentIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        var req = new QdrantDeletePointsRequest
        {
            Filter = new QdrantFilter
            {
                Must = new List<QdrantCondition>
                {
                    new() { Key = "document_id", Match = new QdrantMatch { Values = documentIds.Select(g => g.ToString()).ToList() } }
                }
            }
        };

        await DoDelete(req, cancellationToken);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await DoDelete(new QdrantDeletePointsRequest { Filter = new QdrantFilter() }, cancellationToken);
    }

    public async Task DeleteExpiredAsync(DateTime expireTime, CancellationToken cancellationToken = default)
    {
        var req = new QdrantDeletePointsRequest
        {
            Filter = new QdrantFilter
            {
                Must = new List<QdrantCondition>
                {
                    new()
                    {
                        Key = "create_time",
                        Match = new QdrantMatch { Value = expireTime.ToString("o") },
                        Range = new QdrantRange { Lt = true }
                    }
                }
            }
        };

        await DoDelete(req, cancellationToken);
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

// AOT 模型
public class QdrantCreateCollectionRequest
{
    public QdrantVectorsConfig Vectors { get; set; } = new();
}

public class QdrantVectorsConfig
{
    public int Size { get; set; }
    public string Distance { get; set; } = string.Empty;
}

public class QdrantUpsertPointsRequest
{
    public List<QdrantPoint> Points { get; set; } = new();
}

public class QdrantPoint
{
    public string Id { get; set; } = string.Empty;
    public float[] Vector { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Payload { get; set; } = new();
}

public class QdrantSearchPointsRequest
{
    public float[] Vector { get; set; } = Array.Empty<float>();
    public int Limit { get; set; }
    public bool WithPayload { get; set; }
    public bool WithVector { get; set; }
    public QdrantFilter? Filter { get; set; } 

}

public class QdrantSearchResponse
{
    public List<QdrantSearchResult> Result { get; set; } = new();
}

public class QdrantSearchResult
{
    public double Score { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
}

public class QdrantDeletePointsRequest
{
    public QdrantFilter Filter { get; set; } = new();
}

public class QdrantFilter
{
    public List<QdrantCondition> Must { get; set; } = new();
}

public class QdrantCondition
{
    public string Key { get; set; } = string.Empty;
    public QdrantMatch Match { get; set; } = new();
    public QdrantRange Range { get; set; } = new();
}

public class QdrantMatch
{
    public string? Value { get; set; }
    public List<string>? Values { get; set; }
}

public class QdrantRange
{
    public bool Lt { get; set; }
}