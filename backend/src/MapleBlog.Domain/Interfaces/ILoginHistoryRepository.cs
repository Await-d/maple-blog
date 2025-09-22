using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 登录历史仓储接口
    /// </summary>
    public interface ILoginHistoryRepository : IRepository<LoginHistory>
    {
        /// <summary>
        /// 获取用户的登录历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页面大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>登录历史列表</returns>
        Task<IEnumerable<LoginHistory>> GetUserLoginHistoryAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取最近的登录记录
        /// </summary>
        /// <param name="count">数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>登录历史列表</returns>
        Task<IEnumerable<LoginHistory>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取失败的登录尝试
        /// </summary>
        /// <param name="userId">用户ID（可选）</param>
        /// <param name="ipAddress">IP地址（可选）</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>登录历史列表</returns>
        Task<IEnumerable<LoginHistory>> GetFailedAttemptsAsync(Guid? userId = null, string? ipAddress = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除过期的登录历史
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的数量</returns>
        Task<int> DeleteOldHistoryAsync(int retentionDays = 90, CancellationToken cancellationToken = default);

        /// <summary>
        /// 标记会话已登出
        /// </summary>
        Task<bool> MarkSessionLoggedOutAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取失败尝试次数
        /// </summary>
        Task<int> GetFailedAttemptsCountAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据IP获取失败尝试次数
        /// </summary>
        Task<int> GetFailedAttemptsByIpCountAsync(string ipAddress, TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查是否有来自不同位置的并发会话
        /// </summary>
        Task<bool> HasConcurrentSessionsFromDifferentLocationsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取登录统计信息
        /// </summary>
        Task<LoginStatistics> GetLoginStatisticsAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取登录历史
        /// </summary>
        Task<IEnumerable<LoginHistory>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取活动会话
        /// </summary>
        Task<IEnumerable<LoginHistory>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量标记会话过期
        /// </summary>
        Task<int> BulkMarkSessionsExpiredAsync(TimeSpan sessionTimeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取可疑活动
        /// </summary>
        Task<IEnumerable<LoginHistory>> GetSuspiciousActivitiesAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取标记的尝试
        /// </summary>
        Task<IEnumerable<LoginHistory>> GetFlaggedAttemptsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除旧记录
        /// </summary>
        Task<int> DeleteOldRecordsAsync(int retentionDays, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据邮箱获取失败尝试次数
        /// </summary>
        Task<int> GetFailedAttemptsCountByEmailAsync(string email, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    }
}