using AI.EnterpriseRAG.Core.Constants;
using AI.EnterpriseRAG.Core.Utils;
using System.Text;
using System.Text.RegularExpressions;

namespace AI.EnterpriseRAG.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 企业级语义文本分块（过滤页眉页脚+语义优先+不切断句子）
        /// 兼容基础版/进阶版Token计数
        /// </summary>
        /// <param name="text">待分块文本</param>
        /// <param name="chunkSize">单分块最大Token数</param>
        /// <param name="overlapSize">分块重叠Token数（默认10%）</param>
        /// <param name="useWordBasedToken">是否启用单词级Token计数（默认false，适合中文）</param>
        /// <returns>语义完整的分块列表</returns>
        public static List<string> SplitIntoChunks(
            this string text,
            int chunkSize = Constants.LLMConstants.MAX_CHUNK_TOKEN,
            int overlapSize = 0,
            bool useWordBasedToken = false)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // 1. 核心修复：先清洗垃圾文本（页眉/页脚/版权/页码）
            var cleanedText = CleanHeaderFooterAndCopyright(text);

            // 2. 重叠窗口配置（通用逻辑）
            overlapSize = overlapSize <= 0 ? Math.Max(20, chunkSize / 10) : overlapSize;
            overlapSize = Math.Min(overlapSize, chunkSize / 2);

            var chunks = new List<string>();
            // 3. 按语义块拆分（修复版：优先按章节/标题/段落）
            var semanticBlocks = SplitByEnhancedSemanticBlocks(cleanedText);

            // 4. 遍历语义块分块（核心逻辑）
            foreach (var block in semanticBlocks)
            {
                if (string.IsNullOrWhiteSpace(block))
                    continue;

                // 关键：根据useWordBasedToken选择Token计数方式
                var blockTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(block, true)
                    : TokenCounter.EstimateTokenCount(block);

                if (blockTokenCount <= chunkSize)
                {
                    chunks.Add(block.Trim());
                    continue;
                }

                var blockChunks = SplitBlockIntoSemanticChunks(block, chunkSize, overlapSize, useWordBasedToken);
                chunks.AddRange(blockChunks);
            }

            return chunks;
        }

        #region 核心修复：新增/重构方法
        /// <summary>
        /// 清洗页眉/页脚/版权/页码等垃圾文本
        /// </summary>
        private static string CleanHeaderFooterAndCopyright(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
                            .Select(line => line.Trim())
                            .ToList();

            var cleanedLines = new List<string>();
            foreach (var line in lines)
            {
                // 过滤规则：适配TrackWise文档特征
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Contains("©") && line.Contains("Reserved")) continue; // 版权行
                if (line.Contains("Page") && line.Contains("of")) continue;    // 页码行
                if (line.Contains("confidential and proprietary")) continue;   // 保密声明
                if (line.Length < 5 && line.All(c => char.IsDigit(c) || c == '.')) continue; // 纯数字短行
                if (line.Equals("TrackWise 10 System Requirements", StringComparison.OrdinalIgnoreCase)) continue; // 重复页眉

                cleanedLines.Add(line);
            }

            return string.Join("\n", cleanedLines);
        }

        /// <summary>
        /// 增强版语义块拆分（按章节/标题/段落拆分，保留语义完整）
        /// </summary>
        private static List<string> SplitByEnhancedSemanticBlocks(string text)
        {
            var blocks = new List<string>();
            var lines = text.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => line.Trim())
                            .ToList();

            var currentBlock = new StringBuilder();
            foreach (var line in lines)
            {
                // 标题行：单独成块
                if (IsTitleLine(line))
                {
                    if (currentBlock.Length > 0)
                    {
                        blocks.Add(currentBlock.ToString().Trim());
                        currentBlock.Clear();
                    }
                    currentBlock.AppendLine(line);
                    blocks.Add(currentBlock.ToString().Trim());
                    currentBlock.Clear();
                }
                // 段落分隔：空行表示段落结束
                else if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentBlock.Length > 0)
                    {
                        blocks.Add(currentBlock.ToString().Trim());
                        currentBlock.Clear();
                    }
                }
                // 普通行：追加到当前块
                else
                {
                    currentBlock.AppendLine(line);
                }
            }

            // 处理最后一个块
            if (currentBlock.Length > 0)
            {
                blocks.Add(currentBlock.ToString().Trim());
            }

            return blocks.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
        }

        /// <summary>
        /// 判断是否是标题行
        /// </summary>
        private static bool IsTitleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;

            // 章节标题：1. Introduction、2. TrackWise Architecture...
            if (Regex.IsMatch(line, @"^\d+\.\s+[A-Z][a-zA-Z\s]+")) return true;
            // 附录标题：Appendix A: ...
            if (Regex.IsMatch(line, @"^Appendix\s+[A-Z]+\:\s+.+")) return true;
            // 表格标题：Table 1. ...、Figure 1. ...
            if (Regex.IsMatch(line, @"^Table\s+\d+\.\s+.+") || Regex.IsMatch(line, @"^Figure\s+\d+\.\s+.+")) return true;
            // 全大写/短标题：HARDWARE、SOFTWARE、CLOUD AND VIRTUAL ENVIRONMENTS
            if (line.Length < 100 && line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || c == ':' || c == '.')) return true;

            return false;
        }

        /// <summary>
        /// 拆分大块文本为语义分块（不切断句子）
        /// </summary>
        private static List<string> SplitBlockIntoSemanticChunks(
            string block,
            int chunkSize,
            int overlapSize,
            bool useWordBasedToken)
        {
            var chunks = new List<string>();
            // 按句末分隔符拆分句子（兼容中英文）
            var sentences = SplitByEnhancedSentenceSeparators(block);

            var currentChunk = new List<string>();
            int currentTokenCount = 0;

            foreach (var sentence in sentences)
            {
                var sentenceTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(sentence, true)
                    : TokenCounter.EstimateTokenCount(sentence);

                // 处理超长句子（按单词拆分，不切断）
                if (sentenceTokenCount > chunkSize)
                {
                    if (currentChunk.Any())
                    {
                        chunks.Add(string.Join(" ", currentChunk).Trim());
                        var overlapText = GetEnhancedOverlapText(currentChunk, overlapSize, useWordBasedToken);
                        currentChunk.Clear();
                        if (!string.IsNullOrWhiteSpace(overlapText))
                        {
                            currentChunk.Add(overlapText);
                            currentTokenCount = useWordBasedToken
                                ? TokenCounter.EstimateTokenCount(overlapText, true)
                                : TokenCounter.EstimateTokenCount(overlapText);
                        }
                        else
                        {
                            currentTokenCount = 0;
                        }
                    }

                    var longSentenceChunks = SplitLongSentenceEnhanced(sentence, chunkSize, overlapSize, useWordBasedToken);
                    chunks.AddRange(longSentenceChunks);
                    continue;
                }

                // 普通句子：不超阈值则追加，超阈值则保存当前块
                if (currentTokenCount + sentenceTokenCount > chunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join(" ", currentChunk).Trim());
                    var overlapText = GetEnhancedOverlapText(currentChunk, overlapSize, useWordBasedToken);
                    currentChunk.Clear();
                    if (!string.IsNullOrWhiteSpace(overlapText))
                    {
                        currentChunk.Add(overlapText);
                        currentTokenCount = useWordBasedToken
                            ? TokenCounter.EstimateTokenCount(overlapText, true)
                            : TokenCounter.EstimateTokenCount(overlapText);
                    }
                    else
                    {
                        currentTokenCount = 0;
                    }
                }

                currentChunk.Add(sentence);
                currentTokenCount += sentenceTokenCount;
            }

            // 处理最后一个块
            if (currentChunk.Any())
            {
                chunks.Add(string.Join(" ", currentChunk).Trim());
            }

            return chunks;
        }

        /// <summary>
        /// 增强版句子拆分（兼容中英文句末分隔符）
        /// </summary>
        private static List<string> SplitByEnhancedSentenceSeparators(string text)
        {
            var separators = new[] { '。', '！', '？', '；', '.', '!', '?', ';' };
            var sentences = new List<string>();
            int startIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (separators.Contains(text[i]))
                {
                    var sentence = text.Substring(startIndex, i - startIndex + 1).Trim();
                    if (!string.IsNullOrWhiteSpace(sentence))
                        sentences.Add(sentence);
                    startIndex = i + 1;
                }
            }

            // 处理最后一个句子
            if (startIndex < text.Length)
            {
                var lastSentence = text.Substring(startIndex).Trim();
                if (!string.IsNullOrWhiteSpace(lastSentence))
                    sentences.Add(lastSentence);
            }

            return sentences;
        }

        /// <summary>
        /// 增强版重叠文本获取（更精准的上下文保留）
        /// </summary>
        private static string GetEnhancedOverlapText(List<string> currentChunk, int overlapSize, bool useWordBasedToken)
        {
            var reversed = currentChunk.AsEnumerable().Reverse().ToList();
            var overlapParts = new List<string>();
            int currentTokenCount = 0;

            foreach (var part in reversed)
            {
                var tokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(part, true)
                    : TokenCounter.EstimateTokenCount(part);

                if (currentTokenCount + tokenCount > overlapSize)
                    break;

                overlapParts.Add(part);
                currentTokenCount += tokenCount;
            }

            return string.Join(" ", overlapParts.AsEnumerable().Reverse()).Trim();
        }

        /// <summary>
        /// 增强版超长句子拆分（按单词拆分，保留上下文）
        /// </summary>
        private static List<string> SplitLongSentenceEnhanced(
            string sentence,
            int chunkSize,
            int overlapSize,
            bool useWordBasedToken)
        {
            var chunks = new List<string>();
            var words = sentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = new List<string>();
            int currentTokenCount = 0;

            foreach (var word in words)
            {
                var wordTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(word, true)
                    : TokenCounter.EstimateTokenCount(word);

                // 处理超长单词（单个单词超阈值）
                if (wordTokenCount > chunkSize)
                {
                    if (currentChunk.Any())
                    {
                        chunks.Add(string.Join(" ", currentChunk).Trim());
                        currentChunk.Clear();
                        currentTokenCount = 0;
                    }

                    var wordChunks = SplitLongWordEnhanced(word, chunkSize, useWordBasedToken);
                    chunks.AddRange(wordChunks);
                    continue;
                }

                // 普通单词：超阈值则保存当前块并保留重叠
                if (currentTokenCount + wordTokenCount > chunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join(" ", currentChunk).Trim());
                    var overlapWords = GetOverlapWordsEnhanced(currentChunk, overlapSize, useWordBasedToken);
                    currentChunk.Clear();
                    currentChunk.AddRange(overlapWords);
                    currentTokenCount = useWordBasedToken
                        ? TokenCounter.EstimateTokenCount(string.Join(" ", overlapWords), true)
                        : TokenCounter.EstimateTokenCount(string.Join(" ", overlapWords));
                }

                currentChunk.Add(word);
                currentTokenCount += wordTokenCount;
            }

            // 处理最后一个块
            if (currentChunk.Any())
            {
                chunks.Add(string.Join(" ", currentChunk).Trim());
            }

            return chunks;
        }

        /// <summary>
        /// 增强版重叠单词获取
        /// </summary>
        private static List<string> GetOverlapWordsEnhanced(List<string> currentChunk, int overlapSize, bool useWordBasedToken)
        {
            var reversed = currentChunk.AsEnumerable().Reverse().ToList();
            var overlapWords = new List<string>();
            int currentTokenCount = 0;

            foreach (var word in reversed)
            {
                var tokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(word, true)
                    : TokenCounter.EstimateTokenCount(word);

                if (currentTokenCount + tokenCount > overlapSize)
                    break;

                overlapWords.Add(word);
                currentTokenCount += tokenCount;
            }

            return overlapWords.AsEnumerable().Reverse().ToList();
        }

        /// <summary>
        /// 增强版超长单词拆分（更合理的字符拆分）
        /// </summary>
        private static List<string> SplitLongWordEnhanced(string word, int chunkSize, bool useWordBasedToken)
        {
            var chunks = new List<string>();
            int startIndex = 0;

            while (startIndex < word.Length)
            {
                // 初始按字符数拆分（英文单词按chunkSize/2拆分更合理）
                int charCount = Math.Min(chunkSize / 2, word.Length - startIndex);
                var chunk = word.Substring(startIndex, charCount);

                // 校验Token数
                var chunkTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(chunk, true)
                    : TokenCounter.EstimateTokenCount(chunk);

                // 仍超阈值则进一步缩小
                if (chunkTokenCount > chunkSize && charCount > 1)
                {
                    charCount = charCount / 2;
                    chunk = word.Substring(startIndex, charCount);
                }

                chunks.Add(chunk);
                // 保留少量重叠（避免单词完全切断）
                startIndex += charCount - Math.Min(5, charCount / 5);
            }

            return chunks;
        }
        #endregion

        #region 原有方法（适配修复版逻辑）
        private static List<string> SplitBySemanticBlocks(string text)
        {
            var normalizedText = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var paragraphs = normalizedText.Split('\n')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (paragraphs.Count == 1 && paragraphs[0].Length > Constants.LLMConstants.MAX_CHUNK_TOKEN * 2)
            {
                var largeBlock = paragraphs[0];
                paragraphs = SplitByEnhancedSentenceSeparators(largeBlock)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
            }

            return paragraphs;
        }

        private static List<string> SplitParagraphIntoSemanticChunks(
            string paragraph,
            int chunkSize,
            int overlapSize,
            bool useWordBasedToken)
        {
            return SplitBlockIntoSemanticChunks(paragraph, chunkSize, overlapSize, useWordBasedToken);
        }

        private static List<string> SplitLongSentence(
            string sentence,
            int chunkSize,
            int overlapSize,
            bool useWordBasedToken)
        {
            return SplitLongSentenceEnhanced(sentence, chunkSize, overlapSize, useWordBasedToken);
        }

        private static string GetOverlapText(List<string> currentChunk, int overlapSize, bool useWordBasedToken)
        {
            return GetEnhancedOverlapText(currentChunk, overlapSize, useWordBasedToken);
        }

        private static List<string> GetOverlapWords(List<string> currentChunk, int overlapSize, bool useWordBasedToken)
        {
            return GetOverlapWordsEnhanced(currentChunk, overlapSize, useWordBasedToken);
        }

        private static List<string> SplitLongWord(string word, int chunkSize, bool useWordBasedToken)
        {
            return SplitLongWordEnhanced(word, chunkSize, useWordBasedToken);
        }

        private static List<string> SplitBySentenceSeparators(string text, char[] separators)
        {
            return SplitByEnhancedSentenceSeparators(text);
        }
        #endregion
    }
}