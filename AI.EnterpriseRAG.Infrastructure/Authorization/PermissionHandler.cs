using AI.EnterpriseRAG.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Authorization;
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler( ILogger<PermissionHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 未登录 → 直接跳过，不检查权限
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            _logger.LogWarning("[权限拦截] 用户未登录，拒绝访问");

            return Task.CompletedTask;
        }
        // 核心校验：从 User.Claims 检查是否有对应 perm
        bool hasPermission = context.User.HasClaim(
            "perm",
            requirement.Permission
        );
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "未登录";
        var requirePerm = requirement.Permission;
        if (hasPermission)
        {
            _logger.LogInformation("[权限通过] 用户 {userId} 访问 {perm}",userId, requirePerm);
            context.Succeed(requirement); // 校验通过
        }
        else
        {
            _logger.LogWarning("[权限拦截] 用户 {userId} 缺少权限 {perm}", userId, requirePerm);
        }

        return Task.CompletedTask;
        /*
        // 超级管理员直接通过
        // ==========================================
        if (context.User.HasClaim("role", PermissionConstants.AdminRoleCode))
        {
            context.Succeed(requirement);
            return;
        }

        // ==========================================
        // 实时从 Redis 加载最新权限（立即生效）
        // ==========================================
        var cacheKey = string.Format(PermissionConstants.UserPermissionCacheKey, userId);
        var cachedPermissions = await _cache.GetStringAsync(cacheKey);

        List<string> userPermissions;

        if (string.IsNullOrEmpty(cachedPermissions))
        {
            // 这里你可以从数据库查最新权限
            userPermissions = context.User.FindAll("perm").Select(c => c.Value).ToList();
        }
        else
        {
            userPermissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(cachedPermissions)!;
        }

        // 校验权限
        if (userPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }*/
    }
}
