using AI.EnterpriseRAG.Domain.Interfaces.Services;
using static Xceed.Words.NET.DocX;
using System.Text;
using Xceed.Words.NET;
using Xceed.Document.NET;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Word (.docx) 文档解析器（企业级：识别标题层级/段落/列表）
/// 适配你的 IDocumentParser 接口，可直接接入现有工厂和业务流程
/// </summary>
public class WordDocumentParser : IDocumentParser
{
    /// <summary>
    /// 支持的文件类型标识
    /// </summary>
    public string SupportedFileType => "docx";

    /// <summary>
    /// 异步解析 Word 流并提取结构化文本
    /// </summary>
    /// <param name="stream">Word (.docx) 文件流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提取的结构化文本（保留标题层级和段落）</returns>
    /// <exception cref="ArgumentNullException">流为空时抛出</exception>
    /// <exception cref="ArgumentException">流不可读时抛出</exception>
    /// <exception cref="InvalidDataException">非合法docx文件时抛出</exception>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // 1. 参数校验（和你的PDF/TXT解析器保持一致）
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "Word 流不能为空");
        if (!stream.CanRead)
            throw new ArgumentException("Word 流不可读", nameof(stream));

        // 2. 异步解析（避免阻塞线程池）
        return await Task.Run(() =>
        {
            try
            {
                var contentBuilder = new StringBuilder();

                // 3. 加载 Word 文档（DocX 核心逻辑）
                using var document = DocX.Load(stream);

                // 4. 遍历文档元素，按结构提取文本
                foreach (var paragraph in document.Paragraphs)
                {
                    // 检查取消令牌
                    cancellationToken.ThrowIfCancellationRequested();

                    var paragraphText = paragraph.Text.Trim();
                    if (string.IsNullOrWhiteSpace(paragraphText))
                        continue;

                    // 5. 识别标题样式，添加层级标记（便于后续分块识别）
                    string styledText = GetStyledParagraphText(paragraph, paragraphText);
                    contentBuilder.AppendLine(styledText);
                }

                // 6. 处理表格（可选：表格内容单独成块）
                foreach (var table in document.Tables)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tableText = new StringBuilder();
                    tableText.AppendLine("=== 表格开始 ===");

                    foreach (var row in table.Rows)
                    {
                        foreach (var cell in row.Cells)
                        {
                            var cellText = cell.Paragraphs.Select(p => p.Text.Trim())
                                                        .Where(t => !string.IsNullOrWhiteSpace(t))
                                                        .Aggregate((a, b) => $"{a} {b}");

                            if (!string.IsNullOrWhiteSpace(cellText))
                                tableText.AppendLine(cellText);
                        }
                        tableText.AppendLine("--- 行结束 ---");
                    }

                    tableText.AppendLine("=== 表格结束 ===");
                    contentBuilder.AppendLine(tableText.ToString());
                }

                return contentBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Word 文档解析失败：可能是非法docx文件或文件损坏", ex);
            }
        }, cancellationToken);
    }

    #region 私有辅助方法：识别标题样式并添加层级标记
    /// <summary>
    /// 根据 Word 段落样式生成带层级标记的文本
    /// 例如：H1标题 → "1. 标题内容"，H2标题 → "2. 子标题内容"
    /// </summary>
    private string GetStyledParagraphText(Paragraph paragraph, string text)
    {
        // 核心：识别 Word 内置标题样式
        var styleName = paragraph.StyleName?.ToLowerInvariant() ?? string.Empty;

        // H1 标题（一级标题：如 1. Introduction）
        if (styleName.Contains("heading1") || styleName.Contains("标题 1"))
        {
            return $"1. {text}";
        }
        // H2 标题（二级标题：如 2.1 TrackWise Architecture）
        else if (styleName.Contains("heading2") || styleName.Contains("标题 2"))
        {
            return $"2. {text}";
        }
        // H3 标题（三级标题：如 3.1.1 Hardware Requirements）
        else if (styleName.Contains("heading3") || styleName.Contains("标题 3"))
        {
            return $"3. {text}";
        }
        // 列表项（有序/无序列表）
        else if (paragraph.ListItemType != null)
        {
            return $"• {text}";
        }
        // 普通段落
        else
        {
            return text;
        }
    }
    #endregion
}