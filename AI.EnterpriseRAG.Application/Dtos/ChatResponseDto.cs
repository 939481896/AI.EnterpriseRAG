namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// 问答响应DTO
/// </summary>
public class ChatResponseDto
{
    /// <summary>
    /// 回答内容
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// 参考上下文
    /// </summary>
    public List<string> References { get; set; } = new List<string>();

    /// <summary>
    /// 耗时（秒）
    /// </summary>
    public decimal CostSeconds { get; set; }
}