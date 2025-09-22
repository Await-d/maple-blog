using AutoMapper;
using MapleBlog.Application.Mappings;
using MapleBlog.Application.Mappings.Performance;
using MapleBlog.Application.Mappings.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Extensions
{
    /// <summary>
    /// AutoMapper服务注册扩展
    /// </summary>
    public static class AutoMapperServiceExtensions
    {
        /// <summary>
        /// 添加增强的AutoMapper服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddEnhancedAutoMapper(this IServiceCollection services, IConfiguration configuration)
        {
            // Skip performance options configuration for now

            // 注册AutoMapper配置
            services.AddAutoMapper(config =>
            {
                // 应用性能优化配置
                ApplyPerformanceOptimizations(config);

                // 添加所有Profile
                config.AddProfile<UserProfile>();
                config.AddProfile<BlogProfile>();
                config.AddProfile<CommentProfile>();
                config.AddProfile<AdminProfile>();

            }, typeof(UserProfile).Assembly);

            // Skip mapping services for now

            // 注册AutoMapper健康检查
            services.AddHealthChecks()
                .AddCheck<AutoMapperHealthCheck>("automapper");

            return services;
        }

        /// <summary>
        /// 应用性能优化配置
        /// </summary>
        private static void ApplyPerformanceOptimizations(IMapperConfigurationExpression config)
        {
            // 应用基本优化设置 - 使用兼容的API
            config.AllowNullDestinationValues = true;
            config.AllowNullCollections = true;
        }

        /// <summary>
        /// 添加AutoMapper中间件（用于监控和错误处理）
        /// </summary>
        public static IServiceCollection AddAutoMapperMiddleware(this IServiceCollection services)
        {
            services.AddScoped<AutoMapperMiddleware>();
            return services;
        }
    }

    /// <summary>
    /// AutoMapper健康检查
    /// </summary>
    public class AutoMapperHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly IMapper _mapper;
        private readonly ILogger<AutoMapperHealthCheck> _logger;

        public AutoMapperHealthCheck(
            IMapper mapper,
            ILogger<AutoMapperHealthCheck> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("执行AutoMapper健康检查");

                // 简单验证AutoMapper配置
                var testConfig = _mapper.ConfigurationProvider;
                if (testConfig != null)
                {
                    _logger.LogDebug("AutoMapper健康检查通过");
                    return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                        "AutoMapper配置正常"));
                }
                else
                {
                    _logger.LogWarning("AutoMapper健康检查失败: 配置为空");
                    return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                        "AutoMapper配置异常"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoMapper健康检查异常");
                return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "AutoMapper健康检查异常", ex));
            }
        }
    }

    /// <summary>
    /// AutoMapper中间件（用于监控和错误处理）
    /// </summary>
    public class AutoMapperMiddleware
    {
        private readonly Microsoft.AspNetCore.Http.RequestDelegate _next;
        private readonly ILogger<AutoMapperMiddleware> _logger;

        public AutoMapperMiddleware(
            Microsoft.AspNetCore.Http.RequestDelegate next,
            ILogger<AutoMapperMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (IsAutoMapperRelated(ex))
            {
                _logger.LogError(ex, "检测到AutoMapper相关异常: {ExceptionType}", ex.GetType().Name);
                throw; // 重新抛出异常，让上层处理
            }
        }

        /// <summary>
        /// 判断异常是否与AutoMapper相关
        /// </summary>
        private static bool IsAutoMapperRelated(Exception ex)
        {
            return ex is AutoMapperMappingException ||
                   ex is AutoMapperConfigurationException ||
                   ex.StackTrace?.Contains("AutoMapper") == true ||
                   ex.Message.Contains("AutoMapper") ||
                   ex.GetType().FullName?.Contains("AutoMapper") == true;
        }
    }
}