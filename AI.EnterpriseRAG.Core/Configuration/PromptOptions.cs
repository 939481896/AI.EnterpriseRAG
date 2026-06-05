using System.ComponentModel.DataAnnotations;

namespace AI.EnterpriseRAG.Core.Configuration;

/// <summary>
/// Prompt configuration options (allows overriding via appsettings.json)
/// </summary>
public class PromptOptions
{
    public const string SectionName = "Prompts";

    /// <summary>
    /// Default language for prompts (zh-CN or en-US)
    /// </summary>
    [Required]
    public string DefaultLanguage { get; set; } = "zh-CN";

    /// <summary>
    /// RAG prompt configuration
    /// </summary>
    public RagPromptOptions Rag { get; set; } = new();

    /// <summary>
    /// HyDE prompt configuration
    /// </summary>
    public HydePromptOptions Hyde { get; set; } = new();

    /// <summary>
    /// Self-Reflection prompt configuration
    /// </summary>
    public SelfReflectionPromptOptions SelfReflection { get; set; } = new();

    /// <summary>
    /// Agent prompt configuration
    /// </summary>
    public AgentPromptOptions Agent { get; set; } = new();
}

public class RagPromptOptions
{
    /// <summary>
    /// Custom system prompt (overrides default if set)
    /// </summary>
    public string? SystemPromptOverride { get; set; }

    /// <summary>
    /// Whether to include conversation history in prompt
    /// </summary>
    public bool IncludeHistory { get; set; } = true;

    /// <summary>
    /// Maximum context length in prompt (characters)
    /// </summary>
    [Range(500, 20000)]
    public int MaxContextLength { get; set; } = 5000;
}

public class HydePromptOptions
{
    /// <summary>
    /// Custom HyDE prompt template (overrides default if set)
    /// Available placeholders: {query}, {max_length}
    /// </summary>
    public string? PromptTemplateOverride { get; set; }

    /// <summary>
    /// Whether to add detailed instructions to HyDE prompt
    /// </summary>
    public bool UseDetailedInstructions { get; set; } = true;
}

public class SelfReflectionPromptOptions
{
    /// <summary>
    /// Custom validation prompt (overrides default if set)
    /// Available placeholders: {question}, {answer}, {sources}
    /// </summary>
    public string? PromptTemplateOverride { get; set; }

    /// <summary>
    /// Whether to request improved answer when confidence is low
    /// </summary>
    public bool RequestImprovement { get; set; } = true;
}

public class AgentPromptOptions
{
    /// <summary>
    /// Custom ReAct system prompt (overrides default if set)
    /// Available placeholders: {tools}
    /// </summary>
    public string? ReActPromptOverride { get; set; }

    /// <summary>
    /// Whether to include examples in Agent prompts
    /// </summary>
    public bool IncludeExamples { get; set; } = false;
}
