using AI.EnterpriseRAG.Core.Configuration;
using AI.EnterpriseRAG.Core.Prompts;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AI.EnterpriseRAG.Infrastructure.Services;

/// <summary>
/// Prompt Service Implementation
/// Centralized prompt management with configuration override support
/// </summary>
public class PromptService : IPromptService
{
    private readonly PromptOptions _options;
    private readonly ILogger<PromptService> _logger;

    public PromptService(
        IOptions<PromptOptions> options,
        ILogger<PromptService> logger)
    {
        _options = options.Value;
        _logger = logger;

        _logger.LogInformation("✅ PromptService initialized | Language: {Language}", 
            _options.DefaultLanguage);
    }

    public string GetRagSystemPrompt()
    {
        // Use custom override if provided
        if (!string.IsNullOrEmpty(_options.Rag.SystemPromptOverride))
        {
            _logger.LogDebug("Using custom RAG system prompt override");
            return _options.Rag.SystemPromptOverride;
        }

        return PromptTemplates.GetRagSystemPrompt(_options.DefaultLanguage);
    }

    public string GetRagUserPrompt(string context, string question, string? history = null)
    {
        // Apply context length limit
        if (context.Length > _options.Rag.MaxContextLength)
        {
            _logger.LogDebug("Truncating context from {Original} to {Max} chars",
                context.Length, _options.Rag.MaxContextLength);
            context = context.Substring(0, _options.Rag.MaxContextLength) + "\n...(内容已截断)";
        }

        // Optionally exclude history
        if (!_options.Rag.IncludeHistory)
        {
            history = null;
        }

        return PromptTemplates.GetRagUserPrompt(context, question, history, _options.DefaultLanguage);
    }

    public string GetHydePrompt(string query, int maxLength = 500)
    {
        // Use custom override if provided
        if (!string.IsNullOrEmpty(_options.Hyde.PromptTemplateOverride))
        {
            _logger.LogDebug("Using custom HyDE prompt override");
            return _options.Hyde.PromptTemplateOverride
                .Replace("{query}", query)
                .Replace("{max_length}", maxLength.ToString());
        }

        // Use detailed or simple version based on config
        var key = _options.Hyde.UseDetailedInstructions 
            ? "generate_hypothetical_doc_detailed" 
            : "generate_hypothetical_doc";

        return PromptTemplates.GetPrompt("hyde", key, new Dictionary<string, object>
        {
            ["query"] = query,
            ["max_length"] = maxLength
        }, _options.DefaultLanguage);
    }

    public string GetMultiQueryPrompt(string query, int count = 2)
    {
        return PromptTemplates.GetMultiQueryPrompt(query, count, _options.DefaultLanguage);
    }

    public string GetSelfReflectionPrompt(string question, string answer, string sources)
    {
        // Use custom override if provided
        if (!string.IsNullOrEmpty(_options.SelfReflection.PromptTemplateOverride))
        {
            _logger.LogDebug("Using custom Self-Reflection prompt override");
            return _options.SelfReflection.PromptTemplateOverride
                .Replace("{question}", question)
                .Replace("{answer}", answer)
                .Replace("{sources}", sources);
        }

        // Use simple version if improvement not requested
        var key = _options.SelfReflection.RequestImprovement 
            ? "validate_answer" 
            : "validate_answer_simple";

        return PromptTemplates.GetPrompt("self_reflection", key, new Dictionary<string, object>
        {
            ["question"] = question,
            ["answer"] = answer,
            ["sources"] = sources
        }, _options.DefaultLanguage);
    }

    public string GetAgentReActPrompt(string tools)
    {
        // Use custom override if provided
        if (!string.IsNullOrEmpty(_options.Agent.ReActPromptOverride))
        {
            _logger.LogDebug("Using custom Agent ReAct prompt override");
            return _options.Agent.ReActPromptOverride.Replace("{tools}", tools);
        }

        return PromptTemplates.GetAgentReActPrompt(tools, _options.DefaultLanguage);
    }

    public string GetIntentRecognitionPrompt(string userInput)
    {
        return PromptTemplates.GetIntentRecognitionPrompt(userInput, _options.DefaultLanguage);
    }

    public string GetSessionTitlePrompt(string firstQuestion, int maxLength = 50)
    {
        return PromptTemplates.GetSessionTitlePrompt(firstQuestion, maxLength, _options.DefaultLanguage);
    }

    public string GetCustomPrompt(string category, string key, Dictionary<string, object>? variables = null)
    {
        return PromptTemplates.GetPrompt(category, key, variables, _options.DefaultLanguage);
    }
}
