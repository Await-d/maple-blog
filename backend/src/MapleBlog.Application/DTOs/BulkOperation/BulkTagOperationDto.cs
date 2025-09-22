using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Constants for tag bulk operations (extends existing operations)
/// </summary>
public static class BulkTagOperations
{
    public const string Activate = "activate";
    public const string Deactivate = "deactivate";
    public const string Delete = "delete";
    public const string Merge = "merge";
    public const string Rename = "rename";
    public const string UpdateColor = "update_color";
    public const string UpdateDescription = "update_description";
    public const string CleanupUnused = "cleanup_unused";
    public const string Export = "export";
    public const string GenerateSlugs = "generate_slugs";
    public const string ConsolidateSimilar = "consolidate_similar";
}

/// <summary>
/// Enhanced bulk tag operation request
/// </summary>
public class BulkTagOperationRequest : BulkOperationRequest
{
    public BulkTagOperationRequest()
    {
        EntityType = "Tag";
    }

    /// <summary>
    /// Tag IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one tag ID is required")]
    [MinLength(1, ErrorMessage = "At least one tag ID is required")]
    public new List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Available operations for tags
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    [AllowedTagOperations]
    public new string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Whether to update affected posts' search index
    /// </summary>
    public bool UpdateSearchIndex { get; set; } = true;

    /// <summary>
    /// Whether to clear tag-related caches
    /// </summary>
    public bool ClearCache { get; set; } = true;
}

/// <summary>
/// Bulk tag merge operation (enhanced version of existing MergeTagsRequest)
/// </summary>
public class BulkTagMergeRequest : BulkTagOperationRequest
{
    public BulkTagMergeRequest()
    {
        Operation = BulkTagOperations.Merge;
    }

    /// <summary>
    /// Target tag ID to merge into
    /// </summary>
    [Required(ErrorMessage = "Target tag ID is required")]
    public Guid TargetTagId { get; set; }

    /// <summary>
    /// Whether to delete source tags after merge
    /// </summary>
    public bool DeleteSourceTags { get; set; } = true;

    /// <summary>
    /// How to handle conflicting tag properties
    /// </summary>
    public TagMergeConflictResolution ConflictResolution { get; set; } = TagMergeConflictResolution.KeepTarget;

    /// <summary>
    /// Whether to merge tag descriptions
    /// </summary>
    public bool MergeDescriptions { get; set; } = false;

    /// <summary>
    /// Whether to notify post authors of tag changes
    /// </summary>
    public bool NotifyPostAuthors { get; set; } = false;

    /// <summary>
    /// Custom merge reason for audit purposes
    /// </summary>
    [StringLength(500, ErrorMessage = "Merge reason cannot exceed 500 characters")]
    public string? MergeReason { get; set; }
}

/// <summary>
/// Tag merge conflict resolution strategies
/// </summary>
public enum TagMergeConflictResolution
{
    /// <summary>
    /// Keep target tag properties
    /// </summary>
    KeepTarget = 0,

    /// <summary>
    /// Use source properties if they are more recent
    /// </summary>
    KeepNewer = 1,

    /// <summary>
    /// Use source properties if target is empty
    /// </summary>
    FillEmpty = 2,

    /// <summary>
    /// Combine properties where possible
    /// </summary>
    Combine = 3
}

/// <summary>
/// Bulk tag deletion operation
/// </summary>
public class BulkTagDeleteRequest : BulkTagOperationRequest
{
    public BulkTagDeleteRequest()
    {
        Operation = BulkTagOperations.Delete;
    }

    /// <summary>
    /// Whether to perform soft delete or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// Whether to remove tags from posts or leave as orphaned references
    /// </summary>
    public bool RemoveFromPosts { get; set; } = true;

    /// <summary>
    /// Alternative tags to suggest to posts that lose these tags
    /// </summary>
    public List<Guid> SuggestedReplacementTagIds { get; set; } = new();

    /// <summary>
    /// Whether to notify authors of affected posts
    /// </summary>
    public bool NotifyAffectedAuthors { get; set; } = false;

    /// <summary>
    /// Deletion reason for audit purposes
    /// </summary>
    [StringLength(500, ErrorMessage = "Deletion reason cannot exceed 500 characters")]
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Data retention period for soft-deleted tags
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Bulk tag cleanup operation for unused tags
/// </summary>
public class BulkTagCleanupRequest : BulkTagOperationRequest
{
    public BulkTagCleanupRequest()
    {
        Operation = BulkTagOperations.CleanupUnused;
        // Override EntityIds as they will be determined by the cleanup criteria
        EntityIds = new List<Guid>();
    }

    /// <summary>
    /// Minimum usage count threshold (tags below this will be cleaned up)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Usage threshold must be non-negative")]
    public int UsageThreshold { get; set; } = 0;

    /// <summary>
    /// Maximum age for unused tags (tags older than this without usage will be cleaned)
    /// </summary>
    public TimeSpan? MaxAgeForUnused { get; set; } = TimeSpan.FromDays(180);

    /// <summary>
    /// Whether to include tags that have been manually marked as protected
    /// </summary>
    public bool IncludeProtectedTags { get; set; } = false;

    /// <summary>
    /// Tag name patterns to exclude from cleanup (regex supported)
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new();

    /// <summary>
    /// Whether to dry-run the cleanup (preview only)
    /// </summary>
    public bool DryRun { get; set; } = true;

    /// <summary>
    /// Whether to create a backup before cleanup
    /// </summary>
    public bool CreateBackup { get; set; } = true;
}

/// <summary>
/// Bulk tag rename operation
/// </summary>
public class BulkTagRenameRequest : BulkTagOperationRequest
{
    public BulkTagRenameRequest()
    {
        Operation = BulkTagOperations.Rename;
    }

    /// <summary>
    /// Rename mapping: Tag ID -> New Name
    /// </summary>
    [Required(ErrorMessage = "Rename mapping is required")]
    public Dictionary<Guid, string> RenameMapping { get; set; } = new();

    /// <summary>
    /// Whether to auto-generate new slugs from new names
    /// </summary>
    public bool AutoGenerateSlugs { get; set; } = true;

    /// <summary>
    /// Whether to preserve existing slugs if possible
    /// </summary>
    public bool PreserveExistingSlugs { get; set; } = false;

    /// <summary>
    /// Whether to check for duplicate names after rename
    /// </summary>
    public bool CheckForDuplicates { get; set; } = true;

    /// <summary>
    /// How to handle duplicate names
    /// </summary>
    public DuplicateNameHandling DuplicateHandling { get; set; } = DuplicateNameHandling.AppendNumber;
}

/// <summary>
/// Duplicate name handling strategies
/// </summary>
public enum DuplicateNameHandling
{
    /// <summary>
    /// Fail the operation if duplicates are found
    /// </summary>
    Fail = 0,

    /// <summary>
    /// Append a number to make names unique
    /// </summary>
    AppendNumber = 1,

    /// <summary>
    /// Merge tags with duplicate names
    /// </summary>
    Merge = 2,

    /// <summary>
    /// Skip tags that would create duplicates
    /// </summary>
    Skip = 3
}

/// <summary>
/// Bulk tag appearance update operation
/// </summary>
public class BulkTagAppearanceUpdateRequest : BulkTagOperationRequest
{
    public BulkTagAppearanceUpdateRequest()
    {
        Operation = BulkTagOperations.UpdateColor;
    }

    /// <summary>
    /// New color for tags (hex code)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color code")]
    public string? Color { get; set; }

    /// <summary>
    /// Color assignment strategy
    /// </summary>
    public TagColorStrategy ColorStrategy { get; set; } = TagColorStrategy.SingleColor;

    /// <summary>
    /// Color palette for automatic assignment
    /// </summary>
    public List<string> ColorPalette { get; set; } = new();

    /// <summary>
    /// Whether to base color on tag usage count
    /// </summary>
    public bool ColorByUsage { get; set; } = false;

    /// <summary>
    /// Custom color mapping: Tag ID -> Color
    /// </summary>
    public Dictionary<Guid, string> CustomColorMapping { get; set; } = new();
}

/// <summary>
/// Tag color assignment strategies
/// </summary>
public enum TagColorStrategy
{
    /// <summary>
    /// Assign the same color to all tags
    /// </summary>
    SingleColor = 0,

    /// <summary>
    /// Assign colors randomly from palette
    /// </summary>
    RandomFromPalette = 1,

    /// <summary>
    /// Assign colors based on tag usage (high usage = brighter colors)
    /// </summary>
    ByUsage = 2,

    /// <summary>
    /// Assign colors based on tag name hash (consistent colors)
    /// </summary>
    ByNameHash = 3,

    /// <summary>
    /// Use custom mapping provided
    /// </summary>
    CustomMapping = 4
}

/// <summary>
/// Bulk tag consolidation operation for similar tags
/// </summary>
public class BulkTagConsolidationRequest : BulkTagOperationRequest
{
    public BulkTagConsolidationRequest()
    {
        Operation = BulkTagOperations.ConsolidateSimilar;
    }

    /// <summary>
    /// Similarity threshold for automatic consolidation (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Similarity threshold must be between 0.0 and 1.0")]
    public double SimilarityThreshold { get; set; } = 0.8;

    /// <summary>
    /// Similarity algorithms to use
    /// </summary>
    public List<SimilarityAlgorithm> SimilarityAlgorithms { get; set; } = new()
    {
        SimilarityAlgorithm.LevenshteinDistance,
        SimilarityAlgorithm.SoundexMatch
    };

    /// <summary>
    /// Whether to consider case differences as similarity
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Whether to consider pluralization differences
    /// </summary>
    public bool IgnorePluralization { get; set; } = true;

    /// <summary>
    /// Custom consolidation mapping (override automatic detection)
    /// </summary>
    public Dictionary<Guid, Guid> CustomConsolidationMapping { get; set; } = new();

    /// <summary>
    /// Whether to preview consolidation before applying
    /// </summary>
    public bool PreviewOnly { get; set; } = true;
}

/// <summary>
/// Similarity algorithms for tag consolidation
/// </summary>
public enum SimilarityAlgorithm
{
    /// <summary>
    /// Levenshtein (edit) distance
    /// </summary>
    LevenshteinDistance = 0,

    /// <summary>
    /// Soundex phonetic matching
    /// </summary>
    SoundexMatch = 1,

    /// <summary>
    /// Jaro-Winkler similarity
    /// </summary>
    JaroWinkler = 2,

    /// <summary>
    /// Exact match ignoring case
    /// </summary>
    ExactIgnoreCase = 3,

    /// <summary>
    /// Contains relationship
    /// </summary>
    Contains = 4
}

/// <summary>
/// Bulk tag export operation
/// </summary>
public class BulkTagExportRequest : BulkTagOperationRequest
{
    public BulkTagExportRequest()
    {
        Operation = BulkTagOperations.Export;
    }

    /// <summary>
    /// Export format
    /// </summary>
    [Required(ErrorMessage = "Export format is required")]
    public TagExportFormat Format { get; set; } = TagExportFormat.Json;

    /// <summary>
    /// Whether to include tag usage statistics
    /// </summary>
    public bool IncludeUsageStats { get; set; } = true;

    /// <summary>
    /// Whether to include related posts
    /// </summary>
    public bool IncludeRelatedPosts { get; set; } = false;

    /// <summary>
    /// Whether to include tag relationships/similarity data
    /// </summary>
    public bool IncludeRelationships { get; set; } = false;

    /// <summary>
    /// Date range for usage statistics
    /// </summary>
    public DateRange? UsageStatsPeriod { get; set; }

    /// <summary>
    /// Whether to group tags by usage level
    /// </summary>
    public bool GroupByUsage { get; set; } = false;

    /// <summary>
    /// Whether to include tag cloud weight data
    /// </summary>
    public bool IncludeCloudWeights { get; set; } = true;
}

/// <summary>
/// Tag export formats
/// </summary>
public enum TagExportFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json = 0,

    /// <summary>
    /// CSV format
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
    /// Tag cloud HTML
    /// </summary>
    TagCloudHtml = 4,

    /// <summary>
    /// YAML format
    /// </summary>
    Yaml = 5
}

/// <summary>
/// Response for bulk tag operations
/// </summary>
public class BulkTagOperationResponse : BulkOperationResponse
{
    public BulkTagOperationResponse()
    {
        EntityType = "Tag";
    }

    /// <summary>
    /// Tags that were successfully processed
    /// </summary>
    public List<TagSummaryDto> ProcessedTags { get; set; } = new();

    /// <summary>
    /// Tags that failed to process
    /// </summary>
    public List<FailedTagDto> FailedTags { get; set; } = new();

    /// <summary>
    /// Export file information (for export operations)
    /// </summary>
    public ExportFileInfo? ExportFile { get; set; }

    /// <summary>
    /// Consolidation preview (for consolidation operations)
    /// </summary>
    public TagConsolidationPreview? ConsolidationPreview { get; set; }

    /// <summary>
    /// Cleanup preview (for cleanup operations)
    /// </summary>
    public TagCleanupPreview? CleanupPreview { get; set; }

    /// <summary>
    /// Statistics about the operation
    /// </summary>
    public BulkTagOperationStats Stats { get; set; } = new();
}

/// <summary>
/// Summary information for a processed tag
/// </summary>
public class TagSummaryDto
{
    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Tag color
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Current usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Whether tag is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Changes made to this tag
    /// </summary>
    public List<string> ChangesApplied { get; set; } = new();
}

/// <summary>
/// Information about a tag that failed to process
/// </summary>
public class FailedTagDto
{
    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name (if available)
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
}

/// <summary>
/// Tag consolidation preview
/// </summary>
public class TagConsolidationPreview
{
    /// <summary>
    /// Proposed consolidation groups
    /// </summary>
    public List<TagConsolidationGroup> ConsolidationGroups { get; set; } = new();

    /// <summary>
    /// Total tags that would be consolidated
    /// </summary>
    public int TotalTagsToConsolidate { get; set; }

    /// <summary>
    /// Total posts that would be affected
    /// </summary>
    public int TotalPostsAffected { get; set; }
}

/// <summary>
/// Tag consolidation group
/// </summary>
public class TagConsolidationGroup
{
    /// <summary>
    /// Target tag (the one that others will merge into)
    /// </summary>
    public TagSummaryDto TargetTag { get; set; } = new();

    /// <summary>
    /// Source tags (the ones that will be merged)
    /// </summary>
    public List<TagSummaryDto> SourceTags { get; set; } = new();

    /// <summary>
    /// Similarity score
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Algorithms that detected this similarity
    /// </summary>
    public List<SimilarityAlgorithm> DetectedBySimilarityAlgorithms { get; set; } = new();
}

/// <summary>
/// Tag cleanup preview
/// </summary>
public class TagCleanupPreview
{
    /// <summary>
    /// Tags that would be cleaned up
    /// </summary>
    public List<TagSummaryDto> TagsToCleanup { get; set; } = new();

    /// <summary>
    /// Cleanup statistics by criteria
    /// </summary>
    public TagCleanupStats CleanupStats { get; set; } = new();
}

/// <summary>
/// Tag cleanup statistics
/// </summary>
public class TagCleanupStats
{
    /// <summary>
    /// Tags with zero usage
    /// </summary>
    public int ZeroUsageTags { get; set; }

    /// <summary>
    /// Tags below usage threshold
    /// </summary>
    public int BelowThresholdTags { get; set; }

    /// <summary>
    /// Tags older than max age
    /// </summary>
    public int OldUnusedTags { get; set; }

    /// <summary>
    /// Protected tags excluded from cleanup
    /// </summary>
    public int ProtectedTagsExcluded { get; set; }
}

/// <summary>
/// Statistics for bulk tag operations
/// </summary>
public class BulkTagOperationStats
{
    /// <summary>
    /// Number of active tags processed
    /// </summary>
    public int ActiveTags { get; set; }

    /// <summary>
    /// Number of inactive tags processed
    /// </summary>
    public int InactiveTags { get; set; }

    /// <summary>
    /// Tags by usage ranges
    /// </summary>
    public Dictionary<string, int> TagsByUsageRange { get; set; } = new();

    /// <summary>
    /// Total posts affected by the operation
    /// </summary>
    public int TotalPostsAffected { get; set; }

    /// <summary>
    /// Total usage count before operation
    /// </summary>
    public int TotalUsageBefore { get; set; }

    /// <summary>
    /// Total usage count after operation
    /// </summary>
    public int TotalUsageAfter { get; set; }

    /// <summary>
    /// Search index documents updated
    /// </summary>
    public int SearchIndexUpdates { get; set; }

    /// <summary>
    /// Cache keys cleared
    /// </summary>
    public int CacheKeysCleared { get; set; }
}

/// <summary>
/// Custom validation attribute for allowed tag operations
/// </summary>
public class AllowedTagOperationsAttribute : ValidationAttribute
{
    private static readonly string[] AllowedOperations =
    {
        BulkTagOperations.Activate,
        BulkTagOperations.Deactivate,
        BulkTagOperations.Delete,
        BulkTagOperations.Merge,
        BulkTagOperations.Rename,
        BulkTagOperations.UpdateColor,
        BulkTagOperations.UpdateDescription,
        BulkTagOperations.CleanupUnused,
        BulkTagOperations.Export,
        BulkTagOperations.GenerateSlugs,
        BulkTagOperations.ConsolidateSimilar
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