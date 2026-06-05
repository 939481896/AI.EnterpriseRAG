using AI.EnterpriseRAG.Core.Configuration;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services;

/// <summary>
/// Self-Reflection: Validate and improve answers
/// Prevents hallucinations and increases trustworthiness
/// ✅ Now uses centralized PromptService
/// </summary>
public class SelfReflectionService : ISelfReflectionService
{
    private readonly ILlmService _llm;
    private readonly IPromptService _promptService;
    private readonly ILogger<SelfReflectionService> _logger;
    private readonly SelfReflectionOptions _options;

    public SelfReflectionService(
        ILlmService llm,
        IPromptService promptService,
        ILogger<SelfReflectionService> logger,
        IOptions<SelfReflectionOptions> options)
    {
        _llm = llm;
        _promptService = promptService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ReflectionResult> ValidateAnswerAsync(
        string question,
        string answer,
        List<DocumentChunk> sources,
        CancellationToken ct)
    {
        // ✅ Use centralized prompt
        var sourcesText = string.Join("\n\n", sources.Select((s, i) => $"[{i+1}] {s.Content}"));
        var prompt = _promptService.GetSelfReflectionPrompt(question, answer, sourcesText);

        try
        {
            var response = await _llm.ChatAsync(prompt, ct);
            
            // Clean JSON (remove markdown if present)
            var jsonStr = response.Trim();
            if (jsonStr.StartsWith("```json"))
            {
                jsonStr = jsonStr.Replace("```json", "").Replace("```", "").Trim();
            }
            
            var result = JsonSerializer.Deserialize<ReflectionResult>(jsonStr, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _logger.LogInformation("Self-reflection: Confidence={Confidence}%, Supported={Supported}", 
                result?.Confidence ?? 0, result?.IsSupported ?? "unknown");

            return result ?? new ReflectionResult
            {
                IsSupported = "unknown",
                Confidence = 50,
                Reasoning = "Deserialization failed",
                ImprovedAnswer = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Self-reflection failed, using original answer");
            return new ReflectionResult
            {
                IsSupported = "unknown",
                Confidence = 50,
                Reasoning = "Validation failed",
                ImprovedAnswer = null
            };
        }
    }

    public async Task<string> SelfCorrectAsync(
        string question,
        string initialAnswer,
        List<DocumentChunk> sources,
        CancellationToken ct)
    {
        var reflection = await ValidateAnswerAsync(question, initialAnswer, sources, ct);

        if (reflection.Confidence < _options.MinConfidenceThreshold && !string.IsNullOrEmpty(reflection.ImprovedAnswer))
        {
            _logger.LogWarning("Low confidence ({Confidence}%), using improved answer", 
                reflection.Confidence);
            return reflection.ImprovedAnswer;
        }

        return initialAnswer;
    }
}
