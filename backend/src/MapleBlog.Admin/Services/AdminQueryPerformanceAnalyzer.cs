using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Linq.Expressions;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Admin查询性能分析器
/// 分析和监控数据库查询性能，识别慢查询和优化机会
/// </summary>
public class AdminQueryPerformanceAnalyzer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminQueryPerformanceAnalyzer> _logger;
    private readonly AdminQueryPerformanceOptions _options;
    private readonly ConcurrentQueue<QueryExecutionRecord> _queryHistory;
    private readonly ConcurrentDictionary<string, QueryPerformanceStats> _queryStats;
    private readonly Timer _analysisTimer;

    public AdminQueryPerformanceAnalyzer(
        IServiceProvider serviceProvider,
        ILogger<AdminQueryPerformanceAnalyzer> logger,
        IOptions<AdminQueryPerformanceOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _queryHistory = new ConcurrentQueue<QueryExecutionRecord>();
        _queryStats = new ConcurrentDictionary<string, QueryPerformanceStats>();

        _analysisTimer = new Timer(
            PerformAnalysisCallback,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_options.AnalysisIntervalSeconds)
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Admin Query Performance Analyzer started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunQueryPerformanceTests();
                await AnalyzeQueryPatterns();
                await CleanupOldRecords();
                await Task.Delay(TimeSpan.FromSeconds(_options.TestIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Admin Query Performance Analyzer is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Query Performance Analyzer");
        }
    }

    private async void PerformAnalysisCallback(object? state)
    {
        try
        {
            await AnalyzeQueryPatterns();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in performance analysis callback");
        }
    }

    private async Task RunQueryPerformanceTests()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 定义测试查询集合
            var testQueries = GetTestQueries(context);

            foreach (var testQuery in testQueries)
            {
                var record = await ExecuteAndMeasureQuery(testQuery);
                _queryHistory.Enqueue(record);
                UpdateQueryStats(record);
            }

            _logger.LogDebug("Completed query performance tests for {QueryCount} queries", testQueries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run query performance tests");
        }
    }

    private List<QueryTestDefinition> GetTestQueries(ApplicationDbContext context)
    {
        return new List<QueryTestDefinition>
        {
            // 基础计数查询
            new QueryTestDefinition
            {
                Name = "UserCount",
                Category = "Basic",
                Description = "Count total users",
                Query = () => context.Users.CountAsync(),
                ExpectedMaxTime = _options.BasicQueryThresholdMs
            },

            new QueryTestDefinition
            {
                Name = "PostCount",
                Category = "Basic",
                Description = "Count total posts",
                Query = () => context.Posts.CountAsync(),
                ExpectedMaxTime = _options.BasicQueryThresholdMs
            },

            new QueryTestDefinition
            {
                Name = "CategoryCount",
                Category = "Basic",
                Description = "Count total categories",
                Query = () => context.Categories.CountAsync(),
                ExpectedMaxTime = _options.BasicQueryThresholdMs
            },

            // 索引使用查询
            new QueryTestDefinition
            {
                Name = "UserByEmail",
                Category = "Indexed",
                Description = "Find user by email (should use index)",
                Query = () => context.Users.Where(u => u.Email == "test@example.com").FirstOrDefaultAsync(),
                ExpectedMaxTime = _options.IndexedQueryThresholdMs
            },

            new QueryTestDefinition
            {
                Name = "PostsByStatus",
                Category = "Indexed",
                Description = "Find posts by status",
                Query = () => context.Posts.Where(p => p.Status == Domain.Enums.PostStatus.Published).CountAsync(),
                ExpectedMaxTime = _options.IndexedQueryThresholdMs
            },

            // 复杂连接查询
            new QueryTestDefinition
            {
                Name = "PostsWithCategories",
                Category = "Complex",
                Description = "Posts with category information",
                Query = () => context.Posts
                    .Include(p => p.Category)
                    .Where(p => p.Status == Domain.Enums.PostStatus.Published)
                    .Take(10)
                    .ToListAsync(),
                ExpectedMaxTime = _options.ComplexQueryThresholdMs
            },

            new QueryTestDefinition
            {
                Name = "PostsWithTags",
                Category = "Complex",
                Description = "Posts with tag information",
                Query = () => context.Posts
                    .Include(p => p.Tags)
                    .Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-30))
                    .Take(10)
                    .ToListAsync(),
                ExpectedMaxTime = _options.ComplexQueryThresholdMs
            },

            new QueryTestDefinition
            {
                Name = "CommentsWithPosts",
                Category = "Complex",
                Description = "Comments with post information",
                Query = () => context.Comments
                    .Include(c => c.Post)
                    .Where(c => c.CreatedAt > DateTime.UtcNow.AddDays(-7))
                    .Take(10)
                    .ToListAsync(),
                ExpectedMaxTime = _options.ComplexQueryThresholdMs
            },

            // 聚合查询
            new QueryTestDefinition
            {
                Name = "PostStatsByMonth",
                Category = "Aggregation",
                Description = "Post statistics by month",
                Query = () => context.Posts
                    .Where(p => p.CreatedAt > DateTime.UtcNow.AddMonths(-6))
                    .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .ToListAsync(),
                ExpectedMaxTime = _options.AggregationQueryThresholdMs
            },

            new QueryTestDefinition
            {
                Name = "TagUsageStats",
                Category = "Aggregation",
                Description = "Tag usage statistics",
                Query = () => context.Tags
                    .Select(t => new { t.Name, PostCount = t.Posts.Count() })
                    .OrderByDescending(t => t.PostCount)
                    .Take(10)
                    .ToListAsync(),
                ExpectedMaxTime = _options.AggregationQueryThresholdMs
            },

            // 分页查询
            new QueryTestDefinition
            {
                Name = "PaginatedPosts",
                Category = "Pagination",
                Description = "Paginated posts query",
                Query = () => context.Posts
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip(20)
                    .Take(10)
                    .ToListAsync(),
                ExpectedMaxTime = _options.PaginationQueryThresholdMs
            },

            // 搜索查询
            new QueryTestDefinition
            {
                Name = "PostSearch",
                Category = "Search",
                Description = "Full-text search in posts",
                Query = () => context.Posts
                    .Where(p => p.Title.Contains("test") || p.Content.Contains("test"))
                    .Take(10)
                    .ToListAsync(),
                ExpectedMaxTime = _options.SearchQueryThresholdMs
            }
        };
    }

    private async Task<QueryExecutionRecord> ExecuteAndMeasureQuery(QueryTestDefinition testQuery)
    {
        var record = new QueryExecutionRecord
        {
            QueryName = testQuery.Name,
            QueryCategory = testQuery.Category,
            QueryDescription = testQuery.Description,
            Timestamp = DateTimeOffset.UtcNow,
            ExpectedMaxTime = testQuery.ExpectedMaxTime
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await testQuery.Query();
            stopwatch.Stop();

            record.ExecutionTime = stopwatch.ElapsedMilliseconds;
            record.Success = true;
            record.ResultSize = CalculateResultSize(result);
            record.IsSlowQuery = record.ExecutionTime > testQuery.ExpectedMaxTime;

            // 记录执行计划（如果可用）
            record.ExecutionPlan = await GetExecutionPlan(testQuery);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            record.ExecutionTime = stopwatch.ElapsedMilliseconds;
            record.Success = false;
            record.ErrorMessage = ex.Message;
            record.IsSlowQuery = true;
        }

        return record;
    }

    private int CalculateResultSize(object? result)
    {
        try
        {
            if (result == null) return 0;
            if (result is int count) return count;
            if (result is IEnumerable<object> enumerable) return enumerable.Count();
            return 1;
        }
        catch
        {
            return -1;
        }
    }

    private async Task<string?> GetExecutionPlan(QueryTestDefinition testQuery)
    {
        try
        {
            // 对于SQLite，我们可能无法获取详细的执行计划
            // 但我们可以记录查询的基本信息
            return $"Query: {testQuery.Name}, Category: {testQuery.Category}";
        }
        catch
        {
            return null;
        }

        await Task.CompletedTask;
    }

    private void UpdateQueryStats(QueryExecutionRecord record)
    {
        _queryStats.AddOrUpdate(record.QueryName,
            new QueryPerformanceStats
            {
                QueryName = record.QueryName,
                Category = record.QueryCategory,
                TotalExecutions = 1,
                SuccessfulExecutions = record.Success ? 1 : 0,
                TotalExecutionTime = record.ExecutionTime,
                MinExecutionTime = record.ExecutionTime,
                MaxExecutionTime = record.ExecutionTime,
                SlowQueryCount = record.IsSlowQuery ? 1 : 0,
                FirstSeen = record.Timestamp,
                LastSeen = record.Timestamp
            },
            (key, existing) =>
            {
                existing.TotalExecutions++;
                if (record.Success) existing.SuccessfulExecutions++;
                existing.TotalExecutionTime += record.ExecutionTime;
                existing.MinExecutionTime = Math.Min(existing.MinExecutionTime, record.ExecutionTime);
                existing.MaxExecutionTime = Math.Max(existing.MaxExecutionTime, record.ExecutionTime);
                if (record.IsSlowQuery) existing.SlowQueryCount++;
                existing.LastSeen = record.Timestamp;
                return existing;
            });
    }

    private async Task AnalyzeQueryPatterns()
    {
        try
        {
            var recentQueries = _queryHistory
                .Where(q => q.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-_options.AnalysisWindowMinutes))
                .ToList();

            if (!recentQueries.Any())
            {
                _logger.LogDebug("No recent queries to analyze");
                return;
            }

            // 分析慢查询趋势
            await AnalyzeSlowQueryTrends(recentQueries);

            // 分析查询性能回归
            await AnalyzePerformanceRegressions(recentQueries);

            // 分析查询失败模式
            await AnalyzeQueryFailures(recentQueries);

            // 生成优化建议
            await GenerateOptimizationRecommendations();

            _logger.LogDebug("Query pattern analysis completed for {QueryCount} recent queries", recentQueries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze query patterns");
        }
    }

    private async Task AnalyzeSlowQueryTrends(List<QueryExecutionRecord> recentQueries)
    {
        var slowQueries = recentQueries.Where(q => q.IsSlowQuery).ToList();

        if (slowQueries.Any())
        {
            var slowQueryStats = slowQueries
                .GroupBy(q => q.QueryName)
                .Select(g => new
                {
                    QueryName = g.Key,
                    Count = g.Count(),
                    AverageTime = g.Average(q => q.ExecutionTime),
                    MaxTime = g.Max(q => q.ExecutionTime)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            foreach (var stat in slowQueryStats.Take(5))
            {
                _logger.LogWarning("Slow query detected: {QueryName} - {Count} occurrences, avg: {AvgTime}ms, max: {MaxTime}ms",
                    stat.QueryName, stat.Count, stat.AverageTime, stat.MaxTime);
            }
        }

        await Task.CompletedTask;
    }

    private async Task AnalyzePerformanceRegressions(List<QueryExecutionRecord> recentQueries)
    {
        foreach (var queryGroup in recentQueries.GroupBy(q => q.QueryName))
        {
            var queryName = queryGroup.Key;
            var executions = queryGroup.OrderBy(q => q.Timestamp).ToList();

            if (executions.Count < 3) continue;

            // 比较最近的执行时间与历史平均值
            var recent = executions.TakeLast(Math.Min(5, executions.Count / 2)).Average(e => e.ExecutionTime);
            var historical = executions.Take(executions.Count / 2).Average(e => e.ExecutionTime);

            if (recent > historical * _options.RegressionThresholdMultiplier)
            {
                _logger.LogWarning("Performance regression detected for query {QueryName}: recent avg {RecentTime}ms vs historical avg {HistoricalTime}ms",
                    queryName, recent, historical);
            }
        }

        await Task.CompletedTask;
    }

    private async Task AnalyzeQueryFailures(List<QueryExecutionRecord> recentQueries)
    {
        var failedQueries = recentQueries.Where(q => !q.Success).ToList();

        if (failedQueries.Any())
        {
            var failureStats = failedQueries
                .GroupBy(q => q.ErrorMessage)
                .Select(g => new
                {
                    ErrorMessage = g.Key,
                    Count = g.Count(),
                    Queries = g.Select(q => q.QueryName).Distinct().ToList()
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            foreach (var stat in failureStats)
            {
                _logger.LogError("Query failure pattern: {ErrorMessage} - {Count} occurrences in queries: {Queries}",
                    stat.ErrorMessage, stat.Count, string.Join(", ", stat.Queries));
            }
        }

        await Task.CompletedTask;
    }

    private async Task GenerateOptimizationRecommendations()
    {
        var recommendations = new List<string>();

        foreach (var stat in _queryStats.Values)
        {
            var avgTime = stat.TotalExecutions > 0 ? stat.TotalExecutionTime / stat.TotalExecutions : 0;
            var slowQueryRate = stat.TotalExecutions > 0 ? (double)stat.SlowQueryCount / stat.TotalExecutions : 0;

            if (slowQueryRate > _options.SlowQueryRateThreshold)
            {
                recommendations.Add($"Query '{stat.QueryName}' has high slow query rate ({slowQueryRate:P2}). Consider adding indexes or optimizing query structure.");
            }

            if (avgTime > GetCategoryThreshold(stat.Category) * 2)
            {
                recommendations.Add($"Query '{stat.QueryName}' average execution time ({avgTime}ms) is significantly above threshold. Review query complexity.");
            }

            var successRate = stat.TotalExecutions > 0 ? (double)stat.SuccessfulExecutions / stat.TotalExecutions : 0;
            if (successRate < _options.MinSuccessRate)
            {
                recommendations.Add($"Query '{stat.QueryName}' has low success rate ({successRate:P2}). Investigate error patterns.");
            }
        }

        if (recommendations.Any())
        {
            _logger.LogInformation("Query optimization recommendations:\n{Recommendations}",
                string.Join("\n", recommendations.Select((r, i) => $"{i + 1}. {r}")));
        }

        await Task.CompletedTask;
    }

    private int GetCategoryThreshold(string category)
    {
        return category switch
        {
            "Basic" => _options.BasicQueryThresholdMs,
            "Indexed" => _options.IndexedQueryThresholdMs,
            "Complex" => _options.ComplexQueryThresholdMs,
            "Aggregation" => _options.AggregationQueryThresholdMs,
            "Pagination" => _options.PaginationQueryThresholdMs,
            "Search" => _options.SearchQueryThresholdMs,
            _ => _options.BasicQueryThresholdMs
        };
    }

    private async Task CleanupOldRecords()
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-_options.RecordRetentionHours);
        var removed = 0;

        while (_queryHistory.TryPeek(out var oldestRecord) && oldestRecord.Timestamp < cutoffTime)
        {
            if (_queryHistory.TryDequeue(out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Cleaned up {RemovedCount} old query execution records", removed);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    public Dictionary<string, QueryPerformanceStats> GetQueryStats()
    {
        return new Dictionary<string, QueryPerformanceStats>(_queryStats);
    }

    /// <summary>
    /// 获取最近的查询执行记录
    /// </summary>
    public IEnumerable<QueryExecutionRecord> GetRecentQueryRecords(TimeSpan timeRange)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(timeRange);
        return _queryHistory.Where(r => r.Timestamp >= cutoffTime).OrderByDescending(r => r.Timestamp);
    }

    /// <summary>
    /// 获取性能分析报告
    /// </summary>
    public QueryPerformanceReport GetPerformanceReport()
    {
        var recentRecords = GetRecentQueryRecords(TimeSpan.FromHours(1)).ToList();
        var stats = GetQueryStats();

        return new QueryPerformanceReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            TotalQueries = stats.Values.Sum(s => s.TotalExecutions),
            TotalSlowQueries = stats.Values.Sum(s => s.SlowQueryCount),
            AverageExecutionTime = stats.Values.Any() ? stats.Values.Average(s => s.TotalExecutions > 0 ? s.TotalExecutionTime / s.TotalExecutions : 0) : 0,
            OverallSuccessRate = stats.Values.Sum(s => s.TotalExecutions) > 0 ?
                (double)stats.Values.Sum(s => s.SuccessfulExecutions) / stats.Values.Sum(s => s.TotalExecutions) : 0,
            TopSlowQueries = stats.Values
                .Where(s => s.SlowQueryCount > 0)
                .OrderByDescending(s => s.SlowQueryCount)
                .Take(10)
                .ToList(),
            QueryCategoryStats = stats.Values
                .GroupBy(s => s.Category)
                .ToDictionary(g => g.Key, g => new CategoryStats
                {
                    TotalQueries = g.Sum(s => s.TotalExecutions),
                    SlowQueries = g.Sum(s => s.SlowQueryCount),
                    AverageTime = g.Average(s => s.TotalExecutions > 0 ? s.TotalExecutionTime / s.TotalExecutions : 0)
                })
        };
    }

    public override void Dispose()
    {
        _analysisTimer?.Dispose();
        base.Dispose();
    }
}

// 数据模型类
public class QueryTestDefinition
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public Func<Task<object?>> Query { get; set; } = null!;
    public int ExpectedMaxTime { get; set; }
}

public class QueryExecutionRecord
{
    public string QueryName { get; set; } = "";
    public string QueryCategory { get; set; } = "";
    public string QueryDescription { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public long ExecutionTime { get; set; }
    public bool Success { get; set; }
    public int ResultSize { get; set; }
    public bool IsSlowQuery { get; set; }
    public int ExpectedMaxTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExecutionPlan { get; set; }
}

public class QueryPerformanceStats
{
    public string QueryName { get; set; } = "";
    public string Category { get; set; } = "";
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public long TotalExecutionTime { get; set; }
    public long MinExecutionTime { get; set; }
    public long MaxExecutionTime { get; set; }
    public int SlowQueryCount { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
}

public class QueryPerformanceReport
{
    public DateTimeOffset GeneratedAt { get; set; }
    public int TotalQueries { get; set; }
    public int TotalSlowQueries { get; set; }
    public double AverageExecutionTime { get; set; }
    public double OverallSuccessRate { get; set; }
    public List<QueryPerformanceStats> TopSlowQueries { get; set; } = new();
    public Dictionary<string, CategoryStats> QueryCategoryStats { get; set; } = new();
}

public class CategoryStats
{
    public int TotalQueries { get; set; }
    public int SlowQueries { get; set; }
    public double AverageTime { get; set; }
}

/// <summary>
/// Admin查询性能分析配置选项
/// </summary>
public class AdminQueryPerformanceOptions
{
    public const string SectionName = "AdminQueryPerformance";

    /// <summary>
    /// 测试间隔（秒）
    /// </summary>
    public int TestIntervalSeconds { get; set; } = 300; // 5分钟

    /// <summary>
    /// 分析间隔（秒）
    /// </summary>
    public int AnalysisIntervalSeconds { get; set; } = 600; // 10分钟

    /// <summary>
    /// 分析窗口时间（分钟）
    /// </summary>
    public int AnalysisWindowMinutes { get; set; } = 60;

    /// <summary>
    /// 记录保留时间（小时）
    /// </summary>
    public int RecordRetentionHours { get; set; } = 24;

    /// <summary>
    /// 基础查询阈值（毫秒）
    /// </summary>
    public int BasicQueryThresholdMs { get; set; } = 500;

    /// <summary>
    /// 索引查询阈值（毫秒）
    /// </summary>
    public int IndexedQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 复杂查询阈值（毫秒）
    /// </summary>
    public int ComplexQueryThresholdMs { get; set; } = 2000;

    /// <summary>
    /// 聚合查询阈值（毫秒）
    /// </summary>
    public int AggregationQueryThresholdMs { get; set; } = 3000;

    /// <summary>
    /// 分页查询阈值（毫秒）
    /// </summary>
    public int PaginationQueryThresholdMs { get; set; } = 1500;

    /// <summary>
    /// 搜索查询阈值（毫秒）
    /// </summary>
    public int SearchQueryThresholdMs { get; set; } = 5000;

    /// <summary>
    /// 性能回归阈值倍数
    /// </summary>
    public double RegressionThresholdMultiplier { get; set; } = 1.5;

    /// <summary>
    /// 慢查询率阈值
    /// </summary>
    public double SlowQueryRateThreshold { get; set; } = 0.1; // 10%

    /// <summary>
    /// 最小成功率
    /// </summary>
    public double MinSuccessRate { get; set; } = 0.95; // 95%
}