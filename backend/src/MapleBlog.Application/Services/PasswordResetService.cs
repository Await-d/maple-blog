using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 密码重置服务实现
    /// </summary>
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IEmailVerificationTokenRepository _tokenRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetService> _logger;
        private readonly PasswordResetSettings _settings;

        public PasswordResetService(
            IEmailVerificationTokenRepository tokenRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            ILogger<PasswordResetService> logger,
            IOptions<PasswordResetSettings> settings)
        {
            _tokenRepository = tokenRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// 生成密码重置令牌
        /// </summary>
        public async Task<string> GeneratePasswordResetTokenAsync(User user, CancellationToken cancellationToken = default)
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

                // 删除用户之前未使用的密码重置令牌
                var existingTokens = await _tokenRepository.FindAsync(t => t.UserId == user.Id &&
                                   t.TokenType == EmailTokenType.PasswordReset &&
                                   !t.IsUsed, cancellationToken);

                foreach (var existingToken in existingTokens)
                {
                    _tokenRepository.Remove(existingToken);
                }

                // 创建新的重置令牌
                var resetToken = new EmailVerificationToken
                {
                    UserId = user.Id,
                    Token = token,
                    Email = user.Email.Value,
                    ExpiresAt = DateTime.UtcNow.AddHours(_settings.TokenExpirationHours),
                    TokenType = EmailTokenType.PasswordReset
                };

                await _tokenRepository.AddAsync(resetToken, cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Generated password reset token for user {UserId}", user.Id);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating password reset token for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// 发送密码重置邮件
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                    // 为了安全，不透露邮箱是否存在，总是返回成功
                    return true;
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Password reset requested for inactive user: {Email}", email);
                    return false;
                }

                // 检查重置频率限制
                if (!await CanRequestResetAsync(email, cancellationToken))
                {
                    _logger.LogWarning("Password reset rate limit exceeded for email: {Email}", email);
                    return false;
                }

                var token = await GeneratePasswordResetTokenAsync(user, cancellationToken);
                var success = await _emailService.SendPasswordResetAsync(
                    email,
                    user.UserName,
                    token,
                    cancellationToken);

                if (success)
                {
                    _logger.LogInformation("Password reset email sent to user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to user {UserId}", user.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", email);
                return false;
            }
        }

        /// <summary>
        /// 验证密码重置令牌
        /// </summary>
        public async Task<PasswordResetResult> VerifyPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var resetToken = await _tokenRepository.FirstOrDefaultAsync(t => t.Token == token &&
                                        t.TokenType == EmailTokenType.PasswordReset,
                                   cancellationToken);

                if (resetToken == null)
                {
                    _logger.LogWarning("Invalid password reset token attempted: {Token}", token);
                    return PasswordResetResult.Invalid("无效的重置令牌");
                }

                if (resetToken.IsUsed)
                {
                    _logger.LogWarning("Used password reset token attempted: {Token}", token);
                    return PasswordResetResult.Invalid("重置令牌已被使用");
                }

                if (resetToken.IsExpired)
                {
                    _logger.LogWarning("Expired password reset token attempted: {Token}", token);
                    return PasswordResetResult.Expired(resetToken.Email, resetToken.ExpiresAt);
                }

                var user = await _userRepository.GetByIdAsync(resetToken.UserId, cancellationToken);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("Password reset attempted for inactive user: {UserId}", resetToken.UserId);
                    return PasswordResetResult.Invalid("用户账户已被禁用");
                }

                return PasswordResetResult.Success(resetToken.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password reset token {Token}", token);
                return PasswordResetResult.Invalid("验证过程中发生错误");
            }
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var verificationResult = await VerifyPasswordResetTokenAsync(token, cancellationToken);
                if (!verificationResult.IsValid)
                {
                    return false;
                }

                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User not found for password reset: {Email}", email);
                    return false;
                }

                var resetToken = await _tokenRepository.FirstOrDefaultAsync(t => t.Token == token &&
                                        t.Email == email &&
                                        t.TokenType == EmailTokenType.PasswordReset,
                                   cancellationToken);

                if (resetToken == null)
                {
                    return false;
                }

                // 验证新密码强度
                if (!IsValidPassword(newPassword))
                {
                    _logger.LogWarning("Weak password provided for reset for user {UserId}", user.Id);
                    return false;
                }

                // 哈希新密码
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, _settings.BcryptWorkFactor);

                // 更新用户密码
                user.UpdatePassword(newPasswordHash);

                // 标记令牌为已使用
                resetToken.MarkAsUsed();

                // 撤销用户的所有其他密码重置令牌
                var otherResetTokens = await _tokenRepository.FindAsync(t => t.UserId == user.Id &&
                                   t.TokenType == EmailTokenType.PasswordReset &&
                                   t.Id != resetToken.Id &&
                                   !t.IsUsed, cancellationToken);

                foreach (var otherToken in otherResetTokens)
                {
                    otherToken.MarkAsUsed();
                }

                await _tokenRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Password reset completed successfully for user {UserId}", user.Id);

                // 发送密码重置确认邮件
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendSystemNotificationAsync(
                            email,
                            user.UserName,
                            "密码重置成功",
                            "您的密码已成功重置。如果这不是您的操作，请立即联系我们。",
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending password reset confirmation email to {Email}", email);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email {Email} with token {Token}", email, token);
                return false;
            }
        }

        /// <summary>
        /// 检查密码重置令牌是否过期
        /// </summary>
        public async Task<bool> IsTokenExpiredAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var resetToken = await _tokenRepository.FirstOrDefaultAsync(t => t.Token == token &&
                                        t.TokenType == EmailTokenType.PasswordReset,
                                   cancellationToken);

                return resetToken?.IsExpired ?? true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password reset token expiration for {Token}", token);
                return true;
            }
        }

        /// <summary>
        /// 删除过期的重置令牌
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var expiredTokens = await _tokenRepository.FindAsync(t => t.TokenType == EmailTokenType.PasswordReset &&
                                   (t.ExpiresAt < DateTime.UtcNow || t.IsUsed), cancellationToken);

                foreach (var expiredToken in expiredTokens)
                {
                    _tokenRepository.Remove(expiredToken);
                }

                var count = expiredTokens.Count;
                await _tokenRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} expired password reset tokens", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired password reset tokens");
                return 0;
            }
        }

        /// <summary>
        /// 检查重置频率限制
        /// </summary>
        public async Task<bool> CanRequestResetAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var recentTokens = await _tokenRepository.FindAsync(t => t.Email == email &&
                                   t.TokenType == EmailTokenType.PasswordReset, cancellationToken);

                var recentToken = recentTokens.OrderByDescending(t => t.CreatedAt).FirstOrDefault();

                if (recentToken == null)
                {
                    return true;
                }

                var timeSinceLastRequest = DateTime.UtcNow - recentToken.CreatedAt;
                return timeSinceLastRequest.TotalMinutes >= _settings.RequestCooldownMinutes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking password reset rate limit for {Email}", email);
                return false;
            }
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

            // 检查密码复杂性要求
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
    /// 密码重置设置
    /// </summary>
    public class PasswordResetSettings
    {
        /// <summary>
        /// 令牌过期时间（小时）
        /// </summary>
        public int TokenExpirationHours { get; set; } = 1;

        /// <summary>
        /// 请求冷却时间（分钟）
        /// </summary>
        public int RequestCooldownMinutes { get; set; } = 15;

        /// <summary>
        /// BCrypt工作因子
        /// </summary>
        public int BcryptWorkFactor { get; set; } = 12;

        /// <summary>
        /// 最小密码长度
        /// </summary>
        public int MinPasswordLength { get; set; } = 8;

        /// <summary>
        /// 最小密码复杂性（需要满足的字符类型数量）
        /// </summary>
        public int MinPasswordComplexity { get; set; } = 3;
    }
}