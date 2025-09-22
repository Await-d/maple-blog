using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Text.Json;

namespace MapleBlog.API.Middleware
{
    /// <summary>
    /// 速率限制中间件
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDistributedCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitingSettings _settings;

        public RateLimitingMiddleware(
            RequestDelegate next,
            IDistributedCache cache,
            ILogger<RateLimitingMiddleware> logger,
            RateLimitingSettings settings)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _settings = settings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 获取客户端标识
            var clientId = GetClientIdentifier(context);
            var endpoint = GetEndpointIdentifier(context);

            // 检查是否需要速率限制
            var rateLimitRule = GetRateLimitRule(endpoint);
            if (rateLimitRule == null)
            {
                await _next(context);
                return;
            }

            // 检查速率限制
            var rateLimitResult = await CheckRateLimitAsync(clientId, endpoint, rateLimitRule);

            if (rateLimitResult.IsLimited)
            {
                await HandleRateLimitExceededAsync(context, rateLimitResult);
                return;
            }

            // 添加速率限制头信息
            AddRateLimitHeaders(context, rateLimitResult);

            await _next(context);
        }

        /// <summary>
        /// 获取客户端标识符
        /// </summary>
        private string GetClientIdentifier(HttpContext context)
        {
            // 优先使用认证用户ID
            var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst("userId")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            // 使用IP地址
            var ip = GetClientIpAddress(context);
            return $"ip:{ip}";
        }

        /// <summary>
        /// 获取端点标识符
        /// </summary>
        private string GetEndpointIdentifier(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            var method = context.Request.Method.ToUpperInvariant();
            return $"{method}:{path}";
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 获取速率限制规则
        /// </summary>
        private RateLimitRule? GetRateLimitRule(string endpoint)
        {
            // 认证相关端点
            if (endpoint.Contains("/api/auth/login") || endpoint.Contains("/api/auth/register"))
            {
                return _settings.AuthEndpoints;
            }

            // 邮件相关端点
            if (endpoint.Contains("/api/auth/send-email-verification") ||
                endpoint.Contains("/api/auth/forgot-password"))
            {
                return _settings.EmailEndpoints;
            }

            // API端点
            if (endpoint.StartsWith("get:/api/") || endpoint.StartsWith("post:/api/") ||
                endpoint.StartsWith("put:/api/") || endpoint.StartsWith("delete:/api/"))
            {
                return _settings.ApiEndpoints;
            }

            return null;
        }

        /// <summary>
        /// 检查速率限制
        /// </summary>
        private async Task<RateLimitResult> CheckRateLimitAsync(string clientId, string endpoint, RateLimitRule rule)
        {
            var now = DateTimeOffset.UtcNow;
            var key = $"rate_limit:{clientId}:{endpoint}";

            try
            {
                var cachedData = await _cache.GetStringAsync(key);
                var rateLimitData = string.IsNullOrEmpty(cachedData)
                    ? new RateLimitData()
                    : JsonSerializer.Deserialize<RateLimitData>(cachedData) ?? new RateLimitData();

                // 滑动窗口算法
                var windowStart = now.AddSeconds(-rule.WindowSizeSeconds);

                // 清理过期的请求记录
                rateLimitData.Requests = rateLimitData.Requests
                    .Where(r => r > windowStart.ToUnixTimeSeconds())
                    .ToList();

                // 检查是否超过限制
                if (rateLimitData.Requests.Count >= rule.MaxRequests)
                {
                    var oldestRequest = DateTimeOffset.FromUnixTimeSeconds(rateLimitData.Requests.Min());
                    var retryAfter = oldestRequest.AddSeconds(rule.WindowSizeSeconds) - now;

                    return new RateLimitResult
                    {
                        IsLimited = true,
                        RetryAfter = retryAfter,
                        Remaining = 0,
                        Limit = rule.MaxRequests,
                        Reset = oldestRequest.AddSeconds(rule.WindowSizeSeconds)
                    };
                }

                // 添加当前请求
                rateLimitData.Requests.Add(now.ToUnixTimeSeconds());

                // 更新缓存
                var serializedData = JsonSerializer.Serialize(rateLimitData);
                await _cache.SetStringAsync(key, serializedData,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(rule.WindowSizeSeconds * 2)
                    });

                return new RateLimitResult
                {
                    IsLimited = false,
                    Remaining = rule.MaxRequests - rateLimitData.Requests.Count,
                    Limit = rule.MaxRequests,
                    Reset = now.AddSeconds(rule.WindowSizeSeconds)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for {ClientId} on {Endpoint}", clientId, endpoint);

                // 在错误情况下允许请求通过
                return new RateLimitResult
                {
                    IsLimited = false,
                    Remaining = rule.MaxRequests,
                    Limit = rule.MaxRequests,
                    Reset = now.AddSeconds(rule.WindowSizeSeconds)
                };
            }
        }

        /// <summary>
        /// 处理速率限制超出
        /// </summary>
        private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitResult result)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";

            if (result.RetryAfter.HasValue)
            {
                context.Response.Headers["Retry-After"] =
                    ((int)result.RetryAfter.Value.TotalSeconds).ToString();
            }

            AddRateLimitHeaders(context, result);

            var response = new
            {
                error = "Rate limit exceeded",
                message = "请求过于频繁，请稍后再试",
                retryAfter = result.RetryAfter?.TotalSeconds
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));

            _logger.LogWarning("Rate limit exceeded for {IP} on {Endpoint}",
                GetClientIpAddress(context),
                GetEndpointIdentifier(context));
        }

        /// <summary>
        /// 添加速率限制响应头
        /// </summary>
        private void AddRateLimitHeaders(HttpContext context, RateLimitResult result)
        {
            context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = result.Remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = result.Reset.ToUnixTimeSeconds().ToString();
        }
    }

    /// <summary>
    /// 速率限制数据
    /// </summary>
    public class RateLimitData
    {
        public List<long> Requests { get; set; } = new();
    }

    /// <summary>
    /// 速率限制结果
    /// </summary>
    public class RateLimitResult
    {
        public bool IsLimited { get; set; }
        public TimeSpan? RetryAfter { get; set; }
        public int Remaining { get; set; }
        public int Limit { get; set; }
        public DateTimeOffset Reset { get; set; }
    }

    /// <summary>
    /// 速率限制规则
    /// </summary>
    public class RateLimitRule
    {
        public int MaxRequests { get; set; }
        public int WindowSizeSeconds { get; set; }
    }

    /// <summary>
    /// 速率限制设置
    /// </summary>
    public class RateLimitingSettings
    {
        public RateLimitRule AuthEndpoints { get; set; } = new() { MaxRequests = 5, WindowSizeSeconds = 300 }; // 5次/5分钟
        public RateLimitRule EmailEndpoints { get; set; } = new() { MaxRequests = 3, WindowSizeSeconds = 3600 }; // 3次/小时
        public RateLimitRule ApiEndpoints { get; set; } = new() { MaxRequests = 100, WindowSizeSeconds = 60 }; // 100次/分钟
    }

    /// <summary>
    /// 速率限制中间件扩展
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}