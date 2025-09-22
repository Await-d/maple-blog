using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Admin.Filters
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
            var startTime = DateTime.UtcNow;
            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            // 获取用户信息
            var user = httpContext.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = user?.FindFirst(ClaimTypes.Name)?.Value;

            // 获取请求信息
            var action = $"{context.ActionDescriptor.RouteValues["controller"]}.{context.ActionDescriptor.RouteValues["action"]}";
            var method = request.Method;
            var path = request.Path;
            var queryString = request.QueryString.ToString();
            var userAgent = request.Headers["User-Agent"].FirstOrDefault();
            var clientIp = GetClientIpAddress(httpContext);

            // 获取请求参数
            object? requestData = null;
            try
            {
                if (context.ActionArguments.Any())
                {
                    requestData = context.ActionArguments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取请求参数失败");
            }

            Exception? executionException = null;
            object? responseData = null;

            try
            {
                // 执行操作
                var executedContext = await next();

                // 获取响应数据
                if (executedContext.Result is Microsoft.AspNetCore.Mvc.ObjectResult objectResult)
                {
                    responseData = objectResult.Value;
                }

                executionException = executedContext.Exception;
            }
            catch (Exception ex)
            {
                executionException = ex;
                throw;
            }
            finally
            {
                // 记录审计日志
                await LogAuditAsync(
                    userId, userName, action, method, path, queryString,
                    requestData, responseData, executionException,
                    startTime, DateTime.UtcNow, clientIp, userAgent);
            }
        }

        private async Task LogAuditAsync(
            string? userId, string? userName, string action, string method, string path, string queryString,
            object? requestData, object? responseData, Exception? exception,
            DateTime startTime, DateTime endTime, string? clientIp, string? userAgent)
        {
            try
            {
                var description = $"{method} {path}";
                if (!string.IsNullOrEmpty(queryString))
                {
                    description += queryString;
                }

                var result = exception == null ? "Success" : "Failed";
                if (exception != null)
                {
                    description += $" - Error: {exception.Message}";
                }

                var duration = (endTime - startTime).TotalMilliseconds;
                var newValues = new
                {
                    request = requestData,
                    response = responseData,
                    duration = duration,
                    timestamp = startTime,
                    result = result,
                    error = exception != null ? new
                    {
                        message = exception.Message,
                        stackTrace = exception.StackTrace,
                        type = exception.GetType().Name
                    } : null
                };

                Guid? userGuid = null;
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var parsedUserId))
                {
                    userGuid = parsedUserId;
                }

                await _auditLogService.LogUserActionAsync(
                    userGuid,
                    userName,
                    action,
                    "AdminAction",
                    path,
                    description,
                    null,
                    newValues,
                    clientIp,
                    userAgent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录审计日志失败");
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