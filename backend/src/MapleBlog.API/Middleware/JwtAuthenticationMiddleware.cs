using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Middleware
{
    /// <summary>
    /// JWT认证中间件
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IJwtService _jwtService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(
            RequestDelegate next,
            IJwtService jwtService,
            IDistributedCache cache,
            ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _jwtService = jwtService;
            _cache = cache;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 跳过不需要认证的路径
                if (ShouldSkipAuthentication(context))
                {
                    await _next(context);
                    return;
                }

                var token = ExtractTokenFromRequest(context);

                if (string.IsNullOrEmpty(token))
                {
                    // 如果没有token但需要认证，让后续的认证过程处理
                    await _next(context);
                    return;
                }

                // 检查令牌是否在黑名单中
                if (await IsTokenBlacklistedAsync(token))
                {
                    _logger.LogWarning("Blacklisted token attempted: {Token}", token);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }

                // 验证并解析JWT令牌
                var principal = await _jwtService.ValidateTokenAndGetPrincipalAsync(token);
                if (principal != null)
                {
                    // 设置用户上下文
                    context.User = principal;

                    // 可选：更新用户的最后活动时间
                    await UpdateUserLastActivityAsync(context, principal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JWT authentication middleware");
                // 不中断请求处理，让后续的认证流程处理
            }

            await _next(context);
        }

        /// <summary>
        /// 判断是否应该跳过认证
        /// </summary>
        private static bool ShouldSkipAuthentication(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            // 跳过的路径列表
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/refresh",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/auth/verify-email",
                "/swagger",
                "/health",
                "/metrics"
            };

            if (string.IsNullOrEmpty(path))
                return false;

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }

        /// <summary>
        /// 从请求中提取JWT令牌
        /// </summary>
        private static string? ExtractTokenFromRequest(HttpContext context)
        {
            // 从Authorization header提取
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                return authHeader["Bearer ".Length..];
            }

            // 从Cookie提取（如果配置为使用Cookie）
            if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
            {
                return cookieToken;
            }

            // 从Query参数提取（仅在特定情况下，如WebSocket连接）
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
                var jti = GetJtiFromToken(token);
                if (string.IsNullOrEmpty(jti))
                    return false;

                var blacklistKey = $"blacklist:token:{jti}";
                var blacklisted = await _cache.GetStringAsync(blacklistKey);
                return !string.IsNullOrEmpty(blacklisted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token blacklist for token: {Token}", token);
                return false; // 在错误情况下，不阻止访问
            }
        }

        /// <summary>
        /// 从令牌中获取JTI（JWT ID）
        /// </summary>
        private string? GetJtiFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                return jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 更新用户最后活动时间
        /// </summary>
        private async Task UpdateUserLastActivityAsync(HttpContext context, ClaimsPrincipal principal)
        {
            try
            {
                var userIdClaim = principal.FindFirst("sub") ?? principal.FindFirst("userId");
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    return;

                // 使用缓存避免频繁数据库更新
                var lastActivityKey = $"user:last_activity:{userId}";
                var lastUpdate = await _cache.GetStringAsync(lastActivityKey);

                // 如果最近5分钟内已更新，则跳过
                if (DateTime.TryParse(lastUpdate, out var lastUpdateTime) &&
                    DateTime.UtcNow.Subtract(lastUpdateTime).TotalMinutes < 5)
                {
                    return;
                }

                // 更新缓存
                await _cache.SetStringAsync(lastActivityKey, DateTime.UtcNow.ToString("O"),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });

                // 可以在这里添加异步更新数据库的逻辑
                // 例如：发送到消息队列或后台服务
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user last activity");
            }
        }
    }

    /// <summary>
    /// JWT认证中间件扩展
    /// </summary>
    public static class JwtAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtAuthenticationMiddleware>();
        }
    }
}