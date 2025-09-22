using MapleBlog.Domain.Entities;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 存储配额配置仓储接口
    /// </summary>
    public interface IStorageQuotaConfigurationRepository : IRepository<StorageQuotaConfiguration>
    {
        /// <summary>
        /// 根据角色获取配额配置
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>配额配置</returns>
        Task<StorageQuotaConfiguration?> GetByRoleAsync(UserRoleEnum role, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有激活的配额配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>激活的配额配置列表</returns>
        Task<IReadOnlyList<StorageQuotaConfiguration>> GetActiveConfigurationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取有效期内的配额配置
        /// </summary>
        /// <param name="effectiveDate">有效日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>有效的配额配置列表</returns>
        Task<IReadOnlyList<StorageQuotaConfiguration>> GetEffectiveConfigurationsAsync(DateTime? effectiveDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据角色获取有效的配额配置（考虑优先级和有效期）
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="effectiveDate">有效日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>有效的配额配置</returns>
        Task<StorageQuotaConfiguration?> GetEffectiveConfigurationByRoleAsync(UserRoleEnum role, DateTime? effectiveDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量更新或插入角色配额配置
        /// </summary>
        /// <param name="configurations">配额配置列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否操作成功</returns>
        Task<bool> UpsertConfigurationsAsync(IEnumerable<StorageQuotaConfiguration> configurations, CancellationToken cancellationToken = default);

        /// <summary>
        /// 停用过期的配额配置
        /// </summary>
        /// <param name="currentDate">当前日期</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>停用的配置数量</returns>
        Task<int> DeactivateExpiredConfigurationsAsync(DateTime? currentDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查角色是否存在配额配置
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在配置</returns>
        Task<bool> HasConfigurationAsync(UserRoleEnum role, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有角色的配额统计
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>角色配额统计</returns>
        Task<Dictionary<UserRoleEnum, long>> GetRoleQuotaStatsAsync(CancellationToken cancellationToken = default);
    }
}