using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// Authentication service implementation with security features
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly ILoginTrackingService _loginTrackingService;

        // Password requirements
        private const int MinPasswordLength = 8;
        private const int MaxLoginAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthService(
            IUserRepository userRepository,
            IJwtService jwtService,
            IMapper mapper,
            ILogger<AuthService> logger,
            IEmailService emailService,
            ILoginTrackingService loginTrackingService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _loginTrackingService = loginTrackingService ?? throw new ArgumentNullException(nameof(loginTrackingService));
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting user registration for email: {Email}", request.Email);

                // Validate email format
                if (!Email.IsValidFormat(request.Email))
                {
                    return AuthResult.Failure("Invalid email format.");
                }

                var email = Email.Create(request.Email);

                // Check if email is already in use
                if (await _userRepository.IsEmailInUseAsync(email, cancellationToken: cancellationToken))
                {
                    return AuthResult.Failure("Email address is already registered.");
                }

                // Check if username is already in use
                if (await _userRepository.IsUserNameInUseAsync(request.UserName, cancellationToken: cancellationToken))
                {
                    return AuthResult.Failure("Username is already taken.");
                }

                // Validate password strength
                var passwordValidation = ValidatePasswordStrength(request.Password);
                if (!passwordValidation.IsValid)
                {
                    return AuthResult.Failure(passwordValidation.Errors);
                }

                // Hash password
                var passwordHash = HashPassword(request.Password);

                // Create user entity
                var user = new User(request.UserName, email, passwordHash);
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;

                // Generate email verification token
                var verificationToken = GenerateSecureToken();
                user.SetEmailVerificationToken(verificationToken, 1440); // 24 hours in minutes

                // Save user to database
                await _userRepository.AddAsync(user, cancellationToken);
                await _userRepository.SaveChangesAsync(cancellationToken);

                // Send verification email
                await SendEmailVerificationAsync(user, verificationToken);

                _logger.LogInformation("User {UserId} ({UserName}) registered successfully", user.Id, user.UserName);

                // Map to DTO
                var userDto = _mapper.Map<UserDto>(user);

                // Generate tokens
                var tokenInfo = await _jwtService.GenerateTokensAsync(user);

                // Return success result requiring email verification
                return AuthResult.CreateRequiresEmailVerification(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                return AuthResult.Failure("An error occurred during registration. Please try again.");
            }
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Login attempt for: {EmailOrUsername}", request.EmailOrUsername);

                User? user = null;

                // Try to find user by email first, then by username
                if (request.EmailOrUsername.Contains('@'))
                {
                    user = await _userRepository.FindByEmailAsync(request.EmailOrUsername, cancellationToken);
                }
                else
                {
                    user = await _userRepository.FindByUserNameAsync(request.EmailOrUsername, cancellationToken);
                }

                // If user not found
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for {EmailOrUsername}", request.EmailOrUsername);
                    return AuthResult.Failure("Invalid email/username or password.");
                }

                // Check if account is locked out
                if (user.IsLockedOut())
                {
                    _logger.LogWarning("Login failed: Account locked for user {UserId} until {LockoutEnd}",
                        user.Id, user.LockoutEndDateUtc);
                    return AuthResult.LockedOut(user.LockoutEndDateUtc);
                }

                // Check if account is active
                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed: Account disabled for user {UserId}", user.Id);
                    return AuthResult.Failure("Account is disabled. Please contact support.");
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    // Record failed login attempt
                    user.IncreaseAccessFailedCount();
                    if (user.AccessFailedCount >= MaxLoginAttempts)
                    {
                        user.LockUser(DateTime.UtcNow.Add(LockoutDuration));
                    }
                    await _userRepository.SaveChangesAsync(cancellationToken);

                    _logger.LogWarning("Login failed: Invalid password for user {UserId}. Failed attempts: {FailedAttempts}",
                        user.Id, user.AccessFailedCount);

                    return user.IsLockedOut()
                        ? AuthResult.LockedOut(user.LockoutEndDateUtc)
                        : AuthResult.Failure("Invalid email/username or password.");
                }

                // Check if email verification is required
                if (!user.EmailConfirmed)
                {
                    _logger.LogInformation("Login requires email verification for user {UserId}", user.Id);
                    var userDto = _mapper.Map<UserDto>(user);
                    return AuthResult.CreateRequiresEmailVerification(userDto);
                }

                // Successful login - record it and generate tokens
                user.UpdateLastLoginTime();
                user.ResetAccessFailedCount();
                await _userRepository.SaveChangesAsync(cancellationToken);

                var tokens = await _jwtService.GenerateTokensAsync(user);
                var mappedUser = _mapper.Map<UserDto>(user);

                _logger.LogInformation("User {UserId} ({UserName}) logged in successfully", user.Id, user.UserName);

                return AuthResult.CreateSuccess(mappedUser, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for: {EmailOrUsername}", request.EmailOrUsername);
                return AuthResult.Failure("An error occurred during login. Please try again.");
            }
        }

        public async Task<TokenRefreshResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Token refresh attempt");

                var tokenInfo = await _jwtService.RefreshTokenAsync(request.RefreshToken);

                if (tokenInfo == null)
                {
                    _logger.LogWarning("Token refresh failed: Invalid refresh token");
                    return TokenRefreshResult.Failure("Invalid refresh token.");
                }

                _logger.LogDebug("Token refresh successful");
                return TokenRefreshResult.CreateSuccess(tokenInfo.Value.AccessToken, tokenInfo.Value.RefreshToken, tokenInfo.Value.ExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return TokenRefreshResult.Failure("An error occurred during token refresh.");
            }
        }

        public async Task<OperationResult> LogoutAsync(Guid userId, string? tokenId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (tokenId != null)
                {
                    // Blacklist specific token
                    await _jwtService.BlacklistTokenAsync(tokenId, cancellationToken);
                    _logger.LogInformation("Token {TokenId} blacklisted for user {UserId}", tokenId, userId);
                }
                else
                {
                    // Blacklist all user tokens
                    await _jwtService.BlacklistAllUserTokensAsync(userId);
                    _logger.LogInformation("All tokens blacklisted for user {UserId}", userId);
                }

                return OperationResult.CreateSuccess("Logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                return OperationResult.Failure("An error occurred during logout.");
            }
        }

        public async Task<OperationResult> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);

                if (user == null)
                {
                    // Don't reveal if email exists or not for security
                    _logger.LogInformation("Password reset requested for non-existent email: {Email}", request.Email);
                    return OperationResult.CreateSuccess("If the email address exists, a password reset link has been sent.");
                }

                // Generate reset token
                var resetToken = GenerateSecureToken();
                user.SetPasswordResetToken(resetToken, 60); // 1-hour expiry

                await _userRepository.SaveChangesAsync(cancellationToken);

                // Send reset email
                await SendPasswordResetEmailAsync(user, resetToken);

                _logger.LogInformation("Password reset token generated for user {UserId}", user.Id);

                return OperationResult.CreateSuccess("If the email address exists, a password reset link has been sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request for email: {Email}", request.Email);
                return OperationResult.Failure("An error occurred while processing the password reset request.");
            }
        }

        public async Task<OperationResult> ResetPasswordAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByPasswordResetTokenAsync(request.Token, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Password reset attempted with invalid or expired token");
                    return OperationResult.Failure("Invalid or expired reset token.");
                }

                // Validate new password
                var passwordValidation = ValidatePasswordStrength(request.Password);
                if (!passwordValidation.IsValid)
                {
                    return OperationResult.Failure(passwordValidation.Errors);
                }

                // Hash new password and update user
                var newPasswordHash = HashPassword(request.Password);
                user.UpdatePassword(newPasswordHash);

                await _userRepository.SaveChangesAsync(cancellationToken);

                // Blacklist all existing tokens for security
                await _jwtService.BlacklistAllUserTokensAsync(user.Id);

                _logger.LogInformation("Password reset completed for user {UserId}", user.Id);

                return OperationResult.CreateSuccess("Password has been reset successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset confirmation");
                return OperationResult.Failure("An error occurred while resetting the password.");
            }
        }

        public async Task<OperationResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

                if (user == null)
                {
                    return OperationResult.Failure("User not found.");
                }

                // Verify current password
                if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("Password change failed: Invalid current password for user {UserId}", userId);
                    return OperationResult.Failure("Current password is incorrect.");
                }

                // Validate new password
                var passwordValidation = ValidatePasswordStrength(request.NewPassword);
                if (!passwordValidation.IsValid)
                {
                    return OperationResult.Failure(passwordValidation.Errors);
                }

                // Hash new password and update
                var newPasswordHash = HashPassword(request.NewPassword);
                user.UpdatePassword(newPasswordHash);

                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return OperationResult.CreateSuccess("Password changed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change for user {UserId}", userId);
                return OperationResult.Failure("An error occurred while changing the password.");
            }
        }

        public async Task<OperationResult> VerifyEmailAsync(EmailVerificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailVerificationTokenAsync(request.Token, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("Email verification attempted with invalid or expired token");
                    return OperationResult.Failure("Invalid or expired verification token.");
                }

                user.VerifyEmail();
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);

                return OperationResult.CreateSuccess("Email verified successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return OperationResult.Failure("An error occurred while verifying the email.");
            }
        }

        public async Task<OperationResult> ResendEmailVerificationAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.FindByEmailAsync(email, cancellationToken);

                if (user == null || user.EmailConfirmed)
                {
                    // Don't reveal if email exists for security
                    return OperationResult.CreateSuccess("If the email address exists and is unverified, a verification link has been sent.");
                }

                // Generate new verification token
                var verificationToken = GenerateSecureToken();
                user.SetEmailVerificationToken(verificationToken, 1440); // 24 hours in minutes

                await _userRepository.SaveChangesAsync(cancellationToken);

                // Send verification email
                await SendEmailVerificationAsync(user, verificationToken);

                _logger.LogInformation("Email verification resent for user {UserId}", user.Id);

                return OperationResult.CreateSuccess("If the email address exists and is unverified, a verification link has been sent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification resend for: {Email}", email);
                return OperationResult.Failure("An error occurred while sending the verification email.");
            }
        }

        public async Task<OperationResult> ChangeEmailAsync(Guid userId, ChangeEmailRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

                if (user == null)
                {
                    return OperationResult.Failure("User not found.");
                }

                // Verify password
                if (!VerifyPassword(request.Password, user.PasswordHash))
                {
                    return OperationResult.Failure("Password is incorrect.");
                }

                // Validate new email
                if (!Email.IsValidFormat(request.Email))
                {
                    return OperationResult.Failure("Invalid email format.");
                }

                var newEmail = Email.Create(request.Email);

                // Check if new email is already in use
                if (await _userRepository.IsEmailInUseAsync(newEmail, cancellationToken))
                {
                    return OperationResult.Failure("Email address is already in use.");
                }

                // Update email and mark as unverified
                user.ChangeEmail(newEmail);

                // Generate verification token for new email
                var verificationToken = GenerateSecureToken();
                user.SetEmailVerificationToken(verificationToken, 1440); // 24 hours in minutes

                await _userRepository.SaveChangesAsync(cancellationToken);

                // Send verification email to new address
                await SendEmailVerificationAsync(user, verificationToken);

                _logger.LogInformation("Email changed for user {UserId}, verification required", userId);

                return OperationResult.CreateSuccess("Email address updated. Please check your new email for verification.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email change for user {UserId}", userId);
                return OperationResult.Failure("An error occurred while changing the email address.");
            }
        }

        public async Task<OperationResult<UserDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

                if (user == null)
                {
                    return OperationResult<UserDto>.Failure("User not found.");
                }

                // Update profile
                user.UpdateProfile(request.FirstName, request.LastName, request.Avatar);

                await _userRepository.SaveChangesAsync(cancellationToken);

                var userDto = _mapper.Map<UserDto>(user);

                _logger.LogInformation("Profile updated for user {UserId}", userId);

                return OperationResult<UserDto>.CreateSuccess(userDto, "Profile updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during profile update for user {UserId}", userId);
                return OperationResult<UserDto>.Failure("An error occurred while updating the profile.");
            }
        }

        public async Task<OperationResult<UserDto>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

                if (user == null)
                {
                    return OperationResult<UserDto>.Failure("User not found.");
                }

                var userDto = _mapper.Map<UserDto>(user);
                return OperationResult<UserDto>.CreateSuccess(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return OperationResult<UserDto>.Failure("An error occurred while retrieving user information.");
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Email.IsValidFormat(email))
                    return false;

                var emailValueObject = Email.Create(email);
                return !await _userRepository.IsEmailInUseAsync(emailValueObject, excludeUserId, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsUserNameAvailableAsync(string userName, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return !await _userRepository.IsUserNameInUseAsync(userName, excludeUserId, cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// Hashes a password using BCrypt
        /// </summary>
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        /// <summary>
        /// Verifies a password against its hash
        /// </summary>
        private static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength requirements
        /// </summary>
        private static (bool IsValid, List<string> Errors) ValidatePasswordStrength(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("Password is required.");
                return (false, errors);
            }

            if (password.Length < MinPasswordLength)
                errors.Add($"Password must be at least {MinPasswordLength} characters long.");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number.");

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                errors.Add("Password must contain at least one special character.");

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Generates a cryptographically secure random token
        /// </summary>
        private static string GenerateSecureToken()
        {
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        /// <summary>
        /// Sends email verification email
        /// </summary>
        private async Task SendEmailVerificationAsync(User user, string token)
        {
            var subject = "Verify your email address - Maple Blog";
            var body = $@"
                <h2>Verify Your Email Address</h2>
                <p>Hello {user.DisplayName},</p>
                <p>Thank you for registering with Maple Blog. Please click the link below to verify your email address:</p>
                <p><a href='https://mapleblog.com/verify-email?token={token}'>Verify Email Address</a></p>
                <p>This link will expire in 24 hours.</p>
                <p>If you didn't create an account, please ignore this email.</p>
                <p>Best regards,<br>Maple Blog Team</p>
            ";

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        /// <summary>
        /// Sends password reset email
        /// </summary>
        private async Task SendPasswordResetEmailAsync(User user, string token)
        {
            var subject = "Reset your password - Maple Blog";
            var body = $@"
                <h2>Reset Your Password</h2>
                <p>Hello {user.DisplayName},</p>
                <p>We received a request to reset your password. Click the link below to create a new password:</p>
                <p><a href='https://mapleblog.com/reset-password?token={token}'>Reset Password</a></p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
                <p>Best regards,<br>Maple Blog Team</p>
            ";

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        #endregion

        /// <summary>
        /// Authenticates a user with comprehensive tracking information
        /// </summary>
        public async Task<AuthResult> LoginAsync(LoginRequest request, string clientIp, string userAgent, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Login attempt from IP {ClientIp} with User-Agent {UserAgent}", clientIp, userAgent);

            // Check if email is blocked due to too many failed attempts
            if (await _loginTrackingService.IsEmailBlockedAsync(request.Email, cancellationToken))
            {
                var trackingInfo = new LoginTrackingInfo
                {
                    Email = request.Email,
                    IsSuccessful = false,
                    Result = LoginResult.TooManyAttempts,
                    FailureReason = "Email blocked due to too many failed attempts",
                    IpAddress = clientIp,
                    UserAgent = userAgent,
                    LoginType = LoginType.Standard
                };

                await _loginTrackingService.RecordLoginAttemptAsync(trackingInfo, cancellationToken);
                return AuthResult.Failure("Account temporarily locked due to too many failed attempts. Please try again later.");
            }

            // Check if IP address is blocked
            if (await _loginTrackingService.IsIpAddressBlockedAsync(clientIp, cancellationToken))
            {
                var trackingInfo = new LoginTrackingInfo
                {
                    Email = request.Email,
                    IsSuccessful = false,
                    Result = LoginResult.TooManyAttempts,
                    FailureReason = "IP address blocked due to too many failed attempts",
                    IpAddress = clientIp,
                    UserAgent = userAgent,
                    LoginType = LoginType.Standard
                };

                await _loginTrackingService.RecordLoginAttemptAsync(trackingInfo, cancellationToken);
                return AuthResult.Failure("Too many failed attempts from this location. Please try again later.");
            }

            // Perform the actual login attempt
            var result = await LoginAsync(request, cancellationToken);

            // Extract user information for tracking
            User? user = null;
            if (result.Success && result.User != null)
            {
                // Get the full user entity for tracking
                user = await _userRepository.GetByEmailAsync(request.Email);
            }

            // Create tracking information
            var loginTrackingInfo = new LoginTrackingInfo
            {
                Email = request.Email,
                UserName = user?.UserName,
                UserId = user?.Id,
                IsSuccessful = result.Success,
                Result = DetermineLoginResult(result),
                FailureReason = result.Success ? null : result.ErrorMessage,
                IpAddress = clientIp,
                UserAgent = userAgent,
                LoginType = LoginType.Standard,
                TwoFactorUsed = false, // TODO: Update when 2FA is implemented
                TwoFactorMethod = null,
                SessionId = result.Success ? GenerateSessionId() : null,
                SessionExpiresAt = result.Success ? DateTime.UtcNow.AddHours(24) : null, // Matches token expiry
                Metadata = new Dictionary<string, object>
                {
                    ["LoginAttemptId"] = Guid.NewGuid(),
                    ["AuthMethod"] = "EmailPassword",
                    ["ClientInfo"] = new
                    {
                        IpAddress = clientIp,
                        UserAgent = userAgent,
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Record the login attempt with security analysis
            try
            {
                await _loginTrackingService.RecordLoginAttemptAsync(loginTrackingInfo, cancellationToken);
                _logger.LogInformation("Login tracking recorded for email {Email}, success: {IsSuccessful}",
                    request.Email, result.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record login tracking for email {Email}", request.Email);
                // Don't fail the login if tracking fails, but log the error
            }

            return result;
        }

        /// <summary>
        /// Generate a unique session identifier
        /// </summary>
        private static string GenerateSessionId()
        {
            return Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString("X");
        }

        /// <summary>
        /// Determine the appropriate LoginResult based on AuthResult
        /// </summary>
        private static LoginResult DetermineLoginResult(AuthResult authResult)
        {
            if (authResult.Success)
                return LoginResult.Success;

            // Map common failure reasons to specific LoginResult values
            var errorMessage = authResult.ErrorMessage?.ToLowerInvariant() ?? string.Empty;

            if (errorMessage.Contains("invalid") && (errorMessage.Contains("email") || errorMessage.Contains("password")))
                return LoginResult.InvalidCredentials;

            if (errorMessage.Contains("locked") || errorMessage.Contains("too many"))
                return LoginResult.TooManyAttempts;

            if (errorMessage.Contains("disabled") || errorMessage.Contains("suspended"))
                return LoginResult.AccountDisabled;

            if (errorMessage.Contains("verified") || errorMessage.Contains("verification"))
                return LoginResult.AccountNotVerified;

            if (errorMessage.Contains("security") || errorMessage.Contains("policy"))
                return LoginResult.SecurityPolicyViolation;

            // Default to general failure
            return LoginResult.Failed;
        }
    }

}