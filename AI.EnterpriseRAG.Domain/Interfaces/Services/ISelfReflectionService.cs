using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Services;

/// <summary>
/// Self-reflection service for answer validation
/// </summary>
public interface ISelfReflectionService
{
    /// <summary>
    /// Validate answer against source documents
    /// </summary>
    Task<ReflectionResult> ValidateAnswerAsync(
        string question,
        string answer,
        List<DocumentChunk> sources,
        CancellationToken ct = default);
    
    /// <summary>
    /// Self-correct answer if confidence is low
    /// </summary>
    Task<string> SelfCorrectAsync(
        string question,
        string initialAnswer,
        List<DocumentChunk> sources,
        CancellationToken ct = default);
}

public class ReflectionResult
{
    public string IsSupported { get; set; } = "unknown";
    public string Contradictions { get; set; } = "none";
    public int Confidence { get; set; }
    public string Reasoning { get; set; } = "";
    public string? ImprovedAnswer { get; set; }
}
