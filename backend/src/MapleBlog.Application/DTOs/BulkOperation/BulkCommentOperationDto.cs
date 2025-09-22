using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Constants for comment bulk operations
/// </summary>
public static class BulkCommentOperations
{
    public const string Approve = "approve";
    public const string Reject = "reject";
    public const string MarkAsSpam = "mark_as_spam";
    public const string Delete = "delete";
    public const string Archive = "archive";
    public const string Pin = "pin";
    public const string Unpin = "unpin";
    public const string ChangeAuthor = "change_author";
    public const string MoveToDifferentPost = "move_to_post";
    public const string Export = "export";
    public const string Moderate = "moderate";
    public const string UpdateStatus = "update_status";
}

/// <summary>
/// Base bulk comment operation request
/// </summary>
public class BulkCommentOperationRequest : BulkOperationRequest
{
    public BulkCommentOperationRequest()
    {
        EntityType = "Comment";
    }

    /// <summary>
    /// Comment IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one comment ID is required")]
    [MinLength(1, ErrorMessage = "At least one comment ID is required")]
    public new List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Available operations for comments
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    [AllowedCommentOperations]
    public new string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Whether to send notifications for moderation actions
    /// </summary>
    public bool SendNotifications { get; set; } = true;

    /// <summary>
    /// Whether to update comment counts on posts
    /// </summary>
    public bool UpdatePostCommentCounts { get; set; } = true;
}

/// <summary>
/// Bulk comment status change operation
/// </summary>
public class BulkCommentStatusChangeRequest : BulkCommentOperationRequest
{
    public BulkCommentStatusChangeRequest()
    {
        Operation = BulkCommentOperations.UpdateStatus;
    }

    /// <summary>
    /// New status for the comments
    /// </summary>
    [Required(ErrorMessage = "New status is required")]
    public CommentStatus NewStatus { get; set; }

    /// <summary>
    /// Moderation reason
    /// </summary>
    [StringLength(500, ErrorMessage = "Moderation reason cannot exceed 500 characters")]
    public string? ModerationReason { get; set; }

    /// <summary>
    /// Whether to apply status to all nested replies
    /// </summary>
    public bool ApplyToReplies { get; set; } = false;

    /// <summary>
    /// Custom notification message for affected users
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notification message cannot exceed 1000 characters")]
    public string? NotificationMessage { get; set; }
}

/// <summary>
/// Bulk comment deletion operation
/// </summary>
public class BulkCommentDeleteRequest : BulkCommentOperationRequest
{
    public BulkCommentDeleteRequest()
    {
        Operation = BulkCommentOperations.Delete;
    }

    /// <summary>
    /// Whether to perform soft delete or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// How to handle nested replies
    /// </summary>
    public CommentReplyHandling ReplyHandling { get; set; } = CommentReplyHandling.PreserveReplies;

    /// <summary>
    /// Deletion reason for audit purposes
    /// </summary>
    [StringLength(500, ErrorMessage = "Deletion reason cannot exceed 500 characters")]
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Whether to notify comment authors
    /// </summary>
    public bool NotifyAuthors { get; set; } = true;

    /// <summary>
    /// Data retention period for soft-deleted comments
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Whether to create tombstone entries for deleted comments
    /// </summary>
    public bool CreateTombstones { get; set; } = true;
}

/// <summary>
/// Comment reply handling strategy for bulk operations
/// </summary>
public enum CommentReplyHandling
{
    /// <summary>
    /// Keep replies and make them top-level comments
    /// </summary>
    PreserveReplies = 0,

    /// <summary>
    /// Delete all replies along with parent
    /// </summary>
    DeleteReplies = 1,

    /// <summary>
    /// Move replies to parent's parent (promote one level)
    /// </summary>
    PromoteReplies = 2,

    /// <summary>
    /// Keep replies but mark them as orphaned
    /// </summary>
    OrphanReplies = 3
}

/// <summary>
/// Bulk comment moderation operation
/// </summary>
public class BulkCommentModerationRequest : BulkCommentOperationRequest
{
    public BulkCommentModerationRequest()
    {
        Operation = BulkCommentOperations.Moderate;
    }

    /// <summary>
    /// Moderation action to take
    /// </summary>
    [Required(ErrorMessage = "Moderation action is required")]
    public CommentModerationAction ModerationAction { get; set; }

    /// <summary>
    /// Reason for the moderation action
    /// </summary>
    [Required(ErrorMessage = "Moderation reason is required")]
    [StringLength(500, ErrorMessage = "Moderation reason cannot exceed 500 characters")]
    public string ModerationReason { get; set; } = string.Empty;

    /// <summary>
    /// Whether to apply moderation to author's other comments
    /// </summary>
    public bool ApplyToAuthorComments { get; set; } = false;

    /// <summary>
    /// Whether to add authors to moderation watchlist
    /// </summary>
    public bool AddAuthorsToWatchlist { get; set; } = false;

    /// <summary>
    /// Moderation severity level
    /// </summary>
    public ModerationSeverity Severity { get; set; } = ModerationSeverity.Normal;

    /// <summary>
    /// Custom message to send to comment authors
    /// </summary>
    [StringLength(1000, ErrorMessage = "Author message cannot exceed 1000 characters")]
    public string? AuthorMessage { get; set; }
}

/// <summary>
/// Comment moderation actions
/// </summary>
public enum CommentModerationAction
{
    /// <summary>
    /// Approve the comments
    /// </summary>
    Approve = 0,

    /// <summary>
    /// Reject the comments
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Mark as spam
    /// </summary>
    MarkAsSpam = 2,

    /// <summary>
    /// Flag for review
    /// </summary>
    FlagForReview = 3,

    /// <summary>
    /// Hide from public view
    /// </summary>
    Hide = 4,

    /// <summary>
    /// Require re-moderation
    /// </summary>
    RequireRemoderation = 5
}

/// <summary>
/// Moderation severity levels
/// </summary>
public enum ModerationSeverity
{
    /// <summary>
    /// Low severity - minor issue
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal severity - standard moderation
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High severity - significant issue
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical severity - serious violation
    /// </summary>
    Critical = 3
}

/// <summary>
/// Bulk comment move operation
/// </summary>
public class BulkCommentMoveRequest : BulkCommentOperationRequest
{
    public BulkCommentMoveRequest()
    {
        Operation = BulkCommentOperations.MoveToDifferentPost;
    }

    /// <summary>
    /// Target post ID to move comments to
    /// </summary>
    [Required(ErrorMessage = "Target post ID is required")]
    public Guid TargetPostId { get; set; }

    /// <summary>
    /// How to handle comment threading when moving
    /// </summary>
    public CommentThreadHandling ThreadHandling { get; set; } = CommentThreadHandling.PreserveStructure;

    /// <summary>
    /// Whether to notify comment authors about the move
    /// </summary>
    public bool NotifyAuthors { get; set; } = true;

    /// <summary>
    /// Reason for moving the comments
    /// </summary>
    [StringLength(500, ErrorMessage = "Move reason cannot exceed 500 characters")]
    public string? MoveReason { get; set; }
}

/// <summary>
/// Comment thread handling for move operations
/// </summary>
public enum CommentThreadHandling
{
    /// <summary>
    /// Preserve the comment thread structure
    /// </summary>
    PreserveStructure = 0,

    /// <summary>
    /// Make all comments top-level
    /// </summary>
    FlattenToTopLevel = 1,

    /// <summary>
    /// Group under a single parent comment
    /// </summary>
    GroupUnderParent = 2
}

/// <summary>
/// Bulk comment export operation
/// </summary>
public class BulkCommentExportRequest : BulkCommentOperationRequest
{
    public BulkCommentExportRequest()
    {
        Operation = BulkCommentOperations.Export;
    }

    /// <summary>
    /// Export format
    /// </summary>
    [Required(ErrorMessage = "Export format is required")]
    public CommentExportFormat Format { get; set; } = CommentExportFormat.Json;

    /// <summary>
    /// Whether to include comment metadata
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Whether to include author information
    /// </summary>
    public bool IncludeAuthorInfo { get; set; } = true;

    /// <summary>
    /// Whether to include post information
    /// </summary>
    public bool IncludePostInfo { get; set; } = true;

    /// <summary>
    /// Whether to include moderation history
    /// </summary>
    public bool IncludeModerationHistory { get; set; } = false;

    /// <summary>
    /// Whether to preserve comment thread structure
    /// </summary>
    public bool PreserveThreadStructure { get; set; } = true;

    /// <summary>
    /// Date range for comments to export
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// Whether to anonymize author data
    /// </summary>
    public bool AnonymizeAuthors { get; set; } = false;
}

/// <summary>
/// Comment export formats
/// </summary>
public enum CommentExportFormat
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
    /// Plain text format
    /// </summary>
    Text = 4
}

/// <summary>
/// Response for bulk comment operations
/// </summary>
public class BulkCommentOperationResponse : BulkOperationResponse
{
    public BulkCommentOperationResponse()
    {
        EntityType = "Comment";
    }

    /// <summary>
    /// Comments that were successfully processed
    /// </summary>
    public List<CommentSummaryDto> ProcessedComments { get; set; } = new();

    /// <summary>
    /// Comments that failed to process
    /// </summary>
    public List<FailedCommentDto> FailedComments { get; set; } = new();

    /// <summary>
    /// Export file information (for export operations)
    /// </summary>
    public ExportFileInfo? ExportFile { get; set; }

    /// <summary>
    /// Moderation summary
    /// </summary>
    public ModerationSummary? ModerationSummary { get; set; }

    /// <summary>
    /// Statistics about the operation
    /// </summary>
    public BulkCommentOperationStats Stats { get; set; } = new();
}

/// <summary>
/// Summary information for a processed comment
/// </summary>
public class CommentSummaryDto
{
    /// <summary>
    /// Comment ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Comment content preview (first 100 characters)
    /// </summary>
    public string ContentPreview { get; set; } = string.Empty;

    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Post title the comment belongs to
    /// </summary>
    public string PostTitle { get; set; } = string.Empty;

    /// <summary>
    /// Current status
    /// </summary>
    public CommentStatus Status { get; set; }

    /// <summary>
    /// Changes made to this comment
    /// </summary>
    public List<string> ChangesApplied { get; set; } = new();

    /// <summary>
    /// Whether this comment has replies
    /// </summary>
    public bool HasReplies { get; set; }
}

/// <summary>
/// Information about a comment that failed to process
/// </summary>
public class FailedCommentDto
{
    /// <summary>
    /// Comment ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Content preview (if available)
    /// </summary>
    public string? ContentPreview { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Whether this failure affected child comments
    /// </summary>
    public bool AffectedChildComments { get; set; }
}

/// <summary>
/// Moderation summary for bulk operations
/// </summary>
public class ModerationSummary
{
    /// <summary>
    /// Number of comments approved
    /// </summary>
    public int CommentsApproved { get; set; }

    /// <summary>
    /// Number of comments rejected
    /// </summary>
    public int CommentsRejected { get; set; }

    /// <summary>
    /// Number of comments marked as spam
    /// </summary>
    public int CommentsMarkedAsSpam { get; set; }

    /// <summary>
    /// Number of comments deleted
    /// </summary>
    public int CommentsDeleted { get; set; }

    /// <summary>
    /// Number of authors added to watchlist
    /// </summary>
    public int AuthorsAddedToWatchlist { get; set; }

    /// <summary>
    /// Number of notifications sent
    /// </summary>
    public int NotificationsSent { get; set; }

    /// <summary>
    /// Moderation actions by severity
    /// </summary>
    public Dictionary<ModerationSeverity, int> ActionsBySeverity { get; set; } = new();
}

/// <summary>
/// Statistics for bulk comment operations
/// </summary>
public class BulkCommentOperationStats
{
    /// <summary>
    /// Number of top-level comments processed
    /// </summary>
    public int TopLevelComments { get; set; }

    /// <summary>
    /// Number of reply comments processed
    /// </summary>
    public int ReplyComments { get; set; }

    /// <summary>
    /// Comments by status before operation
    /// </summary>
    public Dictionary<CommentStatus, int> CommentsByPreviousStatus { get; set; } = new();

    /// <summary>
    /// Comments by status after operation
    /// </summary>
    public Dictionary<CommentStatus, int> CommentsByNewStatus { get; set; } = new();

    /// <summary>
    /// Number of unique authors affected
    /// </summary>
    public int UniqueAuthorsAffected { get; set; }

    /// <summary>
    /// Number of unique posts affected
    /// </summary>
    public int UniquePostsAffected { get; set; }

    /// <summary>
    /// Total number of child comments affected
    /// </summary>
    public int ChildCommentsAffected { get; set; }
}

/// <summary>
/// Custom validation attribute for allowed comment operations
/// </summary>
public class AllowedCommentOperationsAttribute : ValidationAttribute
{
    private static readonly string[] AllowedOperations =
    {
        BulkCommentOperations.Approve,
        BulkCommentOperations.Reject,
        BulkCommentOperations.MarkAsSpam,
        BulkCommentOperations.Delete,
        BulkCommentOperations.Archive,
        BulkCommentOperations.Pin,
        BulkCommentOperations.Unpin,
        BulkCommentOperations.ChangeAuthor,
        BulkCommentOperations.MoveToDifferentPost,
        BulkCommentOperations.Export,
        BulkCommentOperations.Moderate,
        BulkCommentOperations.UpdateStatus
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