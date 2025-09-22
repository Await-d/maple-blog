using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;
using System.Security.Claims;

namespace MapleBlog.API.Attributes
{
    /// <summary>
    /// 权限验证特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;

        /// <summary>
        /// 初始化权限验证特性
        /// </summary>
        /// <param name="permission">所需权限</param>
        public RequirePermissionAttribute(string permission)
        {
            _permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }

        /// <summary>
        /// 权限验证
        /// </summary>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 检查用户是否已认证
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "用户未认证",
                    statusCode = 401
                });
                return;
            }

            // 获取用户ID
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "无效的用户身份",
                    statusCode = 401
                });
                return;
            }

            try
            {
                // 获取权限服务
                var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
                if (permissionService == null)
                {
                    context.Result = new StatusCodeResult(500);
                    return;
                }

                // 检查权限
                var hasPermission = await permissionService.HasPermissionAsync(userId, _permission);
                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequirePermissionAttribute>>();
                logger?.LogError(ex, "Error checking permission {Permission} for user {UserId}", _permission, userId);

                context.Result = new StatusCodeResult(500);
                return;
            }
        }
    }

    /// <summary>
    /// 资源权限验证特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireResourcePermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _resource;
        private readonly string _action;

        /// <summary>
        /// 初始化资源权限验证特性
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        public RequireResourcePermissionAttribute(string resource, string action)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// 权限验证
        /// </summary>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 检查用户是否已认证
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "用户未认证",
                    statusCode = 401
                });
                return;
            }

            // 获取用户ID
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "无效的用户身份",
                    statusCode = 401
                });
                return;
            }

            try
            {
                // 获取权限服务
                var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
                if (permissionService == null)
                {
                    context.Result = new StatusCodeResult(500);
                    return;
                }

                // 检查资源权限
                var hasPermission = await permissionService.HasResourcePermissionAsync(userId, _resource, _action);
                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireResourcePermissionAttribute>>();
                logger?.LogError(ex, "Error checking resource permission {Resource}.{Action} for user {UserId}",
                    _resource, _action, userId);

                context.Result = new StatusCodeResult(500);
                return;
            }
        }
    }

    /// <summary>
    /// 数据级权限验证特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireDataPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _resource;
        private readonly string _action;
        private readonly PermissionScope _requiredScope;

        /// <summary>
        /// 初始化数据级权限验证特性
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="requiredScope">所需的权限作用域</param>
        public RequireDataPermissionAttribute(string resource, string action, PermissionScope requiredScope)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _requiredScope = requiredScope;
        }

        /// <summary>
        /// 权限验证
        /// </summary>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 检查用户是否已认证
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "用户未认证",
                    statusCode = 401
                });
                return;
            }

            // 获取用户ID
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    success = false,
                    message = "无效的用户身份",
                    statusCode = 401
                });
                return;
            }

            try
            {
                // 获取权限服务
                var permissionService = context.HttpContext.RequestServices.GetService<IPermissionService>();
                if (permissionService == null)
                {
                    context.Result = new StatusCodeResult(500);
                    return;
                }

                // 检查数据级权限
                var hasPermission = await permissionService
                    .HasResourcePermissionWithScopeAsync(userId, _resource, _action, _requiredScope);

                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireDataPermissionAttribute>>();
                logger?.LogError(ex, "Error checking data permission {Resource}.{Action} with scope {Scope} for user {UserId}",
                    _resource, _action, _requiredScope, userId);

                context.Result = new StatusCodeResult(500);
                return;
            }
        }
    }
}