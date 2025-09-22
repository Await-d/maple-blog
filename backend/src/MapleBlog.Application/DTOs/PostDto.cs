using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Post data transfer object for API responses
/// </summary>
public class PostDto
{
    /// <summary>
    /// Post unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Post title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Post content (Markdown)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content type (markdown, html, richtext)
    /// </summary>
    public string ContentType { get; set; } = "markdown";

    /// <summary>
    /// Post status
    /// </summary>
    public PostStatus Status { get; set; }

    /// <summary>
    /// Publication date and time
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Author ID
    /// </summary>
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Author information
    /// </summary>
    public PostAuthorDto? Author { get; set; }

    /// <summary>
    /// Category information
    /// </summary>
    public CategoryDto? Category { get; set; }

    /// <summary>
    /// Associated tags
    /// </summary>
    public List<TagDto> Tags { get; set; } = new();

    /// <summary>
    /// Post statistics
    /// </summary>
    public PostStatsDto Stats { get; set; } = new();

    /// <summary>
    /// Post settings
    /// </summary>
    public PostSettingsDto Settings { get; set; } = new();

    /// <summary>
    /// SEO metadata
    /// </summary>
    public PostSeoDto Seo { get; set; } = new();

    /// <summary>
    /// Content metadata
    /// </summary>
    public PostContentDto ContentInfo { get; set; } = new();

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
/// Post author information for nested display
/// </summary>
public class PostAuthorDto
{
    /// <summary>
    /// Author unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Author username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Author display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Author avatar URL
    /// </summary>
    public string? Avatar { get; set; }
}

/// <summary>
/// Post statistics data
/// </summary>
public class PostStatsDto
{
    /// <summary>
    /// View count
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Like count
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// Comment count
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Share count
    /// </summary>
    public int ShareCount { get; set; }
}

/// <summary>
/// Post settings data
/// </summary>
public class PostSettingsDto
{
    /// <summary>
    /// Whether comments are allowed
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether post is featured
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Whether post is sticky
    /// </summary>
    public bool IsSticky { get; set; }
}

/// <summary>
/// Post SEO metadata
/// </summary>
public class PostSeoDto
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

    /// <summary>
    /// Canonical URL
    /// </summary>
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Open Graph title
    /// </summary>
    public string? OgTitle { get; set; }

    /// <summary>
    /// Open Graph description
    /// </summary>
    public string? OgDescription { get; set; }

    /// <summary>
    /// Open Graph image URL
    /// </summary>
    public string? OgImageUrl { get; set; }
}

/// <summary>
/// Post content metadata
/// </summary>
public class PostContentDto
{
    /// <summary>
    /// Estimated reading time in minutes
    /// </summary>
    public int? ReadingTime { get; set; }

    /// <summary>
    /// Word count
    /// </summary>
    public int? WordCount { get; set; }

    /// <summary>
    /// Content language
    /// </summary>
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// Post list item DTO for efficient listing
/// </summary>
public class PostListDto
{
    /// <summary>
    /// Post unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Post title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Post status
    /// </summary>
    public PostStatus Status { get; set; }

    /// <summary>
    /// Publication date and time
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Author information
    /// </summary>
    public PostAuthorDto? Author { get; set; }

    /// <summary>
    /// Category information
    /// </summary>
    public CategoryDto? Category { get; set; }

    /// <summary>
    /// Associated tags
    /// </summary>
    public List<TagDto> Tags { get; set; } = new();

    /// <summary>
    /// Tag names (for compatibility)
    /// </summary>
    public List<string> TagNames { get; set; } = new List<string>();

    /// <summary>
    /// Post statistics
    /// </summary>
    public PostStatsDto Stats { get; set; } = new();

    /// <summary>
    /// Content metadata
    /// </summary>
    public PostContentDto ContentInfo { get; set; } = new();

    /// <summary>
    /// Whether post is featured
    /// </summary>
    public bool IsFeatured { get; set; }

    /// <summary>
    /// Whether post is sticky
    /// </summary>
    public bool IsSticky { get; set; }

    /// <summary>
    /// Creation date and time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date and time
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Additional properties needed by SimpleSearchService
    /// <summary>
    /// Author ID
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// Author name
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// View count
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Comment count
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Whether post is published
    /// </summary>
    public bool IsPublished { get; set; }
}

/// <summary>
/// Create post request DTO
/// </summary>
public class CreatePostRequest
{
    /// <summary>
    /// Post title
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (optional, will be generated from title if not provided)
    /// </summary>
    [StringLength(200, ErrorMessage = "Slug cannot exceed 200 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    [StringLength(1000, ErrorMessage = "Summary cannot exceed 1000 characters")]
    public string? Summary { get; set; }

    /// <summary>
    /// Post content
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    [StringLength(20, ErrorMessage = "Content type cannot exceed 20 characters")]
    public string ContentType { get; set; } = "markdown";

    /// <summary>
    /// Category ID (optional)
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Tag IDs
    /// </summary>
    public List<Guid> TagIds { get; set; } = new();

    /// <summary>
    /// Whether comments are allowed
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether post is featured
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Whether post is sticky
    /// </summary>
    public bool IsSticky { get; set; } = false;

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

    /// <summary>
    /// Canonical URL
    /// </summary>
    [StringLength(500, ErrorMessage = "Canonical URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid canonical URL format")]
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Open Graph title
    /// </summary>
    [StringLength(200, ErrorMessage = "OG title cannot exceed 200 characters")]
    public string? OgTitle { get; set; }

    /// <summary>
    /// Open Graph description
    /// </summary>
    [StringLength(500, ErrorMessage = "OG description cannot exceed 500 characters")]
    public string? OgDescription { get; set; }

    /// <summary>
    /// Open Graph image URL
    /// </summary>
    [StringLength(500, ErrorMessage = "OG image URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid OG image URL format")]
    public string? OgImageUrl { get; set; }

    /// <summary>
    /// Content language
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// Update post request DTO
/// </summary>
public class UpdatePostRequest
{
    /// <summary>
    /// Post title
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    [StringLength(200, ErrorMessage = "Slug cannot exceed 200 characters")]
    public string? Slug { get; set; }

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    [StringLength(1000, ErrorMessage = "Summary cannot exceed 1000 characters")]
    public string? Summary { get; set; }

    /// <summary>
    /// Post content
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    [StringLength(20, ErrorMessage = "Content type cannot exceed 20 characters")]
    public string ContentType { get; set; } = "markdown";

    /// <summary>
    /// Category ID (optional)
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Tag IDs
    /// </summary>
    public List<Guid> TagIds { get; set; } = new();

    /// <summary>
    /// Whether comments are allowed
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether post is featured
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Whether post is sticky
    /// </summary>
    public bool IsSticky { get; set; } = false;

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

    /// <summary>
    /// Canonical URL
    /// </summary>
    [StringLength(500, ErrorMessage = "Canonical URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid canonical URL format")]
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Open Graph title
    /// </summary>
    [StringLength(200, ErrorMessage = "OG title cannot exceed 200 characters")]
    public string? OgTitle { get; set; }

    /// <summary>
    /// Open Graph description
    /// </summary>
    [StringLength(500, ErrorMessage = "OG description cannot exceed 500 characters")]
    public string? OgDescription { get; set; }

    /// <summary>
    /// Open Graph image URL
    /// </summary>
    [StringLength(500, ErrorMessage = "OG image URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid OG image URL format")]
    public string? OgImageUrl { get; set; }

    /// <summary>
    /// Content language
    /// </summary>
    [StringLength(10, ErrorMessage = "Language code cannot exceed 10 characters")]
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// Post publication request DTO
/// </summary>
public class PublishPostRequest
{
    /// <summary>
    /// Schedule publication for later (optional)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Post query/filter parameters DTO
/// </summary>
public class PostQueryDto
{
    /// <summary>
    /// Search query
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public PostStatus? Status { get; set; }

    /// <summary>
    /// Filter by category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Filter by tag IDs
    /// </summary>
    public List<Guid>? TagIds { get; set; }

    /// <summary>
    /// Filter by author ID
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// Show featured posts only
    /// </summary>
    public bool? IsFeatured { get; set; }

    /// <summary>
    /// Show sticky posts only
    /// </summary>
    public bool? IsSticky { get; set; }

    /// <summary>
    /// Date range start
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Date range end
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction
    /// </summary>
    public string SortOrder { get; set; } = "DESC";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Paginated post list response DTO
/// </summary>
public class PostListResponse
{
    /// <summary>
    /// Post items
    /// </summary>
    public List<PostListDto> Items { get; set; } = new();

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