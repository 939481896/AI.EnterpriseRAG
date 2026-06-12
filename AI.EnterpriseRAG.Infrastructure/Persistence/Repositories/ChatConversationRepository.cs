
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Infrastructure.Persistence.Repositories;

public class ChatConversationRepository : IChatConversationRepository
{
    private readonly AppEnterpriseAiContext _dbContext;
    public ChatConversationRepository(AppEnterpriseAiContext dbContext)
    {  _dbContext = dbContext; }
    public async Task AddAsync(ChatConversation conversation)
    {
        await _dbContext.ChatConversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<ChatConversation>> GetByUserIdAsync(string userId, int pageSize = 20)
    {
        return await _dbContext.ChatConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreateTime)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<ChatConversation?> GetByIdAsync(Guid id)
    {
        return await _dbContext.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var conversation = await GetByIdAsync(id);
        if (conversation != null)
        {
            _dbContext.ChatConversations.Remove(conversation);
            await _dbContext.SaveChangesAsync();
        }
    }
}
