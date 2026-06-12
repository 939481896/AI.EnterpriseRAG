using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

[Route("api/permission")]
public class PermissionController : BaseApiController
{
    private readonly IMemoryCache _cache;

    public PermissionController(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 踢人下线（吊销Token）
    /// </summary>
    [HttpPost("revoke-token")]
    public  IActionResult RevokeToken([FromQuery] string token)
    {
        _cache.Set($"token:blacklist:{token}", "true", TimeSpan.FromMinutes(30));
        return Ok(MessageResources.Permission.TokenRevoked);
    }

    /// <summary>
    /// 刷新用户权限缓存
    /// </summary>
    [HttpPost("refresh-perm")]
    public  IActionResult RefreshPerm([FromQuery] long userId)
    {
        _cache.Remove($"user:perm:{userId}");
        return Ok(MessageResources.Permission.UserPermissionRefreshed);
    }
}
