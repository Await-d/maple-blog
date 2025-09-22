using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// 审计日志仓储实现
    /// </summary>
    public class AuditLogRepository : BlogBaseRepository<AuditLog>, IAuditLogRepository
    {
        private readonly ILogger<AuditLogRepository> _logger;

        public AuditLogRepository(BlogDbContext context, ILogger<AuditLogRepository> logger)
            : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// 批量添加审计日志
        /// </summary>
        public async Task<int> AddBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = auditLogs.ToList();
                if (!logs.Any())
                    return 0;

                // 设置创建时间
                var now = DateTime.UtcNow;
                foreach (var log in logs)
                {
                    if (log.CreatedAt == default)
                        log.CreatedAt = now;
                }

                await _context.Set<AuditLog>().AddRangeAsync(logs, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("批量添加审计日志成功: {Count} 条", logs.Count);
                return logs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量添加审计日志失败");
                throw;
            }
        }

        /// <summary>
        /// 根据过滤条件查询审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetByFilterAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>().AsQueryable();

                // 应用过滤条件
                query = ApplyFilter(query, filter);

                // 排序
                query = ApplySorting(query, filter.SortBy, filter.IsDescending);

                // 分页
                if (filter.Page > 0 && filter.PageSize > 0)
                {
                    query = query.Skip((filter.Page - 1) * filter.PageSize)
                                 .Take(filter.PageSize);
                }
                else
                {
                    // 默认限制
                    query = query.Take(1000);
                }

                var result = await query.ToListAsync(cancellationToken);
                _logger.LogDebug("根据过滤条件查询审计日志完成: 返回 {Count} 条记录", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据过滤条件查询审计日志失败");
                throw;
            }
        }

        /// <summary>
        /// 统计审计日志数量
        /// </summary>
        public async Task<long> CountByFilterAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>().AsQueryable();
                query = ApplyFilter(query, filter);

                var count = await query.LongCountAsync(cancellationToken);
                _logger.LogDebug("统计审计日志数量完成: {Count} 条", count);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "统计审计日志数量失败");
                throw;
            }
        }

        /// <summary>
        /// 获取审计统计信息
        /// </summary>
        public async Task<Domain.ValueObjects.AuditLogStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>().AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(al => al.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(al => al.CreatedAt <= endDate.Value);

                var statistics = new Domain.ValueObjects.AuditLogStatistics
                {
                    TotalLogs = await query.LongCountAsync(cancellationToken),
                    SuccessfulOperations = await query.LongCountAsync(al => al.Result == "Success", cancellationToken),
                    FailedOperations = await query.LongCountAsync(al => al.Result == "Failed", cancellationToken),
                    HighRiskOperations = await query.LongCountAsync(al => al.RiskLevel == "High" || al.RiskLevel == "Critical", cancellationToken),
                    SensitiveOperations = await query.LongCountAsync(al => al.IsSensitive, cancellationToken),
                    UniqueUsers = await query.Where(al => al.UserId.HasValue).Select(al => al.UserId).Distinct().CountAsync(cancellationToken),
                    UniqueIpAddresses = await query.Where(al => !string.IsNullOrEmpty(al.IpAddress)).Select(al => al.IpAddress).Distinct().CountAsync(cancellationToken),
                    StartDate = startDate,
                    EndDate = endDate
                };

                // 按操作类型统计
                statistics.TopActions = await query
                    .GroupBy(al => al.Action)
                    .Select(g => new { Action = g.Key, Count = g.LongCount() })
                    .OrderByDescending(s => s.Count)
                    .Take(20)
                    .ToDictionaryAsync(g => g.Action, g => g.Count, cancellationToken);

                // 按资源类型统计
                statistics.TopResourceTypes = await query
                    .GroupBy(al => al.ResourceType)
                    .Select(g => new { ResourceType = g.Key, Count = g.LongCount() })
                    .OrderByDescending(s => s.Count)
                    .Take(20)
                    .ToDictionaryAsync(g => g.ResourceType, g => g.Count, cancellationToken);

                // 按小时统计（当前日期）
                var today = DateTime.UtcNow.Date;
                var todayLogs = query.Where(al => al.CreatedAt >= today && al.CreatedAt < today.AddDays(1));
                statistics.ActivityByHour = await todayLogs
                    .GroupBy(al => al.CreatedAt.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.LongCount() })
                    .ToDictionaryAsync(g => g.Hour, g => g.Count, cancellationToken);

                // 按日期统计（最近7天）
                var weekAgo = DateTime.UtcNow.AddDays(-7).Date;
                var weeklyLogs = query.Where(al => al.CreatedAt >= weekAgo);
                statistics.ActivityByDate = await weeklyLogs
                    .GroupBy(al => al.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.LongCount() })
                    .ToDictionaryAsync(g => g.Date, g => g.Count, cancellationToken);

                _logger.LogDebug("获取审计统计信息完成: 总计 {TotalCount} 条记录", statistics.TotalLogs);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计统计信息失败");
                throw;
            }
        }

        /// <summary>
        /// 删除过期的审计日志
        /// </summary>
        public async Task<int> DeleteOldLogsAsync(int retentionDays = 365, int batchSize = 1000, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var totalDeleted = 0;

                while (true)
                {
                    var logsToDelete = await _context.Set<AuditLog>()
                        .Where(al => al.CreatedAt < cutoffDate)
                        .Take(batchSize)
                        .ToListAsync(cancellationToken);

                    if (!logsToDelete.Any())
                        break;

                    _context.Set<AuditLog>().RemoveRange(logsToDelete);
                    await _context.SaveChangesAsync(cancellationToken);

                    totalDeleted += logsToDelete.Count;
                    _logger.LogDebug("已删除 {Count} 条过期审计日志，总计已删除 {TotalDeleted} 条", logsToDelete.Count, totalDeleted);

                    // 给数据库喘息时间
                    await Task.Delay(100, cancellationToken);
                }

                _logger.LogInformation("删除过期审计日志完成: 保留 {RetentionDays} 天，共删除 {TotalDeleted} 条记录", retentionDays, totalDeleted);
                return totalDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除过期审计日志失败");
                throw;
            }
        }

        /// <summary>
        /// 获取最近的审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
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
                throw;
            }
        }

        /// <summary>
        /// 获取用户活动统计
        /// </summary>
        public async Task<IEnumerable<Domain.ValueObjects.UserActivityStats>> GetTopActiveUsersAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>()
                    .Where(al => al.UserId.HasValue);

                if (startDate.HasValue)
                    query = query.Where(al => al.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(al => al.CreatedAt <= endDate.Value);

                var result = await query
                    .GroupBy(al => new { al.UserId, al.UserName })
                    .Select(g => new Domain.ValueObjects.UserActivityStats
                    {
                        UserId = g.Key.UserId!.Value,
                        UserName = g.Key.UserName ?? "Unknown",
                        OperationCount = g.LongCount(),
                        SuccessCount = g.LongCount(al => al.Result == "Success"),
                        FailureCount = g.LongCount(al => al.Result == "Failed"),
                        LastActivity = g.Max(al => al.CreatedAt),
                        FirstActivity = g.Min(al => al.CreatedAt)
                    })
                    .OrderByDescending(s => s.OperationCount)
                    .Take(topCount)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取用户活动统计完成: 返回 {Count} 条记录", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户活动统计失败");
                throw;
            }
        }

        /// <summary>
        /// 获取IP活动统计
        /// </summary>
        public async Task<IEnumerable<Domain.ValueObjects.IpActivityStats>> GetTopActiveIpsAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>()
                    .Where(al => !string.IsNullOrEmpty(al.IpAddress));

                if (startDate.HasValue)
                    query = query.Where(al => al.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(al => al.CreatedAt <= endDate.Value);

                var result = await query
                    .GroupBy(al => al.IpAddress)
                    .Select(g => new Domain.ValueObjects.IpActivityStats
                    {
                        IpAddress = g.Key,
                        TotalOperations = g.LongCount(),
                        UniqueUsers = g.Where(al => al.UserId.HasValue).Select(al => al.UserId).Distinct().Count(),
                        SuccessfulOperations = g.LongCount(al => al.Result == "Success"),
                        FailedOperations = g.LongCount(al => al.Result == "Failed"),
                        LastActivity = g.Max(al => al.CreatedAt),
                        FirstActivity = g.Min(al => al.CreatedAt)
                    })
                    .OrderByDescending(s => s.TotalOperations)
                    .Take(topCount)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取IP活动统计完成: 返回 {Count} 条记录", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取IP活动统计失败");
                throw;
            }
        }

        /// <summary>
        /// 检测异常行为
        /// </summary>
        public async Task<bool> DetectAnomalousActivityAsync(Guid? userId, string? ipAddress, int timeWindow = 1, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-timeWindow);
                var query = _context.Set<AuditLog>()
                    .Where(al => al.CreatedAt >= cutoffTime);

                if (userId.HasValue)
                    query = query.Where(al => al.UserId == userId.Value);

                if (!string.IsNullOrEmpty(ipAddress))
                    query = query.Where(al => al.IpAddress == ipAddress);

                var totalCount = await query.CountAsync(cancellationToken);
                var failedCount = await query.CountAsync(al => al.Result == "Failed", cancellationToken);

                // 异常判断标准
                var isAnomalous = totalCount > 100 || // 请求频率过高
                                 failedCount > 10 || // 失败次数过多
                                 (totalCount > 0 && (double)failedCount / totalCount > 0.5); // 失败率过高

                if (isAnomalous)
                {
                    _logger.LogWarning("检测到异常行为: UserId={UserId}, IP={IpAddress}, Total={Total}, Failed={Failed}",
                        userId, ipAddress, totalCount, failedCount);
                }

                return isAnomalous;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测异常行为失败");
                return false;
            }
        }

        /// <summary>
        /// 获取用户的审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetUserLogsAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Set<AuditLog>()
                    .Where(al => al.UserId == userId)
                    .OrderByDescending(al => al.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取用户审计日志完成: UserId={UserId}, 返回 {Count} 条记录", userId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户审计日志失败: UserId={UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 获取资源的审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetResourceLogsAsync(string resourceType, string resourceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _context.Set<AuditLog>()
                    .Where(al => al.ResourceType == resourceType && al.ResourceId == resourceId)
                    .OrderByDescending(al => al.CreatedAt)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取资源审计日志完成: {ResourceType}={ResourceId}, 返回 {Count} 条记录", resourceType, resourceId, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取资源审计日志失败: {ResourceType}={ResourceId}", resourceType, resourceId);
                throw;
            }
        }

        /// <summary>
        /// 获取安全事件
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(string? riskLevel = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Set<AuditLog>()
                    .Where(al => al.IsSensitive || al.RiskLevel == "High" || al.RiskLevel == "Critical" || al.Result == "Failed");

                if (!string.IsNullOrEmpty(riskLevel))
                    query = query.Where(al => al.RiskLevel == riskLevel);

                if (startDate.HasValue)
                    query = query.Where(al => al.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(al => al.CreatedAt <= endDate.Value);

                var result = await query
                    .OrderByDescending(al => al.CreatedAt)
                    .Take(1000) // 限制返回数量
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("获取安全事件完成: 返回 {Count} 条记录", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取安全事件失败");
                throw;
            }
        }

        /// <summary>
        /// 应用过滤条件
        /// </summary>
        private IQueryable<AuditLog> ApplyFilter(IQueryable<AuditLog> query, AuditLogFilter filter)
        {
            if (filter.UserId.HasValue)
                query = query.Where(al => al.UserId == filter.UserId.Value);

            if (!string.IsNullOrEmpty(filter.UserName))
                query = query.Where(al => al.UserName != null && al.UserName.Contains(filter.UserName));

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(al => al.Action.Contains(filter.Action));

            if (!string.IsNullOrEmpty(filter.ResourceType))
                query = query.Where(al => al.ResourceType == filter.ResourceType);

            if (!string.IsNullOrEmpty(filter.ResourceId))
                query = query.Where(al => al.ResourceId == filter.ResourceId);

            if (filter.StartDate.HasValue)
                query = query.Where(al => al.CreatedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(al => al.CreatedAt <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.IpAddress))
                query = query.Where(al => al.IpAddress == filter.IpAddress);

            if (!string.IsNullOrEmpty(filter.Result))
                query = query.Where(al => al.Result == filter.Result);

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(al => al.Category == filter.Category);

            if (!string.IsNullOrEmpty(filter.RiskLevel))
                query = query.Where(al => al.RiskLevel == filter.RiskLevel);

            if (filter.IsSensitive.HasValue)
                query = query.Where(al => al.IsSensitive == filter.IsSensitive.Value);

            return query;
        }

        /// <summary>
        /// 应用排序
        /// </summary>
        private IQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> query, string? sortBy, bool isDescending)
        {
            return (sortBy?.ToLowerInvariant()) switch
            {
                "createdat" => isDescending ? query.OrderByDescending(al => al.CreatedAt) : query.OrderBy(al => al.CreatedAt),
                "action" => isDescending ? query.OrderByDescending(al => al.Action) : query.OrderBy(al => al.Action),
                "username" => isDescending ? query.OrderByDescending(al => al.UserName) : query.OrderBy(al => al.UserName),
                "resourcetype" => isDescending ? query.OrderByDescending(al => al.ResourceType) : query.OrderBy(al => al.ResourceType),
                "result" => isDescending ? query.OrderByDescending(al => al.Result) : query.OrderBy(al => al.Result),
                "risklevel" => isDescending ? query.OrderByDescending(al => al.RiskLevel) : query.OrderBy(al => al.RiskLevel),
                _ => query.OrderByDescending(al => al.CreatedAt) // 默认按创建时间降序
            };
        }
    }
}