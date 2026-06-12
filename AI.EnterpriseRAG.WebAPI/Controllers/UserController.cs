using AI.EnterpriseRAG.Application.Dtos;
using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// 用户管理接口
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class UserController : BaseApiController
{
    private readonly AppEnterpriseAiContext _context;
    private readonly IPasswordHasher<SysUser> _passwordHasher;
    private readonly ILogger<UserController> _logger;

    public UserController(
        AppEnterpriseAiContext context,
        IPasswordHasher<SysUser> passwordHasher,
        ILogger<UserController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Users.AsQueryable();

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Account,
                u.UserName,
                u.IsEnabled,
                u.CreateTime
            })
            .ToListAsync();

        return Ok(Result<object>.SuccessResult(new
        {
            items = users,
            total,
            page,
            pageSize
        }));
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(long id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Account,
                u.UserName,
                u.IsEnabled,
                u.CreateTime
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(Result.Fail(MessageResources.User.NotFound));

        return Ok(Result<object>.SuccessResult(user));
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        // Check if account already exists
        if (await _context.Users.AnyAsync(u => u.Account == request.Account))
            return BadRequest(Result.Fail(MessageResources.User.AccountExists));

        var user = new SysUser
        {
            Account = request.Account,
            UserName = request.RealName ?? request.Account,
            IsEnabled = true,
            CreateTime = DateTime.Now,
            TenantId = "default"
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created user: {Account}", user.Account);

        return Ok(Result<object>.SuccessResult(new
        {
            user.Id,
            user.Account,
            user.UserName
        }));
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserDto request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(Result.Fail(MessageResources.User.NotFound));

        // Check email uniqueness if changed
        user.UserName = request.RealName ?? user.UserName;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated user: {UserId}", id);

        return Ok(Result.Success(MessageResources.User.UpdateSuccess));
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(long id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(Result.Fail(MessageResources.User.NotFound));

        // Prevent deleting admin
        if (user.Account == "admin")
            return BadRequest(Result.Fail(MessageResources.User.CannotDeleteAdmin));

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted user: {UserId}", id);

        return Ok(Result.Success(MessageResources.User.DeleteSuccess));
    }

    /// <summary>
    /// 启用/禁用用户
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ToggleUserStatus(long id, [FromBody] ToggleStatusDto request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(Result.Fail(MessageResources.User.NotFound));

        // Prevent disabling admin
        if (user.Account == "admin" && !request.IsActive)
            return BadRequest(Result.Fail(MessageResources.User.CannotDisableAdmin));

        user.IsEnabled = request.IsActive;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Toggled user status: {UserId} -> {Status}", id, request.IsActive);

        return Ok(Result.Success(request.IsActive ? MessageResources.User.StatusEnabled : MessageResources.User.StatusDisabled));
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordDto request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(Result.Fail(MessageResources.User.NotFound));

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reset password for user: {UserId}", id);

        return Ok(Result.Success(MessageResources.User.PasswordResetSuccess));
    }
}
