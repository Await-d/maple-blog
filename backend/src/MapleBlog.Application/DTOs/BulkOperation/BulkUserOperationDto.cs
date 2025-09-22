using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Constants for user bulk operations
/// </summary>
public static class BulkUserOperations
{
    public const string Activate = "activate";
    public const string Deactivate = "deactivate";
    public const string Delete = "delete";
    public const string ChangeRole = "change_role";
    public const string ResetPassword = "reset_password";
    public const string SendEmailVerification = "send_email_verification";
    public const string Lock = "lock";
    public const string Unlock = "unlock";
    public const string Export = "export";
}

/// <summary>
/// Bulk user operation request
/// </summary>
public class BulkUserOperationRequest : BulkOperationRequest
{
    public BulkUserOperationRequest()
    {
        EntityType = "User";
    }

    /// <summary>
    /// User IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one user ID is required")]
    [MinLength(1, ErrorMessage = "At least one user ID is required")]
    public new List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Available operations for users
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    [AllowedUserOperations]
    public new string Operation { get; set; } = string.Empty;
}

/// <summary>
/// Bulk user role change operation
/// </summary>
public class BulkUserRoleChangeRequest : BulkUserOperationRequest
{
    public BulkUserRoleChangeRequest()
    {
        Operation = BulkUserOperations.ChangeRole;
    }

    /// <summary>
    /// New role to assign to users
    /// </summary>
    [Required(ErrorMessage = "New role is required")]
    public UserRole NewRole { get; set; }

    /// <summary>
    /// Whether to preserve existing roles or replace them
    /// </summary>
    public bool PreserveExistingRoles { get; set; } = false;

    /// <summary>
    /// Roles to preserve when not preserving all existing roles
    /// </summary>
    public List<UserRole> RolesToPreserve { get; set; } = new();
}

/// <summary>
/// Bulk user password reset operation
/// </summary>
public class BulkUserPasswordResetRequest : BulkUserOperationRequest
{
    public BulkUserPasswordResetRequest()
    {
        Operation = BulkUserOperations.ResetPassword;
    }

    /// <summary>
    /// Whether to send password reset emails
    /// </summary>
    public bool SendEmail { get; set; } = true;

    /// <summary>
    /// Custom email template to use
    /// </summary>
    public string? EmailTemplate { get; set; }

    /// <summary>
    /// Whether to generate temporary passwords
    /// </summary>
    public bool GenerateTemporaryPasswords { get; set; } = false;

    /// <summary>
    /// Password expiry time for temporary passwords
    /// </summary>
    public TimeSpan? TemporaryPasswordExpiry { get; set; } = TimeSpan.FromDays(7);
}

/// <summary>
/// Bulk user deletion operation with additional options
/// </summary>
public class BulkUserDeleteRequest : BulkUserOperationRequest
{
    public BulkUserDeleteRequest()
    {
        Operation = BulkUserOperations.Delete;
    }

    /// <summary>
    /// Whether to perform soft delete (mark as inactive) or hard delete
    /// </summary>
    public bool SoftDelete { get; set; } = true;

    /// <summary>
    /// Whether to anonymize user data instead of deleting
    /// </summary>
    public bool AnonymizeData { get; set; } = false;

    /// <summary>
    /// How to handle user's content (posts, comments)
    /// </summary>
    public UserContentHandling ContentHandling { get; set; } = UserContentHandling.Preserve;

    /// <summary>
    /// Whether to send deletion notification to users
    /// </summary>
    public bool SendNotification { get; set; } = false;

    /// <summary>
    /// Data retention period for soft-deleted users
    /// </summary>
    public TimeSpan? RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// User content handling strategy for bulk deletion
/// </summary>
public enum UserContentHandling
{
    /// <summary>
    /// Keep all user content (posts, comments)
    /// </summary>
    Preserve = 0,

    /// <summary>
    /// Transfer content to another user
    /// </summary>
    Transfer = 1,

    /// <summary>
    /// Anonymize content (replace user info with anonymous)
    /// </summary>
    Anonymize = 2,

    /// <summary>
    /// Delete all user content
    /// </summary>
    Delete = 3
}

/// <summary>
/// Bulk user export operation
/// </summary>
public class BulkUserExportRequest : BulkUserOperationRequest
{
    public BulkUserExportRequest()
    {
        Operation = BulkUserOperations.Export;
    }

    /// <summary>
    /// Export format
    /// </summary>
    [Required(ErrorMessage = "Export format is required")]
    public UserExportFormat Format { get; set; } = UserExportFormat.Csv;

    /// <summary>
    /// Fields to include in export
    /// </summary>
    public List<string> IncludeFields { get; set; } = new();

    /// <summary>
    /// Whether to include user's posts in export
    /// </summary>
    public bool IncludePosts { get; set; } = false;

    /// <summary>
    /// Whether to include user's comments in export
    /// </summary>
    public bool IncludeComments { get; set; } = false;

    /// <summary>
    /// Date range for included content
    /// </summary>
    public DateRange? ContentDateRange { get; set; }

    /// <summary>
    /// Whether to encrypt exported data
    /// </summary>
    public bool EncryptExport { get; set; } = false;

    /// <summary>
    /// Compression level for export file
    /// </summary>
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
}

/// <summary>
/// User export formats
/// </summary>
public enum UserExportFormat
{
    /// <summary>
    /// Comma-separated values
    /// </summary>
    Csv = 0,

    /// <summary>
    /// JSON format
    /// </summary>
    Json = 1,

    /// <summary>
    /// Excel spreadsheet
    /// </summary>
    Excel = 2,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 3
}

/// <summary>
/// Compression levels for exports
/// </summary>
public enum CompressionLevel
{
    /// <summary>
    /// No compression
    /// </summary>
    None = 0,

    /// <summary>
    /// Fastest compression
    /// </summary>
    Fastest = 1,

    /// <summary>
    /// Optimal balance of speed and size
    /// </summary>
    Optimal = 2,

    /// <summary>
    /// Maximum compression
    /// </summary>
    Maximum = 3
}

/// <summary>
/// Date range specification
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date (inclusive)
    /// </summary>
    public DateTime From { get; set; }

    /// <summary>
    /// End date (inclusive)
    /// </summary>
    public DateTime To { get; set; }

    /// <summary>
    /// Validates the date range
    /// </summary>
    public bool IsValid => From <= To;
}

/// <summary>
/// Response for bulk user operations
/// </summary>
public class BulkUserOperationResponse : BulkOperationResponse
{
    public BulkUserOperationResponse()
    {
        EntityType = "User";
    }

    /// <summary>
    /// Users that were successfully processed
    /// </summary>
    public List<UserSummaryDto> ProcessedUsers { get; set; } = new();

    /// <summary>
    /// Users that failed to process
    /// </summary>
    public List<FailedUserDto> FailedUsers { get; set; } = new();

    /// <summary>
    /// Export file information (for export operations)
    /// </summary>
    public ExportFileInfo? ExportFile { get; set; }

    /// <summary>
    /// Statistics about the operation
    /// </summary>
    public BulkUserOperationStats Stats { get; set; } = new();
}

/// <summary>
/// Summary information for a processed user
/// </summary>
public class UserSummaryDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's current role
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Whether user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Changes made to this user
    /// </summary>
    public List<string> ChangesApplied { get; set; } = new();
}

/// <summary>
/// Information about a user that failed to process
/// </summary>
public class FailedUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Username (if available)
    /// </summary>
    public string? UserName { get; set; }

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
/// Export file information
/// </summary>
public class ExportFileInfo
{
    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Download URL or identifier
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// File expiry time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// MIME type
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
}

/// <summary>
/// Statistics for bulk user operations
/// </summary>
public class BulkUserOperationStats
{
    /// <summary>
    /// Number of admin users processed
    /// </summary>
    public int AdminUsers { get; set; }

    /// <summary>
    /// Number of moderator users processed
    /// </summary>
    public int ModeratorUsers { get; set; }

    /// <summary>
    /// Number of author users processed
    /// </summary>
    public int AuthorUsers { get; set; }

    /// <summary>
    /// Number of regular users processed
    /// </summary>
    public int RegularUsers { get; set; }

    /// <summary>
    /// Number of users that were already in the target state
    /// </summary>
    public int UsersAlreadyInTargetState { get; set; }

    /// <summary>
    /// Number of users with dependencies that prevented the operation
    /// </summary>
    public int UsersWithDependencies { get; set; }
}

/// <summary>
/// Custom validation attribute for allowed user operations
/// </summary>
public class AllowedUserOperationsAttribute : ValidationAttribute
{
    private static readonly string[] AllowedOperations =
    {
        BulkUserOperations.Activate,
        BulkUserOperations.Deactivate,
        BulkUserOperations.Delete,
        BulkUserOperations.ChangeRole,
        BulkUserOperations.ResetPassword,
        BulkUserOperations.SendEmailVerification,
        BulkUserOperations.Lock,
        BulkUserOperations.Unlock,
        BulkUserOperations.Export
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