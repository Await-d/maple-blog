using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MapleBlog.Admin.Services;
using System.Text.Json;

namespace MapleBlog.Admin.Controllers;

/// <summary>
/// Admin健康检查控制器
/// 提供数据库健康检查和监控API
/// </summary>
[ApiController]
[Route("admin/api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class HealthController : ControllerBase
{
    private readonly Admin.Services.HealthCheckService _healthCheckService;
    private readonly AdminDatabaseMonitoringService _monitoringService;
    private readonly AdminDatabasePrometheusService _prometheusService;
    private readonly AdminConnectionPoolMonitoringService _connectionPoolService;
    private readonly AdminQueryPerformanceAnalyzer _queryAnalyzer;
    private readonly AdminDatabaseResourceMonitor _resourceMonitor;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        Admin.Services.HealthCheckService healthCheckService,
        AdminDatabaseMonitoringService monitoringService,
        AdminDatabasePrometheusService prometheusService,
        AdminConnectionPoolMonitoringService connectionPoolService,
        AdminQueryPerformanceAnalyzer queryAnalyzer,
        AdminDatabaseResourceMonitor resourceMonitor,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _monitoringService = monitoringService ?? throw new ArgumentNullException(nameof(monitoringService));
        _prometheusService = prometheusService ?? throw new ArgumentNullException(nameof(prometheusService));
        _connectionPoolService = connectionPoolService ?? throw new ArgumentNullException(nameof(connectionPoolService));
        _queryAnalyzer = queryAnalyzer ?? throw new ArgumentNullException(nameof(queryAnalyzer));
        _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取基本健康状态
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTimeOffset.UtcNow,
                duration = healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        status = kvp.Value.Status.ToString(),
                        duration = kvp.Value.Duration.TotalMilliseconds,
                        description = kvp.Value.Description
                    })
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return StatusCode(500, new { error = "Failed to get health status", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取详细健康检查报告
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            var currentMetrics = _monitoringService.GetCurrentMetrics();

            var response = new
            {
                status = healthReport.Status.ToString(),
                timestamp = DateTimeOffset.UtcNow,
                duration = healthReport.TotalDuration.TotalMilliseconds,
                checks = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        status = kvp.Value.Status.ToString(),
                        duration = kvp.Value.Duration.TotalMilliseconds,
                        description = kvp.Value.Description,
                        data = kvp.Value.Data
                    }),
                metrics = currentMetrics,
                summary = _prometheusService.GetMetricsSummary()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get detailed health status");
            return StatusCode(500, new { error = "Failed to get detailed health status", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取数据库监控指标
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult GetMetrics([FromQuery] int hours = 1)
    {
        try
        {
            var currentMetrics = _monitoringService.GetCurrentMetrics();
            var historicalMetrics = _monitoringService.GetHistoricalMetrics(TimeSpan.FromHours(hours));
            var trend = _monitoringService.GetPerformanceTrend(TimeSpan.FromHours(hours));

            var response = new
            {
                current = currentMetrics,
                historical = historicalMetrics,
                trend = trend,
                timeRange = TimeSpan.FromHours(hours).ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics");
            return StatusCode(500, new { error = "Failed to get metrics", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取Prometheus格式指标
    /// </summary>
    [HttpGet("prometheus")]
    public IActionResult GetPrometheusMetrics()
    {
        try
        {
            var metrics = _prometheusService.GetPrometheusMetrics();
            return Content(metrics, "text/plain; version=0.0.4");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Prometheus metrics");
            return StatusCode(500, new { error = "Failed to get Prometheus metrics", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取连接池状态
    /// </summary>
    [HttpGet("connection-pool")]
    public IActionResult GetConnectionPoolStatus([FromQuery] int hours = 1)
    {
        try
        {
            var currentSnapshot = _connectionPoolService.GetCurrentSnapshot();
            var historicalSnapshots = _connectionPoolService.GetHistoricalSnapshots(TimeSpan.FromHours(hours));

            var response = new
            {
                current = currentSnapshot,
                history = historicalSnapshots,
                timeRange = TimeSpan.FromHours(hours).ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection pool status");
            return StatusCode(500, new { error = "Failed to get connection pool status", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取查询性能报告
    /// </summary>
    [HttpGet("query-performance")]
    public IActionResult GetQueryPerformance([FromQuery] int hours = 1)
    {
        try
        {
            var performanceReport = _queryAnalyzer.GetPerformanceReport();
            var queryStats = _queryAnalyzer.GetQueryStats();
            var recentQueries = _queryAnalyzer.GetRecentQueryRecords(TimeSpan.FromHours(hours));

            var response = new
            {
                report = performanceReport,
                statistics = queryStats,
                recentQueries = recentQueries,
                timeRange = TimeSpan.FromHours(hours).ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get query performance");
            return StatusCode(500, new { error = "Failed to get query performance", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取资源使用情况
    /// </summary>
    [HttpGet("resources")]
    public IActionResult GetResourceUsage([FromQuery] int hours = 1)
    {
        try
        {
            var currentSnapshot = _resourceMonitor.GetCurrentSnapshot();
            var historicalSnapshots = _resourceMonitor.GetHistoricalSnapshots(TimeSpan.FromHours(hours));
            var resourceTrend = _resourceMonitor.GetResourceTrend(TimeSpan.FromHours(hours));

            var response = new
            {
                current = currentSnapshot,
                history = historicalSnapshots,
                trend = resourceTrend,
                timeRange = TimeSpan.FromHours(hours).ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get resource usage");
            return StatusCode(500, new { error = "Failed to get resource usage", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取健康检查历史
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHealthHistory([FromQuery] int hours = 24)
    {
        try
        {
            var currentHealth = await _healthCheckService.CheckHealthAsync();
            var historicalMetrics = _monitoringService.GetHistoricalMetrics(TimeSpan.FromHours(hours));
            var performanceTrend = _monitoringService.GetPerformanceTrend(TimeSpan.FromHours(hours));

            var response = new
            {
                current = new
                {
                    status = currentHealth.Status.ToString(),
                    timestamp = DateTimeOffset.UtcNow,
                    duration = currentHealth.TotalDuration.TotalMilliseconds,
                    entries = currentHealth.Entries
                },
                metrics = historicalMetrics,
                trend = performanceTrend,
                timeRange = TimeSpan.FromHours(hours).ToString()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health history");
            return StatusCode(500, new { error = "Failed to get health history", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取综合健康状态摘要
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetHealthSummary()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();
            var currentMetrics = _monitoringService.GetCurrentMetrics();
            var connectionPool = _connectionPoolService.GetCurrentSnapshot();
            var queryPerformance = _queryAnalyzer.GetPerformanceReport();
            var resources = _resourceMonitor.GetCurrentSnapshot();
            var prometheusHealth = _prometheusService.GetHealthStatus();

            var response = new
            {
                overallStatus = healthReport.Status.ToString(),
                timestamp = DateTimeOffset.UtcNow,
                summary = new
                {
                    database = new
                    {
                        status = currentMetrics?.ConnectionInfo.CanConnect == true ? "healthy" : "unhealthy",
                        responseTime = currentMetrics?.ConnectionInfo.ConnectionTime ?? -1,
                        slowQueries = currentMetrics?.PerformanceMetrics.SlowQueryCount ?? 0
                    },
                    connectionPool = new
                    {
                        status = connectionPool?.ConnectionTests.All(t => t.Success) == true ? "healthy" : "degraded",
                        testsPassed = connectionPool?.ConnectionTests.Count(t => t.Success) ?? 0,
                        totalTests = connectionPool?.ConnectionTests.Count ?? 0
                    },
                    queryPerformance = new
                    {
                        averageTime = queryPerformance?.AverageExecutionTime ?? 0,
                        successRate = queryPerformance?.OverallSuccessRate ?? 0,
                        totalQueries = queryPerformance?.TotalQueries ?? 0
                    },
                    resources = new
                    {
                        databaseSize = resources?.DatabaseSpace.DatabaseSizeMB ?? 0,
                        memoryUsage = resources?.SystemResources.ProcessMemoryMB ?? 0,
                        diskUsage = resources?.DatabaseSpace.DiskUsagePercentage ?? 0
                    }
                },
                alerts = GetCurrentAlerts(currentMetrics, connectionPool, queryPerformance, resources)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health summary");
            return StatusCode(500, new { error = "Failed to get health summary", message = ex.Message });
        }
    }

    private List<string> GetCurrentAlerts(
        DatabaseMetricsSnapshot? metrics,
        ConnectionPoolSnapshot? connectionPool,
        QueryPerformanceReport? queryPerformance,
        ResourceSnapshot? resources)
    {
        var alerts = new List<string>();

        try
        {
            // 数据库连接告警
            if (metrics?.ConnectionInfo.CanConnect == false)
            {
                alerts.Add("Database connection is down");
            }
            else if (metrics?.ConnectionInfo.ConnectionTime > 5000)
            {
                alerts.Add($"Database connection time is high ({metrics.ConnectionInfo.ConnectionTime}ms)");
            }

            // 查询性能告警
            if (metrics?.PerformanceMetrics.SlowQueryCount > 3)
            {
                alerts.Add($"High number of slow queries ({metrics.PerformanceMetrics.SlowQueryCount})");
            }

            // 连接池告警
            var failedTests = connectionPool?.ConnectionTests.Count(t => !t.Success) ?? 0;
            if (failedTests > 0)
            {
                alerts.Add($"Connection pool tests failing ({failedTests} failed)");
            }

            // 资源使用告警
            if (resources?.DatabaseSpace.DiskUsagePercentage > 85)
            {
                alerts.Add($"High disk usage ({resources.DatabaseSpace.DiskUsagePercentage:F1}%)");
            }

            if (resources?.SystemResources.ProcessMemoryMB > 512)
            {
                alerts.Add($"High memory usage ({resources.SystemResources.ProcessMemoryMB:F1} MB)");
            }

            if (resources?.DatabaseSpace.DatabaseSizeMB > 2048)
            {
                alerts.Add($"Large database size ({resources.DatabaseSpace.DatabaseSizeMB:F1} MB)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate alerts");
            alerts.Add("Failed to check some alert conditions");
        }

        return alerts;
    }
}