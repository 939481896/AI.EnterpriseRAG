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
}