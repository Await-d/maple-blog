using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Login tracking and security monitoring service interface
/// </summary>
public interface ILoginTrackingService
{
    /// <summary>
    /// Record a login attempt with comprehensive tracking information
    /// </summary>
    Task<LoginHistory> RecordLoginAttemptAsync(LoginTrackingInfo trackingInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update login session when it ends (logout, expiration)
    /// </summary>
    Task UpdateSessionEndAsync(string sessionId, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze login attempt for security risks
    /// </summary>
    Task<SecurityAnalysisResult> AnalyzeLoginSecurityAsync(LoginTrackingInfo trackingInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email has too many recent failed attempts
    /// </summary>
    Task<bool> IsEmailBlockedAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if IP address has too many recent failed attempts
    /// </summary>
    Task<bool> IsIpAddressBlockedAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get login statistics for a user
    /// </summary>
    Task<LoginStatistics> GetUserLoginStatisticsAsync(Guid userId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent login history for a user
    /// </summary>
    Task<IEnumerable<LoginHistory>> GetUserLoginHistoryAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active sessions for a user
    /// </summary>
    Task<IEnumerable<LoginHistory>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminate a specific session
    /// </summary>
    Task TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminate all sessions for a user except the current one
    /// </summary>
    Task TerminateAllOtherSessionsAsync(Guid userId, string currentSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get suspicious activities for security monitoring
    /// </summary>
    Task<IEnumerable<LoginHistory>> GetSuspiciousActivitiesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get flagged login attempts requiring manual review
    /// </summary>
    Task<IEnumerable<LoginHistory>> GetFlaggedAttemptsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark login attempt as reviewed by admin
    /// </summary>
    Task MarkAsReviewedAsync(Guid loginHistoryId, Guid reviewedBy, string? reviewNotes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse user agent string to extract device and browser information
    /// </summary>
    UserAgentInfo ParseUserAgent(string userAgent);

    /// <summary>
    /// Get geographic location from IP address
    /// </summary>
    Task<LocationInfo> GetLocationFromIpAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old login history records
    /// </summary>
    Task CleanupOldRecordsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate security alert for suspicious activities
    /// </summary>
    Task GenerateSecurityAlertAsync(SecurityAlertType alertType, LoginHistory loginHistory, string details, CancellationToken cancellationToken = default);
}

/// <summary>
/// Login tracking information for recording attempts
/// </summary>
public class LoginTrackingInfo
{
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public Guid? UserId { get; set; }
    public bool IsSuccessful { get; set; }
    public LoginResult Result { get; set; }
    public string? FailureReason { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public LoginType LoginType { get; set; } = LoginType.Standard;
    public bool TwoFactorUsed { get; set; }
    public string? TwoFactorMethod { get; set; }
    public string? SessionId { get; set; }
    public DateTime? SessionExpiresAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Security analysis result for login attempts
/// </summary>
public class SecurityAnalysisResult
{
    public int RiskScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public bool ShouldBlock { get; set; }
    public bool ShouldFlag { get; set; }
    public SecurityRecommendation Recommendation { get; set; }
    public string? RecommendationReason { get; set; }
}

/// <summary>
/// User agent parsing result
/// </summary>
public class UserAgentInfo
{
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? BrowserName { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? OperatingSystemVersion { get; set; }
    public bool IsMobile { get; set; }
    public bool IsBot { get; set; }
    
    /// <summary>
    /// Combined browser information (name and version)
    /// </summary>
    public string? Browser => !string.IsNullOrEmpty(BrowserName) && !string.IsNullOrEmpty(BrowserVersion) 
        ? $"{BrowserName} {BrowserVersion}" 
        : BrowserName;
}

/// <summary>
/// Location information from IP address
/// </summary>
public class LocationInfo
{
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public string? Isp { get; set; }
    public string? Organization { get; set; }
    public bool IsVpn { get; set; }
    public bool IsProxy { get; set; }
    public bool IsTor { get; set; }
    
    /// <summary>
    /// Alias for Timezone property
    /// </summary>
    public string? TimeZone => Timezone;
}

/// <summary>
/// Security recommendation types
/// </summary>
public enum SecurityRecommendation
{
    Allow = 0,
    RequireTwoFactor = 1,
    RequireEmailVerification = 2,
    Block = 3,
    FlagForReview = 4,
    RequirePasswordReset = 5
}

/// <summary>
/// Security alert types for monitoring
/// </summary>
public enum SecurityAlertType
{
    SuspiciousLocation = 0,
    TooManyFailedAttempts = 1,
    ConcurrentSessions = 2,
    UnusualBrowser = 3,
    VpnDetected = 4,
    BotDetected = 5,
    PasswordSpray = 6,
    CredentialStuffing = 7,
    AccountTakeover = 8
}