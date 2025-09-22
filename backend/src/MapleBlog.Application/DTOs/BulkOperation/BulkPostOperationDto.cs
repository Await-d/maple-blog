using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Constants for post bulk operations
/// </summary>
public static class BulkPostOperations
{
    public const string Publish = "publish";
    public const string Unpublish = "unpublish";
    public const string Draft = "draft";
    public const string Archive = "archive";
    public const string Delete = "delete";
    public const string ChangeCategory = "change_category";
    public const string AddTags = "add_tags";
    public const string RemoveTags = "remove_tags";
    public const string ReplaceTags = "replace_tags";
    public const string ChangeAuthor = "change_author";
    public const string UpdateStatus = "update_status";
    public const string Export = "export";
    public const string GenerateSummary = "generate_summary";
    public const string OptimizeSeo = "optimize_seo";
}

/// <summary>
/// Base bulk post operation request
/// </summary>
public class BulkPostOperationRequest : BulkOperationRequest
{
    public BulkPostOperationRequest()
    {
        EntityType = "Post";
    }

    /// <summary>
    /// Post IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one post ID is required")]
    [MinLength(1, ErrorMessage = "At least one post ID is required")]
    public new List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Available operations for posts
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    [AllowedPostOperations]
    public new string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Whether to update search index after operation
    /// </summary>
    public bool UpdateSearchIndex { get; set; } = true;

    /// <summary>
    /// Whether to clear related caches
    /// </summary>
    public bool ClearCache { get; set; } = true;
}

/// <summary>
/// Bulk post status change operation
/// </summary>
public class BulkPostStatusChangeRequest : BulkPostOperationRequest
{
    public BulkPostStatusChangeRequest()
    {
        Operation = BulkPostOperations.UpdateStatus;
    }

    /// <summary>
    /// New status for the posts
    /// </summary>
    [Required(ErrorMessage = "New status is required")]
    public PostStatus NewStatus { get; set; }

    /// <summary>
    /// Scheduled publish date (for scheduled status)
    /// </summary>
    public DateTime? ScheduledPublishAt { get; set; }

    /// <summary>
    /// Whether to update publish date for published posts
    /// </summary>
    public bool UpdatePublishDate { get; set; } = false;

    /// <summary>
    /// Whether to send notifications for published posts
    /// </summary>
    public bool SendNotifications { get; set; } = true;
}

/// <summary>
/// Bulk post category change operation
/// </summary>
public class BulkPostCategoryChangeRequest : BulkPostOperationRequest
{
    public BulkPostCategoryChangeRequest()
    {
        Operation = BulkPostOperations.ChangeCategory;
    }

    /// <summary>
    /// New category ID for the posts
    /// </summary>
    [Required(ErrorMessage = "New category is required")]
    public Guid NewCategoryId { get; set; }

    /// <summary>
    /// Whether to update SEO settings based on new category
    /// </summary>
    public bool UpdateSeoFromCategory { get; set; } = true;

    /// <summary>
    /// Whether to add category-specific tags
    /// </summary>
    public bool AddCategoryTags { get; set; } = false;
}

/// <summary>
/// Bulk post tag operation
/// </summary>
public class BulkPostTagOperationRequest : BulkPostOperationRequest
{
    /// <summary>
    /// Tag IDs to add, remove, or replace
    /// </summary>
    [Required(ErrorMessage = "At least one tag ID is required")]
    [MinLength(1, ErrorMessage = "At least one tag ID is required")]
    public List<Guid> TagIds { get; set; } = new();

    /// <summary>
    /// For replace operations, existing tags matching these will be removed first
    /// </summary>
    public List<Guid> TagsToRemoveFirst { get; set; } = new();
}

/// <summary>
/// Bulk post author change operation
/// </summary>
public class BulkPostAuthorChangeRequest : BulkPostOperationRequest
{
    public BulkPostAuthorChangeRequest()
    {
        Operation = BulkPostOperations.ChangeAuthor;
    }

    /// <summary>
    /// New author ID for the posts
    /// </summary>
    [Required(ErrorMessage = "New author ID is required")]
    public Guid NewAuthorId { get; set; }

    /// <summary>
    /// Whether to notify the new author
    /// </summary>
    public bool NotifyNewAuthor { get; set; } = true;

    /// <summary>
    /// Whether to notify the old author
    /// </summary>
    public bool NotifyOldAuthor { get; set; } = true;

    /// <summary>
    /// Reason for the author change
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? ChangeReason { get; set; }
}

/// <summary>
/// Bulk post deletion operation
/// </summary>
public class BulkPostDeleteRequest : BulkPostOperationRequest
{
    public BulkPostDeleteRequest()
    {
        Operation = BulkPostOperations.Delete;
    }

    /// <summary>
    /// Whether to perform soft delete or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// How to handle post comments
    /// </summary>
    public PostContentHandling CommentHandling { get; set; } = PostContentHandling.Preserve;

    /// <summary>
    /// How to handle post attachments
    /// </summary>
    public PostContentHandling AttachmentHandling { get; set; } = PostContentHandling.Preserve;

    /// <summary>
    /// Whether to create redirects for deleted posts
    /// </summary>
    public bool CreateRedirects { get; set; } = true;

    /// <summary>
    /// Redirect target (category page, homepage, etc.)
    /// </summary>
    public string? RedirectTarget { get; set; }

    /// <summary>
    /// Data retention period for soft-deleted posts
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Content handling strategy for bulk post operations
/// </summary>
public enum PostContentHandling
{
    /// <summary>
    /// Keep the content
    /// </summary>
    Preserve = 0,

    /// <summary>
    /// Delete the content
    /// </summary>
    Delete = 1,

    /// <summary>
    /// Archive the content separately
    /// </summary>
    Archive = 2,

    /// <summary>
    /// Move to another location
    /// </summary>
    Move = 3
}

/// <summary>
/// Bulk post export operation
/// </summary>
public class BulkPostExportRequest : BulkPostOperationRequest
{
    public BulkPostExportRequest()
    {
        Operation = BulkPostOperations.Export;
    }

    /// <summary>
    /// Export format
    /// </summary>
    [Required(ErrorMessage = "Export format is required")]
    public PostExportFormat Format { get; set; } = PostExportFormat.Json;

    /// <summary>
    /// Whether to include post content
    /// </summary>
    public bool IncludeContent { get; set; } = true;

    /// <summary>
    /// Whether to include comments
    /// </summary>
    public bool IncludeComments { get; set; } = false;

    /// <summary>
    /// Whether to include attachments
    /// </summary>
    public bool IncludeAttachments { get; set; } = false;

    /// <summary>
    /// Whether to include SEO metadata
    /// </summary>
    public bool IncludeSeoMetadata { get; set; } = true;

    /// <summary>
    /// Whether to include analytics data
    /// </summary>
    public bool IncludeAnalytics { get; set; } = false;

    /// <summary>
    /// Content format for export
    /// </summary>
    public PostContentFormat ContentFormat { get; set; } = PostContentFormat.Markdown;

    /// <summary>
    /// Whether to create a single file or multiple files
    /// </summary>
    public bool CreateArchive { get; set; } = true;
}

/// <summary>
/// Post export formats
/// </summary>
public enum PostExportFormat
{
    /// <summary>
    /// JSON format
    /// </summary>
    Json = 0,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 1,

    /// <summary>
    /// WordPress WXR format
    /// </summary>
    Wxr = 2,

    /// <summary>
    /// RSS/Atom feed format
    /// </summary>
    Rss = 3,

    /// <summary>
    /// CSV format (metadata only)
    /// </summary>
    Csv = 4,

    /// <summary>
    /// Markdown files
    /// </summary>
    Markdown = 5
}

/// <summary>
/// Post content format for export
/// </summary>
public enum PostContentFormat
{
    /// <summary>
    /// Original markdown
    /// </summary>
    Markdown = 0,

    /// <summary>
    /// Rendered HTML
    /// </summary>
    Html = 1,

    /// <summary>
    /// Plain text
    /// </summary>
    Text = 2,

    /// <summary>
    /// Both markdown and HTML
    /// </summary>
    Both = 3
}

/// <summary>
/// Bulk post SEO optimization operation
/// </summary>
public class BulkPostSeoOptimizationRequest : BulkPostOperationRequest
{
    public BulkPostSeoOptimizationRequest()
    {
        Operation = BulkPostOperations.OptimizeSeo;
    }

    /// <summary>
    /// Whether to auto-generate meta descriptions
    /// </summary>
    public bool GenerateMetaDescriptions { get; set; } = true;

    /// <summary>
    /// Whether to optimize title tags
    /// </summary>
    public bool OptimizeTitles { get; set; } = true;

    /// <summary>
    /// Whether to generate/update keywords
    /// </summary>
    public bool UpdateKeywords { get; set; } = true;

    /// <summary>
    /// Whether to optimize image alt texts
    /// </summary>
    public bool OptimizeImageAltTexts { get; set; } = true;

    /// <summary>
    /// Whether to generate social media metadata
    /// </summary>
    public bool GenerateSocialMetadata { get; set; } = true;

    /// <summary>
    /// Target language for SEO optimization
    /// </summary>
    public string? TargetLanguage { get; set; }

    /// <summary>
    /// Primary keywords to focus on
    /// </summary>
    public List<string> PrimaryKeywords { get; set; } = new();
}

/// <summary>
/// Response for bulk post operations
/// </summary>
public class BulkPostOperationResponse : BulkOperationResponse
{
    public BulkPostOperationResponse()
    {
        EntityType = "Post";
    }

    /// <summary>
    /// Posts that were successfully processed
    /// </summary>
    public List<PostSummaryDto> ProcessedPosts { get; set; } = new();

    /// <summary>
    /// Posts that failed to process
    /// </summary>
    public List<FailedPostDto> FailedPosts { get; set; } = new();

    /// <summary>
    /// Export file information (for export operations)
    /// </summary>
    public ExportFileInfo? ExportFile { get; set; }

    /// <summary>
    /// Search index update status
    /// </summary>
    public SearchIndexUpdateStatus? SearchIndexStatus { get; set; }

    /// <summary>
    /// Cache clear status
    /// </summary>
    public CacheClearStatus? CacheStatus { get; set; }

    /// <summary>
    /// Statistics about the operation
    /// </summary>
    public BulkPostOperationStats Stats { get; set; } = new();
}

/// <summary>
/// Summary information for a processed post
/// </summary>
public class PostSummaryDto
{
    /// <summary>
    /// Post ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Post title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Post slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public PostStatus Status { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string? CategoryName { get; set; }

    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Changes made to this post
    /// </summary>
    public List<string> ChangesApplied { get; set; } = new();
}

/// <summary>
/// Information about a post that failed to process
/// </summary>
public class FailedPostDto
{
    /// <summary>
    /// Post ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Post title (if available)
    /// </summary>
    public string? Title { get; set; }

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
/// Search index update status
/// </summary>
public class SearchIndexUpdateStatus
{
    /// <summary>
    /// Whether the update was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of documents updated in the index
    /// </summary>
    public int DocumentsUpdated { get; set; }

    /// <summary>
    /// Update duration in milliseconds
    /// </summary>
    public long UpdateDurationMs { get; set; }

    /// <summary>
    /// Error message if update failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Cache clear status
/// </summary>
public class CacheClearStatus
{
    /// <summary>
    /// Whether the cache clear was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of cache keys cleared
    /// </summary>
    public int KeysCleared { get; set; }

    /// <summary>
    /// Cache regions that were cleared
    /// </summary>
    public List<string> RegionsCleared { get; set; } = new();

    /// <summary>
    /// Error message if cache clear failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Statistics for bulk post operations
/// </summary>
public class BulkPostOperationStats
{
    /// <summary>
    /// Number of published posts processed
    /// </summary>
    public int PublishedPosts { get; set; }

    /// <summary>
    /// Number of draft posts processed
    /// </summary>
    public int DraftPosts { get; set; }

    /// <summary>
    /// Number of archived posts processed
    /// </summary>
    public int ArchivedPosts { get; set; }

    /// <summary>
    /// Number of posts by different authors
    /// </summary>
    public Dictionary<string, int> PostsByAuthor { get; set; } = new();

    /// <summary>
    /// Number of posts by category
    /// </summary>
    public Dictionary<string, int> PostsByCategory { get; set; } = new();

    /// <summary>
    /// Total number of tags affected
    /// </summary>
    public int TagsAffected { get; set; }

    /// <summary>
    /// Total number of comments affected
    /// </summary>
    public int CommentsAffected { get; set; }
}

/// <summary>
/// Custom validation attribute for allowed post operations
/// </summary>
public class AllowedPostOperationsAttribute : ValidationAttribute
{
    private static readonly string[] AllowedOperations =
    {
        BulkPostOperations.Publish,
        BulkPostOperations.Unpublish,
        BulkPostOperations.Draft,
        BulkPostOperations.Archive,
        BulkPostOperations.Delete,
        BulkPostOperations.ChangeCategory,
        BulkPostOperations.AddTags,
        BulkPostOperations.RemoveTags,
        BulkPostOperations.ReplaceTags,
        BulkPostOperations.ChangeAuthor,
        BulkPostOperations.UpdateStatus,
        BulkPostOperations.Export,
        BulkPostOperations.GenerateSummary,
        BulkPostOperations.OptimizeSeo
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