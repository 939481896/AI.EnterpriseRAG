using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// HTML parser using HtmlAgilityPack (MIT license)
/// ✅ Robust HTML parsing with XPath support
/// ✅ Handles malformed HTML gracefully
/// ✅ Perfect for web content, documentation, reports
/// </summary>
public class HtmlParser : IDocumentParser
{
    private readonly ILogger<HtmlParser> _logger;

    public string SupportedFileType => "html";

    public HtmlParser(ILogger<HtmlParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse HTML file and extract clean text content
    /// </summary>
    /// <param name="stream">HTML file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted text with preserved structure</returns>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "HTML stream cannot be null");
        if (!stream.CanRead)
            throw new ArgumentException("HTML stream is not readable", nameof(stream));

        try
        {
            return await Task.Run(() =>
            {
                var document = new HtmlDocument();
                document.Load(stream, Encoding.UTF8);

                _logger.LogInformation("🌐 Parsing HTML document");

                var contentBuilder = new StringBuilder();

                // Extract title
                var title = document.DocumentNode.SelectSingleNode("//title")?.InnerText;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    contentBuilder.AppendLine($"# {title.Trim()}");
                    contentBuilder.AppendLine();
                }

                // Extract meta description (useful for RAG context)
                var description = document.DocumentNode
                    .SelectSingleNode("//meta[@name='description']")
                    ?.GetAttributeValue("content", string.Empty);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    contentBuilder.AppendLine($"**Description:** {description.Trim()}");
                    contentBuilder.AppendLine();
                }

                // Remove script and style elements (noise)
                var scriptsAndStyles = document.DocumentNode.SelectNodes("//script|//style");
                if (scriptsAndStyles != null)
                {
                    foreach (var node in scriptsAndStyles)
                    {
                        node.Remove();
                    }
                }

                // Extract body content with structure
                var body = document.DocumentNode.SelectSingleNode("//body") ?? document.DocumentNode;
                var bodyText = ExtractStructuredText(body, cancellationToken);
                contentBuilder.Append(bodyText);

                var finalText = contentBuilder.ToString();
                _logger.LogInformation("✅ HTML parsing complete: {Length} characters", finalText.Length);
                return finalText;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ HTML parsing cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to parse HTML document");
            throw new InvalidDataException($"Failed to parse HTML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extract text with preserved structure (headings, lists, paragraphs)
    /// </summary>
    private string ExtractStructuredText(HtmlNode node, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();

        foreach (var child in node.ChildNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (child.Name.ToLower())
            {
                case "h1":
                    builder.AppendLine($"\n# {child.InnerText.Trim()}\n");
                    break;

                case "h2":
                    builder.AppendLine($"\n## {child.InnerText.Trim()}\n");
                    break;

                case "h3":
                    builder.AppendLine($"\n### {child.InnerText.Trim()}\n");
                    break;

                case "h4":
                case "h5":
                case "h6":
                    builder.AppendLine($"\n#### {child.InnerText.Trim()}\n");
                    break;

                case "p":
                    var text = child.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        builder.AppendLine(text);
                        builder.AppendLine();
                    }
                    break;

                case "ul":
                case "ol":
                    var listItems = child.SelectNodes(".//li");
                    if (listItems != null)
                    {
                        foreach (var item in listItems)
                        {
                            builder.AppendLine($"- {item.InnerText.Trim()}");
                        }
                        builder.AppendLine();
                    }
                    break;

                case "table":
                    builder.AppendLine("\n=== Table ===");
                    var rows = child.SelectNodes(".//tr");
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            var cells = row.SelectNodes(".//td|.//th");
                            if (cells != null)
                            {
                                var cellTexts = cells.Select(c => c.InnerText.Trim());
                                builder.AppendLine($"| {string.Join(" | ", cellTexts)} |");
                            }
                        }
                    }
                    builder.AppendLine("=============\n");
                    break;

                case "br":
                    builder.AppendLine();
                    break;

                case "div":
                case "section":
                case "article":
                case "main":
                    // Recursively process container elements
                    builder.Append(ExtractStructuredText(child, cancellationToken));
                    break;

                case "#text":
                    var nodeText = child.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(nodeText))
                    {
                        builder.Append(nodeText + " ");
                    }
                    break;

                default:
                    // For unknown elements, still try to extract text
                    if (child.HasChildNodes)
                    {
                        builder.Append(ExtractStructuredText(child, cancellationToken));
                    }
                    break;
            }
        }

        return builder.ToString();
    }
}
