using MapleBlog.Domain.Enums;
using System.Collections.Concurrent;

namespace MapleBlog.Domain.Services;

/// <summary>
/// Service for managing role-permission mappings and authorization
/// </summary>
public class RolePermissionService
{
    private static readonly ConcurrentDictionary<UserRole, HashSet<string>> _rolePermissionCache =
        new();

    private static readonly Dictionary<UserRole, HashSet<string>> _rolePermissions =
        new()
        {
            [UserRole.Guest] = new HashSet<string>
            {
                // Guest permissions - minimal read-only access
                SystemPermission.PostRead,
                SystemPermission.CategoryRead,
                SystemPermission.TagRead,
                SystemPermission.CommentRead
            },

            [UserRole.User] = new HashSet<string>
            {
                // Basic reading permissions
                SystemPermission.CommentCreate,
                SystemPermission.FileRead
            },

            [UserRole.Author] = new HashSet<string>
            {
                // Author can create and manage their own content
                SystemPermission.PostCreate,
                SystemPermission.PostUpdate,
                SystemPermission.PostDelete,
                SystemPermission.PostPublish,
                SystemPermission.PostUnpublish,
                SystemPermission.CategoryCreate,
                SystemPermission.TagCreate,
                SystemPermission.TagUpdate,
                SystemPermission.CommentUpdate,
                SystemPermission.CommentDelete,
                SystemPermission.FileUpload,
                SystemPermission.FileDelete
            },

            [UserRole.Moderator] = new HashSet<string>
            {
                // Moderator can manage content and users
                SystemPermission.CommentModerate,
                SystemPermission.UserRead,
                SystemPermission.CategoryUpdate,
                SystemPermission.CategoryDelete,
                SystemPermission.TagDelete,
                SystemPermission.DashboardView
            },

            [UserRole.Admin] = new HashSet<string>
            {
                // Admin has most system permissions
                SystemPermission.UserCreate,
                SystemPermission.UserUpdate,
                SystemPermission.UserDelete,
                SystemPermission.UserManageRoles,
                SystemPermission.SystemView,
                SystemPermission.SystemManage,
                SystemPermission.RoleRead,
                SystemPermission.RoleCreate,
                SystemPermission.RoleUpdate,
                SystemPermission.RoleDelete,
                SystemPermission.PermissionRead,
                SystemPermission.AnalyticsView,
                SystemPermission.LogsView
            },

            [UserRole.SuperAdmin] = new HashSet<string>
            {
                // SuperAdmin has all permissions
                SystemPermission.SystemBackup,
                SystemPermission.SystemRestore,
                SystemPermission.RoleAssignPermissions,
                SystemPermission.PermissionCreate,
                SystemPermission.PermissionUpdate,
                SystemPermission.PermissionDelete
            }
        };

    /// <summary>
    /// Gets all permissions for a given role, including inherited permissions
    /// </summary>
    /// <param name="role">Role to get permissions for</param>
    /// <returns>Set of permissions</returns>
    public static HashSet<string> GetRolePermissions(UserRole role)
    {
        return _rolePermissionCache.GetOrAdd(role, ComputeRolePermissions);
    }

    /// <summary>
    /// Checks if a role has a specific permission
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <param name="permission">Permission to check for</param>
    /// <returns>True if role has permission</returns>
    public static bool HasPermission(UserRole role, string permission)
    {
        var permissions = GetRolePermissions(role);
        return permissions.Contains(permission);
    }


    /// <summary>
    /// Checks if a user with given roles has any of the specified permissions
    /// </summary>
    /// <param name="userRoles">User's roles</param>
    /// <param name="permissions">Permissions to check for</param>
    /// <returns>True if user has any of the permissions</returns>
    public static bool HasAnyPermission(UserRole userRoles, params string[] permissions)
    {
        if (userRoles == UserRole.None)
            return false;

        var individualRoles = userRoles.GetIndividualRoles();
        return permissions.Any(permission => individualRoles.Any(role => HasPermission(role, permission)));
    }

    /// <summary>
    /// Checks if a user with given roles has all of the specified permissions
    /// </summary>
    /// <param name="userRoles">User's roles</param>
    /// <param name="permissions">Permissions to check for</param>
    /// <returns>True if user has all permissions</returns>
    public static bool HasAllPermissions(UserRole userRoles, params string[] permissions)
    {
        if (userRoles == UserRole.None)
            return false;

        var individualRoles = userRoles.GetIndividualRoles();
        return permissions.All(permission => individualRoles.Any(role => HasPermission(role, permission)));
    }

    /// <summary>
    /// Gets all permissions for a user based on their roles
    /// </summary>
    /// <param name="userRoles">User's roles</param>
    /// <returns>Set of all permissions</returns>
    public static HashSet<string> GetUserPermissions(UserRole userRoles)
    {
        if (userRoles == UserRole.None)
            return new HashSet<string>();

        var allPermissions = new HashSet<string>();
        var individualRoles = userRoles.GetIndividualRoles();

        foreach (var role in individualRoles)
        {
            var rolePermissions = GetRolePermissions(role);
            allPermissions.UnionWith(rolePermissions);
        }

        return allPermissions;
    }

    /// <summary>
    /// Gets permissions that a role would gain from being promoted to another role
    /// </summary>
    /// <param name="currentRole">Current role</param>
    /// <param name="targetRole">Target role</param>
    /// <returns>Set of additional permissions</returns>
    public static HashSet<string> GetAdditionalPermissions(UserRole currentRole, UserRole targetRole)
    {
        var currentPermissions = GetUserPermissions(currentRole);
        var targetPermissions = GetUserPermissions(targetRole);

        var additionalPermissions = new HashSet<string>(targetPermissions);
        additionalPermissions.ExceptWith(currentPermissions);

        return additionalPermissions;
    }

    /// <summary>
    /// Gets permissions that would be lost when demoting from one role to another
    /// </summary>
    /// <param name="currentRole">Current role</param>
    /// <param name="targetRole">Target role</param>
    /// <returns>Set of lost permissions</returns>
    public static HashSet<string> GetLostPermissions(UserRole currentRole, UserRole targetRole)
    {
        var currentPermissions = GetUserPermissions(currentRole);
        var targetPermissions = GetUserPermissions(targetRole);

        var lostPermissions = new HashSet<string>(currentPermissions);
        lostPermissions.ExceptWith(targetPermissions);

        return lostPermissions;
    }

    /// <summary>
    /// Validates if a role change is allowed based on the acting user's permissions
    /// </summary>
    /// <param name="actingUserRoles">Acting user's roles</param>
    /// <param name="targetCurrentRoles">Target user's current roles</param>
    /// <param name="targetNewRoles">Target user's new roles</param>
    /// <returns>True if role change is allowed</returns>
    public static bool CanChangeUserRole(UserRole actingUserRoles, UserRole targetCurrentRoles, UserRole targetNewRoles)
    {
        // SuperAdmin can change anyone's role
        if (actingUserRoles.HasRole(UserRole.SuperAdmin))
            return true;

        // Admin can change roles below admin level
        if (actingUserRoles.HasRole(UserRole.Admin))
        {
            var prohibitedRoles = UserRole.Admin | UserRole.SuperAdmin;
            return !targetCurrentRoles.HasAnyRole(prohibitedRoles) &&
                   !targetNewRoles.HasAnyRole(prohibitedRoles);
        }

        // No one else can change roles
        return false;
    }

    /// <summary>
    /// Gets the minimum role required for a specific permission
    /// </summary>
    /// <param name="permission">Permission to check</param>
    /// <returns>Minimum role required, or null if permission doesn't exist</returns>
    public static UserRole? GetMinimumRoleForPermission(string permission)
    {
        var roleHierarchy = new[] { UserRole.Guest, UserRole.User, UserRole.Author, UserRole.Moderator, UserRole.Admin, UserRole.SuperAdmin };

        foreach (var role in roleHierarchy)
        {
            if (HasPermission(role, permission))
                return role;
        }

        return null;
    }

    /// <summary>
    /// Clears the permission cache (useful for testing or runtime changes)
    /// </summary>
    public static void ClearCache()
    {
        _rolePermissionCache.Clear();
    }

    /// <summary>
    /// Computes all permissions for a role including inherited permissions
    /// </summary>
    private static HashSet<string> ComputeRolePermissions(UserRole role)
    {
        var permissions = new HashSet<string>();

        // Add permissions based on role hierarchy
        switch (role)
        {
            case UserRole.SuperAdmin:
                permissions.UnionWith(_rolePermissions[UserRole.SuperAdmin]);
                goto case UserRole.Admin;
            case UserRole.Admin:
                permissions.UnionWith(_rolePermissions[UserRole.Admin]);
                goto case UserRole.Moderator;
            case UserRole.Moderator:
                permissions.UnionWith(_rolePermissions[UserRole.Moderator]);
                goto case UserRole.Author;
            case UserRole.Author:
                permissions.UnionWith(_rolePermissions[UserRole.Author]);
                goto case UserRole.User;
            case UserRole.User:
                permissions.UnionWith(_rolePermissions[UserRole.User]);
                goto case UserRole.Guest;
            case UserRole.Guest:
                permissions.UnionWith(_rolePermissions[UserRole.Guest]);
                break;
        }

        return permissions;
    }

    /// <summary>
    /// Gets all role-permission mappings for debugging/administrative purposes
    /// </summary>
    /// <returns>Dictionary of role to permissions mapping</returns>
    public static Dictionary<UserRole, HashSet<string>> GetAllRolePermissionMappings()
    {
        var result = new Dictionary<UserRole, HashSet<string>>();
        var allRoles = Enum.GetValues<UserRole>()
            .Where(r => r != UserRole.None && !r.IsCompositeRole())
            .ToArray();

        foreach (var role in allRoles)
        {
            result[role] = GetRolePermissions(role);
        }

        return result;
    }

    /// <summary>
    /// Validates permission string format
    /// </summary>
    /// <param name="permission">Permission to validate</param>
    /// <returns>True if permission format is valid</returns>
    public static bool IsValidPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        // Check if it's a known system permission
        var allSystemPermissions = SystemPermission.GetAllPermissions();
        return allSystemPermissions.Contains(permission);
    }

    /// <summary>
    /// Gets permissions by resource category
    /// </summary>
    /// <param name="resource">Resource name (e.g., "User", "Post", "Comment")</param>
    /// <returns>Permissions for the resource</returns>
    public static IEnumerable<string> GetPermissionsByResource(string resource)
    {
        return SystemPermission.GetPermissionsByResource(resource);
    }
}