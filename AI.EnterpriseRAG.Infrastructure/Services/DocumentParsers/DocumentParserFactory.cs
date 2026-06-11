using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Document parser factory (auto-matches file types)
/// NOTE: This factory is deprecated - prefer using IServiceProvider with DI instead
/// </summary>
[Obsolete("Use IServiceProvider to resolve IDocumentParser instances via DI instead")]
public static class DocumentParserFactory
{
    /// <summary>
    /// Get parser based on filename extension
    /// </summary>
    /// <param name="fileName">Filename with extension</param>
    /// <param name="loggerFactory">Logger factory for creating parsers</param>
    /// <returns>Matching IDocumentParser</returns>
    /// <exception cref="NotSupportedException">Unsupported file type</exception>
    public static IDocumentParser GetParser(string fileName, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName), "Filename cannot be empty");

        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return extension switch
        {
            "pdf" => new PdfPigParser(loggerFactory.CreateLogger<PdfPigParser>()),
            "txt" => new TxtDocumentParser(),
            "docx" => new NpoiWordParser(loggerFactory.CreateLogger<NpoiWordParser>()),
            "xlsx" => new NpoiExcelParser(loggerFactory.CreateLogger<NpoiExcelParser>()),
            "xls" => new NpoiExcelParser(loggerFactory.CreateLogger<NpoiExcelParser>()),
            "md" => new MarkdigParser(loggerFactory.CreateLogger<MarkdigParser>()),
            "html" => new HtmlParser(loggerFactory.CreateLogger<HtmlParser>()),
            "csv" => new CsvParser(loggerFactory.CreateLogger<CsvParser>()),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}")
        };
    }
}