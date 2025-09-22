using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Services;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Extensions;
using MapleBlog.Domain.Interfaces;
using System.Security.Claims;

namespace MapleBlog.Infrastructure.Filters;

/// <summary>
/// 数据权限过滤器
/// 自动应用基于用户角色的数据访问控制
/// </summary>
public class DataPermissionFilter : IAsyncActionFilter
{
    private readonly ILogger<DataPermissionFilter> _logger;

    public DataPermissionFilter(ILogger<DataPermissionFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            // 获取当前用户信息
            var currentUser = GetCurrentUser(context);
            if (currentUser == null)
            {
                _logger.LogWarning("Data permission filter: No authenticated user found");
                context.Result = new UnauthorizedResult();
                return;
            }

            // 将用户信息添加到请求上下文中，供服务层使用
            context.HttpContext.Items["CurrentUserId"] = currentUser.UserId;
            context.HttpContext.Items["CurrentUserRole"] = currentUser.UserRole;

            // 检查是否需要应用数据权限
            if (ShouldApplyDataPermission(context))
            {
                // 获取数据权限服务
                var dataPermissionService = context.HttpContext.RequestServices
                    .GetRequiredService<IDataPermissionService>();

                // 获取用户的数据权限范围
                var dataScope = await dataPermissionService.GetUserDataScopeAsync(currentUser.UserId);

                if (!dataScope.HasAccess)
                {
                    _logger.LogWarning("Data permission filter: User {UserId} does not have data access",
                        currentUser.UserId);
                    context.Result = new ForbidResult();
                    return;
                }

                // 将数据权限范围添加到上下文中
                context.HttpContext.Items["DataPermissionScope"] = dataScope;

                _logger.LogDebug("Data permission filter applied for user {UserId} with role {UserRole}",
                    currentUser.UserId, currentUser.UserRole);
            }

            // 继续执行操作
            var executedContext = await next();

            // 在操作执行后应用数据脱敏
            await ApplyDataMaskingAsync(executedContext, currentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data permission filter");
            context.Result = new StatusCodeResult(500);
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    private CurrentUser? GetCurrentUser(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        if (!Enum.TryParse<UserRole>(roleClaim, true, out var userRole))
        {
            userRole = UserRole.User; // 默认角色
        }

        return new CurrentUser
        {
            UserId = userId,
            UserRole = userRole,
            UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty
        };
    }

    /// <summary>
    /// 检查是否需要应用数据权限
    /// </summary>
    private bool ShouldApplyDataPermission(ActionExecutingContext context)
    {
        // 检查控制器或操作是否标记了跳过数据权限的特性
        var skipDataPermissionAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<SkipDataPermissionAttribute>().FirstOrDefault();

        if (skipDataPermissionAttribute != null)
        {
            _logger.LogDebug("Skipping data permission for action {Action} due to SkipDataPermission attribute",
                context.ActionDescriptor.DisplayName);
            return false;
        }

        // 检查是否为管理员API
        var controllerName = context.RouteData.Values["controller"]?.ToString()?.ToLowerInvariant();
        var isAdminController = controllerName?.Contains("admin") ?? false;

        // 对管理员API默认应用数据权限
        return isAdminController || HasDataPermissionAttribute(context);
    }

    /// <summary>
    /// 检查是否有数据权限特性
    /// </summary>
    private bool HasDataPermissionAttribute(ActionExecutingContext context)
    {
        return context.ActionDescriptor.EndpointMetadata
            .OfType<RequireDataPermissionAttribute>().Any();
    }

    /// <summary>
    /// 应用数据脱敏
    /// </summary>
    private async Task ApplyDataMaskingAsync(ActionExecutedContext context, CurrentUser currentUser)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            try
            {
                var dataPermissionService = context.HttpContext.RequestServices
                    .GetRequiredService<IDataPermissionService>();

                // 对返回的数据应用脱敏
                var maskedData = dataPermissionService.ApplyDataMasking(objectResult.Value, currentUser.UserRole);
                objectResult.Value = maskedData;

                _logger.LogDebug("Data masking applied for user {UserId} with role {UserRole}",
                    currentUser.UserId, currentUser.UserRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying data masking for user {UserId}", currentUser.UserId);
                // 不影响正常响应，只记录错误
            }
        }
    }

    /// <summary>
    /// 当前用户信息
    /// </summary>
    private class CurrentUser
    {
        public Guid UserId { get; set; }
        public UserRole UserRole { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}

/// <summary>
/// 要求数据权限检查的特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireDataPermissionAttribute : Attribute
{
    public string? Resource { get; set; }
    public string? Action { get; set; }

    public RequireDataPermissionAttribute() { }

    public RequireDataPermissionAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }
}

/// <summary>
/// 跳过数据权限检查的特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SkipDataPermissionAttribute : Attribute
{
    public string? Reason { get; set; }

    public SkipDataPermissionAttribute() { }

    public SkipDataPermissionAttribute(string reason)
    {
        Reason = reason;
    }
}

/// <summary>
/// 数据权限上下文扩展方法
/// </summary>
public static class DataPermissionContextExtensions
{
    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    public static Guid? GetCurrentUserId(this Microsoft.AspNetCore.Http.HttpContext context)
    {
        return context.Items["CurrentUserId"] as Guid?;
    }

    /// <summary>
    /// 获取当前用户角色
    /// </summary>
    public static UserRole? GetCurrentUserRole(this Microsoft.AspNetCore.Http.HttpContext context)
    {
        return context.Items["CurrentUserRole"] as UserRole?;
    }

    /// <summary>
    /// 获取数据权限范围
    /// </summary>
    public static MapleBlog.Domain.Enums.DataPermissionScope? GetDataPermissionScope(this Microsoft.AspNetCore.Http.HttpContext context)
    {
        return context.Items["DataPermissionScope"] as MapleBlog.Domain.Enums.DataPermissionScope?;
    }

    /// <summary>
    /// 检查当前用户是否有指定权限
    /// </summary>
    public static bool HasPermission(this Microsoft.AspNetCore.Http.HttpContext context, string resource, string action)
    {
        var scope = context.GetDataPermissionScope();
        if (scope == null) return false;

        // 管理员拥有所有权限
        if (scope.Value.CanAccessAllData()) return true;

        // 基于资源和操作的权限检查
        return CheckResourcePermission(scope.Value, resource, action);
    }

    /// <summary>
    /// 检查资源权限
    /// </summary>
    private static bool CheckResourcePermission(MapleBlog.Domain.Enums.DataPermissionScope scope, string resource, string action)
    {
        return resource.ToLowerInvariant() switch
        {
            "users" => action.ToLowerInvariant() switch
            {
                "read" => scope.CanAccessAllUsers() || scope.CanAccessPublicUsers(),
                "create" or "update" or "delete" => scope.CanAccessAllUsers(),
                _ => false
            },
            "posts" => action.ToLowerInvariant() switch
            {
                "read" => scope.CanAccessAllPosts() || scope.CanAccessPublishedPosts(),
                "create" or "update" or "delete" => scope.CanAccessAllPosts() || scope.CanAccessOwnPosts(),
                _ => false
            },
            "comments" => action.ToLowerInvariant() switch
            {
                "read" => scope.CanAccessAllComments() || scope.CanAccessRelatedComments(),
                "create" or "update" or "delete" => scope.CanAccessAllComments() || scope.CanAccessOwnComments(),
                _ => false
            },
            "system" => scope.CanManageSystem(),
            _ => false
        };
    }
}