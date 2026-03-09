namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// 文档解析接口（企业级抽象）
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// 支持的文件类型
    /// </summary>
    string SupportedFileType { get; }

    /// <summary>
    /// 解析文档
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解析后的文本</returns>
    Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
}