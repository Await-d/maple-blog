using FluentValidation;
using MapleBlog.Application.DTOs;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Application.Validators
{
    /// <summary>
    /// Validator for user login requests
    /// </summary>
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.EmailOrUsername)
                .NotEmpty().WithMessage("Email or username is required.")
                .Length(3, 254).WithMessage("Email or username must be between 3 and 254 characters.")
                .Must(BeValidEmailOrUsername).WithMessage("Invalid email or username format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .Length(1, 128).WithMessage("Password must be between 1 and 128 characters.");

            // RememberMe is optional and has no specific validation rules beyond being a boolean
        }

        /// <summary>
        /// Validates that the input is either a valid email or a valid username
        /// </summary>
        private static bool BeValidEmailOrUsername(string emailOrUsername)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername))
                return false;

            // If it contains '@', treat as email and validate email format
            if (emailOrUsername.Contains('@'))
            {
                return Email.IsValidFormat(emailOrUsername);
            }

            // Otherwise, validate as username
            return IsValidUsername(emailOrUsername);
        }

        /// <summary>
        /// Validates username format
        /// </summary>
        private static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            // Username should be 3-50 characters and contain only letters, numbers, dots, underscores, and hyphens
            if (username.Length < 3 || username.Length > 50)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$");
        }
    }

    /// <summary>
    /// Advanced login validator with additional security checks
    /// </summary>
    public class AdvancedLoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public AdvancedLoginRequestValidator()
        {
            // Include all basic validations
            Include(new LoginRequestValidator());

            // Additional security validations
            RuleFor(x => x.EmailOrUsername)
                .Must(NotContainMaliciousContent).WithMessage("Invalid characters detected in email/username.");

            RuleFor(x => x.Password)
                .Must(NotContainMaliciousContent).WithMessage("Invalid characters detected in password.");

            // Custom rule to prevent common attack patterns
            RuleFor(x => x)
                .Must(NotBeCommonAttackPattern).WithMessage("Invalid login attempt detected.");
        }

        /// <summary>
        /// Checks for common malicious content patterns
        /// </summary>
        private static bool NotContainMaliciousContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Check for basic SQL injection patterns
            var sqlPatterns = new[]
            {
                "script",
                "<script",
                "javascript:",
                "vbscript:",
                "onload=",
                "onerror=",
                "union select",
                "drop table",
                "delete from",
                "insert into",
                "update set",
                "exec(",
                "execute(",
                "sp_",
                "xp_"
            };

            var lowerInput = input.ToLowerInvariant();
            return !sqlPatterns.Any(pattern => lowerInput.Contains(pattern));
        }

        /// <summary>
        /// Checks for common attack patterns in login requests
        /// </summary>
        private static bool NotBeCommonAttackPattern(LoginRequest request)
        {
            if (request == null)
                return false;

            var emailOrUsername = request.EmailOrUsername?.ToLowerInvariant() ?? string.Empty;
            var password = request.Password ?? string.Empty;

            // Check for common credential stuffing patterns
            var commonPatterns = new[]
            {
                "admin",
                "administrator",
                "root",
                "test",
                "guest",
                "demo"
            };

            // If both username and password are common patterns, it might be an attack
            var isCommonUsername = commonPatterns.Any(pattern => emailOrUsername.Contains(pattern));
            var isCommonPassword = commonPatterns.Any(pattern => password.ToLowerInvariant().Contains(pattern));

            // Additional checks for suspicious patterns
            var suspiciousPatterns = new[]
            {
                emailOrUsername == password, // Same username and password
                password.Length > 100, // Unusually long password
                emailOrUsername.Length > 200, // Unusually long username
                emailOrUsername.Contains(".."), // Directory traversal attempt
                emailOrUsername.Contains("//"), // Protocol injection attempt
                password.Contains("'"), // SQL injection attempt
                password.Contains("\""), // SQL injection attempt
                password.Contains(";"), // Command injection attempt
                password.Contains("--"), // SQL comment injection
            };

            // If it's a common username/password combo or shows suspicious patterns, flag it
            if (isCommonUsername && isCommonPassword)
                return false;

            return !suspiciousPatterns.Any(pattern => pattern);
        }
    }

    /// <summary>
    /// Rate limiting validator for login requests
    /// </summary>
    public class RateLimitedLoginRequestValidator : AbstractValidator<LoginRequest>
    {
        private readonly IRateLimitService _rateLimitService;

        public RateLimitedLoginRequestValidator(IRateLimitService rateLimitService)
        {
            _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));

            // Include basic validations
            Include(new AdvancedLoginRequestValidator());

            // Rate limiting validation
            RuleFor(x => x.EmailOrUsername)
                .MustAsync(NotExceedRateLimit).WithMessage("Too many login attempts. Please try again later.");
        }

        private async Task<bool> NotExceedRateLimit(string emailOrUsername, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(emailOrUsername))
                return false;

            // Check rate limit for this email/username
            var isAllowed = await _rateLimitService.IsActionAllowedAsync(
                "login_attempt",
                emailOrUsername,
                maxAttempts: 5,
                timeWindow: TimeSpan.FromMinutes(15),
                cancellationToken);

            return isAllowed;
        }
    }

}