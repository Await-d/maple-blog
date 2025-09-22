using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;
using System.Security.Claims;

namespace MapleBlog.API.Controllers;

/// <summary>
/// Blog posts management API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BlogController : ControllerBase
{
    private readonly IBlogService _blogService;
    private readonly ILogger<BlogController> _logger;

    public BlogController(IBlogService blogService, ILogger<BlogController> logger)
    {
        _blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all published posts with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="categoryId">Filter by category ID</param>
    /// <param name="tagId">Filter by tag ID</param>
    /// <param name="searchTerm">Search term for title and content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of published posts</returns>
    [HttpGet]
    [ResponseCache(Duration = 900, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(PagedResultDto<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<PostDto>>> GetPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? tagId = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            var result = await _blogService.GetPublishedPostsAsync(
                pageNumber, pageSize, categoryId, tagId, searchTerm, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all posts (including drafts) - Admin/Author only
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="status">Filter by post status</param>
    /// <param name="authorId">Filter by author ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of all posts</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(typeof(PagedResultDto<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<PostDto>>> GetAllPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] PostStatus? status = null,
        [FromQuery] Guid? authorId = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Non-admin users can only see their own posts
            if (!isAdmin && authorId.HasValue && authorId.Value != currentUserId)
            {
                return Forbid();
            }

            if (!isAdmin && !authorId.HasValue)
            {
                authorId = currentUserId;
            }

            var result = await _blogService.GetAllPostsAsync(
                pageNumber, pageSize, status, authorId, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific post by ID
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Post details</returns>
    [HttpGet("{id:guid}")]
    [ResponseCache(Duration = 1800, VaryByHeader = "Accept,Accept-Language", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetPost(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _blogService.GetPostByIdAsync(id, cancellationToken);

            if (post == null)
                return NotFound();

            // Check if user can access this post
            if (post.Status != PostStatus.Published && !CanAccessPost(post))
            {
                return NotFound(); // Don't reveal existence of unpublished posts
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific post by slug
    /// </summary>
    /// <param name="slug">Post slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Post details</returns>
    [HttpGet("slug/{slug}")]
    [ResponseCache(Duration = 1800, VaryByHeader = "Accept,Accept-Language", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetPostBySlug(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _blogService.GetPostBySlugAsync(slug, cancellationToken);

            if (post == null)
                return NotFound();

            // Check if user can access this post
            if (post.Status != PostStatus.Published && !CanAccessPost(post))
            {
                return NotFound(); // Don't reveal existence of unpublished posts
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post by slug {Slug}", slug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new post
    /// </summary>
    /// <param name="createDto">Post creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created post</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PostDto>> CreatePost(
        [FromBody] CreatePostDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var post = await _blogService.CreatePostAsync(currentUserId, createDto, cancellationToken);

            return CreatedAtAction(
                nameof(GetPost),
                new { id = post.Id },
                post);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid post creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="updateDto">Post update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated post</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> UpdatePost(
        Guid id,
        [FromBody] UpdatePostDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Check if post exists and user has permission
            var existingPost = await _blogService.GetPostByIdAsync(id, cancellationToken);
            if (existingPost == null)
                return NotFound();

            if (!isAdmin && existingPost.AuthorId != currentUserId)
                return Forbid();

            var post = await _blogService.UpdatePostAsync(id, updateDto, cancellationToken);

            return Ok(post);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid post update request for {PostId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Check if post exists and user has permission
            var existingPost = await _blogService.GetPostByIdAsync(id, cancellationToken);
            if (existingPost == null)
                return NotFound();

            if (!isAdmin && existingPost.AuthorId != currentUserId)
                return Forbid();

            await _blogService.DeletePostAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Publish a post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated post</returns>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> PublishPost(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Check if post exists and user has permission
            var existingPost = await _blogService.GetPostByIdAsync(id, cancellationToken);
            if (existingPost == null)
                return NotFound();

            if (!isAdmin && existingPost.AuthorId != currentUserId)
                return Forbid();

            var post = await _blogService.PublishPostAsync(id, cancellationToken);

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Unpublish a post (set to draft)
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated post</returns>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Roles = "Admin,Author")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> UnpublishPost(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Check if post exists and user has permission
            var existingPost = await _blogService.GetPostByIdAsync(id, cancellationToken);
            if (existingPost == null)
                return NotFound();

            if (!isAdmin && existingPost.AuthorId != currentUserId)
                return Forbid();

            var post = await _blogService.UnpublishPostAsync(id, cancellationToken);

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing post {PostId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get posts by category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of posts in category</returns>
    [HttpGet("category/{categoryId:guid}")]
    [ResponseCache(Duration = 1200, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(PagedResultDto<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<PostDto>>> GetPostsByCategory(
        Guid categoryId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            var result = await _blogService.GetPostsByCategoryAsync(
                categoryId, pageNumber, pageSize, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for category {CategoryId}", categoryId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get posts by tag
    /// </summary>
    /// <param name="tagId">Tag ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of posts with tag</returns>
    [HttpGet("tag/{tagId:guid}")]
    [ResponseCache(Duration = 1200, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(PagedResultDto<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<PostDto>>> GetPostsByTag(
        Guid tagId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            var result = await _blogService.GetPostsByTagAsync(
                tagId, pageNumber, pageSize, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for tag {TagId}", tagId);
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool CanAccessPost(PostDto post)
    {
        if (User.IsInRole("Admin"))
            return true;

        var currentUserId = GetCurrentUserId();
        return post.AuthorId == currentUserId;
    }
}