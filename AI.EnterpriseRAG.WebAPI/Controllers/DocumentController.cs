using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
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
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DocumentController : ControllerBase
{
    private readonly IDocumentUseCase _documentUseCase;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IDocumentUseCase documentUseCase, ILogger<DocumentController> logger)
    {
        _documentUseCase = documentUseCase;
        _logger = logger;
    }

    /// <summary>
    /// 上传文档（强制登录）
    /// </summary>
    /// <param name="file">文档文件（支持pdf/txt/docx）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档处理结果</returns>
    [HttpPost("upload")]
    [Authorize] // 🆕 强制登录
    [Permission("doc.upload")] // 🆕 权限验证
    [ProducesResponseType(typeof(Result<DocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadDocument(IFormFile file, CancellationToken cancellationToken = default)
    {
        // 🆕 1. 获取当前登录用户（优先使用 Account）
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) // 🆕 优先使用 UniqueName（Account）
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("用户未登录或Token无效");
            return Unauthorized(Result.Fail("用户未登录"));
        }

        // 🆕 2. 获取租户ID（从Token Claims）
        var tenantId = User.FindFirstValue("tid") // 🆕 TokenService 中使用 "tid"
                      ?? User.FindFirstValue("tenant_id")
                      ?? User.FindFirstValue("tenantId");

        _logger.LogInformation("用户{UserId}（租户：{TenantId}）开始上传文档", userId, tenantId ?? "无");

        // 企业级文件校验
        if (file == null || file.Length == 0)
            return BadRequest(Result.Fail("请上传有效文件"));

        if (file.Length > 100 * 1024 * 1024) // 100MB限制
            return BadRequest(Result.Fail("文件大小不能超过100MB"));

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
            Status = "处理中"
        };

        return Ok(Result<DocumentUploadResponseDto>.SuccessResult(response, "文档上传成功，后台处理中"));
    }

    [HttpDelete("deleteCollection")]
    [Permission("doc.delete")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteByCollectionNameAsync(Guid guid ,CancellationToken cancellationToken=default)
    {
        await _documentUseCase.DeleteByCollectionNameAsync(guid, cancellationToken);

        return Ok();
    }
}