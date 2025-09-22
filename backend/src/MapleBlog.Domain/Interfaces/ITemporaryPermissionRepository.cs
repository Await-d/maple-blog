using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 临时权限仓储接口
/// </summary>
public interface ITemporaryPermissionRepository : IRepository<TemporaryPermission>
{
    /// <summary>
    /// 获取用户的有效临时权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>临时权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetValidPermissionsAsync(
        Guid userId,
        string? resourceType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户对特定资源的临时权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>临时权限</returns>
    Task<TemporaryPermission?> GetUserResourcePermissionAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取即将过期的临时权限
    /// </summary>
    /// <param name="hoursBeforeExpiry">过期前小时数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>即将过期的临时权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetExpiringPermissionsAsync(
        int hoursBeforeExpiry = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取过期的临时权限
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>过期的临时权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量撤销临时权限
    /// </summary>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="revokedBy">撤销者ID</param>
    /// <param name="reason">撤销原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销的权限数量</returns>
    Task<int> BatchRevokeAsync(
        IEnumerable<Guid> permissionIds,
        Guid revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的临时权限
    /// </summary>
    /// <param name="daysOld">过期多少天</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的权限数量</returns>
    Task<int> CleanupExpiredAsync(int daysOld = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户委派的权限
    /// </summary>
    /// <param name="fromUserId">委派人ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>委派权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetDelegatedPermissionsAsync(
        Guid fromUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户被委派的权限
    /// </summary>
    /// <param name="toUserId">被委派人ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被委派权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetReceivedDelegatedPermissionsAsync(
        Guid toUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录权限使用
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<bool> RecordUsageAsync(Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取临时权限统计
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<TemporaryPermissionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户和资源获取权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetByUserAndResourceAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户和资源获取有效权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <param name="operation">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有效权限列表</returns>
    Task<IEnumerable<TemporaryPermission>> GetActiveByUserAndResourceAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 临时权限统计信息
/// </summary>
public class TemporaryPermissionStatistics
{
    /// <summary>
    /// 总临时权限数
    /// </summary>
    public int TotalPermissions { get; set; }

    /// <summary>
    /// 有效临时权限数
    /// </summary>
    public int ValidPermissions { get; set; }

    /// <summary>
    /// 过期临时权限数
    /// </summary>
    public int ExpiredPermissions { get; set; }

    /// <summary>
    /// 已撤销临时权限数
    /// </summary>
    public int RevokedPermissions { get; set; }

    /// <summary>
    /// 按类型分组的权限数
    /// </summary>
    public Dictionary<TemporaryPermissionType, int> PermissionsByType { get; set; } = new();

    /// <summary>
    /// 按资源类型分组的权限数
    /// </summary>
    public Dictionary<string, int> PermissionsByResourceType { get; set; } = new();

    /// <summary>
    /// 按操作类型分组的权限数
    /// </summary>
    public Dictionary<DataOperation, int> PermissionsByOperation { get; set; } = new();

    /// <summary>
    /// 平均权限持续时间（小时）
    /// </summary>
    public double AverageDurationHours { get; set; }

    /// <summary>
    /// 平均使用次数
    /// </summary>
    public double AverageUsageCount { get; set; }
}