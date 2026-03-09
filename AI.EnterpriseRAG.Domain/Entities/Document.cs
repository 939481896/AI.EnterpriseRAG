using AI.EnterpriseRAG.Domain.Enums;

namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 文档实体（企业级数据模型）
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

    /// <summary>
    /// 关联的分块
    /// </summary>
    public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}