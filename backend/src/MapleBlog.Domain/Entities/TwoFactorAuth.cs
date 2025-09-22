using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 双因素认证实体 - 存储用户的2FA配置信息
/// </summary>
public class TwoFactorAuth : BaseEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 是否启用2FA
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// TOTP密钥（Base32编码）
    /// </summary>
    [StringLength(255)]
    public string? TotpSecret { get; set; }

    /// <summary>
    /// 备用恢复代码（JSON格式存储）
    /// </summary>
    public string? RecoveryCodes { get; set; }

    /// <summary>
    /// 已使用的恢复代码数量
    /// </summary>
    public int UsedRecoveryCodesCount { get; set; } = 0;

    /// <summary>
    /// 是否启用SMS验证
    /// </summary>
    public bool SmsEnabled { get; set; } = false;

    /// <summary>
    /// 是否启用邮箱验证
    /// </summary>
    public bool EmailEnabled { get; set; } = false;

    /// <summary>
    /// 是否启用硬件安全密钥
    /// </summary>
    public bool HardwareKeyEnabled { get; set; } = false;

    /// <summary>
    /// 首选的2FA方法
    /// </summary>
    public TwoFactorMethod PreferredMethod { get; set; } = TwoFactorMethod.TOTP;

    /// <summary>
    /// 2FA配置的创建时间
    /// </summary>
    public DateTime SetupAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后使用2FA的时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 硬件安全密钥
    /// </summary>
    public virtual ICollection<HardwareSecurityKey> HardwareKeys { get; set; } = new List<HardwareSecurityKey>();

    /// <summary>
    /// 导航属性 - 受信任设备
    /// </summary>
    public virtual ICollection<TrustedDevice> TrustedDevices { get; set; } = new List<TrustedDevice>();

    /// <summary>
    /// 检查是否有任何2FA方法启用
    /// </summary>
    public bool HasAnyMethodEnabled()
    {
        return IsEnabled && (
            !string.IsNullOrEmpty(TotpSecret) ||
            SmsEnabled ||
            EmailEnabled ||
            HardwareKeyEnabled
        );
    }

    /// <summary>
    /// 获取所有启用的2FA方法
    /// </summary>
    public IEnumerable<TwoFactorMethod> GetEnabledMethods()
    {
        var methods = new List<TwoFactorMethod>();

        if (!IsEnabled) return methods;

        if (!string.IsNullOrEmpty(TotpSecret))
            methods.Add(TwoFactorMethod.TOTP);

        if (SmsEnabled)
            methods.Add(TwoFactorMethod.SMS);

        if (EmailEnabled)
            methods.Add(TwoFactorMethod.Email);

        if (HardwareKeyEnabled)
            methods.Add(TwoFactorMethod.HardwareKey);

        return methods;
    }

    /// <summary>
    /// 更新最后使用时间
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置TOTP密钥
    /// </summary>
    public void SetTotpSecret(string secret)
    {
        TotpSecret = secret;
        UpdateAuditFields();
    }

    /// <summary>
    /// 启用特定的2FA方法
    /// </summary>
    public void EnableMethod(TwoFactorMethod method)
    {
        IsEnabled = true;

        switch (method)
        {
            case TwoFactorMethod.SMS:
                SmsEnabled = true;
                break;
            case TwoFactorMethod.Email:
                EmailEnabled = true;
                break;
            case TwoFactorMethod.HardwareKey:
                HardwareKeyEnabled = true;
                break;
            case TwoFactorMethod.TOTP:
                // TOTP需要设置密钥
                break;
        }

        UpdateAuditFields();
    }

    /// <summary>
    /// 禁用特定的2FA方法
    /// </summary>
    public void DisableMethod(TwoFactorMethod method)
    {
        switch (method)
        {
            case TwoFactorMethod.TOTP:
                TotpSecret = null;
                break;
            case TwoFactorMethod.SMS:
                SmsEnabled = false;
                break;
            case TwoFactorMethod.Email:
                EmailEnabled = false;
                break;
            case TwoFactorMethod.HardwareKey:
                HardwareKeyEnabled = false;
                break;
        }

        // 如果没有任何方法启用，则禁用2FA
        if (!HasAnyMethodEnabled())
        {
            IsEnabled = false;
        }

        UpdateAuditFields();
    }

    /// <summary>
    /// 检查是否支持指定的2FA方法
    /// </summary>
    public bool SupportsMethod(TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.TOTP => !string.IsNullOrEmpty(TotpSecret),
            TwoFactorMethod.SMS => SmsEnabled,
            TwoFactorMethod.Email => EmailEnabled,
            TwoFactorMethod.HardwareKey => HardwareKeyEnabled,
            TwoFactorMethod.RecoveryCode => !string.IsNullOrEmpty(RecoveryCodes),
            _ => false
        };
    }
}