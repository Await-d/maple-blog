namespace MapleBlog.Application.Validators
{
    /// <summary>
    /// Rate limiting service interface
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Check if a request should be rate limited
        /// </summary>
        Task<bool> IsRateLimitedAsync(string key, int maxRequests, TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current rate limit status
        /// </summary>
        Task<(int RemainingRequests, DateTime ResetTime)> GetRateLimitStatusAsync(string key, int maxRequests, TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if an action is allowed for a specific identifier
        /// </summary>
        Task<bool> IsActionAllowedAsync(string action, string identifier, int maxAttempts, TimeSpan timeWindow, CancellationToken cancellationToken = default);

        /// <summary>
        /// Record an action attempt for a specific identifier
        /// </summary>
        Task RecordActionAsync(string action, string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset attempts for a specific action and identifier
        /// </summary>
        Task ResetAttemptsAsync(string action, string identifier, CancellationToken cancellationToken = default);
    }
}