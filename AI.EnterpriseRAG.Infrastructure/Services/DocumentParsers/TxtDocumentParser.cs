using AI.EnterpriseRAG.Domain.Interfaces.Services;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Txt 文档解析器
/// </summary>
public class TxtDocumentParser : IDocumentParser
{
    public string SupportedFileType => "txt";

    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using (var reader = new StreamReader(stream))
        {
            return await reader.ReadToEndAsync(cancellationToken);
        }
    }
}
