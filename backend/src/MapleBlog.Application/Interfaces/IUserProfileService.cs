using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 用户资料管理服务接口
    /// </summary>
    public interface IUserProfileService
    {
        /// <summary>
        /// 获取用户资料
        /// </summary>
        Task<UserProfileResult> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户基本资料
        /// </summary>
        Task<bool> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 上传并更新用户头像
        /// </summary>
        Task<AvatarUploadResult> UpdateUserAvatarAsync(Guid userId, Stream avatarStream, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除用户头像
        /// </summary>
        Task<bool> DeleteUserAvatarAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更改用户邮箱
        /// </summary>
        Task<bool> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更改用户密码
        /// </summary>
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户安全设置
        /// </summary>
        Task<UserSecuritySettings> GetUserSecuritySettingsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新用户安全设置
        /// </summary>
        Task<bool> UpdateUserSecuritySettingsAsync(Guid userId, UserSecuritySettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证用户密码
        /// </summary>
        Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户活动日志
        /// </summary>
        Task<IEnumerable<UserActivityLog>> GetUserActivityLogsAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 用户资料结果
    /// </summary>
    public class UserProfileResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public UserProfileData? Profile { get; set; }

        public static UserProfileResult CreateSuccess(UserProfileData profile)
        {
            return new UserProfileResult
            {
                Success = true,
                Profile = profile
            };
        }

        public static UserProfileResult CreateError(string errorMessage)
        {
            return new UserProfileResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// 用户资料数据
    /// </summary>
    public class UserProfileData
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// 更新资料请求
    /// </summary>
    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// 头像上传结果
    /// </summary>
    public class AvatarUploadResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AvatarUrl { get; set; }

        public static AvatarUploadResult CreateSuccess(string avatarUrl)
        {
            return new AvatarUploadResult
            {
                Success = true,
                AvatarUrl = avatarUrl
            };
        }

        public static AvatarUploadResult CreateError(string errorMessage)
        {
            return new AvatarUploadResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// 用户安全设置
    /// </summary>
    public class UserSecuritySettings
    {
        public bool TwoFactorEnabled { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool SecurityAlertsEnabled { get; set; } = true;
        public bool LoginNotificationsEnabled { get; set; } = true;
        public int SessionTimeoutMinutes { get; set; } = 60;
        public bool AllowConcurrentLogins { get; set; } = true;
    }

    /// <summary>
    /// 用户活动日志
    /// </summary>
    public class UserActivityLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}