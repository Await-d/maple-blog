using MapleBlog.Application.Interfaces;
using MapleBlog.Infrastructure.Services;
using MapleBlog.API.Filters;

namespace MapleBlog.API.Extensions
{
    /// <summary>
    /// 审计服务注册扩展
    /// </summary>
    public static class AuditServiceExtensions
    {
        /// <summary>
        /// 注册审计相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuditServices(this IServiceCollection services)
        {
            // 注册审计日志服务
            services.AddScoped<IAuditLogService, AuditLogService>();

            // 注册审计过滤器
            services.AddScoped<AuditActionFilter>();

            return services;
        }

        /// <summary>
        /// 配置审计中间件
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder UseAuditServices(this IApplicationBuilder app)
        {
            // 这里可以添加审计相关的中间件配置
            return app;
        }

        /// <summary>
        /// 添加全局审计过滤器
        /// </summary>
        /// <param name="options">MVC选项</param>
        public static void AddGlobalAuditFilter(this Microsoft.AspNetCore.Mvc.MvcOptions options)
        {
            options.Filters.Add<AuditActionFilter>();
        }
    }
}