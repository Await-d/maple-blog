namespace MapleBlog.Admin.DTOs;

/// <summary>
/// 分析报表DTO
/// </summary>
public class AnalyticsReportDto
{
    /// <summary>
    /// 报表ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 报表名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 报表描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 报表类型
    /// </summary>
    public ReportType Type { get; set; }

    /// <summary>
    /// 查询条件
    /// </summary>
    public AnalyticsQueryDto Query { get; set; } = new();

    /// <summary>
    /// 报表数据
    /// </summary>
    public AnalyticsDataDto Data { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 生成耗时（毫秒）
    /// </summary>
    public long GenerationTimeMs { get; set; }

    /// <summary>
    /// 数据记录数
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// 报表状态
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 导出选项
    /// </summary>
    public ExportOptionsDto ExportOptions { get; set; } = new();
}

/// <summary>
/// 分析查询DTO
/// </summary>
public class AnalyticsQueryDto
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 数据维度
    /// </summary>
    public List<string> Dimensions { get; set; } = new();

    /// <summary>
    /// 指标
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// 过滤条件
    /// </summary>
    public Dictionary<string, object> Filters { get; set; } = new();

    /// <summary>
    /// 排序条件
    /// </summary>
    public List<SortCriteriaDto> Sorting { get; set; } = new();

    /// <summary>
    /// 分组条件
    /// </summary>
    public List<string> GroupBy { get; set; } = new();

    /// <summary>
    /// 聚合函数
    /// </summary>
    public Dictionary<string, string> Aggregations { get; set; } = new();

    /// <summary>
    /// 限制结果数量
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// 偏移量
    /// </summary>
    public int Offset { get; set; } = 0;

    /// <summary>
    /// 时间粒度（小时、天、周、月）
    /// </summary>
    public TimeGranularity TimeGranularity { get; set; } = TimeGranularity.Day;

    /// <summary>
    /// 是否包含对比数据
    /// </summary>
    public bool IncludeComparison { get; set; }

    /// <summary>
    /// 对比时间段
    /// </summary>
    public ComparisonPeriodDto? ComparisonPeriod { get; set; }
}

/// <summary>
/// 分析数据DTO
/// </summary>
public class AnalyticsDataDto
{
    /// <summary>
    /// 汇总数据
    /// </summary>
    public Dictionary<string, object> Summary { get; set; } = new();

    /// <summary>
    /// 明细数据
    /// </summary>
    public List<Dictionary<string, object>> Details { get; set; } = new();

    /// <summary>
    /// 时间序列数据
    /// </summary>
    public List<TimeSeriesDataDto> TimeSeries { get; set; } = new();

    /// <summary>
    /// 分布数据
    /// </summary>
    public List<DistributionDataDto> Distribution { get; set; } = new();

    /// <summary>
    /// 对比数据
    /// </summary>
    public ComparisonDataDto? Comparison { get; set; }

    /// <summary>
    /// 趋势分析
    /// </summary>
    public TrendAnalysisDto? TrendAnalysis { get; set; }

    /// <summary>
    /// 异常检测
    /// </summary>
    public List<AnomalyDataDto> Anomalies { get; set; } = new();

    /// <summary>
    /// 预测数据
    /// </summary>
    public List<ForecastDataDto> Forecasts { get; set; } = new();
}

/// <summary>
/// 用户行为分析DTO
/// </summary>
public class UserBehaviorAnalyticsDto
{
    /// <summary>
    /// 用户活跃度分析
    /// </summary>
    public UserActivityAnalysisDto ActivityAnalysis { get; set; } = new();

    /// <summary>
    /// 用户留存分析
    /// </summary>
    public UserRetentionAnalysisDto RetentionAnalysis { get; set; } = new();

    /// <summary>
    /// 用户路径分析
    /// </summary>
    public UserPathAnalysisDto PathAnalysis { get; set; } = new();

    /// <summary>
    /// 用户画像分析
    /// </summary>
    public UserProfileAnalysisDto ProfileAnalysis { get; set; } = new();

    /// <summary>
    /// 用户分群分析
    /// </summary>
    public UserSegmentAnalysisDto SegmentAnalysis { get; set; } = new();
}

/// <summary>
/// 内容表现分析DTO
/// </summary>
public class ContentPerformanceAnalyticsDto
{
    /// <summary>
    /// 内容浏览量分析
    /// </summary>
    public ContentViewAnalysisDto ViewAnalysis { get; set; } = new();

    /// <summary>
    /// 内容互动分析
    /// </summary>
    public ContentEngagementAnalysisDto EngagementAnalysis { get; set; } = new();

    /// <summary>
    /// 内容转化分析
    /// </summary>
    public ContentConversionAnalysisDto ConversionAnalysis { get; set; } = new();

    /// <summary>
    /// 内容质量分析
    /// </summary>
    public ContentQualityAnalysisDto QualityAnalysis { get; set; } = new();

    /// <summary>
    /// SEO表现分析
    /// </summary>
    public SeoPerformanceAnalysisDto SeoAnalysis { get; set; } = new();
}

/// <summary>
/// 网站流量分析DTO
/// </summary>
public class TrafficAnalyticsDto
{
    /// <summary>
    /// 流量来源分析
    /// </summary>
    public TrafficSourceAnalysisDto SourceAnalysis { get; set; } = new();

    /// <summary>
    /// 地理位置分析
    /// </summary>
    public GeographicAnalysisDto GeographicAnalysis { get; set; } = new();

    /// <summary>
    /// 设备和浏览器分析
    /// </summary>
    public DeviceBrowserAnalysisDto DeviceAnalysis { get; set; } = new();

    /// <summary>
    /// 页面性能分析
    /// </summary>
    public PagePerformanceAnalysisDto PerformanceAnalysis { get; set; } = new();

    /// <summary>
    /// 转化漏斗分析
    /// </summary>
    public ConversionFunnelAnalysisDto FunnelAnalysis { get; set; } = new();
}

// 辅助DTO类

/// <summary>
/// 排序条件DTO
/// </summary>
public class SortCriteriaDto
{
    public string Field { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Asc;
}

/// <summary>
/// 对比时间段DTO
/// </summary>
public class ComparisonPeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// 时间序列数据DTO
/// </summary>
public class TimeSeriesDataDto
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Values { get; set; } = new();
}

/// <summary>
/// 分布数据DTO
/// </summary>
public class DistributionDataDto
{
    public string Category { get; set; } = string.Empty;
    public object Value { get; set; } = 0;
    public double Percentage { get; set; }
}

/// <summary>
/// 对比数据DTO
/// </summary>
public class ComparisonDataDto
{
    public Dictionary<string, object> Current { get; set; } = new();
    public Dictionary<string, object> Previous { get; set; } = new();
    public Dictionary<string, object> Changes { get; set; } = new();
    public Dictionary<string, double> ChangePercentages { get; set; } = new();
}

/// <summary>
/// 趋势分析DTO
/// </summary>
public class TrendAnalysisDto
{
    public TrendDirection Direction { get; set; }
    public double Slope { get; set; }
    public double Confidence { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 异常数据DTO
/// </summary>
public class AnomalyDataDto
{
    public DateTime Timestamp { get; set; }
    public string Metric { get; set; } = string.Empty;
    public object ActualValue { get; set; } = 0;
    public object ExpectedValue { get; set; } = 0;
    public double AnomalyScore { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 预测数据DTO
/// </summary>
public class ForecastDataDto
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> PredictedValues { get; set; } = new();
    public Dictionary<string, object> ConfidenceIntervals { get; set; } = new();
}

/// <summary>
/// 导出选项DTO
/// </summary>
public class ExportOptionsDto
{
    public ExportFormat Format { get; set; } = ExportFormat.Json;
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeRawData { get; set; } = false;
    public string FileName { get; set; } = string.Empty;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}

// 具体分析DTO类（简化版本）

public class UserActivityAnalysisDto
{
    public int ActiveUsers { get; set; }
    public int NewUsers { get; set; }
    public int ReturningUsers { get; set; }
    public double AverageSessionDuration { get; set; }
    public List<ActivityPatternDto> Patterns { get; set; } = new();
}

public class UserRetentionAnalysisDto
{
    public double Day1Retention { get; set; }
    public double Day7Retention { get; set; }
    public double Day30Retention { get; set; }
    public List<CohortDataDto> CohortData { get; set; } = new();
}

public class UserPathAnalysisDto
{
    public List<PathStepDto> CommonPaths { get; set; } = new();
    public List<string> EntryPages { get; set; } = new();
    public List<string> ExitPages { get; set; } = new();
}

public class UserProfileAnalysisDto
{
    public Dictionary<string, object> Demographics { get; set; } = new();
    public Dictionary<string, object> Preferences { get; set; } = new();
    public Dictionary<string, object> Behaviors { get; set; } = new();
}

public class UserSegmentAnalysisDto
{
    public List<UserSegmentDto> Segments { get; set; } = new();
    public Dictionary<string, object> SegmentComparison { get; set; } = new();
}

public class ContentViewAnalysisDto
{
    public List<ContentMetricDto> TopContent { get; set; } = new();
    public List<ContentMetricDto> TrendingContent { get; set; } = new();
    public Dictionary<string, object> ViewMetrics { get; set; } = new();
}

public class ContentEngagementAnalysisDto
{
    public Dictionary<string, object> EngagementMetrics { get; set; } = new();
    public List<ContentInteractionDto> Interactions { get; set; } = new();
}

public class ContentConversionAnalysisDto
{
    public Dictionary<string, object> ConversionMetrics { get; set; } = new();
    public List<ConversionPathDto> ConversionPaths { get; set; } = new();
}

public class ContentQualityAnalysisDto
{
    public Dictionary<string, object> QualityScores { get; set; } = new();
    public List<QualityIssueDto> Issues { get; set; } = new();
}

public class SeoPerformanceAnalysisDto
{
    public Dictionary<string, object> SeoMetrics { get; set; } = new();
    public List<KeywordPerformanceDto> Keywords { get; set; } = new();
}

public class TrafficSourceAnalysisDto
{
    public List<TrafficSourceDto> Sources { get; set; } = new();
    public Dictionary<string, object> SourceMetrics { get; set; } = new();
}

public class GeographicAnalysisDto
{
    public List<LocationDataDto> Countries { get; set; } = new();
    public List<LocationDataDto> Cities { get; set; } = new();
}

public class DeviceBrowserAnalysisDto
{
    public List<DeviceDataDto> Devices { get; set; } = new();
    public List<BrowserDataDto> Browsers { get; set; } = new();
}

public class PagePerformanceAnalysisDto
{
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public List<PageSpeedDto> PageSpeeds { get; set; } = new();
}

public class ConversionFunnelAnalysisDto
{
    public List<FunnelStepDto> Steps { get; set; } = new();
    public Dictionary<string, object> FunnelMetrics { get; set; } = new();
}

// 更多辅助DTO类

public class ActivityPatternDto
{
    public string Pattern { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class CohortDataDto
{
    public DateTime CohortDate { get; set; }
    public int CohortSize { get; set; }
    public List<double> RetentionRates { get; set; } = new();
}

public class PathStepDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class UserSegmentDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public Dictionary<string, object> Characteristics { get; set; } = new();
}

public class ContentMetricDto
{
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class ContentInteractionDto
{
    public string InteractionType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Rate { get; set; }
}

public class ConversionPathDto
{
    public List<string> Path { get; set; } = new();
    public int Conversions { get; set; }
    public double ConversionRate { get; set; }
}

public class QualityIssueDto
{
    public string Issue { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public class KeywordPerformanceDto
{
    public string Keyword { get; set; } = string.Empty;
    public int Ranking { get; set; }
    public int Clicks { get; set; }
    public int Impressions { get; set; }
    public double Ctr { get; set; }
}

public class TrafficSourceDto
{
    public string Source { get; set; } = string.Empty;
    public string Medium { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int Users { get; set; }
    public double BounceRate { get; set; }
}

public class LocationDataDto
{
    public string Location { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public int Users { get; set; }
    public double Percentage { get; set; }
}

public class DeviceDataDto
{
    public string DeviceType { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public double Percentage { get; set; }
}

public class BrowserDataDto
{
    public string Browser { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public double Percentage { get; set; }
}

public class PageSpeedDto
{
    public string Url { get; set; } = string.Empty;
    public double LoadTime { get; set; }
    public double FirstContentfulPaint { get; set; }
    public double LargestContentfulPaint { get; set; }
    public double CumulativeLayoutShift { get; set; }
}

public class FunnelStepDto
{
    public string StepName { get; set; } = string.Empty;
    public int Users { get; set; }
    public double ConversionRate { get; set; }
    public double DropoffRate { get; set; }
}

// 枚举

/// <summary>
/// 报表类型
/// </summary>
public enum ReportType
{
    UserBehavior,
    ContentPerformance,
    TrafficAnalysis,
    ConversionFunnel,
    SeoPerformance,
    CustomReport
}

/// <summary>
/// 报表状态
/// </summary>
public enum ReportStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// 时间粒度
/// </summary>
public enum TimeGranularity
{
    Hour,
    Day,
    Week,
    Month,
    Quarter,
    Year
}

/// <summary>
/// 排序方向
/// </summary>
public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>
/// 趋势方向
/// </summary>
public enum TrendDirection
{
    Up,
    Down,
    Stable,
    Volatile
}

/// <summary>
/// 导出格式
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Excel,
    Pdf
}