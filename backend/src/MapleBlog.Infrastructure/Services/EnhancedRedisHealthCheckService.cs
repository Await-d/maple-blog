using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 增强的Redis健康检查服务，提供详细的监控和指标收集
/// </summary>
public class EnhancedRedisHealthCheckService : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<EnhancedRedisHealthCheckService> _logger;
    private readonly RedisHealthCheckOptions _options;
    private static readonly object _lockObject = new();
    private static DateTime _lastDetailedCheck = DateTime.MinValue;
    private static readonly Dictionary<string, object> _cachedMetrics = new();

    public EnhancedRedisHealthCheckService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<EnhancedRedisHealthCheckService> logger,
        IOptions<RedisHealthCheckOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new RedisHealthCheckOptions();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // 检查Redis连接状态
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis connection is not established");
                return HealthCheckResult.Unhealthy("Redis connection is not established");
            }

            // 获取数据库实例
            var database = _connectionMultiplexer.GetDatabase();

            // 执行PING命令测试连接
            var pingTask = database.PingAsync();

            // 使用超时控制
            using var timeoutCts = new CancellationTokenSource(_options.TimeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            TimeSpan pingResult;
            try
            {
                pingResult = await pingTask.WaitAsync(combinedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                _logger.LogError("Redis health check timed out after {TimeoutMs}ms", _options.TimeoutMs);
                return HealthCheckResult.Unhealthy($"Redis health check timed out after {_options.TimeoutMs}ms");
            }

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            // 检查响应时间是否超过阈值
            var status = HealthStatus.Healthy;
            var description = "Redis is healthy";

            if (responseTime > _options.ResponseTimeThresholdMs)
            {
                status = HealthStatus.Degraded;
                description = $"Redis response time {responseTime}ms exceeds threshold {_options.ResponseTimeThresholdMs}ms";

                _logger.LogWarning("Redis response time {ResponseTime}ms exceeds threshold {Threshold}ms",
                    responseTime, _options.ResponseTimeThresholdMs);
            }

            // 收集健康状态数据
            var healthData = await CreateHealthDataAsync(responseTime, pingResult, database, cancellationToken);

            _logger.LogDebug("Redis health check completed. Status: {Status}, Response time: {ResponseTime}ms",
                status, responseTime);

            return new HealthCheckResult(status, description, data: healthData);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection error during health check");
            return HealthCheckResult.Unhealthy("Redis connection error", ex);
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogError(ex, "Redis timeout during health check");
            return HealthCheckResult.Unhealthy("Redis timeout", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Redis health check was cancelled");
            return HealthCheckResult.Unhealthy("Redis health check cancelled", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Redis health check");
            return HealthCheckResult.Unhealthy("Unexpected Redis error", ex);
        }
    }

    private async Task<Dictionary<string, object>> CreateHealthDataAsync(
        long responseTime,
        TimeSpan pingResult,
        IDatabase database,
        CancellationToken cancellationToken)
    {
        var healthData = new Dictionary<string, object>
        {
            ["response_time_ms"] = responseTime,
            ["ping_result_ms"] = pingResult.TotalMilliseconds,
            ["is_connected"] = _connectionMultiplexer.IsConnected,
            ["client_name"] = _connectionMultiplexer.ClientName,
            ["configuration"] = _connectionMultiplexer.Configuration,
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["connection_status"] = GetConnectionStatus()
        };

        // 添加端点信息
        var endPoints = _connectionMultiplexer.GetEndPoints();
        var servers = endPoints.Select(ep => _connectionMultiplexer.GetServer(ep)).ToList();

        healthData["endpoints"] = endPoints.Select(ep => ep.ToString()).ToList();
        healthData["servers_count"] = servers.Count;
        healthData["connected_servers"] = servers.Count(s => s.IsConnected);

        // 如果启用了详细检查且不是频繁调用，执行额外的测试
        if (_options.EnableDetailedChecks && ShouldPerformDetailedCheck())
        {
            await PerformDetailedChecksAsync(database, healthData, servers, cancellationToken);
        }
        else if (_cachedMetrics.Count > 0)
        {
            // 使用缓存的指标数据
            foreach (var metric in _cachedMetrics)
            {
                healthData[metric.Key] = metric.Value;
            }
        }

        return healthData;
    }

    private string GetConnectionStatus()
    {
        if (!_connectionMultiplexer.IsConnected)
            return "Disconnected";

        var endPoints = _connectionMultiplexer.GetEndPoints();
        var connectedCount = endPoints.Count(ep => _connectionMultiplexer.GetServer(ep).IsConnected);

        return connectedCount == endPoints.Length ? "FullyConnected" :
               connectedCount > 0 ? "PartiallyConnected" : "Disconnected";
    }

    private bool ShouldPerformDetailedCheck()
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var interval = TimeSpan.FromSeconds(30); // 限制详细检查频率

            if (now - _lastDetailedCheck >= interval)
            {
                _lastDetailedCheck = now;
                return true;
            }
            return false;
        }
    }

    private async Task PerformDetailedChecksAsync(
        IDatabase database,
        Dictionary<string, object> healthData,
        List<IServer> servers,
        CancellationToken cancellationToken)
    {
        try
        {
            // 执行读写测试
            await PerformReadWriteTestAsync(database, healthData, cancellationToken);

            // 收集服务器信息
            await CollectServerInfoAsync(servers.FirstOrDefault(), healthData, cancellationToken);

            // 收集连接池信息
            CollectConnectionPoolInfo(healthData);

            // 缓存指标数据
            lock (_lockObject)
            {
                _cachedMetrics.Clear();
                foreach (var item in healthData.Where(kv => IsMetricData(kv.Key)))
                {
                    _cachedMetrics[item.Key] = item.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to perform detailed Redis health checks");
            healthData["detailed_checks_error"] = ex.Message;
        }
    }

    private async Task PerformReadWriteTestAsync(IDatabase database, Dictionary<string, object> healthData, CancellationToken cancellationToken)
    {
        var testKey = $"health-check:{Environment.MachineName}:{Guid.NewGuid()}";
        var testValue = $"health-check-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        var setStopwatch = Stopwatch.StartNew();
        await database.StringSetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
        setStopwatch.Stop();

        var getStopwatch = Stopwatch.StartNew();
        var retrievedValue = await database.StringGetAsync(testKey);
        getStopwatch.Stop();

        // 清理测试数据
        _ = Task.Run(async () => {
            try { await database.KeyDeleteAsync(testKey); }
            catch { /* Ignore cleanup errors */ }
        }, cancellationToken);

        healthData["set_operation_ms"] = setStopwatch.ElapsedMilliseconds;
        healthData["get_operation_ms"] = getStopwatch.ElapsedMilliseconds;
        healthData["read_write_test"] = retrievedValue == testValue ? "passed" : "failed";
        healthData["test_key_expiry"] = "1m";
    }

    private async Task CollectServerInfoAsync(IServer? server, Dictionary<string, object> healthData, CancellationToken cancellationToken)
    {
        if (server == null || !server.IsConnected)
        {
            healthData["server_info"] = "unavailable";
            return;
        }

        try
        {
            var info = await server.InfoAsync();
            var serverSection = info.FirstOrDefault(i => i.Key == "Server");
            var memorySection = info.FirstOrDefault(i => i.Key == "Memory");
            var statsSection = info.FirstOrDefault(i => i.Key == "Stats");
            var clientsSection = info.FirstOrDefault(i => i.Key == "Clients");
            var replicationSection = info.FirstOrDefault(i => i.Key == "Replication");

            if (serverSection != null)
            {
                healthData["redis_version"] = serverSection.FirstOrDefault(kv => kv.Key == "redis_version").Value;
                healthData["uptime_seconds"] = serverSection.FirstOrDefault(kv => kv.Key == "uptime_in_seconds").Value;
            }

            if (memorySection != null)
            {
                healthData["used_memory"] = memorySection.FirstOrDefault(kv => kv.Key == "used_memory").Value;
                healthData["used_memory_human"] = memorySection.FirstOrDefault(kv => kv.Key == "used_memory_human").Value;
                healthData["used_memory_peak"] = memorySection.FirstOrDefault(kv => kv.Key == "used_memory_peak").Value;
            }

            if (clientsSection != null)
            {
                healthData["connected_clients"] = clientsSection.FirstOrDefault(kv => kv.Key == "connected_clients").Value;
                healthData["blocked_clients"] = clientsSection.FirstOrDefault(kv => kv.Key == "blocked_clients").Value;
            }

            if (statsSection != null)
            {
                healthData["total_commands_processed"] = statsSection.FirstOrDefault(kv => kv.Key == "total_commands_processed").Value;
                healthData["instantaneous_ops_per_sec"] = statsSection.FirstOrDefault(kv => kv.Key == "instantaneous_ops_per_sec").Value;
            }

            if (replicationSection != null)
            {
                healthData["role"] = replicationSection.FirstOrDefault(kv => kv.Key == "role").Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect Redis server info");
            healthData["server_info_error"] = ex.Message;
        }
    }

    private void CollectConnectionPoolInfo(Dictionary<string, object> healthData)
    {
        try
        {
            // 收集连接池统计信息
            healthData["connection_pool"] = new
            {
                is_connected = _connectionMultiplexer.IsConnected,
                client_name = _connectionMultiplexer.ClientName,
                configuration_string = _connectionMultiplexer.Configuration,
                timeout_milliseconds = _connectionMultiplexer.TimeoutMilliseconds,
                // Note: Some properties might not be available in all versions
                // operational_status = "Active"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect connection pool info");
            healthData["connection_pool_error"] = ex.Message;
        }
    }

    private static bool IsMetricData(string key)
    {
        return key.Contains("_ms") ||
               key.Contains("memory") ||
               key.Contains("clients") ||
               key.Contains("commands") ||
               key.Contains("ops_per_sec") ||
               key.Contains("uptime") ||
               key.Contains("version");
    }
}