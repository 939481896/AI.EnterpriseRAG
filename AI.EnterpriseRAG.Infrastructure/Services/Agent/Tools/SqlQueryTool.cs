using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

/// <summary>
/// SQL查询工具（企业数据分析核心）
/// 支持自然语言 → SQL转换，带安全防护
/// </summary>
public class SqlQueryTool : ITool
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqlQueryTool> _logger;
    private readonly HashSet<string> _allowedTables; // 白名单表
    private readonly HashSet<string> _forbiddenKeywords; // 禁止关键字

    public string Name => "sql_query";
    
    public string Description => @"执行SQL查询分析企业数据。
用于数据统计、报表生成、业务分析等场景。
支持SELECT查询，禁止DELETE/UPDATE/DROP等危险操作。
示例场景：'统计最近30天的订单数量'、'查询销售额TOP10的产品'";

    public string Category => "data";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""query_type"": {
      ""type"": ""string"",
      ""enum"": [""natural_language"", ""direct_sql""],
      ""description"": ""查询类型：natural_language(自然语言) 或 direct_sql(直接SQL)""
    },
    ""query_input"": {
      ""type"": ""string"",
      ""description"": ""查询内容（自然语言描述或SQL语句）""
    },
    ""limit"": {
      ""type"": ""integer"",
      ""default"": 100,
      ""description"": ""返回结果数量限制，默认100，最大1000""
    }
  },
  ""required"": [""query_type"", ""query_input""]
}";

    public SqlQueryTool(
        IConfiguration configuration,
        ILogger<SqlQueryTool> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // 配置白名单表（生产环境从配置文件读取）
        _allowedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "documents",
            "document_chunks",
            "chat_conversations",
            "sys_users",
            "agent_sessions",
            "agent_steps"
        };

        // 禁止的危险关键字
        _forbiddenKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DELETE", "UPDATE", "DROP", "TRUNCATE", "ALTER",
            "CREATE", "INSERT", "EXEC", "EXECUTE", "GRANT", "REVOKE"
        };
    }

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. 参数验证
            if (!arguments.TryGetValue("query_type", out var queryTypeObj) ||
                !arguments.TryGetValue("query_input", out var queryInputObj))
            {
                return ToolResult.Failure("缺少必需参数: query_type, query_input");
            }

            var queryType = queryTypeObj.ToString()!;
            var queryInput = queryInputObj.ToString()!;
            var limit = arguments.TryGetValue("limit", out var limitObj)
                ? Convert.ToInt32(limitObj)
                : 100;

            // 限制最大返回数量
            limit = Math.Min(limit, 1000);

            _logger.LogInformation(
                "用户 {UserId} 执行SQL查询: Type={Type}, Input={Input}",
                context.UserId, queryType, queryInput);

            // 2. 生成或验证SQL
            string sql;
            if (queryType == "natural_language")
            {
                // 自然语言 → SQL（简化实现，生产环境建议使用LLM）
                sql = ConvertNaturalLanguageToSql(queryInput, limit);
            }
            else if (queryType == "direct_sql")
            {
                sql = queryInput;
            }
            else
            {
                return ToolResult.Failure($"不支持的查询类型: {queryType}");
            }

            // 3. 安全检查
            var securityCheck = ValidateSqlSecurity(sql);
            if (!securityCheck.IsValid)
            {
                _logger.LogWarning(
                    "SQL安全检查失败: {Reason}, SQL={Sql}",
                    securityCheck.Reason, sql);
                return ToolResult.Failure($"SQL安全检查失败: {securityCheck.Reason}");
            }

            // 4. 执行查询
            var results = await ExecuteSqlQueryAsync(sql, cancellationToken);

            sw.Stop();

            // 5. 返回结果
            return ToolResult.Success(
                JsonSerializer.Serialize(new
                {
                    query_type = queryType,
                    original_input = queryInput,
                    executed_sql = sql,
                    row_count = results.Count,
                    results = results,
                    execution_time_ms = sw.ElapsedMilliseconds
                }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL查询执行失败");
            return ToolResult.Failure($"SQL查询失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 简化的自然语言 → SQL转换（生产环境建议接入LLM）
    /// </summary>
    private string ConvertNaturalLanguageToSql(string naturalLanguage, int limit)
    {
        var input = naturalLanguage.ToLower();

        // 示例规则匹配（实际应使用LLM或专业的NL2SQL模型）
        if (input.Contains("统计") && input.Contains("文档"))
        {
            return $@"
SELECT 
    FileType as file_type,
    COUNT(*) as count,
    SUM(FileSize) as total_size
FROM documents
GROUP BY FileType
LIMIT {limit}";
        }

        if (input.Contains("最近") && input.Contains("对话"))
        {
            var days = ExtractNumber(input) ?? 7;
            return $@"
SELECT 
    UserId as user_id,
    Question as question,
    Answer as answer,
    CreateTime as create_time
FROM chat_conversations
WHERE CreateTime >= DATE_SUB(NOW(), INTERVAL {days} DAY)
ORDER BY CreateTime DESC
LIMIT {limit}";
        }

        if (input.Contains("agent") && input.Contains("会话"))
        {
            return $@"
SELECT 
    Id as session_id,
    UserId as user_id,
    IntentType as intent_type,
    Status as status,
    TotalCostSeconds as cost_seconds,
    StartTime as start_time
FROM agent_sessions
ORDER BY StartTime DESC
LIMIT {limit}";
        }

        // 默认查询
        return $@"
SELECT 
    'documents' as table_name,
    COUNT(*) as total_count
FROM documents
LIMIT {limit}";
    }

    /// <summary>
    /// SQL安全校验（防止SQL注入和危险操作）
    /// </summary>
    private (bool IsValid, string Reason) ValidateSqlSecurity(string sql)
    {
        // 1. 检查禁止关键字
        foreach (var keyword in _forbiddenKeywords)
        {
            if (Regex.IsMatch(sql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
            {
                return (false, $"包含禁止的关键字: {keyword}");
            }
        }

        // 2. 必须是SELECT语句
        if (!Regex.IsMatch(sql.Trim(), @"^\s*SELECT\b", RegexOptions.IgnoreCase))
        {
            return (false, "仅允许SELECT查询");
        }

        // 3. 检查表名白名单
        var tablePattern = @"FROM\s+(\w+)|JOIN\s+(\w+)";
        var matches = Regex.Matches(sql, tablePattern, RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            var tableName = match.Groups[1].Value != "" 
                ? match.Groups[1].Value 
                : match.Groups[2].Value;

            if (!_allowedTables.Contains(tableName))
            {
                return (false, $"表 '{tableName}' 不在白名单中");
            }
        }

        // 4. 禁止某些危险函数
        var dangerousFunctions = new[] { "LOAD_FILE", "OUTFILE", "DUMPFILE", "SLEEP" };
        foreach (var func in dangerousFunctions)
        {
            if (sql.Contains(func, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"禁止使用函数: {func}");
            }
        }

        return (true, "");
    }

    /// <summary>
    /// 执行SQL查询（模拟实现，生产环境需集成实际数据库）
    /// </summary>
    private async Task<List<Dictionary<string, object>>> ExecuteSqlQueryAsync(
        string sql,
        CancellationToken cancellationToken)
    {
        // 简化实现：返回模拟数据（生产环境需要真实数据库连接）
        _logger.LogWarning("SQL查询工具当前为模拟模式，实际生产需集成数据库驱动");

        await Task.Delay(100, cancellationToken); // 模拟查询延迟

        // 根据SQL内容返回模拟数据
        if (sql.Contains("documents", StringComparison.OrdinalIgnoreCase))
        {
            return new List<Dictionary<string, object>>
            {
                new() { ["file_type"] = "pdf", ["count"] = 25, ["total_size"] = 1024000 },
                new() { ["file_type"] = "txt", ["count"] = 15, ["total_size"] = 512000 },
                new() { ["file_type"] = "docx", ["count"] = 10, ["total_size"] = 768000 }
            };
        }

        if (sql.Contains("chat_conversations", StringComparison.OrdinalIgnoreCase))
        {
            return new List<Dictionary<string, object>>
            {
                new() 
                { 
                    ["user_id"] = "user001", 
                    ["question"] = "产品如何安装？", 
                    ["answer"] = "请参考安装手册...",
                    ["create_time"] = DateTime.Now.AddHours(-2)
                },
                new() 
                { 
                    ["user_id"] = "user002", 
                    ["question"] = "退货流程？", 
                    ["answer"] = "退货需要...",
                    ["create_time"] = DateTime.Now.AddHours(-5)
                }
            };
        }

        if (sql.Contains("agent_sessions", StringComparison.OrdinalIgnoreCase))
        {
            return new List<Dictionary<string, object>>
            {
                new() 
                { 
                    ["session_id"] = Guid.NewGuid(), 
                    ["user_id"] = "user001",
                    ["intent_type"] = "RagQuery",
                    ["status"] = "Completed",
                    ["cost_seconds"] = 3.5m,
                    ["start_time"] = DateTime.Now.AddMinutes(-10)
                }
            };
        }

        // 默认返回
        return new List<Dictionary<string, object>>
        {
            new() { ["table_name"] = "模拟数据", ["total_count"] = 100 }
        };

        /* 生产环境实现示例（需要安装 MySqlConnector 包）:

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var connection = new MySqlConnector.MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 30;

        var results = new List<Dictionary<string, object>>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return results;
        */
    }

    /// <summary>
    /// 从文本中提取数字
    /// </summary>
    private int? ExtractNumber(string text)
    {
        var match = Regex.Match(text, @"\d+");
        return match.Success ? int.Parse(match.Value) : null;
    }
}
