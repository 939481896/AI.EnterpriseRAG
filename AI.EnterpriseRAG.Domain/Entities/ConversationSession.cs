namespace AI.EnterpriseRAG.Domain.Entities;

/// <summary>
/// Conversation Session: Groups related messages together
/// Enables context-aware multi-turn conversations
/// </summary>
public class ConversationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User who owns this session
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Session title (auto-generated from first question)
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Session creation time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last interaction time (for cleanup/archival)
    /// </summary>
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Is session still active?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Total messages in this session
    /// </summary>
    public int MessageCount { get; set; }
    
    /// <summary>
    /// Session metadata (JSON: user preferences, context hints)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Navigation: All messages in this session
    /// </summary>
    public virtual ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}

/// <summary>
/// Conversation Message: Individual Q&A within a session
/// Replaces standalone ChatConversation for memory-aware RAG
/// </summary>
public class ConversationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Parent session ID
    /// </summary>
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// User ID (redundant but useful for queries)
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Message role: "user" or "assistant"
    /// </summary>
    public string Role { get; set; } = "user"; // user | assistant
    
    /// <summary>
    /// Message content (question or answer)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Referenced document chunks (JSON array of DocumentChunk IDs)
    /// </summary>
    public string? ReferenceChunks { get; set; }
    
    /// <summary>
    /// Message sequence number in session
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Message timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Processing cost (seconds)
    /// </summary>
    public decimal CostSeconds { get; set; }
    
    /// <summary>
    /// Was this message successfully processed?
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>
    /// Average search similarity (for analytics)
    /// </summary>
    public float? SearchSimilarity { get; set; }
    
    /// <summary>
    /// V1.0 Metrics
    /// </summary>
    public int ContextTokenCount { get; set; }
    public int PromptTokenCount { get; set; }
    public bool UsedHyDE { get; set; }
    public bool UsedMultiQuery { get; set; }
    public bool UsedSelfReflection { get; set; }
    public int? SelfReflectionConfidence { get; set; }
    
    /// <summary>
    /// Navigation: Parent session
    /// </summary>
    public virtual ConversationSession Session { get; set; } = null!;
}
