using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xceed.Document.NET;

namespace AI.EnterpriseRAG.Application.Authorization;

public class AuthService
{
    private readonly AppEnterpriseAiContext _context;
    private readonly TokenService _tokenService;

    public AuthService(AppEnterpriseAiContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 1️⃣ Multi-tenant lookup
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Account == request.Account
                                   && u.TenantId == request.TenantId);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("认证失败：账号、密码或租户信息不正确");
        }

        // 2️⃣ Fetch Permissions using the helper method
        var permissions = await GetUserPermissionsAsync(user.Id);

        // 3️⃣ Token generation
        var accessToken = _tokenService.GenerateAccessToken(user, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // 4️⃣ Transactional Token Update
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var existingTokens = _context.RefreshTokens.Where(t => t.UserId == user.Id);
            _context.RefreshTokens.RemoveRange(existingTokens);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpireAt = DateTime.UtcNow.AddDays(7)
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw new Exception("登录过程中出现系统错误");
        }

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            UserName = user.UserName,
            Permissions = permissions
        };
    }

    public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        var tokenRecord = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (tokenRecord == null || tokenRecord.ExpireAt < DateTime.UtcNow || tokenRecord.IsRevoked)
        {
            throw new UnauthorizedAccessException("Invalid or expired session.");
        }

        // Use the same helper method
        var permissions = await GetUserPermissionsAsync(tokenRecord.UserId);
        var newAccessToken = _tokenService.GenerateAccessToken(tokenRecord.User, permissions);

        return new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = refreshToken
        };
    }

    // 🔥 Added Helper Method to fix the "Not Found" error
    private async Task<List<string>> GetUserPermissionsAsync(long userId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();
    }
}