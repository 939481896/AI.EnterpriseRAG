using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 角色管理接口
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class RoleController : BaseApiController
{
    private readonly AppEnterpriseAiContext _context;
    private readonly ILogger<RoleController> _logger;

    public RoleController(AppEnterpriseAiContext context, ILogger<RoleController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有角色
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Select(r => new
                {
                    r.Id,
                    r.RoleName,
                    r.RoleCode,
                    PermissionCount = r.RolePermissions.Count,
                    UserCount = r.UserRoles.Count
                })
                .ToListAsync();

            return Ok(Result<object>.SuccessResult(roles));
        });
    }

    /// <summary>
    /// 获取角色详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(long id)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Include(r => r.UserRoles)
                    .ThenInclude(ur => ur.User)
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.RoleName,
                    r.RoleCode,
                    Permissions = r.RolePermissions.Select(rp => new
                    {
                        rp.Permission.Id,
                        rp.Permission.Code,
                        rp.Permission.Name
                    }).ToList(),
                    Users = r.UserRoles.Select(ur => new
                    {
                        ur.User.Id,
                        ur.User.Account,
                        ur.User.UserName
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (role == null)
                return NotFound(Result.Fail(MessageResources.Get("role.notfound")));

            return Ok(Result<object>.SuccessResult(role));
        });
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            // 检查角色代码是否已存在
            if (await _context.Roles.AnyAsync(r => r.RoleCode == request.RoleCode))
                return BadRequest(Result.Fail(MessageResources.Get("role.code_exists")));

            var role = new SysRole
            {
                RoleName = request.RoleName,
                RoleCode = request.RoleCode
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("角色创建成功：{RoleCode}", role.RoleCode);

            return Ok(Result<object>.SuccessResult(new
            {
                role.Id,
                role.RoleName,
                role.RoleCode
            }, MessageResources.Get("role.create_success")));
        });
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(long id, [FromBody] UpdateRoleRequest request)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound(Result.Fail(MessageResources.Get("role.notfound")));

            // 防止修改系统角色代码
            if (role.RoleCode == "admin" && request.RoleCode != "admin")
                return BadRequest(Result.Fail(MessageResources.Get("role.cannot_modify_admin")));

            role.RoleName = request.RoleName;
            role.RoleCode = request.RoleCode;

            await _context.SaveChangesAsync();

            _logger.LogInformation("角色更新成功：{RoleId}", id);

            return Ok(Result.Success(MessageResources.Get("role.update_success")));
        });
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(long id)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return NotFound(Result.Fail(MessageResources.Get("role.notfound")));

            // 防止删除管理员角色
            if (role.RoleCode == "admin")
                return BadRequest(Result.Fail(MessageResources.Get("role.cannot_delete_admin")));

            // 检查是否有用户关联此角色
            var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
            if (hasUsers)
                return BadRequest(Result.Fail(MessageResources.Get("role.has_users")));

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("角色删除成功：{RoleId}", id);

            return Ok(Result.Success(MessageResources.Get("role.delete_success")));
        });
    }

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    [HttpPost("{roleId}/permissions")]
    public async Task<IActionResult> AssignPermissions(
        long roleId,
        [FromBody] AssignPermissionsRequest request)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return NotFound(Result.Fail(MessageResources.Get("role.notfound")));

            // 清除现有权限
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // 添加新权限
            foreach (var permissionId in request.PermissionIds)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("角色{RoleId}权限已更新", roleId);

            return Ok(Result.Success(MessageResources.Get("role.permissions_updated")));
        });
    }
}

// DTOs
public class CreateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class AssignPermissionsRequest
{
    public List<long> PermissionIds { get; set; } = new();
}
