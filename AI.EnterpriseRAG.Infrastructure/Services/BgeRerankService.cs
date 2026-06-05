using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AI.EnterpriseRAG.Infrastructure.Services;

// 支持 bge-rerank / jina-rerank / 通义千问重排
public class BgeRerankService : IRerankService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BgeRerankService> _logger;

    // ✅ Constructor accepts HttpClient from DI (HttpClient factory pattern)
    public BgeRerankService(HttpClient httpClient, ILogger<BgeRerankService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri("http://localhost:8001");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<List<DocumentChunk>> RerankAsync(
        string query, List<DocumentChunk> chunks, int take = 3, CancellationToken ct = default)
    {
        if (chunks.Count <= take) return chunks;

        try
        {
            var request = new RerankRequest
            {
                query = query,
                texts = chunks.Select(x => x.Content).ToList()
            };

            var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, request, RerankJsonContext.Default.RerankRequest, ct);
            ms.Position = 0;
            var content = new StreamContent(ms);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var resp = await _httpClient.PostAsync("/rerank", content, ct);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var result = await JsonSerializer.DeserializeAsync(stream, RerankJsonContext.Default.RerankResponse, ct);

            return result!.Results
                .OrderByDescending(x => x.Score)
                .Take(take)
                .Select(x =>
                {
                    var c = chunks[x.Index];
                    c.Similarity = (float)x.Score;
                    return c;
                })
                .ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "⚠️ Rerank服务不可用，使用原始排序的前{Take}个结果", take);
            return chunks.Take(take).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Rerank服务异常，使用原始排序");
            return chunks.Take(take).ToList();
        }
    }
}

// AOT 模型
public class RerankRequest
{
    public string query { get; set; } = "";
    public List<string> texts { get; set; } = new();
}

public class RerankResponse
{
    public List<RerankResult> Results { get; set; } = new();
}

public class RerankResult
{
    public int Index { get; set; }
    public double Score { get; set; }
}

// AOT 序列化上下文
[JsonSerializable(typeof(RerankRequest))]
[JsonSerializable(typeof(RerankResponse))]
[JsonSerializable(typeof(RerankResult))]
public partial class RerankJsonContext : JsonSerializerContext
{
}