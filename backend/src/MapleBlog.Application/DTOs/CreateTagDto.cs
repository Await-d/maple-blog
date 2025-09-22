using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Data transfer object for creating a new tag
/// </summary>
public class CreateTagDto
{
    /// <summary>
    /// Tag name
    /// </summary>
    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Tag name must be between 1 and 50 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier (will be auto-generated if not provided)
    /// </summary>
    [StringLength(50, ErrorMessage = "Slug must not exceed 50 characters")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
    public string? Slug { get; set; }

    /// <summary>
    /// Tag description
    /// </summary>
    [StringLength(200, ErrorMessage = "Description must not exceed 200 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Tag color (hex code)
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
        ErrorMessage = "Color must be a valid hex color code")]
    public string? Color { get; set; }

    /// <summary>
    /// Whether the tag is visible to public
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// SEO meta description
    /// </summary>
    [StringLength(160, ErrorMessage = "Meta description must not exceed 160 characters")]
    public string? MetaDescription { get; set; }
}