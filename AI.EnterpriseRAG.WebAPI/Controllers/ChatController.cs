using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 智能问答接口（企业级API规范）
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    [ProducesResponseType(typeof(Result<ChatResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        var (answer, references, costSeconds) = await _chatUseCase.ChatAsync(
            request.UserId,
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