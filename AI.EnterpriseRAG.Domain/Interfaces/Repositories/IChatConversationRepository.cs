using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Repositories;

/// <summary>
/// 对话仓储接口
/// </summary>
public interface IChatConversationRepository
{
    Task AddAsync(ChatConversation conversation);
    Task<List<ChatConversation>> GetByUserIdAsync(string userId, int pageSize = 20);
}