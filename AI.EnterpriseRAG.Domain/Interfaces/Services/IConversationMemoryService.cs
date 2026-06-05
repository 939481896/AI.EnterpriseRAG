using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// Conversation Memory Service: Short-term memory for context-aware RAG
/// Enables multi-turn conversations with history tracking
/// </summary>
public interface IConversationMemoryService
{
    /// <summary>
    /// Create a new conversation session
    /// </summary>
    Task<ConversationSession> CreateSessionAsync(string userId, string? initialTitle = null, CancellationToken ct = default);
    
    /// <summary>
    /// Get session by ID
    /// </summary>
    Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Get user's active sessions (sorted by last interaction)
    /// </summary>
    Task<List<ConversationSession>> GetUserSessionsAsync(string userId, int limit = 10, CancellationToken ct = default);
    
    /// <summary>
    /// Add a message to session (auto-creates session if needed)
    /// </summary>
    Task<ConversationMessage> AddMessageAsync(
        string userId,
        Guid? sessionId,
        string role,
        string content,
        string? referenceChunks = null,
        decimal costSeconds = 0,
        bool isSuccess = true,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get recent conversation history (for context window)
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="limit">Max messages to retrieve (default: 10 = 5 turns)</param>
    /// <param name="maxTokens">Optional: limit by token count</param>
    Task<List<ConversationMessage>> GetRecentHistoryAsync(
        Guid sessionId,
        int limit = 10,
        int? maxTokens = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Build context-aware prompt with conversation history
    /// </summary>
    Task<string> BuildContextAwarePromptAsync(
        Guid sessionId,
        string currentQuestion,
        List<DocumentChunk> retrievedChunks,
        int maxHistoryTokens = 1000,
        CancellationToken ct = default);
    
    /// <summary>
    /// Clear old sessions (for cleanup/archival)
    /// </summary>
    Task<int> ArchiveInactiveSessionsAsync(int daysInactive = 30, CancellationToken ct = default);
}
