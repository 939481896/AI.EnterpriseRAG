using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Middleware;

/// <summary>
/// 请求日志增强中间件
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // 📝 获取用户信息
        var userId = context.User?.FindFirst("sub")?.Value ?? 
                     context.User?.FindFirst("userId")?.Value ?? 
                     "anonymous";
        var tenantId = context.User?.FindFirst("tenantId")?.Value ?? "default";
        var clientIp = GetClientIp(context);

        // 📝 记录请求开始
        _logger.LogInformation("🔵 请求开始: {Method} {Path} | User: {UserId} | TraceId: {TraceId} | IP: {IP}",
            context.Request.Method,
            context.Request.Path,
            userId,
            traceId,
            clientIp);

        // 📝 捕获请求体（仅POST/PUT且非文件上传）
        string? requestBody = null;
        if (ShouldLogRequestBody(context))
        {
            requestBody = await ReadRequestBodyAsync(context);
            if (!string.IsNullOrEmpty(requestBody) && requestBody.Length < 10000)
            {
                _logger.LogDebug("📥 Request Body: {Body}", requestBody);
            }
        }

        // 📝 捕获响应体
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "❌ 请求异常: {Method} {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms | UserId: {UserId} | TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                userId,
                traceId);

            throw;
        }
        finally
        {
            stopwatch.Stop();

            // 📝 读取响应体
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

            // 📝 记录响应日志
            var statusCode = context.Response.StatusCode;
            var isSuccess = statusCode >= 200 && statusCode < 300;
            var logLevel = isSuccess ? Microsoft.Extensions.Logging.LogLevel.Information :
                           statusCode >= 400 && statusCode < 500 ? Microsoft.Extensions.Logging.LogLevel.Warning :
                           Microsoft.Extensions.Logging.LogLevel.Error;

            _logger.Log(logLevel,
                "{Icon} 请求完成: {Method} {Path} | StatusCode: {StatusCode} | Duration: {Duration}ms | UserId: {UserId} | TraceId: {TraceId}",
                isSuccess ? "✅" : "❌",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                userId,
                traceId);

            // 📝 慢请求警告（>3秒）
            if (stopwatch.ElapsedMilliseconds > 3000)
            {
                _logger.LogWarning("⚠️ 慢请求检测: {Method} {Path} | Duration: {Duration}ms | UserId: {UserId} | TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    userId,
                    traceId);
            }

            // 📝 恢复响应流
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private bool ShouldLogRequestBody(HttpContext context)
    {
        var method = context.Request.Method;
        var contentType = context.Request.ContentType ?? "";

        return (method == "POST" || method == "PUT" || method == "PATCH") &&
               !contentType.Contains("multipart/form-data") &&
               !contentType.Contains("application/octet-stream");
    }

    private async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            // 简化版：仅支持已缓冲的Body
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
                using var reader = new StreamReader(
                    context.Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true);

                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                return body;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private string GetClientIp(HttpContext context)
    {
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                 context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                 context.Connection.RemoteIpAddress?.ToString() ??
                 "Unknown";

        return ip.Split(',').FirstOrDefault()?.Trim() ?? "Unknown";
    }
}
