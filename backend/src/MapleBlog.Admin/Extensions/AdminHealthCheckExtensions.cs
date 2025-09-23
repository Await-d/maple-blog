using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MapleBlog.Admin.Services;
using System.Text.Json;

namespace MapleBlog.Admin.Extensions;

/// <summary>
/// Admin健康检查扩展方法
/// </summary>
public static class AdminHealthCheckExtensions
{
    /// <summary>
    /// 添加Admin数据库健康检查服务
    /// </summary>
    public static IServiceCollection AddAdminDatabaseHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 配置选项
        services.Configure<AdminDatabaseHealthOptions>(
            configuration.GetSection(AdminDatabaseHealthOptions.SectionName));
        services.Configure<AdminDatabaseMonitoringOptions>(
            configuration.GetSection(AdminDatabaseMonitoringOptions.SectionName));
        services.Configure<AdminDatabasePrometheusOptions>(
            configuration.GetSection(AdminDatabasePrometheusOptions.SectionName));
        services.Configure<AdminConnectionPoolOptions>(
            configuration.GetSection(AdminConnectionPoolOptions.SectionName));
        services.Configure<AdminQueryPerformanceOptions>(
            configuration.GetSection(AdminQueryPerformanceOptions.SectionName));
        services.Configure<AdminDatabaseResourceOptions>(
            configuration.GetSection(AdminDatabaseResourceOptions.SectionName));

        // 注册健康检查服务
        services.AddHealthChecks()
            .AddCheck<AdminDatabaseHealthCheckService>("admin_database",
                tags: new[] { "admin", "database", "detailed" });

        // 注册监控服务
        services.AddSingleton<AdminDatabaseMonitoringService>();
        services.AddHostedService<AdminDatabaseMonitoringService>(provider =>
            provider.GetRequiredService<AdminDatabaseMonitoringService>());

        // 注册Prometheus指标服务
        services.AddSingleton<AdminDatabasePrometheusService>();
        services.AddHostedService<AdminDatabasePrometheusService>(provider =>
            provider.GetRequiredService<AdminDatabasePrometheusService>());

        // 注册连接池监控服务
        services.AddSingleton<AdminConnectionPoolMonitoringService>();
        services.AddHostedService<AdminConnectionPoolMonitoringService>(provider =>
            provider.GetRequiredService<AdminConnectionPoolMonitoringService>());

        // 注册查询性能分析器
        services.AddSingleton<AdminQueryPerformanceAnalyzer>();
        services.AddHostedService<AdminQueryPerformanceAnalyzer>(provider =>
            provider.GetRequiredService<AdminQueryPerformanceAnalyzer>());

        // 注册资源监控服务
        services.AddSingleton<AdminDatabaseResourceMonitor>();
        services.AddHostedService<AdminDatabaseResourceMonitor>(provider =>
            provider.GetRequiredService<AdminDatabaseResourceMonitor>());

        return services;
    }

    /// <summary>
    /// 映射Admin健康检查端点
    /// </summary>
    public static IEndpointRouteBuilder MapAdminHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 基本健康检查端点
        endpoints.MapHealthChecks("/admin/health", new HealthCheckOptions
        {
            ResponseWriter = WriteAdminHealthResponse,
            AllowCachingResponses = false
        });

        // 数据库专用健康检查
        endpoints.MapHealthChecks("/admin/health/database", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("database"),
            ResponseWriter = WriteDetailedHealthResponse,
            AllowCachingResponses = false
        });

        // 详细健康检查（包含所有监控数据）
        endpoints.MapHealthChecks("/admin/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = WriteComprehensiveHealthResponse,
            AllowCachingResponses = false
        });

        // 监控指标端点
        endpoints.MapGet("/admin/health/metrics", GetMonitoringMetrics)
            .WithTags("Admin", "Health", "Metrics");

        // Prometheus指标端点
        endpoints.MapGet("/admin/health/prometheus", GetPrometheusMetrics)
            .WithTags("Admin", "Health", "Prometheus");

        // 连接池状态端点
        endpoints.MapGet("/admin/health/connection-pool", GetConnectionPoolStatus)
            .WithTags("Admin", "Health", "ConnectionPool");

        // 查询性能报告端点
        endpoints.MapGet("/admin/health/query-performance", GetQueryPerformanceReport)
            .WithTags("Admin", "Health", "QueryPerformance");

        // 资源使用情况端点
        endpoints.MapGet("/admin/health/resources", GetResourceUsage)
            .WithTags("Admin", "Health", "Resources");

        // 健康检查历史数据端点
        endpoints.MapGet("/admin/health/history", GetHealthHistory)
            .WithTags("Admin", "Health", "History");

        // 管理员健康检查仪表板
        endpoints.MapGet("/admin/health/dashboard", RenderHealthDashboard)
            .WithTags("Admin", "Health", "Dashboard");

        return endpoints;
    }

    private static async Task WriteAdminHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.TotalMilliseconds,
                    description = kvp.Value.Description,
                    tags = kvp.Value.Tags
                })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static async Task WriteDetailedHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.TotalMilliseconds,
                    description = kvp.Value.Description,
                    tags = kvp.Value.Tags,
                    data = kvp.Value.Data
                })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static async Task WriteComprehensiveHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var monitoringService = context.RequestServices.GetService<AdminDatabaseMonitoringService>();
        var connectionPoolService = context.RequestServices.GetService<AdminConnectionPoolMonitoringService>();
        var queryAnalyzer = context.RequestServices.GetService<AdminQueryPerformanceAnalyzer>();
        var resourceMonitor = context.RequestServices.GetService<AdminDatabaseResourceMonitor>();

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            healthChecks = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString(),
                    duration = kvp.Value.Duration.TotalMilliseconds,
                    description = kvp.Value.Description,
                    data = kvp.Value.Data
                }),
            monitoring = new
            {
                database = monitoringService?.GetCurrentMetrics(),
                connectionPool = connectionPoolService?.GetCurrentSnapshot(),
                queryPerformance = queryAnalyzer?.GetPerformanceReport(),
                resources = resourceMonitor?.GetCurrentSnapshot()
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static async Task<IResult> GetMonitoringMetrics(HttpContext context)
    {
        try
        {
            var monitoringService = context.RequestServices.GetService<AdminDatabaseMonitoringService>();
            var prometheusService = context.RequestServices.GetService<AdminDatabasePrometheusService>();

            if (monitoringService == null || prometheusService == null)
            {
                return Results.NotFound("Monitoring services not available");
            }

            var metrics = new
            {
                current = monitoringService.GetCurrentMetrics(),
                trend = monitoringService.GetPerformanceTrend(TimeSpan.FromHours(1)),
                summary = prometheusService.GetMetricsSummary(),
                health = prometheusService.GetHealthStatus()
            };

            return Results.Ok(metrics);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get monitoring metrics: {ex.Message}");
        }
    }

    private static async Task<IResult> GetPrometheusMetrics(HttpContext context)
    {
        try
        {
            var prometheusService = context.RequestServices.GetService<AdminDatabasePrometheusService>();

            if (prometheusService == null)
            {
                return Results.NotFound("Prometheus service not available");
            }

            var metrics = prometheusService.GetPrometheusMetrics();
            context.Response.ContentType = "text/plain; version=0.0.4";
            return Results.Text(metrics);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get Prometheus metrics: {ex.Message}");
        }
    }

    private static async Task<IResult> GetConnectionPoolStatus(HttpContext context)
    {
        try
        {
            var connectionPoolService = context.RequestServices.GetService<AdminConnectionPoolMonitoringService>();

            if (connectionPoolService == null)
            {
                return Results.NotFound("Connection pool monitoring service not available");
            }

            var hoursParam = context.Request.Query["hours"].FirstOrDefault();
            var hours = int.TryParse(hoursParam, out var h) ? h : 1;

            var status = new
            {
                current = connectionPoolService.GetCurrentSnapshot(),
                history = connectionPoolService.GetHistoricalSnapshots(TimeSpan.FromHours(hours))
            };

            return Results.Ok(status);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get connection pool status: {ex.Message}");
        }
    }

    private static async Task<IResult> GetQueryPerformanceReport(HttpContext context)
    {
        try
        {
            var queryAnalyzer = context.RequestServices.GetService<AdminQueryPerformanceAnalyzer>();

            if (queryAnalyzer == null)
            {
                return Results.NotFound("Query performance analyzer not available");
            }

            var hoursParam = context.Request.Query["hours"].FirstOrDefault();
            var hours = int.TryParse(hoursParam, out var h) ? h : 1;

            var report = new
            {
                performance = queryAnalyzer.GetPerformanceReport(),
                statistics = queryAnalyzer.GetQueryStats(),
                recentQueries = queryAnalyzer.GetRecentQueryRecords(TimeSpan.FromHours(hours))
            };

            return Results.Ok(report);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get query performance report: {ex.Message}");
        }
    }

    private static async Task<IResult> GetResourceUsage(HttpContext context)
    {
        try
        {
            var resourceMonitor = context.RequestServices.GetService<AdminDatabaseResourceMonitor>();

            if (resourceMonitor == null)
            {
                return Results.NotFound("Resource monitor not available");
            }

            var hoursParam = context.Request.Query["hours"].FirstOrDefault();
            var hours = int.TryParse(hoursParam, out var h) ? h : 1;

            var resources = new
            {
                current = resourceMonitor.GetCurrentSnapshot(),
                trend = resourceMonitor.GetResourceTrend(TimeSpan.FromHours(hours)),
                history = resourceMonitor.GetHistoricalSnapshots(TimeSpan.FromHours(hours))
            };

            return Results.Ok(resources);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get resource usage: {ex.Message}");
        }
    }

    private static async Task<IResult> GetHealthHistory(HttpContext context)
    {
        try
        {
            var healthCheckService = context.RequestServices.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
            var monitoringService = context.RequestServices.GetService<AdminDatabaseMonitoringService>();

            if (healthCheckService == null || monitoringService == null)
            {
                return Results.NotFound("Health check services not available");
            }

            // 执行当前健康检查
            var currentHealth = await healthCheckService.CheckHealthAsync();

            var hoursParam = context.Request.Query["hours"].FirstOrDefault();
            var hours = int.TryParse(hoursParam, out var h) ? h : 24;

            var history = new
            {
                current = new
                {
                    status = currentHealth.Status.ToString(),
                    timestamp = DateTimeOffset.UtcNow,
                    duration = currentHealth.TotalDuration.TotalMilliseconds,
                    entries = currentHealth.Entries
                },
                metrics = monitoringService.GetHistoricalMetrics(TimeSpan.FromHours(hours)),
                trend = monitoringService.GetPerformanceTrend(TimeSpan.FromHours(hours))
            };

            return Results.Ok(history);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get health history: {ex.Message}");
        }
    }

    private static async Task<IResult> RenderHealthDashboard(HttpContext context)
    {
        try
        {
            var html = GenerateHealthDashboardHtml();
            context.Response.ContentType = "text/html";
            return Results.Text(html, "text/html");
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to render health dashboard: {ex.Message}");
        }
    }

    private static string GenerateHealthDashboardHtml()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <title>Admin Database Health Dashboard</title>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 1200px; margin: 0 auto; }
        .header { background: #f5f5f5; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
        .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .metric-card { background: white; border: 1px solid #ddd; border-radius: 8px; padding: 20px; }
        .metric-title { font-size: 18px; font-weight: bold; margin-bottom: 10px; color: #333; }
        .metric-value { font-size: 24px; font-weight: bold; }
        .status-healthy { color: #28a745; }
        .status-degraded { color: #ffc107; }
        .status-unhealthy { color: #dc3545; }
        .refresh-btn { background: #007bff; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; }
        .refresh-btn:hover { background: #0056b3; }
        .loading { display: none; }
        pre { background: #f8f9fa; padding: 10px; border-radius: 4px; overflow-x: auto; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Admin Database Health Dashboard</h1>
            <p>Real-time monitoring of database health and performance metrics</p>
            <button class=""refresh-btn"" onclick=""refreshData()"">Refresh Data</button>
            <span class=""loading"">Loading...</span>
        </div>

        <div class=""metrics-grid"">
            <div class=""metric-card"">
                <div class=""metric-title"">Health Status</div>
                <div id=""health-status"" class=""metric-value"">Loading...</div>
                <div id=""health-details""></div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-title"">Database Metrics</div>
                <div id=""database-metrics"">Loading...</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-title"">Connection Pool</div>
                <div id=""connection-pool"">Loading...</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-title"">Query Performance</div>
                <div id=""query-performance"">Loading...</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-title"">Resource Usage</div>
                <div id=""resource-usage"">Loading...</div>
            </div>

            <div class=""metric-card"">
                <div class=""metric-title"">Prometheus Metrics</div>
                <div id=""prometheus-metrics"">Loading...</div>
            </div>
        </div>

        <div style=""margin-top: 40px;"">
            <h2>Raw Health Check Data</h2>
            <pre id=""raw-data"">Loading...</pre>
        </div>
    </div>

    <script>
        async function refreshData() {
            const loading = document.querySelector('.loading');
            loading.style.display = 'inline';

            try {
                // Health status
                const healthResponse = await fetch('/admin/health/detailed');
                const healthData = await healthResponse.json();
                updateHealthStatus(healthData);

                // Monitoring metrics
                const metricsResponse = await fetch('/admin/health/metrics');
                const metricsData = await metricsResponse.json();
                updateMetrics(metricsData);

                // Connection pool
                const poolResponse = await fetch('/admin/health/connection-pool');
                const poolData = await poolResponse.json();
                updateConnectionPool(poolData);

                // Query performance
                const queryResponse = await fetch('/admin/health/query-performance');
                const queryData = await queryResponse.json();
                updateQueryPerformance(queryData);

                // Resource usage
                const resourceResponse = await fetch('/admin/health/resources');
                const resourceData = await resourceResponse.json();
                updateResourceUsage(resourceData);

                // Raw data
                document.getElementById('raw-data').textContent = JSON.stringify(healthData, null, 2);

            } catch (error) {
                console.error('Error refreshing data:', error);
            }

            loading.style.display = 'none';
        }

        function updateHealthStatus(data) {
            const statusElement = document.getElementById('health-status');
            const detailsElement = document.getElementById('health-details');

            statusElement.textContent = data.status;
            statusElement.className = 'metric-value status-' + data.status.toLowerCase();

            detailsElement.innerHTML = `
                <p>Duration: ${data.totalDuration.toFixed(2)}ms</p>
                <p>Timestamp: ${new Date(data.timestamp).toLocaleString()}</p>
            `;
        }

        function updateMetrics(data) {
            const element = document.getElementById('database-metrics');
            const current = data.current;

            element.innerHTML = `
                <p>Connection Time: ${current.connectionInfo.connectionTime}ms</p>
                <p>Avg Response: ${current.performanceMetrics.averageResponseTime.toFixed(2)}ms</p>
                <p>Slow Queries: ${current.performanceMetrics.slowQueryCount}</p>
                <p>DB Size: ${(current.resourceMetrics.databaseSizeBytes / 1024 / 1024).toFixed(2)} MB</p>
            `;
        }

        function updateConnectionPool(data) {
            const element = document.getElementById('connection-pool');
            const current = data.current;

            element.innerHTML = `
                <p>Provider: ${current.poolStats.providerName}</p>
                <p>State: ${current.poolStats.currentState}</p>
                <p>Tests: ${current.connectionTests.filter(t => t.success).length}/${current.connectionTests.length} passed</p>
            `;
        }

        function updateQueryPerformance(data) {
            const element = document.getElementById('query-performance');
            const perf = data.performance;

            element.innerHTML = `
                <p>Total Queries: ${perf.totalQueries}</p>
                <p>Slow Queries: ${perf.totalSlowQueries}</p>
                <p>Avg Time: ${perf.averageExecutionTime.toFixed(2)}ms</p>
                <p>Success Rate: ${(perf.overallSuccessRate * 100).toFixed(1)}%</p>
            `;
        }

        function updateResourceUsage(data) {
            const element = document.getElementById('resource-usage');
            const current = data.current;

            element.innerHTML = `
                <p>Memory: ${current.systemResources.processMemoryMB.toFixed(1)} MB</p>
                <p>Disk Usage: ${current.databaseSpace.diskUsagePercentage.toFixed(1)}%</p>
                <p>Query Time: ${current.performanceCounters.simpleQueryTimeMs}ms</p>
            `;
        }

        // Auto-refresh every 30 seconds
        setInterval(refreshData, 30000);

        // Initial load
        refreshData();
    </script>
</body>
</html>";
    }
}
