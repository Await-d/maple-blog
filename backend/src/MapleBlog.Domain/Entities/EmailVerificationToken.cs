using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// 邮箱验证令牌实体
    /// </summary>
    public class EmailVerificationToken : BaseEntity
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户导航属性
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// 验证令牌
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 是否已使用
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// 使用时间
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// 令牌类型
        /// </summary>
        public EmailTokenType TokenType { get; set; } = EmailTokenType.EmailVerification;

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// 检查令牌是否有效
        /// </summary>
        public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// 检查令牌是否过期
        /// </summary>
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        /// <summary>
        /// 标记令牌为已使用
        /// </summary>
        public void MarkAsUsed()
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 邮箱令牌类型
    /// </summary>
    public enum EmailTokenType
    {
        /// <summary>
        /// 邮箱验证
        /// </summary>
        EmailVerification = 1,

        /// <summary>
        /// 密码重置
        /// </summary>
        PasswordReset = 2,

        /// <summary>
        /// 邮箱变更
        /// </summary>
        EmailChange = 3
    }
}