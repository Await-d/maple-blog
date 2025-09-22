using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MapleBlog.Domain.Entities;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Infrastructure.Services
{
    /// <summary>
    /// 审计日志服务实现
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly BlogDbContext _context;
        private readonly ILogger<AuditLogService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // 配置
        private const int MaxRetentionDays = 365; // 默认保留一年
        private const int BatchSize = 1000;

        public AuditLogService(
            BlogDbContext context,
            ILogger<AuditLogService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// 记录审计日志
        /// </summary>
        /// <param name="auditLog">审计日志</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            try
            {
                if (auditLog == null)
                {
                    _logger.LogWarning("审计日志对象为空");
                    return false;
                }

                // 设置创建时间
                auditLog.CreatedAt = DateTime.UtcNow;

                _context.Set<AuditLog>().Add(auditLog);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("审计日志记录成功: {Summary}", auditLog.GetSummary());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录审计日志失败: {Summary}", auditLog?.GetSummary() ?? "未知操作");
                return false;
            }
        }

        /// <summary>
        /// 批量记录审计日志
        /// </summary>
        /// <param name="auditLogs">审计日志列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功记录的数量</returns>
        public async Task<int> LogBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = auditLogs.ToList();
                if (!logs.Any())
                {
                    return 0;
                }

                var now = DateTime.UtcNow;
                foreach (var log in logs)
                {
                    log.CreatedAt = now;
                }

                _context.Set<AuditLog>().AddRange(logs);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("批量记录审计日志成功: {Count} 条", logs.Count);
                return logs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量记录审计日志失败");
                return 0;
            }
        }

        /// <summary>
        /// 记录用户操作
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="action">操作类型</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="description">描述</param>
        /// <param name="oldValues">变更前数据</param>
        /// <param name="newValues">变更后数据</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">User Agent</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> LogUserActionAsync(
            Guid? userId,
            string? userName,
            string action,
            string resourceType,
            string? resourceId = null,
            string? description = null,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Description = description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Category = GetActionCategory(action, resourceType),
                    RiskLevel = GetRiskLevel(action, resourceType),
                    IsSensitive = IsSensitiveAction(action, resourceType)
                };

                // 序列化变更数据
                if (oldValues != null)
                {
                    auditLog.OldValues = JsonSerializer.Serialize(oldValues, _jsonOptions);
                }

                if (newValues != null)
                {
                    auditLog.NewValues = JsonSerializer.Serialize(newValues, _jsonOptions);
                }

                return await LogAsync(auditLog, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录用户操作审计日志失败: {Action} {ResourceType}", action, resourceType);
                return false;
            }
        }

        /// <summary>
        /// 记录认证事件
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="userName">用户名</param>
        /// <param name="action">认证操作</param>
        /// <param name="result">结果</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">User Agent</param>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> LogAuthenticationAsync(
            Guid? userId,
            string? userName,
            string action,
            string result,
            string? ipAddress = null,
            string? userAgent = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    ResourceType = "Authentication",
                    Result = result,
                    ErrorMessage = errorMessage,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Category = "Authentication",
                    RiskLevel = result == "Failed" ? "High" : "Medium",
                    IsSensitive = true
                };

                return await LogAsync(auditLog, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录认证审计日志失败: {Action} {Result}", action, result);
                return false;
            }
        }

        /// <summary>
        /// 查询审计日志
        /// </summary>
        /// <param name="filter">查询过滤器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志列表</returns>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>().AsQueryable();

                // 应用过滤器
                if (filter.UserId.HasValue)
                {
                    query = query.Where(al => al.UserId == filter.UserId.Value);
                }

                if (!string.IsNullOrEmpty(filter.Action))
                {
                    query = query.Where(al => al.Action.Contains(filter.Action));
                }

                if (!string.IsNullOrEmpty(filter.ResourceType))
                {
                    query = query.Where(al => al.ResourceType == filter.ResourceType);
                }

                if (!string.IsNullOrEmpty(filter.ResourceId))
                {
                    query = query.Where(al => al.ResourceId == filter.ResourceId);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= filter.EndDate.Value);
                }

                if (!string.IsNullOrEmpty(filter.IpAddress))
                {
                    query = query.Where(al => al.IpAddress == filter.IpAddress);
                }

                if (!string.IsNullOrEmpty(filter.Result))
                {
                    query = query.Where(al => al.Result == filter.Result);
                }

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(al => al.Category == filter.Category);
                }

                if (!string.IsNullOrEmpty(filter.RiskLevel))
                {
                    query = query.Where(al => al.RiskLevel == filter.RiskLevel);
                }

                if (filter.IsSensitive.HasValue)
                {
                    query = query.Where(al => al.IsSensitive == filter.IsSensitive.Value);
                }

                // 分页
                query = query.OrderByDescending(al => al.CreatedAt);
                if (filter.Page > 0 && filter.PageSize > 0)
                {
                    query = query.Skip((filter.Page - 1) * filter.PageSize)
                                 .Take(filter.PageSize);
                }
                else
                {
                    // 默认限制返回数量
                    query = query.Take(1000);
                }

                var result = await query.ToListAsync(cancellationToken);
                _logger.LogDebug("查询审计日志完成: 返回 {Count} 条记录", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询审计日志失败");
                return Enumerable.Empty<AuditLog>();
            }
        }

        /// <summary>
        /// 获取审计统计信息
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        public async Task<AuditLogStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>().AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= endDate.Value);
                }

                var statistics = new AuditLogStatistics
                {
                    TotalLogs = await query.CountAsync(cancellationToken),
                    SuccessCount = await query.CountAsync(al => al.Result == "Success", cancellationToken),
                    FailureCount = await query.CountAsync(al => al.Result == "Failed", cancellationToken),
                    HighRiskOperationCount = await query.CountAsync(al => al.RiskLevel == "High" || al.RiskLevel == "Critical", cancellationToken),
                    SensitiveOperationCount = await query.CountAsync(al => al.IsSensitive, cancellationToken),
                    UniqueUserCount = await query.Where(al => al.UserId.HasValue).Select(al => al.UserId).Distinct().CountAsync(cancellationToken)
                };

                // 按操作类型统计
                statistics.ActionStatistics = await query
                    .GroupBy(al => al.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(s => s.Count)
                    .Take(20)
                    .ToDictionaryAsync(g => g.Action, g => (long)g.Count, cancellationToken);

                // 按资源类型统计
                statistics.ResourceStatistics = await query
                    .GroupBy(al => al.ResourceType)
                    .Select(g => new ResourceStatisticDto
                    {
                        ResourceType = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(s => s.Count)
                    .Take(20)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取审计统计信息完成: 总计 {TotalCount} 条记录", statistics.TotalCount);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计统计信息失败");
                return new AuditLogStatistics();
            }
        }

        /// <summary>
        /// 清理过期审计日志
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理的记录数</returns>
        public async Task<int> CleanupOldLogsAsync(int retentionDays = MaxRetentionDays, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var totalCleaned = 0;

                while (true)
                {
                    // 分批删除以避免长时间锁定
                    var logsToDelete = await _context.Set<AuditLog>()
                        .Where(al => al.CreatedAt < cutoffDate)
                        .Take(BatchSize)
                        .ToListAsync(cancellationToken);

                    if (!logsToDelete.Any())
                        break;

                    _context.Set<AuditLog>().RemoveRange(logsToDelete);
                    await _context.SaveChangesAsync(cancellationToken);

                    totalCleaned += logsToDelete.Count;

                    _logger.LogDebug("已清理 {Count} 条过期审计日志，总计已清理 {TotalCleaned} 条", logsToDelete.Count, totalCleaned);

                    // 避免长时间运行，给其他操作让路
                    await Task.Delay(100, cancellationToken);
                }

                _logger.LogInformation("清理过期审计日志完成: 保留 {RetentionDays} 天，共清理 {TotalCleaned} 条记录", retentionDays, totalCleaned);
                return totalCleaned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期审计日志失败");
                return 0;
            }
        }

        /// <summary>
        /// 获取操作分类
        /// </summary>
        private static string GetActionCategory(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();

            return actionLower switch
            {
                var a when a.Contains("login") || a.Contains("logout") || a.Contains("register") => "Authentication",
                var a when a.Contains("permission") || a.Contains("role") || a.Contains("access") => "Authorization",
                var a when a.Contains("create") || a.Contains("update") || a.Contains("delete") || a.Contains("modify") => "DataModification",
                var a when a.Contains("config") || a.Contains("setting") || a.Contains("system") => "SystemConfiguration",
                _ => "General"
            };
        }

        /// <summary>
        /// 获取风险级别
        /// </summary>
        private static string GetRiskLevel(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();
            var resourceLower = resourceType.ToLowerInvariant();

            // 高风险操作
            if (actionLower.Contains("delete") || actionLower.Contains("remove"))
                return "High";

            // 中高风险操作
            if (resourceLower.Contains("user") || resourceLower.Contains("role") || resourceLower.Contains("permission"))
            {
                if (actionLower.Contains("create") || actionLower.Contains("update"))
                    return "Medium";
            }

            // 系统配置相关
            if (resourceLower.Contains("system") || resourceLower.Contains("config"))
                return "Medium";

            return "Low";
        }

        /// <summary>
        /// 检查是否为敏感操作
        /// </summary>
        private static bool IsSensitiveAction(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();
            var resourceLower = resourceType.ToLowerInvariant();

            // 认证相关
            if (actionLower.Contains("login") || actionLower.Contains("logout") || actionLower.Contains("register"))
                return true;

            // 权限和角色相关
            if (resourceLower.Contains("role") || resourceLower.Contains("permission"))
                return true;

            // 删除操作
            if (actionLower.Contains("delete") || actionLower.Contains("remove"))
                return true;

            // 系统配置
            if (resourceLower.Contains("system") || resourceLower.Contains("config"))
                return true;

            return false;
        }

        /// <summary>
        /// 获取最近的审计日志
        /// </summary>
        /// <param name="count">记录数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最近的审计日志列表</returns>
        public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Set<AuditLog>()
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(count)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取最近审计日志完成: 返回 {Count} 条记录", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取最近审计日志失败");
                return Enumerable.Empty<AuditLog>();
            }
        }

        /// <summary>
        /// 统计审计日志数量
        /// </summary>
        /// <param name="action">操作类型</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>日志数量</returns>
        public async Task<int> CountAsync(string? action = null, string? resourceType = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>().AsQueryable();

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(al => al.Action.Contains(action));
                }

                if (!string.IsNullOrEmpty(resourceType))
                {
                    query = query.Where(al => al.ResourceType == resourceType);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= endDate.Value);
                }

                var result = await query.CountAsync(cancellationToken);
                _logger.LogDebug("统计审计日志数量完成: {Count} 条", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "统计审计日志数量失败");
                return 0;
            }
        }
    }


}