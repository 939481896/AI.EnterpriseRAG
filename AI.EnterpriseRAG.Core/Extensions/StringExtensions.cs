using AI.EnterpriseRAG.Core.Constants;
using AI.EnterpriseRAG.Core.Utils; // 引入TokenCounter

namespace AI.EnterpriseRAG.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 企业级语义文本分块（兼容基础版/进阶版Token计数）
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
            bool useWordBasedToken = false) // 新增：进阶版开关
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            // 1. 重叠窗口配置（通用逻辑）
            overlapSize = overlapSize <= 0 ? Math.Max(20, chunkSize / 10) : overlapSize;
            overlapSize = Math.Min(overlapSize, chunkSize / 2);

            var chunks = new List<string>();
            var paragraphs = SplitBySemanticBlocks(text);

            // 2. 遍历段落分块（核心逻辑）
            foreach (var paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;

                // 关键：根据useWordBasedToken选择Token计数方式
                var paragraphTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(paragraph, true) // 进阶版
                    : TokenCounter.EstimateTokenCount(paragraph);      // 基础版

                if (paragraphTokenCount <= chunkSize)
                {
                    chunks.Add(paragraph.Trim());
                    continue;
                }

                var sentenceChunks = SplitParagraphIntoSemanticChunks(paragraph, chunkSize, overlapSize, useWordBasedToken);
                chunks.AddRange(sentenceChunks);
            }

            return chunks;
        }

        /// <summary>
        /// 拆分段落为语义分块（适配Token计数方式）
        /// </summary>
        private static List<string> SplitParagraphIntoSemanticChunks(
            string paragraph,
            int chunkSize,
            int overlapSize,
            bool useWordBasedToken)
        {
            var chunks = new List<string>();
            var sentences = SplitBySentenceSeparators(paragraph, new[] { '。', '！', '？', '；', '.', '!', '?', ';' });

            var currentChunk = new List<string>();
            int currentTokenCount = 0;

            foreach (var sentence in sentences)
            {
                // 关键：适配Token计数方式
                var sentenceTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(sentence, true)
                    : TokenCounter.EstimateTokenCount(sentence);

                // 处理超长句子
                if (sentenceTokenCount > chunkSize)
                {
                    if (currentChunk.Any())
                    {
                        chunks.Add(string.Join("", currentChunk).Trim());
                        var overlapText = GetOverlapText(currentChunk, overlapSize, useWordBasedToken);
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

                    var longSentenceChunks = SplitLongSentence(sentence, chunkSize, overlapSize, useWordBasedToken);
                    chunks.AddRange(longSentenceChunks);
                    continue;
                }

                // 处理普通句子
                if (currentTokenCount + sentenceTokenCount > chunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join("", currentChunk).Trim());
                    var overlapText = GetOverlapText(currentChunk, overlapSize, useWordBasedToken);
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

            if (currentChunk.Any())
            {
                chunks.Add(string.Join("", currentChunk).Trim());
            }

            return chunks;
        }

        /// <summary>
        /// 拆分超长句子（适配Token计数方式）
        /// </summary>
        private static List<string> SplitLongSentence(
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

                if (wordTokenCount > chunkSize)
                {
                    if (currentChunk.Any())
                    {
                        chunks.Add(string.Join(" ", currentChunk).Trim());
                        currentChunk.Clear();
                        currentTokenCount = 0;
                    }

                    var wordChunks = SplitLongWord(word, chunkSize, useWordBasedToken);
                    chunks.AddRange(wordChunks);
                    continue;
                }

                if (currentTokenCount + wordTokenCount > chunkSize && currentChunk.Any())
                {
                    chunks.Add(string.Join(" ", currentChunk).Trim());
                    var overlapWords = GetOverlapWords(currentChunk, overlapSize, useWordBasedToken);
                    currentChunk.Clear();
                    currentChunk.AddRange(overlapWords);
                    currentTokenCount = useWordBasedToken
                        ? TokenCounter.EstimateTokenCount(string.Join(" ", overlapWords), true)
                        : TokenCounter.EstimateTokenCount(string.Join(" ", overlapWords));
                }

                currentChunk.Add(word);
                currentTokenCount += wordTokenCount;
            }

            if (currentChunk.Any())
            {
                chunks.Add(string.Join(" ", currentChunk).Trim());
            }

            return chunks;
        }

        /// <summary>
        /// 获取重叠文本（适配Token计数方式）
        /// </summary>
        private static string GetOverlapText(List<string> currentChunk, int overlapSize, bool useWordBasedToken)
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

            return string.Join("", overlapParts.AsEnumerable().Reverse()).Trim();
        }

        /// <summary>
        /// 获取重叠单词（适配Token计数方式）
        /// </summary>
        private static List<string> GetOverlapWords(List<string> currentChunk, int overlapSize, bool useWordBasedToken)
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
        /// 拆分超长单词（适配Token计数方式）
        /// </summary>
        private static List<string> SplitLongWord(string word, int chunkSize, bool useWordBasedToken)
        {
            var chunks = new List<string>();
            int startIndex = 0;

            while (startIndex < word.Length)
            {
                int charCount = Math.Min(chunkSize, word.Length - startIndex);
                var chunk = word.Substring(startIndex, charCount);

                // 校验Token数（适配计数方式）
                var chunkTokenCount = useWordBasedToken
                    ? TokenCounter.EstimateTokenCount(chunk, true)
                    : TokenCounter.EstimateTokenCount(chunk);

                // 如果拆分后的chunk仍超阈值，进一步缩小
                if (chunkTokenCount > chunkSize && charCount > 1)
                {
                    charCount = charCount / 2;
                    chunk = word.Substring(startIndex, charCount);
                }

                chunks.Add(chunk);
                startIndex += charCount - Math.Min(10, charCount / 5);
            }

            return chunks;
        }

        // 以下是原有辅助方法（无修改）
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
                paragraphs = SplitBySentenceSeparators(largeBlock, new[] { '。', '！', '？', '；' })
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
            }

            return paragraphs;
        }

        private static List<string> SplitBySentenceSeparators(string text, char[] separators)
        {
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

            if (startIndex < text.Length)
            {
                var lastSentence = text.Substring(startIndex).Trim();
                if (!string.IsNullOrWhiteSpace(lastSentence))
                    sentences.Add(lastSentence);
            }

            return sentences;
        }
    }
}