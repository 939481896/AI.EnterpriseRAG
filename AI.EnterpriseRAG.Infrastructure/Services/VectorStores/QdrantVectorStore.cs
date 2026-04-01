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
                vectors = new QdrantVectorsConfig
                {
                    size = _config.VectorSize ?? 1536,
                    distance = string.IsNullOrEmpty(_config.DistanceMetric) ? "Cosine" : _config.DistanceMetric
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
                query = queryVector,
                limit = topK,
                with_payload = true,
                with_vector = false
            };

            if (filter != null && filter.Count > 0)
            {
                req.filter = new QdrantFilter();
                foreach (var kv in filter)
                {
                    if (kv.Value is List<string> values)
                    {
                        req.filter.must.Add(new QdrantCondition
                        {
                            key = kv.Key,
                            match = new QdrantMatch { values = values }
                        });
                    }
                    else
                    {
                        req.filter.must.Add(new QdrantCondition
                        {
                            key = kv.Key,
                            match = new QdrantMatch { value = kv.Value.ToString() }
                        });
                    }
                }
            }

            var url = $"collections/{collectionName ?? _config.CollectionName}/points/query";
            var resp = await Retry(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, req, AotJsonContext.Default.QdrantSearchPointsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync(url, content, cancellationToken);
            }, cancellationToken);

            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync(stream, AotJsonContext.Default.QdrantSearchResponse, cancellationToken);

            var list = new List<DocumentChunk>();
            if (result?.result == null) return list;

            foreach (var item in result.result)
            {
                var p = item.payload;
                list.Add(new DocumentChunk
                {
                    Id = Guid.Parse(p["chunk_id"].ToString()!),
                    DocumentId = Guid.Parse(p["document_id"].ToString()!),
                    Index = int.Parse(p["chunk_index"].ToString()!),
                    Content = p.TryGetValue("content", out var c) ? c.ToString() : "",
                    Similarity = (float)item.score
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
                    new() { key = "document_id", match = new QdrantMatch { value = documentId.ToString() } }
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
                    new() { key = "document_id", match = new QdrantMatch { values = documentIds.Select(g => g.ToString()).ToList() } }
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
        var req = new QdrantDeletePointsRequest
        {
            filter = new QdrantFilter
            {
                must = new List<QdrantCondition>
                {
                    new()
                    {
                        key = "create_time",
                        range = new QdrantRange { lt = true }
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
    public float[] query { get; set; } = Array.Empty<float>();
    public int limit { get; set; }
    public bool with_payload { get; set; }
    public bool with_vector { get; set; }
    public QdrantFilter? filter { get; set; }
}

public class QdrantSearchResponse
{
    public List<QdrantSearchResult> result { get; set; } = new();
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
    public QdrantMatch match { get; set; } = new();
    public QdrantRange range { get; set; } = new();
}

public class QdrantMatch
{
    public string? value { get; set; }
    public List<string>? values { get; set; }
}

public class QdrantRange
{
    public bool lt { get; set; }
}