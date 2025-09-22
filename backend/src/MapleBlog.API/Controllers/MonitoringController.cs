using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MapleBlog.Infrastructure.Services;

namespace MapleBlog.API.Controllers;

/// <summary>
/// 系统监控API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class MonitoringController : ControllerBase
{
    private readonly IRedisMonitoringService? _redisMonitoringService;
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<MonitoringController> _logger;

    public MonitoringController(
        IServiceProvider serviceProvider,
        HealthCheckService healthCheckService,
        ILogger<MonitoringController> logger)
    {
        _redisMonitoringService = serviceProvider.GetService<IRedisMonitoringService>();
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealth()
    {
        try
        {
            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                status = healthReport.Status.ToString(),
                duration = healthReport.TotalDuration.TotalMilliseconds,
                timestamp = DateTimeOffset.UtcNow,
                checks = healthReport.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    data = entry.Value.Data.Count > 0 ? entry.Value.Data : null
                }).ToArray()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return StatusCode(500, new { error = "Failed to get health status" });
        }
    }

    /// <summary>
    /// 获取Redis连接状态
    /// </summary>
    [HttpGet("redis/status")]
    public ActionResult<object> GetRedisStatus()
    {
        try
        {
            if (_redisMonitoringService == null)
            {
                return Ok(new
                {
                    status = "not_configured",
                    message = "Redis is not configured",
                    timestamp = DateTimeOffset.UtcNow
                });
            }

            var connectionStatus = _redisMonitoringService.GetConnectionStatus();
            return Ok(connectionStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis status");
            return StatusCode(500, new { error = "Failed to get Redis status" });
        }
    }

    /// <summary>
    /// 获取Redis性能指标
    /// </summary>
    [HttpGet("redis/metrics")]
    public ActionResult<object> GetRedisMetrics([FromQuery] string? endpoint = null)
    {
        try
        {
            if (_redisMonitoringService == null)
            {
                return Ok(new
                {
                    status = "not_configured",
                    message = "Redis is not configured",
                    timestamp = DateTimeOffset.UtcNow
                });
            }

            if (string.IsNullOrEmpty(endpoint))
            {
                var allMetrics = _redisMonitoringService.GetAllMetrics();
                return Ok(allMetrics);
            }
            else
            {
                var metrics = _redisMonitoringService.GetMetrics(endpoint);
                return Ok(metrics);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Redis metrics");
            return StatusCode(500, new { error = "Failed to get Redis metrics" });
        }
    }

    /// <summary>
    /// 获取系统信息
    /// </summary>
    [HttpGet("system")]
    public ActionResult<object> GetSystemInfo()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

            var response = new
            {
                machine_name = Environment.MachineName,
                os_version = Environment.OSVersion.ToString(),
                processor_count = Environment.ProcessorCount,
                working_set = Environment.WorkingSet,
                gc_memory = GC.GetTotalMemory(false),
                uptime = new
                {
                    days = uptime.Days,
                    hours = uptime.Hours,
                    minutes = uptime.Minutes,
                    seconds = uptime.Seconds,
                    total_milliseconds = uptime.TotalMilliseconds
                },
                process = new
                {
                    id = process.Id,
                    process_name = process.ProcessName,
                    start_time = process.StartTime,
                    threads = process.Threads.Count,
                    handles = process.HandleCount
                },
                memory = new
                {
                    working_set = process.WorkingSet64,
                    private_memory = process.PrivateMemorySize64,
                    virtual_memory = process.VirtualMemorySize64,
                    gc_total_memory = GC.GetTotalMemory(false),
                    gc_total_memory_forced = GC.GetTotalMemory(true)
                },
                gc = new
                {
                    gen0_collections = GC.CollectionCount(0),
                    gen1_collections = GC.CollectionCount(1),
                    gen2_collections = GC.CollectionCount(2)
                },
                timestamp = DateTimeOffset.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system info");
            return StatusCode(500, new { error = "Failed to get system info" });
        }
    }

    /// <summary>
    /// 获取应用程序配置状态
    /// </summary>
    [HttpGet("config")]
    public ActionResult<object> GetConfigurationStatus()
    {
        try
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            var response = new
            {
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                has_redis = !string.IsNullOrEmpty(configuration.GetConnectionString("Redis")),
                has_database = !string.IsNullOrEmpty(configuration.GetConnectionString("DefaultConnection")),
                database_provider = configuration.GetValue<string>("DatabaseProvider", "SQLite"),
                cors_origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>(),
                jwt_configured = !string.IsNullOrEmpty(configuration.GetSection("JwtSettings:SecretKey").Value),
                timestamp = DateTimeOffset.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration status");
            return StatusCode(500, new { error = "Failed to get configuration status" });
        }
    }
}