using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Domain.Entities;

public class SysUser
{
    public long Id { get; set; }
    public string Account { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public string TenantId { get; set; } = "default";

    public virtual ICollection<SysUserRole> UserRoles { get; set; } = [];
}

public class SysRole
{
    public long Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty; // admin, member, guest
    public ICollection<SysUserRole> UserRoles { get; set; } = new List<SysUserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

}

public class SysUserRole
{
    public long UserId { get; set; }
    public SysUser User { get; set; } = default!;

    public long RoleId { get; set; }
    public SysRole Role { get; set; } = default!;
}

public class Permission
{
    public long Id { get; set; }

    public string Code { get; set; } = default!; // user.read
    public string Name { get; set; } = default!;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
public class RolePermission
{
    public long RoleId { get; set; }
    public SysRole Role { get; set; } = default!;

    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

public class RefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }

    // Add this line to allow access to the User object
    public virtual SysUser User { get; set; } = default!;

    public string Token { get; set; } = default!;

    public DateTime ExpireAt { get; set; }

    public bool IsRevoked { get; set; }

    public string Device { get; set; } = "web";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}