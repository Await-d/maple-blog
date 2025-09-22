using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Services
{
    /// <summary>
    /// Service for accessing current user context information from HTTP context
    /// </summary>
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ThreadLocal<UserContextData?> _backgroundUserContext;
        private readonly IRepository<User> _userRepository;

        public UserContextService(IHttpContextAccessor httpContextAccessor, IRepository<User> userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _backgroundUserContext = new ThreadLocal<UserContextData?>();
        }

        /// <summary>
        /// Gets the current user ID
        /// </summary>
        public Guid? GetCurrentUserId()
        {
            // First check background context for background operations
            if (_backgroundUserContext.Value?.UserId != null)
            {
                return _backgroundUserContext.Value.UserId;
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        /// <summary>
        /// Gets the current user ID or throws if not authenticated
        /// </summary>
        public Guid GetRequiredUserId()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User must be authenticated to perform this operation");

            return userId.Value;
        }

        /// <summary>
        /// Gets the current username
        /// </summary>
        public string? GetCurrentUserName()
        {
            // First check background context
            if (!string.IsNullOrEmpty(_backgroundUserContext.Value?.UserName))
            {
                return _backgroundUserContext.Value.UserName;
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// Gets the current user's role
        /// </summary>
        public string? GetCurrentUserRole()
        {
            // First check background context
            if (!string.IsNullOrEmpty(_backgroundUserContext.Value?.UserRole))
            {
                return _backgroundUserContext.Value.UserRole;
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Gets the current user's email
        /// </summary>
        public string? GetCurrentUserEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Gets the current user entity
        /// </summary>
        public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return null;

            return await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        }

        /// <summary>
        /// Checks if the current user is authenticated
        /// </summary>
        public bool IsAuthenticated =>
            _backgroundUserContext.Value?.UserId != null ||
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

        /// <summary>
        /// Checks if the current user is in the specified role
        /// </summary>
        public bool IsInRole(string role)
        {
            // Check background context first
            if (_backgroundUserContext.Value?.UserRole == role)
                return true;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            return user.IsInRole(role);
        }

        /// <summary>
        /// Checks if the current user is an admin
        /// </summary>
        public bool IsAdmin => IsInRole(MapleBlog.Domain.Enums.UserRole.Admin.ToString());

        /// <summary>
        /// Gets the client IP address
        /// </summary>
        public string? GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return null;

            // Check for forwarded IP first (common in load balancer scenarios)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // Take the first IP if multiple are present
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            // Fall back to connection remote IP
            return context.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Gets the user agent string
        /// </summary>
        public string? GetUserAgent()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Headers["User-Agent"].FirstOrDefault();
        }

        /// <summary>
        /// Gets all user claims
        /// </summary>
        public Dictionary<string, string> GetUserClaims()
        {
            var claims = new Dictionary<string, string>();

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return claims;

            foreach (var claim in user.Claims)
            {
                claims[claim.Type] = claim.Value;
            }

            return claims;
        }

        /// <summary>
        /// Gets a specific claim value
        /// </summary>
        public string? GetClaimValue(string claimType)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirst(claimType)?.Value;
        }

        /// <summary>
        /// Sets the current user context (for background operations)
        /// </summary>
        public void SetUserContext(Guid userId, string userName, string userRole)
        {
            _backgroundUserContext.Value = new UserContextData
            {
                UserId = userId,
                UserName = userName,
                UserRole = userRole
            };
        }

        /// <summary>
        /// Clears the current user context
        /// </summary>
        public void ClearUserContext()
        {
            _backgroundUserContext.Value = null;
        }

        /// <summary>
        /// Disposes the service and cleans up resources
        /// </summary>
        public void Dispose()
        {
            _backgroundUserContext?.Dispose();
        }
    }

    /// <summary>
    /// Internal class to store user context data for background operations
    /// </summary>
    internal class UserContextData
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
    }
}