using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers
{
    // 支持 AOT 的客户端
    public class UnstructuredClient
    {
        private readonly HttpClient _httpClient;
        private readonly UnstructuredOptions _options;

        public UnstructuredClient(IOptions<VectorStoreOptions> vectorStoreOptions)
        {
            _options = vectorStoreOptions.Value.Unstructured;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
            _httpClient.Timeout = TimeSpan.FromMinutes(8);
        }

        public async Task<List<UnstructuredChunk>> ParseDocumentAsync(
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync(
                $"{_options.ApiUrl}/parse-document",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            // AOT 安全：使用 JsonSerializerContext
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync(
                stream,
                UnstructuredJsonContext.Default.UnstructuredResponse,
                cancellationToken);

            return result?.Chunks ?? [];
        }
    }



    // ==========================================
    // 以下是 AOT 核心：Source Generation 模型
    // ==========================================
    public class UnstructuredResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("chunks")]
        public List<UnstructuredChunk> Chunks { get; set; } = new();

        [JsonPropertyName("total_chunks")]
        public int TotalChunks { get; set; }
    }

    public class UnstructuredChunk
    {
        [JsonPropertyName("chunk_id")]
        public string ChunkId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty;

        [JsonPropertyName("page_number")]
        public int PageNumber { get; set; }
    }

    // AOT 序列化上下文
    [JsonSerializable(typeof(UnstructuredResponse))]
    [JsonSerializable(typeof(List<UnstructuredChunk>))]
    public partial class UnstructuredJsonContext : JsonSerializerContext
    {
    }
}
