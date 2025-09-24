using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Data transfer object for creating a new post
/// </summary>
public class CreatePostDto
{
    /// <summary>
    /// Post title
    /// </summary>
    [Required(ErrorMessage = "Post title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (will be auto-generated if not provided)
    /// </summary>
    [StringLength(200, ErrorMessage = "Slug must not exceed 200 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
    public string? Slug { get; set; }

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    [StringLength(500, ErrorMessage = "Summary must not exceed 500 characters")]
    public string? Summary { get; set; }

    /// <summary>
    /// Post content
    /// </summary>
    [Required(ErrorMessage = "Post content is required")]
    [MinLength(10, ErrorMessage = "Content must be at least 10 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Content type (markdown, html, richtext)
    /// </summary>
    [StringLength(20, ErrorMessage = "Content type must not exceed 20 characters")]
    public string ContentType { get; set; } = "markdown";

    /// <summary>
    /// Post status
    /// </summary>
    public PostStatus Status { get; set; } = PostStatus.Draft;

    /// <summary>
    /// Publication date and time (for scheduled posts)
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Featured image URL
    /// </summary>
    [StringLength(500, ErrorMessage = "Featured image URL must not exceed 500 characters")]
    [Url(ErrorMessage = "Featured image must be a valid URL")]
    public string? FeaturedImage { get; set; }

    /// <summary>
    /// Featured image alt text
    /// </summary>
    [StringLength(200, ErrorMessage = "Image alt text must not exceed 200 characters")]
    public string? FeaturedImageAlt { get; set; }

    /// <summary>
    /// Category ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Tag IDs associated with this post
    /// </summary>
    public List<Guid> TagIds { get; set; } = new();

    /// <summary>
    /// Whether comments are allowed on this post
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// Whether the post is featured
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// Whether the post is pinned
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Custom SEO title (if different from post title)
    /// </summary>
    [StringLength(70, ErrorMessage = "SEO title must not exceed 70 characters")]
    public string? SeoTitle { get; set; }

    /// <summary>
    /// SEO meta description
    /// </summary>
    [StringLength(160, ErrorMessage = "Meta description must not exceed 160 characters")]
    public string? MetaDescription { get; set; }

    /// <summary>
    /// SEO meta keywords
    /// </summary>
    [StringLength(255, ErrorMessage = "Meta keywords must not exceed 255 characters")]
    public string? MetaKeywords { get; set; }

    /// <summary>
    /// Canonical URL (for SEO)
    /// </summary>
    [StringLength(500, ErrorMessage = "Canonical URL must not exceed 500 characters")]
    [Url(ErrorMessage = "Canonical URL must be a valid URL")]
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Custom CSS for this post
    /// </summary>
    public string? CustomCss { get; set; }

    /// <summary>
    /// Custom JavaScript for this post
    /// </summary>
    public string? CustomJs { get; set; }

    /// <summary>
    /// Post template to use
    /// </summary>
    [StringLength(50, ErrorMessage = "Template must not exceed 50 characters")]
    public string? Template { get; set; }

    /// <summary>
    /// Post language code (ISO 639-1)
    /// </summary>
    [StringLength(10, ErrorMessage = "Language must not exceed 10 characters")]
    [RegularExpression(@"^[a-z]{2}(-[A-Z]{2})?$",
        ErrorMessage = "Language must be a valid language code (e.g., 'en', 'en-US')")]
    public string? Language { get; set; }

    /// <summary>
    /// Reading time estimate in minutes (will be auto-calculated if not provided)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Reading time must be between 1 and 300 minutes")]
    public int? ReadingTimeMinutes { get; set; }

    /// <summary>
    /// Author ID (will be set from current user if not provided)
    /// </summary>
    public Guid? AuthorId { get; set; }
}