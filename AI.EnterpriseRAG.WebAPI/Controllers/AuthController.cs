using AI.EnterpriseRAG.Application.Authorization;
using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using Microsoft.AspNetCore.Mvc;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginRequest request)
    {
        // 参数验证
        if (!ModelState.IsValid)
            return BadRequest(Result.Fail(MessageResources.Common.ParameterError));

        var response = await _authService.LoginAsync(request);
        return Ok(Result<LoginResponse>.SuccessResult(response));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<Result<TokenResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Fail(MessageResources.Validation.Required("刷新令牌")));

        var response = await _authService.RefreshAccessTokenAsync(request.RefreshToken);
        return Ok(Result<TokenResponse>.SuccessResult(response));
    }
}