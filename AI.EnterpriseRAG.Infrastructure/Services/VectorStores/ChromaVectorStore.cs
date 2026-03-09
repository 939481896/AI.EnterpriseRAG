using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure.Services.VectorStores;

/// <summary>
/// Chroma向量库实现（100%匹配官方curl示例）
/// 核心接口：add(插入)、query(检索)、collections(创建/查询)
/// </summary>
public class ChromaVectorStore : IVectorStore
{
    #region 配置与依赖
    private readonly HttpClient _httpClient;
    private readonly ChromaConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Chroma配置（对齐官方curl的路径参数）
    /// </summary>
    public class ChromaConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public int TimeoutSeconds { get; set; } = 30;
        public string Tenant { get; set; } = "default_tenant";
        public string Database { get; set; } = "default_database";
        public string CollectionName { get; set; } = "enterprise_rag_collection";

        public string CollectionId { get; set; }
        public int RetryCount { get; set; } = 2;
        public int RetryDelayMs { get; set; } = 1000;
    }
    #endregion

    #region 构造函数（依赖注入）
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

        // 初始化HttpClient（匹配官方请求头）
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.BaseUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        // JSON序列化（严格匹配官方camelCase）
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }
    #endregion

    #region 核心业务方法
    /// <summary>
    /// 初始化集合（POST /api/v2/tenants/{t}/databases/{d}/collections）
    /// 匹配官方curl：创建集合，带get_or_create=true
    /// </summary>
    public async Task<string> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 构造官方创建集合请求体（完全匹配curl示例）
            var createRequest = new CreateCollectionRequest
            {
                Name = _config.CollectionName,
                GetOrCreate = true, // 官方curl必选字段
                Metadata = new Dictionary<string, object>
                {
                    { "created_at", DateTime.UtcNow.ToString("o") },
                    { "description", "EnterpriseRAG vector collection" }
                },
                Configuration = null,
                Schema = null
            };

            // 调用官方创建集合接口
            var response = await RetryAsync(async () =>
            {
                return await _httpClient.PostAsJsonAsync(
                    GetCollectionsRootPath(),
                    createRequest,
                    _jsonOptions,
                    cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();
            var entity =await response.Content.ReadFromJsonAsync<GetResponse>();
            _config.CollectionId = entity?.id??"";
            return entity?.id??"";
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

    /// <summary>
    /// 插入向量（POST /api/v2/tenants/{t}/databases/{d}/collections/{name}/add）
    /// 完全匹配官方curl的add接口，而非upsert
    /// </summary>
    public async Task InsertAsync(DocumentChunk chunk, float[] vector, CancellationToken cancellationToken = default)
    {
        // 前置参数校验
        if (chunk == null) throw new BusinessException(400, "DocumentChunk不能为空");
        if (vector == null || vector.Length == 0) throw new BusinessException(400, "向量数据不能为空");
        if (string.IsNullOrEmpty(_config.CollectionName)) throw new BusinessException(400, "集合名称不能为空");

        try
        {
            // 构造官方add请求体（100%匹配curl示例）
            var addRequest = new AddRecordsRequest
            {
                Ids = new List<string>() { chunk.Id.ToString() },
                Embeddings = new List<float[]> { vector },

                Metadatas = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "document_id", chunk.DocumentId.ToString() },
                        { "chunk_index", chunk.Index },
                        { "chunk_id", chunk.Id.ToString() }
                    }
                },
                Documents = new List<string> { chunk.Content ?? string.Empty },
                Uris = null // 官方curl可选字段
            };
            // 调用官方add接口（路径完全匹配curl）
            var response = await RetryAsync(async () =>
           {
               return await _httpClient.PostAsJsonAsync(
                   $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}/add",
                   addRequest,
                   _jsonOptions,
                   cancellationToken);
           }, cancellationToken);
            // 解析官方错误响应
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode, $"向量插入失败：{response.StatusCode} - {errorMsg}");
            }
            // 更新向量JSON（保留原有业务逻辑）
            chunk.VectorJson = JsonSerializer.Serialize(vector, _jsonOptions);
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

    /// <summary>
    /// 检索向量（POST /api/v2/tenants/{t}/databases/{d}/collections/{name}/query）
    /// 匹配官方curl：用n_results而非k，保留where/where_document可选字段
    /// </summary>
    public async Task<List<DocumentChunk>> SearchAsync(float[] queryVector, int topK = 3, CancellationToken cancellationToken = default)
    {
        // 前置参数校验
        if (queryVector == null || queryVector.Length == 0) throw new BusinessException(400, "查询向量不能为空");
        if (topK < 1) throw new BusinessException(400, "TopK必须大于0");

        try
        {
            // 构造官方query请求体（完全匹配curl示例）
            var queryRequest = new QueryCollectionRequest
            {
                QueryEmbeddings = new List<float[]> { queryVector },
                NResults = topK, // 官方curl用n_results而非k
                Include = new List<string> { "distances", "metadatas", "documents", "embeddings" },
                Where = null, // 元数据过滤（可选）
                WhereDocument = null, // 文档内容过滤（可选）
                Ids = null // 指定ID检索（可选）
            };
            string path1 = $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionName)}/query";
            // 调用官方query接口（路径匹配curl）
            var response = await RetryAsync(async () =>
            {
                return await _httpClient.PostAsJsonAsync(
                    $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}/query",
                    queryRequest,
                    _jsonOptions,
                    cancellationToken);
            }, cancellationToken);

            response.EnsureSuccessStatusCode();

            // 解析官方query响应（匹配curl返回格式）
            var queryResponse = await response.Content.ReadFromJsonAsync<QueryCollectionResponse>(_jsonOptions, cancellationToken)
                                ?? new QueryCollectionResponse();

            // 转换为业务实体DocumentChunk
            var resultChunks = new List<DocumentChunk>();
            if (queryResponse.Ids == null || !queryResponse.Ids.Any() || queryResponse.Ids[0].Count == 0)
                return resultChunks;

            // 官方响应格式：二维数组（多查询场景），取第一个查询结果
            for (int i = 0; i < queryResponse.Ids[0].Count; i++)
            {
                var metadata = queryResponse.Metadatas?[0][i];
                if (metadata == null) continue;

                // ========== 相似度计算核心逻辑 ==========
                // 1. 获取Chroma返回的距离值（余弦距离，范围0~2）
                float distance = queryResponse.Distances?[0][i] ?? 0;
                // 2. 距离转相似度：余弦距离越小，相似度越高，公式：相似度 = 1 - (距离 / 2)
                float similarity = 1 - (distance / 2);
                // 3. 边界限制：确保相似度在0~1之间（避免异常值）
                similarity = Math.Max(0, Math.Min(1, similarity));
                // =======================================

                resultChunks.Add(new DocumentChunk
                {
                    Id = Guid.Parse(metadata["chunk_id"]!.ToString()!),
                    DocumentId = Guid.Parse(metadata["document_id"]!.ToString()!),
                    Index = int.Parse(metadata["chunk_index"]!.ToString()!),
                    Content = queryResponse.Documents?[0][i] ?? string.Empty,
                    VectorJson = JsonSerializer.Serialize(queryResponse.Embeddings?[0][i] ?? Array.Empty<float>(), _jsonOptions),
                    Similarity = similarity // 将计算后的相似度赋值给实体字段
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

    /// <summary>
    /// 根据文档ID删除集合内对应向量记录
    /// 匹配官方curl：POST /collections/{id}/delete + where条件过滤document_id
    /// </summary>
    /// <param name="documentId">文档ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        // 前置校验
        if (documentId == Guid.Empty) throw new BusinessException(400, "文档ID不能为空");
        if (string.IsNullOrEmpty(_config.CollectionId))
        {
            await InitAsync(cancellationToken); // 自动初始化集合
        }

        try
        {
            // 构造官方DELETE记录请求体（完全匹配curl，where为对象JSON，非序列化字符串）
            var deleteRequest = new DeleteRecordsRequest
            {
                Where = new Dictionary<string, object> // 直接传对象，HttpClient自动序列化为JSON
                {
                    { "document_id", documentId.ToString() }
                },
                Ids = null,
                WhereDocument = null
            };

            // 调用官方POST /delete接口（完全匹配curl，无额外header）
            var response = await RetryAsync(async () =>
            {
                return await _httpClient.PostAsJsonAsync(
                    $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}/delete",
                    deleteRequest,
                    _jsonOptions,
                    cancellationToken);
            }, cancellationToken);

            // 处理官方响应
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode,
                    $"删除文档{documentId}向量失败：{response.StatusCode} - {errorMsg}");
            }

            // 解析删除结果
            var deleteResponse = await response.Content.ReadFromJsonAsync<DeleteRecordsResponse>(_jsonOptions, cancellationToken);
            if (deleteResponse?.DeletedCount == 0)
            {
                throw new BusinessException(404, $"文档{documentId}未找到对应的向量记录");
            }

            //_logger?.LogInformation("文档{DocumentId}的向量记录已成功删除，共删除{Count}条",documentId, deleteResponse?.DeletedCount ?? 0);
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

    /// <summary>
    /// 批量删除指定文档ID的向量记录
    /// 匹配官方curl：POST /collections/{id}/delete + where.$in语法
    /// </summary>
    /// <param name="documentIds">文档ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BatchDeleteByDocumentIdsAsync(List<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        // 前置校验
        if (documentIds == null || !documentIds.Any()) throw new BusinessException(400, "文档ID列表不能为空");
        if (string.IsNullOrEmpty(_config.CollectionId))
        {
            await InitAsync(cancellationToken);
        }

        try
        {
            // 构造批量删除where条件（Chroma支持$in语法，完全匹配官方过滤规则）
            var deleteRequest = new DeleteRecordsRequest
            {
                Where = new Dictionary<string, object>
                {
                    {
                        "document_id",
                        new Dictionary<string, object> { { "$in", documentIds.Select(id => id.ToString()).ToList() } }
                    }
                },
                Ids = null,
                WhereDocument = null
            };

            // 调用官方删除接口
            var response = await RetryAsync(async () =>
            {
                return await _httpClient.PostAsJsonAsync(
                    $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}/delete",
                    deleteRequest,
                    _jsonOptions,
                    cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode,
                    $"批量删除{documentIds.Count}个文档向量失败：{response.StatusCode} - {errorMsg}");
            }

            var deleteResponse = await response.Content.ReadFromJsonAsync<DeleteRecordsResponse>(_jsonOptions, cancellationToken);
            //_logger?.LogInformation("批量删除向量记录完成，共删除{Count}条", deleteResponse?.DeletedCount ?? 0);
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

    /// <summary>
    /// 清空整个集合的所有向量记录（保留集合本身）
    /// 匹配官方curl：POST /collections/{id}/delete + 空where条件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.CollectionId))
        {
            await InitAsync(cancellationToken);
        }

        try
        {
            // 空where条件 = 删除集合内所有记录（保留集合）
            var deleteRequest = new DeleteRecordsRequest
            {
                Where = new Dictionary<string, object>(), // 空对象匹配所有记录
                Ids = null,
                WhereDocument = null
            };

            var response = await RetryAsync(async () =>
            {
                return await _httpClient.PostAsJsonAsync(
                    $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}/delete",
                    deleteRequest,
                    _jsonOptions,
                    cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode,
                    $"清空向量集合记录失败：{response.StatusCode} - {errorMsg}");
            }

            var deleteResponse = await response.Content.ReadFromJsonAsync<DeleteRecordsResponse>(_jsonOptions, cancellationToken);
            //_logger?.LogWarning("已清空向量集合{CollectionId}的所有记录，共删除{Count}条",_config.CollectionId, deleteResponse?.DeletedCount ?? 0);
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

    /// <summary>
    /// 删除过期向量记录（需插入时在metadata中添加create_time字段）
    /// </summary>
    /// <param name="expireTime">过期时间（UTC）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteExpiredAsync(DateTime expireTime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.CollectionId))
        {
            await InitAsync(cancellationToken);
        }

        try
        {
            var deleteRequest = new DeleteRecordsRequest
            {
                Where = new Dictionary<string, object>
                {
                    {
                        "create_time",
                        new Dictionary<string, object> { { "$lt", expireTime.ToString("o") } } // 时间戳用ISO8601格式
                    }
                },
                Ids = null,
                WhereDocument = null
            };

            var response = await RetryAsync(async () =>
            {
                return await _httpClient.PostAsJsonAsync(
                    $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}/delete",
                    deleteRequest,
                    _jsonOptions,
                    cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode,
                    $"删除过期向量失败：{response.StatusCode} - {errorMsg}");
            }

            var deleteResponse = await response.Content.ReadFromJsonAsync<DeleteRecordsResponse>(_jsonOptions, cancellationToken);
            //_logger?.LogInformation("删除过期向量记录完成，共删除{Count}条（过期时间：{ExpireTime}）",deleteResponse?.DeletedCount ?? 0, expireTime.ToString("o"));
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

    /// <summary>
    /// 删除整个向量集合（高危操作，集合被删除后需重新创建）
    /// 匹配官方curl：DELETE /collections/{id} 无请求体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_config.CollectionId))
        {
            throw new BusinessException(400, "向量集合未初始化，无法删除");
        }

        try
        {
            // 调用官方DELETE /collections/{id} 接口（完全匹配curl，无请求体）
            var response = await RetryAsync(async () =>
            {
                return await _httpClient.DeleteAsync(
                    $"{GetCollectionsRootPath()}/{Uri.EscapeDataString(_config.CollectionId)}",
                    cancellationToken);
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new BusinessException((int)response.StatusCode,
                    $"删除向量集合{_config.CollectionId}失败：{response.StatusCode} - {errorMsg}");
            }

            //_logger?.LogCritical("向量集合{CollectionId}已被永久删除，需重新初始化才能使用", _config.CollectionId);
            // 清空缓存的CollectionId，后续操作会重新创建
            _config.CollectionId = string.Empty;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"删除向量集合{_config.CollectionId}失败：{ex.Message}");
        }
    }

    #endregion

    #region 辅助方法（路径/重试）
    /// <summary>
    /// 获取集合根路径（完全匹配官方curl：/api/v2/tenants/{t}/databases/{d}/collections）
    /// </summary>
    private string GetCollectionsRootPath()
    {
        return $"/api/v2/tenants/{Uri.EscapeDataString(_config.Tenant)}/databases/{Uri.EscapeDataString(_config.Database)}/collections";
    }

    /// <summary>
    /// 重试机制（仅处理临时服务故障）
    /// </summary>
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

    /// <summary>
    /// 健康检查（匹配官方heartbeat接口）
    /// </summary>
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

    
    #endregion

    #region 官方API模型（100%匹配curl请求/响应）
    /// <summary>创建集合请求（匹配官方curl的POST collections请求体）</summary>
    private class CreateCollectionRequest
    {
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("get_or_create")]
        public bool GetOrCreate { get; set; } = true;
        public Dictionary<string, object>? Metadata { get; set; }
        public object? Configuration { get; set; }
        public object? Schema { get; set; }
    }

    /// <summary>插入向量请求（匹配官方curl的POST /add请求体）</summary>
    private class AddRecordsRequest
    {
        public List<string> Ids { get; set; } = new();
        public List<float[]> Embeddings { get; set; } = new();
        public List<Dictionary<string, object>>? Metadatas { get; set; }
        public List<string>? Documents { get; set; }
        public List<string>? Uris { get; set; }
    }

    /// <summary>检索向量请求（匹配官方curl的POST /query请求体）</summary>
    private class QueryCollectionRequest
    {
        [JsonPropertyName("query_embeddings")]
        public List<float[]> QueryEmbeddings { get; set; } = new();
        [JsonPropertyName("n_results")]
        public int NResults { get; set; } = 3;
        public List<string> Include { get; set; } = new();
        public string? Where { get; set; }
        [JsonPropertyName("where_document")]
        public string? WhereDocument { get; set; }
        public List<string>? Ids { get; set; }
    }

    /// <summary>检索向量响应（匹配官方curl的query响应格式）</summary>
    private class QueryCollectionResponse
    {
        public List<List<string>>? Ids { get; set; }
        public List<List<string>>? Documents { get; set; }
        public List<List<Dictionary<string, object>>>? Metadatas { get; set; }
        public List<List<float[]>>? Embeddings { get; set; }
        public List<List<float>>? Distances { get; set; }
    }

    private class GetResponse
    {
        public string id { get; set; }
        public string name { get; set; }
        public string tenant { get; set; }
        public string database { get; set; }
    }

    /// <summary>删除集合内记录请求（核心修正：匹配官方curl，无序列化字符串）</summary>
    private class DeleteRecordsRequest
    {
        public Dictionary<string, object>? Where { get; set; } // 元数据过滤：对象类型
        public List<string>? Ids { get; set; } // 指定ID删除：字符串数组
        [JsonPropertyName("where_document")]
        public Dictionary<string, object>? WhereDocument { get; set; } // 文档内容过滤：对象类型
    }

    /// <summary>删除集合内记录响应（匹配官方返回格式）</summary>
    private class DeleteRecordsResponse
    {
        [JsonPropertyName("deleted_count")]
        public int DeletedCount { get; set; } // 官方返回的删除数量
        public string? Status { get; set; } = "success"; // 官方返回的操作状态
    }
    #endregion
}
