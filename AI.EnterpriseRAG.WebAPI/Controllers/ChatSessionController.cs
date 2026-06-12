using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 会话管理接口
/// </summary>
[ApiController]
[Route("api/chat/sessions")]
[Produces("application/json")]
[Authorize]
public class ChatSessionController : ControllerBase
{
    private readonly AppEnterpriseAiContext _context;
    private readonly ILogger<ChatSessionController> _logger;

    public ChatSessionController(
        AppEnterpriseAiContext context,
        ILogger<ChatSessionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户会话列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] int limit = 20)
    {
        // 从Token获取用户ID（更安全）
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(Result.Fail("用户未登录"));

        var sessions = await _context.ConversationSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastInteractionAt)
            .Take(limit)
            .Select(s => new
            {
                s.Id,
                s.UserId,
                s.Title,
                s.CreatedAt,
                s.LastInteractionAt,
                s.IsActive,
                MessageCount = _context.ConversationMessages
                    .Count(m => m.SessionId == s.Id)
            })
            .ToListAsync();

        return Ok(Result<object>.SuccessResult(sessions));
    }

    /// <summary>
    /// 获取会话详情（包含消息历史）
    /// </summary>
    [HttpGet("{sessionId}/messages")]
    public async Task<IActionResult> GetSessionMessages(Guid sessionId)
    {
        var session = await _context.ConversationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound(Result.Fail(MessageResources.Chat.SessionNotFound));

        var messages = await _context.ConversationMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.SessionId,
                m.Role,
                Message = m.Content,
                Timestamp = m.CreatedAt
            })
            .ToListAsync();

        return Ok(Result<object>.SuccessResult(new
        {
            session = new
            {
                session.Id,
                session.UserId,
                session.Title,
                session.CreatedAt,
                session.LastInteractionAt
            },
            messages
        }));
    }

    /// <summary>
    /// 更新会话标题
    /// </summary>
    [HttpPatch("{sessionId}")]
    public async Task<IActionResult> UpdateSessionTitle(
        Guid sessionId,
        [FromBody] UpdateSessionTitleDto request)
    {
        var session = await _context.ConversationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound(Result.Fail(MessageResources.Chat.SessionNotFound));

        session.Title = request.Title;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated session title: {SessionId}", sessionId);

        return Ok(Result.Success(MessageResources.Chat.TitleUpdated));
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        var session = await _context.ConversationSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound(Result.Fail(MessageResources.Chat.SessionNotFound));

        // Soft delete
        session.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted session: {SessionId}", sessionId);

        return Ok(Result.Success(MessageResources.Chat.SessionDeleted));
    }

    /// <summary>
    /// 创建新会话
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto request)
    {
        // 从Token获取用户ID（更安全）
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(Result.Fail("用户未登录"));

        var session = new ConversationSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title ?? "新会话",
            CreatedAt = DateTime.Now,
            LastInteractionAt = DateTime.Now,
            IsActive = true
        };

        _context.ConversationSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created session: {SessionId}", session.Id);

        return Ok(Result<object>.SuccessResult(new
        {
            Id = session.Id.ToString(),
            session.UserId,
            session.Title,
            session.CreatedAt
        }));
    }
}

public class UpdateSessionTitleDto
{
    public string Title { get; set; } = string.Empty;
}

public class CreateSessionDto
{
    public string? Title { get; set; }
}
