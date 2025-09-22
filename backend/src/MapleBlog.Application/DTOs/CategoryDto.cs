using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Category data transfer object for API responses
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// Category unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parent category ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Parent category information (for nested display)
    /// </summary>
    public CategoryDto? Parent { get; set; }

    /// <summary>
    /// Child categories
    /// </summary>
    public List<CategoryDto> Children { get; set; } = new();

    /// <summary>
    /// Hierarchy path
    /// </summary>
    public string? TreePath { get; set; }

    /// <summary>
    /// Hierarchy level (0 = root)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether category is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether category is visible (for backward compatibility)
    /// </summary>
    public bool IsVisible => IsActive;

    /// <summary>
    /// Number of posts in this category
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// Category appearance
    /// </summary>
    public CategoryAppearanceDto Appearance { get; set; } = new();

    /// <summary>
    /// SEO metadata
    /// </summary>
    public CategorySeoDto Seo { get; set; } = new();

    /// <summary>
    /// Creation date and time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date and time
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Category appearance settings
/// </summary>
public class CategoryAppearanceDto
{
    /// <summary>
    /// Category color (hex code)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Category icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Cover image URL
    /// </summary>
    public string? CoverImageUrl { get; set; }
}

/// <summary>
/// Category SEO metadata
/// </summary>
public class CategorySeoDto
{
    /// <summary>
    /// SEO title
    /// </summary>
    public string? MetaTitle { get; set; }

    /// <summary>
    /// SEO description
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// SEO keywords
    /// </summary>
    public string? MetaKeywords { get; set; }
}

/// <summary>
/// Category list item DTO for efficient listing
/// </summary>
public class CategoryListDto
{
    /// <summary>
    /// Category unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parent category ID
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Hierarchy level (0 = root)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether category is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether category is visible (for backward compatibility)
    /// </summary>
    public bool IsVisible => IsActive;

    /// <summary>
    /// Number of posts in this category
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// Category appearance
    /// </summary>
    public CategoryAppearanceDto Appearance { get; set; } = new();

    /// <summary>
    /// Creation date and time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date and time
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Create category request DTO
/// </summary>
public class CreateCategoryRequest
{
    /// <summary>
    /// Category name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (optional, will be generated from name if not provided)
    /// </summary>
    [StringLength(100, ErrorMessage = "Slug cannot exceed 100 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Category description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Parent category ID (optional)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether category is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Category color (hex code)
    /// </summary>
    [StringLength(7, ErrorMessage = "Color must be a valid hex code")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code (e.g., #FF5733)")]
    public string? Color { get; set; }

    /// <summary>
    /// Category icon
    /// </summary>
    [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
    public string? Icon { get; set; }

    /// <summary>
    /// Cover image URL
    /// </summary>
    [StringLength(500, ErrorMessage = "Cover image URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid cover image URL format")]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// SEO title
    /// </summary>
    [StringLength(200, ErrorMessage = "Meta title cannot exceed 200 characters")]
    public string? MetaTitle { get; set; }

    /// <summary>
    /// SEO description
    /// </summary>
    [StringLength(500, ErrorMessage = "Meta description cannot exceed 500 characters")]
    public string? MetaDescription { get; set; }

    /// <summary>
    /// SEO keywords
    /// </summary>
    [StringLength(500, ErrorMessage = "Meta keywords cannot exceed 500 characters")]
    public string? MetaKeywords { get; set; }
}

/// <summary>
/// Update category request DTO
/// </summary>
public class UpdateCategoryRequest
{
    /// <summary>
    /// Category name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    [StringLength(100, ErrorMessage = "Slug cannot exceed 100 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Category description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Parent category ID (optional)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether category is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Category color (hex code)
    /// </summary>
    [StringLength(7, ErrorMessage = "Color must be a valid hex code")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code (e.g., #FF5733)")]
    public string? Color { get; set; }

    /// <summary>
    /// Category icon
    /// </summary>
    [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
    public string? Icon { get; set; }

    /// <summary>
    /// Cover image URL
    /// </summary>
    [StringLength(500, ErrorMessage = "Cover image URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid cover image URL format")]
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// SEO title
    /// </summary>
    [StringLength(200, ErrorMessage = "Meta title cannot exceed 200 characters")]
    public string? MetaTitle { get; set; }

    /// <summary>
    /// SEO description
    /// </summary>
    [StringLength(500, ErrorMessage = "Meta description cannot exceed 500 characters")]
    public string? MetaDescription { get; set; }

    /// <summary>
    /// SEO keywords
    /// </summary>
    [StringLength(500, ErrorMessage = "Meta keywords cannot exceed 500 characters")]
    public string? MetaKeywords { get; set; }
}

/// <summary>
/// Category tree DTO for hierarchical display
/// </summary>
public class CategoryTreeDto
{
    /// <summary>
    /// Category unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hierarchy level (0 = root)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether category is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether category is visible (for backward compatibility)
    /// </summary>
    public bool IsVisible => IsActive;

    /// <summary>
    /// Number of posts in this category
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// Category appearance
    /// </summary>
    public CategoryAppearanceDto Appearance { get; set; } = new();

    /// <summary>
    /// Child categories
    /// </summary>
    public List<CategoryTreeDto> Children { get; set; } = new();
}

/// <summary>
/// Category query/filter parameters DTO
/// </summary>
public class CategoryQueryDto
{
    /// <summary>
    /// Search query
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by parent ID (null for root categories)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Include inactive categories
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// Include empty categories (with zero posts)
    /// </summary>
    public bool IncludeEmpty { get; set; } = true;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "DisplayOrder";

    /// <summary>
    /// Sort direction
    /// </summary>
    public string SortOrder { get; set; } = "ASC";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paginated category list response DTO
/// </summary>
public class CategoryListResponse
{
    /// <summary>
    /// Category items
    /// </summary>
    public List<CategoryListDto> Items { get; set; } = new();

    /// <summary>
    /// Total item count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Total page count
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious { get; set; }
}

/// <summary>
/// Move category request DTO
/// </summary>
public class MoveCategoryRequest
{
    /// <summary>
    /// New parent category ID (null to move to root level)
    /// </summary>
    public Guid? NewParentId { get; set; }

    /// <summary>
    /// New display order
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Category order DTO for reordering operations
/// </summary>
public class CategoryOrderDto
{
    /// <summary>
    /// Category ID
    /// </summary>
    [Required]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// New display order
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Post migration request DTO for moving posts between categories
/// </summary>
public class PostMigrationRequest
{
    /// <summary>
    /// Source category ID (the category being deleted or merged)
    /// </summary>
    [Required]
    public Guid FromCategoryId { get; set; }

    /// <summary>
    /// Target category ID (null for uncategorized posts)
    /// </summary>
    public Guid? ToCategoryId { get; set; }

    /// <summary>
    /// Migration strategy
    /// </summary>
    [Required]
    public PostMigrationStrategy Strategy { get; set; } = PostMigrationStrategy.MoveToParent;

    /// <summary>
    /// Whether to update SEO redirects for affected posts
    /// </summary>
    public bool UpdateSeoRedirects { get; set; } = true;

    /// <summary>
    /// Whether to preserve post creation timestamps
    /// </summary>
    public bool PreserveTimestamps { get; set; } = true;

    /// <summary>
    /// Additional notes for the migration operation
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Post migration strategy enumeration
/// </summary>
public enum PostMigrationStrategy
{
    /// <summary>
    /// Move posts to parent category
    /// </summary>
    MoveToParent = 0,

    /// <summary>
    /// Move posts to specified target category
    /// </summary>
    MoveToTarget = 1,

    /// <summary>
    /// Move posts to default uncategorized category
    /// </summary>
    MoveToUncategorized = 2,

    /// <summary>
    /// Delete posts along with category (dangerous)
    /// </summary>
    DeletePosts = 3
}

/// <summary>
/// Post migration result DTO
/// </summary>
public class PostMigrationResult
{
    /// <summary>
    /// Whether the migration was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of posts successfully migrated
    /// </summary>
    public int PostsMigrated { get; set; }

    /// <summary>
    /// Number of posts that failed to migrate
    /// </summary>
    public int PostsFailed { get; set; }

    /// <summary>
    /// Total number of posts processed
    /// </summary>
    public int TotalPosts { get; set; }

    /// <summary>
    /// Source category information
    /// </summary>
    public CategoryBasicInfo? SourceCategory { get; set; }

    /// <summary>
    /// Target category information
    /// </summary>
    public CategoryBasicInfo? TargetCategory { get; set; }

    /// <summary>
    /// Migration operation message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of error messages for failed migrations
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Migration start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Migration completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Migration duration in milliseconds
    /// </summary>
    public long DurationMs => CompletedAt.HasValue ?
        (long)(CompletedAt.Value - StartedAt).TotalMilliseconds : 0;

    /// <summary>
    /// Creates a successful migration result
    /// </summary>
    public static PostMigrationResult CreateSuccess(int migrated, CategoryBasicInfo? source, CategoryBasicInfo? target, string? message = null)
    {
        return new PostMigrationResult
        {
            Success = true,
            PostsMigrated = migrated,
            TotalPosts = migrated,
            SourceCategory = source,
            TargetCategory = target,
            Message = message ?? $"Successfully migrated {migrated} posts",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed migration result
    /// </summary>
    public static PostMigrationResult CreateFailure(string errorMessage, CategoryBasicInfo? source = null)
    {
        return new PostMigrationResult
        {
            Success = false,
            SourceCategory = source,
            Message = errorMessage,
            Errors = { errorMessage },
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Category merge request DTO
/// </summary>
public class CategoryMergeRequest
{
    /// <summary>
    /// Source category IDs to merge from
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one source category is required")]
    public List<Guid> SourceCategoryIds { get; set; } = new();

    /// <summary>
    /// Target category ID to merge into
    /// </summary>
    [Required]
    public Guid TargetCategoryId { get; set; }

    /// <summary>
    /// Whether to delete source categories after merge
    /// </summary>
    public bool DeleteSourceCategories { get; set; } = true;

    /// <summary>
    /// How to handle conflicting category properties
    /// </summary>
    public CategoryMergeConflictResolution ConflictResolution { get; set; } = CategoryMergeConflictResolution.KeepTarget;

    /// <summary>
    /// Additional notes for the merge operation
    /// </summary>
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Category merge conflict resolution strategy
/// </summary>
public enum CategoryMergeConflictResolution
{
    /// <summary>
    /// Keep target category properties
    /// </summary>
    KeepTarget = 0,

    /// <summary>
    /// Update target with source properties if source is newer
    /// </summary>
    KeepNewer = 1,

    /// <summary>
    /// Merge descriptions and metadata
    /// </summary>
    MergeMetadata = 2
}

/// <summary>
/// Category merge result DTO
/// </summary>
public class CategoryMergeResult
{
    /// <summary>
    /// Whether the merge was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of categories merged
    /// </summary>
    public int CategoriesMerged { get; set; }

    /// <summary>
    /// Number of posts migrated during merge
    /// </summary>
    public int PostsMigrated { get; set; }

    /// <summary>
    /// Target category information
    /// </summary>
    public CategoryBasicInfo? TargetCategory { get; set; }

    /// <summary>
    /// Source categories that were merged
    /// </summary>
    public List<CategoryBasicInfo> SourceCategories { get; set; } = new();

    /// <summary>
    /// Operation message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Merge operation start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Merge operation completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Creates a successful merge result
    /// </summary>
    public static CategoryMergeResult CreateSuccess(int categoriesMerged, int postsMigrated, CategoryBasicInfo target, List<CategoryBasicInfo> sources)
    {
        return new CategoryMergeResult
        {
            Success = true,
            CategoriesMerged = categoriesMerged,
            PostsMigrated = postsMigrated,
            TargetCategory = target,
            SourceCategories = sources,
            Message = $"Successfully merged {categoriesMerged} categories and migrated {postsMigrated} posts",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed merge result
    /// </summary>
    public static CategoryMergeResult CreateFailure(string errorMessage)
    {
        return new CategoryMergeResult
        {
            Success = false,
            Message = errorMessage,
            Errors = { errorMessage },
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Enhanced delete category request with migration options
/// </summary>
public class DeleteCategoryWithMigrationRequest
{
    /// <summary>
    /// Category ID to delete
    /// </summary>
    [Required]
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Post migration options
    /// </summary>
    public PostMigrationRequest? PostMigration { get; set; }

    /// <summary>
    /// Whether to force delete even if category has children
    /// </summary>
    public bool ForceDelete { get; set; } = false;

    /// <summary>
    /// How to handle child categories
    /// </summary>
    public ChildCategoryHandling ChildHandling { get; set; } = ChildCategoryHandling.PreventDeletion;

    /// <summary>
    /// Additional confirmation token for destructive operations
    /// </summary>
    public string? ConfirmationToken { get; set; }
}

/// <summary>
/// Child category handling strategy for category deletion
/// </summary>
public enum ChildCategoryHandling
{
    /// <summary>
    /// Prevent deletion if category has children
    /// </summary>
    PreventDeletion = 0,

    /// <summary>
    /// Move child categories to parent of deleted category
    /// </summary>
    MoveToParent = 1,

    /// <summary>
    /// Move child categories to specified target category
    /// </summary>
    MoveToTarget = 2,

    /// <summary>
    /// Delete child categories recursively (dangerous)
    /// </summary>
    DeleteRecursively = 3
}

/// <summary>
/// Category deletion validation result
/// </summary>
public class CategoryDeletionValidation
{
    /// <summary>
    /// Whether the category can be safely deleted
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Number of posts that need migration
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// Number of child categories
    /// </summary>
    public int ChildCategoryCount { get; set; }

    /// <summary>
    /// Number of descendant categories (all levels)
    /// </summary>
    public int DescendantCategoryCount { get; set; }

    /// <summary>
    /// List of blocking issues preventing deletion
    /// </summary>
    public List<string> BlockingIssues { get; set; } = new();

    /// <summary>
    /// List of warnings about the deletion
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Suggested migration strategies
    /// </summary>
    public List<CategoryBasicInfo> SuggestedTargetCategories { get; set; } = new();

    /// <summary>
    /// Parent category (if available for migration)
    /// </summary>
    public CategoryBasicInfo? ParentCategory { get; set; }
}