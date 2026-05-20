using AI.EnterpriseRAG.Domain.Entities;

namespace AI.EnterpriseRAG.Domain.Interfaces.Agent;

/// <summary>
/// 意图识别服务接口
/// </summary>
public interface IIntentRecognitionService
{
    /// <summary>
    /// 识别用户意图
    /// </summary>
    /// <param name="userInput">用户输入</param>
    /// <param name="context">上下文信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>识别的意图</returns>
    Task<IntentResult> RecognizeAsync(
        string userInput,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 意图识别结果
/// </summary>
public class IntentResult
{
    /// <summary>
    /// 意图类型
    /// </summary>
    public IntentType Type { get; set; }

    /// <summary>
    /// 置信度（0-1）
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// 提取的实体
    /// </summary>
    public Dictionary<string, string> Entities { get; set; } = new();

    /// <summary>
    /// 建议的工具列表
    /// </summary>
    public List<string> SuggestedTools { get; set; } = new();

    /// <summary>
    /// 意图描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 意图类型枚举
/// </summary>
public enum IntentType
{
    /// <summary>
    /// RAG知识库问答
    /// </summary>
    RagQuery,

    /// <summary>
    /// 数据分析
    /// </summary>
    DataAnalysis,

    /// <summary>
    /// 故障诊断/根因分析
    /// </summary>
    Troubleshooting,

    /// <summary>
    /// 数据采集
    /// </summary>
    DataCollection,

    /// <summary>
    /// 任务执行
    /// </summary>
    TaskExecution,

    /// <summary>
    /// 闲聊
    /// </summary>
    Chitchat,

    /// <summary>
    /// 未知意图
    /// </summary>
    Unknown
}
