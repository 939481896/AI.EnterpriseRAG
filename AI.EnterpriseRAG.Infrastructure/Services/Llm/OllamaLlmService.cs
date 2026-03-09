

using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Llm;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmOptions _options;

    public string ModelName => "ollama";

    public OllamaLlmService(HttpClient httpClient, IOptions<LlmOptions> llmOptions)
    {
        _httpClient = httpClient;
        _options = llmOptions.Value;
        _httpClient.BaseAddress = new Uri(_options.Ollama.BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new { 
                model= _options.Ollama.ModelName, 
                prompt=prompt,
                stream=false,
                options = new { temperature = 0.1f } // 企业级低随机性配置

            };
            var response = await _httpClient.PostAsJsonAsync("/api/generate", request,cancellationToken);
            response.EnsureSuccessStatusCode();
            var result=await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken);
            return result?.Response?.Trim() ?? "未查询到相关答案";
        }
        catch (Exception ex) {
            throw new BusinessException(500, $"Ollama模型调用失败：{ex.Message}");
        }
        finally { }
    }

    public async Task<float[]> EmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. 校验输入文本（避免空文本）
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new BusinessException(400, "生成向量的输入文本不能为空");
            }

            var request = new
            {
                model = _options.Ollama.EmbeddingModelName,
                input = text
            };

            // 2. 修正接口路径：/api/embeddings（不是 /api/embed）
            var response = await _httpClient.PostAsJsonAsync("/api/embed", request, cancellationToken);
            response.EnsureSuccessStatusCode(); // 确保HTTP请求成功

            // 3. 解析响应
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);

            // 4. 处理二维数组：取第一个元素（单文本的向量）
            var vector = result?.embeddings?.FirstOrDefault() ?? new List<float>();

            // 5. 转换为 float[] 并返回（空时返回空数组）
            return vector.ToArray();
        }
        catch (HttpRequestException ex)
        {
            throw new BusinessException(500, $"Ollama接口调用失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            throw new BusinessException(500, $"Ollama向量生成失败：{ex.Message}");
        }
    }

    #region 模型类
    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
    }

    private class OllamaEmbeddingResponse
    {
        // 
        public string model { get; set; }
        // 接口返回的是二维数组（List<List<float>>）
        public List<List<float>> embeddings { get; set; }
        public long total_duration { get; set; }
    }
    #endregion
}
