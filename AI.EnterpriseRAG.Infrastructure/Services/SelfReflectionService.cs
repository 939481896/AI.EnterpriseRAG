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
/// </summary>
public class SelfReflectionService : ISelfReflectionService
{
    private readonly ILlmService _llm;
    private readonly ILogger<SelfReflectionService> _logger;
    private readonly SelfReflectionOptions _options;

    public SelfReflectionService(
        ILlmService llm,
        ILogger<SelfReflectionService> logger,
        IOptions<SelfReflectionOptions> options)
    {
        _llm = llm;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<ReflectionResult> ValidateAnswerAsync(
        string question,
        string answer,
        List<DocumentChunk> sources,
        CancellationToken ct)
    {
        var prompt = $@"Evaluate this answer for accuracy and source support.

Question: {question}

Generated Answer: {answer}

Source Documents:
{string.Join("\n\n", sources.Select((s, i) => $"[{i+1}] {s.Content}"))}

Evaluate:
1. Is the answer supported by the sources? (yes/no/partial)
2. Are there any contradictions or inconsistencies?
3. What is the confidence score? (0-100)
4. If confidence < 70, provide an improved answer.

Respond ONLY with valid JSON:
{{
  ""isSupported"": ""yes/no/partial"",
  ""contradictions"": ""any issues found or 'none'"",
  ""confidence"": 85,
  ""reasoning"": ""why this confidence score"",
  ""improvedAnswer"": ""better answer if needed or null""
}}";

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
