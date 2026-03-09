using AI.EnterpriseRAG.Domain.Interfaces.Services;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Txt 文档解析器
/// </summary>
public class TxtDocumentParser : IDocumentParser
{
    public string SupportedFileType => "txt";

    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // 参数校验
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "TXT 流不能为空");
        if (!stream.CanRead)
            throw new ArgumentException("TXT 流不可读", nameof(stream));

        // 异步读取（避免内存溢出，逐行读取）
        var content = new StringBuilder();
        using (var reader = new StreamReader(stream, Encoding.UTF8, true))
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                content.AppendLine(line);
            }
        }

        return content.ToString();
    }
}