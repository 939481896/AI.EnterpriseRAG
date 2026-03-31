using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure.Services.VectorStores;

public class ChromaVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly ChromaConfig _config;
    private bool _disposed;

    public class ChromaConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public int TimeoutSeconds { get; set; } = 30;
        public string Tenant { get; set; } = "default_tenant";
        public string Database { get; set; } = "default_database";
        public string CollectionName { get; set; } = "enterprise_rag_collection";
        public string CollectionId { get; set; } = string.Empty;
        public int RetryCount { get; set; } = 2;
        public int RetryDelayMs { get; set; } = 1000;
    }

    public ChromaVectorStore(IOptions<VectorStoreOptions> vectorStoreOptions)
    {
        var originOptions = vectorStoreOptions.Value.Chroma;
        _config = new ChromaConfig
        {
            BaseUrl = string.IsNullOrEmpty(originOptions.BaseUrl) ? "http://localhost:8000" : originOptions.BaseUrl,
            TimeoutSeconds = originOptions.Timeout ?? 30,
            Tenant = originOptions.Tenant ?? "default_tenant",
            Database = originOptions.Database ?? "default_database",
            CollectionName = string.IsNullOrEmpty(originOptions.CollectionName) ? "enterprise_rag_collection" : originOptions.CollectionName,
            RetryCount = originOptions.RetryCount ?? 2,
            RetryDelayMs = originOptions.RetryDelayMilliseconds ?? 1000
        };

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var createRequest = new ChromaCreateCollectionRequest
            {
                Name = _config.CollectionName,
                GetOrCreate = true,
                Metadata = new Dictionary<string, object>
                {
                    { "created_at", DateTime.UtcNow.ToString("o") },
                    { "description", "EnterpriseRAG vector collection" }
                }
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, createRequest, AotJsonContext.Default.ChromaCreateCollectionRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync(GetCollectionsRootPath(), content, cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var entity = await JsonSerializer.DeserializeAsync(stream, AotJsonContext.Default.ChromaGetResponse, cancellationToken);

            _config.CollectionId = entity?.id ?? "";
            return entity?.id ?? "";
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"初始化Chroma集合失败：{ex.Message}");
        }
    }

    public async Task InsertAsync(DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default)
    {
        if (chunk == null) throw new BusinessException(400, "DocumentChunk不能为空");
        if (vector == null || vector.Length == 0) throw new BusinessException(400, "向量数据不能为空");

        if (string.IsNullOrEmpty(_config.CollectionId))
            await InitAsync(cancellationToken);

        try
        {
            var addRequest = new ChromaAddRecordsRequest
            {
                Ids = new List<string> { chunk.Id.ToString() },
                Embeddings = new List<float[]> { vector },
                Metadatas = new List<Dictionary<string, object>>
                {
                    new()
                    {
                        { "document_id", chunk.DocumentId.ToString() },
                        { "chunk_index", chunk.Index },
                        { "chunk_id", chunk.Id.ToString() }
                    }
                },
                Documents = new List<string> { chunk.Content ?? string.Empty }
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, addRequest, AotJsonContext.Default.ChromaAddRecordsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}/add", content, cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode, $"向量插入失败：{response.StatusCode} - {errorMsg}");
            }

            chunk.VectorJson = JsonSerializer.Serialize(vector, AotJsonContext.Default.SingleArray);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"向量插入失败：{ex.Message}");
        }
    }

    public async Task<List<DocumentChunk>> SearchAsync(string collectionName, float[] queryVector, int topK = 3, Dictionary<string, object>? filter = null, CancellationToken cancellationToken = default)
    {
        if (queryVector == null || queryVector.Length == 0)
            throw new BusinessException(400, "查询向量不能为空");

        if (string.IsNullOrEmpty(_config.CollectionId))
            await InitAsync(cancellationToken);

        try
        {
            var queryRequest = new ChromaQueryCollectionRequest
            {
                QueryEmbeddings = new List<float[]> { queryVector },
                NResults = topK,
                Include = new List<string> { "distances", "metadatas", "documents", "embeddings" },
                Where=filter
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, queryRequest, AotJsonContext.Default.ChromaQueryCollectionRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}/query", content, cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var queryResponse = await JsonSerializer.DeserializeAsync(stream, AotJsonContext.Default.ChromaQueryCollectionResponse, cancellationToken)
                                ?? new ChromaQueryCollectionResponse();

            var resultChunks = new List<DocumentChunk>();
            if (queryResponse.Ids == null || !queryResponse.Ids.Any() || queryResponse.Ids[0].Count == 0)
                return resultChunks;

            for (int i = 0; i < queryResponse.Ids[0].Count; i++)
            {
                var metadata = queryResponse.Metadatas?[0][i];
                if (metadata == null) continue;

                float distance = queryResponse.Distances?[0][i] ?? 0;
                float similarity = 1 - (distance / 2);
                similarity = Math.Max(0, Math.Min(1, similarity));

                resultChunks.Add(new DocumentChunk
                {
                    Id = Guid.Parse(metadata["chunk_id"]!.ToString()!),
                    DocumentId = Guid.Parse(metadata["document_id"]!.ToString()!),
                    Index = int.Parse(metadata["chunk_index"]!.ToString()!),
                    Content = queryResponse.Documents?[0][i] ?? string.Empty,
                    Similarity = similarity
                });
            }

            return resultChunks;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"向量检索失败：{ex.Message}");
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
        if (documentId == Guid.Empty) throw new BusinessException(400, "文档ID不能为空");
        if (string.IsNullOrEmpty(_config.CollectionId)) await InitAsync(cancellationToken);

        try
        {
            var deleteRequest = new ChromaDeleteRecordsRequest
            {
                Where = new Dictionary<string, object>
                {
                    { "document_id", documentId.ToString() }
                }
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, deleteRequest, AotJsonContext.Default.ChromaDeleteRecordsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}/delete", content, cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode, $"删除文档{documentId}向量失败：{response.StatusCode} - {errorMsg}");
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"删除文档{documentId}向量失败：{ex.Message}");
        }
    }

    public async Task BatchDeleteByDocumentIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        if (documentIds == null || !documentIds.Any()) throw new BusinessException(400, "文档ID列表不能为空");
        if (string.IsNullOrEmpty(_config.CollectionId)) await InitAsync(cancellationToken);

        try
        {
            var deleteRequest = new ChromaDeleteRecordsRequest
            {
                Where = new Dictionary<string, object>
                {
                    {
                        "document_id",
                        new Dictionary<string, object> { { "$in", documentIds.Select(id => id.ToString()).ToList() } }
                    }
                }
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, deleteRequest, AotJsonContext.Default.ChromaDeleteRecordsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}/delete", content, cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode, $"批量删除失败：{response.StatusCode} - {errorMsg}");
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"批量删除文档向量失败：{ex.Message}");
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.CollectionId)) await InitAsync(cancellationToken);

        try
        {
            var deleteRequest = new ChromaDeleteRecordsRequest
            {
                Where = new Dictionary<string, object>()
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, deleteRequest, AotJsonContext.Default.ChromaDeleteRecordsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}/delete", content, cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"清空向量集合记录失败：{ex.Message}");
        }
    }

    public async Task DeleteExpiredAsync(DateTime expireTime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.CollectionId)) await InitAsync(cancellationToken);

        try
        {
            var deleteRequest = new ChromaDeleteRecordsRequest
            {
                Where = new Dictionary<string, object>
                {
                    {
                        "create_time",
                        new Dictionary<string, object> { { "$lt", expireTime.ToString("o") } }
                    }
                }
            };

            var response = await RetryAsync(async () =>
            {
                var ms = new MemoryStream();
                await JsonSerializer.SerializeAsync(ms, deleteRequest, AotJsonContext.Default.ChromaDeleteRecordsRequest, cancellationToken);
                ms.Position = 0;
                var content = new StreamContent(ms);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return await _httpClient.PostAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}/delete", content, cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"删除过期向量失败：{ex.Message}");
        }
    }

    public async Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.CollectionId))
            throw new BusinessException(400, "向量集合未初始化，无法删除");

        try
        {
            var response = await RetryAsync(async () =>
            {
                return await _httpClient.DeleteAsync($"{GetCollectionsRootPath()}/{_config.CollectionId}", cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();
            _config.CollectionId = string.Empty;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"删除向量集合失败：{ex.Message}");
        }
    }

    private string GetCollectionsRootPath()
    {
        return $"/api/v2/tenants/{_config.Tenant}/databases/{_config.Database}/collections";
    }

    private async Task<HttpResponseMessage> RetryAsync(Func<Task<HttpResponseMessage>> requestFunc, CancellationToken cancellationToken)
    {
        int retry = 0;
        while (retry <= _config.RetryCount)
        {
            try
            {
                return await requestFunc();
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout && retry < _config.RetryCount)
            {
                retry++;
                await Task.Delay(_config.RetryDelayMs, cancellationToken);
            }
        }
        return await requestFunc();
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v2/heartbeat", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
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

#region AOT 模型
public class ChromaCreateCollectionRequest
{
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("get_or_create")]
    public bool GetOrCreate { get; set; } = true;
    public Dictionary<string, object>? Metadata { get; set; }
    public object? Configuration { get; set; }
    public object? Schema { get; set; }
}

public class ChromaAddRecordsRequest
{
    public List<string> Ids { get; set; } = new();
    public List<float[]> Embeddings { get; set; } = new();
    public List<Dictionary<string, object>>? Metadatas { get; set; }
    public List<string>? Documents { get; set; }
    public List<string>? Uris { get; set; }
}

public class ChromaQueryCollectionRequest
{
    [JsonPropertyName("query_embeddings")]
    public List<float[]> QueryEmbeddings { get; set; } = new();
    [JsonPropertyName("n_results")]
    public int NResults { get; set; } = 3;
    public List<string> Include { get; set; } = new();
    public Dictionary<string, object>? Where { get; set; }
    [JsonPropertyName("where_document")]
    public Dictionary<string, object>? WhereDocument { get; set; }
    public List<string>? Ids { get; set; }
}

public class ChromaQueryCollectionResponse
{
    public List<List<string>>? Ids { get; set; }
    public List<List<string>>? Documents { get; set; }
    public List<List<Dictionary<string, object>>>? Metadatas { get; set; }
    public List<List<float[]>>? Embeddings { get; set; }
    public List<List<float>>? Distances { get; set; }
}

public class ChromaGetResponse
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string tenant { get; set; } = string.Empty;
    public string database { get; set; } = string.Empty;
}

public class ChromaDeleteRecordsRequest
{
    public Dictionary<string, object>? Where { get; set; }
    public List<string>? Ids { get; set; }
    [JsonPropertyName("where_document")]
    public Dictionary<string, object>? WhereDocument { get; set; }
}

public class ChromaDeleteRecordsResponse
{
    [JsonPropertyName("deleted_count")]
    public int DeletedCount { get; set; }
}
#endregion