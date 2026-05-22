using AI.EnterpriseRAG.Domain.Interfaces.Agent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AI.EnterpriseRAG.Infrastructure.Services.Agent.Tools;

/// <summary>
/// 服务器监控工具（DevOps运维核心）
/// </summary>
public class ServerMonitorTool : ITool
{
    private readonly ILogger<ServerMonitorTool> _logger;

    public string Name => "server_monitor";
    
    public string Description => @"实时监控服务器状态，包括：
1. CPU使用率
2. 内存使用率
3. 磁盘空间
4. 进程监控
5. 网络连接
适用场景：系统性能监控、故障预警、资源优化";

    public string Category => "devops";
    public bool RequiresAuth => true;

    public string ParametersSchema => @"{
  ""type"": ""object"",
  ""properties"": {
    ""monitor_type"": {
      ""type"": ""string"",
      ""enum"": [""cpu"", ""memory"", ""disk"", ""process"", ""network"", ""all""],
      ""description"": ""监控类型：cpu、memory、disk、process、network 或 all(全部)""
    },
    ""process_name"": {
      ""type"": ""string"",
      ""description"": ""进程名称（monitor_type=process时必填）""
    }
  },
  ""required"": [""monitor_type""]
}";

    public ServerMonitorTool(ILogger<ServerMonitorTool> logger)
    {
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> arguments,
        ToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            if (!arguments.TryGetValue("monitor_type", out var monitorTypeObj))
            {
                return ToolResult.Failure("缺少必需参数: monitor_type");
            }

            var monitorType = monitorTypeObj.ToString()!;
            _logger.LogInformation(
                "用户 {UserId} 执行服务器监控: Type={Type}",
                context.UserId, monitorType);

            object result = monitorType switch
            {
                "cpu" => await GetCpuUsageAsync(cancellationToken),
                "memory" => GetMemoryUsage(),
                "disk" => GetDiskUsage(),
                "process" => GetProcessInfo(
                    arguments.TryGetValue("process_name", out var procObj) 
                        ? procObj.ToString()! 
                        : null),
                "network" => GetNetworkInfo(),
                "all" => await GetAllMonitorDataAsync(cancellationToken),
                _ => throw new ArgumentException($"不支持的监控类型: {monitorType}")
            };

            sw.Stop();

            return ToolResult.Success(
                JsonSerializer.Serialize(new
                {
                    monitor_type = monitorType,
                    timestamp = DateTime.UtcNow,
                    hostname = Environment.MachineName,
                    os = GetOsInfo(),
                    data = result
                }),
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "服务器监控失败");
            return ToolResult.Failure($"服务器监控失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取CPU使用率
    /// </summary>
    private async Task<object> GetCpuUsageAsync(CancellationToken ct)
    {
        // 获取处理器数量
        var processorCount = Environment.ProcessorCount;

        // 使用PerformanceCounter获取CPU使用率（需要多次采样）
        var cpuUsages = new List<float>();
        
        for (int i = 0; i < 3; i++)
        {
            var startTime = DateTime.UtcNow;
            var startCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

            await Task.Delay(500, ct); // 采样间隔

            var endTime = DateTime.UtcNow;
            var endCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (processorCount * totalMsPassed);

            cpuUsages.Add((float)(cpuUsageTotal * 100));
        }

        var avgCpuUsage = cpuUsages.Average();

        return new
        {
            processor_count = processorCount,
            usage_percent = Math.Round(avgCpuUsage, 2),
            status = GetStatusByUsage(avgCpuUsage),
            samples = cpuUsages.Select(u => Math.Round(u, 2))
        };
    }

    /// <summary>
    /// 获取内存使用情况
    /// </summary>
    private object GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64; // 工作集大小
        var privateBytes = process.PrivateMemorySize64; // 私有字节

        // 获取系统总内存（跨平台）
        long totalMemory = 0;
        long availableMemory = 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows平台
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                totalMemory = (long)memStatus.ullTotalPhys;
                availableMemory = (long)memStatus.ullAvailPhys;
            }
        }
        else
        {
            // Linux/Mac平台（简化实现）
            totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            availableMemory = totalMemory - workingSet;
        }

        var usedMemory = totalMemory - availableMemory;
        var usagePercent = totalMemory > 0 
            ? (double)usedMemory / totalMemory * 100 
            : 0;

        return new
        {
            total_mb = totalMemory / 1024 / 1024,
            used_mb = usedMemory / 1024 / 1024,
            available_mb = availableMemory / 1024 / 1024,
            usage_percent = Math.Round(usagePercent, 2),
            status = GetStatusByUsage((float)usagePercent),
            process_working_set_mb = workingSet / 1024 / 1024,
            process_private_mb = privateBytes / 1024 / 1024
        };
    }

    /// <summary>
    /// 获取磁盘使用情况
    /// </summary>
    private object GetDiskUsage()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d =>
            {
                var usedSpace = d.TotalSize - d.AvailableFreeSpace;
                var usagePercent = (double)usedSpace / d.TotalSize * 100;

                return new
                {
                    name = d.Name,
                    type = d.DriveType.ToString(),
                    format = d.DriveFormat,
                    total_gb = Math.Round((double)d.TotalSize / 1024 / 1024 / 1024, 2),
                    used_gb = Math.Round((double)usedSpace / 1024 / 1024 / 1024, 2),
                    available_gb = Math.Round((double)d.AvailableFreeSpace / 1024 / 1024 / 1024, 2),
                    usage_percent = Math.Round(usagePercent, 2),
                    status = GetStatusByUsage((float)usagePercent)
                };
            })
            .ToList();

        return new
        {
            drives,
            drive_count = drives.Count
        };
    }

    /// <summary>
    /// 获取进程信息
    /// </summary>
    private object GetProcessInfo(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            // 返回系统进程总览
            var allProcesses = Process.GetProcesses();
            var topProcesses = allProcesses
                .OrderByDescending(p => p.WorkingSet64)
                .Take(10)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.ProcessName,
                    memory_mb = p.WorkingSet64 / 1024 / 1024,
                    cpu_time_seconds = p.TotalProcessorTime.TotalSeconds,
                    threads = p.Threads.Count,
                    start_time = p.StartTime
                })
                .ToList();

            return new
            {
                total_process_count = allProcesses.Length,
                top_memory_processes = topProcesses
            };
        }
        else
        {
            // 查询特定进程
            var processes = Process.GetProcessesByName(processName);
            if (!processes.Any())
            {
                return new
                {
                    found = false,
                    message = $"未找到进程: {processName}"
                };
            }

            var processInfo = processes.Select(p => new
            {
                id = p.Id,
                name = p.ProcessName,
                memory_mb = p.WorkingSet64 / 1024 / 1024,
                cpu_time_seconds = p.TotalProcessorTime.TotalSeconds,
                threads = p.Threads.Count,
                start_time = p.StartTime,
                status = p.Responding ? "running" : "not_responding"
            }).ToList();

            return new
            {
                found = true,
                process_count = processes.Length,
                processes = processInfo
            };
        }
    }

    /// <summary>
    /// 获取网络信息
    /// </summary>
    private object GetNetworkInfo()
    {
        var connections = System.Net.NetworkInformation.IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpConnections();

        var listeningPorts = System.Net.NetworkInformation.IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Select(ep => ep.Port)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        var connectionsByState = connections
            .GroupBy(c => c.State)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        return new
        {
            total_connections = connections.Length,
            connections_by_state = connectionsByState,
            listening_ports = listeningPorts,
            listening_port_count = listeningPorts.Count
        };
    }

    /// <summary>
    /// 获取所有监控数据
    /// </summary>
    private async Task<object> GetAllMonitorDataAsync(CancellationToken ct)
    {
        return new
        {
            cpu = await GetCpuUsageAsync(ct),
            memory = GetMemoryUsage(),
            disk = GetDiskUsage(),
            process_overview = GetProcessInfo(null),
            network = GetNetworkInfo()
        };
    }

    private string GetStatusByUsage(float usagePercent)
    {
        return usagePercent switch
        {
            < 50 => "healthy",
            < 80 => "warning",
            _ => "critical"
        };
    }

    private object GetOsInfo()
    {
        return new
        {
            platform = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.OSArchitecture.ToString(),
            framework = RuntimeInformation.FrameworkDescription,
            is_64bit = Environment.Is64BitOperatingSystem
        };
    }

    // Windows API for memory info
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
