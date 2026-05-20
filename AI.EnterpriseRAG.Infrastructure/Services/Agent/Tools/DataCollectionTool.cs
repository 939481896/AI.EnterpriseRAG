using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

/// <summary>
/// 数据采集工具（支持Web抓取、API调用）
/// </summary>
public class DataCollectionTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataCollectionTool> _logger;

    public string Name => "data_collection";
    public string Description => "从外部数据源采集数据，支持Web页面抓取、REST API调用。用于获取实时信息或外部系统数据。";
    public string Category => "data";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""source_type"": {
      ""type"": ""string"",
      ""enum"": [""web"", ""api""],
      ""description"": ""数据源类型: web(网页抓取) 或 api(REST API)""
    },
    ""url"": {
      ""type"": ""string"",
      ""description"": ""目标URL地址""
    },
    ""method"": {
      ""type"": ""string"",
      ""enum"": [""GET"", ""POST""],
      ""default"": ""GET"",
      ""description"": ""HTTP请求方法""
    },
    ""headers"": {
      ""type"": ""object"",
      ""description"": ""HTTP请求头（可选）""
    }
  },
  ""required"": [""source_type"", ""url""]
}";

    public DataCollectionTool(ILogger<DataCollectionTool> logger)
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EnterpriseRAG-Agent/1.0");
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. 参数验证
            if (!arguments.TryGetValue("source_type", out var sourceTypeObj) ||
                !arguments.TryGetValue("url", out var urlObj))
            {
                return ToolResult.Failure("缺少必需参数: source_type, url");
            }

            var sourceType = sourceTypeObj.ToString()!;
            var url = urlObj.ToString()!;

            // 安全性检查：防止SSRF攻击
            if (!IsUrlSafe(url))
                return ToolResult.Failure("不允许访问内部网络或敏感地址");

            var method = arguments.TryGetValue("method", out var methodObj)
                ? methodObj.ToString()!.ToUpper()
                : "GET";

            _logger.LogInformation(
                "执行数据采集: Type={Type}, URL={Url}, Method={Method}",
                sourceType, url, method);

            // 2. 执行数据采集
            string content;
            if (sourceType == "web")
            {
                content = await FetchWebPageAsync(url, cancellationToken);
            }
            else if (sourceType == "api")
            {
                content = await CallApiAsync(url, method, arguments, cancellationToken);
            }
            else
            {
                return ToolResult.Failure($"不支持的数据源类型: {sourceType}");
            }

            sw.Stop();

            // 3. 返回结果（限制长度避免Token溢出）
            var truncatedContent = content.Length > 10000
                ? content.Substring(0, 10000) + "...(内容已截断)"
                : content;

            return ToolResult.Success(
                JsonSerializer.Serialize(new
                {
                    source_type = sourceType,
                    url,
                    content_length = content.Length,
                    content = truncatedContent,
                    collected_at = DateTime.UtcNow
                }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据采集失败");
            return ToolResult.Failure($"数据采集失败: {ex.Message}");
        }
    }

    private async Task<string> FetchWebPageAsync(string url, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task<string> CallApiAsync(
        string url,
        string method,
        Dictionary<string, object> arguments,
        CancellationToken ct)
    {
        HttpResponseMessage response;

        if (method == "GET")
        {
            response = await _httpClient.GetAsync(url, ct);
        }
        else if (method == "POST")
        {
            var body = arguments.TryGetValue("body", out var bodyObj)
                ? JsonSerializer.Serialize(bodyObj)
                : "{}";
            var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(url, content, ct);
        }
        else
        {
            throw new NotSupportedException($"不支持的HTTP方法: {method}");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private bool IsUrlSafe(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // 禁止访问内网地址（简单实现，生产环境需更严格）
        var host = uri.Host.ToLower();
        if (host == "localhost" ||
            host == "127.0.0.1" ||
            host.StartsWith("192.168.") ||
            host.StartsWith("10.") ||
            host.StartsWith("172."))
        {
            return false;
        }

        return uri.Scheme == "http" || uri.Scheme == "https";
    }
}
