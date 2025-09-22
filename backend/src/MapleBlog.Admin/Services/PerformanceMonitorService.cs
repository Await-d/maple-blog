using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Backend Performance Monitor Service for Admin Dashboard
/// Provides comprehensive server-side performance monitoring, analysis, and optimization insights
/// </summary>
public interface IPerformanceMonitorService
{
    Task<PerformanceMetrics> GetCurrentMetricsAsync();
    Task<PerformanceReport> GenerateReportAsync();
    Task<IEnumerable<PerformanceAlert>> GetActiveAlertsAsync();
    Task<IEnumerable<PerformanceInsight>> AnalyzePerformanceAsync();
    Task<PerformanceTrend> GetPerformanceTrendAsync(TimeSpan period);
    Task RecordCustomMetricAsync(string name, double value, Dictionary<string, object>? tags = null);
    Task<bool> StartMonitoringAsync();
    Task<bool> StopMonitoringAsync();
    void ClearAlerts();
    Task<string> ExportMetricsAsync();
}

public class PerformanceMonitorService : BackgroundService, IPerformanceMonitorService
{
    private readonly ILogger<PerformanceMonitorService> _logger;
    private readonly BlogDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly PerformanceMonitorOptions _options;

    private readonly ConcurrentDictionary<string, DatabaseQueryMetrics> _queryMetrics = new();
    private readonly ConcurrentDictionary<string, ApiEndpointMetrics> _endpointMetrics = new();
    private readonly ConcurrentQueue<PerformanceAlert> _alerts = new();
    private readonly ConcurrentQueue<CustomMetric> _customMetrics = new();

    private PerformanceMetrics _currentMetrics = new();
    private PerformanceBaseline? _baseline;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    private bool _isMonitoring;
    private readonly Timer _metricsTimer;
    private readonly Timer _alertTimer;

    public PerformanceMonitorService(
        ILogger<PerformanceMonitorService> logger,
        BlogDbContext context,
        IMemoryCache cache,
        IOptions<PerformanceMonitorOptions> options)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _options = options.Value;

        // Initialize performance counters
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

        // Setup timers
        _metricsTimer = new Timer(CollectMetrics, null, Timeout.Infinite, Timeout.Infinite);
        _alertTimer = new Timer(CheckAlerts, null, Timeout.Infinite, Timeout.Infinite);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartMonitoringAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAllMetricsAsync();
                await Task.Delay(_options.CollectionInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance monitoring execution");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    public async Task<bool> StartMonitoringAsync()
    {
        if (_isMonitoring) return true;

        try
        {
            _isMonitoring = true;

            // Start timers
            _metricsTimer.Change(TimeSpan.Zero, _options.CollectionInterval);
            _alertTimer.Change(TimeSpan.Zero, _options.AlertCheckInterval);

            // Record baseline after startup period
            _ = Task.Delay(_options.BaselineDelay).ContinueWith(async _ =>
            {
                await RecordBaselineAsync();
            });

            _logger.LogInformation("Performance monitoring started");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start performance monitoring");
            return false;
        }
    }

    public async Task<bool> StopMonitoringAsync()
    {
        if (!_isMonitoring) return true;

        try
        {
            _isMonitoring = false;

            // Stop timers
            _metricsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _alertTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _logger.LogInformation("Performance monitoring stopped");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop performance monitoring");
            return false;
        }
    }

    public async Task<PerformanceMetrics> GetCurrentMetricsAsync()
    {
        await CollectAllMetricsAsync();
        return _currentMetrics;
    }

    private async Task CollectAllMetricsAsync()
    {
        try
        {
            var metrics = new PerformanceMetrics
            {
                Timestamp = DateTime.UtcNow,
                System = await CollectSystemMetricsAsync(),
                Database = await CollectDatabaseMetricsAsync(),
                Api = CollectApiMetrics(),
                Memory = await CollectMemoryMetricsAsync(),
                Cache = await CollectCacheMetricsAsync(),
                Custom = CollectCustomMetrics()
            };

            _currentMetrics = metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }

    private async Task<SystemMetrics> CollectSystemMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();

            return new SystemMetrics
            {
                CpuUsage = await GetCpuUsageAsync(),
                MemoryUsage = GetMemoryUsage(),
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                Uptime = DateTime.UtcNow - process.StartTime,
                GcCollections = new GcMetrics
                {
                    Gen0 = GC.CollectionCount(0),
                    Gen1 = GC.CollectionCount(1),
                    Gen2 = GC.CollectionCount(2)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
            return new SystemMetrics();
        }
    }

    private async Task<DatabaseMetrics> CollectDatabaseMetricsAsync()
    {
        try
        {
            var connectionState = _context.Database.GetDbConnection().State;
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();

            var poolSize = 0;
            var activeConnections = 0;

            // Get connection pool information if available
            if (_context.Database.GetDbConnection() is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
            {
                // This would require additional setup for SQL Server
            }

            return new DatabaseMetrics
            {
                ConnectionState = connectionState.ToString(),
                ActiveConnections = activeConnections,
                ConnectionPoolSize = poolSize,
                PendingMigrations = pendingMigrations.Count(),
                QueryMetrics = _queryMetrics.Values.ToList(),
                AverageQueryTime = _queryMetrics.Values.Any()
                    ? _queryMetrics.Values.Average(q => q.AverageExecutionTime)
                    : 0,
                SlowQueries = _queryMetrics.Values
                    .Where(q => q.AverageExecutionTime > _options.SlowQueryThreshold.TotalMilliseconds)
                    .OrderByDescending(q => q.AverageExecutionTime)
                    .Take(10)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting database metrics");
            return new DatabaseMetrics();
        }
    }

    private ApiMetrics CollectApiMetrics()
    {
        try
        {
            var endpoints = _endpointMetrics.Values.ToList();

            return new ApiMetrics
            {
                TotalRequests = endpoints.Sum(e => e.RequestCount),
                AverageResponseTime = endpoints.Any()
                    ? endpoints.Average(e => e.AverageResponseTime)
                    : 0,
                ErrorRate = endpoints.Any()
                    ? endpoints.Sum(e => e.ErrorCount) / (double)endpoints.Sum(e => e.RequestCount) * 100
                    : 0,
                EndpointMetrics = endpoints,
                SlowestEndpoints = endpoints
                    .OrderByDescending(e => e.AverageResponseTime)
                    .Take(10)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting API metrics");
            return new ApiMetrics();
        }
    }

    private async Task<MemoryMetrics> CollectMemoryMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var gcInfo = GC.GetGCMemoryInfo();

            return new MemoryMetrics
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                ManagedMemory = GC.GetTotalMemory(false),
                Gen0HeapSize = gcInfo.GenerationInfo[0].SizeBytes,
                Gen1HeapSize = gcInfo.GenerationInfo[1].SizeBytes,
                Gen2HeapSize = gcInfo.GenerationInfo[2].SizeBytes,
                LargeObjectHeapSize = gcInfo.GenerationInfo[3].SizeBytes,
                MemoryPressure = GetMemoryPressure()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting memory metrics");
            return new MemoryMetrics();
        }
    }

    private async Task<CacheMetrics> CollectCacheMetricsAsync()
    {
        try
        {
            // This would require custom implementation to track cache metrics
            // For now, returning basic structure
            return new CacheMetrics
            {
                HitRate = 0, // Would need to implement cache hit tracking
                MissRate = 0,
                EntryCount = 0,
                MemoryUsage = 0,
                EvictionCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting cache metrics");
            return new CacheMetrics();
        }
    }

    private List<CustomMetric> CollectCustomMetrics()
    {
        var metrics = new List<CustomMetric>();

        while (_customMetrics.TryDequeue(out var metric))
        {
            metrics.Add(metric);
        }

        return metrics;
    }

    private async Task<double> GetCpuUsageAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return _cpuCounter.NextValue();
            }
            else
            {
                // For Linux/macOS, would need different implementation
                return 0;
            }
        }
        catch
        {
            return 0;
        }
    }

    private long GetMemoryUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return process.WorkingSet64;
        }
        catch
        {
            return 0;
        }
    }

    private double GetMemoryPressure()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var gcInfo = GC.GetGCMemoryInfo();

            return (double)process.WorkingSet64 / (1024 * 1024 * 1024); // GB
        }
        catch
        {
            return 0;
        }
    }

    public void RecordDatabaseQuery(string query, TimeSpan executionTime, bool success)
    {
        var key = query.Length > 100 ? query.Substring(0, 100) + "..." : query;

        _queryMetrics.AddOrUpdate(key,
            new DatabaseQueryMetrics
            {
                Query = key,
                ExecutionCount = 1,
                TotalExecutionTime = executionTime.TotalMilliseconds,
                AverageExecutionTime = executionTime.TotalMilliseconds,
                ErrorCount = success ? 0 : 1,
                LastExecuted = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.ExecutionCount++;
                existing.TotalExecutionTime += executionTime.TotalMilliseconds;
                existing.AverageExecutionTime = existing.TotalExecutionTime / existing.ExecutionCount;
                if (!success) existing.ErrorCount++;
                existing.LastExecuted = DateTime.UtcNow;
                return existing;
            });
    }

    public void RecordApiRequest(string endpoint, string method, TimeSpan responseTime, int statusCode)
    {
        var key = $"{method}:{endpoint}";

        _endpointMetrics.AddOrUpdate(key,
            new ApiEndpointMetrics
            {
                Endpoint = endpoint,
                Method = method,
                RequestCount = 1,
                TotalResponseTime = responseTime.TotalMilliseconds,
                AverageResponseTime = responseTime.TotalMilliseconds,
                ErrorCount = statusCode >= 400 ? 1 : 0,
                LastCalled = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.RequestCount++;
                existing.TotalResponseTime += responseTime.TotalMilliseconds;
                existing.AverageResponseTime = existing.TotalResponseTime / existing.RequestCount;
                if (statusCode >= 400) existing.ErrorCount++;
                existing.LastCalled = DateTime.UtcNow;
                return existing;
            });
    }

    public async Task RecordCustomMetricAsync(string name, double value, Dictionary<string, object>? tags = null)
    {
        var metric = new CustomMetric
        {
            Name = name,
            Value = value,
            Tags = tags ?? new Dictionary<string, object>(),
            Timestamp = DateTime.UtcNow
        };

        _customMetrics.Enqueue(metric);
    }

    private async Task RecordBaselineAsync()
    {
        try
        {
            var metrics = await GetCurrentMetricsAsync();
            _baseline = new PerformanceBaseline
            {
                Timestamp = DateTime.UtcNow,
                SystemCpuUsage = metrics.System.CpuUsage,
                SystemMemoryUsage = metrics.System.MemoryUsage,
                AverageQueryTime = metrics.Database.AverageQueryTime,
                AverageApiResponseTime = metrics.Api.AverageResponseTime
            };

            _logger.LogInformation("Performance baseline recorded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record performance baseline");
        }
    }

    private void CollectMetrics(object? state)
    {
        if (!_isMonitoring) return;

        try
        {
            _ = Task.Run(CollectAllMetricsAsync);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in metrics collection timer");
        }
    }

    private void CheckAlerts(object? state)
    {
        if (!_isMonitoring) return;

        try
        {
            _ = Task.Run(CheckPerformanceAlertsAsync);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in alert checking timer");
        }
    }

    private async Task CheckPerformanceAlertsAsync()
    {
        try
        {
            var metrics = _currentMetrics;

            // Check CPU usage
            if (metrics.System.CpuUsage > _options.CpuThreshold)
            {
                AddAlert(new PerformanceAlert
                {
                    Type = AlertType.Critical,
                    Metric = "CPU Usage",
                    Value = metrics.System.CpuUsage,
                    Threshold = _options.CpuThreshold,
                    Message = $"High CPU usage detected: {metrics.System.CpuUsage:F1}%",
                    Timestamp = DateTime.UtcNow,
                    Suggestion = "Consider optimizing CPU-intensive operations or scaling horizontally"
                });
            }

            // Check memory usage
            var memoryUsageGB = metrics.Memory.WorkingSet / (1024.0 * 1024.0 * 1024.0);
            if (memoryUsageGB > _options.MemoryThresholdGB)
            {
                AddAlert(new PerformanceAlert
                {
                    Type = AlertType.Warning,
                    Metric = "Memory Usage",
                    Value = memoryUsageGB,
                    Threshold = _options.MemoryThresholdGB,
                    Message = $"High memory usage detected: {memoryUsageGB:F2} GB",
                    Timestamp = DateTime.UtcNow,
                    Suggestion = "Review memory usage patterns and implement garbage collection optimization"
                });
            }

            // Check database performance
            if (metrics.Database.AverageQueryTime > _options.SlowQueryThreshold.TotalMilliseconds)
            {
                AddAlert(new PerformanceAlert
                {
                    Type = AlertType.Warning,
                    Metric = "Database Query Time",
                    Value = metrics.Database.AverageQueryTime,
                    Threshold = _options.SlowQueryThreshold.TotalMilliseconds,
                    Message = $"Slow database queries detected: {metrics.Database.AverageQueryTime:F0}ms average",
                    Timestamp = DateTime.UtcNow,
                    Suggestion = "Optimize database queries, add indexes, or implement query caching"
                });
            }

            // Check API performance
            if (metrics.Api.AverageResponseTime > _options.SlowApiThreshold.TotalMilliseconds)
            {
                AddAlert(new PerformanceAlert
                {
                    Type = AlertType.Warning,
                    Metric = "API Response Time",
                    Value = metrics.Api.AverageResponseTime,
                    Threshold = _options.SlowApiThreshold.TotalMilliseconds,
                    Message = $"Slow API responses detected: {metrics.Api.AverageResponseTime:F0}ms average",
                    Timestamp = DateTime.UtcNow,
                    Suggestion = "Optimize API endpoints, implement caching, or review business logic"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking performance alerts");
        }
    }

    private void AddAlert(PerformanceAlert alert)
    {
        _alerts.Enqueue(alert);

        // Keep only recent alerts
        while (_alerts.Count > _options.MaxAlerts)
        {
            _alerts.TryDequeue(out _);
        }

        _logger.LogWarning("Performance alert: {Message}", alert.Message);
    }

    public async Task<IEnumerable<PerformanceAlert>> GetActiveAlertsAsync()
    {
        var cutoff = DateTime.UtcNow.Subtract(_options.AlertRetentionPeriod);
        return _alerts.Where(a => a.Timestamp > cutoff).ToList();
    }

    public void ClearAlerts()
    {
        while (_alerts.TryDequeue(out _)) { }
    }

    public async Task<IEnumerable<PerformanceInsight>> AnalyzePerformanceAsync()
    {
        var insights = new List<PerformanceInsight>();
        var metrics = _currentMetrics;

        // CPU Analysis
        if (metrics.System.CpuUsage > 80)
        {
            insights.Add(new PerformanceInsight
            {
                Category = "System",
                Severity = "High",
                Title = "High CPU Usage",
                Description = "The system is experiencing high CPU utilization",
                Impact = "May cause slow response times and poor user experience",
                Recommendations = new[]
                {
                    "Profile CPU-intensive operations",
                    "Implement async processing for heavy workloads",
                    "Consider horizontal scaling",
                    "Optimize algorithmic complexity"
                },
                Metrics = new Dictionary<string, double>
                {
                    ["CpuUsage"] = metrics.System.CpuUsage,
                    ["ThreadCount"] = metrics.System.ThreadCount
                }
            });
        }

        // Memory Analysis
        var memoryUsageGB = metrics.Memory.WorkingSet / (1024.0 * 1024.0 * 1024.0);
        if (memoryUsageGB > 4.0) // 4GB threshold
        {
            insights.Add(new PerformanceInsight
            {
                Category = "Memory",
                Severity = "Medium",
                Title = "High Memory Usage",
                Description = "The application is using significant memory resources",
                Impact = "May lead to garbage collection pressure and performance degradation",
                Recommendations = new[]
                {
                    "Review object lifecycle and disposal patterns",
                    "Implement memory pooling for large objects",
                    "Optimize data structures and caching strategies",
                    "Consider memory profiling to identify leaks"
                },
                Metrics = new Dictionary<string, double>
                {
                    ["WorkingSetGB"] = memoryUsageGB,
                    ["ManagedMemoryGB"] = metrics.Memory.ManagedMemory / (1024.0 * 1024.0 * 1024.0),
                    ["Gen2Collections"] = metrics.System.GcCollections.Gen2
                }
            });
        }

        // Database Performance Analysis
        if (metrics.Database.SlowQueries.Any())
        {
            insights.Add(new PerformanceInsight
            {
                Category = "Database",
                Severity = "Medium",
                Title = "Slow Database Queries",
                Description = "Several database queries are executing slowly",
                Impact = "Slow queries can bottleneck application performance",
                Recommendations = new[]
                {
                    "Add appropriate database indexes",
                    "Optimize query structure and joins",
                    "Implement query result caching",
                    "Consider database query analysis tools"
                },
                Metrics = new Dictionary<string, double>
                {
                    ["AverageQueryTime"] = metrics.Database.AverageQueryTime,
                    ["SlowQueryCount"] = metrics.Database.SlowQueries.Count
                }
            });
        }

        return insights;
    }

    public async Task<PerformanceTrend> GetPerformanceTrendAsync(TimeSpan period)
    {
        // This would require storing historical metrics
        // For now, return current vs baseline comparison
        if (_baseline == null)
        {
            return new PerformanceTrend
            {
                Period = period,
                Trends = new Dictionary<string, double>()
            };
        }

        var current = _currentMetrics;
        return new PerformanceTrend
        {
            Period = period,
            Trends = new Dictionary<string, double>
            {
                ["CpuUsageTrend"] = current.System.CpuUsage - _baseline.SystemCpuUsage,
                ["MemoryUsageTrend"] = current.System.MemoryUsage - _baseline.SystemMemoryUsage,
                ["QueryTimeTrend"] = current.Database.AverageQueryTime - _baseline.AverageQueryTime,
                ["ApiResponseTrend"] = current.Api.AverageResponseTime - _baseline.AverageApiResponseTime
            }
        };
    }

    public async Task<PerformanceReport> GenerateReportAsync()
    {
        var metrics = await GetCurrentMetricsAsync();
        var alerts = await GetActiveAlertsAsync();
        var insights = await AnalyzePerformanceAsync();
        var trend = await GetPerformanceTrendAsync(TimeSpan.FromHours(24));

        return new PerformanceReport
        {
            Timestamp = DateTime.UtcNow,
            Metrics = metrics,
            Alerts = alerts.ToList(),
            Insights = insights.ToList(),
            Trend = trend,
            Recommendations = GenerateRecommendations(insights)
        };
    }

    private List<string> GenerateRecommendations(IEnumerable<PerformanceInsight> insights)
    {
        var recommendations = new HashSet<string>();

        foreach (var insight in insights)
        {
            foreach (var recommendation in insight.Recommendations)
            {
                recommendations.Add(recommendation);
            }
        }

        return recommendations.ToList();
    }

    public async Task<string> ExportMetricsAsync()
    {
        var report = await GenerateReportAsync();
        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public override void Dispose()
    {
        _metricsTimer?.Dispose();
        _alertTimer?.Dispose();
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        base.Dispose();
    }
}

// Configuration Options
public class PerformanceMonitorOptions
{
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan AlertCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan BaselineDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan AlertRetentionPeriod { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan SlowApiThreshold { get; set; } = TimeSpan.FromSeconds(2);
    public double CpuThreshold { get; set; } = 80.0;
    public double MemoryThresholdGB { get; set; } = 4.0;
    public int MaxAlerts { get; set; } = 1000;
}

// Data Models
public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public SystemMetrics System { get; set; } = new();
    public DatabaseMetrics Database { get; set; } = new();
    public ApiMetrics Api { get; set; } = new();
    public MemoryMetrics Memory { get; set; } = new();
    public CacheMetrics Cache { get; set; } = new();
    public List<CustomMetric> Custom { get; set; } = new();
}

public class SystemMetrics
{
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public TimeSpan Uptime { get; set; }
    public GcMetrics GcCollections { get; set; } = new();
}

public class GcMetrics
{
    public int Gen0 { get; set; }
    public int Gen1 { get; set; }
    public int Gen2 { get; set; }
}

public class DatabaseMetrics
{
    public string ConnectionState { get; set; } = string.Empty;
    public int ActiveConnections { get; set; }
    public int ConnectionPoolSize { get; set; }
    public int PendingMigrations { get; set; }
    public double AverageQueryTime { get; set; }
    public List<DatabaseQueryMetrics> QueryMetrics { get; set; } = new();
    public List<DatabaseQueryMetrics> SlowQueries { get; set; } = new();
}

public class DatabaseQueryMetrics
{
    public string Query { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public double TotalExecutionTime { get; set; }
    public double AverageExecutionTime { get; set; }
    public int ErrorCount { get; set; }
    public DateTime LastExecuted { get; set; }
}

public class ApiMetrics
{
    public int TotalRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public List<ApiEndpointMetrics> EndpointMetrics { get; set; } = new();
    public List<ApiEndpointMetrics> SlowestEndpoints { get; set; } = new();
}

public class ApiEndpointMetrics
{
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public double TotalResponseTime { get; set; }
    public double AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
    public DateTime LastCalled { get; set; }
}

public class MemoryMetrics
{
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long ManagedMemory { get; set; }
    public long Gen0HeapSize { get; set; }
    public long Gen1HeapSize { get; set; }
    public long Gen2HeapSize { get; set; }
    public long LargeObjectHeapSize { get; set; }
    public double MemoryPressure { get; set; }
}

public class CacheMetrics
{
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public int EntryCount { get; set; }
    public long MemoryUsage { get; set; }
    public int EvictionCount { get; set; }
}

public class CustomMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, object> Tags { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class PerformanceAlert
{
    public AlertType Type { get; set; }
    public string Metric { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Suggestion { get; set; }
}

public enum AlertType
{
    Info,
    Warning,
    Critical
}

public class PerformanceInsight
{
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string[] Recommendations { get; set; } = Array.Empty<string>();
    public Dictionary<string, double> Metrics { get; set; } = new();
}

public class PerformanceTrend
{
    public TimeSpan Period { get; set; }
    public Dictionary<string, double> Trends { get; set; } = new();
}

public class PerformanceBaseline
{
    public DateTime Timestamp { get; set; }
    public double SystemCpuUsage { get; set; }
    public long SystemMemoryUsage { get; set; }
    public double AverageQueryTime { get; set; }
    public double AverageApiResponseTime { get; set; }
}

public class PerformanceReport
{
    public DateTime Timestamp { get; set; }
    public PerformanceMetrics Metrics { get; set; } = new();
    public List<PerformanceAlert> Alerts { get; set; } = new();
    public List<PerformanceInsight> Insights { get; set; } = new();
    public PerformanceTrend Trend { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}