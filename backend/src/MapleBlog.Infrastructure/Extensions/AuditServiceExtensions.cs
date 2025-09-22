using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MapleBlog.Application.Interfaces;
using MapleBlog.Infrastructure.Services;
using MapleBlog.Infrastructure.Middleware;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Repositories;

namespace MapleBlog.Infrastructure.Extensions
{
    /// <summary>
    /// 审计服务扩展
    /// </summary>
    public static class AuditServiceExtensions
    {
        /// <summary>
        /// 添加审计服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuditServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册审计日志服务
            services.AddScoped<IAuditLogService, AuditLogService>();

            // 注册审计日志仓储
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // 配置审计日志选项
            services.Configure<AuditLoggingOptions>(options =>
            {
                configuration.GetSection("AuditLogging").Bind(options);
            });

            // 添加审计日志配置
            services.AddSingleton<AuditLoggingOptions>(provider =>
            {
                var options = new AuditLoggingOptions();
                configuration.GetSection("AuditLogging").Bind(options);
                return options;
            });

            return services;
        }

        /// <summary>
        /// 使用审计日志中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <param name="options">配置选项</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder app, AuditLoggingOptions? options = null)
        {
            if (options != null)
            {
                return app.UseMiddleware<AuditLoggingMiddleware>(options);
            }
            else
            {
                return app.UseMiddleware<AuditLoggingMiddleware>();
            }
        }

        /// <summary>
        /// 使用完整的审计系统
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseAuditSystem(this IApplicationBuilder app)
        {
            // 添加审计日志中间件
            app.UseAuditLogging();

            return app;
        }
    }
}