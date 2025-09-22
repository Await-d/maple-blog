using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 数据库健康检查服务
/// </summary>
public class DatabaseHealthCheckService : IHealthCheck
{
    private readonly BlogDbContext _context;
    private readonly ILogger<DatabaseHealthCheckService> _logger;

    public DatabaseHealthCheckService(
        BlogDbContext context,
        ILogger<DatabaseHealthCheckService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // 检查数据库连接
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                _logger.LogWarning("Cannot connect to database");
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // 执行简单查询测试
            var userCount = await _context.Users.CountAsync(cancellationToken);

            stopwatch.Stop();
            var responseTime = stopwatch.ElapsedMilliseconds;

            var healthData = new Dictionary<string, object>
            {
                ["response_time_ms"] = responseTime,
                ["database_provider"] = _context.Database.ProviderName,
                ["connection_state"] = _context.Database.GetDbConnection().State.ToString(),
                ["user_count"] = userCount,
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            // 检查响应时间
            var status = responseTime > 5000 ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = status == HealthStatus.Degraded
                ? $"Database response time {responseTime}ms is slow"
                : "Database is healthy";

            _logger.LogDebug("Database health check completed. Status: {Status}, Response time: {ResponseTime}ms",
                status, responseTime);

            return new HealthCheckResult(status, description, data: healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}