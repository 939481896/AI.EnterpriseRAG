using AI.EnterpriseRAG.Core.Models;
using AI.EnterpriseRAG.Core.Resources;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AI.EnterpriseRAG.WebAPI.Controllers;

/// <summary>
/// Base API controller with common functionality for all controllers
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Current user context extracted from JWT token
    /// </summary>
    public class CurrentUserContext
    {
        /// <summary>
        /// User ID (Account from JWT UniqueName claim)
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Tenant ID for multi-tenancy support
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// User's numeric ID (if available)
        /// </summary>
        public string? NumericUserId { get; set; }

        /// <summary>
        /// Whether user context is valid
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }

    /// <summary>
    /// Get current authenticated user context from JWT token
    /// Use this when [Authorize] attribute is NOT present (optional authentication)
    /// </summary>
    /// <returns>User context or null if not authenticated</returns>
    protected CurrentUserContext? GetCurrentUser()
    {
        // Try to extract UserId from multiple claim types (in priority order)
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)  // Preferred: Account
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return BuildUserContext(userId);
    }

    /// <summary>
    /// Get current authenticated user context from JWT token
    /// Use this when [Authorize] attribute IS present (authentication guaranteed)
    /// Throws exception if user is not authenticated (should never happen with [Authorize])
    /// </summary>
    /// <returns>User context (never null)</returns>
    /// <exception cref="InvalidOperationException">If user is not authenticated (indicates misconfiguration)</exception>
    protected CurrentUserContext GetCurrentUserRequired()
    {
        var user = GetCurrentUser();

        if (user == null || !user.IsAuthenticated)
        {
            throw new InvalidOperationException(
                "GetCurrentUserRequired called but user is not authenticated. " +
                "Ensure [Authorize] attribute is present on controller/action.");
        }

        return user;
    }

    /// <summary>
    /// Build user context from userId and claims
    /// </summary>
    private CurrentUserContext BuildUserContext(string userId)
    {
        // Extract additional context
        var userName = User.FindFirstValue(ClaimTypes.GivenName)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Name)
                      ?? userId;

        var tenantId = User.FindFirstValue("tid")
                      ?? User.FindFirstValue("tenant_id")
                      ?? User.FindFirstValue("tenantId")
                      ?? "default";

        var numericUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return new CurrentUserContext
        {
            UserId = userId,
            UserName = userName,
            TenantId = tenantId,
            NumericUserId = numericUserId
        };
    }

    /// <summary>
    /// Get current user or return Unauthorized result if not authenticated
    /// </summary>
    /// <returns>ActionResult with user context or Unauthorized</returns>
    protected ActionResult<CurrentUserContext> GetCurrentUserOrUnauthorized()
    {
        var user = GetCurrentUser();
        
        if (user == null || !user.IsAuthenticated)
        {
            return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));
        }

        return user;
    }

    /// <summary>
    /// Execute an action with current user context
    /// Use ONLY when [Authorize] attribute is NOT present (handles optional authentication)
    /// For endpoints with [Authorize], use ExecuteWithUserRequiredAsync instead
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="action">Action to execute with user context</param>
    /// <returns>Action result</returns>
    protected async Task<ActionResult<T>> ExecuteWithUserAsync<T>(
        Func<CurrentUserContext, Task<ActionResult<T>>> action)
    {
        var userResult = GetCurrentUserOrUnauthorized();

        if (userResult.Result is UnauthorizedObjectResult unauthorizedResult)
        {
            return unauthorizedResult;
        }

        var user = userResult.Value!;
        return await action(user);
    }

    /// <summary>
    /// Execute an action with current user context
    /// Use ONLY when [Authorize] attribute is NOT present (handles optional authentication)
    /// For endpoints with [Authorize], use ExecuteWithUserRequiredAsync instead
    /// </summary>
    /// <param name="action">Action to execute with user context</param>
    /// <returns>Action result</returns>
    protected async Task<IActionResult> ExecuteWithUserAsync(
        Func<CurrentUserContext, Task<IActionResult>> action)
    {
        var userResult = GetCurrentUserOrUnauthorized();

        if (userResult.Result is UnauthorizedObjectResult unauthorizedResult)
        {
            return unauthorizedResult;
        }

        var user = userResult.Value!;
        return await action(user);
    }

    /// <summary>
    /// Execute an action with current user context (authentication guaranteed by [Authorize])
    /// RECOMMENDED: Use this when [Authorize] attribute IS present on controller/action
    /// No redundant null checks - [Authorize] already validated authentication
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="action">Action to execute with user context</param>
    /// <returns>Action result</returns>
    protected async Task<ActionResult<T>> ExecuteWithUserRequiredAsync<T>(
        Func<CurrentUserContext, Task<ActionResult<T>>> action)
    {
        var user = GetCurrentUserRequired();
        return await action(user);
    }

    /// <summary>
    /// Execute an action with current user context (authentication guaranteed by [Authorize])
    /// RECOMMENDED: Use this when [Authorize] attribute IS present on controller/action
    /// No redundant null checks - [Authorize] already validated authentication
    /// </summary>
    /// <param name="action">Action to execute with user context</param>
    /// <returns>Action result</returns>
    protected async Task<IActionResult> ExecuteWithUserRequiredAsync(
        Func<CurrentUserContext, Task<IActionResult>> action)
    {
        var user = GetCurrentUserRequired();
        return await action(user);
    }
}
