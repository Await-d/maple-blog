using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using System.Diagnostics;
using System.Data;
using System.Text.Json;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Admin专用的数据库健康检查服务
/// 提供更详细的数据库监控和性能分析
/// </summary>
public class AdminDatabaseHealthCheckService : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminDatabaseHealthCheckService> _logger;
    private readonly AdminDatabaseHealthOptions _options;

    public AdminDatabaseHealthCheckService(
        ApplicationDbContext context,
        ILogger<AdminDatabaseHealthCheckService> logger,
        IOptions<AdminDatabaseHealthOptions> options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var healthData = new Dictionary<string, object>();

            // 1. 基本连接检查
            var connectionResult = await CheckDatabaseConnection(healthData, cancellationToken);
            if (connectionResult != HealthStatus.Healthy)
            {
                stopwatch.Stop();
                return new HealthCheckResult(connectionResult, "Database connection failed", data: healthData);
            }

            // 2. 执行查询性能测试
            await CheckQueryPerformance(healthData, cancellationToken);

            // 3. 检查连接池状态
            await CheckConnectionPoolStatus(healthData);

            // 4. 检查数据库空间使用情况
            await CheckDatabaseSpaceUsage(healthData, cancellationToken);

            // 5. 检查索引状态
            await CheckIndexHealth(healthData, cancellationToken);

            // 6. 检查表统计信息
            await CheckTableStatistics(healthData, cancellationToken);

            // 7. 检查慢查询和锁等待
            await CheckPerformanceMetrics(healthData, cancellationToken);

            stopwatch.Stop();
            var totalResponseTime = stopwatch.ElapsedMilliseconds;
            healthData["total_check_time_ms"] = totalResponseTime;
            healthData["timestamp"] = DateTimeOffset.UtcNow;
            healthData["check_version"] = "1.0.0";

            // 综合健康状态评估
            var overallStatus = EvaluateOverallHealth(healthData);
            var description = GetHealthDescription(overallStatus, healthData);

            _logger.LogInformation("Admin database health check completed. Status: {Status}, Total time: {TotalTime}ms",
                overallStatus, totalResponseTime);

            return new HealthCheckResult(overallStatus, description, data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin database health check failed with exception");
            var errorData = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["error_type"] = ex.GetType().Name,
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            return HealthCheckResult.Unhealthy("Database health check failed", ex, errorData);
        }
    }

    private async Task<HealthStatus> CheckDatabaseConnection(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        var connectionStopwatch = Stopwatch.StartNew();

        try
        {
            // 检查数据库连接
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            connectionStopwatch.Stop();

            healthData["connection_check"] = new
            {
                can_connect = canConnect,
                response_time_ms = connectionStopwatch.ElapsedMilliseconds,
                provider_name = _context.Database.ProviderName,
                connection_state = _context.Database.GetDbConnection().State.ToString(),
                connection_timeout = _context.Database.GetDbConnection().ConnectionTimeout
            };

            if (!canConnect)
            {
                return HealthStatus.Unhealthy;
            }

            // 根据连接时间评估状态
            return connectionStopwatch.ElapsedMilliseconds > _options.ConnectionTimeoutThresholdMs
                ? HealthStatus.Degraded : HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            connectionStopwatch.Stop();
            healthData["connection_check"] = new
            {
                can_connect = false,
                error = ex.Message,
                response_time_ms = connectionStopwatch.ElapsedMilliseconds
            };
            return HealthStatus.Unhealthy;
        }
    }

    private async Task CheckQueryPerformance(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        var performanceMetrics = new List<object>();

        // 测试查询1：简单计数查询
        var countStopwatch = Stopwatch.StartNew();
        try
        {
            var userCount = await _context.Users.CountAsync(cancellationToken);
            countStopwatch.Stop();

            performanceMetrics.Add(new
            {
                query_type = "user_count",
                result = userCount,
                response_time_ms = countStopwatch.ElapsedMilliseconds,
                status = countStopwatch.ElapsedMilliseconds > _options.SlowQueryThresholdMs ? "slow" : "normal"
            });
        }
        catch (Exception ex)
        {
            countStopwatch.Stop();
            performanceMetrics.Add(new
            {
                query_type = "user_count",
                error = ex.Message,
                response_time_ms = countStopwatch.ElapsedMilliseconds,
                status = "failed"
            });
        }

        // 测试查询2：复杂查询
        var complexStopwatch = Stopwatch.StartNew();
        try
        {
            var postStats = await _context.Posts
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            complexStopwatch.Stop();

            performanceMetrics.Add(new
            {
                query_type = "post_statistics",
                result_count = postStats.Count,
                response_time_ms = complexStopwatch.ElapsedMilliseconds,
                status = complexStopwatch.ElapsedMilliseconds > _options.SlowQueryThresholdMs ? "slow" : "normal"
            });
        }
        catch (Exception ex)
        {
            complexStopwatch.Stop();
            performanceMetrics.Add(new
            {
                query_type = "post_statistics",
                error = ex.Message,
                response_time_ms = complexStopwatch.ElapsedMilliseconds,
                status = "failed"
            });
        }

        healthData["query_performance"] = performanceMetrics;
    }

    private async Task CheckConnectionPoolStatus(Dictionary<string, object> healthData)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            var poolInfo = new
            {
                connection_string_hash = connection.ConnectionString?.GetHashCode().ToString("X"),
                connection_timeout = connection.ConnectionTimeout,
                database_name = connection.Database,
                server_version = connection.ServerVersion ?? "Unknown",
                state = connection.State.ToString()
            };

            healthData["connection_pool"] = poolInfo;
        }
        catch (Exception ex)
        {
            healthData["connection_pool"] = new { error = ex.Message };
        }

        await Task.CompletedTask;
    }

    private async Task CheckDatabaseSpaceUsage(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        try
        {
            // 对于SQLite，获取数据库文件大小
            if (_context.Database.ProviderName?.Contains("Sqlite") == true)
            {
                var connectionString = _context.Database.GetDbConnection().ConnectionString;
                var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)");

                if (dataSourceMatch.Success)
                {
                    var dbPath = dataSourceMatch.Groups[1].Value;
                    if (File.Exists(dbPath))
                    {
                        var fileInfo = new FileInfo(dbPath);
                        healthData["database_space"] = new
                        {
                            file_path = dbPath,
                            file_size_bytes = fileInfo.Length,
                            file_size_mb = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2),
                            last_modified = fileInfo.LastWriteTime,
                            is_readonly = fileInfo.IsReadOnly
                        };
                    }
                }
            }
            else
            {
                // 对于其他数据库，可以执行特定的空间查询
                healthData["database_space"] = new { provider = _context.Database.ProviderName, note = "Space check not implemented for this provider" };
            }
        }
        catch (Exception ex)
        {
            healthData["database_space"] = new { error = ex.Message };
        }

        await Task.CompletedTask;
    }

    private async Task CheckIndexHealth(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        try
        {
            // 检查主要表的索引使用情况
            var indexChecks = new List<object>();

            // 检查用户表索引
            var userIndexStopwatch = Stopwatch.StartNew();
            var userByEmail = await _context.Users
                .Where(u => u.Email.Value.Contains("@"))
                .CountAsync(cancellationToken);
            userIndexStopwatch.Stop();

            indexChecks.Add(new
            {
                table = "Users",
                index_type = "email_search",
                response_time_ms = userIndexStopwatch.ElapsedMilliseconds,
                result_count = userByEmail,
                status = userIndexStopwatch.ElapsedMilliseconds > _options.IndexScanThresholdMs ? "slow" : "optimal"
            });

            healthData["index_health"] = indexChecks;
        }
        catch (Exception ex)
        {
            healthData["index_health"] = new { error = ex.Message };
        }
    }

    private async Task CheckTableStatistics(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        try
        {
            var tableStats = new List<object>();

            // 获取主要表的统计信息
            var tables = new (string Name, Func<Task<int>> Query)[]
            {
                ("Users", () => _context.Users.CountAsync(cancellationToken)),
                ("Posts", () => _context.Posts.CountAsync(cancellationToken)),
                ("Categories", () => _context.Categories.CountAsync(cancellationToken)),
                ("Tags", () => _context.Tags.CountAsync(cancellationToken)),
                ("Comments", () => _context.Comments.CountAsync(cancellationToken))
            };

            foreach (var table in tables)
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var count = await table.Query();
                    stopwatch.Stop();

                    tableStats.Add(new
                    {
                        table_name = table.Name,
                        record_count = count,
                        query_time_ms = stopwatch.ElapsedMilliseconds,
                        status = stopwatch.ElapsedMilliseconds > _options.TableScanThresholdMs ? "slow" : "normal"
                    });
                }
                catch (Exception ex)
                {
                    tableStats.Add(new
                    {
                        table_name = table.Name,
                        error = ex.Message,
                        status = "error"
                    });
                }
            }

            healthData["table_statistics"] = tableStats;
        }
        catch (Exception ex)
        {
            healthData["table_statistics"] = new { error = ex.Message };
        }
    }

    private async Task CheckPerformanceMetrics(
        Dictionary<string, object> healthData,
        CancellationToken cancellationToken)
    {
        try
        {
            var performanceData = new Dictionary<string, object>();

            // 检查最近的数据增长情况
            var recentPosts = await _context.Posts
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-7))
                .CountAsync(cancellationToken);

            var recentComments = await _context.Comments
                .Where(c => c.CreatedAt > DateTime.UtcNow.AddDays(-7))
                .CountAsync(cancellationToken);

            performanceData["recent_activity"] = new
            {
                posts_last_7_days = recentPosts,
                comments_last_7_days = recentComments,
                growth_indicator = recentPosts > _options.HighActivityThreshold ? "high" : "normal"
            };

            // 模拟事务性能测试
            var transactionStopwatch = Stopwatch.StartNew();
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            // 执行一个简单的读操作来测试事务性能
            await _context.Users.AnyAsync(cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            transactionStopwatch.Stop();

            performanceData["transaction_performance"] = new
            {
                test_transaction_time_ms = transactionStopwatch.ElapsedMilliseconds,
                status = transactionStopwatch.ElapsedMilliseconds > _options.TransactionTimeoutMs ? "slow" : "normal"
            };

            healthData["performance_metrics"] = performanceData;
        }
        catch (Exception ex)
        {
            healthData["performance_metrics"] = new { error = ex.Message };
        }
    }

    private HealthStatus EvaluateOverallHealth(Dictionary<string, object> healthData)
    {
        var issues = new List<string>();

        // 检查连接状态
        if (healthData.TryGetValue("connection_check", out var connectionObj))
        {
            var connectionData = JsonSerializer.Serialize(connectionObj);
            if (connectionData.Contains("\"can_connect\":false"))
            {
                return HealthStatus.Unhealthy;
            }
        }

        // 检查查询性能
        if (healthData.TryGetValue("query_performance", out var queryObj))
        {
            var queryData = JsonSerializer.Serialize(queryObj);
            if (queryData.Contains("\"status\":\"failed\""))
            {
                issues.Add("Query performance issues detected");
            }
            else if (queryData.Contains("\"status\":\"slow\""))
            {
                issues.Add("Slow queries detected");
            }
        }

        // 检查索引健康状况
        if (healthData.TryGetValue("index_health", out var indexObj))
        {
            var indexData = JsonSerializer.Serialize(indexObj);
            if (indexData.Contains("\"status\":\"slow\""))
            {
                issues.Add("Index performance issues detected");
            }
        }

        return issues.Count switch
        {
            0 => HealthStatus.Healthy,
            _ when issues.Count < 3 => HealthStatus.Degraded,
            _ => HealthStatus.Unhealthy
        };
    }

    private string GetHealthDescription(HealthStatus status, Dictionary<string, object> healthData)
    {
        return status switch
        {
            HealthStatus.Healthy => "Admin database is performing optimally",
            HealthStatus.Degraded => "Admin database has some performance issues but is functional",
            HealthStatus.Unhealthy => "Admin database has critical issues requiring attention",
            _ => "Admin database status unknown"
        };
    }
}

/// <summary>
/// Admin数据库健康检查配置选项
/// </summary>
public class AdminDatabaseHealthOptions
{
    public const string SectionName = "AdminDatabaseHealth";

    /// <summary>
    /// 连接超时阈值（毫秒）
    /// </summary>
    public int ConnectionTimeoutThresholdMs { get; set; } = 5000;

    /// <summary>
    /// 慢查询阈值（毫秒）
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 2000;

    /// <summary>
    /// 索引扫描阈值（毫秒）
    /// </summary>
    public int IndexScanThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 表扫描阈值（毫秒）
    /// </summary>
    public int TableScanThresholdMs { get; set; } = 1500;

    /// <summary>
    /// 事务超时阈值（毫秒）
    /// </summary>
    public int TransactionTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// 高活动阈值
    /// </summary>
    public int HighActivityThreshold { get; set; } = 100;

    /// <summary>
    /// 启用详细日志
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// 缓存健康检查结果的时间（秒）
    /// </summary>
    public int CacheResultsForSeconds { get; set; } = 30;
}