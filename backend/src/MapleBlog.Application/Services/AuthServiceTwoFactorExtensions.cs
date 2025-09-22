using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Services;

/// <summary>
/// AuthService的双因素认证扩展
/// </summary>
public static class AuthServiceTwoFactorExtensions
{
    /// <summary>
    /// 带2FA检查的登录方法
    /// </summary>
    public static async Task<AuthResult> LoginWithTwoFactorAsync(
        this IAuthService authService,
        LoginRequest request,
        string clientIp,
        string userAgent,
        ITwoFactorAuthService twoFactorAuthService,
        IDeviceFingerprintService deviceFingerprintService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 首先进行基本的身份验证
            var basicAuthResult = await authService.LoginAsync(request, cancellationToken);

            // 如果基本认证失败，直接返回
            if (!basicAuthResult.Success || basicAuthResult.User == null)
            {
                return basicAuthResult;
            }

            var user = basicAuthResult.User;

            // 检查是否需要2FA
            var requires2FA = await twoFactorAuthService.IsTwoFactorRequiredAsync(user.Id, cancellationToken);
            var has2FA = await twoFactorAuthService.IsTwoFactorEnabledAsync(user.Id, cancellationToken);

            if (!requires2FA && !has2FA)
            {
                // 不需要2FA，返回成功结果
                return basicAuthResult;
            }

            // 检查设备是否受信任
            var deviceFingerprint = deviceFingerprintService.GenerateFingerprint(userAgent, clientIp);
            var isDeviceTrusted = await twoFactorAuthService.IsDeviceTrustedAsync(user.Id, deviceFingerprint, cancellationToken);

            if (isDeviceTrusted)
            {
                logger.LogInformation("User {UserId} login from trusted device, skipping 2FA", user.Id);
                return basicAuthResult;
            }

            // 需要2FA验证
            logger.LogInformation("User {UserId} requires 2FA verification", user.Id);
            return AuthResult.CreateRequiresTwoFactor(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during 2FA login process for: {EmailOrUsername}", request.EmailOrUsername);
            return AuthResult.Failure("An error occurred during login. Please try again.");
        }
    }

    /// <summary>
    /// 完成2FA登录验证
    /// </summary>
    public static async Task<AuthResult> CompleteTwoFactorLoginAsync(
        this IAuthService authService,
        CompleteTwoFactorLoginRequest request,
        string clientIp,
        string userAgent,
        ITwoFactorAuthService twoFactorAuthService,
        IDeviceFingerprintService deviceFingerprintService,
        IJwtService jwtService,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Completing 2FA login for user {UserId}", request.UserId);

            // 创建设备信息
            var deviceInfo = new DeviceInfoDto
            {
                UserAgent = userAgent,
                IpAddress = clientIp,
                DeviceName = ExtractDeviceName(userAgent),
                DeviceFingerprint = deviceFingerprintService.GenerateFingerprint(userAgent, clientIp)
            };

            // 验证2FA代码
            var verificationResult = await twoFactorAuthService.VerifyCodeAsync(
                request.UserId,
                request.Code,
                request.Method,
                request.RememberDevice,
                deviceInfo,
                cancellationToken);

            if (!verificationResult.Success || verificationResult.Data?.IsValid != true)
            {
                logger.LogWarning("2FA verification failed for user {UserId}", request.UserId);
                return AuthResult.Failure("Invalid verification code.");
            }

            // 2FA验证成功，生成JWT令牌
            var user = await authService.GetUserAsync(request.UserId, cancellationToken);
            if (!user.Success || user.Data == null)
            {
                return AuthResult.Failure("User not found.");
            }

            // 创建User实体实例来生成令牌（使用我们已有的数据）
            var userForToken = new MapleBlog.Domain.Entities.User
            {
                Id = request.UserId,
                UserName = user.Data.UserName!,
                Email = MapleBlog.Domain.ValueObjects.Email.Create(user.Data.Email!),
                Role = user.Data.Role
            };

            var tokens = await jwtService.GenerateTokensAsync(userForToken, cancellationToken);

            logger.LogInformation("2FA login completed successfully for user {UserId}", request.UserId);

            return AuthResult.CreateSuccess(user.Data, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing 2FA login for user {UserId}", request.UserId);
            return AuthResult.Failure("An error occurred during 2FA verification.");
        }
    }

    /// <summary>
    /// 从用户代理提取设备名称
    /// </summary>
    private static string ExtractDeviceName(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

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
}

/// <summary>
/// 完成2FA登录请求
/// </summary>
public class CompleteTwoFactorLoginRequest
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 验证码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 验证方法
    /// </summary>
    public Domain.Enums.TwoFactorMethod Method { get; set; }

    /// <summary>
    /// 是否记住设备
    /// </summary>
    public bool RememberDevice { get; set; } = false;
}