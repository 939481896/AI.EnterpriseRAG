using AI.EnterpriseRAG.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace AI.EnterpriseRAG.Infrastructure.Authorization;

public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string PermissionPolicyPrefix = "PermissionPolicy_";

    // 缓存动态生成的策略，避免重复构建
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policyCache = new();

    // 包装默认提供者，处理非动态策略
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider;

    public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _defaultProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // 1. 先尝试从默认提供者获取（支持 Roles、静态策略等）
        var policy = await _defaultProvider.GetPolicyAsync(policyName);
        if (policy != null)
            return policy;

        // 2. 处理动态权限策略
        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return _policyCache.GetOrAdd(policyName, name =>
            {
                var permission = name[PermissionPolicyPrefix.Length..]; // 更高效地移除前缀
                return new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
            });
        }

        // 3. 未识别的策略，返回 null（后续可能触发 Fallback）
        return null;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _defaultProvider.GetDefaultPolicyAsync(); // 直接委托

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _defaultProvider.GetFallbackPolicyAsync(); // 直接委托
}