using System.Security.Claims;
using System.Text.Json;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Admin.Middleware
{
    /// <summary>
    /// 管理员安全中间件
    /// </summary>
    public class AdminSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminSecurityMiddleware> _logger;

        // 安全配置
        private readonly HashSet<string> _allowedIpRanges;
        private readonly TimeSpan _maxRequestDuration = TimeSpan.FromMinutes(5);
        private readonly Dictionary<string, DateTime> _lastRequestTimes = new();
        private readonly Dictionary<string, int> _requestCounts = new();
        private readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);
        private const int MaxRequestsPerWindow = 60;

        public AdminSecurityMiddleware(RequestDelegate next, ILogger<AdminSecurityMiddleware> logger, IConfiguration configuration)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 配置允许的IP范围（可从配置文件读取）
            _allowedIpRanges = configuration.GetSection("AdminSecurity:AllowedIpRanges")
                .Get<string[]>()?.ToHashSet() ?? new HashSet<string>();
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService, IPermissionService permissionService)
        {
            var startTime = DateTime.UtcNow;
            var path = context.Request.Path.Value?.ToLowerInvariant();

            try
            {
                // 跳过健康检查和公开端点
                if (ShouldSkipSecurity(path))
                {
                    await _next(context);
                    return;
                }

                // IP白名单检查（如果配置了）
                if (!await ValidateIpAddressAsync(context))
                {
                    await HandleSecurityViolation(context, auditLogService, "IP地址不在允许范围内", "IpRestriction");
                    return;
                }

                // 请求频率限制
                if (!await ValidateRateLimitAsync(context))
                {
                    await HandleSecurityViolation(context, auditLogService, "请求频率超出限制", "RateLimit");
                    return;
                }

                // 验证请求头安全性
                if (!ValidateSecurityHeaders(context))
                {
                    await HandleSecurityViolation(context, auditLogService, "安全头验证失败", "SecurityHeaders");
                    return;
                }

                // 验证用户权限（如果已认证）
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    if (!await ValidateUserPermissions(context, permissionService))
                    {
                        await HandleSecurityViolation(context, auditLogService, "用户权限验证失败", "PermissionDenied");
                        return;
                    }
                }

                // 添加安全响应头
                AddSecurityResponseHeaders(context);

                // 执行请求
                await _next(context);

                // 检查处理时间
                var duration = DateTime.UtcNow - startTime;
                if (duration > _maxRequestDuration)
                {
                    _logger.LogWarning("请求处理时间过长: {Duration}ms, Path: {Path}",
                        duration.TotalMilliseconds, path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "管理员安全中间件执行异常: {Path}", path);
                await HandleSecurityViolation(context, auditLogService, $"系统异常: {ex.Message}", "SystemError");
            }
        }

        /// <summary>
        /// 检查是否应跳过安全验证
        /// </summary>
        private static bool ShouldSkipSecurity(string? path)
        {
            if (path == null) return false;

            var skipPaths = new[]
            {
                "/health",
                "/swagger",
                "/api/admin/auth/login",
                "/api/admin/auth/refresh",
                "/error"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }

        /// <summary>
        /// 验证IP地址
        /// </summary>
        private async Task<bool> ValidateIpAddressAsync(HttpContext context)
        {
            // 如果没有配置IP白名单，则跳过检查
            if (!_allowedIpRanges.Any())
                return true;

            var clientIp = GetClientIpAddress(context);
            if (string.IsNullOrEmpty(clientIp))
                return false;

            // 检查是否在允许的IP范围内
            var isAllowed = _allowedIpRanges.Any(range => IsIpInRange(clientIp, range));

            if (!isAllowed)
            {
                _logger.LogWarning("未授权的IP访问尝试: {ClientIp}", clientIp);
            }

            return isAllowed;
        }

        /// <summary>
        /// 验证请求频率限制
        /// </summary>
        private async Task<bool> ValidateRateLimitAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var now = DateTime.UtcNow;

            lock (_requestCounts)
            {
                // 清理过期记录
                var expiredKeys = _lastRequestTimes
                    .Where(kvp => now - kvp.Value > _rateLimitWindow)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _lastRequestTimes.Remove(key);
                    _requestCounts.Remove(key);
                }

                // 更新当前请求计数
                if (_requestCounts.ContainsKey(clientId))
                {
                    _requestCounts[clientId]++;
                }
                else
                {
                    _requestCounts[clientId] = 1;
                }

                _lastRequestTimes[clientId] = now;

                // 检查是否超出限制
                if (_requestCounts[clientId] > MaxRequestsPerWindow)
                {
                    _logger.LogWarning("客户端 {ClientId} 请求频率超出限制: {Count}/{Limit}",
                        clientId, _requestCounts[clientId], MaxRequestsPerWindow);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证安全头
        /// </summary>
        private bool ValidateSecurityHeaders(HttpContext context)
        {
            var request = context.Request;

            // 检查必需的安全头
            var requiredHeaders = new Dictionary<string, string[]>
            {
                { "X-Requested-With", new[] { "XMLHttpRequest" } }, // 防止CSRF（可选）
                // 可以添加更多必需的安全头
            };

            foreach (var header in requiredHeaders)
            {
                if (!request.Headers.ContainsKey(header.Key))
                {
                    continue; // 这里可以根据需要决定是否强制要求
                }

                var headerValue = request.Headers[header.Key].FirstOrDefault();
                if (!header.Value.Contains(headerValue))
                {
                    _logger.LogWarning("安全头验证失败: {HeaderName} = {HeaderValue}",
                        header.Key, headerValue);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证用户权限
        /// </summary>
        private async Task<bool> ValidateUserPermissions(HttpContext context, IPermissionService permissionService)
        {
            var user = context.User;
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return false;
            }

            // 检查用户是否有管理员权限
            var hasAdminRole = user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
            if (!hasAdminRole)
            {
                _logger.LogWarning("非管理员用户尝试访问管理接口: {UserId}", userId);
                return false;
            }

            // 可以根据路径检查更细粒度的权限
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(path))
            {
                var requiredPermission = GetRequiredPermissionForPath(path);
                if (!string.IsNullOrEmpty(requiredPermission))
                {
                    var hasPermission = await permissionService.HasPermissionAsync(userId, requiredPermission);
                    if (!hasPermission)
                    {
                        _logger.LogWarning("用户 {UserId} 缺少权限 {Permission} 访问路径 {Path}",
                            userId, requiredPermission, path);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 添加安全响应头
        /// </summary>
        private static void AddSecurityResponseHeaders(HttpContext context)
        {
            var response = context.Response;
            var headers = response.Headers;

            // 防止点击劫持
            headers["X-Frame-Options"] = "DENY";

            // 防止MIME类型嗅探
            headers["X-Content-Type-Options"] = "nosniff";

            // 启用XSS保护
            headers["X-XSS-Protection"] = "1; mode=block";

            // 强制HTTPS
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            // 内容安全策略
            headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";

            // 引用策略
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // 权限策略
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        }

        /// <summary>
        /// 处理安全违规
        /// </summary>
        private async Task HandleSecurityViolation(HttpContext context, IAuditLogService auditLogService, string reason, string violationType)
        {
            var clientIp = GetClientIpAddress(context);
            var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
            var path = context.Request.Path.Value;
            var method = context.Request.Method;

            // 记录安全违规审计日志
            try
            {
                await auditLogService.LogUserActionAsync(
                    null,
                    "Anonymous",
                    "SecurityViolation",
                    violationType,
                    null,
                    $"安全违规: {reason} | Path: {path} | Method: {method}",
                    null,
                    new { reason, violationType, path, method, clientIp, userAgent },
                    clientIp,
                    userAgent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录安全违规审计日志失败");
            }

            // 返回403错误
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = "访问被拒绝",
                code = violationType,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        private static string? GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = ipAddress.Split(',')[0].Trim();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress;
        }

        /// <summary>
        /// 获取客户端标识符
        /// </summary>
        private static string GetClientIdentifier(HttpContext context)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user_{userId}";
            }

            var clientIp = GetClientIpAddress(context);
            return $"ip_{clientIp ?? "unknown"}";
        }

        /// <summary>
        /// 检查IP是否在指定范围内
        /// </summary>
        private static bool IsIpInRange(string clientIp, string ipRange)
        {
            // 简单实现，支持单个IP和CIDR格式
            if (ipRange.Contains('/'))
            {
                // CIDR格式，这里简化处理
                return clientIp.StartsWith(ipRange.Split('/')[0].Substring(0, ipRange.Split('/')[0].LastIndexOf('.')));
            }
            else
            {
                // 单个IP
                return clientIp == ipRange;
            }
        }

        /// <summary>
        /// 根据路径获取所需权限
        /// </summary>
        private static string? GetRequiredPermissionForPath(string path)
        {
            return path switch
            {
                var p when p.Contains("dashboard") => "Dashboard.View",
                var p when p.Contains("users") => "Users.Read",
                var p when p.Contains("roles") => "Roles.Read",
                var p when p.Contains("permissions") => "Roles.Read",
                var p when p.Contains("posts") => "Posts.Read",
                var p when p.Contains("comments") => "Comments.Read",
                var p when p.Contains("analytics") => "Analytics.View",
                var p when p.Contains("system") => "System.Admin",
                _ => null
            };
        }
    }
}