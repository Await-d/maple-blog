using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MapleBlog.Admin.DTOs;
using MapleBlog.Infrastructure.Data;
using System.Text.Json;
using System.Linq.Expressions;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 综合数据分析服务
/// </summary>
public class AnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly TimeSpan _defaultCacheDuration = TimeSpan.FromMinutes(15);

    public AnalyticsService(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 生成分析报表
    /// </summary>
    public async Task<AnalyticsReportDto> GenerateReportAsync(
        ReportType reportType,
        AnalyticsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var report = new AnalyticsReportDto
        {
            Name = $"{reportType} Report",
            Type = reportType,
            Query = query,
            Status = ReportStatus.Processing
        };

        try
        {
            _logger.LogInformation("Generating {ReportType} report for period {StartDate} to {EndDate}",
                reportType, query.StartDate, query.EndDate);

            // 尝试从缓存获取
            var cacheKey = GenerateCacheKey(reportType, query);
            var cachedData = await GetCachedDataAsync<AnalyticsDataDto>(cacheKey, cancellationToken);

            if (cachedData != null)
            {
                report.Data = cachedData;
                report.Status = ReportStatus.Completed;
                report.GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("Report retrieved from cache in {Time}ms", report.GenerationTimeMs);
                return report;
            }

            // 根据报表类型生成数据
            report.Data = reportType switch
            {
                ReportType.UserBehavior => await GenerateUserBehaviorAnalyticsAsync(query, cancellationToken),
                ReportType.ContentPerformance => await GenerateContentPerformanceAnalyticsAsync(query, cancellationToken),
                ReportType.TrafficAnalysis => await GenerateTrafficAnalyticsAsync(query, cancellationToken),
                ReportType.ConversionFunnel => await GenerateConversionFunnelAnalyticsAsync(query, cancellationToken),
                ReportType.SeoPerformance => await GenerateSeoPerformanceAnalyticsAsync(query, cancellationToken),
                _ => await GenerateCustomAnalyticsAsync(query, cancellationToken)
            };

            report.RecordCount = report.Data.Details?.Count ?? 0;
            report.Status = ReportStatus.Completed;
            report.GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // 缓存结果
            await SetCachedDataAsync(cacheKey, report.Data, cancellationToken);

            _logger.LogInformation("Report generated successfully in {Time}ms with {Count} records",
                report.GenerationTimeMs, report.RecordCount);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating {ReportType} report", reportType);
            report.Status = ReportStatus.Failed;
            report.ErrorMessage = ex.Message;
            report.GenerationTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return report;
        }
    }

    /// <summary>
    /// 用户行为分析
    /// </summary>
    public async Task<UserBehaviorAnalyticsDto> GetUserBehaviorAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var analytics = new UserBehaviorAnalyticsDto();

        // 用户活跃度分析
        analytics.ActivityAnalysis = await AnalyzeUserActivityAsync(startDate, endDate, cancellationToken);

        // 用户留存分析
        analytics.RetentionAnalysis = await AnalyzeUserRetentionAsync(startDate, endDate, cancellationToken);

        // 用户路径分析
        analytics.PathAnalysis = await AnalyzeUserPathsAsync(startDate, endDate, cancellationToken);

        // 用户画像分析
        analytics.ProfileAnalysis = await AnalyzeUserProfilesAsync(startDate, endDate, cancellationToken);

        // 用户分群分析
        analytics.SegmentAnalysis = await AnalyzeUserSegmentsAsync(startDate, endDate, cancellationToken);

        return analytics;
    }

    /// <summary>
    /// 内容表现分析
    /// </summary>
    public async Task<ContentPerformanceAnalyticsDto> GetContentPerformanceAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var analytics = new ContentPerformanceAnalyticsDto();

        // 内容浏览量分析
        analytics.ViewAnalysis = await AnalyzeContentViewsAsync(startDate, endDate, cancellationToken);

        // 内容互动分析
        analytics.EngagementAnalysis = await AnalyzeContentEngagementAsync(startDate, endDate, cancellationToken);

        // 内容转化分析
        analytics.ConversionAnalysis = await AnalyzeContentConversionAsync(startDate, endDate, cancellationToken);

        // 内容质量分析
        analytics.QualityAnalysis = await AnalyzeContentQualityAsync(startDate, endDate, cancellationToken);

        // SEO表现分析
        analytics.SeoAnalysis = await AnalyzeSeoPerformanceAsync(startDate, endDate, cancellationToken);

        return analytics;
    }

    /// <summary>
    /// 网站流量分析
    /// </summary>
    public async Task<TrafficAnalyticsDto> GetTrafficAnalyticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var analytics = new TrafficAnalyticsDto();

        // 流量来源分析
        analytics.SourceAnalysis = await AnalyzeTrafficSourcesAsync(startDate, endDate, cancellationToken);

        // 地理位置分析
        analytics.GeographicAnalysis = await AnalyzeGeographicDataAsync(startDate, endDate, cancellationToken);

        // 设备和浏览器分析
        analytics.DeviceAnalysis = await AnalyzeDeviceBrowserDataAsync(startDate, endDate, cancellationToken);

        // 页面性能分析
        analytics.PerformanceAnalysis = await AnalyzePagePerformanceAsync(startDate, endDate, cancellationToken);

        // 转化漏斗分析
        analytics.FunnelAnalysis = await AnalyzeConversionFunnelAsync(startDate, endDate, cancellationToken);

        return analytics;
    }

    /// <summary>
    /// 执行多维度数据查询
    /// </summary>
    public async Task<AnalyticsDataDto> ExecuteMultiDimensionalQueryAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var result = new AnalyticsDataDto();

        // 构建基础查询
        var baseQuery = BuildBaseQuery(query);

        // 应用维度分组
        if (query.Dimensions.Any())
        {
            result.Distribution = await ExecuteDimensionalAnalysisAsync(baseQuery, query.Dimensions, cancellationToken);
        }

        // 应用时间序列分析
        if (query.TimeGranularity != TimeGranularity.Day || query.IncludeComparison)
        {
            result.TimeSeries = await ExecuteTimeSeriesAnalysisAsync(baseQuery, query, cancellationToken);
        }

        // 应用聚合函数
        if (query.Aggregations.Any())
        {
            result.Summary = await ExecuteAggregationsAsync(baseQuery, query.Aggregations, cancellationToken);
        }

        // 执行对比分析
        if (query.IncludeComparison && query.ComparisonPeriod != null)
        {
            result.Comparison = await ExecuteComparisonAnalysisAsync(query, cancellationToken);
        }

        // 执行趋势分析
        result.TrendAnalysis = await AnalyzeTrendsAsync(result.TimeSeries, cancellationToken);

        // 执行异常检测
        result.Anomalies = await DetectAnomaliesAsync(result.TimeSeries, cancellationToken);

        // 生成预测数据
        if (query.Metrics.Contains("forecast"))
        {
            result.Forecasts = await GenerateForecastsAsync(result.TimeSeries, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// 执行时间序列分析
    /// </summary>
    public async Task<List<TimeSeriesDataDto>> ExecuteTimeSeriesAnalysisAsync(
        AnalyticsQueryDto query,
        string metricName,
        CancellationToken cancellationToken = default)
    {
        var timeSeries = new List<TimeSeriesDataDto>();

        // 根据时间粒度确定分组间隔
        var interval = GetTimeInterval(query.TimeGranularity);
        var currentDate = query.StartDate;

        while (currentDate <= query.EndDate)
        {
            var nextDate = AddInterval(currentDate, interval);

            // 查询该时间段的数据
            var values = await GetMetricValuesForPeriodAsync(
                metricName,
                currentDate,
                nextDate > query.EndDate ? query.EndDate : nextDate,
                cancellationToken);

            timeSeries.Add(new TimeSeriesDataDto
            {
                Timestamp = currentDate,
                Values = values
            });

            currentDate = nextDate;
        }

        return timeSeries;
    }

    /// <summary>
    /// 执行同期对比分析
    /// </summary>
    public async Task<ComparisonDataDto> ExecuteComparisonAnalysisAsync(
        AnalyticsQueryDto currentQuery,
        AnalyticsQueryDto previousQuery,
        CancellationToken cancellationToken = default)
    {
        var comparison = new ComparisonDataDto();

        // 获取当前期数据
        var currentData = await ExecuteAggregationsAsync(
            BuildBaseQuery(currentQuery),
            currentQuery.Aggregations,
            cancellationToken);

        // 获取对比期数据
        var previousData = await ExecuteAggregationsAsync(
            BuildBaseQuery(previousQuery),
            previousQuery.Aggregations,
            cancellationToken);

        comparison.Current = currentData;
        comparison.Previous = previousData;

        // 计算变化量和变化率
        comparison.Changes = new Dictionary<string, object>();
        comparison.ChangePercentages = new Dictionary<string, double>();

        foreach (var metric in currentData.Keys)
        {
            if (previousData.ContainsKey(metric))
            {
                var currentValue = Convert.ToDouble(currentData[metric]);
                var previousValue = Convert.ToDouble(previousData[metric]);

                comparison.Changes[metric] = currentValue - previousValue;
                comparison.ChangePercentages[metric] = previousValue != 0
                    ? ((currentValue - previousValue) / previousValue) * 100
                    : 0;
            }
        }

        return comparison;
    }

    // 私有辅助方法

    private async Task<AnalyticsDataDto> GenerateUserBehaviorAnalyticsAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        var data = new AnalyticsDataDto();

        // 查询用户行为数据
        var users = await _context.Users
            .Where(u => u.CreatedAt >= query.StartDate && u.CreatedAt <= query.EndDate)
            .Include(u => u.Posts)
            .Include(u => u.Comments)
            .ToListAsync(cancellationToken);

        // 汇总数据
        data.Summary = new Dictionary<string, object>
        {
            ["TotalUsers"] = users.Count,
            ["ActiveUsers"] = users.Count(u => u.LastActivityAt >= query.StartDate),
            ["NewUsers"] = users.Count(u => u.CreatedAt >= query.StartDate),
            ["AveragePosts"] = users.Any() ? users.Average(u => u.Posts.Count) : 0,
            ["AverageComments"] = users.Any() ? users.Average(u => u.Comments.Count) : 0
        };

        // 明细数据
        data.Details = users.Select(u => new Dictionary<string, object>
        {
            ["UserId"] = u.Id,
            ["Username"] = u.Username,
            ["Email"] = u.Email,
            ["RegisterDate"] = u.CreatedAt,
            ["LastActivity"] = u.LastActivityAt,
            ["PostCount"] = u.Posts.Count,
            ["CommentCount"] = u.Comments.Count
        }).ToList();

        // 时间序列数据
        data.TimeSeries = await GenerateTimeSeriesDataAsync(
            query.StartDate,
            query.EndDate,
            query.TimeGranularity,
            async (start, end) => new Dictionary<string, object>
            {
                ["NewUsers"] = await _context.Users.CountAsync(
                    u => u.CreatedAt >= start && u.CreatedAt < end, cancellationToken),
                ["ActiveUsers"] = await _context.Users.CountAsync(
                    u => u.LastActivityAt >= start && u.LastActivityAt < end, cancellationToken)
            });

        return data;
    }

    private async Task<AnalyticsDataDto> GenerateContentPerformanceAnalyticsAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        var data = new AnalyticsDataDto();

        // 查询内容数据
        var posts = await _context.Posts
            .Where(p => p.CreatedAt >= query.StartDate && p.CreatedAt <= query.EndDate)
            .Include(p => p.Comments)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .ToListAsync(cancellationToken);

        // 汇总数据
        data.Summary = new Dictionary<string, object>
        {
            ["TotalPosts"] = posts.Count,
            ["PublishedPosts"] = posts.Count(p => p.Status == MapleBlog.Domain.Enums.PostStatus.Published),
            ["TotalViews"] = posts.Sum(p => p.ViewCount),
            ["AverageViews"] = posts.Any() ? posts.Average(p => p.ViewCount) : 0,
            ["TotalComments"] = posts.Sum(p => p.Comments.Count),
            ["AverageReadTime"] = posts.Any() ? posts.Average(p => CalculateReadTime(p.Content)) : 0
        };

        // 明细数据
        data.Details = posts.Select(p => new Dictionary<string, object>
        {
            ["PostId"] = p.Id,
            ["Title"] = p.Title,
            ["Author"] = p.Author?.Username ?? "Unknown",
            ["PublishDate"] = p.PublishedAt ?? p.CreatedAt,
            ["ViewCount"] = p.ViewCount,
            ["CommentCount"] = p.Comments.Count,
            ["Tags"] = string.Join(", ", p.PostTags.Select(pt => pt.Tag.Name))
        }).ToList();

        // 分布数据 - 按分类统计
        var categoryDistribution = posts
            .GroupBy(p => p.Category?.Name ?? "Uncategorized")
            .Select(g => new DistributionDataDto
            {
                Category = g.Key,
                Value = g.Count(),
                Percentage = posts.Any() ? (double)g.Count() / posts.Count * 100 : 0
            })
            .ToList();

        data.Distribution = categoryDistribution;

        return data;
    }

    private async Task<AnalyticsDataDto> GenerateTrafficAnalyticsAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        var data = new AnalyticsDataDto();

        // 查询访问数据（这里使用 SearchQueries 作为访问日志的示例）
        var searches = await _context.SearchQueries
            .Where(s => s.SearchedAt >= query.StartDate && s.SearchedAt <= query.EndDate)
            .ToListAsync(cancellationToken);

        // 汇总数据
        data.Summary = new Dictionary<string, object>
        {
            ["TotalSearches"] = searches.Count,
            ["UniqueQueries"] = searches.Select(s => s.Query).Distinct().Count(),
            ["AverageResultsReturned"] = searches.Any() ? searches.Average(s => s.ResultsReturned) : 0,
            ["SearchesWithResults"] = searches.Count(s => s.ResultsReturned > 0)
        };

        // 热门搜索词
        var topSearches = searches
            .GroupBy(s => s.Query)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new Dictionary<string, object>
            {
                ["Query"] = g.Key,
                ["Count"] = g.Count(),
                ["AverageResults"] = g.Average(s => s.ResultsReturned)
            })
            .ToList();

        data.Details = topSearches;

        return data;
    }

    private async Task<AnalyticsDataDto> GenerateConversionFunnelAnalyticsAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        var data = new AnalyticsDataDto();

        // 定义转化漏斗步骤
        var funnelSteps = new[]
        {
            "Visit",
            "View Content",
            "Engage",
            "Register",
            "Create Content"
        };

        // 模拟漏斗数据（实际应该基于真实的用户行为数据）
        var funnelData = new List<FunnelStepDto>();
        var previousUsers = 10000; // 初始访问用户数

        foreach (var step in funnelSteps)
        {
            var dropoffRate = Random.Shared.Next(10, 40) / 100.0;
            var currentUsers = (int)(previousUsers * (1 - dropoffRate));

            funnelData.Add(new FunnelStepDto
            {
                StepName = step,
                Users = currentUsers,
                ConversionRate = previousUsers > 0 ? (double)currentUsers / previousUsers * 100 : 0,
                DropoffRate = dropoffRate * 100
            });

            previousUsers = currentUsers;
        }

        data.Summary = new Dictionary<string, object>
        {
            ["TotalSteps"] = funnelSteps.Length,
            ["OverallConversionRate"] = funnelData.Last().Users / 10000.0 * 100,
            ["BiggestDropoff"] = funnelData.OrderByDescending(f => f.DropoffRate).First().StepName
        };

        data.Details = funnelData.Select(f => new Dictionary<string, object>
        {
            ["Step"] = f.StepName,
            ["Users"] = f.Users,
            ["ConversionRate"] = f.ConversionRate,
            ["DropoffRate"] = f.DropoffRate
        }).ToList();

        return data;
    }

    private async Task<AnalyticsDataDto> GenerateSeoPerformanceAnalyticsAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        var data = new AnalyticsDataDto();

        // 查询SEO相关数据
        var posts = await _context.Posts
            .Where(p => p.PublishedAt >= query.StartDate && p.PublishedAt <= query.EndDate)
            .ToListAsync(cancellationToken);

        // 分析SEO指标
        data.Summary = new Dictionary<string, object>
        {
            ["PostsWithMetaDescription"] = posts.Count(p => !string.IsNullOrEmpty(p.MetaDescription)),
            ["AverageMetaDescriptionLength"] = posts
                .Where(p => !string.IsNullOrEmpty(p.MetaDescription))
                .Select(p => p.MetaDescription.Length)
                .DefaultIfEmpty(0)
                .Average(),
            ["PostsWithSeoKeywords"] = posts.Count(p => !string.IsNullOrEmpty(p.MetaKeywords)),
            ["AverageTitleLength"] = posts.Select(p => p.Title.Length).DefaultIfEmpty(0).Average(),
            ["PostsWithOptimalTitleLength"] = posts.Count(p => p.Title.Length >= 30 && p.Title.Length <= 60)
        };

        return data;
    }

    private async Task<AnalyticsDataDto> GenerateCustomAnalyticsAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        var data = new AnalyticsDataDto();

        // 自定义分析逻辑
        data.Summary = new Dictionary<string, object>
        {
            ["CustomMetric1"] = "Value1",
            ["CustomMetric2"] = "Value2"
        };

        data.Details = new List<Dictionary<string, object>>();

        return data;
    }

    private IQueryable<object> BuildBaseQuery(AnalyticsQueryDto query)
    {
        // 根据查询条件构建基础查询
        // 这是一个简化的示例，实际实现需要根据具体需求动态构建查询
        return _context.Posts.AsQueryable();
    }

    private async Task<List<DistributionDataDto>> ExecuteDimensionalAnalysisAsync(
        IQueryable<object> baseQuery,
        List<string> dimensions,
        CancellationToken cancellationToken)
    {
        // 执行维度分析
        var distribution = new List<DistributionDataDto>();

        // 这里需要根据实际的维度动态构建分组查询
        // 示例实现
        foreach (var dimension in dimensions)
        {
            distribution.Add(new DistributionDataDto
            {
                Category = dimension,
                Value = Random.Shared.Next(100, 1000),
                Percentage = Random.Shared.Next(1, 100)
            });
        }

        return distribution;
    }

    private async Task<List<TimeSeriesDataDto>> ExecuteTimeSeriesAnalysisAsync(
        IQueryable<object> baseQuery,
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        return await GenerateTimeSeriesDataAsync(
            query.StartDate,
            query.EndDate,
            query.TimeGranularity,
            async (start, end) => new Dictionary<string, object>
            {
                ["Value"] = Random.Shared.Next(100, 1000)
            });
    }

    private async Task<Dictionary<string, object>> ExecuteAggregationsAsync(
        IQueryable<object> baseQuery,
        Dictionary<string, string> aggregations,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object>();

        foreach (var aggregation in aggregations)
        {
            result[aggregation.Key] = aggregation.Value switch
            {
                "sum" => Random.Shared.Next(1000, 10000),
                "avg" => Random.Shared.Next(10, 100),
                "min" => Random.Shared.Next(1, 10),
                "max" => Random.Shared.Next(100, 1000),
                "count" => Random.Shared.Next(10, 1000),
                _ => 0
            };
        }

        return result;
    }

    private async Task<ComparisonDataDto> ExecuteComparisonAnalysisAsync(
        AnalyticsQueryDto query,
        CancellationToken cancellationToken)
    {
        if (query.ComparisonPeriod == null)
            return new ComparisonDataDto();

        var previousQuery = new AnalyticsQueryDto
        {
            StartDate = query.ComparisonPeriod.StartDate,
            EndDate = query.ComparisonPeriod.EndDate,
            Aggregations = query.Aggregations
        };

        return await ExecuteComparisonAnalysisAsync(query, previousQuery, cancellationToken);
    }

    private async Task<TrendAnalysisDto> AnalyzeTrendsAsync(
        List<TimeSeriesDataDto> timeSeries,
        CancellationToken cancellationToken)
    {
        if (timeSeries == null || !timeSeries.Any())
            return new TrendAnalysisDto { Direction = TrendDirection.Stable };

        // 简单的趋势分析
        var values = timeSeries
            .Where(ts => ts.Values.ContainsKey("Value"))
            .Select(ts => Convert.ToDouble(ts.Values["Value"]))
            .ToList();

        if (values.Count < 2)
            return new TrendAnalysisDto { Direction = TrendDirection.Stable };

        var firstHalf = values.Take(values.Count / 2).Average();
        var secondHalf = values.Skip(values.Count / 2).Average();

        var changePercent = (secondHalf - firstHalf) / firstHalf * 100;

        return new TrendAnalysisDto
        {
            Direction = changePercent > 5 ? TrendDirection.Up :
                       changePercent < -5 ? TrendDirection.Down :
                       TrendDirection.Stable,
            Slope = changePercent,
            Confidence = Math.Min(Math.Abs(changePercent) / 10, 1),
            Description = $"Trend is {(changePercent > 0 ? "increasing" : "decreasing")} by {Math.Abs(changePercent):F2}%"
        };
    }

    private async Task<List<AnomalyDataDto>> DetectAnomaliesAsync(
        List<TimeSeriesDataDto> timeSeries,
        CancellationToken cancellationToken)
    {
        var anomalies = new List<AnomalyDataDto>();

        if (timeSeries == null || !timeSeries.Any())
            return anomalies;

        // 简单的异常检测算法（基于标准差）
        var values = timeSeries
            .Where(ts => ts.Values.ContainsKey("Value"))
            .Select(ts => Convert.ToDouble(ts.Values["Value"]))
            .ToList();

        if (values.Count < 3)
            return anomalies;

        var mean = values.Average();
        var stdDev = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / values.Count);

        for (int i = 0; i < timeSeries.Count; i++)
        {
            if (timeSeries[i].Values.TryGetValue("Value", out var value))
            {
                var doubleValue = Convert.ToDouble(value);
                var zScore = Math.Abs((doubleValue - mean) / stdDev);

                if (zScore > 2) // 2个标准差之外视为异常
                {
                    anomalies.Add(new AnomalyDataDto
                    {
                        Timestamp = timeSeries[i].Timestamp,
                        Metric = "Value",
                        ActualValue = doubleValue,
                        ExpectedValue = mean,
                        AnomalyScore = zScore,
                        Description = $"Value {doubleValue} is {zScore:F2} standard deviations from mean"
                    });
                }
            }
        }

        return anomalies;
    }

    private async Task<List<ForecastDataDto>> GenerateForecastsAsync(
        List<TimeSeriesDataDto> timeSeries,
        CancellationToken cancellationToken)
    {
        var forecasts = new List<ForecastDataDto>();

        if (timeSeries == null || timeSeries.Count < 3)
            return forecasts;

        // 简单的线性预测
        var values = timeSeries
            .Where(ts => ts.Values.ContainsKey("Value"))
            .Select(ts => Convert.ToDouble(ts.Values["Value"]))
            .ToList();

        var lastValue = values.Last();
        var trend = values.Count > 1 ? values.Last() - values[values.Count - 2] : 0;

        // 预测未来5个时间点
        for (int i = 1; i <= 5; i++)
        {
            var forecastValue = lastValue + (trend * i);
            var confidence = Math.Max(0, 1 - (i * 0.1)); // 置信度随时间递减

            forecasts.Add(new ForecastDataDto
            {
                Timestamp = timeSeries.Last().Timestamp.AddDays(i),
                PredictedValues = new Dictionary<string, object> { ["Value"] = forecastValue },
                ConfidenceIntervals = new Dictionary<string, object>
                {
                    ["Lower"] = forecastValue * (1 - (1 - confidence)),
                    ["Upper"] = forecastValue * (1 + (1 - confidence))
                }
            });
        }

        return forecasts;
    }

    private async Task<UserActivityAnalysisDto> AnalyzeUserActivityAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var activeUsers = await _context.Users
            .CountAsync(u => u.LastActivityAt >= startDate && u.LastActivityAt <= endDate, cancellationToken);

        var newUsers = await _context.Users
            .CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate, cancellationToken);

        return new UserActivityAnalysisDto
        {
            ActiveUsers = activeUsers,
            NewUsers = newUsers,
            ReturningUsers = Math.Max(0, activeUsers - newUsers),
            AverageSessionDuration = Random.Shared.Next(5, 30), // 分钟
            Patterns = new List<ActivityPatternDto>
            {
                new() { Pattern = "Morning (6-12)", Count = Random.Shared.Next(100, 500), Percentage = 35 },
                new() { Pattern = "Afternoon (12-18)", Count = Random.Shared.Next(100, 500), Percentage = 40 },
                new() { Pattern = "Evening (18-24)", Count = Random.Shared.Next(100, 500), Percentage = 25 }
            }
        };
    }

    private async Task<UserRetentionAnalysisDto> AnalyzeUserRetentionAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // 计算留存率（简化实现）
        var totalNewUsers = await _context.Users
            .CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate, cancellationToken);

        return new UserRetentionAnalysisDto
        {
            Day1Retention = 80.5,
            Day7Retention = 45.3,
            Day30Retention = 25.7,
            CohortData = GenerateCohortData(startDate, endDate)
        };
    }

    private List<CohortDataDto> GenerateCohortData(DateTime startDate, DateTime endDate)
    {
        var cohortData = new List<CohortDataDto>();
        var current = startDate;

        while (current <= endDate)
        {
            cohortData.Add(new CohortDataDto
            {
                CohortDate = current,
                CohortSize = Random.Shared.Next(50, 200),
                RetentionRates = new List<double> { 100, 80, 60, 45, 35, 30, 25 }
            });
            current = current.AddDays(7);
        }

        return cohortData;
    }

    private async Task<UserPathAnalysisDto> AnalyzeUserPathsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new UserPathAnalysisDto
        {
            CommonPaths = new List<PathStepDto>
            {
                new() { From = "Home", To = "Blog", Count = 500, Percentage = 45 },
                new() { From = "Blog", To = "Post", Count = 350, Percentage = 70 },
                new() { From = "Post", To = "Comment", Count = 100, Percentage = 28 }
            },
            EntryPages = new List<string> { "/", "/blog", "/about" },
            ExitPages = new List<string> { "/contact", "/blog/post-1", "/about" }
        };
    }

    private async Task<UserProfileAnalysisDto> AnalyzeUserProfilesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new UserProfileAnalysisDto
        {
            Demographics = new Dictionary<string, object>
            {
                ["AverageAge"] = 32,
                ["MalePercentage"] = 55,
                ["FemalePercentage"] = 45
            },
            Preferences = new Dictionary<string, object>
            {
                ["PreferredContent"] = "Technology",
                ["AverageReadTime"] = "5 minutes"
            },
            Behaviors = new Dictionary<string, object>
            {
                ["MostActiveTime"] = "Evening",
                ["DevicePreference"] = "Mobile"
            }
        };
    }

    private async Task<UserSegmentAnalysisDto> AnalyzeUserSegmentsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new UserSegmentAnalysisDto
        {
            Segments = new List<UserSegmentDto>
            {
                new()
                {
                    Name = "Power Users",
                    Description = "Highly engaged users with frequent activity",
                    UserCount = 150,
                    Characteristics = new Dictionary<string, object>
                    {
                        ["AvgPostsPerWeek"] = 5,
                        ["AvgCommentsPerWeek"] = 15
                    }
                },
                new()
                {
                    Name = "Casual Readers",
                    Description = "Users who primarily consume content",
                    UserCount = 800,
                    Characteristics = new Dictionary<string, object>
                    {
                        ["AvgPostsPerWeek"] = 0,
                        ["AvgCommentsPerWeek"] = 2
                    }
                }
            },
            SegmentComparison = new Dictionary<string, object>
            {
                ["MostValuableSegment"] = "Power Users",
                ["LargestSegment"] = "Casual Readers"
            }
        };
    }

    private async Task<ContentViewAnalysisDto> AnalyzeContentViewsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var postsData = await _context.Posts
            .Where(p => p.PublishedAt >= startDate && p.PublishedAt <= endDate)
            .OrderByDescending(p => p.ViewCount)
            .Take(10)
            .Select(p => new 
            {
                p.Id,
                p.Title,
                p.Slug,
                p.ViewCount,
                CommentCount = p.Comments.Count
            })
            .ToListAsync(cancellationToken);

        var topPosts = postsData.Select(p => new ContentMetricDto
        {
            ContentId = p.Id,
            Title = p.Title,
            Url = $"/blog/{p.Slug}",
            Metrics = new Dictionary<string, object>
            {
                ["Views"] = p.ViewCount,
                ["Comments"] = p.CommentCount
            }
        }).ToList();

        return new ContentViewAnalysisDto
        {
            TopContent = topPosts,
            TrendingContent = topPosts.Take(5).ToList(),
            ViewMetrics = new Dictionary<string, object>
            {
                ["TotalViews"] = topPosts.Sum(p => Convert.ToInt32(p.Metrics["Views"])),
                ["AverageViews"] = topPosts.Any() ? topPosts.Average(p => Convert.ToInt32(p.Metrics["Views"])) : 0
            }
        };
    }

    private async Task<ContentEngagementAnalysisDto> AnalyzeContentEngagementAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new ContentEngagementAnalysisDto
        {
            EngagementMetrics = new Dictionary<string, object>
            {
                ["AverageEngagementRate"] = 15.5,
                ["CommentRate"] = 8.3,
                ["ShareRate"] = 5.2
            },
            Interactions = new List<ContentInteractionDto>
            {
                new() { InteractionType = "Comment", Count = 450, Rate = 8.3 },
                new() { InteractionType = "Like", Count = 1200, Rate = 22.1 },
                new() { InteractionType = "Share", Count = 280, Rate = 5.2 }
            }
        };
    }

    private async Task<ContentConversionAnalysisDto> AnalyzeContentConversionAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new ContentConversionAnalysisDto
        {
            ConversionMetrics = new Dictionary<string, object>
            {
                ["SignupConversionRate"] = 3.5,
                ["SubscriptionConversionRate"] = 1.8
            },
            ConversionPaths = new List<ConversionPathDto>
            {
                new()
                {
                    Path = new List<string> { "Blog", "Post", "Signup" },
                    Conversions = 45,
                    ConversionRate = 3.5
                }
            }
        };
    }

    private async Task<ContentQualityAnalysisDto> AnalyzeContentQualityAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new ContentQualityAnalysisDto
        {
            QualityScores = new Dictionary<string, object>
            {
                ["ReadabilityScore"] = 75,
                ["SEOScore"] = 82,
                ["EngagementScore"] = 68
            },
            Issues = new List<QualityIssueDto>
            {
                new()
                {
                    Issue = "Missing meta descriptions",
                    Severity = "Medium",
                    Recommendation = "Add meta descriptions to improve SEO"
                }
            }
        };
    }

    private async Task<SeoPerformanceAnalysisDto> AnalyzeSeoPerformanceAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new SeoPerformanceAnalysisDto
        {
            SeoMetrics = new Dictionary<string, object>
            {
                ["AveragePosition"] = 12.5,
                ["ClickThroughRate"] = 2.8,
                ["Impressions"] = 15000
            },
            Keywords = new List<KeywordPerformanceDto>
            {
                new()
                {
                    Keyword = "blog platform",
                    Ranking = 5,
                    Clicks = 250,
                    Impressions = 5000,
                    Ctr = 5.0
                }
            }
        };
    }

    private async Task<TrafficSourceAnalysisDto> AnalyzeTrafficSourcesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new TrafficSourceAnalysisDto
        {
            Sources = new List<TrafficSourceDto>
            {
                new()
                {
                    Source = "Google",
                    Medium = "Organic",
                    Sessions = 5000,
                    Users = 4500,
                    BounceRate = 35.5
                },
                new()
                {
                    Source = "Direct",
                    Medium = "None",
                    Sessions = 3000,
                    Users = 2800,
                    BounceRate = 28.3
                }
            },
            SourceMetrics = new Dictionary<string, object>
            {
                ["TopSource"] = "Google",
                ["OrganicPercentage"] = 55
            }
        };
    }

    private async Task<GeographicAnalysisDto> AnalyzeGeographicDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new GeographicAnalysisDto
        {
            Countries = new List<LocationDataDto>
            {
                new() { Location = "United States", Sessions = 3000, Users = 2800, Percentage = 35 },
                new() { Location = "China", Sessions = 2000, Users = 1900, Percentage = 23 }
            },
            Cities = new List<LocationDataDto>
            {
                new() { Location = "New York", Sessions = 500, Users = 480, Percentage = 5.8 },
                new() { Location = "Beijing", Sessions = 450, Users = 430, Percentage = 5.2 }
            }
        };
    }

    private async Task<DeviceBrowserAnalysisDto> AnalyzeDeviceBrowserDataAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new DeviceBrowserAnalysisDto
        {
            Devices = new List<DeviceDataDto>
            {
                new() { DeviceType = "Mobile", Brand = "Apple", Sessions = 4000, Percentage = 45 },
                new() { DeviceType = "Desktop", Brand = "Windows", Sessions = 3500, Percentage = 40 }
            },
            Browsers = new List<BrowserDataDto>
            {
                new() { Browser = "Chrome", Version = "119", Sessions = 5000, Percentage = 57 },
                new() { Browser = "Safari", Version = "17", Sessions = 2000, Percentage = 23 }
            }
        };
    }

    private async Task<PagePerformanceAnalysisDto> AnalyzePagePerformanceAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new PagePerformanceAnalysisDto
        {
            PerformanceMetrics = new Dictionary<string, object>
            {
                ["AverageLoadTime"] = 2.5,
                ["AverageFCP"] = 1.2,
                ["AverageLCP"] = 2.8
            },
            PageSpeeds = new List<PageSpeedDto>
            {
                new()
                {
                    Url = "/",
                    LoadTime = 1.8,
                    FirstContentfulPaint = 0.9,
                    LargestContentfulPaint = 2.1,
                    CumulativeLayoutShift = 0.05
                }
            }
        };
    }

    private async Task<ConversionFunnelAnalysisDto> AnalyzeConversionFunnelAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return new ConversionFunnelAnalysisDto
        {
            Steps = new List<FunnelStepDto>
            {
                new() { StepName = "Landing", Users = 10000, ConversionRate = 100, DropoffRate = 0 },
                new() { StepName = "View Content", Users = 7000, ConversionRate = 70, DropoffRate = 30 },
                new() { StepName = "Signup", Users = 500, ConversionRate = 7.1, DropoffRate = 92.9 }
            },
            FunnelMetrics = new Dictionary<string, object>
            {
                ["OverallConversionRate"] = 5.0,
                ["BiggestDropoff"] = "View Content to Signup"
            }
        };
    }

    private TimeSpan GetTimeInterval(TimeGranularity granularity)
    {
        return granularity switch
        {
            TimeGranularity.Hour => TimeSpan.FromHours(1),
            TimeGranularity.Day => TimeSpan.FromDays(1),
            TimeGranularity.Week => TimeSpan.FromDays(7),
            TimeGranularity.Month => TimeSpan.FromDays(30),
            TimeGranularity.Quarter => TimeSpan.FromDays(90),
            TimeGranularity.Year => TimeSpan.FromDays(365),
            _ => TimeSpan.FromDays(1)
        };
    }

    private DateTime AddInterval(DateTime date, TimeSpan interval)
    {
        return date.Add(interval);
    }

    private async Task<Dictionary<string, object>> GetMetricValuesForPeriodAsync(
        string metricName,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        // 根据指标名称获取特定时间段的数据
        return new Dictionary<string, object>
        {
            [metricName] = Random.Shared.Next(100, 1000)
        };
    }

    private async Task<List<TimeSeriesDataDto>> GenerateTimeSeriesDataAsync(
        DateTime startDate,
        DateTime endDate,
        TimeGranularity granularity,
        Func<DateTime, DateTime, Task<Dictionary<string, object>>> valueGenerator)
    {
        var timeSeries = new List<TimeSeriesDataDto>();
        var interval = GetTimeInterval(granularity);
        var current = startDate;

        while (current <= endDate)
        {
            var next = AddInterval(current, interval);
            if (next > endDate) next = endDate;

            var values = await valueGenerator(current, next);
            timeSeries.Add(new TimeSeriesDataDto
            {
                Timestamp = current,
                Values = values
            });

            current = next;
        }

        return timeSeries;
    }

    private double CalculateReadTime(string content)
    {
        // 假设平均阅读速度为200词/分钟
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Round(wordCount / 200.0, 1);
    }

    private string GenerateCacheKey(ReportType reportType, AnalyticsQueryDto query)
    {
        var key = $"analytics:{reportType}:{query.StartDate:yyyyMMdd}:{query.EndDate:yyyyMMdd}";

        if (query.Dimensions.Any())
            key += $":dims={string.Join(",", query.Dimensions)}";

        if (query.Metrics.Any())
            key += $":metrics={string.Join(",", query.Metrics)}";

        return key;
    }

    private async Task<T?> GetCachedDataAsync<T>(string key, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<T>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve cached data for key {Key}", key);
        }

        return null;
    }

    private async Task SetCachedDataAsync<T>(string key, T data, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _defaultCacheDuration
            };

            var json = JsonSerializer.Serialize(data);
            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache data for key {Key}", key);
        }
    }
}

