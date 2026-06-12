namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// 对话历史DTO
/// </summary>
public class ChatConversationDto
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public string UserId { get; set; } = string.Empty;
}
