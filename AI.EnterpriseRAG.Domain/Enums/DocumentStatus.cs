namespace AI.EnterpriseRAG.Domain.Enums;

/// <summary>
/// 文档状态（企业级状态管理）
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 解析中
    /// </summary>
    Parsing = 1,

    /// <summary>
    /// 已入库
    /// </summary>
    Vectorized = 2,

    /// <summary>
    /// 处理失败
    /// </summary>
    Failed = 3
}
