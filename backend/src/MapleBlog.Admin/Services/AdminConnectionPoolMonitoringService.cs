using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Data;
using System.Data.Common;

namespace MapleBlog.Admin.Services;

/// <summary>
/// Admin连接池监控服务
/// 专门监控数据库连接池的状态和性能
/// </summary>
public class AdminConnectionPoolMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdminConnectionPoolMonitoringService> _logger;
    private readonly AdminConnectionPoolOptions _options;
    private readonly ConcurrentQueue<ConnectionPoolSnapshot> _poolHistory;
    private volatile ConnectionPoolSnapshot _currentSnapshot;

    public AdminConnectionPoolMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<AdminConnectionPoolMonitoringService> logger,
        IOptions<AdminConnectionPoolOptions> options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _poolHistory = new ConcurrentQueue<ConnectionPoolSnapshot>();
        _currentSnapshot = new ConnectionPoolSnapshot();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Admin Connection Pool Monitoring Service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await MonitorConnectionPool();
                await CleanupOldSnapshots();
                await Task.Delay(TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Admin Connection Pool Monitoring Service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Admin Connection Pool Monitoring Service");
        }
    }

    private async Task MonitorConnectionPool()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var snapshot = new ConnectionPoolSnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                PoolStats = await CollectPoolStatistics(context),
                ConnectionTests = await RunConnectionTests(context),
                LoadTests = await RunLoadTests(),
                PerformanceMetrics = await CollectPerformanceMetrics(context)
            };

            _currentSnapshot = snapshot;
            _poolHistory.Enqueue(snapshot);

            await CheckPoolHealth(snapshot);

            _logger.LogDebug("Connection pool monitoring completed at {Timestamp}", snapshot.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor connection pool");
        }
    }

    private async Task<ConnectionPoolStats> CollectPoolStatistics(ApplicationDbContext context)
    {
        var stats = new ConnectionPoolStats();

        try
        {
            var connection = context.Database.GetDbConnection();

            stats.ProviderName = context.Database.ProviderName ?? "Unknown";
            stats.ConnectionString = ObfuscateConnectionString(connection.ConnectionString);
            stats.ConnectionTimeout = connection.ConnectionTimeout;
            stats.DatabaseName = connection.Database;
            stats.ServerVersion = connection.ServerVersion ?? "Unknown";
            stats.CurrentState = connection.State.ToString();

            // 尝试获取连接池相关信息
            if (connection is DbConnection dbConnection)
            {
                // 对于不同的数据库提供商，获取特定的连接池信息
                stats.PoolInfo = await GetProviderSpecificPoolInfo(dbConnection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect connection pool statistics");
            stats.ErrorMessage = ex.Message;
        }

        return stats;
    }

    private async Task<Dictionary<string, object>> GetProviderSpecificPoolInfo(DbConnection connection)
    {
        var poolInfo = new Dictionary<string, object>();

        try
        {
            // 基本连接信息
            poolInfo["ConnectionType"] = connection.GetType().Name;
            poolInfo["DataSource"] = connection.DataSource ?? "Unknown";
            poolInfo["State"] = connection.State.ToString();

            // 对于SQLite，获取文件相关信息
            if (connection.GetType().Name.Contains("SqliteConnection"))
            {
                await GetSqlitePoolInfo(connection, poolInfo);
            }
            // 对于SQL Server，可以获取更多连接池信息
            else if (connection.GetType().Name.Contains("SqlConnection"))
            {
                await GetSqlServerPoolInfo(connection, poolInfo);
            }
            // 对于PostgreSQL
            else if (connection.GetType().Name.Contains("NpgsqlConnection"))
            {
                await GetPostgreSqlPoolInfo(connection, poolInfo);
            }
        }
        catch (Exception ex)
        {
            poolInfo["Error"] = ex.Message;
        }

        return poolInfo;
    }

    private async Task GetSqlitePoolInfo(DbConnection connection, Dictionary<string, object> poolInfo)
    {
        try
        {
            // SQLite特定信息
            var connectionString = connection.ConnectionString;
            var dataSourceMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=([^;]+)");

            if (dataSourceMatch.Success)
            {
                var dbPath = dataSourceMatch.Groups[1].Value;
                if (File.Exists(dbPath))
                {
                    var fileInfo = new FileInfo(dbPath);
                    poolInfo["DatabaseFile"] = dbPath;
                    poolInfo["FileSize"] = fileInfo.Length;
                    poolInfo["LastModified"] = fileInfo.LastWriteTime;
                    poolInfo["IsReadOnly"] = fileInfo.IsReadOnly;
                }
            }

            // SQLite没有传统意义上的连接池，但我们可以检查连接状态
            poolInfo["SupportsConnectionPooling"] = false;
            poolInfo["CacheMode"] = ExtractConnectionStringParameter(connectionString, "Cache");
            poolInfo["JournalMode"] = ExtractConnectionStringParameter(connectionString, "Journal Mode");
        }
        catch (Exception ex)
        {
            poolInfo["SqliteError"] = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task GetSqlServerPoolInfo(DbConnection connection, Dictionary<string, object> poolInfo)
    {
        try
        {
            // SQL Server连接池信息
            poolInfo["SupportsConnectionPooling"] = true;

            var connectionString = connection.ConnectionString;
            poolInfo["Pooling"] = ExtractConnectionStringParameter(connectionString, "Pooling", "true");
            poolInfo["MaxPoolSize"] = ExtractConnectionStringParameter(connectionString, "Max Pool Size", "100");
            poolInfo["MinPoolSize"] = ExtractConnectionStringParameter(connectionString, "Min Pool Size", "0");
            poolInfo["ConnectionLifetime"] = ExtractConnectionStringParameter(connectionString, "Connection Lifetime", "0");

            // 如果连接已打开，尝试获取更多信息
            if (connection.State == ConnectionState.Open)
            {
                // 这里可以执行SQL Server特定的查询来获取连接池统计信息
                // 例如：SELECT * FROM sys.dm_exec_connections
            }
        }
        catch (Exception ex)
        {
            poolInfo["SqlServerError"] = ex.Message;
        }

        await Task.CompletedTask;
    }

    private async Task GetPostgreSqlPoolInfo(DbConnection connection, Dictionary<string, object> poolInfo)
    {
        try
        {
            // PostgreSQL连接池信息
            poolInfo["SupportsConnectionPooling"] = true;

            var connectionString = connection.ConnectionString;
            poolInfo["Pooling"] = ExtractConnectionStringParameter(connectionString, "Pooling", "true");
            poolInfo["MaxPoolSize"] = ExtractConnectionStringParameter(connectionString, "Maximum Pool Size", "100");
            poolInfo["MinPoolSize"] = ExtractConnectionStringParameter(connectionString, "Minimum Pool Size", "1");
            poolInfo["ConnectionLifetime"] = ExtractConnectionStringParameter(connectionString, "Connection Lifetime", "15");
        }
        catch (Exception ex)
        {
            poolInfo["PostgreSqlError"] = ex.Message;
        }

        await Task.CompletedTask;
    }

    private string ExtractConnectionStringParameter(string connectionString, string parameter, string defaultValue = "")
    {
        try
        {
            var pattern = $@"{parameter}\s*=\s*([^;]+)";
            var match = System.Text.RegularExpressions.Regex.Match(connectionString, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private async Task<List<ConnectionTestResult>> RunConnectionTests(ApplicationDbContext context)
    {
        var tests = new List<ConnectionTestResult>();

        // 测试1：基本连接测试
        var basicTest = await RunBasicConnectionTest(context);
        tests.Add(basicTest);

        // 测试2：并发连接测试
        var concurrentTest = await RunConcurrentConnectionTest();
        tests.Add(concurrentTest);

        // 测试3：连接超时测试
        var timeoutTest = await RunConnectionTimeoutTest(context);
        tests.Add(timeoutTest);

        // 测试4：连接恢复测试
        var recoveryTest = await RunConnectionRecoveryTest(context);
        tests.Add(recoveryTest);

        return tests;
    }

    private async Task<ConnectionTestResult> RunBasicConnectionTest(ApplicationDbContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var test = new ConnectionTestResult { TestName = "BasicConnection", StartTime = DateTimeOffset.UtcNow };

        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            stopwatch.Stop();

            test.Success = canConnect;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.Details = $"Connection test result: {canConnect}";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            test.Success = false;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<ConnectionTestResult> RunConcurrentConnectionTest()
    {
        var stopwatch = Stopwatch.StartNew();
        var test = new ConnectionTestResult { TestName = "ConcurrentConnections", StartTime = DateTimeOffset.UtcNow };

        try
        {
            var concurrentTasks = Enumerable.Range(0, _options.ConcurrentConnectionsToTest)
                .Select(async _ =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    return await context.Database.CanConnectAsync();
                });

            var results = await Task.WhenAll(concurrentTasks);
            stopwatch.Stop();

            var successCount = results.Count(r => r);
            test.Success = successCount == _options.ConcurrentConnectionsToTest;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.Details = $"Successful connections: {successCount}/{_options.ConcurrentConnectionsToTest}";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            test.Success = false;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<ConnectionTestResult> RunConnectionTimeoutTest(ApplicationDbContext context)
    {
        var test = new ConnectionTestResult { TestName = "ConnectionTimeout", StartTime = DateTimeOffset.UtcNow };

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.ConnectionTimeoutTestSeconds));
            var stopwatch = Stopwatch.StartNew();

            var canConnect = await context.Database.CanConnectAsync(cts.Token);
            stopwatch.Stop();

            test.Success = canConnect && !cts.Token.IsCancellationRequested;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.Details = $"Connection within timeout: {test.Success}";
        }
        catch (OperationCanceledException)
        {
            test.Success = false;
            test.Duration = _options.ConnectionTimeoutTestSeconds * 1000;
            test.ErrorMessage = "Connection timeout exceeded";
        }
        catch (Exception ex)
        {
            test.Success = false;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<ConnectionTestResult> RunConnectionRecoveryTest(ApplicationDbContext context)
    {
        var test = new ConnectionTestResult { TestName = "ConnectionRecovery", StartTime = DateTimeOffset.UtcNow };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 首先确保连接正常
            var initialConnect = await context.Database.CanConnectAsync();
            if (!initialConnect)
            {
                test.Success = false;
                test.ErrorMessage = "Initial connection failed";
                return test;
            }

            // 尝试关闭连接然后重新连接
            await context.Database.CloseConnectionAsync();
            await Task.Delay(100); // 短暂等待

            var reconnect = await context.Database.CanConnectAsync();
            stopwatch.Stop();

            test.Success = reconnect;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.Details = $"Recovery successful: {reconnect}";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            test.Success = false;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<List<LoadTestResult>> RunLoadTests()
    {
        var loadTests = new List<LoadTestResult>();

        // 负载测试1：突发连接测试
        var burstTest = await RunBurstConnectionTest();
        loadTests.Add(burstTest);

        // 负载测试2：持续负载测试
        var sustainedTest = await RunSustainedLoadTest();
        loadTests.Add(sustainedTest);

        return loadTests;
    }

    private async Task<LoadTestResult> RunBurstConnectionTest()
    {
        var test = new LoadTestResult { TestName = "BurstConnections", StartTime = DateTimeOffset.UtcNow };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var tasks = Enumerable.Range(0, _options.BurstConnectionCount)
                .Select(async i =>
                {
                    var taskStopwatch = Stopwatch.StartNew();
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var result = await context.Database.CanConnectAsync();
                        taskStopwatch.Stop();
                        return new { Success = result, Duration = taskStopwatch.ElapsedMilliseconds, Index = i };
                    }
                    catch (Exception ex)
                    {
                        taskStopwatch.Stop();
                        return new { Success = false, Duration = taskStopwatch.ElapsedMilliseconds, Index = i, Error = ex.Message };
                    }
                });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            var successCount = results.Count(r => r.Success);
            var avgDuration = results.Average(r => r.Duration);

            test.Success = successCount >= _options.BurstConnectionCount * 0.9; // 90% success rate
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ConnectionsAttempted = _options.BurstConnectionCount;
            test.ConnectionsSuccessful = successCount;
            test.AverageConnectionTime = avgDuration;
            test.Details = $"Burst test: {successCount}/{_options.BurstConnectionCount} successful";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            test.Success = false;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<LoadTestResult> RunSustainedLoadTest()
    {
        var test = new LoadTestResult { TestName = "SustainedLoad", StartTime = DateTimeOffset.UtcNow };
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var connectionTimes = new List<double>();
            var successCount = 0;
            var testDuration = TimeSpan.FromSeconds(_options.SustainedLoadTestDurationSeconds);
            var interval = TimeSpan.FromMilliseconds(500);

            while (stopwatch.Elapsed < testDuration)
            {
                var connectionStopwatch = Stopwatch.StartNew();
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var result = await context.Database.CanConnectAsync();
                    connectionStopwatch.Stop();

                    if (result)
                    {
                        successCount++;
                        connectionTimes.Add(connectionStopwatch.ElapsedMilliseconds);
                    }
                }
                catch
                {
                    connectionStopwatch.Stop();
                }

                await Task.Delay(interval);
            }

            stopwatch.Stop();

            test.Success = successCount > 0 && connectionTimes.Average() < _options.AcceptableConnectionTimeMs;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ConnectionsAttempted = (int)(testDuration.TotalMilliseconds / interval.TotalMilliseconds);
            test.ConnectionsSuccessful = successCount;
            test.AverageConnectionTime = connectionTimes.Any() ? connectionTimes.Average() : 0;
            test.Details = $"Sustained test: {successCount} successful connections over {testDuration.TotalSeconds}s";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            test.Success = false;
            test.Duration = stopwatch.ElapsedMilliseconds;
            test.ErrorMessage = ex.Message;
        }

        return test;
    }

    private async Task<ConnectionPerformanceMetrics> CollectPerformanceMetrics(ApplicationDbContext context)
    {
        var metrics = new ConnectionPerformanceMetrics();

        try
        {
            // 收集连接性能指标
            var openStopwatch = Stopwatch.StartNew();
            await context.Database.OpenConnectionAsync();
            openStopwatch.Stop();
            metrics.ConnectionOpenTime = openStopwatch.ElapsedMilliseconds;

            var closeStopwatch = Stopwatch.StartNew();
            await context.Database.CloseConnectionAsync();
            closeStopwatch.Stop();
            metrics.ConnectionCloseTime = closeStopwatch.ElapsedMilliseconds;

            // 测试简单查询性能
            var queryStopwatch = Stopwatch.StartNew();
            var count = await context.Users.CountAsync();
            queryStopwatch.Stop();
            metrics.SimpleQueryTime = queryStopwatch.ElapsedMilliseconds;
            metrics.QueryResult = count;
        }
        catch (Exception ex)
        {
            metrics.ErrorMessage = ex.Message;
        }

        return metrics;
    }

    private async Task CheckPoolHealth(ConnectionPoolSnapshot snapshot)
    {
        var alerts = new List<string>();

        // 检查连接测试失败
        var failedTests = snapshot.ConnectionTests.Where(t => !t.Success).ToList();
        if (failedTests.Any())
        {
            alerts.Add($"Connection tests failed: {string.Join(", ", failedTests.Select(t => t.TestName))}");
        }

        // 检查负载测试
        var failedLoadTests = snapshot.LoadTests.Where(t => !t.Success).ToList();
        if (failedLoadTests.Any())
        {
            alerts.Add($"Load tests failed: {string.Join(", ", failedLoadTests.Select(t => t.TestName))}");
        }

        // 检查性能指标
        if (snapshot.PerformanceMetrics.ConnectionOpenTime > _options.AlertConnectionOpenTimeMs)
        {
            alerts.Add($"Connection open time ({snapshot.PerformanceMetrics.ConnectionOpenTime}ms) exceeds threshold");
        }

        if (alerts.Any())
        {
            _logger.LogWarning("Connection pool health alerts: {Alerts}", string.Join("; ", alerts));
        }

        await Task.CompletedTask;
    }

    private async Task CleanupOldSnapshots()
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-_options.SnapshotRetentionHours);
        var removed = 0;

        while (_poolHistory.TryPeek(out var oldestSnapshot) && oldestSnapshot.Timestamp < cutoffTime)
        {
            if (_poolHistory.TryDequeue(out _))
            {
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("Cleaned up {RemovedCount} old connection pool snapshots", removed);
        }

        await Task.CompletedTask;
    }

    private string ObfuscateConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Not available";

        // 简单的连接字符串混淆，隐藏敏感信息
        var obfuscated = System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd|User Id|UID)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        return obfuscated;
    }

    /// <summary>
    /// 获取当前连接池快照
    /// </summary>
    public ConnectionPoolSnapshot GetCurrentSnapshot()
    {
        return _currentSnapshot;
    }

    /// <summary>
    /// 获取连接池历史数据
    /// </summary>
    public IEnumerable<ConnectionPoolSnapshot> GetHistoricalSnapshots(TimeSpan timeRange)
    {
        var cutoffTime = DateTimeOffset.UtcNow.Subtract(timeRange);
        return _poolHistory.Where(s => s.Timestamp >= cutoffTime).OrderBy(s => s.Timestamp);
    }
}

// 数据模型类
public class ConnectionPoolSnapshot
{
    public DateTimeOffset Timestamp { get; set; }
    public ConnectionPoolStats PoolStats { get; set; } = new();
    public List<ConnectionTestResult> ConnectionTests { get; set; } = new();
    public List<LoadTestResult> LoadTests { get; set; } = new();
    public ConnectionPerformanceMetrics PerformanceMetrics { get; set; } = new();
}

public class ConnectionPoolStats
{
    public string ProviderName { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public int ConnectionTimeout { get; set; }
    public string DatabaseName { get; set; } = "";
    public string ServerVersion { get; set; } = "";
    public string CurrentState { get; set; } = "";
    public Dictionary<string, object> PoolInfo { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ConnectionTestResult
{
    public string TestName { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public bool Success { get; set; }
    public long Duration { get; set; }
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LoadTestResult
{
    public string TestName { get; set; } = "";
    public DateTimeOffset StartTime { get; set; }
    public bool Success { get; set; }
    public long Duration { get; set; }
    public int ConnectionsAttempted { get; set; }
    public int ConnectionsSuccessful { get; set; }
    public double AverageConnectionTime { get; set; }
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConnectionPerformanceMetrics
{
    public long ConnectionOpenTime { get; set; }
    public long ConnectionCloseTime { get; set; }
    public long SimpleQueryTime { get; set; }
    public int QueryResult { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Admin连接池监控配置选项
/// </summary>
public class AdminConnectionPoolOptions
{
    public const string SectionName = "AdminConnectionPool";

    /// <summary>
    /// 监控间隔（秒）
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 120;

    /// <summary>
    /// 快照保留时间（小时）
    /// </summary>
    public int SnapshotRetentionHours { get; set; } = 12;

    /// <summary>
    /// 并发连接测试数量
    /// </summary>
    public int ConcurrentConnectionsToTest { get; set; } = 5;

    /// <summary>
    /// 连接超时测试时间（秒）
    /// </summary>
    public int ConnectionTimeoutTestSeconds { get; set; } = 10;

    /// <summary>
    /// 突发连接测试数量
    /// </summary>
    public int BurstConnectionCount { get; set; } = 10;

    /// <summary>
    /// 持续负载测试持续时间（秒）
    /// </summary>
    public int SustainedLoadTestDurationSeconds { get; set; } = 30;

    /// <summary>
    /// 可接受的连接时间（毫秒）
    /// </summary>
    public double AcceptableConnectionTimeMs { get; set; } = 1000;

    /// <summary>
    /// 连接打开时间告警阈值（毫秒）
    /// </summary>
    public long AlertConnectionOpenTimeMs { get; set; } = 2000;
}