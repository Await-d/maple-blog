using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// Represents a login attempt or session in the system
    /// </summary>
    public class LoginHistory : BaseEntity
    {
        /// <summary>
        /// User ID associated with the login attempt
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Email address used for login attempt
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Username used for login attempt
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Whether the login attempt was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Result of the login attempt
        /// </summary>
        public LoginResult Result { get; set; } = LoginResult.Failed;

        /// <summary>
        /// Reason for login failure (if applicable)
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// IP address from which the login was attempted
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string from the login request
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Device information extracted from user agent
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Browser information extracted from user agent
        /// </summary>
        public string? BrowserInfo { get; set; }

        /// <summary>
        /// Operating system information extracted from user agent
        /// </summary>
        public string? OperatingSystem { get; set; }

        /// <summary>
        /// Approximate location based on IP address
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Country code based on IP address
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// City based on IP address
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Session ID if login was successful
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// When the session expires (if login was successful)
        /// </summary>
        public DateTime? SessionExpiresAt { get; set; }

        /// <summary>
        /// When the user logged out (if applicable)
        /// </summary>
        public DateTime? LogoutAt { get; set; }

        /// <summary>
        /// Duration of the session in minutes
        /// </summary>
        public int? SessionDurationMinutes { get; set; }

        /// <summary>
        /// Type of login attempt
        /// </summary>
        public LoginType LoginType { get; set; } = LoginType.Standard;

        /// <summary>
        /// Whether two-factor authentication was used
        /// </summary>
        public bool TwoFactorUsed { get; set; } = false;

        /// <summary>
        /// Method of two-factor authentication used
        /// </summary>
        public string? TwoFactorMethod { get; set; }

        /// <summary>
        /// Additional metadata as JSON string
        /// </summary>
        public string? MetadataJson { get; set; }

        /// <summary>
        /// Risk score for this login attempt (0-100, higher = more risky)
        /// </summary>
        public int RiskScore { get; set; } = 0;

        /// <summary>
        /// Risk factors that contributed to the risk score
        /// </summary>
        public string? RiskFactors { get; set; }

        /// <summary>
        /// Whether this login was flagged for review
        /// </summary>
        public bool IsFlagged { get; set; } = false;

        /// <summary>
        /// Whether this login was blocked by security rules
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// Device type (Mobile, Desktop, Tablet, etc.)
        /// </summary>
        public string? DeviceType { get; set; }

        /// <summary>
        /// Browser name and version
        /// </summary>
        public string? Browser { get; set; }

        /// <summary>
        /// Device model information
        /// </summary>
        public string? DeviceModel { get; set; }

        /// <summary>
        /// Whether the device is mobile
        /// </summary>
        public bool IsMobile { get; set; } = false;

        /// <summary>
        /// Screen resolution
        /// </summary>
        public string? ScreenResolution { get; set; }

        /// <summary>
        /// Region information
        /// </summary>
        public string? Region { get; set; }

        /// <summary>
        /// Postal code
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Latitude coordinate
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Longitude coordinate
        /// </summary>
        public double? Longitude { get; set; }

        /// <summary>
        /// Timezone information
        /// </summary>
        public string? Timezone { get; set; }
    }

    /// <summary>
    /// Enumeration of login attempt results
    /// </summary>
    public enum LoginResult
    {
        /// <summary>
        /// Login attempt failed
        /// </summary>
        Failed = 0,

        /// <summary>
        /// Login was successful
        /// </summary>
        Success = 1,

        /// <summary>
        /// Login failed due to invalid credentials
        /// </summary>
        InvalidCredentials = 2,

        /// <summary>
        /// Login failed due to account being locked
        /// </summary>
        AccountLocked = 3,

        /// <summary>
        /// Login failed due to account being disabled
        /// </summary>
        AccountDisabled = 4,

        /// <summary>
        /// Login failed due to account not being verified
        /// </summary>
        AccountNotVerified = 5,

        /// <summary>
        /// Login failed due to too many attempts
        /// </summary>
        TooManyAttempts = 6,

        /// <summary>
        /// Login failed due to two-factor authentication failure
        /// </summary>
        TwoFactorFailed = 7,

        /// <summary>
        /// Login failed due to security policy
        /// </summary>
        SecurityPolicyViolation = 8,

        /// <summary>
        /// Login was blocked by fraud detection
        /// </summary>
        FraudDetected = 9
    }

    /// <summary>
    /// Enumeration of login types
    /// </summary>
    public enum LoginType
    {
        /// <summary>
        /// Standard email/password login
        /// </summary>
        Standard = 0,

        /// <summary>
        /// API token authentication
        /// </summary>
        ApiToken = 1,

        /// <summary>
        /// Social media login (OAuth)
        /// </summary>
        Social = 2,

        /// <summary>
        /// Single sign-on
        /// </summary>
        SSO = 3,

        /// <summary>
        /// Administrative login
        /// </summary>
        Administrative = 4,

        /// <summary>
        /// Password reset login
        /// </summary>
        PasswordReset = 5,

        /// <summary>
        /// Email verification login
        /// </summary>
        EmailVerification = 6
    }
}