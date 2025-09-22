using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Data transfer object for reordering categories
/// </summary>
public class ReorderCategoriesDto
{
    /// <summary>
    /// List of category reorder items
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one category order item is required")]
    public List<CategoryOrderItem> Categories { get; set; } = new();
}

/// <summary>
/// Individual category order item
/// </summary>
public class CategoryOrderItem
{
    /// <summary>
    /// Category ID
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// New display order for this category
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Parent category ID (for hierarchical reordering)
    /// </summary>
    public Guid? ParentId { get; set; }
}