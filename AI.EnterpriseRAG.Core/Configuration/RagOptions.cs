using System.ComponentModel.DataAnnotations;

namespace AI.EnterpriseRAG.Core.Configuration;

/// <summary>
/// Core RAG pipeline configuration
/// </summary>
public class RagOptions
{
    public const string SectionName = "RAG";
    
    /// <summary>
    /// Minimum similarity threshold for chunk filtering (0.0-1.0)
    /// Chinese text typically uses lower threshold (~0.2)
    /// </summary>
    [Range(0.0, 1.0)]
    public float MinSimilarityThreshold { get; set; } = 0.2f;
    
    /// <summary>
    /// Maximum context tokens to send to LLM
    /// </summary>
    [Range(500, 32000)]
    public int MaxContextTokens { get; set; } = 3000;
    
    /// <summary>
    /// Maximum prompt tokens (context + question + instructions)
    /// </summary>
    [Range(500, 32000)]
    public int MaxPromptTokens { get; set; } = 4000;
    
    /// <summary>
    /// Number of chunks to retrieve from vector store
    /// </summary>
    [Range(1, 100)]
    public int RetrievalTopK { get; set; } = 20;
    
    /// <summary>
    /// Number of chunks after reranking
    /// </summary>
    [Range(1, 20)]
    public int RerankTopK { get; set; } = 5;
    
    /// <summary>
    /// Enable/disable reranking step
    /// </summary>
    public bool EnableRerank { get; set; } = true;
    
    /// <summary>
    /// Timeout for vector search operations (seconds)
    /// </summary>
    [Range(1, 300)]
    public int SearchTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// HyDE (Hypothetical Document Embeddings) configuration
/// </summary>
public class HydeOptions
{
    public const string SectionName = "RAG:HyDE";
    
    /// <summary>
    /// Enable/disable HyDE query rewriting
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Prompt template for generating hypothetical documents
    /// {0} = question
    /// </summary>
    public string PromptTemplate { get; set; } = @"Generate a detailed paragraph that would answer this question perfectly:

Question: {0}

Write a comprehensive answer (2-3 sentences):";
    
    /// <summary>
    /// Maximum length of generated hypothetical document
    /// </summary>
    [Range(50, 2000)]
    public int MaxLength { get; set; } = 500;
}

/// <summary>
/// Multi-Query generation configuration
/// </summary>
public class MultiQueryOptions
{
    public const string SectionName = "RAG:MultiQuery";
    
    /// <summary>
    /// Enable/disable multi-query fusion
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Number of similar queries to generate
    /// </summary>
    [Range(1, 5)]
    public int QueryCount { get; set; } = 2;
    
    /// <summary>
    /// Prompt template for generating similar queries
    /// {0} = original question, {1} = count
    /// </summary>
    public string PromptTemplate { get; set; } = @"Generate {1} similar questions that would help find the same information:

Original: {0}

List {1} variations (one per line):";
    
    /// <summary>
    /// RRF (Reciprocal Rank Fusion) k parameter
    /// Higher values = more weight to lower-ranked results
    /// </summary>
    [Range(10, 100)]
    public int RrfK { get; set; } = 60;
}

/// <summary>
/// Hybrid Search (Vector + BM25) configuration
/// </summary>
public class HybridSearchOptions
{
    public const string SectionName = "RAG:HybridSearch";
    
    /// <summary>
    /// Enable/disable hybrid search (falls back to vector-only if disabled)
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// BM25 k1 parameter (term saturation)
    /// Typical values: 1.2-2.0
    /// </summary>
    [Range(0.5, 3.0)]
    public double Bm25K1 { get; set; } = 1.5;
    
    /// <summary>
    /// BM25 b parameter (length normalization)
    /// Typical values: 0.5-0.9
    /// </summary>
    [Range(0.0, 1.0)]
    public double Bm25B { get; set; } = 0.75;
    
    /// <summary>
    /// Maximum candidates for BM25 scoring (performance limit)
    /// </summary>
    [Range(100, 5000)]
    public int Bm25MaxCandidates { get; set; } = 500;
    
    /// <summary>
    /// Minimum token length for BM25 matching
    /// </summary>
    [Range(1, 5)]
    public int MinTokenLength { get; set; } = 2;
    
    /// <summary>
    /// RRF k parameter for vector+BM25 fusion
    /// </summary>
    [Range(10, 100)]
    public int RrfK { get; set; } = 60;
}

/// <summary>
/// Conversation Memory configuration
/// </summary>
public class MemoryOptions
{
    public const string SectionName = "RAG:Memory";
    
    /// <summary>
    /// Enable/disable conversation memory
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Maximum messages to retrieve from history
    /// </summary>
    [Range(2, 50)]
    public int MaxHistoryMessages { get; set; } = 10;
    
    /// <summary>
    /// Maximum tokens for conversation history
    /// </summary>
    [Range(100, 4000)]
    public int MaxHistoryTokens { get; set; } = 1000;
    
    /// <summary>
    /// Estimated tokens per character (for Chinese text)
    /// </summary>
    [Range(2, 6)]
    public int EstimatedTokensPerChar { get; set; } = 4;
    
    /// <summary>
    /// Days of inactivity before archiving sessions
    /// </summary>
    [Range(1, 365)]
    public int SessionArchiveDays { get; set; } = 30;
    
    /// <summary>
    /// Auto-generate session titles from first question
    /// </summary>
    public bool AutoGenerateTitles { get; set; } = true;
    
    /// <summary>
    /// Maximum length of auto-generated titles
    /// </summary>
    [Range(20, 200)]
    public int MaxTitleLength { get; set; } = 50;
}

/// <summary>
/// Self-Reflection (answer validation) configuration
/// </summary>
public class SelfReflectionOptions
{
    public const string SectionName = "RAG:SelfReflection";
    
    /// <summary>
    /// Enable/disable self-reflection validation
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Minimum confidence score to accept answer (0-100)
    /// Below this threshold, answer will be corrected
    /// </summary>
    [Range(0, 100)]
    public int MinConfidenceThreshold { get; set; } = 70;
    
    /// <summary>
    /// Prompt template for answer validation
    /// {0} = question, {1} = answer, {2} = sources
    /// </summary>
    public string ValidationPromptTemplate { get; set; } = @"Evaluate this answer for accuracy and source support.

Question: {0}

Generated Answer: {1}

Source Documents:
{2}

Evaluate:
1. Is the answer supported by the sources? (yes/no/partial)
2. Are there any contradictions or inconsistencies?
3. What is the confidence score? (0-100)
4. If confidence < {3}, provide an improved answer.

Respond ONLY with valid JSON:
{{
  ""IsSupported"": ""yes/no/partial"",
  ""Contradictions"": ""any issues found or 'none'"",
  ""Confidence"": 85,
  ""Reasoning"": ""why this confidence score"",
  ""ImprovedAnswer"": ""better answer if needed or null""
}}";
    
    /// <summary>
    /// Maximum attempts to validate/correct answer
    /// </summary>
    [Range(1, 3)]
    public int MaxValidationAttempts { get; set; } = 1;
}

/// <summary>
/// Citation system configuration
/// </summary>
public class CitationOptions
{
    public const string SectionName = "RAG:Citation";
    
    /// <summary>
    /// Enable/disable citation numbers in answers
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Citation format: Bracket, Parenthesis, Superscript
    /// </summary>
    public string Format { get; set; } = "Bracket"; // [1], (1), ¹
    
    /// <summary>
    /// Require LLM to cite sources in answer
    /// </summary>
    public bool RequireCitations { get; set; } = true;
}
