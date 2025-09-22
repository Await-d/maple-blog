using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Admin.DTOs;

/// <summary>
/// 系统指标数据传输对象
/// 用于传输系统监控的各种指标数据
/// </summary>
public class SystemMetricsDto
{
    /// <summary>
    /// 系统健康状态
    /// </summary>
    public SystemHealthDto Health { get; set; } = new();

    /// <summary>
    /// 系统性能指标
    /// </summary>
    public SystemPerformanceDto Performance { get; set; } = new();

    /// <summary>
    /// 数据库监控指标
    /// </summary>
    public DatabaseMetricsDto Database { get; set; } = new();

    /// <summary>
    /// 缓存监控指标
    /// </summary>
    public CacheMetricsDto Cache { get; set; } = new();

    /// <summary>
    /// 外部服务状态
    /// </summary>
    public List<ExternalServiceStatusDto> ExternalServices { get; set; } = new();

    /// <summary>
    /// 应用程序指标
    /// </summary>
    public ApplicationMetricsDto Application { get; set; } = new();

    /// <summary>
    /// 系统警告列表
    /// </summary>
    public List<SystemAlertDto> Alerts { get; set; } = new();

    /// <summary>
    /// 指标收集时间
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 系统健康状态
/// </summary>
public class SystemHealthDto
{
    /// <summary>
    /// 整体健康状态
    /// </summary>
    public HealthStatus OverallStatus { get; set; }

    /// <summary>
    /// 系统正常运行时间（秒）
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// 最后重启时间
    /// </summary>
    public DateTime LastRestartTime { get; set; }

    /// <summary>
    /// 运行环境
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// 应用版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 各组件健康状态
    /// </summary>
    public Dictionary<string, ComponentHealthDto> Components { get; set; } = new();
}

/// <summary>
/// 组件健康状态
/// </summary>
public class ComponentHealthDto
{
    /// <summary>
    /// 组件状态
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// 组件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 检查耗时（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 额外数据
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// 系统性能指标
/// </summary>
public class SystemPerformanceDto
{
    /// <summary>
    /// CPU使用率 (0-100)
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// 内存使用量（字节）
    /// </summary>
    public long MemoryUsedBytes { get; set; }

    /// <summary>
    /// 总内存量（字节）
    /// </summary>
    public long MemoryTotalBytes { get; set; }

    /// <summary>
    /// 内存使用率 (0-100)
    /// </summary>
    public double MemoryUsagePercent => MemoryTotalBytes > 0 ? (double)MemoryUsedBytes / MemoryTotalBytes * 100 : 0;

    /// <summary>
    /// 磁盘使用量（字节）
    /// </summary>
    public long DiskUsedBytes { get; set; }

    /// <summary>
    /// 总磁盘容量（字节）
    /// </summary>
    public long DiskTotalBytes { get; set; }

    /// <summary>
    /// 磁盘使用率 (0-100)
    /// </summary>
    public double DiskUsagePercent => DiskTotalBytes > 0 ? (double)DiskUsedBytes / DiskTotalBytes * 100 : 0;

    /// <summary>
    /// 网络接收字节数
    /// </summary>
    public long NetworkBytesReceived { get; set; }

    /// <summary>
    /// 网络发送字节数
    /// </summary>
    public long NetworkBytesSent { get; set; }

    /// <summary>
    /// 垃圾回收信息
    /// </summary>
    public GarbageCollectionDto GarbageCollection { get; set; } = new();

    /// <summary>
    /// 线程池信息
    /// </summary>
    public ThreadPoolDto ThreadPool { get; set; } = new();
}

/// <summary>
/// 垃圾回收指标
/// </summary>
public class GarbageCollectionDto
{
    /// <summary>
    /// 第0代垃圾回收次数
    /// </summary>
    public int Gen0Collections { get; set; }

    /// <summary>
    /// 第1代垃圾回收次数
    /// </summary>
    public int Gen1Collections { get; set; }

    /// <summary>
    /// 第2代垃圾回收次数
    /// </summary>
    public int Gen2Collections { get; set; }

    /// <summary>
    /// 托管堆总内存（字节）
    /// </summary>
    public long TotalMemory { get; set; }
}

/// <summary>
/// 线程池指标
/// </summary>
public class ThreadPoolDto
{
    /// <summary>
    /// 工作线程数
    /// </summary>
    public int WorkerThreads { get; set; }

    /// <summary>
    /// 最大工作线程数
    /// </summary>
    public int MaxWorkerThreads { get; set; }

    /// <summary>
    /// I/O线程数
    /// </summary>
    public int CompletionPortThreads { get; set; }

    /// <summary>
    /// 最大I/O线程数
    /// </summary>
    public int MaxCompletionPortThreads { get; set; }

    /// <summary>
    /// 排队的工作项数
    /// </summary>
    public long QueuedWorkItems { get; set; }
}

/// <summary>
/// 数据库监控指标
/// </summary>
public class DatabaseMetricsDto
{
    /// <summary>
    /// 数据库连接状态
    /// </summary>
    public HealthStatus ConnectionStatus { get; set; }

    /// <summary>
    /// 连接池使用情况
    /// </summary>
    public ConnectionPoolDto ConnectionPool { get; set; } = new();

    /// <summary>
    /// 查询性能指标
    /// </summary>
    public QueryPerformanceDto QueryPerformance { get; set; } = new();

    /// <summary>
    /// 数据库大小（字节）
    /// </summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// 表统计信息
    /// </summary>
    public List<TableStatsDto> TableStats { get; set; } = new();

    /// <summary>
    /// 数据库响应时间（毫秒）
    /// </summary>
    public double ResponseTimeMs { get; set; }
}

/// <summary>
/// 连接池指标
/// </summary>
public class ConnectionPoolDto
{
    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 空闲连接数
    /// </summary>
    public int IdleConnections { get; set; }

    /// <summary>
    /// 最大连接数
    /// </summary>
    public int MaxConnections { get; set; }

    /// <summary>
    /// 连接池使用率
    /// </summary>
    public double UsagePercent => MaxConnections > 0 ? (double)ActiveConnections / MaxConnections * 100 : 0;
}

/// <summary>
/// 查询性能指标
/// </summary>
public class QueryPerformanceDto
{
    /// <summary>
    /// 平均查询时间（毫秒）
    /// </summary>
    public double AverageQueryTimeMs { get; set; }

    /// <summary>
    /// 最慢查询时间（毫秒）
    /// </summary>
    public double SlowestQueryTimeMs { get; set; }

    /// <summary>
    /// 总查询次数
    /// </summary>
    public long TotalQueries { get; set; }

    /// <summary>
    /// 失败查询次数
    /// </summary>
    public long FailedQueries { get; set; }

    /// <summary>
    /// 查询成功率
    /// </summary>
    public double SuccessRate => TotalQueries > 0 ? (double)(TotalQueries - FailedQueries) / TotalQueries * 100 : 100;
}

/// <summary>
/// 表统计信息
/// </summary>
public class TableStatsDto
{
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 记录数
    /// </summary>
    public long RecordCount { get; set; }

    /// <summary>
    /// 表大小（字节）
    /// </summary>
    public long TableSizeBytes { get; set; }

    /// <summary>
    /// 索引大小（字节）
    /// </summary>
    public long IndexSizeBytes { get; set; }
}

/// <summary>
/// 缓存监控指标
/// </summary>
public class CacheMetricsDto
{
    /// <summary>
    /// Redis缓存指标
    /// </summary>
    public RedisCacheDto? Redis { get; set; }

    /// <summary>
    /// 内存缓存指标
    /// </summary>
    public MemoryCacheDto MemoryCache { get; set; } = new();
}

/// <summary>
/// Redis缓存指标
/// </summary>
public class RedisCacheDto
{
    /// <summary>
    /// 连接状态
    /// </summary>
    public HealthStatus ConnectionStatus { get; set; }

    /// <summary>
    /// 已用内存（字节）
    /// </summary>
    public long UsedMemoryBytes { get; set; }

    /// <summary>
    /// 最大内存（字节）
    /// </summary>
    public long MaxMemoryBytes { get; set; }

    /// <summary>
    /// 内存使用率
    /// </summary>
    public double MemoryUsagePercent => MaxMemoryBytes > 0 ? (double)UsedMemoryBytes / MaxMemoryBytes * 100 : 0;

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double HitRatio { get; set; }

    /// <summary>
    /// 键总数
    /// </summary>
    public long TotalKeys { get; set; }

    /// <summary>
    /// 过期键数
    /// </summary>
    public long ExpiredKeys { get; set; }

    /// <summary>
    /// 驱逐键数
    /// </summary>
    public long EvictedKeys { get; set; }

    /// <summary>
    /// 连接的客户端数
    /// </summary>
    public int ConnectedClients { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public double ResponseTimeMs { get; set; }
}

/// <summary>
/// 内存缓存指标
/// </summary>
public class MemoryCacheDto
{
    /// <summary>
    /// 缓存项数量
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// 估计内存使用量（字节）
    /// </summary>
    public long EstimatedMemoryUsage { get; set; }

    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double HitRatio => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) * 100 : 0;
}

/// <summary>
/// 外部服务状态
/// </summary>
public class ExternalServiceStatusDto
{
    /// <summary>
    /// 服务名称
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 服务URL
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// 服务状态
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 服务版本
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// 应用程序指标
/// </summary>
public class ApplicationMetricsDto
{
    /// <summary>
    /// 每秒请求数
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// 错误率 (0-100)
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// 活跃用户数
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// 活跃连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 应用程序异常数
    /// </summary>
    public long ExceptionCount { get; set; }

    /// <summary>
    /// 最近1小时的请求统计
    /// </summary>
    public List<RequestStatsDto> HourlyStats { get; set; } = new();
}

/// <summary>
/// 请求统计
/// </summary>
public class RequestStatsDto
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 请求数量
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// 错误数量
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
}

/// <summary>
/// 系统警告
/// </summary>
public class SystemAlertDto
{
    /// <summary>
    /// 警告ID
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// 警告级别
    /// </summary>
    public AlertLevel Level { get; set; }

    /// <summary>
    /// 警告类型
    /// </summary>
    public AlertType Type { get; set; }

    /// <summary>
    /// 警告标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 警告描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 警告来源
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 触发时间
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// 是否已确认
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// 确认者
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// 相关指标值
    /// </summary>
    public Dictionary<string, object> MetricValues { get; set; } = new();
}

/// <summary>
/// 健康状态枚举
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// 健康
    /// </summary>
    Healthy,

    /// <summary>
    /// 降级
    /// </summary>
    Degraded,

    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy,

    /// <summary>
    /// 未知
    /// </summary>
    Unknown
}

/// <summary>
/// 警告级别
/// </summary>
public enum AlertLevel
{
    /// <summary>
    /// 信息
    /// </summary>
    Info,

    /// <summary>
    /// 警告
    /// </summary>
    Warning,

    /// <summary>
    /// 错误
    /// </summary>
    Error,

    /// <summary>
    /// 严重
    /// </summary>
    Critical
}

/// <summary>
/// 警告类型
/// </summary>
public enum AlertType
{
    /// <summary>
    /// 性能警告
    /// </summary>
    Performance,

    /// <summary>
    /// 资源警告
    /// </summary>
    Resource,

    /// <summary>
    /// 连接警告
    /// </summary>
    Connection,

    /// <summary>
    /// 安全警告
    /// </summary>
    Security,

    /// <summary>
    /// 应用程序警告
    /// </summary>
    Application,

    /// <summary>
    /// 系统警告
    /// </summary>
    System
}