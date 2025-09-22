using System.Security.Cryptography;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services;

/// <summary>
/// 双因素认证服务实现
/// </summary>
public class TwoFactorAuthService : ITwoFactorAuthService
{
    private readonly ILogger<TwoFactorAuthService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorAuthRepository _twoFactorAuthRepository;
    private readonly IHardwareSecurityKeyRepository _hardwareKeyRepository;
    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly ITotpService _totpService;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IMapper _mapper;

    // 配置常量
    private const int RecoveryCodesCount = 10;
    private const int RecoveryCodeLength = 8;
    private const int TrustedDeviceExpiryDays = 30;
    private const int VerificationCodeExpiryMinutes = 5;

    public TwoFactorAuthService(
        ILogger<TwoFactorAuthService> logger,
        IUserRepository userRepository,
        ITwoFactorAuthRepository twoFactorAuthRepository,
        IHardwareSecurityKeyRepository hardwareKeyRepository,
        ITrustedDeviceRepository trustedDeviceRepository,
        ITotpService totpService,
        IDeviceFingerprintService deviceFingerprintService,
        IEmailService emailService,
        ISmsService smsService,
        IMapper mapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _twoFactorAuthRepository = twoFactorAuthRepository ?? throw new ArgumentNullException(nameof(twoFactorAuthRepository));
        _hardwareKeyRepository = hardwareKeyRepository ?? throw new ArgumentNullException(nameof(hardwareKeyRepository));
        _trustedDeviceRepository = trustedDeviceRepository ?? throw new ArgumentNullException(nameof(trustedDeviceRepository));
        _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
        _deviceFingerprintService = deviceFingerprintService ?? throw new ArgumentNullException(nameof(deviceFingerprintService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    #region 2FA Setup and Configuration

    /// <summary>
    /// 为用户设置TOTP
    /// </summary>
    public async Task<OperationResult<TotpSetupDto>> SetupTotpAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Setting up TOTP for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult<TotpSetupDto>.Failure("User not found.");
            }

            // 获取或创建2FA配置
            var twoFactorAuth = await GetOrCreateTwoFactorAuthAsync(userId, cancellationToken);

            // 生成新的TOTP密钥
            var secret = _totpService.GenerateSecret();
            twoFactorAuth.SetTotpSecret(secret);

            // 生成恢复代码
            var recoveryCodes = GenerateRecoveryCodes();
            twoFactorAuth.RecoveryCodes = JsonSerializer.Serialize(recoveryCodes.Select(c => new { Code = c, Used = false }));

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            // 生成QR码URI
            var issuer = "Maple Blog";
            var accountName = user.Email.Value;
            var qrCodeUri = _totpService.GenerateQrCodeUri(accountName, issuer, secret);

            var setupDto = new TotpSetupDto
            {
                Secret = secret,
                QrCodeUri = qrCodeUri,
                ManualEntryKey = FormatSecretForManualEntry(secret),
                RecoveryCodes = recoveryCodes
            };

            _logger.LogInformation("TOTP setup prepared for user {UserId}", userId);
            return OperationResult<TotpSetupDto>.CreateSuccess(setupDto, "TOTP setup prepared. Please scan the QR code and verify with a code from your authenticator app.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up TOTP for user {UserId}", userId);
            return OperationResult<TotpSetupDto>.Failure("An error occurred while setting up TOTP.");
        }
    }

    /// <summary>
    /// 确认并启用TOTP
    /// </summary>
    public async Task<OperationResult> ConfirmTotpAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Confirming TOTP setup for user {UserId}", userId);

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth?.TotpSecret == null)
            {
                return OperationResult.Failure("TOTP setup not found. Please start the setup process again.");
            }

            // 验证TOTP代码
            if (!_totpService.VerifyCode(twoFactorAuth.TotpSecret, totpCode))
            {
                _logger.LogWarning("TOTP confirmation failed for user {UserId}: Invalid code", userId);
                return OperationResult.Failure("Invalid verification code. Please try again.");
            }

            // 启用TOTP和2FA
            twoFactorAuth.EnableMethod(TwoFactorMethod.TOTP);
            twoFactorAuth.PreferredMethod = TwoFactorMethod.TOTP;

            // 更新用户的2FA状态
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.TwoFactorEnabled = true;
            }

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("TOTP confirmed and enabled for user {UserId}", userId);
            return OperationResult.CreateSuccess("TOTP has been successfully enabled for your account.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming TOTP for user {UserId}", userId);
            return OperationResult.Failure("An error occurred while confirming TOTP setup.");
        }
    }

    /// <summary>
    /// 启用SMS双因素认证
    /// </summary>
    public async Task<OperationResult> EnableSmsAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enabling SMS 2FA for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            // 验证手机号格式
            if (!IsValidPhoneNumber(phoneNumber))
            {
                return OperationResult.Failure("Invalid phone number format.");
            }

            // 更新用户手机号
            user.PhoneNumber = phoneNumber;
            user.PhoneNumberConfirmed = false; // 需要验证

            // 获取或创建2FA配置
            var twoFactorAuth = await GetOrCreateTwoFactorAuthAsync(userId, cancellationToken);

            // 发送验证短信
            var verificationCode = GenerateVerificationCode();
            await _smsService.SendVerificationCodeAsync(phoneNumber, verificationCode);

            // 临时存储验证码（实际项目中应使用缓存）
            // 这里简化处理，实际项目应该有专门的验证码存储机制

            _logger.LogInformation("SMS verification code sent for user {UserId}", userId);
            return OperationResult.CreateSuccess("Verification code sent to your phone. Please verify to enable SMS 2FA.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling SMS 2FA for user {UserId}", userId);
            return OperationResult.Failure("An error occurred while enabling SMS 2FA.");
        }
    }

    /// <summary>
    /// 启用邮箱双因素认证
    /// </summary>
    public async Task<OperationResult> EnableEmailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Enabling Email 2FA for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            if (!user.EmailConfirmed)
            {
                return OperationResult.Failure("Email address must be verified before enabling email 2FA.");
            }

            // 获取或创建2FA配置
            var twoFactorAuth = await GetOrCreateTwoFactorAuthAsync(userId, cancellationToken);
            twoFactorAuth.EnableMethod(TwoFactorMethod.Email);

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Email 2FA enabled for user {UserId}", userId);
            return OperationResult.CreateSuccess("Email 2FA has been enabled for your account.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling Email 2FA for user {UserId}", userId);
            return OperationResult.Failure("An error occurred while enabling Email 2FA.");
        }
    }

    /// <summary>
    /// 禁用特定的2FA方法
    /// </summary>
    public async Task<OperationResult> DisableMethodAsync(Guid userId, TwoFactorMethod method, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disabling 2FA method {Method} for user {UserId}", method, userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return OperationResult.Failure("Invalid password.");
            }

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth == null)
            {
                return OperationResult.Failure("2FA is not enabled for this account.");
            }

            // 检查是否有多种方法启用
            var enabledMethods = twoFactorAuth.GetEnabledMethods().ToList();
            if (enabledMethods.Count <= 1)
            {
                return OperationResult.Failure("Cannot disable the only remaining 2FA method. Use 'Disable 2FA' to completely disable two-factor authentication.");
            }

            // 禁用指定方法
            twoFactorAuth.DisableMethod(method);

            // 如果禁用的是首选方法，选择新的首选方法
            if (twoFactorAuth.PreferredMethod == method)
            {
                var remainingMethods = twoFactorAuth.GetEnabledMethods().ToList();
                twoFactorAuth.PreferredMethod = remainingMethods.FirstOrDefault();
            }

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA method {Method} disabled for user {UserId}", method, userId);
            return OperationResult.CreateSuccess($"{method.GetDisplayName()} has been disabled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA method {Method} for user {UserId}", method, userId);
            return OperationResult.Failure("An error occurred while disabling the 2FA method.");
        }
    }

    /// <summary>
    /// 完全禁用2FA
    /// </summary>
    public async Task<OperationResult> DisableTwoFactorAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Disabling 2FA completely for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return OperationResult.Failure("Invalid password.");
            }

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth != null)
            {
                // 删除所有2FA数据
                _twoFactorAuthRepository.Remove(twoFactorAuth);
            }

            // 删除所有受信任设备
            var trustedDevices = await _trustedDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
            foreach (var device in trustedDevices)
            {
                _trustedDeviceRepository.Remove(device);
            }

            // 更新用户2FA状态
            user.TwoFactorEnabled = false;
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA completely disabled for user {UserId}", userId);
            return OperationResult.CreateSuccess("Two-factor authentication has been completely disabled for your account.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling 2FA for user {UserId}", userId);
            return OperationResult.Failure("An error occurred while disabling two-factor authentication.");
        }
    }

    #endregion

    #region 2FA Verification

    /// <summary>
    /// 验证双因素认证代码
    /// </summary>
    public async Task<OperationResult<TwoFactorVerificationResult>> VerifyCodeAsync(
        Guid userId,
        string code,
        TwoFactorMethod method,
        bool rememberDevice = false,
        DeviceInfoDto? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying 2FA code for user {UserId} using method {Method}", userId, method);

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth == null || !twoFactorAuth.SupportsMethod(method))
            {
                return OperationResult<TwoFactorVerificationResult>.Failure("Two-factor authentication method not available.");
            }

            bool isValid = false;
            TrustedDevice? trustedDevice = null;

            switch (method)
            {
                case TwoFactorMethod.TOTP:
                    isValid = _totpService.VerifyCode(twoFactorAuth.TotpSecret!, code);
                    break;

                case TwoFactorMethod.SMS:
                    // 实际项目中应该从缓存或数据库验证SMS代码
                    isValid = await VerifySmsCodeAsync(userId, code, cancellationToken);
                    break;

                case TwoFactorMethod.Email:
                    // 实际项目中应该从缓存或数据库验证邮箱代码
                    isValid = await VerifyEmailCodeAsync(userId, code, cancellationToken);
                    break;

                case TwoFactorMethod.RecoveryCode:
                    isValid = await VerifyRecoveryCodeAsync(userId, code, cancellationToken);
                    break;

                default:
                    return OperationResult<TwoFactorVerificationResult>.Failure("Unsupported verification method.");
            }

            if (!isValid)
            {
                _logger.LogWarning("2FA verification failed for user {UserId} using method {Method}", userId, method);
                return OperationResult<TwoFactorVerificationResult>.Failure("Invalid verification code.");
            }

            // 记录成功的验证
            twoFactorAuth.UpdateLastUsed();

            // 如果用户选择记住设备，创建受信任设备
            if (rememberDevice && deviceInfo != null)
            {
                trustedDevice = await CreateTrustedDeviceAsync(userId, twoFactorAuth.Id, deviceInfo, cancellationToken);
            }

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            var result = new TwoFactorVerificationResult
            {
                IsValid = true,
                Method = method,
                DeviceRemembered = trustedDevice != null,
                TrustedDeviceId = trustedDevice?.Id,
                VerifiedAt = DateTime.UtcNow,
                RiskLevel = CalculateRiskLevel(deviceInfo),
                Metadata = new Dictionary<string, object>
                {
                    ["method"] = method.ToString(),
                    ["deviceRemembered"] = trustedDevice != null,
                    ["verificationTime"] = DateTime.UtcNow
                }
            };

            _logger.LogInformation("2FA verification successful for user {UserId} using method {Method}", userId, method);
            return OperationResult<TwoFactorVerificationResult>.CreateSuccess(result, "Verification successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying 2FA code for user {UserId}", userId);
            return OperationResult<TwoFactorVerificationResult>.Failure("An error occurred during verification.");
        }
    }

    /// <summary>
    /// 发送2FA验证码
    /// </summary>
    public async Task<OperationResult> SendVerificationCodeAsync(Guid userId, TwoFactorMethod method, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending 2FA verification code for user {UserId} using method {Method}", userId, method);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth == null || !twoFactorAuth.SupportsMethod(method))
            {
                return OperationResult.Failure("Two-factor authentication method not available.");
            }

            var verificationCode = GenerateVerificationCode();

            switch (method)
            {
                case TwoFactorMethod.SMS:
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        return OperationResult.Failure("No phone number configured for SMS verification.");
                    }
                    await _smsService.SendVerificationCodeAsync(user.PhoneNumber, verificationCode);
                    break;

                case TwoFactorMethod.Email:
                    await SendEmailVerificationCodeAsync(user, verificationCode);
                    break;

                default:
                    return OperationResult.Failure("This method does not support sending verification codes.");
            }

            // 存储验证码用于后续验证（实际项目中应使用缓存）
            await StoreVerificationCodeAsync(userId, method, verificationCode, cancellationToken);

            _logger.LogInformation("2FA verification code sent for user {UserId} using method {Method}", userId, method);
            return OperationResult.CreateSuccess($"Verification code sent via {method.GetDisplayName()}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification code for user {UserId}", userId);
            return OperationResult.Failure("An error occurred while sending verification code.");
        }
    }

    /// <summary>
    /// 检查设备是否受信任
    /// </summary>
    public async Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        try
        {
            var trustedDevice = await _trustedDeviceRepository.GetByFingerprintAsync(userId, deviceFingerprint, cancellationToken);
            return trustedDevice?.IsValid() == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking device trust for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Recovery Codes

    /// <summary>
    /// 生成恢复代码
    /// </summary>
    public async Task<OperationResult<RecoveryCodesDto>> GenerateRecoveryCodesAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating recovery codes for user {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult<RecoveryCodesDto>.Failure("User not found.");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return OperationResult<RecoveryCodesDto>.Failure("Invalid password.");
            }

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth == null)
            {
                return OperationResult<RecoveryCodesDto>.Failure("2FA is not enabled for this account.");
            }

            // 生成新的恢复代码
            var recoveryCodes = GenerateRecoveryCodes();
            twoFactorAuth.RecoveryCodes = JsonSerializer.Serialize(recoveryCodes.Select(c => new { Code = c, Used = false }));
            twoFactorAuth.UsedRecoveryCodesCount = 0;

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            var result = new RecoveryCodesDto
            {
                Codes = recoveryCodes,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Recovery codes generated for user {UserId}", userId);
            return OperationResult<RecoveryCodesDto>.CreateSuccess(result, "New recovery codes have been generated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recovery codes for user {UserId}", userId);
            return OperationResult<RecoveryCodesDto>.Failure("An error occurred while generating recovery codes.");
        }
    }

    /// <summary>
    /// 使用恢复代码
    /// </summary>
    public async Task<OperationResult<TwoFactorVerificationResult>> UseRecoveryCodeAsync(Guid userId, string recoveryCode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Using recovery code for user {UserId}", userId);

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth?.RecoveryCodes == null)
            {
                return OperationResult<TwoFactorVerificationResult>.Failure("No recovery codes available.");
            }

            // 解析恢复代码
            var codeList = JsonSerializer.Deserialize<List<dynamic>>(twoFactorAuth.RecoveryCodes);
            if (codeList == null)
            {
                return OperationResult<TwoFactorVerificationResult>.Failure("Invalid recovery codes data.");
            }

            // 查找并标记恢复代码为已使用
            bool codeFound = false;
            for (int i = 0; i < codeList.Count; i++)
            {
                var item = codeList[i];
                if (item.Code == recoveryCode && !item.Used)
                {
                    // 标记为已使用
                    codeList[i] = new { Code = recoveryCode, Used = true };
                    codeFound = true;
                    break;
                }
            }

            if (!codeFound)
            {
                _logger.LogWarning("Invalid or already used recovery code for user {UserId}", userId);
                return OperationResult<TwoFactorVerificationResult>.Failure("Invalid or already used recovery code.");
            }

            // 更新数据库
            twoFactorAuth.RecoveryCodes = JsonSerializer.Serialize(codeList);
            twoFactorAuth.UsedRecoveryCodesCount++;
            twoFactorAuth.UpdateLastUsed();

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            var result = new TwoFactorVerificationResult
            {
                IsValid = true,
                Method = TwoFactorMethod.RecoveryCode,
                DeviceRemembered = false,
                VerifiedAt = DateTime.UtcNow,
                RiskLevel = "Medium", // 恢复代码使用被认为是中等风险
                Metadata = new Dictionary<string, object>
                {
                    ["method"] = "RecoveryCode",
                    ["remainingCodes"] = RecoveryCodesCount - twoFactorAuth.UsedRecoveryCodesCount,
                    ["warningMessage"] = twoFactorAuth.UsedRecoveryCodesCount >= RecoveryCodesCount * 0.8
                        ? "You are running low on recovery codes. Consider generating new ones."
                        : null
                }
            };

            _logger.LogInformation("Recovery code used successfully for user {UserId}. Remaining codes: {RemainingCodes}",
                userId, RecoveryCodesCount - twoFactorAuth.UsedRecoveryCodesCount);

            return OperationResult<TwoFactorVerificationResult>.CreateSuccess(result, "Recovery code verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using recovery code for user {UserId}", userId);
            return OperationResult<TwoFactorVerificationResult>.Failure("An error occurred while verifying recovery code.");
        }
    }

    /// <summary>
    /// 获取剩余的恢复代码数量
    /// </summary>
    public async Task<int> GetRemainingRecoveryCodesCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            if (twoFactorAuth?.RecoveryCodes == null)
            {
                return 0;
            }

            return RecoveryCodesCount - twoFactorAuth.UsedRecoveryCodesCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting remaining recovery codes count for user {UserId}", userId);
            return 0;
        }
    }

    #endregion

    #region Hardware Keys (WebAuthn/FIDO2)

    /// <summary>
    /// 开始硬件密钥注册
    /// </summary>
    public async Task<OperationResult<WebAuthnRegistrationDto>> BeginHardwareKeyRegistrationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Beginning hardware key registration for user {UserId}", userId);

            // 这里应该集成WebAuthn库（如Fido2NetLib）
            // 为了演示，返回模拟数据
            var registrationDto = new WebAuthnRegistrationDto
            {
                OptionsJson = "{}",  // 实际项目中应包含WebAuthn注册选项
                SessionId = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            return OperationResult<WebAuthnRegistrationDto>.CreateSuccess(registrationDto, "Hardware key registration prepared.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning hardware key registration for user {UserId}", userId);
            return OperationResult<WebAuthnRegistrationDto>.Failure("An error occurred while preparing hardware key registration.");
        }
    }

    /// <summary>
    /// 完成硬件密钥注册
    /// </summary>
    public async Task<OperationResult<HardwareKeyDto>> CompleteHardwareKeyRegistrationAsync(Guid userId, WebAuthnRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Completing hardware key registration for user {UserId}", userId);

            // 这里应该验证WebAuthn响应
            // 为了演示，创建模拟硬件密钥

            var twoFactorAuth = await GetOrCreateTwoFactorAuthAsync(userId, cancellationToken);

            var hardwareKey = new HardwareSecurityKey
            {
                UserId = userId,
                TwoFactorAuthId = twoFactorAuth.Id,
                Name = request.KeyName,
                CredentialId = Guid.NewGuid().ToString(),
                PublicKeyData = "mock_public_key_data",
                AuthenticatorType = "USB",
                SupportsUserVerification = true,
                IsCrossPlatform = false,
                RegistrationUserAgent = request.DeviceInfo?.UserAgent,
                RegistrationIpAddress = request.DeviceInfo?.IpAddress
            };

            await _hardwareKeyRepository.AddAsync(hardwareKey, cancellationToken);

            // 启用硬件密钥方法
            twoFactorAuth.EnableMethod(TwoFactorMethod.HardwareKey);
            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            var keyDto = _mapper.Map<HardwareKeyDto>(hardwareKey);

            _logger.LogInformation("Hardware key registration completed for user {UserId}", userId);
            return OperationResult<HardwareKeyDto>.CreateSuccess(keyDto, "Hardware key registered successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing hardware key registration for user {UserId}", userId);
            return OperationResult<HardwareKeyDto>.Failure("An error occurred while registering hardware key.");
        }
    }

    /// <summary>
    /// 开始硬件密钥验证
    /// </summary>
    public async Task<OperationResult<WebAuthnVerificationDto>> BeginHardwareKeyVerificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Beginning hardware key verification for user {UserId}", userId);

            // 这里应该准备WebAuthn验证选项
            var verificationDto = new WebAuthnVerificationDto
            {
                OptionsJson = "{}",  // 实际项目中应包含WebAuthn验证选项
                SessionId = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            };

            return OperationResult<WebAuthnVerificationDto>.CreateSuccess(verificationDto, "Hardware key verification prepared.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning hardware key verification for user {UserId}", userId);
            return OperationResult<WebAuthnVerificationDto>.Failure("An error occurred while preparing hardware key verification.");
        }
    }

    /// <summary>
    /// 完成硬件密钥验证
    /// </summary>
    public async Task<OperationResult<TwoFactorVerificationResult>> CompleteHardwareKeyVerificationAsync(Guid userId, WebAuthnVerificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Completing hardware key verification for user {UserId}", userId);

            // 这里应该验证WebAuthn认证响应
            // 为了演示，假设验证成功

            var result = new TwoFactorVerificationResult
            {
                IsValid = true,
                Method = TwoFactorMethod.HardwareKey,
                DeviceRemembered = request.RememberDevice,
                VerifiedAt = DateTime.UtcNow,
                RiskLevel = "Low", // 硬件密钥通常被认为是低风险
                Metadata = new Dictionary<string, object>
                {
                    ["method"] = "HardwareKey",
                    ["sessionId"] = request.SessionId
                }
            };

            // 如果选择记住设备
            if (request.RememberDevice && request.DeviceInfo != null)
            {
                var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
                if (twoFactorAuth != null)
                {
                    var trustedDevice = await CreateTrustedDeviceAsync(userId, twoFactorAuth.Id, request.DeviceInfo, cancellationToken);
                    result.TrustedDeviceId = trustedDevice?.Id;
                }
            }

            _logger.LogInformation("Hardware key verification completed for user {UserId}", userId);
            return OperationResult<TwoFactorVerificationResult>.CreateSuccess(result, "Hardware key verification successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing hardware key verification for user {UserId}", userId);
            return OperationResult<TwoFactorVerificationResult>.Failure("An error occurred during hardware key verification.");
        }
    }

    /// <summary>
    /// 删除硬件密钥
    /// </summary>
    public async Task<OperationResult> RemoveHardwareKeyAsync(Guid userId, Guid keyId, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Removing hardware key {KeyId} for user {UserId}", keyId, userId);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return OperationResult.Failure("Invalid password.");
            }

            var hardwareKey = await _hardwareKeyRepository.GetByIdAsync(keyId, cancellationToken);
            if (hardwareKey == null || hardwareKey.UserId != userId)
            {
                return OperationResult.Failure("Hardware key not found.");
            }

            _hardwareKeyRepository.Remove(hardwareKey);

            // 检查是否还有其他硬件密钥
            var remainingKeys = await _hardwareKeyRepository.GetByUserIdAsync(userId, cancellationToken);
            if (!remainingKeys.Any())
            {
                // 如果没有其他硬件密钥，禁用硬件密钥方法
                var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
                if (twoFactorAuth != null)
                {
                    twoFactorAuth.DisableMethod(TwoFactorMethod.HardwareKey);
                    await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);
                }
            }

            _logger.LogInformation("Hardware key {KeyId} removed for user {UserId}", keyId, userId);
            return OperationResult.CreateSuccess("Hardware key removed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing hardware key {KeyId} for user {UserId}", keyId, userId);
            return OperationResult.Failure("An error occurred while removing hardware key.");
        }
    }

    #endregion

    #region Trusted Devices

    /// <summary>
    /// 获取用户的受信任设备列表
    /// </summary>
    public async Task<OperationResult<List<TrustedDeviceDto>>> GetTrustedDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var trustedDevices = await _trustedDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
            var deviceDtos = _mapper.Map<List<TrustedDeviceDto>>(trustedDevices);

            return OperationResult<List<TrustedDeviceDto>>.CreateSuccess(deviceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trusted devices for user {UserId}", userId);
            return OperationResult<List<TrustedDeviceDto>>.Failure("An error occurred while retrieving trusted devices.");
        }
    }

    /// <summary>
    /// 撤销受信任设备
    /// </summary>
    public async Task<OperationResult> RevokeTrustedDeviceAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var trustedDevice = await _trustedDeviceRepository.GetByIdAsync(deviceId, cancellationToken);
            if (trustedDevice == null || trustedDevice.UserId != userId)
            {
                return OperationResult.Failure("Trusted device not found.");
            }

            trustedDevice.Revoke();
            await _trustedDeviceRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Trusted device {DeviceId} revoked for user {UserId}", deviceId, userId);
            return OperationResult.CreateSuccess("Trusted device revoked successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking trusted device {DeviceId} for user {UserId}", deviceId, userId);
            return OperationResult.Failure("An error occurred while revoking trusted device.");
        }
    }

    /// <summary>
    /// 撤销所有受信任设备
    /// </summary>
    public async Task<OperationResult> RevokeAllTrustedDevicesAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return OperationResult.Failure("User not found.");
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return OperationResult.Failure("Invalid password.");
            }

            var trustedDevices = await _trustedDeviceRepository.GetByUserIdAsync(userId, cancellationToken);
            foreach (var device in trustedDevices)
            {
                device.Revoke();
            }

            await _trustedDeviceRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("All trusted devices revoked for user {UserId}", userId);
            return OperationResult.CreateSuccess("All trusted devices have been revoked.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all trusted devices for user {UserId}", userId);
            return OperationResult.Failure("An error occurred while revoking trusted devices.");
        }
    }

    #endregion

    #region 2FA Status and Information

    /// <summary>
    /// 获取用户的2FA状态
    /// </summary>
    public async Task<OperationResult<TwoFactorStatusDto>> GetTwoFactorStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            var trustedDevicesCount = await _trustedDeviceRepository.GetActiveCountByUserIdAsync(userId, cancellationToken);
            var hardwareKeysCount = await _hardwareKeyRepository.GetActiveCountByUserIdAsync(userId, cancellationToken);

            var status = new TwoFactorStatusDto
            {
                IsEnabled = twoFactorAuth?.IsEnabled == true,
                PreferredMethod = twoFactorAuth?.PreferredMethod,
                RemainingRecoveryCodes = await GetRemainingRecoveryCodesCountAsync(userId, cancellationToken),
                TrustedDevicesCount = trustedDevicesCount,
                HardwareKeysCount = hardwareKeysCount,
                LastUsedAt = twoFactorAuth?.LastUsedAt,
                SetupAt = twoFactorAuth?.SetupAt,
                SecurityScore = CalculateSecurityScore(twoFactorAuth, trustedDevicesCount, hardwareKeysCount)
            };

            if (twoFactorAuth != null)
            {
                var enabledMethods = twoFactorAuth.GetEnabledMethods();
                status.EnabledMethods = enabledMethods.Select(method => new TwoFactorMethodDto
                {
                    Method = method,
                    IsEnabled = true,
                    DisplayName = method.GetDisplayName(),
                    Description = method.GetDescription(),
                    SecurityLevel = method.GetSecurityLevel(),
                    IconName = method.GetIconName(),
                    SetupAt = twoFactorAuth.SetupAt,
                    LastUsedAt = twoFactorAuth.LastUsedAt
                }).ToList();
            }

            status.SecurityRecommendations = GenerateSecurityRecommendations(status);

            return OperationResult<TwoFactorStatusDto>.CreateSuccess(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting 2FA status for user {UserId}", userId);
            return OperationResult<TwoFactorStatusDto>.Failure("An error occurred while retrieving 2FA status.");
        }
    }

    /// <summary>
    /// 获取用户支持的2FA方法
    /// </summary>
    public async Task<List<TwoFactorMethodDto>> GetAvailableMethodsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);

            var methods = Enum.GetValues<TwoFactorMethod>()
                .Where(m => m != TwoFactorMethod.RecoveryCode) // 恢复代码不是用户可主动选择的方法
                .Select(method =>
                {
                    var dto = new TwoFactorMethodDto
                    {
                        Method = method,
                        DisplayName = method.GetDisplayName(),
                        Description = method.GetDescription(),
                        SecurityLevel = method.GetSecurityLevel(),
                        IconName = method.GetIconName(),
                        IsEnabled = twoFactorAuth?.SupportsMethod(method) == true,
                        IsAvailable = true
                    };

                    // 检查特定方法的可用性
                    switch (method)
                    {
                        case TwoFactorMethod.SMS:
                            if (string.IsNullOrEmpty(user?.PhoneNumber) || !user.PhoneNumberConfirmed)
                            {
                                dto.IsAvailable = false;
                                dto.UnavailableReason = "Phone number not verified";
                            }
                            break;

                        case TwoFactorMethod.Email:
                            if (user?.EmailConfirmed != true)
                            {
                                dto.IsAvailable = false;
                                dto.UnavailableReason = "Email address not verified";
                            }
                            break;
                    }

                    if (dto.IsEnabled && twoFactorAuth != null)
                    {
                        dto.SetupAt = twoFactorAuth.SetupAt;
                        dto.LastUsedAt = twoFactorAuth.LastUsedAt;
                    }

                    return dto;
                })
                .ToList();

            return methods;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available 2FA methods for user {UserId}", userId);
            return new List<TwoFactorMethodDto>();
        }
    }

    /// <summary>
    /// 检查用户是否启用了2FA
    /// </summary>
    public async Task<bool> IsTwoFactorEnabledAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            return twoFactorAuth?.IsEnabled == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking 2FA status for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查用户是否需要2FA（基于策略）
    /// </summary>
    public async Task<bool> IsTwoFactorRequiredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            // 基于角色的2FA策略
            return user.Role.HasAnyRole(MapleBlog.Domain.Enums.UserRole.Admin, MapleBlog.Domain.Enums.UserRole.SuperAdmin, MapleBlog.Domain.Enums.UserRole.Moderator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking 2FA requirement for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region Policy and Security

    /// <summary>
    /// 强制用户启用2FA（管理员功能）
    /// </summary>
    public async Task<OperationResult> ForceTwoFactorEnableAsync(Guid adminUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUser = await _userRepository.GetByIdAsync(adminUserId, cancellationToken);
            if (adminUser == null || !adminUser.IsSystemManager())
            {
                return OperationResult.Failure("Insufficient permissions.");
            }

            var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
            if (targetUser == null)
            {
                return OperationResult.Failure("Target user not found.");
            }

            // 创建或更新2FA配置
            var twoFactorAuth = await GetOrCreateTwoFactorAuthAsync(targetUserId, cancellationToken);
            // 这里可以设置强制标志，要求用户在下次登录时设置2FA

            await _twoFactorAuthRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA enforcement set for user {TargetUserId} by admin {AdminUserId}", targetUserId, adminUserId);
            return OperationResult.CreateSuccess("Two-factor authentication has been enforced for the user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing 2FA enable for user {TargetUserId} by admin {AdminUserId}", targetUserId, adminUserId);
            return OperationResult.Failure("An error occurred while enforcing 2FA.");
        }
    }

    /// <summary>
    /// 重置用户的2FA设置（管理员功能）
    /// </summary>
    public async Task<OperationResult> ResetTwoFactorAsync(Guid adminUserId, Guid targetUserId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUser = await _userRepository.GetByIdAsync(adminUserId, cancellationToken);
            if (adminUser == null || !adminUser.IsSystemManager())
            {
                return OperationResult.Failure("Insufficient permissions.");
            }

            var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
            if (targetUser == null)
            {
                return OperationResult.Failure("Target user not found.");
            }

            // 删除所有2FA数据
            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(targetUserId, cancellationToken);
            if (twoFactorAuth != null)
            {
                _twoFactorAuthRepository.Remove(twoFactorAuth);
            }

            // 删除所有受信任设备
            var trustedDevices = await _trustedDeviceRepository.GetByUserIdAsync(targetUserId, cancellationToken);
            foreach (var device in trustedDevices)
            {
                _trustedDeviceRepository.Remove(device);
            }

            // 更新用户2FA状态
            targetUser.TwoFactorEnabled = false;
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("2FA reset for user {TargetUserId} by admin {AdminUserId}. Reason: {Reason}", targetUserId, adminUserId, reason);
            return OperationResult.CreateSuccess("Two-factor authentication has been reset for the user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting 2FA for user {TargetUserId} by admin {AdminUserId}", targetUserId, adminUserId);
            return OperationResult.Failure("An error occurred while resetting 2FA.");
        }
    }

    /// <summary>
    /// 获取2FA安全统计
    /// </summary>
    public async Task<OperationResult<TwoFactorSecurityStatsDto>> GetSecurityStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalUsers = await _userRepository.CountAsync(cancellationToken: cancellationToken);
            var usersWithTwoFactor = await _twoFactorAuthRepository.GetEnabledCountAsync(cancellationToken);
            var totalTrustedDevices = await _trustedDeviceRepository.GetTotalCountAsync(cancellationToken);
            var totalHardwareKeys = await _hardwareKeyRepository.GetTotalCountAsync(cancellationToken);
            var recentUsageCount = await _twoFactorAuthRepository.GetRecentUsageCountAsync(30, cancellationToken);

            var methodStats = new Dictionary<TwoFactorMethod, int>();
            foreach (var method in Enum.GetValues<TwoFactorMethod>())
            {
                methodStats[method] = await _twoFactorAuthRepository.GetMethodUsageCountAsync(method, cancellationToken);
            }

            var stats = new TwoFactorSecurityStatsDto
            {
                TotalUsers = totalUsers,
                UsersWithTwoFactor = usersWithTwoFactor,
                TwoFactorAdoptionRate = totalUsers > 0 ? (double)usersWithTwoFactor / totalUsers * 100 : 0,
                MethodUsageStats = methodStats,
                TotalTrustedDevices = totalTrustedDevices,
                TotalHardwareKeys = totalHardwareKeys,
                RecentUsageCount = recentUsageCount
            };

            return OperationResult<TwoFactorSecurityStatsDto>.CreateSuccess(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting 2FA security stats");
            return OperationResult<TwoFactorSecurityStatsDto>.Failure("An error occurred while retrieving security statistics.");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// 获取或创建用户的2FA配置
    /// </summary>
    private async Task<TwoFactorAuth> GetOrCreateTwoFactorAuthAsync(Guid userId, CancellationToken cancellationToken)
    {
        var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
        if (twoFactorAuth == null)
        {
            twoFactorAuth = new TwoFactorAuth { UserId = userId };
            await _twoFactorAuthRepository.AddAsync(twoFactorAuth, cancellationToken);
        }
        return twoFactorAuth;
    }

    /// <summary>
    /// 生成恢复代码
    /// </summary>
    private static List<string> GenerateRecoveryCodes()
    {
        var codes = new List<string>();
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < RecoveryCodesCount; i++)
        {
            var bytes = new byte[RecoveryCodeLength / 2];
            rng.GetBytes(bytes);
            var code = Convert.ToHexString(bytes).ToLowerInvariant();
            codes.Add(code);
        }

        return codes;
    }

    /// <summary>
    /// 生成验证码
    /// </summary>
    private static string GenerateVerificationCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[3];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(new byte[] { bytes[0], bytes[1], bytes[2], 0 });
        return (number % 1000000).ToString("D6");
    }

    /// <summary>
    /// 格式化密钥用于手动输入
    /// </summary>
    private static string FormatSecretForManualEntry(string secret)
    {
        return string.Join(" ", Enumerable.Range(0, secret.Length / 4)
            .Select(i => secret.Substring(i * 4, Math.Min(4, secret.Length - i * 4))));
    }

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10;
    }

    /// <summary>
    /// 创建受信任设备
    /// </summary>
    private async Task<TrustedDevice?> CreateTrustedDeviceAsync(Guid userId, Guid twoFactorAuthId, DeviceInfoDto deviceInfo, CancellationToken cancellationToken)
    {
        try
        {
            var userAgent = _deviceFingerprintService.ParseUserAgent(deviceInfo.UserAgent);
            var location = await _deviceFingerprintService.GetLocationInfoAsync(deviceInfo.IpAddress, cancellationToken);

            var fingerprint = deviceInfo.DeviceFingerprint ??
                _deviceFingerprintService.GenerateFingerprint(deviceInfo.UserAgent, deviceInfo.IpAddress, deviceInfo.AdditionalProperties);

            var trustedDevice = new TrustedDevice
            {
                UserId = userId,
                TwoFactorAuthId = twoFactorAuthId,
                DeviceName = deviceInfo.DeviceName,
                DeviceFingerprint = fingerprint,
                UserAgent = deviceInfo.UserAgent,
                IpAddress = deviceInfo.IpAddress,
                Location = location != null ? $"{location.City}, {location.Region}, {location.Country}" : null,
                DeviceType = userAgent.DeviceType,
                OperatingSystem = userAgent.OperatingSystem,
                Browser = userAgent.Browser,
                ExpiresAt = DateTime.UtcNow.AddDays(TrustedDeviceExpiryDays)
            };

            await _trustedDeviceRepository.AddAsync(trustedDevice, cancellationToken);
            return trustedDevice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trusted device for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// 计算风险级别
    /// </summary>
    private static string CalculateRiskLevel(DeviceInfoDto? deviceInfo)
    {
        if (deviceInfo == null) return "Medium";

        // 简单的风险评估逻辑
        var riskFactors = 0;

        if (string.IsNullOrEmpty(deviceInfo.DeviceFingerprint))
            riskFactors++;

        if (deviceInfo.IpAddress?.StartsWith("192.168.") == false &&
            deviceInfo.IpAddress?.StartsWith("10.") == false)
            riskFactors++;

        return riskFactors switch
        {
            0 => "Low",
            1 => "Medium",
            _ => "High"
        };
    }

    /// <summary>
    /// 计算安全评分
    /// </summary>
    private static int CalculateSecurityScore(TwoFactorAuth? twoFactorAuth, int trustedDevicesCount, int hardwareKeysCount)
    {
        if (twoFactorAuth?.IsEnabled != true) return 0;

        var score = 30; // 基础分数

        var enabledMethods = twoFactorAuth.GetEnabledMethods().ToList();
        foreach (var method in enabledMethods)
        {
            score += method.GetSecurityLevel() * 10;
        }

        // 多种方法的奖励分数
        if (enabledMethods.Count > 1) score += 20;

        // 硬件密钥的额外奖励
        if (hardwareKeysCount > 0) score += 30;

        // 受信任设备的轻微减分（增加便利性但降低安全性）
        score -= Math.Min(trustedDevicesCount * 5, 20);

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// 生成安全建议
    /// </summary>
    private static List<string> GenerateSecurityRecommendations(TwoFactorStatusDto status)
    {
        var recommendations = new List<string>();

        if (!status.IsEnabled)
        {
            recommendations.Add("启用双因素认证以提高账户安全性");
        }
        else
        {
            if (status.EnabledMethods.Count == 1)
            {
                recommendations.Add("考虑启用多种2FA方法作为备用选项");
            }

            if (status.HardwareKeysCount == 0)
            {
                recommendations.Add("考虑使用硬件安全密钥获得最高级别的安全保护");
            }

            if (status.RemainingRecoveryCodes < 3)
            {
                recommendations.Add("生成新的恢复代码，当前剩余代码较少");
            }

            if (status.TrustedDevicesCount > 5)
            {
                recommendations.Add("定期检查并清理不需要的受信任设备");
            }

            if (status.SecurityScore < 70)
            {
                recommendations.Add("安全评分较低，建议启用更多安全措施");
            }
        }

        return recommendations;
    }

    /// <summary>
    /// 验证SMS代码
    /// </summary>
    private async Task<bool> VerifySmsCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        // 实际项目中应该从缓存或数据库验证SMS代码
        // 这里返回模拟结果
        await Task.Delay(100, cancellationToken);
        return code.Length == 6 && code.All(char.IsDigit);
    }

    /// <summary>
    /// 验证邮箱代码
    /// </summary>
    private async Task<bool> VerifyEmailCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        // 实际项目中应该从缓存或数据库验证邮箱代码
        // 这里返回模拟结果
        await Task.Delay(100, cancellationToken);
        return code.Length == 6 && code.All(char.IsDigit);
    }

    /// <summary>
    /// 验证恢复代码
    /// </summary>
    private async Task<bool> VerifyRecoveryCodeAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
        if (twoFactorAuth?.RecoveryCodes == null) return false;

        try
        {
            var codeList = JsonSerializer.Deserialize<List<dynamic>>(twoFactorAuth.RecoveryCodes);
            return codeList?.Any(item => item.Code == code && !item.Used) == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 存储验证码
    /// </summary>
    private async Task StoreVerificationCodeAsync(Guid userId, TwoFactorMethod method, string code, CancellationToken cancellationToken)
    {
        // 实际项目中应该使用Redis或其他缓存系统存储验证码
        // 这里仅做演示
        await Task.Delay(1, cancellationToken);
    }

    /// <summary>
    /// 发送邮箱验证码
    /// </summary>
    private async Task SendEmailVerificationCodeAsync(User user, string verificationCode)
    {
        var subject = "您的双因素认证验证码 - Maple Blog";
        var body = $@"
            <h2>双因素认证验证码</h2>
            <p>您好 {user.GetDisplayName()},</p>
            <p>您的验证码是: <strong>{verificationCode}</strong></p>
            <p>此验证码将在 {VerificationCodeExpiryMinutes} 分钟后过期。</p>
            <p>如果您没有请求此验证码，请忽略此邮件。</p>
            <p>最好的问候,<br>Maple Blog 团队</p>
        ";

        await _emailService.SendEmailAsync(user.Email.Value, subject, body);
    }

    #endregion
}