using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NPOI.XWPF.UserModel;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Enterprise-grade Word document parser using NPOI (Apache 2.0)
/// ✅ Supports .docx (modern Office format)
/// ✅ Extracts paragraphs, tables, headers/footers with structure
/// ✅ 100% .NET native, production-ready
/// Note: For legacy .doc support, install NPOI.HWPF package separately
/// </summary>
public class NpoiWordParser : IDocumentParser
{
    private readonly ILogger<NpoiWordParser> _logger;

    public string SupportedFileType => "docx";

    public NpoiWordParser(ILogger<NpoiWordParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse Word document stream (.docx) and extract structured text
    /// </summary>
    /// <param name="stream">Word file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted text with preserved structure</returns>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "Word stream cannot be null");
        if (!stream.CanRead)
            throw new ArgumentException("Word stream is not readable", nameof(stream));

        try
        {
            return await Task.Run(() => ParseDocx(stream, cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Word parsing cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to parse Word document");
            throw new InvalidDataException($"Failed to parse Word document: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse modern .docx format (Office 2007+)
    /// </summary>
    private string ParseDocx(Stream stream, CancellationToken cancellationToken)
    {
        using var document = new XWPFDocument(stream);
        var contentBuilder = new StringBuilder();

        _logger.LogInformation("📄 Parsing .docx: {Paragraphs} paragraphs, {Tables} tables",
            document.Paragraphs.Count, document.Tables.Count);

        // 1. Extract document properties (metadata)
        var metadata = ExtractDocxMetadata(document);
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            contentBuilder.AppendLine(metadata);
            contentBuilder.AppendLine();
        }

        // 2. Extract headers
        foreach (var header in document.HeaderList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var paragraph in header.Paragraphs)
            {
                var text = paragraph.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    contentBuilder.AppendLine($"[Header] {text}");
                }
            }
        }

        // 3. Extract body paragraphs with style detection
        foreach (var paragraph in document.Paragraphs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = paragraph.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;

            // Detect heading styles (H1, H2, H3...)
            var style = paragraph.Style?.ToLower() ?? "";
            if (style.Contains("heading") || style.Contains("title"))
            {
                // Extract heading level
                int level = 1;
                if (style.Contains("2")) level = 2;
                else if (style.Contains("3")) level = 3;
                else if (style.Contains("4")) level = 4;

                contentBuilder.AppendLine($"\n{new string('#', level)} {text}\n");
            }
            else
            {
                contentBuilder.AppendLine(text);
            }
        }

        // 4. Extract tables
        if (document.Tables.Count > 0)
        {
            contentBuilder.AppendLine("\n=== Document Tables ===\n");

            for (int tableIndex = 0; tableIndex < document.Tables.Count; tableIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var table = document.Tables[tableIndex];
                contentBuilder.AppendLine($"Table {tableIndex + 1}:");

                foreach (var row in table.Rows)
                {
                    var cells = new List<string>();
                    foreach (var cell in row.GetTableCells())
                    {
                        var cellText = cell.GetText()?.Trim() ?? "";
                        cells.Add(cellText);
                    }
                    contentBuilder.AppendLine($"| {string.Join(" | ", cells)} |");
                }
                contentBuilder.AppendLine();
            }
        }

        // 5. Extract footers
        foreach (var footer in document.FooterList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var paragraph in footer.Paragraphs)
            {
                var text = paragraph.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    contentBuilder.AppendLine($"[Footer] {text}");
                }
            }
        }

        var finalText = contentBuilder.ToString();
        _logger.LogInformation("✅ .docx parsing complete: {Length} characters", finalText.Length);
        return finalText;
    }

    /// <summary>
    /// Extract .docx metadata for RAG context
    /// </summary>
    private string ExtractDocxMetadata(XWPFDocument document)
    {
        try
        {
            var properties = document.GetProperties();
            if (properties == null)
                return string.Empty;

            var metadata = new StringBuilder();
            metadata.AppendLine("=== Document Metadata ===");

            var coreProps = properties.CoreProperties;
            if (coreProps != null)
            {
                if (!string.IsNullOrWhiteSpace(coreProps.Title))
                    metadata.AppendLine($"Title: {coreProps.Title}");
                if (!string.IsNullOrWhiteSpace(coreProps.Creator))
                    metadata.AppendLine($"Author: {coreProps.Creator}");
                if (!string.IsNullOrWhiteSpace(coreProps.Subject))
                    metadata.AppendLine($"Subject: {coreProps.Subject}");
                if (!string.IsNullOrWhiteSpace(coreProps.Keywords))
                    metadata.AppendLine($"Keywords: {coreProps.Keywords}");
                if (coreProps.Created.HasValue)
                    metadata.AppendLine($"Created: {coreProps.Created.Value:yyyy-MM-dd}");
                if (coreProps.Modified.HasValue)
                    metadata.AppendLine($"Modified: {coreProps.Modified.Value:yyyy-MM-dd}");
            }

            metadata.AppendLine($"Paragraphs: {document.Paragraphs.Count}");
            metadata.AppendLine($"Tables: {document.Tables.Count}");
            metadata.AppendLine("========================");

            return metadata.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract Word metadata");
            return string.Empty;
        }
    }
}
