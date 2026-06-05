using System.Collections.Generic;

namespace AI.EnterpriseRAG.Core.Prompts;

/// <summary>
/// Centralized Prompt Template Management
/// All LLM prompts should be defined here for easy maintenance and versioning
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// Prompt template registry (language -> category -> key -> template)
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _templates = new()
    {
        ["zh-CN"] = new Dictionary<string, Dictionary<string, string>>
        {
            // ============================================
            // RAG Core Prompts
            // ============================================
            ["rag"] = new Dictionary<string, string>
            {
                ["system"] = @"你是一个专业的企业级AI助手，基于提供的文档回答问题。
请遵循以下原则：
1. 仅基于参考文档回答，不要编造信息
2. 如果文档中没有相关信息，请诚实说明
3. 使用 [数字] 标注引用来源
4. 保持回答简洁专业",

                ["user_with_context"] = @"### 参考文档
{context}

### 当前问题
{question}

请基于上述参考文档回答问题，并使用 [1][2] 等标注引用来源。",

                ["user_with_context_and_history"] = @"### 对话历史
{history}

### 参考文档
{context}

### 当前问题
{question}

请基于对话历史和参考文档回答问题，并使用 [1][2] 等标注引用来源。"
            },

            // ============================================
            // HyDE (Query Rewriting) Prompts
            // ============================================
            ["hyde"] = new Dictionary<string, string>
            {
                ["generate_hypothetical_doc"] = @"请为以下问题生成一个详细的、信息丰富的假想回答（2-3段）：

问题：{query}

生成一个假想的专业回答：",

                ["generate_hypothetical_doc_detailed"] = @"你是一个专业的内容生成助手。请为以下问题生成一个假想的、高质量的回答文档。

问题：{query}

要求：
1. 回答应该详细且信息丰富（2-3段）
2. 使用专业术语
3. 结构清晰，逻辑严密
4. 长度约 {max_length} 字符

假想回答："
            },

            // ============================================
            // Multi-Query Prompts
            // ============================================
            ["multi_query"] = new Dictionary<string, string>
            {
                ["generate_similar_queries"] = @"请为以下问题生成 {count} 个意思相同但表达不同的问法：

原始问题：{query}

要求：
1. 保持原意不变
2. 使用不同的表达方式
3. 每个问题一行

相似问题：
1.",

                ["decompose_complex_query"] = @"请将以下复杂问题分解为 {count} 个更简单的子问题：

复杂问题：{query}

要求：
1. 子问题应该独立可回答
2. 所有子问题的答案组合起来能回答原问题
3. 每个问题一行

子问题：
1."
            },

            // ============================================
            // Self-Reflection Prompts
            // ============================================
            ["self_reflection"] = new Dictionary<string, string>
            {
                ["validate_answer"] = @"请评估以下答案的准确性和可信度。

问题：{question}

生成的答案：{answer}

参考文档：
{sources}

请评估：
1. 答案是否有文档支持？（yes/no/partial）
2. 是否存在矛盾或不一致？
3. 置信度评分？（0-100）
4. 如果置信度 < 70，请提供改进后的答案

请仅返回有效的 JSON 格式：
{{
  ""isSupported"": ""yes/no/partial"",
  ""contradictions"": ""发现的问题或'无'"",
  ""confidence"": 85,
  ""reasoning"": ""置信度评分的理由"",
  ""improvedAnswer"": ""改进后的答案或null""
}}",

                ["validate_answer_simple"] = @"请评估这个答案是否准确（基于提供的文档）：

问题：{question}
答案：{answer}

文档：
{sources}

评估结果（JSON）：
{{
  ""confidence"": 85,
  ""isSupported"": ""yes"",
  ""improvedAnswer"": null
}}"
            },

            // ============================================
            // Agent ReAct Prompts
            // ============================================
            ["agent"] = new Dictionary<string, string>
            {
                ["react_system"] = @"你是一个智能 AI Agent，可以使用工具来完成任务。

可用工具：
{tools}

请使用以下格式思考和行动：

Thought: 我需要做什么
Action: 工具名称
Arguments: {{""参数名"": ""参数值""}}
Observation: [工具执行结果]
... (重复 Thought/Action/Observation 直到找到答案)
Thought: 我现在知道最终答案了
Final Answer: [最终答案]

开始！",

                ["react_user"] = @"用户问题：{user_input}

请开始思考并使用工具：",

                ["intent_recognition"] = @"请识别用户意图并分类：

用户输入：{user_input}

可能的意图类型：
- question: 问答查询（需要从知识库检索）
- data_query: 数据查询（需要查询数据库）
- analysis: 分析任务（需要日志分析等）
- action: 执行操作（需要发送邮件、创建工单等）
- chat: 闲聊（无需工具）

请返回 JSON：
{{
  ""type"": ""question"",
  ""confidence"": 0.95,
  ""reasoning"": ""用户在询问知识库中的信息""
}}"
            },

            // ============================================
            // Memory & Context Prompts
            // ============================================
            ["memory"] = new Dictionary<string, string>
            {
                ["generate_session_title"] = @"请为以下对话生成一个简短的标题（不超过 {max_length} 字）：

第一个问题：{first_question}

标题：",

                ["summarize_history"] = @"请总结以下对话的关键信息：

{history}

总结（不超过 200 字）："
            }
        },

        // ============================================
        // English Templates
        // ============================================
        ["en-US"] = new Dictionary<string, Dictionary<string, string>>
        {
            ["rag"] = new Dictionary<string, string>
            {
                ["system"] = @"You are a professional enterprise AI assistant that answers questions based on provided documents.
Please follow these principles:
1. Only answer based on reference documents, do not fabricate information
2. If the document does not contain relevant information, honestly state so
3. Use [number] to cite sources
4. Keep answers concise and professional",

                ["user_with_context"] = @"### Reference Documents
{context}

### Current Question
{question}

Please answer the question based on the above reference documents and use [1][2] to cite sources."
            },

            ["hyde"] = new Dictionary<string, string>
            {
                ["generate_hypothetical_doc"] = @"Generate a detailed, informative hypothetical answer (2-3 paragraphs) for the following question:

Question: {query}

Write a comprehensive hypothetical answer:"
            },

            ["self_reflection"] = new Dictionary<string, string>
            {
                ["validate_answer"] = @"Evaluate the accuracy and reliability of the following answer.

Question: {question}

Generated Answer: {answer}

Reference Documents:
{sources}

Evaluate:
1. Is the answer supported by documents? (yes/no/partial)
2. Are there any contradictions?
3. Confidence score? (0-100)
4. If confidence < 70, provide an improved answer

Return only valid JSON:
{{
  ""isSupported"": ""yes/no/partial"",
  ""contradictions"": ""issues found or 'none'"",
  ""confidence"": 85,
  ""reasoning"": ""reason for confidence score"",
  ""improvedAnswer"": ""improved answer or null""
}}"
            }
        }
    };

    /// <summary>
    /// Get prompt template with variable replacement
    /// </summary>
    /// <param name="category">Category (e.g., "rag", "hyde", "agent")</param>
    /// <param name="key">Template key</param>
    /// <param name="variables">Variables to replace in template</param>
    /// <param name="language">Language code (default: zh-CN)</param>
    /// <returns>Formatted prompt</returns>
    public static string GetPrompt(
        string category,
        string key,
        Dictionary<string, object>? variables = null,
        string language = "zh-CN")
    {
        if (!_templates.TryGetValue(language, out var categoryDict))
        {
            language = "zh-CN"; // Fallback to Chinese
            categoryDict = _templates[language];
        }

        if (!categoryDict.TryGetValue(category, out var keyDict))
            throw new KeyNotFoundException($"Prompt category '{category}' not found");

        if (!keyDict.TryGetValue(key, out var template))
            throw new KeyNotFoundException($"Prompt key '{key}' not found in category '{category}'");

        // Replace variables
        if (variables != null)
        {
            foreach (var (varName, varValue) in variables)
            {
                template = template.Replace($"{{{varName}}}", varValue?.ToString() ?? string.Empty);
            }
        }

        return template;
    }

    /// <summary>
    /// Get RAG system prompt
    /// </summary>
    public static string GetRagSystemPrompt(string language = "zh-CN")
        => GetPrompt("rag", "system", language: language);

    /// <summary>
    /// Get RAG user prompt with context
    /// </summary>
    public static string GetRagUserPrompt(string context, string question, string? history = null, string language = "zh-CN")
    {
        var key = string.IsNullOrEmpty(history) ? "user_with_context" : "user_with_context_and_history";
        var variables = new Dictionary<string, object>
        {
            ["context"] = context,
            ["question"] = question
        };

        if (!string.IsNullOrEmpty(history))
        {
            variables["history"] = history;
        }

        return GetPrompt("rag", key, variables, language);
    }

    /// <summary>
    /// Get HyDE prompt for generating hypothetical document
    /// </summary>
    public static string GetHydePrompt(string query, int maxLength = 500, string language = "zh-CN")
        => GetPrompt("hyde", "generate_hypothetical_doc_detailed", new Dictionary<string, object>
        {
            ["query"] = query,
            ["max_length"] = maxLength
        }, language);

    /// <summary>
    /// Get Multi-Query prompt for generating similar queries
    /// </summary>
    public static string GetMultiQueryPrompt(string query, int count = 2, string language = "zh-CN")
        => GetPrompt("multi_query", "generate_similar_queries", new Dictionary<string, object>
        {
            ["query"] = query,
            ["count"] = count
        }, language);

    /// <summary>
    /// Get Self-Reflection validation prompt
    /// </summary>
    public static string GetSelfReflectionPrompt(
        string question,
        string answer,
        string sources,
        string language = "zh-CN")
        => GetPrompt("self_reflection", "validate_answer", new Dictionary<string, object>
        {
            ["question"] = question,
            ["answer"] = answer,
            ["sources"] = sources
        }, language);

    /// <summary>
    /// Get Agent ReAct system prompt
    /// </summary>
    public static string GetAgentReActPrompt(string tools, string language = "zh-CN")
        => GetPrompt("agent", "react_system", new Dictionary<string, object>
        {
            ["tools"] = tools
        }, language);

    /// <summary>
    /// Get Intent Recognition prompt
    /// </summary>
    public static string GetIntentRecognitionPrompt(string userInput, string language = "zh-CN")
        => GetPrompt("agent", "intent_recognition", new Dictionary<string, object>
        {
            ["user_input"] = userInput
        }, language);

    /// <summary>
    /// Get session title generation prompt
    /// </summary>
    public static string GetSessionTitlePrompt(string firstQuestion, int maxLength = 50, string language = "zh-CN")
        => GetPrompt("memory", "generate_session_title", new Dictionary<string, object>
        {
            ["first_question"] = firstQuestion,
            ["max_length"] = maxLength
        }, language);
}
