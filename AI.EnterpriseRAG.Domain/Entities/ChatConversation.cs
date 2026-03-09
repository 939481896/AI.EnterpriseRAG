namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// 对话记录实体（企业级审计日志）
/// </summary>
public class ChatConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 问题内容
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// 回答内容
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 参考上下文（JSON存储）
    /// </summary>
    public string ReferenceContexts { get; set; } = string.Empty;

    /// <summary>
    /// 耗时（秒）
    /// </summary>
    public decimal CostSeconds { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 对话时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    public float? SearchSimilarity { get; set; } // 检索平均相似度
    public int ContextTokenCount { get; set; } // 上下文Token数
    public int PromptTokenCount { get; set; } // 完整Prompt Token数
}