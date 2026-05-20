using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

/// <summary>
/// 系统日志分析工具（根因分析）
/// </summary>
public class LogAnalysisTool : ITool
{
    private readonly ILogger<LogAnalysisTool> _logger;

    public string Name => "log_analysis";
    public string Description => "分析系统日志，识别错误模式、异常堆栈，进行根因分析。用于故障诊断和问题排查。";
    public string Category => "system";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""log_source"": {
      ""type"": ""string"",
      ""description"": ""日志来源（如application、system、api）""
    },
    ""time_range"": {
      ""type"": ""string"",
      ""description"": ""时间范围（如last_hour、last_day、custom）""
    },
    ""error_pattern"": {
      ""type"": ""string"",
      ""description"": ""错误模式关键词（可选）""
    }
  },
  ""required"": [""log_source""]
}";

    public LogAnalysisTool(ILogger<LogAnalysisTool> logger)
    {
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
            // 1. 解析参数
            if (!arguments.TryGetValue("log_source", out var logSourceObj))
                return ToolResult.Failure("缺少必需参数: log_source");

            var logSource = logSourceObj.ToString()!;
            var timeRange = arguments.TryGetValue("time_range", out var timeRangeObj)
                ? timeRangeObj.ToString()!
                : "last_hour";

            var errorPattern = arguments.TryGetValue("error_pattern", out var patternObj)
                ? patternObj.ToString()
                : null;

            _logger.LogInformation(
                "执行日志分析: Source={Source}, TimeRange={TimeRange}",
                logSource, timeRange);

            // 2. 读取日志文件（示例实现）
            var logEntries = await ReadLogsAsync(logSource, timeRange, errorPattern, cancellationToken);

            // 3. 分析错误模式
            var analysis = AnalyzeErrorPatterns(logEntries);

            sw.Stop();

            return ToolResult.Success(
                JsonSerializer.Serialize(new
                {
                    log_source = logSource,
                    time_range = timeRange,
                    total_entries = logEntries.Count,
                    error_count = analysis.ErrorCount,
                    warning_count = analysis.WarningCount,
                    top_errors = analysis.TopErrors,
                    root_cause_suggestions = analysis.RootCauseSuggestions,
                    analyzed_at = DateTime.UtcNow
                }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "日志分析失败");
            return ToolResult.Failure($"日志分析失败: {ex.Message}");
        }
    }

    private async Task<List<LogEntry>> ReadLogsAsync(
        string logSource,
        string timeRange,
        string? errorPattern,
        CancellationToken ct)
    {
        // 示例实现：从文件读取日志
        var logPath = Path.Combine("Logs", $"app-{DateTime.Now:yyyyMMdd}.log");
        var entries = new List<LogEntry>();

        if (!File.Exists(logPath))
            return entries;

        var lines = await File.ReadAllLinesAsync(logPath, ct);
        var cutoffTime = GetCutoffTime(timeRange);

        foreach (var line in lines.TakeLast(1000)) // 限制读取最近1000条
        {
            var entry = ParseLogLine(line);
            if (entry != null &&
                entry.Timestamp >= cutoffTime &&
                (errorPattern == null || line.Contains(errorPattern, StringComparison.OrdinalIgnoreCase)))
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private DateTime GetCutoffTime(string timeRange)
    {
        return timeRange switch
        {
            "last_hour" => DateTime.UtcNow.AddHours(-1),
            "last_day" => DateTime.UtcNow.AddDays(-1),
            "last_week" => DateTime.UtcNow.AddDays(-7),
            _ => DateTime.UtcNow.AddHours(-1)
        };
    }

    private LogEntry? ParseLogLine(string line)
    {
        try
        {
            // 简单解析格式: "2024-01-01 12:00:00 [ERR] Message"
            var parts = line.Split(' ', 3);
            if (parts.Length < 3) return null;

            var timestamp = DateTime.Parse($"{parts[0]} {parts[1]}");
            var level = parts[2].Trim('[', ']');
            var message = parts.Length > 3 ? parts[3] : "";

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message
            };
        }
        catch
        {
            return null;
        }
    }

    private LogAnalysis AnalyzeErrorPatterns(List<LogEntry> entries)
    {
        var errorCount = entries.Count(e => e.Level == "ERR");
        var warningCount = entries.Count(e => e.Level == "WRN");

        // 统计高频错误
        var topErrors = entries
            .Where(e => e.Level == "ERR")
            .GroupBy(e => ExtractErrorType(e.Message))
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new ErrorPattern
            {
                ErrorType = g.Key,
                Count = g.Count(),
                SampleMessage = g.First().Message
            })
            .ToList();

        // 生成根因建议
        var suggestions = GenerateRootCauseSuggestions(topErrors);

        return new LogAnalysis
        {
            ErrorCount = errorCount,
            WarningCount = warningCount,
            TopErrors = topErrors,
            RootCauseSuggestions = suggestions
        };
    }

    private string ExtractErrorType(string message)
    {
        // 简单提取异常类型（如"NullReferenceException"）
        var match = System.Text.RegularExpressions.Regex.Match(message, @"(\w+Exception)");
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    private List<string> GenerateRootCauseSuggestions(List<ErrorPattern> topErrors)
    {
        var suggestions = new List<string>();

        foreach (var error in topErrors)
        {
            if (error.ErrorType.Contains("NullReference"))
                suggestions.Add("检查空引用：确保对象在使用前已正确初始化");
            else if (error.ErrorType.Contains("Timeout"))
                suggestions.Add("超时问题：检查网络连接、数据库性能或外部服务可用性");
            else if (error.ErrorType.Contains("OutOfMemory"))
                suggestions.Add("内存不足：检查内存泄漏或增加系统内存配置");
            else
                suggestions.Add($"高频错误 {error.ErrorType}：建议详细检查相关代码逻辑");
        }

        return suggestions;
    }

    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
    }

    private class LogAnalysis
    {
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public List<ErrorPattern> TopErrors { get; set; } = new();
        public List<string> RootCauseSuggestions { get; set; } = new();
    }

    private class ErrorPattern
    {
        public string ErrorType { get; set; } = "";
        public int Count { get; set; }
        public string SampleMessage { get; set; } = "";
    }
}
