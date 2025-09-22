using System.ComponentModel;

namespace MapleBlog.Domain.Enums
{
    /// <summary>
    /// Defines the roles a user can have in the system using Flags enum for combinable roles
    /// </summary>
    [Flags]
    public enum UserRole
    {
        /// <summary>
        /// No specific role - default state
        /// </summary>
        [Description("None")]
        None = 0,

        /// <summary>
        /// Guest user with minimal permissions (anonymous access)
        /// </summary>
        [Description("Guest")]
        Guest = 1,

        /// <summary>
        /// Regular user with basic permissions (read, comment)
        /// </summary>
        [Description("User")]
        User = 2,

        /// <summary>
        /// Author who can create and manage their own blog posts
        /// </summary>
        [Description("Author")]
        Author = 4,

        /// <summary>
        /// Moderator who can moderate content, comments, and manage other users
        /// </summary>
        [Description("Moderator")]
        Moderator = 8,

        /// <summary>
        /// Administrator with system-wide permissions
        /// </summary>
        [Description("Admin")]
        Admin = 16,

        /// <summary>
        /// Super administrator with full system control and user management
        /// </summary>
        [Description("SuperAdmin")]
        SuperAdmin = 32,

        // Composite roles for common combinations
        /// <summary>
        /// Content manager with both author and moderator privileges
        /// </summary>
        [Description("Content Manager")]
        ContentManager = Author | Moderator,

        /// <summary>
        /// System manager with administrative and super admin privileges
        /// </summary>
        [Description("System Manager")]
        SystemManager = Admin | SuperAdmin,

        /// <summary>
        /// Full access - all roles combined
        /// </summary>
        [Description("Full Access")]
        FullAccess = Guest | User | Author | Moderator | Admin | SuperAdmin
    }

    /// <summary>
    /// Extension methods for UserRole enum
    /// </summary>
    public static class UserRoleExtensions
    {
        /// <summary>
        /// Checks if the user has the specified role
        /// </summary>
        /// <param name="userRoles">User's current roles</param>
        /// <param name="role">Role to check for</param>
        /// <returns>True if user has the role</returns>
        public static bool HasRole(this UserRole userRoles, UserRole role)
        {
            return (userRoles & role) == role;
        }

        /// <summary>
        /// Checks if the user has any of the specified roles
        /// </summary>
        /// <param name="userRoles">User's current roles</param>
        /// <param name="roles">Roles to check for</param>
        /// <returns>True if user has any of the roles</returns>
        public static bool HasAnyRole(this UserRole userRoles, params UserRole[] roles)
        {
            return roles.Any(role => userRoles.HasRole(role));
        }

        /// <summary>
        /// Checks if the user has all of the specified roles
        /// </summary>
        /// <param name="userRoles">User's current roles</param>
        /// <param name="roles">Roles to check for</param>
        /// <returns>True if user has all the roles</returns>
        public static bool HasAllRoles(this UserRole userRoles, params UserRole[] roles)
        {
            return roles.All(role => userRoles.HasRole(role));
        }

        /// <summary>
        /// Gets the display name for a role
        /// </summary>
        /// <param name="role">Role to get display name for</param>
        /// <returns>Display name of the role</returns>
        public static string GetDisplayName(this UserRole role)
        {
            var field = role.GetType().GetField(role.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                           .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? role.ToString();
        }

        /// <summary>
        /// Gets all individual roles from a combined role
        /// </summary>
        /// <param name="roles">Combined roles</param>
        /// <returns>Array of individual roles</returns>
        public static UserRole[] GetIndividualRoles(this UserRole roles)
        {
            if (roles == UserRole.None)
                return Array.Empty<UserRole>();

            var individualRoles = new List<UserRole>();
            var allRoles = Enum.GetValues<UserRole>()
                .Where(r => r != UserRole.None && !IsCompositeRole(r))
                .ToArray();

            foreach (var role in allRoles)
            {
                if (roles.HasRole(role))
                    individualRoles.Add(role);
            }

            return individualRoles.ToArray();
        }

        /// <summary>
        /// Gets all roles (individual and composite) that match the user's roles
        /// </summary>
        /// <param name="roles">User's roles</param>
        /// <returns>Array of all matching roles</returns>
        public static UserRole[] GetAllMatchingRoles(this UserRole roles)
        {
            if (roles == UserRole.None)
                return Array.Empty<UserRole>();

            var matchingRoles = new List<UserRole>();
            var allRoles = Enum.GetValues<UserRole>()
                .Where(r => r != UserRole.None)
                .ToArray();

            foreach (var role in allRoles)
            {
                if (roles.HasRole(role))
                    matchingRoles.Add(role);
            }

            return matchingRoles.ToArray();
        }

        /// <summary>
        /// Adds a role to the user's current roles
        /// </summary>
        /// <param name="userRoles">User's current roles</param>
        /// <param name="roleToAdd">Role to add</param>
        /// <returns>Updated roles</returns>
        public static UserRole AddRole(this UserRole userRoles, UserRole roleToAdd)
        {
            return userRoles | roleToAdd;
        }

        /// <summary>
        /// Removes a role from the user's current roles
        /// </summary>
        /// <param name="userRoles">User's current roles</param>
        /// <param name="roleToRemove">Role to remove</param>
        /// <returns>Updated roles</returns>
        public static UserRole RemoveRole(this UserRole userRoles, UserRole roleToRemove)
        {
            return userRoles & ~roleToRemove;
        }

        /// <summary>
        /// Gets the role hierarchy level for privilege comparison
        /// </summary>
        /// <param name="role">Role to get level for</param>
        /// <returns>Hierarchy level (higher number = more privileges)</returns>
        public static int GetHierarchyLevel(this UserRole role)
        {
            return role switch
            {
                UserRole.None => 0,
                UserRole.Guest => 1,
                UserRole.User => 2,
                UserRole.Author => 3,
                UserRole.Moderator => 4,
                UserRole.Admin => 5,
                UserRole.SuperAdmin => 6,
                _ => GetMaxHierarchyLevel(role)
            };
        }

        /// <summary>
        /// Checks if a role is a composite role (combination of other roles)
        /// </summary>
        /// <param name="role">Role to check</param>
        /// <returns>True if it's a composite role</returns>
        public static bool IsCompositeRole(this UserRole role)
        {
            return role switch
            {
                UserRole.ContentManager or
                UserRole.SystemManager or
                UserRole.FullAccess => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if user can perform an action on a target user based on role hierarchy
        /// </summary>
        /// <param name="userRoles">Acting user's roles</param>
        /// <param name="targetUserRoles">Target user's roles</param>
        /// <returns>True if action is allowed</returns>
        public static bool CanManageUser(this UserRole userRoles, UserRole targetUserRoles)
        {
            var userLevel = userRoles.GetHierarchyLevel();
            var targetLevel = targetUserRoles.GetHierarchyLevel();

            // Super admins can manage anyone
            if (userRoles.HasRole(UserRole.SuperAdmin))
                return true;

            // Admins can manage users below admin level
            if (userRoles.HasRole(UserRole.Admin))
                return !targetUserRoles.HasRole(UserRole.Admin | UserRole.SuperAdmin);

            // Moderators can manage regular users and authors
            if (userRoles.HasRole(UserRole.Moderator))
                return targetUserRoles.HasRole(UserRole.User | UserRole.Author) &&
                       !targetUserRoles.HasRole(UserRole.Moderator | UserRole.Admin | UserRole.SuperAdmin);

            return false;
        }

        /// <summary>
        /// Gets role names as comma-separated string
        /// </summary>
        /// <param name="roles">Roles to convert</param>
        /// <returns>Comma-separated role names</returns>
        public static string ToDisplayString(this UserRole roles)
        {
            if (roles == UserRole.None)
                return "None";

            var roleNames = roles.GetIndividualRoles()
                .Select(r => r.GetDisplayName())
                .ToArray();

            return string.Join(", ", roleNames);
        }

        /// <summary>
        /// Converts role names string back to UserRole enum
        /// </summary>
        /// <param name="roleNames">Comma-separated role names</param>
        /// <returns>Combined UserRole enum</returns>
        public static UserRole FromDisplayString(string roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleNames) || roleNames.Equals("None", StringComparison.OrdinalIgnoreCase))
                return UserRole.None;

            var roles = UserRole.None;
            var names = roleNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var name in names)
            {
                var trimmedName = name.Trim();
                var allRoles = Enum.GetValues<UserRole>();

                var role = allRoles.FirstOrDefault(r =>
                    r.GetDisplayName().Equals(trimmedName, StringComparison.OrdinalIgnoreCase) ||
                    r.ToString().Equals(trimmedName, StringComparison.OrdinalIgnoreCase));

                if (role != UserRole.None)
                    roles = roles.AddRole(role);
            }

            return roles;
        }

        /// <summary>
        /// Helper method to get the maximum hierarchy level for composite roles
        /// </summary>
        private static int GetMaxHierarchyLevel(UserRole role)
        {
            var individualRoles = role.GetIndividualRoles();
            return individualRoles.Any() ? individualRoles.Max(r => r.GetHierarchyLevel()) : 0;
        }
    }
}