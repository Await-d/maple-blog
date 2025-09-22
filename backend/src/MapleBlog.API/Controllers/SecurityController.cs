using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs;
using System.Security.Claims;

namespace MapleBlog.API.Controllers;

/// <summary>
/// Security monitoring and login history management API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly ILoginTrackingService _loginTrackingService;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(
        ILoginTrackingService loginTrackingService,
        ILogger<SecurityController> logger)
    {
        _loginTrackingService = loginTrackingService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's login history
    /// </summary>
    [HttpGet("login-history")]
    [Authorize]
    public async Task<IActionResult> GetUserLoginHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User not authenticated");

            var loginHistory = await _loginTrackingService.GetUserLoginHistoryAsync(
                userId.Value, page, pageSize, cancellationToken);

            var response = new
            {
                Data = loginHistory.Select(lh => new
                {
                    lh.Id,
                    lh.IsSuccessful,
                    lh.Result,
                    lh.IpAddress,
                    lh.Location,
                    lh.DeviceInfo,
                    lh.BrowserInfo,
                    lh.OperatingSystem,
                    lh.LoginType,
                    lh.TwoFactorUsed,
                    lh.RiskScore,
                    lh.IsFlagged,
                    lh.IsBlocked,
                    lh.CreatedAt,
                    lh.LogoutAt,
                    lh.SessionDurationMinutes
                }),
                Page = page,
                PageSize = pageSize,
                HasMore = loginHistory.Count() == pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user login history");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get current user's active sessions
    /// </summary>
    [HttpGet("active-sessions")]
    [Authorize]
    public async Task<IActionResult> GetActiveSessions(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User not authenticated");

            var sessions = await _loginTrackingService.GetActiveSessionsAsync(userId.Value, cancellationToken);

            var response = sessions.Select(s => new
            {
                s.Id,
                s.IpAddress,
                s.Location,
                s.DeviceInfo,
                s.BrowserInfo,
                s.OperatingSystem,
                s.SessionId,
                s.CreatedAt,
                s.SessionExpiresAt,
                IsCurrent = IsCurrentSession(s.SessionId)
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active sessions for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get user's login statistics
    /// </summary>
    [HttpGet("login-statistics")]
    [Authorize]
    public async Task<IActionResult> GetLoginStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User not authenticated");

            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var statistics = await _loginTrackingService.GetUserLoginStatisticsAsync(
                userId.Value, from, to, cancellationToken);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve login statistics for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Terminate a specific session
    /// </summary>
    [HttpPost("terminate-session/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> TerminateSession(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User not authenticated");

            // Verify the session belongs to the current user
            var sessions = await _loginTrackingService.GetActiveSessionsAsync(userId.Value, cancellationToken);
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);

            if (session == null)
                return NotFound("Session not found or does not belong to the current user");

            await _loginTrackingService.TerminateSessionAsync(sessionId, cancellationToken);

            _logger.LogInformation("User {UserId} terminated session {SessionId}", userId, sessionId);

            return Ok(new { Message = "Session terminated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate session {SessionId} for user {UserId}",
                sessionId, GetCurrentUserId());
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Terminate all other sessions except the current one
    /// </summary>
    [HttpPost("terminate-all-other-sessions")]
    [Authorize]
    public async Task<IActionResult> TerminateAllOtherSessions(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("User not authenticated");

            var currentSessionId = GetCurrentSessionId();
            if (string.IsNullOrEmpty(currentSessionId))
            {
                _logger.LogWarning("Could not determine current session ID for user {UserId}", userId);
                return BadRequest("Could not determine current session");
            }

            await _loginTrackingService.TerminateAllOtherSessionsAsync(
                userId.Value, currentSessionId, cancellationToken);

            _logger.LogInformation("User {UserId} terminated all other sessions", userId);

            return Ok(new { Message = "All other sessions terminated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate other sessions for user {UserId}", GetCurrentUserId());
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get suspicious activities (Admin only)
    /// </summary>
    [HttpGet("suspicious-activities")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSuspiciousActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var activities = await _loginTrackingService.GetSuspiciousActivitiesAsync(
                page, pageSize, cancellationToken);

            var response = new
            {
                Data = activities.Select(a => new
                {
                    a.Id,
                    a.Email,
                    a.UserName,
                    a.IsSuccessful,
                    a.Result,
                    a.IpAddress,
                    a.Location,
                    a.DeviceInfo,
                    a.BrowserInfo,
                    a.RiskScore,
                    a.RiskFactors,
                    a.IsFlagged,
                    a.IsBlocked,
                    a.CreatedAt
                }),
                Page = page,
                PageSize = pageSize,
                HasMore = activities.Count() == pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve suspicious activities");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get flagged login attempts (Admin only)
    /// </summary>
    [HttpGet("flagged-attempts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetFlaggedAttempts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var attempts = await _loginTrackingService.GetFlaggedAttemptsAsync(
                page, pageSize, cancellationToken);

            var response = new
            {
                Data = attempts.Select(a => new
                {
                    a.Id,
                    a.Email,
                    a.UserName,
                    a.IsSuccessful,
                    a.Result,
                    a.FailureReason,
                    a.IpAddress,
                    a.Location,
                    a.DeviceInfo,
                    a.BrowserInfo,
                    a.RiskScore,
                    a.RiskFactors,
                    a.IsFlagged,
                    a.IsBlocked,
                    a.CreatedAt
                }),
                Page = page,
                PageSize = pageSize,
                HasMore = attempts.Count() == pageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve flagged attempts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Mark a login attempt as reviewed (Admin only)
    /// </summary>
    [HttpPost("mark-reviewed/{loginHistoryId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarkAsReviewed(
        Guid loginHistoryId,
        [FromBody] ReviewLoginAttemptRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUserId = GetCurrentUserId();
            if (adminUserId == null)
                return Unauthorized("Admin not authenticated");

            await _loginTrackingService.MarkAsReviewedAsync(
                loginHistoryId, adminUserId.Value, request.ReviewNotes, cancellationToken);

            _logger.LogInformation("Admin {AdminUserId} reviewed login attempt {LoginHistoryId}",
                adminUserId, loginHistoryId);

            return Ok(new { Message = "Login attempt marked as reviewed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark login attempt {LoginHistoryId} as reviewed", loginHistoryId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get security dashboard summary (Admin only)
    /// </summary>
    [HttpGet("dashboard-summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSecurityDashboardSummary(CancellationToken cancellationToken = default)
    {
        try
        {
            var suspiciousCount = (await _loginTrackingService.GetSuspiciousActivitiesAsync(1, 1, cancellationToken)).Count();
            var flaggedCount = (await _loginTrackingService.GetFlaggedAttemptsAsync(1, 1, cancellationToken)).Count();

            // Get recent suspicious activities for the summary
            var recentSuspicious = await _loginTrackingService.GetSuspiciousActivitiesAsync(1, 5, cancellationToken);

            var summary = new
            {
                SuspiciousActivitiesCount = suspiciousCount,
                FlaggedAttemptsCount = flaggedCount,
                RecentSuspiciousActivities = recentSuspicious.Select(a => new
                {
                    a.Id,
                    a.Email,
                    a.IpAddress,
                    a.Location,
                    a.RiskScore,
                    a.CreatedAt
                }),
                LastUpdated = DateTime.UtcNow
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve security dashboard summary");
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                         User.FindFirst("sub")?.Value ??
                         User.FindFirst("userId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetCurrentSessionId()
    {
        // Try to extract session ID from JWT token claims
        return User.FindFirst("sessionId")?.Value ??
               User.FindFirst("jti")?.Value; // JWT ID can be used as session ID
    }

    private bool IsCurrentSession(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        var currentSessionId = GetCurrentSessionId();
        return !string.IsNullOrEmpty(currentSessionId) && currentSessionId == sessionId;
    }

    #endregion
}

/// <summary>
/// Request model for reviewing login attempts
/// </summary>
public class ReviewLoginAttemptRequest
{
    public string? ReviewNotes { get; set; }
}