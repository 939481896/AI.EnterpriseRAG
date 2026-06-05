using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace AI.EnterpriseRAG.WebAPI.Middleware;

/// <summary>
/// 开发环境自动认证中间件
/// 用途：开发时绕过JWT验证，自动注入默认用户
/// ⚠️ 仅在Development环境启用
/// </summary>
public class DevAutoAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DevAutoAuthMiddleware> _logger;
    private readonly bool _isEnabled;

    public DevAutoAuthMiddleware(
        RequestDelegate next,
        IWebHostEnvironment env,
        ILogger<DevAutoAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _isEnabled = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 只在开发环境且未提供Token时生效
        if (_isEnabled && !context.Request.Headers.ContainsKey("Authorization"))
        {
            // 检查是否是需要认证的接口
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            if (NeedsAuthentication(path))
            {
                _logger.LogDebug("🔓 开发环境自动认证：{Path}", path);
                
                // 创建默认用户身份
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "admin"),
                    new Claim(ClaimTypes.NameIdentifier, "admin"),
                    new Claim("unique_name", "admin"),  // JWT标准Claim
                    new Claim("sub", "admin"),
                    new Claim("tid", "default"),  // 租户ID
                    new Claim("perm", "chat.ask"),
                    new Claim("perm", "doc.read"),
                    new Claim("perm", "doc.upload"),
                    new Claim("perm", "doc.delete"),
                };

                var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                context.User = principal;
                
                _logger.LogInformation("✅ 开发环境自动认证成功：{User}", "admin");
            }
        }

        await _next(context);
    }

    private bool NeedsAuthentication(string path)
    {
        // 需要认证的路径
        var protectedPaths = new[]
        {
            "/api/chat/",
            "/api/document/upload",
            "/api/document/delete",
            "/api/agent/"
        };

        // 不需要认证的路径
        var publicPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/register",
            "/health",
            "/swagger"
        };

        // 先检查公开路径
        if (publicPaths.Any(p => path.StartsWith(p)))
            return false;

        // 再检查保护路径
        return protectedPaths.Any(p => path.StartsWith(p));
    }
}

/// <summary>
/// 中间件扩展方法
/// </summary>
public static class DevAutoAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseDevAutoAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DevAutoAuthMiddleware>();
    }
}
