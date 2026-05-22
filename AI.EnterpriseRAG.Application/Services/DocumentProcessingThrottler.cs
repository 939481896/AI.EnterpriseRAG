using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AI.EnterpriseRAG.Application.Services;

/// <summary>
/// 文档处理并发控制服务（避免系统过载）
/// </summary>
public class DocumentProcessingThrottler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _processingTasks;
    private readonly ILogger<DocumentProcessingThrottler> _logger;

    /// <summary>
    /// 最大并发处理文档数
    /// </summary>
    public int MaxConcurrency { get; }

    public DocumentProcessingThrottler(
        ILogger<DocumentProcessingThrottler> logger,
        int maxConcurrency = 3) // 默认最多同时处理3个文档
    {
        MaxConcurrency = maxConcurrency;
        _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _processingTasks = new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>();
        _logger = logger;
    }

    /// <summary>
    /// 获取当前正在处理的文档数
    /// </summary>
    public int CurrentProcessing => MaxConcurrency - _semaphore.CurrentCount;

    /// <summary>
    /// 请求处理许可（阻塞直到有空闲槽位）
    /// </summary>
    public async Task<IDisposable> AcquireAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("文档{DocumentId}请求处理槽位，当前正在处理：{Current}/{Max}",
            documentId, CurrentProcessing, MaxConcurrency);

        await _semaphore.WaitAsync(cancellationToken);

        var tcs = new TaskCompletionSource<bool>();
        _processingTasks.TryAdd(documentId, tcs);

        _logger.LogInformation("文档{DocumentId}获得处理槽位，当前正在处理：{Current}/{Max}",
            documentId, CurrentProcessing, MaxConcurrency);

        return new ProcessingSlot(this, documentId);
    }

    private void Release(Guid documentId)
    {
        _semaphore.Release();
        _processingTasks.TryRemove(documentId, out _);

        _logger.LogInformation("文档{DocumentId}释放处理槽位，当前正在处理：{Current}/{Max}",
            documentId, CurrentProcessing, MaxConcurrency);
    }

    /// <summary>
    /// 处理槽位（IDisposable，自动释放）
    /// </summary>
    private class ProcessingSlot : IDisposable
    {
        private readonly DocumentProcessingThrottler _throttler;
        private readonly Guid _documentId;
        private bool _disposed = false;

        public ProcessingSlot(DocumentProcessingThrottler throttler, Guid documentId)
        {
            _throttler = throttler;
            _documentId = documentId;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _throttler.Release(_documentId);
                _disposed = true;
            }
        }
    }
}
