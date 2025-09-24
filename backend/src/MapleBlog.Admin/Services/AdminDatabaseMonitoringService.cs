using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Admin数据库监控服务
/// 负责持续监控数据库性能指标并提供历史数据
/// </summary>
public class AdminDatabaseMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminDatabaseMonitoringService> _logger;
    private readonly AdminDatabaseMonitoringOptions _options;
    private readonly ConcurrentQueue<DatabaseMetricsSnapshot> _metricsHistory;
    private readonly Timer _metricsCollectionTimer;
    private volatile DatabaseMetricsSnapshot _currentMetrics;

    public AdminDatabaseMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<AdminDatabaseMonitoringService> logger,
        IOptions<AdminDatabaseMonitoringOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _metricsHistory = new ConcurrentQueue<DatabaseMetricsSnapshot>();
        _currentMetrics = new DatabaseMetricsSnapshot();

        // 初始化指标收集定时器
        _metricsCollectionTimer = new Timer(
            CollectMetricsCallback,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_options.CollectionIntervalSeconds)
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Admin Database Monitoring Service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CollectDatabaseMetrics();
                await CleanupOldMetrics();
                await Task.Delay(TimeSpan.FromSeconds(_options.CollectionIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Admin Database Monitoring Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Database Monitoring Service");
        }
    }

    private async void CollectMetricsCallback(object? state)
    {
        try
        {
            await CollectDatabaseMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting database metrics in timer callback");
        }
    }

    private async Task CollectDatabaseMetrics()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var metrics = new DatabaseMetricsSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                ConnectionInfo = await CollectConnectionMetrics(context),
                PerformanceMetrics = await CollectPerformanceMetrics(context),
                ResourceMetrics = await CollectResourceMetrics(context),
                QueryMetrics = await CollectQueryMetrics(context),
                TableMetrics = await CollectTableMetrics(context)
            };

            // 更新当前指标
            _currentMetrics = metrics;

            // 添加到历史记录
            _metricsHistory.Enqueue(metrics);

            // 检查是否需要触发告警
            await CheckAlerts(metrics);

            _logger.LogDebug("Database metrics collected successfully at {Timestamp}", metrics.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect database metrics");
        }
    }

    private async Task<ConnectionMetrics> CollectConnectionMetrics(ApplicationDbContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            stopwatch.Stop();

            var connection = context.Database.GetDbConnection();

            return new ConnectionMetrics
            {
                CanConnect = canConnect,
                ConnectionTime = stopwatch.ElapsedMilliseconds,
                ConnectionState = connection.State.ToString(),
                ProviderName = context.Database.ProviderName ?? "Unknown",
                DatabaseName = connection.Database,
                ServerVersion = connection.ServerVersion ?? "Unknown",
                ConnectionTimeout = connection.ConnectionTimeout
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ConnectionMetrics
            {
                CanConnect = false,
                ConnectionTime = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<PerformanceMetrics> CollectPerformanceMetrics(ApplicationDbContext context)
    {
        var performanceMetrics = new PerformanceMetrics();
        var queryTests = new List<QueryTestResult>();

        // 测试基本查询性能
        var testQueries = new (string Name, Func<Task<int>> Query)[]
        {
            ("UserCount", () => context.Users.CountAsync()),
            ("PostCount", () => context.Posts.CountAsync()),
            ("RecentPosts", () => context.Posts.Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-7)).CountAsync()),
            ("ActiveUsers", () => context.Users.Where(u => u.LastLoginAt > DateTime.UtcNow.AddDays(-30)).CountAsync())
        };

        foreach (var test in testQueries)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await test.Query();
                stopwatch.Stop();

                queryTests.Add(new QueryTestResult
                {
                    QueryName = test.Name,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    ResultCount = result,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                queryTests.Add(new QueryTestResult
                {
                    QueryName = test.Name,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        performanceMetrics.QueryTests = queryTests;
        performanceMetrics.AverageResponseTime = queryTests.Where(q => q.Success).Average(q => q.ExecutionTime);
        performanceMetrics.SlowQueryCount = queryTests.Count(q => q.Success && q.ExecutionTime > _options.SlowQueryThresholdMs);

        return performanceMetrics;
    }

    private async Task<ResourceMetrics> CollectResourceMetrics(ApplicationDbContext context)
    {
        var resourceMetrics = new ResourceMetrics();

        try
        {
            // 对于SQLite，获取文件信息
            if (context.Database.ProviderName?.Contains("Sqlite") == true)
            {
                var connectionString = context.Database.GetDbConnection().ConnectionString;
                var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)");

                if (dataSourceMatch.Success)
                {
                    var dbPath = dataSourceMatch.Groups[1].Value;
                    if (File.Exists(dbPath))
                    {
                        var fileInfo = new FileInfo(dbPath);
                        resourceMetrics.DatabaseSizeBytes = fileInfo.Length;
                        resourceMetrics.LastModified = fileInfo.LastWriteTime;
                    }
                }
            }

            // 收集系统资源信息
            var process = Process.GetCurrentProcess();
            resourceMetrics.MemoryUsageBytes = process.WorkingSet64;
            resourceMetrics.CpuTimeMs = process.TotalProcessorTime.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect resource metrics");
        }

        await Task.CompletedTask;
        return resourceMetrics;
    }

    private async Task<QueryMetrics> CollectQueryMetrics(ApplicationDbContext context)
    {
        var queryMetrics = new QueryMetrics();

        try
        {
            // 收集查询统计信息
            var recentPostsStopwatch = Stopwatch.StartNew();
            var recentPosts = await context.Posts
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddHours(-1))
                .CountAsync();
            recentPostsStopwatch.Stop();

            queryMetrics.RecentActivity = new ActivityMetrics
            {
                PostsLastHour = recentPosts,
                QueryTime = recentPostsStopwatch.ElapsedMilliseconds
            };

            // 模拟并发查询测试
            var concurrentTasks = Enumerable.Range(0, 5)
                .Select(async _ =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await context.Users.AnyAsync();
                        sw.Stop();
                        return sw.ElapsedMilliseconds;
                    }
                    catch
                    {
                        sw.Stop();
                        return -1;
                    }
                });

            var concurrentResults = await Task.WhenAll(concurrentTasks);
            var successfulQueries = concurrentResults.Where(r => r >= 0).ToArray();

            queryMetrics.ConcurrentQueryPerformance = new ConcurrentQueryMetrics
            {
                SuccessfulQueries = successfulQueries.Length,
                FailedQueries = concurrentResults.Length - successfulQueries.Length,
                AverageResponseTime = successfulQueries.Length > 0 ? successfulQueries.Average() : 0,
                MaxResponseTime = successfulQueries.Length > 0 ? successfulQueries.Max() : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect query metrics");
        }

        return queryMetrics;
    }

    private async Task<TableMetrics> CollectTableMetrics(ApplicationDbContext context)
    {
        var tableMetrics = new TableMetrics();
        var tableCounts = new Dictionary<string, long>();

        try
        {
            var tables = new (string Name, Func<Task<long>> Query)[]
            {
                ("Users", () => context.Users.LongCountAsync()),
                ("Posts", () => context.Posts.LongCountAsync()),
                ("Categories", () => context.Categories.LongCountAsync()),
                ("Tags", () => context.Tags.LongCountAsync()),
                ("Comments", () => context.Comments.LongCountAsync())
            };

            foreach (var table in tables)
            {
                try
                {
                    var count = await table.Query();
                    tableCounts[table.Name] = count;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get count for table {TableName}", table.Name);
                    tableCounts[table.Name] = -1;
                }
            }

            tableMetrics.TableCounts = tableCounts;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect table metrics");
        }

        return tableMetrics;
    }

    private async Task CheckAlerts(DatabaseMetricsSnapshot metrics)
    {
        var alerts = new List<string>();

        // 检查连接时间
        if (metrics.ConnectionInfo.ConnectionTime > _options.ConnectionTimeoutAlertMs)
        {
            alerts.Add($"Database connection time ({metrics.ConnectionInfo.ConnectionTime}ms) exceeds threshold");
        }

        // 检查查询性能
        if (metrics.PerformanceMetrics.AverageResponseTime > _options.AverageResponseTimeAlertMs)
        {
            alerts.Add($"Average query response time ({metrics.PerformanceMetrics.AverageResponseTime:F2}ms) exceeds threshold");
        }

        // 检查慢查询数量
        if (metrics.PerformanceMetrics.SlowQueryCount > _options.SlowQueryCountAlert)
        {
            alerts.Add($"Too many slow queries detected ({metrics.PerformanceMetrics.SlowQueryCount})");
        }

        // 检查数据库大小
        if (metrics.ResourceMetrics.DatabaseSizeBytes > _options.DatabaseSizeAlertBytes)
        {
            var sizeMB = metrics.ResourceMetrics.DatabaseSizeBytes / (1024.0 * 1024.0);
            alerts.Add($"Database size ({sizeMB:F2} MB) exceeds threshold");
        }

        if (alerts.Any())
        {
            _logger.LogWarning("Database alerts triggered: {Alerts}", string.Join("; ", alerts));
            // 这里可以添加其他告警通知机制，如发送邮件、推送通知等
        }

        await Task.CompletedTask;
    }

    private async Task CleanupOldMetrics()
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-_options.MetricsRetentionHours);
        var removed = 0;

        while (_metricsHistory.TryPeek(out var oldestMetric) && oldestMetric.Timestamp < cutoffTime)
        {
            if (_metricsHistory.TryDequeue(out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Cleaned up {RemovedCount} old metrics entries", removed);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取当前数据库指标
    /// </summary>
    public DatabaseMetricsSnapshot GetCurrentMetrics()
    {
        return _currentMetrics;
    }

    /// <summary>
    /// 获取历史指标数据
    /// </summary>
    public IEnumerable<DatabaseMetricsSnapshot> GetHistoricalMetrics(TimeSpan timeRange)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(timeRange);
        return _metricsHistory.Where(m => m.Timestamp >= cutoffTime).OrderBy(m => m.Timestamp);
    }

    /// <summary>
    /// 获取性能趋势分析
    /// </summary>
    public DatabasePerformanceTrend GetPerformanceTrend(TimeSpan timeRange)
    {
        var metrics = GetHistoricalMetrics(timeRange).ToList();

        if (!metrics.Any())
        {
            return new DatabasePerformanceTrend();
        }

        var responseTimes = metrics.Select(m => m.PerformanceMetrics.AverageResponseTime).ToList();
        var connectionTimes = metrics.Select(m => (double)m.ConnectionInfo.ConnectionTime).ToList();

        return new DatabasePerformanceTrend
        {
            TimeRange = timeRange,
            DataPoints = metrics.Count,
            AverageResponseTime = responseTimes.Average(),
            MinResponseTime = responseTimes.Min(),
            MaxResponseTime = responseTimes.Max(),
            AverageConnectionTime = connectionTimes.Average(),
            MinConnectionTime = connectionTimes.Min(),
            MaxConnectionTime = connectionTimes.Max(),
            TotalSlowQueries = metrics.Sum(m => m.PerformanceMetrics.SlowQueryCount),
            HealthyPercentage = metrics.Count(m => m.ConnectionInfo.CanConnect) * 100.0 / metrics.Count
        };
    }

    public override void Dispose()
    {
        _metricsCollectionTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// 数据库指标快照
/// </summary>
public class DatabaseMetricsSnapshot
{
    public DateTimeOffset Timestamp { get; set; }
    public ConnectionMetrics ConnectionInfo { get; set; } = new();
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    public ResourceMetrics ResourceMetrics { get; set; } = new();
    public QueryMetrics QueryMetrics { get; set; } = new();
    public TableMetrics TableMetrics { get; set; } = new();
}

public class ConnectionMetrics
{
    public bool CanConnect { get; set; }
    public long ConnectionTime { get; set; }
    public string ConnectionState { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string ServerVersion { get; set; } = "";
    public int ConnectionTimeout { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PerformanceMetrics
{
    public List<QueryTestResult> QueryTests { get; set; } = new();
    public double AverageResponseTime { get; set; }
    public int SlowQueryCount { get; set; }
}

public class QueryTestResult
{
    public string QueryName { get; set; } = "";
    public long ExecutionTime { get; set; }
    public int ResultCount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ResourceMetrics
{
    public long DatabaseSizeBytes { get; set; }
    public long MemoryUsageBytes { get; set; }
    public double CpuTimeMs { get; set; }
    public DateTime LastModified { get; set; }
}

public class QueryMetrics
{
    public ActivityMetrics RecentActivity { get; set; } = new();
    public ConcurrentQueryMetrics ConcurrentQueryPerformance { get; set; } = new();
}

public class ActivityMetrics
{
    public int PostsLastHour { get; set; }
    public long QueryTime { get; set; }
}

public class ConcurrentQueryMetrics
{
    public int SuccessfulQueries { get; set; }
    public int FailedQueries { get; set; }
    public double AverageResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
}

public class TableMetrics
{
    public Dictionary<string, long> TableCounts { get; set; } = new();
}

public class DatabasePerformanceTrend
{
    public TimeSpan TimeRange { get; set; }
    public int DataPoints { get; set; }
    public double AverageResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double AverageConnectionTime { get; set; }
    public double MinConnectionTime { get; set; }
    public double MaxConnectionTime { get; set; }
    public int TotalSlowQueries { get; set; }
    public double HealthyPercentage { get; set; }
}

/// <summary>
/// Admin数据库监控配置选项
/// </summary>
public class AdminDatabaseMonitoringOptions
{
    public const string SectionName = "AdminDatabaseMonitoring";

    /// <summary>
    /// 指标收集间隔（秒）
    /// </summary>
    public int CollectionIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 指标保留时间（小时）
    /// </summary>
    public int MetricsRetentionHours { get; set; } = 24;

    /// <summary>
    /// 慢查询阈值（毫秒）
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 2000;

    /// <summary>
    /// 连接超时告警阈值（毫秒）
    /// </summary>
    public int ConnectionTimeoutAlertMs { get; set; } = 5000;

    /// <summary>
    /// 平均响应时间告警阈值（毫秒）
    /// </summary>
    public int AverageResponseTimeAlertMs { get; set; } = 3000;

    /// <summary>
    /// 慢查询数量告警阈值
    /// </summary>
    public int SlowQueryCountAlert { get; set; } = 3;

    /// <summary>
    /// 数据库大小告警阈值（字节）
    /// </summary>
    public long DatabaseSizeAlertBytes { get; set; } = 1024L * 1024 * 1024; // 1GB

    /// <summary>
    /// 启用告警
    /// </summary>
    public bool EnableAlerts { get; set; } = true;
}