using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.Application.Services.Permissions;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Repositories;

namespace MapleBlog.API.Extensions
{
    /// <summary>
    /// 权限服务注册扩展
    /// </summary>
    public static class PermissionServiceExtensions
    {
        /// <summary>
        /// 注册权限相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddPermissionServices(this IServiceCollection services)
        {
            // 注册权限相关仓储
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IUserRoleRepository, UserRoleRepository>();
            services.AddScoped<IDataPermissionRuleRepository, DataPermissionRuleRepository>();
            services.AddScoped<ITemporaryPermissionRepository, TemporaryPermissionRepository>();

            // 注册权限服务
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IDataPermissionService, DataPermissionService>();
            // services.AddScoped<PermissionRuleEngine>();
            // services.AddScoped<ConditionExpressionParser>();
            // services.AddScoped<PermissionAuditService>();

            return services;
        }

        /// <summary>
        /// 初始化权限系统
        /// </summary>
        /// <param name="app">应用程序构建器</param>
        /// <returns>应用程序构建器</returns>
        public static async Task<IApplicationBuilder> InitializePermissionSystemAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<PermissionService>>();

            try
            {
                logger.LogInformation("开始初始化权限系统...");
                await permissionService.InitializeDefaultPermissionsAsync();
                logger.LogInformation("权限系统初始化完成");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "权限系统初始化失败");
                throw;
            }

            return app;
        }
    }
}