using System.ComponentModel.DataAnnotations;

namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 文档权限类型（按位标志，支持组合）
/// </summary>
[Flags]
public enum DocumentPermissionType
{
    None = 0,           // 无权限
    Read = 1,           // 只读（1）
    Write = 2,          // 读写（2）
    Delete = 4,         // 删除（4）
    Share = 8,          // 分享/授权（8）
    Admin = 15          // 管理员（所有权限：1+2+4+8=15）
}

/// <summary>
/// 用户-文档权限表
/// </summary>
public class UserDocumentPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 用户ID（外键）
    /// </summary>
    public long UserId { get; set; }
    
    /// <summary>
    /// 文档ID（外键）
    /// </summary>
    public Guid DocumentId { get; set; }
    
    /// <summary>
    /// 权限类型（按位标志）
    /// </summary>
    public DocumentPermissionType PermissionType { get; set; }
    
    /// <summary>
    /// 授权人账号
    /// </summary>
    [MaxLength(100)]
    public string GrantedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// 授权时间
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 过期时间（NULL=永久）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 授权原因
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    // 导航属性
    public virtual SysUser User { get; set; } = null!;
    public virtual Document Document { get; set; } = null!;
}

/// <summary>
/// 角色-文档权限表（批量授权）
/// </summary>
public class RoleDocumentPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public long RoleId { get; set; }
    public Guid DocumentId { get; set; }
    public DocumentPermissionType PermissionType { get; set; }
    
    [MaxLength(100)]
    public string GrantedBy { get; set; } = string.Empty;
    
    public DateTime GrantedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // 导航属性
    public virtual SysRole Role { get; set; } = null!;
    public virtual Document Document { get; set; } = null!;
}

/// <summary>
/// 文档分类表
/// </summary>
public class DocumentCategory
{
    public long Id { get; set; }
    
    [MaxLength(50)]
    public string CategoryCode { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;
    
    public long? ParentId { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // 导航属性
    public virtual DocumentCategory? Parent { get; set; }
    public virtual ICollection<DocumentCategory> Children { get; set; } = new List<DocumentCategory>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}

/// <summary>
/// 文档标签表
/// </summary>
public class DocumentTag
{
    public long Id { get; set; }
    
    [MaxLength(50)]
    public string TagName { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string? TagColor { get; set; }
    
    public DateTime CreateTime { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual ICollection<DocumentTagRelation> DocumentRelations { get; set; } = new List<DocumentTagRelation>();
}

/// <summary>
/// 文档-标签关联表
/// </summary>
public class DocumentTagRelation
{
    public Guid DocumentId { get; set; }
    public long TagId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual Document Document { get; set; } = null!;
    public virtual DocumentTag Tag { get; set; } = null!;
}

/// <summary>
/// 用户-分类权限表
/// </summary>
public class UserCategoryPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public long UserId { get; set; }
    public long CategoryId { get; set; }
    public DocumentPermissionType PermissionType { get; set; }
    
    [MaxLength(100)]
    public string GrantedBy { get; set; } = string.Empty;
    
    public DateTime GrantedAt { get; set; } = DateTime.Now;
    
    // 导航属性
    public virtual SysUser User { get; set; } = null!;
    public virtual DocumentCategory Category { get; set; } = null!;
}

/// <summary>
/// 权限审计日志
/// </summary>
public class PermissionAuditLog
{
    public long Id { get; set; }
    
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    public long? TargetUserId { get; set; }
    public Guid? DocumentId { get; set; }
    
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Grant/Revoke/Access
    
    public DocumentPermissionType? PermissionType { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(50)]
    public string? IP { get; set; }
    
    public DateTime CreateTime { get; set; } = DateTime.Now;
}
