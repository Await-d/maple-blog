using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MapleBlog.Infrastructure.Caching;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring caching services
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Adds caching services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure cache options
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        if (!cacheOptions.Enabled)
        {
            // Add a no-op cache service when caching is disabled
            services.AddSingleton<ICacheService, NoOpCacheService>();
            return services;
        }

        switch (cacheOptions.ProviderType)
        {
            case CacheProviderType.Memory:
                AddMemoryCache(services);
                break;

            case CacheProviderType.Redis:
                // Redis caching requires additional package reference
                // Falling back to memory cache for now
                AddMemoryCache(services);
                break;

            case CacheProviderType.SqlServer:
                // SQL Server caching requires additional package reference
                // Falling back to memory cache for now
                AddMemoryCache(services);
                break;

            default:
                AddMemoryCache(services);
                break;
        }

        // Add cached service decorators
        AddCachedServices(services);

        return services;
    }

    private static void AddMemoryCache(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
    }

    // Redis caching temporarily disabled due to missing package reference
    // private static void AddRedisCache(IServiceCollection services, IConfiguration configuration, CacheOptions cacheOptions)
    // {
    //     var connectionString = cacheOptions.RedisConnectionString ??
    //                           configuration.GetConnectionString("Redis") ??
    //                           "localhost:6379";
    //
    //     services.AddStackExchangeRedisCache(options =>
    //     {
    //         options.Configuration = connectionString;
    //         options.InstanceName = "MapleBlog";
    //     });
    //
    //     services.AddSingleton<ICacheService, DistributedCacheService>();
    // }

    // SQL Server caching temporarily disabled due to missing package reference
    // private static void AddSqlServerCache(IServiceCollection services, IConfiguration configuration)
    // {
    //     var connectionString = configuration.GetConnectionString("DefaultConnection") ??
    //                           throw new InvalidOperationException("Default connection string is required for SQL Server caching");
    //
    //     services.AddDistributedSqlServerCache(options =>
    //     {
    //         options.ConnectionString = connectionString;
    //         options.SchemaName = "dbo";
    //         options.TableName = "CacheEntries";
    //     });
    //
    //     services.AddSingleton<ICacheService, DistributedCacheService>();
    // }

    private static void AddCachedServices(IServiceCollection services)
    {
        // Decorate existing services with caching
        services.Decorate<IBlogService, CachedBlogService>();

        // Add other cached service decorators as needed
        // services.Decorate<ICategoryService, CachedCategoryService>();
        // services.Decorate<ITagService, CachedTagService>();
    }

    // Response caching and compression temporarily disabled due to missing package references
    // /// <summary>
    // /// Adds response caching middleware configuration
    // /// </summary>
    // public static IServiceCollection AddResponseCachingServices(this IServiceCollection services, IConfiguration configuration)
    // {
    //     services.AddResponseCaching(options =>
    //     {
    //         options.MaximumBodySize = 64 * 1024; // 64KB
    //         options.SizeLimit = 50 * 1024 * 1024; // 50MB
    //     });
    //
    //     // Configure response cache options
    //     services.Configure<ResponseCacheOptions>(configuration.GetSection("ResponseCache"));
    //
    //     return services;
    // }
    //
    // /// <summary>
    // /// Adds compression services for better performance
    // /// </summary>
    // public static IServiceCollection AddCompressionServices(this IServiceCollection services)
    // {
    //     services.AddResponseCompression(options =>
    //     {
    //         options.EnableForHttps = true;
    //         options.Providers.Add<BrotliCompressionProvider>();
    //         options.Providers.Add<GzipCompressionProvider>();
    //     });
    //
    //     services.Configure<BrotliCompressionProviderOptions>(options =>
    //     {
    //         options.Level = System.IO.Compression.CompressionLevel.Optimal;
    //     });
    //
    //     services.Configure<GzipCompressionProviderOptions>(options =>
    //     {
    //         options.Level = System.IO.Compression.CompressionLevel.Optimal;
    //     });
    //
    //     return services;
    // }
}

/// <summary>
/// No-operation cache service for when caching is disabled
/// </summary>
internal class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        => await factory();

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
        => Task.FromResult(keys.ToDictionary(k => k, k => (T?)null));

    public Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        => Task.CompletedTask;
}

/// <summary>
/// Extension method to decorate services with caching
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface))
            ?? throw new InvalidOperationException($"Service {typeof(TInterface).Name} is not registered");

        var decoratedType = serviceDescriptor.ImplementationType
                           ?? serviceDescriptor.ImplementationInstance?.GetType()
                           ?? serviceDescriptor.ImplementationFactory?.GetType().GetGenericArguments().First();

        if (serviceDescriptor.ImplementationInstance != null)
        {
            services.Remove(serviceDescriptor);
            services.AddSingleton<TInterface>(provider =>
            {
                var decoratorArgs = typeof(TDecorator).GetConstructors()
                    .First()
                    .GetParameters()
                    .Select(p =>
                    {
                        if (p.ParameterType == typeof(TInterface))
                            return serviceDescriptor.ImplementationInstance;
                        return provider.GetRequiredService(p.ParameterType);
                    })
                    .ToArray();

                return (TInterface)Activator.CreateInstance(typeof(TDecorator), decoratorArgs)!;
            });
        }
        else if (serviceDescriptor.ImplementationFactory != null)
        {
            services.Remove(serviceDescriptor);
            services.Add(ServiceDescriptor.Describe(typeof(TInterface), provider =>
            {
                var originalService = serviceDescriptor.ImplementationFactory(provider);
                var decoratorArgs = typeof(TDecorator).GetConstructors()
                    .First()
                    .GetParameters()
                    .Select(p =>
                    {
                        if (p.ParameterType == typeof(TInterface))
                            return originalService;
                        return provider.GetRequiredService(p.ParameterType);
                    })
                    .ToArray();

                return (TInterface)Activator.CreateInstance(typeof(TDecorator), decoratorArgs)!;
            }, serviceDescriptor.Lifetime));
        }
        else if (decoratedType != null)
        {
            services.Remove(serviceDescriptor);
            services.Add(ServiceDescriptor.Describe(decoratedType, decoratedType, serviceDescriptor.Lifetime));
            services.Add(ServiceDescriptor.Describe(typeof(TInterface), provider =>
            {
                var originalService = provider.GetRequiredService(decoratedType);
                var decoratorArgs = typeof(TDecorator).GetConstructors()
                    .First()
                    .GetParameters()
                    .Select(p =>
                    {
                        if (p.ParameterType == typeof(TInterface))
                            return originalService;
                        return provider.GetRequiredService(p.ParameterType);
                    })
                    .ToArray();

                return (TInterface)Activator.CreateInstance(typeof(TDecorator), decoratorArgs)!;
            }, serviceDescriptor.Lifetime));
        }

        return services;
    }
}