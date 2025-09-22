using System.Text.Json.Serialization;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Base interface for bulk operation responses
/// </summary>
public interface IBulkOperationResponse
{
    /// <summary>
    /// Operation identifier matching the request
    /// </summary>
    Guid OperationId { get; }

    /// <summary>
    /// Overall success status of the operation
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Current status of the operation
    /// </summary>
    BulkOperationStatus Status { get; }

    /// <summary>
    /// Total number of items processed
    /// </summary>
    int TotalItems { get; }

    /// <summary>
    /// Number of successfully processed items
    /// </summary>
    int SuccessCount { get; }

    /// <summary>
    /// Number of failed items
    /// </summary>
    int FailureCount { get; }

    /// <summary>
    /// Operation start time
    /// </summary>
    DateTime StartedAt { get; }

    /// <summary>
    /// Operation completion time (null if still running)
    /// </summary>
    DateTime? CompletedAt { get; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    long DurationMs { get; }
}

/// <summary>
/// Comprehensive bulk operation response
/// </summary>
/// <typeparam name="TKey">Type of the entity key</typeparam>
public class BulkOperationResponse<TKey> : IBulkOperationResponse
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Operation identifier matching the request
    /// </summary>
    public Guid OperationId { get; set; }

    /// <summary>
    /// Entity type that was operated on
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Operation that was performed
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Overall success status of the operation
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Current status of the operation
    /// </summary>
    public BulkOperationStatus Status { get; set; }

    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of successfully processed items
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed items
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Number of skipped items
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    [JsonPropertyName("progressPercentage")]
    public decimal ProgressPercentage => TotalItems > 0 ?
        Math.Round((decimal)(SuccessCount + FailureCount + SkippedCount) / TotalItems * 100, 2) : 0;

    /// <summary>
    /// Operation start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Operation completion time (null if still running)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long DurationMs => CompletedAt.HasValue ?
        (long)(CompletedAt.Value - StartedAt).TotalMilliseconds :
        (long)(DateTime.UtcNow - StartedAt).TotalMilliseconds;

    /// <summary>
    /// Estimated completion time (null if completed or cannot estimate)
    /// </summary>
    public DateTime? EstimatedCompletionAt { get; set; }

    /// <summary>
    /// Overall operation message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Detailed results for each processed item
    /// </summary>
    public List<BulkOperationItemResult<TKey>> ItemResults { get; set; } = new();

    /// <summary>
    /// Global errors not related to specific items
    /// </summary>
    public List<BulkOperationError> Errors { get; set; } = new();

    /// <summary>
    /// Warnings that don't prevent operation completion
    /// </summary>
    public List<BulkOperationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Metadata about the operation execution
    /// </summary>
    public BulkOperationMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Create a successful response
    /// </summary>
    public static BulkOperationResponse<TKey> Success(
        Guid operationId,
        string entityType,
        string operation,
        int totalItems,
        string? message = null)
    {
        return new BulkOperationResponse<TKey>
        {
            OperationId = operationId,
            EntityType = entityType,
            Operation = operation,
            IsSuccess = true,
            Status = BulkOperationStatus.Completed,
            TotalItems = totalItems,
            SuccessCount = totalItems,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Message = message ?? $"Successfully processed {totalItems} items"
        };
    }

    /// <summary>
    /// Create a failed response
    /// </summary>
    public static BulkOperationResponse<TKey> Failure(
        Guid operationId,
        string entityType,
        string operation,
        string errorMessage)
    {
        return new BulkOperationResponse<TKey>
        {
            OperationId = operationId,
            EntityType = entityType,
            Operation = operation,
            IsSuccess = false,
            Status = BulkOperationStatus.Failed,
            Message = errorMessage,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Errors = { new BulkOperationError { Message = errorMessage, ErrorCode = "BULK_OPERATION_FAILED" } }
        };
    }

    /// <summary>
    /// Create a partial success response
    /// </summary>
    public static BulkOperationResponse<TKey> PartialSuccess(
        Guid operationId,
        string entityType,
        string operation,
        int totalItems,
        int successCount,
        int failureCount,
        string? message = null)
    {
        return new BulkOperationResponse<TKey>
        {
            OperationId = operationId,
            EntityType = entityType,
            Operation = operation,
            IsSuccess = failureCount == 0,
            Status = failureCount == 0 ? BulkOperationStatus.Completed : BulkOperationStatus.CompletedWithErrors,
            TotalItems = totalItems,
            SuccessCount = successCount,
            FailureCount = failureCount,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            Message = message ?? $"Processed {totalItems} items: {successCount} succeeded, {failureCount} failed"
        };
    }
}

/// <summary>
/// Strongly-typed bulk operation response for GUID-based entities
/// </summary>
public class BulkOperationResponse : BulkOperationResponse<Guid>
{
}

/// <summary>
/// Result for a single item in a bulk operation
/// </summary>
/// <typeparam name="TKey">Type of the entity key</typeparam>
public class BulkOperationItemResult<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Entity ID that was processed
    /// </summary>
    public TKey EntityId { get; set; } = default!;

    /// <summary>
    /// Whether this item was processed successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Status of this specific item
    /// </summary>
    public BulkOperationItemStatus Status { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error details if the item failed
    /// </summary>
    public BulkOperationError? Error { get; set; }

    /// <summary>
    /// Any warnings for this item
    /// </summary>
    public List<BulkOperationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Additional data returned for this item
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>
    /// Time when this item was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Error information for bulk operations
/// </summary>
public class BulkOperationError
{
    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object?> Details { get; set; } = new();

    /// <summary>
    /// Exception type if applicable
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Stack trace for debugging (only in development)
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Warning information for bulk operations
/// </summary>
public class BulkOperationWarning
{
    /// <summary>
    /// Warning code for programmatic handling
    /// </summary>
    public string WarningCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional warning details
    /// </summary>
    public Dictionary<string, object?> Details { get; set; } = new();
}

/// <summary>
/// Metadata about bulk operation execution
/// </summary>
public class BulkOperationMetadata
{
    /// <summary>
    /// Number of batches processed
    /// </summary>
    public int BatchCount { get; set; }

    /// <summary>
    /// Average processing time per item in milliseconds
    /// </summary>
    public double AverageItemProcessingTimeMs { get; set; }

    /// <summary>
    /// Peak memory usage during operation
    /// </summary>
    public long PeakMemoryUsageBytes { get; set; }

    /// <summary>
    /// Database queries executed
    /// </summary>
    public int DatabaseQueries { get; set; }

    /// <summary>
    /// Cache hits during operation
    /// </summary>
    public int CacheHits { get; set; }

    /// <summary>
    /// Cache misses during operation
    /// </summary>
    public int CacheMisses { get; set; }

    /// <summary>
    /// Additional performance metrics
    /// </summary>
    public Dictionary<string, object?> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Status enumeration for bulk operations
/// </summary>
public enum BulkOperationStatus
{
    /// <summary>
    /// Operation is queued and waiting to start
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Operation is currently running
    /// </summary>
    Running = 1,

    /// <summary>
    /// Operation completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Operation completed but with some errors
    /// </summary>
    CompletedWithErrors = 3,

    /// <summary>
    /// Operation failed completely
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Operation was cancelled
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Operation timed out
    /// </summary>
    TimedOut = 6,

    /// <summary>
    /// Operation is paused
    /// </summary>
    Paused = 7
}

/// <summary>
/// Status enumeration for individual items in bulk operations
/// </summary>
public enum BulkOperationItemStatus
{
    /// <summary>
    /// Item is waiting to be processed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Item is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Item was processed successfully
    /// </summary>
    Success = 2,

    /// <summary>
    /// Item processing failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Item was skipped (e.g., due to business rules)
    /// </summary>
    Skipped = 4,

    /// <summary>
    /// Item was processed with warnings
    /// </summary>
    Warning = 5
}