using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Infrastructure.Caching;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.API.Controllers;

/// <summary>
/// Cache management and monitoring API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class CacheController : ControllerBase
{
    private readonly ICacheManager _cacheManager;
    private readonly IResponseCacheConfigurationService _cacheConfigService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(
        ICacheManager cacheManager,
        IResponseCacheConfigurationService cacheConfigService,
        ILogger<CacheController> logger)
    {
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _cacheConfigService = cacheConfigService ?? throw new ArgumentNullException(nameof(cacheConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get cache statistics and performance metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    [HttpGet("stats")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(typeof(CacheStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CacheStatistics>> GetCacheStatistics(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _cacheManager.GetStatisticsAsync();
            if (stats == null)
            {
                return Ok(new CacheStatistics
                {
                    IsConnected = false,
                    TotalKeys = 0,
                    UsedMemory = 0,
                    UsedMemoryHuman = "N/A",
                    HitCount = 0,
                    MissCount = 0,
                    LastUpdated = DateTime.UtcNow
                });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, "Internal server error while retrieving cache statistics");
        }
    }

    /// <summary>
    /// Get cache configuration details
    /// </summary>
    /// <returns>Cache configuration information</returns>
    [HttpGet("config")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult GetCacheConfiguration()
    {
        try
        {
            var configInfo = new
            {
                GlobalEnabled = true, // This would come from configuration
                Rules = new[]
                {
                    new { Path = "/api/blog", Duration = "15 minutes", Method = "GET" },
                    new { Path = "/api/category", Duration = "1 hour", Method = "GET" },
                    new { Path = "/api/tag", Duration = "30 minutes", Method = "GET" },
                    new { Path = "/api/search", Duration = "15 minutes", Method = "GET" },
                    new { Path = "/api/archive", Duration = "30 minutes", Method = "GET" },
                    new { Path = "/api/home", Duration = "10 minutes", Method = "GET" }
                },
                NoCachePaths = new[]
                {
                    "/api/auth/*",
                    "/api/admin/*",
                    "/api/comments/*/like",
                    "/api/posts/*/views",
                    "/api/user/profile",
                    "/api/notification*"
                }
            };

            return Ok(configInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache configuration");
            return StatusCode(500, "Internal server error while retrieving cache configuration");
        }
    }

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("clear")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ClearAllCache(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheManager.ClearAllAsync();
            _logger.LogInformation("Cache cleared by admin user: {UserId}", User.Identity?.Name);

            return Ok(new { message = "All cache entries cleared successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, "Internal server error while clearing cache");
        }
    }

    /// <summary>
    /// Clear cache by pattern
    /// </summary>
    /// <param name="pattern">Cache key pattern (e.g., posts:*, category:*)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("clear/{pattern}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ClearCacheByPattern(
        [Required] string pattern,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return BadRequest("Pattern cannot be empty");
        }

        try
        {
            await _cacheManager.InvalidateByPatternAsync(pattern);
            _logger.LogInformation("Cache cleared by pattern '{Pattern}' by admin user: {UserId}",
                pattern, User.Identity?.Name);

            return Ok(new
            {
                message = $"Cache entries matching pattern '{pattern}' cleared successfully",
                pattern = pattern,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
            return StatusCode(500, $"Internal server error while clearing cache by pattern: {pattern}");
        }
    }

    /// <summary>
    /// Warm up cache for specific content types
    /// </summary>
    /// <param name="contentType">Content type to warm up (posts, categories, tags, homepage)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("warmup/{contentType}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> WarmUpCache(
        [Required] string contentType,
        CancellationToken cancellationToken = default)
    {
        var validContentTypes = new[] { "posts", "categories", "tags", "homepage" };
        if (!validContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            return BadRequest($"Invalid content type. Valid types: {string.Join(", ", validContentTypes)}");
        }

        try
        {
            await _cacheManager.WarmUpAsync(contentType.ToLowerInvariant());
            _logger.LogInformation("Cache warmed up for content type '{ContentType}' by admin user: {UserId}",
                contentType, User.Identity?.Name);

            return Ok(new
            {
                message = $"Cache warming initiated for content type '{contentType}'",
                contentType = contentType,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up cache for content type: {ContentType}", contentType);
            return StatusCode(500, $"Internal server error while warming up cache for: {contentType}");
        }
    }

    /// <summary>
    /// Get cache performance metrics for a specific time period
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache performance metrics</returns>
    [HttpGet("performance")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })] // Cache for 5 minutes
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetCachePerformance(
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _cacheManager.GetStatisticsAsync();

            // Mock performance data - in a real implementation, this would come from monitoring storage
            var performanceData = new
            {
                TimeRange = $"Last {hours} hours",
                OverallStats = stats,
                Metrics = new
                {
                    AverageResponseTime = "45ms",
                    CacheHitRatio = stats?.HitRatio ?? 0.0,
                    TotalRequests = stats?.TotalCommands ?? 0,
                    CacheHits = stats?.HitCount ?? 0,
                    CacheMisses = stats?.MissCount ?? 0,
                    PopularEndpoints = new[]
                    {
                        new { Endpoint = "/api/blog", Hits = 1250, HitRatio = 0.89 },
                        new { Endpoint = "/api/category", Hits = 856, HitRatio = 0.95 },
                        new { Endpoint = "/api/tag", Hits = 623, HitRatio = 0.82 },
                        new { Endpoint = "/api/home", Hits = 445, HitRatio = 0.91 }
                    }
                },
                Recommendations = new[]
                {
                    "Consider increasing cache duration for /api/tag endpoints",
                    "Monitor memory usage - approaching 75% capacity",
                    "Cache hit ratio for search endpoints could be improved"
                }
            };

            return Ok(performanceData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache performance metrics");
            return StatusCode(500, "Internal server error while retrieving cache performance metrics");
        }
    }
}