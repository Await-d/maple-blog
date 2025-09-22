namespace MapleBlog.Domain.Models;

/// <summary>
/// 权限统计信息
/// 用于监控和分析权限系统的使用情况
/// </summary>
public class PermissionStatistics
{
    /// <summary>
    /// 统计时间
    /// </summary>
    public DateTime StatisticsTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 权限检查总次数
    /// </summary>
    public long TotalPermissionChecks { get; set; }

    /// <summary>
    /// 权限检查成功次数
    /// </summary>
    public long SuccessfulChecks { get; set; }

    /// <summary>
    /// 权限检查失败次数
    /// </summary>
    public long FailedChecks { get; set; }

    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// 平均检查响应时间（毫秒）
    /// </summary>
    public double AverageCheckTime { get; set; }

    /// <summary>
    /// 最大检查响应时间（毫秒）
    /// </summary>
    public double MaxCheckTime { get; set; }

    /// <summary>
    /// 最小检查响应时间（毫秒）
    /// </summary>
    public double MinCheckTime { get; set; }

    /// <summary>
    /// 活跃数据权限规则数量
    /// </summary>
    public int ActiveRulesCount { get; set; }

    /// <summary>
    /// 临时权限数量
    /// </summary>
    public int TemporaryPermissionsCount { get; set; }

    /// <summary>
    /// 过期的临时权限数量
    /// </summary>
    public int ExpiredTemporaryPermissionsCount { get; set; }

    /// <summary>
    /// 按资源类型分组的访问统计
    /// </summary>
    public Dictionary<string, ResourceAccessStatistics> ResourceStatistics { get; set; } = new();

    /// <summary>
    /// 按用户分组的访问统计（TOP用户）
    /// </summary>
    public Dictionary<Guid, UserAccessStatistics> TopUserStatistics { get; set; } = new();

    /// <summary>
    /// 错误统计
    /// </summary>
    public Dictionary<string, int> ErrorStatistics { get; set; } = new();

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double CacheHitRate => CacheHits + CacheMisses > 0
        ? (double)CacheHits / (CacheHits + CacheMisses) * 100
        : 0;

    /// <summary>
    /// 权限检查成功率
    /// </summary>
    public double SuccessRate => TotalPermissionChecks > 0
        ? (double)SuccessfulChecks / TotalPermissionChecks * 100
        : 0;
}

/// <summary>
/// 资源访问统计
/// </summary>
public class ResourceAccessStatistics
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 总访问次数
    /// </summary>
    public long TotalAccess { get; set; }

    /// <summary>
    /// 成功访问次数
    /// </summary>
    public long SuccessfulAccess { get; set; }

    /// <summary>
    /// 拒绝访问次数
    /// </summary>
    public long DeniedAccess { get; set; }

    /// <summary>
    /// 按操作类型分组的统计
    /// </summary>
    public Dictionary<string, long> OperationStatistics { get; set; } = new();

    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// 访问成功率
    /// </summary>
    public double SuccessRate => TotalAccess > 0
        ? (double)SuccessfulAccess / TotalAccess * 100
        : 0;
}

/// <summary>
/// 用户访问统计
/// </summary>
public class UserAccessStatistics
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 总访问次数
    /// </summary>
    public long TotalAccess { get; set; }

    /// <summary>
    /// 成功访问次数
    /// </summary>
    public long SuccessfulAccess { get; set; }

    /// <summary>
    /// 拒绝访问次数
    /// </summary>
    public long DeniedAccess { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>
    /// 访问的资源类型统计
    /// </summary>
    public Dictionary<string, long> ResourceTypeAccess { get; set; } = new();

    /// <summary>
    /// 访问成功率
    /// </summary>
    public double SuccessRate => TotalAccess > 0
        ? (double)SuccessfulAccess / TotalAccess * 100
        : 0;
}