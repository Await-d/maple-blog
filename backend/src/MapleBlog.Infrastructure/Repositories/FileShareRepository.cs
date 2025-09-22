using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
// using MapleBlog.Domain.Entities; // Commented out to avoid conflicts with System.IO.FileShare
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for managing file shares
    /// </summary>
    public class FileShareRepository : BaseRepository<Domain.Entities.FileShare>, IFileShareRepository
    {
        public FileShareRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get a file share by its share ID
        /// </summary>
        public async Task<Domain.Entities.FileShare?> GetByShareIdAsync(string shareId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(fs => fs.File)
                .Include(fs => fs.SharedBy)
                .Include(fs => fs.SharedWith)
                .FirstOrDefaultAsync(fs => fs.ShareId == shareId && fs.IsActive, cancellationToken);
        }

        /// <summary>
        /// Get all shares for a specific file
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileShare>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(fs => fs.SharedBy)
                .Include(fs => fs.SharedWith)
                .Where(fs => fs.FileId == fileId && fs.IsActive)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get all files shared by a user
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileShare>> GetSharedByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(fs => fs.File)
                .Include(fs => fs.SharedWith)
                .Where(fs => fs.SharedById == userId && fs.IsActive)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get all files shared with a user
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileShare>> GetSharedWithUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(fs => fs.File)
                .Include(fs => fs.SharedBy)
                .Where(fs => fs.SharedWithId == userId && fs.IsActive)
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get active shares (not expired, not revoked)
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileShare>> GetActiveSharesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(fs => fs.File)
                .Include(fs => fs.SharedBy)
                .Include(fs => fs.SharedWith)
                .Where(fs =>
                    (fs.SharedById == userId || fs.SharedWithId == userId) &&
                    fs.IsActive &&
                    fs.RevokedAt == null &&
                    (fs.ExpiresAt == null || fs.ExpiresAt > now) &&
                    (fs.MaxAccessCount == null || fs.AccessCount < fs.MaxAccessCount))
                .OrderByDescending(fs => fs.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Check if a user has access to a file through shares
        /// </summary>
        public async Task<bool> UserHasAccessAsync(Guid fileId, Guid userId, Domain.Entities.FilePermission requiredPermission, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var shares = await _dbSet
                .Where(fs =>
                    fs.FileId == fileId &&
                    (fs.SharedWithId == userId || fs.SharedWithId == null && !fs.RequiresAuthentication) &&
                    fs.IsActive &&
                    fs.RevokedAt == null &&
                    (fs.ExpiresAt == null || fs.ExpiresAt > now) &&
                    (fs.MaxAccessCount == null || fs.AccessCount < fs.MaxAccessCount))
                .ToListAsync(cancellationToken);

            return shares.Any(fs => (fs.Permission & requiredPermission) == requiredPermission);
        }

        /// <summary>
        /// Increment access count for a share
        /// </summary>
        public async Task IncrementAccessCountAsync(Guid shareId, CancellationToken cancellationToken = default)
        {
            var share = await _dbSet.FindAsync(new object[] { shareId }, cancellationToken);
            if (share != null)
            {
                share.AccessCount++;
                share.LastAccessedAt = DateTime.UtcNow;
                share.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Mark a share as revoked
        /// </summary>
        public async Task<bool> RevokeShareAsync(Guid shareId, Guid revokedById, string? reason = null, CancellationToken cancellationToken = default)
        {
            var share = await _dbSet.FindAsync(new object[] { shareId }, cancellationToken);
            if (share != null && share.IsActive)
            {
                share.IsActive = false;
                share.RevokedAt = DateTime.UtcNow;
                share.RevokedById = revokedById;
                share.RevocationReason = reason;
                share.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clean up expired shares
        /// </summary>
        public async Task<int> DeleteExpiredSharesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expiredShares = await _dbSet
                .Where(fs =>
                    fs.ExpiresAt != null && fs.ExpiresAt < now ||
                    fs.MaxAccessCount != null && fs.AccessCount >= fs.MaxAccessCount)
                .ToListAsync(cancellationToken);

            if (expiredShares.Any())
            {
                foreach (var share in expiredShares)
                {
                    share.IsActive = false;
                    share.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            return expiredShares.Count;
        }

        /// <summary>
        /// Get share statistics for a user
        /// </summary>
        public async Task<FileShareStatistics> GetShareStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var shares = await _dbSet
                .Where(fs => fs.SharedById == userId || fs.SharedWithId == userId)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            return new FileShareStatistics
            {
                TotalShares = shares.Count,
                ActiveShares = shares.Count(fs => fs.IsActive && fs.RevokedAt == null && (fs.ExpiresAt == null || fs.ExpiresAt > now)),
                ExpiredShares = shares.Count(fs => fs.ExpiresAt != null && fs.ExpiresAt <= now),
                RevokedShares = shares.Count(fs => fs.RevokedAt != null),
                FilesShared = shares.Where(fs => fs.SharedById == userId).Select(fs => fs.FileId).Distinct().Count(),
                FilesSharedWithUser = shares.Where(fs => fs.SharedWithId == userId).Select(fs => fs.FileId).Distinct().Count(),
                TotalAccessCount = shares.Sum(fs => (long)fs.AccessCount),
                LastShareCreated = shares.Where(fs => fs.SharedById == userId).Max(fs => (DateTime?)fs.CreatedAt),
                LastShareAccessed = shares.Max(fs => fs.LastAccessedAt)
            };
        }
    }

    /// <summary>
    /// Repository implementation for file access logs
    /// </summary>
    public class FileAccessLogRepository : BaseRepository<Domain.Entities.FileAccessLog>, IFileAccessLogRepository
    {
        public FileAccessLogRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Get access logs for a file
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileAccessLog>> GetByFileIdAsync(Guid fileId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(log => log.User)
                .Where(log => log.FileId == fileId);

            if (startDate.HasValue)
                query = query.Where(log => log.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.CreatedAt <= endDate.Value);

            return await query
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get access logs for a user
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileAccessLog>> GetByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Include(log => log.File)
                .Where(log => log.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(log => log.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.CreatedAt <= endDate.Value);

            return await query
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Get access logs for a share
        /// </summary>
        public async Task<IEnumerable<Domain.Entities.FileAccessLog>> GetByShareIdAsync(Guid shareId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(log => log.User)
                .Include(log => log.File)
                .Where(log => log.FileShareId == shareId)
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Log a file access
        /// </summary>
        public async Task LogAccessAsync(Guid fileId, Guid? userId, Domain.Entities.FileAccessType accessType, bool isSuccessful, string? ipAddress = null, string? userAgent = null, Guid? shareId = null, CancellationToken cancellationToken = default)
        {
            var log = new Domain.Entities.FileAccessLog
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                UserId = userId,
                FileShareId = shareId,
                AccessType = accessType,
                IsSuccessful = isSuccessful,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _dbSet.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Get access statistics for a file
        /// </summary>
        public async Task<FileAccessStatistics> GetAccessStatisticsAsync(Guid fileId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(log => log.FileId == fileId);

            if (startDate.HasValue)
                query = query.Where(log => log.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.CreatedAt <= endDate.Value);

            var logs = await query.ToListAsync(cancellationToken);

            var stats = new FileAccessStatistics
            {
                TotalAccesses = logs.Count,
                UniqueUsers = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().Count(),
                SuccessfulAccesses = logs.Count(l => l.IsSuccessful),
                FailedAccesses = logs.Count(l => !l.IsSuccessful),
                LastAccessed = logs.Max(l => (DateTime?)l.CreatedAt)
            };

            // Group by access type
            foreach (var group in logs.GroupBy(l => l.AccessType))
            {
                stats.AccessByType[group.Key] = group.Count();
            }

            // Calculate daily trends
            var dailyGroups = logs
                .GroupBy(l => l.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AccessTrend
                {
                    Date = g.Key,
                    AccessCount = g.Count(),
                    UniqueUsers = g.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().Count()
                });

            stats.DailyTrends.AddRange(dailyGroups);

            // Find most frequent user
            var userAccesses = logs
                .Where(l => l.UserId.HasValue)
                .GroupBy(l => l.UserId!.Value)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (userAccesses != null)
            {
                stats.MostFrequentUser = userAccesses.Key.ToString();
            }

            return stats;
        }

        /// <summary>
        /// Clean up old access logs
        /// </summary>
        public async Task<int> DeleteOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            var oldLogs = await _dbSet
                .Where(log => log.CreatedAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Any())
            {
                _dbSet.RemoveRange(oldLogs);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return oldLogs.Count;
        }
    }
}