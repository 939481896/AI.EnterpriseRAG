# Repository Methods to Add on Next Application Restart

Due to Hot Reload limitations in Visual Studio (ENC0023), the following repository methods could not be added to the interface during this session. Add them when the application restarts:

## IDocumentRepository Interface Updates

Add the following methods to `AI.EnterpriseRAG.Domain/Interfaces/Repositories/IDocumentRepository.cs`:

```csharp
/// <summary>
/// 删除文档记录
/// </summary>
Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);

/// <summary>
/// 查询用户的文档列表（分页）
/// </summary>
Task<IEnumerable<Document>> GetByUserAsync(
    string userId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);

/// <summary>
/// 统计用户的文档总数
/// </summary>
Task<int> CountByUserAsync(string userId, CancellationToken cancellationToken = default);
```

## DocumentRepository Implementation Updates

Add the following implementations to `AI.EnterpriseRAG.Infrastructure/Persistence/Repositories/DocumentRepository.cs`:

```csharp
/// <summary>
/// 删除文档记录
/// </summary>
public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
{
    var document = await _dbContext.Documents.FindAsync(new object[] { documentId }, cancellationToken);
    if (document != null)
    {
        _dbContext.Documents.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// 查询用户的文档列表（分页）
/// </summary>
public async Task<IEnumerable<Document>> GetByUserAsync(
    string userId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Documents
        .Where(d => d.UploadedBy == userId)
        .OrderByDescending(d => d.CreateTime)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);
}

/// <summary>
/// 统计用户的文档总数
/// </summary>
public async Task<int> CountByUserAsync(string userId, CancellationToken cancellationToken = default)
{
    return await _dbContext.Documents
        .Where(d => d.UploadedBy == userId)
        .CountAsync(cancellationToken);
}
```

## DocumentUseCase Cleanup

After adding the repository methods above, you can update `DocumentUseCase.cs` to use the repository methods instead of direct context access:

### Current (Temporary) Implementation
```csharp
// 5. Delete document record using context directly
_context.Documents.Remove(document);
await _context.SaveChangesAsync(cancellationToken);
```

### Preferred (After Repository Update)
```csharp
// 5. Delete document record
await _documentRepository.DeleteAsync(documentId, cancellationToken);
```

## Why These Changes Are Needed

1. **DeleteAsync**: Currently, `DocumentUseCase.DeleteDocumentInternalAsync` uses `_context.Documents.Remove()` directly, which bypasses the repository pattern.

2. **GetByUserAsync & CountByUserAsync**: The existing partial class `DocumentUseCase_GetDocuments.cs` already implements `GetUserDocumentsAsync` using `_context` directly. These repository methods would allow following the repository pattern consistently.

## Current Status

✅ **System is working** with temporary direct context access
⚠️ **Recommendation**: Add these repository methods on next restart to maintain consistent architecture

## Testing After Adding Methods

After adding these methods and restarting:

1. Test document deletion:
```bash
DELETE /api/Document/{documentId}
```

2. Test document list:
```bash
GET /api/Document/list?page=1&pageSize=20
```

3. Verify failed documents can be re-uploaded
4. Verify reprocess endpoint works correctly
