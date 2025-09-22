using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Extensions;

/// <summary>
/// Extension methods for DataPermissionScope enum
/// </summary>
public static class DataPermissionScopeExtensions
{
    /// <summary>
    /// 检查是否可以访问所有数据
    /// </summary>
    public static bool CanAccessAllData(this DataPermissionScope scope)
    {
        return scope == DataPermissionScope.Global;
    }

    /// <summary>
    /// 检查是否可以访问所有用户
    /// </summary>
    public static bool CanAccessAllUsers(this DataPermissionScope scope)
    {
        return scope == DataPermissionScope.Global;
    }

    /// <summary>
    /// 检查是否可以访问公开用户信息
    /// </summary>
    public static bool CanAccessPublicUsers(this DataPermissionScope scope)
    {
        return scope >= DataPermissionScope.Organization;
    }

    /// <summary>
    /// 检查是否可以访问所有文章
    /// </summary>
    public static bool CanAccessAllPosts(this DataPermissionScope scope)
    {
        return scope == DataPermissionScope.Global;
    }

    /// <summary>
    /// 检查是否可以访问已发布的文章
    /// </summary>
    public static bool CanAccessPublishedPosts(this DataPermissionScope scope)
    {
        return scope >= DataPermissionScope.Department;
    }

    /// <summary>
    /// 检查是否可以访问自己的文章
    /// </summary>
    public static bool CanAccessOwnPosts(this DataPermissionScope scope)
    {
        return scope >= DataPermissionScope.Own;
    }

    /// <summary>
    /// 检查是否可以访问所有评论
    /// </summary>
    public static bool CanAccessAllComments(this DataPermissionScope scope)
    {
        return scope == DataPermissionScope.Global;
    }

    /// <summary>
    /// 检查是否可以访问相关评论
    /// </summary>
    public static bool CanAccessRelatedComments(this DataPermissionScope scope)
    {
        return scope >= DataPermissionScope.Department;
    }

    /// <summary>
    /// 检查是否可以访问自己的评论
    /// </summary>
    public static bool CanAccessOwnComments(this DataPermissionScope scope)
    {
        return scope >= DataPermissionScope.Own;
    }

    /// <summary>
    /// 检查是否可以管理系统
    /// </summary>
    public static bool CanManageSystem(this DataPermissionScope scope)
    {
        return scope == DataPermissionScope.Global;
    }
}