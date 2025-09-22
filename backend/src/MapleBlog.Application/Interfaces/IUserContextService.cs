namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Service for accessing current user context information
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>
        /// Gets the current user ID
        /// </summary>
        /// <returns>Current user ID, or null if not authenticated</returns>
        Guid? GetCurrentUserId();

        /// <summary>
        /// Gets the current user ID or throws if not authenticated
        /// </summary>
        /// <returns>Current user ID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
        Guid GetRequiredUserId();

        /// <summary>
        /// Gets the current username
        /// </summary>
        /// <returns>Current username, or null if not authenticated</returns>
        string? GetCurrentUserName();

        /// <summary>
        /// Gets the current user's role
        /// </summary>
        /// <returns>Current user role, or null if not authenticated</returns>
        string? GetCurrentUserRole();

        /// <summary>
        /// Gets the current user's email
        /// </summary>
        /// <returns>Current user email, or null if not authenticated</returns>
        string? GetCurrentUserEmail();

        /// <summary>
        /// Gets the current user entity
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current user entity, or null if not authenticated</returns>
        Task<MapleBlog.Domain.Entities.User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Checks if the current user is in the specified role
        /// </summary>
        /// <param name="role">Role to check</param>
        /// <returns>True if user is in role, false otherwise</returns>
        bool IsInRole(string role);

        /// <summary>
        /// Checks if the current user is an admin
        /// </summary>
        /// <returns>True if user is admin, false otherwise</returns>
        bool IsAdmin { get; }

        /// <summary>
        /// Gets the client IP address
        /// </summary>
        /// <returns>Client IP address, or null if not available</returns>
        string? GetClientIpAddress();

        /// <summary>
        /// Gets the user agent string
        /// </summary>
        /// <returns>User agent string, or null if not available</returns>
        string? GetUserAgent();

        /// <summary>
        /// Gets all user claims
        /// </summary>
        /// <returns>Dictionary of claim types to values</returns>
        Dictionary<string, string> GetUserClaims();

        /// <summary>
        /// Gets a specific claim value
        /// </summary>
        /// <param name="claimType">Type of claim to retrieve</param>
        /// <returns>Claim value, or null if not found</returns>
        string? GetClaimValue(string claimType);

        /// <summary>
        /// Sets the current user context (for background operations)
        /// </summary>
        /// <param name="userId">User ID to set</param>
        /// <param name="userName">Username to set</param>
        /// <param name="userRole">User role to set</param>
        void SetUserContext(Guid userId, string userName, string userRole);

        /// <summary>
        /// Clears the current user context
        /// </summary>
        void ClearUserContext();
    }
}