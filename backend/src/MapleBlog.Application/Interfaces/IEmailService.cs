namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 邮件服务接口
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// 发送邮件
        /// </summary>
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量发送邮件
        /// </summary>
        Task<bool> SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送邮件验证
        /// </summary>
        Task<bool> SendEmailVerificationAsync(string email, string userName, string verificationToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送密码重置邮件
        /// </summary>
        Task<bool> SendPasswordResetAsync(string email, string userName, string resetToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送欢迎邮件
        /// </summary>
        Task<bool> SendWelcomeEmailAsync(string email, string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送通知邮件
        /// </summary>
        Task<bool> SendNotificationEmailAsync(string email, string userName, string title, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送评论通知邮件
        /// </summary>
        Task<bool> SendCommentNotificationAsync(string email, string userName, string postTitle, string commentContent, string postUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送系统通知邮件
        /// </summary>
        Task<bool> SendSystemNotificationAsync(string email, string userName, string subject, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// 测试邮件服务连接
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取邮件服务状态
        /// </summary>
        Task<EmailServiceStatus> GetServiceStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送存储配额警告邮件
        /// </summary>
        Task<bool> SendQuotaWarningEmailAsync(string email, string warningType, object quotaInfo, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 邮件服务状态
    /// </summary>
    public class EmailServiceStatus
    {
        public bool IsAvailable { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime LastChecked { get; set; }
        public int QueuedEmails { get; set; }
        public int SentToday { get; set; }
        public int FailedToday { get; set; }
    }
}