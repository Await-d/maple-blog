using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// Comprehensive response cache configuration system
/// </summary>
public class ResponseCacheConfiguration
{
    /// <summary>
    /// Global cache settings
    /// </summary>
    public GlobalCacheSettings Global { get; set; } = new();

    /// <summary>
    /// Path-specific cache rules
    /// </summary>
    public List<CacheRule> Rules { get; set; } = new();

    /// <summary>
    /// Static content cache settings
    /// </summary>
    public StaticContentCacheSettings StaticContent { get; set; } = new();

    /// <summary>
    /// API endpoint cache settings
    /// </summary>
    public ApiCacheSettings Api { get; set; } = new();

    /// <summary>
    /// Cache invalidation settings
    /// </summary>
    public CacheInvalidationSettings Invalidation { get; set; } = new();
}

/// <summary>
/// Global cache settings
/// </summary>
public class GlobalCacheSettings
{
    /// <summary>
    /// Enable/disable response caching globally
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache duration for responses
    /// </summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum cache size in bytes
    /// </summary>
    public long MaxCacheSize { get; set; } = 100_000_000; // 100MB

    /// <summary>
    /// Enable ETag generation and validation
    /// </summary>
    public bool EnableETag { get; set; } = true;

    /// <summary>
    /// Enable Last-Modified header
    /// </summary>
    public bool EnableLastModified { get; set; } = true;

    /// <summary>
    /// Enable Vary header based on request headers
    /// </summary>
    public bool EnableVaryHeader { get; set; } = true;

    /// <summary>
    /// Default Vary headers to include
    /// </summary>
    public string[] DefaultVaryHeaders { get; set; } = { "Accept", "Accept-Encoding", "Accept-Language" };

    /// <summary>
    /// Enable cache compression
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Cache provider type
    /// </summary>
    public CacheProviderType Provider { get; set; } = CacheProviderType.Memory;
}

/// <summary>
/// Cache rule for specific paths or patterns
/// </summary>
public class CacheRule
{
    /// <summary>
    /// Rule name for identification
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Path pattern (supports wildcards and regex)
    /// </summary>
    public string PathPattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether the pattern is a regex
    /// </summary>
    public bool IsRegex { get; set; } = false;

    /// <summary>
    /// HTTP methods this rule applies to
    /// </summary>
    public string[] Methods { get; set; } = { "GET" };

    /// <summary>
    /// Cache duration for this rule
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache-Control header value
    /// </summary>
    public string CacheControl { get; set; } = "public, max-age=300";

    /// <summary>
    /// Whether to enable caching for this rule
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Priority of this rule (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Vary headers specific to this rule
    /// </summary>
    public string[]? VaryHeaders { get; set; }

    /// <summary>
    /// Whether to generate ETag for this rule
    /// </summary>
    public bool GenerateETag { get; set; } = true;

    /// <summary>
    /// Whether to include Last-Modified header
    /// </summary>
    public bool IncludeLastModified { get; set; } = true;

    /// <summary>
    /// Cache key generation strategy
    /// </summary>
    public CacheKeyStrategy KeyStrategy { get; set; } = CacheKeyStrategy.PathAndQuery;

    /// <summary>
    /// Additional headers to include in cache key
    /// </summary>
    public string[]? KeyHeaders { get; set; }

    /// <summary>
    /// Condition for applying this rule (lambda expression as string)
    /// </summary>
    public string? Condition { get; set; }

    private Regex? _compiledRegex;
    public Regex CompiledRegex
    {
        get
        {
            if (IsRegex && _compiledRegex == null)
            {
                _compiledRegex = new Regex(PathPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            return _compiledRegex!;
        }
    }
}

/// <summary>
/// Static content cache settings
/// </summary>
public class StaticContentCacheSettings
{
    /// <summary>
    /// Enable static content caching
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache duration for images
    /// </summary>
    public TimeSpan ImageCacheDuration { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Cache duration for CSS/JS files
    /// </summary>
    public TimeSpan StyleScriptCacheDuration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Cache duration for fonts
    /// </summary>
    public TimeSpan FontCacheDuration { get; set; } = TimeSpan.FromDays(365);

    /// <summary>
    /// Cache duration for other static files
    /// </summary>
    public TimeSpan DefaultStaticCacheDuration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// File extensions considered as images
    /// </summary>
    public string[] ImageExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".ico" };

    /// <summary>
    /// File extensions for CSS/JS
    /// </summary>
    public string[] StyleScriptExtensions { get; set; } = { ".css", ".js", ".ts", ".jsx", ".tsx" };

    /// <summary>
    /// File extensions for fonts
    /// </summary>
    public string[] FontExtensions { get; set; } = { ".woff", ".woff2", ".ttf", ".eot", ".otf" };
}

/// <summary>
/// API endpoint cache settings
/// </summary>
public class ApiCacheSettings
{
    /// <summary>
    /// Default cache duration for API responses
    /// </summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache duration for blog posts
    /// </summary>
    public TimeSpan PostsCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Cache duration for categories
    /// </summary>
    public TimeSpan CategoriesCacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Cache duration for tags
    /// </summary>
    public TimeSpan TagsCacheDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Cache duration for user profiles
    /// </summary>
    public TimeSpan UserProfilesCacheDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Cache duration for statistics
    /// </summary>
    public TimeSpan StatsCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache duration for search results
    /// </summary>
    public TimeSpan SearchCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Paths that should never be cached
    /// </summary>
    public string[] NoCachePaths { get; set; } =
    {
        "/api/auth/*",
        "/api/admin/*",
        "/api/comments/*/like",
        "/api/posts/*/views"
    };

    /// <summary>
    /// Enable cache for authenticated requests
    /// </summary>
    public bool CacheAuthenticatedRequests { get; set; } = false;

    /// <summary>
    /// Include Authorization header in cache key for authenticated requests
    /// </summary>
    public bool IncludeAuthInCacheKey { get; set; } = true;
}

/// <summary>
/// Cache invalidation settings
/// </summary>
public class CacheInvalidationSettings
{
    /// <summary>
    /// Enable automatic cache invalidation
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache invalidation patterns when posts are modified
    /// </summary>
    public string[] PostInvalidationPatterns { get; set; } =
    {
        "posts:*",
        "post:*",
        "categories:*",
        "tags:*",
        "stats:*",
        "homepage:*"
    };

    /// <summary>
    /// Cache invalidation patterns when categories are modified
    /// </summary>
    public string[] CategoryInvalidationPatterns { get; set; } =
    {
        "categories:*",
        "category:*",
        "posts:*"
    };

    /// <summary>
    /// Cache invalidation patterns when tags are modified
    /// </summary>
    public string[] TagInvalidationPatterns { get; set; } =
    {
        "tags:*",
        "tag:*",
        "posts:*"
    };

    /// <summary>
    /// Enable cache warming after invalidation
    /// </summary>
    public bool EnableCacheWarming { get; set; } = true;

    /// <summary>
    /// Delay before starting cache warming
    /// </summary>
    public TimeSpan CacheWarmingDelay { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Cache key generation strategy
/// </summary>
public enum CacheKeyStrategy
{
    /// <summary>
    /// Use only the path
    /// </summary>
    PathOnly,

    /// <summary>
    /// Use path and query parameters
    /// </summary>
    PathAndQuery,

    /// <summary>
    /// Use path, query, and specified headers
    /// </summary>
    PathQueryHeaders,

    /// <summary>
    /// Custom key generation
    /// </summary>
    Custom
}

/// <summary>
/// Response cache configuration service
/// </summary>
public interface IResponseCacheConfigurationService
{
    /// <summary>
    /// Get cache rule for a specific path and method
    /// </summary>
    CacheRule? GetCacheRule(string path, string method);

    /// <summary>
    /// Get cache duration for a specific path
    /// </summary>
    TimeSpan GetCacheDuration(string path, string method);

    /// <summary>
    /// Check if caching is enabled for a specific path
    /// </summary>
    bool IsCachingEnabled(string path, string method);

    /// <summary>
    /// Generate cache key for a request
    /// </summary>
    string GenerateCacheKey(string path, string query, IDictionary<string, string> headers, CacheRule rule);

    /// <summary>
    /// Get cache-control header value
    /// </summary>
    string GetCacheControlHeader(CacheRule rule);

    /// <summary>
    /// Get vary headers for a rule
    /// </summary>
    string[] GetVaryHeaders(CacheRule rule);

    /// <summary>
    /// Check if ETag should be generated
    /// </summary>
    bool ShouldGenerateETag(CacheRule rule);

    /// <summary>
    /// Check if Last-Modified should be included
    /// </summary>
    bool ShouldIncludeLastModified(CacheRule rule);

    /// <summary>
    /// Get invalidation patterns for a content type
    /// </summary>
    string[] GetInvalidationPatterns(string contentType);
}

/// <summary>
/// Implementation of response cache configuration service
/// </summary>
public class ResponseCacheConfigurationService : IResponseCacheConfigurationService
{
    private readonly ResponseCacheConfiguration _configuration;
    private readonly List<CacheRule> _orderedRules;

    public ResponseCacheConfigurationService(IOptions<ResponseCacheConfiguration> configuration)
    {
        _configuration = configuration.Value;
        _orderedRules = _configuration.Rules
            .Where(r => r.Enabled)
            .OrderByDescending(r => r.Priority)
            .ToList();
    }

    public CacheRule? GetCacheRule(string path, string method)
    {
        if (!_configuration.Global.Enabled)
            return null;

        foreach (var rule in _orderedRules)
        {
            if (!rule.Methods.Contains(method, StringComparer.OrdinalIgnoreCase))
                continue;

            if (IsPathMatch(path, rule))
                return rule;
        }

        return null;
    }

    public TimeSpan GetCacheDuration(string path, string method)
    {
        var rule = GetCacheRule(path, method);
        return rule?.Duration ?? _configuration.Global.DefaultDuration;
    }

    public bool IsCachingEnabled(string path, string method)
    {
        if (!_configuration.Global.Enabled)
            return false;

        // Check if path is in no-cache list
        foreach (var noCachePath in _configuration.Api.NoCachePaths)
        {
            if (IsWildcardMatch(path, noCachePath))
                return false;
        }

        var rule = GetCacheRule(path, method);
        return rule?.Enabled ?? false;
    }

    public string GenerateCacheKey(string path, string query, IDictionary<string, string> headers, CacheRule rule)
    {
        var keyBuilder = new List<string> { path };

        switch (rule.KeyStrategy)
        {
            case CacheKeyStrategy.PathOnly:
                break;

            case CacheKeyStrategy.PathAndQuery:
                if (!string.IsNullOrEmpty(query))
                    keyBuilder.Add(query);
                break;

            case CacheKeyStrategy.PathQueryHeaders:
                if (!string.IsNullOrEmpty(query))
                    keyBuilder.Add(query);

                if (rule.KeyHeaders != null)
                {
                    foreach (var header in rule.KeyHeaders)
                    {
                        if (headers.TryGetValue(header, out var value))
                            keyBuilder.Add($"{header}:{value}");
                    }
                }
                break;

            case CacheKeyStrategy.Custom:
                // Custom logic can be implemented here
                break;
        }

        return string.Join("|", keyBuilder);
    }

    public string GetCacheControlHeader(CacheRule rule)
    {
        return rule.CacheControl;
    }

    public string[] GetVaryHeaders(CacheRule rule)
    {
        return rule.VaryHeaders ?? _configuration.Global.DefaultVaryHeaders;
    }

    public bool ShouldGenerateETag(CacheRule rule)
    {
        return rule.GenerateETag && _configuration.Global.EnableETag;
    }

    public bool ShouldIncludeLastModified(CacheRule rule)
    {
        return rule.IncludeLastModified && _configuration.Global.EnableLastModified;
    }

    public string[] GetInvalidationPatterns(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "post" => _configuration.Invalidation.PostInvalidationPatterns,
            "category" => _configuration.Invalidation.CategoryInvalidationPatterns,
            "tag" => _configuration.Invalidation.TagInvalidationPatterns,
            _ => Array.Empty<string>()
        };
    }

    private bool IsPathMatch(string path, CacheRule rule)
    {
        if (rule.IsRegex)
        {
            return rule.CompiledRegex.IsMatch(path);
        }

        return IsWildcardMatch(path, rule.PathPattern);
    }

    private static bool IsWildcardMatch(string path, string pattern)
    {
        if (pattern.Contains('*'))
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }

        return string.Equals(path, pattern, StringComparison.OrdinalIgnoreCase);
    }
}