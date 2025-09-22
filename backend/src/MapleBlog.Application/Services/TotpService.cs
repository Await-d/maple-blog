using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Application.Services;

/// <summary>
/// TOTP (Time-based One-Time Password) 服务实现
/// 基于 RFC 6238 标准
/// </summary>
public class TotpService : ITotpService
{
    private readonly ILogger<TotpService> _logger;

    // TOTP 配置常量
    private const int DefaultDigits = 6;
    private const int DefaultTimeStep = 30; // 30秒时间步长
    private const string DefaultAlgorithm = "SHA1";
    private const int SecretKeyLength = 20; // 160位密钥

    public TotpService(ILogger<TotpService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 生成TOTP密钥
    /// </summary>
    public string GenerateSecret()
    {
        try
        {
            var secretBytes = new byte[SecretKeyLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secretBytes);
            }

            // 转换为Base32编码
            var secret = Base32Encode(secretBytes);

            _logger.LogDebug("Generated new TOTP secret");
            return secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TOTP secret");
            throw;
        }
    }

    /// <summary>
    /// 生成QR码URI
    /// </summary>
    public string GenerateQrCodeUri(string accountName, string issuer, string secret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accountName))
                throw new ArgumentException("Account name cannot be empty", nameof(accountName));

            if (string.IsNullOrWhiteSpace(issuer))
                throw new ArgumentException("Issuer cannot be empty", nameof(issuer));

            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("Secret cannot be empty", nameof(secret));

            // URL编码参数
            var encodedAccountName = Uri.EscapeDataString(accountName);
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedSecret = Uri.EscapeDataString(secret);

            // 构建otpauth URI
            var uri = $"otpauth://totp/{encodedIssuer}:{encodedAccountName}" +
                      $"?secret={encodedSecret}" +
                      $"&issuer={encodedIssuer}" +
                      $"&algorithm={DefaultAlgorithm}" +
                      $"&digits={DefaultDigits}" +
                      $"&period={DefaultTimeStep}";

            _logger.LogDebug("Generated QR code URI for account: {AccountName}", accountName);
            return uri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code URI for account: {AccountName}", accountName);
            throw;
        }
    }

    /// <summary>
    /// 验证TOTP代码
    /// </summary>
    public bool VerifyCode(string secret, string code, int window = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("TOTP verification failed: Empty secret");
                return false;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("TOTP verification failed: Empty code");
                return false;
            }

            // 清理输入代码
            code = code.Replace(" ", "").Replace("-", "");

            if (code.Length != DefaultDigits)
            {
                _logger.LogWarning("TOTP verification failed: Invalid code length {Length}", code.Length);
                return false;
            }

            if (!int.TryParse(code, out var inputCode))
            {
                _logger.LogWarning("TOTP verification failed: Code is not numeric");
                return false;
            }

            var secretBytes = Base32Decode(secret);
            var currentTimeStep = GetCurrentTimeStep();

            // 检查当前时间窗口和前后窗口
            for (int i = -window; i <= window; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedCode = GenerateCodeForTimeStep(secretBytes, timeStep);

                if (expectedCode == inputCode)
                {
                    _logger.LogDebug("TOTP verification successful for time step offset: {Offset}", i);
                    return true;
                }
            }

            _logger.LogWarning("TOTP verification failed: No matching code found in time window");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP code");
            return false;
        }
    }

    /// <summary>
    /// 生成当前时间的TOTP代码
    /// </summary>
    public string GenerateCode(string secret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new ArgumentException("Secret cannot be empty", nameof(secret));

            var secretBytes = Base32Decode(secret);
            var currentTimeStep = GetCurrentTimeStep();
            var code = GenerateCodeForTimeStep(secretBytes, currentTimeStep);

            return code.ToString($"D{DefaultDigits}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating TOTP code");
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// 获取当前时间步长
    /// </summary>
    private static long GetCurrentTimeStep()
    {
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTimestamp / DefaultTimeStep;
    }

    /// <summary>
    /// 为指定时间步长生成TOTP代码
    /// </summary>
    private static int GenerateCodeForTimeStep(byte[] secret, long timeStep)
    {
        // 将时间步长转换为8字节大端序数组
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timeBytes);
        }

        // 使用HMAC-SHA1计算哈希
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timeBytes);

        // 动态截断
        var offset = hash[hash.Length - 1] & 0x0F;
        var code = ((hash[offset] & 0x7F) << 24) |
                   ((hash[offset + 1] & 0xFF) << 16) |
                   ((hash[offset + 2] & 0xFF) << 8) |
                   (hash[offset + 3] & 0xFF);

        // 取模得到指定位数的代码
        return (int)(code % Math.Pow(10, DefaultDigits));
    }

    /// <summary>
    /// Base32编码
    /// </summary>
    private static string Base32Encode(byte[] data)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();

        for (int i = 0; i < data.Length; i += 5)
        {
            var chunk = new byte[5];
            var chunkLength = Math.Min(5, data.Length - i);
            Array.Copy(data, i, chunk, 0, chunkLength);

            // 将40位数据转换为8个5位值
            var buffer = ((long)chunk[0] << 32) |
                        ((long)chunk[1] << 24) |
                        ((long)chunk[2] << 16) |
                        ((long)chunk[3] << 8) |
                        chunk[4];

            for (int j = 7; j >= 0; j--)
            {
                if (i * 8 / 5 + (7 - j) < data.Length * 8 / 5 + (data.Length * 8 % 5 > 0 ? 1 : 0))
                {
                    var index = (int)((buffer >> (j * 5)) & 0x1F);
                    result.Append(base32Chars[index]);
                }
            }
        }

        // 添加填充
        while (result.Length % 8 != 0)
        {
            result.Append('=');
        }

        return result.ToString();
    }

    /// <summary>
    /// Base32解码
    /// </summary>
    private static byte[] Base32Decode(string base32)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        // 移除填充和空格
        base32 = base32.Replace("=", "").Replace(" ", "").ToUpperInvariant();

        var result = new List<byte>();
        var buffer = 0L;
        var bufferLength = 0;

        foreach (char c in base32)
        {
            var index = base32Chars.IndexOf(c);
            if (index < 0)
                throw new ArgumentException($"Invalid Base32 character: {c}");

            buffer = (buffer << 5) | (uint)index;
            bufferLength += 5;

            if (bufferLength >= 8)
            {
                result.Add((byte)(buffer >> (bufferLength - 8)));
                bufferLength -= 8;
            }
        }

        return result.ToArray();
    }

    #endregion
}