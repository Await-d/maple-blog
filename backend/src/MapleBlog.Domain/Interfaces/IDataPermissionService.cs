using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Models;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 数据权限服务接口
/// 定义企业级数据访问控制和权限检查的核心功能
/// </summary>
public interface IDataPermissionService
{
    #region 核心权限检查方法

    /// <summary>
    /// 检查用户是否有权限对实体执行指定操作
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="entity">实体对象</param>
    /// <param name="operation">数据操作类型</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasDataAccessAsync<T>(Guid userId, T entity, DataOperation operation) where T : class;

    /// <summary>
    /// 检查用户是否可以访问指定资源
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <returns>是否有权限</returns>
    Task<bool> CanAccessResourceAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation);

    #endregion

    #region 批量权限检查

    /// <summary>
    /// 批量检查数据访问权限
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="entities">实体集合</param>
    /// <param name="operation">操作类型</param>
    /// <returns>权限检查结果字典</returns>
    Task<Dictionary<Guid, bool>> CheckBatchDataAccessAsync<T>(Guid userId, IEnumerable<T> entities, DataOperation operation) where T : BaseEntity;

    /// <summary>
    /// 过滤用户有权限访问的实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="entities">实体集合</param>
    /// <param name="operation">操作类型</param>
    /// <returns>有权限的实体列表</returns>
    Task<IEnumerable<T>> FilterAccessibleEntitiesAsync<T>(Guid userId, IEnumerable<T> entities, DataOperation operation) where T : BaseEntity;

    #endregion

    #region 查询过滤

    /// <summary>
    /// 根据数据权限过滤查询
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="userId">用户ID</param>
    /// <param name="query">查询对象</param>
    /// <param name="operation">操作类型</param>
    /// <returns>过滤后的查询</returns>
    Task<IQueryable<T>> FilterByDataPermissionsAsync<T>(Guid userId, IQueryable<T> query, DataOperation operation) where T : BaseEntity;

    /// <summary>
    /// 应用用户数据权限过滤（兼容旧接口）
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="currentUserId">当前用户ID</param>
    /// <param name="userRole">用户角色</param>
    /// <returns>过滤后的查询</returns>
    IQueryable<T> ApplyUserDataFilter<T>(IQueryable<T> query, Guid currentUserId, UserRoleEnum userRole) where T : BaseEntity;

    #endregion

    #region 权限范围和规则

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <returns>数据权限范围</returns>
    Task<DataPermissionScope> GetUserDataScopeAsync(Guid userId, string resourceType = null);

    /// <summary>
    /// 检查用户是否有权限访问特定资源（兼容旧接口）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="resourceId">资源ID（可选）</param>
    /// <returns>是否有权限</returns>
    Task<bool> HasPermissionAsync(Guid userId, string resource, string action, Guid? resourceId = null);

    /// <summary>
    /// 获取用户的数据权限规则
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <returns>权限规则列表</returns>
    Task<IEnumerable<DataPermissionRule>> GetUserPermissionRulesAsync(Guid userId, string resourceType = null);

    #endregion

    #region 临时权限和委派

    /// <summary>
    /// 授予临时权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="grantedBy">授权者ID</param>
    /// <returns>操作结果</returns>
    Task<bool> GrantTemporaryPermissionAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation, DateTime expiresAt, Guid grantedBy);

    /// <summary>
    /// 撤销临时权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <returns>操作结果</returns>
    Task<bool> RevokeTemporaryPermissionAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation);

    /// <summary>
    /// 委派权限给其他用户
    /// </summary>
    /// <param name="fromUserId">委派人ID</param>
    /// <param name="toUserId">接受人ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <param name="expiresAt">过期时间</param>
    /// <returns>操作结果</returns>
    Task<bool> DelegatePermissionAsync(Guid fromUserId, Guid toUserId, string resourceType, Guid resourceId, DataOperation operation, DateTime expiresAt);

    #endregion

    #region 数据脱敏和安全

    /// <summary>
    /// 应用敏感数据脱敏
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <param name="userRole">用户角色</param>
    /// <returns>脱敏后的实体</returns>
    T ApplyDataMasking<T>(T entity, UserRoleEnum userRole) where T : class;

    /// <summary>
    /// 批量应用数据脱敏
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">实体集合</param>
    /// <param name="userId">用户ID</param>
    /// <returns>脱敏后的实体集合</returns>
    Task<IEnumerable<T>> ApplyBatchDataMaskingAsync<T>(IEnumerable<T> entities, Guid userId) where T : class;

    #endregion

    #region 权限缓存和性能

    /// <summary>
    /// 清除用户权限缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    Task<bool> ClearUserPermissionCacheAsync(Guid userId);

    /// <summary>
    /// 预热用户权限缓存
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>操作结果</returns>
    Task<bool> WarmupUserPermissionCacheAsync(Guid userId);

    /// <summary>
    /// 获取权限检查统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    Task<PermissionStatistics> GetPermissionStatisticsAsync();

    #endregion
}

/// <summary>
/// 数据权限范围
/// 定义用户可以访问的数据范围和操作权限
/// </summary>
public class DataPermissionScope
{
    /// <summary>
    /// 是否有数据访问权限
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRoleEnum UserRole { get; set; }

    /// <summary>
    /// 是否可以访问所有数据（管理员权限）
    /// </summary>
    public bool CanAccessAllData { get; set; }

    /// <summary>
    /// 是否可以访问自己的数据
    /// </summary>
    public bool CanAccessOwnData { get; set; }

    /// <summary>
    /// 是否可以访问所有用户数据
    /// </summary>
    public bool CanAccessAllUsers { get; set; }

    /// <summary>
    /// 是否可以访问公开用户信息
    /// </summary>
    public bool CanAccessPublicUsers { get; set; }

    /// <summary>
    /// 是否可以访问所有文章
    /// </summary>
    public bool CanAccessAllPosts { get; set; }

    /// <summary>
    /// 是否可以访问自己的文章
    /// </summary>
    public bool CanAccessOwnPosts { get; set; }

    /// <summary>
    /// 是否可以访问已发布的文章
    /// </summary>
    public bool CanAccessPublishedPosts { get; set; }

    /// <summary>
    /// 是否可以访问所有评论
    /// </summary>
    public bool CanAccessAllComments { get; set; }

    /// <summary>
    /// 是否可以访问自己的评论
    /// </summary>
    public bool CanAccessOwnComments { get; set; }

    /// <summary>
    /// 是否可以访问相关评论（如自己文章的评论）
    /// </summary>
    public bool CanAccessRelatedComments { get; set; }

    /// <summary>
    /// 是否可以管理系统
    /// </summary>
    public bool CanManageSystem { get; set; }

    /// <summary>
    /// 获取权限摘要
    /// </summary>
    /// <returns>权限描述</returns>
    public string GetPermissionSummary()
    {
        if (CanAccessAllData)
            return "Full access (Administrator)";

        var permissions = new List<string>();

        if (CanAccessOwnData)
            permissions.Add("Own data");

        if (CanAccessPublicUsers)
            permissions.Add("Public users");

        if (CanAccessAllPosts)
            permissions.Add("All posts");
        else if (CanAccessPublishedPosts)
            permissions.Add("Published posts");

        if (CanAccessOwnPosts)
            permissions.Add("Own posts");

        if (CanAccessAllComments)
            permissions.Add("All comments");
        else if (CanAccessOwnComments)
            permissions.Add("Own comments");

        if (CanManageSystem)
            permissions.Add("System management");

        return permissions.Any() ? string.Join(", ", permissions) : "Limited access";
    }

    /// <summary>
    /// 检查是否有指定资源的访问权限
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <returns>是否有权限</returns>
    public bool HasResourcePermission(string resource, string action)
    {
        if (CanAccessAllData)
            return true;

        return resource.ToLowerInvariant() switch
        {
            "users" => action.ToLowerInvariant() switch
            {
                "read" => CanAccessAllUsers || CanAccessPublicUsers || CanAccessOwnData,
                "create" or "update" or "delete" => CanAccessAllUsers,
                _ => false
            },
            "posts" => action.ToLowerInvariant() switch
            {
                "read" => CanAccessAllPosts || CanAccessPublishedPosts,
                "create" or "update" or "delete" => CanAccessAllPosts || CanAccessOwnPosts,
                _ => false
            },
            "comments" => action.ToLowerInvariant() switch
            {
                "read" => CanAccessAllComments || CanAccessRelatedComments || CanAccessOwnComments,
                "create" or "update" or "delete" => CanAccessAllComments || CanAccessOwnComments,
                "moderate" => CanAccessAllComments || CanAccessRelatedComments,
                _ => false
            },
            "system" => CanManageSystem,
            _ => false
        };
    }
}