using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Middleware
{
    /// <summary>
    /// 权限验证中间件
    /// </summary>
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
        {
            // 检查是否需要权限验证
            if (!RequiresPermission(context))
            {
                await _next(context);
                return;
            }

            // 获取用户ID
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Permission check failed: User ID not found in claims");
                await HandleUnauthorized(context, "用户身份验证失败");
                return;
            }

            // 获取需要的权限
            var requiredPermission = GetRequiredPermission(context);
            if (string.IsNullOrEmpty(requiredPermission))
            {
                await _next(context);
                return;
            }

            try
            {
                // 检查用户权限
                var hasPermission = await permissionService.HasPermissionAsync(userId, requiredPermission);
                if (!hasPermission)
                {
                    _logger.LogWarning("Permission denied for user {UserId} on permission {Permission}",
                        userId, requiredPermission);
                    await HandleForbidden(context, $"缺少权限: {requiredPermission}");
                    return;
                }

                _logger.LogDebug("Permission granted for user {UserId} on permission {Permission}",
                    userId, requiredPermission);
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}",
                    requiredPermission, userId);
                await HandleInternalError(context, "权限验证过程中发生错误");
            }
        }

        /// <summary>
        /// 检查请求是否需要权限验证
        /// </summary>
        private static bool RequiresPermission(HttpContext context)
        {
            // 跳过健康检查和静态文件
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path == null)
                return false;

            // 跳过的路径
            var skipPaths = new[]
            {
                "/health",
                "/health/ready",
                "/health/live",
                "/swagger",
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/refresh"
            };

            foreach (var skipPath in skipPaths)
            {
                if (path.StartsWith(skipPath))
                    return false;
            }

            // 只对API路径进行权限检查
            return path.StartsWith("/api/");
        }

        /// <summary>
        /// 获取当前请求所需的权限
        /// </summary>
        private static string GetRequiredPermission(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            var method = context.Request.Method.ToUpperInvariant();

            if (path == null)
                return string.Empty;

            // 根据路径和方法确定权限
            return path switch
            {
                var p when p.StartsWith("/api/posts") => GetPostPermission(method),
                var p when p.StartsWith("/api/users") => GetUserPermission(method),
                var p when p.StartsWith("/api/roles") => GetRolePermission(method),
                var p when p.StartsWith("/api/comments") => GetCommentPermission(method),
                var p when p.StartsWith("/api/categories") => GetCategoryPermission(method),
                var p when p.StartsWith("/api/admin") => GetAdminPermission(path, method),
                _ => string.Empty
            };
        }

        private static string GetPostPermission(string method) => method switch
        {
            "GET" => "Posts.Read",
            "POST" => "Posts.Create",
            "PUT" or "PATCH" => "Posts.Update",
            "DELETE" => "Posts.Delete",
            _ => "Posts.Read"
        };

        private static string GetUserPermission(string method) => method switch
        {
            "GET" => "Users.Read",
            "POST" => "Users.Create",
            "PUT" or "PATCH" => "Users.Update",
            "DELETE" => "Users.Delete",
            _ => "Users.Read"
        };

        private static string GetRolePermission(string method) => method switch
        {
            "GET" => "Roles.Read",
            "POST" => "Roles.Create",
            "PUT" or "PATCH" => "Roles.Update",
            "DELETE" => "Roles.Delete",
            _ => "Roles.Read"
        };

        private static string GetCommentPermission(string method) => method switch
        {
            "GET" => "Comments.Read",
            "POST" => "Comments.Create",
            "PUT" or "PATCH" => "Comments.Update",
            "DELETE" => "Comments.Delete",
            _ => "Comments.Read"
        };

        private static string GetCategoryPermission(string method) => method switch
        {
            "GET" => "Categories.Read",
            "POST" => "Categories.Create",
            "PUT" or "PATCH" => "Categories.Update",
            "DELETE" => "Categories.Delete",
            _ => "Categories.Read"
        };

        private static string GetAdminPermission(string path, string method)
        {
            // 管理员API权限更加严格
            return path switch
            {
                var p when p.Contains("dashboard") => "Dashboard.View",
                var p when p.Contains("analytics") => "Analytics.View",
                var p when p.Contains("system") => "System.Admin",
                var p when p.Contains("monitor") => "System.Monitor",
                var p when p.Contains("config") => "System.Config",
                _ => "System.Admin"
            };
        }

        private static async Task HandleUnauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message,
                statusCode = 401
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private static async Task HandleForbidden(HttpContext context, string message)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message,
                statusCode = 403
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private static async Task HandleInternalError(HttpContext context, string message)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message,
                statusCode = 500
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }

    /// <summary>
    /// 权限中间件扩展方法
    /// </summary>
    public static class PermissionMiddlewareExtensions
    {
        public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PermissionMiddleware>();
        }
    }
}