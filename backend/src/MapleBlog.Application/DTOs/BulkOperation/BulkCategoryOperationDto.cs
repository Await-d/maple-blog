using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Constants for category bulk operations
/// </summary>
public static class BulkCategoryOperations
{
    public const string Activate = "activate";
    public const string Deactivate = "deactivate";
    public const string Delete = "delete";
    public const string Move = "move";
    public const string Merge = "merge";
    public const string Reorder = "reorder";
    public const string UpdateParent = "update_parent";
    public const string Export = "export";
    public const string UpdateSeo = "update_seo";
    public const string UpdateAppearance = "update_appearance";
}

/// <summary>
/// Base bulk category operation request
/// </summary>
public class BulkCategoryOperationRequest : BulkOperationRequest
{
    public BulkCategoryOperationRequest()
    {
        EntityType = "Category";
    }

    /// <summary>
    /// Category IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one category ID is required")]
    [MinLength(1, ErrorMessage = "At least one category ID is required")]
    public new List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Available operations for categories
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    [AllowedCategoryOperations]
    public new string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Whether to update affected posts' search index
    /// </summary>
    public bool UpdateSearchIndex { get; set; } = true;

    /// <summary>
    /// Whether to clear related caches
    /// </summary>
    public bool ClearCache { get; set; } = true;
}

/// <summary>
/// Bulk category move operation
/// </summary>
public class BulkCategoryMoveRequest : BulkCategoryOperationRequest
{
    public BulkCategoryMoveRequest()
    {
        Operation = BulkCategoryOperations.Move;
    }

    /// <summary>
    /// New parent category ID (null to move to root level)
    /// </summary>
    public Guid? NewParentId { get; set; }

    /// <summary>
    /// Whether to preserve existing hierarchy within moved categories
    /// </summary>
    public bool PreserveHierarchy { get; set; } = true;

    /// <summary>
    /// How to handle display order for moved categories
    /// </summary>
    public CategoryOrderHandling OrderHandling { get; set; } = CategoryOrderHandling.AppendToEnd;

    /// <summary>
    /// Whether to update SEO paths after move
    /// </summary>
    public bool UpdateSeoPaths { get; set; } = true;

    /// <summary>
    /// Whether to create redirects for old category URLs
    /// </summary>
    public bool CreateRedirects { get; set; } = true;
}

/// <summary>
/// Category order handling strategies
/// </summary>
public enum CategoryOrderHandling
{
    /// <summary>
    /// Append categories to the end of the target parent's children
    /// </summary>
    AppendToEnd = 0,

    /// <summary>
    /// Insert categories at the beginning of the target parent's children
    /// </summary>
    InsertAtBeginning = 1,

    /// <summary>
    /// Preserve original order numbers where possible
    /// </summary>
    PreserveOrder = 2,

    /// <summary>
    /// Reorder all children alphabetically
    /// </summary>
    OrderAlphabetically = 3
}

/// <summary>
/// Bulk category merge operation
/// </summary>
public class BulkCategoryMergeRequest : BulkCategoryOperationRequest
{
    public BulkCategoryMergeRequest()
    {
        Operation = BulkCategoryOperations.Merge;
    }

    /// <summary>
    /// Target category ID to merge into
    /// </summary>
    [Required(ErrorMessage = "Target category ID is required")]
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
    /// Whether to merge child categories as well
    /// </summary>
    public bool MergeChildCategories { get; set; } = true;

    /// <summary>
    /// How to handle duplicate posts (same post in multiple categories)
    /// </summary>
    public DuplicatePostHandling DuplicatePostHandling { get; set; } = DuplicatePostHandling.KeepInTarget;
}

/// <summary>
/// Duplicate post handling during category merge
/// </summary>
public enum DuplicatePostHandling
{
    /// <summary>
    /// Keep post in target category only
    /// </summary>
    KeepInTarget = 0,

    /// <summary>
    /// Allow post to remain in multiple categories
    /// </summary>
    AllowMultiple = 1,

    /// <summary>
    /// Remove from all merged categories
    /// </summary>
    RemoveFromAll = 2
}

/// <summary>
/// Bulk category deletion operation
/// </summary>
public class BulkCategoryDeleteRequest : BulkCategoryOperationRequest
{
    public BulkCategoryDeleteRequest()
    {
        Operation = BulkCategoryOperations.Delete;
    }

    /// <summary>
    /// Whether to perform soft delete or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// How to handle child categories
    /// </summary>
    public ChildCategoryHandling ChildHandling { get; set; } = ChildCategoryHandling.PreventDeletion;

    /// <summary>
    /// How to handle posts in these categories
    /// </summary>
    public PostMigrationStrategy PostMigration { get; set; } = PostMigrationStrategy.MoveToParent;

    /// <summary>
    /// Target category for post migration (if using MoveToTarget strategy)
    /// </summary>
    public Guid? PostMigrationTargetId { get; set; }

    /// <summary>
    /// Whether to create redirects for deleted category URLs
    /// </summary>
    public bool CreateRedirects { get; set; } = true;

    /// <summary>
    /// Data retention period for soft-deleted categories
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Bulk category reorder operation
/// </summary>
public class BulkCategoryReorderRequest : BulkCategoryOperationRequest
{
    public BulkCategoryReorderRequest()
    {
        Operation = BulkCategoryOperations.Reorder;
    }

    /// <summary>
    /// New order mapping: Category ID -> Display Order
    /// </summary>
    [Required(ErrorMessage = "Category order mapping is required")]
    public Dictionary<Guid, int> CategoryOrders { get; set; } = new();

    /// <summary>
    /// Whether to reorder globally or within each parent
    /// </summary>
    public bool ReorderGlobally { get; set; } = false;

    /// <summary>
    /// Specific parent ID to reorder within (null for root categories)
    /// </summary>
    public Guid? WithinParentId { get; set; }

    /// <summary>
    /// Whether to automatically adjust orders to prevent gaps
    /// </summary>
    public bool AdjustForGaps { get; set; } = true;
}

/// <summary>
/// Bulk category SEO update operation
/// </summary>
public class BulkCategorySeoUpdateRequest : BulkCategoryOperationRequest
{
    public BulkCategorySeoUpdateRequest()
    {
        Operation = BulkCategoryOperations.UpdateSeo;
    }

    /// <summary>
    /// SEO template settings
    /// </summary>
    public CategorySeoTemplate? SeoTemplate { get; set; }

    /// <summary>
    /// Whether to auto-generate meta descriptions
    /// </summary>
    public bool GenerateMetaDescriptions { get; set; } = true;

    /// <summary>
    /// Whether to auto-generate meta keywords from category and post data
    /// </summary>
    public bool GenerateMetaKeywords { get; set; } = true;

    /// <summary>
    /// Whether to optimize URL slugs
    /// </summary>
    public bool OptimizeSlugs { get; set; } = false;

    /// <summary>
    /// Target language for SEO optimization
    /// </summary>
    public string? TargetLanguage { get; set; }
}

/// <summary>
/// Category SEO template for bulk updates
/// </summary>
public class CategorySeoTemplate
{
    /// <summary>
    /// Meta title template (use {CategoryName}, {PostCount}, etc.)
    /// </summary>
    public string? MetaTitleTemplate { get; set; }

    /// <summary>
    /// Meta description template
    /// </summary>
    public string? MetaDescriptionTemplate { get; set; }

    /// <summary>
    /// Meta keywords template
    /// </summary>
    public string? MetaKeywordsTemplate { get; set; }

    /// <summary>
    /// Available template variables
    /// </summary>
    public static readonly string[] TemplateVariables =
    {
        "{CategoryName}",
        "{CategoryDescription}",
        "{PostCount}",
        "{ParentCategory}",
        "{SiteName}",
        "{CurrentDate}",
        "{CurrentYear}"
    };
}

/// <summary>
/// Bulk category appearance update operation
/// </summary>
public class BulkCategoryAppearanceUpdateRequest : BulkCategoryOperationRequest
{
    public BulkCategoryAppearanceUpdateRequest()
    {
        Operation = BulkCategoryOperations.UpdateAppearance;
    }

    /// <summary>
    /// New color for categories (hex code)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? Color { get; set; }

    /// <summary>
    /// New icon for categories
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Cover image URL template or specific URL
    /// </summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>
    /// Whether to apply appearance to child categories
    /// </summary>
    public bool ApplyToChildren { get; set; } = false;

    /// <summary>
    /// Color scheme to apply
    /// </summary>
    public CategoryColorScheme? ColorScheme { get; set; }
}

/// <summary>
/// Category color scheme for bulk appearance updates
/// </summary>
public class CategoryColorScheme
{
    /// <summary>
    /// Primary color
    /// </summary>
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary color
    /// </summary>
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Text color
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// Background color
    /// </summary>
    public string? BackgroundColor { get; set; }
}

/// <summary>
/// Bulk category export operation
/// </summary>
public class BulkCategoryExportRequest : BulkCategoryOperationRequest
{
    public BulkCategoryExportRequest()
    {
        Operation = BulkCategoryOperations.Export;
    }

    /// <summary>
    /// Export format
    /// </summary>
    [Required(ErrorMessage = "Export format is required")]
    public CategoryExportFormat Format { get; set; } = CategoryExportFormat.Json;

    /// <summary>
    /// Whether to include child categories
    /// </summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>
    /// Whether to include category statistics
    /// </summary>
    public bool IncludeStatistics { get; set; } = true;

    /// <summary>
    /// Whether to include SEO metadata
    /// </summary>
    public bool IncludeSeoMetadata { get; set; } = true;

    /// <summary>
    /// Whether to include appearance settings
    /// </summary>
    public bool IncludeAppearance { get; set; } = true;

    /// <summary>
    /// Whether to include posts in each category
    /// </summary>
    public bool IncludePosts { get; set; } = false;

    /// <summary>
    /// Whether to preserve hierarchy structure in export
    /// </summary>
    public bool PreserveHierarchy { get; set; } = true;
}

/// <summary>
/// Category export formats
/// </summary>
public enum CategoryExportFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json = 0,

    /// <summary>
    /// CSV format (flattened hierarchy)
    /// </summary>
    Csv = 1,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 2,

    /// <summary>
    /// Excel spreadsheet
    /// </summary>
    Excel = 3,

    /// <summary>
    /// YAML format
    /// </summary>
    Yaml = 4
}

/// <summary>
/// Response for bulk category operations
/// </summary>
public class BulkCategoryOperationResponse : BulkOperationResponse
{
    public BulkCategoryOperationResponse()
    {
        EntityType = "Category";
    }

    /// <summary>
    /// Categories that were successfully processed
    /// </summary>
    public List<CategorySummaryDto> ProcessedCategories { get; set; } = new();

    /// <summary>
    /// Categories that failed to process
    /// </summary>
    public List<FailedCategoryDto> FailedCategories { get; set; } = new();

    /// <summary>
    /// Export file information (for export operations)
    /// </summary>
    public ExportFileInfo? ExportFile { get; set; }

    /// <summary>
    /// Post migration results (for operations affecting posts)
    /// </summary>
    public PostMigrationSummary? PostMigrationSummary { get; set; }

    /// <summary>
    /// Statistics about the operation
    /// </summary>
    public BulkCategoryOperationStats Stats { get; set; } = new();
}

/// <summary>
/// Summary information for a processed category
/// </summary>
public class CategorySummaryDto
{
    /// <summary>
    /// Category ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Parent category name (if any)
    /// </summary>
    public string? ParentName { get; set; }

    /// <summary>
    /// Current hierarchy level
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Number of posts in this category
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// Whether category is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Changes made to this category
    /// </summary>
    public List<string> ChangesApplied { get; set; } = new();
}

/// <summary>
/// Information about a category that failed to process
/// </summary>
public class FailedCategoryDto
{
    /// <summary>
    /// Category ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name (if available)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this failure affected child categories
    /// </summary>
    public bool AffectedChildCategories { get; set; }
}

/// <summary>
/// Post migration summary for category operations
/// </summary>
public class PostMigrationSummary
{
    /// <summary>
    /// Total posts migrated
    /// </summary>
    public int PostsMigrated { get; set; }

    /// <summary>
    /// Posts that failed to migrate
    /// </summary>
    public int PostsMigrationFailed { get; set; }

    /// <summary>
    /// Target categories for migration
    /// </summary>
    public List<CategorySummaryDto> TargetCategories { get; set; } = new();

    /// <summary>
    /// Migration errors
    /// </summary>
    public List<string> MigrationErrors { get; set; } = new();
}

/// <summary>
/// Statistics for bulk category operations
/// </summary>
public class BulkCategoryOperationStats
{
    /// <summary>
    /// Number of root categories processed
    /// </summary>
    public int RootCategories { get; set; }

    /// <summary>
    /// Number of child categories processed
    /// </summary>
    public int ChildCategories { get; set; }

    /// <summary>
    /// Categories by hierarchy level
    /// </summary>
    public Dictionary<int, int> CategoriesByLevel { get; set; } = new();

    /// <summary>
    /// Total posts affected by the operation
    /// </summary>
    public int TotalPostsAffected { get; set; }

    /// <summary>
    /// Number of redirects created
    /// </summary>
    public int RedirectsCreated { get; set; }

    /// <summary>
    /// Cache keys cleared
    /// </summary>
    public int CacheKeysCleared { get; set; }

    /// <summary>
    /// Search index documents updated
    /// </summary>
    public int SearchIndexUpdates { get; set; }
}

/// <summary>
/// Custom validation attribute for allowed category operations
/// </summary>
public class AllowedCategoryOperationsAttribute : ValidationAttribute
{
    private static readonly string[] AllowedOperations =
    {
        BulkCategoryOperations.Activate,
        BulkCategoryOperations.Deactivate,
        BulkCategoryOperations.Delete,
        BulkCategoryOperations.Move,
        BulkCategoryOperations.Merge,
        BulkCategoryOperations.Reorder,
        BulkCategoryOperations.UpdateParent,
        BulkCategoryOperations.Export,
        BulkCategoryOperations.UpdateSeo,
        BulkCategoryOperations.UpdateAppearance
    };

    public override bool IsValid(object? value)
    {
        if (value is not string operation)
            return false;

        return AllowedOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"The field {name} must be one of the following values: {string.Join(", ", AllowedOperations)}.";
    }
}