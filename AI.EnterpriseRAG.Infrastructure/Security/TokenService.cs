using AI.EnterpriseRAG.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens; // Using the faster, AOT-friendly handler
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Security;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(SysUser user, List<string> permissions)
    {
        // 1. Setup Claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Account),
            new Claim("tid", user.TenantId ?? string.Empty), // Standard 'tid' for Tenant ID
        };

        // Add permissions as multiple claims with the same type
        // This allows user.HasClaim("permission", "doc.read") to work naturally
        foreach (var p in permissions)
        {
            claims.Add(new Claim("perm", p));
        }

        // 2. Security Key from Config
        var secretKey = _config["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Generate Token using the modern JsonWebTokenHandler
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"] ?? "rag.auth",
            Audience = _config["Jwt:Audience"] ?? "rag.api",
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = creds
        };

        return handler.CreateToken(descriptor);
    }

    public string GenerateRefreshToken()
    {
        // High-entropy random string
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}