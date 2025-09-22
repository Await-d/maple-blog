using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Middleware
{
    /// <summary>
    /// 审计日志中间件 - 自动记录HTTP请求和响应
    /// </summary>
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;
        private readonly AuditLoggingOptions _options;

        // 敏感信息字段列表
        private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "token", "secret", "key", "authorization", "cookie",
            "x-api-key", "x-auth-token", "bearer", "refresh_token", "access_token"
        };

        // 不需要审计的路径
        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/health", "/swagger", "/favicon.ico", "/robots.txt", "/sitemap.xml"
        };

        public AuditLoggingMiddleware(
            RequestDelegate next,
            ILogger<AuditLoggingMiddleware> logger,
            AuditLoggingOptions? options = null)
        {
            _next = next;
            _logger = logger;
            _options = options ?? new AuditLoggingOptions();
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService, IUserContextService userContextService)
        {
            // 检查是否需要审计此请求
            if (!ShouldAuditRequest(context))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalResponseBodyStream = context.Response.Body;
            string? requestBody = null;
            string? responseBody = null;

            try
            {
                // 读取请求体
                if (_options.LogRequestBody && HasContentBody(context.Request))
                {
                    requestBody = await ReadRequestBodyAsync(context.Request);
                }

                // 包装响应流以捕获响应体
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // 执行下一个中间件
                await _next(context);

                stopwatch.Stop();

                // 读取响应体
                if (_options.LogResponseBody && responseBodyStream.Length > 0)
                {
                    responseBody = await ReadResponseBodyAsync(responseBodyStream);
                }

                // 复制响应到原始流
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);

                // 记录审计日志
                await LogAuditAsync(context, userContextService, auditLogService, stopwatch.ElapsedMilliseconds, requestBody, responseBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "审计日志中间件处理请求时发生错误: {Path}", context.Request.Path);

                // 记录错误的审计日志
                await LogAuditAsync(context, userContextService, auditLogService, stopwatch.ElapsedMilliseconds, requestBody, null, ex);

                // 恢复原始响应流
                context.Response.Body = originalResponseBodyStream;
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBodyStream;
            }
        }

        /// <summary>
        /// 判断是否需要审计请求
        /// </summary>
        private bool ShouldAuditRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (string.IsNullOrEmpty(path))
                return false;

            // 排除特定路径
            if (ExcludedPaths.Any(excluded => path.StartsWith(excluded)))
                return false;

            // 排除静态文件
            if (path.Contains(".") && (path.EndsWith(".css") || path.EndsWith(".js") ||
                path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".gif") ||
                path.EndsWith(".ico") || path.EndsWith(".svg") || path.EndsWith(".woff") ||
                path.EndsWith(".woff2") || path.EndsWith(".ttf") || path.EndsWith(".eot")))
                return false;

            // 只审计配置的HTTP方法
            if (_options.HttpMethodsToLog.Any() &&
                !_options.HttpMethodsToLog.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// 检查请求是否有内容体
        /// </summary>
        private static bool HasContentBody(HttpRequest request)
        {
            return request.ContentLength > 0 ||
                   request.Headers.ContainsKey("Transfer-Encoding") ||
                   request.Headers.ContainsKey("Content-Type");
        }

        /// <summary>
        /// 读取请求体
        /// </summary>
        private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
        {
            try
            {
                if (request.ContentLength > _options.MaxBodySizeToLog)
                {
                    return $"[请求体过大: {request.ContentLength} bytes]";
                }

                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                request.Body.Position = 0;

                var content = Encoding.UTF8.GetString(buffer);

                // 脱敏处理
                return SanitizeContent(content, request.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取请求体失败");
                return "[读取请求体失败]";
            }
        }

        /// <summary>
        /// 读取响应体
        /// </summary>
        private async Task<string?> ReadResponseBodyAsync(MemoryStream responseBody)
        {
            try
            {
                if (responseBody.Length > _options.MaxBodySizeToLog)
                {
                    return $"[响应体过大: {responseBody.Length} bytes]";
                }

                responseBody.Seek(0, SeekOrigin.Begin);
                var buffer = new byte[responseBody.Length];
                await responseBody.ReadAsync(buffer, 0, (int)responseBody.Length);

                var content = Encoding.UTF8.GetString(buffer);
                return SanitizeContent(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取响应体失败");
                return "[读取响应体失败]";
            }
        }

        /// <summary>
        /// 记录审计日志
        /// </summary>
        private async Task LogAuditAsync(
            HttpContext context,
            IUserContextService userContextService,
            IAuditLogService auditLogService,
            long duration,
            string? requestBody,
            string? responseBody,
            Exception? exception = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = $"{context.Request.Method}",
                    ResourceType = "HttpRequest",
                    ResourceId = context.TraceIdentifier,
                    Description = $"{context.Request.Method} {context.Request.Path}",
                    CreatedAt = DateTime.UtcNow
                };

                // 设置用户信息
                var currentUserId = userContextService.GetCurrentUserId();
                var currentUserName = userContextService.GetCurrentUserName();
                var currentUserEmail = userContextService.GetCurrentUserEmail();

                if (currentUserId.HasValue)
                {
                    auditLog.UserId = currentUserId;
                    auditLog.UserName = currentUserName;
                    auditLog.UserEmail = currentUserEmail;
                }

                // 设置请求信息
                var sessionId = context.Session?.Id ?? context.TraceIdentifier;
                auditLog.SetRequestInfo(
                    GetClientIpAddress(context),
                    context.Request.Headers["User-Agent"].FirstOrDefault(),
                    context.Request.Path + context.Request.QueryString,
                    context.Request.Method,
                    sessionId
                );

                // 设置响应信息
                var result = exception != null ? "Failed" : "Success";
                var errorMessage = exception?.Message;
                auditLog.SetResponseInfo(context.Response.StatusCode, duration, result, errorMessage);

                // 设置分类和风险级别
                var category = GetActionCategory(context.Request.Method, context.Request.Path);
                var riskLevel = GetRiskLevel(context.Request.Method, context.Request.Path, context.Response.StatusCode);
                var isSensitive = IsSensitiveRequest(context.Request.Path, context.Request.Method);
                auditLog.SetClassification(category, riskLevel, isSensitive);

                // 设置额外数据
                var additionalData = new
                {
                    RequestHeaders = SanitizeHeaders(context.Request.Headers),
                    ResponseHeaders = SanitizeHeaders(context.Response.Headers),
                    RequestBody = requestBody,
                    ResponseBody = responseBody,
                    QueryString = context.Request.QueryString.ToString(),
                    ContentType = context.Request.ContentType,
                    ResponseContentType = context.Response.ContentType,
                    UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                    Referer = context.Request.Headers["Referer"].FirstOrDefault(),
                    Exception = exception != null ? new
                    {
                        Message = exception.Message,
                        Type = exception.GetType().Name,
                        StackTrace = _options.IncludeStackTrace ? exception.StackTrace : null
                    } : null
                };

                auditLog.AdditionalData = JsonSerializer.Serialize(additionalData, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // 异步记录审计日志（不阻塞请求）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await auditLogService.LogAsync(auditLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "记录审计日志失败");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建审计日志对象失败");
            }
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        private static string? GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
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
        /// 脱敏处理内容
        /// </summary>
        private string SanitizeContent(string content, string? contentType)
        {
            if (string.IsNullOrEmpty(content) || content.Length > _options.MaxBodySizeToLog)
                return content;

            try
            {
                // JSON内容脱敏
                if (contentType?.Contains("application/json") == true)
                {
                    return SanitizeJsonContent(content);
                }

                // 表单数据脱敏
                if (contentType?.Contains("application/x-www-form-urlencoded") == true)
                {
                    return SanitizeFormContent(content);
                }

                return content;
            }
            catch
            {
                return content; // 脱敏失败时返回原内容
            }
        }

        /// <summary>
        /// 脱敏JSON内容
        /// </summary>
        private string SanitizeJsonContent(string jsonContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var sanitized = SanitizeJsonElement(doc.RootElement);
                return JsonSerializer.Serialize(sanitized);
            }
            catch
            {
                return jsonContent;
            }
        }

        /// <summary>
        /// 脱敏JSON元素
        /// </summary>
        private object SanitizeJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var value = SensitiveFields.Contains(property.Name)
                            ? "***SENSITIVE***"
                            : SanitizeJsonElement(property.Value);
                        obj[property.Name] = value;
                    }
                    return obj;

                case JsonValueKind.Array:
                    return element.EnumerateArray().Select(SanitizeJsonElement).ToArray();

                case JsonValueKind.String:
                    return element.GetString() ?? "";

                case JsonValueKind.Number:
                    return element.GetDecimal();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return element.ToString();
            }
        }

        /// <summary>
        /// 脱敏表单内容
        /// </summary>
        private string SanitizeFormContent(string formContent)
        {
            var pairs = formContent.Split('&');
            var sanitized = new List<string>();

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0];
                    var value = SensitiveFields.Contains(key) ? "***SENSITIVE***" : parts[1];
                    sanitized.Add($"{key}={value}");
                }
                else
                {
                    sanitized.Add(pair);
                }
            }

            return string.Join("&", sanitized);
        }

        /// <summary>
        /// 脱敏HTTP头
        /// </summary>
        private Dictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
        {
            var sanitized = new Dictionary<string, string>();

            foreach (var header in headers)
            {
                var value = SensitiveFields.Any(field => header.Key.Contains(field, StringComparison.OrdinalIgnoreCase))
                    ? "***SENSITIVE***"
                    : header.Value.ToString();

                sanitized[header.Key] = value;
            }

            return sanitized;
        }

        /// <summary>
        /// 获取操作分类
        /// </summary>
        private static string GetActionCategory(string method, string path)
        {
            var pathLower = path.ToLowerInvariant();

            if (pathLower.Contains("auth") || pathLower.Contains("login") || pathLower.Contains("register"))
                return "Authentication";

            if (pathLower.Contains("admin") || pathLower.Contains("permission") || pathLower.Contains("role"))
                return "Authorization";

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) ||
                method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                return "DataModification";

            return "General";
        }

        /// <summary>
        /// 获取风险级别
        /// </summary>
        private static string GetRiskLevel(string method, string path, int statusCode)
        {
            var pathLower = path.ToLowerInvariant();

            // 高风险操作
            if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                return "High";

            if (pathLower.Contains("admin") || pathLower.Contains("system") || pathLower.Contains("config"))
                return "High";

            // 服务器错误
            if (statusCode >= 500)
                return "Medium";

            // 认证相关
            if (pathLower.Contains("auth") || pathLower.Contains("login"))
                return "Medium";

            // 客户端错误
            if (statusCode >= 400)
                return "Low";

            return "Low";
        }

        /// <summary>
        /// 判断是否为敏感请求
        /// </summary>
        private static bool IsSensitiveRequest(string path, string method)
        {
            var pathLower = path.ToLowerInvariant();

            return pathLower.Contains("auth") ||
                   pathLower.Contains("login") ||
                   pathLower.Contains("register") ||
                   pathLower.Contains("password") ||
                   pathLower.Contains("admin") ||
                   pathLower.Contains("permission") ||
                   method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 审计日志中间件配置选项
    /// </summary>
    public class AuditLoggingOptions
    {
        /// <summary>
        /// 是否记录请求体
        /// </summary>
        public bool LogRequestBody { get; set; } = true;

        /// <summary>
        /// 是否记录响应体
        /// </summary>
        public bool LogResponseBody { get; set; } = true;

        /// <summary>
        /// 最大记录的请求/响应体大小（字节）
        /// </summary>
        public int MaxBodySizeToLog { get; set; } = 10240; // 10KB

        /// <summary>
        /// 需要审计的HTTP方法列表（空表示全部）
        /// </summary>
        public List<string> HttpMethodsToLog { get; set; } = new();

        /// <summary>
        /// 是否包含异常堆栈信息
        /// </summary>
        public bool IncludeStackTrace { get; set; } = false;

        /// <summary>
        /// 是否启用异步记录（推荐）
        /// </summary>
        public bool EnableAsyncLogging { get; set; } = true;
    }
}