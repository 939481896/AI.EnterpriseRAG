namespace AI.EnterpriseRAG.Domain.Interfaces.UseCases;

/// <summary>
/// 文档用例接口（支持权限控制）
/// </summary>
public interface IDocumentUseCase
{
    /// <summary>
    /// 上传并处理文档
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="fileSize">文件大小</param>
    /// <param name="stream">文件流</param>
    /// <param name="uploadedBy">上传者账号</param>
    /// <param name="tenantId">租户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档ID</returns>
    Task<Guid> UploadAndProcessDocumentAsync(
        string fileName, 
        string fileType, 
        long fileSize, 
        Stream stream, 
        string uploadedBy,      // 🆕 上传者
        string? tenantId = null, // 🆕 租户ID
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task DeleteByCollectionNameAsync(Guid collectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的文档列表
    /// </summary>
    Task<object> GetUserDocumentsAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重新处理文档（用于恢复失败的任务）
    /// </summary>
    /// <param name="documentId">文档ID</param>
    /// <param name="fileStream">文件流（如果为null则从存储读取）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReprocessDocumentAsync(
        Guid documentId, 
        Stream? fileStream = null, 
        CancellationToken cancellationToken = default);
}