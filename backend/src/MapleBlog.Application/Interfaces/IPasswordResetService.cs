using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 密码重置服务接口
    /// </summary>
    public interface IPasswordResetService
    {
        /// <summary>
        /// 生成密码重置令牌
        /// </summary>
        Task<string> GeneratePasswordResetTokenAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送密码重置邮件
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证密码重置令牌
        /// </summary>
        Task<PasswordResetResult> VerifyPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 重置密码
        /// </summary>
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查密码重置令牌是否过期
        /// </summary>
        Task<bool> IsTokenExpiredAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除过期的重置令牌
        /// </summary>
        Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查重置频率限制
        /// </summary>
        Task<bool> CanRequestResetAsync(string email, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 密码重置结果
    /// </summary>
    public class PasswordResetResult
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public string? Email { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public static PasswordResetResult Success(string email)
        {
            return new PasswordResetResult
            {
                IsValid = true,
                IsExpired = false,
                Email = email
            };
        }

        public static PasswordResetResult Invalid(string errorMessage)
        {
            return new PasswordResetResult
            {
                IsValid = false,
                IsExpired = false,
                ErrorMessage = errorMessage
            };
        }

        public static PasswordResetResult Expired(string email, DateTime expiresAt)
        {
            return new PasswordResetResult
            {
                IsValid = false,
                IsExpired = true,
                Email = email,
                ExpiresAt = expiresAt,
                ErrorMessage = "密码重置令牌已过期"
            };
        }
    }
}