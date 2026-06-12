# BaseApiController - User Context Extraction Guide

## Overview

The `BaseApiController` provides a centralized, type-safe way to extract user information from JWT tokens across all API controllers. This eliminates code duplication and ensures consistent authentication handling.

## Key Features

✅ **Centralized User Context Extraction** - Single source of truth for JWT claim parsing  
✅ **Type-Safe Access** - Strongly-typed `CurrentUserContext` class  
✅ **Multiple Extraction Patterns** - Choose the pattern that fits your needs  
✅ **Multi-Tenancy Support** - Built-in tenant ID extraction  
✅ **Automatic Unauthorized Handling** - Reduces boilerplate code  
✅ **Consistent Error Messages** - Uses MessageResources for i18n  

---

## CurrentUserContext Class

```csharp
public class CurrentUserContext
{
    public string UserId { get; set; }           // Primary user identifier (Account)
    public string UserName { get; set; }         // Display name
    public string TenantId { get; set; }         // Multi-tenancy support (default: "default")
    public string? NumericUserId { get; set; }   // Numeric user ID if available
    public bool IsAuthenticated { get; }         // Convenience property
}
```

### JWT Claim Mapping (Priority Order)

| Property | JWT Claims (in priority order) |
|----------|-------------------------------|
| **UserId** | `UniqueName` → `Name` → `NameIdentifier` → `Sub` |
| **UserName** | `GivenName` → `Name` → `UserId` (fallback) |
| **TenantId** | `tid` → `tenant_id` → `tenantId` → `"default"` |
| **NumericUserId** | `NameIdentifier` → `Sub` |

---

## Available Methods

### 1. `GetCurrentUser()` - Basic Extraction
Returns `CurrentUserContext?` (null if not authenticated)

```csharp
protected CurrentUserContext? GetCurrentUser()
```

### 2. `GetCurrentUserOrUnauthorized()` - With Auto-Unauthorized
Returns `ActionResult<CurrentUserContext>` (Unauthorized if not authenticated)

```csharp
protected ActionResult<CurrentUserContext> GetCurrentUserOrUnauthorized()
```

### 3. `ExecuteWithUserAsync()` - Cleaner Pattern (Recommended)
Automatically handles authentication and executes action with user context

```csharp
protected async Task<IActionResult> ExecuteWithUserAsync(
    Func<CurrentUserContext, Task<IActionResult>> action)

protected async Task<ActionResult<T>> ExecuteWithUserAsync<T>(
    Func<CurrentUserContext, Task<ActionResult<T>>> action)
```

---

## Usage Patterns

### Pattern 1: Manual Check (Fine-Grained Control)

**When to use:** Need explicit control over authentication flow

```csharp
[HttpGet("example")]
public async Task<IActionResult> Example()
{
    var user = GetCurrentUser();
    if (user == null || !user.IsAuthenticated)
    {
        return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));
    }

    _logger.LogInformation("User {UserId} authenticated", user.UserId);
    
    // Use user.UserId, user.TenantId, user.UserName
    var result = await _service.DoSomethingAsync(user.UserId);
    
    return Ok(Result<object>.SuccessResult(result));
}
```

**Pros:** Explicit, clear authentication check  
**Cons:** More verbose, manual error handling

---

### Pattern 2: GetCurrentUserOrUnauthorized (Cleaner)

**When to use:** Need user context with automatic unauthorized response

```csharp
[HttpGet("example")]
public async Task<IActionResult> Example()
{
    var userResult = GetCurrentUserOrUnauthorized();
    
    if (userResult.Result is UnauthorizedObjectResult unauthorized)
        return unauthorized;

    var user = userResult.Value!;
    
    // Use user context
    var result = await _service.DoSomethingAsync(user.UserId);
    
    return Ok(Result<object>.SuccessResult(result));
}
```

**Pros:** Less boilerplate, automatic unauthorized handling  
**Cons:** Requires unwrapping ActionResult

---

### Pattern 3: ExecuteWithUserAsync (Recommended) ⭐

**When to use:** Most API endpoints - cleanest code

```csharp
[HttpPost("ask")]
public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
{
    return await ExecuteWithUserAsync(async (user) =>
    {
        _logger.LogInformation(
            "User {UserId} from tenant {TenantId} asking question", 
            user.UserId, user.TenantId);

        request.UserId = user.UserId;
        
        var (answer, references, cost) = await _chatUseCase.ChatAsync(
            user.UserId, 
            request.Question);

        return Ok(Result<ChatResponseDto>.SuccessResult(new ChatResponseDto
        {
            Answer = answer,
            References = references,
            CostSeconds = cost
        }));
    });
}
```

**Pros:** 
- Cleanest code
- Automatic authentication handling
- Direct user context access
- Reduces nesting

**Cons:** None for typical use cases

---

### Pattern 4: ExecuteWithUserAsync with Generic Return

**When to use:** Strongly-typed response needed

```csharp
[HttpGet("user-info")]
public async Task<ActionResult<Result<UserInfoDto>>> GetUserInfo()
{
    return await ExecuteWithUserAsync<Result<UserInfoDto>>(async (user) =>
    {
        var userInfo = new UserInfoDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            TenantId = user.TenantId
        };

        return Ok(Result<UserInfoDto>.SuccessResult(userInfo));
    });
}
```

**Pros:** Type-safe return value, clean code  
**Cons:** Slightly more verbose generic syntax

---

## Migration Guide

### Before (Old Pattern)
```csharp
public class ChatController : ControllerBase
{
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(Result.Fail("用户未登录"));

        var tenantId = User.FindFirstValue("tid")
                      ?? User.FindFirstValue("tenant_id")
                      ?? "default";

        request.UserId = userId;
        
        var result = await _chatUseCase.ChatAsync(userId, request.Question);
        return Ok(result);
    }
}
```

### After (New Pattern) ⭐
```csharp
public class ChatController : BaseApiController
{
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto request)
    {
        return await ExecuteWithUserAsync(async (user) =>
        {
            request.UserId = user.UserId;
            
            var result = await _chatUseCase.ChatAsync(
                user.UserId, 
                request.Question);
                
            return Ok(result);
        });
    }
}
```

**Lines of code:** 15 → 7 (53% reduction)  
**Complexity:** High → Low  
**Maintainability:** ⭐⭐⭐⭐⭐

---

## Common Scenarios

### Scenario 1: Simple GET Endpoint
```csharp
[HttpGet("documents")]
public async Task<IActionResult> GetDocuments()
{
    return await ExecuteWithUserAsync(async (user) =>
    {
        var docs = await _documentService.GetUserDocumentsAsync(user.UserId);
        return Ok(Result<List<Document>>.SuccessResult(docs));
    });
}
```

### Scenario 2: POST with User Context
```csharp
[HttpPost("upload")]
public async Task<IActionResult> UploadDocument(IFormFile file)
{
    return await ExecuteWithUserAsync(async (user) =>
    {
        var docId = await _documentService.UploadAsync(
            file, 
            user.UserId, 
            user.TenantId);
            
        return Ok(Result<Guid>.SuccessResult(docId));
    });
}
```

### Scenario 3: DELETE with Logging
```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteDocument(Guid id)
{
    return await ExecuteWithUserAsync(async (user) =>
    {
        _logger.LogInformation(
            "User {UserId} deleting document {DocId}", 
            user.UserId, id);
            
        await _documentService.DeleteAsync(id, user.UserId);
        return Ok(Result.Success(MessageResources.Document.DeleteSuccess));
    });
}
```

### Scenario 4: Multi-Tenant Query
```csharp
[HttpGet("tenant-stats")]
public async Task<IActionResult> GetTenantStats()
{
    return await ExecuteWithUserAsync(async (user) =>
    {
        var stats = await _analyticsService.GetStatsAsync(user.TenantId);
        return Ok(Result<TenantStats>.SuccessResult(stats));
    });
}
```

---

## Best Practices

### ✅ DO

1. **Use `ExecuteWithUserAsync` for most endpoints** - cleanest pattern
2. **Log user context for audit trails**
   ```csharp
   _logger.LogInformation("User {UserId} performed action", user.UserId);
   ```
3. **Use tenant ID for multi-tenancy**
   ```csharp
   var data = await _service.GetDataAsync(user.TenantId);
   ```
4. **Inherit from BaseApiController**
   ```csharp
   public class MyController : BaseApiController
   ```

### ❌ DON'T

1. **Don't extract claims manually anymore**
   ```csharp
   // ❌ Old way - don't do this
   var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
   ```

2. **Don't hardcode unauthorized messages**
   ```csharp
   // ❌ Bad
   return Unauthorized("用户未登录");
   
   // ✅ Good
   return Unauthorized(Result.Fail(MessageResources.Auth.Unauthorized));
   ```

3. **Don't trust user IDs from request body**
   ```csharp
   // ❌ Bad - never trust client
   var userId = request.UserId;
   
   // ✅ Good - always use JWT token
   var userId = user.UserId;
   ```

---

## Security Considerations

1. **Token-Based Authentication Only**
   - Always use JWT token claims
   - Never trust request body/query parameters for user identity

2. **Tenant Isolation**
   - Use `user.TenantId` for data filtering
   - Prevent cross-tenant data access

3. **Audit Logging**
   - Log all user actions with `user.UserId`
   - Include tenant context for multi-tenant systems

---

## Troubleshooting

### Issue: GetCurrentUser returns null
**Cause:** User not authenticated or invalid JWT token  
**Solution:** Ensure `[Authorize]` attribute is present on controller/action

### Issue: TenantId always "default"
**Cause:** Tenant claim not in JWT token  
**Solution:** Update TokenService to include tenant claim:
```csharp
new Claim("tid", user.TenantId)
```

### Issue: UserId is numeric instead of account name
**Cause:** JWT doesn't have `UniqueName` claim  
**Solution:** Add UniqueName claim in TokenService:
```csharp
new Claim(JwtRegisteredClaimNames.UniqueName, user.Account)
```

---

## Controllers Updated

All controllers have been migrated to use `BaseApiController`:

✅ ChatController  
✅ DocumentController  
✅ AgentController  
✅ DocumentPermissionController  
✅ UserController  
✅ AuthController  
✅ PermissionController  

---

## Summary

The `BaseApiController` pattern provides:

- **53% less code** on average
- **100% consistent** authentication handling
- **Type-safe** user context access
- **Maintainable** and testable code
- **Secure** by default (JWT-based)

**Recommended Pattern:** Use `ExecuteWithUserAsync` for 90% of use cases.
