using MapleBlog.Domain.Entities;
using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 审计日志服务接口
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// 记录审计日志
        /// </summary>
        /// <param name="auditLog">审计日志</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量记录审计日志
        /// </summary>
        /// <param name="auditLogs">审计日志列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功记录的数量</returns>
        Task<int> LogBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);

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
        Task<bool> LogUserActionAsync(
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
            CancellationToken cancellationToken = default);

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
        Task<bool> LogAuthenticationAsync(
            Guid? userId,
            string? userName,
            string action,
            string result,
            string? ipAddress = null,
            string? userAgent = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询审计日志
        /// </summary>
        /// <param name="filter">查询过滤器</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志列表</returns>
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取审计统计信息
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        Task<AuditLogStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理过期审计日志
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理的记录数</returns>
        Task<int> CleanupOldLogsAsync(int retentionDays = 365, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取最近的审计日志
        /// </summary>
        /// <param name="limit">数量限制</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志列表</returns>
        Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统计指定条件的审计日志数量
        /// </summary>
        /// <param name="action">操作类型</param>
        /// <param name="resourceType">资源类型</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>数量</returns>
        Task<int> CountAsync(string? action = null, string? resourceType = null, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default);
    }
}