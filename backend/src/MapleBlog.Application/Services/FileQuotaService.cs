using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs.File;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// Service for managing user file quotas and storage usage
    /// </summary>
    public class FileQuotaService : IFileQuotaService
    {
        private readonly ILogger<FileQuotaService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFilePermissionService _filePermissionService;
        private readonly IEmailService _emailService;

        public FileQuotaService(
            ILogger<FileQuotaService> logger,
            IConfiguration configuration,
            IRepository<User> userRepository,
            IFileRepository fileRepository,
            IFilePermissionService filePermissionService,
            IEmailService emailService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _filePermissionService = filePermissionService ?? throw new ArgumentNullException(nameof(filePermissionService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<UserStorageQuotaDto> GetUserQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for quota check: {UserId}", userId);
                    return new UserStorageQuotaDto
                    {
                        UserId = userId,
                        IsQuotaExceeded = true
                    };
                }

                var permissions = await _filePermissionService.GetUserFilePermissionsAsync(userId, cancellationToken);
                var currentUsage = await _fileRepository.GetUserStorageUsageAsync(userId, cancellationToken);
                var fileCount = await _fileRepository.GetUserFileCountAsync(userId, cancellationToken);

                var quota = permissions.StorageQuota;
                var availableSpace = Math.Max(0, quota - currentUsage);
                var usagePercentage = quota > 0 ? (double)currentUsage / quota * 100 : 0;

                return new UserStorageQuotaDto
                {
                    UserId = userId,
                    CurrentUsage = currentUsage,
                    MaxQuota = quota,
                    FileCount = fileCount,
                    AvailableSpace = availableSpace,
                    UsagePercentage = usagePercentage,
                    IsQuotaExceeded = currentUsage > quota
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user quota for {UserId}", userId);
                return new UserStorageQuotaDto
                {
                    UserId = userId,
                    IsQuotaExceeded = true
                };
            }
        }

        public async Task<bool> HasAvailableSpaceAsync(Guid userId, long fileSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var quotaInfo = await GetUserQuotaAsync(userId, cancellationToken);
                return quotaInfo.AvailableSpace >= fileSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking available space for user {UserId}, file size {FileSize}", userId, fileSize);
                return false;
            }
        }

        public async Task<bool> UpdateUserQuotaAsync(Guid userId, long newQuota, string reason, Guid adminUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for quota update: {UserId}", userId);
                    return false;
                }

                // For now, we'll log the quota change
                // In a production system, you might want to store custom quotas in a separate table
                _logger.LogInformation("Quota update requested for user {UserId}: {NewQuota} bytes. Reason: {Reason}. Admin: {AdminUserId}",
                    userId, newQuota, reason, adminUserId);

                // TODO: Implement custom quota storage in database
                // This would involve creating a UserQuota entity and repository

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quota for user {UserId}", userId);
                return false;
            }
        }

        public async Task<GlobalStorageStatsDto> GetGlobalStorageStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
                var activeFiles = allFiles.Where(f => !f.IsDeleted).ToList();

                var totalStorageUsed = activeFiles.Sum(f => f.FileSize);
                var totalStorageAllocated = 0L;
                var usageByRole = new Dictionary<string, long>();
                var topUsers = new List<UserStorageInfoDto>();

                foreach (var user in allUsers)
                {
                    var userRole = user.Role;
                    var permissions = await _filePermissionService.GetUserFilePermissionsAsync(user.Id, cancellationToken);
                    totalStorageAllocated += permissions.StorageQuota;

                    var userFiles = activeFiles.Where(f => f.UserId == user.Id).ToList();
                    var userUsage = userFiles.Sum(f => f.FileSize);

                    if (!usageByRole.ContainsKey(userRole.ToString()))
                        usageByRole[userRole.ToString()] = 0;
                    usageByRole[userRole.ToString()] += userUsage;

                    if (userFiles.Any())
                    {
                        topUsers.Add(new UserStorageInfoDto
                        {
                            UserId = user.Id,
                            UserName = user.UserName,
                            Role = userRole.ToString(),
                            StorageUsed = userUsage,
                            Quota = permissions.StorageQuota,
                            FileCount = userFiles.Count,
                            UsagePercentage = permissions.StorageQuota > 0 ? (double)userUsage / permissions.StorageQuota * 100 : 0
                        });
                    }
                }

                var activeUsers = topUsers.Count;
                var averageUsagePerUser = activeUsers > 0 ? totalStorageUsed / activeUsers : 0;
                var usersNearLimit = topUsers.Count(u => u.UsagePercentage >= 80);
                var usersAtLimit = topUsers.Count(u => u.UsagePercentage >= 100);

                return new GlobalStorageStatsDto
                {
                    TotalStorageUsed = totalStorageUsed,
                    TotalStorageAllocated = totalStorageAllocated,
                    ActiveUsers = activeUsers,
                    AverageUsagePerUser = averageUsagePerUser,
                    UsersNearLimit = usersNearLimit,
                    UsersAtLimit = usersAtLimit,
                    UsageByRole = usageByRole,
                    TopUsers = topUsers.OrderByDescending(u => u.StorageUsed).Take(10)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global storage statistics");
                return new GlobalStorageStatsDto();
            }
        }

        public async Task<IEnumerable<UserQuotaWarningDto>> GetUsersApproachingLimitAsync(double warningThreshold = 0.8, CancellationToken cancellationToken = default)
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                var warnings = new List<UserQuotaWarningDto>();

                foreach (var user in allUsers)
                {
                    var quotaInfo = await GetUserQuotaAsync(user.Id, cancellationToken);

                    if (quotaInfo.UsagePercentage >= warningThreshold * 100)
                    {
                        warnings.Add(new UserQuotaWarningDto
                        {
                            UserId = user.Id,
                            UserName = user.UserName,
                            Email = user.Email,
                            CurrentUsage = quotaInfo.CurrentUsage,
                            Quota = quotaInfo.MaxQuota,
                            UsagePercentage = quotaInfo.UsagePercentage,
                            FileCount = quotaInfo.FileCount,
                            LastNotificationSent = null // TODO: Track notification history
                        });
                    }
                }

                return warnings.OrderByDescending(w => w.UsagePercentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users approaching quota limit");
                return new List<UserQuotaWarningDto>();
            }
        }

        public async Task<IEnumerable<StorageUsageHistoryDto>> GetUsageHistoryAsync(Guid userId, int days = 30, CancellationToken cancellationToken = default)
        {
            try
            {
                // For now, we'll generate historical data based on current state
                // In a production system, you would store daily usage snapshots
                var history = new List<StorageUsageHistoryDto>();
                var quotaInfo = await GetUserQuotaAsync(userId, cancellationToken);

                for (int i = days; i >= 0; i--)
                {
                    var date = DateTime.UtcNow.Date.AddDays(-i);

                    // Simulate historical usage (in production, this would come from stored snapshots)
                    var simulatedUsage = (long)(quotaInfo.CurrentUsage * (0.5 + (double)(days - i) / days * 0.5));
                    var simulatedFileCount = (int)(quotaInfo.FileCount * (0.5 + (double)(days - i) / days * 0.5));

                    history.Add(new StorageUsageHistoryDto
                    {
                        Date = date,
                        StorageUsed = simulatedUsage,
                        FileCount = simulatedFileCount,
                        Quota = quotaInfo.MaxQuota,
                        UsagePercentage = quotaInfo.MaxQuota > 0 ? (double)simulatedUsage / quotaInfo.MaxQuota * 100 : 0
                    });
                }

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage history for user {UserId}", userId);
                return new List<StorageUsageHistoryDto>();
            }
        }

        public async Task<long> RecalculateUserStorageAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userFiles = await _fileRepository.GetAllByUserIdAsync(userId, cancellationToken);
                var activeFiles = userFiles.Where(f => !f.IsDeleted);
                var totalUsage = activeFiles.Sum(f => f.FileSize);

                _logger.LogInformation("Recalculated storage usage for user {UserId}: {TotalUsage} bytes from {FileCount} files",
                    userId, totalUsage, activeFiles.Count());

                return totalUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating storage for user {UserId}", userId);
                return 0;
            }
        }

        public async Task SendQuotaWarningAsync(Guid userId, long currentUsage, long quota, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for quota warning: {UserId}", userId);
                    return;
                }

                var usagePercentage = quota > 0 ? (double)currentUsage / quota * 100 : 0;

                _logger.LogWarning("Quota warning for user {UserId} ({UserName}): {UsagePercentage:F1}% usage ({CurrentUsage}/{Quota} bytes)",
                    userId, user.UserName, usagePercentage, currentUsage, quota);

                // Send email notification
                var warningType = usagePercentage >= 95 ? "critical" : usagePercentage >= 80 ? "warning" : "info";
                await _emailService.SendQuotaWarningEmailAsync(user.Email, warningType, new
                {
                    UsedSpace = currentUsage,
                    MaxSpace = quota,
                    FileCount = await _fileRepository.GetUserFileCountAsync(userId, cancellationToken),
                    UsagePercentage = usagePercentage
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending quota warning for user {UserId}", userId);
            }
        }

        public async Task<int> CleanupOldQuotaHistoryAsync(int olderThanDays = 90, CancellationToken cancellationToken = default)
        {
            try
            {
                // For now, we'll just log the cleanup request
                // In a production system, you would clean up historical usage records
                _logger.LogInformation("Quota history cleanup requested for records older than {Days} days", olderThanDays);

                // TODO: Implement cleanup of historical usage records
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up quota history");
                return 0;
            }
        }
    }
}