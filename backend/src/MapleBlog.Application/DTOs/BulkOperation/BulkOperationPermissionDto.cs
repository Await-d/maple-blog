using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Permission requirements for bulk operations
/// </summary>
public class BulkOperationPermission
{
    /// <summary>
    /// Entity type being operated on
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Operation being performed
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Required system permissions
    /// </summary>
    public List<string> RequiredPermissions { get; set; } = new();

    /// <summary>
    /// Required user roles
    /// </summary>
    public List<UserRoleEnum> RequiredRoles { get; set; } = new();

    /// <summary>
    /// Minimum role level required
    /// </summary>
    public UserRoleEnum? MinimumRole { get; set; }

    /// <summary>
    /// Whether the operation requires elevated permissions
    /// </summary>
    public bool RequiresElevatedPermissions { get; set; }

    /// <summary>
    /// Whether the operation requires confirmation
    /// </summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// Maximum number of items allowed for this operation
    /// </summary>
    public int? MaxItemLimit { get; set; }

    /// <summary>
    /// Whether the operation is restricted by time of day
    /// </summary>
    public TimeRestriction? TimeRestriction { get; set; }

    /// <summary>
    /// Whether the operation requires audit approval
    /// </summary>
    public bool RequiresAuditApproval { get; set; }

    /// <summary>
    /// Custom permission validators
    /// </summary>
    public List<string> CustomValidators { get; set; } = new();
}

/// <summary>
/// Time restriction for bulk operations
/// </summary>
public class TimeRestriction
{
    /// <summary>
    /// Allowed start time (UTC)
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// Allowed end time (UTC)
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Allowed days of week
    /// </summary>
    public List<DayOfWeek> AllowedDays { get; set; } = new();

    /// <summary>
    /// Whether weekends are excluded
    /// </summary>
    public bool ExcludeWeekends { get; set; }

    /// <summary>
    /// Whether holidays are excluded
    /// </summary>
    public bool ExcludeHolidays { get; set; }
}

/// <summary>
/// Bulk operation permission check request
/// </summary>
public class BulkOperationPermissionCheckRequest
{
    /// <summary>
    /// User ID requesting the operation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Bulk operation request to check permissions for
    /// </summary>
    public IBulkOperationRequest OperationRequest { get; set; } = default!;

    /// <summary>
    /// Current user's roles
    /// </summary>
    public UserRoleEnum UserRoles { get; set; }

    /// <summary>
    /// Current user's permissions
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    /// <summary>
    /// Client context information
    /// </summary>
    public BulkOperationClientInfo? ClientInfo { get; set; }

    /// <summary>
    /// Whether this is a preview/dry-run check
    /// </summary>
    public bool IsPreview { get; set; }
}

/// <summary>
/// Bulk operation permission check result
/// </summary>
public class BulkOperationPermissionCheckResult
{
    /// <summary>
    /// Whether the operation is allowed
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// Reasons why the operation is not allowed
    /// </summary>
    public List<string> DenialReasons { get; set; } = new();

    /// <summary>
    /// Missing permissions required for the operation
    /// </summary>
    public List<string> MissingPermissions { get; set; } = new();

    /// <summary>
    /// Missing roles required for the operation
    /// </summary>
    public List<UserRoleEnum> MissingRoles { get; set; } = new();

    /// <summary>
    /// Whether confirmation is required
    /// </summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// Confirmation token if required
    /// </summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Whether audit approval is required
    /// </summary>
    public bool RequiresAuditApproval { get; set; }

    /// <summary>
    /// Maximum number of items allowed in this operation
    /// </summary>
    public int? MaxAllowedItems { get; set; }

    /// <summary>
    /// Warnings about the operation
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Additional context for the permission check
    /// </summary>
    public Dictionary<string, object?> Context { get; set; } = new();

    /// <summary>
    /// Create a successful permission check result
    /// </summary>
    public static BulkOperationPermissionCheckResult Allow()
    {
        return new BulkOperationPermissionCheckResult { IsAllowed = true };
    }

    /// <summary>
    /// Create a denied permission check result
    /// </summary>
    public static BulkOperationPermissionCheckResult Deny(string reason)
    {
        return new BulkOperationPermissionCheckResult
        {
            IsAllowed = false,
            DenialReasons = { reason }
        };
    }

    /// <summary>
    /// Create a denied permission check result with missing permissions
    /// </summary>
    public static BulkOperationPermissionCheckResult DenyMissingPermissions(
        IEnumerable<string> missingPermissions)
    {
        return new BulkOperationPermissionCheckResult
        {
            IsAllowed = false,
            MissingPermissions = missingPermissions.ToList(),
            DenialReasons = { "Insufficient permissions" }
        };
    }
}

/// <summary>
/// Bulk operation entity permission check request
/// </summary>
public class BulkOperationEntityPermissionCheckRequest
{
    /// <summary>
    /// User ID requesting the operation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Entity IDs to check permissions for
    /// </summary>
    public List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Entity type
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Operation to perform
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Current user's roles
    /// </summary>
    public UserRoleEnum UserRoles { get; set; }

    /// <summary>
    /// Current user's permissions
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();
}

/// <summary>
/// Bulk operation entity permission check result
/// </summary>
public class BulkOperationEntityPermissionCheckResult
{
    /// <summary>
    /// Entity IDs that are allowed to be operated on
    /// </summary>
    public List<Guid> AllowedEntityIds { get; set; } = new();

    /// <summary>
    /// Entity IDs that are denied
    /// </summary>
    public List<BulkOperationEntityDenial> DeniedEntities { get; set; } = new();

    /// <summary>
    /// Whether all entities are allowed
    /// </summary>
    public bool AllEntitiesAllowed => DeniedEntities.Count == 0;

    /// <summary>
    /// Number of allowed entities
    /// </summary>
    public int AllowedCount => AllowedEntityIds.Count;

    /// <summary>
    /// Number of denied entities
    /// </summary>
    public int DeniedCount => DeniedEntities.Count;
}

/// <summary>
/// Information about a denied entity in bulk operations
/// </summary>
public class BulkOperationEntityDenial
{
    /// <summary>
    /// Entity ID that was denied
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Reason for denial
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Additional context about the denial
    /// </summary>
    public Dictionary<string, object?> Context { get; set; } = new();
}

/// <summary>
/// Bulk operation audit entry
/// </summary>
public class BulkOperationAuditEntry
{
    /// <summary>
    /// Unique identifier for the audit entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Operation ID from the bulk operation request
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// User who performed the operation
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Entity type operated on
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Operation performed
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Number of entities affected
    /// </summary>
    public int EntityCount { get; set; }

    /// <summary>
    /// Entity IDs that were operated on
    /// </summary>
    public List<Guid> EntityIds { get; set; } = new();

    /// <summary>
    /// Parameters used in the operation
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
    /// Operation start time
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Success and failure counts
    /// </summary>
    public BulkOperationAuditCounts Counts { get; set; } = new();

    /// <summary>
    /// Error messages if the operation failed
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Reason provided for the operation
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Client information
    /// </summary>
    public BulkOperationClientInfo? ClientInfo { get; set; }

    /// <summary>
    /// Permission context at the time of operation
    /// </summary>
    public BulkOperationPermissionContext? PermissionContext { get; set; }

    /// <summary>
    /// Additional audit metadata
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Audit counts for bulk operations
/// </summary>
public class BulkOperationAuditCounts
{
    /// <summary>
    /// Number of successfully processed entities
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed entities
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of skipped entities
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Total entities attempted
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Permission context for audit purposes
/// </summary>
public class BulkOperationPermissionContext
{
    /// <summary>
    /// User roles at the time of operation
    /// </summary>
    public UserRoleEnum UserRoles { get; set; }

    /// <summary>
    /// User permissions at the time of operation
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    /// <summary>
    /// Whether elevated permissions were used
    /// </summary>
    public bool UsedElevatedPermissions { get; set; }

    /// <summary>
    /// Confirmation token used (if any)
    /// </summary>
    public string? ConfirmationToken { get; set; }

    /// <summary>
    /// Whether audit approval was obtained
    /// </summary>
    public bool HadAuditApproval { get; set; }

    /// <summary>
    /// Permission check timestamp
    /// </summary>
    public DateTime PermissionCheckAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bulk operation security context
/// </summary>
public class BulkOperationSecurityContext
{
    /// <summary>
    /// Current user ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current user roles
    /// </summary>
    public UserRoleEnum UserRoles { get; set; }

    /// <summary>
    /// Current user permissions
    /// </summary>
    public List<string> UserPermissions { get; set; } = new();

    /// <summary>
    /// Whether the user has administrative privileges
    /// </summary>
    public bool IsAdmin => UserRoles.HasRole(UserRoleEnum.Admin | UserRoleEnum.SuperAdmin);

    /// <summary>
    /// Whether the user can perform privileged operations
    /// </summary>
    public bool CanPerformPrivilegedOperations => IsAdmin || UserRoles.HasRole(UserRoleEnum.Moderator);

    /// <summary>
    /// Security restrictions for this user
    /// </summary>
    public List<string> SecurityRestrictions { get; set; } = new();

    /// <summary>
    /// Maximum entities allowed per operation for this user
    /// </summary>
    public int? MaxEntitiesPerOperation { get; set; }

    /// <summary>
    /// Operations that require additional confirmation for this user
    /// </summary>
    public List<string> OperationsRequiringConfirmation { get; set; } = new();

    /// <summary>
    /// Client context for security validation
    /// </summary>
    public BulkOperationClientInfo? ClientInfo { get; set; }
}