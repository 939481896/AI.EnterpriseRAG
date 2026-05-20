using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

namespace AI.EnterpriseRAG.WebAPI;

/// <summary>
/// 工具注册后台服务（应用启动时自动注册所有工具）
/// </summary>
public class ToolRegistrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolRegistrationService> _logger;

    public ToolRegistrationService(
        IServiceProvider serviceProvider,
        ILogger<ToolRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

        // 注册所有工具
        var ragSearchTool = scope.ServiceProvider.GetRequiredService<RagSearchTool>();
        var dataCollectionTool = scope.ServiceProvider.GetRequiredService<DataCollectionTool>();
        var logAnalysisTool = scope.ServiceProvider.GetRequiredService<LogAnalysisTool>();

        registry.RegisterTool(ragSearchTool);
        registry.RegisterTool(dataCollectionTool);
        registry.RegisterTool(logAnalysisTool);

        _logger.LogInformation("已注册 {Count} 个Agent工具", registry.GetAllTools().Count());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("工具注册服务已停止");
        return Task.CompletedTask;
    }
}
