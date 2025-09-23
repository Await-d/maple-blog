using MapleBlog.Admin.Filters;
using MapleBlog.Admin.Hubs;
using MapleBlog.Admin.Middleware;
using MapleBlog.Admin.Services;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.Infrastructure.Filters;
using Microsoft.Extensions.Configuration;
// Temporarily disabled due to .NET 10 compatibility issues
// using Microsoft.OpenApi.Models;
// using Swashbuckle.AspNetCore.SwaggerGen; // Removed - using Scalar instead
using AutoMapper;

namespace MapleBlog.Admin.Extensions
{
    /// <summary>
    /// 管理员服务注册扩展
    /// </summary>
    public static class AdminServiceExtensions
    {
        /// <summary>
        /// 注册管理员相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAdminServices(this IServiceCollection services)
        {
            // 注册管理员特定服务
            services.AddScoped<AdminSecurityFilter>();

            // 注册SignalR Hub
            services.AddScoped<AdminDashboardHub>();

            // 注册管理后台核心服务 - 使用Admin的接口
            services.AddScoped<MapleBlog.Admin.Services.IDashboardService, MapleBlog.Admin.Services.DashboardService>();
            // 暂时注释掉有编译错误的服务
            // services.AddScoped<IAnalyticsService, AnalyticsService>();
            // services.AddScoped<IContentManagementService, ContentManagementService>();
            // services.AddScoped<IUserManagementService, UserManagementService>();

            // 注册其他必要的服务依赖
            // if (!services.Any(s => s.ServiceType == typeof(IStatsService)))
            // {
            //     services.AddScoped<IStatsService, StatsService>();
            // }

            return services;
        }

        /// <summary>
        /// 注册权限相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddPermissionServices(this IServiceCollection services)
        {
            // 注册权限服务 - 使用完整的实现
            if (!services.Any(s => s.ServiceType == typeof(IPermissionService)))
            {
                services.AddScoped<IPermissionService, MapleBlog.Application.Services.PermissionService>();
            }

            // 注册权限相关的仓储
            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Domain.Interfaces.IPermissionRepository)))
            {
                services.AddScoped<MapleBlog.Domain.Interfaces.IPermissionRepository, MapleBlog.Infrastructure.Repositories.PermissionRepository>();
            }

            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Domain.Interfaces.IRoleRepository)))
            {
                services.AddScoped<MapleBlog.Domain.Interfaces.IRoleRepository, MapleBlog.Infrastructure.Repositories.RoleRepository>();
            }

            // 注册数据权限服务
            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Domain.Interfaces.IDataPermissionService)))
            {
                services.AddScoped<MapleBlog.Domain.Interfaces.IDataPermissionService, MapleBlog.Application.Services.DataPermissionService>();
            }

            // 注册权限规则引擎和条件解析器
            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Application.Services.Permissions.PermissionRuleEngine)))
            {
                services.AddScoped<MapleBlog.Application.Services.Permissions.PermissionRuleEngine>();
            }

            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Application.Services.Permissions.ConditionExpressionParser)))
            {
                services.AddScoped<MapleBlog.Application.Services.Permissions.ConditionExpressionParser>();
            }

            // 注册数据权限规则仓储
            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Domain.Interfaces.IDataPermissionRuleRepository)))
            {
                services.AddScoped<MapleBlog.Domain.Interfaces.IDataPermissionRuleRepository, MapleBlog.Infrastructure.Repositories.DataPermissionRuleRepository>();
            }

            // 注册临时权限仓储
            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Domain.Interfaces.ITemporaryPermissionRepository)))
            {
                services.AddScoped<MapleBlog.Domain.Interfaces.ITemporaryPermissionRepository, MapleBlog.Infrastructure.Repositories.TemporaryPermissionRepository>();
            }

            // 注册数据权限过滤器
            services.AddScoped<DataPermissionFilter>();

            return services;
        }

        /// <summary>
        /// 注册审计相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuditServices(this IServiceCollection services)
        {
            // 注册审计日志服务 - 使用Infrastructure层的完整实现
            if (!services.Any(s => s.ServiceType == typeof(IAuditLogService)))
            {
                services.AddScoped<IAuditLogService, MapleBlog.Infrastructure.Services.AuditLogService>();
            }

            // 注册审计日志仓储
            if (!services.Any(s => s.ServiceType == typeof(MapleBlog.Domain.Interfaces.IAuditLogRepository)))
            {
                services.AddScoped<MapleBlog.Domain.Interfaces.IAuditLogRepository, MapleBlog.Infrastructure.Repositories.AuditLogRepository>();
            }

            // 注册审计过滤器
            services.AddScoped<AuditActionFilter>();

            return services;
        }

        /// <summary>
        /// 添加全局审计过滤器
        /// </summary>
        /// <param name="options">MVC选项</param>
        /// <returns>MVC选项</returns>
        public static Microsoft.AspNetCore.Mvc.MvcOptions AddGlobalAuditFilter(
            this Microsoft.AspNetCore.Mvc.MvcOptions options)
        {
            // 添加全局审计过滤器
            options.Filters.Add<AuditActionFilter>();
            // 添加数据权限过滤器
            options.Filters.Add<DataPermissionFilter>();
            return options;
        }

        /// <summary>
        /// 配置Admin Swagger文档
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection ConfigureAdminSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(c =>
            {
                /* Temporarily disabled for .NET 10 compatibility
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Maple Blog Admin API",
                    Version = "v1",
                    Description = "管理后台API接口文档",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Maple Blog Team",
                        Email = "admin@mapleblog.com"
                    }
                });

                // 添加JWT认证配置
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT授权头格式: Bearer {token}",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
                */

                // 包含XML注释
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// 配置AutoMapper以避免冲突
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAdminAutoMapper(this IServiceCollection services)
        {
            // TODO: Fix AutoMapper configuration for Admin project
            // Temporarily disabled to resolve compilation issues
            /*
            // 只注册Admin相关的Mapping Profile，避免重复注册
            if (!services.Any(s => s.ServiceType == typeof(AutoMapper.IMapper)))
            {
                services.AddAutoMapper(typeof(AdminServiceExtensions).Assembly);
            }
            */
            return services;
        }

        /// <summary>
        /// 使用审计服务中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseAuditServices(this IApplicationBuilder app)
        {
            // 添加审计中间件
            app.UseMiddleware<AuditMiddleware>();
            return app;
        }

        /// <summary>
        /// 初始化权限系统
        /// </summary>
        /// <param name="app">应用程序</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public static async Task<bool> InitializePermissionSystemAsync(this WebApplication app, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

                // 初始化默认权限和角色
                return await permissionService.InitializeDefaultPermissionsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetService<ILogger<WebApplication>>();
                logger?.LogError(ex, "权限系统初始化失败");
                return false;
            }
        }
    }
}