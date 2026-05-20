using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using System.Collections.Concurrent;
using System.Text;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent;

/// <summary>
/// 工具注册中心实现
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();

    public void RegisterTool(ITool tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
            throw new ArgumentException("工具名称不能为空");

        _tools[tool.Name] = tool;
    }

    public ITool? GetTool(string toolName)
    {
        _tools.TryGetValue(toolName, out var tool);
        return tool;
    }

    public IEnumerable<ITool> GetAllTools() => _tools.Values;

    public IEnumerable<ITool> GetToolsByCategory(string category)
        => _tools.Values.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    public string GenerateToolsPrompt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# 可用工具列表");
        sb.AppendLine();

        foreach (var tool in _tools.Values)
        {
            sb.AppendLine($"## {tool.Name}");
            sb.AppendLine($"**描述**: {tool.Description}");
            sb.AppendLine($"**分类**: {tool.Category}");
            sb.AppendLine($"**参数Schema**: {tool.ParametersSchema}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
