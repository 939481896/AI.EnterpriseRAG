namespace AI.EnterpriseRAG.Domain.Interfaces.UseCases;

/// <summary>
/// 文档用例接口
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
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档ID</returns>
    Task<Guid> UploadAndProcessDocumentAsync(string fileName, string fileType, long fileSize, Stream stream, CancellationToken cancellationToken = default);

    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task DeleteByCollectionNameAsync(Guid collectionId, CancellationToken cancellationToken = default);
}