using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Controllers;

/// <summary>
/// Categories management API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all categories with hierarchical structure
    /// </summary>
    /// <param name="includeHidden">Include hidden categories (admin only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hierarchical list of categories</returns>
    [HttpGet]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(
        [FromQuery] bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Only admins can see hidden categories
            if (includeHidden && !User.IsInRole("Admin"))
            {
                includeHidden = false;
            }

            var categories = await _categoryService.GetCategoriesAsync(includeHidden, cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get root categories (top-level categories)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root categories</returns>
    [HttpGet("root")]
    [ResponseCache(Duration = 3600, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetRootCategories(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryService.GetRootCategoriesAsync(false, cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root categories");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific category by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);

            if (category == null)
                return NotFound();

            // Check if user can access hidden categories
            if (!category.IsVisible && !User.IsInRole("Admin"))
                return NotFound();

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific category by slug
    /// </summary>
    /// <param name="slug">Category slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category details</returns>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategoryBySlug(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryService.GetCategoryBySlugAsync(slug, cancellationToken);

            if (category == null)
                return NotFound();

            // Check if user can access hidden categories
            if (!category.IsVisible && !User.IsInRole("Admin"))
                return NotFound();

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by slug {Slug}", slug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get subcategories of a specific category
    /// </summary>
    /// <param name="parentId">Parent category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subcategories</returns>
    [HttpGet("{parentId:guid}/subcategories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetSubcategories(
        Guid parentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subcategories = await _categoryService.GetSubcategoriesAsync(parentId, cancellationToken);
            return Ok(subcategories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subcategories for {ParentId}", parentId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="createDto">Category creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        [FromBody] CreateCategoryDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var category = await _categoryService.CreateCategoryAsync(createDto, cancellationToken);

            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                category);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid category creation request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="updateDto">Category update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var category = await _categoryService.UpdateCategoryAsync(id, updateDto, cancellationToken);

            if (category == null)
                return NotFound();

            return Ok(category);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid category update request for {CategoryId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="movePostsToCategory">Move posts to this category ID instead of deleting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(
        Guid id,
        [FromQuery] Guid? movePostsToCategory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _categoryService.DeleteCategoryWithPostMoveAsync(id, movePostsToCategory, cancellationToken);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid category deletion request for {CategoryId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Reorder categories
    /// </summary>
    /// <param name="reorderDto">Category reorder data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("reorder")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReorderCategories(
        [FromBody] ReorderCategoriesDto reorderDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _categoryService.ReorderCategoriesAsync(reorderDto, cancellationToken);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid category reorder request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get category statistics
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category statistics</returns>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(CategoryStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryStatsDto>> GetCategoryStats(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _categoryService.GetCategoryStatsAsync(id, cancellationToken);

            if (stats == null)
                return NotFound();

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category stats for {CategoryId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}