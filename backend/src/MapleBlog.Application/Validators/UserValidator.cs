using FluentValidation;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Application.Validators
{
    /// <summary>
    /// Validator for user registration requests
    /// </summary>
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        private readonly IAuthService _authService;

        public RegisterRequestValidator(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.")
                .Must(BeValidEmail).WithMessage("Invalid email format.")
                .MustAsync(BeUniqueEmail).WithMessage("Email address is already registered.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required.")
                .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
                .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Username can only contain letters, numbers, dots, underscores, and hyphens.")
                .MustAsync(BeUniqueUserName).WithMessage("Username is already taken.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
                .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
                .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
                .Must(HaveDigit).WithMessage("Password must contain at least one number.")
                .Must(HaveSpecialCharacter).WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .Length(1, 100).WithMessage("First name must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("First name can only contain letters, spaces, hyphens, apostrophes, and periods.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .Length(1, 100).WithMessage("Last name must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Last name can only contain letters, spaces, hyphens, apostrophes, and periods.");

            RuleFor(x => x.AgreeToTerms)
                .Equal(true).WithMessage("You must agree to the terms of service.");
        }

        private static bool BeValidEmail(string email)
        {
            return Email.IsValidFormat(email);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            return await _authService.IsEmailAvailableAsync(email, cancellationToken: cancellationToken);
        }

        private async Task<bool> BeUniqueUserName(string userName, CancellationToken cancellationToken)
        {
            return await _authService.IsUserNameAvailableAsync(userName, cancellationToken: cancellationToken);
        }

        private static bool HaveUppercase(string password)
        {
            return password.Any(char.IsUpper);
        }

        private static bool HaveLowercase(string password)
        {
            return password.Any(char.IsLower);
        }

        private static bool HaveDigit(string password)
        {
            return password.Any(char.IsDigit);
        }

        private static bool HaveSpecialCharacter(string password)
        {
            return password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }

    /// <summary>
    /// Validator for user profile update requests
    /// </summary>
    public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
    {
        public UpdateUserProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .Length(1, 100).WithMessage("First name must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("First name can only contain letters, spaces, hyphens, apostrophes, and periods.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .Length(1, 100).WithMessage("Last name must be between 1 and 100 characters.")
                .Matches(@"^[a-zA-Z\s\-'\.]+$").WithMessage("Last name can only contain letters, spaces, hyphens, apostrophes, and periods.");

            RuleFor(x => x.Avatar)
                .MaximumLength(500).WithMessage("Avatar URL cannot exceed 500 characters.")
                .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Avatar)).WithMessage("Invalid avatar URL format.");
        }

        private static bool BeValidUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return true;

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    /// <summary>
    /// Validator for email change requests
    /// </summary>
    public class ChangeEmailRequestValidator : AbstractValidator<ChangeEmailRequest>
    {
        private readonly IAuthService _authService;

        public ChangeEmailRequestValidator(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.")
                .Must(BeValidEmail).WithMessage("Invalid email format.")
                .MustAsync(BeUniqueEmail).WithMessage("Email address is already in use.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }

        private static bool BeValidEmail(string email)
        {
            return Email.IsValidFormat(email);
        }

        private async Task<bool> BeUniqueEmail(ChangeEmailRequest request, string email, CancellationToken cancellationToken)
        {
            // Note: In a real implementation, we would need the current user's ID to exclude from uniqueness check
            // This could be passed through validation context or as a parameter
            return await _authService.IsEmailAvailableAsync(email, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Validator for password change requests
    /// </summary>
    public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
                .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
                .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
                .Must(HaveDigit).WithMessage("Password must contain at least one number.")
                .Must(HaveSpecialCharacter).WithMessage("Password must contain at least one special character.")
                .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
        }

        private static bool HaveUppercase(string password)
        {
            return password.Any(char.IsUpper);
        }

        private static bool HaveLowercase(string password)
        {
            return password.Any(char.IsLower);
        }

        private static bool HaveDigit(string password)
        {
            return password.Any(char.IsDigit);
        }

        private static bool HaveSpecialCharacter(string password)
        {
            return password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }

    /// <summary>
    /// Validator for password reset requests
    /// </summary>
    public class PasswordResetRequestValidator : AbstractValidator<PasswordResetRequest>
    {
        public PasswordResetRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .Must(BeValidEmail).WithMessage("Invalid email format.");
        }

        private static bool BeValidEmail(string email)
        {
            return Email.IsValidFormat(email);
        }
    }

    /// <summary>
    /// Validator for password reset confirmation requests
    /// </summary>
    public class PasswordResetConfirmRequestValidator : AbstractValidator<PasswordResetConfirmRequest>
    {
        public PasswordResetConfirmRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Reset token is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
                .Must(HaveUppercase).WithMessage("Password must contain at least one uppercase letter.")
                .Must(HaveLowercase).WithMessage("Password must contain at least one lowercase letter.")
                .Must(HaveDigit).WithMessage("Password must contain at least one number.")
                .Must(HaveSpecialCharacter).WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");
        }

        private static bool HaveUppercase(string password)
        {
            return password.Any(char.IsUpper);
        }

        private static bool HaveLowercase(string password)
        {
            return password.Any(char.IsLower);
        }

        private static bool HaveDigit(string password)
        {
            return password.Any(char.IsDigit);
        }

        private static bool HaveSpecialCharacter(string password)
        {
            return password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }

    /// <summary>
    /// Validator for email verification requests
    /// </summary>
    public class EmailVerificationRequestValidator : AbstractValidator<EmailVerificationRequest>
    {
        public EmailVerificationRequestValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Verification token is required.");
        }
    }

    /// <summary>
    /// Validator for refresh token requests
    /// </summary>
    public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
    {
        public RefreshTokenRequestValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }
}