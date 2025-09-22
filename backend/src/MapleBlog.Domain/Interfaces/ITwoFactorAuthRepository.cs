using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 双因素认证仓储接口
/// </summary>
public interface ITwoFactorAuthRepository : IRepository<TwoFactorAuth>
{
    /// <summary>
    /// 根据用户ID获取2FA配置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>2FA配置</returns>
    Task<TwoFactorAuth?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否启用了2FA
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启用</returns>
    Task<bool> IsEnabledForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取启用2FA的用户数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数量</returns>
    Task<int> GetEnabledCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取使用特定方法的用户数量
    /// </summary>
    /// <param name="method">2FA方法</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数量</returns>
    Task<int> GetMethodUsageCountAsync(TwoFactorMethod method, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近使用2FA的次数
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>使用次数</returns>
    Task<int> GetRecentUsageCountAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取需要2FA的用户列表（基于策略）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户ID列表</returns>
    Task<List<Guid>> GetUsersRequiring2FAAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新2FA配置
    /// </summary>
    /// <param name="configs">配置列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<bool> BatchUpdateAsync(IEnumerable<TwoFactorAuth> configs, CancellationToken cancellationToken = default);
}

/// <summary>
/// 硬件安全密钥仓储接口
/// </summary>
public interface IHardwareSecurityKeyRepository : IRepository<HardwareSecurityKey>
{
    /// <summary>
    /// 根据用户ID获取硬件密钥列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>硬件密钥列表</returns>
    Task<List<HardwareSecurityKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据凭证ID获取硬件密钥
    /// </summary>
    /// <param name="credentialId">凭证ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>硬件密钥</returns>
    Task<HardwareSecurityKey?> GetByCredentialIdAsync(string credentialId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的活跃硬件密钥数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>密钥数量</returns>
    Task<int> GetActiveCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取总的硬件密钥数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>总数量</returns>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近注册的硬件密钥
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>硬件密钥列表</returns>
    Task<List<HardwareSecurityKey>> GetRecentlyRegisteredAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新硬件密钥的使用计数器
    /// </summary>
    /// <param name="credentialId">凭证ID</param>
    /// <param name="newCounter">新计数器值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    Task<bool> UpdateSignatureCounterAsync(string credentialId, uint newCounter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量禁用用户的硬件密钥
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>禁用结果</returns>
    Task<bool> DisableAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 受信任设备仓储接口
/// </summary>
public interface ITrustedDeviceRepository : IRepository<TrustedDevice>
{
    /// <summary>
    /// 根据用户ID获取受信任设备列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受信任设备列表</returns>
    Task<List<TrustedDevice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据设备指纹获取受信任设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fingerprint">设备指纹</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受信任设备</returns>
    Task<TrustedDevice?> GetByFingerprintAsync(Guid userId, string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的活跃受信任设备数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备数量</returns>
    Task<int> GetActiveCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取总的受信任设备数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>总数量</returns>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取即将过期的受信任设备
    /// </summary>
    /// <param name="days">天数阈值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设备列表</returns>
    Task<List<TrustedDevice>> GetExpiringAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的受信任设备
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理数量</returns>
    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户的所有受信任设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销结果</returns>
    Task<bool> RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可疑的受信任设备（基于异常活动）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可疑设备列表</returns>
    Task<List<TrustedDevice>> GetSuspiciousDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按地理位置分组获取设备统计
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>位置统计</returns>
    Task<Dictionary<string, int>> GetDevicesByLocationAsync(CancellationToken cancellationToken = default);
}