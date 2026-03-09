
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;

public class ChatConversationRepository : IChatConversationRepository
{
    private readonly AppContext _dbContext;
    public ChatConversationRepository(AppContext dbContext)
    {  _dbContext = dbContext; }
    public async Task AddAsync(ChatConversation conversation)
    {
        await _dbContext.ChatConversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<ChatConversation>> GetByUserIdAsync(string userId, int pageSize = 20)
    {
        return await _dbContext.ChatConversations.AsNoTracking().Where(c=>c.UserId == userId).Take(pageSize).ToListAsync();
    }
}
