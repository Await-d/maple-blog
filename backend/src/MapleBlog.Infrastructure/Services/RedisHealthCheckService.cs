using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Redis健康检查服务，提供详细的连接状态和性能监控
/// </summary>
public class RedisHealthCheckService : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisHealthCheckService> _logger;
    private readonly RedisHealthCheckOptions _options;

    public RedisHealthCheckService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisHealthCheckService> logger,
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
            var pingResult = await database.PingAsync();
            stopwatch.Stop();

            var responseTime = stopwatch.ElapsedMilliseconds;

            // 检查响应时间是否超过阈值
            if (responseTime > _options.ResponseTimeThresholdMs)
            {
                _logger.LogWarning("Redis response time {ResponseTime}ms exceeds threshold {Threshold}ms",
                    responseTime, _options.ResponseTimeThresholdMs);

                return HealthCheckResult.Degraded(
                    $"Redis response time {responseTime}ms exceeds threshold {_options.ResponseTimeThresholdMs}ms",
                    data: CreateHealthData(responseTime, pingResult));
            }

            // 收集额外的健康状态信息
            var healthData = CreateHealthData(responseTime, pingResult);

            // 如果启用了详细检查，执行额外的测试
            if (_options.EnableDetailedChecks)
            {
                await PerformDetailedChecksAsync(database, healthData, cancellationToken);
            }

            _logger.LogDebug("Redis health check passed. Response time: {ResponseTime}ms", responseTime);

            return HealthCheckResult.Healthy("Redis is healthy", healthData);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Redis health check");
            return HealthCheckResult.Unhealthy("Unexpected Redis error", ex);
        }
    }

    private Dictionary<string, object> CreateHealthData(long responseTime, TimeSpan pingResult)
    {
        var endPoints = _connectionMultiplexer.GetEndPoints();
        var servers = endPoints.Select(ep => _connectionMultiplexer.GetServer(ep)).ToList();

        return new Dictionary<string, object>
        {
            ["response_time_ms"] = responseTime,
            ["ping_result_ms"] = pingResult.TotalMilliseconds,
            ["is_connected"] = _connectionMultiplexer.IsConnected,
            ["endpoints"] = endPoints.Select(ep => ep.ToString()).ToList(),
            ["servers_count"] = servers.Count,
            ["connected_servers"] = servers.Count(s => s.IsConnected),
            ["client_name"] = _connectionMultiplexer.ClientName,
            ["configuration"] = _connectionMultiplexer.Configuration,
            // Note: LastHeartbeat and OperatingSystem properties may not be available in all Redis client versions
            // ["last_heartbeat"] = _connectionMultiplexer.LastHeartbeat,
            // ["operating_system"] = _connectionMultiplexer.OperatingSystem,
            ["timestamp"] = DateTimeOffset.UtcNow
        };
    }

    private async Task PerformDetailedChecksAsync(IDatabase database, Dictionary<string, object> healthData, CancellationToken cancellationToken)
    {
        try
        {
            // 测试基本的读写操作
            var testKey = $"health-check:{Guid.NewGuid()}";
            var testValue = "health-check-value";

            var setStopwatch = Stopwatch.StartNew();
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            setStopwatch.Stop();

            var getStopwatch = Stopwatch.StartNew();
            var retrievedValue = await database.StringGetAsync(testKey);
            getStopwatch.Stop();

            await database.KeyDeleteAsync(testKey);

            healthData["set_operation_ms"] = setStopwatch.ElapsedMilliseconds;
            healthData["get_operation_ms"] = getStopwatch.ElapsedMilliseconds;
            healthData["read_write_test"] = retrievedValue == testValue ? "passed" : "failed";

            // 获取服务器信息
            var endPoints = _connectionMultiplexer.GetEndPoints();
            if (endPoints.Length > 0)
            {
                var server = _connectionMultiplexer.GetServer(endPoints[0]);
                var info = await server.InfoAsync();

                var serverSection = info.FirstOrDefault(i => i.Key == "Server");
                var memorySection = info.FirstOrDefault(i => i.Key == "Memory");
                var statsSection = info.FirstOrDefault(i => i.Key == "Stats");
                var clientsSection = info.FirstOrDefault(i => i.Key == "Clients");

                if (serverSection != null)
                {
                    healthData["redis_version"] = serverSection.FirstOrDefault(kv => kv.Key == "redis_version").Value;
                }
                if (memorySection != null)
                {
                    healthData["used_memory"] = memorySection.FirstOrDefault(kv => kv.Key == "used_memory").Value;
                }
                if (clientsSection != null)
                {
                    healthData["connected_clients"] = clientsSection.FirstOrDefault(kv => kv.Key == "connected_clients").Value;
                }
                if (statsSection != null)
                {
                    healthData["total_commands_processed"] = statsSection.FirstOrDefault(kv => kv.Key == "total_commands_processed").Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to perform detailed Redis health checks");
            healthData["detailed_checks_error"] = ex.Message;
        }
    }
}

/// <summary>
/// Redis健康检查配置选项
/// </summary>
public class RedisHealthCheckOptions
{
    /// <summary>
    /// 响应时间阈值（毫秒），超过此值将标记为降级状态
    /// </summary>
    public long ResponseTimeThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 是否启用详细检查（包括读写测试和服务器信息）
    /// </summary>
    public bool EnableDetailedChecks { get; set; } = true;

    /// <summary>
    /// 健康检查超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;
}