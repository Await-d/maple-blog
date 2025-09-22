namespace MapleBlog.Application.DTOs.Search;

/// <summary>
/// 趋势分析时间周期
/// </summary>
public enum TrendPeriod
{
    /// <summary>
    /// 过去24小时
    /// </summary>
    Daily = 1,

    /// <summary>
    /// 过去7天
    /// </summary>
    Weekly = 7,

    /// <summary>
    /// 过去30天
    /// </summary>
    Monthly = 30,

    /// <summary>
    /// 过去90天
    /// </summary>
    Quarterly = 90,

    /// <summary>
    /// 过去365天
    /// </summary>
    Yearly = 365
}

/// <summary>
/// 搜索分析概览
/// </summary>
public class SearchAnalyticsOverview
{
    /// <summary>
    /// 总搜索次数
    /// </summary>
    public long TotalSearches { get; set; }

    /// <summary>
    /// 唯一搜索词数量
    /// </summary>
    public int UniqueTermsCount { get; set; }

    /// <summary>
    /// 平均搜索响应时间（毫秒）
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// 今日搜索次数
    /// </summary>
    public int TodaySearches { get; set; }

    /// <summary>
    /// 热门搜索词
    /// </summary>
    public IEnumerable<SearchTermStatsDto> TopSearchTerms { get; set; } = new List<SearchTermStatsDto>();

    /// <summary>
    /// 最近搜索趋势
    /// </summary>
    public IEnumerable<SearchTrendDto> RecentTrends { get; set; } = new List<SearchTrendDto>();

    /// <summary>
    /// 搜索成功率
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 平均每次搜索结果数量
    /// </summary>
    public double AverageResultsPerSearch { get; set; }
}

/// <summary>
/// 搜索趋势分析
/// </summary>
public class SearchTrendAnalysis
{
    /// <summary>
    /// 分析周期
    /// </summary>
    public TrendPeriod Period { get; set; }

    /// <summary>
    /// 总搜索次数
    /// </summary>
    public long TotalSearches { get; set; }

    /// <summary>
    /// 与上一周期比较的增长率
    /// </summary>
    public double GrowthRate { get; set; }

    /// <summary>
    /// 时间序列数据
    /// </summary>
    public IEnumerable<SearchTrendDto> TrendData { get; set; } = new List<SearchTrendDto>();

    /// <summary>
    /// 最受欢迎的搜索词
    /// </summary>
    public IEnumerable<SearchTermStatsDto> TrendingTerms { get; set; } = new List<SearchTermStatsDto>();

    /// <summary>
    /// 峰值搜索时间
    /// </summary>
    public DateTime PeakSearchTime { get; set; }

    /// <summary>
    /// 峰值搜索次数
    /// </summary>
    public int PeakSearchCount { get; set; }

    /// <summary>
    /// 搜索模式分析
    /// </summary>
    public string SearchPattern { get; set; } = string.Empty;
}

/// <summary>
/// 查询分析响应
/// </summary>
public class QueryAnalysisResponse
{
    /// <summary>
    /// 查询内容
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 查询复杂度评分
    /// </summary>
    public int ComplexityScore { get; set; }

    /// <summary>
    /// 预期结果数量
    /// </summary>
    public int ExpectedResultCount { get; set; }

    /// <summary>
    /// 建议优化的查询
    /// </summary>
    public string SuggestedOptimizedQuery { get; set; } = string.Empty;

    /// <summary>
    /// 查询关键词
    /// </summary>
    public IEnumerable<string> ExtractedKeywords { get; set; } = new List<string>();

    /// <summary>
    /// 查询意图分析
    /// </summary>
    public string QueryIntent { get; set; } = string.Empty;

    /// <summary>
    /// 语言检测结果
    /// </summary>
    public string DetectedLanguage { get; set; } = "zh-CN";

    /// <summary>
    /// 查询分类
    /// </summary>
    public string QueryCategory { get; set; } = string.Empty;

    /// <summary>
    /// 搜索建议
    /// </summary>
    public IEnumerable<string> SearchSuggestions { get; set; } = new List<string>();
}

/// <summary>
/// 搜索性能分析
/// </summary>
public class SearchPerformanceAnalysis
{
    /// <summary>
    /// 分析时间段
    /// </summary>
    public DateTime AnalysisPeriodStart { get; set; }

    /// <summary>
    /// 分析时间段结束
    /// </summary>
    public DateTime AnalysisPeriodEnd { get; set; }

    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// 最快响应时间（毫秒）
    /// </summary>
    public double FastestResponseTime { get; set; }

    /// <summary>
    /// 最慢响应时间（毫秒）
    /// </summary>
    public double SlowestResponseTime { get; set; }

    /// <summary>
    /// 95%分位响应时间（毫秒）
    /// </summary>
    public double P95ResponseTime { get; set; }

    /// <summary>
    /// 搜索成功率
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// 搜索失败率
    /// </summary>
    public double FailureRate { get; set; }

    /// <summary>
    /// 超时搜索次数
    /// </summary>
    public int TimeoutCount { get; set; }

    /// <summary>
    /// 错误搜索次数
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// 索引命中率
    /// </summary>
    public double IndexHitRate { get; set; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// 性能趋势数据
    /// </summary>
    public IEnumerable<PerformanceDataPoint> PerformanceTrend { get; set; } = new List<PerformanceDataPoint>();
}

/// <summary>
/// 性能数据点
/// </summary>
public class PerformanceDataPoint
{
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public double ResponseTime { get; set; }

    /// <summary>
    /// 搜索次数
    /// </summary>
    public int SearchCount { get; set; }

    /// <summary>
    /// 成功次数
    /// </summary>
    public int SuccessCount { get; set; }
}

/// <summary>
/// 搜索优化建议
/// </summary>
public class SearchOptimizationSuggestion
{
    /// <summary>
    /// 建议类型
    /// </summary>
    public string SuggestionType { get; set; } = string.Empty;

    /// <summary>
    /// 建议标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 建议描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 预期改进效果
    /// </summary>
    public string ExpectedImprovement { get; set; } = string.Empty;

    /// <summary>
    /// 实施难度（1-5）
    /// </summary>
    public int ImplementationDifficulty { get; set; }

    /// <summary>
    /// 优先级（1-5）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 相关查询样例
    /// </summary>
    public IEnumerable<string> RelatedQueries { get; set; } = new List<string>();

    /// <summary>
    /// 建议的具体操作
    /// </summary>
    public IEnumerable<string> RecommendedActions { get; set; } = new List<string>();

    /// <summary>
    /// 影响的搜索词数量
    /// </summary>
    public int AffectedQueriesCount { get; set; }

    /// <summary>
    /// 预计性能提升百分比
    /// </summary>
    public double EstimatedPerformanceGain { get; set; }
}