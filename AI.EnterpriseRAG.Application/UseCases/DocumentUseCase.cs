using AI.EnterpriseRAG.Application.Services; // 新增：并发控制
using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Extensions;
using AI.EnterpriseRAG.Core.Utils;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Infrastructure.Persistence;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Document = AI.EnterpriseRAG.Domain.Entities.Document;

namespace AI.EnterpriseRAG.Application.UseCases;

/// <summary>
/// 文档用例实现（企业级业务编排）- 适配新分块逻辑 + 大文档优化
/// </summary>
public partial class DocumentUseCase : IDocumentUseCase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly AppEnterpriseAiContext _context;
    private readonly IEnumerable<IDocumentParser> _documentParsers;
    private readonly IServiceScopeFactory _serviceScopeFactory; // ✅ Changed from IServiceProvider to IServiceScopeFactory
    private readonly ILogger<DocumentUseCase> _logger;
    private readonly DocumentChunkingService _chunkingService;
    private readonly DocumentProcessingThrottler _throttler; // 新增：并发控制

    // 企业级文件存储路径
    private readonly string _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");

    public DocumentUseCase(
        IDocumentRepository documentRepository,
        AppEnterpriseAiContext context,
        IEnumerable<IDocumentParser> documentParsers,
        IServiceScopeFactory serviceScopeFactory, // ✅ Changed from IServiceProvider to IServiceScopeFactory
        ILogger<DocumentUseCase> logger,
        DocumentProcessingThrottler throttler) // 新增：注入并发控制
    {
        _documentRepository = documentRepository;
        _context = context;
        _documentParsers = documentParsers;
        _serviceScopeFactory = serviceScopeFactory; // ✅ Changed
        _logger = logger;
        _chunkingService = new DocumentChunkingService();
        _throttler = throttler; // 新增

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task DeleteByCollectionNameAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope(); // ✅ Use scope factory
        try
        {
            var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
            await vectorStore.ClearAllAsync(cancellationToken);
            _logger.LogInformation("✅ 集合清空处理完成：{CollectionId}", collectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 集合清空失败：{CollectionId}", collectionId);
            throw;
        }
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await DeleteDocumentInternalAsync(documentId, cancellationToken);
    }

    /// <summary>
    /// 内部删除文档方法（删除文件、数据库记录、向量数据）
    /// </summary>
    private async Task DeleteDocumentInternalAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ [删除文档] 开始删除文档：{DocumentId}", documentId);

        try
        {
            // 1. Get document info
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
            {
                _logger.LogWarning("文档不存在：{DocumentId}", documentId);
                return;
            }

            // 2. Delete physical file
            var fileExtension = Path.GetExtension(document.Name);
            var filePath = Path.Combine(_storagePath, $"{documentId}{fileExtension}");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("✅ 文件删除成功：{FilePath}", filePath);
            }

            // 3. Delete vector data
            using var scope = _serviceScopeFactory.CreateScope(); // ✅ Use scope factory
            var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
            await vectorStore.DeleteByDocumentIdAsync(documentId, cancellationToken);
            _logger.LogInformation("✅ 向量数据删除成功：{DocumentId}", documentId);

            // 4. Delete chunks from database
            await _documentRepository.DeleteChunksByDocumentIdAsync(documentId, cancellationToken);
            _logger.LogInformation("✅ 分块数据删除成功：{DocumentId}", documentId);

            // 5. Delete document record using context directly
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("✅ 文档记录删除成功：{DocumentId}", documentId);

            _logger.LogInformation("✅ [删除文档] 文档删除完成：{DocumentId}", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [删除文档] 删除文档失败：{DocumentId}", documentId);
            throw;
        }
    }

    public async Task<Guid> UploadAndProcessDocumentAsync(
        string fileName, 
        string fileType, 
        long fileSize, 
        Stream stream, 
        string uploadedBy,      // 🆕 上传者
        string? tenantId = null, // 🆕 租户ID（可选）
        CancellationToken cancellationToken = default)
    {
        // 1. 企业级参数校验
        if (string.IsNullOrEmpty(fileName))
            throw new BusinessException("文件名不能为空");

        if (string.IsNullOrEmpty(fileType))
            throw new BusinessException("文件类型不能为空");

        if (string.IsNullOrEmpty(uploadedBy))
            throw new BusinessException("上传者不能为空");

        fileType = fileType.TrimStart('.').ToLower();

        // 2. 检查支持的解析器
        var parser = _documentParsers.FirstOrDefault(p => p.SupportedFileType == fileType);
        if (parser == null)
            throw new BusinessException($"不支持的文件类型：{fileType}，仅支持pdf/txt");

        // 🆕 3. 计算文件哈希（用于重复检测）
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
            throw new BusinessException($"计算文件哈希失败：{ex.Message}");
        }

        // 🆕 4. 检查是否重复上传（同一用户/租户 + 相同哈希）
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
                return existingDoc.Id;
            }
            else
            {
                // ✅ Allow re-upload if previous attempt failed or is still processing
                _logger.LogWarning(
                    "检测到重复文件但状态为{Status}，将删除旧记录并重新上传：{FileName}",
                    existingDoc.Status, fileName);

                // Delete failed/incomplete document and allow re-upload
                await DeleteDocumentInternalAsync(existingDoc.Id, cancellationToken);
            }
        }

        // 5. 创建文档实体（包含权限信息和哈希）
        var document = new Document
        {
            Name = fileName,
            FileType = fileType,
            FileSize = fileSize,
            FileHash = fileHash,            // 🆕 保存哈希
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件保存失败：{FileName}", fileName);
            throw new BusinessException("文件保存失败：" + ex.Message);
        }

        // 7. 保存文档记录
        await _documentRepository.AddAsync(document);

        _logger.LogInformation(
            "文档创建成功：ID={DocumentId}，名称={Name}，哈希={Hash}",
            document.Id, fileName, fileHash);

        // 8. 异步处理文档（带并发控制 + 独立Scope）
        _ = Task.Run(async () =>
        {
            // 6.0 获取处理槽位（阻塞直到有空闲槽位）
            using var slot = await _throttler.AcquireAsync(document.Id);
            using var scope = _serviceScopeFactory.CreateScope(); // ✅ Use scope factory
            try
            {
                var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                // ========== Core: Use .NET Native Parsers (Replaced Unstructured) ==========
                // 6.1 Parse document using appropriate .NET parser
                _logger.LogInformation("Starting document parsing: {DocumentId}", document.Id);
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                // Get the appropriate parser for this file type
                var documentParser = _documentParsers.FirstOrDefault(p => p.SupportedFileType == fileType);
                if (documentParser == null)
                {
                    throw new BusinessException($"No parser found for file type: {fileType}");
                }

                // Parse document to extract text
                var rawContent = await documentParser.ParseAsync(fileStream, cancellationToken);
                _logger.LogInformation("Document parsing complete, content length: {Length} characters", rawContent.Length);

                // 6.3 Clean text (remove headers/footers/copyright/page numbers)
                var cleanContent = DocumentCleaner.CleanDocumentText(rawContent);

                // Optimization: Release memory early for large documents
                rawContent = null;
                GC.Collect(0, GCCollectionMode.Optimized);

                // 6.4 判断是否启用单词级Token计数
                bool useWordBasedToken = IsEnglishContent(cleanContent);

                // 6.5 使用自定义分块重新分割（精确控制chunk大小）
                var semanticChunks = _chunkingService.CreateSemanticChunks(
                    cleanText: cleanContent,
                    fileName: fileName,
                    fileType: fileType,
                    chunkSize: 250,        // ✅ 250字符 ≈ 125 tokens（适合语义检索）
                    overlapSize: 50,       // ✅ 20%重叠窗口（避免边界信息丢失）
                    useWordBasedToken: useWordBasedToken);

                // 6.6 转换为DocumentChunk实体（保留元数据）
                var chunks = semanticChunks.Select((sc, index) => new DocumentChunk
                {
                    DocumentId = document.Id,
                    Content = sc.Content,
                    Index = index,
                    TokenCount = useWordBasedToken
                        ? TokenCounter.EstimateTokenCount(sc.Content, true)
                        : TokenCounter.EstimateTokenCount(sc.Content),
                    SectionTitle = sc.SectionTitle,  // ✅ 保留章节标题
                    SectionLevel = sc.SectionLevel   // ✅ 保留标题层级
                }).ToList();

                /* 
                🔹 .NET Native Parsing Approach:
                1. Use appropriate .NET parser based on file type (PdfPig, NPOI, Markdig, etc.)
                2. Extract complete text with structure preservation
                3. Use DocumentChunkingService for semantic chunking (precise 250-character control)
                4. Advantages: No Python dependencies + Better performance + Enterprise licenses
                */
                _logger.LogInformation("Document {DocumentId} parsing complete, chunk count: {ChunkCount}", document.Id, chunks.Count);

                // 6.3 初始化向量库
                await vectorStore.InitAsync();

                // 6.4 批量向量化并入库（提升性能）
                var batchSize = 10; // 每批处理10个chunk
                var startTime = DateTime.Now;
                var processedCount = 0;

                for (int i = 0; i < chunks.Count; i += batchSize)
                {
                    var batch = chunks.Skip(i).Take(batchSize).ToList();
                    var validBatch = batch.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();

                    if (!validBatch.Any())
                        continue;

                    // 批量生成向量（并发）
                    var vectorTasks = validBatch.Select(c => llmService.EmbeddingAsync(c.Content)).ToArray();
                    var vectors = await Task.WhenAll(vectorTasks);

                    // 批量存储
                    for (int j = 0; j < validBatch.Count; j++)
                    {
                        var chunk = validBatch[j];
                        var vector = vectors[j];

                        if (vector == null || vector.Length == 0)
                        {
                            _logger.LogWarning("文档{DocumentId}分块{Index}向量生成失败", document.Id, i + j);
                            continue;
                        }

                        // 存入向量库
                        await vectorStore.InsertAsync(chunk, vector);

                        // 保存分块记录
                        await docRepo.AddChunkAsync(chunk);

                        processedCount++;
                    }

                    _logger.LogInformation("文档{DocumentId}处理进度：{Processed}/{Total} ({Percent:F1}%)",
                        document.Id, processedCount, chunks.Count, (double)processedCount / chunks.Count * 100);

                    // 每批处理后强制GC（避免内存累积）
                    if (i % (batchSize * 5) == 0)
                    {
                        GC.Collect(1, GCCollectionMode.Optimized);
                    }
                }

                var elapsed = DateTime.Now - startTime;
                _logger.LogInformation("文档{DocumentId}向量化完成，耗时：{Elapsed}秒", document.Id, elapsed.TotalSeconds);

                // 6.5 更新文档状态
                document.Status = DocumentStatus.Vectorized;
                document.CompleteTime = DateTime.Now;
                await docRepo.UpdateAsync(document);

                _logger.LogInformation("文档{DocumentId}处理完成", document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档{DocumentId}处理失败", document.Id);
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                document.Status = DocumentStatus.Failed;
                document.CompleteTime = DateTime.Now;
                await docRepo.UpdateAsync(document);
            }
        }, CancellationToken.None);

        return document.Id;
    }

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

    /// <summary>
    /// 简单判断文本是否以英文为主
    /// </summary>
    private bool IsEnglishContent(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // 统计中英文字符比例
        int englishCharCount = text.Count(c => char.IsLetter(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c));
        int chineseCharCount = text.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        int totalValidChars = englishCharCount + chineseCharCount;

        if (totalValidChars == 0)
            return false;

        // 英文占比超过60%则启用单词级Token计数
        return (double)englishCharCount / totalValidChars > 0.6;
    }
}