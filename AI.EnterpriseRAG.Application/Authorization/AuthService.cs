using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Application.Authorization;

public class AuthService
{
    private readonly AppEnterpriseAiContext _context;
    private readonly TokenService _tokenService;
    private readonly IPasswordHasher<SysUser> _passwordHasher;


    public AuthService(AppEnterpriseAiContext context, TokenService tokenService,IPasswordHasher<SysUser> passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // 1️ 多租户查询 + 校验用户是否启用
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Account == request.Account
                                   && u.TenantId == request.TenantId);

        // 用户不存在 / 用户被禁用 / 密码错误 统一提示，防枚举攻击
        if (user == null || !user.IsEnabled)
            throw new UnauthorizedAccessException("认证失败");

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify != PasswordVerificationResult.Success)
            throw new UnauthorizedAccessException("认证失败");

        // 2️ 获取权限
        var permissions = await GetUserPermissionsAsync(user.Id);

        // 3️ 生成令牌
        var accessToken = _tokenService.GenerateAccessToken(user, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // 支持 MySQL 重试的事务
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingTokens = _context.RefreshTokens.Where(t => t.UserId == user.Id);
                _context.RefreshTokens.RemoveRange(existingTokens);

                _context.RefreshTokens.Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpireAt = DateTime.UtcNow.AddDays(7),
                    Device = "web",
                    IsRevoked = false
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw new Exception("登录失败，请稍后重试");
            }
        });

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 1800,
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

    private async Task<List<string>> GetUserPermissionsAsync(long userId)
    {
        return await _context.Permissions
            .AsNoTracking()
            .Where(p => p.RolePermissions
                .Any(rp => rp.Role.UserRoles
                    .Any(ur => ur.UserId == userId)))
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync();
    }
}