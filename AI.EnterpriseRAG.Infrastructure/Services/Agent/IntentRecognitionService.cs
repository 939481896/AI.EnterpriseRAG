using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using AI.EnterpriseRAG.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent;

/// <summary>
/// 意图识别服务实现（基于LLM）
/// </summary>
public class IntentRecognitionService : IIntentRecognitionService
{
    private readonly ILlmService _llmService;
    private readonly ILogger<IntentRecognitionService> _logger;

    public IntentRecognitionService(
        ILlmService llmService,
        ILogger<IntentRecognitionService> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<IntentResult> RecognizeAsync(
        string userInput,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 构建意图识别Prompt
            var prompt = BuildIntentPrompt(userInput, context);

            // 调用LLM识别意图
            var response = await _llmService.ChatAsync(prompt, cancellationToken);

            // 解析LLM响应
            var intentResult = ParseIntentResponse(response);

            _logger.LogInformation(
                "意图识别完成: Type={Type}, Confidence={Confidence}",
                intentResult.Type,
                intentResult.Confidence);

            return intentResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "意图识别失败: {Input}", userInput);
            return new IntentResult
            {
                Type = IntentType.Unknown,
                Confidence = 0,
                Description = "意图识别失败"
            };
        }
    }

    private string BuildIntentPrompt(string userInput, Dictionary<string, object>? context)
    {
        var prompt = @"你是一个企业级AI助手的意图识别模块。请分析用户输入，识别其意图类型。

## 支持的意图类型
1. RagQuery: 用户想从知识库查询信息
2. DataAnalysis: 用户需要数据分析
3. Troubleshooting: 用户需要故障诊断或根因分析
4. DataCollection: 用户需要采集数据
5. TaskExecution: 用户需要执行特定任务
6. Chitchat: 闲聊

## 用户输入
" + userInput + @"

## 输出格式（必须严格JSON）
{
  ""intent_type"": ""RagQuery"",
  ""confidence"": 0.95,
  ""entities"": {
    ""query_keywords"": ""文档,查询""
  },
  ""suggested_tools"": [""rag_search_tool""],
  ""description"": ""用户想查询知识库信息""
}

请直接输出JSON，不要额外解释:";

        return prompt;
    }

    private IntentResult ParseIntentResponse(string response)
    {
        try
        {
            // 清理可能的Markdown代码块
            response = response.Trim();
            if (response.StartsWith("```json"))
                response = response.Substring(7);
            if (response.StartsWith("```"))
                response = response.Substring(3);
            if (response.EndsWith("```"))
                response = response.Substring(0, response.Length - 3);
            response = response.Trim();

            var json = JsonDocument.Parse(response);
            var root = json.RootElement;

            var intentTypeStr = root.GetProperty("intent_type").GetString() ?? "Unknown";
            var intentType = Enum.TryParse<IntentType>(intentTypeStr, out var type)
                ? type
                : IntentType.Unknown;

            var entities = new Dictionary<string, string>();
            if (root.TryGetProperty("entities", out var entitiesObj))
            {
                foreach (var prop in entitiesObj.EnumerateObject())
                {
                    entities[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            var suggestedTools = new List<string>();
            if (root.TryGetProperty("suggested_tools", out var toolsArray))
            {
                suggestedTools = toolsArray.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            return new IntentResult
            {
                Type = intentType,
                Confidence = root.TryGetProperty("confidence", out var conf)
                    ? (float)conf.GetDouble()
                    : 0.5f,
                Entities = entities,
                SuggestedTools = suggestedTools,
                Description = root.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? ""
                    : ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析意图识别响应失败: {Response}", response);
            return new IntentResult
            {
                Type = IntentType.Unknown,
                Confidence = 0
            };
        }
    }
}
