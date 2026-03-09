using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Core.Constants
{
    public static class LLMConstants
    {
        #region 1. Prompt 相关（兼容原有模板+新增增强版）
        /// <summary>
        /// 原有基础版RAG Prompt模板（兼容历史代码）
        /// </summary>
        public const string RAG_PROMPT_TEMPLATE = @"
你是企业级智能问答助手，严格遵守以下规则：
1. 仅基于提供的上下文回答问题，禁止使用外部知识；
2. 若上下文无相关信息，直接返回""未查询到相关答案""，禁止编造内容；
3. 回答简洁明了，只针对问题本身，不添加无关解释；
4. 输出格式为纯文本，禁止使用Markdown等格式。

上下文：
{0}

问题：{1}

回答：";

        /// <summary>
        /// 增强版企业级Prompt模板（用于ChatUseCase优化后）
        /// </summary>
        public const string RAG_PROMPT_ENHANCED_TEMPLATE = @"### 角色
你是一个严谨的企业级智能问答助手，你的核心职责是基于提供的上下文信息回答用户问题。

### 规则
1. 必须严格基于以下上下文回答，禁止使用任何外部知识；
2. 如果上下文没有相关信息，直接回复：""未查询到与该问题相关的有效信息""，不要编造内容；
3. 回答要简洁、准确、有条理，优先使用上下文中原话，必要时可适当总结；
4. 避免使用专业术语堆砌，确保普通用户能理解。

### 上下文
{0}

### 问题
{1}

### 回答
";

        /// <summary>
        /// 空上下文兜底Prompt模板
        /// </summary>
        public const string RAG_PROMPT_EMPTY_CONTEXT_TEMPLATE = @"{0}";

        /// <summary>
        /// 空上下文默认回复语
        /// </summary>
        public const string EMPTY_CONTEXT_DEFAULT_ANSWER = "未查询到与该问题相关的有效信息";
        #endregion

        #region 2. Token 相关（保留原有+补充优化所需）
        /// <summary>
        /// 单分块最大Token数（用于文本分块）
        /// </summary>
        public const int MAX_CHUNK_TOKEN = 500;

        /// <summary>
        /// Prompt最大Token数（用于ChatUseCase校验）
        /// </summary>
        public const int MAX_PROMPT_TOKEN = 2000;

        /// <summary>
        /// 上下文最大Token数（Prompt中上下文的单独限制）
        /// </summary>
        public const int MAX_CONTEXT_TOKEN = 1500; // 小于MAX_PROMPT_TOKEN，预留问题+指令的Token空间

        /// <summary>
        /// 分块重叠窗口占比（10%）
        /// </summary>
        public const float CHUNK_OVERLAP_RATIO = 0.1f;

        /// <summary>
        /// 最小重叠Token数（兜底，避免重叠为0）
        /// </summary>
        public const int MIN_CHUNK_OVERLAP_TOKEN = 20;
        #endregion

        #region 3. 向量检索相关（保留原有+补充优化所需）
        /// <summary>
        /// 向量检索默认TopK数量
        /// </summary>
        public const int DEFAULT_SEARCH_TOP_K = 3;

        /// <summary>
        /// 向量检索最小相似度阈值（过滤低质量匹配）
        /// </summary>
        public const float MIN_SEARCH_SIMILARITY = 0.7f;

        /// <summary>
        /// 向量维度（根据使用的Embedding模型调整，比如nomic-embed-text是768维）
        /// </summary>
        public const int VECTOR_DIMENSION = 768;
        #endregion

        #region 4. 通用错误提示（统一规范）
        /// <summary>
        /// 向量生成失败提示语
        /// </summary>
        public const string VECTOR_GENERATE_FAILED_MSG = "向量生成失败，无法进行语义检索";

        /// <summary>
        /// Prompt超长提示语
        /// </summary>
        public const string PROMPT_EXCEED_TOKEN_MSG = "问题+上下文过长，请简化问题或拆分查询";
        #endregion
    }
}