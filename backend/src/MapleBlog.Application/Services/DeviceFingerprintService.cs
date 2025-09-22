using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Application.Services;

/// <summary>
/// 设备指纹服务实现
/// </summary>
public class DeviceFingerprintService : IDeviceFingerprintService
{
    private readonly ILogger<DeviceFingerprintService> _logger;
    private readonly HttpClient _httpClient;

    // 用户代理解析的正则表达式
    private static readonly Regex BrowserRegex = new(@"(Chrome|Firefox|Safari|Edge|Opera)\/?([\d\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex OSRegex = new(@"(Windows NT [\d\.]+|Mac OS X [\d_\.]+|Linux|Android [\d\.]+|iOS [\d_\.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MobileRegex = new(@"Mobile|Android|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DeviceFingerprintService(ILogger<DeviceFingerprintService> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// 生成设备指纹
    /// </summary>
    public string GenerateFingerprint(string userAgent, string ipAddress, Dictionary<string, string>? additionalFactors = null)
    {
        try
        {
            var factors = new Dictionary<string, string>
            {
                ["userAgent"] = NormalizeUserAgent(userAgent),
                ["ipAddress"] = NormalizeIpAddress(ipAddress)
            };

            // 添加额外因素
            if (additionalFactors != null)
            {
                foreach (var factor in additionalFactors)
                {
                    factors[factor.Key] = factor.Value ?? string.Empty;
                }
            }

            // 按键排序确保一致性
            var sortedFactors = factors.OrderBy(kvp => kvp.Key).ToList();

            // 创建指纹字符串
            var fingerprintData = string.Join("|", sortedFactors.Select(kvp => $"{kvp.Key}:{kvp.Value}"));

            // 使用SHA256生成哈希
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprintData));
            var fingerprint = Convert.ToHexString(hashBytes).ToLowerInvariant();

            _logger.LogDebug("Generated device fingerprint with {FactorCount} factors", factors.Count);
            return fingerprint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating device fingerprint");
            throw;
        }
    }

    /// <summary>
    /// 解析用户代理信息
    /// </summary>
    public UserAgentInfo ParseUserAgent(string userAgent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return new UserAgentInfo
                {
                    BrowserName = "Unknown",
                    OperatingSystem = "Unknown",
                    DeviceType = "Unknown",
                    IsMobile = false
                };
            }

            var browserMatch = BrowserRegex.Match(userAgent);
            var osMatch = OSRegex.Match(userAgent);
            var isMobile = MobileRegex.IsMatch(userAgent);

            var browser = browserMatch.Success
                ? $"{browserMatch.Groups[1].Value} {browserMatch.Groups[2].Value}"
                : "Unknown";

            var os = osMatch.Success ? NormalizeOperatingSystem(osMatch.Groups[1].Value) : "Unknown";

            var deviceType = DetermineDeviceType(userAgent, isMobile);

            var result = new UserAgentInfo
            {
                BrowserName = browserMatch.Success ? browserMatch.Groups[1].Value : "Unknown",
                BrowserVersion = browserMatch.Success ? browserMatch.Groups[2].Value : "Unknown",
                OperatingSystem = os,
                DeviceType = deviceType,
                IsMobile = isMobile
            };

            _logger.LogDebug("Parsed user agent: {Browser} on {OS}, Device: {DeviceType}, Mobile: {IsMobile}",
                result.Browser, result.OperatingSystem, result.DeviceType, result.IsMobile);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing user agent: {UserAgent}", userAgent);
            return new UserAgentInfo
            {
                BrowserName = "Unknown",
                OperatingSystem = "Unknown",
                DeviceType = "Unknown",
                IsMobile = false
            };
        }
    }

    /// <summary>
    /// 获取地理位置信息
    /// </summary>
    public async Task<LocationInfo?> GetLocationInfoAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || IsPrivateIpAddress(ipAddress))
            {
                _logger.LogDebug("Skipping location lookup for private/invalid IP: {IpAddress}", ipAddress);
                return new LocationInfo
                {
                    Country = "Unknown",
                    Region = "Unknown",
                    City = "Unknown",
                    Timezone = "Unknown"
                };
            }

            // 使用免费的IP地理位置API（实际项目中可能需要付费服务）
            var response = await _httpClient.GetStringAsync($"http://ip-api.com/json/{ipAddress}?fields=status,country,regionName,city,timezone,lat,lon", cancellationToken);

            var locationData = JsonSerializer.Deserialize<JsonElement>(response);

            if (locationData.GetProperty("status").GetString() == "success")
            {
                var location = new LocationInfo
                {
                    Country = locationData.TryGetProperty("country", out var country) ? country.GetString() ?? "Unknown" : "Unknown",
                    Region = locationData.TryGetProperty("regionName", out var region) ? region.GetString() ?? "Unknown" : "Unknown",
                    City = locationData.TryGetProperty("city", out var city) ? city.GetString() ?? "Unknown" : "Unknown",
                    Timezone = locationData.TryGetProperty("timezone", out var timezone) ? timezone.GetString() ?? "Unknown" : "Unknown"
                };

                if (locationData.TryGetProperty("lat", out var lat) && lat.ValueKind == JsonValueKind.Number)
                    location.Latitude = lat.GetDouble();

                if (locationData.TryGetProperty("lon", out var lon) && lon.ValueKind == JsonValueKind.Number)
                    location.Longitude = lon.GetDouble();

                _logger.LogDebug("Retrieved location for IP {IpAddress}: {Country}, {Region}, {City}",
                    ipAddress, location.Country, location.Region, location.City);

                return location;
            }

            _logger.LogWarning("Failed to get location for IP {IpAddress}: API returned unsuccessful status", ipAddress);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error getting location for IP {IpAddress}", ipAddress);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Timeout getting location for IP {IpAddress}", ipAddress);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for IP {IpAddress}", ipAddress);
            return null;
        }
    }

    #region Private Methods

    /// <summary>
    /// 标准化用户代理字符串
    /// </summary>
    private static string NormalizeUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "unknown";

        // 移除版本号中的具体构建信息，保留主要版本
        var normalized = userAgent;

        // 简化Chrome版本信息
        normalized = Regex.Replace(normalized, @"Chrome/(\d+\.\d+)\.\d+\.\d+", "Chrome/$1", RegexOptions.IgnoreCase);

        // 简化Firefox版本信息
        normalized = Regex.Replace(normalized, @"Firefox/(\d+\.\d+)", "Firefox/$1", RegexOptions.IgnoreCase);

        // 简化Safari版本信息
        normalized = Regex.Replace(normalized, @"Safari/(\d+\.\d+)", "Safari/$1", RegexOptions.IgnoreCase);

        return normalized.ToLowerInvariant();
    }

    /// <summary>
    /// 标准化IP地址
    /// </summary>
    private static string NormalizeIpAddress(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return "0.0.0.0";

        // 对于IPv4，保留前3个八位组
        if (System.Net.IPAddress.TryParse(ipAddress, out var ip))
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.0";
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // 对于IPv6，保留前64位
                var bytes = ip.GetAddressBytes();
                for (int i = 8; i < 16; i++)
                {
                    bytes[i] = 0;
                }
                return new System.Net.IPAddress(bytes).ToString();
            }
        }

        return ipAddress;
    }

    /// <summary>
    /// 标准化操作系统名称
    /// </summary>
    private static string NormalizeOperatingSystem(string os)
    {
        if (string.IsNullOrWhiteSpace(os))
            return "Unknown";

        // 标准化Windows版本
        if (os.Contains("Windows NT"))
        {
            return os.Contains("10.0") ? "Windows 10" :
                   os.Contains("6.3") ? "Windows 8.1" :
                   os.Contains("6.2") ? "Windows 8" :
                   os.Contains("6.1") ? "Windows 7" :
                   "Windows";
        }

        // 标准化macOS版本
        if (os.Contains("Mac OS X"))
        {
            return "macOS";
        }

        // 标准化Android版本
        if (os.Contains("Android"))
        {
            var versionMatch = Regex.Match(os, @"Android ([\d\.]+)");
            if (versionMatch.Success)
            {
                var version = versionMatch.Groups[1].Value;
                var majorVersion = version.Split('.')[0];
                return $"Android {majorVersion}";
            }
            return "Android";
        }

        // 标准化iOS版本
        if (os.Contains("iOS"))
        {
            var versionMatch = Regex.Match(os, @"iOS ([\d_\.]+)");
            if (versionMatch.Success)
            {
                var version = versionMatch.Groups[1].Value.Replace("_", ".");
                var majorVersion = version.Split('.')[0];
                return $"iOS {majorVersion}";
            }
            return "iOS";
        }

        if (os.Contains("Linux"))
            return "Linux";

        return os;
    }

    /// <summary>
    /// 确定设备类型
    /// </summary>
    private static string DetermineDeviceType(string userAgent, bool isMobile)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        var lowerAgent = userAgent.ToLowerInvariant();

        if (lowerAgent.Contains("ipad"))
            return "Tablet";

        if (lowerAgent.Contains("android") && lowerAgent.Contains("mobile"))
            return "Mobile";

        if (lowerAgent.Contains("android") && !lowerAgent.Contains("mobile"))
            return "Tablet";

        if (isMobile)
            return "Mobile";

        if (lowerAgent.Contains("smart tv") || lowerAgent.Contains("television"))
            return "Smart TV";

        if (lowerAgent.Contains("bot") || lowerAgent.Contains("crawler") || lowerAgent.Contains("spider"))
            return "Bot";

        return "Desktop";
    }

    /// <summary>
    /// 检查是否为私有IP地址
    /// </summary>
    private static bool IsPrivateIpAddress(string ipAddress)
    {
        if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
            return true;

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            // 私有IPv4地址范围
            return (bytes[0] == 10) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 127); // 回环地址
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // 私有IPv6地址范围
            return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || System.Net.IPAddress.IsLoopback(ip);
        }

        return false;
    }

    #endregion
}