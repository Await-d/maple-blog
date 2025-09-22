using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// 管理后台控制器基类
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public abstract class BaseAdminController : ControllerBase
    {
        protected readonly ILogger Logger;
        protected readonly IPermissionService PermissionService;
        protected readonly IAuditLogService AuditLogService;

        protected BaseAdminController(
            ILogger logger,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            PermissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            AuditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        protected Guid? CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
            }
        }

        /// <summary>
        /// 获取当前用户名
        /// </summary>
        protected string? CurrentUserName => User.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>
        /// 获取当前用户邮箱
        /// </summary>
        protected string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        protected string? ClientIpAddress
        {
            get
            {
                var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = ipAddress.Split(',')[0].Trim();
                }

                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
                }

                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                }

                return ipAddress;
            }
        }

        /// <summary>
        /// 获取用户代理信息
        /// </summary>
        protected string? UserAgent => Request.Headers["User-Agent"].FirstOrDefault();

        /// <summary>
        /// 检查当前用户是否有指定权限
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>是否有权限</returns>
        protected async Task<bool> HasPermissionAsync(string permission)
        {
            if (CurrentUserId == null) return false;
            return await PermissionService.HasPermissionAsync(CurrentUserId.Value, permission);
        }

        /// <summary>
        /// 检查当前用户是否有资源权限
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <returns>是否有权限</returns>
        protected async Task<bool> HasResourcePermissionAsync(string resource, string action)
        {
            if (CurrentUserId == null) return false;
            return await PermissionService.HasResourcePermissionAsync(CurrentUserId.Value, resource, action);
        }

        /// <summary>
        /// 验证权限，无权限时返回403
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>权限验证结果</returns>
        protected async Task<IActionResult?> ValidatePermissionAsync(string permission)
        {
            if (!await HasPermissionAsync(permission))
            {
                Logger.LogWarning("用户 {UserId} 访问权限 {Permission} 被拒绝", CurrentUserId, permission);
                return Forbid($"缺少权限: {permission}");
            }
            return null;
        }

        /// <summary>
        /// 验证资源权限，无权限时返回403
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <returns>权限验证结果</returns>
        protected async Task<IActionResult?> ValidateResourcePermissionAsync(string resource, string action)
        {
            if (!await HasResourcePermissionAsync(resource, action))
            {
                Logger.LogWarning("用户 {UserId} 访问资源权限 {Resource}.{Action} 被拒绝",
                    CurrentUserId, resource, action);
                return Forbid($"缺少权限: {resource}.{action}");
            }
            return null;
        }

        /// <summary>
        /// 记录审计日志
        /// </summary>
        /// <param name="action">操作类型</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="description">描述</param>
        /// <param name="oldValues">变更前数据</param>
        /// <param name="newValues">变更后数据</param>
        protected async Task LogAuditAsync(
            string action,
            string resourceType,
            string? resourceId = null,
            string? description = null,
            object? oldValues = null,
            object? newValues = null)
        {
            try
            {
                await AuditLogService.LogUserActionAsync(
                    CurrentUserId,
                    CurrentUserName,
                    action,
                    resourceType,
                    resourceId,
                    description,
                    oldValues,
                    newValues,
                    ClientIpAddress,
                    UserAgent
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "记录审计日志失败: {Action} {ResourceType}", action, resourceType);
                // 不抛出异常，避免影响主要业务流程
            }
        }

        /// <summary>
        /// 返回成功结果
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="message">消息</param>
        /// <returns>成功结果</returns>
        protected IActionResult Success(object? data = null, string message = "操作成功")
        {
            return Ok(new
            {
                success = true,
                message = message,
                data = data,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 返回分页成功结果
        /// </summary>
        /// <param name="data">数据列表</param>
        /// <param name="total">总数量</param>
        /// <param name="pageNumber">页号</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="message">消息</param>
        /// <returns>分页成功结果</returns>
        protected IActionResult SuccessWithPagination<T>(
            IEnumerable<T> data,
            int total,
            int pageNumber,
            int pageSize,
            string message = "查询成功")
        {
            return Ok(new
            {
                success = true,
                message = message,
                data = data,
                pagination = new
                {
                    total = total,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)total / pageSize)
                },
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 返回错误结果
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="statusCode">状态码</param>
        /// <returns>错误结果</returns>
        protected IActionResult Error(string message, int statusCode = 400)
        {
            Logger.LogWarning("API错误: {Message} (状态码: {StatusCode})", message, statusCode);

            return StatusCode(statusCode, new
            {
                success = false,
                message = message,
                statusCode = statusCode,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 返回验证错误结果
        /// </summary>
        /// <param name="errors">验证错误信息</param>
        /// <returns>验证错误结果</returns>
        protected IActionResult ValidationError(Dictionary<string, string[]> errors)
        {
            Logger.LogWarning("验证错误: {Errors}", string.Join(", ",
                errors.SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}"))));

            return BadRequest(new
            {
                success = false,
                message = "数据验证失败",
                errors = errors,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 返回未找到结果
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="resourceId">资源ID</param>
        /// <returns>未找到结果</returns>
        protected IActionResult NotFoundResult(string resourceType, object? resourceId = null)
        {
            var message = resourceId != null
                ? $"{resourceType} (ID: {resourceId}) 未找到"
                : $"{resourceType} 未找到";

            return NotFound(new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 处理异常并返回适当的错误响应
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="action">操作名称</param>
        /// <returns>错误响应</returns>
        protected IActionResult HandleException(Exception ex, string action)
        {
            Logger.LogError(ex, "{Action} 操作失败: {Message}", action, ex.Message);

            // 在开发环境中返回详细错误信息
            if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"{action} 操作失败",
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.UtcNow
                });
            }

            return StatusCode(500, new
            {
                success = false,
                message = $"{action} 操作失败，请稍后重试",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 验证模型状态
        /// </summary>
        /// <returns>验证结果</returns>
        protected IActionResult? ValidateModelState()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                return ValidationError(errors);
            }

            return null;
        }

        /// <summary>
        /// 检查是否为超级管理员
        /// </summary>
        /// <returns>是否为超级管理员</returns>
        protected bool IsSuperAdmin()
        {
            return User.IsInRole("SuperAdmin");
        }

        /// <summary>
        /// 验证超级管理员权限
        /// </summary>
        /// <returns>权限验证结果</returns>
        protected IActionResult? ValidateSuperAdminPermission()
        {
            if (!IsSuperAdmin())
            {
                Logger.LogWarning("用户 {UserId} 尝试访问超级管理员功能被拒绝", CurrentUserId);
                return Forbid("此操作需要超级管理员权限");
            }
            return null;
        }
    }
}