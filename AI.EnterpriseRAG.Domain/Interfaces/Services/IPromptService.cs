namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// Prompt Service Interface
/// Centralized prompt management with configuration support
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Get RAG system prompt
    /// </summary>
    string GetRagSystemPrompt();

    /// <summary>
    /// Get RAG user prompt with context and optional history
    /// </summary>
    string GetRagUserPrompt(string context, string question, string? history = null);

    /// <summary>
    /// Get HyDE prompt for query rewriting
    /// </summary>
    string GetHydePrompt(string query, int maxLength = 500);

    /// <summary>
    /// Get Multi-Query prompt for generating similar queries
    /// </summary>
    string GetMultiQueryPrompt(string query, int count = 2);

    /// <summary>
    /// Get Self-Reflection validation prompt
    /// </summary>
    string GetSelfReflectionPrompt(string question, string answer, string sources);

    /// <summary>
    /// Get Agent ReAct system prompt
    /// </summary>
    string GetAgentReActPrompt(string tools);

    /// <summary>
    /// Get Intent Recognition prompt
    /// </summary>
    string GetIntentRecognitionPrompt(string userInput);

    /// <summary>
    /// Get session title generation prompt
    /// </summary>
    string GetSessionTitlePrompt(string firstQuestion, int maxLength = 50);

    /// <summary>
    /// Get custom prompt by category and key
    /// </summary>
    string GetCustomPrompt(string category, string key, Dictionary<string, object>? variables = null);
}
