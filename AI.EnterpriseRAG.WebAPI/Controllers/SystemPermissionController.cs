using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 系统权限管理接口
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class SystemPermissionController : BaseApiController
{
    private readonly AppEnterpriseAiContext _context;
    private readonly ILogger<SystemPermissionController> _logger;

    public SystemPermissionController(
        AppEnterpriseAiContext context,
        ILogger<SystemPermissionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有权限
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPermissions()
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var permissions = await _context.Permissions
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    RoleCount = p.RolePermissions.Count
                })
                .ToListAsync();

            return Ok(Result<object>.SuccessResult(permissions));
        });
    }

    /// <summary>
    /// 获取按模块分组的权限
    /// </summary>
    [HttpGet("grouped")]
    public async Task<IActionResult> GetGroupedPermissions()
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var permissions = await _context.Permissions
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    RoleCount = p.RolePermissions.Count
                })
                .ToListAsync();

            // Group permissions by module (first part of code before '.')
            var grouped = permissions
                .GroupBy(p => p.Code.Contains('.') ? p.Code.Split('.')[0] : "其他")
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );

            return Ok(Result<object>.SuccessResult(grouped));
        });
    }

    /// <summary>
    /// 获取权限详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPermission(long id)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                    .ThenInclude(rp => rp.Role)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Name,
                    Roles = p.RolePermissions.Select(rp => new
                    {
                        rp.Role.Id,
                        rp.Role.RoleName,
                        rp.Role.RoleCode
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (permission == null)
                return NotFound(Result.Fail(MessageResources.Get("permission.notfound")));

            return Ok(Result<object>.SuccessResult(permission));
        });
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            // 检查权限代码是否已存在
            if (await _context.Permissions.AnyAsync(p => p.Code == request.Code))
                return BadRequest(Result.Fail(MessageResources.Get("permission.code_exists")));

            var permission = new Permission
            {
                Code = request.Code,
                Name = request.Name
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("权限创建成功：{PermissionCode}", permission.Code);

            return Ok(Result<object>.SuccessResult(new
            {
                permission.Id,
                permission.Code,
                permission.Name
            }, MessageResources.Get("permission.create_success")));
        });
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePermission(long id, [FromBody] UpdatePermissionRequest request)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
                return NotFound(Result.Fail(MessageResources.Get("permission.notfound")));

            permission.Name = request.Name;
            permission.Code = request.Code;

            await _context.SaveChangesAsync();

            _logger.LogInformation("权限更新成功：{PermissionId}", id);

            return Ok(Result.Success(MessageResources.Get("permission.update_success")));
        });
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePermission(long id)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
                return NotFound(Result.Fail(MessageResources.Get("permission.notfound")));

            // 检查是否有角色关联此权限
            var hasRoles = await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == id);
            if (hasRoles)
                return BadRequest(Result.Fail(MessageResources.Get("permission.has_roles")));

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("权限删除成功：{PermissionId}", id);

            return Ok(Result.Success(MessageResources.Get("permission.delete_success")));
        });
    }
}

// DTOs
public class CreatePermissionRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UpdatePermissionRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
