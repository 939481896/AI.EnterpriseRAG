
using AI.EnterpriseRAG.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

using System.Net.Http;
using System.Security.Claims;

namespace AI.EnterpriseRAG.Infrastructure.Security;

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public TokenBlacklistMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            // 内存判断Token是否被吊销
            if (_cache.TryGetValue($"token:blacklist:{token}", out _))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token已被管理员吊销");
                return;
            }
        }

        await _next(context);
    }
}
