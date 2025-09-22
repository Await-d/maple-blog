using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 双因素认证审计日志实体
/// </summary>
public class TwoFactorAuditLog : BaseEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 审计事件类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// 事件描述
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 2FA方法
    /// </summary>
    public TwoFactorMethod? Method { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    [StringLength(255)]
    public string? FailureReason { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 地理位置
    /// </summary>
    [StringLength(255)]
    public string? Location { get; set; }

    /// <summary>
    /// 设备指纹
    /// </summary>
    [StringLength(64)]
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 风险评分（0-100）
    /// </summary>
    public int RiskScore { get; set; } = 0;

    /// <summary>
    /// 是否标记为可疑
    /// </summary>
    public bool IsSuspicious { get; set; } = false;

    /// <summary>
    /// 会话ID
    /// </summary>
    [StringLength(255)]
    public string? SessionId { get; set; }

    /// <summary>
    /// 额外的元数据（JSON格式）
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 创建成功的审计记录
    /// </summary>
    public static TwoFactorAuditLog CreateSuccess(
        Guid userId,
        string eventType,
        string description,
        TwoFactorMethod? method = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? sessionId = null,
        Dictionary<string, object>? metadata = null)
    {
        return new TwoFactorAuditLog
        {
            UserId = userId,
            EventType = eventType,
            Description = description,
            Method = method,
            IsSuccess = true,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null,
            RiskScore = CalculateRiskScore(ipAddress, userAgent, false)
        };
    }

    /// <summary>
    /// 创建失败的审计记录
    /// </summary>
    public static TwoFactorAuditLog CreateFailure(
        Guid userId,
        string eventType,
        string description,
        string failureReason,
        TwoFactorMethod? method = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? sessionId = null,
        Dictionary<string, object>? metadata = null)
    {
        return new TwoFactorAuditLog
        {
            UserId = userId,
            EventType = eventType,
            Description = description,
            Method = method,
            IsSuccess = false,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            Metadata = metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : null,
            RiskScore = CalculateRiskScore(ipAddress, userAgent, true),
            IsSuspicious = true // 失败事件默认标记为可疑
        };
    }

    /// <summary>
    /// 计算风险评分
    /// </summary>
    private static int CalculateRiskScore(string? ipAddress, string? userAgent, bool isFailure)
    {
        var score = 0;

        // 基础分数
        if (isFailure) score += 30;

        // IP地址风险评估
        if (string.IsNullOrEmpty(ipAddress))
            score += 20;
        else if (IsPrivateIpAddress(ipAddress))
            score -= 10; // 内网IP降低风险
        else
            score += 10; // 公网IP增加风险

        // 用户代理风险评估
        if (string.IsNullOrEmpty(userAgent))
            score += 15;
        else if (userAgent.Contains("bot", StringComparison.OrdinalIgnoreCase))
            score += 25; // 机器人用户代理高风险

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// 检查是否为私有IP地址
    /// </summary>
    private static bool IsPrivateIpAddress(string ipAddress)
    {
        if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
            return false;

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 127);
        }

        return false;
    }

    /// <summary>
    /// 更新地理位置信息
    /// </summary>
    public void UpdateLocation(string location)
    {
        Location = location;
        UpdateAuditFields();
    }

    /// <summary>
    /// 更新设备指纹
    /// </summary>
    public void UpdateDeviceFingerprint(string fingerprint)
    {
        DeviceFingerprint = fingerprint;
        UpdateAuditFields();
    }

    /// <summary>
    /// 标记为可疑
    /// </summary>
    public void MarkAsSuspicious(string reason)
    {
        IsSuspicious = true;
        if (!string.IsNullOrEmpty(reason))
        {
            var currentMetadata = string.IsNullOrEmpty(Metadata)
                ? new Dictionary<string, object>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata) ?? new Dictionary<string, object>();

            currentMetadata["suspiciousReason"] = reason;
            currentMetadata["markedSuspiciousAt"] = DateTime.UtcNow;

            Metadata = System.Text.Json.JsonSerializer.Serialize(currentMetadata);
        }
        UpdateAuditFields();
    }
}

/// <summary>
/// 2FA审计事件类型常量
/// </summary>
public static class TwoFactorAuditEventTypes
{
    public const string TotpSetup = "TOTP_SETUP";
    public const string TotpEnabled = "TOTP_ENABLED";
    public const string TotpDisabled = "TOTP_DISABLED";
    public const string TotpVerification = "TOTP_VERIFICATION";

    public const string SmsSetup = "SMS_SETUP";
    public const string SmsEnabled = "SMS_ENABLED";
    public const string SmsDisabled = "SMS_DISABLED";
    public const string SmsSent = "SMS_SENT";
    public const string SmsVerification = "SMS_VERIFICATION";

    public const string EmailSetup = "EMAIL_SETUP";
    public const string EmailEnabled = "EMAIL_ENABLED";
    public const string EmailDisabled = "EMAIL_DISABLED";
    public const string EmailSent = "EMAIL_SENT";
    public const string EmailVerification = "EMAIL_VERIFICATION";

    public const string RecoveryCodesGenerated = "RECOVERY_CODES_GENERATED";
    public const string RecoveryCodeUsed = "RECOVERY_CODE_USED";

    public const string HardwareKeyRegistrationBegin = "HARDWARE_KEY_REGISTRATION_BEGIN";
    public const string HardwareKeyRegistrationComplete = "HARDWARE_KEY_REGISTRATION_COMPLETE";
    public const string HardwareKeyVerification = "HARDWARE_KEY_VERIFICATION";
    public const string HardwareKeyRemoved = "HARDWARE_KEY_REMOVED";

    public const string TrustedDeviceAdded = "TRUSTED_DEVICE_ADDED";
    public const string TrustedDeviceRevoked = "TRUSTED_DEVICE_REVOKED";
    public const string AllTrustedDevicesRevoked = "ALL_TRUSTED_DEVICES_REVOKED";

    public const string TwoFactorEnabled = "TWO_FACTOR_ENABLED";
    public const string TwoFactorDisabled = "TWO_FACTOR_DISABLED";
    public const string TwoFactorForced = "TWO_FACTOR_FORCED";
    public const string TwoFactorReset = "TWO_FACTOR_RESET";

    public const string SecurityBreach = "SECURITY_BREACH";
    public const string SuspiciousActivity = "SUSPICIOUS_ACTIVITY";
    public const string PolicyViolation = "POLICY_VIOLATION";
}