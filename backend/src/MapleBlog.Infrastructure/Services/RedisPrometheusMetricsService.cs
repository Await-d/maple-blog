using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Redis Prometheus指标收集服务，导出详细的Redis监控指标
/// </summary>
public class RedisPrometheusMetricsService : BackgroundService, IRedisPrometheusMetricsService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisPrometheusMetricsService> _logger;
    private readonly RedisPrometheusOptions _options;
    private readonly Timer _metricsTimer;

    // Prometheus指标定义
    private static readonly Gauge _redisConnectionStatus = Metrics
        .CreateGauge("redis_connection_status", "Redis connection status (1 = connected, 0 = disconnected)",
            new[] { "endpoint", "client_name" });

    private static readonly Gauge _redisResponseTime = Metrics
        .CreateGauge("redis_response_time_milliseconds", "Redis ping response time in milliseconds",
            new[] { "endpoint" });

    private static readonly Gauge _redisUsedMemory = Metrics
        .CreateGauge("redis_used_memory_bytes", "Redis used memory in bytes",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisUsedMemoryPeak = Metrics
        .CreateGauge("redis_used_memory_peak_bytes", "Redis peak memory usage in bytes",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisConnectedClients = Metrics
        .CreateGauge("redis_connected_clients_total", "Number of connected Redis clients",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisBlockedClients = Metrics
        .CreateGauge("redis_blocked_clients_total", "Number of blocked Redis clients",
            new[] { "endpoint", "instance" });

    private static readonly Counter _redisTotalCommandsProcessed = Metrics
        .CreateCounter("redis_commands_processed_total", "Total number of commands processed by Redis",
            new[] { "endpoint", "instance" });

    private static readonly Counter _redisTotalConnections = Metrics
        .CreateCounter("redis_connections_received_total", "Total number of connections received by Redis",
            new[] { "endpoint", "instance" });

    private static readonly Counter _redisRejectedConnections = Metrics
        .CreateCounter("redis_connections_rejected_total", "Total number of rejected connections",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisKeyspaceHits = Metrics
        .CreateGauge("redis_keyspace_hits_total", "Number of successful lookups of keys in the main dictionary",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisKeyspaceMisses = Metrics
        .CreateGauge("redis_keyspace_misses_total", "Number of failed lookups of keys in the main dictionary",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisEvictedKeys = Metrics
        .CreateGauge("redis_evicted_keys_total", "Number of evicted keys due to maxmemory limit",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisExpiredKeys = Metrics
        .CreateGauge("redis_expired_keys_total", "Total number of key expiration events",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisUptimeSeconds = Metrics
        .CreateGauge("redis_uptime_seconds", "Number of seconds since Redis server start",
            new[] { "endpoint", "instance", "version" });

    private static readonly Gauge _redisInstantaneousOpsPerSec = Metrics
        .CreateGauge("redis_instantaneous_ops_per_sec", "Number of commands processed per second",
            new[] { "endpoint", "instance" });

    private static readonly Gauge _redisHealthCheckStatus = Metrics
        .CreateGauge("redis_health_check_status", "Redis health check status (2 = healthy, 1 = degraded, 0 = unhealthy)",
            new[] { "endpoint", "check_type" });

    private static readonly Histogram _redisOperationDuration = Metrics
        .CreateHistogram("redis_operation_duration_seconds", "Duration of Redis operations",
            new HistogramConfiguration
            {
                Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 },
                LabelNames = new[] { "endpoint", "operation" }
            });

    // 用于存储上次收集的值，计算增量
    private readonly ConcurrentDictionary<string, long> _lastCommandsProcessed = new();
    private readonly ConcurrentDictionary<string, long> _lastTotalConnections = new();
    private readonly ConcurrentDictionary<string, long> _lastRejectedConnections = new();

    public RedisPrometheusMetricsService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisPrometheusMetricsService> logger,
        IOptions<RedisPrometheusOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new RedisPrometheusOptions();

        // 设置定时收集指标
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(_options.MetricsCollectionIntervalSeconds));

        // 订阅连接事件
        _connectionMultiplexer.ConnectionFailed += OnConnectionFailed;
        _connectionMultiplexer.ConnectionRestored += OnConnectionRestored;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis Prometheus metrics service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectHealthMetrics(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Redis metrics collection");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Redis Prometheus metrics service stopped");
    }

    public async Task<Dictionary<string, double>> GetCurrentMetricsAsync()
    {
        var metrics = new Dictionary<string, double>();

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                metrics["connection_status"] = 0;
                return metrics;
            }

            var database = _connectionMultiplexer.GetDatabase();
            var stopwatch = Stopwatch.StartNew();

            await database.PingAsync();
            stopwatch.Stop();

            metrics["connection_status"] = 1;
            metrics["response_time_ms"] = stopwatch.ElapsedMilliseconds;

            // 收集服务器指标
            var endPoints = _connectionMultiplexer.GetEndPoints();
            if (endPoints.Length > 0)
            {
                var server = _connectionMultiplexer.GetServer(endPoints[0]);
                if (server.IsConnected)
                {
                    var info = await server.InfoAsync();
                    var serverMetrics = ParseServerInfoForMetrics(info);

                    foreach (var metric in serverMetrics)
                    {
                        metrics[metric.Key] = metric.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect current Redis metrics");
            metrics["connection_status"] = 0;
        }

        return metrics;
    }

    private async Task CollectHealthMetrics(CancellationToken cancellationToken)
    {
        var endPoints = _connectionMultiplexer.GetEndPoints();

        foreach (var endPoint in endPoints)
        {
            var endPointStr = endPoint.ToString();

            try
            {
                if (!_connectionMultiplexer.IsConnected)
                {
                    _redisConnectionStatus.WithLabels(endPointStr, _connectionMultiplexer.ClientName ?? "unknown").Set(0);
                    _redisHealthCheckStatus.WithLabels(endPointStr, "connection").Set(0);
                    continue;
                }

                var database = _connectionMultiplexer.GetDatabase();
                var stopwatch = Stopwatch.StartNew();

                using var timer = _redisOperationDuration.WithLabels(endPointStr, "ping").NewTimer();
                await database.PingAsync();

                stopwatch.Stop();

                // 连接状态指标
                _redisConnectionStatus.WithLabels(endPointStr, _connectionMultiplexer.ClientName ?? "unknown").Set(1);
                _redisResponseTime.WithLabels(endPointStr).Set(stopwatch.ElapsedMilliseconds);

                // 健康状态指标
                var healthStatus = stopwatch.ElapsedMilliseconds <= _options.ResponseTimeThresholdMs ? 2 : 1;
                _redisHealthCheckStatus.WithLabels(endPointStr, "ping").Set(healthStatus);

                _logger.LogDebug("Collected health metrics for Redis endpoint {EndPoint}, response time: {ResponseTime}ms",
                    endPointStr, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect health metrics for Redis endpoint {EndPoint}", endPointStr);
                _redisConnectionStatus.WithLabels(endPointStr, _connectionMultiplexer.ClientName ?? "unknown").Set(0);
                _redisHealthCheckStatus.WithLabels(endPointStr, "ping").Set(0);
            }
        }
    }

    private async void CollectMetrics(object? state)
    {
        try
        {
            if (!_connectionMultiplexer.IsConnected)
                return;

            var endPoints = _connectionMultiplexer.GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                try
                {
                    var server = _connectionMultiplexer.GetServer(endPoint);
                    if (!server.IsConnected)
                        continue;

                    var endPointStr = endPoint.ToString();

                    using var timer = _redisOperationDuration.WithLabels(endPointStr, "info").NewTimer();
                    var info = await server.InfoAsync();

                    await ProcessServerInfo(info, endPointStr);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect metrics for endpoint {EndPoint}", endPoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect Redis Prometheus metrics");
        }
    }

    private async Task ProcessServerInfo(IGrouping<string, KeyValuePair<string, string>>[] info, string endPoint)
    {
        try
        {
            var instanceName = "redis-instance"; // 可以从配置中获取

            var serverInfo = info.FirstOrDefault(g => g.Key == "Server");
            var memoryInfo = info.FirstOrDefault(g => g.Key == "Memory");
            var statsInfo = info.FirstOrDefault(g => g.Key == "Stats");
            var clientsInfo = info.FirstOrDefault(g => g.Key == "Clients");

            // 服务器信息
            if (serverInfo != null)
            {
                var version = serverInfo.FirstOrDefault(kv => kv.Key == "redis_version").Value ?? "unknown";
                var uptime = long.TryParse(serverInfo.FirstOrDefault(kv => kv.Key == "uptime_in_seconds").Value, out var uptimeValue) ? uptimeValue : 0;

                _redisUptimeSeconds.WithLabels(endPoint, instanceName, version).Set(uptime);
            }

            // 内存信息
            if (memoryInfo != null)
            {
                var usedMemory = long.TryParse(memoryInfo.FirstOrDefault(kv => kv.Key == "used_memory").Value, out var memoryValue) ? memoryValue : 0;
                var usedMemoryPeak = long.TryParse(memoryInfo.FirstOrDefault(kv => kv.Key == "used_memory_peak").Value, out var memoryPeakValue) ? memoryPeakValue : 0;

                _redisUsedMemory.WithLabels(endPoint, instanceName).Set(usedMemory);
                _redisUsedMemoryPeak.WithLabels(endPoint, instanceName).Set(usedMemoryPeak);
            }

            // 客户端信息
            if (clientsInfo != null)
            {
                var connectedClients = int.TryParse(clientsInfo.FirstOrDefault(kv => kv.Key == "connected_clients").Value, out var clientsValue) ? clientsValue : 0;
                var blockedClients = int.TryParse(clientsInfo.FirstOrDefault(kv => kv.Key == "blocked_clients").Value, out var blockedValue) ? blockedValue : 0;

                _redisConnectedClients.WithLabels(endPoint, instanceName).Set(connectedClients);
                _redisBlockedClients.WithLabels(endPoint, instanceName).Set(blockedClients);
            }

            // 统计信息 - 使用增量值更新计数器
            if (statsInfo != null)
            {
                var commandsProcessed = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "total_commands_processed").Value, out var commandsValue) ? commandsValue : 0;
                var totalConnections = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "total_connections_received").Value, out var connectionsValue) ? connectionsValue : 0;
                var rejectedConnections = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "rejected_connections").Value, out var rejectedValue) ? rejectedValue : 0;
                var keyspaceHits = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "keyspace_hits").Value, out var hitsValue) ? hitsValue : 0;
                var keyspaceMisses = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "keyspace_misses").Value, out var missesValue) ? missesValue : 0;
                var evictedKeys = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "evicted_keys").Value, out var evictedValue) ? evictedValue : 0;
                var expiredKeys = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "expired_keys").Value, out var expiredValue) ? expiredValue : 0;
                var instantaneousOps = double.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "instantaneous_ops_per_sec").Value, out var opsValue) ? opsValue : 0;

                // 处理计数器增量
                UpdateCounterMetric(_redisTotalCommandsProcessed, _lastCommandsProcessed, endPoint, instanceName, "commands_processed", commandsProcessed);
                UpdateCounterMetric(_redisTotalConnections, _lastTotalConnections, endPoint, instanceName, "total_connections", totalConnections);
                UpdateCounterMetric(_redisRejectedConnections, _lastRejectedConnections, endPoint, instanceName, "rejected_connections", rejectedConnections);

                // 更新Gauge指标
                _redisKeyspaceHits.WithLabels(endPoint, instanceName).Set(keyspaceHits);
                _redisKeyspaceMisses.WithLabels(endPoint, instanceName).Set(keyspaceMisses);
                _redisEvictedKeys.WithLabels(endPoint, instanceName).Set(evictedKeys);
                _redisExpiredKeys.WithLabels(endPoint, instanceName).Set(expiredKeys);
                _redisInstantaneousOpsPerSec.WithLabels(endPoint, instanceName).Set(instantaneousOps);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process Redis server info for Prometheus metrics");
        }
    }

    private void UpdateCounterMetric(Counter counter, ConcurrentDictionary<string, long> lastValues,
        string endPoint, string instanceName, string metricType, long currentValue)
    {
        var key = $"{endPoint}:{metricType}";
        var lastValue = lastValues.GetValueOrDefault(key, 0);

        if (currentValue >= lastValue)
        {
            var increment = currentValue - lastValue;
            if (increment > 0)
            {
                counter.WithLabels(endPoint, instanceName).Inc(increment);
            }
        }

        lastValues.AddOrUpdate(key, currentValue, (k, v) => currentValue);
    }

    private Dictionary<string, double> ParseServerInfoForMetrics(IGrouping<string, KeyValuePair<string, string>>[] info)
    {
        var metrics = new Dictionary<string, double>();

        try
        {
            var memoryInfo = info.FirstOrDefault(g => g.Key == "Memory");
            var statsInfo = info.FirstOrDefault(g => g.Key == "Stats");
            var clientsInfo = info.FirstOrDefault(g => g.Key == "Clients");

            if (memoryInfo != null)
            {
                if (long.TryParse(memoryInfo.FirstOrDefault(kv => kv.Key == "used_memory").Value, out var usedMemory))
                    metrics["used_memory_bytes"] = usedMemory;
            }

            if (clientsInfo != null)
            {
                if (int.TryParse(clientsInfo.FirstOrDefault(kv => kv.Key == "connected_clients").Value, out var clients))
                    metrics["connected_clients"] = clients;
            }

            if (statsInfo != null)
            {
                if (long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "total_commands_processed").Value, out var commands))
                    metrics["total_commands_processed"] = commands;

                if (double.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "instantaneous_ops_per_sec").Value, out var ops))
                    metrics["ops_per_sec"] = ops;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse server info for metrics");
        }

        return metrics;
    }

    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        var endPoint = e.EndPoint?.ToString() ?? "unknown";
        _redisConnectionStatus.WithLabels(endPoint, _connectionMultiplexer.ClientName ?? "unknown").Set(0);
        _redisHealthCheckStatus.WithLabels(endPoint, "connection").Set(0);

        _logger.LogError(e.Exception, "Redis connection failed: {EndPoint}, Failure type: {FailureType}",
            e.EndPoint, e.FailureType);
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        var endPoint = e.EndPoint?.ToString() ?? "unknown";
        _redisConnectionStatus.WithLabels(endPoint, _connectionMultiplexer.ClientName ?? "unknown").Set(1);
        _redisHealthCheckStatus.WithLabels(endPoint, "connection").Set(2);

        _logger.LogInformation("Redis connection restored: {EndPoint}", e.EndPoint);
    }

    public override void Dispose()
    {
        _metricsTimer?.Dispose();

        if (_connectionMultiplexer != null)
        {
            _connectionMultiplexer.ConnectionFailed -= OnConnectionFailed;
            _connectionMultiplexer.ConnectionRestored -= OnConnectionRestored;
        }

        base.Dispose();
    }
}

/// <summary>
/// Redis Prometheus指标服务接口
/// </summary>
public interface IRedisPrometheusMetricsService
{
    Task<Dictionary<string, double>> GetCurrentMetricsAsync();
}

/// <summary>
/// Redis Prometheus配置选项
/// </summary>
public class RedisPrometheusOptions
{
    /// <summary>
    /// 指标收集间隔（秒）
    /// </summary>
    public int MetricsCollectionIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 健康检查间隔（秒）
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// 响应时间阈值（毫秒），超过此值将标记为降级状态
    /// </summary>
    public long ResponseTimeThresholdMs { get; set; } = 1000;

    /// <summary>
    /// 是否启用详细指标收集
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;

    /// <summary>
    /// 指标标签前缀
    /// </summary>
    public string MetricLabelPrefix { get; set; } = "maple_blog";
}