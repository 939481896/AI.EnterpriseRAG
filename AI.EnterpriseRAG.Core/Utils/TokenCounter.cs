using System.Text;

namespace AI.EnterpriseRAG.Core.Utils;

/// <summary>
/// Token计数器（企业级Token管理，全系统通用）
/// </summary>
public static class TokenCounter
{
    /// <summary>
    /// 基础版：估算Token数量（适配中英文，默认推荐）
    /// </summary>
    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int chineseCharCount = 0;
        int nonChineseCharCount = 0;
        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
                chineseCharCount++;
            else
                nonChineseCharCount++;
        }

        int chineseToken = chineseCharCount;
        int nonChineseToken = (int)Math.Ceiling(nonChineseCharCount / 1.5);
        int totalToken = chineseToken + nonChineseToken;
        int compensation = (int)Math.Ceiling(totalToken * 0.05);

        return Math.Max(1, totalToken + compensation);
    }

    /// <summary>
    /// 进阶版：基于单词的Token估算（更精准，适合英文为主的文本）
    /// </summary>
    public static int EstimateTokenCount(string text, bool useWordBased)
    {
        if (!useWordBased)
            return EstimateTokenCount(text);

        if (string.IsNullOrEmpty(text))
            return 0;

        var words = SplitTextIntoWords(text);
        int wordCount = words.Count;
        int punctuationCount = text.Count(c => char.IsPunctuation(c));
        int tokenCount = wordCount + (int)Math.Ceiling(punctuationCount / 3.0);

        return Math.Max(1, tokenCount);
    }

    /// <summary>
    /// 校验文本是否超过Token阈值（企业级校验）
    /// </summary>
    public static bool IsExceedMaxToken(string text, int maxToken, bool useWordBased = false)
    {
        return EstimateTokenCount(text, useWordBased) > maxToken;
    }

    /// <summary>
    /// 拆分文本为单词/字符单元（内部辅助方法）
    /// </summary>
    private static List<string> SplitTextIntoWords(string text)
    {
        var words = new List<string>();
        StringBuilder currentWord = new StringBuilder();

        foreach (char c in text)
        {
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                words.Add(c.ToString());
            }
            else if (char.IsLetterOrDigit(c))
            {
                currentWord.Append(c);
            }
            else
            {
                if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                if (!char.IsWhiteSpace(c))
                {
                    words.Add(c.ToString());
                }
            }
        }

        if (currentWord.Length > 0)
        {
            words.Add(currentWord.ToString());
        }

        return words;
    }
}