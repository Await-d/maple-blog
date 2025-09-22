using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Tag data transfer object for API responses
/// </summary>
public class TagDto
{
    /// <summary>
    /// Tag unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tag description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether tag is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether tag is visible (for backward compatibility)
    /// </summary>
    public bool IsVisible => IsActive;

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
/// Tag list item DTO for efficient listing
/// </summary>
public class TagListDto
{
    /// <summary>
    /// Tag unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tag description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether tag is active
    /// </summary>
    public bool IsActive { get; set; }

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
/// Create tag request DTO
/// </summary>
public class CreateTagRequest
{
    /// <summary>
    /// Tag name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (optional, will be generated from name if not provided)
    /// </summary>
    [StringLength(50, ErrorMessage = "Slug cannot exceed 50 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Tag description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    [StringLength(7, ErrorMessage = "Color must be a valid hex code")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code (e.g., #FF5733)")]
    public string? Color { get; set; }

    /// <summary>
    /// Whether tag is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update tag request DTO
/// </summary>
public class UpdateTagRequest
{
    /// <summary>
    /// Tag name
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    [StringLength(50, ErrorMessage = "Slug cannot exceed 50 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Tag description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    [StringLength(7, ErrorMessage = "Color must be a valid hex code")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code (e.g., #FF5733)")]
    public string? Color { get; set; }

    /// <summary>
    /// Whether tag is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Tag query/filter parameters DTO
/// </summary>
public class TagQueryDto
{
    /// <summary>
    /// Search query
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Include inactive tags
    /// </summary>
    public bool IncludeInactive { get; set; } = false;

    /// <summary>
    /// Include unused tags (with zero usage count)
    /// </summary>
    public bool IncludeUnused { get; set; } = true;

    /// <summary>
    /// Minimum usage count filter
    /// </summary>
    public int? MinUsageCount { get; set; }

    /// <summary>
    /// Maximum usage count filter
    /// </summary>
    public int? MaxUsageCount { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "Name";

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
/// Paginated tag list response DTO
/// </summary>
public class TagListResponse
{
    /// <summary>
    /// Tag items
    /// </summary>
    public List<TagListDto> Items { get; set; } = new();

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
/// Tag cloud item DTO for tag cloud visualization
/// </summary>
public class TagCloudDto
{
    /// <summary>
    /// Tag unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Relative weight for cloud visualization (0.1 to 1.0)
    /// </summary>
    public double Weight { get; set; }
}

/// <summary>
/// Tag suggestion DTO for intelligent tag suggestions
/// </summary>
public class TagSuggestionDto
{
    /// <summary>
    /// Tag unique identifier (null for new suggestions)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Suggested tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Relevance score (0.0 to 1.0)
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Usage count (0 for new tags)
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether this is an existing tag
    /// </summary>
    public bool IsExisting { get; set; }
}

/// <summary>
/// Bulk tag operations request DTO
/// </summary>
public class BulkTagOperationRequest
{
    /// <summary>
    /// Tag IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one tag ID is required")]
    [MinLength(1, ErrorMessage = "At least one tag ID is required")]
    public List<Guid> TagIds { get; set; } = new();

    /// <summary>
    /// Operation to perform
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    [AllowedValues("activate", "deactivate", "delete", "merge")]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Target tag ID for merge operation
    /// </summary>
    public Guid? TargetTagId { get; set; }
}

/// <summary>
/// Tag merge request DTO
/// </summary>
public class MergeTagsRequest
{
    /// <summary>
    /// Source tag IDs to merge
    /// </summary>
    [Required(ErrorMessage = "At least one source tag ID is required")]
    [MinLength(1, ErrorMessage = "At least one source tag ID is required")]
    public List<Guid> SourceTagIds { get; set; } = new();

    /// <summary>
    /// Target tag ID to merge into
    /// </summary>
    [Required(ErrorMessage = "Target tag ID is required")]
    public Guid TargetTagId { get; set; }

    /// <summary>
    /// Whether to delete source tags after merge
    /// </summary>
    public bool DeleteSourceTags { get; set; } = true;
}

/// <summary>
/// Tag statistics DTO
/// </summary>
public class TagStatsDto
{
    /// <summary>
    /// Total number of tags
    /// </summary>
    public int TotalTags { get; set; }

    /// <summary>
    /// Number of active tags
    /// </summary>
    public int ActiveTags { get; set; }

    /// <summary>
    /// Number of used tags (with posts)
    /// </summary>
    public int UsedTags { get; set; }

    /// <summary>
    /// Number of unused tags (without posts)
    /// </summary>
    public int UnusedTags { get; set; }

    /// <summary>
    /// Average usage per tag
    /// </summary>
    public double AverageUsage { get; set; }

    /// <summary>
    /// Most used tags (top 10)
    /// </summary>
    public List<TagCloudDto> MostUsedTags { get; set; } = new();

    /// <summary>
    /// Recently created tags (last 30 days)
    /// </summary>
    public List<TagListDto> RecentlyCreated { get; set; } = new();
}

/// <summary>
/// Tag auto-complete suggestion DTO
/// </summary>
public class TagAutoCompleteDto
{
    /// <summary>
    /// Tag unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }
}

/// <summary>
/// Custom validation attribute for allowed values
/// </summary>
public class AllowedValuesAttribute : ValidationAttribute
{
    private readonly string[] _allowedValues;

    public AllowedValuesAttribute(params string[] allowedValues)
    {
        _allowedValues = allowedValues;
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return false;
        return _allowedValues.Contains(value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} must be one of the following values: {string.Join(", ", _allowedValues)}.";
    }
}