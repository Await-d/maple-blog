using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 邮箱验证服务接口
    /// </summary>
    public interface IEmailVerificationService
    {
        /// <summary>
        /// 生成邮箱验证令牌
        /// </summary>
        Task<string> GenerateEmailVerificationTokenAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证邮箱验证令牌
        /// </summary>
        Task<EmailVerificationResult> VerifyEmailTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        Task<bool> SendEmailVerificationAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 重新发送邮箱验证邮件
        /// </summary>
        Task<bool> ResendEmailVerificationAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 确认邮箱验证
        /// </summary>
        Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查邮箱验证令牌是否过期
        /// </summary>
        Task<bool> IsTokenExpiredAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除过期的验证令牌
        /// </summary>
        Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 邮箱验证结果
    /// </summary>
    public class EmailVerificationResult
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public string? Email { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public static EmailVerificationResult Success(string email)
        {
            return new EmailVerificationResult
            {
                IsValid = true,
                IsExpired = false,
                Email = email
            };
        }

        public static EmailVerificationResult Invalid(string errorMessage)
        {
            return new EmailVerificationResult
            {
                IsValid = false,
                IsExpired = false,
                ErrorMessage = errorMessage
            };
        }

        public static EmailVerificationResult Expired(string email, DateTime expiresAt)
        {
            return new EmailVerificationResult
            {
                IsValid = false,
                IsExpired = true,
                Email = email,
                ExpiresAt = expiresAt,
                ErrorMessage = "验证令牌已过期"
            };
        }
    }
}