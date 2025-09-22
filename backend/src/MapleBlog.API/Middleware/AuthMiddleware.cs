using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Middleware
{
    /// <summary>
    /// 认证中间件 - 简化的JWT令牌验证和用户上下文设置
    /// </summary>
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtService _jwtService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<AuthMiddleware> _logger;

        public AuthMiddleware(
            RequestDelegate next,
            IJwtService jwtService,
            IDistributedCache cache,
            ILogger<AuthMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 提取并验证JWT令牌
                var token = ExtractTokenFromRequest(context);

                if (!string.IsNullOrEmpty(token))
                {
                    // 检查令牌黑名单
                    if (await IsTokenBlacklistedAsync(token))
                    {
                        _logger.LogWarning("Attempt to use blacklisted token: {TokenPrefix}",
                            token.Substring(0, Math.Min(10, token.Length)) + "...");
                        await HandleUnauthorizedAsync(context, "Token has been revoked");
                        return;
                    }

                    // 验证令牌并设置用户上下文
                    var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(token);
                    if (principal != null)
                    {
                        context.User = principal;

                        // 记录成功的认证
                        var userId = principal.FindFirst("sub")?.Value ?? principal.FindFirst("userId")?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            _logger.LogDebug("User {UserId} authenticated successfully", userId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid token provided: {TokenPrefix}",
                            token.Substring(0, Math.Min(10, token.Length)) + "...");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authentication middleware");
                // 继续处理请求，让后续的认证/授权逻辑处理
            }

            await _next(context);
        }

        /// <summary>
        /// 从请求中提取JWT令牌
        /// </summary>
        private static string? ExtractTokenFromRequest(HttpContext context)
        {
            // 从Authorization header提取
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..];
            }

            // 从Cookie提取
            if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
                return cookieToken;
            }

            // 从Query参数提取（主要用于WebSocket连接）
            if (context.Request.Query.TryGetValue("access_token", out var queryToken))
            {
                return queryToken;
            }

            return null;
        }

        /// <summary>
        /// 检查令牌是否在黑名单中
        /// </summary>
        private async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            try
            {
                var jti = await ExtractJtiFromTokenAsync(token);
                if (string.IsNullOrEmpty(jti))
                    return false;

                var blacklistKey = $"auth:blacklist:{jti}";
                var isBlacklisted = await _cache.GetStringAsync(blacklistKey);
                return !string.IsNullOrEmpty(isBlacklisted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token blacklist");
                return false; // 在错误情况下，不阻止访问
            }
        }

        /// <summary>
        /// 从令牌中提取JTI（JWT ID）
        /// </summary>
        private async Task<string?> ExtractJtiFromTokenAsync(string token)
        {
            try
            {
                var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(token);
                return principal?.FindFirst("jti")?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 处理未授权访问
        /// </summary>
        private static async Task HandleUnauthorizedAsync(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message,
                statusCode = 401,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }

    /// <summary>
    /// AuthMiddleware扩展方法
    /// </summary>
    public static class AuthMiddlewareExtensions
    {
        /// <summary>
        /// 添加认证中间件到请求管道
        /// </summary>
        public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthMiddleware>();
        }
    }
}