using AI.EnterpriseRAG.Domain.Interfaces.Services;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// 文档解析器工厂（自动匹配文件类型）
/// </summary>
public static class DocumentParserFactory
{
    /// <summary>
    /// 根据文件名获取对应的解析器
    /// </summary>
    /// <param name="fileName">文件名（含扩展名）</param>
    /// <returns>匹配的IDocumentParser</returns>
    /// <exception cref="NotSupportedException">不支持的文件类型</exception>
    public static IDocumentParser GetParser(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName), "文件名不能为空");

        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "pdf" => new PdfDocumentParser(),
            "txt" => new TxtDocumentParser(),
             "docx" => new WordDocumentParser(),
            _ => throw new NotSupportedException($"不支持的文件类型：{extension}")
        };
    }
}