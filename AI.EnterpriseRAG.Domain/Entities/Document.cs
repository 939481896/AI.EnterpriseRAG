using AI.EnterpriseRAG.Domain.Enums;

namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 文档实体（企业级数据模型 + 权限控制）
/// </summary>
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 文档名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型（pdf/txt/docx）
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 存储路径
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 文档状态
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 处理完成时间
    /// </summary>
    public DateTime? CompleteTime { get; set; }

    // ========== 🆕 权限控制字段 ==========

    /// <summary>
    /// 上传者（用户Account）
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID（多租户隔离）
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// 是否公开（公开文档所有人可见）
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 关联的分块
    /// </summary>
    public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}