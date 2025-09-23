using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;
// using Swashbuckle.AspNetCore.Annotations; // Removed - using Scalar instead
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// API测试控制器
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    // [SwaggerTag("🧪 API测试", "用于测试管理后台API功能的测试端点")] // Removed - Swashbuckle attribute
    public class TestController : BaseAdminController
    {
        public TestController(
            ILogger<TestController> logger,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
            : base(logger, permissionService, auditLogService)
        {
        }

        /// <summary>
        /// 健康检查
        /// </summary>
        /// <returns>系统健康状态</returns>
        /// <response code="200">系统正常运行</response>
        [HttpGet("health")]
        [AllowAnonymous]
        // [SwaggerOperation] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        public IActionResult Health()
        {
            var response = new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Environment = HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.EnvironmentName ?? "Unknown",
                Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime,
                MemoryUsage = GC.GetTotalMemory(false),
                ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count
            };

            return Success(response);
        }

        /// <summary>
        /// Echo测试
        /// </summary>
        /// <param name="request">Echo请求</param>
        /// <returns>Echo响应</returns>
        /// <response code="200">Echo成功</response>
        /// <response code="400">请求参数错误</response>
        [HttpPost("echo")]
        // [SwaggerOperation] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        public async Task<IActionResult> Echo([FromBody] EchoRequest request)
        {
            try
            {
                var validation = ValidateModelState();
                if (validation != null) return validation;

                var response = new EchoResponse
                {
                    Message = request.Message,
                    Timestamp = DateTime.UtcNow,
                    RequestId = Guid.NewGuid(),
                    ClientInfo = new ClientInfo
                    {
                        IpAddress = ClientIpAddress,
                        UserAgent = UserAgent,
                        UserId = CurrentUserId,
                        UserName = CurrentUserName
                    }
                };

                await LogAuditAsync("EchoTest", "Test", null, $"Echo消息: {request.Message}");

                return Success(response, "Echo测试成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Echo测试");
            }
        }

        /// <summary>
        /// 权限测试
        /// </summary>
        /// <param name="permission">需要测试的权限</param>
        /// <returns>权限检查结果</returns>
        /// <response code="200">权限检查完成</response>
        /// <response code="403">权限不足</response>
        [HttpGet("permission/{permission}")]
        // [SwaggerOperation] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        public async Task<IActionResult> TestPermission([Required] string permission)
        {
            try
            {
                var hasPermission = await HasPermissionAsync(permission);

                var response = new PermissionTestResponse
                {
                    Permission = permission,
                    HasPermission = hasPermission,
                    UserId = CurrentUserId,
                    UserName = CurrentUserName,
                    IsSuperAdmin = IsSuperAdmin(),
                    TestTime = DateTime.UtcNow
                };

                if (!hasPermission)
                {
                    return Forbid($"用户缺少权限: {permission}");
                }

                await LogAuditAsync("PermissionTest", "Test", null, $"测试权限: {permission}");

                return Success(response, "权限测试成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "权限测试");
            }
        }

        /// <summary>
        /// 异常测试
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <returns>测试结果</returns>
        /// <response code="200">测试完成</response>
        /// <response code="400">参数错误</response>
        /// <response code="500">服务器错误</response>
        [HttpGet("error/{errorType}")]
        // [SwaggerOperation] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        public IActionResult TestError([Required] string errorType)
        {
            try
            {
                var permissionCheck = ValidateSuperAdminPermission();
                if (permissionCheck != null) return permissionCheck;

                return errorType.ToLower() switch
                {
                    "validation" => Error("这是一个验证错误示例", 400),
                    "notfound" => NotFoundResult("测试资源", "test-id"),
                    "forbidden" => Forbid("这是一个权限错误示例"),
                    "exception" => throw new InvalidOperationException("这是一个异常测试"),
                    _ => Error("不支持的错误类型", 400)
                };
            }
            catch (Exception ex)
            {
                return HandleException(ex, "异常测试");
            }
        }

        /// <summary>
        /// 批量操作测试
        /// </summary>
        /// <param name="request">批量请求</param>
        /// <returns>批量处理结果</returns>
        [HttpPost("batch")]
        // [SwaggerOperation] // Removed - Swashbuckle attribute
        // [SwaggerResponse] // Removed - Swashbuckle attribute
        public async Task<IActionResult> TestBatch([FromBody] BatchTestRequest request)
        {
            try
            {
                var validation = ValidateModelState();
                if (validation != null) return validation;

                var results = new List<BatchItemResult>();
                var successCount = 0;
                var failCount = 0;

                foreach (var item in request.Items)
                {
                    try
                    {
                        // 模拟处理逻辑
                        await Task.Delay(10); // 模拟异步操作

                        var success = !string.IsNullOrEmpty(item.Name);
                        if (success)
                        {
                            successCount++;
                            results.Add(new BatchItemResult
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Success = true,
                                Message = "处理成功"
                            });
                        }
                        else
                        {
                            failCount++;
                            results.Add(new BatchItemResult
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Success = false,
                                Message = "名称不能为空"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        results.Add(new BatchItemResult
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Success = false,
                            Message = ex.Message
                        });
                    }
                }

                var response = new BatchTestResponse
                {
                    TotalCount = request.Items.Count,
                    SuccessCount = successCount,
                    FailCount = failCount,
                    Results = results,
                    ProcessedAt = DateTime.UtcNow
                };

                await LogAuditAsync("BatchTest", "Test", null, $"批量处理 {request.Items.Count} 个项目");

                return Success(response, "批量测试完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量测试");
            }
        }
    }

    #region DTO类定义

    /// <summary>
    /// 健康检查响应
    /// </summary>
    public class HealthResponse
    {
        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 环境
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// 运行时长
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// 内存使用量
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadCount { get; set; }
    }

    /// <summary>
    /// Echo请求
    /// </summary>
    public class EchoRequest
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        [Required(ErrorMessage = "消息内容不能为空")]
        [StringLength(1000, ErrorMessage = "消息内容不能超过1000个字符")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Echo响应
    /// </summary>
    public class EchoResponse
    {
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 请求ID
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// 客户端信息
        /// </summary>
        public ClientInfo ClientInfo { get; set; } = new();
    }

    /// <summary>
    /// 客户端信息
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }
    }

    /// <summary>
    /// 权限测试响应
    /// </summary>
    public class PermissionTestResponse
    {
        /// <summary>
        /// 权限名称
        /// </summary>
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// 是否有权限
        /// </summary>
        public bool HasPermission { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 是否超级管理员
        /// </summary>
        public bool IsSuperAdmin { get; set; }

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; }
    }

    /// <summary>
    /// 批量测试请求
    /// </summary>
    public class BatchTestRequest
    {
        /// <summary>
        /// 测试项目列表
        /// </summary>
        [Required(ErrorMessage = "测试项目列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要一个测试项目")]
        public IList<BatchTestItem> Items { get; set; } = new List<BatchTestItem>();
    }

    /// <summary>
    /// 批量测试项目
    /// </summary>
    public class BatchTestItem
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        [Required(ErrorMessage = "项目ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 项目值
        /// </summary>
        public int Value { get; set; }
    }

    /// <summary>
    /// 批量测试响应
    /// </summary>
    public class BatchTestResponse
    {
        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 处理结果
        /// </summary>
        public IList<BatchItemResult> Results { get; set; } = new List<BatchItemResult>();

        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// 批量项目结果
    /// </summary>
    public class BatchItemResult
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 处理消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}