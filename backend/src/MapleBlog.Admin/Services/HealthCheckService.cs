using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Admin.DTOs;
using System.Diagnostics;
using System.Net.NetworkInformation;
using StackExchange.Redis;
using MSHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using DTOHealthStatus = MapleBlog.Admin.DTOs.HealthStatus;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 健康检查服务接口
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查数据库连接健康状态
    /// </summary>
    Task<ComponentHealthDto> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查Redis缓存健康状态
    /// </summary>
    Task<ComponentHealthDto> CheckRedisHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查内存缓存健康状态
    /// </summary>
    Task<ComponentHealthDto> CheckMemoryCacheHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查外部服务健康状态
    /// </summary>
    Task<List<ExternalServiceStatusDto>> CheckExternalServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行自定义健康检查
    /// </summary>
    Task<ComponentHealthDto> ExecuteCustomHealthCheckAsync(string checkName, Func<Task<HealthCheckResult>> healthCheck, CancellationToken cancellationToken = default);
}

/// <summary>
/// 健康检查服务实现
/// 负责检查系统各组件的健康状态
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConnectionMultiplexer? _redis;

    private static readonly Process _currentProcess = Process.GetCurrentProcess();
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthCheckService(
        ApplicationDbContext context,
        IMemoryCache memoryCache,
        ILogger<HealthCheckService> logger,
        IConfiguration configuration,
        IDistributedCache? distributedCache = null,
        IConnectionMultiplexer? redis = null)
    {
        _context = context;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _configuration = configuration;
        _redis = redis;
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    public async Task<SystemHealthDto> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var health = new SystemHealthDto
            {
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                Version = GetApplicationVersion(),
                LastRestartTime = _startTime,
                UptimeSeconds = (long)(DateTime.UtcNow - _startTime).TotalSeconds
            };

            // 并行检查各组件健康状态
            var tasks = new[]
            {
                Task.Run(async () => ("Database", await CheckDatabaseHealthAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("Redis", await CheckRedisHealthAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("MemoryCache", await CheckMemoryCacheHealthAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("Application", await CheckApplicationHealthAsync(cancellationToken)), cancellationToken),
                Task.Run(async () => ("System", await CheckSystemResourcesAsync(cancellationToken)), cancellationToken)
            };

            var results = await Task.WhenAll(tasks);

            foreach (var (componentName, componentHealth) in results)
            {
                health.Components[componentName] = componentHealth;
            }

            // 计算整体健康状态
            health.OverallStatus = CalculateOverallHealth(health.Components.Values);

            stopwatch.Stop();
            _logger.LogInformation("System health check completed in {ElapsedMs}ms, Overall status: {Status}",
                stopwatch.ElapsedMilliseconds, health.OverallStatus);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during system health check");
            return new SystemHealthDto
            {
                OverallStatus = DTOHealthStatus.Unhealthy,
                Environment = "Unknown",
                Version = "Unknown",
                Components = new Dictionary<string, ComponentHealthDto>
                {
                    ["SystemCheck"] = new ComponentHealthDto
                    {
                        Status = DTOHealthStatus.Unhealthy,
                        Description = "Health check system failure",
                        ErrorMessage = ex.Message
                    }
                }
            };
        }
    }

    public async Task<SystemHealthDto> CheckHealthAsync()
    {
        return await GetSystemHealthAsync();
    }

    /// <summary>
    /// 检查数据库连接健康状态
    /// </summary>
    public async Task<ComponentHealthDto> CheckDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 测试数据库连接
            using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            // 执行简单查询测试
            var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync(cancellationToken);

            stopwatch.Stop();

            if (result != null)
            {
                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Healthy,
                    Description = "Database connection is healthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Data = new Dictionary<string, object>
                    {
                        ["ConnectionString"] = _context.Database.GetConnectionString() ?? "Unknown",
                        ["Provider"] = _context.Database.ProviderName ?? "Unknown",
                        ["CanConnect"] = true
                    }
                };
            }
            else
            {
                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Degraded,
                    Description = "Database query returned null",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = "Query result was null"
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");

            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Unhealthy,
                Description = "Database connection failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                Data = new Dictionary<string, object>
                {
                    ["Exception"] = ex.GetType().Name,
                    ["CanConnect"] = false
                }
            };
        }
    }

    /// <summary>
    /// 检查Redis缓存健康状态
    /// </summary>
    public async Task<ComponentHealthDto> CheckRedisHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (_redis == null || _distributedCache == null)
            {
                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Degraded,
                    Description = "Redis not configured",
                    ResponseTimeMs = 0,
                    Data = new Dictionary<string, object>
                    {
                        ["Configured"] = false,
                        ["Available"] = false
                    }
                };
            }

            var database = _redis.GetDatabase();

            // 测试基本的set/get操作
            var testKey = $"health-check:{Guid.NewGuid()}";
            var testValue = "test-value";

            await database.StringSetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            var retrievedValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);

            stopwatch.Stop();

            if (retrievedValue == testValue)
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var info = await server.InfoAsync();

                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Healthy,
                    Description = "Redis connection is healthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Data = new Dictionary<string, object>
                    {
                        ["Configured"] = true,
                        ["Available"] = true,
                        ["TestSuccessful"] = true,
                        ["ConnectedClients"] = info.SelectMany(g => g).FirstOrDefault(x => x.Key == "connected_clients").Value ?? "Unknown",
                        ["UsedMemory"] = info.SelectMany(g => g).FirstOrDefault(x => x.Key == "used_memory").Value ?? "Unknown"
                    }
                };
            }
            else
            {
                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Degraded,
                    Description = "Redis test operation failed",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = $"Expected '{testValue}', got '{retrievedValue}'"
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Redis health check failed");

            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Unhealthy,
                Description = "Redis connection failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                Data = new Dictionary<string, object>
                {
                    ["Configured"] = _redis != null,
                    ["Available"] = false,
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// 检查内存缓存健康状态
    /// </summary>
    public async Task<ComponentHealthDto> CheckMemoryCacheHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // 测试内存缓存读写
            var testKey = $"health-check-{Guid.NewGuid()}";
            var testValue = "test-value";

            _memoryCache.Set(testKey, testValue, TimeSpan.FromMinutes(1));
            var retrievedValue = _memoryCache.Get<string>(testKey);
            _memoryCache.Remove(testKey);

            stopwatch.Stop();

            await Task.CompletedTask; // 保持异步签名

            if (retrievedValue == testValue)
            {
                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Healthy,
                    Description = "Memory cache is healthy",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Data = new Dictionary<string, object>
                    {
                        ["Available"] = true,
                        ["TestSuccessful"] = true,
                        ["CacheType"] = _memoryCache.GetType().Name
                    }
                };
            }
            else
            {
                return new ComponentHealthDto
                {
                    Status = DTOHealthStatus.Degraded,
                    Description = "Memory cache test failed",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = $"Expected '{testValue}', got '{retrievedValue}'"
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Memory cache health check failed");

            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Unhealthy,
                Description = "Memory cache failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                Data = new Dictionary<string, object>
                {
                    ["Available"] = false,
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// 检查外部服务健康状态
    /// </summary>
    public async Task<List<ExternalServiceStatusDto>> CheckExternalServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = new List<ExternalServiceStatusDto>();

        try
        {
            // 从配置获取外部服务列表
            var externalServices = _configuration.GetSection("ExternalServices").GetChildren();

            var tasks = externalServices.Select(async service =>
            {
                var serviceName = service["Name"] ?? service.Key;
                var serviceUrl = service["Url"] ?? "";

                if (string.IsNullOrEmpty(serviceUrl))
                {
                    return new ExternalServiceStatusDto
                    {
                        ServiceName = serviceName,
                        ServiceUrl = serviceUrl,
                        Status = DTOHealthStatus.Unknown,
                        ErrorMessage = "Service URL not configured",
                        LastChecked = DateTime.UtcNow
                    };
                }

                return await CheckExternalServiceAsync(serviceName, serviceUrl, cancellationToken);
            });

            var results = await Task.WhenAll(tasks);
            services.AddRange(results);

            // 如果没有配置外部服务，添加默认检查
            if (!services.Any())
            {
                services.Add(new ExternalServiceStatusDto
                {
                    ServiceName = "Configuration",
                    ServiceUrl = "N/A",
                    Status = DTOHealthStatus.Healthy,
                    ResponseTimeMs = 0,
                    LastChecked = DateTime.UtcNow,
                    ErrorMessage = null
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking external services");
            services.Add(new ExternalServiceStatusDto
            {
                ServiceName = "External Services Check",
                ServiceUrl = "N/A",
                Status = DTOHealthStatus.Unhealthy,
                ResponseTimeMs = 0,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            });
        }

        return services;
    }

    /// <summary>
    /// 执行自定义健康检查
    /// </summary>
    public async Task<ComponentHealthDto> ExecuteCustomHealthCheckAsync(string checkName, Func<Task<HealthCheckResult>> healthCheck, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await healthCheck();
            stopwatch.Stop();

            return new ComponentHealthDto
            {
                Status = ConvertHealthStatus(result.Status),
                Description = result.Description ?? $"{checkName} health check",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = result.Exception?.Message,
                Data = result.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Custom health check '{CheckName}' failed", checkName);

            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Unhealthy,
                Description = $"{checkName} health check failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message,
                Data = new Dictionary<string, object>
                {
                    ["Exception"] = ex.GetType().Name
                }
            };
        }
    }

    /// <summary>
    /// 检查应用程序健康状态
    /// </summary>
    private async Task<ComponentHealthDto> CheckApplicationHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Task.Delay(10, cancellationToken); // 模拟检查

            var threadCount = _currentProcess.Threads.Count;
            var workingSet = _currentProcess.WorkingSet64;
            var gcGen0 = GC.CollectionCount(0);
            var gcGen1 = GC.CollectionCount(1);
            var gcGen2 = GC.CollectionCount(2);

            stopwatch.Stop();

            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Healthy,
                Description = "Application is running normally",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Data = new Dictionary<string, object>
                {
                    ["ProcessId"] = _currentProcess.Id,
                    ["ThreadCount"] = threadCount,
                    ["WorkingSetMB"] = workingSet / (1024 * 1024),
                    ["GC_Gen0"] = gcGen0,
                    ["GC_Gen1"] = gcGen1,
                    ["GC_Gen2"] = gcGen2,
                    ["UptimeSeconds"] = (DateTime.UtcNow - _startTime).TotalSeconds
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Unhealthy,
                Description = "Application health check failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 检查系统资源状态
    /// </summary>
    private async Task<ComponentHealthDto> CheckSystemResourcesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Task.Delay(10, cancellationToken); // 模拟检查

            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = _currentProcess.WorkingSet64;
            var status = DTOHealthStatus.Healthy;

            // 简单的资源使用检查
            if (workingSet > 1024 * 1024 * 1024) // 超过1GB
            {
                status = DTOHealthStatus.Degraded;
            }

            stopwatch.Stop();

            return new ComponentHealthDto
            {
                Status = status,
                Description = "System resources check",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                Data = new Dictionary<string, object>
                {
                    ["ManagedMemoryMB"] = totalMemory / (1024 * 1024),
                    ["WorkingSetMB"] = workingSet / (1024 * 1024),
                    ["ProcessorCount"] = Environment.ProcessorCount,
                    ["OSVersion"] = Environment.OSVersion.ToString(),
                    ["MachineName"] = Environment.MachineName
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealthDto
            {
                Status = DTOHealthStatus.Unhealthy,
                Description = "System resources check failed",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 检查单个外部服务
    /// </summary>
    private async Task<ExternalServiceStatusDto> CheckExternalServiceAsync(string serviceName, string serviceUrl, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync(serviceUrl, cancellationToken);
            stopwatch.Stop();

            return new ExternalServiceStatusDto
            {
                ServiceName = serviceName,
                ServiceUrl = serviceUrl,
                Status = response.IsSuccessStatusCode ? DTOHealthStatus.Healthy : DTOHealthStatus.Degraded,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                Version = response.Headers.GetValues("X-Version").FirstOrDefault()
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ExternalServiceStatusDto
            {
                ServiceName = serviceName,
                ServiceUrl = serviceUrl,
                Status = DTOHealthStatus.Unhealthy,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// 获取应用程序版本
    /// </summary>
    private string GetApplicationVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// 计算整体健康状态
    /// </summary>
    private DTOHealthStatus CalculateOverallHealth(IEnumerable<ComponentHealthDto> components)
    {
        if (!components.Any())
            return DTOHealthStatus.Unknown;

        if (components.Any(c => c.Status == DTOHealthStatus.Unhealthy))
            return DTOHealthStatus.Unhealthy;

        if (components.Any(c => c.Status == DTOHealthStatus.Degraded))
            return DTOHealthStatus.Degraded;

        if (components.All(c => c.Status == DTOHealthStatus.Healthy))
            return DTOHealthStatus.Healthy;

        return DTOHealthStatus.Unknown;
    }

    /// <summary>
    /// 转换.NET健康检查状态到自定义状态
    /// </summary>
    private DTOHealthStatus ConvertHealthStatus(MSHealthStatus status)
    {
        return status switch
        {
            MSHealthStatus.Healthy => DTOHealthStatus.Healthy,
            MSHealthStatus.Degraded => DTOHealthStatus.Degraded,
            MSHealthStatus.Unhealthy => DTOHealthStatus.Unhealthy,
            _ => DTOHealthStatus.Unknown
        };
    }
}