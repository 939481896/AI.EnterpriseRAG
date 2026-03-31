using AI.EnterpriseRAG.Application.Authorization;
using AI.EnterpriseRAG.Application.Dtos; // Ensure your Dtos are mapped
using Microsoft.AspNetCore.Mvc;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")] // Standard: api/auth
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // In a real app, you'd add: if (!ModelState.IsValid) return BadRequest();
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshAccessTokenAsync(request.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Session expired. Please login again." });
        }
    }
}