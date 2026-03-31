using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.EnterpriseRAG.Application.Dtos;

public class LoginRequest
{
    public string Account { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string TenantId { get; set; } = default!; // REQUIRED for multi-tenancy
}
public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public long ExpiresIn { get; set; }

    public string UserName { get; set; } = default!;
    public List<string> Permissions { get; set; } = new();
}

public class RefreshTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
