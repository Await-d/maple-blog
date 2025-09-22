using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;
using System.Security.Claims;

namespace MapleBlog.API.Controllers;

/// <summary>
/// 双因素认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TwoFactorAuthController : ControllerBase
{
    private readonly ITwoFactorAuthService _twoFactorAuthService;
    private readonly ILogger<TwoFactorAuthController> _logger;

    public TwoFactorAuthController(
        ITwoFactorAuthService twoFactorAuthService,
        ILogger<TwoFactorAuthController> logger)
    {
        _twoFactorAuthService = twoFactorAuthService ?? throw new ArgumentNullException(nameof(twoFactorAuthService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 2FA Setup and Configuration

    /// <summary>
    /// 获取2FA状态
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.GetTwoFactorStatusAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 获取可用的2FA方法
    /// </summary>
    [HttpGet("methods")]
    public async Task<IActionResult> GetAvailableMethods()
    {
        var userId = GetCurrentUserId();
        var methods = await _twoFactorAuthService.GetAvailableMethodsAsync(userId);
        return Ok(methods);
    }

    /// <summary>
    /// 设置TOTP认证
    /// </summary>
    [HttpPost("setup/totp")]
    public async Task<IActionResult> SetupTotp()
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.SetupTotpAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 确认TOTP设置
    /// </summary>
    [HttpPost("setup/totp/confirm")]
    public async Task<IActionResult> ConfirmTotp([FromBody] ConfirmTotpRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.ConfirmTotpAsync(userId, request.Code);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 启用SMS双因素认证
    /// </summary>
    [HttpPost("setup/sms")]
    public async Task<IActionResult> EnableSms([FromBody] EnableSmsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.EnableSmsAsync(userId, request.PhoneNumber);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 启用邮箱双因素认证
    /// </summary>
    [HttpPost("setup/email")]
    public async Task<IActionResult> EnableEmail()
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.EnableEmailAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 禁用特定的2FA方法
    /// </summary>
    [HttpPost("disable/method")]
    public async Task<IActionResult> DisableMethod([FromBody] DisableMethodRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.DisableMethodAsync(userId, request.Method, request.Password);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 完全禁用2FA
    /// </summary>
    [HttpPost("disable")]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.DisableTwoFactorAsync(userId, request.Password);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion

    #region 2FA Verification

    /// <summary>
    /// 验证2FA代码
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var deviceInfo = GetDeviceInfo();

        var result = await _twoFactorAuthService.VerifyCodeAsync(
            userId,
            request.Code,
            request.Method,
            request.RememberDevice,
            deviceInfo);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 发送验证码
    /// </summary>
    [HttpPost("send-code")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.SendVerificationCodeAsync(userId, request.Method);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 检查设备是否受信任
    /// </summary>
    [HttpGet("device/trusted")]
    public async Task<IActionResult> IsDeviceTrusted()
    {
        var userId = GetCurrentUserId();
        var deviceFingerprint = GetDeviceFingerprint();

        if (string.IsNullOrEmpty(deviceFingerprint))
            return BadRequest("Device fingerprint not available");

        var isTrusted = await _twoFactorAuthService.IsDeviceTrustedAsync(userId, deviceFingerprint);
        return Ok(new { IsTrusted = isTrusted });
    }

    #endregion

    #region Recovery Codes

    /// <summary>
    /// 生成恢复代码
    /// </summary>
    [HttpPost("recovery-codes/generate")]
    public async Task<IActionResult> GenerateRecoveryCodes([FromBody] GenerateRecoveryCodesRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.GenerateRecoveryCodesAsync(userId, request.Password);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 使用恢复代码
    /// </summary>
    [HttpPost("recovery-codes/use")]
    public async Task<IActionResult> UseRecoveryCode([FromBody] UseRecoveryCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.UseRecoveryCodeAsync(userId, request.RecoveryCode);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 获取剩余恢复代码数量
    /// </summary>
    [HttpGet("recovery-codes/count")]
    public async Task<IActionResult> GetRemainingRecoveryCodesCount()
    {
        var userId = GetCurrentUserId();
        var count = await _twoFactorAuthService.GetRemainingRecoveryCodesCountAsync(userId);
        return Ok(new { RemainingCount = count });
    }

    #endregion

    #region Hardware Keys (WebAuthn/FIDO2)

    /// <summary>
    /// 开始硬件密钥注册
    /// </summary>
    [HttpPost("hardware-key/register/begin")]
    public async Task<IActionResult> BeginHardwareKeyRegistration()
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.BeginHardwareKeyRegistrationAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 完成硬件密钥注册
    /// </summary>
    [HttpPost("hardware-key/register/complete")]
    public async Task<IActionResult> CompleteHardwareKeyRegistration([FromBody] WebAuthnRegistrationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        request.DeviceInfo = GetDeviceInfo();
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.CompleteHardwareKeyRegistrationAsync(userId, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 开始硬件密钥验证
    /// </summary>
    [HttpPost("hardware-key/verify/begin")]
    public async Task<IActionResult> BeginHardwareKeyVerification()
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.BeginHardwareKeyVerificationAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 完成硬件密钥验证
    /// </summary>
    [HttpPost("hardware-key/verify/complete")]
    public async Task<IActionResult> CompleteHardwareKeyVerification([FromBody] WebAuthnVerificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        request.DeviceInfo = GetDeviceInfo();
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.CompleteHardwareKeyVerificationAsync(userId, request);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 删除硬件密钥
    /// </summary>
    [HttpDelete("hardware-key/{keyId}")]
    public async Task<IActionResult> RemoveHardwareKey(Guid keyId, [FromBody] RemoveHardwareKeyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.RemoveHardwareKeyAsync(userId, keyId, request.Password);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion

    #region Trusted Devices

    /// <summary>
    /// 获取受信任设备列表
    /// </summary>
    [HttpGet("trusted-devices")]
    public async Task<IActionResult> GetTrustedDevices()
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.GetTrustedDevicesAsync(userId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 撤销受信任设备
    /// </summary>
    [HttpDelete("trusted-devices/{deviceId}")]
    public async Task<IActionResult> RevokeTrustedDevice(Guid deviceId)
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.RevokeTrustedDeviceAsync(userId, deviceId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 撤销所有受信任设备
    /// </summary>
    [HttpPost("trusted-devices/revoke-all")]
    public async Task<IActionResult> RevokeAllTrustedDevices([FromBody] RevokeAllDevicesRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        var result = await _twoFactorAuthService.RevokeAllTrustedDevicesAsync(userId, request.Password);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion

    #region Admin Functions

    /// <summary>
    /// 强制用户启用2FA（仅管理员）
    /// </summary>
    [HttpPost("admin/force-enable/{targetUserId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ForceTwoFactorEnable(Guid targetUserId)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _twoFactorAuthService.ForceTwoFactorEnableAsync(adminUserId, targetUserId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 重置用户2FA（仅管理员）
    /// </summary>
    [HttpPost("admin/reset/{targetUserId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ResetTwoFactor(Guid targetUserId, [FromBody] ResetTwoFactorRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUserId = GetCurrentUserId();
        var result = await _twoFactorAuthService.ResetTwoFactorAsync(adminUserId, targetUserId, request.Reason);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// 获取2FA安全统计（仅管理员）
    /// </summary>
    [HttpGet("admin/stats")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetSecurityStats()
    {
        var result = await _twoFactorAuthService.GetSecurityStatsAsync();

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// 获取设备信息
    /// </summary>
    private DeviceInfoDto GetDeviceInfo()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var ipAddress = GetClientIpAddress();

        return new DeviceInfoDto
        {
            UserAgent = userAgent,
            IpAddress = ipAddress,
            DeviceName = ExtractDeviceName(userAgent),
            DeviceFingerprint = GetDeviceFingerprint()
        };
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        // 检查X-Forwarded-For头（适用于代理/负载均衡器后的环境）
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // 检查X-Real-IP头
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp.Trim();
        }

        // 使用远程IP地址
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// 从用户代理提取设备名称
    /// </summary>
    private static string ExtractDeviceName(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        // 简单的设备名称提取逻辑
        if (userAgent.Contains("iPhone"))
            return "iPhone";
        if (userAgent.Contains("iPad"))
            return "iPad";
        if (userAgent.Contains("Android"))
            return "Android Device";
        if (userAgent.Contains("Windows"))
            return "Windows PC";
        if (userAgent.Contains("Macintosh"))
            return "Mac";
        if (userAgent.Contains("Linux"))
            return "Linux PC";

        return "Unknown Device";
    }

    /// <summary>
    /// 获取设备指纹
    /// </summary>
    private string? GetDeviceFingerprint()
    {
        // 设备指纹通常由前端JavaScript生成并通过头部传递
        return Request.Headers["X-Device-Fingerprint"].FirstOrDefault();
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// 确认TOTP请求
/// </summary>
public class ConfirmTotpRequest
{
    /// <summary>
    /// TOTP验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// 启用SMS请求
/// </summary>
public class EnableSmsRequest
{
    /// <summary>
    /// 手机号码
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// 禁用方法请求
/// </summary>
public class DisableMethodRequest
{
    /// <summary>
    /// 要禁用的方法
    /// </summary>
    public TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 密码确认
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 禁用2FA请求
/// </summary>
public class DisableTwoFactorRequest
{
    /// <summary>
    /// 密码确认
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 验证代码请求
/// </summary>
public class VerifyCodeRequest
{
    /// <summary>
    /// 验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 验证方法
    /// </summary>
    public TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 是否记住设备
    /// </summary>
    public bool RememberDevice { get; set; } = false;
}

/// <summary>
/// 发送验证码请求
/// </summary>
public class SendCodeRequest
{
    /// <summary>
    /// 发送方法
    /// </summary>
    public TwoFactorMethod Method { get; set; }
}

/// <summary>
/// 生成恢复代码请求
/// </summary>
public class GenerateRecoveryCodesRequest
{
    /// <summary>
    /// 密码确认
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 使用恢复代码请求
/// </summary>
public class UseRecoveryCodeRequest
{
    /// <summary>
    /// 恢复代码
    /// </summary>
    public string RecoveryCode { get; set; } = string.Empty;
}

/// <summary>
/// 删除硬件密钥请求
/// </summary>
public class RemoveHardwareKeyRequest
{
    /// <summary>
    /// 密码确认
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 撤销所有设备请求
/// </summary>
public class RevokeAllDevicesRequest
{
    /// <summary>
    /// 密码确认
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 重置2FA请求
/// </summary>
public class ResetTwoFactorRequest
{
    /// <summary>
    /// 重置原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

#endregion