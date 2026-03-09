namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// 文档上传请求DTO
/// </summary>
public class DocumentUploadRequestDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}