// Minimal SwaggerConfiguration for .NET 10 RC compatibility
// Temporarily simplified to avoid Microsoft.OpenApi.Models namespace issues

namespace MapleBlog.Admin.Documentation
{
    /// <summary>
    /// Simplified Swagger configuration class for .NET 10 RC compatibility
    /// </summary>
    /// <summary>
    /// OpenAPI configuration for .NET 10
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// 添加OpenAPI配置
        /// </summary>
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            // Use built-in OpenAPI support in .NET 10
            services.AddOpenApi();
            return services;
        }
    }
}