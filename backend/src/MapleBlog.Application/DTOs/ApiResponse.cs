namespace MapleBlog.Application.DTOs;

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Response data (if any)
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Validation errors (if any)
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Response timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Create a successful response
    /// </summary>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>Success response</returns>
    public static ApiResponse CreateSuccess(object? data = null, string message = "Operation completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Validation errors</param>
    /// <returns>Error response</returns>
    public static ApiResponse Error(string message, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Create a validation error response
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="message">Error message</param>
    /// <returns>Validation error response</returns>
    public static ApiResponse ValidationError(Dictionary<string, string[]> errors, string message = "Validation failed")
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Generic API response wrapper with typed data
/// </summary>
/// <typeparam name="T">Type of response data</typeparam>
public class ApiResponse<T> : ApiResponse
{
    /// <summary>
    /// Typed response data
    /// </summary>
    public new T? Data { get; set; }

    /// <summary>
    /// Create a successful response with typed data
    /// </summary>
    /// <param name="data">Response data</param>
    /// <param name="message">Success message</param>
    /// <returns>Success response</returns>
    public static ApiResponse<T> CreateSuccess(T? data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Create an error response with typed data structure
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Validation errors</param>
    /// <returns>Error response</returns>
    public static new ApiResponse<T> Error(string message, Dictionary<string, string[]>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Create a validation error response with typed data structure
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="message">Error message</param>
    /// <returns>Validation error response</returns>
    public static new ApiResponse<T> ValidationError(Dictionary<string, string[]> errors, string message = "Validation failed")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}