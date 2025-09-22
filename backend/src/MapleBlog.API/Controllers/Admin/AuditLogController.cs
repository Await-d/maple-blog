using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.API.Controllers.Admin
{
    /// <summary>
    /// 审计日志管理控制器
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(
            IAuditLogService auditLogService,
            ILogger<AuditLogController> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// 获取审计日志列表
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] AuditLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _auditLogService.GetAuditLogsAsync(filter, cancellationToken);
                var totalCount = await _auditLogService.CountAsync(
                    filter.Action,
                    filter.ResourceType,
                    filter.StartDate,
                    filter.EndDate,
                    cancellationToken);

                var result = new
                {
                    Data = logs,
                    TotalCount = totalCount,
                    PageSize = filter.PageSize,
                    Page = filter.Page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志列表失败");
                return StatusCode(500, new { message = "获取审计日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取审计日志详情
        /// </summary>
        /// <param name="id">审计日志ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志详情</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAuditLog(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = new AuditLogFilter { Page = 1, PageSize = 1 };
                var logs = await _auditLogService.GetAuditLogsAsync(filter, cancellationToken);
                var log = logs.FirstOrDefault(l => l.Id == id);

                if (log == null)
                {
                    return NotFound(new { message = "审计日志不存在" });
                }

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志详情失败: {Id}", id);
                return StatusCode(500, new { message = "获取审计日志详情失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取审计统计信息
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = await _auditLogService.GetStatisticsAsync(startDate, endDate, cancellationToken);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计统计信息失败");
                return StatusCode(500, new { message = "获取审计统计信息失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取最近的审计日志
        /// </summary>
        /// <param name="limit">数量限制</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最近审计日志</returns>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentLogs(
            [FromQuery, Range(1, 1000)] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _auditLogService.GetRecentLogsAsync(limit, cancellationToken);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取最近审计日志失败");
                return StatusCode(500, new { message = "获取最近审计日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取用户审计日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页面大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户审计日志</returns>
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserLogs(
            [FromRoute] Guid userId,
            [FromQuery, Range(1, int.MaxValue)] int page = 1,
            [FromQuery, Range(1, 100)] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = new AuditLogFilter
                {
                    UserId = userId,
                    Page = page,
                    PageSize = pageSize
                };

                var logs = await _auditLogService.GetAuditLogsAsync(filter, cancellationToken);
                var totalCount = await _auditLogService.CountAsync(
                    cancellationToken: cancellationToken);

                var result = new
                {
                    Data = logs,
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    Page = page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户审计日志失败: {UserId}", userId);
                return StatusCode(500, new { message = "获取用户审计日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 导出审计日志
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <param name="format">导出格式（csv, excel）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>导出文件</returns>
        [HttpPost("export")]
        public async Task<IActionResult> ExportLogs(
            [FromBody] AuditLogFilter filter,
            [FromQuery] string format = "csv",
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 限制导出数量以避免性能问题
                filter.PageSize = Math.Min(filter.PageSize, 10000);
                filter.Page = 1;

                var logs = await _auditLogService.GetAuditLogsAsync(filter, cancellationToken);
                var logsArray = logs.ToArray();

                var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

                switch (format.ToLowerInvariant())
                {
                    case "csv":
                        var csvContent = GenerateCsvContent(logsArray);
                        return File(
                            System.Text.Encoding.UTF8.GetBytes(csvContent),
                            "text/csv",
                            $"{fileName}.csv");

                    case "json":
                        var jsonContent = System.Text.Json.JsonSerializer.Serialize(logsArray, new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = true,
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        });
                        return File(
                            System.Text.Encoding.UTF8.GetBytes(jsonContent),
                            "application/json",
                            $"{fileName}.json");

                    default:
                        return BadRequest(new { message = "不支持的导出格式，支持：csv, json" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出审计日志失败");
                return StatusCode(500, new { message = "导出审计日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 清理过期审计日志
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理结果</returns>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupOldLogs(
            [FromQuery, Range(1, 3650)] int retentionDays = 365,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var deletedCount = await _auditLogService.CleanupOldLogsAsync(retentionDays, cancellationToken);

                return Ok(new
                {
                    message = "清理完成",
                    deletedCount,
                    retentionDays
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期审计日志失败");
                return StatusCode(500, new { message = "清理过期审计日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 手动记录审计日志
        /// </summary>
        /// <param name="request">审计日志请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        [HttpPost("manual")]
        public async Task<IActionResult> LogManually(
            [FromBody] ManualAuditLogRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _auditLogService.LogUserActionAsync(
                    request.UserId,
                    request.UserName,
                    request.Action,
                    request.ResourceType,
                    request.ResourceId,
                    request.Description,
                    request.OldValues,
                    request.NewValues,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.Request.Headers["User-Agent"].FirstOrDefault(),
                    cancellationToken);

                if (success)
                {
                    return Ok(new { message = "审计日志记录成功" });
                }
                else
                {
                    return StatusCode(500, new { message = "审计日志记录失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动记录审计日志失败");
                return StatusCode(500, new { message = "手动记录审计日志失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 获取审计日志数量统计
        /// </summary>
        /// <param name="action">操作类型</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>数量统计</returns>
        [HttpGet("count")]
        public async Task<IActionResult> GetCount(
            [FromQuery] string? action,
            [FromQuery] string? resourceType,
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var count = await _auditLogService.CountAsync(action, resourceType, startTime, endTime, cancellationToken);
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志数量失败");
                return StatusCode(500, new { message = "获取审计日志数量失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 生成CSV内容
        /// </summary>
        /// <param name="logs">审计日志</param>
        /// <returns>CSV内容</returns>
        private string GenerateCsvContent(AuditLog[] logs)
        {
            var csv = new System.Text.StringBuilder();

            // 添加标题行
            csv.AppendLine("创建时间,用户名,操作,资源类型,资源ID,描述,IP地址,结果,风险级别,是否敏感,分类");

            // 添加数据行
            foreach (var log in logs)
            {
                csv.AppendLine($"\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\"," +
                              $"\"{log.UserName?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.Action?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.ResourceType?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.ResourceId?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.Description?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.IpAddress?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.Result?.Replace("\"", "\"\"")}\"," +
                              $"\"{log.RiskLevel?.Replace("\"", "\"\"")}\"," +
                              $"{log.IsSensitive}," +
                              $"\"{log.Category?.Replace("\"", "\"\"")}\"");
            }

            return csv.ToString();
        }
    }

    /// <summary>
    /// 手动审计日志请求
    /// </summary>
    public class ManualAuditLogRequest
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [Required(ErrorMessage = "操作类型不能为空")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型
        /// </summary>
        [Required(ErrorMessage = "资源类型不能为空")]
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// 资源ID
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 变更前数据
        /// </summary>
        public object? OldValues { get; set; }

        /// <summary>
        /// 变更后数据
        /// </summary>
        public object? NewValues { get; set; }
    }
}