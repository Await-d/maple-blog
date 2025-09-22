using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Admin.DTOs;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 系统监控服务接口
/// </summary>
public interface ISystemMonitorService
{
    /// <summary>
    /// 获取完整的系统指标
    /// </summary>
    Task<SystemMetricsDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统性能指标
    /// </summary>
    Task<SystemPerformanceDto> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取数据库指标
    /// </summary>
    Task<DatabaseMetricsDto> GetDatabaseMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取缓存指标
    /// </summary>
    Task<CacheMetricsDto> GetCacheMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取应用程序指标
    /// </summary>
    Task<ApplicationMetricsDto> GetApplicationMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统警告
    /// </summary>
    Task<List<SystemAlertDto>> GetSystemAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查警告条件
    /// </summary>
    Task CheckAlertConditionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动监控
    /// </summary>
    Task StartMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止监控
    /// </summary>
    Task StopMonitoringAsync();
}

/// <summary>
/// 系统监控配置选项
/// </summary>
public class SystemMonitorOptions
{
    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// 缓存过期时间（分钟）
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// CPU使用率警告阈值
    /// </summary>
    public double CpuWarningThreshold { get; set; } = 80;

    /// <summary>
    /// CPU使用率错误阈值
    /// </summary>
    public double CpuErrorThreshold { get; set; } = 95;

    /// <summary>
    /// 内存使用率警告阈值
    /// </summary>
    public double MemoryWarningThreshold { get; set; } = 80;

    /// <summary>
    /// 内存使用率错误阈值
    /// </summary>
    public double MemoryErrorThreshold { get; set; } = 95;

    /// <summary>
    /// 磁盘使用率警告阈值
    /// </summary>
    public double DiskWarningThreshold { get; set; } = 80;

    /// <summary>
    /// 磁盘使用率错误阈值
    /// </summary>
    public double DiskErrorThreshold { get; set; } = 95;

    /// <summary>
    /// 数据库响应时间警告阈值（毫秒）
    /// </summary>
    public double DatabaseResponseWarningMs { get; set; } = 1000;

    /// <summary>
    /// 数据库响应时间错误阈值（毫秒）
    /// </summary>
    public double DatabaseResponseErrorMs { get; set; } = 5000;

    /// <summary>
    /// 是否启用自动警告
    /// </summary>
    public bool EnableAutoAlerting { get; set; } = true;

    /// <summary>
    /// 警告通知邮箱
    /// </summary>
    public List<string> AlertEmailRecipients { get; set; } = new();
}

/// <summary>
/// 系统监控服务实现
/// 负责收集和监控系统各种性能指标和健康状态
/// </summary>
public class SystemMonitorService : ISystemMonitorService, IHostedService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<SystemMonitorService> _logger;
    private readonly IOptions<SystemMonitorOptions> _options;
    private readonly IConnectionMultiplexer? _redis;

    private Timer? _monitoringTimer;
    private readonly ConcurrentDictionary<string, SystemAlertDto> _activeAlerts = new();
    private readonly ConcurrentQueue<RequestStatsDto> _requestStats = new();
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private readonly Process _currentProcess;

    private const string CACHE_KEY_PREFIX = "SystemMonitor:";
    private const string CACHE_KEY_METRICS = CACHE_KEY_PREFIX + "Metrics";
    private const string CACHE_KEY_ALERTS = CACHE_KEY_PREFIX + "Alerts";

    public SystemMonitorService(
        ApplicationDbContext context,
        IMemoryCache memoryCache,
        IHealthCheckService healthCheckService,
        ILogger<SystemMonitorService> logger,
        IOptions<SystemMonitorOptions> options,
        IDistributedCache? distributedCache = null,
        IConnectionMultiplexer? redis = null)
    {
        _context = context;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _healthCheckService = healthCheckService;
        _logger = logger;
        _options = options;
        _redis = redis;
        _currentProcess = Process.GetCurrentProcess();

        // 初始化性能计数器（仅在Windows上可用）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters");
            }
        }
    }

    /// <summary>
    /// 获取完整的系统指标
    /// </summary>
    public async Task<SystemMetricsDto> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试从缓存获取
            if (_memoryCache.TryGetValue(CACHE_KEY_METRICS, out SystemMetricsDto? cachedMetrics))
            {
                if (cachedMetrics != null && cachedMetrics.CollectedAt > DateTime.UtcNow.AddMinutes(-_options.Value.CacheExpirationMinutes))
                {
                    _logger.LogDebug("System metrics loaded from cache");
                    return cachedMetrics;
                }
            }

            var stopwatch = Stopwatch.StartNew();

            // 并行收集各种指标
            var tasks = new[]
            {
                Task.Run(async () => await _healthCheckService.GetSystemHealthAsync(cancellationToken), cancellationToken),
                Task.Run(async () => await GetPerformanceMetricsAsync(cancellationToken), cancellationToken),
                Task.Run(async () => await GetDatabaseMetricsAsync(cancellationToken), cancellationToken),
                Task.Run(async () => await GetCacheMetricsAsync(cancellationToken), cancellationToken),
                Task.Run(async () => await _healthCheckService.CheckExternalServicesAsync(cancellationToken), cancellationToken),
                Task.Run(async () => await GetApplicationMetricsAsync(cancellationToken), cancellationToken),
                Task.Run(async () => await GetSystemAlertsAsync(cancellationToken), cancellationToken)
            };

            var results = await Task.WhenAll(tasks);

            var metrics = new SystemMetricsDto
            {
                Health = results[0] as SystemHealthDto ?? new SystemHealthDto(),
                Performance = results[1] as SystemPerformanceDto ?? new SystemPerformanceDto(),
                Database = results[2] as DatabaseMetricsDto ?? new DatabaseMetricsDto(),
                Cache = results[3] as CacheMetricsDto ?? new CacheMetricsDto(),
                ExternalServices = results[4] as List<ExternalServiceStatusDto> ?? new List<ExternalServiceStatusDto>(),
                Application = results[5] as ApplicationMetricsDto ?? new ApplicationMetricsDto(),
                Alerts = results[6] as List<SystemAlertDto> ?? new List<SystemAlertDto>(),
                CollectedAt = DateTime.UtcNow
            };

            stopwatch.Stop();

            // 缓存结果
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_options.Value.CacheExpirationMinutes),
                Priority = CacheItemPriority.High
            };
            _memoryCache.Set(CACHE_KEY_METRICS, metrics, cacheOptions);

            _logger.LogDebug("System metrics collected in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
            throw;
        }
    }

    /// <summary>
    /// 获取系统性能指标
    /// </summary>
    public async Task<SystemPerformanceDto> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken); // 保持异步签名

            var performance = new SystemPerformanceDto();

            // CPU使用率
            if (_cpuCounter != null)
            {
                try
                {
                    performance.CpuUsagePercent = _cpuCounter.NextValue();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get CPU usage from performance counter");
                    // 使用进程级别的估算
                    performance.CpuUsagePercent = GetProcessCpuUsage();
                }
            }
            else
            {
                performance.CpuUsagePercent = GetProcessCpuUsage();
            }

            // 内存使用情况
            var totalMemory = GC.GetTotalMemory(false);
            performance.MemoryUsedBytes = _currentProcess.WorkingSet64;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                performance.MemoryTotalBytes = GetTotalPhysicalMemoryWindows();
            }
            else
            {
                performance.MemoryTotalBytes = GetTotalPhysicalMemoryLinux();
            }

            // 磁盘使用情况
            var diskInfo = GetDiskUsage();
            performance.DiskUsedBytes = diskInfo.Used;
            performance.DiskTotalBytes = diskInfo.Total;

            // 网络统计
            var networkStats = GetNetworkStatistics();
            performance.NetworkBytesReceived = networkStats.BytesReceived;
            performance.NetworkBytesSent = networkStats.BytesSent;

            // 垃圾回收信息
            performance.GarbageCollection = new GarbageCollectionDto
            {
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalMemory = totalMemory
            };

            // 线程池信息
            ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);

            performance.ThreadPool = new ThreadPoolDto
            {
                WorkerThreads = maxWorkerThreads - availableWorkerThreads,
                MaxWorkerThreads = maxWorkerThreads,
                CompletionPortThreads = maxCompletionPortThreads - availableCompletionPortThreads,
                MaxCompletionPortThreads = maxCompletionPortThreads,
                QueuedWorkItems = ThreadPool.ThreadCount
            };

            return performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
            return new SystemPerformanceDto();
        }
    }

    /// <summary>
    /// 获取数据库指标
    /// </summary>
    public async Task<DatabaseMetricsDto> GetDatabaseMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var metrics = new DatabaseMetricsDto();

            // 检查数据库连接健康状态
            var healthCheck = await _healthCheckService.CheckDatabaseHealthAsync(cancellationToken);
            metrics.ConnectionStatus = healthCheck.Status;
            metrics.ResponseTimeMs = healthCheck.ResponseTimeMs;

            if (healthCheck.Status != HealthStatus.Unhealthy)
            {
                // 获取连接池信息
                metrics.ConnectionPool = await GetConnectionPoolMetrics(cancellationToken);

                // 获取查询性能指标
                metrics.QueryPerformance = await GetQueryPerformanceMetrics(cancellationToken);

                // 获取数据库大小和表统计
                var (databaseSize, tableStats) = await GetDatabaseSizeAndTableStats(cancellationToken);
                metrics.DatabaseSizeBytes = databaseSize;
                metrics.TableStats = tableStats;
            }

            stopwatch.Stop();
            _logger.LogDebug("Database metrics collected in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting database metrics");
            return new DatabaseMetricsDto
            {
                ConnectionStatus = HealthStatus.Unhealthy,
                ResponseTimeMs = 0
            };
        }
    }

    /// <summary>
    /// 获取缓存指标
    /// </summary>
    public async Task<CacheMetricsDto> GetCacheMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = new CacheMetricsDto();

            // 内存缓存指标
            metrics.MemoryCache = GetMemoryCacheMetrics();

            // Redis缓存指标
            if (_redis != null)
            {
                metrics.Redis = await GetRedisCacheMetrics(cancellationToken);
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting cache metrics");
            return new CacheMetricsDto();
        }
    }

    /// <summary>
    /// 获取应用程序指标
    /// </summary>
    public async Task<ApplicationMetricsDto> GetApplicationMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken); // 保持异步签名

            var metrics = new ApplicationMetricsDto();

            // 从请求统计计算指标
            var recentStats = _requestStats.Where(s => s.Timestamp > DateTime.UtcNow.AddHours(-1)).ToList();

            if (recentStats.Any())
            {
                var totalRequests = recentStats.Sum(s => s.RequestCount);
                var totalErrors = recentStats.Sum(s => s.ErrorCount);
                var avgResponseTime = recentStats.Average(s => s.AverageResponseTimeMs);

                metrics.RequestsPerSecond = totalRequests / 3600.0; // 过去1小时的平均RPS
                metrics.AverageResponseTimeMs = avgResponseTime;
                metrics.ErrorRate = totalRequests > 0 ? (double)totalErrors / totalRequests * 100 : 0;
            }

            // 从其他来源获取指标
            metrics.ActiveUsers = await GetActiveUsersCount(cancellationToken);
            metrics.ActiveConnections = GetActiveConnectionsCount();
            metrics.ExceptionCount = GetExceptionCount();

            // 小时统计
            metrics.HourlyStats = GetHourlyRequestStats();

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting application metrics");
            return new ApplicationMetricsDto();
        }
    }

    /// <summary>
    /// 获取系统警告
    /// </summary>
    public async Task<List<SystemAlertDto>> GetSystemAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken); // 保持异步签名

            // 从内存中获取活跃警告
            var alerts = _activeAlerts.Values.OrderByDescending(a => a.TriggeredAt).ToList();

            _logger.LogDebug("Retrieved {AlertCount} active alerts", alerts.Count);

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system alerts");
            return new List<SystemAlertDto>();
        }
    }

    /// <summary>
    /// 检查警告条件
    /// </summary>
    public async Task CheckAlertConditionsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.EnableAutoAlerting)
            return;

        try
        {
            var metrics = await GetSystemMetricsAsync(cancellationToken);

            // 检查性能警告
            await CheckPerformanceAlerts(metrics.Performance);

            // 检查数据库警告
            await CheckDatabaseAlerts(metrics.Database);

            // 检查健康状态警告
            await CheckHealthAlerts(metrics.Health);

            // 清理过期警告
            CleanupExpiredAlerts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking alert conditions");
        }
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting system monitoring service");

        var interval = TimeSpan.FromSeconds(_options.Value.MonitoringIntervalSeconds);
        _monitoringTimer = new Timer(async _ =>
        {
            try
            {
                await CheckAlertConditionsAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitoring timer");
            }
        }, null, TimeSpan.Zero, interval);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        _logger.LogInformation("Stopping system monitoring service");

        _monitoringTimer?.Dispose();
        _monitoringTimer = null;

        await Task.CompletedTask;
    }

    #region IHostedService Implementation

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartMonitoringAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopMonitoringAsync();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 获取进程CPU使用率
    /// </summary>
    private double GetProcessCpuUsage()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = _currentProcess.TotalProcessorTime;
            Thread.Sleep(100);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = _currentProcess.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return cpuUsageTotal * 100;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取Windows系统总物理内存
    /// </summary>
    private long GetTotalPhysicalMemoryWindows()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            using var results = searcher.Get();
            foreach (ManagementObject result in results)
            {
                return Convert.ToInt64(result["TotalPhysicalMemory"]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get total physical memory on Windows");
        }
        return 0;
    }

    /// <summary>
    /// 获取Linux系统总物理内存
    /// </summary>
    private long GetTotalPhysicalMemoryLinux()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            var memTotalLine = lines.FirstOrDefault(line => line.StartsWith("MemTotal:"));
            if (memTotalLine != null)
            {
                var parts = memTotalLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && long.TryParse(parts[1], out long memKb))
                {
                    return memKb * 1024; // Convert from KB to bytes
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get total physical memory on Linux");
        }
        return 0;
    }

    /// <summary>
    /// 获取磁盘使用情况
    /// </summary>
    private (long Used, long Total) GetDiskUsage()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            var totalSize = drives.Sum(d => d.TotalSize);
            var totalUsed = drives.Sum(d => d.TotalSize - d.AvailableFreeSpace);
            return (totalUsed, totalSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get disk usage");
            return (0, 0);
        }
    }

    /// <summary>
    /// 获取网络统计
    /// </summary>
    private (long BytesReceived, long BytesSent) GetNetworkStatistics()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            long totalBytesReceived = 0;
            long totalBytesSent = 0;

            foreach (var ni in networkInterfaces)
            {
                var stats = ni.GetIPStatistics();
                totalBytesReceived += stats.BytesReceived;
                totalBytesSent += stats.BytesSent;
            }

            return (totalBytesReceived, totalBytesSent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get network statistics");
            return (0, 0);
        }
    }

    /// <summary>
    /// 获取连接池指标
    /// </summary>
    private async Task<ConnectionPoolDto> GetConnectionPoolMetrics(CancellationToken cancellationToken)
    {
        try
        {
            // 这里需要根据具体的数据库提供商实现
            // 由于SQLite不支持连接池概念，我们提供一个基本实现
            await Task.Delay(1, cancellationToken);

            return new ConnectionPoolDto
            {
                ActiveConnections = 1, // SQLite通常只有一个连接
                IdleConnections = 0,
                MaxConnections = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get connection pool metrics");
            return new ConnectionPoolDto();
        }
    }

    /// <summary>
    /// 获取查询性能指标
    /// </summary>
    private async Task<QueryPerformanceDto> GetQueryPerformanceMetrics(CancellationToken cancellationToken)
    {
        try
        {
            // 执行简单查询测试性能
            var stopwatch = Stopwatch.StartNew();
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            stopwatch.Stop();

            return new QueryPerformanceDto
            {
                AverageQueryTimeMs = stopwatch.ElapsedMilliseconds,
                SlowestQueryTimeMs = stopwatch.ElapsedMilliseconds,
                TotalQueries = 1,
                FailedQueries = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get query performance metrics");
            return new QueryPerformanceDto
            {
                TotalQueries = 1,
                FailedQueries = 1
            };
        }
    }

    /// <summary>
    /// 获取数据库大小和表统计
    /// </summary>
    private async Task<(long DatabaseSize, List<TableStatsDto> TableStats)> GetDatabaseSizeAndTableStats(CancellationToken cancellationToken)
    {
        try
        {
            var tableStats = new List<TableStatsDto>();
            long databaseSize = 0;

            // 对于SQLite，获取文件大小
            var connectionString = _context.Database.GetConnectionString();
            if (!string.IsNullOrEmpty(connectionString))
            {
                var dbPath = ExtractDatabasePath(connectionString);
                if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                {
                    databaseSize = new FileInfo(dbPath).Length;
                }
            }

            // 获取表统计（简化实现）
            var postCount = await _context.Set<MapleBlog.Domain.Entities.Post>().CountAsync(cancellationToken);
            var userCount = await _context.Set<MapleBlog.Domain.Entities.User>().CountAsync(cancellationToken);

            tableStats.Add(new TableStatsDto
            {
                TableName = "Posts",
                RecordCount = postCount,
                TableSizeBytes = postCount * 1024 // 估算
            });

            tableStats.Add(new TableStatsDto
            {
                TableName = "Users",
                RecordCount = userCount,
                TableSizeBytes = userCount * 512 // 估算
            });

            return (databaseSize, tableStats);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get database size and table stats");
            return (0, new List<TableStatsDto>());
        }
    }

    /// <summary>
    /// 从连接字符串提取数据库路径
    /// </summary>
    private string? ExtractDatabasePath(string connectionString)
    {
        try
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Trim().Substring("Data Source=".Length);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract database path from connection string");
        }
        return null;
    }

    /// <summary>
    /// 获取内存缓存指标
    /// </summary>
    private MemoryCacheDto GetMemoryCacheMetrics()
    {
        try
        {
            // .NET的MemoryCache没有公开这些统计信息，我们提供估算值
            return new MemoryCacheDto
            {
                ItemCount = 0, // 无法直接获取
                EstimatedMemoryUsage = 0, // 无法直接获取
                HitCount = 0, // 需要自定义实现
                MissCount = 0 // 需要自定义实现
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory cache metrics");
            return new MemoryCacheDto();
        }
    }

    /// <summary>
    /// 获取Redis缓存指标
    /// </summary>
    private async Task<RedisCacheDto> GetRedisCacheMetrics(CancellationToken cancellationToken)
    {
        try
        {
            if (_redis == null)
                return new RedisCacheDto { ConnectionStatus = HealthStatus.Unknown };

            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            var stopwatch = Stopwatch.StartNew();
            await database.PingAsync();
            stopwatch.Stop();

            var info = await server.InfoAsync();
            var infoDict = info.ToDictionary(x => x.Key, x => x.Value);

            return new RedisCacheDto
            {
                ConnectionStatus = HealthStatus.Healthy,
                UsedMemoryBytes = ParseLong(infoDict.GetValueOrDefault("used_memory", "0")),
                MaxMemoryBytes = ParseLong(infoDict.GetValueOrDefault("maxmemory", "0")),
                HitRatio = CalculateRedisHitRatio(infoDict),
                TotalKeys = ParseLong(infoDict.GetValueOrDefault("db0", "keys=0").Split(',')[0].Split('=')[1]),
                ExpiredKeys = ParseLong(infoDict.GetValueOrDefault("expired_keys", "0")),
                EvictedKeys = ParseLong(infoDict.GetValueOrDefault("evicted_keys", "0")),
                ConnectedClients = int.Parse(infoDict.GetValueOrDefault("connected_clients", "0")),
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Redis cache metrics");
            return new RedisCacheDto
            {
                ConnectionStatus = HealthStatus.Unhealthy
            };
        }
    }

    /// <summary>
    /// 解析长整型值
    /// </summary>
    private long ParseLong(string value)
    {
        return long.TryParse(value, out long result) ? result : 0;
    }

    /// <summary>
    /// 计算Redis命中率
    /// </summary>
    private double CalculateRedisHitRatio(Dictionary<string, string> info)
    {
        try
        {
            var hits = ParseLong(info.GetValueOrDefault("keyspace_hits", "0"));
            var misses = ParseLong(info.GetValueOrDefault("keyspace_misses", "0"));
            var total = hits + misses;
            return total > 0 ? (double)hits / total * 100 : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取活跃用户数
    /// </summary>
    private async Task<int> GetActiveUsersCount(CancellationToken cancellationToken)
    {
        try
        {
            // 获取过去30分钟内活跃的用户
            var thirtyMinutesAgo = DateTime.UtcNow.AddMinutes(-30);
            return await _context.Set<MapleBlog.Domain.Entities.User>()
                .Where(u => u.LastLoginDate > thirtyMinutesAgo)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active users count");
            return 0;
        }
    }

    /// <summary>
    /// 获取活跃连接数
    /// </summary>
    private int GetActiveConnectionsCount()
    {
        try
        {
            // 这里需要根据具体的连接跟踪实现
            return _currentProcess.Threads.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active connections count");
            return 0;
        }
    }

    /// <summary>
    /// 获取异常数量
    /// </summary>
    private long GetExceptionCount()
    {
        try
        {
            // 这里需要根据具体的异常跟踪实现
            return 0; // 需要自定义异常计数器
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取小时请求统计
    /// </summary>
    private List<RequestStatsDto> GetHourlyRequestStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var hourlyStats = new List<RequestStatsDto>();

            for (int i = 23; i >= 0; i--)
            {
                var hourStart = now.AddHours(-i).Date.AddHours(now.AddHours(-i).Hour);
                var hourEnd = hourStart.AddHours(1);

                var statsForHour = _requestStats
                    .Where(s => s.Timestamp >= hourStart && s.Timestamp < hourEnd)
                    .ToList();

                hourlyStats.Add(new RequestStatsDto
                {
                    Timestamp = hourStart,
                    RequestCount = statsForHour.Sum(s => s.RequestCount),
                    ErrorCount = statsForHour.Sum(s => s.ErrorCount),
                    AverageResponseTimeMs = statsForHour.Any() ? statsForHour.Average(s => s.AverageResponseTimeMs) : 0
                });
            }

            return hourlyStats;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get hourly request stats");
            return new List<RequestStatsDto>();
        }
    }

    /// <summary>
    /// 检查性能警告
    /// </summary>
    private async Task CheckPerformanceAlerts(SystemPerformanceDto performance)
    {
        await Task.Delay(1); // 保持异步签名

        // CPU使用率警告
        if (performance.CpuUsagePercent > _options.Value.CpuErrorThreshold)
        {
            CreateAlert("HIGH_CPU_USAGE", AlertLevel.Critical, AlertType.Performance,
                "CPU使用率过高", $"CPU使用率达到 {performance.CpuUsagePercent:F1}%",
                new Dictionary<string, object> { ["CpuUsage"] = performance.CpuUsagePercent });
        }
        else if (performance.CpuUsagePercent > _options.Value.CpuWarningThreshold)
        {
            CreateAlert("HIGH_CPU_USAGE", AlertLevel.Warning, AlertType.Performance,
                "CPU使用率较高", $"CPU使用率达到 {performance.CpuUsagePercent:F1}%",
                new Dictionary<string, object> { ["CpuUsage"] = performance.CpuUsagePercent });
        }

        // 内存使用率警告
        if (performance.MemoryUsagePercent > _options.Value.MemoryErrorThreshold)
        {
            CreateAlert("HIGH_MEMORY_USAGE", AlertLevel.Critical, AlertType.Resource,
                "内存使用率过高", $"内存使用率达到 {performance.MemoryUsagePercent:F1}%",
                new Dictionary<string, object> { ["MemoryUsage"] = performance.MemoryUsagePercent });
        }
        else if (performance.MemoryUsagePercent > _options.Value.MemoryWarningThreshold)
        {
            CreateAlert("HIGH_MEMORY_USAGE", AlertLevel.Warning, AlertType.Resource,
                "内存使用率较高", $"内存使用率达到 {performance.MemoryUsagePercent:F1}%",
                new Dictionary<string, object> { ["MemoryUsage"] = performance.MemoryUsagePercent });
        }

        // 磁盘使用率警告
        if (performance.DiskUsagePercent > _options.Value.DiskErrorThreshold)
        {
            CreateAlert("HIGH_DISK_USAGE", AlertLevel.Critical, AlertType.Resource,
                "磁盘使用率过高", $"磁盘使用率达到 {performance.DiskUsagePercent:F1}%",
                new Dictionary<string, object> { ["DiskUsage"] = performance.DiskUsagePercent });
        }
        else if (performance.DiskUsagePercent > _options.Value.DiskWarningThreshold)
        {
            CreateAlert("HIGH_DISK_USAGE", AlertLevel.Warning, AlertType.Resource,
                "磁盘使用率较高", $"磁盘使用率达到 {performance.DiskUsagePercent:F1}%",
                new Dictionary<string, object> { ["DiskUsage"] = performance.DiskUsagePercent });
        }
    }

    /// <summary>
    /// 检查数据库警告
    /// </summary>
    private async Task CheckDatabaseAlerts(DatabaseMetricsDto database)
    {
        await Task.Delay(1); // 保持异步签名

        if (database.ConnectionStatus == HealthStatus.Unhealthy)
        {
            CreateAlert("DATABASE_CONNECTION_FAILED", AlertLevel.Critical, AlertType.Connection,
                "数据库连接失败", "无法连接到数据库",
                new Dictionary<string, object> { ["ConnectionStatus"] = database.ConnectionStatus.ToString() });
        }
        else if (database.ResponseTimeMs > _options.Value.DatabaseResponseErrorMs)
        {
            CreateAlert("DATABASE_SLOW_RESPONSE", AlertLevel.Error, AlertType.Performance,
                "数据库响应过慢", $"数据库响应时间 {database.ResponseTimeMs:F0}ms",
                new Dictionary<string, object> { ["ResponseTime"] = database.ResponseTimeMs });
        }
        else if (database.ResponseTimeMs > _options.Value.DatabaseResponseWarningMs)
        {
            CreateAlert("DATABASE_SLOW_RESPONSE", AlertLevel.Warning, AlertType.Performance,
                "数据库响应较慢", $"数据库响应时间 {database.ResponseTimeMs:F0}ms",
                new Dictionary<string, object> { ["ResponseTime"] = database.ResponseTimeMs });
        }
    }

    /// <summary>
    /// 检查健康状态警告
    /// </summary>
    private async Task CheckHealthAlerts(SystemHealthDto health)
    {
        await Task.Delay(1); // 保持异步签名

        if (health.OverallStatus == HealthStatus.Unhealthy)
        {
            CreateAlert("SYSTEM_UNHEALTHY", AlertLevel.Critical, AlertType.System,
                "系统状态不健康", "系统整体健康状态为不健康",
                new Dictionary<string, object> { ["OverallStatus"] = health.OverallStatus.ToString() });
        }
        else if (health.OverallStatus == HealthStatus.Degraded)
        {
            CreateAlert("SYSTEM_DEGRADED", AlertLevel.Warning, AlertType.System,
                "系统状态降级", "系统整体健康状态为降级",
                new Dictionary<string, object> { ["OverallStatus"] = health.OverallStatus.ToString() });
        }
    }

    /// <summary>
    /// 创建警告
    /// </summary>
    private void CreateAlert(string alertId, AlertLevel level, AlertType type, string title, string description, Dictionary<string, object> metricValues)
    {
        var alert = new SystemAlertDto
        {
            AlertId = alertId,
            Level = level,
            Type = type,
            Title = title,
            Description = description,
            Source = "SystemMonitor",
            TriggeredAt = DateTime.UtcNow,
            IsAcknowledged = false,
            MetricValues = metricValues
        };

        _activeAlerts.AddOrUpdate(alertId, alert, (key, existingAlert) =>
        {
            // 更新现有警告
            existingAlert.TriggeredAt = DateTime.UtcNow;
            existingAlert.MetricValues = metricValues;
            return existingAlert;
        });

        _logger.LogWarning("Alert triggered: {AlertId} - {Title}", alertId, title);
    }

    /// <summary>
    /// 清理过期警告
    /// </summary>
    private void CleanupExpiredAlerts()
    {
        var expiredThreshold = DateTime.UtcNow.AddHours(-24); // 24小时前的警告过期

        var expiredAlerts = _activeAlerts.Where(kvp => kvp.Value.TriggeredAt < expiredThreshold).ToList();

        foreach (var expiredAlert in expiredAlerts)
        {
            _activeAlerts.TryRemove(expiredAlert.Key, out _);
            _logger.LogDebug("Expired alert removed: {AlertId}", expiredAlert.Key);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        _currentProcess?.Dispose();
    }

    #endregion
}