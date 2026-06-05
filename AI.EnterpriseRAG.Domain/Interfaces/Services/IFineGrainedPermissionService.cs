using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// 精细化权限服务接口
/// </summary>
public interface IFineGrainedPermissionService
{
    // ========== 权限授予/撤销 ==========
    
    /// <summary>
    /// 授予用户文档权限
    /// </summary>
    Task GrantDocumentPermissionAsync(
        long userId,
        Guid documentId,
        DocumentPermissionType permissionType,
        string grantedBy,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// 撤销用户文档权限
    /// </summary>
    Task RevokeDocumentPermissionAsync(
        long userId,
        Guid documentId,
        string revokedBy,
        string? reason = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// 批量授予角色文档权限
    /// </summary>
    Task GrantRoleDocumentPermissionAsync(
        long roleId,
        Guid documentId,
        DocumentPermissionType permissionType,
        string grantedBy,
        CancellationToken ct = default);
    
    /// <summary>
    /// 授予用户分类权限（影响该分类下所有文档）
    /// </summary>
    Task GrantCategoryPermissionAsync(
        long userId,
        long categoryId,
        DocumentPermissionType permissionType,
        string grantedBy,
        CancellationToken ct = default);
    
    // ========== 权限检查 ==========
    
    /// <summary>
    /// 检查用户是否有文档权限
    /// </summary>
    Task<bool> HasDocumentPermissionAsync(
        string userId,
        Guid documentId,
        DocumentPermissionType requiredPermission,
        CancellationToken ct = default);
    
    /// <summary>
    /// 获取用户对文档的权限类型
    /// </summary>
    Task<DocumentPermissionType> GetUserDocumentPermissionAsync(
        string userId,
        Guid documentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// 获取用户可访问的文档ID列表（支持权限类型过滤）
    /// </summary>
    Task<List<string>> GetUserAllowedDocumentIdsAsync(
        string userId,
        DocumentPermissionType? requiredPermission = null,
        CancellationToken ct = default);
    
    // ========== 权限查询 ==========
    
    /// <summary>
    /// 获取文档的所有授权用户
    /// </summary>
    Task<List<UserDocumentPermission>> GetDocumentPermissionsAsync(
        Guid documentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// 获取用户的所有文档权限
    /// </summary>
    Task<List<UserDocumentPermission>> GetUserPermissionsAsync(
        long userId,
        CancellationToken ct = default);
    
    // ========== 审计日志 ==========
    
    /// <summary>
    /// 记录权限操作日志
    /// </summary>
    Task LogPermissionActionAsync(
        string userId,
        string action,
        Guid? documentId = null,
        long? targetUserId = null,
        DocumentPermissionType? permissionType = null,
        string? reason = null,
        string? ip = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// 获取权限审计日志
    /// </summary>
    Task<List<PermissionAuditLog>> GetPermissionAuditLogsAsync(
        Guid? documentId = null,
        string? userId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int pageSize = 100,
        CancellationToken ct = default);
}
