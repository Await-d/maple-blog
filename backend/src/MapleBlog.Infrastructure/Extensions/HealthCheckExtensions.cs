using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Infrastructure.Services;
using StackExchange.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;

namespace MapleBlog.Infrastructure.Extensions;

/// <summary>
/// 健康检查服务扩展
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// 添加应用程序健康检查
    /// </summary>
    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var healthCheckConfig = configuration.GetSection("HealthChecks");
        var timeout = healthCheckConfig.GetValue<TimeSpan>("Timeout", TimeSpan.FromSeconds(10));
        var enableDetailedErrors = healthCheckConfig.GetValue<bool>("DetailedErrors", false);

        var healthChecksBuilder = services.AddHealthChecks();

        // 添加基本应用程序健康检查
        healthChecksBuilder.AddCheck("self", () => HealthCheckResult.Healthy("Application is running"),
            tags: new[] { "live", "self" });

        // 添加数据库健康检查
        // Note: AddDbContextCheck is not available in .NET 10 RC, using custom check
        healthChecksBuilder.AddCheck<DatabaseHealthCheckService>("database",
            tags: new[] { "ready", "db", "database" });

        // 添加Redis健康检查（如果配置了Redis）
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            AddRedisHealthChecks(services, healthChecksBuilder, redisConnectionString, configuration);
        }

        // 配置健康检查选项
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            options.Registrations.ToList().ForEach(registration =>
            {
                registration.Timeout = timeout;
            });
        });

        return services;
    }

    private static void AddRedisHealthChecks(
        IServiceCollection services,
        IHealthChecksBuilder healthChecksBuilder,
        string connectionString,
        IConfiguration configuration)
    {
        // 配置Redis健康检查选项
        services.Configure<RedisHealthCheckOptions>(configuration.GetSection("Redis:HealthCheck"));
        services.Configure<RedisMonitoringOptions>(configuration.GetSection("Redis:Monitoring"));
        services.Configure<RedisPrometheusOptions>(configuration.GetSection("Redis:Prometheus"));

        // 基本Redis连接检查
        healthChecksBuilder.AddRedis(connectionString, "redis-basic",
            tags: new[] { "ready", "redis", "basic", "cache" });

        // 详细Redis健康检查
        healthChecksBuilder.AddCheck<RedisHealthCheckService>("redis-detailed",
            tags: new[] { "ready", "redis", "detailed" });

        // 增强Redis监控检查（仅在生产环境或启用监控时）
        var enableEnhancedMonitoring = configuration.GetValue<bool>("Redis:Monitoring:Enabled", true);
        if (enableEnhancedMonitoring)
        {
            healthChecksBuilder.AddCheck<EnhancedRedisHealthCheckService>("redis-enhanced",
                tags: new[] { "ready", "redis", "enhanced", "monitoring" });

            // 注册Prometheus指标服务
            var enablePrometheusMetrics = configuration.GetValue<bool>("Redis:Prometheus:Enabled", true);
            if (enablePrometheusMetrics)
            {
                services.AddSingleton<IRedisPrometheusMetricsService, RedisPrometheusMetricsService>();
                services.AddHostedService<RedisPrometheusMetricsService>(provider =>
                    (RedisPrometheusMetricsService)provider.GetRequiredService<IRedisPrometheusMetricsService>());
            }
        }
    }

    /// <summary>
    /// 添加健康检查UI（仅在开发环境）
    /// </summary>
    public static IServiceCollection AddHealthCheckUI(this IServiceCollection services, IConfiguration configuration)
    {
        var uiConfig = configuration.GetSection("HealthChecks:UI");

        if (uiConfig.Exists())
        {
            services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(30);
                setup.SetMinimumSecondsBetweenFailureNotifications(60);
                setup.MaximumHistoryEntriesPerEndpoint(50);

                // 添加健康检查端点
                setup.AddHealthCheckEndpoint("Maple Blog API", "/health");
            }).AddInMemoryStorage();
        }

        return services;
    }


    /// <summary>
    /// 获取健康检查标签配置
    /// </summary>
    public static Dictionary<string, string[]> GetHealthCheckTags(IConfiguration configuration)
    {
        var tagsSection = configuration.GetSection("HealthChecks:Tags");
        var tags = new Dictionary<string, string[]>();

        foreach (var section in tagsSection.GetChildren())
        {
            var tagValues = section.Get<string[]>();
            if (tagValues != null)
            {
                tags[section.Key] = tagValues;
            }
        }

        return tags.Count > 0 ? tags : GetDefaultTags();
    }

    private static Dictionary<string, string[]> GetDefaultTags()
    {
        return new Dictionary<string, string[]>
        {
            ["Database"] = new[] { "db", "ready", "database" },
            ["Redis"] = new[] { "redis", "ready", "cache" },
            ["Self"] = new[] { "live", "self" }
        };
    }

    /// <summary>
    /// 映射健康检查端点
    /// </summary>
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 基本健康检查端点
        endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteResponse,
            AllowCachingResponses = false
        });

        // 存活检查端点（用于Kubernetes liveness probe）
        endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse,
            AllowCachingResponses = false
        });

        // 就绪检查端点（用于Kubernetes readiness probe）
        endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse,
            AllowCachingResponses = false
        });

        // Redis专用健康检查端点
        endpoints.MapHealthChecks("/health/redis", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("redis"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse,
            AllowCachingResponses = false
        });

        // 数据库专用健康检查端点
        endpoints.MapHealthChecks("/health/database", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("database"),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse,
            AllowCachingResponses = false
        });

        // 详细健康检查端点（包含所有详细信息）
        endpoints.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse,
            AllowCachingResponses = false
        });

        // Redis指标端点
        endpoints.MapGet("/health/redis/metrics", async (IRedisPrometheusMetricsService? metricsService) =>
        {
            if (metricsService == null)
            {
                return Results.NotFound("Redis metrics service not available");
            }

            try
            {
                var metrics = await metricsService.GetCurrentMetricsAsync();
                return Results.Ok(metrics);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to get Redis metrics: {ex.Message}");
            }
        }).WithTags("Health", "Redis", "Metrics");

        // 健康检查UI仪表板端点
        endpoints.MapGet("/health/ui", async (HttpContext context, HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            await HealthCheckUIService.WriteHealthCheckUI(context, report);
        }).WithTags("Health", "UI");

        return endpoints;
    }
}