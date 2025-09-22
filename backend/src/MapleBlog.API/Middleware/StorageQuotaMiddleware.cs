using System.Security.Claims;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Middleware
{
    /// <summary>
    /// 存储配额检查中间件
    /// </summary>
    public class StorageQuotaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StorageQuotaMiddleware> _logger;
        private readonly IServiceProvider _serviceProvider;

        public StorageQuotaMiddleware(
            RequestDelegate next,
            ILogger<StorageQuotaMiddleware> logger,
            IServiceProvider serviceProvider)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只检查文件上传相关的请求
            if (ShouldCheckQuota(context))
            {
                var quotaCheckResult = await CheckQuotaAsync(context);
                if (!quotaCheckResult.IsValid)
                {
                    _logger.LogWarning("File upload blocked due to quota violation: {Reason}", quotaCheckResult.Reason);

                    context.Response.StatusCode = 413; // Payload Too Large
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = "Quota exceeded",
                        message = quotaCheckResult.Reason,
                        details = new
                        {
                            currentUsage = quotaCheckResult.CurrentUsage,
                            maxQuota = quotaCheckResult.MaxQuota,
                            remainingSpace = quotaCheckResult.RemainingSpace
                        }
                    };

                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    return;
                }
            }

            await _next(context);
        }

        private static bool ShouldCheckQuota(HttpContext context)
        {
            // 检查是否为文件上传请求
            var path = context.Request.Path.Value?.ToLowerInvariant();
            var method = context.Request.Method.ToUpperInvariant();

            if (method != "POST" && method != "PUT")
                return false;

            // 检查是否为文件上传相关的端点
            return path?.Contains("/api/files/upload") == true ||
                   path?.Contains("/api/files") == true ||
                   context.Request.ContentType?.Contains("multipart/form-data") == true;
        }

        private async Task<QuotaCheckResult> CheckQuotaAsync(HttpContext context)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var storageQuotaService = scope.ServiceProvider.GetRequiredService<IStorageQuotaService>();

                // 获取用户ID
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return new QuotaCheckResult
                    {
                        IsValid = false,
                        Reason = "User not authenticated"
                    };
                }

                // 获取文件大小
                var contentLength = context.Request.ContentLength;
                if (!contentLength.HasValue)
                {
                    return new QuotaCheckResult
                    {
                        IsValid = false,
                        Reason = "Content-Length header is required"
                    };
                }

                // 检查配额
                var quotaInfo = await storageQuotaService.GetUserStorageQuotaAsync(userId, context.RequestAborted);

                if (quotaInfo.MaxQuota > 0 && (quotaInfo.CurrentUsage + contentLength.Value) > quotaInfo.MaxQuota)
                {
                    return new QuotaCheckResult
                    {
                        IsValid = false,
                        Reason = $"Upload would exceed storage quota. Available: {quotaInfo.FormattedAvailableSpace}",
                        CurrentUsage = quotaInfo.CurrentUsage,
                        MaxQuota = quotaInfo.MaxQuota,
                        RemainingSpace = quotaInfo.AvailableSpace
                    };
                }

                return new QuotaCheckResult { IsValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage quota");
                return new QuotaCheckResult
                {
                    IsValid = false,
                    Reason = "Internal error during quota check"
                };
            }
        }

        private class QuotaCheckResult
        {
            public bool IsValid { get; set; }
            public string? Reason { get; set; }
            public long CurrentUsage { get; set; }
            public long MaxQuota { get; set; }
            public long RemainingSpace { get; set; }
        }
    }

    /// <summary>
    /// 存储配额中间件扩展方法
    /// </summary>
    public static class StorageQuotaMiddlewareExtensions
    {
        /// <summary>
        /// 添加存储配额检查中间件
        /// </summary>
        /// <param name="builder">应用程序构建器</param>
        /// <returns>应用程序构建器</returns>
        public static IApplicationBuilder UseStorageQuotaCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StorageQuotaMiddleware>();
        }
    }
}