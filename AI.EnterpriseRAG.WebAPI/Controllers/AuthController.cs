using AI.EnterpriseRAG.Application.Authorization;
using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
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
            return BadRequest(Result.Fail("参数验证失败"));

        var response = await _authService.LoginAsync(request);
        return Ok(Result<LoginResponse>.SuccessResult(response));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<Result<TokenResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(Result.Fail("刷新令牌不能为空"));

        var response = await _authService.RefreshAccessTokenAsync(request.RefreshToken);
        return Ok(Result<TokenResponse>.SuccessResult(response));
    }
}