using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories;

/// <summary>
/// 临时权限仓储实现
/// </summary>
public class TemporaryPermissionRepository : BaseRepository<TemporaryPermission>, ITemporaryPermissionRepository
{
    public TemporaryPermissionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 获取用户的有效临时权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetValidPermissionsAsync(
        Guid userId,
        string? resourceType = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.TemporaryPermissions
            .Where(p => p.UserId == userId &&
                       p.IsActive &&
                       !p.IsRevoked &&
                       p.EffectiveFrom <= now &&
                       p.ExpiresAt > now &&
                       (p.UsageLimit == 0 || p.UsedCount < p.UsageLimit));

        if (!string.IsNullOrEmpty(resourceType))
        {
            query = query.Where(p => p.ResourceType == resourceType);
        }

        return await query
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .Include(p => p.RevokedByUser)
            .OrderBy(p => p.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户对特定资源的临时权限
    /// </summary>
    public async Task<TemporaryPermission?> GetUserResourcePermissionAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.TemporaryPermissions
            .Where(p => p.UserId == userId &&
                       p.ResourceType == resourceType &&
                       p.ResourceId == resourceId &&
                       p.Operation == operation &&
                       p.IsActive &&
                       !p.IsRevoked &&
                       p.EffectiveFrom <= now &&
                       p.ExpiresAt > now &&
                       (p.UsageLimit == 0 || p.UsedCount < p.UsageLimit))
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 获取即将过期的临时权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetExpiringPermissionsAsync(
        int hoursBeforeExpiry = 24,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddHours(hoursBeforeExpiry);

        return await _context.TemporaryPermissions
            .Where(p => p.IsActive &&
                       !p.IsRevoked &&
                       p.ExpiresAt > now &&
                       p.ExpiresAt <= expiryThreshold)
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderBy(p => p.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取过期的临时权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.TemporaryPermissions
            .Where(p => p.IsActive &&
                       !p.IsRevoked &&
                       p.ExpiresAt <= now)
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderBy(p => p.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量撤销临时权限
    /// </summary>
    public async Task<int> BatchRevokeAsync(
        IEnumerable<Guid> permissionIds,
        Guid revokedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var permissionIdList = permissionIds.ToList();
        var permissions = await _context.TemporaryPermissions
            .Where(p => permissionIdList.Contains(p.Id) && p.IsActive && !p.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            permission.Revoke(revokedBy, reason);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return permissions.Count;
    }

    /// <summary>
    /// 清理过期的临时权限
    /// </summary>
    public async Task<int> CleanupExpiredAsync(int daysOld = 30, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

        var expiredPermissions = await _context.TemporaryPermissions
            .Where(p => p.ExpiresAt < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.TemporaryPermissions.RemoveRange(expiredPermissions);
        await _context.SaveChangesAsync(cancellationToken);

        return expiredPermissions.Count;
    }

    /// <summary>
    /// 获取用户委派的权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetDelegatedPermissionsAsync(
        Guid fromUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TemporaryPermissions
            .Where(p => p.DelegatedFrom == fromUserId &&
                       p.Type == TemporaryPermissionType.Delegated)
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户被委派的权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetReceivedDelegatedPermissionsAsync(
        Guid toUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TemporaryPermissions
            .Where(p => p.UserId == toUserId &&
                       p.Type == TemporaryPermissionType.Delegated &&
                       p.DelegatedFrom.HasValue)
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 记录权限使用
    /// </summary>
    public async Task<bool> RecordUsageAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await _context.TemporaryPermissions.FindAsync(new object[] { permissionId }, cancellationToken);
        if (permission == null || !permission.IsValid())
        {
            return false;
        }

        permission.RecordUsage();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 获取临时权限统计
    /// </summary>
    public async Task<TemporaryPermissionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var statistics = new TemporaryPermissionStatistics
        {
            TotalPermissions = await _context.TemporaryPermissions.CountAsync(cancellationToken),
            ValidPermissions = await _context.TemporaryPermissions.CountAsync(
                p => p.IsActive && !p.IsRevoked && p.EffectiveFrom <= now && p.ExpiresAt > now, cancellationToken),
            ExpiredPermissions = await _context.TemporaryPermissions.CountAsync(
                p => p.ExpiresAt <= now, cancellationToken),
            RevokedPermissions = await _context.TemporaryPermissions.CountAsync(
                p => p.IsRevoked, cancellationToken)
        };

        // 按类型分组
        var permissionsByType = await _context.TemporaryPermissions
            .GroupBy(p => p.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        statistics.PermissionsByType = permissionsByType.ToDictionary(x => x.Type, x => x.Count);

        // 按资源类型分组
        var permissionsByResourceType = await _context.TemporaryPermissions
            .GroupBy(p => p.ResourceType)
            .Select(g => new { ResourceType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        statistics.PermissionsByResourceType = permissionsByResourceType.ToDictionary(x => x.ResourceType, x => x.Count);

        // 按操作类型分组
        var permissionsByOperation = await _context.TemporaryPermissions
            .GroupBy(p => p.Operation)
            .Select(g => new { Operation = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        statistics.PermissionsByOperation = permissionsByOperation.ToDictionary(x => x.Operation, x => x.Count);

        // 计算平均持续时间
        var durations = await _context.TemporaryPermissions
            .Where(p => p.ExpiresAt > p.EffectiveFrom)
            .Select(p => EF.Functions.DateDiffHour(p.EffectiveFrom, p.ExpiresAt))
            .ToListAsync(cancellationToken);

        statistics.AverageDurationHours = durations.Any() ? durations.Average() : 0;

        // 计算平均使用次数
        var usageCounts = await _context.TemporaryPermissions
            .Select(p => p.UsedCount)
            .ToListAsync(cancellationToken);

        statistics.AverageUsageCount = usageCounts.Any() ? usageCounts.Average() : 0;

        return statistics;
    }

    /// <summary>
    /// 根据用户和资源获取权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetByUserAndResourceAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default)
    {
        return await _context.TemporaryPermissions
            .Where(p => p.UserId == userId &&
                       p.ResourceType == resourceType &&
                       p.ResourceId == resourceId &&
                       p.Operation == operation)
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .Include(p => p.RevokedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据用户和资源获取有效权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetActiveByUserAndResourceAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.TemporaryPermissions
            .Where(p => p.UserId == userId &&
                       p.ResourceType == resourceType &&
                       p.ResourceId == resourceId &&
                       p.Operation == operation &&
                       p.IsActive &&
                       !p.IsRevoked &&
                       p.EffectiveFrom <= now &&
                       p.ExpiresAt > now &&
                       (p.UsageLimit == 0 || p.UsedCount < p.UsageLimit))
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取按资源分组的权限数量
    /// </summary>
    public async Task<Dictionary<string, int>> GetPermissionCountByResourceAsync(CancellationToken cancellationToken = default)
    {
        var results = await _context.TemporaryPermissions
            .Where(p => p.IsActive && !p.IsRevoked)
            .GroupBy(p => p.ResourceType)
            .Select(g => new { ResourceType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return results.ToDictionary(x => x.ResourceType, x => x.Count);
    }

    /// <summary>
    /// 获取用户最近的权限活动
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetRecentUserActivityAsync(
        Guid userId,
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        return await _context.TemporaryPermissions
            .Where(p => p.UserId == userId &&
                       (p.CreatedAt >= startDate || p.LastUsedAt >= startDate))
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .Include(p => p.RevokedByUser)
            .OrderByDescending(p => p.LastUsedAt ?? p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量更新权限过期时间
    /// </summary>
    public async Task<int> BatchUpdateExpiryAsync(
        IEnumerable<Guid> permissionIds,
        DateTime newExpiryTime,
        CancellationToken cancellationToken = default)
    {
        var permissionIdList = permissionIds.ToList();
        var permissions = await _context.TemporaryPermissions
            .Where(p => permissionIdList.Contains(p.Id) && p.IsActive && !p.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            permission.ExpiresAt = newExpiryTime;
            permission.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return permissions.Count;
    }

    /// <summary>
    /// 检查用户是否有特定资源的有效权限
    /// </summary>
    public async Task<bool> HasValidPermissionAsync(
        Guid userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        CancellationToken cancellationToken = default)
    {
        var permission = await GetUserResourcePermissionAsync(userId, resourceType, resourceId, operation, cancellationToken);
        return permission != null && permission.IsValid();
    }

    /// <summary>
    /// 获取即将到达使用限制的权限
    /// </summary>
    public async Task<IEnumerable<TemporaryPermission>> GetNearUsageLimitAsync(
        double thresholdPercentage = 0.8,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.TemporaryPermissions
            .Where(p => p.IsActive &&
                       !p.IsRevoked &&
                       p.EffectiveFrom <= now &&
                       p.ExpiresAt > now &&
                       p.UsageLimit > 0 &&
                       p.UsedCount >= p.UsageLimit * thresholdPercentage)
            .Include(p => p.User)
            .Include(p => p.GrantedByUser)
            .Include(p => p.DelegatedFromUser)
            .OrderBy(p => p.UsageLimit - p.UsedCount)
            .ToListAsync(cancellationToken);
    }
}