using AI.EnterpriseRAG.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
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
        // 使用和Controller一样的fallback链读取用户ID
        var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                     ?? context.User.FindFirstValue(ClaimTypes.Name)
                     ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? "anonymous";

        var path = context.Request.Path;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString();

        // 这里可以存数据库
        _logger.LogInformation($"【权限审计】用户 {userId} 访问 {method} {path} IP:{ip}");
        await _next(context);
    }
}
