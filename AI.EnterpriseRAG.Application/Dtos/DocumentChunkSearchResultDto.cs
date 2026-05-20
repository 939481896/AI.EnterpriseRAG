namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// 向量检索结果DTO（分离查询模型和持久化模型）
/// </summary>
public class DocumentChunkSearchResultDto
{
    /// <summary>
    /// 分块ID
    /// </summary>
    public Guid ChunkId { get; set; }

    /// <summary>
    /// 文档ID
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// 分块内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 分块索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 相似度得分
    /// </summary>
    public float Similarity { get; set; }

    /// <summary>
    /// 文档名称（从导航属性获取）
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public string FileType { get; set; } = string.Empty;

    /// <summary>
    /// 章节标题
    /// </summary>
    public string SectionTitle { get; set; } = string.Empty;

    /// <summary>
    /// 章节层级
    /// </summary>
    public int SectionLevel { get; set; }

    /// <summary>
    /// Token数量
    /// </summary>
    public int TokenCount { get; set; }
}
