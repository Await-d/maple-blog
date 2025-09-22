using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// 登录历史仓储实现
    /// </summary>
    public class LoginHistoryRepository : BaseRepository<LoginHistory>, ILoginHistoryRepository
    {
        public LoginHistoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 获取用户的登录历史
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetUserLoginHistoryAsync(
            Guid userId, 
            int page = 1, 
            int pageSize = 20, 
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取最近的登录记录
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetRecentAsync(
            int count = 50, 
            CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .OrderByDescending(h => h.CreatedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取失败的登录尝试
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetFailedAttemptsAsync(
            Guid? userId = null, 
            string? ipAddress = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null, 
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(h => !h.IsSuccessful);

            if (userId.HasValue)
                query = query.Where(h => h.UserId == userId.Value);

            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(h => h.IpAddress == ipAddress);

            if (startDate.HasValue)
                query = query.Where(h => h.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(h => h.CreatedAt <= endDate.Value);

            return await query
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 删除过期的登录历史
        /// </summary>
        public async Task<int> DeleteOldHistoryAsync(
            int retentionDays = 90, 
            CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            
            var oldRecords = await _dbSet
                .Where(h => h.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldRecords.Any())
            {
                _dbSet.RemoveRange(oldRecords);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return oldRecords.Count;
        }

        /// <summary>
        /// 标记会话已登出
        /// </summary>
        public async Task<bool> MarkSessionLoggedOutAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _dbSet
                .FirstOrDefaultAsync(h => h.SessionId == sessionId, cancellationToken);

            if (session != null)
            {
                session.LogoutAt = DateTime.UtcNow;
                session.SessionDurationMinutes = (int)(session.LogoutAt.Value - session.CreatedAt).TotalMinutes;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取失败尝试次数
        /// </summary>
        public async Task<int> GetFailedAttemptsCountAsync(Guid userId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow.Subtract(timeWindow);
            return await _dbSet
                .CountAsync(h => h.UserId == userId && 
                               !h.IsSuccessful && 
                               h.CreatedAt >= startTime, cancellationToken);
        }

        /// <summary>
        /// 根据IP获取失败尝试次数
        /// </summary>
        public async Task<int> GetFailedAttemptsByIpCountAsync(string ipAddress, TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow.Subtract(timeWindow);
            return await _dbSet
                .CountAsync(h => h.IpAddress == ipAddress && 
                               !h.IsSuccessful && 
                               h.CreatedAt >= startTime, cancellationToken);
        }

        /// <summary>
        /// 检查是否有来自不同位置的并发会话
        /// </summary>
        public async Task<bool> HasConcurrentSessionsFromDifferentLocationsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var activeSessions = await _dbSet
                .Where(h => h.UserId == userId && 
                          h.IsSuccessful && 
                          h.LogoutAt == null &&
                          h.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .ToListAsync(cancellationToken);

            // Check if there are sessions from different IPs
            var uniqueIps = activeSessions.Select(s => s.IpAddress).Distinct().Count();
            return uniqueIps > 1;
        }

        /// <summary>
        /// 获取登录统计信息
        /// </summary>
        public async Task<LoginStatistics> GetLoginStatisticsAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var history = await _dbSet
                .Where(h => h.UserId == userId && 
                          h.CreatedAt >= startDate && 
                          h.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            return new LoginStatistics
            {
                TotalAttempts = history.Count,
                SuccessfulLogins = history.Count(h => h.IsSuccessful),
                FailedAttempts = history.Count(h => !h.IsSuccessful),
                UniqueIpAddresses = history.Select(h => h.IpAddress).Distinct().Count(),
                UniqueDevices = history.Select(h => h.DeviceInfo).Distinct().Count(),
                AverageSessionDuration = history
                    .Where(h => h.SessionDurationMinutes.HasValue)
                    .Select(h => h.SessionDurationMinutes!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                LastSuccessfulLogin = history.Where(h => h.IsSuccessful).Max(h => (DateTime?)h.CreatedAt),
                LastFailedLogin = history.Where(h => !h.IsSuccessful).Max(h => (DateTime?)h.CreatedAt),
                MostUsedIpAddress = history
                    .GroupBy(h => h.IpAddress)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key,
                MostUsedDevice = history
                    .GroupBy(h => h.DeviceInfo)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key
            };
        }

        /// <summary>
        /// 根据用户ID获取登录历史
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await GetUserLoginHistoryAsync(userId, page, pageSize, cancellationToken);
        }

        /// <summary>
        /// 获取活动会话
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(h => h.UserId == userId && 
                          h.IsSuccessful && 
                          h.LogoutAt == null &&
                          h.CreatedAt >= DateTime.UtcNow.AddHours(-24))
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 批量标记会话过期
        /// </summary>
        public async Task<int> BulkMarkSessionsExpiredAsync(TimeSpan sessionTimeout, CancellationToken cancellationToken = default)
        {
            var expiredTime = DateTime.UtcNow.Subtract(sessionTimeout);
            var expiredSessions = await _dbSet
                .Where(h => h.IsSuccessful && 
                          h.LogoutAt == null && 
                          h.CreatedAt < expiredTime)
                .ToListAsync(cancellationToken);

            foreach (var session in expiredSessions)
            {
                session.LogoutAt = DateTime.UtcNow;
                session.SessionDurationMinutes = (int)sessionTimeout.TotalMinutes;
            }

            if (expiredSessions.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return expiredSessions.Count;
        }

        /// <summary>
        /// 获取可疑活动
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetSuspiciousActivitiesAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow.Subtract(timeWindow);
            return await _dbSet
                .Where(h => h.CreatedAt >= startTime && 
                          (h.IsFlagged || h.RiskScore > 50))
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取标记的尝试
        /// </summary>
        public async Task<IEnumerable<LoginHistory>> GetFlaggedAttemptsAsync(TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow.Subtract(timeWindow);
            return await _dbSet
                .Where(h => h.CreatedAt >= startTime && h.IsFlagged)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 删除旧记录
        /// </summary>
        public async Task<int> DeleteOldRecordsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            return await DeleteOldHistoryAsync(retentionDays, cancellationToken);
        }

        /// <summary>
        /// 根据邮箱获取失败尝试次数
        /// </summary>
        public async Task<int> GetFailedAttemptsCountByEmailAsync(string email, TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow.Subtract(timeWindow);
            return await _dbSet
                .CountAsync(h => h.Email == email && 
                               !h.IsSuccessful && 
                               h.CreatedAt >= startTime, cancellationToken);
        }
    }
}