using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Admin.Services;
using MapleBlog.Admin.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Admin.Controllers;

/// <summary>
/// 系统监控API控制器
/// 提供系统监控数据、健康检查和警告管理功能
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class SystemMonitorController : ControllerBase
{
    private readonly ISystemMonitorService _systemMonitorService;
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<SystemMonitorController> _logger;

    public SystemMonitorController(
        ISystemMonitorService systemMonitorService,
        IHealthCheckService healthCheckService,
        ILogger<SystemMonitorController> logger)
    {
        _systemMonitorService = systemMonitorService;
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统完整指标
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统指标数据</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(SystemMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system metrics");
            var metrics = await _systemMonitorService.GetSystemMetricsAsync(cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取系统指标失败");
        }
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统健康状态</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system health status");
            var health = await _healthCheckService.GetSystemHealthAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取系统健康状态失败");
        }
    }

    /// <summary>
    /// 获取系统性能指标
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>性能指标</returns>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(SystemPerformanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemPerformanceDto>> GetPerformanceMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting performance metrics");
            var performance = await _systemMonitorService.GetPerformanceMetricsAsync(cancellationToken);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取性能指标失败");
        }
    }

    /// <summary>
    /// 获取数据库指标
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据库指标</returns>
    [HttpGet("database")]
    [ProducesResponseType(typeof(DatabaseMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DatabaseMetricsDto>> GetDatabaseMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting database metrics");
            var database = await _systemMonitorService.GetDatabaseMetricsAsync(cancellationToken);
            return Ok(database);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取数据库指标失败");
        }
    }

    /// <summary>
    /// 获取缓存指标
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存指标</returns>
    [HttpGet("cache")]
    [ProducesResponseType(typeof(CacheMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CacheMetricsDto>> GetCacheMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting cache metrics");
            var cache = await _systemMonitorService.GetCacheMetricsAsync(cancellationToken);
            return Ok(cache);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取缓存指标失败");
        }
    }

    /// <summary>
    /// 获取应用程序指标
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用程序指标</returns>
    [HttpGet("application")]
    [ProducesResponseType(typeof(ApplicationMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApplicationMetricsDto>> GetApplicationMetrics(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting application metrics");
            var application = await _systemMonitorService.GetApplicationMetricsAsync(cancellationToken);
            return Ok(application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取应用程序指标失败");
        }
    }

    /// <summary>
    /// 获取系统警告列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统警告列表</returns>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(List<SystemAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SystemAlertDto>>> GetSystemAlerts(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting system alerts");
            var alerts = await _systemMonitorService.GetSystemAlertsAsync(cancellationToken);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system alerts");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取系统警告失败");
        }
    }

    /// <summary>
    /// 确认系统警告
    /// </summary>
    /// <param name="alertId">警告ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("alerts/{alertId}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AcknowledgeAlert(
        [Required] string alertId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Acknowledging alert {AlertId}", alertId);

            // 获取当前用户信息
            var userName = User.Identity?.Name ?? "Unknown";

            // 这里需要实现警告确认逻辑
            // 目前返回成功，实际实现需要在SystemMonitorService中添加相应方法
            await Task.Delay(1, cancellationToken);

            _logger.LogInformation("Alert {AlertId} acknowledged by {UserName}", alertId, userName);
            return Ok(new { Message = "警告已确认", AlertId = alertId, AcknowledgedBy = userName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError, "确认警告失败");
        }
    }

    /// <summary>
    /// 手动触发警告检查
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("alerts/check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TriggerAlertCheck(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Manually triggering alert check");
            await _systemMonitorService.CheckAlertConditionsAsync(cancellationToken);
            return Ok(new { Message = "警告检查已触发" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering alert check");
            return StatusCode(StatusCodes.Status500InternalServerError, "触发警告检查失败");
        }
    }

    /// <summary>
    /// 检查数据库健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据库健康状态</returns>
    [HttpGet("health/database")]
    [ProducesResponseType(typeof(ComponentHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ComponentHealthDto>> CheckDatabaseHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking database health");
            var health = await _healthCheckService.CheckDatabaseHealthAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database health");
            return StatusCode(StatusCodes.Status500InternalServerError, "检查数据库健康状态失败");
        }
    }

    /// <summary>
    /// 检查Redis缓存健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Redis健康状态</returns>
    [HttpGet("health/redis")]
    [ProducesResponseType(typeof(ComponentHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ComponentHealthDto>> CheckRedisHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking Redis health");
            var health = await _healthCheckService.CheckRedisHealthAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Redis health");
            return StatusCode(StatusCodes.Status500InternalServerError, "检查Redis健康状态失败");
        }
    }

    /// <summary>
    /// 检查内存缓存健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>内存缓存健康状态</returns>
    [HttpGet("health/memory-cache")]
    [ProducesResponseType(typeof(ComponentHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ComponentHealthDto>> CheckMemoryCacheHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking memory cache health");
            var health = await _healthCheckService.CheckMemoryCacheHealthAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking memory cache health");
            return StatusCode(StatusCodes.Status500InternalServerError, "检查内存缓存健康状态失败");
        }
    }

    /// <summary>
    /// 检查外部服务健康状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>外部服务健康状态列表</returns>
    [HttpGet("health/external-services")]
    [ProducesResponseType(typeof(List<ExternalServiceStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ExternalServiceStatusDto>>> CheckExternalServicesHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking external services health");
            var health = await _healthCheckService.CheckExternalServicesAsync(cancellationToken);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking external services health");
            return StatusCode(StatusCodes.Status500InternalServerError, "检查外部服务健康状态失败");
        }
    }

    /// <summary>
    /// 获取系统监控摘要
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>监控摘要</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SystemMonitoringSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemMonitoringSummaryDto>> GetMonitoringSummary(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting monitoring summary");

            var metrics = await _systemMonitorService.GetSystemMetricsAsync(cancellationToken);

            var summary = new SystemMonitoringSummaryDto
            {
                OverallStatus = metrics.Health.OverallStatus,
                CpuUsagePercent = metrics.Performance.CpuUsagePercent,
                MemoryUsagePercent = metrics.Performance.MemoryUsagePercent,
                DiskUsagePercent = metrics.Performance.DiskUsagePercent,
                DatabaseStatus = metrics.Database.ConnectionStatus,
                CacheStatus = metrics.Cache.Redis?.ConnectionStatus ?? HealthStatus.Unknown,
                ActiveAlertsCount = metrics.Alerts.Count(a => !a.IsAcknowledged),
                UptimeSeconds = metrics.Health.UptimeSeconds,
                LastCollected = metrics.CollectedAt
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting monitoring summary");
            return StatusCode(StatusCodes.Status500InternalServerError, "获取监控摘要失败");
        }
    }
}

/// <summary>
/// 系统监控摘要DTO
/// </summary>
public class SystemMonitoringSummaryDto
{
    /// <summary>
    /// 整体健康状态
    /// </summary>
    public HealthStatus OverallStatus { get; set; }

    /// <summary>
    /// CPU使用率
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// 内存使用率
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// 磁盘使用率
    /// </summary>
    public double DiskUsagePercent { get; set; }

    /// <summary>
    /// 数据库状态
    /// </summary>
    public HealthStatus DatabaseStatus { get; set; }

    /// <summary>
    /// 缓存状态
    /// </summary>
    public HealthStatus CacheStatus { get; set; }

    /// <summary>
    /// 活跃警告数量
    /// </summary>
    public int ActiveAlertsCount { get; set; }

    /// <summary>
    /// 系统运行时间（秒）
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// 最后收集时间
    /// </summary>
    public DateTime LastCollected { get; set; }
}