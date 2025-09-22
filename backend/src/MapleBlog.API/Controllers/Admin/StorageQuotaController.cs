using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.API.Controllers.Admin
{
    /// <summary>
    /// 存储配额管理控制器
    /// </summary>
    [ApiController]
    [Route("api/admin/storage-quota")]
    [Authorize]
    [Tags("Admin - Storage Quota")]
    public class StorageQuotaController : ControllerBase
    {
        private readonly IStorageQuotaService _storageQuotaService;
        private readonly ILogger<StorageQuotaController> _logger;

        public StorageQuotaController(
            IStorageQuotaService storageQuotaService,
            ILogger<StorageQuotaController> logger)
        {
            _storageQuotaService = storageQuotaService ?? throw new ArgumentNullException(nameof(storageQuotaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取指定用户的存储配额信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户存储配额信息</returns>
        [HttpGet("users/{userId:guid}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetUserStorageQuota(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var quotaInfo = await _storageQuotaService.GetUserStorageQuotaAsync(userId, cancellationToken);
                return Ok(quotaInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage quota for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 获取所有角色的配额配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>所有角色配额配置</returns>
        [HttpGet("configurations")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllQuotaConfigurations(CancellationToken cancellationToken = default)
        {
            try
            {
                var configurations = await _storageQuotaService.GetAllRoleQuotaConfigurationsAsync(cancellationToken);
                return Ok(configurations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota configurations");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 获取指定角色的配额配置
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>角色配额配置</returns>
        [HttpGet("configurations/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRoleQuotaConfiguration(MapleBlog.Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = await _storageQuotaService.GetRoleQuotaConfigurationAsync(role, cancellationToken);
                return Ok(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota configuration for role {Role}", role);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 更新角色的配额配置
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="configuration">新的配额配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        [HttpPut("configurations/{role}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoleQuotaConfiguration(
            MapleBlog.Domain.Enums.UserRole role,
            [FromBody] StorageQuotaConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (configuration == null)
                {
                    return BadRequest(new { message = "Configuration data is required" });
                }

                var result = await _storageQuotaService.UpdateRoleQuotaConfigurationAsync(role, configuration, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Storage quota configuration updated for role {Role}", role);
                    return Ok(new { message = "Configuration updated successfully" });
                }

                return BadRequest(new { message = "Failed to update configuration" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quota configuration for role {Role}", role);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 初始化默认配额配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>初始化结果</returns>
        [HttpPost("configurations/initialize")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> InitializeDefaultConfigurations(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _storageQuotaService.InitializeDefaultQuotaConfigurationsAsync(cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Default storage quota configurations initialized");
                    return Ok(new { message = "Default configurations initialized successfully" });
                }

                return BadRequest(new { message = "Failed to initialize default configurations" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default quota configurations");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 获取接近配额限制的用户列表
        /// </summary>
        /// <param name="thresholdPercentage">阈值百分比（0.8 = 80%）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>接近限制的用户列表</returns>
        [HttpGet("warnings")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> GetUsersNearQuotaLimit(
            [FromQuery] double thresholdPercentage = 0.8,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (thresholdPercentage <= 0 || thresholdPercentage > 1)
                {
                    return BadRequest(new { message = "Threshold percentage must be between 0 and 1" });
                }

                var warnings = await _storageQuotaService.GetUsersNearQuotaLimitAsync(thresholdPercentage, cancellationToken);
                return Ok(warnings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users near quota limit");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 发送配额警告通知
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="warningType">警告类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发送结果</returns>
        [HttpPost("warnings/{userId:guid}/send")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> SendQuotaWarning(
            Guid userId,
            [FromQuery] QuotaWarningType warningType = QuotaWarningType.Approaching,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _storageQuotaService.SendQuotaWarningNotificationAsync(userId, warningType, cancellationToken);

                _logger.LogInformation("Quota warning {WarningType} sent to user {UserId}", warningType, userId);
                return Ok(new { message = "Warning notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending quota warning to user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 获取系统存储使用统计
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>系统存储统计信息</returns>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemStorageStats(CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _storageQuotaService.GetSystemStorageStatsAsync(cancellationToken);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system storage statistics");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 验证文件上传是否符合配额限制
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="mimeType">文件MIME类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        [HttpPost("validate-upload")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<IActionResult> ValidateFileUpload(
            [FromQuery] Guid userId,
            [FromQuery] long fileSize,
            [FromQuery] string mimeType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    return BadRequest(new { message = "MIME type is required" });
                }

                if (fileSize <= 0)
                {
                    return BadRequest(new { message = "File size must be greater than 0" });
                }

                var validationResult = await _storageQuotaService.ValidateFileUploadAsync(userId, fileSize, mimeType, cancellationToken);
                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file upload for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 重新计算用户存储使用量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重新计算的存储使用量</returns>
        [HttpPost("users/{userId:guid}/recalculate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateUserStorage(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var usage = await _storageQuotaService.CalculateUserStorageUsageAsync(userId, cancellationToken);

                _logger.LogInformation("Storage usage recalculated for user {UserId}: {Usage} bytes", userId, usage);
                return Ok(new { userId, storageUsage = usage, message = "Storage usage recalculated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating storage usage for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 清理过期的配额历史记录
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理结果</returns>
        [HttpDelete("history/cleanup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupExpiredQuotaHistory(
            [FromQuery] int retentionDays = 90,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (retentionDays <= 0)
                {
                    return BadRequest(new { message = "Retention days must be greater than 0" });
                }

                var cleanedCount = await _storageQuotaService.CleanupExpiredQuotaHistoryAsync(retentionDays, cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired quota history records", cleanedCount);
                return Ok(new { cleanedRecords = cleanedCount, message = "History cleanup completed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up quota history");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }
    }
}