using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Middleware;

/// <summary>
/// 全局请求日志 + 性能监控 + 审计日志
/// </summary>
public class GlobalLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalLogMiddleware> _logger;

    public GlobalLogMiddleware(RequestDelegate next, ILogger<GlobalLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8]; // 短请求ID
        var path = context.Request.Path;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "未登录";
        var permissions = string.Join(",", context.User.FindAll("perm").Select(c => c.Value));

        try
        {
            // 开始执行请求
            await _next(context);

            // 请求结束
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            // 结构化日志输出
            var log = new
            {
                RequestId = requestId,
                UserId = userId,
                IP = ip,
                Method = method,
                Path = path,
                StatusCode = statusCode,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                Permissions = permissions
            };

            _logger.LogInformation("[全局请求日志] {log}", JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = false }));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // 异常日志
            var errorLog = new
            {
                RequestId = requestId,
                UserId = userId,
                IP = ip,
                Method = method,
                Path = path,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Error = ex.Message,
                StackTrace = ex.StackTrace
            };

            _logger.LogError(ex, "[全局异常日志] {log}", JsonSerializer.Serialize(errorLog));
            throw; // 继续抛给全局异常处理
        }
    }
}