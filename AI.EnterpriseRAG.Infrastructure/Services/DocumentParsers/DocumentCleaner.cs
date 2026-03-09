using System.Text.RegularExpressions;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// 企业级文档文本清洗工具
/// </summary>
public static class DocumentCleaner
{
    /// <summary>
    /// 清洗页眉、页脚、页码、版权声明等垃圾文本
    /// </summary>
    /// <param name="rawText">原始提取文本</param>
    /// <returns>清洗后的纯净文本</returns>
    public static string CleanDocumentText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        // 按行拆分并清洗
        var lines = rawText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                           .Select(line => line.Trim())
                           .ToList();

        var cleanedLines = new List<string>();
        foreach (var line in lines)
        {
            // 跳过空行
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 过滤规则（企业级适配）
            if (IsCopyrightLine(line)) continue;       // 版权行 © 2025 xxx
            if (IsPageNumberLine(line)) continue;      // 页码行 Page X of Y
            if (IsHeaderFooterLine(line)) continue;    // 页眉页脚（重复标题/保密声明）
            if (IsShortNoiseLine(line)) continue;      // 短噪音行（纯数字/符号）

            cleanedLines.Add(line);
        }

        // 合并为连续文本，保留段落分隔
        return string.Join("\n", cleanedLines);
    }

    #region 私有过滤规则
    /// <summary>
    /// 判断是否是版权声明行
    /// </summary>
    private static bool IsCopyrightLine(string line)
    {
        return line.Contains('©') && line.Contains("Reserved", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断是否是页码行
    /// </summary>
    private static bool IsPageNumberLine(string line)
    {
        return Regex.IsMatch(line, @"Page\s*\d+\s*of\s*\d+", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// 判断是否是页眉页脚行
    /// </summary>
    private static bool IsHeaderFooterLine(string line)
    {
        return line.Contains("confidential and proprietary", StringComparison.OrdinalIgnoreCase)
               || line.Equals("TrackWise 10 System Requirements", StringComparison.OrdinalIgnoreCase)
               || line.Contains("System Requirements", StringComparison.OrdinalIgnoreCase) && line.Length < 50;
    }

    /// <summary>
    /// 判断是否是短噪音行（纯数字/符号，无有效文本）
    /// </summary>
    private static bool IsShortNoiseLine(string line)
    {
        return line.Length < 5 && line.All(c => char.IsDigit(c) || c == '.' || c == '•' || c == '-');
    }
    #endregion
}