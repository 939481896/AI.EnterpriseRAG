using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.DocumentParsers;

/// <summary>
/// CSV parser using CsvHelper (MS-PL/Apache 2.0)
/// ✅ High-performance CSV reading with automatic type detection
/// ✅ Handles various delimiters, quotes, escaping
/// ✅ Perfect for data exports, reports, logs
/// </summary>
public class CsvParser : IDocumentParser
{
    private readonly ILogger<CsvParser> _logger;

    public string SupportedFileType => "csv";

    public CsvParser(ILogger<CsvParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse CSV file and convert to markdown table format
    /// </summary>
    /// <param name="stream">CSV file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV data in markdown table format</returns>
    public async Task<string> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream), "CSV stream cannot be null");
        if (!stream.CanRead)
            throw new ArgumentException("CSV stream is not readable", nameof(stream));

        try
        {
            return await Task.Run(() =>
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true, // Assume first row is header
                    TrimOptions = TrimOptions.Trim,
                    MissingFieldFound = null, // Ignore missing fields
                    BadDataFound = context =>
                    {
                        _logger.LogWarning("Bad CSV data at row {Row}: {RawRecord}",
                            context.Context.Parser.Row, context.RawRecord);
                    }
                };

                using var csv = new CsvReader(reader, config);

                var contentBuilder = new StringBuilder();
                contentBuilder.AppendLine("=== CSV Data ===\n");

                // Read header
                csv.Read();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;

                if (headers == null || headers.Length == 0)
                {
                    _logger.LogWarning("No CSV headers found");
                    return "Empty CSV file";
                }

                _logger.LogInformation("📊 Parsing CSV: {Columns} columns detected", headers.Length);

                // Write header row
                contentBuilder.AppendLine($"| {string.Join(" | ", headers)} |");
                contentBuilder.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("---", headers.Length))} |");

                // Read data rows
                int rowCount = 0;
                const int maxRowsToExtract = 1000; // Limit for very large CSV files

                while (csv.Read() && rowCount < maxRowsToExtract)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var values = new List<string>();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var value = csv.GetField(i)?.Trim() ?? string.Empty;
                        values.Add(value);
                    }

                    contentBuilder.AppendLine($"| {string.Join(" | ", values)} |");
                    rowCount++;
                }

                if (rowCount >= maxRowsToExtract)
                {
                    contentBuilder.AppendLine($"\n*(Truncated: showing first {maxRowsToExtract} rows)*");
                }

                contentBuilder.AppendLine($"\n**Total Rows Extracted:** {rowCount}");

                var finalText = contentBuilder.ToString();
                _logger.LogInformation("✅ CSV parsing complete: {Rows} rows, {Columns} columns",
                    rowCount, headers.Length);
                return finalText;
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ CSV parsing cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to parse CSV file");
            throw new InvalidDataException($"Failed to parse CSV: {ex.Message}", ex);
        }
    }
}
