using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.WebAPI.Services;

/// <summary>
/// 数据库种子数据初始化服务
/// </summary>
public class DatabaseSeeder
{
    private readonly AppEnterpriseAiContext _context;
    private readonly IPasswordHasher<SysUser> _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppEnterpriseAiContext context,
        IPasswordHasher<SysUser> passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// 初始化所有种子数据
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("开始初始化种子数据...");

            await SeedPermissionsAsync();
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await AssignAdminPermissionsAsync();

            _logger.LogInformation("✅ 种子数据初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 种子数据初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 初始化系统权限
    /// </summary>
    private async Task SeedPermissionsAsync()
    {
        _logger.LogInformation("检查系统权限...");

        var permissionDefinitions = new List<(string Code, string Name)>
        {
            // ==================== 菜单权限 ====================
            ("menu.admin", "访问管理后台"),
            ("menu.user", "访问用户管理菜单"),
            ("menu.role", "访问角色管理菜单"),
            ("menu.permission", "访问权限管理菜单"),
            ("menu.document", "访问文档管理菜单"),
            ("menu.chat", "访问智能问答菜单"),
            ("menu.agent", "访问Agent工作区菜单"),

            // ==================== 用户管理权限 ====================
            ("user.read", "查看用户"),
            ("user.create", "创建用户"),
            ("user.update", "更新用户"),
            ("user.delete", "删除用户"),
            ("user.manage", "管理用户"),

            // ==================== 角色管理权限 ====================
            ("role.read", "查看角色"),
            ("role.create", "创建角色"),
            ("role.update", "更新角色"),
            ("role.delete", "删除角色"),
            ("role.manage", "管理角色"),

            // ==================== 权限管理权限 ====================
            ("permission.read", "查看权限"),
            ("permission.create", "创建权限"),
            ("permission.update", "更新权限"),
            ("permission.delete", "删除权限"),
            ("permission.manage", "管理权限"),

            // ==================== 文档管理权限 ====================
            ("doc.read", "查看文档"),
            ("doc.upload", "上传文档"),
            ("doc.delete", "删除文档"),
            ("doc.share", "分享文档"),
            ("doc.manage", "管理文档"),

            // ==================== 对话管理权限 ====================
            ("chat.read", "查看对话"),
            ("chat.ask", "发起问答"),
            ("chat.history", "查看历史"),
            ("chat.delete", "删除对话"),
            ("chat.manage", "管理对话"),

            // ==================== Agent管理权限 ====================
            ("agent.read", "查看Agent"),
            ("agent.execute", "执行Agent"),
            ("agent.manage", "管理Agent"),

            // ==================== 系统管理权限 ====================
            ("system.read", "查看系统信息"),
            ("system.config", "系统配置"),
            ("system.logs", "查看日志"),
            ("system.monitor", "系统监控"),
            ("system.manage", "系统管理"),
        };

        // Get existing permission codes
        var existingCodes = await _context.Permissions
            .Select(p => p.Code)
            .ToListAsync();

        // Find missing permissions
        var missingPermissions = permissionDefinitions
            .Where(def => !existingCodes.Contains(def.Code))
            .Select(def => new Permission { Code = def.Code, Name = def.Name })
            .ToList();

        if (missingPermissions.Any())
        {
            _context.Permissions.AddRange(missingPermissions);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"✅ 创建了 {missingPermissions.Count} 个缺失的系统权限");
        }
        else
        {
            _logger.LogInformation($"✅ 所有 {permissionDefinitions.Count} 个系统权限已存在");
        }
    }

    /// <summary>
    /// 初始化角色
    /// </summary>
    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("检查系统角色...");

        var roleDefinitions = new List<(string RoleName, string RoleCode)>
        {
            ("超级管理员", "admin"),
            ("成员", "member"),
            ("访客", "guest")
        };

        // Get existing role codes
        var existingCodes = await _context.Roles
            .Select(r => r.RoleCode)
            .ToListAsync();

        // Find missing roles
        var missingRoles = roleDefinitions
            .Where(def => !existingCodes.Contains(def.RoleCode))
            .Select(def => new SysRole { RoleName = def.RoleName, RoleCode = def.RoleCode })
            .ToList();

        if (missingRoles.Any())
        {
            _context.Roles.AddRange(missingRoles);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"✅ 创建了 {missingRoles.Count} 个缺失的角色");
        }
        else
        {
            _logger.LogInformation($"✅ 所有 {roleDefinitions.Count} 个角色已存在");
        }
    }

    /// <summary>
    /// 初始化管理员用户
    /// </summary>
    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Account == "admin"))
        {
            _logger.LogInformation("管理员用户已存在，跳过初始化");
            return;
        }

        _logger.LogInformation("初始化管理员用户...");

        var admin = new SysUser
        {
            Account = "admin",
            UserName = "系统管理员",
            IsEnabled = true,
            CreateTime = DateTime.UtcNow,
            TenantId = "default"
        };

        // 默认密码：Admin@123
        admin.PasswordHash = _passwordHasher.HashPassword(admin, "Admin@123");

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        // 分配管理员角色
        var adminRole = await _context.Roles.FirstAsync(r => r.RoleCode == "admin");
        _context.UserRoles.Add(new SysUserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id
        });
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 创建管理员用户 (账号: admin, 密码: Admin@123)");
    }

    /// <summary>
    /// 为管理员角色分配所有权限
    /// </summary>
    private async Task AssignAdminPermissionsAsync()
    {
        var adminRole = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.RoleCode == "admin");

        if (adminRole == null)
        {
            _logger.LogWarning("⚠️ 管理员角色不存在，跳过权限分配");
            return;
        }

        _logger.LogInformation("检查管理员角色权限...");

        // Get all permissions
        var allPermissions = await _context.Permissions.ToListAsync();

        // Get currently assigned permission IDs
        var assignedPermissionIds = adminRole.RolePermissions
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        // Find missing permissions
        var missingPermissions = allPermissions
            .Where(p => !assignedPermissionIds.Contains(p.Id))
            .ToList();

        if (missingPermissions.Any())
        {
            foreach (var permission in missingPermissions)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"✅ 为管理员角色新增了 {missingPermissions.Count} 个权限");
        }
        else
        {
            _logger.LogInformation($"✅ 管理员角色已拥有所有 {allPermissions.Count} 个权限");
        }
    }

    /// <summary>
    /// 为成员角色分配基础权限
    /// </summary>
    private async Task AssignMemberPermissionsAsync()
    {
        var memberRole = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstAsync(r => r.RoleCode == "member");

        if (memberRole.RolePermissions.Any())
        {
            return;
        }

        var memberPermissionCodes = new[]
        {
            "doc.read", "doc.upload", "doc.delete",
            "chat.read", "chat.ask", "chat.history", "chat.delete",
            "agent.read", "agent.execute"
        };

        var permissions = await _context.Permissions
            .Where(p => memberPermissionCodes.Contains(p.Code))
            .ToListAsync();

        foreach (var permission in permissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = memberRole.Id,
                PermissionId = permission.Id
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation($"✅ 为成员角色分配了 {permissions.Count} 个权限");
    }

    /// <summary>
    /// 为访客角色分配只读权限
    /// </summary>
    private async Task AssignGuestPermissionsAsync()
    {
        var guestRole = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstAsync(r => r.RoleCode == "guest");

        if (guestRole.RolePermissions.Any())
        {
            return;
        }

        var guestPermissionCodes = new[]
        {
            "doc.read",
            "chat.read", "chat.ask", "chat.history"
        };

        var permissions = await _context.Permissions
            .Where(p => guestPermissionCodes.Contains(p.Code))
            .ToListAsync();

        foreach (var permission in permissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = guestRole.Id,
                PermissionId = permission.Id
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation($"✅ 为访客角色分配了 {permissions.Count} 个权限");
    }
}
