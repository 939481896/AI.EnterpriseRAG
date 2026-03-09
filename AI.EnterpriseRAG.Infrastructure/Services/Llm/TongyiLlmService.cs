using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Llm;

/// <summary>
/// 通义千问API服务（企业级实现）
/// </summary>
public class TongyiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly TongyiOptions _options;

    public string ModelName => "tongyi";

    public TongyiLlmService(HttpClient httpClient, IOptions<LlmOptions> llmOptions)
    {
        _httpClient = httpClient;
        _options = llmOptions.Value.Tongyi;

        // 企业级鉴权配置
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new BusinessException(500, "通义千问API Key未配置");

        try
        {
            var request = new
            {
                model = _options.ModelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.1f,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<TongyiResponse>(responseContent);

            return result?.Output?.Text?.Trim() ?? "未查询到相关答案";
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"通义千问调用失败：{ex.Message}");
        }
    }

    public async Task<float[]> EmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
            throw new BusinessException(500, "通义千问API Key未配置");

        try
        {
            var request = new
            {
                model = "text-embedding-v1",
                input = new { text = text }
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://dashscope.aliyuncs.com/api/v1/services/embeddings/text-embedding/text-embedding", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<TongyiEmbeddingResponse>(responseContent);

            return result?.Output?.Embeddings?[0]?.Embedding ?? Array.Empty<float>();
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"通义千问向量生成失败：{ex.Message}");
        }
    }

    #region 模型类
    private class TongyiResponse
    {
        public TongyiOutput Output { get; set; } = new();
    }

    private class TongyiOutput
    {
        public string Text { get; set; } = string.Empty;
    }

    private class TongyiEmbeddingResponse
    {
        public TongyiEmbeddingOutput Output { get; set; } = new();
    }

    private class TongyiEmbeddingOutput
    {
        public List<TongyiEmbeddingItem> Embeddings { get; set; } = new();
    }

    private class TongyiEmbeddingItem
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
    #endregion
}