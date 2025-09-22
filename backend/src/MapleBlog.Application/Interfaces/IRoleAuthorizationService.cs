using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Service interface for role-based authorization and management
/// </summary>
public interface IRoleAuthorizationService
{
    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has the role</returns>
    Task<bool> HasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has any of the specified roles
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">Roles to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has any of the roles</returns>
    Task<bool> HasAnyRoleAsync(Guid userId, UserRole[] roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has all of the specified roles
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">Roles to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has all the roles</returns>
    Task<bool> HasAllRolesAsync(Guid userId, UserRole[] roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has a specific permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has the permission</returns>
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has any of the specified permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissions">Permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has any of the permissions</returns>
    Task<bool> HasAnyPermissionAsync(Guid userId, string[] permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has all of the specified permissions
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permissions">Permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has all the permissions</returns>
    Task<bool> HasAllPermissionsAsync(Guid userId, string[] permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user's current roles
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's roles</returns>
    Task<UserRole> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Set of user permissions</returns>
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user (requires appropriate permissions)
    /// </summary>
    /// <param name="actingUserId">User performing the action</param>
    /// <param name="targetUserId">User to assign role to</param>
    /// <param name="role">Role to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> AssignRoleAsync(Guid actingUserId, Guid targetUserId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user (requires appropriate permissions)
    /// </summary>
    /// <param name="actingUserId">User performing the action</param>
    /// <param name="targetUserId">User to remove role from</param>
    /// <param name="role">Role to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> RemoveRoleAsync(Guid actingUserId, Guid targetUserId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets user roles, replacing existing roles (requires appropriate permissions)
    /// </summary>
    /// <param name="actingUserId">User performing the action</param>
    /// <param name="targetUserId">User to set roles for</param>
    /// <param name="roles">New roles to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> SetUserRolesAsync(Guid actingUserId, Guid targetUserId, UserRole roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a role change is allowed
    /// </summary>
    /// <param name="actingUserId">User performing the action</param>
    /// <param name="targetUserId">User whose role is being changed</param>
    /// <param name="newRoles">New roles to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<OperationResult> ValidateRoleChangeAsync(Guid actingUserId, Guid targetUserId, UserRole newRoles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can manage another user based on role hierarchy
    /// </summary>
    /// <param name="actingUserId">User performing the action</param>
    /// <param name="targetUserId">User to be managed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if management is allowed</returns>
    Task<bool> CanManageUserAsync(Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by role with pagination
    /// </summary>
    /// <param name="role">Role to filter by</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated users with the role</returns>
    Task<PagedResultDto<UserDto>> GetUsersByRoleAsync(UserRole role, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role distribution statistics</returns>
    Task<Dictionary<UserRole, int>> GetRoleStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a permission exists in the system
    /// </summary>
    /// <param name="permission">Permission to validate</param>
    /// <returns>True if permission is valid</returns>
    bool IsValidPermission(string permission);

    /// <summary>
    /// Gets all available permissions in the system
    /// </summary>
    /// <returns>List of all permissions</returns>
    IEnumerable<string> GetAllPermissions();

    /// <summary>
    /// Gets permissions by resource category
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <returns>Permissions for the resource</returns>
    IEnumerable<string> GetPermissionsByResource(string resource);

    /// <summary>
    /// Gets minimum role required for a permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>Minimum role required, or null if permission doesn't exist</returns>
    UserRole? GetMinimumRoleForPermission(string permission);
}