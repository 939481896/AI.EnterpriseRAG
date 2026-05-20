namespace AI.EnterpriseRAG.Domain.Interfaces.Agent;

/// <summary>
/// 工具注册中心接口
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// 注册工具
    /// </summary>
    void RegisterTool(ITool tool);

    /// <summary>
    /// 获取工具
    /// </summary>
    ITool? GetTool(string toolName);

    /// <summary>
    /// 获取所有工具
    /// </summary>
    IEnumerable<ITool> GetAllTools();

    /// <summary>
    /// 获取指定分类的工具
    /// </summary>
    IEnumerable<ITool> GetToolsByCategory(string category);

    /// <summary>
    /// 生成工具列表的LLM Prompt描述
    /// </summary>
    string GenerateToolsPrompt();
}
