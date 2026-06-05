using AI.EnterpriseRAG.Core.Configuration;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services;

/// <summary>
/// Conversation Memory Service Implementation
/// Brain-like short-term memory for context-aware RAG
/// </summary>
public class ConversationMemoryService : IConversationMemoryService
{
    private readonly AppEnterpriseAiContext _context;
    private readonly ILogger<ConversationMemoryService> _logger;
    private readonly MemoryOptions _options;

    public ConversationMemoryService(
        AppEnterpriseAiContext context,
        ILogger<ConversationMemoryService> logger,
        IOptions<MemoryOptions> options)
    {
        _context = context;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<ConversationSession> CreateSessionAsync(
        string userId,
        string? initialTitle = null,
        CancellationToken ct = default)
    {
        var session = new ConversationSession
        {
            UserId = userId,
            Title = initialTitle ?? "新对话",
            CreatedAt = DateTime.UtcNow,
            LastInteractionAt = DateTime.UtcNow,
            IsActive = true,
            MessageCount = 0
        };

        _context.ConversationSessions.Add(session);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("✅ Created session {SessionId} for user {UserId}", session.Id, userId);
        return session;
    }

    public async Task<ConversationSession?> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _context.ConversationSessions
            .Include(s => s.Messages.OrderBy(m => m.SequenceNumber))
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public async Task<List<ConversationSession>> GetUserSessionsAsync(
        string userId,
        int limit = 10,
        CancellationToken ct = default)
    {
        var effectiveLimit = Math.Min(limit, _options.MaxHistoryMessages);

        return await _context.ConversationSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastInteractionAt)
            .Take(effectiveLimit)
            .ToListAsync(ct);
    }
    
    public async Task<ConversationMessage> AddMessageAsync(
        string userId,
        Guid? sessionId,
        string role,
        string content,
        string? referenceChunks = null,
        decimal costSeconds = 0,
        bool isSuccess = true,
        CancellationToken ct = default)
    {
        // Auto-create session if not provided
        ConversationSession session;
        if (sessionId == null || sessionId == Guid.Empty)
        {
            session = await CreateSessionAsync(userId, ExtractTitle(content), ct);
        }
        else
        {
            session = await GetSessionAsync(sessionId.Value, ct)
                ?? throw new InvalidOperationException($"Session {sessionId} not found");
        }
        
        // Create message
        var message = new ConversationMessage
        {
            SessionId = session.Id,
            UserId = userId,
            Role = role,
            Content = content,
            ReferenceChunks = referenceChunks,
            SequenceNumber = session.MessageCount + 1,
            CreatedAt = DateTime.UtcNow,
            CostSeconds = costSeconds,
            IsSuccess = isSuccess
        };
        
        // Update session
        session.MessageCount++;
        session.LastInteractionAt = DateTime.UtcNow;
        if (session.MessageCount == 1 && _options.AutoGenerateTitles && string.IsNullOrEmpty(session.Title))
        {
            session.Title = ExtractTitle(content);
        }
        
        _context.ConversationMessages.Add(message);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogDebug("📝 Added {Role} message to session {SessionId} (seq: {Seq})",
            role, session.Id, message.SequenceNumber);
        
        return message;
    }
    
    public async Task<List<ConversationMessage>> GetRecentHistoryAsync(
        Guid sessionId,
        int limit = 10,
        int? maxTokens = null,
        CancellationToken ct = default)
    {
        var messages = await _context.ConversationMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.SequenceNumber)
            .Take(limit)
            .ToListAsync(ct);
        
        // Reverse to chronological order
        messages.Reverse();
        
        // Token-based truncation if needed
        if (maxTokens.HasValue)
        {
            messages = TruncateByTokens(messages, maxTokens.Value);
        }
        
        _logger.LogDebug("📚 Retrieved {Count} history messages for session {SessionId}",
            messages.Count, sessionId);
        
        return messages;
    }
    
    public async Task<string> BuildContextAwarePromptAsync(
        Guid sessionId,
        string currentQuestion,
        List<DocumentChunk> retrievedChunks,
        int maxHistoryTokens = 1000,
        CancellationToken ct = default)
    {
        var history = await GetRecentHistoryAsync(sessionId, limit: 10, maxTokens: maxHistoryTokens, ct);
        
        var prompt = new StringBuilder();
        prompt.AppendLine("你是一个专业的企业级AI助手，基于提供的文档回答问题。");
        prompt.AppendLine();
        
        // Conversation history (if any)
        if (history.Any())
        {
            prompt.AppendLine("### 对话历史");
            foreach (var msg in history)
            {
                var prefix = msg.Role == "user" ? "用户" : "助手";
                prompt.AppendLine($"{prefix}: {msg.Content}");
            }
            prompt.AppendLine();
        }
        
        // Retrieved context
        if (retrievedChunks.Any())
        {
            prompt.AppendLine("### 参考文档");
            for (int i = 0; i < retrievedChunks.Count; i++)
            {
                prompt.AppendLine($"[{i + 1}] {retrievedChunks[i].Content}");
                prompt.AppendLine();
            }
        }
        
        // Current question
        prompt.AppendLine("### 当前问题");
        prompt.AppendLine(currentQuestion);
        prompt.AppendLine();
        prompt.AppendLine("请基于上述对话历史和参考文档回答问题。如果参考文档中没有相关信息，请诚实说明。");
        
        return prompt.ToString();
    }
    
    public async Task<int> ArchiveInactiveSessionsAsync(int daysInactive = 30, CancellationToken ct = default)
    {
        var effectiveDays = Math.Max(daysInactive, _options.SessionArchiveDays);
        var cutoffDate = DateTime.UtcNow.AddDays(-effectiveDays);

        var inactiveSessions = await _context.ConversationSessions
            .Where(s => s.IsActive && s.LastInteractionAt < cutoffDate)
            .ToListAsync(ct);

        foreach (var session in inactiveSessions)
        {
            session.IsActive = false;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("🗄️ Archived {Count} inactive sessions (>{Days} days)",
            inactiveSessions.Count, effectiveDays);

        return inactiveSessions.Count;
    }

    #region Helpers

    private string ExtractTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "新对话";

        var title = content.Trim();
        if (title.Length > _options.MaxTitleLength)
        {
            title = title.Substring(0, _options.MaxTitleLength) + "...";
        }

        return title;
    }

    private List<ConversationMessage> TruncateByTokens(List<ConversationMessage> messages, int maxTokens)
    {
        var result = new List<ConversationMessage>();
        var currentTokens = 0;

        // Keep most recent messages within token budget
        for (int i = messages.Count - 1; i >= 0; i--)
        {
            var estimatedTokens = messages[i].Content.Length / _options.EstimatedTokensPerChar;
            if (currentTokens + estimatedTokens > maxTokens)
                break;

            result.Insert(0, messages[i]);
            currentTokens += estimatedTokens;
        }

        return result;
    }

    #endregion
}
