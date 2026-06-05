using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.WebAPI.Attribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// Document permission management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class DocumentPermissionController : ControllerBase
{
    private readonly IFineGrainedPermissionService _permissionService;
    private readonly ILogger<DocumentPermissionController> _logger;

    public DocumentPermissionController(
        IFineGrainedPermissionService permissionService,
        ILogger<DocumentPermissionController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Grant document permission to user
    /// </summary>
    [HttpPost("grant")]
    [Permission("doc.share")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GrantPermission(
        [FromBody] GrantPermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                         ?? User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUser))
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail("User not authenticated"));
        }

        try
        {
            await _permissionService.GrantDocumentPermissionAsync(
                userId: request.UserId,
                documentId: request.DocumentId,
                permissionType: request.PermissionType,
                grantedBy: currentUser,
                expiresAt: request.ExpiresAt,
                reason: request.Reason,
                ct: cancellationToken);

            _logger.LogInformation(
                "User {GrantedBy} granted {PermissionType} permission to user {UserId} for document {DocumentId}",
                currentUser, request.PermissionType, request.UserId, request.DocumentId);

            return Ok(Result.Success("Permission granted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant permission");
            return BadRequest(Result.Fail($"Failed to grant permission: {ex.Message}"));
        }
    }

    /// <summary>
    /// Revoke document permission from user
    /// </summary>
    [HttpPost("revoke")]
    [Permission("doc.share")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokePermission(
        [FromBody] RevokePermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                         ?? User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUser))
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail("User not authenticated"));
        }

        try
        {
            await _permissionService.RevokeDocumentPermissionAsync(
                userId: request.UserId,
                documentId: request.DocumentId,
                revokedBy: currentUser,
                reason: request.Reason,
                ct: cancellationToken);

            _logger.LogInformation(
                "User {RevokedBy} revoked permission from user {UserId} for document {DocumentId}",
                currentUser, request.UserId, request.DocumentId);

            return Ok(Result.Success("Permission revoked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke permission");
            return BadRequest(Result.Fail($"Failed to revoke permission: {ex.Message}"));
        }
    }

    /// <summary>
    /// Grant document permission to role
    /// </summary>
    [HttpPost("grant-role")]
    [Permission("doc.share")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GrantRolePermission(
        [FromBody] GrantRolePermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                         ?? User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUser))
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail("User not authenticated"));
        }

        try
        {
            await _permissionService.GrantRoleDocumentPermissionAsync(
                roleId: request.RoleId,
                documentId: request.DocumentId,
                permissionType: request.PermissionType,
                grantedBy: currentUser,
                ct: cancellationToken);

            _logger.LogInformation(
                "User {GrantedBy} granted {PermissionType} permission to role {RoleId} for document {DocumentId}",
                currentUser, request.PermissionType, request.RoleId, request.DocumentId);

            return Ok(Result.Success("Role permission granted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant role permission");
            return BadRequest(Result.Fail($"Failed to grant role permission: {ex.Message}"));
        }
    }

    /// <summary>
    /// Grant category permission to user
    /// </summary>
    [HttpPost("grant-category")]
    [Permission("doc.share")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GrantCategoryPermission(
        [FromBody] GrantCategoryPermissionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                         ?? User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUser))
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail("User not authenticated"));
        }

        try
        {
            await _permissionService.GrantCategoryPermissionAsync(
                userId: request.UserId,
                categoryId: request.CategoryId,
                permissionType: request.PermissionType,
                grantedBy: currentUser,
                ct: cancellationToken);

            _logger.LogInformation(
                "User {GrantedBy} granted {PermissionType} permission to user {UserId} for category {CategoryId}",
                currentUser, request.PermissionType, request.UserId, request.CategoryId);

            return Ok(Result.Success("Category permission granted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grant category permission");
            return BadRequest(Result.Fail($"Failed to grant category permission: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all permissions for a document
    /// </summary>
    [HttpGet("document/{documentId}")]
    [Permission("doc.read")]
    [ProducesResponseType(typeof(Result<List<UserDocumentPermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDocumentPermissions(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _permissionService.GetDocumentPermissionsAsync(documentId, cancellationToken);

            var dtos = permissions.Select(p => new UserDocumentPermissionDto
            {
                Id = p.Id,
                UserId = p.UserId,
                DocumentId = p.DocumentId,
                PermissionType = p.PermissionType.ToString(),
                GrantedBy = p.GrantedBy,
                GrantedAt = p.GrantedAt,
                ExpiresAt = p.ExpiresAt,
                IsActive = p.IsActive,
                Reason = p.Reason
            }).ToList();

            _logger.LogInformation("Retrieved {Count} permissions for document {DocumentId}", dtos.Count, documentId);
            return Ok(Result<List<UserDocumentPermissionDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve document permissions");
            return BadRequest(Result.Fail($"Failed to retrieve document permissions: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all permissions for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [Permission("doc.read")]
    [ProducesResponseType(typeof(Result<List<UserDocumentPermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserPermissions(
        long userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var permissions = await _permissionService.GetUserPermissionsAsync(userId, cancellationToken);

            var dtos = permissions.Select(p => new UserDocumentPermissionDto
            {
                Id = p.Id,
                UserId = p.UserId,
                DocumentId = p.DocumentId,
                PermissionType = p.PermissionType.ToString(),
                GrantedBy = p.GrantedBy,
                GrantedAt = p.GrantedAt,
                ExpiresAt = p.ExpiresAt,
                IsActive = p.IsActive,
                Reason = p.Reason
            }).ToList();

            _logger.LogInformation("Retrieved {Count} permissions for user {UserId}", dtos.Count, userId);
            return Ok(Result<List<UserDocumentPermissionDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user permissions");
            return BadRequest(Result.Fail($"Failed to retrieve user permissions: {ex.Message}"));
        }
    }

    /// <summary>
    /// Check if current user has permission for a document
    /// </summary>
    [HttpGet("check")]
    [Permission("doc.read")]
    [ProducesResponseType(typeof(Result<CheckPermissionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckPermission(
        [FromQuery] Guid documentId,
        [FromQuery] DocumentPermissionType requiredPermission,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                         ?? User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUser))
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail("User not authenticated"));
        }

        try
        {
            var hasPermission = await _permissionService.HasDocumentPermissionAsync(
                currentUser, documentId, requiredPermission, cancellationToken);

            var currentPermission = await _permissionService.GetUserDocumentPermissionAsync(
                currentUser, documentId, cancellationToken);

            var response = new CheckPermissionResponseDto
            {
                HasPermission = hasPermission,
                CurrentPermission = currentPermission.ToString(),
                DocumentId = documentId
            };

            _logger.LogInformation(
                "User {UserId} permission check for document {DocumentId}: {HasPermission} (current: {CurrentPermission})",
                currentUser, documentId, hasPermission, currentPermission);

            return Ok(Result<CheckPermissionResponseDto>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Permission check failed");
            return BadRequest(Result.Fail($"Permission check failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get all document IDs that current user has access to
    /// </summary>
    [HttpGet("allowed-documents")]
    [Permission("doc.read")]
    [ProducesResponseType(typeof(Result<UserAllowedDocumentsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserAllowedDocuments(
        [FromQuery] DocumentPermissionType? requiredPermission = null,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                         ?? User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(currentUser))
        {
            _logger.LogWarning("User not authenticated");
            return Unauthorized(Result.Fail("User not authenticated"));
        }

        try
        {
            var documentIds = await _permissionService.GetUserAllowedDocumentIdsAsync(
                currentUser, requiredPermission, cancellationToken);

            var response = new UserAllowedDocumentsDto
            {
                DocumentIds = documentIds,
                TotalCount = documentIds.Count
            };

            _logger.LogInformation(
                "User {UserId} has access to {Count} documents",
                currentUser, documentIds.Count);

            return Ok(Result<UserAllowedDocumentsDto>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve allowed documents");
            return BadRequest(Result.Fail($"Failed to retrieve allowed documents: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get permission audit logs
    /// </summary>
    [HttpGet("audit-logs")]
    [Permission("admin")]
    [ProducesResponseType(typeof(Result<List<PermissionAuditLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] Guid? documentId = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _permissionService.GetPermissionAuditLogsAsync(
                documentId, userId, startTime, endTime, pageSize, cancellationToken);

            var dtos = logs.Select(log => new PermissionAuditLogDto
            {
                Id = log.Id,
                UserId = log.UserId,
                Action = log.Action,
                DocumentId = log.DocumentId,
                TargetUserId = log.TargetUserId,
                PermissionType = log.PermissionType?.ToString(),
                Reason = log.Reason,
                IP = log.IP,
                CreateTime = log.CreateTime
            }).ToList();

            _logger.LogInformation("Retrieved {Count} audit logs", dtos.Count);
            return Ok(Result<List<PermissionAuditLogDto>>.SuccessResult(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return BadRequest(Result.Fail($"Failed to retrieve audit logs: {ex.Message}"));
        }
    }
}
