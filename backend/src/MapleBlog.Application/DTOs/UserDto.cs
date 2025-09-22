using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs
{
    /// <summary>
    /// Data transfer object for user information
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// User's unique identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's unique username
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's full name (computed)
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// User's display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// User's avatar URL
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// User's role in the system
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// Whether the user's email has been verified
        /// </summary>
        public bool IsEmailVerified { get; set; }

        /// <summary>
        /// Whether the user account is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Date and time when the user was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time of the user's last login
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Date and time when the user was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Data transfer object for user login request
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Email address or username
        /// </summary>
        [Required(ErrorMessage = "Email or username is required")]
        [StringLength(254, MinimumLength = 3, ErrorMessage = "Email or username must be between 3 and 254 characters")]
        public string EmailOrUsername { get; set; } = string.Empty;

        /// <summary>
        /// Email address (for backward compatibility)
        /// </summary>
        public string Email
        {
            get => EmailOrUsername;
            set => EmailOrUsername = value;
        }

        /// <summary>
        /// User's password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Whether to remember the login (longer token expiry)
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Data transfer object for user registration request
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// User's email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's desired username
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores, and hyphens")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// User's password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation
        /// </summary>
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// User's first name
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Agreement to terms of service
        /// </summary>
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the terms of service")]
        public bool AgreeToTerms { get; set; } = false;
    }

    /// <summary>
    /// Data transfer object for password reset request
    /// </summary>
    public class PasswordResetRequest
    {
        /// <summary>
        /// Email address of the user requesting password reset
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for password reset confirmation
    /// </summary>
    public class PasswordResetConfirmRequest
    {
        /// <summary>
        /// Password reset token
        /// </summary>
        [Required(ErrorMessage = "Reset token is required")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// New password
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Password confirmation
        /// </summary>
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for user profile update request
    /// </summary>
    public class UpdateUserProfileRequest
    {
        /// <summary>
        /// User's first name
        /// </summary>
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "First name must be between 1 and 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name
        /// </summary>
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Last name must be between 1 and 100 characters")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's avatar URL
        /// </summary>
        [StringLength(500, ErrorMessage = "Avatar URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? Avatar { get; set; }
    }

    /// <summary>
    /// Data transfer object for email change request
    /// </summary>
    public class ChangeEmailRequest
    {
        /// <summary>
        /// New email address
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Current password for verification
        /// </summary>
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for password change request
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Current password
        /// </summary>
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// New password
        /// </summary>
        [Required(ErrorMessage = "New password is required")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// New password confirmation
        /// </summary>
        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for email verification request
    /// </summary>
    public class EmailVerificationRequest
    {
        /// <summary>
        /// Email verification token
        /// </summary>
        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for refresh token request
    /// </summary>
    public class RefreshTokenRequest
    {
        /// <summary>
        /// Refresh token
        /// </summary>
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}