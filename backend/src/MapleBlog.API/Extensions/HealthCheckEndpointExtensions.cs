using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MapleBlog.Infrastructure.Services;

namespace MapleBlog.API.Extensions;

/// <summary>
/// 健康检查端点扩展
/// </summary>
public static class HealthCheckEndpointExtensions
{
    /// <summary>
    /// 配置健康检查端点
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        var configuration = app.Configuration;
        var environment = app.Environment;

        // 详细健康检查端点
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse,
            AllowCachingResponses = false
        });

        // 就绪检查端点（用于Kubernetes readiness probe）
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteReadinessResponse,
            AllowCachingResponses = false
        });

        // 存活检查端点（用于Kubernetes liveness probe）
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = HealthCheckResponseWriter.WriteLivenessResponse,
            AllowCachingResponses = false
        });

        // 简单健康检查端点
        app.MapHealthChecks("/health/simple", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteSimpleResponse,
            AllowCachingResponses = true
        });

        // Redis特定健康检查端点
        app.MapHealthChecks("/health/redis", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("redis"),
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse,
            AllowCachingResponses = false
        });

        // 数据库健康检查端点
        app.MapHealthChecks("/health/database", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db"),
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse,
            AllowCachingResponses = false
        });

        // 监控健康检查端点（包含详细指标）
        app.MapHealthChecks("/health/monitoring", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("monitoring"),
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse,
            AllowCachingResponses = false
        });

        // 健康检查UI（仅在开发环境）
        if (environment.IsDevelopment())
        {
            try
            {
                app.MapHealthChecksUI(setup =>
                {
                    setup.UIPath = "/health-ui";
                    setup.ApiPath = "/health-ui-api";
                });
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Failed to configure Health Checks UI. Feature may not be available.");
            }
        }

        return app;
    }

    /// <summary>
    /// 配置带安全性的健康检查端点（生产环境使用）
    /// </summary>
    public static WebApplication MapSecureHealthCheckEndpoints(this WebApplication app)
    {
        var environment = app.Environment;

        if (environment.IsProduction())
        {
            // 生产环境只公开基本的健康检查
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteSimpleResponse,
                AllowCachingResponses = false
            });

            // 内部监控端点（可能需要额外的认证）
            app.MapHealthChecks("/internal/health", new HealthCheckOptions
            {
                ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse,
                AllowCachingResponses = false
            });

            // Kubernetes健康检查端点
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = HealthCheckResponseWriter.WriteReadinessResponse,
                AllowCachingResponses = false
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live"),
                ResponseWriter = HealthCheckResponseWriter.WriteLivenessResponse,
                AllowCachingResponses = false
            });
        }
        else
        {
            // 非生产环境使用完整的端点配置
            app.MapHealthCheckEndpoints();
        }

        return app;
    }

    /// <summary>
    /// 配置健康检查CORS策略
    /// </summary>
    public static IServiceCollection AddHealthCheckCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("HealthCheckCors", policy =>
            {
                policy.WithOrigins("*")
                      .WithMethods("GET")
                      .WithHeaders("*");
            });
        });

        return services;
    }
}