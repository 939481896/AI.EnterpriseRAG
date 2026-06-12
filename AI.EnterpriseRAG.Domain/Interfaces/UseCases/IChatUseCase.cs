namespace AI.EnterpriseRAG.Domain.Interfaces.UseCases;

/// <summary>
/// 问答用例接口
/// </summary>
public interface IChatUseCase
{
    /// <summary>
    /// 智能问答
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="question">问题</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回答结果</returns>
    Task<(string Answer, List<string> References, decimal CostSeconds)> ChatAsync(string userId, string question, CancellationToken cancellationToken = default);

    /// <summary>
    /// V1.0 智能问答 (HyDE + Multi-Query + Self-Reflection)
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="question">问题</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回答结果</returns>
    Task<(string Answer, List<string> References, decimal CostSeconds)> ChatV1Async(string userId, string question, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的对话历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>对话历史列表</returns>
    Task<List<object>> GetUserConversationsAsync(
        string userId, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除对话记录
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteConversationAsync(
        Guid conversationId, 
        CancellationToken cancellationToken = default);
}