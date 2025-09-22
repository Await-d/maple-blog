using MapleBlog.Application.DTOs.BulkOperation;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Service for handling bulk operations across different entity types
/// </summary>
public interface IBulkOperationService
{
    /// <summary>
    /// Execute a bulk operation asynchronously
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <param name="securityContext">Security context for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    Task<BulkOperationResponse> ExecuteAsync(
        IBulkOperationRequest request,
        BulkOperationSecurityContext securityContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a bulk operation with progress tracking
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <param name="securityContext">Security context for the operation</param>
    /// <param name="progressCallback">Progress callback for real-time updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation response</returns>
    Task<BulkOperationResponse> ExecuteWithProgressAsync(
        IBulkOperationRequest request,
        BulkOperationSecurityContext securityContext,
        IProgress<BulkOperationProgress>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check permissions for a bulk operation
    /// </summary>
    /// <param name="checkRequest">Permission check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission check result</returns>
    Task<BulkOperationPermissionCheckResult> CheckPermissionsAsync(
        BulkOperationPermissionCheckRequest checkRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check entity-level permissions for a bulk operation
    /// </summary>
    /// <param name="checkRequest">Entity permission check request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity permission check result</returns>
    Task<BulkOperationEntityPermissionCheckResult> CheckEntityPermissionsAsync(
        BulkOperationEntityPermissionCheckRequest checkRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a running bulk operation
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current operation status</returns>
    Task<BulkOperationResponse?> GetOperationStatusAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running bulk operation
    /// </summary>
    /// <param name="operationId">Operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelOperationAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a bulk operation request
    /// </summary>
    /// <param name="request">Bulk operation request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<BulkOperationValidationResult> ValidateAsync(
        IBulkOperationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview the effects of a bulk operation without executing it
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <param name="securityContext">Security context for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview result</returns>
    Task<BulkOperationPreviewResult> PreviewAsync(
        IBulkOperationRequest request,
        BulkOperationSecurityContext securityContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a bulk operation for later execution
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <param name="securityContext">Security context for the operation</param>
    /// <param name="scheduledAt">When to execute the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scheduled operation ID</returns>
    Task<Guid> ScheduleAsync(
        IBulkOperationRequest request,
        BulkOperationSecurityContext securityContext,
        DateTime scheduledAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit history for bulk operations
    /// </summary>
    /// <param name="filter">Audit filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit entries</returns>
    Task<PagedResult<BulkOperationAuditEntry>> GetAuditHistoryAsync(
        BulkOperationAuditFilter filter,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for handling specific entity type bulk operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IBulkOperationService<TEntity, TRequest, TResponse>
    where TRequest : class, IBulkOperationRequest
    where TResponse : class, IBulkOperationResponse
{
    /// <summary>
    /// Execute a bulk operation for a specific entity type
    /// </summary>
    /// <param name="request">Typed bulk operation request</param>
    /// <param name="securityContext">Security context for the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Typed bulk operation response</returns>
    Task<TResponse> ExecuteAsync(
        TRequest request,
        BulkOperationSecurityContext securityContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate entities before bulk operation
    /// </summary>
    /// <param name="entityIds">Entity IDs to validate</param>
    /// <param name="operation">Operation to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<BulkOperationEntityValidationResult> ValidateEntitiesAsync(
        List<Guid> entityIds,
        string operation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress information for bulk operations
/// </summary>
public class BulkOperationProgress
{
    /// <summary>
    /// Operation ID
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Current stage of the operation
    /// </summary>
    public string CurrentStage { get; set; } = string.Empty;

    /// <summary>
    /// Items processed so far
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Total items to process
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public decimal ProgressPercentage => TotalItems > 0 ?
        Math.Round((decimal)ItemsProcessed / TotalItems * 100, 2) : 0;

    /// <summary>
    /// Items processed successfully
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Items that failed to process
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Current item being processed
    /// </summary>
    public string? CurrentItem { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Processing rate (items per second)
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Additional progress metadata
    /// </summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>
    /// Timestamp of this progress update
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation result for bulk operations
/// </summary>
public class BulkOperationValidationResult
{
    /// <summary>
    /// Whether the request is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Entity-specific validation results
    /// </summary>
    public Dictionary<Guid, List<string>> EntityErrors { get; set; } = new();

    /// <summary>
    /// Estimated operation impact
    /// </summary>
    public BulkOperationImpactEstimate? ImpactEstimate { get; set; }
}

/// <summary>
/// Entity validation result for bulk operations
/// </summary>
public class BulkOperationEntityValidationResult
{
    /// <summary>
    /// Valid entity IDs
    /// </summary>
    public List<Guid> ValidEntityIds { get; set; } = new();

    /// <summary>
    /// Invalid entity IDs with reasons
    /// </summary>
    public Dictionary<Guid, List<string>> InvalidEntities { get; set; } = new();

    /// <summary>
    /// Entity metadata for valid entities
    /// </summary>
    public Dictionary<Guid, object> EntityMetadata { get; set; } = new();

    /// <summary>
    /// Whether all entities are valid
    /// </summary>
    public bool AllEntitiesValid => InvalidEntities.Count == 0;
}

/// <summary>
/// Preview result for bulk operations
/// </summary>
public class BulkOperationPreviewResult
{
    /// <summary>
    /// Operation ID for the preview
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Estimated items to be processed
    /// </summary>
    public int EstimatedItemsToProcess { get; set; }

    /// <summary>
    /// Estimated duration
    /// </summary>
    public TimeSpan EstimatedDuration { get; set; }

    /// <summary>
    /// Changes that would be made
    /// </summary>
    public List<BulkOperationChange> PendingChanges { get; set; } = new();

    /// <summary>
    /// Items that would be affected
    /// </summary>
    public List<Guid> AffectedEntityIds { get; set; } = new();

    /// <summary>
    /// Potential issues or warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Impact estimate
    /// </summary>
    public BulkOperationImpactEstimate ImpactEstimate { get; set; } = new();

    /// <summary>
    /// Whether the preview indicates a safe operation
    /// </summary>
    public bool IsSafeOperation { get; set; }

    /// <summary>
    /// Recommendations for the operation
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Represents a change that would be made in a bulk operation
/// </summary>
public class BulkOperationChange
{
    /// <summary>
    /// Entity ID that would be changed
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Type of change
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Field or property being changed
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Current value
    /// </summary>
    public object? CurrentValue { get; set; }

    /// <summary>
    /// New value after change
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this change is reversible
    /// </summary>
    public bool IsReversible { get; set; } = true;

    /// <summary>
    /// Risk level of this change
    /// </summary>
    public BulkOperationRiskLevel RiskLevel { get; set; } = BulkOperationRiskLevel.Low;
}

/// <summary>
/// Impact estimate for bulk operations
/// </summary>
public class BulkOperationImpactEstimate
{
    /// <summary>
    /// Overall risk level
    /// </summary>
    public BulkOperationRiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Estimated processing time
    /// </summary>
    public TimeSpan EstimatedProcessingTime { get; set; }

    /// <summary>
    /// Database impact estimate
    /// </summary>
    public DatabaseImpactEstimate DatabaseImpact { get; set; } = new();

    /// <summary>
    /// Cache impact estimate
    /// </summary>
    public CacheImpactEstimate CacheImpact { get; set; } = new();

    /// <summary>
    /// Search index impact estimate
    /// </summary>
    public SearchIndexImpactEstimate SearchIndexImpact { get; set; } = new();

    /// <summary>
    /// User impact estimate
    /// </summary>
    public UserImpactEstimate UserImpact { get; set; } = new();

    /// <summary>
    /// System resource usage estimate
    /// </summary>
    public ResourceUsageEstimate ResourceUsage { get; set; } = new();
}

/// <summary>
/// Risk levels for bulk operations
/// </summary>
public enum BulkOperationRiskLevel
{
    /// <summary>
    /// Low risk operation
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium risk operation
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High risk operation
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical risk operation
    /// </summary>
    Critical = 3
}

/// <summary>
/// Database impact estimate
/// </summary>
public class DatabaseImpactEstimate
{
    /// <summary>
    /// Estimated number of database queries
    /// </summary>
    public int EstimatedQueries { get; set; }

    /// <summary>
    /// Estimated database load impact (0.0 to 1.0)
    /// </summary>
    public double LoadImpact { get; set; }

    /// <summary>
    /// Whether long-running transactions are expected
    /// </summary>
    public bool HasLongRunningTransactions { get; set; }

    /// <summary>
    /// Tables that will be affected
    /// </summary>
    public List<string> AffectedTables { get; set; } = new();
}

/// <summary>
/// Cache impact estimate
/// </summary>
public class CacheImpactEstimate
{
    /// <summary>
    /// Cache regions that will be affected
    /// </summary>
    public List<string> AffectedRegions { get; set; } = new();

    /// <summary>
    /// Estimated cache invalidation impact (0.0 to 1.0)
    /// </summary>
    public double InvalidationImpact { get; set; }

    /// <summary>
    /// Whether full cache clear is needed
    /// </summary>
    public bool RequiresFullCacheClear { get; set; }
}

/// <summary>
/// Search index impact estimate
/// </summary>
public class SearchIndexImpactEstimate
{
    /// <summary>
    /// Estimated documents to be updated in search index
    /// </summary>
    public int EstimatedDocumentUpdates { get; set; }

    /// <summary>
    /// Whether full reindex is needed
    /// </summary>
    public bool RequiresFullReindex { get; set; }

    /// <summary>
    /// Search indexes that will be affected
    /// </summary>
    public List<string> AffectedIndexes { get; set; } = new();
}

/// <summary>
/// User impact estimate
/// </summary>
public class UserImpactEstimate
{
    /// <summary>
    /// Number of users potentially affected
    /// </summary>
    public int PotentiallyAffectedUsers { get; set; }

    /// <summary>
    /// Whether user notifications will be sent
    /// </summary>
    public bool WillSendNotifications { get; set; }

    /// <summary>
    /// User-facing changes that will occur
    /// </summary>
    public List<string> UserFacingChanges { get; set; } = new();
}

/// <summary>
/// Resource usage estimate
/// </summary>
public class ResourceUsageEstimate
{
    /// <summary>
    /// Estimated peak memory usage in MB
    /// </summary>
    public long EstimatedPeakMemoryMB { get; set; }

    /// <summary>
    /// Estimated CPU usage percentage
    /// </summary>
    public double EstimatedCpuUsage { get; set; }

    /// <summary>
    /// Estimated network bandwidth usage in MB
    /// </summary>
    public long EstimatedNetworkUsageMB { get; set; }

    /// <summary>
    /// Whether the operation is resource-intensive
    /// </summary>
    public bool IsResourceIntensive { get; set; }
}

/// <summary>
/// Filter for bulk operation audit queries
/// </summary>
public class BulkOperationAuditFilter
{
    /// <summary>
    /// User ID filter
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Entity type filter
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Operation filter
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Date range filter
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// Success status filter
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Minimum entity count filter
    /// </summary>
    public int? MinEntityCount { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "StartedAt";

    /// <summary>
    /// Sort direction
    /// </summary>
    public string SortOrder { get; set; } = "DESC";
}

/// <summary>
/// Paginated result wrapper
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items in current page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious { get; set; }
}