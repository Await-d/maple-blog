using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories;

/// <summary>
/// 数据权限规则仓储实现
/// </summary>
public class DataPermissionRuleRepository : BaseRepository<DataPermissionRule>, IDataPermissionRuleRepository
{
    public DataPermissionRuleRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据用户ID获取权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetByUserIdAsync(Guid userId, string? resourceType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DataPermissionRules
            .Where(r => r.UserId == userId && r.IsActive);

        if (!string.IsNullOrEmpty(resourceType))
        {
            query = query.Where(r => r.ResourceType == resourceType);
        }

        return await query
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据角色ID获取权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetByRoleIdAsync(Guid roleId, string? resourceType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DataPermissionRules
            .Where(r => r.RoleId == roleId && r.IsActive);

        if (!string.IsNullOrEmpty(resourceType))
        {
            query = query.Where(r => r.ResourceType == resourceType);
        }

        return await query
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户对特定资源的权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetUserResourcePermissionsAsync(
        Guid userId,
        string resourceType,
        DataOperation operation,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DataPermissionRules
            .Where(r => r.UserId == userId &&
                       r.ResourceType == resourceType &&
                       r.Operation == operation &&
                       r.IsActive);

        if (resourceId.HasValue)
        {
            query = query.Where(r => r.ResourceId == null || r.ResourceId == resourceId.Value);
        }

        // 同时获取基于角色的权限规则
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user != null && user.UserRoles != null)
        {
            var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
            var roleRules = await _context.DataPermissionRules
                .Where(r => roleIds.Contains(r.RoleId!.Value) &&
                           r.ResourceType == resourceType &&
                           r.Operation == operation &&
                           r.IsActive)
                .ToListAsync(cancellationToken);

            var userRules = await query.ToListAsync(cancellationToken);

            return userRules.Concat(roleRules)
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.Source == PermissionSource.Direct ? 1 : 0) // 直接权限优先
                .ToList();
        }

        return await query
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查是否存在冲突的权限规则
    /// </summary>
    public async Task<bool> HasConflictingRuleAsync(DataPermissionRule rule, CancellationToken cancellationToken = default)
    {
        return await _context.DataPermissionRules
            .Where(r => r.Id != rule.Id &&
                       r.UserId == rule.UserId &&
                       r.ResourceType == rule.ResourceType &&
                       r.Operation == rule.Operation &&
                       r.ResourceId == rule.ResourceId &&
                       r.IsActive &&
                       r.IsAllowed != rule.IsAllowed)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取有效的权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetEffectiveRulesAsync(
        Guid userId,
        string resourceType,
        DataOperation operation,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.DataPermissionRules
            .Where(r => r.UserId == userId &&
                       r.ResourceType == resourceType &&
                       r.Operation == operation &&
                       r.IsActive &&
                       (r.EffectiveFrom == null || r.EffectiveFrom <= now) &&
                       (r.EffectiveTo == null || r.EffectiveTo > now))
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取用户权限规则
    /// </summary>
    public async Task<Dictionary<Guid, IEnumerable<DataPermissionRule>>> GetBatchUserRulesAsync(
        IEnumerable<Guid> userIds,
        string? resourceType = null,
        CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.ToList();
        var query = _context.DataPermissionRules
            .Where(r => userIdList.Contains(r.UserId) && r.IsActive);

        if (!string.IsNullOrEmpty(resourceType))
        {
            query = query.Where(r => r.ResourceType == resourceType);
        }

        var rules = await query
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .ToListAsync(cancellationToken);

        return rules
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Priority).ThenBy(r => r.CreatedAt).AsEnumerable());
    }

    /// <summary>
    /// 清理过期的权限规则
    /// </summary>
    public async Task<int> CleanupExpiredRulesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredRules = await _context.DataPermissionRules
            .Where(r => r.IsActive &&
                       r.EffectiveTo.HasValue &&
                       r.EffectiveTo < now)
            .ToListAsync(cancellationToken);

        foreach (var rule in expiredRules)
        {
            rule.IsActive = false;
            rule.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return expiredRules.Count;
    }

    /// <summary>
    /// 获取权限规则统计
    /// </summary>
    public async Task<DataPermissionRuleStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var statistics = new DataPermissionRuleStatistics
        {
            TotalRules = await _context.DataPermissionRules.CountAsync(cancellationToken),
            ActiveRules = await _context.DataPermissionRules.CountAsync(r => r.IsActive, cancellationToken),
            ExpiredRules = await _context.DataPermissionRules.CountAsync(
                r => r.EffectiveTo.HasValue && r.EffectiveTo < now, cancellationToken),
            TemporaryRules = await _context.DataPermissionRules.CountAsync(
                r => r.IsTemporary && r.IsActive, cancellationToken)
        };

        // 按资源类型分组的规则数
        var rulesByResourceType = await _context.DataPermissionRules
            .Where(r => r.IsActive)
            .GroupBy(r => r.ResourceType)
            .Select(g => new { ResourceType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        statistics.RulesByResourceType = rulesByResourceType.ToDictionary(x => x.ResourceType, x => x.Count);

        // 按操作类型分组的规则数
        var rulesByOperation = await _context.DataPermissionRules
            .Where(r => r.IsActive)
            .GroupBy(r => r.Operation)
            .Select(g => new { Operation = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        statistics.RulesByOperation = rulesByOperation.ToDictionary(x => x.Operation, x => x.Count);

        return statistics;
    }

    /// <summary>
    /// 根据资源类型和操作获取规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetRulesByResourceAndOperationAsync(
        string resourceType,
        DataOperation operation,
        CancellationToken cancellationToken = default)
    {
        return await _context.DataPermissionRules
            .Where(r => r.ResourceType == resourceType &&
                       r.Operation == operation &&
                       r.IsActive)
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户继承的角色权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetInheritedRoleRulesAsync(
        Guid userId,
        string? resourceType = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Enumerable.Empty<DataPermissionRule>();
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        var query = _context.DataPermissionRules
            .Where(r => roleIds.Contains(r.RoleId!.Value) && r.IsActive);

        if (!string.IsNullOrEmpty(resourceType))
        {
            query = query.Where(r => r.ResourceType == resourceType);
        }

        return await query
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取特定时间范围内的权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetRulesByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.DataPermissionRules
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
            .Include(r => r.User)
            .Include(r => r.Role)
            .Include(r => r.GrantedByUser)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 软删除权限规则
    /// </summary>
    public async Task<bool> SoftDeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var rule = await _context.DataPermissionRules.FindAsync(new object[] { ruleId }, cancellationToken);
        if (rule == null)
        {
            return false;
        }

        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 批量创建权限规则
    /// </summary>
    public async Task<bool> BatchCreateAsync(IEnumerable<DataPermissionRule> rules, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.DataPermissionRules.AddRangeAsync(rules, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 批量更新权限规则状态
    /// </summary>
    public async Task<int> BatchUpdateStatusAsync(IEnumerable<Guid> ruleIds, bool isActive, CancellationToken cancellationToken = default)
    {
        var ruleIdList = ruleIds.ToList();
        var rules = await _context.DataPermissionRules
            .Where(r => ruleIdList.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            rule.IsActive = isActive;
            rule.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return rules.Count;
    }
}