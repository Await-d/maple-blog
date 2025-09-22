// Minimal SwaggerConfiguration for .NET 10 RC compatibility
// Temporarily simplified to avoid Microsoft.OpenApi.Models namespace issues

namespace MapleBlog.Admin.Documentation
{
    /// <summary>
    /// Simplified Swagger configuration class for .NET 10 RC compatibility
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// 添加Swagger配置
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            // Simplified Swagger configuration without OpenApi types
            services.AddSwaggerGen();
            return services;
        }
    }
}