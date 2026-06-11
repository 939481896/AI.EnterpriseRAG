using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Enterprise-grade PDF parser using PdfPig (Apache 2.0 license)
/// ✅ Replaces iText7 to avoid AGPL licensing issues
/// ✅ 100% .NET native, no Python dependencies
/// ✅ Supports text extraction, tables, and metadata
/// </summary>
public class PdfPigParser : IDocumentParser
{
    private readonly ILogger<PdfPigParser> _logger;

    public string SupportedFileType => "pdf";

    public PdfPigParser(ILogger<PdfPigParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse PDF stream and extract structured text
    /// </summary>
    /// <param name="stream">PDF file stream</param>
    /// <param name="cancellationToken">Cancellation token for long operations</param>
    /// <returns>Extracted text content with preserved structure</returns>
    /// <exception cref="ArgumentNullException">Stream is null</exception>
    /// <exception cref="ArgumentException">Stream is not readable</exception>
    /// <exception cref="InvalidDataException">Invalid PDF format</exception>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // 1. Parameter validation
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "PDF stream cannot be null");
        if (!stream.CanRead)
            throw new ArgumentException("PDF stream is not readable", nameof(stream));

        try
        {
            // 2. Execute parsing asynchronously (avoid blocking thread pool)
            return await Task.Run(() =>
            {
                var contentBuilder = new StringBuilder();

                // 3. Open PDF document with PdfPig
                using var document = PdfDocument.Open(stream);
                
                _logger.LogInformation("📄 Parsing PDF: {Pages} pages detected", document.NumberOfPages);

                // 4. Iterate through all pages
                for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                {
                    // Check cancellation token (support mid-operation cancellation)
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = document.GetPage(pageNum);

                    // 5. Add page header for better structure
                    contentBuilder.AppendLine($"\n=== Page {pageNum}/{document.NumberOfPages} ===\n");

                    // 6. Extract text with layout preservation
                    var pageText = ExtractPageText(page);
                    contentBuilder.AppendLine(pageText);

                    // 7. Log progress for large documents
                    if (pageNum % 10 == 0)
                    {
                        _logger.LogDebug("Processed {Current}/{Total} pages", pageNum, document.NumberOfPages);
                    }
                }

                // 8. Extract metadata (optional but useful for RAG context)
                var metadata = ExtractMetadata(document);
                if (!string.IsNullOrWhiteSpace(metadata))
                {
                    contentBuilder.Insert(0, metadata + "\n\n");
                }

                var finalText = contentBuilder.ToString();
                _logger.LogInformation("✅ PDF parsing complete: {Length} characters extracted", finalText.Length);

                return finalText;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ PDF parsing cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to parse PDF document");
            throw new InvalidDataException($"Failed to parse PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extract text from a single page with layout preservation
    /// </summary>
    private string ExtractPageText(Page page)
    {
        try
        {
            // PdfPig provides multiple text extraction strategies:
            // 1. page.Text - Simple concatenation
            // 2. page.GetWords() - Word-level extraction (better for tables)
            
            var words = page.GetWords();
            if (!words.Any())
                return string.Empty;

            var textBuilder = new StringBuilder();
            var currentLine = new List<Word>();
            double? previousY = null;
            const double lineThreshold = 5.0; // Tolerance for same-line detection

            // Group words by line (Y-coordinate proximity)
            foreach (var word in words.OrderBy(w => -w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left))
            {
                if (previousY.HasValue && Math.Abs(word.BoundingBox.Bottom - previousY.Value) > lineThreshold)
                {
                    // New line detected, flush current line
                    if (currentLine.Any())
                    {
                        textBuilder.AppendLine(string.Join(" ", currentLine.Select(w => w.Text)));
                        currentLine.Clear();
                    }
                }

                currentLine.Add(word);
                previousY = word.BoundingBox.Bottom;
            }

            // Flush last line
            if (currentLine.Any())
            {
                textBuilder.AppendLine(string.Join(" ", currentLine.Select(w => w.Text)));
            }

            return textBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract structured text from page, falling back to simple extraction");
            return page.Text; // Fallback to simple text extraction
        }
    }

    /// <summary>
    /// Extract PDF metadata for RAG context enrichment
    /// </summary>
    private string ExtractMetadata(PdfDocument document)
    {
        try
        {
            var info = document.Information;
            var metadata = new StringBuilder();

            metadata.AppendLine("=== Document Metadata ===");

            if (!string.IsNullOrWhiteSpace(info.Title))
                metadata.AppendLine($"Title: {info.Title}");
            if (!string.IsNullOrWhiteSpace(info.Author))
                metadata.AppendLine($"Author: {info.Author}");
            if (!string.IsNullOrWhiteSpace(info.Subject))
                metadata.AppendLine($"Subject: {info.Subject}");
            if (!string.IsNullOrWhiteSpace(info.Keywords))
                metadata.AppendLine($"Keywords: {info.Keywords}");

            // Creation and Modified dates are string properties in PdfPig
            if (!string.IsNullOrWhiteSpace(info.CreationDate))
                metadata.AppendLine($"Created: {info.CreationDate}");
            if (!string.IsNullOrWhiteSpace(info.ModifiedDate))
                metadata.AppendLine($"Modified: {info.ModifiedDate}");

            metadata.AppendLine($"Pages: {document.NumberOfPages}");
            metadata.AppendLine("========================");

            return metadata.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract PDF metadata");
            return string.Empty;
        }
    }
}
