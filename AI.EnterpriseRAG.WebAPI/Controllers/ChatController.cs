using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.WebAPI.Attribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 智能问答接口（企业级API规范）
/// </summary>
[Route("api/[controller]")]
//[Permission("chat.manage")]
public class ChatController : BaseApiController
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
    //[Permission("chat.ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            request.UserId = user.UserId;
            // 用 Token 里的真实用户ID，不相信前端传入的 request.UserId
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
        });
    }

    /// <summary>
    /// V1.0 智能问答 (HyDE + Multi-Query + Self-Reflection)
    /// </summary>
    /// <param name="request">问答请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>问答结果</returns>
    [HttpPost("ask-v1")]
    //[Permission("chat.ask")]
    public async Task<IActionResult> AskV1([FromBody] ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            request.UserId = user.UserId;
            // 用 Token 里的真实用户ID，不相信前端传入的 request.UserId
            var (answer, references, costSeconds) = await _chatUseCase.ChatV1Async(
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
        });
    }

    /// <summary>
    /// 获取对话历史
    /// </summary>
    /// <param name="pageSize">每页数量（默认20）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>对话历史列表</returns>
    [HttpGet("history")]
    [Authorize]
    //[Permission("chat.history")]
    [ProducesResponseType(typeof(Result<List<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = GetCurrentUserRequired();

            var conversations = await _chatUseCase.GetUserConversationsAsync(
                user.UserId,
                pageSize, 
                cancellationToken);

            return Ok(Result<List<object>>.SuccessResult(
                conversations, 
                MessageResources.Get("chat.history.retrieved", conversations.Count)));
        }
        catch (Exception ex)
        {
            return BadRequest(Result.Fail($"{MessageResources.Get("chat.history.failed")}：{ex.Message}"));
        }
    }

    /// <summary>
    /// 删除对话记录
    /// </summary>
    /// <param name="conversationId">对话ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("history/{conversationId}")]
    [Authorize]
    //[Permission("chat.delete")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = GetCurrentUserRequired();

            await _chatUseCase.DeleteConversationAsync(conversationId, cancellationToken);

            return Ok(Result.SuccessResult(MessageResources.Chat.SessionDeleted));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(Result.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(Result.Fail($"{MessageResources.Get("chat.delete.failed")}：{ex.Message}"));
        }
    }

    
}
