using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Controllers
{
    /// <summary>
    /// Home controller for homepage data aggregation and caching
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("DefaultPolicy")]
    public class HomeController : ControllerBase
    {
        private readonly IHomeService _homeService;
        private readonly IStatsService _statsService;
        private readonly IRecommendationService _recommendationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IHomeService homeService,
            IStatsService statsService,
            IRecommendationService recommendationService,
            ILogger<HomeController> logger)
        {
            _homeService = homeService ?? throw new ArgumentNullException(nameof(homeService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets comprehensive home page data with optimal caching
        /// </summary>
        /// <returns>Home page data aggregation</returns>
        [HttpGet]
        [ResponseCache(Duration = 300, VaryByHeader = "Authorization")] // 5 minutes cache
        [ProducesResponseType(typeof(HomePageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<HomePageDto>> GetHomePageData(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Home page data requested");

                // Check if user is authenticated for personalization
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    _logger.LogDebug("Returning personalized home page data for user {UserId}", userId.Value);
                    var personalizedData = await _homeService.GetPersonalizedHomePageDataAsync(userId.Value, cancellationToken);

                    // Set cache headers for personalized content
                    Response.Headers.Add("Cache-Control", "private, max-age=600"); // 10 minutes for personalized

                    return Ok(personalizedData);
                }
                else
                {
                    _logger.LogDebug("Returning anonymous home page data");
                    var anonymousData = await _homeService.GetHomePageDataAsync(cancellationToken);

                    // Set cache headers for anonymous content
                    Response.Headers.Add("Cache-Control", "public, max-age=900"); // 15 minutes for anonymous

                    return Ok(anonymousData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving home page data");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving home page data" });
            }
        }

        /// <summary>
        /// Gets featured posts for hero section
        /// </summary>
        /// <param name="count">Number of featured posts to return (default: 5, max: 10)</param>
        /// <returns>Featured posts</returns>
        [HttpGet("featured")]
        [ResponseCache(Duration = 3600)] // 1 hour cache
        [ProducesResponseType(typeof(IReadOnlyList<PostSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<PostSummaryDto>>> GetFeaturedPosts(
            [FromQuery] int count = 5,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (count <= 0 || count > 10)
                {
                    return BadRequest(new { message = "Count must be between 1 and 10" });
                }

                var featuredPosts = await _homeService.GetFeaturedPostsAsync(count, cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=3600");

                return Ok(featuredPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured posts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving featured posts" });
            }
        }

        /// <summary>
        /// Gets latest published posts
        /// </summary>
        /// <param name="count">Number of latest posts to return (default: 10, max: 20)</param>
        /// <returns>Latest posts</returns>
        [HttpGet("latest")]
        [ResponseCache(Duration = 300)] // 5 minutes cache
        [ProducesResponseType(typeof(IReadOnlyList<PostSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<PostSummaryDto>>> GetLatestPosts(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    return BadRequest(new { message = "Count must be between 1 and 20" });
                }

                var latestPosts = await _homeService.GetLatestPostsAsync(count, cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=300");

                return Ok(latestPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest posts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving latest posts" });
            }
        }

        /// <summary>
        /// Gets popular posts by views
        /// </summary>
        /// <param name="count">Number of popular posts to return (default: 10, max: 20)</param>
        /// <param name="daysBack">Number of days to look back (default: 30, max: 365)</param>
        /// <returns>Popular posts</returns>
        [HttpGet("popular")]
        [ResponseCache(Duration = 1800)] // 30 minutes cache
        [ProducesResponseType(typeof(IReadOnlyList<PostSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<PostSummaryDto>>> GetPopularPosts(
            [FromQuery] int count = 10,
            [FromQuery] int daysBack = 30,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    return BadRequest(new { message = "Count must be between 1 and 20" });
                }

                if (daysBack <= 0 || daysBack > 365)
                {
                    return BadRequest(new { message = "DaysBack must be between 1 and 365" });
                }

                var popularPosts = await _homeService.GetPopularPostsAsync(count, daysBack, cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=1800");

                return Ok(popularPosts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving popular posts");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving popular posts" });
            }
        }

        /// <summary>
        /// Gets personalized recommendations for authenticated users
        /// </summary>
        /// <param name="count">Number of recommendations to return (default: 10, max: 20)</param>
        /// <returns>Personalized recommendations</returns>
        [HttpGet("recommendations")]
        [Authorize]
        [ResponseCache(Duration = 600, VaryByHeader = "Authorization")] // 10 minutes cache
        [ProducesResponseType(typeof(IReadOnlyList<PostSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IReadOnlyList<PostSummaryDto>>> GetPersonalizedRecommendations(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    return BadRequest(new { message = "Count must be between 1 and 20" });
                }

                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User authentication required for personalized recommendations" });
                }

                var recommendations = await _recommendationService.GetPersonalizedRecommendationsAsync(
                    userId.Value, count, cancellationToken);

                // Set cache headers for personalized content
                Response.Headers.Add("Cache-Control", "private, max-age=600");

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personalized recommendations");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving recommendations" });
            }
        }

        /// <summary>
        /// Gets website statistics
        /// </summary>
        /// <returns>Site statistics</returns>
        [HttpGet("stats")]
        [ResponseCache(Duration = 1800)] // 30 minutes cache
        [ProducesResponseType(typeof(SiteStatsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<SiteStatsDto>> GetSiteStats(CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _statsService.GetSiteStatsAsync(cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=1800");

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site statistics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving site statistics" });
            }
        }

        /// <summary>
        /// Gets category statistics
        /// </summary>
        /// <param name="includeEmpty">Whether to include categories with no posts</param>
        /// <returns>Category statistics</returns>
        [HttpGet("categories")]
        [ResponseCache(Duration = 3600)] // 1 hour cache
        [ProducesResponseType(typeof(IReadOnlyList<CategorySummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<CategorySummaryDto>>> GetCategoryStats(
            [FromQuery] bool includeEmpty = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = await _statsService.GetCategoryStatsAsync(includeEmpty, cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=3600");

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category statistics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving category statistics" });
            }
        }

        /// <summary>
        /// Gets tag statistics for tag cloud
        /// </summary>
        /// <param name="count">Maximum number of tags to return (default: 50, max: 100)</param>
        /// <param name="minUsage">Minimum usage count to include (default: 1)</param>
        /// <returns>Tag statistics</returns>
        [HttpGet("tags")]
        [ResponseCache(Duration = 1800)] // 30 minutes cache
        [ProducesResponseType(typeof(IReadOnlyList<TagSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<TagSummaryDto>>> GetTagStats(
            [FromQuery] int count = 50,
            [FromQuery] int minUsage = 1,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (count <= 0 || count > 100)
                {
                    return BadRequest(new { message = "Count must be between 1 and 100" });
                }

                if (minUsage < 1)
                {
                    return BadRequest(new { message = "MinUsage must be at least 1" });
                }

                var tags = await _statsService.GetTagStatsAsync(count, minUsage, cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=1800");

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag statistics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving tag statistics" });
            }
        }

        /// <summary>
        /// Gets active authors with recent activity
        /// </summary>
        /// <param name="count">Number of authors to return (default: 10, max: 20)</param>
        /// <returns>Active authors</returns>
        [HttpGet("authors")]
        [ResponseCache(Duration = 900)] // 15 minutes cache
        [ProducesResponseType(typeof(IReadOnlyList<AuthorSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<AuthorSummaryDto>>> GetActiveAuthors(
            [FromQuery] int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (count <= 0 || count > 20)
                {
                    return BadRequest(new { message = "Count must be between 1 and 20" });
                }

                var authors = await _statsService.GetActiveAuthorsAsync(count, cancellationToken);

                // Set cache headers
                Response.Headers.Add("Cache-Control", "public, max-age=900");

                return Ok(authors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active authors");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving active authors" });
            }
        }

        /// <summary>
        /// Records user interaction for analytics and recommendations
        /// </summary>
        /// <param name="request">Interaction data</param>
        /// <returns>Success response</returns>
        [HttpPost("interaction")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RecordInteraction(
            [FromBody] RecordInteractionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request body is required" });
                }

                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "User authentication required" });
                }

                await _recommendationService.RecordUserInteractionAsync(
                    userId.Value,
                    request.PostId,
                    request.InteractionType,
                    request.Duration,
                    cancellationToken);

                return Ok(new { message = "Interaction recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording user interaction");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while recording interaction" });
            }
        }

        /// <summary>
        /// Refreshes home page cache (admin only)
        /// </summary>
        /// <returns>Success response</returns>
        [HttpPost("refresh-cache")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RefreshCache(CancellationToken cancellationToken = default)
        {
            try
            {
                await _homeService.RefreshHomePageCacheAsync(cancellationToken);
                return Ok(new { message = "Home page cache refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing home page cache");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while refreshing cache" });
            }
        }

        #region Private Methods

        /// <summary>
        /// Gets the current user ID from JWT claims
        /// </summary>
        /// <returns>User ID if authenticated, null otherwise</returns>
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// Request model for recording user interactions
    /// </summary>
    public class RecordInteractionRequest
    {
        /// <summary>
        /// Post ID that was interacted with
        /// </summary>
        public Guid PostId { get; set; }

        /// <summary>
        /// Type of interaction (view, like, comment, share)
        /// </summary>
        public string InteractionType { get; set; } = "view";

        /// <summary>
        /// Duration of interaction (for views)
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }
}