using AI.EnterpriseRAG.Core.Constants;
using AI.EnterpriseRAG.Core.Extensions;
using Microsoft.VisualBasic;
using System.Text;
using System.Text.RegularExpressions;
using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// 企业级文档语义分块服务（适配PDF/TXT/Word）
/// </summary>
public class DocumentChunkingService
{
    /// <summary>
    /// 将清洗后的文本拆分为语义完整的分块
    /// </summary>
    /// <param name="cleanText">清洗后的纯文本</param>
    /// <param name="fileName">文件名（用于元数据）</param>
    /// <param name="fileType">文件类型（pdf/txt/docx）</param>
    /// <param name="chunkSize">单分块最大Token数（默认取常量）</param>
    /// <param name="overlapSize">分块重叠Token数（默认10%）</param>
    /// <param name="useWordBasedToken">是否启用单词级Token计数（默认false，适合中文）</param>
    /// <returns>带元数据的语义分块列表</returns>
    public List<DocumentChunk> CreateSemanticChunks(
        string cleanText,
        string fileName,
        string fileType,
        int chunkSize = LLMConstants.MAX_CHUNK_TOKEN,
        int overlapSize = 0,
        bool useWordBasedToken = false)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
            return new List<DocumentChunk>();

        // 1. 按标题/章节重建结构化段落
        var sections = BuildStructuredSections(cleanText);

        // 2. 生成语义分块
        var chunks = new List<DocumentChunk>();
        int chunkId = 0;

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Content))
                continue;

            // 调用原有StringExtensions的SplitIntoChunks方法
            var semanticSubChunks = section.Content.SplitIntoChunks(
                chunkSize: chunkSize,
                overlapSize: overlapSize,
                useWordBasedToken: useWordBasedToken
            );

            // 为每个分块添加元数据
            foreach (var subChunk in semanticSubChunks)
            {
                chunks.Add(new DocumentChunk
                {
                    ChunkId = $"chunk-{fileName}-{++chunkId}",
                    SectionTitle = section.Title,
                    Content = subChunk.Trim(),
                    FileName = fileName,
                    FileType = fileType,
                    SectionLevel = section.Level
                });
            }
        }

        return chunks;
    }

    #region 私有方法：结构化段落重建
    /// <summary>
    /// 从纯文本中识别标题层级，重建结构化章节
    /// </summary>
    private List<DocumentSection> BuildStructuredSections(string text)
    {
        var lines = text.Split('\n').Select(line => line.Trim()).ToList();
        var sections = new List<DocumentSection>();
        DocumentSection currentSection = new() { Title = "默认章节", Level = 0, Content = "" };
        var contentBuilder = new StringBuilder();

        foreach (var line in lines)
        {
            // 识别标题行（适配技术文档的标题规则）
            if (IsTitleLine(line, out int level))
            {
                // 保存上一个章节
                if (contentBuilder.Length > 0)
                {
                    currentSection.Content = contentBuilder.ToString().Trim();
                    sections.Add(currentSection);
                    contentBuilder.Clear();
                }

                // 新建章节
                currentSection = new DocumentSection
                {
                    Title = line,
                    Level = level,
                    Content = ""
                };
            }
            else
            {
                // 普通行追加到当前章节
                contentBuilder.AppendLine(line);
            }
        }

        // 保存最后一个章节
        if (contentBuilder.Length > 0)
        {
            currentSection.Content = contentBuilder.ToString().Trim();
            sections.Add(currentSection);
        }

        return sections;
    }

    /// <summary>
    /// 判断是否是标题行，并返回标题层级（H1=1, H2=2...）
    /// </summary>
    private bool IsTitleLine(string line, out int level)
    {
        level = 0;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        // 规则1：数字+点开头（1. / 2.1. / 3.2.1.）
        var numberMatch = Regex.Match(line, @"^(\d+)(\.\d+)*\.\s+");
        if (numberMatch.Success)
        {
            level = numberMatch.Value.Count(c => c == '.'); // 1. → 1级，2.1. → 2级
            return true;
        }

        // 规则2：附录开头（Appendix A / Appendix B.1）
        if (Regex.IsMatch(line, @"^Appendix\s+[A-Z](\.\d+)*", RegexOptions.IgnoreCase))
        {
            level = 1;
            return true;
        }

        // 规则3：全大写短行（HARDWARE / SOFTWARE / CLOUD AND VIRTUAL ENVIRONMENTS）
        if (line.Length < 100 && line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || c == ':'))
        {
            level = 2;
            return true;
        }

        return false;
    }
    #endregion
}

/// <summary>
/// 文档章节模型（结构化拆分用）
/// </summary>
public class DocumentSection
{
    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 章节内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 标题层级（H1=1, H2=2...）
    /// </summary>
    public int Level { get; set; }
}
