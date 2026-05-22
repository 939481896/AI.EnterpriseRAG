using AI.EnterpriseRAG.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;  // ← 明确指定System.IO

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 日志查询API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogger<LogsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 查询最近的应用日志
    /// </summary>
    [HttpGet("recent")]
    public IActionResult GetRecentLogs(
        [FromQuery] string? level = null,
        [FromQuery] int take = 100)
    {
        try
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            var latestFile = Directory.GetFiles(logsPath, "app-*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();

            if (latestFile == null)
            {
                return Ok(new { logs = Array.Empty<string>(), message = "暂无日志" });
            }

            var logs = System.IO.File.ReadLines(latestFile)
                .Reverse()
                .Take(take)
                .Where(line => string.IsNullOrEmpty(level) || line.Contains($"[{level.ToUpper()}]"))
                .ToList();

            return Ok(new { logs, count = logs.Count, file = Path.GetFileName(latestFile) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取日志失败");
            return StatusCode(500, new { error = "读取日志失败" });
        }
    }

    /// <summary>
    /// 根据TraceId查询请求链路
    /// </summary>
    [HttpGet("trace/{traceId}")]
    public IActionResult GetLogsByTraceId(string traceId)
    {
        try
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            var logFiles = Directory.GetFiles(logsPath, "app-*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .Take(7); // 查询最近7天

            var matchedLogs = new List<string>();
            foreach (var file in logFiles)
            {
                var logs = System.IO.File.ReadLines(file)
                    .Where(line => line.Contains(traceId))
                    .ToList();
                matchedLogs.AddRange(logs);
            }

            return Ok(new { 
                traceId, 
                logs = matchedLogs, 
                count = matchedLogs.Count 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询TraceId日志失败 | TraceId: {TraceId}", traceId);
            return StatusCode(500, new { error = "查询日志失败" });
        }
    }

    /// <summary>
    /// 查询错误日志
    /// </summary>
    [HttpGet("errors")]
    public IActionResult GetErrorLogs([FromQuery] int take = 50)
    {
        try
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            var latestErrorFile = Directory.GetFiles(logsPath, "errors-*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();

            if (latestErrorFile == null)
            {
                return Ok(new { errors = Array.Empty<string>(), message = "暂无错误日志" });
            }

            var content = System.IO.File.ReadAllText(latestErrorFile);
            var errors = content.Split("────────────────────────────────────")
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Take(take)
                .ToList();

            return Ok(new { 
                errors, 
                count = errors.Count, 
                file = Path.GetFileName(latestErrorFile) 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取错误日志失败");
            return StatusCode(500, new { error = "读取错误日志失败" });
        }
    }

    /// <summary>
    /// 查询性能慢请求
    /// </summary>
    [HttpGet("slow-requests")]
    public IActionResult GetSlowRequests([FromQuery] int minDuration = 3000)
    {
        try
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            var latestFile = Directory.GetFiles(logsPath, "app-*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();

            if (latestFile == null)
            {
                return Ok(new { requests = Array.Empty<string>(), message = "暂无日志" });
            }

            var slowRequests = System.IO.File.ReadLines(latestFile)
                .Where(line => line.Contains("⚠️ 慢请求检测"))
                .Take(100)
                .ToList();

            return Ok(new { 
                requests = slowRequests, 
                count = slowRequests.Count,
                threshold = minDuration 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询慢请求失败");
            return StatusCode(500, new { error = "查询慢请求失败" });
        }
    }

    /// <summary>
    /// 获取日志统计
    /// </summary>
    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        try
        {
            var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            var latestFile = Directory.GetFiles(logsPath, "app-*.log")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault();

            if (latestFile == null)
            {
                return Ok(new { message = "暂无日志" });
            }

            var lines = System.IO.File.ReadAllLines(latestFile);
            var stats = new
            {
                total = lines.Length,
                info = lines.Count(l => l.Contains("[INF]")),
                warning = lines.Count(l => l.Contains("[WRN]")),
                error = lines.Count(l => l.Contains("[ERR]")),
                debug = lines.Count(l => l.Contains("[DBG]")),
                file = Path.GetFileName(latestFile),
                size = $"{new FileInfo(latestFile).Length / 1024}KB"
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取日志统计失败");
            return StatusCode(500, new { error = "获取统计失败" });
        }
    }
}
