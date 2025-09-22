namespace MapleBlog.Domain.Enums;

/// <summary>
/// 双因素认证方法枚举
/// </summary>
public enum TwoFactorMethod
{
    /// <summary>
    /// 基于时间的一次性密码（Google Authenticator等）
    /// </summary>
    TOTP = 1,

    /// <summary>
    /// 短信验证码
    /// </summary>
    SMS = 2,

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    Email = 3,

    /// <summary>
    /// 硬件安全密钥（WebAuthn/FIDO2）
    /// </summary>
    HardwareKey = 4,

    /// <summary>
    /// 备用恢复代码
    /// </summary>
    RecoveryCode = 5
}

/// <summary>
/// 双因素认证方法扩展方法
/// </summary>
public static class TwoFactorMethodExtensions
{
    /// <summary>
    /// 获取方法的显示名称
    /// </summary>
    public static string GetDisplayName(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.TOTP => "身份验证器应用",
            TwoFactorMethod.SMS => "短信验证码",
            TwoFactorMethod.Email => "邮箱验证码",
            TwoFactorMethod.HardwareKey => "硬件安全密钥",
            TwoFactorMethod.RecoveryCode => "备用恢复代码",
            _ => "未知方法"
        };
    }

    /// <summary>
    /// 获取方法的描述
    /// </summary>
    public static string GetDescription(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.TOTP => "使用Google Authenticator、Authy等应用生成的时间敏感代码",
            TwoFactorMethod.SMS => "通过短信接收验证码到您的手机",
            TwoFactorMethod.Email => "通过邮件接收验证码到您的邮箱",
            TwoFactorMethod.HardwareKey => "使用YubiKey等物理安全密钥进行验证",
            TwoFactorMethod.RecoveryCode => "当其他方法不可用时使用的一次性备用代码",
            _ => "未知认证方法"
        };
    }

    /// <summary>
    /// 获取方法的安全级别（1-5，5为最高）
    /// </summary>
    public static int GetSecurityLevel(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.HardwareKey => 5,  // 最高安全级别
            TwoFactorMethod.TOTP => 4,         // 高安全级别
            TwoFactorMethod.SMS => 2,          // 中等安全级别（易受SIM交换攻击）
            TwoFactorMethod.Email => 2,        // 中等安全级别（依赖邮箱安全）
            TwoFactorMethod.RecoveryCode => 1, // 低安全级别（一次性使用）
            _ => 0
        };
    }

    /// <summary>
    /// 检查方法是否需要外部通信
    /// </summary>
    public static bool RequiresExternalCommunication(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.SMS => true,
            TwoFactorMethod.Email => true,
            TwoFactorMethod.TOTP => false,
            TwoFactorMethod.HardwareKey => false,
            TwoFactorMethod.RecoveryCode => false,
            _ => false
        };
    }

    /// <summary>
    /// 检查方法是否可以离线使用
    /// </summary>
    public static bool CanWorkOffline(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.TOTP => true,
            TwoFactorMethod.HardwareKey => true,
            TwoFactorMethod.RecoveryCode => true,
            TwoFactorMethod.SMS => false,
            TwoFactorMethod.Email => false,
            _ => false
        };
    }

    /// <summary>
    /// 获取方法的图标名称（用于前端显示）
    /// </summary>
    public static string GetIconName(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.TOTP => "shield-check",
            TwoFactorMethod.SMS => "device-mobile",
            TwoFactorMethod.Email => "mail",
            TwoFactorMethod.HardwareKey => "key",
            TwoFactorMethod.RecoveryCode => "document-text",
            _ => "question-mark"
        };
    }

    /// <summary>
    /// 检查方法是否支持批量验证
    /// </summary>
    public static bool SupportsBatchVerification(this TwoFactorMethod method)
    {
        return method switch
        {
            TwoFactorMethod.RecoveryCode => true, // 恢复代码一次使用完就失效
            _ => false
        };
    }
}