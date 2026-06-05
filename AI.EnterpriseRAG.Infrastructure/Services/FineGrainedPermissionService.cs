using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AI.EnterpriseRAG.Infrastructure.Services;

/// <summary>
/// 精细化权限服务实现
/// </summary>
public class FineGrainedPermissionService : IFineGrainedPermissionService
{
    private readonly AppEnterpriseAiContext _context;
    private readonly ILogger<FineGrainedPermissionService> _logger;

    public FineGrainedPermissionService(
        AppEnterpriseAiContext context,
        ILogger<FineGrainedPermissionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ========== 权限授予/撤销 ==========

    public async Task GrantDocumentPermissionAsync(
        long userId,
        Guid documentId,
        DocumentPermissionType permissionType,
        string grantedBy,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("授予文档权限 | UserId: {UserId} | DocumentId: {DocumentId} | Permission: {Permission}",
            userId, documentId, permissionType);

        var existing = await _context.UserDocumentPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentId == documentId && p.IsActive, ct);

        if (existing != null)
        {
            existing.PermissionType = permissionType;
            existing.GrantedBy = grantedBy;
            existing.GrantedAt = DateTime.Now;
            existing.ExpiresAt = expiresAt;
            existing.Reason = reason;
            _logger.LogDebug("更新已存在的权限记录");
        }
        else
        {
            var permission = new UserDocumentPermission
            {
                UserId = userId,
                DocumentId = documentId,
                PermissionType = permissionType,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.Now,
                ExpiresAt = expiresAt,
                Reason = reason,
                IsActive = true
            };
            await _context.UserDocumentPermissions.AddAsync(permission, ct);
            _logger.LogDebug("创建新权限记录");
        }

        await _context.SaveChangesAsync(ct);

        await LogPermissionActionAsync(
            grantedBy,
            "Grant",
            documentId,
            userId,
            permissionType,
            reason,
            null,
            ct);

        _logger.LogInformation("✅ 权限授予成功");
    }

    public async Task RevokeDocumentPermissionAsync(
        long userId,
        Guid documentId,
        string revokedBy,
        string? reason = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("撤销文档权限 | UserId: {UserId} | DocumentId: {DocumentId}", userId, documentId);

        var permission = await _context.UserDocumentPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentId == documentId && p.IsActive, ct);

        if (permission == null)
        {
            _logger.LogWarning("⚠️ 权限记录不存在");
            throw new BusinessException(404, "权限记录不存在");
        }

        permission.IsActive = false;
        await _context.SaveChangesAsync(ct);

        await LogPermissionActionAsync(
            revokedBy,
            "Revoke",
            documentId,
            userId,
            permission.PermissionType,
            reason,
            null,
            ct);

        _logger.LogInformation("✅ 权限撤销成功");
    }

    public async Task GrantRoleDocumentPermissionAsync(
        long roleId,
        Guid documentId,
        DocumentPermissionType permissionType,
        string grantedBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("授予角色文档权限 | RoleId: {RoleId} | DocumentId: {DocumentId} | Permission: {Permission}",
            roleId, documentId, permissionType);

        var existing = await _context.RoleDocumentPermissions
            .FirstOrDefaultAsync(p => p.RoleId == roleId && p.DocumentId == documentId && p.IsActive, ct);

        if (existing != null)
        {
            existing.PermissionType = permissionType;
            existing.GrantedBy = grantedBy;
            existing.GrantedAt = DateTime.Now;
        }
        else
        {
            var permission = new RoleDocumentPermission
            {
                RoleId = roleId,
                DocumentId = documentId,
                PermissionType = permissionType,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.Now,
                IsActive = true
            };
            await _context.RoleDocumentPermissions.AddAsync(permission, ct);
        }

        await _context.SaveChangesAsync(ct);

        await LogPermissionActionAsync(
            grantedBy,
            "GrantRole",
            documentId,
            null,
            permissionType,
            $"RoleId: {roleId}",
            null,
            ct);

        _logger.LogInformation("✅ 角色权限授予成功");
    }

    public async Task GrantCategoryPermissionAsync(
        long userId,
        long categoryId,
        DocumentPermissionType permissionType,
        string grantedBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("授予分类权限 | UserId: {UserId} | CategoryId: {CategoryId} | Permission: {Permission}",
            userId, categoryId, permissionType);

        var existing = await _context.UserCategoryPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.CategoryId == categoryId, ct);

        if (existing != null)
        {
            existing.PermissionType = permissionType;
            existing.GrantedBy = grantedBy;
            existing.GrantedAt = DateTime.Now;
        }
        else
        {
            var permission = new UserCategoryPermission
            {
                UserId = userId,
                CategoryId = categoryId,
                PermissionType = permissionType,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.Now
            };
            await _context.UserCategoryPermissions.AddAsync(permission, ct);
        }

        await _context.SaveChangesAsync(ct);

        await LogPermissionActionAsync(
            grantedBy,
            "GrantCategory",
            null,
            userId,
            permissionType,
            $"CategoryId: {categoryId}",
            null,
            ct);

        _logger.LogInformation("✅ 分类权限授予成功");
    }

    // ========== 权限检查 ==========

    public async Task<bool> HasDocumentPermissionAsync(
        string userId,
        Guid documentId,
        DocumentPermissionType requiredPermission,
        CancellationToken ct = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            _logger.LogWarning("⚠️ 无效的用户ID: {UserId}", userId);
            return false;
        }

        var userPermission = await GetUserDocumentPermissionAsync(userId, documentId, ct);
        return userPermission.HasFlag(requiredPermission);
    }

    public async Task<DocumentPermissionType> GetUserDocumentPermissionAsync(
        string userId,
        Guid documentId,
        CancellationToken ct = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            _logger.LogWarning("⚠️ 无效的用户ID: {UserId}", userId);
            return DocumentPermissionType.None;
        }

        var now = DateTime.Now;
        var permissions = DocumentPermissionType.None;

        // 1. 检查直接授予的用户权限
        var userPermission = await _context.UserDocumentPermissions
            .Where(p => p.UserId == userIdLong
                     && p.DocumentId == documentId
                     && p.IsActive
                     && (p.ExpiresAt == null || p.ExpiresAt > now))
            .Select(p => p.PermissionType)
            .FirstOrDefaultAsync(ct);

        if (userPermission != DocumentPermissionType.None)
        {
            permissions |= userPermission;
        }

        // 2. 检查通过角色授予的权限
        var rolePermissions = await (
            from ur in _context.UserRoles
            join rp in _context.RoleDocumentPermissions on ur.RoleId equals rp.RoleId
            where ur.UserId == userIdLong
               && rp.DocumentId == documentId
               && rp.IsActive
               && (rp.ExpiresAt == null || rp.ExpiresAt > now)
            select rp.PermissionType
        ).ToListAsync(ct);

        foreach (var rolePermission in rolePermissions)
        {
            permissions |= rolePermission;
        }

        // 3. 检查通过分类授予的权限
        var categoryPermissions = await (
            from doc in _context.Documents
            join cp in _context.UserCategoryPermissions on doc.CategoryId equals cp.CategoryId
            where doc.Id == documentId
               && cp.UserId == userIdLong
            select cp.PermissionType
        ).ToListAsync(ct);

        foreach (var categoryPermission in categoryPermissions)
        {
            permissions |= categoryPermission;
        }

        // 4. 检查文档是否公开
        var isPublic = await _context.Documents
            .Where(d => d.Id == documentId && d.IsPublic)
            .AnyAsync(ct);

        if (isPublic)
        {
            permissions |= DocumentPermissionType.Read;
        }

        // 5. 检查是否是文档上传者（拥有者）
        var isOwner = await _context.Documents
            .Where(d => d.Id == documentId && d.UploadedBy == userId)
            .AnyAsync(ct);

        if (isOwner)
        {
            permissions = DocumentPermissionType.Admin; // 拥有者拥有所有权限
        }

        return permissions;
    }

    public async Task<List<string>> GetUserAllowedDocumentIdsAsync(
        string userId,
        DocumentPermissionType? requiredPermission = null,
        CancellationToken ct = default)
    {
        if (!long.TryParse(userId, out var userIdLong))
        {
            _logger.LogWarning("⚠️ 无效的用户ID: {UserId}", userId);
            return new List<string>();
        }

        var now = DateTime.Now;
        var documentIds = new HashSet<string>();

        // 1. 直接授予的文档权限
        var directPermissions = await _context.UserDocumentPermissions
            .Where(p => p.UserId == userIdLong
                     && p.IsActive
                     && (p.ExpiresAt == null || p.ExpiresAt > now)
                     && (!requiredPermission.HasValue || (p.PermissionType & requiredPermission.Value) == requiredPermission.Value))
            .Select(p => p.DocumentId.ToString())
            .ToListAsync(ct);

        foreach (var docId in directPermissions)
        {
            documentIds.Add(docId);
        }

        // 2. 通过角色授予的权限
        var roleDocuments = await (
            from ur in _context.UserRoles
            join rp in _context.RoleDocumentPermissions on ur.RoleId equals rp.RoleId
            where ur.UserId == userIdLong
               && rp.IsActive
               && (rp.ExpiresAt == null || rp.ExpiresAt > now)
               && (!requiredPermission.HasValue || (rp.PermissionType & requiredPermission.Value) == requiredPermission.Value)
            select rp.DocumentId.ToString()
        ).ToListAsync(ct);

        foreach (var docId in roleDocuments)
        {
            documentIds.Add(docId);
        }

        // 3. 通过分类授予的权限
        var categoryDocuments = await (
            from doc in _context.Documents
            join cp in _context.UserCategoryPermissions on doc.CategoryId equals cp.CategoryId
            where cp.UserId == userIdLong
               && (!requiredPermission.HasValue || (cp.PermissionType & requiredPermission.Value) == requiredPermission.Value)
            select doc.Id.ToString()
        ).ToListAsync(ct);

        foreach (var docId in categoryDocuments)
        {
            documentIds.Add(docId);
        }

        // 4. 公开文档（仅读权限）
        if (!requiredPermission.HasValue || requiredPermission.Value == DocumentPermissionType.Read)
        {
            var publicDocuments = await _context.Documents
                .Where(d => d.IsPublic)
                .Select(d => d.Id.ToString())
                .ToListAsync(ct);

            foreach (var docId in publicDocuments)
            {
                documentIds.Add(docId);
            }
        }

        // 5. 用户上传的文档（拥有所有权限）
        var ownedDocuments = await _context.Documents
            .Where(d => d.UploadedBy == userId)
            .Select(d => d.Id.ToString())
            .ToListAsync(ct);

        foreach (var docId in ownedDocuments)
        {
            documentIds.Add(docId);
        }

        return documentIds.ToList();
    }

    // ========== 权限查询 ==========

    public async Task<List<UserDocumentPermission>> GetDocumentPermissionsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return await _context.UserDocumentPermissions
            .Include(p => p.User)
            .Where(p => p.DocumentId == documentId && p.IsActive)
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync(ct);
    }

    public async Task<List<UserDocumentPermission>> GetUserPermissionsAsync(
        long userId,
        CancellationToken ct = default)
    {
        return await _context.UserDocumentPermissions
            .Include(p => p.Document)
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.GrantedAt)
            .ToListAsync(ct);
    }

    // ========== 审计日志 ==========

    public async Task LogPermissionActionAsync(
        string userId,
        string action,
        Guid? documentId = null,
        long? targetUserId = null,
        DocumentPermissionType? permissionType = null,
        string? reason = null,
        string? ip = null,
        CancellationToken ct = default)
    {
        var log = new PermissionAuditLog
        {
            UserId = userId,
            TargetUserId = targetUserId,
            DocumentId = documentId,
            Action = action,
            PermissionType = permissionType,
            Reason = reason,
            IP = ip,
            CreateTime = DateTime.Now
        };

        await _context.PermissionAuditLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<List<PermissionAuditLog>> GetPermissionAuditLogsAsync(
        Guid? documentId = null,
        string? userId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var query = _context.PermissionAuditLogs.AsQueryable();

        if (documentId.HasValue)
            query = query.Where(l => l.DocumentId == documentId);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(l => l.UserId == userId);

        if (startTime.HasValue)
            query = query.Where(l => l.CreateTime >= startTime);

        if (endTime.HasValue)
            query = query.Where(l => l.CreateTime <= endTime);

        return await query
            .OrderByDescending(l => l.CreateTime)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
