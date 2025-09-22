using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Admin数据库Prometheus指标收集服务
/// 提供符合Prometheus格式的数据库监控指标
/// </summary>
public class AdminDatabasePrometheusService : BackgroundService
{
    private readonly AdminDatabaseMonitoringService _monitoringService;
    private readonly ILogger<AdminDatabasePrometheusService> _logger;
    private readonly AdminDatabasePrometheusOptions _options;
    private readonly ConcurrentDictionary<string, double> _currentMetrics;
    private readonly Timer _metricsUpdateTimer;

    public AdminDatabasePrometheusService(
        AdminDatabaseMonitoringService monitoringService,
        ILogger<AdminDatabasePrometheusService> logger,
        IOptions<AdminDatabasePrometheusOptions> options)
    {
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _currentMetrics = new ConcurrentDictionary<string, double>();

        // 初始化指标更新定时器
        _metricsUpdateTimer = new Timer(
            UpdateMetricsCallback,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_options.UpdateIntervalSeconds)
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Admin Database Prometheus Service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdatePrometheusMetrics();
                await Task.Delay(TimeSpan.FromSeconds(_options.UpdateIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Admin Database Prometheus Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Database Prometheus Service");
        }
    }

    private async void UpdateMetricsCallback(object? state)
    {
        try
        {
            await UpdatePrometheusMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Prometheus metrics in timer callback");
        }
    }

    private async Task UpdatePrometheusMetrics()
    {
        try
        {
            var snapshot = _monitoringService.GetCurrentMetrics();

            if (snapshot == null)
            {
                _logger.LogWarning("No database metrics available for Prometheus export");
                return;
            }

            // 更新连接指标
            UpdateConnectionMetrics(snapshot.ConnectionInfo);

            // 更新性能指标
            UpdatePerformanceMetrics(snapshot.PerformanceMetrics);

            // 更新资源指标
            UpdateResourceMetrics(snapshot.ResourceMetrics);

            // 更新查询指标
            UpdateQueryMetrics(snapshot.QueryMetrics);

            // 更新表指标
            UpdateTableMetrics(snapshot.TableMetrics);

            _logger.LogDebug("Prometheus metrics updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Prometheus metrics");
        }

        await Task.CompletedTask;
    }

    private void UpdateConnectionMetrics(ConnectionMetrics connectionInfo)
    {
        // 连接状态指标 (0=disconnected, 1=connected)
        _currentMetrics["maple_blog_admin_db_connection_status"] = connectionInfo.CanConnect ? 1 : 0;

        // 连接时间指标
        _currentMetrics["maple_blog_admin_db_connection_time_ms"] = connectionInfo.ConnectionTime;

        // 连接超时配置
        _currentMetrics["maple_blog_admin_db_connection_timeout_seconds"] = connectionInfo.ConnectionTimeout;
    }

    private void UpdatePerformanceMetrics(PerformanceMetrics performanceMetrics)
    {
        // 平均响应时间
        _currentMetrics["maple_blog_admin_db_query_response_time_ms"] = performanceMetrics.AverageResponseTime;

        // 慢查询数量
        _currentMetrics["maple_blog_admin_db_slow_queries_total"] = performanceMetrics.SlowQueryCount;

        // 单个查询性能指标
        foreach (var queryTest in performanceMetrics.QueryTests)
        {
            var metricName = $"maple_blog_admin_db_query_{queryTest.QueryName.ToLowerInvariant()}_time_ms";
            _currentMetrics[metricName] = queryTest.ExecutionTime;

            var successMetricName = $"maple_blog_admin_db_query_{queryTest.QueryName.ToLowerInvariant()}_success";
            _currentMetrics[successMetricName] = queryTest.Success ? 1 : 0;

            if (queryTest.Success)
            {
                var countMetricName = $"maple_blog_admin_db_query_{queryTest.QueryName.ToLowerInvariant()}_result_count";
                _currentMetrics[countMetricName] = queryTest.ResultCount;
            }
        }
    }

    private void UpdateResourceMetrics(ResourceMetrics resourceMetrics)
    {
        // 数据库文件大小
        _currentMetrics["maple_blog_admin_db_size_bytes"] = resourceMetrics.DatabaseSizeBytes;

        // 内存使用量
        _currentMetrics["maple_blog_admin_db_memory_usage_bytes"] = resourceMetrics.MemoryUsageBytes;

        // CPU时间
        _currentMetrics["maple_blog_admin_db_cpu_time_ms"] = resourceMetrics.CpuTimeMs;
    }

    private void UpdateQueryMetrics(QueryMetrics queryMetrics)
    {
        // 最近活动指标
        _currentMetrics["maple_blog_admin_db_posts_last_hour"] = queryMetrics.RecentActivity.PostsLastHour;
        _currentMetrics["maple_blog_admin_db_recent_activity_query_time_ms"] = queryMetrics.RecentActivity.QueryTime;

        // 并发查询性能
        _currentMetrics["maple_blog_admin_db_concurrent_queries_successful"] = queryMetrics.ConcurrentQueryPerformance.SuccessfulQueries;
        _currentMetrics["maple_blog_admin_db_concurrent_queries_failed"] = queryMetrics.ConcurrentQueryPerformance.FailedQueries;
        _currentMetrics["maple_blog_admin_db_concurrent_queries_avg_time_ms"] = queryMetrics.ConcurrentQueryPerformance.AverageResponseTime;
        _currentMetrics["maple_blog_admin_db_concurrent_queries_max_time_ms"] = queryMetrics.ConcurrentQueryPerformance.MaxResponseTime;
    }

    private void UpdateTableMetrics(TableMetrics tableMetrics)
    {
        // 表记录数指标
        foreach (var tableCount in tableMetrics.TableCounts)
        {
            var metricName = $"maple_blog_admin_db_table_{tableCount.Key.ToLowerInvariant()}_records";
            _currentMetrics[metricName] = tableCount.Value >= 0 ? tableCount.Value : 0;
        }
    }

    /// <summary>
    /// 获取当前Prometheus格式的指标
    /// </summary>
    public string GetPrometheusMetrics()
    {
        var sb = new StringBuilder();

        // 添加指标说明和类型
        AddMetricHeader(sb, "maple_blog_admin_db_connection_status", "gauge", "Database connection status (0=disconnected, 1=connected)");
        AddMetricHeader(sb, "maple_blog_admin_db_connection_time_ms", "gauge", "Database connection time in milliseconds");
        AddMetricHeader(sb, "maple_blog_admin_db_connection_timeout_seconds", "gauge", "Database connection timeout in seconds");
        AddMetricHeader(sb, "maple_blog_admin_db_query_response_time_ms", "gauge", "Average database query response time in milliseconds");
        AddMetricHeader(sb, "maple_blog_admin_db_slow_queries_total", "counter", "Total number of slow database queries");
        AddMetricHeader(sb, "maple_blog_admin_db_size_bytes", "gauge", "Database size in bytes");
        AddMetricHeader(sb, "maple_blog_admin_db_memory_usage_bytes", "gauge", "Database process memory usage in bytes");
        AddMetricHeader(sb, "maple_blog_admin_db_cpu_time_ms", "gauge", "Database process CPU time in milliseconds");
        AddMetricHeader(sb, "maple_blog_admin_db_posts_last_hour", "gauge", "Number of posts created in the last hour");
        AddMetricHeader(sb, "maple_blog_admin_db_concurrent_queries_successful", "gauge", "Number of successful concurrent queries");
        AddMetricHeader(sb, "maple_blog_admin_db_concurrent_queries_failed", "gauge", "Number of failed concurrent queries");

        // 添加当前指标值
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var metric in _currentMetrics)
        {
            sb.AppendLine($"{metric.Key} {metric.Value} {timestamp}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取JSON格式的指标摘要
    /// </summary>
    public object GetMetricsSummary()
    {
        var snapshot = _monitoringService.GetCurrentMetrics();
        var trend = _monitoringService.GetPerformanceTrend(TimeSpan.FromHours(1));

        return new
        {
            timestamp = DateTimeOffset.UtcNow,
            connection = new
            {
                status = snapshot?.ConnectionInfo.CanConnect == true ? "healthy" : "unhealthy",
                response_time_ms = snapshot?.ConnectionInfo.ConnectionTime ?? -1,
                provider = snapshot?.ConnectionInfo.ProviderName ?? "unknown"
            },
            performance = new
            {
                average_response_time_ms = snapshot?.PerformanceMetrics.AverageResponseTime ?? -1,
                slow_queries = snapshot?.PerformanceMetrics.SlowQueryCount ?? 0,
                trend_data_points = trend.DataPoints,
                trend_healthy_percentage = trend.HealthyPercentage
            },
            resources = new
            {
                database_size_mb = (snapshot?.ResourceMetrics.DatabaseSizeBytes ?? 0) / (1024.0 * 1024.0),
                memory_usage_mb = (snapshot?.ResourceMetrics.MemoryUsageBytes ?? 0) / (1024.0 * 1024.0)
            },
            tables = snapshot?.TableMetrics.TableCounts ?? new Dictionary<string, long>(),
            activity = new
            {
                posts_last_hour = snapshot?.QueryMetrics.RecentActivity.PostsLastHour ?? 0,
                concurrent_queries_success_rate = CalculateConcurrentQuerySuccessRate(snapshot?.QueryMetrics.ConcurrentQueryPerformance)
            }
        };
    }

    /// <summary>
    /// 获取健康检查状态
    /// </summary>
    public object GetHealthStatus()
    {
        var snapshot = _monitoringService.GetCurrentMetrics();

        var isHealthy = snapshot?.ConnectionInfo.CanConnect == true &&
                       (snapshot?.PerformanceMetrics.AverageResponseTime ?? double.MaxValue) < _options.HealthyResponseTimeThresholdMs &&
                       (snapshot?.PerformanceMetrics.SlowQueryCount ?? int.MaxValue) < _options.HealthySlowQueryThreshold;

        return new
        {
            status = isHealthy ? "healthy" : "unhealthy",
            timestamp = DateTimeOffset.UtcNow,
            checks = new
            {
                connection = snapshot?.ConnectionInfo.CanConnect == true,
                performance = (snapshot?.PerformanceMetrics.AverageResponseTime ?? double.MaxValue) < _options.HealthyResponseTimeThresholdMs,
                slow_queries = (snapshot?.PerformanceMetrics.SlowQueryCount ?? int.MaxValue) < _options.HealthySlowQueryThreshold
            },
            metrics = GetMetricsSummary()
        };
    }

    private void AddMetricHeader(StringBuilder sb, string metricName, string type, string help)
    {
        sb.AppendLine($"# HELP {metricName} {help}");
        sb.AppendLine($"# TYPE {metricName} {type}");
    }

    private double CalculateConcurrentQuerySuccessRate(ConcurrentQueryMetrics? metrics)
    {
        if (metrics == null || (metrics.SuccessfulQueries + metrics.FailedQueries) == 0)
            return 0;

        return (double)metrics.SuccessfulQueries / (metrics.SuccessfulQueries + metrics.FailedQueries) * 100;
    }

    public override void Dispose()
    {
        _metricsUpdateTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Admin数据库Prometheus服务配置选项
/// </summary>
public class AdminDatabasePrometheusOptions
{
    public const string SectionName = "AdminDatabasePrometheus";

    /// <summary>
    /// 指标更新间隔（秒）
    /// </summary>
    public int UpdateIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 健康响应时间阈值（毫秒）
    /// </summary>
    public double HealthyResponseTimeThresholdMs { get; set; } = 2000;

    /// <summary>
    /// 健康慢查询阈值
    /// </summary>
    public int HealthySlowQueryThreshold { get; set; } = 2;

    /// <summary>
    /// 启用详细指标
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;

    /// <summary>
    /// 指标名称前缀
    /// </summary>
    public string MetricPrefix { get; set; } = "maple_blog_admin_db";
}