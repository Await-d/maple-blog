using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 双因素认证服务接口
/// </summary>
public interface ITwoFactorAuthService
{
    #region 2FA Setup and Configuration

    /// <summary>
    /// 为用户设置TOTP
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>TOTP设置信息</returns>
    Task<OperationResult<TotpSetupDto>> SetupTotpAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 确认并启用TOTP
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="totpCode">TOTP验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> ConfirmTotpAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用SMS双因素认证
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> EnableSmsAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用邮箱双因素认证
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> EnableEmailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用特定的2FA方法
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="method">要禁用的方法</param>
    /// <param name="password">用户密码确认</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> DisableMethodAsync(Guid userId, TwoFactorMethod method, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完全禁用2FA
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="password">用户密码确认</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> DisableTwoFactorAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    #endregion

    #region 2FA Verification

    /// <summary>
    /// 验证双因素认证代码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="code">验证码</param>
    /// <param name="method">验证方法</param>
    /// <param name="rememberDevice">是否记住设备</param>
    /// <param name="deviceInfo">设备信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<TwoFactorVerificationResult>> VerifyCodeAsync(
        Guid userId,
        string code,
        TwoFactorMethod method,
        bool rememberDevice = false,
        DeviceInfoDto? deviceInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送2FA验证码（SMS或邮箱）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="method">发送方法</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> SendVerificationCodeAsync(Guid userId, TwoFactorMethod method, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设备是否受信任
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceFingerprint">设备指纹</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否受信任</returns>
    Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);

    #endregion

    #region Recovery Codes

    /// <summary>
    /// 生成恢复代码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="password">用户密码确认</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恢复代码列表</returns>
    Task<OperationResult<RecoveryCodesDto>> GenerateRecoveryCodesAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用恢复代码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="recoveryCode">恢复代码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<TwoFactorVerificationResult>> UseRecoveryCodeAsync(Guid userId, string recoveryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取剩余的恢复代码数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>剩余数量</returns>
    Task<int> GetRemainingRecoveryCodesCountAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Hardware Keys (WebAuthn/FIDO2)

    /// <summary>
    /// 开始硬件密钥注册
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>注册选项</returns>
    Task<OperationResult<WebAuthnRegistrationDto>> BeginHardwareKeyRegistrationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成硬件密钥注册
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">注册请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>注册结果</returns>
    Task<OperationResult<HardwareKeyDto>> CompleteHardwareKeyRegistrationAsync(Guid userId, WebAuthnRegistrationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始硬件密钥验证
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证选项</returns>
    Task<OperationResult<WebAuthnVerificationDto>> BeginHardwareKeyVerificationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成硬件密钥验证
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">验证请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<OperationResult<TwoFactorVerificationResult>> CompleteHardwareKeyVerificationAsync(Guid userId, WebAuthnVerificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除硬件密钥
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="keyId">密钥ID</param>
    /// <param name="password">用户密码确认</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> RemoveHardwareKeyAsync(Guid userId, Guid keyId, string password, CancellationToken cancellationToken = default);

    #endregion

    #region Trusted Devices

    /// <summary>
    /// 获取用户的受信任设备列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受信任设备列表</returns>
    Task<OperationResult<List<TrustedDeviceDto>>> GetTrustedDevicesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销受信任设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceId">设备ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> RevokeTrustedDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销所有受信任设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="password">用户密码确认</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> RevokeAllTrustedDevicesAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    #endregion

    #region 2FA Status and Information

    /// <summary>
    /// 获取用户的2FA状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>2FA状态</returns>
    Task<OperationResult<TwoFactorStatusDto>> GetTwoFactorStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户支持的2FA方法
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>支持的方法列表</returns>
    Task<List<TwoFactorMethodDto>> GetAvailableMethodsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否启用了2FA
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启用</returns>
    Task<bool> IsTwoFactorEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否需要2FA（基于策略）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否需要2FA</returns>
    Task<bool> IsTwoFactorRequiredAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Policy and Security

    /// <summary>
    /// 强制用户启用2FA（管理员功能）
    /// </summary>
    /// <param name="adminUserId">管理员用户ID</param>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> ForceTwoFactorEnableAsync(Guid adminUserId, Guid targetUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重置用户的2FA设置（管理员功能）
    /// </summary>
    /// <param name="adminUserId">管理员用户ID</param>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="reason">重置原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<OperationResult> ResetTwoFactorAsync(Guid adminUserId, Guid targetUserId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取2FA安全统计
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安全统计</returns>
    Task<OperationResult<TwoFactorSecurityStatsDto>> GetSecurityStatsAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// TOTP服务接口
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// 生成TOTP密钥
    /// </summary>
    /// <returns>Base32编码的密钥</returns>
    string GenerateSecret();

    /// <summary>
    /// 生成QR码URI
    /// </summary>
    /// <param name="accountName">账户名（通常是邮箱）</param>
    /// <param name="issuer">发行方名称</param>
    /// <param name="secret">TOTP密钥</param>
    /// <returns>QR码URI</returns>
    string GenerateQrCodeUri(string accountName, string issuer, string secret);

    /// <summary>
    /// 验证TOTP代码
    /// </summary>
    /// <param name="secret">TOTP密钥</param>
    /// <param name="code">要验证的代码</param>
    /// <param name="window">时间窗口（默认1分钟）</param>
    /// <returns>是否有效</returns>
    bool VerifyCode(string secret, string code, int window = 1);

    /// <summary>
    /// 生成当前时间的TOTP代码
    /// </summary>
    /// <param name="secret">TOTP密钥</param>
    /// <returns>TOTP代码</returns>
    string GenerateCode(string secret);
}

/// <summary>
/// 设备指纹服务接口
/// </summary>
public interface IDeviceFingerprintService
{
    /// <summary>
    /// 生成设备指纹
    /// </summary>
    /// <param name="userAgent">用户代理</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="additionalFactors">其他因素</param>
    /// <returns>设备指纹</returns>
    string GenerateFingerprint(string userAgent, string ipAddress, Dictionary<string, string>? additionalFactors = null);

    /// <summary>
    /// 解析用户代理信息
    /// </summary>
    /// <param name="userAgent">用户代理字符串</param>
    /// <returns>解析结果</returns>
    UserAgentInfo ParseUserAgent(string userAgent);

    /// <summary>
    /// 获取地理位置信息
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>地理位置信息</returns>
    Task<LocationInfo?> GetLocationInfoAsync(string ipAddress, CancellationToken cancellationToken = default);
}

