using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MapleBlog.Infrastructure.Caching;

namespace MapleBlog.Infrastructure.Extensions;

/// <summary>
/// Response caching service configuration extensions
/// </summary>
public static class ResponseCacheExtensions
{
    /// <summary>
    /// Add response caching services with intelligent cache rules
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddResponseCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure response cache options
        services.Configure<ResponseCacheConfiguration>(configuration.GetSection("ResponseCache"));

        // Add default configuration if not present in config
        services.PostConfigure<ResponseCacheConfiguration>(options =>
        {
            if (options.Rules.Count == 0)
            {
                ConfigureDefaultCacheRules(options);
            }
        });

        // Register core services
        services.AddScoped<IResponseCacheConfigurationService, ResponseCacheConfigurationService>();
        services.AddScoped<ICacheManager, CacheManager>();

        // Add cache maintenance service
        services.AddHostedService<CacheMaintenanceService>();

        // Add built-in response caching
        services.AddResponseCaching();

        return services;
    }

    /// <summary>
    /// Add enhanced cache services with intelligent provider selection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddEnhancedCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure cache options
        services.Configure<CacheOptions>(configuration.GetSection("Cache"));

        var cacheProvider = configuration.GetValue<string>("Cache:ProviderType", "Memory");
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(redisConnectionString))
        {
            // Use Redis as enhanced cache service
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<IEnhancedCacheService, RedisCacheService>();
        }
        else
        {
            // Use enhanced memory cache
            services.AddScoped<ICacheService, EnhancedMemoryCacheService>();
            services.AddScoped<IEnhancedCacheService, EnhancedMemoryCacheService>();
        }

        return services;
    }

    private static void ConfigureDefaultCacheRules(ResponseCacheConfiguration options)
    {
        options.Global = new GlobalCacheSettings
        {
            Enabled = true,
            DefaultDuration = TimeSpan.FromMinutes(5),
            MaxCacheSize = 100_000_000, // 100MB
            EnableETag = true,
            EnableLastModified = true,
            EnableVaryHeader = true,
            DefaultVaryHeaders = new[] { "Accept", "Accept-Encoding", "Accept-Language" },
            EnableCompression = true,
            Provider = CacheProviderType.Memory
        };

        options.StaticContent = new StaticContentCacheSettings
        {
            Enabled = true,
            ImageCacheDuration = TimeSpan.FromDays(30),
            StyleScriptCacheDuration = TimeSpan.FromDays(7),
            FontCacheDuration = TimeSpan.FromDays(365),
            DefaultStaticCacheDuration = TimeSpan.FromDays(1),
            ImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".ico" },
            StyleScriptExtensions = new[] { ".css", ".js", ".ts", ".jsx", ".tsx" },
            FontExtensions = new[] { ".woff", ".woff2", ".ttf", ".eot", ".otf" }
        };

        options.Api = new ApiCacheSettings
        {
            DefaultDuration = TimeSpan.FromMinutes(5),
            PostsCacheDuration = TimeSpan.FromMinutes(15),
            CategoriesCacheDuration = TimeSpan.FromHours(1),
            TagsCacheDuration = TimeSpan.FromMinutes(30),
            UserProfilesCacheDuration = TimeSpan.FromMinutes(10),
            StatsCacheDuration = TimeSpan.FromMinutes(5),
            SearchCacheDuration = TimeSpan.FromMinutes(15),
            NoCachePaths = new[]
            {
                "/api/auth/*",
                "/api/admin/*",
                "/api/comments/*/like",
                "/api/posts/*/views",
                "/api/user/profile",
                "/api/notification*"
            },
            CacheAuthenticatedRequests = false,
            IncludeAuthInCacheKey = true
        };

        options.Invalidation = new CacheInvalidationSettings
        {
            Enabled = true,
            PostInvalidationPatterns = new[]
            {
                "posts:*",
                "post:*",
                "categories:*",
                "tags:*",
                "stats:*",
                "homepage:*"
            },
            CategoryInvalidationPatterns = new[]
            {
                "categories:*",
                "category:*",
                "posts:*"
            },
            TagInvalidationPatterns = new[]
            {
                "tags:*",
                "tag:*",
                "posts:*"
            },
            EnableCacheWarming = true,
            CacheWarmingDelay = TimeSpan.FromSeconds(10)
        };

        // Add intelligent cache rules
        options.Rules = CreateDefaultCacheRules();
    }

    private static List<CacheRule> CreateDefaultCacheRules()
    {
        return new List<CacheRule>
        {
            // Static content rules (highest priority)
            new CacheRule
            {
                Name = "Static Images",
                PathPattern = @".*\.(jpg|jpeg|png|gif|webp|svg|ico)$",
                IsRegex = true,
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromDays(30),
                CacheControl = "public, max-age=2592000, immutable",
                Priority = 1000,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathOnly
            },

            new CacheRule
            {
                Name = "Static Fonts",
                PathPattern = @".*\.(woff|woff2|ttf|eot|otf)$",
                IsRegex = true,
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromDays(365),
                CacheControl = "public, max-age=31536000, immutable",
                Priority = 1000,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathOnly
            },

            new CacheRule
            {
                Name = "Static CSS/JS",
                PathPattern = @".*\.(css|js)$",
                IsRegex = true,
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromDays(7),
                CacheControl = "public, max-age=604800",
                Priority = 1000,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathOnly
            },

            // API endpoint rules
            new CacheRule
            {
                Name = "Blog Posts List",
                PathPattern = "/api/blog*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(15),
                CacheControl = "public, max-age=900",
                Priority = 800,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathAndQuery,
                VaryHeaders = new[] { "Accept", "Accept-Language" }
            },

            new CacheRule
            {
                Name = "Categories",
                PathPattern = "/api/category*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromHours(1),
                CacheControl = "public, max-age=3600",
                Priority = 800,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathAndQuery
            },

            new CacheRule
            {
                Name = "Tags",
                PathPattern = "/api/tag*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(30),
                CacheControl = "public, max-age=1800",
                Priority = 800,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathAndQuery
            },

            new CacheRule
            {
                Name = "Search Results",
                PathPattern = "/api/search*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(15),
                CacheControl = "public, max-age=900",
                Priority = 700,
                GenerateETag = false, // Search results may be dynamic
                IncludeLastModified = false,
                KeyStrategy = CacheKeyStrategy.PathAndQuery,
                VaryHeaders = new[] { "Accept", "Accept-Language" }
            },

            new CacheRule
            {
                Name = "Archive Pages",
                PathPattern = "/api/archive*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(30),
                CacheControl = "public, max-age=1800",
                Priority = 700,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathAndQuery
            },

            new CacheRule
            {
                Name = "Statistics",
                PathPattern = "/api/stats*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(5),
                CacheControl = "public, max-age=300",
                Priority = 600,
                GenerateETag = false, // Stats change frequently
                IncludeLastModified = false,
                KeyStrategy = CacheKeyStrategy.PathAndQuery
            },

            new CacheRule
            {
                Name = "Homepage Data",
                PathPattern = "/api/home*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(10),
                CacheControl = "public, max-age=600",
                Priority = 700,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathAndQuery
            },

            // Default API rule (lower priority)
            new CacheRule
            {
                Name = "Default API",
                PathPattern = "/api/*",
                Methods = new[] { "GET" },
                Duration = TimeSpan.FromMinutes(5),
                CacheControl = "public, max-age=300",
                Priority = 100,
                GenerateETag = true,
                IncludeLastModified = true,
                KeyStrategy = CacheKeyStrategy.PathAndQuery,
                VaryHeaders = new[] { "Accept", "Accept-Encoding" }
            }
        };
    }
}