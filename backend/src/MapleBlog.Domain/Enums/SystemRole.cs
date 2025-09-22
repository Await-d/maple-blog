
namespace MapleBlog.Domain.Enums;

/// <summary>
/// 系统预定义角色工具类 - 基于UserRole枚举的辅助工具
/// 该类提供与旧字符串角色系统的兼容性和权限管理功能
/// </summary>
public static class SystemRole
{
    /// <summary>
    /// 超级管理员角色名
    /// </summary>
    public const string SuperAdmin = nameof(UserRole.SuperAdmin);

    /// <summary>
    /// 管理员角色名
    /// </summary>
    public const string Admin = nameof(UserRole.Admin);

    /// <summary>
    /// 作者角色名
    /// </summary>
    public const string Author = nameof(UserRole.Author);

    /// <summary>
    /// 审核员角色名
    /// </summary>
    public const string Moderator = nameof(UserRole.Moderator);

    /// <summary>
    /// 普通用户角色名
    /// </summary>
    public const string User = nameof(UserRole.User);

    /// <summary>
    /// 访客角色名
    /// </summary>
    public const string Guest = nameof(UserRole.Guest);

    /// <summary>
    /// 获取所有系统角色名称
    /// </summary>
    /// <returns>系统角色名称列表</returns>
    public static IEnumerable<string> GetAllRoles()
    {
        return new[]
        {
            Guest,
            User,
            Author,
            Moderator,
            Admin,
            SuperAdmin
        };
    }

    /// <summary>
    /// 获取所有系统角色枚举值
    /// </summary>
    /// <returns>系统角色枚举列表</returns>
    public static IEnumerable<UserRole> GetAllRoleEnums()
    {
        return new[]
        {
            UserRole.Guest,
            UserRole.User,
            UserRole.Author,
            UserRole.Moderator,
            UserRole.Admin,
            UserRole.SuperAdmin
        };
    }

    /// <summary>
    /// 将角色名转换为UserRole枚举
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <returns>UserRole枚举值</returns>
    public static UserRole? ParseRole(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return null;

        return roleName.Trim() switch
        {
            Guest => UserRole.Guest,
            User => UserRole.User,
            Author => UserRole.Author,
            Moderator => UserRole.Moderator,
            Admin => UserRole.Admin,
            SuperAdmin => UserRole.SuperAdmin,
            _ => Enum.TryParse<UserRole>(roleName, true, out var role) ? role : null
        };
    }

    /// <summary>
    /// 将UserRole枚举转换为角色名
    /// </summary>
    /// <param name="role">UserRole枚举值</param>
    /// <returns>角色名称</returns>
    public static string ToRoleName(UserRole role)
    {
        return role switch
        {
            UserRole.Guest => Guest,
            UserRole.User => User,
            UserRole.Author => Author,
            UserRole.Moderator => Moderator,
            UserRole.Admin => Admin,
            UserRole.SuperAdmin => SuperAdmin,
            _ => role.ToString()
        };
    }

    /// <summary>
    /// 获取角色的默认权限（基于字符串角色名）
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <returns>权限列表</returns>
    public static IEnumerable<string> GetDefaultPermissions(string roleName)
    {
        var role = ParseRole(roleName);
        return role.HasValue ? GetDefaultPermissions(role.Value) : Enumerable.Empty<string>();
    }

    /// <summary>
    /// 获取角色的默认权限（基于UserRole枚举）
    /// 注意：建议直接使用RolePermissionService.GetRolePermissions()
    /// </summary>
    /// <param name="role">用户角色枚举</param>
    /// <returns>权限列表</returns>
    public static IEnumerable<string> GetDefaultPermissions(UserRole role)
    {
        // 为了避免循环引用，这里使用直接映射
        return role switch
        {
            UserRole.SuperAdmin => SystemPermission.GetAllPermissions(),
            UserRole.Admin => GetAdminPermissions(),
            UserRole.Author => GetAuthorPermissions(),
            UserRole.Moderator => GetModeratorPermissions(),
            UserRole.User => GetUserPermissions(),
            UserRole.Guest => GetGuestPermissions(),
            _ => Enumerable.Empty<string>()
        };
    }

    /// <summary>
    /// 获取角色的默认权限（旧方法，保留兼容性）
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <returns>权限列表</returns>
    [Obsolete("Use GetDefaultPermissions(UserRole) or RolePermissionService.GetRolePermissions instead")]
    public static IEnumerable<string> GetLegacyDefaultPermissions(string roleName)
    {
        return roleName switch
        {
            SuperAdmin => SystemPermission.GetAllPermissions(),
            Admin => GetAdminPermissions(),
            Author => GetAuthorPermissions(),
            Moderator => GetModeratorPermissions(),
            User => GetUserPermissions(),
            Guest => GetGuestPermissions(),
            _ => Enumerable.Empty<string>()
        };
    }

    private static IEnumerable<string> GetAdminPermissions()
    {
        return new[]
        {
            // 用户管理
            SystemPermission.UserRead,
            SystemPermission.UserCreate,
            SystemPermission.UserUpdate,
            SystemPermission.UserManageRoles,

            // 内容管理
            SystemPermission.PostRead,
            SystemPermission.PostCreate,
            SystemPermission.PostUpdate,
            SystemPermission.PostDelete,
            SystemPermission.PostPublish,
            SystemPermission.PostUnpublish,

            // 分类标签
            SystemPermission.CategoryRead,
            SystemPermission.CategoryCreate,
            SystemPermission.CategoryUpdate,
            SystemPermission.CategoryDelete,
            SystemPermission.TagRead,
            SystemPermission.TagCreate,
            SystemPermission.TagUpdate,
            SystemPermission.TagDelete,

            // 评论管理
            SystemPermission.CommentRead,
            SystemPermission.CommentUpdate,
            SystemPermission.CommentDelete,
            SystemPermission.CommentModerate,

            // 角色权限管理
            SystemPermission.RoleRead,
            SystemPermission.RoleCreate,
            SystemPermission.RoleUpdate,
            SystemPermission.RoleAssignPermissions,
            SystemPermission.PermissionRead,

            // 系统管理
            SystemPermission.SystemView,
            SystemPermission.SystemManage,
            SystemPermission.DashboardView,
            SystemPermission.AnalyticsView,
            SystemPermission.LogsView,

            // 文件管理
            SystemPermission.FileRead,
            SystemPermission.FileUpload,
            SystemPermission.FileDelete
        };
    }

    private static IEnumerable<string> GetAuthorPermissions()
    {
        return new[]
        {
            // 文章管理
            SystemPermission.PostRead,
            SystemPermission.PostCreate,
            SystemPermission.PostUpdate,

            // 分类标签（只读）
            SystemPermission.CategoryRead,
            SystemPermission.TagRead,

            // 评论管理（自己的）
            SystemPermission.CommentRead,
            SystemPermission.CommentUpdate,

            // 文件管理
            SystemPermission.FileRead,
            SystemPermission.FileUpload,

            // 仪表盘
            SystemPermission.DashboardView
        };
    }

    private static IEnumerable<string> GetModeratorPermissions()
    {
        return new[]
        {
            // 内容查看
            SystemPermission.PostRead,

            // 评论管理
            SystemPermission.CommentRead,
            SystemPermission.CommentUpdate,
            SystemPermission.CommentDelete,
            SystemPermission.CommentModerate,

            // 基础信息
            SystemPermission.CategoryRead,
            SystemPermission.TagRead,
            SystemPermission.UserRead,

            // 仪表盘
            SystemPermission.DashboardView
        };
    }

    private static IEnumerable<string> GetUserPermissions()
    {
        return new[]
        {
            // 基础阅读权限
            SystemPermission.PostRead,
            SystemPermission.CategoryRead,
            SystemPermission.TagRead,

            // 评论权限
            SystemPermission.CommentRead,
            SystemPermission.CommentCreate
        };
    }

    private static IEnumerable<string> GetGuestPermissions()
    {
        return new[]
        {
            // 访客基础阅读权限
            SystemPermission.PostRead,
            SystemPermission.CategoryRead,
            SystemPermission.TagRead,
            SystemPermission.CommentRead
        };
    }
}