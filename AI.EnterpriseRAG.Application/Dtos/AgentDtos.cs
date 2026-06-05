namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// Agent execution request DTO
/// </summary>
public class AgentExecuteRequestDto
{
    /// <summary>
    /// User input
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Maximum iterations
    /// </summary>
    public int? MaxIterations { get; set; } = 10;
}

/// <summary>
/// Agent execution response DTO
/// </summary>
public class AgentExecuteResponseDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Final answer
    /// </summary>
    public string FinalAnswer { get; set; } = string.Empty;

    /// <summary>
    /// Execution steps
    /// </summary>
    public List<AgentStepDto> Steps { get; set; } = new();

    /// <summary>
    /// Total steps
    /// </summary>
    public int TotalSteps { get; set; }
}

/// <summary>
/// Agent step DTO
/// </summary>
public class AgentStepDto
{
    /// <summary>
    /// Step index
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// Event type
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Thought content
    /// </summary>
    public string? Thought { get; set; }

    /// <summary>
    /// Tool name
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Tool arguments
    /// </summary>
    public Dictionary<string, object>? ToolArguments { get; set; }

    /// <summary>
    /// Observation result
    /// </summary>
    public string? Observation { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Agent session response DTO
/// </summary>
public class AgentSessionResponseDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User intent
    /// </summary>
    public string Intent { get; set; } = string.Empty;

    /// <summary>
    /// Intent type
    /// </summary>
    public string IntentType { get; set; } = string.Empty;

    /// <summary>
    /// Session status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Final answer
    /// </summary>
    public string? FinalAnswer { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total cost in seconds
    /// </summary>
    public decimal? TotalCostSeconds { get; set; }

    /// <summary>
    /// Session steps
    /// </summary>
    public List<AgentSessionStepDto> Steps { get; set; } = new();
}

/// <summary>
/// Agent session step DTO
/// </summary>
public class AgentSessionStepDto
{
    /// <summary>
    /// Step index
    /// </summary>
    public int StepIndex { get; set; }

    /// <summary>
    /// Step type
    /// </summary>
    public string StepType { get; set; } = string.Empty;

    /// <summary>
    /// Tool name
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Tool arguments
    /// </summary>
    public string? ToolArguments { get; set; }

    /// <summary>
    /// Tool result
    /// </summary>
    public string? ToolResult { get; set; }

    /// <summary>
    /// Is success
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }
}
