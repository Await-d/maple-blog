using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Validators
{
    /// <summary>
    /// In-memory rate limiting service implementation
    /// </summary>
    public class InMemoryRateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryRateLimitService> _logger;

        public InMemoryRateLimitService(IMemoryCache cache, ILogger<InMemoryRateLimitService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> IsRateLimitedAsync(string key, int maxRequests, TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(false);

            var cacheKey = $"rate_limit:{key}";
            var now = DateTime.UtcNow;

            var requestData = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();

            // Remove expired entries
            var windowStart = now.Subtract(timeWindow);
            requestData.RemoveAll(timestamp => timestamp < windowStart);

            // Check if rate limit exceeded
            if (requestData.Count >= maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded for key {Key}. Current requests: {Count}, Max: {Max}",
                    key, requestData.Count, maxRequests);
                return Task.FromResult(true);
            }

            // Add current request
            requestData.Add(now);

            // Update cache
            _cache.Set(cacheKey, requestData, timeWindow);

            _logger.LogDebug("Rate limit check for key {Key}. Current requests: {Count}, Max: {Max}",
                key, requestData.Count, maxRequests);

            return Task.FromResult(false);
        }

        public Task<(int RemainingRequests, DateTime ResetTime)> GetRateLimitStatusAsync(string key, int maxRequests, TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult((maxRequests, DateTime.UtcNow.Add(timeWindow)));

            var cacheKey = $"rate_limit:{key}";
            var now = DateTime.UtcNow;

            var requestData = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();

            // Remove expired entries
            var windowStart = now.Subtract(timeWindow);
            requestData.RemoveAll(timestamp => timestamp < windowStart);

            var remainingRequests = Math.Max(0, maxRequests - requestData.Count);
            var resetTime = requestData.Any() ? requestData.Min().Add(timeWindow) : now.Add(timeWindow);

            return Task.FromResult((remainingRequests, resetTime));
        }

        public async Task<bool> IsActionAllowedAsync(string action, string identifier, int maxAttempts, TimeSpan timeWindow, CancellationToken cancellationToken = default)
        {
            var key = $"{action}:{identifier}";
            var isLimited = await IsRateLimitedAsync(key, maxAttempts, timeWindow, cancellationToken);
            return !isLimited;
        }

        public Task RecordActionAsync(string action, string identifier, CancellationToken cancellationToken = default)
        {
            var key = $"{action}:{identifier}";
            var cacheKey = $"rate_limit:{key}";
            var now = DateTime.UtcNow;

            var requestData = _cache.Get<List<DateTime>>(cacheKey) ?? new List<DateTime>();
            requestData.Add(now);

            // Cache for twice the window size to ensure cleanup
            _cache.Set(cacheKey, requestData, TimeSpan.FromMinutes(30));

            _logger.LogDebug("Recorded action {Action} for identifier {Identifier}", action, identifier);

            return Task.CompletedTask;
        }

        public Task ResetAttemptsAsync(string action, string identifier, CancellationToken cancellationToken = default)
        {
            var key = $"{action}:{identifier}";
            var cacheKey = $"rate_limit:{key}";

            _cache.Remove(cacheKey);

            _logger.LogDebug("Reset attempts for action {Action} and identifier {Identifier}", action, identifier);

            return Task.CompletedTask;
        }
    }
}