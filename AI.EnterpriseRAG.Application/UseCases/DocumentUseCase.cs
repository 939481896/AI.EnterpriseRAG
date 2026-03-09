using AI.EnterpriseRAG.Core.Exceptions;
using AI.EnterpriseRAG.Core.Extensions;
using AI.EnterpriseRAG.Core.Utils;
using AI.EnterpriseRAG.Domain.Entities;
using AI.EnterpriseRAG.Domain.Enums;
using AI.EnterpriseRAG.Domain.Interfaces.Repositories;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using AI.EnterpriseRAG.Domain.Interfaces.UseCases;
using AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers; // 新增：引入分块服务
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Document = AI.EnterpriseRAG.Domain.Entities.Document;

namespace AI.EnterpriseRAG.Application.UseCases;

/// <summary>
/// 文档用例实现（企业级业务编排）- 适配新分块逻辑
/// </summary>
public class DocumentUseCase : IDocumentUseCase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IEnumerable<IDocumentParser> _documentParsers;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentUseCase> _logger;
    private readonly DocumentChunkingService _chunkingService; // 新增：分块服务

    // 企业级文件存储路径
    private readonly string _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");

    public DocumentUseCase(
        IDocumentRepository documentRepository,
        IEnumerable<IDocumentParser> documentParsers,
        IServiceProvider serviceProvider,
        ILogger<DocumentUseCase> logger)
    {
        _documentRepository = documentRepository;
        _documentParsers = documentParsers;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _chunkingService = new DocumentChunkingService(); // 初始化分块服务

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

    public async Task<Guid> UploadAndProcessDocumentAsync(string fileName, string fileType, long fileSize, Stream stream, CancellationToken cancellationToken = default)
    {
        // 1. 企业级参数校验
        if (string.IsNullOrEmpty(fileName))
            throw new BusinessException("文件名不能为空");

        if (string.IsNullOrEmpty(fileType))
            throw new BusinessException("文件类型不能为空");

        fileType = fileType.TrimStart('.').ToLower();

        // 2. 检查支持的解析器
        var parser = _documentParsers.FirstOrDefault(p => p.SupportedFileType == fileType);
        if (parser == null)
            throw new BusinessException($"不支持的文件类型：{fileType}，仅支持pdf/txt");

        // 3. 创建文档实体
        var document = new Document
        {
            Name = fileName,
            FileType = fileType,
            FileSize = fileSize,
            Status = DocumentStatus.Parsing
        };

        // 4. 保存文件到本地
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

        // 5. 保存文档记录
        await _documentRepository.AddAsync(document);

        // 6. 异步处理文档（手动创建独立Scope）
        _ = Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                var llmService = scope.ServiceProvider.GetRequiredService<ILlmService>();
                var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
                var docRepo = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                // 6.1 解析文档
                _logger.LogInformation("开始解析文档：{DocumentId}", document.Id);
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var rawContent = await parser.ParseAsync(fileStream); // 原始文本（含页眉页脚）

                // ========== 核心：企业级分块流程 ==========
                // 6.2.1 清洗文本（去页眉/页脚/版权/页码）
                var cleanContent = DocumentCleaner.CleanDocumentText(rawContent);

                // 6.2.2 判断是否启用单词级Token计数
                bool useWordBasedToken = IsEnglishContent(cleanContent);

                // 6.2.3 生成结构化语义分块（最终调用SplitIntoChunks）
                var semanticChunks = _chunkingService.CreateSemanticChunks(
                    cleanText: cleanContent,
                    fileName: fileName,
                    fileType: fileType,
                    useWordBasedToken: useWordBasedToken);

                // 6.2.4 转换为你的DocumentChunk实体
                var chunks = semanticChunks.Select((sc, index) => new DocumentChunk
                {
                    DocumentId = document.Id,
                    Content = sc.Content,
                    Index = index,
                    TokenCount = useWordBasedToken
                        ? TokenCounter.EstimateTokenCount(sc.Content, true)
                        : TokenCounter.EstimateTokenCount(sc.Content),
                    // SectionTitle = sc.SectionTitle
                }).ToList();
                // ========== 核心修改结束 ==========

                _logger.LogInformation("文档{DocumentId}解析完成，分块数：{ChunkCount}", document.Id, chunks.Count);

                // 6.3 初始化向量库
                await vectorStore.InitAsync();

                // 6.4 逐块向量化并入库
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = chunks[i];
                    if (string.IsNullOrWhiteSpace(chunk.Content))
                        continue;

                    // 生成向量（使用新Scope的llmService）
                    var vector = await llmService.EmbeddingAsync(chunk.Content);
                    if (vector == null || vector.Length == 0)
                    {
                        _logger.LogWarning("文档{DocumentId}分块{Index}向量生成失败", document.Id, i);
                        continue;
                    }

                    // 存入向量库
                    await vectorStore.InsertAsync(chunk, vector);

                    // 保存分块记录（使用新Scope的repo）
                    await docRepo.AddChunkAsync(chunk);

                    _logger.LogInformation("文档{DocumentId}分块{Index}处理完成", document.Id, i);
                }

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