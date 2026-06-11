# 🔧 DocumentRecoveryService Fixes & DI Best Practices

## 🐛 **Issue 1: Missing Vector Database Cleanup**

### **Problem:**
When `DocumentRecoveryService` marks timed-out documents as "Failed", it only updates the SQL database but **doesn't delete embeddings** from the vector database (Qdrant/Chroma). This causes:

- ❌ Orphaned vectors waste storage
- ❌ Pollute RAG search results (failed documents still returned)
- ❌ Inconsistent state between SQL and vector DB

### **Root Cause:**
```csharp
// ❌ OLD CODE (DocumentRecoveryService.cs, lines 65-72)
foreach (var doc in timedOutDocuments)
{
    doc.Status = DocumentStatus.Failed;
    doc.UpdateTime = DateTime.UtcNow;
    await documentRepository.UpdateAsync(doc);  // Only updates SQL
    // ⚠️ Missing: Vector cleanup!
}
```

### **Solution:**
```csharp
// ✅ NEW CODE - Complete cleanup
var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

foreach (var doc in timedOutDocuments)
{
    try
    {
        // 1️⃣ Delete embeddings from vector database
        await vectorStore.DeleteByDocumentIdAsync(doc.Id, cancellationToken);
        
        // 2️⃣ Delete chunks from SQL database
        await documentRepository.DeleteChunksByDocumentIdAsync(doc.Id, cancellationToken);
        
        // 3️⃣ Mark document as failed
        doc.Status = DocumentStatus.Failed;
        doc.UpdateTime = DateTime.UtcNow;
        await documentRepository.UpdateAsync(doc);
        
        _logger.LogWarning("✅ Document cleaned up: {DocId}", doc.Id);
    }
    catch (Exception cleanupEx)
    {
        // Still mark as failed even if cleanup fails
        _logger.LogError(cleanupEx, "Failed cleanup for {DocId}", doc.Id);
        doc.Status = DocumentStatus.Failed;
        await documentRepository.UpdateAsync(doc);
    }
}
```

### **Files Modified:**
- ✅ `AI.EnterpriseRAG.Application/Services/DocumentRecoveryService.cs`
  - Added `using AI.EnterpriseRAG.Domain.Interfaces.Services;`
  - Added vector store cleanup in timed-out document handling

---

## 🏗️ **Issue 2: Dependency Injection Patterns - Best Practices**

### **Question:**
> "Why doesn't DocumentRecoveryService use DI? I see many services using `scope.ServiceProvider.GetRequiredService<T>()`... is that correct?"

### **Answer: Both Patterns Are Valid - Context Matters!**

---

## **Pattern 1: Constructor Injection (Standard DI)**

### **When to Use:**
- ✅ Regular API controllers
- ✅ Short-lived services (per-request scope)
- ✅ Dependencies needed throughout class lifetime
- ✅ **Best practice for unit testing**

### **Example:**
```csharp
public class ChatController : ControllerBase
{
    private readonly IChatUseCase _chatUseCase;
    private readonly ILogger<ChatController> _logger;

    // ✅ Constructor Injection - clean and testable
    public ChatController(
        IChatUseCase chatUseCase,
        ILogger<ChatController> logger)
    {
        _chatUseCase = chatUseCase;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        // Direct usage of injected dependencies
        var response = await _chatUseCase.ChatAsync(request);
        return Ok(response);
    }
}
```

**Benefits:**
- ✅ Clear dependencies visible in constructor
- ✅ Easy to mock for unit tests
- ✅ Follows SOLID principles
- ✅ Compiler checks all dependencies

---

## **Pattern 2: Service Locator via `IServiceProvider.CreateScope()`**

### **When to Use:**
- ✅ **Background services** (`IHostedService`, `BackgroundService`)
- ✅ **Long-running operations** that outlive HTTP request
- ✅ **Fire-and-forget tasks** (e.g., `Task.Run()`)
- ✅ Need **multiple independent scopes**

### **Example 1: Background Service**
```csharp
public class DocumentRecoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    // ✅ Only inject IServiceProvider (singleton-safe)
    public DocumentRecoveryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // ✅ Create NEW scope for background operation
        using var scope = _serviceProvider.CreateScope();
        
        // ✅ Resolve scoped services within this scope
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        
        // Do work...
        // Scope automatically disposed when exiting 'using' block
    }
}
```

### **Example 2: Fire-and-Forget Task in UseCase**
```csharp
public class DocumentUseCase : IDocumentUseCase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IServiceProvider _serviceProvider;

    public DocumentUseCase(
        IDocumentRepository documentRepository,
        IServiceProvider serviceProvider)
    {
        _documentRepository = documentRepository;
        _serviceProvider = serviceProvider;
    }

    public async Task UploadAsync(...)
    {
        // ✅ Use injected repository in main request scope
        await _documentRepository.AddAsync(document);

        // ✅ Fire-and-forget background task needs NEW scope
        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
            var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
            
            // Process document... (outlives HTTP request)
            // Scope disposed after completion
        });
    }
}
```

---

## **Why This Pattern Is Necessary**

### **Problem Scenario (What Happens If You Don't Use Scopes):**

```csharp
// ❌ WRONG: Injecting scoped services in singleton
public class DocumentRecoveryService : IHostedService
{
    private readonly IVectorStore _vectorStore;        // ❌ Scoped service
    private readonly IDocumentRepository _repository;  // ❌ Has DbContext (scoped)

    public DocumentRecoveryService(
        IVectorStore vectorStore,
        IDocumentRepository repository)
    {
        _vectorStore = vectorStore;      // ⚠️ DANGER!
        _repository = repository;        // ⚠️ DANGER!
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // ❌ Problem: Using scoped services in singleton context
        // ❌ Result: Memory leaks, disposed DbContext errors
        await _repository.GetByStatusAsync(...);  // 💥 ObjectDisposedException
    }
}
```

**Why it fails:**
1. `DocumentRecoveryService` is registered as **Singleton** (lives for entire application lifetime)
2. `IDocumentRepository` has `DbContext` which is **Scoped** (per-request lifetime)
3. When you inject scoped service into singleton → **captured in long-lived object**
4. DbContext gets disposed after first operation → **subsequent calls fail**

---

### **Correct Pattern:**

```csharp
// ✅ CORRECT: Create new scope for each operation
public class DocumentRecoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;  // ✅ Singleton-safe

    public DocumentRecoveryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // ✅ Create NEW scope = fresh DbContext + scoped services
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
        
        await repository.GetByStatusAsync(...);  // ✅ Works perfectly
        
        // DbContext disposed when scope exits
    }
}
```

---

## **Decision Tree: Which Pattern to Use?**

```
┌─ Is your service an IHostedService or BackgroundService?
│
├─ YES → ✅ Use IServiceProvider + CreateScope()
│         Reason: Singleton lifetime, needs independent scopes
│
└─ NO → Does it fire long-running background tasks (Task.Run)?
    │
    ├─ YES → ✅ Use IServiceProvider + CreateScope() for background work
    │         Example: DocumentUseCase processing documents
    │
    └─ NO → ✅ Use Constructor Injection
              Reason: Simpler, testable, follows SOLID
```

---

## **Comparison Table**

| Aspect | Constructor Injection | Service Locator (IServiceProvider) |
|--------|----------------------|-----------------------------------|
| **Testability** | ✅ Excellent (easy mocking) | ⚠️ Harder (need to mock service provider) |
| **Clarity** | ✅ Dependencies visible | ❌ Hidden dependencies |
| **Scope Control** | ❌ Limited | ✅ Full control over lifetime |
| **Background Services** | ❌ Not suitable | ✅ Required |
| **Fire-and-Forget Tasks** | ❌ Causes memory leaks | ✅ Safe |
| **SOLID Compliance** | ✅ Yes | ⚠️ Violates some principles |
| **Use Case** | Regular services | Long-running/background work |

---

## **Real-World Examples in Your Codebase**

### **✅ Correct: ChatController (Constructor Injection)**
```csharp
public class ChatController : ControllerBase
{
    private readonly IChatUseCase _chatUseCase;  // ✅ Per-request scope
    
    public ChatController(IChatUseCase chatUseCase)
    {
        _chatUseCase = chatUseCase;
    }
}
```

### **✅ Correct: DocumentRecoveryService (Service Locator)**
```csharp
public class DocumentRecoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;  // ✅ Singleton-safe
    
    public DocumentRecoveryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();  // ✅ New scope
        // ...
    }
}
```

### **✅ Correct: DocumentUseCase (Hybrid Pattern)**
```csharp
public class DocumentUseCase : IDocumentUseCase
{
    private readonly IDocumentRepository _documentRepository;  // ✅ For main request
    private readonly IServiceProvider _serviceProvider;         // ✅ For background tasks
    
    public DocumentUseCase(
        IDocumentRepository documentRepository,
        IServiceProvider serviceProvider)
    {
        _documentRepository = documentRepository;
        _serviceProvider = serviceProvider;
    }

    public async Task UploadAsync(...)
    {
        await _documentRepository.AddAsync(...);  // ✅ Use injected repo
        
        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();  // ✅ New scope for background
            var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
            // ...
        });
    }
}
```

---

## **Summary**

### **Fixed Issues:**
1. ✅ **DocumentRecoveryService now cleans up vector database** when marking documents as failed
2. ✅ **Added proper error handling** for cleanup failures
3. ✅ **Clarified DI patterns** - both approaches are valid depending on context

### **Best Practices:**
- ✅ **Constructor Injection** → Default choice for regular services
- ✅ **Service Locator** → Use for background services and fire-and-forget tasks
- ✅ Always **dispose scopes** with `using` statement
- ✅ Never capture scoped services in singleton-lifetime objects

### **Build Status:**
✅ **Build successful** - All changes compile correctly

---

**Date:** 2025-01-XX  
**Author:** AI Assistant  
**Version:** 1.0.0
