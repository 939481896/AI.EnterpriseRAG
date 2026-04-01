using AI.EnterpriseRAG.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AI.EnterpriseRAG.Infrastructure.Authorization;

public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("PermissionPolicy_"))
        {
            var permission = policyName.Replace("PermissionPolicy_", "");

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult(policy)!;
        }

        return Task.FromResult<AuthorizationPolicy?>(null);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return Task.FromResult(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build());
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult<AuthorizationPolicy?>(null);
    }
}
