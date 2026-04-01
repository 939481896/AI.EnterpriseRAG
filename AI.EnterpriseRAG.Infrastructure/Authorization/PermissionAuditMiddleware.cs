using AI.EnterpriseRAG.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AI.EnterpriseRAG.Infrastructure.Authorization;

public class PermissionAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalLogMiddleware> _logger;


    public PermissionAuditMiddleware(RequestDelegate next, ILogger<GlobalLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var path = context.Request.Path;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString();

        // 这里可以存数据库
        //Console.WriteLine($"【权限审计】用户 {userId} 访问 {method} {path} IP:{ip}");
        _logger.LogInformation($"【权限审计】用户 {userId} 访问 {method} {path} IP:{ip}");
        await _next(context);
    }
}
