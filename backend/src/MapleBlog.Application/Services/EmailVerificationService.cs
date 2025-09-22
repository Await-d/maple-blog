using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 邮箱验证服务实现
    /// </summary>
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly IEmailVerificationTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailVerificationService> _logger;
        private readonly EmailVerificationSettings _settings;

        public EmailVerificationService(
            IEmailVerificationTokenRepository tokenRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            ILogger<EmailVerificationService> logger,
            IOptions<EmailVerificationSettings> settings)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// 生成邮箱验证令牌
        /// </summary>
        public async Task<string> GenerateEmailVerificationTokenAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                // 生成安全的随机令牌
                var tokenBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tokenBytes);
                }
                var token = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

                // 删除用户之前未使用的邮箱验证令牌
                var existingTokens = await _tokenRepository.FindAsync(t => t.UserId == user.Id &&
                                               t.TokenType == EmailTokenType.EmailVerification &&
                                               !t.IsUsed, cancellationToken);

                foreach (var existingToken in existingTokens)
                {
                    _tokenRepository.Remove(existingToken);
                }

                // 创建新的验证令牌
                var verificationToken = new EmailVerificationToken
                {
                    UserId = user.Id,
                    Token = token,
                    Email = user.Email.Value,
                    ExpiresAt = DateTime.UtcNow.AddHours(_settings.TokenExpirationHours),
                    TokenType = EmailTokenType.EmailVerification
                };

                await _tokenRepository.AddAsync(verificationToken, cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Generated email verification token for user {UserId}", user.Id);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating email verification token for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// 验证邮箱验证令牌
        /// </summary>
        public async Task<EmailVerificationResult> VerifyEmailTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var verificationToken = await _tokenRepository.FirstOrDefaultAsync(t => t.Token == token &&
                                        t.TokenType == EmailTokenType.EmailVerification,
                                   cancellationToken);

                if (verificationToken == null)
                {
                    _logger.LogWarning("Invalid email verification token attempted: {Token}", token);
                    return EmailVerificationResult.Invalid("无效的验证令牌");
                }

                if (verificationToken.IsUsed)
                {
                    _logger.LogWarning("Used email verification token attempted: {Token}", token);
                    return EmailVerificationResult.Invalid("验证令牌已被使用");
                }

                if (verificationToken.IsExpired)
                {
                    _logger.LogWarning("Expired email verification token attempted: {Token}", token);
                    return EmailVerificationResult.Expired(verificationToken.Email, verificationToken.ExpiresAt);
                }

                return EmailVerificationResult.Success(verificationToken.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email token {Token}", token);
                return EmailVerificationResult.Invalid("验证过程中发生错误");
            }
        }

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        public async Task<bool> SendEmailVerificationAsync(User user, CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await GenerateEmailVerificationTokenAsync(user, cancellationToken);
                var success = await _emailService.SendEmailVerificationAsync(
                    user.Email.Value,
                    user.UserName,
                    token,
                    cancellationToken);

                if (success)
                {
                    _logger.LogInformation("Email verification sent to user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send email verification to user {UserId}", user.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification to user {UserId}", user.Id);
                return false;
            }
        }

        /// <summary>
        /// 重新发送邮箱验证邮件
        /// </summary>
        public async Task<bool> ResendEmailVerificationAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Attempted to resend verification to non-existent email: {Email}", email);
                    return false;
                }

                if (user.IsEmailVerified)
                {
                    _logger.LogWarning("Attempted to resend verification to already verified email: {Email}", email);
                    return false;
                }

                // 检查发送频率限制
                var recentTokens = await _tokenRepository.FindAsync(t => t.Email == email &&
                                   t.TokenType == EmailTokenType.EmailVerification, cancellationToken);

                var recentToken = recentTokens.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

                if (recentToken != null &&
                    DateTime.UtcNow.Subtract(recentToken.CreatedAt).TotalMinutes < _settings.ResendCooldownMinutes)
                {
                    _logger.LogWarning("Email verification resend attempted too soon for {Email}", email);
                    return false;
                }

                return await SendEmailVerificationAsync(user, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending email verification to {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// 确认邮箱验证
        /// </summary>
        public async Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var verificationResult = await VerifyEmailTokenAsync(token, cancellationToken);
                if (!verificationResult.IsValid)
                {
                    return false;
                }

                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User not found for email confirmation: {Email}", email);
                    return false;
                }

                var verificationToken = await _tokenRepository.FirstOrDefaultAsync(t => t.Token == token &&
                                        t.Email == email &&
                                        t.TokenType == EmailTokenType.EmailVerification,
                                   cancellationToken);

                if (verificationToken == null)
                {
                    return false;
                }

                // 标记用户邮箱为已验证
                user.VerifyEmail();

                // 标记令牌为已使用
                verificationToken.MarkAsUsed();

                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Email confirmed successfully for user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email {Email} with token {Token}", email, token);
                return false;
            }
        }

        /// <summary>
        /// 检查邮箱验证令牌是否过期
        /// </summary>
        public async Task<bool> IsTokenExpiredAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var verificationToken = await _tokenRepository.FirstOrDefaultAsync(t => t.Token == token &&
                                        t.TokenType == EmailTokenType.EmailVerification,
                                   cancellationToken);

                return verificationToken?.IsExpired ?? true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification token expiration for {Token}", token);
                return true;
            }
        }

        /// <summary>
        /// 删除过期的验证令牌
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var expiredTokens = await _tokenRepository.FindAsync(t => t.TokenType == EmailTokenType.EmailVerification &&
                                   (t.ExpiresAt < DateTime.UtcNow || t.IsUsed), cancellationToken);

                foreach (var expiredToken in expiredTokens)
                {
                    _tokenRepository.Remove(expiredToken);
                }

                var count = expiredTokens.Count;
                await _tokenRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired email verification tokens", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired email verification tokens");
                return 0;
            }
        }

        /// <summary>
        /// 获取用户邮箱验证状态
        /// </summary>
        public async Task<EmailVerificationStatus> GetEmailVerificationStatusAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null)
                {
                    return new EmailVerificationStatus
                    {
                        Email = email,
                        IsVerified = false,
                        CanResend = false,
                        ErrorMessage = "用户不存在"
                    };
                }

                if (user.IsEmailVerified)
                {
                    return new EmailVerificationStatus
                    {
                        Email = email,
                        IsVerified = true,
                        CanResend = false
                    };
                }

                // 检查是否可以重新发送
                var recentTokens = await _tokenRepository.FindAsync(t => t.Email == email &&
                                   t.TokenType == EmailTokenType.EmailVerification, cancellationToken);

                var recentToken = recentTokens.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

                bool canResend = recentToken == null ||
                    DateTime.UtcNow.Subtract(recentToken.CreatedAt).TotalMinutes >= _settings.ResendCooldownMinutes;

                return new EmailVerificationStatus
                {
                    Email = email,
                    IsVerified = false,
                    CanResend = canResend,
                    LastSentAt = recentToken?.CreatedAt,
                    TokenExpiresAt = recentToken?.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email verification status for {Email}", email);
                return new EmailVerificationStatus
                {
                    Email = email,
                    IsVerified = false,
                    CanResend = false,
                    ErrorMessage = "获取验证状态失败"
                };
            }
        }
    }

    /// <summary>
    /// 邮箱验证设置
    /// </summary>
    public class EmailVerificationSettings
    {
        /// <summary>
        /// 令牌过期时间（小时）
        /// </summary>
        public int TokenExpirationHours { get; set; } = 24;

        /// <summary>
        /// 重新发送冷却时间（分钟）
        /// </summary>
        public int ResendCooldownMinutes { get; set; } = 5;

        /// <summary>
        /// 最大重新发送次数
        /// </summary>
        public int MaxResendAttempts { get; set; } = 5;
    }

    /// <summary>
    /// 邮箱验证状态
    /// </summary>
    public class EmailVerificationStatus
    {
        /// <summary>
        /// 邮箱地址
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 是否已验证
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// 是否可以重新发送
        /// </summary>
        public bool CanResend { get; set; }

        /// <summary>
        /// 最后发送时间
        /// </summary>
        public DateTime? LastSentAt { get; set; }

        /// <summary>
        /// 令牌过期时间
        /// </summary>
        public DateTime? TokenExpiresAt { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}