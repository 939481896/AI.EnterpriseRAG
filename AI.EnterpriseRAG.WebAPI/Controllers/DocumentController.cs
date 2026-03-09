using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 文档管理接口（企业级API规范）
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
    /// 上传文档
    /// </summary>
    /// <param name="file">文档文件（支持pdf/txt）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档处理结果</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(Result<DocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(IFormFile file, CancellationToken cancellationToken = default)
    {
        // 企业级文件校验
        if (file == null || file.Length == 0)
            return BadRequest(Result.Fail("请上传有效文件"));

        if (file.Length > 100 * 1024 * 1024) // 100MB限制
            return BadRequest(Result.Fail("文件大小不能超过100MB"));

        var fileName = Path.GetFileName(file.FileName);
        var fileType = Path.GetExtension(fileName).TrimStart('.').ToLower();

        _logger.LogInformation("开始上传文档：{FileName}，大小：{FileSize}字节", fileName, file.Length);

        // 处理文档
        var documentId = await _documentUseCase.UploadAndProcessDocumentAsync(
            fileName,
            fileType,
            file.Length,
            file.OpenReadStream(),
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
}