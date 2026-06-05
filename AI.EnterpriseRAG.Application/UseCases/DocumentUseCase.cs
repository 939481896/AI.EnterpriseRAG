using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Extensions;
using AI.EnterpriseRAG.Core.Utils;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;
using AI.EnterpriseRAG.Application.Services; // 新增：并发控制
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
    private readonly IEnumerable<IDocumentParser> _documentParsers;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentUseCase> _logger;
    private readonly DocumentChunkingService _chunkingService;
    private readonly DocumentProcessingThrottler _throttler; // 新增：并发控制

    // 企业级文件存储路径
    private readonly string _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");

    public DocumentUseCase(
        IDocumentRepository documentRepository,
        IEnumerable<IDocumentParser> documentParsers,
        IServiceProvider serviceProvider,
        ILogger<DocumentUseCase> logger,
        DocumentProcessingThrottler throttler) // 新增：注入并发控制
    {
        _documentRepository = documentRepository;
        _documentParsers = documentParsers;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _chunkingService = new DocumentChunkingService();
        _throttler = throttler; // 新增

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task DeleteByCollectionNameAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        try
        {
            var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
            var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
            var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

            await vectorStore.ClearAllAsync(cancellationToken);

            _logger.LogInformation("集合清空处理完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "集合清空失败");
        }
        throw new NotImplementedException();
    }

    public Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
            _logger.LogInformation(
                "检测到重复文件：{FileName}，哈希：{Hash}，已存在文档ID：{ExistingId}",
                fileName, fileHash, existingDoc.Id);

            // 返回已有文档ID（避免重复处理）
            return existingDoc.Id;
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
            using var scope = _serviceProvider.CreateScope();
            try
            {
                var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                // ========== 核心：混合方案（Unstructured解析 + 自定义分块）==========
                // 6.1 使用Unstructured解析文档（支持多格式：PDF/Word/Excel/PPT...）
                _logger.LogInformation("开始解析文档：{DocumentId}", document.Id);
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var unstructuredClient = scope.ServiceProvider.GetRequiredService<UnstructuredClient>();
                var unstructuredChunks = await unstructuredClient.ParseDocumentAsync(fileStream, document.Name);

                // 6.2 优化：使用StringBuilder流式合并（避免内存峰值）
                var contentBuilder = new System.Text.StringBuilder(unstructuredChunks.Count * 800); // 预分配容量
                foreach (var chunk in unstructuredChunks)
                {
                    if (!string.IsNullOrWhiteSpace(chunk.Content))
                    {
                        contentBuilder.Append(chunk.Content);
                        contentBuilder.Append("\n\n");
                    }
                }
                var rawContent = contentBuilder.ToString();
                _logger.LogInformation("文档解析完成，原始内容长度：{Length}字符", rawContent.Length);

                // 6.3 清洗文本（去页眉/页脚/版权/页码）
                var cleanContent = DocumentCleaner.CleanDocumentText(rawContent);

                // 优化：大文档提前释放内存
                contentBuilder.Clear();
                contentBuilder = null;
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
                🔹 混合方案说明：
                1. Unstructured负责文档解析（支持PDF、Word、Excel、PPT等多格式）
                2. 合并所有Unstructured返回的chunk为完整文本
                3. 使用DocumentChunkingService重新分块（精确控制250字符）
                4. 优势：保留多格式支持 + 精确控制Chunk大小
                */
                _logger.LogInformation("文档{DocumentId}解析完成，分块数：{ChunkCount}", document.Id, chunks.Count);

                // 6.3 初始化向量库
                await vectorStore.InitAsync();

                // 6.4 优化：批量向量化并入库（提升性能）
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

                    // 优化：每批处理后强制GC（避免内存累积）
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