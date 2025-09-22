using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 受信任设备实体 - 记住已通过2FA验证的设备
/// </summary>
public class TrustedDevice : BaseEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 双因素认证记录ID
    /// </summary>
    [Required]
    public Guid TwoFactorAuthId { get; set; }

    /// <summary>
    /// 设备名称（用户定义）
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 设备指纹（基于浏览器特征生成）
    /// </summary>
    [Required]
    [StringLength(64)]
    public string DeviceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 用户代理字符串
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 地理位置信息
    /// </summary>
    [StringLength(255)]
    public string? Location { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    [StringLength(50)]
    public string? DeviceType { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [StringLength(100)]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器信息
    /// </summary>
    [StringLength(100)]
    public string? Browser { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 信任级别（1-5，5为最高）
    /// </summary>
    public int TrustLevel { get; set; } = 1;

    /// <summary>
    /// 验证次数
    /// </summary>
    public int VerificationCount { get; set; } = 0;

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 双因素认证配置
    /// </summary>
    public virtual TwoFactorAuth TwoFactorAuth { get; set; } = null!;

    /// <summary>
    /// 检查设备是否已过期
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt <= DateTime.UtcNow;
    }

    /// <summary>
    /// 检查设备是否有效（激活且未过期）
    /// </summary>
    public bool IsValid()
    {
        return IsActive && !IsExpired();
    }

    /// <summary>
    /// 更新最后使用时间和信任级别
    /// </summary>
    public void UpdateUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        VerificationCount++;

        // 根据使用频率提升信任级别
        if (VerificationCount >= 10 && TrustLevel < 3)
            TrustLevel = 3;
        else if (VerificationCount >= 50 && TrustLevel < 4)
            TrustLevel = 4;
        else if (VerificationCount >= 100 && TrustLevel < 5)
            TrustLevel = 5;

        UpdateAuditFields();
    }

    /// <summary>
    /// 延长信任期限
    /// </summary>
    public void ExtendTrust(TimeSpan extension)
    {
        ExpiresAt = DateTime.UtcNow.Add(extension);
        UpdateAuditFields();
    }

    /// <summary>
    /// 撤销设备信任
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查设备指纹是否匹配
    /// </summary>
    public bool MatchesFingerprint(string fingerprint)
    {
        return DeviceFingerprint.Equals(fingerprint, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取设备描述信息
    /// </summary>
    public string GetDeviceDescription()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(DeviceName))
            parts.Add(DeviceName);

        if (!string.IsNullOrEmpty(DeviceType))
            parts.Add(DeviceType);

        if (!string.IsNullOrEmpty(OperatingSystem))
            parts.Add(OperatingSystem);

        if (!string.IsNullOrEmpty(Browser))
            parts.Add(Browser);

        if (!string.IsNullOrEmpty(Location))
            parts.Add(Location);

        return string.Join(" - ", parts);
    }

    /// <summary>
    /// 获取信任级别描述
    /// </summary>
    public string GetTrustLevelDescription()
    {
        return TrustLevel switch
        {
            1 => "低",
            2 => "一般",
            3 => "中等",
            4 => "高",
            5 => "很高",
            _ => "未知"
        };
    }
}