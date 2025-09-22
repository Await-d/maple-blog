using MapleBlog.Application.DTOs.File;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Service for managing user file quotas and storage usage
    /// </summary>
    public interface IFileQuotaService
    {
        /// <summary>
        /// Gets user storage quota information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User storage quota details</returns>
        Task<UserStorageQuotaDto> GetUserQuotaAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if user has available storage space for a file
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileSize">Size of file to upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user has space, false otherwise</returns>
        Task<bool> HasAvailableSpaceAsync(Guid userId, long fileSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user's storage quota (admin function)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="newQuota">New quota in bytes</param>
        /// <param name="reason">Reason for quota change</param>
        /// <param name="adminUserId">Admin performing the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateUserQuotaAsync(Guid userId, long newQuota, string reason, Guid adminUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage usage statistics for all users
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Global storage statistics</returns>
        Task<GlobalStorageStatsDto> GetGlobalStorageStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets users approaching their storage limit
        /// </summary>
        /// <param name="warningThreshold">Threshold percentage (e.g., 0.8 for 80%)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of users approaching limit</returns>
        Task<IEnumerable<UserQuotaWarningDto>> GetUsersApproachingLimitAsync(double warningThreshold = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets quota usage history for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="days">Number of days to look back</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage usage history</returns>
        Task<IEnumerable<StorageUsageHistoryDto>> GetUsageHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recalculates storage usage for a user (maintenance function)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recalculated storage usage</returns>
        Task<long> RecalculateUserStorageAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends quota warning notification to user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="currentUsage">Current storage usage</param>
        /// <param name="quota">Total quota</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SendQuotaWarningAsync(Guid userId, long currentUsage, long quota, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up old quota history records
        /// </summary>
        /// <param name="olderThanDays">Days to keep</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of records cleaned</returns>
        Task<int> CleanupOldQuotaHistoryAsync(int olderThanDays = 90, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Global storage statistics
    /// </summary>
    public class GlobalStorageStatsDto
    {
        /// <summary>
        /// Total storage used across all users
        /// </summary>
        public long TotalStorageUsed { get; set; }

        /// <summary>
        /// Total storage allocated across all users
        /// </summary>
        public long TotalStorageAllocated { get; set; }

        /// <summary>
        /// Number of active users with files
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// Average storage usage per user
        /// </summary>
        public long AverageUsagePerUser { get; set; }

        /// <summary>
        /// Users over 80% quota usage
        /// </summary>
        public int UsersNearLimit { get; set; }

        /// <summary>
        /// Users at 100% quota usage
        /// </summary>
        public int UsersAtLimit { get; set; }

        /// <summary>
        /// Storage usage by user role
        /// </summary>
        public Dictionary<string, long> UsageByRole { get; set; } = new();

        /// <summary>
        /// Top users by storage usage
        /// </summary>
        public IEnumerable<UserStorageInfoDto> TopUsers { get; set; } = new List<UserStorageInfoDto>();
    }

    /// <summary>
    /// User quota warning information
    /// </summary>
    public class UserQuotaWarningDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Current storage usage
        /// </summary>
        public long CurrentUsage { get; set; }

        /// <summary>
        /// Storage quota
        /// </summary>
        public long Quota { get; set; }

        /// <summary>
        /// Usage percentage
        /// </summary>
        public double UsagePercentage { get; set; }

        /// <summary>
        /// Number of files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Last notification sent
        /// </summary>
        public DateTime? LastNotificationSent { get; set; }
    }

    /// <summary>
    /// Storage usage history entry
    /// </summary>
    public class StorageUsageHistoryDto
    {
        /// <summary>
        /// Date of usage record
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Storage usage on that date
        /// </summary>
        public long StorageUsed { get; set; }

        /// <summary>
        /// Number of files on that date
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Quota at that time
        /// </summary>
        public long Quota { get; set; }

        /// <summary>
        /// Usage percentage
        /// </summary>
        public double UsagePercentage { get; set; }
    }

    /// <summary>
    /// User storage information for statistics
    /// </summary>
    public class UserStorageInfoDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User role
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Storage usage
        /// </summary>
        public long StorageUsed { get; set; }

        /// <summary>
        /// Storage quota
        /// </summary>
        public long Quota { get; set; }

        /// <summary>
        /// Number of files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Usage percentage
        /// </summary>
        public double UsagePercentage { get; set; }
    }
}