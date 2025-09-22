using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Admin数据库资源监控服务
/// 监控数据库空间使用、资源消耗和系统性能
/// </summary>
public class AdminDatabaseResourceMonitor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminDatabaseResourceMonitor> _logger;
    private readonly AdminDatabaseResourceOptions _options;
    private readonly ConcurrentQueue<ResourceSnapshot> _resourceHistory;
    private volatile ResourceSnapshot _currentSnapshot;

    public AdminDatabaseResourceMonitor(
        IServiceProvider serviceProvider,
        ILogger<AdminDatabaseResourceMonitor> logger,
        IOptions<AdminDatabaseResourceOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _resourceHistory = new ConcurrentQueue<ResourceSnapshot>();
        _currentSnapshot = new ResourceSnapshot();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Admin Database Resource Monitor started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await MonitorDatabaseResources();
                await CheckResourceAlerts();
                await CleanupOldSnapshots();
                await Task.Delay(TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Admin Database Resource Monitor is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Database Resource Monitor");
        }
    }

    private async Task MonitorDatabaseResources()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var snapshot = new ResourceSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                DatabaseSpace = await CollectDatabaseSpaceInfo(context),
                SystemResources = await CollectSystemResourceInfo(),
                TableStatistics = await CollectTableStatistics(context),
                IndexStatistics = await CollectIndexStatistics(context),
                TransactionLog = await CollectTransactionLogInfo(context),
                PerformanceCounters = await CollectPerformanceCounters(context)
            };

            _currentSnapshot = snapshot;
            _resourceHistory.Enqueue(snapshot);

            _logger.LogDebug("Database resource monitoring completed at {Timestamp}", snapshot.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor database resources");
        }
    }

    private async Task<DatabaseSpaceInfo> CollectDatabaseSpaceInfo(ApplicationDbContext context)
    {
        var spaceInfo = new DatabaseSpaceInfo();

        try
        {
            var connection = context.Database.GetDbConnection();
            spaceInfo.ProviderName = context.Database.ProviderName ?? "Unknown";

            if (context.Database.ProviderName?.Contains("Sqlite") == true)
            {
                await CollectSqliteSpaceInfo(connection, spaceInfo);
            }
            else if (context.Database.ProviderName?.Contains("SqlServer") == true)
            {
                await CollectSqlServerSpaceInfo(context, spaceInfo);
            }
            else if (context.Database.ProviderName?.Contains("Npgsql") == true)
            {
                await CollectPostgreSqlSpaceInfo(context, spaceInfo);
            }
            else
            {
                spaceInfo.Notes = "Space monitoring not implemented for this provider";
            }
        }
        catch (Exception ex)
        {
            spaceInfo.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Failed to collect database space information");
        }

        return spaceInfo;
    }

    private async Task CollectSqliteSpaceInfo(System.Data.Common.DbConnection connection, DatabaseSpaceInfo spaceInfo)
    {
        try
        {
            var connectionString = connection.ConnectionString;
            var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)");

            if (dataSourceMatch.Success)
            {
                var dbPath = dataSourceMatch.Groups[1].Value;
                var resolvedPath = Path.GetFullPath(dbPath);

                if (File.Exists(resolvedPath))
                {
                    var fileInfo = new FileInfo(resolvedPath);
                    var directoryInfo = fileInfo.Directory;

                    spaceInfo.DatabaseFilePath = resolvedPath;
                    spaceInfo.DatabaseSizeBytes = fileInfo.Length;
                    spaceInfo.DatabaseSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                    spaceInfo.LastModified = fileInfo.LastWriteTime;

                    // 获取磁盘空间信息
                    if (directoryInfo != null)
                    {
                        var driveInfo = new DriveInfo(directoryInfo.Root.FullName);
                        spaceInfo.TotalDiskSpaceBytes = driveInfo.TotalSize;
                        spaceInfo.FreeDiskSpaceBytes = driveInfo.AvailableFreeSpace;
                        spaceInfo.UsedDiskSpaceBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
                        spaceInfo.DiskUsagePercentage = Math.Round((double)spaceInfo.UsedDiskSpaceBytes / driveInfo.TotalSize * 100, 2);
                    }

                    // SQLite特定信息
                    spaceInfo.SupportsTruncate = false;
                    spaceInfo.SupportsCompression = false;
                    spaceInfo.GrowthSettings = "Auto-grow (SQLite)";

                    // 检查是否有WAL文件
                    var walFile = resolvedPath + "-wal";
                    if (File.Exists(walFile))
                    {
                        var walInfo = new FileInfo(walFile);
                        spaceInfo.TransactionLogSizeBytes = walInfo.Length;
                        spaceInfo.TransactionLogSizeMB = Math.Round(walInfo.Length / (1024.0 * 1024.0), 2);
                    }

                    // 检查是否有SHM文件
                    var shmFile = resolvedPath + "-shm";
                    if (File.Exists(shmFile))
                    {
                        var shmInfo = new FileInfo(shmFile);
                        spaceInfo.SharedMemorySizeBytes = shmInfo.Length;
                    }
                }
                else
                {
                    spaceInfo.ErrorMessage = "Database file not found";
                }
            }
            else
            {
                spaceInfo.ErrorMessage = "Could not parse database path from connection string";
            }
        }
        catch (Exception ex)
        {
            spaceInfo.ErrorMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task CollectSqlServerSpaceInfo(ApplicationDbContext context, DatabaseSpaceInfo spaceInfo)
    {
        try
        {
            // SQL Server特定的空间查询
            // 注意：这需要适当的SQL Server权限
            var query = @"
                SELECT
                    DB_NAME() as DatabaseName,
                    SUM(size) * 8 / 1024 as DatabaseSizeMB,
                    SUM(CASE WHEN type = 0 THEN size END) * 8 / 1024 as DataSizeMB,
                    SUM(CASE WHEN type = 1 THEN size END) * 8 / 1024 as LogSizeMB
                FROM sys.database_files";

            // 在实际实现中，您需要执行这个查询
            spaceInfo.Notes = "SQL Server space monitoring requires specific implementation";
        }
        catch (Exception ex)
        {
            spaceInfo.ErrorMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task CollectPostgreSqlSpaceInfo(ApplicationDbContext context, DatabaseSpaceInfo spaceInfo)
    {
        try
        {
            // PostgreSQL特定的空间查询
            var query = @"
                SELECT
                    pg_database_size(current_database()) as database_size,
                    pg_size_pretty(pg_database_size(current_database())) as database_size_pretty";

            // 在实际实现中，您需要执行这个查询
            spaceInfo.Notes = "PostgreSQL space monitoring requires specific implementation";
        }
        catch (Exception ex)
        {
            spaceInfo.ErrorMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task<SystemResourceInfo> CollectSystemResourceInfo()
    {
        var resourceInfo = new SystemResourceInfo();

        try
        {
            var process = Process.GetCurrentProcess();

            // 内存使用情况
            resourceInfo.ProcessMemoryBytes = process.WorkingSet64;
            resourceInfo.ProcessMemoryMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);
            resourceInfo.PrivateMemoryBytes = process.PrivateMemorySize64;
            resourceInfo.PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / (1024.0 * 1024.0), 2);

            // CPU使用情况
            resourceInfo.ProcessCpuTime = process.TotalProcessorTime;
            resourceInfo.ProcessCpuTimeMs = process.TotalProcessorTime.TotalMilliseconds;

            // 线程和句柄
            resourceInfo.ThreadCount = process.Threads.Count;
            resourceInfo.HandleCount = process.HandleCount;

            // GC信息
            resourceInfo.GCTotalMemory = GC.GetTotalMemory(false);
            resourceInfo.GCGen0Collections = GC.CollectionCount(0);
            resourceInfo.GCGen1Collections = GC.CollectionCount(1);
            resourceInfo.GCGen2Collections = GC.CollectionCount(2);

            // 系统总体资源（如果可获得）
            if (OperatingSystem.IsWindows())
            {
                await CollectWindowsSystemInfo(resourceInfo);
            }
            else if (OperatingSystem.IsLinux())
            {
                await CollectLinuxSystemInfo(resourceInfo);
            }
        }
        catch (Exception ex)
        {
            resourceInfo.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Failed to collect system resource information");
        }

        return resourceInfo;
    }

    private async Task CollectWindowsSystemInfo(SystemResourceInfo resourceInfo)
    {
        try
        {
            // Windows特定的系统信息收集
            // 这里可以使用性能计数器等Windows API
            resourceInfo.OperatingSystem = "Windows";
            resourceInfo.Notes = "Windows system monitoring not fully implemented";
        }
        catch (Exception ex)
        {
            resourceInfo.ErrorMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task CollectLinuxSystemInfo(SystemResourceInfo resourceInfo)
    {
        try
        {
            // Linux特定的系统信息收集
            resourceInfo.OperatingSystem = "Linux";

            // 读取内存信息
            if (File.Exists("/proc/meminfo"))
            {
                var memInfo = await File.ReadAllTextAsync("/proc/meminfo");
                var totalMemMatch = System.Text.RegularExpressions.Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
                var freeMemMatch = System.Text.RegularExpressions.Regex.Match(memInfo, @"MemAvailable:\s+(\d+)\s+kB");

                if (totalMemMatch.Success && long.TryParse(totalMemMatch.Groups[1].Value, out var totalMem))
                {
                    resourceInfo.SystemTotalMemoryBytes = totalMem * 1024;
                    resourceInfo.SystemTotalMemoryMB = Math.Round(totalMem / 1024.0, 2);
                }

                if (freeMemMatch.Success && long.TryParse(freeMemMatch.Groups[1].Value, out var freeMem))
                {
                    resourceInfo.SystemFreeMemoryBytes = freeMem * 1024;
                    resourceInfo.SystemFreeMemoryMB = Math.Round(freeMem / 1024.0, 2);
                }
            }

            // 读取CPU信息
            if (File.Exists("/proc/loadavg"))
            {
                var loadAvg = await File.ReadAllTextAsync("/proc/loadavg");
                var parts = loadAvg.Split(' ');
                if (parts.Length >= 3)
                {
                    if (double.TryParse(parts[0], out var load1))
                        resourceInfo.SystemLoad1Min = load1;
                    if (double.TryParse(parts[1], out var load5))
                        resourceInfo.SystemLoad5Min = load5;
                    if (double.TryParse(parts[2], out var load15))
                        resourceInfo.SystemLoad15Min = load15;
                }
            }
        }
        catch (Exception ex)
        {
            resourceInfo.ErrorMessage = ex.Message;
        }
    }

    private async Task<List<TableStatistic>> CollectTableStatistics(ApplicationDbContext context)
    {
        var statistics = new List<TableStatistic>();

        try
        {
            // 主要表的统计信息
            var tables = new[]
            {
                new { Name = "Users", EntitySet = context.Users },
                new { Name = "Posts", EntitySet = context.Posts },
                new { Name = "Categories", EntitySet = context.Categories },
                new { Name = "Tags", EntitySet = context.Tags },
                new { Name = "Comments", EntitySet = context.Comments }
            };

            foreach (var table in tables)
            {
                var statistic = new TableStatistic
                {
                    TableName = table.Name
                };

                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var count = await table.EntitySet.CountAsync();
                    stopwatch.Stop();

                    statistic.RecordCount = count;
                    statistic.CountQueryTime = stopwatch.ElapsedMilliseconds;

                    // 对于SQLite，尝试获取更多表信息
                    if (context.Database.ProviderName?.Contains("Sqlite") == true)
                    {
                        // 这里可以执行SQLite特定的查询来获取表大小等信息
                        // 例如：PRAGMA table_info(table_name)
                    }

                    statistic.LastUpdated = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    statistic.ErrorMessage = ex.Message;
                }

                statistics.Add(statistic);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect table statistics");
        }

        return statistics;
    }

    private async Task<List<IndexStatistic>> CollectIndexStatistics(ApplicationDbContext context)
    {
        var statistics = new List<IndexStatistic>();

        try
        {
            // 对于SQLite，可以查询索引信息
            if (context.Database.ProviderName?.Contains("Sqlite") == true)
            {
                // 执行SQLite特定的索引查询
                // 例如：SELECT * FROM sqlite_master WHERE type = 'index'
                statistics.Add(new IndexStatistic
                {
                    IndexName = "SQLite Indexes",
                    TableName = "Various",
                    Notes = "SQLite index monitoring requires specific implementation"
                });
            }
            else
            {
                statistics.Add(new IndexStatistic
                {
                    IndexName = "Provider Indexes",
                    TableName = "Various",
                    Notes = $"Index monitoring not implemented for {context.Database.ProviderName}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect index statistics");
        }

        await Task.CompletedTask;
        return statistics;
    }

    private async Task<TransactionLogInfo> CollectTransactionLogInfo(ApplicationDbContext context)
    {
        var logInfo = new TransactionLogInfo();

        try
        {
            if (context.Database.ProviderName?.Contains("Sqlite") == true)
            {
                // SQLite使用WAL（Write-Ahead Logging）
                var connection = context.Database.GetDbConnection();
                var connectionString = connection.ConnectionString;
                var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)");

                if (dataSourceMatch.Success)
                {
                    var dbPath = dataSourceMatch.Groups[1].Value;
                    var walFile = dbPath + "-wal";

                    if (File.Exists(walFile))
                    {
                        var walInfo = new FileInfo(walFile);
                        logInfo.LogSizeBytes = walInfo.Length;
                        logInfo.LogSizeMB = Math.Round(walInfo.Length / (1024.0 * 1024.0), 2);
                        logInfo.LogType = "WAL (Write-Ahead Log)";
                        logInfo.LastModified = walInfo.LastWriteTime;
                    }
                    else
                    {
                        logInfo.LogType = "WAL (Write-Ahead Log)";
                        logInfo.Notes = "WAL file not found or not in WAL mode";
                    }
                }
            }
            else
            {
                logInfo.Notes = $"Transaction log monitoring not implemented for {context.Database.ProviderName}";
            }
        }
        catch (Exception ex)
        {
            logInfo.ErrorMessage = ex.Message;
        }

        await Task.CompletedTask;
        return logInfo;
    }

    private async Task<PerformanceCounters> CollectPerformanceCounters(ApplicationDbContext context)
    {
        var counters = new PerformanceCounters();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // 简单查询性能测试
            var userCount = await context.Users.CountAsync();
            var simpleQueryTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // 复杂查询性能测试
            var complexQuery = await context.Posts
                .Include(p => p.Category)
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddDays(-30))
                .Take(5)
                .ToListAsync();
            var complexQueryTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Stop();

            counters.SimpleQueryTimeMs = simpleQueryTime;
            counters.ComplexQueryTimeMs = complexQueryTime;
            counters.QueryResultCount = userCount;
            counters.LastMeasured = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            counters.ErrorMessage = ex.Message;
        }

        return counters;
    }

    private async Task CheckResourceAlerts()
    {
        var snapshot = _currentSnapshot;
        var alerts = new List<string>();

        try
        {
            // 检查数据库大小
            if (snapshot.DatabaseSpace.DatabaseSizeBytes > _options.DatabaseSizeAlertBytes)
            {
                alerts.Add($"Database size ({snapshot.DatabaseSpace.DatabaseSizeMB} MB) exceeds threshold");
            }

            // 检查磁盘空间
            if (snapshot.DatabaseSpace.DiskUsagePercentage > _options.DiskUsageAlertPercentage)
            {
                alerts.Add($"Disk usage ({snapshot.DatabaseSpace.DiskUsagePercentage}%) exceeds threshold");
            }

            // 检查内存使用
            if (snapshot.SystemResources.ProcessMemoryMB > _options.MemoryUsageAlertMB)
            {
                alerts.Add($"Process memory usage ({snapshot.SystemResources.ProcessMemoryMB} MB) exceeds threshold");
            }

            // 检查查询性能
            if (snapshot.PerformanceCounters.SimpleQueryTimeMs > _options.QueryPerformanceAlertMs)
            {
                alerts.Add($"Query performance ({snapshot.PerformanceCounters.SimpleQueryTimeMs} ms) exceeds threshold");
            }

            if (alerts.Any())
            {
                _logger.LogWarning("Database resource alerts: {Alerts}", string.Join("; ", alerts));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check resource alerts");
        }

        await Task.CompletedTask;
    }

    private async Task CleanupOldSnapshots()
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-_options.SnapshotRetentionHours);
        var removed = 0;

        while (_resourceHistory.TryPeek(out var oldestSnapshot) && oldestSnapshot.Timestamp < cutoffTime)
        {
            if (_resourceHistory.TryDequeue(out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Cleaned up {RemovedCount} old resource snapshots", removed);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取当前资源快照
    /// </summary>
    public ResourceSnapshot GetCurrentSnapshot()
    {
        return _currentSnapshot;
    }

    /// <summary>
    /// 获取历史资源数据
    /// </summary>
    public IEnumerable<ResourceSnapshot> GetHistoricalSnapshots(TimeSpan timeRange)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(timeRange);
        return _resourceHistory.Where(s => s.Timestamp >= cutoffTime).OrderBy(s => s.Timestamp);
    }

    /// <summary>
    /// 获取资源使用趋势
    /// </summary>
    public ResourceUsageTrend GetResourceTrend(TimeSpan timeRange)
    {
        var snapshots = GetHistoricalSnapshots(timeRange).ToList();

        if (!snapshots.Any())
        {
            return new ResourceUsageTrend();
        }

        var databaseSizes = snapshots.Select(s => s.DatabaseSpace.DatabaseSizeMB).ToList();
        var memoryUsages = snapshots.Select(s => s.SystemResources.ProcessMemoryMB).ToList();

        return new ResourceUsageTrend
        {
            TimeRange = timeRange,
            DataPoints = snapshots.Count,
            DatabaseSizeGrowth = databaseSizes.Last() - databaseSizes.First(),
            AverageDatabaseSize = databaseSizes.Average(),
            MaxDatabaseSize = databaseSizes.Max(),
            AverageMemoryUsage = memoryUsages.Average(),
            MaxMemoryUsage = memoryUsages.Max(),
            MemoryUsageGrowth = memoryUsages.Last() - memoryUsages.First()
        };
    }
}

// 数据模型类
public class ResourceSnapshot
{
    public DateTimeOffset Timestamp { get; set; }
    public DatabaseSpaceInfo DatabaseSpace { get; set; } = new();
    public SystemResourceInfo SystemResources { get; set; } = new();
    public List<TableStatistic> TableStatistics { get; set; } = new();
    public List<IndexStatistic> IndexStatistics { get; set; } = new();
    public TransactionLogInfo TransactionLog { get; set; } = new();
    public PerformanceCounters PerformanceCounters { get; set; } = new();
}

public class DatabaseSpaceInfo
{
    public string ProviderName { get; set; } = "";
    public string? DatabaseFilePath { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public double DatabaseSizeMB { get; set; }
    public DateTime LastModified { get; set; }
    public long TotalDiskSpaceBytes { get; set; }
    public long FreeDiskSpaceBytes { get; set; }
    public long UsedDiskSpaceBytes { get; set; }
    public double DiskUsagePercentage { get; set; }
    public long TransactionLogSizeBytes { get; set; }
    public double TransactionLogSizeMB { get; set; }
    public long SharedMemorySizeBytes { get; set; }
    public bool SupportsTruncate { get; set; }
    public bool SupportsCompression { get; set; }
    public string? GrowthSettings { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SystemResourceInfo
{
    public long ProcessMemoryBytes { get; set; }
    public double ProcessMemoryMB { get; set; }
    public long PrivateMemoryBytes { get; set; }
    public double PrivateMemoryMB { get; set; }
    public TimeSpan ProcessCpuTime { get; set; }
    public double ProcessCpuTimeMs { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public long GCTotalMemory { get; set; }
    public int GCGen0Collections { get; set; }
    public int GCGen1Collections { get; set; }
    public int GCGen2Collections { get; set; }
    public long SystemTotalMemoryBytes { get; set; }
    public double SystemTotalMemoryMB { get; set; }
    public long SystemFreeMemoryBytes { get; set; }
    public double SystemFreeMemoryMB { get; set; }
    public double SystemLoad1Min { get; set; }
    public double SystemLoad5Min { get; set; }
    public double SystemLoad15Min { get; set; }
    public string? OperatingSystem { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TableStatistic
{
    public string TableName { get; set; } = "";
    public long RecordCount { get; set; }
    public long CountQueryTime { get; set; }
    public long TableSizeBytes { get; set; }
    public double TableSizeMB { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string? ErrorMessage { get; set; }
}

public class IndexStatistic
{
    public string IndexName { get; set; } = "";
    public string TableName { get; set; } = "";
    public long IndexSizeBytes { get; set; }
    public double IndexSizeMB { get; set; }
    public double FragmentationPercentage { get; set; }
    public long UsageCount { get; set; }
    public DateTimeOffset LastUsed { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TransactionLogInfo
{
    public string? LogType { get; set; }
    public long LogSizeBytes { get; set; }
    public double LogSizeMB { get; set; }
    public DateTime LastModified { get; set; }
    public bool AutoGrowth { get; set; }
    public long GrowthSizeBytes { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PerformanceCounters
{
    public long SimpleQueryTimeMs { get; set; }
    public long ComplexQueryTimeMs { get; set; }
    public int QueryResultCount { get; set; }
    public DateTimeOffset LastMeasured { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ResourceUsageTrend
{
    public TimeSpan TimeRange { get; set; }
    public int DataPoints { get; set; }
    public double DatabaseSizeGrowth { get; set; }
    public double AverageDatabaseSize { get; set; }
    public double MaxDatabaseSize { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double MaxMemoryUsage { get; set; }
    public double MemoryUsageGrowth { get; set; }
}

/// <summary>
/// Admin数据库资源监控配置选项
/// </summary>
public class AdminDatabaseResourceOptions
{
    public const string SectionName = "AdminDatabaseResource";

    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 180; // 3分钟

    /// <summary>
    /// 快照保留时间（小时）
    /// </summary>
    public int SnapshotRetentionHours { get; set; } = 48;

    /// <summary>
    /// 数据库大小告警阈值（字节）
    /// </summary>
    public long DatabaseSizeAlertBytes { get; set; } = 2L * 1024 * 1024 * 1024; // 2GB

    /// <summary>
    /// 磁盘使用率告警阈值（百分比）
    /// </summary>
    public double DiskUsageAlertPercentage { get; set; } = 85.0;

    /// <summary>
    /// 内存使用告警阈值（MB）
    /// </summary>
    public double MemoryUsageAlertMB { get; set; } = 512.0;

    /// <summary>
    /// 查询性能告警阈值（毫秒）
    /// </summary>
    public long QueryPerformanceAlertMs { get; set; } = 2000;
}