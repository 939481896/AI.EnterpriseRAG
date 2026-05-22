using AI.EnterpriseRAG.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UnstructuredClient> _logger;
        private bool _serviceAvailable = true; // 服务可用性标记

        public UnstructuredClient(
            IOptions<VectorStoreOptions> vectorStoreOptions,
            ILogger<UnstructuredClient> logger)
        {
            _options = vectorStoreOptions.Value.Unstructured;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _options.ApiKey);
            _httpClient.Timeout = TimeSpan.FromMinutes(8);
        }

        public async Task<List<UnstructuredChunk>> ParseDocumentAsync(
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            // 🔧 修复：如果服务不可用，使用降级方案
            if (!_serviceAvailable)
            {
                _logger.LogWarning("Unstructured服务不可用，使用简单文本解析（降级方案）");
                return await FallbackParseAsync(fileStream, fileName, cancellationToken);
            }

            try
            {
                using var content = new MultipartFormDataContent();
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                content.Add(streamContent, "file", fileName);

                _logger.LogInformation("正在调用Unstructured服务解析文档: {FileName}", fileName);

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

                _logger.LogInformation("文档解析成功，共 {Count} 个分块", result?.Chunks?.Count ?? 0);
                return result?.Chunks ?? [];
            }
            catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
            {
                // 🔧 修复：连接失败时标记服务不可用，使用降级方案
                _serviceAvailable = false;
                _logger.LogWarning(ex,
                    "Unstructured服务连接失败（{Url}），切换到降级方案。" +
                    "提示：如需完整解析功能，请启动Python服务：python AI.EnterpriseRAG.Parser\\unstructured_api.py",
                    _options.ApiUrl);

                // 使用降级方案
                fileStream.Position = 0; // 重置流位置
                return await FallbackParseAsync(fileStream, fileName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档解析失败: {FileName}", fileName);

                // 尝试降级方案
                try
                {
                    fileStream.Position = 0;
                    return await FallbackParseAsync(fileStream, fileName, cancellationToken);
                }
                catch
                {
                    throw; // 降级方案也失败，抛出原始异常
                }
            }
        }

        /// <summary>
        /// 降级方案：简单的文本分块（当Unstructured服务不可用时）
        /// </summary>
        private async Task<List<UnstructuredChunk>> FallbackParseAsync(
            Stream fileStream,
            string fileName,
            CancellationToken cancellationToken)
        {
            var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

            // 只支持纯文本文件的简单解析
            if (fileExtension != ".txt")
            {
                _logger.LogWarning(
                    "降级方案仅支持.txt文件，当前文件: {FileName}。" +
                    "PDF/DOCX文件需要启动Unstructured服务才能解析。",
                    fileName);

                return new List<UnstructuredChunk>
                {
                    new UnstructuredChunk
                    {
                        ChunkId = "fallback-0",
                        Title = Path.GetFileNameWithoutExtension(fileName),
                        Content = $"【降级提示】文件 {fileName} 需要完整的解析服务支持。" +
                                  "请启动Python服务：cd AI.EnterpriseRAG.Parser && python unstructured_api.py",
                        FileType = fileExtension,
                        PageNumber = 1
                    }
                };
            }

            // 读取文本内容
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync(cancellationToken);

            // 简单分块（每500字符一块）
            var chunks = new List<UnstructuredChunk>();
            var chunkSize = 500;
            var overlap = 50;

            for (int i = 0; i < content.Length; i += (chunkSize - overlap))
            {
                var length = Math.Min(chunkSize, content.Length - i);
                var chunkContent = content.Substring(i, length);

                chunks.Add(new UnstructuredChunk
                {
                    ChunkId = $"fallback-{chunks.Count}",
                    Title = $"{Path.GetFileNameWithoutExtension(fileName)} - 分块{chunks.Count + 1}",
                    Content = chunkContent,
                    FileType = "txt",
                    PageNumber = chunks.Count + 1
                });
            }

            _logger.LogInformation(
                "使用降级方案解析完成: {FileName}, 共 {Count} 个分块",
                fileName, chunks.Count);

            return chunks;
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
