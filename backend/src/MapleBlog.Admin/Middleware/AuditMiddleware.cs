using System.Security.Claims;
using System.Text.Json;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Admin.Middleware
{
    /// <summary>
    /// 审计中间件
    /// </summary>
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            // 只对API请求进行审计
            if (!ShouldAudit(context))
            {
                await _next(context);
                return;
            }

            var startTime = DateTime.UtcNow;
            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var requestBody = await ReadRequestBodyAsync(context);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求处理异常: {Path}", context.Request.Path);
                throw;
            }
            finally
            {
                var endTime = DateTime.UtcNow;
                var responseBodyContent = await ReadResponseBodyAsync(responseBody);

                await LogRequestAsync(context, auditLogService, requestBody, responseBodyContent, startTime, endTime);

                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }

        private static bool ShouldAudit(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // 排除健康检查、Swagger等路径
            var excludePaths = new[]
            {
                "/health",
                "/swagger",
                "/api-docs",
                "/_framework",
                "/css",
                "/js",
                "/images",
                "/favicon.ico"
            };

            if (excludePaths.Any(exclude => path?.StartsWith(exclude) == true))
            {
                return false;
            }

            // 只审计API请求
            return path?.StartsWith("/api") == true ||
                   context.Request.Headers["Content-Type"].ToString().Contains("application/json");
        }

        private static async Task<string?> ReadRequestBodyAsync(HttpContext context)
        {
            try
            {
                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                }

                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();

                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                }

                return string.IsNullOrEmpty(body) ? null : body;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<string?> ReadResponseBodyAsync(MemoryStream responseBody)
        {
            try
            {
                responseBody.Position = 0;
                using var reader = new StreamReader(responseBody, leaveOpen: true);
                var content = await reader.ReadToEndAsync();
                responseBody.Position = 0;
                return string.IsNullOrEmpty(content) ? null : content;
            }
            catch
            {
                return null;
            }
        }

        private async Task LogRequestAsync(
            HttpContext context,
            IAuditLogService auditLogService,
            string? requestBody,
            string? responseBody,
            DateTime startTime,
            DateTime endTime)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                var user = context.User;

                var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = user?.FindFirst(ClaimTypes.Name)?.Value;

                var duration = (endTime - startTime).TotalMilliseconds;
                var clientIp = GetClientIpAddress(context);
                var userAgent = request.Headers["User-Agent"].FirstOrDefault();

                var requestData = new
                {
                    method = request.Method,
                    path = request.Path.Value,
                    queryString = request.QueryString.Value,
                    headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    body = requestBody,
                    contentType = request.ContentType,
                    contentLength = request.ContentLength
                };

                var responseData = new
                {
                    statusCode = response.StatusCode,
                    contentType = response.ContentType,
                    body = responseBody,
                    headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                    duration = duration
                };

                var description = $"{request.Method} {request.Path} - {response.StatusCode}";

                Guid? userGuid = null;
                if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var parsedUserId))
                {
                    userGuid = parsedUserId;
                }

                await auditLogService.LogUserActionAsync(
                    userGuid,
                    userName,
                    "HttpRequest",
                    "ApiRequest",
                    request.Path,
                    description,
                    requestData,
                    responseData,
                    clientIp,
                    userAgent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录HTTP请求审计日志失败");
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