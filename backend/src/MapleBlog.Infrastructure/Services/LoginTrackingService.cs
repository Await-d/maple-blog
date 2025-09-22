using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Login tracking and security monitoring service implementation
/// </summary>
public class LoginTrackingService : ILoginTrackingService
{
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly ILogger<LoginTrackingService> _logger;
    private readonly SecuritySettings _securitySettings;

    // Risk factors and their weights
    private readonly Dictionary<string, int> _riskFactorWeights = new()
    {
        ["UnknownLocation"] = 30,
        ["UnknownDevice"] = 20,
        ["UnknownBrowser"] = 15,
        ["VpnDetected"] = 25,
        ["TorDetected"] = 40,
        ["ProxyDetected"] = 20,
        ["BotDetected"] = 50,
        ["RecentFailedAttempts"] = 35,
        ["ConcurrentSessions"] = 25,
        ["UnusualTimeOfDay"] = 10,
        ["HighFrequencyAttempts"] = 30,
        ["SuspiciousUserAgent"] = 20
    };

    public LoginTrackingService(
        ILoginHistoryRepository loginHistoryRepository,
        ILogger<LoginTrackingService> logger,
        IOptions<SecuritySettings> securitySettings)
    {
        _loginHistoryRepository = loginHistoryRepository;
        _logger = logger;
        _securitySettings = securitySettings.Value;
    }

    /// <summary>
    /// Record a comprehensive login attempt with security analysis
    /// </summary>
    public async Task<LoginHistory> RecordLoginAttemptAsync(LoginTrackingInfo trackingInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse user agent for device/browser info
            var userAgentInfo = ParseUserAgent(trackingInfo.UserAgent);

            // Get location information from IP
            var locationInfo = await GetLocationFromIpAsync(trackingInfo.IpAddress, cancellationToken);

            // Perform security analysis
            var securityAnalysis = await AnalyzeLoginSecurityAsync(trackingInfo, cancellationToken);

            // Create login history record
            var loginHistory = new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = trackingInfo.UserId,
                Email = trackingInfo.Email,
                UserName = trackingInfo.UserName,
                IsSuccessful = trackingInfo.IsSuccessful,
                Result = trackingInfo.Result,
                FailureReason = trackingInfo.FailureReason,
                IpAddress = trackingInfo.IpAddress,
                UserAgent = trackingInfo.UserAgent,
                DeviceInfo = FormatDeviceInfo(userAgentInfo),
                BrowserInfo = FormatBrowserInfo(userAgentInfo),
                OperatingSystem = FormatOperatingSystemInfo(userAgentInfo),
                Location = FormatLocationString(locationInfo),
                Country = locationInfo.Country,
                City = locationInfo.City,
                SessionId = trackingInfo.SessionId,
                SessionExpiresAt = trackingInfo.SessionExpiresAt,
                LoginType = trackingInfo.LoginType,
                TwoFactorUsed = trackingInfo.TwoFactorUsed,
                TwoFactorMethod = trackingInfo.TwoFactorMethod,
                MetadataJson = trackingInfo.Metadata != null ? JsonSerializer.Serialize(trackingInfo.Metadata) : null,
                RiskScore = securityAnalysis.RiskScore,
                RiskFactors = string.Join(", ", securityAnalysis.RiskFactors),
                IsFlagged = securityAnalysis.ShouldFlag,
                IsBlocked = securityAnalysis.ShouldBlock,
                CreatedAt = DateTime.UtcNow
            };

            // Save login history
            await _loginHistoryRepository.AddAsync(loginHistory, cancellationToken);

            // Generate security alerts if needed
            if (securityAnalysis.ShouldFlag || securityAnalysis.ShouldBlock)
            {
                await GenerateSecurityAlertAsync(
                    DetermineAlertType(securityAnalysis, locationInfo, userAgentInfo),
                    loginHistory,
                    securityAnalysis.RecommendationReason ?? "High risk login detected",
                    cancellationToken);
            }

            _logger.LogInformation(
                "Login attempt recorded: Email={Email}, IP={IpAddress}, Success={IsSuccessful}, RiskScore={RiskScore}",
                trackingInfo.Email, trackingInfo.IpAddress, trackingInfo.IsSuccessful, securityAnalysis.RiskScore);

            return loginHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record login attempt for email {Email}", trackingInfo.Email);
            throw;
        }
    }

    /// <summary>
    /// Update login session when it ends
    /// </summary>
    public async Task UpdateSessionEndAsync(string sessionId, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            await _loginHistoryRepository.MarkSessionLoggedOutAsync(sessionId, cancellationToken);
            _logger.LogDebug("Session {SessionId} marked as ended at {EndTime}", sessionId, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session end for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Analyze login attempt for comprehensive security risks
    /// </summary>
    public async Task<SecurityAnalysisResult> AnalyzeLoginSecurityAsync(LoginTrackingInfo trackingInfo, CancellationToken cancellationToken = default)
    {
        var result = new SecurityAnalysisResult();
        var riskFactors = new List<string>();
        var riskScore = 0;

        try
        {
            // Check for recent failed attempts from same email
            var recentFailures = await _loginHistoryRepository.GetFailedAttemptsCountByEmailAsync(
                trackingInfo.Email, TimeSpan.FromMinutes(_securitySettings.FailedAttemptWindowMinutes), cancellationToken);

            if (recentFailures >= _securitySettings.MaxFailedAttemptsPerEmail)
            {
                riskFactors.Add("RecentFailedAttempts");
                riskScore += _riskFactorWeights["RecentFailedAttempts"];
            }

            // Check for recent failed attempts from same IP
            var ipFailures = await _loginHistoryRepository.GetFailedAttemptsByIpCountAsync(
                trackingInfo.IpAddress, TimeSpan.FromMinutes(_securitySettings.FailedAttemptWindowMinutes), cancellationToken);

            if (ipFailures >= _securitySettings.MaxFailedAttemptsPerIp)
            {
                riskFactors.Add("HighFrequencyAttempts");
                riskScore += _riskFactorWeights["HighFrequencyAttempts"];
            }

            // Check for concurrent sessions from different locations if user exists
            if (trackingInfo.UserId.HasValue)
            {
                var hasConcurrentSessions = await _loginHistoryRepository.HasConcurrentSessionsFromDifferentLocationsAsync(
                    trackingInfo.UserId.Value, cancellationToken);

                if (hasConcurrentSessions)
                {
                    riskFactors.Add("ConcurrentSessions");
                    riskScore += _riskFactorWeights["ConcurrentSessions"];
                }

                // Check if this is a new location for the user
                if (await IsNewLocationForUserAsync(trackingInfo.UserId.Value, trackingInfo.IpAddress, cancellationToken))
                {
                    riskFactors.Add("UnknownLocation");
                    riskScore += _riskFactorWeights["UnknownLocation"];
                }

                // Check if this is a new device/browser for the user
                var deviceUserAgentInfo = ParseUserAgent(trackingInfo.UserAgent);
                if (await IsNewDeviceForUserAsync(trackingInfo.UserId.Value, deviceUserAgentInfo, cancellationToken))
                {
                    riskFactors.Add("UnknownDevice");
                    riskScore += _riskFactorWeights["UnknownDevice"];
                }
            }

            // Analyze user agent for suspicious patterns
            var userAgentInfo = ParseUserAgent(trackingInfo.UserAgent);
            if (userAgentInfo.IsBot)
            {
                riskFactors.Add("BotDetected");
                riskScore += _riskFactorWeights["BotDetected"];
            }

            if (IsSuspiciousUserAgent(trackingInfo.UserAgent))
            {
                riskFactors.Add("SuspiciousUserAgent");
                riskScore += _riskFactorWeights["SuspiciousUserAgent"];
            }

            // Get location info for additional checks
            var locationInfo = await GetLocationFromIpAsync(trackingInfo.IpAddress, cancellationToken);

            if (locationInfo.IsVpn)
            {
                riskFactors.Add("VpnDetected");
                riskScore += _riskFactorWeights["VpnDetected"];
            }

            if (locationInfo.IsTor)
            {
                riskFactors.Add("TorDetected");
                riskScore += _riskFactorWeights["TorDetected"];
            }

            if (locationInfo.IsProxy)
            {
                riskFactors.Add("ProxyDetected");
                riskScore += _riskFactorWeights["ProxyDetected"];
            }

            // Check for unusual time of day (if we have historical data)
            if (trackingInfo.UserId.HasValue && await IsUnusualTimeOfDayAsync(trackingInfo.UserId.Value, DateTime.UtcNow, cancellationToken))
            {
                riskFactors.Add("UnusualTimeOfDay");
                riskScore += _riskFactorWeights["UnusualTimeOfDay"];
            }

            // Determine recommendations based on risk score and factors
            result.RiskScore = Math.Min(riskScore, 100); // Cap at 100
            result.RiskFactors = riskFactors;
            result.ShouldBlock = riskScore >= _securitySettings.BlockThresholdScore || recentFailures >= _securitySettings.MaxFailedAttemptsPerEmail;
            result.ShouldFlag = riskScore >= _securitySettings.FlagThresholdScore;

            if (result.ShouldBlock)
            {
                result.Recommendation = SecurityRecommendation.Block;
                result.RecommendationReason = "High risk score or too many failed attempts";
            }
            else if (result.ShouldFlag)
            {
                result.Recommendation = SecurityRecommendation.FlagForReview;
                result.RecommendationReason = "Elevated risk score requires review";
            }
            else if (riskFactors.Contains("UnknownLocation") || riskFactors.Contains("UnknownDevice"))
            {
                result.Recommendation = SecurityRecommendation.RequireTwoFactor;
                result.RecommendationReason = "Login from new location or device";
            }
            else
            {
                result.Recommendation = SecurityRecommendation.Allow;
                result.RecommendationReason = "Low risk login attempt";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze login security for email {Email}", trackingInfo.Email);

            // Return a safe default in case of error
            return new SecurityAnalysisResult
            {
                RiskScore = 50,
                RiskFactors = new List<string> { "AnalysisError" },
                ShouldFlag = true,
                Recommendation = SecurityRecommendation.FlagForReview,
                RecommendationReason = "Security analysis failed, requires manual review"
            };
        }
    }

    /// <summary>
    /// Check if email is currently blocked due to too many failed attempts
    /// </summary>
    public async Task<bool> IsEmailBlockedAsync(string email, CancellationToken cancellationToken = default)
    {
        var failedAttempts = await _loginHistoryRepository.GetFailedAttemptsCountByEmailAsync(
            email, TimeSpan.FromMinutes(_securitySettings.FailedAttemptWindowMinutes), cancellationToken);

        return failedAttempts >= _securitySettings.MaxFailedAttemptsPerEmail;
    }

    /// <summary>
    /// Check if IP address is currently blocked due to too many failed attempts
    /// </summary>
    public async Task<bool> IsIpAddressBlockedAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        var failedAttempts = await _loginHistoryRepository.GetFailedAttemptsByIpCountAsync(
            ipAddress, TimeSpan.FromMinutes(_securitySettings.FailedAttemptWindowMinutes), cancellationToken);

        return failedAttempts >= _securitySettings.MaxFailedAttemptsPerIp;
    }

    /// <summary>
    /// Get comprehensive login statistics for a user
    /// </summary>
    public async Task<LoginStatistics> GetUserLoginStatisticsAsync(Guid userId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _loginHistoryRepository.GetLoginStatisticsAsync(userId, fromDate, toDate, cancellationToken);
    }

    /// <summary>
    /// Get recent login history for a user with pagination
    /// </summary>
    public async Task<IEnumerable<LoginHistory>> GetUserLoginHistoryAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _loginHistoryRepository.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Get all active sessions for a user
    /// </summary>
    public async Task<IEnumerable<LoginHistory>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _loginHistoryRepository.GetActiveSessionsAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Terminate a specific session by session ID
    /// </summary>
    public async Task TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _loginHistoryRepository.MarkSessionLoggedOutAsync(sessionId, cancellationToken);
        _logger.LogInformation("Session {SessionId} terminated", sessionId);
    }

    /// <summary>
    /// Terminate all sessions for a user except the current one
    /// </summary>
    public async Task TerminateAllOtherSessionsAsync(Guid userId, string currentSessionId, CancellationToken cancellationToken = default)
    {
        var activeSessions = await _loginHistoryRepository.GetActiveSessionsAsync(userId, cancellationToken);
        var sessionsToTerminate = activeSessions
            .Where(s => s.SessionId != currentSessionId && !string.IsNullOrEmpty(s.SessionId))
            .Select(s => s.SessionId!)
            .ToList();

        if (sessionsToTerminate.Any())
        {
            // Mark sessions as expired with a timeout
            var sessionTimeout = TimeSpan.FromHours(24); // Default session timeout
            await _loginHistoryRepository.BulkMarkSessionsExpiredAsync(sessionTimeout, cancellationToken);
            _logger.LogInformation("Terminated {Count} sessions for user {UserId}", sessionsToTerminate.Count, userId);
        }
    }

    /// <summary>
    /// Get suspicious activities for security monitoring
    /// </summary>
    public async Task<IEnumerable<LoginHistory>> GetSuspiciousActivitiesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // Get suspicious activities from the last 24 hours
        var timeWindow = TimeSpan.FromHours(24);
        return await _loginHistoryRepository.GetSuspiciousActivitiesAsync(timeWindow, cancellationToken);
    }

    /// <summary>
    /// Get flagged login attempts requiring manual review
    /// </summary>
    public async Task<IEnumerable<LoginHistory>> GetFlaggedAttemptsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // Get flagged attempts from the last 24 hours
        var timeWindow = TimeSpan.FromHours(24);
        return await _loginHistoryRepository.GetFlaggedAttemptsAsync(timeWindow, cancellationToken);
    }

    /// <summary>
    /// Mark login attempt as reviewed by admin (placeholder implementation)
    /// </summary>
    public async Task MarkAsReviewedAsync(Guid loginHistoryId, Guid reviewedBy, string? reviewNotes = null, CancellationToken cancellationToken = default)
    {
        // This would typically update a review status field in the database
        // For now, we'll just log the action
        _logger.LogInformation("Login attempt {LoginHistoryId} reviewed by {ReviewedBy}: {ReviewNotes}",
            loginHistoryId, reviewedBy, reviewNotes);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Parse user agent string to extract device and browser information
    /// </summary>
    public UserAgentInfo ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return new UserAgentInfo();
        }

        var info = new UserAgentInfo();

        // Detect if it's a bot
        var botPatterns = new[]
        {
            @"bot", @"crawler", @"spider", @"scraper", @"curl", @"wget",
            @"python", @"java", @"perl", @"ruby", @"go-http-client"
        };

        info.IsBot = botPatterns.Any(pattern =>
            Regex.IsMatch(userAgent, pattern, RegexOptions.IgnoreCase));

        // Extract browser information
        if (userAgent.Contains("Chrome"))
        {
            info.BrowserName = "Chrome";
            var match = Regex.Match(userAgent, @"Chrome\/(\d+\.?\d*)");
            if (match.Success) info.BrowserVersion = match.Groups[1].Value;
        }
        else if (userAgent.Contains("Firefox"))
        {
            info.BrowserName = "Firefox";
            var match = Regex.Match(userAgent, @"Firefox\/(\d+\.?\d*)");
            if (match.Success) info.BrowserVersion = match.Groups[1].Value;
        }
        else if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
        {
            info.BrowserName = "Safari";
            var match = Regex.Match(userAgent, @"Version\/(\d+\.?\d*)");
            if (match.Success) info.BrowserVersion = match.Groups[1].Value;
        }
        else if (userAgent.Contains("Edge"))
        {
            info.BrowserName = "Edge";
            var match = Regex.Match(userAgent, @"Edge\/(\d+\.?\d*)");
            if (match.Success) info.BrowserVersion = match.Groups[1].Value;
        }

        // Extract operating system
        if (userAgent.Contains("Windows NT"))
        {
            info.OperatingSystem = "Windows";
            var match = Regex.Match(userAgent, @"Windows NT (\d+\.?\d*)");
            if (match.Success) info.OperatingSystemVersion = match.Groups[1].Value;
        }
        else if (userAgent.Contains("Mac OS X"))
        {
            info.OperatingSystem = "macOS";
            var match = Regex.Match(userAgent, @"Mac OS X (\d+[_\.]?\d*[_\.]?\d*)");
            if (match.Success) info.OperatingSystemVersion = match.Groups[1].Value.Replace("_", ".");
        }
        else if (userAgent.Contains("Linux"))
        {
            info.OperatingSystem = "Linux";
        }
        else if (userAgent.Contains("Android"))
        {
            info.OperatingSystem = "Android";
            var match = Regex.Match(userAgent, @"Android (\d+\.?\d*)");
            if (match.Success) info.OperatingSystemVersion = match.Groups[1].Value;
        }
        else if (userAgent.Contains("iOS") || userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
        {
            info.OperatingSystem = "iOS";
            var match = Regex.Match(userAgent, @"OS (\d+[_\.]?\d*[_\.]?\d*)");
            if (match.Success) info.OperatingSystemVersion = match.Groups[1].Value.Replace("_", ".");
        }

        // Detect mobile devices
        info.IsMobile = userAgent.Contains("Mobile") || userAgent.Contains("Android") ||
                       userAgent.Contains("iPhone") || userAgent.Contains("iPad");

        // Extract device information
        if (userAgent.Contains("iPhone"))
        {
            info.DeviceType = "Mobile";
            info.DeviceName = "iPhone";
        }
        else if (userAgent.Contains("iPad"))
        {
            info.DeviceType = "Tablet";
            info.DeviceName = "iPad";
        }
        else if (userAgent.Contains("Android"))
        {
            info.DeviceType = info.IsMobile ? "Mobile" : "Tablet";
            info.DeviceName = "Android Device";
        }
        else
        {
            info.DeviceType = "Desktop";
        }

        return info;
    }

    /// <summary>
    /// Get geographic location information from IP address (stub implementation)
    /// </summary>
    public async Task<LocationInfo> GetLocationFromIpAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would call a geolocation service like MaxMind, IPinfo, etc.
        // For now, return a placeholder with some basic checks

        await Task.Delay(1, cancellationToken); // Simulate async operation

        var locationInfo = new LocationInfo();

        // Check for local/private IP addresses
        if (IsPrivateIpAddress(ipAddress))
        {
            locationInfo.Country = "Unknown";
            locationInfo.City = "Local Network";
            return locationInfo;
        }

        // Placeholder implementation - in production, integrate with a real IP geolocation service
        locationInfo.Country = "Unknown";
        locationInfo.City = "Unknown";
        locationInfo.IsVpn = false;
        locationInfo.IsProxy = false;
        locationInfo.IsTor = false;

        return locationInfo;
    }

    /// <summary>
    /// Clean up old login history records based on retention policy
    /// </summary>
    public async Task CleanupOldRecordsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        var retentionDays = (int)retentionPeriod.TotalDays;
        await _loginHistoryRepository.DeleteOldRecordsAsync(retentionDays, cancellationToken);
        _logger.LogInformation("Cleaned up login history records older than {RetentionDays} days", retentionDays);
    }

    /// <summary>
    /// Generate security alert for suspicious activities
    /// </summary>
    public async Task GenerateSecurityAlertAsync(SecurityAlertType alertType, LoginHistory loginHistory, string details, CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would create notifications, send emails, etc.
        _logger.LogWarning(
            "Security Alert: {AlertType} - User: {Email}, IP: {IpAddress}, Details: {Details}",
            alertType, loginHistory.Email, loginHistory.IpAddress, details);

        await Task.CompletedTask;
    }

    #region Private Helper Methods

    private static string? FormatDeviceInfo(UserAgentInfo userAgentInfo)
    {
        if (string.IsNullOrEmpty(userAgentInfo.DeviceType))
            return null;

        return string.IsNullOrEmpty(userAgentInfo.DeviceName)
            ? userAgentInfo.DeviceType
            : $"{userAgentInfo.DeviceName} ({userAgentInfo.DeviceType})";
    }

    private static string? FormatBrowserInfo(UserAgentInfo userAgentInfo)
    {
        if (string.IsNullOrEmpty(userAgentInfo.BrowserName))
            return null;

        return string.IsNullOrEmpty(userAgentInfo.BrowserVersion)
            ? userAgentInfo.BrowserName
            : $"{userAgentInfo.BrowserName} {userAgentInfo.BrowserVersion}";
    }

    private static string? FormatOperatingSystemInfo(UserAgentInfo userAgentInfo)
    {
        if (string.IsNullOrEmpty(userAgentInfo.OperatingSystem))
            return null;

        return string.IsNullOrEmpty(userAgentInfo.OperatingSystemVersion)
            ? userAgentInfo.OperatingSystem
            : $"{userAgentInfo.OperatingSystem} {userAgentInfo.OperatingSystemVersion}";
    }

    private static string? FormatLocationString(LocationInfo locationInfo)
    {
        if (string.IsNullOrEmpty(locationInfo.City) && string.IsNullOrEmpty(locationInfo.Country))
            return null;

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(locationInfo.City))
            parts.Add(locationInfo.City);
        if (!string.IsNullOrEmpty(locationInfo.Country))
            parts.Add(locationInfo.Country);

        return string.Join(", ", parts);
    }

    private SecurityAlertType DetermineAlertType(SecurityAnalysisResult analysis, LocationInfo location, UserAgentInfo userAgent)
    {
        if (analysis.RiskFactors.Contains("TorDetected"))
            return SecurityAlertType.VpnDetected;
        if (analysis.RiskFactors.Contains("BotDetected"))
            return SecurityAlertType.BotDetected;
        if (analysis.RiskFactors.Contains("UnknownLocation"))
            return SecurityAlertType.SuspiciousLocation;
        if (analysis.RiskFactors.Contains("RecentFailedAttempts"))
            return SecurityAlertType.TooManyFailedAttempts;
        if (analysis.RiskFactors.Contains("ConcurrentSessions"))
            return SecurityAlertType.ConcurrentSessions;

        return SecurityAlertType.AccountTakeover; // Default for high-risk situations
    }

    private async Task<bool> IsNewLocationForUserAsync(Guid userId, string ipAddress, CancellationToken cancellationToken)
    {
        // Check if user has logged in from this location before (within last 30 days)
        var recentLogins = await _loginHistoryRepository.GetByUserIdAsync(userId, 1, 50, cancellationToken);
        var recentIps = recentLogins.Where(lh => lh.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                                   .Select(lh => lh.IpAddress)
                                   .Where(ip => !string.IsNullOrEmpty(ip))
                                   .Distinct()
                                   .ToList();

        return !recentIps.Contains(ipAddress);
    }

    private async Task<bool> IsNewDeviceForUserAsync(Guid userId, UserAgentInfo userAgentInfo, CancellationToken cancellationToken)
    {
        // Check if user has used this browser/device combination before
        var recentLogins = await _loginHistoryRepository.GetByUserIdAsync(userId, 1, 50, cancellationToken);
        var deviceFingerprint = $"{userAgentInfo.BrowserName}|{userAgentInfo.OperatingSystem}|{userAgentInfo.DeviceType}";

        var knownFingerprints = recentLogins.Where(lh => lh.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .Where(lh => !string.IsNullOrEmpty(lh.BrowserInfo) && !string.IsNullOrEmpty(lh.OperatingSystem))
            .Select(lh => $"{lh.BrowserInfo}|{lh.OperatingSystem}|{lh.DeviceInfo}")
            .Distinct()
            .ToList();

        return !knownFingerprints.Any(fp => fp.Contains(deviceFingerprint.Split('|')[0] ?? "")
                                          && fp.Contains(deviceFingerprint.Split('|')[1] ?? ""));
    }

    private async Task<bool> IsUnusualTimeOfDayAsync(Guid userId, DateTime loginTime, CancellationToken cancellationToken)
    {
        // Get user's typical login hours from history
        var recentLogins = await _loginHistoryRepository.GetByUserIdAsync(userId, 1, 100, cancellationToken);
        var successfulLogins = recentLogins.Where(lh => lh.IsSuccessful)
                                          .Select(lh => lh.CreatedAt.Hour)
                                          .ToList();

        if (!successfulLogins.Any())
            return false; // No history to compare

        var currentHour = loginTime.Hour;
        var typicalHours = successfulLogins.GroupBy(h => h)
                                          .OrderByDescending(g => g.Count())
                                          .Take(8) // Consider top 8 most common hours
                                          .Select(g => g.Key)
                                          .ToList();

        return !typicalHours.Contains(currentHour);
    }

    private static bool IsSuspiciousUserAgent(string userAgent)
    {
        var suspiciousPatterns = new[]
        {
            @"^curl", @"^wget", @"^python", @"^java", @"^perl",
            @"sqlmap", @"nikto", @"nmap", @"masscan",
            @"<script", @"javascript:", @"eval\(",
            string.Empty // Empty user agent
        };

        return suspiciousPatterns.Any(pattern =>
            Regex.IsMatch(userAgent, pattern, RegexOptions.IgnoreCase));
    }

    private static bool IsPrivateIpAddress(string ipAddress)
    {
        // Check for common private IP ranges
        var privateRanges = new[]
        {
            @"^127\.", @"^10\.", @"^172\.1[6-9]\.", @"^172\.2[0-9]\.", @"^172\.3[0-1]\.",
            @"^192\.168\.", @"^::1$", @"^fe80:", @"^localhost$"
        };

        return privateRanges.Any(range =>
            Regex.IsMatch(ipAddress, range, RegexOptions.IgnoreCase));
    }

    #endregion
}

/// <summary>
/// Security settings configuration
/// </summary>
public class SecuritySettings
{
    public int MaxFailedAttemptsPerEmail { get; set; } = 5;
    public int MaxFailedAttemptsPerIp { get; set; } = 10;
    public int FailedAttemptWindowMinutes { get; set; } = 15;
    public int BlockThresholdScore { get; set; } = 80;
    public int FlagThresholdScore { get; set; } = 60;
}