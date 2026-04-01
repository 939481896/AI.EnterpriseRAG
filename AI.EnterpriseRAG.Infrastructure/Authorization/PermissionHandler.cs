using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AI.EnterpriseRAG.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AI.EnterpriseRAG.Infrastructure.Authorization;
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 未登录 → 直接跳过，不检查权限
        if (context.User.Identity is not { IsAuthenticated: true })
        {
            return Task.CompletedTask;
        }
        // 核心校验：从 User.Claims 检查是否有对应 perm
        bool hasPermission = context.User.HasClaim(
            "perm",
            requirement.Permission
        );

        if (hasPermission)
        {
            context.Succeed(requirement); // 校验通过
        }

        return Task.CompletedTask;
    }
}
