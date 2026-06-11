using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Globalization;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// Enterprise-grade Excel parser using NPOI (Apache 2.0)
/// ✅ Supports both .xls (legacy) and .xlsx (modern) formats
/// ✅ Extracts sheets, rows, cells with formulas and formatting
/// ✅ Perfect for financial reports, data tables, inventories
/// </summary>
public class NpoiExcelParser : IDocumentParser
{
    private readonly ILogger<NpoiExcelParser> _logger;

    public string SupportedFileType => "xlsx"; // Also handles .xls

    public NpoiExcelParser(ILogger<NpoiExcelParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse Excel spreadsheet (.xls or .xlsx) and extract structured data
    /// </summary>
    /// <param name="stream">Excel file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted data in markdown table format</returns>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "Excel stream cannot be null");
        if (!stream.CanRead)
            throw new ArgumentException("Excel stream is not readable", nameof(stream));

        try
        {
            return await Task.Run(() =>
            {
                IWorkbook? workbook = null;

                // Auto-detect Excel format
                try
                {
                    workbook = new XSSFWorkbook(stream); // Try .xlsx first
                }
                catch (Exception)
                {
                    _logger.LogDebug("Trying legacy .xls format");
                    stream.Position = 0;
                    workbook = new HSSFWorkbook(stream); // Fallback to .xls
                }

                if (workbook == null)
                    throw new InvalidDataException("Unable to open Excel file");

                var contentBuilder = new StringBuilder();

                _logger.LogInformation("📊 Parsing Excel: {Sheets} sheets detected", workbook.NumberOfSheets);

                // Extract workbook metadata
                contentBuilder.AppendLine("=== Excel Workbook ===");
                contentBuilder.AppendLine($"Total Sheets: {workbook.NumberOfSheets}");
                contentBuilder.AppendLine("========================\n");

                // Process each sheet
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sheet = workbook.GetSheetAt(sheetIndex);
                    if (sheet == null)
                        continue;

                    var sheetText = ParseSheet(sheet, cancellationToken);
                    contentBuilder.AppendLine(sheetText);
                    contentBuilder.AppendLine();
                }

                var finalText = contentBuilder.ToString();
                _logger.LogInformation("✅ Excel parsing complete: {Length} characters", finalText.Length);
                return finalText;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Excel parsing cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to parse Excel document");
            throw new InvalidDataException($"Failed to parse Excel: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parse individual Excel sheet
    /// </summary>
    private string ParseSheet(ISheet sheet, CancellationToken cancellationToken)
    {
        var sheetBuilder = new StringBuilder();

        sheetBuilder.AppendLine($"## Sheet: {sheet.SheetName}");
        sheetBuilder.AppendLine();

        // Skip empty sheets
        if (sheet.PhysicalNumberOfRows == 0)
        {
            sheetBuilder.AppendLine("*(Empty sheet)*");
            return sheetBuilder.ToString();
        }

        _logger.LogDebug("Processing sheet '{Name}' with {Rows} rows", sheet.SheetName, sheet.PhysicalNumberOfRows);

        // Find column boundaries (detect actual data range)
        int minCol = int.MaxValue;
        int maxCol = int.MinValue;

        for (int rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null) continue;

            minCol = Math.Min(minCol, row.FirstCellNum);
            maxCol = Math.Max(maxCol, row.LastCellNum - 1);
        }

        // Extract rows in markdown table format
        bool isFirstRow = true;
        int processedRows = 0;
        const int maxRowsToExtract = 1000; // Limit for very large spreadsheets

        for (int rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum && processedRows < maxRowsToExtract; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = sheet.GetRow(rowIndex);
            if (row == null)
            {
                sheetBuilder.AppendLine("| *(empty row)* |");
                continue;
            }

            var cells = new List<string>();

            // Extract all cells in the row
            for (int colIndex = minCol; colIndex <= maxCol; colIndex++)
            {
                var cell = row.GetCell(colIndex);
                var cellValue = ExtractCellValue(cell);
                cells.Add(cellValue);
            }

            // Write table row
            sheetBuilder.AppendLine($"| {string.Join(" | ", cells)} |");

            // Add table header separator after first row (assume first row is header)
            if (isFirstRow)
            {
                var separator = string.Join(" | ", Enumerable.Repeat("---", cells.Count));
                sheetBuilder.AppendLine($"| {separator} |");
                isFirstRow = false;
            }

            processedRows++;
        }

        if (processedRows >= maxRowsToExtract && sheet.PhysicalNumberOfRows > maxRowsToExtract)
        {
            sheetBuilder.AppendLine($"\n*(Truncated: showing first {maxRowsToExtract} of {sheet.PhysicalNumberOfRows} rows)*");
        }

        return sheetBuilder.ToString();
    }

    /// <summary>
    /// Extract cell value with type handling
    /// </summary>
    private string ExtractCellValue(ICell? cell)
    {
        if (cell == null)
            return string.Empty;

        try
        {
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue.Trim();

                case CellType.Numeric:
                    // Handle dates (stored as numeric in Excel)
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        var dateValue = cell.DateCellValue;
                        return $"{dateValue:yyyy-MM-dd}"; // String interpolation for formatting
                    }
                    return cell.NumericCellValue.ToString();

                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();

                case CellType.Formula:
                    // Try to get cached formula result
                    try
                    {
                        return cell.NumericCellValue.ToString("G");
                    }
                    catch
                    {
                        try
                        {
                            return cell.StringCellValue;
                        }
                        catch
                        {
                            return $"[Formula: {cell.CellFormula}]";
                        }
                    }

                case CellType.Error:
                    return "[ERROR]";

                case CellType.Blank:
                default:
                    return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract cell value at {Address}", cell.Address);
            return "[Error]";
        }
    }
}
