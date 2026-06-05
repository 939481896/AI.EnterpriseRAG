using AI.EnterpriseRAG.Domain.Enums;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AI.EnterpriseRAG.Application.Services;

/// <summary>
/// 文档恢复服务 - 处理服务重启后未完成的文档
/// </summary>
public class DocumentRecoveryService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentRecoveryService> _logger;

    public DocumentRecoveryService(
        IServiceProvider serviceProvider,
        ILogger<DocumentRecoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 应用启动时执行
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 [文档恢复服务] 开始扫描未完成的文档...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            // 1. 查找所有 Parsing 状态的文档
            var parsingDocuments = await documentRepository.GetByStatusAsync(
                DocumentStatus.Parsing, 
                cancellationToken);

            if (!parsingDocuments.Any())
            {
                _logger.LogInformation("✅ [文档恢复服务] 没有需要恢复的文档");
                return;
            }

            _logger.LogWarning("⚠️ [文档恢复服务] 发现 {Count} 个未完成的文档，开始恢复...", 
                parsingDocuments.Count());

            // 2. 检查是否超时（超过30分钟）
            var now = DateTime.UtcNow;
            var timeoutThreshold = TimeSpan.FromMinutes(30);

            var timedOutDocuments = parsingDocuments
                .Where(d => (now - d.CreateTime) > timeoutThreshold)
                .ToList();

            if (timedOutDocuments.Any())
            {
                _logger.LogWarning("🔴 [文档恢复服务] {Count} 个文档处理超时（>30分钟），标记为失败", 
                    timedOutDocuments.Count);

                foreach (var doc in timedOutDocuments)
                {
                    doc.Status = DocumentStatus.Failed;
                    doc.UpdateTime = DateTime.UtcNow;
                    await documentRepository.UpdateAsync(doc);
                    _logger.LogError("❌ [文档恢复] 文档超时：{DocId} - {FileName}", 
                        doc.Id, doc.Name);
                }
            }

            // 3. 恢复未超时的文档
            var documentsToRecover = parsingDocuments
                .Where(d => (now - d.CreateTime) <= timeoutThreshold)
                .ToList();

            if (!documentsToRecover.Any())
            {
                _logger.LogInformation("✅ [文档恢复服务] 恢复完成");
                return;
            }

            _logger.LogInformation("🔄 [文档恢复服务] 开始重新处理 {Count} 个文档...", 
                documentsToRecover.Count);

            // 4. 重新提交到处理队列
            var documentUseCase = scope.ServiceProvider.GetRequiredService<IDocumentUseCase>();

            foreach (var document in documentsToRecover)
            {
                try
                {
                    _logger.LogInformation("🔄 [文档恢复] 重新处理文档：{DocId} - {FileName}", 
                        document.Id, document.Name);

                    // 从存储读取文件
                    var filePath = GetDocumentFilePath(document);
                    if (!File.Exists(filePath))
                    {
                        _logger.LogError("❌ [文档恢复] 文件不存在：{FilePath}", filePath);
                        document.Status = DocumentStatus.Failed;
                        await documentRepository.UpdateAsync(document);
                        continue;
                    }

                    // 重新处理
                    using var fileStream = File.OpenRead(filePath);
                    await documentUseCase.ReprocessDocumentAsync(
                        document.Id, 
                        fileStream, 
                        cancellationToken);

                    _logger.LogInformation("✅ [文档恢复] 文档已重新提交处理：{DocId}", document.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [文档恢复] 恢复文档失败：{DocId} - {Error}", 
                        document.Id, ex.Message);
                    
                    // 标记为失败
                    document.Status = DocumentStatus.Failed;
                    await documentRepository.UpdateAsync(document);
                }
            }

            _logger.LogInformation("✅ [文档恢复服务] 恢复流程完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [文档恢复服务] 执行失败：{Error}", ex.Message);
        }
    }

    /// <summary>
    /// 应用停止时执行
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 [文档恢复服务] 停止");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取文档存储路径
    /// </summary>
    private string GetDocumentFilePath(Domain.Entities.Document document)
    {
        var storagePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Uploads");
        
        var extension = Path.GetExtension(document.Name);
        return Path.Combine(storagePath, $"{document.Id}{extension}");
    }
}
