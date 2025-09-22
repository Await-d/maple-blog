using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for managing file shares
    /// </summary>
    public interface IFileShareRepository : IRepository<Entities.FileShare>
    {
        /// <summary>
        /// Get a file share by its share ID
        /// </summary>
        Task<Entities.FileShare?> GetByShareIdAsync(string shareId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all shares for a specific file
        /// </summary>
        Task<IEnumerable<Entities.FileShare>> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all files shared by a user
        /// </summary>
        Task<IEnumerable<Entities.FileShare>> GetSharedByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all files shared with a user
        /// </summary>
        Task<IEnumerable<Entities.FileShare>> GetSharedWithUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get active shares (not expired, not revoked)
        /// </summary>
        Task<IEnumerable<Entities.FileShare>> GetActiveSharesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a user has access to a file through shares
        /// </summary>
        Task<bool> UserHasAccessAsync(Guid fileId, Guid userId, FilePermission requiredPermission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increment access count for a share
        /// </summary>
        Task IncrementAccessCountAsync(Guid shareId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark a share as revoked
        /// </summary>
        Task<bool> RevokeShareAsync(Guid shareId, Guid revokedById, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clean up expired shares
        /// </summary>
        Task<int> DeleteExpiredSharesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get share statistics for a user
        /// </summary>
        Task<FileShareStatistics> GetShareStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Repository interface for file access logs
    /// </summary>
    public interface IFileAccessLogRepository : IRepository<FileAccessLog>
    {
        /// <summary>
        /// Get access logs for a file
        /// </summary>
        Task<IEnumerable<FileAccessLog>> GetByFileIdAsync(Guid fileId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get access logs for a user
        /// </summary>
        Task<IEnumerable<FileAccessLog>> GetByUserIdAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get access logs for a share
        /// </summary>
        Task<IEnumerable<FileAccessLog>> GetByShareIdAsync(Guid shareId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Log a file access
        /// </summary>
        Task LogAccessAsync(Guid fileId, Guid? userId, FileAccessType accessType, bool isSuccessful, string? ipAddress = null, string? userAgent = null, Guid? shareId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get access statistics for a file
        /// </summary>
        Task<FileAccessStatistics> GetAccessStatisticsAsync(Guid fileId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clean up old access logs
        /// </summary>
        Task<int> DeleteOldLogsAsync(int retentionDays, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// File share statistics
    /// </summary>
    public class FileShareStatistics
    {
        public int TotalShares { get; set; }
        public int ActiveShares { get; set; }
        public int ExpiredShares { get; set; }
        public int RevokedShares { get; set; }
        public int FilesShared { get; set; }
        public int FilesSharedWithUser { get; set; }
        public long TotalAccessCount { get; set; }
        public DateTime? LastShareCreated { get; set; }
        public DateTime? LastShareAccessed { get; set; }
    }

    /// <summary>
    /// File access statistics
    /// </summary>
    public class FileAccessStatistics
    {
        public int TotalAccesses { get; set; }
        public int UniqueUsers { get; set; }
        public int SuccessfulAccesses { get; set; }
        public int FailedAccesses { get; set; }
        public Dictionary<FileAccessType, int> AccessByType { get; set; } = new Dictionary<FileAccessType, int>();
        public List<AccessTrend> DailyTrends { get; set; } = new List<AccessTrend>();
        public DateTime? LastAccessed { get; set; }
        public string? MostFrequentUser { get; set; }
    }

    /// <summary>
    /// Daily access trend data
    /// </summary>
    public class AccessTrend
    {
        public DateTime Date { get; set; }
        public int AccessCount { get; set; }
        public int UniqueUsers { get; set; }
    }
}