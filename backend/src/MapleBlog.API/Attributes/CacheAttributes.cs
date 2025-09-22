using Microsoft.AspNetCore.Mvc;

namespace MapleBlog.API.Attributes;

/// <summary>
/// Cache attribute for static content with long expiration
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class StaticCacheAttribute : ResponseCacheAttribute
{
    public StaticCacheAttribute()
    {
        Duration = 86400; // 24 hours
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept-Encoding";
    }

    public StaticCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept-Encoding";
    }
}

/// <summary>
/// Cache attribute for API responses with medium expiration
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiCacheAttribute : ResponseCacheAttribute
{
    public ApiCacheAttribute()
    {
        Duration = 300; // 5 minutes
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept,Accept-Language";
    }

    public ApiCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept,Accept-Language";
    }
}

/// <summary>
/// Cache attribute for short-lived content
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ShortCacheAttribute : ResponseCacheAttribute
{
    public ShortCacheAttribute()
    {
        Duration = 60; // 1 minute
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept";
    }

    public ShortCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept";
    }
}

/// <summary>
/// Cache attribute for long-lived content
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LongCacheAttribute : ResponseCacheAttribute
{
    public LongCacheAttribute()
    {
        Duration = 3600; // 1 hour
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept,Accept-Language";
    }

    public LongCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept,Accept-Language";
    }
}

/// <summary>
/// No cache attribute for sensitive or dynamic content
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class NoCacheAttribute : ResponseCacheAttribute
{
    public NoCacheAttribute()
    {
        Duration = 0;
        Location = ResponseCacheLocation.None;
        NoStore = true;
    }
}

/// <summary>
/// ETag-enabled cache attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ETagCacheAttribute : ResponseCacheAttribute
{
    public ETagCacheAttribute()
    {
        Duration = 300; // 5 minutes
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept,Accept-Language";
    }

    public ETagCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept,Accept-Language";
    }
}

/// <summary>
/// Private cache attribute for user-specific content
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PrivateCacheAttribute : ResponseCacheAttribute
{
    public PrivateCacheAttribute()
    {
        Duration = 300; // 5 minutes
        Location = ResponseCacheLocation.Client;
        VaryByHeader = "Authorization";
    }

    public PrivateCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Client;
        VaryByHeader = "Authorization";
    }
}

/// <summary>
/// Search cache attribute for search results
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SearchCacheAttribute : ResponseCacheAttribute
{
    public SearchCacheAttribute()
    {
        Duration = 900; // 15 minutes
        Location = ResponseCacheLocation.Any;
        VaryByQueryKeys = new[] { "q", "page", "size", "sort" };
        VaryByHeader = "Accept,Accept-Language";
    }
}

/// <summary>
/// Archive cache attribute for archive pages
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ArchiveCacheAttribute : ResponseCacheAttribute
{
    public ArchiveCacheAttribute()
    {
        Duration = 1800; // 30 minutes
        Location = ResponseCacheLocation.Any;
        VaryByQueryKeys = new[] { "year", "month", "page", "size" };
        VaryByHeader = "Accept,Accept-Language";
    }
}

/// <summary>
/// Statistics cache attribute for frequently changing stats
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class StatsCacheAttribute : ResponseCacheAttribute
{
    public StatsCacheAttribute()
    {
        Duration = 300; // 5 minutes
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept";
    }

    public StatsCacheAttribute(int duration)
    {
        Duration = duration;
        Location = ResponseCacheLocation.Any;
        VaryByHeader = "Accept";
    }
}