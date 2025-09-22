using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Controllers
{
    /// <summary>
    /// 用户存储配额控制器
    /// </summary>
    [ApiController]
    [Route("api/user/storage")]
    [Authorize]
    [Tags("User Storage")]
    public class UserStorageController : ControllerBase
    {
        private readonly IStorageQuotaService _storageQuotaService;
        private readonly ILogger<UserStorageController> _logger;

        public UserStorageController(
            IStorageQuotaService storageQuotaService,
            ILogger<UserStorageController> logger)
        {
            _storageQuotaService = storageQuotaService ?? throw new ArgumentNullException(nameof(storageQuotaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取当前用户的存储配额信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>当前用户的存储配额信息</returns>
        [HttpGet("quota")]
        public async Task<IActionResult> GetMyStorageQuota(CancellationToken cancellationToken = default)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var quotaInfo = await _storageQuotaService.GetUserStorageQuotaAsync(userId, cancellationToken);
                return Ok(quotaInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage quota for current user");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 检查文件上传是否符合当前用户的配额限制
        /// </summary>
        /// <param name="fileSize">文件大小（字节）</param>
        /// <param name="mimeType">文件MIME类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>上传验证结果</returns>
        [HttpPost("validate-upload")]
        public async Task<IActionResult> ValidateMyFileUpload(
            [FromQuery] long fileSize,
            [FromQuery] string mimeType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

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
                _logger.LogError(ex, "Error validating file upload for current user");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 检查当前用户是否有足够的存储空间
        /// </summary>
        /// <param name="fileSize">需要的文件大小（字节）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>空间可用性检查结果</returns>
        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckMyStorageAvailability(
            [FromQuery] long fileSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                if (fileSize <= 0)
                {
                    return BadRequest(new { message = "File size must be greater than 0" });
                }

                var hasSpace = await _storageQuotaService.CheckStorageAvailabilityAsync(userId, fileSize, cancellationToken);
                var quotaInfo = await _storageQuotaService.GetUserStorageQuotaAsync(userId, cancellationToken);

                return Ok(new
                {
                    hasAvailableSpace = hasSpace,
                    requestedSize = fileSize,
                    currentUsage = quotaInfo.CurrentUsage,
                    maxQuota = quotaInfo.MaxQuota,
                    availableSpace = quotaInfo.AvailableSpace,
                    usagePercentage = quotaInfo.UsagePercentage,
                    formattedRequestedSize = FormatBytes(fileSize),
                    formattedCurrentUsage = quotaInfo.FormattedCurrentUsage,
                    formattedMaxQuota = quotaInfo.FormattedMaxQuota,
                    formattedAvailableSpace = quotaInfo.FormattedAvailableSpace
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage availability for current user");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 获取当前用户的存储使用量统计
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>存储使用量统计</returns>
        [HttpGet("usage")]
        public async Task<IActionResult> GetMyStorageUsage(CancellationToken cancellationToken = default)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var storageUsage = await _storageQuotaService.CalculateUserStorageUsageAsync(userId, cancellationToken);
                var fileCount = await _storageQuotaService.GetUserFileCountAsync(userId, cancellationToken);
                var quotaInfo = await _storageQuotaService.GetUserStorageQuotaAsync(userId, cancellationToken);

                return Ok(new
                {
                    userId,
                    storageUsage,
                    fileCount,
                    maxQuota = quotaInfo.MaxQuota,
                    availableSpace = quotaInfo.AvailableSpace,
                    usagePercentage = quotaInfo.UsagePercentage,
                    isQuotaExceeded = quotaInfo.IsQuotaExceeded,
                    statusMessage = quotaInfo.StatusMessage,
                    formattedStorageUsage = FormatBytes(storageUsage),
                    formattedMaxQuota = quotaInfo.FormattedMaxQuota,
                    formattedAvailableSpace = quotaInfo.FormattedAvailableSpace
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage usage for current user");
                return StatusCode(500, new { message = "Internal server error occurred" });
            }
        }

        /// <summary>
        /// 格式化字节大小为人类可读格式
        /// </summary>
        /// <param name="bytes">字节数</param>
        /// <returns>格式化的字符串</returns>
        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            if (bytes < 0) return "无限制";

            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}