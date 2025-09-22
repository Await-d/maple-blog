using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Admin.Filters
{
    /// <summary>
    /// 管理员安全过滤器
    /// </summary>
    public class AdminSecurityFilter : IAsyncActionFilter
    {
        private readonly ILogger<AdminSecurityFilter> _logger;
        private readonly IPermissionService _permissionService;

        public AdminSecurityFilter(ILogger<AdminSecurityFilter> logger, IPermissionService permissionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;

            // 验证用户是否已认证
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "用户未认证",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            // 获取用户信息
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "无效的用户身份",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            // 验证管理员权限
            var hasAdminRole = user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
            if (!hasAdminRole)
            {
                _logger.LogWarning("非管理员用户 {UserId} 尝试访问管理接口", userId);
                context.Result = new ForbidResult();
                return;
            }

            // 检查账户状态（可选，需要扩展用户模型）
            if (!await ValidateAccountStatus(userId))
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "账户状态异常",
                    timestamp = DateTime.UtcNow
                })
                {
                    StatusCode = 403
                };
                return;
            }

            // 验证会话有效性
            if (!ValidateSessionSecurity(httpContext))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "会话安全验证失败",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            // 记录访问日志
            LogAccess(httpContext, userId);

            try
            {
                // 执行操作
                await next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "管理员操作执行异常 - 用户: {UserId}, 路径: {Path}",
                    userId, httpContext.Request.Path);
                throw;
            }
        }

        /// <summary>
        /// 验证账户状态
        /// </summary>
        private async Task<bool> ValidateAccountStatus(Guid userId)
        {
            try
            {
                // 这里可以添加更多账户状态检查
                // 例如：账户是否被锁定、是否过期等
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证账户状态失败: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 验证会话安全性
        /// </summary>
        private bool ValidateSessionSecurity(HttpContext context)
        {
            try
            {
                // 检查请求来源
                var referer = context.Request.Headers["Referer"].FirstOrDefault();
                var origin = context.Request.Headers["Origin"].FirstOrDefault();

                // 验证CSRF Token（如果实现了CSRF保护）
                // 这里可以添加更多会话安全检查

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "会话安全验证异常");
                return false;
            }
        }

        /// <summary>
        /// 记录访问日志
        /// </summary>
        private void LogAccess(HttpContext context, Guid userId)
        {
            try
            {
                var request = context.Request;
                var path = request.Path.Value;
                var method = request.Method;
                var userAgent = request.Headers["User-Agent"].FirstOrDefault();
                var clientIp = GetClientIpAddress(context);

                _logger.LogDebug("管理员访问 - 用户: {UserId}, 方法: {Method}, 路径: {Path}, IP: {ClientIp}",
                    userId, method, path, clientIp);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录访问日志失败");
            }
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
    }
}