using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.WebAPI.Attribute;
using Microsoft.AspNetCore.Authorization; // 🆕 添加授权
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt; // 🆕 添加JWT
using System.Security.Claims; // 🆕 添加Claims

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 文档管理接口（企业级API规范 + 权限控制）
/// </summary>
[Route("api/[controller]")]
public class DocumentController : BaseApiController
{
    private readonly IDocumentUseCase _documentUseCase;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentUseCase documentUseCase, ILogger<DocumentController> logger)
    {
        _documentUseCase = documentUseCase;
        _logger = logger;
    }

    /// <summary>
    /// 上传文档
    /// </summary>
    /// <param name="file">文档文件（支持pdf/txt/docx）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档处理结果</returns>
    [HttpPost("upload")]
    [Authorize] // 强制登录
    [Permission("doc.upload")] // 权限验证
    [ProducesResponseType(typeof(Result<DocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadDocument(IFormFile file, CancellationToken cancellationToken = default)
    {
        // 1. 获取当前登录用户
        var user = GetCurrentUser();
        if (user == null || !user.IsAuthenticated)
        {
            _logger.LogWarning("用户未登录或Token无效");
            return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));
        }

        var userId = user.UserId;
        var tenantId = user.TenantId;

        _logger.LogInformation("用户{UserId}（租户：{TenantId}）开始上传文档", userId, tenantId ?? "无");

        // 企业级文件校验
        if (file == null || file.Length == 0)
            return BadRequest(Result.Fail(MessageResources.Validation.Required("文件")));

        if (file.Length > 100 * 1024 * 1024) // 100MB限制
            return BadRequest(Result.Fail(MessageResources.Validation.FileTooLarge(100)));

        var fileName = Path.GetFileName(file.FileName);
        var fileType = Path.GetExtension(fileName).TrimStart('.').ToLower();

        _logger.LogInformation("开始上传文档：{FileName}，大小：{FileSize}字节", fileName, file.Length);

        // 🆕 3. 处理文档（传入上传者和租户信息）
        var documentId = await _documentUseCase.UploadAndProcessDocumentAsync(
            fileName,
            fileType,
            file.Length,
            file.OpenReadStream(),
            userId,    // 🆕 上传者（Account）
            tenantId,  // 🆕 租户ID
            cancellationToken);

        // 返回结果
        var response = new DocumentUploadResponseDto
        {
            DocumentId = documentId,
            DocumentName = fileName,
            Status = MessageResources.Document.Processing
        };

        return Ok(Result<DocumentUploadResponseDto>.SuccessResult(response, MessageResources.Document.UploadSuccess));
    }

    /// <summary>
    /// 获取文档列表
    /// </summary>
    [HttpGet("list")]
    [Authorize]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var user = GetCurrentUser();
        if (user == null || !user.IsAuthenticated)
            return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));

        var userId = user.UserId;

        var documents = await _documentUseCase.GetUserDocumentsAsync(
            userId,
            page,
            pageSize,
            cancellationToken);

        return Ok(Result<object>.SuccessResult(documents));
    }

    /// <summary>
    /// 删除单个文档
    /// </summary>
    [HttpDelete("{documentId}")]
    [Authorize]
    [Permission("doc.delete")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = GetCurrentUser();
            if (user == null || !user.IsAuthenticated)
                return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));

            var userId = user.UserId;

            _logger.LogInformation("用户{UserId}开始删除文档：{DocumentId}", userId, documentId);

            await _documentUseCase.DeleteByDocumentIdAsync(documentId, cancellationToken);

            return Ok(Result.SuccessResult(MessageResources.Document.DeleteSuccess));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文档失败：{DocumentId}", documentId);
            return BadRequest(Result.Fail($"{MessageResources.Get("document.delete.failed")}：{ex.Message}"));
        }
    }

    /// <summary>
    /// 重新处理失败的文档
    /// </summary>
    [HttpPost("{documentId}/reprocess")]
    [Authorize]
    [Permission("doc.upload")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReprocessDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = GetCurrentUser();
            if (user == null || !user.IsAuthenticated)
                return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));

            var userId = user.UserId;

            _logger.LogInformation("用户{UserId}开始重新处理文档：{DocumentId}", userId, documentId);

            await _documentUseCase.ReprocessDocumentAsync(documentId, null, cancellationToken);

            return Ok(Result.SuccessResult(MessageResources.Get("document.reprocess.success")));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新处理文档失败：{DocumentId}", documentId);
            return BadRequest(Result.Fail($"{MessageResources.Get("document.reprocess.failed")}：{ex.Message}"));
        }
    }

    [HttpDelete("deleteCollection")]
    [Permission("doc.delete")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteByCollectionNameAsync(Guid guid ,CancellationToken cancellationToken=default)
    {
        await _documentUseCase.DeleteByCollectionNameAsync(guid, cancellationToken);

        return Ok(Result.SuccessResult(MessageResources.Get("document.collection.deleted")));
    }
}