namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 网站分析数据DTO
    /// </summary>
    public class WebsiteAnalyticsDto
    {
        /// <summary>
        /// 总页面浏览量
        /// </summary>
        public long TotalPageViews { get; set; }

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

        /// <summary>
        /// 页面平均加载时间
        /// </summary>
        public double AveragePageLoadTime { get; set; }

        /// <summary>
        /// 转化率
        /// </summary>
        public double ConversionRate { get; set; }

        /// <summary>
        /// 每日访问趋势
        /// </summary>
        public IEnumerable<DailyVisitStatsDto> DailyTrends { get; set; } = new List<DailyVisitStatsDto>();

        /// <summary>
        /// 热门页面
        /// </summary>
        public IEnumerable<PageStatsDto> TopPages { get; set; } = new List<PageStatsDto>();

        /// <summary>
        /// 热门入口页面
        /// </summary>
        public IEnumerable<PageStatsDto> TopLandingPages { get; set; } = new List<PageStatsDto>();

        /// <summary>
        /// 热门退出页面
        /// </summary>
        public IEnumerable<PageStatsDto> TopExitPages { get; set; } = new List<PageStatsDto>();
    }

    /// <summary>
    /// 用户行为分析DTO
    /// </summary>
    public class UserBehaviorAnalyticsDto
    {
        /// <summary>
        /// 活跃用户数
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// 用户会话分布
        /// </summary>
        public IEnumerable<SessionDurationStatsDto> SessionDurations { get; set; } = new List<SessionDurationStatsDto>();

        /// <summary>
        /// 页面深度分析
        /// </summary>
        public IEnumerable<PageDepthStatsDto> PageDepths { get; set; } = new List<PageDepthStatsDto>();

        /// <summary>
        /// 用户路径分析
        /// </summary>
        public IEnumerable<UserPathStatsDto> UserPaths { get; set; } = new List<UserPathStatsDto>();

        /// <summary>
        /// 操作频率分析
        /// </summary>
        public IEnumerable<ActionFrequencyStatsDto> ActionFrequencies { get; set; } = new List<ActionFrequencyStatsDto>();

        /// <summary>
        /// 时间段分析
        /// </summary>
        public IEnumerable<HourlyActivityStatsDto> HourlyActivity { get; set; } = new List<HourlyActivityStatsDto>();

        /// <summary>
        /// 设备使用分析
        /// </summary>
        public DeviceUsageStatsDto DeviceUsage { get; set; } = new();

        /// <summary>
        /// 用户忠诚度分析
        /// </summary>
        public UserLoyaltyStatsDto LoyaltyStats { get; set; } = new();
    }

    /// <summary>
    /// 内容表现分析DTO
    /// </summary>
    public class ContentPerformanceAnalyticsDto
    {
        /// <summary>
        /// 总内容数
        /// </summary>
        public int TotalContent { get; set; }

        /// <summary>
        /// 平均浏览量
        /// </summary>
        public double AverageViews { get; set; }

        /// <summary>
        /// 平均评论数
        /// </summary>
        public double AverageComments { get; set; }

        /// <summary>
        /// 平均分享数
        /// </summary>
        public double AverageShares { get; set; }

        /// <summary>
        /// 内容参与度
        /// </summary>
        public double EngagementRate { get; set; }

        /// <summary>
        /// 表现最佳内容
        /// </summary>
        public IEnumerable<ContentPerformanceDto> TopPerformingContent { get; set; } = new List<ContentPerformanceDto>();

        /// <summary>
        /// 表现趋势
        /// </summary>
        public IEnumerable<DailyPerformanceStatsDto> PerformanceTrends { get; set; } = new List<DailyPerformanceStatsDto>();

        /// <summary>
        /// 分类表现
        /// </summary>
        public IEnumerable<CategoryPerformanceDto> CategoryPerformance { get; set; } = new List<CategoryPerformanceDto>();

        /// <summary>
        /// 标签表现
        /// </summary>
        public IEnumerable<TagPerformanceDto> TagPerformance { get; set; } = new List<TagPerformanceDto>();

        /// <summary>
        /// 作者表现
        /// </summary>
        public IEnumerable<AuthorPerformanceDto> AuthorPerformance { get; set; } = new List<AuthorPerformanceDto>();
    }

    /// <summary>
    /// 搜索关键词分析DTO
    /// </summary>
    public class SearchKeywordAnalyticsDto
    {
        /// <summary>
        /// 关键词
        /// </summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>
        /// 搜索次数
        /// </summary>
        public int SearchCount { get; set; }

        /// <summary>
        /// 结果点击次数
        /// </summary>
        public int ResultClicks { get; set; }

        /// <summary>
        /// 点击率
        /// </summary>
        public double ClickThroughRate { get; set; }

        /// <summary>
        /// 平均搜索结果位置
        /// </summary>
        public double AveragePosition { get; set; }

        /// <summary>
        /// 无结果搜索次数
        /// </summary>
        public int NoResultSearches { get; set; }

        /// <summary>
        /// 趋势变化
        /// </summary>
        public double TrendChange { get; set; }

        /// <summary>
        /// 相关建议
        /// </summary>
        public IEnumerable<string> RelatedKeywords { get; set; } = new List<string>();
    }

    /// <summary>
    /// 地理位置分析DTO
    /// </summary>
    public class GeographicAnalyticsDto
    {
        /// <summary>
        /// 国家代码
        /// </summary>
        public string CountryCode { get; set; } = string.Empty;

        /// <summary>
        /// 国家名称
        /// </summary>
        public string CountryName { get; set; } = string.Empty;

        /// <summary>
        /// 地区/省份
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// 城市
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// 访客数
        /// </summary>
        public int Visitors { get; set; }

        /// <summary>
        /// 页面浏览量
        /// </summary>
        public long PageViews { get; set; }

        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// 转化率
        /// </summary>
        public double ConversionRate { get; set; }

        /// <summary>
        /// 经纬度坐标
        /// </summary>
        public CoordinatesDto? Coordinates { get; set; }
    }

    /// <summary>
    /// 设备和浏览器分析DTO
    /// </summary>
    public class DeviceBrowserAnalyticsDto
    {
        /// <summary>
        /// 设备类型分布
        /// </summary>
        public IEnumerable<DeviceTypeStatsDto> DeviceTypes { get; set; } = new List<DeviceTypeStatsDto>();

        /// <summary>
        /// 操作系统分布
        /// </summary>
        public IEnumerable<OperatingSystemStatsDto> OperatingSystems { get; set; } = new List<OperatingSystemStatsDto>();

        /// <summary>
        /// 浏览器分布
        /// </summary>
        public IEnumerable<BrowserStatsDto> Browsers { get; set; } = new List<BrowserStatsDto>();

        /// <summary>
        /// 屏幕分辨率分布
        /// </summary>
        public IEnumerable<ScreenResolutionStatsDto> ScreenResolutions { get; set; } = new List<ScreenResolutionStatsDto>();

        /// <summary>
        /// 移动设备品牌分布
        /// </summary>
        public IEnumerable<MobileDeviceStatsDto> MobileDevices { get; set; } = new List<MobileDeviceStatsDto>();

        /// <summary>
        /// 网络类型分布
        /// </summary>
        public IEnumerable<NetworkTypeStatsDto> NetworkTypes { get; set; } = new List<NetworkTypeStatsDto>();
    }

    /// <summary>
    /// 流量来源分析DTO
    /// </summary>
    public class TrafficSourceAnalyticsDto
    {
        /// <summary>
        /// 来源类型
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// 来源名称
        /// </summary>
        public string SourceName { get; set; } = string.Empty;

        /// <summary>
        /// 媒介
        /// </summary>
        public string Medium { get; set; } = string.Empty;

        /// <summary>
        /// 活动
        /// </summary>
        public string? Campaign { get; set; }

        /// <summary>
        /// 访客数
        /// </summary>
        public int Visitors { get; set; }

        /// <summary>
        /// 会话数
        /// </summary>
        public int Sessions { get; set; }

        /// <summary>
        /// 页面浏览量
        /// </summary>
        public long PageViews { get; set; }

        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// 转化率
        /// </summary>
        public double ConversionRate { get; set; }

        /// <summary>
        /// 收入贡献
        /// </summary>
        public decimal RevenueContribution { get; set; }
    }

    /// <summary>
    /// 用户留存分析DTO
    /// </summary>
    public class UserRetentionAnalyticsDto
    {
        /// <summary>
        /// 队列日期
        /// </summary>
        public DateTime CohortDate { get; set; }

        /// <summary>
        /// 队列大小
        /// </summary>
        public int CohortSize { get; set; }

        /// <summary>
        /// 留存率数据
        /// </summary>
        public IEnumerable<RetentionPeriodDto> RetentionPeriods { get; set; } = new List<RetentionPeriodDto>();

        /// <summary>
        /// 平均留存率
        /// </summary>
        public double AverageRetentionRate { get; set; }

        /// <summary>
        /// 留存趋势
        /// </summary>
        public IEnumerable<RetentionTrendDto> RetentionTrends { get; set; } = new List<RetentionTrendDto>();

        /// <summary>
        /// 流失分析
        /// </summary>
        public ChurnAnalysisDto ChurnAnalysis { get; set; } = new();
    }

    /// <summary>
    /// 转化漏斗DTO
    /// </summary>
    public class ConversionFunnelDto
    {
        /// <summary>
        /// 漏斗名称
        /// </summary>
        public string FunnelName { get; set; } = string.Empty;

        /// <summary>
        /// 漏斗步骤
        /// </summary>
        public IEnumerable<FunnelStepDto> Steps { get; set; } = new List<FunnelStepDto>();

        /// <summary>
        /// 总体转化率
        /// </summary>
        public double OverallConversionRate { get; set; }

        /// <summary>
        /// 平均完成时间
        /// </summary>
        public TimeSpan AverageCompletionTime { get; set; }

        /// <summary>
        /// 流失点分析
        /// </summary>
        public IEnumerable<DropOffAnalysisDto> DropOffPoints { get; set; } = new List<DropOffAnalysisDto>();
    }

    /// <summary>
    /// A/B测试结果DTO
    /// </summary>
    public class ABTestResultDto
    {
        /// <summary>
        /// 测试ID
        /// </summary>
        public Guid TestId { get; set; }

        /// <summary>
        /// 测试名称
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// 测试状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 变体结果
        /// </summary>
        public IEnumerable<TestVariantResultDto> Variants { get; set; } = new List<TestVariantResultDto>();

        /// <summary>
        /// 获胜变体
        /// </summary>
        public string? WinnerVariant { get; set; }

        /// <summary>
        /// 统计显著性
        /// </summary>
        public double StatisticalSignificance { get; set; }

        /// <summary>
        /// 置信区间
        /// </summary>
        public double ConfidenceInterval { get; set; }
    }

    /// <summary>
    /// 定制报告DTO
    /// </summary>
    public class CustomReportDto
    {
        /// <summary>
        /// 报告ID
        /// </summary>
        public Guid ReportId { get; set; }

        /// <summary>
        /// 报告名称
        /// </summary>
        public string ReportName { get; set; } = string.Empty;

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// 数据范围
        /// </summary>
        public DateRangeDto DateRange { get; set; } = new();

        /// <summary>
        /// 报告数据
        /// </summary>
        public object ReportData { get; set; } = new();

        /// <summary>
        /// 图表数据
        /// </summary>
        public IEnumerable<ChartDataDto> Charts { get; set; } = new List<ChartDataDto>();

        /// <summary>
        /// 关键指标
        /// </summary>
        public IEnumerable<KeyMetricDto> KeyMetrics { get; set; } = new List<KeyMetricDto>();

        /// <summary>
        /// 洞察和建议
        /// </summary>
        public IEnumerable<InsightDto> Insights { get; set; } = new List<InsightDto>();
    }

    /// <summary>
    /// 定制报告请求DTO
    /// </summary>
    public class CustomReportRequestDto
    {
        /// <summary>
        /// 报告名称
        /// </summary>
        public string ReportName { get; set; } = string.Empty;

        /// <summary>
        /// 报告类型
        /// </summary>
        public string ReportType { get; set; } = string.Empty;

        /// <summary>
        /// 数据源
        /// </summary>
        public IEnumerable<string> DataSources { get; set; } = new List<string>();

        /// <summary>
        /// 时间范围
        /// </summary>
        public DateRangeDto DateRange { get; set; } = new();

        /// <summary>
        /// 筛选条件
        /// </summary>
        public Dictionary<string, object> Filters { get; set; } = new();

        /// <summary>
        /// 分组字段
        /// </summary>
        public IEnumerable<string> GroupBy { get; set; } = new List<string>();

        /// <summary>
        /// 指标
        /// </summary>
        public IEnumerable<string> Metrics { get; set; } = new List<string>();

        /// <summary>
        /// 排序规则
        /// </summary>
        public IEnumerable<SortRuleDto> SortRules { get; set; } = new List<SortRuleDto>();

        /// <summary>
        /// 限制记录数
        /// </summary>
        public int? Limit { get; set; }
    }

    /// <summary>
    /// 导出请求DTO
    /// </summary>
    public class ExportRequestDto
    {
        /// <summary>
        /// 导出格式
        /// </summary>
        public string ExportFormat { get; set; } = "Excel";

        /// <summary>
        /// 数据范围
        /// </summary>
        public DateRangeDto DateRange { get; set; } = new();

        /// <summary>
        /// 包含的数据类型
        /// </summary>
        public IEnumerable<string> DataTypes { get; set; } = new List<string>();

        /// <summary>
        /// 筛选条件
        /// </summary>
        public Dictionary<string, object> Filters { get; set; } = new();

        /// <summary>
        /// 是否包含原始数据
        /// </summary>
        public bool IncludeRawData { get; set; }

        /// <summary>
        /// 是否包含图表
        /// </summary>
        public bool IncludeCharts { get; set; }
    }

    /// <summary>
    /// 导出结果DTO
    /// </summary>
    public class ExportResultDto
    {
        /// <summary>
        /// 导出ID
        /// </summary>
        public Guid ExportId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 导出状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 记录数量
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// 实时访客DTO
    /// </summary>
    public class RealTimeVisitorDto
    {
        /// <summary>
        /// 当前在线访客数
        /// </summary>
        public int CurrentVisitors { get; set; }

        /// <summary>
        /// 活跃页面
        /// </summary>
        public IEnumerable<ActivePageDto> ActivePages { get; set; } = new List<ActivePageDto>();

        /// <summary>
        /// 访客地理分布
        /// </summary>
        public IEnumerable<VisitorLocationDto> VisitorLocations { get; set; } = new List<VisitorLocationDto>();

        /// <summary>
        /// 流量来源
        /// </summary>
        public IEnumerable<RealTimeTrafficSourceDto> TrafficSources { get; set; } = new List<RealTimeTrafficSourceDto>();

        /// <summary>
        /// 最近访问活动
        /// </summary>
        public IEnumerable<RecentVisitDto> RecentVisits { get; set; } = new List<RecentVisitDto>();

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 性能指标DTO
    /// </summary>
    public class PerformanceMetricsDto
    {
        /// <summary>
        /// 平均页面加载时间
        /// </summary>
        public double AveragePageLoadTime { get; set; }

        /// <summary>
        /// 首屏渲染时间
        /// </summary>
        public double FirstContentfulPaint { get; set; }

        /// <summary>
        /// 最大内容渲染时间
        /// </summary>
        public double LargestContentfulPaint { get; set; }

        /// <summary>
        /// 累计布局偏移
        /// </summary>
        public double CumulativeLayoutShift { get; set; }

        /// <summary>
        /// 首次输入延迟
        /// </summary>
        public double FirstInputDelay { get; set; }

        /// <summary>
        /// 时间到交互
        /// </summary>
        public double TimeToInteractive { get; set; }

        /// <summary>
        /// 性能得分
        /// </summary>
        public int PerformanceScore { get; set; }

        /// <summary>
        /// 速度指数
        /// </summary>
        public double SpeedIndex { get; set; }

        /// <summary>
        /// 按页面分组的性能数据
        /// </summary>
        public IEnumerable<PagePerformanceDto> PagePerformance { get; set; } = new List<PagePerformanceDto>();

        /// <summary>
        /// 按设备类型分组的性能数据
        /// </summary>
        public IEnumerable<DevicePerformanceDto> DevicePerformance { get; set; } = new List<DevicePerformanceDto>();
    }

    /// <summary>
    /// 目标完成情况DTO
    /// </summary>
    public class GoalCompletionDto
    {
        /// <summary>
        /// 目标ID
        /// </summary>
        public Guid GoalId { get; set; }

        /// <summary>
        /// 目标名称
        /// </summary>
        public string GoalName { get; set; } = string.Empty;

        /// <summary>
        /// 目标类型
        /// </summary>
        public string GoalType { get; set; } = string.Empty;

        /// <summary>
        /// 完成次数
        /// </summary>
        public int Completions { get; set; }

        /// <summary>
        /// 完成率
        /// </summary>
        public double CompletionRate { get; set; }

        /// <summary>
        /// 目标价值
        /// </summary>
        public decimal GoalValue { get; set; }

        /// <summary>
        /// 完成趋势
        /// </summary>
        public IEnumerable<GoalCompletionTrendDto> CompletionTrends { get; set; } = new List<GoalCompletionTrendDto>();

        /// <summary>
        /// 路径分析
        /// </summary>
        public IEnumerable<GoalPathDto> CompletionPaths { get; set; } = new List<GoalPathDto>();
    }

    #region 辅助DTO类


    /// <summary>
    /// 页面统计DTO
    /// </summary>
    public class PageStatsDto
    {
        public string PagePath { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public long PageViews { get; set; }
        public int UniquePageViews { get; set; }
        public TimeSpan AverageTimeOnPage { get; set; }
        public double ExitRate { get; set; }
    }

    /// <summary>
    /// 会话时长统计DTO
    /// </summary>
    public class SessionDurationStatsDto
    {
        public string DurationRange { get; set; } = string.Empty;
        public int SessionCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 页面深度统计DTO
    /// </summary>
    public class PageDepthStatsDto
    {
        public int PageDepth { get; set; }
        public int SessionCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 用户路径统计DTO
    /// </summary>
    public class UserPathStatsDto
    {
        public string PathSequence { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public double ConversionRate { get; set; }
    }

    /// <summary>
    /// 操作频率统计DTO
    /// </summary>
    public class ActionFrequencyStatsDto
    {
        public string ActionType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 小时活动统计DTO
    /// </summary>
    public class HourlyActivityStatsDto
    {
        public int Hour { get; set; }
        public int ActivityCount { get; set; }
        public double ActivityPercentage { get; set; }
    }

    /// <summary>
    /// 设备使用统计DTO
    /// </summary>
    public class DeviceUsageStatsDto
    {
        public IEnumerable<DeviceTypeStatsDto> DeviceTypes { get; set; } = new List<DeviceTypeStatsDto>();
        public IEnumerable<BrowserStatsDto> Browsers { get; set; } = new List<BrowserStatsDto>();
        public IEnumerable<OperatingSystemStatsDto> OperatingSystems { get; set; } = new List<OperatingSystemStatsDto>();
    }

    /// <summary>
    /// 用户忠诚度统计DTO
    /// </summary>
    public class UserLoyaltyStatsDto
    {
        public int NewUsers { get; set; }
        public int ReturningUsers { get; set; }
        public double LoyaltyScore { get; set; }
        public IEnumerable<LoyaltySegmentDto> LoyaltySegments { get; set; } = new List<LoyaltySegmentDto>();
    }

    /// <summary>
    /// 内容表现DTO
    /// </summary>
    public class ContentPerformanceDto
    {
        public Guid ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int ShareCount { get; set; }
        public double EngagementRate { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    /// <summary>
    /// 日表现统计DTO
    /// </summary>
    public class DailyPerformanceStatsDto
    {
        public DateTime Date { get; set; }
        public double AverageEngagement { get; set; }
        public long TotalViews { get; set; }
        public int TotalComments { get; set; }
        public int TotalShares { get; set; }
    }

    /// <summary>
    /// 分类表现DTO
    /// </summary>
    public class CategoryPerformanceDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ContentCount { get; set; }
        public long TotalViews { get; set; }
        public double AverageEngagement { get; set; }
    }

    /// <summary>
    /// 标签表现DTO
    /// </summary>
    public class TagPerformanceDto
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public long TotalViews { get; set; }
        public double PopularityScore { get; set; }
    }

    /// <summary>
    /// 作者表现DTO
    /// </summary>
    public class AuthorPerformanceDto
    {
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int ContentCount { get; set; }
        public long TotalViews { get; set; }
        public double AverageEngagement { get; set; }
        public int TotalComments { get; set; }
    }

    /// <summary>
    /// 坐标DTO
    /// </summary>
    public class CoordinatesDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>
    /// 设备类型统计DTO
    /// </summary>
    public class DeviceTypeStatsDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 操作系统统计DTO
    /// </summary>
    public class OperatingSystemStatsDto
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 浏览器统计DTO
    /// </summary>
    public class BrowserStatsDto
    {
        public string Browser { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 屏幕分辨率统计DTO
    /// </summary>
    public class ScreenResolutionStatsDto
    {
        public string Resolution { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 移动设备统计DTO
    /// </summary>
    public class MobileDeviceStatsDto
    {
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 网络类型统计DTO
    /// </summary>
    public class NetworkTypeStatsDto
    {
        public string NetworkType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 留存周期DTO
    /// </summary>
    public class RetentionPeriodDto
    {
        public int Period { get; set; }
        public int RetainedUsers { get; set; }
        public double RetentionRate { get; set; }
    }

    /// <summary>
    /// 留存趋势DTO
    /// </summary>
    public class RetentionTrendDto
    {
        public DateTime Date { get; set; }
        public double RetentionRate { get; set; }
    }

    /// <summary>
    /// 流失分析DTO
    /// </summary>
    public class ChurnAnalysisDto
    {
        public double ChurnRate { get; set; }
        public IEnumerable<ChurnReasonDto> ChurnReasons { get; set; } = new List<ChurnReasonDto>();
        public double PredictedChurnRate { get; set; }
    }

    /// <summary>
    /// 漏斗步骤DTO
    /// </summary>
    public class FunnelStepDto
    {
        public int StepOrder { get; set; }
        public string StepName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public double ConversionRate { get; set; }
        public double DropOffRate { get; set; }
    }

    /// <summary>
    /// 流失点分析DTO
    /// </summary>
    public class DropOffAnalysisDto
    {
        public string StepName { get; set; } = string.Empty;
        public int DropOffCount { get; set; }
        public double DropOffRate { get; set; }
        public IEnumerable<string> PossibleReasons { get; set; } = new List<string>();
    }

    /// <summary>
    /// 测试变体结果DTO
    /// </summary>
    public class TestVariantResultDto
    {
        public string VariantName { get; set; } = string.Empty;
        public int ParticipantCount { get; set; }
        public int ConversionCount { get; set; }
        public double ConversionRate { get; set; }
        public double ConfidenceInterval { get; set; }
    }

    /// <summary>
    /// 日期范围DTO
    /// </summary>
    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// 图表数据DTO
    /// </summary>
    public class ChartDataDto
    {
        public string ChartType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public object Data { get; set; } = new();
        public object Options { get; set; } = new();
    }

    /// <summary>
    /// 关键指标DTO
    /// </summary>
    public class KeyMetricDto
    {
        public string Name { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public string Unit { get; set; } = string.Empty;
        public double? ChangePercent { get; set; }
        public string Trend { get; set; } = string.Empty;
    }

    /// <summary>
    /// 洞察DTO
    /// </summary>
    public class InsightDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    /// <summary>
    /// 排序规则DTO
    /// </summary>
    public class SortRuleDto
    {
        public string Field { get; set; } = string.Empty;
        public string Direction { get; set; } = "asc";
    }

    /// <summary>
    /// 活跃页面DTO
    /// </summary>
    public class ActivePageDto
    {
        public string PagePath { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public int ActiveVisitors { get; set; }
    }

    /// <summary>
    /// 访客位置DTO
    /// </summary>
    public class VisitorLocationDto
    {
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int VisitorCount { get; set; }
    }

    /// <summary>
    /// 实时流量来源DTO
    /// </summary>
    public class RealTimeTrafficSourceDto
    {
        public string Source { get; set; } = string.Empty;
        public int VisitorCount { get; set; }
    }

    /// <summary>
    /// 近期访问DTO
    /// </summary>
    public class RecentVisitDto
    {
        public string PagePath { get; set; } = string.Empty;
        public string VisitorLocation { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 页面性能DTO
    /// </summary>
    public class PagePerformanceDto
    {
        public string PagePath { get; set; } = string.Empty;
        public double LoadTime { get; set; }
        public double PerformanceScore { get; set; }
    }

    /// <summary>
    /// 设备性能DTO
    /// </summary>
    public class DevicePerformanceDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public double AverageLoadTime { get; set; }
        public double PerformanceScore { get; set; }
    }

    /// <summary>
    /// 目标完成趋势DTO
    /// </summary>
    public class GoalCompletionTrendDto
    {
        public DateTime Date { get; set; }
        public int Completions { get; set; }
        public double CompletionRate { get; set; }
    }

    /// <summary>
    /// 目标路径DTO
    /// </summary>
    public class GoalPathDto
    {
        public string PathSequence { get; set; } = string.Empty;
        public int CompletionCount { get; set; }
        public double ConversionRate { get; set; }
    }

    /// <summary>
    /// 忠诚度细分DTO
    /// </summary>
    public class LoyaltySegmentDto
    {
        public string SegmentName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 流失原因DTO
    /// </summary>
    public class ChurnReasonDto
    {
        public string Reason { get; set; } = string.Empty;
        public double Percentage { get; set; }
    }

    #endregion
}