using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Data transfer object for merging multiple tags into one
/// </summary>
public class MergeTagsDto
{
    /// <summary>
    /// ID of the target tag to merge into
    /// </summary>
    [Required]
    public Guid TargetTagId { get; set; }

    /// <summary>
    /// IDs of tags to be merged into the target tag (will be deleted)
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one source tag ID is required")]
    public List<Guid> SourceTagIds { get; set; } = new();

    /// <summary>
    /// Whether to update the target tag's name if provided
    /// </summary>
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Tag name must be between 1 and 50 characters")]
    public string? NewName { get; set; }

    /// <summary>
    /// Whether to update the target tag's description if provided
    /// </summary>
    [StringLength(200, ErrorMessage = "Description must not exceed 200 characters")]
    public string? NewDescription { get; set; }

    /// <summary>
    /// Whether to update the target tag's color if provided
    /// </summary>
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
        ErrorMessage = "Color must be a valid hex color code")]
    public string? NewColor { get; set; }

    /// <summary>
    /// Optional reason for the merge (for audit purposes)
    /// </summary>
    [StringLength(500, ErrorMessage = "Merge reason must not exceed 500 characters")]
    public string? MergeReason { get; set; }
}