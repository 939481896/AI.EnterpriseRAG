namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

public interface IPermissionService
{
    Task<List<string>> GetUserAllowedDocumentIdsAsync(string userId, CancellationToken ct = default);
    Task<string> GetUserCollectionNameAsync(string userId, CancellationToken ct = default);
}