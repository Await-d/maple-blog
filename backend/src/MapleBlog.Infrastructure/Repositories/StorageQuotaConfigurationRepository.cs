using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// 存储配额配置仓储实现
    /// </summary>
    public class StorageQuotaConfigurationRepository : BaseRepository<StorageQuotaConfiguration>, IStorageQuotaConfigurationRepository
    {
        public StorageQuotaConfigurationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<StorageQuotaConfiguration?> GetByRoleAsync(MapleBlog.Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            return await _context.StorageQuotaConfigurations
                .Where(c => c.Role == role)
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StorageQuotaConfiguration>> GetActiveConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StorageQuotaConfigurations
                .Where(c => c.IsActive)
                .OrderBy(c => c.Role)
                .ThenByDescending(c => c.Priority)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<StorageQuotaConfiguration>> GetEffectiveConfigurationsAsync(DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
        {
            var checkDate = effectiveDate ?? DateTime.UtcNow;

            return await _context.StorageQuotaConfigurations
                .Where(c => c.IsActive &&
                           c.EffectiveFrom <= checkDate &&
                           (c.EffectiveTo == null || c.EffectiveTo >= checkDate))
                .OrderBy(c => c.Role)
                .ThenByDescending(c => c.Priority)
                .ToListAsync(cancellationToken);
        }

        public async Task<StorageQuotaConfiguration?> GetEffectiveConfigurationByRoleAsync(MapleBlog.Domain.Enums.UserRole role, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
        {
            var checkDate = effectiveDate ?? DateTime.UtcNow;

            return await _context.StorageQuotaConfigurations
                .Where(c => c.Role == role &&
                           c.IsActive &&
                           c.EffectiveFrom <= checkDate &&
                           (c.EffectiveTo == null || c.EffectiveTo >= checkDate))
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> UpsertConfigurationsAsync(IEnumerable<StorageQuotaConfiguration> configurations, CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var config in configurations)
                {
                    var existing = await GetByRoleAsync(config.Role, cancellationToken);
                    if (existing != null)
                    {
                        // 更新现有配置
                        existing.MaxQuotaBytes = config.MaxQuotaBytes;
                        existing.MaxFileCount = config.MaxFileCount;
                        existing.MaxFileSize = config.MaxFileSize;
                        existing.AllowedFileTypes = config.AllowedFileTypes;
                        existing.ForbiddenFileTypes = config.ForbiddenFileTypes;
                        existing.AllowPublicFiles = config.AllowPublicFiles;
                        existing.WarningThreshold = config.WarningThreshold;
                        existing.CriticalThreshold = config.CriticalThreshold;
                        existing.AutoCleanupEnabled = config.AutoCleanupEnabled;
                        existing.AutoCleanupDays = config.AutoCleanupDays;
                        existing.IsActive = config.IsActive;
                        existing.Priority = config.Priority;
                        existing.Description = config.Description;
                        existing.EffectiveFrom = config.EffectiveFrom;
                        existing.EffectiveTo = config.EffectiveTo;
                        existing.UpdatedAt = DateTime.UtcNow;

                        Update(existing);
                    }
                    else
                    {
                        // 创建新配置
                        config.Id = Guid.NewGuid();
                        config.CreatedAt = DateTime.UtcNow;
                        config.UpdatedAt = DateTime.UtcNow;
                        await AddAsync(config, cancellationToken);
                    }
                }

                await SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> DeactivateExpiredConfigurationsAsync(DateTime? currentDate = null, CancellationToken cancellationToken = default)
        {
            var checkDate = currentDate ?? DateTime.UtcNow;

            var expiredConfigurations = await _context.StorageQuotaConfigurations
                .Where(c => c.IsActive &&
                           c.EffectiveTo != null &&
                           c.EffectiveTo < checkDate)
                .ToListAsync(cancellationToken);

            foreach (var config in expiredConfigurations)
            {
                config.IsActive = false;
                config.UpdatedAt = DateTime.UtcNow;
            }

            if (expiredConfigurations.Any())
            {
                UpdateRange(expiredConfigurations);
                await SaveChangesAsync(cancellationToken);
            }

            return expiredConfigurations.Count;
        }

        public async Task<bool> HasConfigurationAsync(MapleBlog.Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            return await _context.StorageQuotaConfigurations
                .AnyAsync(c => c.Role == role && c.IsActive, cancellationToken);
        }

        public async Task<Dictionary<MapleBlog.Domain.Enums.UserRole, long>> GetRoleQuotaStatsAsync(CancellationToken cancellationToken = default)
        {
            var stats = await _context.StorageQuotaConfigurations
                .Where(c => c.IsActive)
                .GroupBy(c => c.Role)
                .Select(g => new { Role = g.Key, TotalQuota = g.Sum(c => c.MaxQuotaBytes) })
                .ToDictionaryAsync(x => x.Role, x => x.TotalQuota, cancellationToken);

            return stats;
        }
    }
}