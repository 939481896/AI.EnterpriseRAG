using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // 🆕 添加日志

namespace AI.EnterpriseRAG.Infrastructure.Persistence.Repositories
{
    public class PermissionRepository : IPermissionService
    {
        private readonly AppEnterpriseAiContext _context;
        private readonly ILogger<PermissionRepository> _logger; // 🆕 添加日志

        public PermissionRepository(
            AppEnterpriseAiContext context,
            ILogger<PermissionRepository> logger) // 🆕 注入日志
        {
            _context = context;
            _logger = logger;
        }

        // 多租户集合路由：用户 → 对应向量库Collection
        public async Task<string> GetUserCollectionNameAsync(string userId, CancellationToken ct = default)
        {
            // 🔧 修复：使用统一的Collection（简化多租户实现）
            await Task.CompletedTask;
            return "enterprise_rag_collection";

            // 企业级多租户实现示例：
            // var userDepartment = await GetUserDepartmentAsync(userId);
            // return $"{userDepartment.ToLower()}_rag_collection";
        }

        // 权限过滤：用户只能访问有权限的文档
        public async Task<List<string>> GetUserAllowedDocumentIdsAsync(string userId, CancellationToken ct = default)
        {
            // 1. 查询用户信息
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Account == userId, ct);

            if (user == null)
            {
                _logger.LogWarning("[权限] 用户 {UserId} 不存在", userId); // 🆕 使用ILogger
                return new List<string>();
            }

            // 2. 超级管理员：访问所有已向量化的文档
            var isAdmin = user.UserRoles.Any(ur => ur.Role.RoleCode == "admin");
            if (isAdmin)
            {
                var allDocuments = await _context.Documents
                    .Where(d => d.Status == Domain.Enums.DocumentStatus.Vectorized)
                    .Select(d => d.Id.ToString())
                    .ToListAsync(ct);

                _logger.LogInformation("[权限] 管理员 {UserId} 可访问所有文档：{Count}个", userId, allDocuments.Count); // 🆕 使用ILogger
                return allDocuments;
            }

            // 3. 普通用户：访问自己的 + 公开的 + 同租户的文档
            var tenantId = user.TenantId;

            var documents = await _context.Documents
                .Where(d =>
                    d.Status == Domain.Enums.DocumentStatus.Vectorized &&
                    (
                        d.UploadedBy == userId ||      // 自己上传的
                        d.IsPublic ||                  // 公开文档
                        (d.TenantId == tenantId && !string.IsNullOrEmpty(tenantId)) // 同租户文档
                    )
                )
                .Select(d => d.Id.ToString())
                .ToListAsync(ct);

            _logger.LogInformation("[权限] 用户 {UserId}（租户：{TenantId}）可访问文档：{Count}个", 
                userId, tenantId ?? "无", documents.Count); // 🆕 使用ILogger

            // 如果没有文档，返回空列表（会在ChatUseCase中被拦截）
            if (!documents.Any())
            {
                _logger.LogWarning("[权限] 用户 {UserId} 无可访问文档", userId); // 🆕 使用ILogger
            }

            return documents;

            /* 
            企业级扩展：
            1. 支持文档标签过滤：d.Tags.Any(t => user.AllowedTags.Contains(t))
            2. 支持部门层级权限：GetDepartmentHierarchy(user.DepartmentId)
            3. 支持时间范围过滤：d.CreateTime >= user.AccessStartDate
            4. 支持文档分类权限：d.Category.In(user.AllowedCategories)
            */
        }

        private Task<string> GetUserDepartmentAsync(string userId)
        {
            // 示例：固定返回，实际从身份中心读取
            return Task.FromResult("enterprise");
        }
    }
}
