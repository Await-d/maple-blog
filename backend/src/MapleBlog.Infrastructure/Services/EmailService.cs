using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using MapleBlog.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromAddress;
        private readonly string _fromName;
        private readonly bool _enabled;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _enabled = configuration.GetValue<bool>("Email:Enabled");
            _fromAddress = configuration["Email:FromAddress"] ?? "noreply@mapleblog.com";
            _fromName = configuration["Email:FromName"] ?? "Maple Blog";

            if (_enabled)
            {
                var host = configuration["Email:Smtp:Host"] ?? "localhost";
                var port = configuration.GetValue<int>("Email:Smtp:Port", 587);
                var enableSsl = configuration.GetValue<bool>("Email:Smtp:EnableSsl", true);
                var username = configuration["Email:Smtp:Username"];
                var password = configuration["Email:Smtp:Password"];

                _smtpClient = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _smtpClient.Credentials = new NetworkCredential(username, password);
                }
            }
            else
            {
                _smtpClient = new SmtpClient();
                _logger.LogWarning("Email service is disabled. Emails will not be sent.");
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Email service is disabled. Skipping email to {To}", to);
                return true;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_fromAddress, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                message.To.Add(new MailAddress(to));

                await _smtpClient.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Email sent successfully to {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                return false;
            }
        }

        public async Task<bool> SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        {
            var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, body, isHtml, cancellationToken));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }

        public async Task<bool> SendEmailVerificationAsync(string email, string userName, string verificationToken, CancellationToken cancellationToken = default)
        {
            var verificationUrl = $"{_configuration["Application:BaseUrl"]}/verify-email?token={verificationToken}";
            var subject = "Verify Your Email - Maple Blog";
            var body = $@"
                <h2>Hello {userName},</h2>
                <p>Thank you for registering with Maple Blog. Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationUrl}' style='padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>Verify Email</a></p>
                <p>Or copy and paste this link in your browser:</p>
                <p>{verificationUrl}</p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not create an account, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>The Maple Blog Team</p>
            ";

            return await SendEmailAsync(email, subject, body, true, cancellationToken);
        }

        public async Task<bool> SendPasswordResetAsync(string email, string userName, string resetToken, CancellationToken cancellationToken = default)
        {
            var resetUrl = $"{_configuration["Application:BaseUrl"]}/reset-password?token={resetToken}";
            var subject = "Password Reset Request - Maple Blog";
            var body = $@"
                <h2>Hello {userName},</h2>
                <p>We received a request to reset your password. Click the link below to create a new password:</p>
                <p><a href='{resetUrl}' style='padding: 10px 20px; background-color: #2196F3; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a></p>
                <p>Or copy and paste this link in your browser:</p>
                <p>{resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you did not request a password reset, please ignore this email and your password will remain unchanged.</p>
                <br/>
                <p>Best regards,<br/>The Maple Blog Team</p>
            ";

            return await SendEmailAsync(email, subject, body, true, cancellationToken);
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string userName, CancellationToken cancellationToken = default)
        {
            var subject = "Welcome to Maple Blog!";
            var body = $@"
                <h2>Welcome {userName}!</h2>
                <p>Thank you for joining Maple Blog. We're excited to have you as part of our community!</p>
                <p>Here are some things you can do to get started:</p>
                <ul>
                    <li>Complete your profile</li>
                    <li>Explore interesting articles</li>
                    <li>Write your first blog post</li>
                    <li>Connect with other writers</li>
                </ul>
                <p><a href='{_configuration["Application:BaseUrl"]}/dashboard' style='padding: 10px 20px; background-color: #FF9800; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>Go to Dashboard</a></p>
                <p>If you have any questions, feel free to contact our support team.</p>
                <br/>
                <p>Happy blogging!<br/>The Maple Blog Team</p>
            ";

            return await SendEmailAsync(email, subject, body, true, cancellationToken);
        }

        public async Task<bool> SendNotificationEmailAsync(string email, string userName, string title, string message, CancellationToken cancellationToken = default)
        {
            var subject = $"Notification: {title}";
            var body = $@"
                <h2>Hello {userName},</h2>
                <h3>{title}</h3>
                <p>{message}</p>
                <p><a href='{_configuration["Application:BaseUrl"]}/notifications' style='padding: 10px 20px; background-color: #9C27B0; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>View All Notifications</a></p>
                <br/>
                <p>Best regards,<br/>The Maple Blog Team</p>
            ";

            return await SendEmailAsync(email, subject, body, true, cancellationToken);
        }

        public async Task<bool> SendCommentNotificationAsync(string email, string userName, string postTitle, string commentContent, string postUrl, CancellationToken cancellationToken = default)
        {
            var subject = $"New Comment on Your Post: {postTitle}";
            var body = $@"
                <h2>Hello {userName},</h2>
                <p>Someone commented on your blog post <strong>{postTitle}</strong>:</p>
                <blockquote style='border-left: 3px solid #ccc; padding-left: 10px; margin: 10px 0;'>
                    {commentContent}
                </blockquote>
                <p><a href='{postUrl}' style='padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>View Comment</a></p>
                <br/>
                <p>Best regards,<br/>The Maple Blog Team</p>
            ";

            return await SendEmailAsync(email, subject, body, true, cancellationToken);
        }

        public async Task<bool> SendSystemNotificationAsync(string email, string userName, string subject, string message, CancellationToken cancellationToken = default)
        {
            var body = $@"
                <h2>System Notification</h2>
                <p>Dear {userName},</p>
                <p>{message}</p>
                <p>This is an automated system notification. Please do not reply to this email.</p>
                <br/>
                <p>Best regards,<br/>The Maple Blog System</p>
            ";

            return await SendEmailAsync(email, $"[System] {subject}", body, true, cancellationToken);
        }

        public async Task<bool> SendQuotaWarningEmailAsync(string email, string warningType, object quotaInfo, CancellationToken cancellationToken = default)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Email service is disabled. Skipping quota warning email to {Email}", email);
                return true;
            }

            try
            {
                dynamic info = quotaInfo;
                var usedSpaceGB = (double)info.UsedSpace / (1024 * 1024 * 1024);
                var maxSpaceGB = (double)info.MaxSpace / (1024 * 1024 * 1024);
                var usagePercentage = (double)info.UsedSpace / info.MaxSpace * 100;

                string subject;
                string urgencyColor;
                string actionText;

                switch (warningType.ToLower())
                {
                    case "critical":
                        subject = "⚠️ Critical: Storage Quota Almost Full";
                        urgencyColor = "#f44336";
                        actionText = "Immediate action required! Your storage is critically low.";
                        break;
                    case "warning":
                        subject = "⚠️ Warning: Storage Quota Approaching Limit";
                        urgencyColor = "#ff9800";
                        actionText = "Your storage usage is high. Consider managing your files.";
                        break;
                    default:
                        subject = "Storage Quota Notification";
                        urgencyColor = "#2196F3";
                        actionText = "Your storage usage has reached a notable threshold.";
                        break;
                }

                var body = $@"
                    <h2>Storage Quota {warningType}</h2>
                    <div style='padding: 15px; background-color: {urgencyColor}; color: white; border-radius: 5px; margin: 10px 0;'>
                        <p style='margin: 0; font-size: 16px;'>{actionText}</p>
                    </div>
                    <h3>Current Usage:</h3>
                    <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 10px 0;'>
                        <p><strong>Used Space:</strong> {usedSpaceGB:F2} GB</p>
                        <p><strong>Total Quota:</strong> {maxSpaceGB:F2} GB</p>
                        <p><strong>Usage Percentage:</strong> {usagePercentage:F1}%</p>
                        <p><strong>File Count:</strong> {info.FileCount}</p>
                    </div>
                    <h3>Recommended Actions:</h3>
                    <ul>
                        <li>Delete unnecessary files</li>
                        <li>Archive old content</li>
                        <li>Consider upgrading your storage plan</li>
                    </ul>
                    <p style='margin-top: 20px;'>
                        <a href='{_configuration["Application:BaseUrl"]}/dashboard/storage' style='padding: 10px 20px; background-color: {urgencyColor}; color: white; text-decoration: none; border-radius: 5px; display: inline-block;'>Manage Storage</a>
                    </p>
                    <br/>
                    <p>Best regards,<br/>The Maple Blog Team</p>
                ";

                return await SendEmailAsync(email, subject, body, true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send quota warning email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (!_enabled)
            {
                _logger.LogWarning("Email service is disabled");
                return false;
            }

            try
            {
                // Test by sending a test email to the from address
                return await SendEmailAsync(_fromAddress, "Test Connection", "This is a test email to verify SMTP connection.", false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email service connection test failed");
                return false;
            }
        }

        public Task<EmailServiceStatus> GetServiceStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new EmailServiceStatus
            {
                IsAvailable = _enabled,
                StatusMessage = _enabled ? "Email service is operational" : "Email service is disabled",
                LastChecked = DateTime.UtcNow,
                QueuedEmails = 0, // Would need a queue implementation for real counts
                SentToday = 0,    // Would need tracking implementation
                FailedToday = 0   // Would need tracking implementation
            };

            return Task.FromResult(status);
        }

        public void Dispose()
        {
            _smtpClient?.Dispose();
        }
    }
}