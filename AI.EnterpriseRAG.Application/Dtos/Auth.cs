using System.ComponentModel.DataAnnotations;

namespace AI.EnterpriseRAG.Application.Dtos;


/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "账号不能为空")]
    public string Account { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "租户ID不能为空")]
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresIn { get; set; } // 30分钟 = 1800
    public string UserName { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}

/// <summary>
/// 刷新令牌请求（【修复】只保留一个 RefreshToken，和接口匹配）
/// </summary>
public class RefreshTokenRequest
{
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 刷新令牌响应
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
