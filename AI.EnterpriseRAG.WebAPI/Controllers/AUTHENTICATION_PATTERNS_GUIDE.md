# Authentication Patterns Guide

## ❓ Your Question: Is validation still required with `[Authorize]` attribute?

### ✅ **Short Answer: NO!**

When `[Authorize]` attribute is present, **authentication is already validated by ASP.NET Core middleware BEFORE your controller method executes**.

---

## 🔍 How `[Authorize]` Works

```
HTTP Request → JWT Middleware → [Authorize] Check → Controller Method
                                        ↓
                                   If Invalid: 401 Unauthorized
                                   (Never reaches controller)
```

### What happens:
1. **JWT Authentication Middleware** validates the token
2. **Authorization Middleware** checks if user is authenticated
3. **If invalid:** Returns 401 Unauthorized immediately
4. **If valid:** Populates `HttpContext.User` with claims
5. **Only then:** Your controller method executes

---

## ❌ Old Pattern (Redundant)

```csharp
[Authorize] // ← Already validates authentication here!
public async Task<IActionResult> Example()
{
    var user = GetCurrentUser();
    
    // ❌ This check is REDUNDANT!
    // If user is null here, it means [Authorize] is broken (never happens)
    if (user == null || !user.IsAuthenticated)
        return Unauthorized(...);
    
    // Your logic...
}
```

**Problem:** Checking for null after `[Authorize]` is like:
- Checking if water is wet
- Checking if 2+2=4
- Defensive coding against impossible conditions

---

## ✅ Correct Patterns

### Pattern A: With `[Authorize]` Attribute (Authentication Required)

#### Method 1: Direct Access (Simple)
```csharp
[Authorize]
public async Task<IActionResult> GetDocuments()
{
    // No null check needed! [Authorize] guarantees authentication
    var user = GetCurrentUserRequired();
    
    var docs = await _service.GetUserDocumentsAsync(user.UserId);
    return Ok(docs);
}
```

#### Method 2: ExecuteWithUserRequiredAsync (Recommended) ⭐
```csharp
[Authorize]
public async Task<IActionResult> UploadDocument(IFormFile file)
{
    return await ExecuteWithUserRequiredAsync(async (user) =>
    {
        // user is guaranteed non-null
        var docId = await _service.UploadAsync(file, user.UserId, user.TenantId);
        return Ok(Result<Guid>.SuccessResult(docId));
    });
}
```

**Why recommended:**
- ✅ No null checks
- ✅ Clean, functional style
- ✅ Exception handling built-in
- ✅ Less boilerplate code

---

### Pattern B: Without `[Authorize]` Attribute (Optional Authentication)

#### Method 1: Manual Check
```csharp
[AllowAnonymous] // or no attribute
public async Task<IActionResult> GetPublicContent()
{
    var user = GetCurrentUser(); // Returns null if not authenticated
    
    // NOW the null check makes sense!
    if (user == null)
    {
        // Show public content
        return Ok(await _service.GetPublicContentAsync());
    }
    
    // Show personalized content
    return Ok(await _service.GetPersonalizedContentAsync(user.UserId));
}
```

#### Method 2: ExecuteWithUserAsync (Handles Unauthorized)
```csharp
[AllowAnonymous]
public async Task<IActionResult> RestrictedButOptional()
{
    return await ExecuteWithUserAsync(async (user) =>
    {
        // Only called if user is authenticated
        return Ok(await _service.GetDataAsync(user.UserId));
    });
    // Returns 401 if not authenticated
}
```

---

## 📋 Complete Method Reference

### For Endpoints WITH `[Authorize]`

| Method | When to Use | Returns | Throws on No Auth |
|--------|-------------|---------|-------------------|
| `GetCurrentUserRequired()` | Simple cases, need direct user context | `CurrentUserContext` | ✅ Yes (Exception) |
| `ExecuteWithUserRequiredAsync()` | **Recommended** - Complex logic, async operations | `IActionResult` | ✅ Yes (Exception) |

**Key Point:** Both methods assume authentication is guaranteed by `[Authorize]`

### For Endpoints WITHOUT `[Authorize]`

| Method | When to Use | Returns | Handles Unauthorized |
|--------|-------------|---------|----------------------|
| `GetCurrentUser()` | Need to check if user is authenticated | `CurrentUserContext?` (nullable) | ❌ Manual check required |
| `GetCurrentUserOrUnauthorized()` | Want automatic 401 response | `ActionResult<CurrentUserContext>` | ✅ Returns 401 |
| `ExecuteWithUserAsync()` | **Recommended** - Action only for authenticated users | `IActionResult` | ✅ Returns 401 |

---

## 🎯 Real-World Examples

### Example 1: Document Upload (Authentication Required)

```csharp
[HttpPost("upload")]
[Authorize] // ← Authentication required
[Permission("doc.upload")]
public async Task<IActionResult> UploadDocument(IFormFile file)
{
    return await ExecuteWithUserRequiredAsync(async (user) =>
    {
        // No null check! [Authorize] guarantees user is authenticated
        _logger.LogInformation("User {UserId} uploading {FileName}", 
            user.UserId, file.FileName);

        var docId = await _documentService.UploadAsync(
            file, 
            user.UserId,    // Safe to use
            user.TenantId); // Safe to use
        
        return Ok(Result<Guid>.SuccessResult(docId, 
            MessageResources.Document.UploadSuccess));
    });
}
```

### Example 2: Public Content with Optional Personalization

```csharp
[HttpGet("news")]
[AllowAnonymous] // ← Authentication optional
public async Task<IActionResult> GetNews()
{
    var user = GetCurrentUser();
    
    if (user == null)
    {
        // Show generic news
        var publicNews = await _newsService.GetPublicNewsAsync();
        return Ok(Result<List<News>>.SuccessResult(publicNews));
    }
    
    // Show personalized news
    var personalizedNews = await _newsService.GetPersonalizedNewsAsync(
        user.UserId, 
        user.TenantId);
    return Ok(Result<List<News>>.SuccessResult(personalizedNews));
}
```

### Example 3: Chat History (Authentication Required)

```csharp
[HttpGet("history")]
[Authorize]
public async Task<IActionResult> GetHistory(int pageSize = 20)
{
    return await ExecuteWithUserRequiredAsync(async (user) =>
    {
        var conversations = await _chatService.GetUserConversationsAsync(
            user.UserId, 
            pageSize);
        
        return Ok(Result<List<Conversation>>.SuccessResult(conversations));
    });
}
```

### Example 4: Agent Execution with Streaming (Authentication Required)

```csharp
[HttpPost("execute")]
[Authorize]
public async Task ExecuteAgent([FromBody] AgentRequestDto request, 
    CancellationToken ct)
{
    var user = GetCurrentUserRequired(); // No null check needed

    // Set SSE headers
    Response.Headers.Add("Content-Type", "text/event-stream");
    
    await foreach (var stepEvent in _agentOrchestrator.ExecuteAsync(
        request.Input,
        user.UserId,
        user.TenantId,
        ct))
    {
        await Response.WriteAsync(FormatSseEvent(stepEvent), ct);
    }
}
```

---

## 🚨 Common Mistakes

### Mistake 1: Redundant Null Check with `[Authorize]`
```csharp
// ❌ BAD
[Authorize]
public async Task<IActionResult> Example()
{
    var user = GetCurrentUser();
    if (user == null) // ← REDUNDANT! [Authorize] already checked this
        return Unauthorized(...);
}

// ✅ GOOD
[Authorize]
public async Task<IActionResult> Example()
{
    var user = GetCurrentUserRequired(); // Throws if null (indicates bug)
    // Use user directly
}
```

### Mistake 2: Not Using `[Authorize]` But Expecting User
```csharp
// ❌ BAD - No [Authorize] attribute
public async Task<IActionResult> Example()
{
    var user = GetCurrentUserRequired(); // ← Will throw for anonymous users!
}

// ✅ GOOD - Add [Authorize]
[Authorize]
public async Task<IActionResult> Example()
{
    var user = GetCurrentUserRequired(); // Now safe
}
```

### Mistake 3: Using Wrong Method for Optional Auth
```csharp
// ❌ BAD
[AllowAnonymous]
public async Task<IActionResult> Example()
{
    var user = GetCurrentUserRequired(); // ← Throws for anonymous!
}

// ✅ GOOD
[AllowAnonymous]
public async Task<IActionResult> Example()
{
    var user = GetCurrentUser(); // Returns null for anonymous
    if (user != null)
    {
        // Personalized logic
    }
}
```

---

## 🎓 Decision Tree

```
┌─────────────────────────────────────┐
│  Is authentication required?        │
└──────────────┬──────────────────────┘
               │
       ┌───────┴────────┐
       │                │
      YES              NO
       │                │
       ▼                ▼
   [Authorize]    [AllowAnonymous]
       │                │
       ▼                ▼
GetCurrentUser      GetCurrentUser()
   Required()       (nullable)
       │                │
       ▼                ▼
ExecuteWithUser     ExecuteWithUser
   RequiredAsync       Async
       │                │
       ▼                ▼
   No null          Manual null
   checks           check or auto
   needed           401 response
```

---

## 📊 Performance Comparison

| Pattern | Auth Check Location | Controller Executes | Performance |
|---------|---------------------|---------------------|-------------|
| `[Authorize]` + null check | Middleware + Controller | Always (after middleware) | 🐌 Slightly slower (redundant check) |
| `[Authorize]` + Required | Middleware only | Always | ⚡ Fastest |
| No `[Authorize]` + manual | Controller only | Always | ⚡ Fast (single check) |

**Takeaway:** Using `GetCurrentUserRequired()` with `[Authorize]` is the **most performant** pattern.

---

## ✅ Best Practices Summary

1. **Use `[Authorize]` attribute** for endpoints requiring authentication
2. **Use `GetCurrentUserRequired()`** or `ExecuteWithUserRequiredAsync()` with `[Authorize]`
3. **Use `GetCurrentUser()`** (nullable) with `[AllowAnonymous]` for optional auth
4. **Never trust user IDs from request body** - always use JWT claims
5. **Log user context** for audit trails
6. **Use `user.TenantId`** for multi-tenant data isolation

---

## 🔐 Security Considerations

### ✅ DO
- Always use `[Authorize]` for protected endpoints
- Extract user ID from JWT token (not request)
- Use tenant ID for data isolation
- Log user actions with user context

### ❌ DON'T
- Trust user IDs from request body/query parameters
- Skip `[Authorize]` and rely on manual checks only
- Expose internal user IDs to clients
- Use `GetCurrentUserRequired()` without `[Authorize]`

---

## 📚 Migration Checklist

When migrating existing controllers:

- [ ] Add `[Authorize]` to controller class or methods requiring auth
- [ ] Replace manual claim extraction with `GetCurrentUserRequired()`
- [ ] Remove redundant null checks after `[Authorize]`
- [ ] Use `ExecuteWithUserRequiredAsync()` for cleaner async code
- [ ] Update tests to verify `[Authorize]` attribute presence
- [ ] Verify tenant isolation logic uses `user.TenantId`
- [ ] Remove hardcoded user IDs from request DTOs

---

## 🎯 Key Takeaway

> **If `[Authorize]` is present, authentication is GUARANTEED.**  
> **No null checks needed - it's redundant and indicates a misunderstanding of ASP.NET Core's authentication pipeline.**

Use the right tool for the job:
- **`[Authorize]` + `GetCurrentUserRequired()`** = Authentication required ✅
- **No `[Authorize]` + `GetCurrentUser()`** = Authentication optional ✅
