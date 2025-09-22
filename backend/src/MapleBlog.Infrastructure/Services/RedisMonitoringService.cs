using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Redis监控服务，提供连接状态监控和性能指标收集
/// </summary>
public class RedisMonitoringService : BackgroundService, IRedisMonitoringService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisMonitoringService> _logger;
    private readonly RedisMonitoringOptions _options;
    private readonly ConcurrentDictionary<string, RedisMetrics> _metrics;
    private readonly Timer _metricsTimer;

    public RedisMonitoringService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisMonitoringService> logger,
        IOptions<RedisMonitoringOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new RedisMonitoringOptions();
        _metrics = new ConcurrentDictionary<string, RedisMetrics>();

        // 设置定时收集指标
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(_options.MetricsCollectionIntervalSeconds));

        // 订阅连接事件
        _connectionMultiplexer.ConnectionFailed += OnConnectionFailed;
        _connectionMultiplexer.ConnectionRestored += OnConnectionRestored;
        _connectionMultiplexer.ErrorMessage += OnErrorMessage;
        _connectionMultiplexer.InternalError += OnInternalError;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis monitoring service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorConnectionHealth(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Redis monitoring");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Redis monitoring service stopped");
    }

    public RedisConnectionStatus GetConnectionStatus()
    {
        var endPoints = _connectionMultiplexer.GetEndPoints();
        var servers = endPoints.Select(ep => _connectionMultiplexer.GetServer(ep)).ToList();

        return new RedisConnectionStatus
        {
            IsConnected = _connectionMultiplexer.IsConnected,
            EndPoints = endPoints.Select(ep => ep.ToString()).ToList(),
            ConnectedServers = servers.Count(s => s.IsConnected),
            TotalServers = servers.Count,
            ClientName = _connectionMultiplexer.ClientName,
            // Note: LastHeartbeat and OperatingSystem properties may not be available in all Redis client versions
            // LastHeartbeat = _connectionMultiplexer.LastHeartbeat,
            // OperatingSystem = _connectionMultiplexer.OperatingSystem,
            Configuration = _connectionMultiplexer.Configuration,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public RedisMetrics GetMetrics(string serverEndpoint = "default")
    {
        return _metrics.GetValueOrDefault(serverEndpoint, new RedisMetrics());
    }

    public Dictionary<string, RedisMetrics> GetAllMetrics()
    {
        return new Dictionary<string, RedisMetrics>(_metrics);
    }

    private async Task MonitorConnectionHealth(CancellationToken cancellationToken)
    {
        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis connection is down");
                return;
            }

            var database = _connectionMultiplexer.GetDatabase();
            var stopwatch = Stopwatch.StartNew();

            await database.PingAsync();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > _options.ResponseTimeWarningThresholdMs)
            {
                _logger.LogWarning("Redis response time is high: {ResponseTime}ms", stopwatch.ElapsedMilliseconds);
            }

            _logger.LogDebug("Redis health check completed in {ResponseTime}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor Redis connection health");
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

                    var info = await server.InfoAsync();
                    var metrics = ParseServerInfo(info);
                    metrics.Timestamp = DateTimeOffset.UtcNow;
                    metrics.EndPoint = endPoint.ToString();

                    _metrics.AddOrUpdate(endPoint.ToString(), metrics, (key, oldValue) => metrics);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect metrics for endpoint {EndPoint}", endPoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect Redis metrics");
        }
    }

    private RedisMetrics ParseServerInfo(IGrouping<string, KeyValuePair<string, string>>[] info)
    {
        var metrics = new RedisMetrics();

        try
        {
            var serverInfo = info.FirstOrDefault(g => g.Key == "Server");
            var memoryInfo = info.FirstOrDefault(g => g.Key == "Memory");
            var statsInfo = info.FirstOrDefault(g => g.Key == "Stats");
            var clientsInfo = info.FirstOrDefault(g => g.Key == "Clients");

            if (serverInfo != null)
            {
                metrics.RedisVersion = serverInfo.FirstOrDefault(kv => kv.Key == "redis_version").Value;
                metrics.UptimeInSeconds = long.TryParse(serverInfo.FirstOrDefault(kv => kv.Key == "uptime_in_seconds").Value, out var uptime) ? uptime : 0;
            }

            if (memoryInfo != null)
            {
                metrics.UsedMemory = long.TryParse(memoryInfo.FirstOrDefault(kv => kv.Key == "used_memory").Value, out var memory) ? memory : 0;
                metrics.UsedMemoryRss = long.TryParse(memoryInfo.FirstOrDefault(kv => kv.Key == "used_memory_rss").Value, out var memoryRss) ? memoryRss : 0;
                metrics.UsedMemoryPeak = long.TryParse(memoryInfo.FirstOrDefault(kv => kv.Key == "used_memory_peak").Value, out var memoryPeak) ? memoryPeak : 0;
            }

            if (statsInfo != null)
            {
                metrics.TotalCommandsProcessed = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "total_commands_processed").Value, out var commands) ? commands : 0;
                metrics.TotalConnections = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "total_connections_received").Value, out var connections) ? connections : 0;
                metrics.RejectedConnections = long.TryParse(statsInfo.FirstOrDefault(kv => kv.Key == "rejected_connections").Value, out var rejected) ? rejected : 0;
            }

            if (clientsInfo != null)
            {
                metrics.ConnectedClients = int.TryParse(clientsInfo.FirstOrDefault(kv => kv.Key == "connected_clients").Value, out var clients) ? clients : 0;
                metrics.BlockedClients = int.TryParse(clientsInfo.FirstOrDefault(kv => kv.Key == "blocked_clients").Value, out var blocked) ? blocked : 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Redis server info");
        }

        return metrics;
    }

    private void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "Redis connection failed: {EndPoint}, Failure type: {FailureType}",
            e.EndPoint, e.FailureType);
    }

    private void OnConnectionRestored(object? sender, ConnectionFailedEventArgs e)
    {
        _logger.LogInformation("Redis connection restored: {EndPoint}", e.EndPoint);
    }

    private void OnErrorMessage(object? sender, RedisErrorEventArgs e)
    {
        _logger.LogError("Redis error: {Message} on {EndPoint}", e.Message, e.EndPoint);
    }

    private void OnInternalError(object? sender, InternalErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "Redis internal error: {Origin}", e.Origin);
    }

    public override void Dispose()
    {
        _metricsTimer?.Dispose();

        if (_connectionMultiplexer != null)
        {
            _connectionMultiplexer.ConnectionFailed -= OnConnectionFailed;
            _connectionMultiplexer.ConnectionRestored -= OnConnectionRestored;
            _connectionMultiplexer.ErrorMessage -= OnErrorMessage;
            _connectionMultiplexer.InternalError -= OnInternalError;
        }

        base.Dispose();
    }
}

/// <summary>
/// Redis监控服务接口
/// </summary>
public interface IRedisMonitoringService
{
    RedisConnectionStatus GetConnectionStatus();
    RedisMetrics GetMetrics(string serverEndpoint = "default");
    Dictionary<string, RedisMetrics> GetAllMetrics();
}

/// <summary>
/// Redis连接状态信息
/// </summary>
public class RedisConnectionStatus
{
    public bool IsConnected { get; set; }
    public List<string> EndPoints { get; set; } = new();
    public int ConnectedServers { get; set; }
    public int TotalServers { get; set; }
    public string? ClientName { get; set; }
    // Note: LastHeartbeat and OperatingSystem properties removed due to Redis client compatibility
    // public DateTime LastHeartbeat { get; set; }
    // public string? OperatingSystem { get; set; }
    public string? Configuration { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Redis性能指标
/// </summary>
public class RedisMetrics
{
    public string? EndPoint { get; set; }
    public string? RedisVersion { get; set; }
    public long UptimeInSeconds { get; set; }
    public long UsedMemory { get; set; }
    public long UsedMemoryRss { get; set; }
    public long UsedMemoryPeak { get; set; }
    public long TotalCommandsProcessed { get; set; }
    public long TotalConnections { get; set; }
    public long RejectedConnections { get; set; }
    public int ConnectedClients { get; set; }
    public int BlockedClients { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Redis监控配置选项
/// </summary>
public class RedisMonitoringOptions
{
    /// <summary>
    /// 健康检查间隔（秒）
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 指标收集间隔（秒）
    /// </summary>
    public int MetricsCollectionIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 响应时间警告阈值（毫秒）
    /// </summary>
    public long ResponseTimeWarningThresholdMs { get; set; } = 1000;
}