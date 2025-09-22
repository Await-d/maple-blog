using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 数据权限规则仓储接口
/// </summary>
public interface IDataPermissionRuleRepository : IRepository<DataPermissionRule>
{
    /// <summary>
    /// 根据用户ID获取权限规则
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限规则列表</returns>
    Task<IEnumerable<DataPermissionRule>> GetByUserIdAsync(Guid userId, string? resourceType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色ID获取权限规则
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限规则列表</returns>
    Task<IEnumerable<DataPermissionRule>> GetByRoleIdAsync(Guid roleId, string? resourceType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户对特定资源的权限规则
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="operation">操作类型</param>
    /// <param name="resourceId">资源ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限规则列表</returns>
    Task<IEnumerable<DataPermissionRule>> GetUserResourcePermissionsAsync(
        Guid userId,
        string resourceType,
        DataOperation operation,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否存在冲突的权限规则
    /// </summary>
    /// <param name="rule">权限规则</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在冲突</returns>
    Task<bool> HasConflictingRuleAsync(DataPermissionRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取有效的权限规则
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="operation">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>有效权限规则列表</returns>
    Task<IEnumerable<DataPermissionRule>> GetEffectiveRulesAsync(
        Guid userId,
        string resourceType,
        DataOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取用户权限规则
    /// </summary>
    /// <param name="userIds">用户ID列表</param>
    /// <param name="resourceType">资源类型（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户权限规则字典</returns>
    Task<Dictionary<Guid, IEnumerable<DataPermissionRule>>> GetBatchUserRulesAsync(
        IEnumerable<Guid> userIds,
        string? resourceType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的权限规则
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的规则数量</returns>
    Task<int> CleanupExpiredRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取权限规则统计
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<DataPermissionRuleStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 数据权限规则统计信息
/// </summary>
public class DataPermissionRuleStatistics
{
    /// <summary>
    /// 总规则数
    /// </summary>
    public int TotalRules { get; set; }

    /// <summary>
    /// 活跃规则数
    /// </summary>
    public int ActiveRules { get; set; }

    /// <summary>
    /// 过期规则数
    /// </summary>
    public int ExpiredRules { get; set; }

    /// <summary>
    /// 临时规则数
    /// </summary>
    public int TemporaryRules { get; set; }

    /// <summary>
    /// 按资源类型分组的规则数
    /// </summary>
    public Dictionary<string, int> RulesByResourceType { get; set; } = new();

    /// <summary>
    /// 按操作类型分组的规则数
    /// </summary>
    public Dictionary<DataOperation, int> RulesByOperation { get; set; } = new();

    /// <summary>
    /// 按权限范围分组的规则数
    /// </summary>
    public Dictionary<DataPermissionScope, int> RulesByScope { get; set; } = new();
}