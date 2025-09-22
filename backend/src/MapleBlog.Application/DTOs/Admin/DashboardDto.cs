namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 仪表盘概览数据DTO
    /// </summary>
    public class DashboardOverviewDto
    {
        /// <summary>
        /// 总用户数
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// 今日新增用户数
        /// </summary>
        public int TodayNewUsers { get; set; }

        /// <summary>
        /// 总文章数
        /// </summary>
        public int TotalPosts { get; set; }

        /// <summary>
        /// 今日新增文章数
        /// </summary>
        public int TodayNewPosts { get; set; }

        /// <summary>
        /// 总评论数
        /// </summary>
        public int TotalComments { get; set; }

        /// <summary>
        /// 今日新增评论数
        /// </summary>
        public int TodayNewComments { get; set; }

        /// <summary>
        /// 总访问量
        /// </summary>
        public long TotalViews { get; set; }

        /// <summary>
        /// 今日访问量
        /// </summary>
        public long TodayViews { get; set; }

        /// <summary>
        /// 在线用户数
        /// </summary>
        public int OnlineUsers { get; set; }

        /// <summary>
        /// 系统状态
        /// </summary>
        public SystemStatusDto SystemStatus { get; set; } = new();
    }

    /// <summary>
    /// 实时统计数据DTO
    /// </summary>
    public class RealTimeStatsDto
    {
        /// <summary>
        /// 当前在线用户数
        /// </summary>
        public int CurrentOnlineUsers { get; set; }

        /// <summary>
        /// 今日页面浏览量
        /// </summary>
        public long TodayPageViews { get; set; }

        /// <summary>
        /// 今日独立访客数
        /// </summary>
        public int TodayUniqueVisitors { get; set; }

        /// <summary>
        /// 当前服务器负载
        /// </summary>
        public double CurrentCpuUsage { get; set; }

        /// <summary>
        /// 当前内存使用率
        /// </summary>
        public double CurrentMemoryUsage { get; set; }

        /// <summary>
        /// 最近1小时访问趋势
        /// </summary>
        public IEnumerable<HourlyStatsDto> HourlyTrends { get; set; } = new List<HourlyStatsDto>();

        /// <summary>
        /// 最新活动
        /// </summary>
        public IEnumerable<RealtimeActivityDto> LatestActivities { get; set; } = new List<RealtimeActivityDto>();
    }

    /// <summary>
    /// 系统性能监控数据DTO
    /// </summary>
    public class SystemPerformanceDto
    {
        /// <summary>
        /// CPU使用率历史数据
        /// </summary>
        public IEnumerable<PerformanceDataPointDto> CpuUsage { get; set; } = new List<PerformanceDataPointDto>();

        /// <summary>
        /// 内存使用率历史数据
        /// </summary>
        public IEnumerable<PerformanceDataPointDto> MemoryUsage { get; set; } = new List<PerformanceDataPointDto>();

        /// <summary>
        /// 磁盘使用率
        /// </summary>
        public double DiskUsage { get; set; }

        /// <summary>
        /// 网络IO统计
        /// </summary>
        public NetworkIoStatsDto NetworkIo { get; set; } = new();

        /// <summary>
        /// 数据库连接池状态
        /// </summary>
        public DatabaseConnectionStatsDto DatabaseStats { get; set; } = new();

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double CacheHitRate { get; set; }

        /// <summary>
        /// 响应时间统计
        /// </summary>
        public ResponseTimeStatsDto ResponseTimes { get; set; } = new();
    }

    /// <summary>
    /// 内容统计数据DTO
    /// </summary>
    public class ContentStatsDto
    {
        /// <summary>
        /// 总内容数
        /// </summary>
        public int TotalContent { get; set; }

        /// <summary>
        /// 已发布内容数
        /// </summary>
        public int PublishedContent { get; set; }

        /// <summary>
        /// 草稿数
        /// </summary>
        public int DraftContent { get; set; }

        /// <summary>
        /// 待审核内容数
        /// </summary>
        public int PendingContent { get; set; }

        /// <summary>
        /// 内容类型分布
        /// </summary>
        public IEnumerable<ContentTypeStatsDto> ContentTypeDistribution { get; set; } = new List<ContentTypeStatsDto>();

        /// <summary>
        /// 内容发布趋势
        /// </summary>
        public IEnumerable<DailyStatsDto> PublishTrends { get; set; } = new List<DailyStatsDto>();

        /// <summary>
        /// 热门分类
        /// </summary>
        public IEnumerable<CategoryStatsDto> PopularCategories { get; set; } = new List<CategoryStatsDto>();

        /// <summary>
        /// 热门标签
        /// </summary>
        public IEnumerable<TagStatsDto> PopularTags { get; set; } = new List<TagStatsDto>();
    }

    /// <summary>
    /// 用户活跃度数据DTO
    /// </summary>
    public class UserActivityDto
    {
        /// <summary>
        /// 活跃用户数
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// 新注册用户数
        /// </summary>
        public int NewUsers { get; set; }

        /// <summary>
        /// 用户活跃度趋势
        /// </summary>
        public IEnumerable<DailyStatsDto> ActivityTrends { get; set; } = new List<DailyStatsDto>();

        /// <summary>
        /// 用户行为统计
        /// </summary>
        public UserBehaviorStatsDto BehaviorStats { get; set; } = new();

        /// <summary>
        /// 用户留存率
        /// </summary>
        public UserRetentionDto RetentionStats { get; set; } = new();

        /// <summary>
        /// 地理分布
        /// </summary>
        public IEnumerable<GeographicStatsDto> GeographicDistribution { get; set; } = new List<GeographicStatsDto>();
    }

    /// <summary>
    /// 热门内容DTO
    /// </summary>
    public class PopularContentDto
    {
        /// <summary>
        /// 内容ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 浏览量
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// 评论数
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 点赞数
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime PublishedAt { get; set; }

        /// <summary>
        /// 链接地址
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 近期活动DTO
    /// </summary>
    public class RecentActivityDto
    {
        /// <summary>
        /// 活动ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 活动类型
        /// </summary>
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// 活动描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// 资源ID
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 活动时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 风险级别
        /// </summary>
        public string RiskLevel { get; set; } = "Low";
    }

    /// <summary>
    /// 系统告警DTO
    /// </summary>
    public class SystemAlertDto
    {
        /// <summary>
        /// 告警ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 告警标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 告警消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 告警级别
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// 告警类型
        /// </summary>
        public string AlertType { get; set; } = string.Empty;

        /// <summary>
        /// 是否已读
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 相关数据
        /// </summary>
        public object? Metadata { get; set; }

        /// <summary>
        /// 建议操作
        /// </summary>
        public string? SuggestedAction { get; set; }
    }

    /// <summary>
    /// 待处理任务DTO
    /// </summary>
    public class PendingTasksDto
    {
        /// <summary>
        /// 待审核内容数
        /// </summary>
        public int PendingContent { get; set; }

        /// <summary>
        /// 待审核评论数
        /// </summary>
        public int PendingComments { get; set; }

        /// <summary>
        /// 待处理举报数
        /// </summary>
        public int PendingReports { get; set; }

        /// <summary>
        /// 待回复消息数
        /// </summary>
        public int PendingMessages { get; set; }

        /// <summary>
        /// 系统错误数
        /// </summary>
        public int SystemErrors { get; set; }

        /// <summary>
        /// 待更新任务数
        /// </summary>
        public int PendingUpdates { get; set; }

        /// <summary>
        /// 任务详情列表
        /// </summary>
        public IEnumerable<TaskSummaryDto> TaskSummaries { get; set; } = new List<TaskSummaryDto>();
    }

    /// <summary>
    /// 访问趋势DTO
    /// </summary>
    public class VisitTrendDto
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 页面浏览量
        /// </summary>
        public long PageViews { get; set; }

        /// <summary>
        /// 独立访客数
        /// </summary>
        public int UniqueVisitors { get; set; }

        /// <summary>
        /// 新访客数
        /// </summary>
        public int NewVisitors { get; set; }

        /// <summary>
        /// 回访客数
        /// </summary>
        public int ReturningVisitors { get; set; }

        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }
    }

    /// <summary>
    /// 系统健康检查DTO
    /// </summary>
    public class SystemHealthDto
    {
        /// <summary>
        /// 整体健康状态
        /// </summary>
        public string OverallStatus { get; set; } = "Healthy";

        /// <summary>
        /// 数据库状态
        /// </summary>
        public HealthCheckItemDto Database { get; set; } = new();

        /// <summary>
        /// 缓存状态
        /// </summary>
        public HealthCheckItemDto Cache { get; set; } = new();

        /// <summary>
        /// 文件系统状态
        /// </summary>
        public HealthCheckItemDto FileSystem { get; set; } = new();

        /// <summary>
        /// 外部服务状态
        /// </summary>
        public IEnumerable<HealthCheckItemDto> ExternalServices { get; set; } = new List<HealthCheckItemDto>();

        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// 系统信息
        /// </summary>
        public SystemInfoDto SystemInfo { get; set; } = new();
    }

    /// <summary>
    /// 数据清理结果DTO
    /// </summary>
    public class DataCleanupResultDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 清理的记录数
        /// </summary>
        public Dictionary<string, int> CleanedRecords { get; set; } = new();

        /// <summary>
        /// 释放的存储空间（字节）
        /// </summary>
        public long FreedSpace { get; set; }

        /// <summary>
        /// 清理耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 清理详情
        /// </summary>
        public IEnumerable<string> Details { get; set; } = new List<string>();
    }

    #region 辅助DTO类

    /// <summary>
    /// 系统状态DTO
    /// </summary>
    public class SystemStatusDto
    {
        /// <summary>
        /// CPU使用率
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// 内存使用率
        /// </summary>
        public double MemoryUsage { get; set; }

        /// <summary>
        /// 磁盘使用率
        /// </summary>
        public double DiskUsage { get; set; }

        /// <summary>
        /// 系统运行时长
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// 服务状态
        /// </summary>
        public string ServiceStatus { get; set; } = "Running";
    }


    /// <summary>
    /// 实时活动DTO
    /// </summary>
    public class RealtimeActivityDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 活动类型
        /// </summary>
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// 活动描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 性能数据点DTO
    /// </summary>
    public class PerformanceDataPointDto
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 数值
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// 网络IO统计DTO
    /// </summary>
    public class NetworkIoStatsDto
    {
        /// <summary>
        /// 接收字节数
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// 发送字节数
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// 网络错误数
        /// </summary>
        public int NetworkErrors { get; set; }
    }

    /// <summary>
    /// 数据库连接统计DTO
    /// </summary>
    public class DatabaseConnectionStatsDto
    {
        /// <summary>
        /// 活跃连接数
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// 平均查询时间
        /// </summary>
        public double AverageQueryTime { get; set; }

        /// <summary>
        /// 慢查询数
        /// </summary>
        public int SlowQueries { get; set; }
    }

    /// <summary>
    /// 响应时间统计DTO
    /// </summary>
    public class ResponseTimeStatsDto
    {
        /// <summary>
        /// 平均响应时间
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// P95响应时间
        /// </summary>
        public double P95ResponseTime { get; set; }

        /// <summary>
        /// P99响应时间
        /// </summary>
        public double P99ResponseTime { get; set; }

        /// <summary>
        /// 最大响应时间
        /// </summary>
        public double MaxResponseTime { get; set; }
    }

    /// <summary>
    /// 内容类型统计DTO
    /// </summary>
    public class ContentTypeStatsDto
    {
        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 百分比
        /// </summary>
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 日统计DTO
    /// </summary>
    public class DailyStatsDto
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 数值
        /// </summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// 分类统计DTO
    /// </summary>
    public class CategoryStatsDto
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 内容数量
        /// </summary>
        public int ContentCount { get; set; }

        /// <summary>
        /// 浏览量
        /// </summary>
        public long ViewCount { get; set; }
    }

    /// <summary>
    /// 标签统计DTO
    /// </summary>
    public class TagStatsDto
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 总浏览量
        /// </summary>
        public long ViewCount { get; set; }
    }

    /// <summary>
    /// 用户行为统计DTO
    /// </summary>
    public class UserBehaviorStatsDto
    {
        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// 平均页面浏览数
        /// </summary>
        public double AveragePageViews { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// 评论参与率
        /// </summary>
        public double CommentParticipationRate { get; set; }
    }

    /// <summary>
    /// 用户留存DTO
    /// </summary>
    public class UserRetentionDto
    {
        /// <summary>
        /// 7天留存率
        /// </summary>
        public double Day7Retention { get; set; }

        /// <summary>
        /// 30天留存率
        /// </summary>
        public double Day30Retention { get; set; }

        /// <summary>
        /// 90天留存率
        /// </summary>
        public double Day90Retention { get; set; }
    }

    /// <summary>
    /// 地理统计DTO
    /// </summary>
    public class GeographicStatsDto
    {
        /// <summary>
        /// 国家/地区
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// 省份/州
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// 用户数
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// 访问量
        /// </summary>
        public long VisitCount { get; set; }
    }

    /// <summary>
    /// 任务摘要DTO
    /// </summary>
    public class TaskSummaryDto
    {
        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; } = string.Empty;

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 截止时间
        /// </summary>
        public DateTime? DueDate { get; set; }
    }

    /// <summary>
    /// 健康检查项DTO
    /// </summary>
    public class HealthCheckItemDto
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 响应时间
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 系统信息DTO
    /// </summary>
    public class SystemInfoDto
    {
        /// <summary>
        /// 操作系统
        /// </summary>
        public string OperatingSystem { get; set; } = string.Empty;

        /// <summary>
        /// .NET版本
        /// </summary>
        public string DotNetVersion { get; set; } = string.Empty;

        /// <summary>
        /// 应用版本
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;

        /// <summary>
        /// 服务器时间
        /// </summary>
        public DateTime ServerTime { get; set; }

        /// <summary>
        /// 时区
        /// </summary>
        public string TimeZone { get; set; } = string.Empty;
    }

    #endregion
}