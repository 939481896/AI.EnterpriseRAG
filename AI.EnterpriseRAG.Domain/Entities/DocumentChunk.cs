namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 文档分块实体
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
    /// 向量数据（JSON存储）
    /// </summary>
    public string VectorJson { get; set; } = string.Empty;

    /// <summary>
    /// 关联文档
    /// </summary>
    public virtual Document Document { get; set; } = null!;

    public float Similarity { get; set; } // 检索相似度（VectorStore返回时赋值）

}