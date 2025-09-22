using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 硬件安全密钥实体 - 用于WebAuthn/FIDO2认证
/// </summary>
public class HardwareSecurityKey : BaseEntity
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
    /// 密钥名称（用户定义）
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// WebAuthn凭证ID
    /// </summary>
    [Required]
    [StringLength(500)]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// 公钥数据（Base64编码）
    /// </summary>
    [Required]
    public string PublicKeyData { get; set; } = string.Empty;

    /// <summary>
    /// 签名计数器
    /// </summary>
    public uint SignatureCounter { get; set; } = 0;

    /// <summary>
    /// 认证器的AAGUID
    /// </summary>
    [StringLength(36)]
    public string? AuthenticatorAAGUID { get; set; }

    /// <summary>
    /// 认证器类型
    /// </summary>
    [StringLength(50)]
    public string AuthenticatorType { get; set; } = "unknown";

    /// <summary>
    /// 是否支持用户验证
    /// </summary>
    public bool SupportsUserVerification { get; set; } = false;

    /// <summary>
    /// 是否为跨平台设备
    /// </summary>
    public bool IsCrossPlatform { get; set; } = false;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 注册时的用户代理
    /// </summary>
    [StringLength(500)]
    public string? RegistrationUserAgent { get; set; }

    /// <summary>
    /// 注册时的IP地址
    /// </summary>
    [StringLength(45)]
    public string? RegistrationIpAddress { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// 导航属性 - 双因素认证配置
    /// </summary>
    public virtual TwoFactorAuth TwoFactorAuth { get; set; } = null!;

    /// <summary>
    /// 更新最后使用时间和签名计数器
    /// </summary>
    public void UpdateUsage(uint newCounter)
    {
        LastUsedAt = DateTime.UtcNow;
        SignatureCounter = newCounter;
        UpdateAuditFields();
    }

    /// <summary>
    /// 禁用硬件密钥
    /// </summary>
    public void Disable()
    {
        IsActive = false;
        UpdateAuditFields();
    }

    /// <summary>
    /// 启用硬件密钥
    /// </summary>
    public void Enable()
    {
        IsActive = true;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查签名计数器是否有效（防重放攻击）
    /// </summary>
    public bool IsCounterValid(uint newCounter)
    {
        // 对于某些认证器，计数器可能为0（不支持计数器）
        if (SignatureCounter == 0 && newCounter == 0)
            return true;

        // 新计数器必须大于存储的计数器
        return newCounter > SignatureCounter;
    }

    /// <summary>
    /// 获取设备描述信息
    /// </summary>
    public string GetDeviceDescription()
    {
        var platform = IsCrossPlatform ? "跨平台" : "平台绑定";
        var userVerification = SupportsUserVerification ? "支持用户验证" : "不支持用户验证";
        return $"{Name} ({AuthenticatorType}, {platform}, {userVerification})";
    }
}