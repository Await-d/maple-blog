using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Data transfer object for updating an existing category
/// </summary>
public class UpdateCategoryDto
{
    /// <summary>
    /// Category name
    /// </summary>
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (will be auto-generated if not provided)
    /// </summary>
    [StringLength(100, ErrorMessage = "Slug must not exceed 100 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
    public string? Slug { get; set; }

    /// <summary>
    /// Category description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Parent category ID (null for root categories)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Display order within the same parent level
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether the category is visible to public
    /// </summary>
    public bool IsVisible { get; set; } = true;

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
    /// Category icon (CSS class or URL)
    /// </summary>
    [StringLength(100, ErrorMessage = "Icon must not exceed 100 characters")]
    public string? Icon { get; set; }

    /// <summary>
    /// Category color (hex code)
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
        ErrorMessage = "Color must be a valid hex color code")]
    public string? Color { get; set; }

    /// <summary>
    /// Update slug even if it already exists (admin only)
    /// </summary>
    public bool ForceSlugUpdate { get; set; } = false;
}