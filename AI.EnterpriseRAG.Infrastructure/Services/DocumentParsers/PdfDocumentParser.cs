using AI.EnterpriseRAG.Domain.Interfaces.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

public class PdfDocumentParser : IDocumentParser
{
    // 支持的文件类型标识
    public string SupportedFileType => "pdf";

    /// <summary>
    /// 异步解析 PDF 流并提取文本
    /// </summary>
    /// <param name="stream">PDF 文件流（需保持可读状态）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>提取的文本内容</returns>
    /// <exception cref="ArgumentNullException">流为空时抛出</exception>
    /// <exception cref="PdfException">PDF 解析失败时抛出</exception>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // 参数校验
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "PDF 流不能为空");
        if (!stream.CanRead)
            throw new ArgumentException("PDF 流不可读", nameof(stream));

        // 异步执行解析（避免阻塞线程池）
        return await Task.Run(() =>
        {
            var content = new StringBuilder();

            // 使用 using 确保 PdfReader 资源释放
            using (var pdfReader = new PdfReader(stream))
            {
                // 禁用内存限制以支持大文件解析
                var pdfDocument = new PdfDocument(pdfReader);

                // 遍历所有页面
                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    // 检查取消令牌
                    cancellationToken.ThrowIfCancellationRequested();

                    // 提取页面文本
                    var strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(pageNum), strategy);
                    content.AppendLine(pageText);
                }

                pdfDocument.Close();
            }

            return content.ToString();
        }, cancellationToken);
    }
}