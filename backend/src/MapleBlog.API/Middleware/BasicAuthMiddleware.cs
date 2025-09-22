using System.Net;
using System.Text;

namespace MapleBlog.API.Middleware
{
    /// <summary>
    /// 基本身份验证中间件，用于保护Swagger文档
    /// </summary>
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BasicAuthMiddleware> _logger;

        public BasicAuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<BasicAuthMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只对Swagger相关路径应用基本认证
            if (!IsSwaggerPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // 检查基本认证头
            if (!IsAuthorized(context.Request))
            {
                _logger.LogWarning("Unauthorized access attempt to Swagger documentation from {RemoteIpAddress}",
                    context.Connection.RemoteIpAddress);

                await SendUnauthorizedResponse(context);
                return;
            }

            await _next(context);
        }

        /// <summary>
        /// 判断是否为Swagger相关路径
        /// </summary>
        private static bool IsSwaggerPath(PathString path)
        {
            return path.StartsWithSegments("/api-docs") ||
                   path.StartsWithSegments("/swagger");
        }

        /// <summary>
        /// 验证基本认证
        /// </summary>
        private bool IsAuthorized(HttpRequest request)
        {
            var authHeader = request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            {
                return false;
            }

            try
            {
                var token = authHeader["Basic ".Length..].Trim();
                var credentialsBytes = Convert.FromBase64String(token);
                var credentials = Encoding.UTF8.GetString(credentialsBytes).Split(':', 2);

                if (credentials.Length != 2)
                    return false;

                var username = credentials[0];
                var password = credentials[1];

                // 从配置中获取认证信息
                var configUsername = _configuration["Swagger:BasicAuth:Username"] ?? "admin";
                var configPassword = _configuration["Swagger:BasicAuth:Password"] ?? "swagger123";

                return username == configUsername && password == configPassword;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse basic authentication header");
                return false;
            }
        }

        /// <summary>
        /// 发送未授权响应
        /// </summary>
        private static async Task SendUnauthorizedResponse(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Basic realm=\"Swagger Documentation\"";
            context.Response.ContentType = "text/html";

            var html = """
                <!DOCTYPE html>
                <html>
                <head>
                    <title>API文档访问受限</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 50px; text-align: center; }
                        .container { max-width: 600px; margin: 0 auto; }
                        .warning { color: #d32f2f; font-size: 18px; margin-bottom: 20px; }
                        .info { color: #666; margin-top: 20px; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <h1>🔒 API文档访问受限</h1>
                        <div class="warning">此API文档需要身份验证才能访问</div>
                        <p>请联系系统管理员获取访问凭据</p>
                        <div class="info">
                            <small>Maple Blog API Documentation - 生产环境</small>
                        </div>
                    </div>
                </body>
                </html>
                """;

            await context.Response.WriteAsync(html);
        }
    }
}