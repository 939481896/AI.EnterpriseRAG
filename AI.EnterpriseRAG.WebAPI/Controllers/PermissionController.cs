using AI.EnterpriseRAG.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

[ApiController]
[Route("api/permission")]
public class PermissionController : ControllerBase
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
        return Ok("Token已吊销，用户将强制下线");
    }

    /// <summary>
    /// 刷新用户权限缓存
    /// </summary>
    [HttpPost("refresh-perm")]
    public  IActionResult RefreshPerm([FromQuery] long userId)
    {
        _cache.Remove($"user:perm:{userId}");
        return Ok("用户权限已刷新，下次接口自动重新加载");
    }
}
