using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.WebAPI.Attribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 智能问答接口（企业级API规范）
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Permission("chat.manage")]
public class ChatController : ControllerBase
{
    private readonly IChatUseCase _chatUseCase;

    public ChatController(IChatUseCase chatUseCase)
    {
        _chatUseCase = chatUseCase;
    }

    /// <summary>
    /// 智能问答
    /// </summary>
    /// <param name="request">问答请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>问答结果</returns>
    [HttpPost("ask")]
    [Permission("chat.ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        // 从 Token 自动获取当前登录用户 ID，禁止前端传入！
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(Result.Fail("用户未登录"));

        // 用 Token 里的真实用户ID，不相信前端传入的 request.UserId
        var (answer, references, costSeconds) = await _chatUseCase.ChatAsync(
            userId,
            request.Question,
            cancellationToken);

        var response = new ChatResponseDto
        {
            Answer = answer,
            References = references,
            CostSeconds = costSeconds
        };

        return Ok(Result<ChatResponseDto>.SuccessResult(response));
    }
}