using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 用户资料管理服务实现
    /// </summary>
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly ILogger<UserProfileService> _logger;
        private readonly UserProfileSettings _settings;

        public UserProfileService(
            IUserRepository userRepository,
            IFileStorageService fileStorageService,
            IEmailVerificationService emailVerificationService,
            ILogger<UserProfileService> logger,
            IOptions<UserProfileSettings> settings)
        {
            _userRepository = userRepository;
            _fileStorageService = fileStorageService;
            _emailVerificationService = emailVerificationService;
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// 获取用户资料
        /// </summary>
        public async Task<UserProfileResult> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    return UserProfileResult.CreateError("用户不存在");
                }

                var profile = new UserProfileData
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email.Value,
                    DisplayName = user.DisplayName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    AvatarUrl = user.AvatarUrl,
                    Bio = user.Bio,
                    Website = user.Website,
                    Location = user.Location,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                };

                return UserProfileResult.CreateSuccess(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user {UserId}", userId);
                return UserProfileResult.CreateError("获取用户资料失败");
            }
        }

        /// <summary>
        /// 更新用户基本资料
        /// </summary>
        public async Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to update profile for non-existent user {UserId}", userId);
                    return false;
                }

                // 验证输入数据
                if (!ValidateProfileRequest(request))
                {
                    _logger.LogWarning("Invalid profile update request for user {UserId}", userId);
                    return false;
                }

                // 检查用户名是否已被使用（如果要更新用户名的话）
                // 注意：这里假设用户名不在UpdateProfileRequest中，如果需要支持用户名更改，需要添加额外验证

                // 更新用户资料
                user.UpdateProfile(
                    displayName: request.DisplayName,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    bio: request.Bio,
                    website: request.Website,
                    location: request.Location
                );

                if (request.DateOfBirth.HasValue)
                {
                    user.DateOfBirth = request.DateOfBirth.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Gender))
                {
                    user.Gender = request.Gender;
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    user.PhoneNumber = request.PhoneNumber;
                    user.PhoneNumberConfirmed = false; // 需要重新验证电话号码
                }

                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User profile updated successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 上传并更新用户头像
        /// </summary>
        public async Task<AvatarUploadResult> UpdateUserAvatarAsync(Guid userId, Stream avatarStream, string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    return AvatarUploadResult.CreateError("用户不存在");
                }

                // 验证文件
                if (!IsValidImageFile(fileName, avatarStream))
                {
                    return AvatarUploadResult.CreateError("无效的图片文件");
                }

                // 重置流位置
                avatarStream.Position = 0;

                // 生成唯一文件名
                var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
                var newFileName = $"avatar_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";

                // 上传文件
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    avatarStream,
                    newFileName,
                    "avatars",
                    cancellationToken);

                if (!uploadResult.Success)
                {
                    return AvatarUploadResult.CreateError(uploadResult.ErrorMessage ?? "文件上传失败");
                }

                // 删除旧头像
                if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old avatar for user {UserId}", userId);
                    }
                }

                // 更新用户头像URL
                user.AvatarUrl = uploadResult.FileUrl;
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Avatar updated successfully for user {UserId}", userId);
                return AvatarUploadResult.CreateSuccess(uploadResult.FileUrl!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar for user {UserId}", userId);
                return AvatarUploadResult.CreateError("头像更新失败");
            }
        }

        /// <summary>
        /// 删除用户头像
        /// </summary>
        public async Task<bool> DeleteUserAvatarAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to delete avatar for non-existent user {UserId}", userId);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(user.AvatarUrl))
                {
                    return true; // 没有头像，认为删除成功
                }

                // 删除文件
                await _fileStorageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);

                // 清空用户头像URL
                user.AvatarUrl = null;
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Avatar deleted successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 更改用户邮箱
        /// </summary>
        public async Task<bool> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to change email for non-existent user {UserId}", userId);
                    return false;
                }

                // 验证新邮箱格式
                if (!Email.IsValidEmail(newEmail))
                {
                    _logger.LogWarning("Invalid email format provided for user {UserId}: {Email}", userId, newEmail);
                    return false;
                }

                // 检查新邮箱是否已被使用
                var emailInUse = await _userRepository.IsEmailInUseAsync(newEmail, userId, cancellationToken);

                if (emailInUse)
                {
                    _logger.LogWarning("Email already in use for user {UserId}: {Email}", userId, newEmail);
                    return false;
                }

                // 更改邮箱
                user.ChangeEmail(newEmail);

                await _userRepository.SaveChangesAsync(cancellationToken);

                // 发送邮箱验证
                await _emailVerificationService.SendEmailVerificationAsync(user, cancellationToken);

                _logger.LogInformation("Email changed successfully for user {UserId} to {NewEmail}", userId, newEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing email for user {UserId} to {NewEmail}", userId, newEmail);
                return false;
            }
        }

        /// <summary>
        /// 更改用户密码
        /// </summary>
        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to change password for non-existent user {UserId}", userId);
                    return false;
                }

                // 验证当前密码
                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid current password provided for user {UserId}", userId);
                    return false;
                }

                // 验证新密码强度
                if (!IsValidPassword(newPassword))
                {
                    _logger.LogWarning("Weak new password provided for user {UserId}", userId);
                    return false;
                }

                // 检查新密码是否与当前密码相同
                if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
                {
                    _logger.LogWarning("New password is same as current password for user {UserId}", userId);
                    return false;
                }

                // 更新密码
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, _settings.BcryptWorkFactor);
                user.UpdatePassword(newPasswordHash);

                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 获取用户安全设置
        /// </summary>
        public async Task<UserSecuritySettings> GetUserSecuritySettingsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    throw new ArgumentException("用户不存在", nameof(userId));
                }

                return new UserSecuritySettings
                {
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnabled = user.LockoutEnabled,
                    EmailNotificationsEnabled = true, // 这些设置可能需要单独的实体存储
                    SecurityAlertsEnabled = true,
                    LoginNotificationsEnabled = true,
                    SessionTimeoutMinutes = 60,
                    AllowConcurrentLogins = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security settings for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 更新用户安全设置
        /// </summary>
        public async Task<bool> UpdateUserSecuritySettingsAsync(Guid userId, UserSecuritySettings settings, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to update security settings for non-existent user {UserId}", userId);
                    return false;
                }

                user.TwoFactorEnabled = settings.TwoFactorEnabled;
                user.LockoutEnabled = settings.LockoutEnabled;

                // 其他安全设置可能需要单独的实体存储
                // 这里只是示例，实际项目中可能需要创建 UserSecuritySettings 实体

                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Security settings updated successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating security settings for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 验证用户密码
        /// </summary>
        public async Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

                if (user == null)
                {
                    return false;
                }

                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 获取用户活动日志
        /// </summary>
        public async Task<IEnumerable<UserActivityLog>> GetUserActivityLogsAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                // 这里应该从专门的用户活动日志表中查询
                // 目前返回空列表作为占位符
                await Task.CompletedTask;
                return new List<UserActivityLog>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity logs for user {UserId}", userId);
                return new List<UserActivityLog>();
            }
        }

        /// <summary>
        /// 验证资料更新请求
        /// </summary>
        private bool ValidateProfileRequest(UpdateProfileRequest request)
        {
            // 验证显示名称长度
            if (!string.IsNullOrWhiteSpace(request.DisplayName) && request.DisplayName.Length > 100)
            {
                return false;
            }

            // 验证姓名长度
            if (!string.IsNullOrWhiteSpace(request.FirstName) && request.FirstName.Length > 50)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName) && request.LastName.Length > 50)
            {
                return false;
            }

            // 验证个人简介长度
            if (!string.IsNullOrWhiteSpace(request.Bio) && request.Bio.Length > 1000)
            {
                return false;
            }

            // 验证网站URL格式
            if (!string.IsNullOrWhiteSpace(request.Website))
            {
                if (!Uri.TryCreate(request.Website, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "http" && uri.Scheme != "https"))
                {
                    return false;
                }
            }

            // 验证位置长度
            if (!string.IsNullOrWhiteSpace(request.Location) && request.Location.Length > 100)
            {
                return false;
            }

            // 验证出生日期
            if (request.DateOfBirth.HasValue)
            {
                var age = DateTime.Today.Year - request.DateOfBirth.Value.Year;
                if (request.DateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;

                if (age < 13 || age > 120) // 年龄限制
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证图片文件
        /// </summary>
        private bool IsValidImageFile(string fileName, Stream fileStream)
        {
            // 验证文件扩展名
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return false;
            }

            // 验证文件大小
            if (fileStream.Length > _settings.MaxAvatarSizeBytes)
            {
                return false;
            }

            // 可以添加更多验证，如检查文件头等

            return true;
        }

        /// <summary>
        /// 验证密码强度
        /// </summary>
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < _settings.MinPasswordLength)
            {
                return false;
            }

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            int complexityScore = 0;
            if (hasUpper) complexityScore++;
            if (hasLower) complexityScore++;
            if (hasDigit) complexityScore++;
            if (hasSpecial) complexityScore++;

            return complexityScore >= _settings.MinPasswordComplexity;
        }
    }

    /// <summary>
    /// 用户资料服务设置
    /// </summary>
    public class UserProfileSettings
    {
        /// <summary>
        /// 最大头像文件大小（字节）
        /// </summary>
        public long MaxAvatarSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB

        /// <summary>
        /// BCrypt工作因子
        /// </summary>
        public int BcryptWorkFactor { get; set; } = 12;

        /// <summary>
        /// 最小密码长度
        /// </summary>
        public int MinPasswordLength { get; set; } = 8;

        /// <summary>
        /// 最小密码复杂性
        /// </summary>
        public int MinPasswordComplexity { get; set; } = 3;
    }

    /// <summary>
    /// 文件存储服务接口（需要实现）
    /// </summary>
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default);
        Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 文件上传结果
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? FileUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}