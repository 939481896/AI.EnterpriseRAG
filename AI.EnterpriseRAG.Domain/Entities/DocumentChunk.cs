namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 文档分块实体（持久化模型 - 仅存储核心字段）
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 关联文档ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// 分块内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 分块序号
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Token数量
    /// </summary>
    public int TokenCount { get; set; }

    /// <summary>
    /// 分块唯一标识（用于向量库关联）
    /// </summary>
    public string ChunkId { get; set; } = string.Empty;

    /// <summary>
    /// 所属章节标题
    /// </summary>
    public string SectionTitle { get; set; } = string.Empty;

    /// <summary>
    /// 章节层级
    /// </summary>
    public int SectionLevel { get; set; }

    /// <summary>
    /// 创建时间（用于过期删除）
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 关联文档（导航属性）
    /// </summary>
    public virtual Document Document { get; set; } = null!;

    // ==========================================
    // 非持久化字段（仅用于查询和传输）
    // ==========================================

    /// <summary>
    /// 相似度得分（向量检索时临时赋值，不持久化到DB）
    /// 使用 [NotMapped] 标记或在DbContext中忽略
    /// </summary>
    public float Similarity { get; set; }

    // 移除冗余字段说明：
    // - VectorJson/Embedding: 向量数据已存储在Qdrant，无需DB重复存储
    // - FileName/FileType: 通过Document导航属性获取
    // - KeyWords: 未使用，待需求明确后再添加
}