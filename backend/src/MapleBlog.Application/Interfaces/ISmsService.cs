using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// SMS服务接口
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// 发送验证码短信
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="verificationCode">验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<bool> SendVerificationCodeAsync(string phoneNumber, string verificationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送自定义短信
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="message">短信内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <returns>是否有效</returns>
    bool IsValidPhoneNumber(string phoneNumber);

    /// <summary>
    /// 标准化手机号格式
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="countryCode">国家代码（默认+86）</param>
    /// <returns>标准化后的手机号</returns>
    string NormalizePhoneNumber(string phoneNumber, string countryCode = "+86");
}

/// <summary>
/// SMS服务实现（演示版本）
/// </summary>
public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly HttpClient _httpClient;

    // 配置常量 - 实际项目中应从配置文件读取
    private const string SmsApiUrl = "https://api.sms-provider.com/send";
    private const string ApiKey = "your-sms-api-key";

    public SmsService(ILogger<SmsService> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 发送验证码短信
    /// </summary>
    public async Task<bool> SendVerificationCodeAsync(string phoneNumber, string verificationCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsValidPhoneNumber(phoneNumber))
            {
                _logger.LogWarning("Invalid phone number format: {PhoneNumber}", phoneNumber);
                return false;
            }

            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            var message = $"您的Maple Blog验证码是: {verificationCode}，5分钟内有效。请勿向他人泄露。";

            var success = await SendSmsAsync(normalizedPhone, message, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Verification code SMS sent successfully to {PhoneNumber}", MaskPhoneNumber(normalizedPhone));
            }
            else
            {
                _logger.LogWarning("Failed to send verification code SMS to {PhoneNumber}", MaskPhoneNumber(normalizedPhone));
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification code SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
    }

    /// <summary>
    /// 发送自定义短信
    /// </summary>
    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsValidPhoneNumber(phoneNumber))
            {
                _logger.LogWarning("Invalid phone number format: {PhoneNumber}", phoneNumber);
                return false;
            }

            var normalizedPhone = NormalizePhoneNumber(phoneNumber);

            // 检查消息长度
            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                _logger.LogWarning("Invalid SMS message length: {Length}", message?.Length ?? 0);
                return false;
            }

            // 实际项目中这里应该调用真实的SMS API
            // 为了演示，我们模拟发送过程
            var success = await SendSmsViaMockProvider(normalizedPhone, message, cancellationToken);

            if (success)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}", MaskPhoneNumber(normalizedPhone));
            }
            else
            {
                _logger.LogWarning("Failed to send SMS to {PhoneNumber}", MaskPhoneNumber(normalizedPhone));
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
    }

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // 移除所有非数字字符（除了+号）
        var cleanNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d+]", "");

        // 检查基本格式
        if (cleanNumber.Length < 10 || cleanNumber.Length > 15)
            return false;

        // 检查是否以+开头或纯数字
        if (cleanNumber.StartsWith("+"))
        {
            return cleanNumber.Length >= 11 && cleanNumber.Substring(1).All(char.IsDigit);
        }

        return cleanNumber.All(char.IsDigit);
    }

    /// <summary>
    /// 标准化手机号格式
    /// </summary>
    public string NormalizePhoneNumber(string phoneNumber, string countryCode = "+86")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return string.Empty;

        // 移除所有非数字字符（除了+号）
        var cleanNumber = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d+]", "");

        // 如果已经包含国家代码，直接返回
        if (cleanNumber.StartsWith("+"))
            return cleanNumber;

        // 如果以0开头（中国手机号格式），移除0
        if (cleanNumber.StartsWith("0"))
            cleanNumber = cleanNumber.Substring(1);

        // 添加国家代码
        return $"{countryCode}{cleanNumber}";
    }

    #region Private Methods

    /// <summary>
    /// 通过模拟SMS提供商发送短信
    /// 实际项目中应该替换为真实的SMS API调用
    /// </summary>
    private async Task<bool> SendSmsViaMockProvider(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        try
        {
            // 模拟API调用延迟
            await Task.Delay(500, cancellationToken);

            // 在开发环境中，我们只记录日志，不真正发送SMS
            _logger.LogInformation("Mock SMS sent to {PhoneNumber}: {Message}",
                MaskPhoneNumber(phoneNumber), message.Substring(0, Math.Min(50, message.Length)));

            // 模拟成功率（实际项目中应该基于真实的API响应）
            return true;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("SMS sending cancelled for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mock SMS provider error for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
    }

    /// <summary>
    /// 真实SMS API调用示例（注释掉的实现）
    /// </summary>
    private async Task<bool> SendSmsViaRealProvider(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        try
        {
            var requestData = new
            {
                to = phoneNumber,
                message = message,
                from = "Maple Blog"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

            var response = await _httpClient.PostAsync(SmsApiUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("SMS API response: {Response}", responseContent);
                return true;
            }
            else
            {
                _logger.LogWarning("SMS API returned error status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling SMS API for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "SMS API call timeout for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling SMS API for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
    }

    /// <summary>
    /// 掩码手机号用于日志记录
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 4)
            return "****";

        if (phoneNumber.StartsWith("+"))
        {
            if (phoneNumber.Length < 8)
                return phoneNumber.Substring(0, 3) + "****";
            return phoneNumber.Substring(0, 3) + "****" + phoneNumber.Substring(phoneNumber.Length - 4);
        }

        if (phoneNumber.Length < 7)
            return "****" + phoneNumber.Substring(phoneNumber.Length - 2);

        return phoneNumber.Substring(0, 3) + "****" + phoneNumber.Substring(phoneNumber.Length - 4);
    }

    #endregion
}