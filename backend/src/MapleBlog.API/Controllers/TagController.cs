using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Controllers;

/// <summary>
/// Tags management API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ILogger<TagController> _logger;

    public TagController(ITagService tagService, ILogger<TagController> logger)
    {
        _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all tags with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <param name="searchTerm">Search term for tag names</param>
    /// <param name="sortBy">Sort order (name, usage, created)</param>
    /// <param name="includeHidden">Include hidden tags (admin only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of tags</returns>
    [HttpGet]
    [ResponseCache(Duration = 1800, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(PagedResultDto<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<TagDto>>> GetTags(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string sortBy = "usage",
        [FromQuery] bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            // Only admins can see hidden tags
            if (includeHidden && !User.IsInRole("Admin"))
            {
                includeHidden = false;
            }

            var result = await _tagService.GetTagsAsync(
                pageNumber, pageSize, searchTerm, sortBy, includeHidden, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get popular tags (most used)
    /// </summary>
    /// <param name="limit">Number of tags to return (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of popular tags</returns>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(IEnumerable<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetPopularTags(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 100)
        {
            return BadRequest("Limit must be between 1 and 100");
        }

        try
        {
            var tags = await _tagService.GetPopularTagsAsync(limit, cancellationToken);
            return Ok(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular tags");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get tag suggestions based on search term
    /// </summary>
    /// <param name="term">Search term</param>
    /// <param name="limit">Number of suggestions to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tag suggestions</returns>
    [HttpGet("suggest")]
    [ProducesResponseType(typeof(IEnumerable<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TagDto>>> GetTagSuggestions(
        [FromQuery] string term,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return BadRequest("Search term must be at least 2 characters");
        }

        if (limit < 1 || limit > 50)
        {
            return BadRequest("Limit must be between 1 and 50");
        }

        try
        {
            var suggestions = await _tagService.GetTagSuggestionsAsync(term, limit, cancellationToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag suggestions for term: {Term}", term);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific tag by ID
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetTag(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagService.GetTagByIdAsync(id, cancellationToken);

            if (tag == null)
                return NotFound();

            // Check if user can access hidden tags
            if (!tag.IsVisible && !User.IsInRole("Admin"))
                return NotFound();

            return Ok(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag {TagId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific tag by slug
    /// </summary>
    /// <param name="slug">Tag slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag details</returns>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetTagBySlug(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagService.GetTagBySlugAsync(slug, cancellationToken);

            if (tag == null)
                return NotFound();

            // Check if user can access hidden tags
            if (!tag.IsVisible && !User.IsInRole("Admin"))
                return NotFound();

            return Ok(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by slug {Slug}", slug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific tag by name
    /// </summary>
    /// <param name="name">Tag name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag details</returns>
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetTagByName(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagService.GetTagByNameAsync(name, cancellationToken);

            if (tag == null)
                return NotFound();

            // Check if user can access hidden tags
            if (!tag.IsVisible && !User.IsInRole("Admin"))
                return NotFound();

            return Ok(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by name {Name}", name);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new tag
    /// </summary>
    /// <param name="createDto">Tag creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created tag</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> CreateTag(
        [FromBody] CreateTagDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var tag = await _tagService.CreateTagAsync(createDto, cancellationToken);

            return CreatedAtAction(
                nameof(GetTag),
                new { id = tag.Id },
                tag);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tag creation request");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "Tag already exists");
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="updateDto">Tag update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated tag</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> UpdateTag(
        Guid id,
        [FromBody] UpdateTagDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var tag = await _tagService.UpdateTagAsync(id, updateDto, cancellationToken);

            if (tag == null)
                return NotFound();

            return Ok(tag);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tag update request for {TagId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag {TagId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _tagService.DeleteTagAsync(id, cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag {TagId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Merge tags (combine multiple tags into one)
    /// </summary>
    /// <param name="mergeDto">Tag merge data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged tag</returns>
    [HttpPost("merge")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TagDto>> MergeTags(
        [FromBody] MergeTagsDto mergeDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var mergedTag = await _tagService.MergeTagsAsync(mergeDto, cancellationToken);
            return Ok(mergedTag);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tag merge request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging tags");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get tag statistics
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag statistics</returns>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(TagStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagStatsDto>> GetTagStats(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _tagService.GetTagStatsAsync(id, cancellationToken);

            if (stats == null)
                return NotFound();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag stats for {TagId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all tag statistics for analytics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag analytics data</returns>
    [HttpGet("analytics")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TagAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TagAnalyticsDto>> GetTagAnalytics(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var analytics = await _tagService.GetTagAnalyticsAsync(cancellationToken);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag analytics");
            return StatusCode(500, "Internal server error");
        }
    }
}