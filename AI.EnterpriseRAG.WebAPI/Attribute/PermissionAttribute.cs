
using Microsoft.AspNetCore.Authorization;
namespace AI.EnterpriseRAG.WebAPI.Attribute;
public class PermissionAttribute : AuthorizeAttribute
{
    // 固定策略名
    private const string PolicyPrefix = "PermissionPolicy_";

    public PermissionAttribute(string permission)
    {
        Policy = $"{PolicyPrefix}{permission}";
    }
}