using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs.File;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 基于角色的存储配额管理服务实现
    /// </summary>
    public class StorageQuotaService : IStorageQuotaService
    {
        private readonly ILogger<StorageQuotaService> _logger;
        private readonly IStorageQuotaConfigurationRepository _quotaConfigRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IEmailService _emailService;

        public StorageQuotaService(
            ILogger<StorageQuotaService> logger,
            IStorageQuotaConfigurationRepository quotaConfigRepository,
            IRepository<User> userRepository,
            IFileRepository fileRepository,
            IEmailService emailService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _quotaConfigRepository = quotaConfigRepository ?? throw new ArgumentNullException(nameof(quotaConfigRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<UserStorageQuotaDto> GetUserStorageQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for storage quota check: {UserId}", userId);
                    return new UserStorageQuotaDto
                    {
                        UserId = userId,
                        IsQuotaExceeded = true
                    };
                }

                var quotaConfig = await GetRoleQuotaConfigurationAsync(user.Role, cancellationToken);
                var currentUsage = await CalculateUserStorageUsageAsync(userId, cancellationToken);
                var fileCount = await GetUserFileCountAsync(userId, cancellationToken);

                var maxQuota = quotaConfig.MaxQuotaBytes;
                var availableSpace = maxQuota < 0 ? long.MaxValue : Math.Max(0, maxQuota - currentUsage);
                var usagePercentage = maxQuota > 0 ? (double)currentUsage / maxQuota * 100 : 0;

                return new UserStorageQuotaDto
                {
                    UserId = userId,
                    CurrentUsage = currentUsage,
                    MaxQuota = maxQuota,
                    FileCount = fileCount,
                    AvailableSpace = availableSpace,
                    UsagePercentage = usagePercentage,
                    IsQuotaExceeded = maxQuota > 0 && currentUsage > maxQuota
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user storage quota for {UserId}", userId);
                return new UserStorageQuotaDto
                {
                    UserId = userId,
                    IsQuotaExceeded = true
                };
            }
        }

        public async Task<StorageQuotaConfiguration> GetRoleQuotaConfigurationAsync(UserRoleEnum role, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = await _quotaConfigRepository.GetEffectiveConfigurationByRoleAsync(role, cancellationToken: cancellationToken);
                if (config != null)
                {
                    return config;
                }

                // 如果没有找到配置，返回默认配置
                _logger.LogWarning("No quota configuration found for role {Role}, using default configuration", role);
                return StorageQuotaConfiguration.CreateDefault(role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota configuration for role {Role}", role);
                return StorageQuotaConfiguration.CreateDefault(role);
            }
        }

        /// <inheritdoc />
        /// <inheritdoc />
        /// <inheritdoc />
        public async Task<bool> CheckUploadPermissionAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // 获取用户信息
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return false;

            // 管理员总是有上传权限
            if (user.Role == UserRoleEnum.Admin)
                return true;

            // 检查用户状态是否正常
            if (!user.IsActive)
                return false;

            // 获取用户的角色配额配置
            var quotaConfig = await GetRoleQuotaConfigurationAsync(user.Role, cancellationToken);
            
            // 如果配额配置为0，表示该角色没有上传权限
            if (quotaConfig.MaxQuotaBytes == 0)
                return false;

            return true;
        }

        public async Task<bool> CheckStorageAvailabilityAsync(Guid userId, long fileSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var quotaInfo = await GetUserStorageQuotaAsync(userId, cancellationToken);

                // SuperAdmin 无限制
                if (quotaInfo.MaxQuota < 0)
                    return true;

                return quotaInfo.AvailableSpace >= fileSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage availability for user {UserId}, file size {FileSize}", userId, fileSize);
                return false;
            }
        }

        public async Task<QuotaValidationResultDto> ValidateFileUploadAsync(Guid userId, long fileSize, string mimeType, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return new QuotaValidationResultDto
                    {
                        IsValid = false,
                        FailureReason = "User not found"
                    };
                }

                var quotaConfig = await GetRoleQuotaConfigurationAsync(user.Role, cancellationToken);
                var currentUsage = await CalculateUserStorageUsageAsync(userId, cancellationToken);
                var currentFileCount = await GetUserFileCountAsync(userId, cancellationToken);

                var result = new QuotaValidationResultDto
                {
                    CurrentUsage = currentUsage,
                    TotalQuota = quotaConfig.MaxQuotaBytes,
                    RemainingSpace = quotaConfig.MaxQuotaBytes < 0 ? long.MaxValue : Math.Max(0, quotaConfig.MaxQuotaBytes - currentUsage)
                };

                // 检查文件类型
                if (!quotaConfig.IsFileTypeAllowed(mimeType))
                {
                    result.IsValid = false;
                    result.FailureReason = "File type not allowed";
                    result.IsFileTypeAllowed = false;
                    return result;
                }
                result.IsFileTypeAllowed = true;

                // 检查单文件大小限制
                if (!quotaConfig.IsFileSizeAllowed(fileSize))
                {
                    result.IsValid = false;
                    result.FailureReason = $"File size exceeds limit ({quotaConfig.FormattedMaxFileSize})";
                    result.ExceedsFileSizeLimit = true;
                    return result;
                }

                // 检查文件数量限制
                if (quotaConfig.MaxFileCount.HasValue && currentFileCount >= quotaConfig.MaxFileCount.Value)
                {
                    result.IsValid = false;
                    result.FailureReason = $"File count limit exceeded ({quotaConfig.MaxFileCount})";
                    result.ExceedsFileCountLimit = true;
                    return result;
                }

                // 检查存储空间
                if (quotaConfig.MaxQuotaBytes > 0 && (currentUsage + fileSize) > quotaConfig.MaxQuotaBytes)
                {
                    result.IsValid = false;
                    result.FailureReason = $"Storage quota exceeded. Available: {result.RemainingSpace} bytes";
                    return result;
                }

                result.IsValid = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file upload for user {UserId}", userId);
                return new QuotaValidationResultDto
                {
                    IsValid = false,
                    FailureReason = "Validation error occurred"
                };
            }
        }

        public async Task<long> CalculateUserStorageUsageAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _fileRepository.GetUserStorageUsageAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating storage usage for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<int> GetUserFileCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _fileRepository.GetUserFileCountAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<IEnumerable<StorageQuotaConfiguration>> GetAllRoleQuotaConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _quotaConfigRepository.GetEffectiveConfigurationsAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all role quota configurations");
                return Enumerable.Empty<StorageQuotaConfiguration>();
            }
        }

        public async Task<bool> UpdateRoleQuotaConfigurationAsync(UserRoleEnum role, StorageQuotaConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                configuration.Role = role;
                configuration.UpdatedAt = DateTime.UtcNow;

                return await _quotaConfigRepository.UpsertConfigurationsAsync(new[] { configuration }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quota configuration for role {Role}", role);
                return false;
            }
        }

        public async Task<bool> InitializeDefaultQuotaConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var roles = new[]
                {
                    Domain.Enums.UserRole.Guest,
                    Domain.Enums.UserRole.User,
                    Domain.Enums.UserRole.Author,
                    Domain.Enums.UserRole.Moderator,
                    Domain.Enums.UserRole.Admin,
                    Domain.Enums.UserRole.SuperAdmin
                };

                var configurations = new List<StorageQuotaConfiguration>();

                foreach (var role in roles)
                {
                    var hasConfig = await _quotaConfigRepository.HasConfigurationAsync(role, cancellationToken);
                    if (!hasConfig)
                    {
                        configurations.Add(StorageQuotaConfiguration.CreateDefault(role));
                    }
                }

                if (configurations.Any())
                {
                    var result = await _quotaConfigRepository.UpsertConfigurationsAsync(configurations, cancellationToken);
                    _logger.LogInformation("Initialized {Count} default quota configurations", configurations.Count);
                    return result;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default quota configurations");
                return false;
            }
        }

        public async Task<IEnumerable<UserQuotaWarningDto>> GetUsersNearQuotaLimitAsync(double thresholdPercentage = 0.8, CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.GetAllAsync(cancellationToken);
                var warnings = new List<UserQuotaWarningDto>();

                foreach (var user in users)
                {
                    var quotaInfo = await GetUserStorageQuotaAsync(user.Id, cancellationToken);

                    if (quotaInfo.MaxQuota > 0 && quotaInfo.UsagePercentage >= thresholdPercentage * 100)
                    {
                        warnings.Add(new UserQuotaWarningDto
                        {
                            UserId = user.Id,
                            UserName = user.UserName,
                            Email = user.Email,
                            CurrentUsage = quotaInfo.CurrentUsage,
                            Quota = quotaInfo.MaxQuota,
                            UsagePercentage = quotaInfo.UsagePercentage,
                            FileCount = quotaInfo.FileCount
                        });
                    }
                }

                return warnings.OrderByDescending(w => w.UsagePercentage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users near quota limit");
                return Enumerable.Empty<UserQuotaWarningDto>();
            }
        }

        public async Task SendQuotaWarningNotificationAsync(Guid userId, QuotaWarningType warningType, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for quota warning notification: {UserId}", userId);
                    return;
                }

                var quotaInfo = await GetUserStorageQuotaAsync(userId, cancellationToken);

                _logger.LogWarning("Storage quota {WarningType} warning for user {UserId} ({UserName}): {UsagePercentage:F1}% usage ({CurrentUsage}/{MaxQuota} bytes)",
                    warningType, userId, user.UserName, quotaInfo.UsagePercentage, quotaInfo.CurrentUsage, quotaInfo.MaxQuota);

                // Send email notification
                var warningTypeStr = warningType switch
                {
                    QuotaWarningType.Approaching => "warning",
                    QuotaWarningType.Critical => "critical",
                    QuotaWarningType.Exceeded => "exceeded",
                    _ => "info"
                };
                
                await _emailService.SendQuotaWarningEmailAsync(user.Email, warningTypeStr, new
                {
                    UsedSpace = quotaInfo.CurrentUsage,
                    MaxSpace = quotaInfo.MaxQuota,
                    FileCount = quotaInfo.FileCount,
                    UsagePercentage = quotaInfo.UsagePercentage
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending quota warning notification for user {UserId}", userId);
            }
        }

        public async Task<int> CleanupExpiredQuotaHistoryAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
        {
            try
            {
                // 清理过期的配额配置
                var deactivatedCount = await _quotaConfigRepository.DeactivateExpiredConfigurationsAsync(cancellationToken: cancellationToken);

                _logger.LogInformation("Deactivated {Count} expired quota configurations", deactivatedCount);

                // TODO: 实现配额历史记录清理
                // var cleanedHistoryCount = await _quotaHistoryRepository.CleanupOldRecordsAsync(retentionDays, cancellationToken);

                return deactivatedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired quota history");
                return 0;
            }
        }

        public async Task<SystemStorageStatsDto> GetSystemStorageStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var allUsers = await _userRepository.GetAllAsync(cancellationToken);
                var roleQuotaStats = await _quotaConfigRepository.GetRoleQuotaStatsAsync(cancellationToken);

                var stats = new SystemStorageStatsDto();
                var roleUsageStats = new Dictionary<UserRoleEnum, RoleStorageStatsDto>();

                foreach (var roleGroup in allUsers.GroupBy(u => u.Role))
                {
                    var role = roleGroup.Key;
                    var roleUsers = roleGroup.ToList();

                    long totalUsage = 0;
                    int activeUserCount = 0;

                    foreach (var user in roleUsers)
                    {
                        var userUsage = await CalculateUserStorageUsageAsync(user.Id, cancellationToken);
                        totalUsage += userUsage;
                        if (userUsage > 0) activeUserCount++;
                    }

                    var roleQuota = roleQuotaStats.TryGetValue(role, out var quota) ? quota : 0;
                    var totalRoleQuota = roleQuota > 0 ? roleQuota * roleUsers.Count : 0;

                    roleUsageStats[role] = new RoleStorageStatsDto
                    {
                        Role = role,
                        UserCount = roleUsers.Count,
                        TotalUsage = totalUsage,
                        TotalQuota = totalRoleQuota
                    };

                    stats.TotalStorageUsed += totalUsage;
                    stats.TotalQuotaAllocated += totalRoleQuota;
                    stats.ActiveUserCount += activeUserCount;
                }

                stats.UsageByRole = roleUsageStats;

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system storage statistics");
                return new SystemStorageStatsDto();
            }
        }
    }
}