using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 审计日志仓储接口
    /// </summary>
    public interface IAuditLogRepository : IRepository<AuditLog>
    {
        /// <summary>
        /// 批量添加审计日志
        /// </summary>
        /// <param name="auditLogs">审计日志列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>添加的数量</returns>
        Task<int> AddBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据过滤条件查询审计日志
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志列表</returns>
        Task<IEnumerable<AuditLog>> GetByFilterAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统计审计日志数量
        /// </summary>
        /// <param name="filter">过滤条件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>数量</returns>
        Task<long> CountByFilterAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取审计统计信息
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>统计信息</returns>
        Task<AuditLogStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除过期的审计日志
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="batchSize">批次大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的数量</returns>
        Task<int> DeleteOldLogsAsync(int retentionDays = 365, int batchSize = 1000, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取最近的审计日志
        /// </summary>
        /// <param name="count">数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审计日志列表</returns>
        Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户活动统计
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="topCount">返回数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户活动统计</returns>
        Task<IEnumerable<UserActivityStats>> GetTopActiveUsersAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取IP活动统计
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="topCount">返回数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>IP活动统计</returns>
        Task<IEnumerable<IpActivityStats>> GetTopActiveIpsAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检测异常行为
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="timeWindow">时间窗口（小时）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否异常</returns>
        Task<bool> DetectAnomalousActivityAsync(Guid? userId, string? ipAddress, int timeWindow = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的审计日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页面大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户审计日志</returns>
        Task<IEnumerable<AuditLog>> GetUserLogsAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取资源的审计日志
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>资源审计日志</returns>
        Task<IEnumerable<AuditLog>> GetResourceLogsAsync(string resourceType, string resourceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取安全事件
        /// </summary>
        /// <param name="riskLevel">风险级别</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>安全事件</returns>
        Task<IEnumerable<AuditLog>> GetSecurityEventsAsync(string? riskLevel = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    }
}