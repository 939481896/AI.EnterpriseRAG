using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AI.EnterpriseRAG.Application.UseCases;

public partial class DocumentUseCase
{
    /// <summary>
    /// 获取用户的文档列表（分页）
    /// </summary>
    public async Task<object> GetUserDocumentsAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .Where(d => d.UploadedBy == userId)
            .OrderByDescending(d => d.CreateTime);

        var total = await query.CountAsync(cancellationToken);
        var documents = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.FileType,
                d.FileSize,
                d.Status,
                d.CreateTime,
                d.CompleteTime,
                d.UploadedBy,
                d.IsPublic,
                d.CategoryId
            })
            .ToListAsync(cancellationToken);

        return new
        {
            items = documents,
            total,
            page,
            pageSize
        };
    }
}
