using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;

namespace MapleBlog.API.Filters
{
    /// <summary>
    /// 审计操作过滤器
    /// </summary>
    public class AuditActionFilter : IAsyncActionFilter
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditActionFilter> _logger;

        public AuditActionFilter(IAuditLogService auditLogService, ILogger<AuditActionFilter> logger)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var stopwatch = Stopwatch.StartNew();
            var auditLog = CreateAuditLog(context);

            ActionExecutedContext? executedContext = null;
            try
            {
                // 执行操作
                executedContext = await next();

                // 记录响应信息
                auditLog.SetResponseInfo(
                    context.HttpContext.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    executedContext.Exception == null ? "Success" : "Failed",
                    executedContext.Exception?.Message
                );

                // 如果是修改操作，记录变更数据
                await CaptureChangeData(context, executedContext, auditLog);
            }
            catch (Exception ex)
            {
                auditLog.SetResponseInfo(500, stopwatch.ElapsedMilliseconds, "Failed", ex.Message);
                _logger.LogError(ex, "操作执行异常: {Action}", auditLog.Action);
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // 异步记录审计日志，不影响主要操作流程
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _auditLogService.LogAsync(auditLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "记录审计日志失败: {Summary}", auditLog.GetSummary());
                    }
                });
            }
        }

        /// <summary>
        /// 创建审计日志对象
        /// </summary>
        private AuditLog CreateAuditLog(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var user = httpContext.User;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                CorrelationId = Guid.NewGuid(), // 可以用于关联相关操作
            };

            // 设置用户信息
            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                var userNameClaim = user.FindFirst(ClaimTypes.Name);

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    auditLog.UserId = userId;
                }

                auditLog.UserName = userNameClaim?.Value;
            }

            // 设置请求信息
            auditLog.SetRequestInfo(
                GetClientIpAddress(httpContext),
                request.Headers["User-Agent"].FirstOrDefault(),
                request.Path.Value,
                request.Method,
                httpContext.Session?.Id
            );

            // 设置操作信息
            var (action, resourceType, resourceId) = GetActionInfo(context);
            auditLog.Action = action;
            auditLog.ResourceType = resourceType;
            auditLog.ResourceId = resourceId;

            // 设置分类和风险级别
            auditLog.SetClassification(
                GetActionCategory(action, resourceType),
                GetRiskLevel(action, resourceType, request.Method),
                IsSensitiveOperation(action, resourceType)
            );

            return auditLog;
        }

        /// <summary>
        /// 获取操作信息
        /// </summary>
        private static (string action, string resourceType, string? resourceId) GetActionInfo(ActionExecutingContext context)
        {
            var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";
            var actionName = context.ActionDescriptor.RouteValues["action"] ?? "Unknown";
            var httpMethod = context.HttpContext.Request.Method;

            // 从路由参数中尝试获取资源ID
            string? resourceId = null;
            if (context.RouteData.Values.TryGetValue("id", out var idValue))
            {
                resourceId = idValue?.ToString();
            }

            // 根据HTTP方法和控制器名称确定操作类型
            var action = httpMethod switch
            {
                "GET" => actionName.ToLowerInvariant() switch
                {
                    "details" or "get" => "Read",
                    "index" or "list" => "List",
                    _ => "Read"
                },
                "POST" => actionName.ToLowerInvariant() switch
                {
                    "login" => "Login",
                    "logout" => "Logout",
                    "register" => "Register",
                    _ => "Create"
                },
                "PUT" or "PATCH" => "Update",
                "DELETE" => "Delete",
                _ => actionName
            };

            // 确定资源类型
            var resourceType = controllerName switch
            {
                "Posts" or "Blog" => "Post",
                "Users" or "User" => "User",
                "Comments" or "Comment" => "Comment",
                "Categories" or "Category" => "Category",
                "Tags" or "Tag" => "Tag",
                "Roles" or "Role" => "Role",
                "Auth" or "Authentication" => "Authentication",
                _ => controllerName
            };

            return (action, resourceType, resourceId);
        }

        /// <summary>
        /// 捕获变更数据
        /// </summary>
        private async Task CaptureChangeData(ActionExecutingContext context, ActionExecutedContext executedContext, AuditLog auditLog)
        {
            var httpMethod = context.HttpContext.Request.Method;

            // 只对修改操作记录变更数据
            if (httpMethod != "POST" && httpMethod != "PUT" && httpMethod != "PATCH" && httpMethod != "DELETE")
                return;

            try
            {
                // 记录请求参数作为新值
                if (context.ActionArguments.Any())
                {
                    var requestData = context.ActionArguments
                        .Where(arg => arg.Value != null && !IsSystemType(arg.Value.GetType()))
                        .ToDictionary(arg => arg.Key, arg => arg.Value);

                    if (requestData.Any())
                    {
                        auditLog.NewValues = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                }

                // 对于删除操作，可以尝试获取被删除的数据
                if (httpMethod == "DELETE" && !string.IsNullOrEmpty(auditLog.ResourceId))
                {
                    // 这里可以根据资源类型和ID查询原始数据
                    // 但需要在删除前调用，这里暂时记录资源ID
                    auditLog.Description = $"删除 {auditLog.ResourceType} (ID: {auditLog.ResourceId})";
                }

                // 记录响应数据（仅对创建和更新操作）
                if ((httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "PATCH") &&
                    executedContext.Result is ObjectResult objectResult &&
                    objectResult.Value != null)
                {
                    // 避免记录敏感信息
                    if (!ContainsSensitiveData(objectResult.Value))
                    {
                        auditLog.AdditionalData = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions
                        {
                            WriteIndented = false,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "捕获变更数据时出现异常");
                // 不影响主要流程，继续执行
            }
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        private static string? GetClientIpAddress(HttpContext context)
        {
            // 尝试从各种代理头中获取真实IP
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // X-Forwarded-For可能包含多个IP，取第一个
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
        /// 获取操作分类
        /// </summary>
        private static string GetActionCategory(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();

            return actionLower switch
            {
                "login" or "logout" or "register" => "Authentication",
                _ when resourceType.ToLowerInvariant().Contains("role") ||
                       resourceType.ToLowerInvariant().Contains("permission") => "Authorization",
                "create" or "update" or "delete" => "DataModification",
                _ when resourceType.ToLowerInvariant().Contains("system") ||
                       resourceType.ToLowerInvariant().Contains("config") => "SystemConfiguration",
                _ => "General"
            };
        }

        /// <summary>
        /// 获取风险级别
        /// </summary>
        private static string GetRiskLevel(string action, string resourceType, string httpMethod)
        {
            var actionLower = action.ToLowerInvariant();
            var resourceLower = resourceType.ToLowerInvariant();

            // 删除操作总是高风险
            if (actionLower == "delete" || httpMethod == "DELETE")
                return "High";

            // 认证操作
            if (actionLower is "login" or "logout" or "register")
                return "Medium";

            // 权限和角色相关操作
            if (resourceLower.Contains("role") || resourceLower.Contains("permission"))
                return actionLower == "create" || actionLower == "update" ? "High" : "Medium";

            // 用户管理操作
            if (resourceLower.Contains("user"))
                return actionLower == "create" || actionLower == "update" ? "Medium" : "Low";

            // 系统配置
            if (resourceLower.Contains("system") || resourceLower.Contains("config"))
                return "Medium";

            return "Low";
        }

        /// <summary>
        /// 检查是否为敏感操作
        /// </summary>
        private static bool IsSensitiveOperation(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();
            var resourceLower = resourceType.ToLowerInvariant();

            return actionLower is "delete" or "login" or "logout" or "register" ||
                   resourceLower.Contains("role") ||
                   resourceLower.Contains("permission") ||
                   resourceLower.Contains("system") ||
                   resourceLower.Contains("config");
        }

        /// <summary>
        /// 检查是否为系统类型
        /// </summary>
        private static bool IsSystemType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(Guid) ||
                   type.IsEnum;
        }

        /// <summary>
        /// 检查是否包含敏感数据
        /// </summary>
        private static bool ContainsSensitiveData(object value)
        {
            if (value == null) return false;

            var json = JsonSerializer.Serialize(value);
            var sensitiveFields = new[] { "password", "secret", "key", "token", "credential" };

            return sensitiveFields.Any(field =>
                json.Contains($"\"{field}\"", StringComparison.OrdinalIgnoreCase) ||
                json.Contains($"_{field}", StringComparison.OrdinalIgnoreCase));
        }
    }
}