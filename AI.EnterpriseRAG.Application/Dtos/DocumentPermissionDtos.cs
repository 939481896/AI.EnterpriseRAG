using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// Grant permission request DTO
/// </summary>
public class GrantPermissionRequestDto
{
    /// <summary>
    /// User ID to grant permission to
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Permission type
    /// </summary>
    public DocumentPermissionType PermissionType { get; set; }

    /// <summary>
    /// Permission expiration time (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Reason for granting permission
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Revoke permission request DTO
/// </summary>
public class RevokePermissionRequestDto
{
    /// <summary>
    /// User ID to revoke permission from
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Reason for revoking permission
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Grant role permission request DTO
/// </summary>
public class GrantRolePermissionRequestDto
{
    /// <summary>
    /// Role ID to grant permission to
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Permission type
    /// </summary>
    public DocumentPermissionType PermissionType { get; set; }
}

/// <summary>
/// Grant category permission request DTO
/// </summary>
public class GrantCategoryPermissionRequestDto
{
    /// <summary>
    /// User ID to grant permission to
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public long CategoryId { get; set; }

    /// <summary>
    /// Permission type
    /// </summary>
    public DocumentPermissionType PermissionType { get; set; }

    /// <summary>
    /// Reason for granting permission
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// User document permission response DTO
/// </summary>
public class UserDocumentPermissionDto
{
    /// <summary>
    /// Permission ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Permission type
    /// </summary>
    public string PermissionType { get; set; } = string.Empty;

    /// <summary>
    /// Granted by
    /// </summary>
    public string GrantedBy { get; set; } = string.Empty;

    /// <summary>
    /// Granted at
    /// </summary>
    public DateTime GrantedAt { get; set; }

    /// <summary>
    /// Expires at (optional)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Reason for permission
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Permission audit log response DTO
/// </summary>
public class PermissionAuditLogDto
{
    /// <summary>
    /// Log ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Action performed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Document ID (optional)
    /// </summary>
    public Guid? DocumentId { get; set; }

    /// <summary>
    /// Target user ID (optional)
    /// </summary>
    public long? TargetUserId { get; set; }

    /// <summary>
    /// Permission type (optional)
    /// </summary>
    public string? PermissionType { get; set; }

    /// <summary>
    /// Reason for action
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// IP address
    /// </summary>
    public string? IP { get; set; }

    /// <summary>
    /// Create time
    /// </summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// User allowed documents response DTO
/// </summary>
public class UserAllowedDocumentsDto
{
    /// <summary>
    /// List of document IDs the user has access to
    /// </summary>
    public List<string> DocumentIds { get; set; } = new();

    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Check permission response DTO
/// </summary>
public class CheckPermissionResponseDto
{
    /// <summary>
    /// Whether the user has permission
    /// </summary>
    public bool HasPermission { get; set; }

    /// <summary>
    /// User's current permission level
    /// </summary>
    public string? CurrentPermission { get; set; }

    /// <summary>
    /// Document ID
    /// </summary>
    public Guid DocumentId { get; set; }
}
