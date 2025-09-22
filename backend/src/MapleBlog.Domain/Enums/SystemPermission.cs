namespace MapleBlog.Domain.Enums;

/// <summary>
/// 系统预定义权限
/// </summary>
public static class SystemPermission
{
    #region 用户管理权限
    public const string UserRead = "User.Read";
    public const string UserCreate = "User.Create";
    public const string UserUpdate = "User.Update";
    public const string UserDelete = "User.Delete";
    public const string UserManageRoles = "User.ManageRoles";
    #endregion

    #region 博客文章权限
    public const string PostRead = "Post.Read";
    public const string PostCreate = "Post.Create";
    public const string PostUpdate = "Post.Update";
    public const string PostDelete = "Post.Delete";
    public const string PostPublish = "Post.Publish";
    public const string PostUnpublish = "Post.Unpublish";
    #endregion

    #region 分类管理权限
    public const string CategoryRead = "Category.Read";
    public const string CategoryCreate = "Category.Create";
    public const string CategoryUpdate = "Category.Update";
    public const string CategoryDelete = "Category.Delete";
    #endregion

    #region 标签管理权限
    public const string TagRead = "Tag.Read";
    public const string TagCreate = "Tag.Create";
    public const string TagUpdate = "Tag.Update";
    public const string TagDelete = "Tag.Delete";
    #endregion

    #region 评论管理权限
    public const string CommentRead = "Comment.Read";
    public const string CommentCreate = "Comment.Create";
    public const string CommentUpdate = "Comment.Update";
    public const string CommentDelete = "Comment.Delete";
    public const string CommentModerate = "Comment.Moderate";
    #endregion

    #region 系统管理权限
    public const string SystemView = "System.View";
    public const string SystemManage = "System.Manage";
    public const string SystemBackup = "System.Backup";
    public const string SystemRestore = "System.Restore";
    #endregion

    #region 角色权限管理
    public const string RoleRead = "Role.Read";
    public const string RoleCreate = "Role.Create";
    public const string RoleUpdate = "Role.Update";
    public const string RoleDelete = "Role.Delete";
    public const string RoleAssignPermissions = "Role.AssignPermissions";
    #endregion

    #region 权限管理
    public const string PermissionRead = "Permission.Read";
    public const string PermissionCreate = "Permission.Create";
    public const string PermissionUpdate = "Permission.Update";
    public const string PermissionDelete = "Permission.Delete";
    #endregion

    #region 管理面板权限
    public const string DashboardView = "Dashboard.View";
    public const string AnalyticsView = "Analytics.View";
    public const string LogsView = "Logs.View";
    #endregion

    #region 文件管理权限
    public const string FileRead = "File.Read";
    public const string FileUpload = "File.Upload";
    public const string FileDelete = "File.Delete";
    #endregion

    /// <summary>
    /// 获取所有系统权限
    /// </summary>
    /// <returns>系统权限列表</returns>
    public static IEnumerable<string> GetAllPermissions()
    {
        return typeof(SystemPermission)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .Cast<string>()
            .ToList();
    }

    /// <summary>
    /// 根据资源获取权限
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <returns>权限列表</returns>
    public static IEnumerable<string> GetPermissionsByResource(string resource)
    {
        return GetAllPermissions()
            .Where(p => p.StartsWith($"{resource}.", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}