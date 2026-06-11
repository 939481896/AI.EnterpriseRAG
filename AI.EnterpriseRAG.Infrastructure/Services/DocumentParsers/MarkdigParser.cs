using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Markdig;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Markdown parser using Markdig (BSD-2-Clause license)
/// ✅ Fast and extensible CommonMark + GFM parser
/// ✅ Supports tables, task lists, footnotes, emoji
/// ✅ Perfect for documentation, READMEs, knowledge bases
/// </summary>
public class MarkdigParser : IDocumentParser
{
    private readonly ILogger<MarkdigParser> _logger;
    private readonly MarkdownPipeline _pipeline;

    public string SupportedFileType => "md";

    public MarkdigParser(ILogger<MarkdigParser> logger)
    {
        _logger = logger;

        // Configure Markdig pipeline with advanced features
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Enables tables, task lists, auto-links, etc.
            .UseEmojiAndSmiley() // :smile: -> 😊
            .UseAutoLinks() // Auto-detect URLs
            .UsePipeTables() // GitHub-style tables
            .UseTaskLists() // - [ ] Task items
            .UseFootnotes() // [^1]: footnote
            .UseGenericAttributes() // {#id .class}
            .Build();
    }

    /// <summary>
    /// Parse Markdown file and convert to plain text with preserved structure
    /// </summary>
    /// <param name="stream">Markdown file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain text representation of Markdown content</returns>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "Markdown stream cannot be null");
        if (!stream.CanRead)
            throw new ArgumentException("Markdown stream is not readable", nameof(stream));

        try
        {
            // Read markdown content
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var markdownContent = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(markdownContent))
            {
                _logger.LogWarning("Empty markdown file detected");
                return string.Empty;
            }

            _logger.LogInformation("📝 Parsing Markdown: {Length} characters", markdownContent.Length);

            // Parse Markdown to HTML first (to process all extensions)
            var htmlContent = Markdown.ToHtml(markdownContent, _pipeline);

            // Convert HTML back to clean text while preserving structure
            var plainText = HtmlToPlainText(htmlContent);

            // Alternatively, for RAG systems, you might want to keep the markdown as-is
            // since it already has good structure:
            // return markdownContent;

            _logger.LogInformation("✅ Markdown parsing complete: {Length} characters extracted", plainText.Length);
            return plainText;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Markdown parsing cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to parse Markdown document");
            throw new InvalidDataException($"Failed to parse Markdown: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Convert HTML to clean plain text while preserving structure
    /// (Simple implementation - can be enhanced with HtmlAgilityPack for complex HTML)
    /// </summary>
    private string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = html;

        // Remove HTML tags while preserving structure
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<h[1-6][^>]*>(.*?)</h[1-6]>", "\n\n## $1\n\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<p[^>]*>(.*?)</p>", "$1\n\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", "\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<li[^>]*>(.*?)</li>", "- $1\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", string.Empty);

        // Decode HTML entities
        text = System.Net.WebUtility.HtmlDecode(text);

        // Clean up excessive whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Alternative: Keep Markdown format for RAG systems
    /// (Markdown structure is already semantic and LLM-friendly)
    /// </summary>
    public async Task<string> ParseAsMarkdownAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync(cancellationToken);

        _logger.LogInformation("📝 Loaded Markdown as-is: {Length} characters", content.Length);
        return content;
    }
}
