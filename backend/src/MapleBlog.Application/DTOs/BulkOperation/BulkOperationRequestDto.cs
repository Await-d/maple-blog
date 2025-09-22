using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs.BulkOperation;

/// <summary>
/// Base interface for all bulk operations
/// </summary>
public interface IBulkOperationRequest
{
    /// <summary>
    /// Unique identifier for this bulk operation request
    /// </summary>
    Guid OperationId { get; }

    /// <summary>
    /// Entity type being operated on
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Operation type being performed
    /// </summary>
    string Operation { get; }

    /// <summary>
    /// Whether this operation should be performed in a transaction
    /// </summary>
    bool UseTransaction { get; }

    /// <summary>
    /// Whether to continue processing if some items fail
    /// </summary>
    bool ContinueOnError { get; }

    /// <summary>
    /// Maximum number of items to process in a single batch
    /// </summary>
    int BatchSize { get; }

    /// <summary>
    /// User-provided reason for this bulk operation (for audit purposes)
    /// </summary>
    string? Reason { get; }
}

/// <summary>
/// Generic bulk operation request for specific entity types
/// </summary>
/// <typeparam name="TKey">Type of the entity key (typically Guid)</typeparam>
public class BulkOperationRequest<TKey> : IBulkOperationRequest
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Unique identifier for this bulk operation request
    /// </summary>
    public Guid OperationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Entity type being operated on
    /// </summary>
    [Required(ErrorMessage = "Entity type is required")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Operation to perform on the entities
    /// </summary>
    [Required(ErrorMessage = "Operation is required")]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// List of entity IDs to operate on
    /// </summary>
    [Required(ErrorMessage = "At least one entity ID is required")]
    [MinLength(1, ErrorMessage = "At least one entity ID is required")]
    [MaxLength(10000, ErrorMessage = "Cannot process more than 10,000 items at once")]
    public List<TKey> EntityIds { get; set; } = new();

    /// <summary>
    /// Optional parameters for the operation
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
    /// Whether this operation should be performed in a transaction
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Whether to continue processing if some items fail
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Maximum number of items to process in a single batch (0 = all at once)
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Batch size must be between 0 and 1000")]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// User-provided reason for this bulk operation (for audit purposes)
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }

    /// <summary>
    /// Client information for audit trail
    /// </summary>
    public BulkOperationClientInfo? ClientInfo { get; set; }

    /// <summary>
    /// Priority level for the operation
    /// </summary>
    public BulkOperationPriority Priority { get; set; } = BulkOperationPriority.Normal;

    /// <summary>
    /// When this operation should be executed (null = immediately)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Tags for categorizing this operation
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Strongly-typed bulk operation request for GUID-based entities
/// </summary>
public class BulkOperationRequest : BulkOperationRequest<Guid>
{
}

/// <summary>
/// Client information for bulk operations
/// </summary>
public class BulkOperationClientInfo
{
    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Application or source initiating the operation
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Session or request identifier
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Priority levels for bulk operations
/// </summary>
public enum BulkOperationPriority
{
    /// <summary>
    /// Low priority - can be delayed
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard processing
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - process quickly
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - immediate processing
    /// </summary>
    Critical = 3
}

/// <summary>
/// Specialized bulk operation request for operations requiring additional data
/// </summary>
/// <typeparam name="TKey">Type of the entity key</typeparam>
/// <typeparam name="TData">Type of additional data per entity</typeparam>
public class BulkOperationRequestWithData<TKey, TData> : BulkOperationRequest<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Data associated with each entity ID
    /// </summary>
    [Required(ErrorMessage = "Entity data is required")]
    public Dictionary<TKey, TData> EntityData { get; set; } = new();

    /// <summary>
    /// Override the EntityIds to be consistent with EntityData keys
    /// </summary>
    public new List<TKey> EntityIds
    {
        get => EntityData.Keys.ToList();
        set => throw new NotSupportedException("Use EntityData instead of EntityIds for operations with data");
    }
}

/// <summary>
/// Bulk operation request with typed parameters
/// </summary>
/// <typeparam name="TKey">Type of the entity key</typeparam>
/// <typeparam name="TParameters">Type of the operation parameters</typeparam>
public class BulkOperationRequest<TKey, TParameters> : BulkOperationRequest<TKey>
    where TKey : IEquatable<TKey>
    where TParameters : class
{
    /// <summary>
    /// Strongly-typed parameters for the operation
    /// </summary>
    [Required(ErrorMessage = "Operation parameters are required")]
    public TParameters TypedParameters { get; set; } = default!;

    /// <summary>
    /// Override Parameters to be read-only and derived from TypedParameters
    /// </summary>
    public new Dictionary<string, object?> Parameters =>
        TypedParameters?.GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(TypedParameters)) ?? new();
}