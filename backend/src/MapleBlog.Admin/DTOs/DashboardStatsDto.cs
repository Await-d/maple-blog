namespace MapleBlog.Admin.DTOs;

/// <summary>
/// 仪表盘统计数据DTO
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// 用户统计
    /// </summary>
    public UserStatsDto UserStats { get; set; } = new();

    /// <summary>
    /// 内容统计
    /// </summary>
    public ContentStatsDto ContentStats { get; set; } = new();

    /// <summary>
    /// 系统统计
    /// </summary>
    public SystemStatsDto SystemStats { get; set; } = new();

    /// <summary>
    /// 访问统计
    /// </summary>
    public TrafficStatsDto TrafficStats { get; set; } = new();

    /// <summary>
    /// 趋势数据
    /// </summary>
    public TrendDataDto TrendData { get; set; } = new();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // 便捷属性（用于兼容）
    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers => UserStats?.TotalUsers ?? 0;

    /// <summary>
    /// 总评论数
    /// </summary>
    public int TotalComments => ContentStats?.TotalComments ?? 0;

    /// <summary>
    /// 总浏览量
    /// </summary>
    public long TotalViews => TrafficStats?.TotalPageViews ?? 0;

    /// <summary>
    /// 总文章数
    /// </summary>
    public int TotalPosts => ContentStats?.TotalPosts ?? 0;
}

/// <summary>
/// 用户统计DTO
/// </summary>
public class UserStatsDto
{
    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// 活跃用户数
    /// </summary>
    public int ActiveUsers { get; set; }

    /// <summary>
    /// 新注册用户数（今日）
    /// </summary>
    public int NewUsersToday { get; set; }

    /// <summary>
    /// 新注册用户数（本周）
    /// </summary>
    public int NewUsersThisWeek { get; set; }

    /// <summary>
    /// 新注册用户数（本月）
    /// </summary>
    public int NewUsersThisMonth { get; set; }

    /// <summary>
    /// 在线用户数
    /// </summary>
    public int OnlineUsers { get; set; }

    /// <summary>
    /// 用户增长率（%）
    /// </summary>
    public double GrowthRate { get; set; }

    /// <summary>
    /// 按角色分布
    /// </summary>
    public Dictionary<string, int> UsersByRole { get; set; } = new();

    /// <summary>
    /// 按状态分布
    /// </summary>
    public Dictionary<string, int> UsersByStatus { get; set; } = new();
}

/// <summary>
/// 内容统计DTO
/// </summary>
public class ContentStatsDto
{
    /// <summary>
    /// 总文章数
    /// </summary>
    public int TotalPosts { get; set; }

    /// <summary>
    /// 已发布文章数
    /// </summary>
    public int PublishedPosts { get; set; }

    /// <summary>
    /// 草稿文章数
    /// </summary>
    public int DraftPosts { get; set; }

    /// <summary>
    /// 今日新文章数
    /// </summary>
    public int PostsToday { get; set; }

    /// <summary>
    /// 本周新文章数
    /// </summary>
    public int PostsThisWeek { get; set; }

    /// <summary>
    /// 本月新文章数
    /// </summary>
    public int PostsThisMonth { get; set; }

    /// <summary>
    /// 总评论数
    /// </summary>
    public int TotalComments { get; set; }

    /// <summary>
    /// 待审核评论数
    /// </summary>
    public int PendingComments { get; set; }

    /// <summary>
    /// 今日新评论数
    /// </summary>
    public int CommentsToday { get; set; }

    /// <summary>
    /// 总分类数
    /// </summary>
    public int TotalCategories { get; set; }

    /// <summary>
    /// 总标签数
    /// </summary>
    public int TotalTags { get; set; }

    /// <summary>
    /// 内容增长率（%）
    /// </summary>
    public double ContentGrowthRate { get; set; }

    /// <summary>
    /// 热门分类
    /// </summary>
    public List<CategoryStatsDto> PopularCategories { get; set; } = new();

    /// <summary>
    /// 热门标签
    /// </summary>
    public List<TagStatsDto> PopularTags { get; set; } = new();
}

/// <summary>
/// 系统统计DTO
/// </summary>
public class SystemStatsDto
{
    /// <summary>
    /// 系统启动时间
    /// </summary>
    public DateTime SystemStartTime { get; set; }

    /// <summary>
    /// 系统运行时间（秒）
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// CPU使用率（%）
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// 内存使用量（MB）
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// 内存使用率（%）
    /// </summary>
    public double MemoryUsagePercentage { get; set; }

    /// <summary>
    /// 磁盘使用量（GB）
    /// </summary>
    public double DiskUsageGB { get; set; }

    /// <summary>
    /// 磁盘使用率（%）
    /// </summary>
    public double DiskUsagePercentage { get; set; }

    /// <summary>
    /// 数据库连接数
    /// </summary>
    public int DatabaseConnections { get; set; }

    /// <summary>
    /// 缓存命中率（%）
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// 错误日志数量（今日）
    /// </summary>
    public int ErrorsToday { get; set; }

    /// <summary>
    /// 警告日志数量（今日）
    /// </summary>
    public int WarningsToday { get; set; }

    /// <summary>
    /// 系统健康状态
    /// </summary>
    public SystemHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// 健康检查详情
    /// </summary>
    public List<HealthCheckDto> HealthChecks { get; set; } = new();
}

/// <summary>
/// 访问统计DTO
/// </summary>
public class TrafficStatsDto
{
    /// <summary>
    /// 今日访问量
    /// </summary>
    public int VisitsToday { get; set; }

    /// <summary>
    /// 昨日访问量
    /// </summary>
    public int VisitsYesterday { get; set; }

    /// <summary>
    /// 本周访问量
    /// </summary>
    public int VisitsThisWeek { get; set; }

    /// <summary>
    /// 本月访问量
    /// </summary>
    public int VisitsThisMonth { get; set; }

    /// <summary>
    /// 总访问量
    /// </summary>
    public long TotalVisits { get; set; }

    /// <summary>
    /// 总页面浏览量
    /// </summary>
    public long TotalPageViews { get; set; }

    /// <summary>
    /// 今日独立访客数
    /// </summary>
    public int UniqueVisitorsToday { get; set; }

    /// <summary>
    /// 平均页面浏览时间（秒）
    /// </summary>
    public double AvgPageViewTime { get; set; }

    /// <summary>
    /// 跳出率（%）
    /// </summary>
    public double BounceRate { get; set; }

    /// <summary>
    /// 热门页面
    /// </summary>
    public List<PageStatsDto> PopularPages { get; set; } = new();

    /// <summary>
    /// 来源统计
    /// </summary>
    public List<ReferrerStatsDto> TopReferrers { get; set; } = new();

    /// <summary>
    /// 地理位置统计
    /// </summary>
    public List<LocationStatsDto> TopLocations { get; set; } = new();
}

/// <summary>
/// 趋势数据DTO
/// </summary>
public class TrendDataDto
{
    /// <summary>
    /// 用户增长趋势（过去30天）
    /// </summary>
    public List<DayStatsDto> UserGrowthTrend { get; set; } = new();

    /// <summary>
    /// 内容增长趋势（过去30天）
    /// </summary>
    public List<DayStatsDto> ContentGrowthTrend { get; set; } = new();

    /// <summary>
    /// 访问量趋势（过去30天）
    /// </summary>
    public List<DayStatsDto> TrafficTrend { get; set; } = new();

    /// <summary>
    /// 系统性能趋势（过去24小时）
    /// </summary>
    public List<HourStatsDto> PerformanceTrend { get; set; } = new();
}

/// <summary>
/// 分类统计DTO
/// </summary>
public class CategoryStatsDto
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 访问量
    /// </summary>
    public int ViewCount { get; set; }
}

/// <summary>
/// 标签统计DTO
/// </summary>
public class TagStatsDto
{
    /// <summary>
    /// 标签ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UseCount { get; set; }
}

/// <summary>
/// 页面统计DTO
/// </summary>
public class PageStatsDto
{
    /// <summary>
    /// 页面路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 页面标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 访问次数
    /// </summary>
    public int Views { get; set; }

    /// <summary>
    /// 独立访客数
    /// </summary>
    public int UniqueViews { get; set; }
}

/// <summary>
/// 来源统计DTO
/// </summary>
public class ReferrerStatsDto
{
    /// <summary>
    /// 来源域名
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// 访问次数
    /// </summary>
    public int Views { get; set; }

    /// <summary>
    /// 来源类型（搜索引擎、社交媒体等）
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// 地理位置统计DTO
/// </summary>
public class LocationStatsDto
{
    /// <summary>
    /// 国家/地区
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// 城市
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// 访问次数
    /// </summary>
    public int Views { get; set; }
}

/// <summary>
/// 日统计DTO
/// </summary>
public class DayStatsDto
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 数值
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// 变化量
    /// </summary>
    public int Change { get; set; }

    /// <summary>
    /// 变化率（%）
    /// </summary>
    public double ChangeRate { get; set; }
}

/// <summary>
/// 小时统计DTO
/// </summary>
public class HourStatsDto
{
    /// <summary>
    /// 时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// CPU使用率
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// 内存使用率
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public double ResponseTime { get; set; }
}

/// <summary>
/// 健康检查DTO
/// </summary>
public class HealthCheckDto
{
    /// <summary>
    /// 检查名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public SystemHealthStatus Status { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastCheck { get; set; }
}

/// <summary>
/// 系统健康状态枚举
/// </summary>
public enum SystemHealthStatus
{
    /// <summary>
    /// 健康
    /// </summary>
    Healthy,

    /// <summary>
    /// 警告
    /// </summary>
    Warning,

    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy,

    /// <summary>
    /// 降级
    /// </summary>
    Degraded
}