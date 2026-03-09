namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// 文档上传响应DTO
/// </summary>
public class DocumentUploadResponseDto
{
    /// <summary>
    /// 文档ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// 文档名称
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// 处理状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
}