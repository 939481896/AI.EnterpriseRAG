using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// Example controller demonstrating the use of BaseApiController helpers
/// This shows different patterns for extracting user context
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class ExampleController : BaseApiController
{
    private readonly IChatUseCase _chatUseCase;
    private readonly ILogger<ExampleController> _logger;

    public ExampleController(IChatUseCase chatUseCase, ILogger<ExampleController> logger)
    {
        _chatUseCase = chatUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Pattern 1: GetCurrentUserRequired() - For endpoints WITH [Authorize]
    /// ✅ RECOMMENDED: No redundant null checks
    /// [Authorize] already guarantees authentication
    /// </summary>
    [HttpGet("pattern1-with-authorize")]
    [Authorize] // ← Authentication guaranteed by middleware
    public async Task<IActionResult> Pattern1WithAuthorize()
    {
        // No null check needed! [Authorize] already validated authentication
        var user = GetCurrentUserRequired();

        _logger.LogInformation("User {UserId} from tenant {TenantId}", user.UserId, user.TenantId);

        return Ok(Result<object>.SuccessResult(new { 
            userId = user.UserId, 
            userName = user.UserName,
            tenantId = user.TenantId
        }));
    }

    /// <summary>
    /// Pattern 1b: GetCurrentUser() - For endpoints WITHOUT [Authorize]
    /// Use when authentication is optional
    /// </summary>
    [HttpGet("pattern1-no-authorize")]
    [AllowAnonymous] // ← Authentication is optional
    public async Task<IActionResult> Pattern1NoAuthorize()
    {
        var user = GetCurrentUser();

        // Check required when [Authorize] is NOT present
        if (user == null || !user.IsAuthenticated)
        {
            return Ok(Result<object>.SuccessResult(new { 
                message = "Anonymous user",
                authenticated = false
            }));
        }

        return Ok(Result<object>.SuccessResult(new { 
            message = $"Welcome {user.UserName}",
            authenticated = true,
            userId = user.UserId
        }));
    }

    /// <summary>
    /// Pattern 2: GetCurrentUserOrUnauthorized (for optional auth endpoints)
    /// Use ONLY when [Authorize] is NOT present
    /// </summary>
    [HttpGet("pattern2")]
    [AllowAnonymous]
    public async Task<IActionResult> Pattern2Example()
    {
        var userResult = GetCurrentUserOrUnauthorized();

        // Check if unauthorized (only needed without [Authorize])
        if (userResult.Result is UnauthorizedObjectResult unauthorizedResult)
        {
            return unauthorizedResult;
        }

        var user = userResult.Value!;

        _logger.LogInformation("User {UserId} authenticated", user.UserId);

        return Ok(Result<object>.SuccessResult(new { 
            userId = user.UserId,
            tenantId = user.TenantId 
        }));
    }

    /// <summary>
    /// Pattern 3: ExecuteWithUserRequiredAsync - For endpoints WITH [Authorize]
    /// ⭐ RECOMMENDED: Cleanest code when [Authorize] is present
    /// No redundant authentication checks
    /// </summary>
    [HttpPost("pattern3-with-authorize")]
    [Authorize] // ← Authentication guaranteed
    public async Task<IActionResult> Pattern3WithAuthorize([FromBody] ChatRequestDto request)
    {
        // No authentication check needed!
        return await ExecuteWithUserRequiredAsync(async (user) =>
        {
            _logger.LogInformation(
                "User {UserId} ({UserName}) from tenant {TenantId} is asking a question", 
                user.UserId, user.UserName, user.TenantId);

            request.UserId = user.UserId;

            var (answer, references, cost) = await _chatUseCase.ChatAsync(
                user.UserId, 
                request.Question);

            var response = new ChatResponseDto
            {
                Answer = answer,
                References = references,
                CostSeconds = cost
            };

            return Ok(Result<ChatResponseDto>.SuccessResult(response));
        });
    }

    /// <summary>
    /// Pattern 3b: ExecuteWithUserAsync - For endpoints WITHOUT [Authorize]
    /// Use when authentication is optional and you need to handle unauthorized
    /// </summary>
    [HttpPost("pattern3-no-authorize")]
    [AllowAnonymous]
    public async Task<IActionResult> Pattern3NoAuthorize([FromBody] ChatRequestDto request)
    {
        // Handles authentication check internally
        return await ExecuteWithUserAsync(async (user) =>
        {
            request.UserId = user.UserId;

            var (answer, references, cost) = await _chatUseCase.ChatAsync(
                user.UserId, 
                request.Question);

            return Ok(Result<ChatResponseDto>.SuccessResult(new ChatResponseDto
            {
                Answer = answer,
                References = references,
                CostSeconds = cost
            }));
        });
    }

    /// <summary>
    /// Pattern 4: ExecuteWithUserRequiredAsync with generic return type
    /// For endpoints WITH [Authorize]
    /// </summary>
    [HttpGet("pattern4")]
    [Authorize]
    public async Task<ActionResult<Result<UserInfoDto>>> Pattern4Example()
    {
        return await ExecuteWithUserRequiredAsync<Result<UserInfoDto>>(async (user) =>
        {
            var userInfo = new UserInfoDto
            {
                UserId = user.UserId,
                UserName = user.UserName,
                TenantId = user.TenantId
            };

            return Ok(Result<UserInfoDto>.SuccessResult(userInfo));
        });
    }
}

/// <summary>
/// Example DTO for demonstration
/// </summary>
public class UserInfoDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}
