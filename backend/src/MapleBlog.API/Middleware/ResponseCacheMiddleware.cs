using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Infrastructure.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MapleBlog.API.Middleware;

/// <summary>
/// Advanced response caching middleware with intelligent cache control
/// </summary>
public class ResponseCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseCacheMiddleware> _logger;
    private readonly IResponseCacheConfigurationService _configurationService;
    private readonly ICacheManager _cacheManager;
    private readonly ResponseCacheConfiguration _configuration;

    public ResponseCacheMiddleware(
        RequestDelegate next,
        ILogger<ResponseCacheMiddleware> logger,
        IResponseCacheConfigurationService configurationService,
        ICacheManager cacheManager,
        IOptions<ResponseCacheConfiguration> configuration)
    {
        _next = next;
        _logger = logger;
        _configurationService = configurationService;
        _cacheManager = cacheManager;
        _configuration = configuration.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip caching for non-GET requests or if globally disabled
        if (context.Request.Method != HttpMethods.Get || !_configuration.Global.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // Check if caching is enabled for this path
        if (!_configurationService.IsCachingEnabled(path, method))
        {
            await _next(context);
            return;
        }

        var cacheRule = _configurationService.GetCacheRule(path, method);
        if (cacheRule == null)
        {
            await _next(context);
            return;
        }

        try
        {
            await ProcessCachedRequestAsync(context, cacheRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cached request for path: {Path}", path);
            // Continue without caching on error
            await _next(context);
        }
    }

    private async Task ProcessCachedRequestAsync(HttpContext context, CacheRule cacheRule)
    {
        var path = context.Request.Path.Value ?? "";
        var headers = ExtractHeaders(context.Request);
        var cacheKey = _configurationService.GenerateCacheKey(
            path,
            context.Request.QueryString.Value ?? "",
            headers,
            cacheRule);

        // Check for conditional requests (ETag, If-Modified-Since)
        if (await HandleConditionalRequestAsync(context, cacheRule, cacheKey))
        {
            return; // 304 Not Modified response sent
        }

        // Try to get from cache
        var cachedResponse = await _cacheManager.Cache.GetAsync<CachedResponse>(cacheKey);
        if (cachedResponse != null && !IsExpired(cachedResponse, cacheRule))
        {
            await WriteCachedResponseAsync(context, cachedResponse, cacheRule);
            _logger.LogDebug("Served cached response for path: {Path}, Key: {CacheKey}, Age: {Age}s",
                path, cacheKey, (DateTime.UtcNow - cachedResponse.CreatedAt).TotalSeconds);
            return;
        }

        // Capture response for caching
        await CaptureAndCacheResponseAsync(context, cacheRule, cacheKey);
    }

    private async Task<bool> HandleConditionalRequestAsync(HttpContext context, CacheRule cacheRule, string cacheKey)
    {
        var request = context.Request;
        var response = context.Response;

        // Handle If-None-Match (ETag)
        if (_configurationService.ShouldGenerateETag(cacheRule))
        {
            var ifNoneMatch = request.Headers.IfNoneMatch.FirstOrDefault();
            if (!string.IsNullOrEmpty(ifNoneMatch))
            {
                var cachedResponse = await _cacheManager.Cache.GetAsync<CachedResponse>(cacheKey);
                if (cachedResponse != null && cachedResponse.ETag == ifNoneMatch)
                {
                    SetCacheHeaders(response, cacheRule, cachedResponse);
                    response.StatusCode = 304; // Not Modified
                    return true;
                }
            }
        }

        // Handle If-Modified-Since
        if (_configurationService.ShouldIncludeLastModified(cacheRule))
        {
            var ifModifiedSince = request.Headers.IfModifiedSince.FirstOrDefault();
            if (!string.IsNullOrEmpty(ifModifiedSince) && DateTime.TryParse(ifModifiedSince, out var modifiedDate))
            {
                var cachedResponse = await _cacheManager.Cache.GetAsync<CachedResponse>(cacheKey);
                if (cachedResponse != null && cachedResponse.LastModified <= modifiedDate)
                {
                    SetCacheHeaders(response, cacheRule, cachedResponse);
                    response.StatusCode = 304; // Not Modified
                    return true;
                }
            }
        }

        return false;
    }

    private async Task WriteCachedResponseAsync(HttpContext context, CachedResponse cachedResponse, CacheRule cacheRule)
    {
        var response = context.Response;

        // Set status code and headers
        response.StatusCode = cachedResponse.StatusCode;
        response.ContentType = cachedResponse.ContentType;

        // Set cache headers
        SetCacheHeaders(response, cacheRule, cachedResponse);

        // Set custom headers
        foreach (var header in cachedResponse.Headers)
        {
            response.Headers[header.Key] = header.Value;
        }

        // Write body
        if (cachedResponse.Body != null)
        {
            await response.Body.WriteAsync(cachedResponse.Body);
        }
    }

    private async Task CaptureAndCacheResponseAsync(HttpContext context, CacheRule cacheRule, string cacheKey)
    {
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            // Only cache successful responses
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = responseBodyStream.ToArray();

                var cachedResponse = new CachedResponse
                {
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType ?? "application/octet-stream",
                    Body = responseBody,
                    Headers = ExtractResponseHeaders(context.Response),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                // Generate ETag if enabled
                if (_configurationService.ShouldGenerateETag(cacheRule))
                {
                    cachedResponse.ETag = GenerateETag(responseBody, context.Request);
                    context.Response.Headers.ETag = cachedResponse.ETag;
                }

                // Set Last-Modified if enabled
                if (_configurationService.ShouldIncludeLastModified(cacheRule))
                {
                    var lastModified = cachedResponse.LastModified.Value.ToString("R");
                    cachedResponse.Headers["Last-Modified"] = lastModified;
                    context.Response.Headers.LastModified = lastModified;
                }

                // Set cache headers
                SetCacheHeaders(context.Response, cacheRule, cachedResponse);

                // Cache the response
                await _cacheManager.Cache.SetAsync(cacheKey, cachedResponse, cacheRule.Duration);

                _logger.LogDebug("Cached response for path: {Path}, Key: {CacheKey}, Size: {Size} bytes",
                    context.Request.Path, cacheKey, responseBody.Length);
            }

            // Copy cached response back to original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private void SetCacheHeaders(HttpResponse response, CacheRule cacheRule, CachedResponse? cachedResponse = null)
    {
        // Set Cache-Control header
        var cacheControl = _configurationService.GetCacheControlHeader(cacheRule);
        response.Headers.CacheControl = cacheControl;

        // Set Vary headers
        var varyHeaders = _configurationService.GetVaryHeaders(cacheRule);
        if (varyHeaders.Any())
        {
            response.Headers.Vary = string.Join(", ", varyHeaders);
        }

        // Set Age header for cached responses
        if (cachedResponse != null)
        {
            var age = (int)(DateTime.UtcNow - cachedResponse.CreatedAt).TotalSeconds;
            response.Headers.Age = age.ToString();
        }

        // Set Expires header if not already set
        if (!response.Headers.ContainsKey("Expires"))
        {
            var expires = cachedResponse?.CreatedAt.Add(cacheRule.Duration) ?? DateTime.UtcNow.Add(cacheRule.Duration);
            response.Headers.Expires = expires.ToString("R");
        }

        // Set ETag if available
        if (!string.IsNullOrEmpty(cachedResponse?.ETag))
        {
            response.Headers.ETag = cachedResponse.ETag;
        }

        // Set Last-Modified if available
        if (cachedResponse?.LastModified != null)
        {
            response.Headers.LastModified = cachedResponse.LastModified.Value.ToString("R");
        }

        // Add cache hit indicator for debugging (development only)
        if (_configuration.Global.Enabled && cachedResponse != null)
        {
            response.Headers["X-Cache"] = "HIT";
            response.Headers["X-Cache-Key"] = cacheControl.Contains("public") ? "public" : "private";
        }
        else
        {
            response.Headers["X-Cache"] = "MISS";
        }
    }

    private Dictionary<string, string> ExtractHeaders(HttpRequest request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value.ToArray());
        }

        return headers;
    }

    private Dictionary<string, string> ExtractResponseHeaders(HttpResponse response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            if (!IsResponseCacheHeader(header.Key))
            {
                headers[header.Key] = string.Join(", ", header.Value.ToArray());
            }
        }

        return headers;
    }

    private static bool IsResponseCacheHeader(string headerName)
    {
        var cacheHeaders = new[]
        {
            "Cache-Control", "Expires", "ETag", "Last-Modified", "Vary",
            "Age", "Pragma", "Date", "Content-Length"
        };

        return cacheHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }

    private string GenerateETag(byte[] content, HttpRequest request)
    {
        var hashInput = new StringBuilder();

        // Content hash (primary)
        using var contentHash = SHA256.Create();
        var contentHashBytes = contentHash.ComputeHash(content);
        hashInput.Append(Convert.ToHexString(contentHashBytes)[..16]);

        // Path and query (for uniqueness)
        hashInput.Append(request.Path);
        hashInput.Append(request.QueryString);

        // Include relevant headers in ETag generation for better cache invalidation
        var relevantHeaders = new[] { "Accept", "Accept-Encoding", "Accept-Language", "User-Agent" };
        foreach (var headerName in relevantHeaders)
        {
            if (request.Headers.TryGetValue(headerName, out var headerValue))
            {
                // Only include first value and normalize
                var value = headerValue.FirstOrDefault()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(value))
                {
                    hashInput.Append("|");
                    hashInput.Append(headerName);
                    hashInput.Append(":");
                    hashInput.Append(value);
                }
            }
        }

        // Add timestamp precision for cache freshness (optional)
        if (_configuration.Global.EnableLastModified)
        {
            var precision = DateTime.UtcNow.ToString("yyyyMMddHH"); // Hour precision
            hashInput.Append("|ts:");
            hashInput.Append(precision);
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput.ToString()));
        var hash = Convert.ToHexString(hashBytes)[..20]; // Use first 20 characters for better uniqueness

        return $"\"{hash}\"";
    }

    private static bool IsExpired(CachedResponse cachedResponse, CacheRule cacheRule)
    {
        var expirationTime = cachedResponse.CreatedAt.Add(cacheRule.Duration);
        var isExpired = expirationTime < DateTime.UtcNow;

        return isExpired;
    }
}

/// <summary>
/// Cached response model
/// </summary>
public class CachedResponse
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public byte[]? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? ETag { get; set; }
    public DateTime? LastModified { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Legacy ResponseCacheOptions for backward compatibility
/// </summary>
public class ResponseCacheOptions
{
    public bool EnableCaching { get; set; } = true;
    public Dictionary<string, string> PathConfigurations { get; set; } = new();
}

/// <summary>
/// Legacy CacheConfiguration for migration
/// </summary>
public class CacheConfiguration
{
    public TimeSpan? MaxAge { get; set; }
    public string CacheControl { get; set; } = "";
    public string[]? VaryByHeaders { get; set; }
}