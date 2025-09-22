using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// TOTP设置DTO
/// </summary>
public class TotpSetupDto
{
    /// <summary>
    /// TOTP密钥（Base32编码）
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// QR码URI
    /// </summary>
    public string QrCodeUri { get; set; } = string.Empty;

    /// <summary>
    /// 备用的手动输入密钥
    /// </summary>
    public string ManualEntryKey { get; set; } = string.Empty;

    /// <summary>
    /// 备用恢复代码
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = new();
}

/// <summary>
/// 双因素验证结果DTO
/// </summary>
public class TwoFactorVerificationResult
{
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 使用的验证方法
    /// </summary>
    public TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 设备是否被记住
    /// </summary>
    public bool DeviceRemembered { get; set; }

    /// <summary>
    /// 受信任设备ID
    /// </summary>
    public Guid? TrustedDeviceId { get; set; }

    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 验证的风险级别
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    /// 额外的安全信息
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// 恢复代码DTO
/// </summary>
public class RecoveryCodesDto
{
    /// <summary>
    /// 恢复代码列表
    /// </summary>
    public List<string> Codes { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 使用说明
    /// </summary>
    public string Instructions { get; set; } = "这些恢复代码只能使用一次。请将它们保存在安全的地方。";

    /// <summary>
    /// 每个代码只能使用一次的警告
    /// </summary>
    public string Warning { get; set; } = "每个恢复代码只能使用一次。使用后将失效。";
}

/// <summary>
/// 双因素认证状态DTO
/// </summary>
public class TwoFactorStatusDto
{
    /// <summary>
    /// 是否启用2FA
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 启用的方法列表
    /// </summary>
    public List<TwoFactorMethodDto> EnabledMethods { get; set; } = new();

    /// <summary>
    /// 首选方法
    /// </summary>
    public TwoFactorMethod? PreferredMethod { get; set; }

    /// <summary>
    /// 剩余恢复代码数量
    /// </summary>
    public int RemainingRecoveryCodes { get; set; }

    /// <summary>
    /// 受信任设备数量
    /// </summary>
    public int TrustedDevicesCount { get; set; }

    /// <summary>
    /// 硬件密钥数量
    /// </summary>
    public int HardwareKeysCount { get; set; }

    /// <summary>
    /// 最后使用2FA的时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 设置时间
    /// </summary>
    public DateTime? SetupAt { get; set; }

    /// <summary>
    /// 是否强制启用（由管理员要求）
    /// </summary>
    public bool IsForced { get; set; }

    /// <summary>
    /// 安全级别评分
    /// </summary>
    public int SecurityScore { get; set; }

    /// <summary>
    /// 安全建议
    /// </summary>
    public List<string> SecurityRecommendations { get; set; } = new();
}

/// <summary>
/// 双因素认证方法DTO
/// </summary>
public class TwoFactorMethodDto
{
    /// <summary>
    /// 方法类型
    /// </summary>
    public TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 安全级别（1-5）
    /// </summary>
    public int SecurityLevel { get; set; }

    /// <summary>
    /// 图标名称
    /// </summary>
    public string IconName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// 不可用原因
    /// </summary>
    public string? UnavailableReason { get; set; }

    /// <summary>
    /// 设置时间
    /// </summary>
    public DateTime? SetupAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// 硬件密钥DTO
/// </summary>
public class HardwareKeyDto
{
    /// <summary>
    /// 密钥ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 密钥名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 认证器类型
    /// </summary>
    public string AuthenticatorType { get; set; } = string.Empty;

    /// <summary>
    /// 是否支持用户验证
    /// </summary>
    public bool SupportsUserVerification { get; set; }

    /// <summary>
    /// 是否为跨平台设备
    /// </summary>
    public bool IsCrossPlatform { get; set; }

    /// <summary>
    /// 注册时间
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public uint UsageCount { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 设备描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 受信任设备DTO
/// </summary>
public class TrustedDeviceDto
{
    /// <summary>
    /// 设备ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 地理位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 信任级别
    /// </summary>
    public int TrustLevel { get; set; }

    /// <summary>
    /// 信任级别描述
    /// </summary>
    public string TrustLevelDescription { get; set; } = string.Empty;

    /// <summary>
    /// 添加时间
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 验证次数
    /// </summary>
    public int VerificationCount { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// 设备描述
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 设备信息DTO
/// </summary>
public class DeviceInfoDto
{
    /// <summary>
    /// 设备名称
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 用户代理
    /// </summary>
    public string UserAgent { get; set; } = string.Empty;

    /// <summary>
    /// IP地址
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 设备指纹
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 额外属性
    /// </summary>
    public Dictionary<string, string>? AdditionalProperties { get; set; }
}

/// <summary>
/// WebAuthn注册DTO
/// </summary>
public class WebAuthnRegistrationDto
{
    /// <summary>
    /// 注册选项（JSON）
    /// </summary>
    public string OptionsJson { get; set; } = string.Empty;

    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// WebAuthn注册请求DTO
/// </summary>
public class WebAuthnRegistrationRequest
{
    /// <summary>
    /// 密钥名称
    /// </summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 认证器响应（JSON）
    /// </summary>
    public string AuthenticatorResponse { get; set; } = string.Empty;

    /// <summary>
    /// 设备信息
    /// </summary>
    public DeviceInfoDto? DeviceInfo { get; set; }
}

/// <summary>
/// WebAuthn验证DTO
/// </summary>
public class WebAuthnVerificationDto
{
    /// <summary>
    /// 验证选项（JSON）
    /// </summary>
    public string OptionsJson { get; set; } = string.Empty;

    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// WebAuthn验证请求DTO
/// </summary>
public class WebAuthnVerificationRequest
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// 认证器响应（JSON）
    /// </summary>
    public string AuthenticatorResponse { get; set; } = string.Empty;

    /// <summary>
    /// 是否记住设备
    /// </summary>
    public bool RememberDevice { get; set; }

    /// <summary>
    /// 设备信息
    /// </summary>
    public DeviceInfoDto? DeviceInfo { get; set; }
}

/// <summary>
/// 双因素认证安全统计DTO
/// </summary>
public class TwoFactorSecurityStatsDto
{
    /// <summary>
    /// 总用户数
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// 启用2FA的用户数
    /// </summary>
    public int UsersWithTwoFactor { get; set; }

    /// <summary>
    /// 2FA启用率
    /// </summary>
    public double TwoFactorAdoptionRate { get; set; }

    /// <summary>
    /// 各方法使用统计
    /// </summary>
    public Dictionary<TwoFactorMethod, int> MethodUsageStats { get; set; } = new();

    /// <summary>
    /// 受信任设备总数
    /// </summary>
    public int TotalTrustedDevices { get; set; }

    /// <summary>
    /// 硬件密钥总数
    /// </summary>
    public int TotalHardwareKeys { get; set; }

    /// <summary>
    /// 最近30天的2FA使用次数
    /// </summary>
    public int RecentUsageCount { get; set; }

    /// <summary>
    /// 最近的安全事件
    /// </summary>
    public List<SecurityEventSummaryDto> RecentSecurityEvents { get; set; } = new();

    /// <summary>
    /// 统计生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 安全事件摘要DTO
/// </summary>
public class SecurityEventSummaryDto
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 事件描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// 严重程度
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }
}