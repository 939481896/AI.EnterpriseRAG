using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Extensions;
using AI.EnterpriseRAG.Core.Utils;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AI.EnterpriseRAG.Application.UseCases;

/// <summary>
/// 文档上传优化版（带重复检测）
/// </summary>
public partial class DocumentUseCase
{
    /// <summary>
    /// 优化后的上传方法（带重复检测）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="fileSize">文件大小</param>
    /// <param name="stream">文件流</param>
    /// <param name="uploadedBy">上传者</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="allowDuplicate">是否允许重复上传（默认false）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文档ID和是否为新文档</returns>
    public async Task<(Guid DocumentId, bool IsNew, string Message)> UploadAndProcessDocumentAsyncV2(
        string fileName, 
        string fileType, 
        long fileSize, 
        Stream stream, 
        string uploadedBy,
        string? tenantId = null,
        bool allowDuplicate = false,
        CancellationToken cancellationToken = default)
    {
        // 1. 参数校验
        if (string.IsNullOrEmpty(fileName))
            throw new BusinessException(400, "文件名不能为空");
        
        if (string.IsNullOrEmpty(fileType))
            throw new BusinessException(400, "文件类型不能为空");
        
        if (string.IsNullOrEmpty(uploadedBy))
            throw new BusinessException(400, "上传者不能为空");
        
        fileType = fileType.TrimStart('.').ToLower();
        
        // 2. 检查文件类型支持
        var parser = _documentParsers.FirstOrDefault(p => p.SupportedFileType == fileType);
        if (parser == null)
            throw new BusinessException(400, $"不支持的文件类型：{fileType}，仅支持pdf/txt/docx");
        
        // 3. 计算文件哈希（用于重复检测）
        string fileHash;
        try
        {
            _logger.LogDebug("开始计算文件哈希：{FileName}", fileName);
            fileHash = await FileHasher.ComputeMD5Async(stream, resetPosition: true);
            _logger.LogInformation("文件哈希计算完成：{Hash}", fileHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算文件哈希失败");
            throw new BusinessException(500, $"计算文件哈希失败：{ex.Message}");
        }
        
        // 4. 检查是否重复上传（同一用户/租户 + 相同哈希）
        if (!allowDuplicate)
        {
            var existingDoc = await _documentRepository.GetByFileHashAsync(
                fileHash, 
                uploadedBy, 
                tenantId, 
                cancellationToken);

            if (existingDoc != null)
            {
                // ✅ Only return existing if it's successfully processed
                if (existingDoc.Status == DocumentStatus.Vectorized)
                {
                    _logger.LogInformation(
                        "检测到重复文件（已处理成功）：{FileName}，哈希：{Hash}，已存在文档ID：{ExistingId}",
                        fileName, fileHash, existingDoc.Id);

                    // 返回已有文档
                    return (
                        existingDoc.Id, 
                        IsNew: false, 
                        Message: $"文件已存在（上传时间：{existingDoc.CreateTime:yyyy-MM-dd HH:mm}），无需重复上传"
                    );
                }
                else
                {
                    // ✅ Allow re-upload if previous attempt failed or is still processing
                    _logger.LogWarning(
                        "检测到重复文件但状态为{Status}，将删除旧记录并重新上传：{FileName}",
                        existingDoc.Status, fileName);

                    // Delete failed/incomplete document using internal delete
                    await DeleteDocumentInternalAsync(existingDoc.Id, cancellationToken);
                }
            }
        }
        
        // 5. 创建新文档实体
        var document = new Document
        {
            Name = fileName,
            FileType = fileType,
            FileSize = fileSize,
            FileHash = fileHash,         // 🆕 保存哈希
            Status = DocumentStatus.Parsing,
            UploadedBy = uploadedBy,
            TenantId = tenantId,
            IsPublic = false
        };
        
        // 6. 保存文件到本地
        var filePath = Path.Combine(_storagePath, $"{document.Id}.{fileType}");
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream, cancellationToken);
            document.StoragePath = filePath;
            
            _logger.LogInformation("文件保存成功：{FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件保存失败：{FileName}", fileName);
            throw new BusinessException(500, $"文件保存失败：{ex.Message}");
        }
        
        // 7. 保存数据库记录
        await _documentRepository.AddAsync(document);
        
        _logger.LogInformation(
            "文档创建成功：ID={DocumentId}，名称={Name}，哈希={Hash}",
            document.Id, fileName, fileHash);
        
        // 8. 触发后台处理（原有逻辑）
        _ = Task.Run(async () =>
        {
            using var slot = await _throttler.AcquireAsync(document.Id);
            await ProcessDocumentInternalAsync(document.Id, CancellationToken.None); // ✅ Pass ID, not entity
        }, CancellationToken.None);
        
        return (
            document.Id, 
            IsNew: true, 
            Message: "文档上传成功，正在后台处理中"
        );
    }
    
    /// <summary>
    /// 批量检测重复文件（上传前预检）
    /// </summary>
    /// <param name="files">文件列表（文件名+哈希）</param>
    /// <param name="uploadedBy">上传者</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns>重复文件列表</returns>
    public async Task<List<DuplicateFileInfo>> CheckDuplicateFilesAsync(
        List<FileHashInfo> files,
        string uploadedBy,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var duplicates = new List<DuplicateFileInfo>();
        
        foreach (var file in files)
        {
            var existingDoc = await _documentRepository.GetByFileHashAsync(
                file.FileHash, 
                uploadedBy, 
                tenantId, 
                cancellationToken);
            
            if (existingDoc != null)
            {
                duplicates.Add(new DuplicateFileInfo
                {
                    FileName = file.FileName,
                    FileHash = file.FileHash,
                    ExistingDocumentId = existingDoc.Id,
                    ExistingDocumentName = existingDoc.Name,
                    UploadTime = existingDoc.CreateTime
                });
            }
        }
        
        return duplicates;
    }
    
    /// <summary>
    /// 文档内部处理逻辑（从原有代码抽取）
    /// </summary>
    private async Task ProcessDocumentInternalAsync(
        Guid documentId, // ✅ Changed from Document to Guid to avoid EF tracking issues
        CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope(); // ✅ Use scope factory

        try
        {
            var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
            var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
            var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            // ✅ Retrieve document from new scope to avoid tracking issues
            var document = await docRepo.GetByIdAsync(documentId);
            if (document == null)
            {
                _logger.LogError("文档不存在：{DocumentId}", documentId);
                return;
            }

            // Get file path
            var fileExtension = Path.GetExtension(document.Name);
            var filePath = Path.Combine(_storagePath, $"{document.Id}{fileExtension}");

            if (!File.Exists(filePath))
            {
                throw new BusinessException(404, $"文档文件不存在：{filePath}");
            }

            // Parse document
            _logger.LogInformation("🔄 [ProcessInternal] Starting document parsing: {DocumentId}", documentId);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

            var parser = _documentParsers.FirstOrDefault(p => p.SupportedFileType == document.FileType);
            if (parser == null)
            {
                throw new BusinessException($"No parser found for file type: {document.FileType}");
            }

            var rawContent = await parser.ParseAsync(fileStream, cancellationToken);
            var cleanContent = DocumentCleaner.CleanDocumentText(rawContent);

            // 分块
            var semanticChunks = _chunkingService.CreateSemanticChunks(
                cleanContent,
                document.Name,
                document.FileType,
                chunkSize: 250,
                overlapSize: 50);

            // ✅ Convert semantic chunks to DocumentChunk entities with proper DocumentId
            var chunks = semanticChunks.Select((sc, index) => new DocumentChunk
            {
                DocumentId = document.Id, // ✅ Set DocumentId to avoid FK constraint error
                Content = sc.Content,
                Index = index,
                TokenCount = TokenCounter.EstimateTokenCount(sc.Content),
                SectionTitle = sc.SectionTitle,
                SectionLevel = sc.SectionLevel
            }).ToList();

            _logger.LogInformation("🔄 [ProcessInternal] 分块完成，共{Count}块", chunks.Count);

            // 向量化
            await vectorStore.InitAsync();
            var batchSize = 10;
            for (int i = 0; i < chunks.Count; i += batchSize)
            {
                var batch = chunks.Skip(i).Take(batchSize).ToList();
                var vectorTasks = batch.Select(c => llmService.EmbeddingAsync(c.Content));
                var vectors = await Task.WhenAll(vectorTasks);

                for (int j = 0; j < batch.Count; j++)
                {
                    await docRepo.AddChunkAsync(batch[j]);
                    await vectorStore.InsertAsync(batch[j], vectors[j]);
                }

                _logger.LogInformation("🔄 [ProcessInternal] 进度：{Processed}/{Total}", 
                    Math.Min(i + batchSize, chunks.Count), chunks.Count);
            }

            // 更新状态
            document.Status = DocumentStatus.Vectorized;
            document.CompleteTime = DateTime.Now;
            await docRepo.UpdateAsync(document);

            _logger.LogInformation("✅ [ProcessInternal] 文档处理完成：{DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [ProcessInternal] 文档处理失败：{DocumentId}", documentId);

            try
            {
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                var document = await docRepo.GetByIdAsync(documentId);
                if (document != null)
                {
                    document.Status = DocumentStatus.Failed;
                    document.CompleteTime = DateTime.Now;
                    await docRepo.UpdateAsync(document);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update document status to Failed for {DocumentId}", documentId);
            }
        }
    }

    /// <summary>
    /// 重新处理文档（用于恢复失败或中断的任务）
    /// </summary>
    public async Task ReprocessDocumentAsync(
        Guid documentId, 
        Stream? fileStream = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 [重新处理] 开始重新处理文档：{DocumentId}", documentId);

        // 1. 获取文档记录
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new BusinessException(404, $"文档不存在：{documentId}");
        }

        // 2. 如果没有提供流，从存储读取
        Stream? streamToProcess = fileStream;
        var needsCleanup = false;

        try
        {
            if (streamToProcess == null)
            {
                var extension = Path.GetExtension(document.Name);
                var filePath = Path.Combine(_storagePath, $"{document.Id}{extension}");

                if (!File.Exists(filePath))
                {
                    throw new BusinessException(404, $"文档文件不存在：{filePath}");
                }

                streamToProcess = File.OpenRead(filePath);
                needsCleanup = true;
            }

            // 3. 重置状态为 Parsing
            document.Status = DocumentStatus.Parsing;
            document.UpdateTime = DateTime.UtcNow;
            await _documentRepository.UpdateAsync(document);

            // 4. 清理旧的分块和向量（如果存在）
            _logger.LogInformation("🧹 [重新处理] 清理旧数据：{DocumentId}", documentId);
            await _documentRepository.DeleteChunksByDocumentIdAsync(documentId, cancellationToken);

            using (var scope = _serviceScopeFactory.CreateScope()) // ✅ Use scope factory
            {
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
                await vectorStore.DeleteByDocumentIdAsync(documentId, cancellationToken);
            }

            // 5. 重新提交到后台处理队列
            _logger.LogInformation("📤 [重新处理] 提交后台任务：{DocumentId}", documentId);

            // Capture necessary data before Task.Run
            var documentIdCopy = document.Id;
            var documentFileType = document.FileType;
            var documentName = document.Name;
            var fileExtension = Path.GetExtension(document.Name);
            var storagePath = Path.Combine(_storagePath, $"{document.Id}{fileExtension}");

            if (!File.Exists(storagePath))
            {
                throw new BusinessException(404, $"文档文件不存在：{storagePath}");
            }

            // 异步提交后台处理（不等待）
            _ = Task.Run(async () =>
            {
                IDisposable? processingSlot = null;
                IServiceScope? processingScope = null;

                try
                {
                    _logger.LogInformation("🔄 [Reprocess] Task.Run started for document: {DocumentId}", documentIdCopy);

                    processingSlot = await _throttler.AcquireAsync(documentIdCopy);
                    _logger.LogInformation("🔄 [Reprocess] Slot acquired for: {DocumentId}", documentIdCopy);

                    processingScope = _serviceScopeFactory.CreateScope(); // ✅ Use scope factory
                    _logger.LogInformation("🔄 [Reprocess] Scope created for: {DocumentId}", documentIdCopy);

                    var llmService = processingScope.ServiceProvider.GetRequiredService<ILlmService>();
                    var vectorStore = processingScope.ServiceProvider.GetRequiredService<IVectorStore>();
                    var docRepo = processingScope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                    _logger.LogInformation("🔄 [Reprocess] Services resolved for: {DocumentId}", documentIdCopy);

                    _logger.LogInformation("🔄 [Reprocess] Starting document parsing: {DocumentId}", documentIdCopy);

                    // ✅ Use .NET native parser (replaced Unstructured)
                    using var fileReadStream = new FileStream(storagePath, FileMode.Open, FileAccess.Read);

                    // Get appropriate parser for file type
                    var parser = _documentParsers.FirstOrDefault(p => p.SupportedFileType == documentFileType);
                    if (parser == null)
                    {
                        throw new BusinessException($"No parser found for file type: {documentFileType}");
                    }

                    // Parse document - use CancellationToken.None for background processing
                    var rawContent = await parser.ParseAsync(fileReadStream, CancellationToken.None);
                    var cleanContent = DocumentCleaner.CleanDocumentText(rawContent);

                    // 分块
                    var semanticChunks = _chunkingService.CreateSemanticChunks(
                        cleanContent,
                        documentName,
                        documentFileType,
                        chunkSize: 250,
                        overlapSize: 50);

                    // ✅ Convert semantic chunks to DocumentChunk entities with proper DocumentId
                    var chunks = semanticChunks.Select((sc, index) => new DocumentChunk
                    {
                        DocumentId = documentIdCopy, // ✅ Set DocumentId to avoid FK constraint error
                        Content = sc.Content,
                        Index = index,
                        TokenCount = TokenCounter.EstimateTokenCount(sc.Content),
                        SectionTitle = sc.SectionTitle,
                        SectionLevel = sc.SectionLevel
                    }).ToList();

                    _logger.LogInformation("🔄 [重新处理] 分块完成，共{Count}块", chunks.Count);

                    // 向量化
                    var batchSize = 10;
                    for (int i = 0; i < chunks.Count; i += batchSize)
                    {
                        var batch = chunks.Skip(i).Take(batchSize).ToList();
                        var vectorTasks = batch.Select(c => llmService.EmbeddingAsync(c.Content));
                        var vectors = await Task.WhenAll(vectorTasks);

                        for (int j = 0; j < batch.Count; j++)
                        {
                            await docRepo.AddChunkAsync(batch[j]);
                            await vectorStore.InsertAsync(batch[j], vectors[j]);
                        }

                        _logger.LogInformation("🔄 [重新处理] 进度：{Processed}/{Total}", 
                            Math.Min(i + batchSize, chunks.Count), chunks.Count);
                    }

                    // 更新状态
                    var documentToUpdate = await docRepo.GetByIdAsync(documentIdCopy);
                    documentToUpdate.Status = DocumentStatus.Vectorized;
                    documentToUpdate.CompleteTime = DateTime.Now;
                    await docRepo.UpdateAsync(documentToUpdate);

                    _logger.LogInformation("✅ [重新处理] 文档处理完成：{DocumentId}", documentIdCopy);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [重新处理] 文档处理失败：{DocumentId} | Error: {Message} | StackTrace: {StackTrace}", 
                        documentIdCopy, ex.Message, ex.StackTrace);

                    try
                    {
                        var docRepo = processingScope?.ServiceProvider.GetRequiredService<IDocumentRepository>();
                        if (docRepo != null)
                        {
                            var documentToUpdate = await docRepo.GetByIdAsync(documentIdCopy);
                            documentToUpdate.Status = DocumentStatus.Failed;
                            documentToUpdate.CompleteTime = DateTime.Now;
                            await docRepo.UpdateAsync(documentToUpdate);
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Failed to update document status to Failed for {DocumentId}", documentIdCopy);
                    }
                }
                finally
                {
                    processingScope?.Dispose();
                    processingSlot?.Dispose();
                    _logger.LogInformation("🔄 [Reprocess] Cleanup complete for: {DocumentId}", documentIdCopy);
                }
            }, CancellationToken.None); // ✅ Use None to prevent premature cancellation

            _logger.LogInformation("✅ [重新处理] 文档已重新提交：{DocumentId}", documentId);
        }
        finally
        {
            if (needsCleanup && streamToProcess != null)
            {
                await streamToProcess.DisposeAsync();
            }
        }
    }
}

/// <summary>
/// 文件哈希信息（用于批量检测）
/// </summary>
public class FileHashInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
}

/// <summary>
/// 重复文件信息
/// </summary>
public class DuplicateFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public Guid ExistingDocumentId { get; set; }
    public string ExistingDocumentName { get; set; } = string.Empty;
    public DateTime UploadTime { get; set; }
}
