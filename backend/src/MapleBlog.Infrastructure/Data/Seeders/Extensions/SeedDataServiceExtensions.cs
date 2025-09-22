using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MapleBlog.Infrastructure.Data.Seeders.Core;
using MapleBlog.Infrastructure.Data.Seeders.Providers;

namespace MapleBlog.Infrastructure.Data.Seeders.Extensions;

/// <summary>
/// Extension methods for registering seed data services
/// </summary>
public static class SeedDataServiceExtensions
{
    /// <summary>
    /// Adds seed data services to the service collection
    /// </summary>
    public static IServiceCollection AddSeedDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register seed data providers
        services.AddScoped<ProductionSeedDataProvider>();
        services.AddScoped<DevelopmentSeedDataProvider>();

        // Register seed data manager
        services.AddScoped<SeedDataManager>(provider =>
        {
            var context = provider.GetRequiredService<BlogDbContext>();
            var serviceProvider = provider;
            var logger = provider.GetRequiredService<ILogger<SeedDataManager>>();

            var manager = new SeedDataManager(context, serviceProvider, logger);

            // Register providers based on configuration
            var enabledEnvironments = configuration.GetSection("SeedData:EnabledEnvironments").Get<string[]>()
                ?? new[] { "Development", "Production" };

            if (enabledEnvironments.Contains("Production"))
            {
                manager.RegisterProvider(provider.GetRequiredService<ProductionSeedDataProvider>());
            }

            if (enabledEnvironments.Contains("Development"))
            {
                manager.RegisterProvider(provider.GetRequiredService<DevelopmentSeedDataProvider>());
            }

            return manager;
        });

        // Register seed data configuration
        services.Configure<SeedDataOptions>(configuration.GetSection("SeedData"));

        return services;
    }

    /// <summary>
    /// Seeds data based on the current environment
    /// </summary>
    public static async Task<IHost> SeedDataAsync(this IHost host, bool forceSeeding = false)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var environment = services.GetRequiredService<IHostEnvironment>();
        var logger = services.GetRequiredService<ILogger<SeedDataManager>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        try
        {
            // Check if seeding is enabled
            var seedingEnabled = configuration.GetValue<bool>("SeedData:Enabled", true);
            if (!seedingEnabled && !forceSeeding)
            {
                logger.LogInformation("Seed data is disabled in configuration");
                return host;
            }

            var manager = services.GetRequiredService<SeedDataManager>();
            var environmentName = environment.EnvironmentName;

            logger.LogInformation("Starting seed data process for environment: {Environment}", environmentName);

            var result = await manager.SeedAsync(environmentName, forceSeeding);

            if (result.IsSuccess)
            {
                logger.LogInformation("Seed data process completed successfully: {Summary}", result.GetSummary());
            }
            else if (result.RequiresConfirmation)
            {
                logger.LogWarning("Seed data process requires confirmation: {Message}", result.ErrorMessage);
            }
            else
            {
                logger.LogError("Seed data process failed: {Summary}", result.GetSummary());

                // In development, don't crash the application
                if (environment.IsDevelopment())
                {
                    logger.LogWarning("Continuing startup despite seeding failure in development environment");
                }
                else
                {
                    throw new InvalidOperationException($"Seed data process failed: {result.ErrorMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during seed data process");

            // In development, log and continue
            if (environment.IsDevelopment())
            {
                logger.LogWarning("Continuing startup despite critical seeding error in development environment");
            }
            else
            {
                throw;
            }
        }

        return host;
    }

    /// <summary>
    /// Cleans test data from the database
    /// </summary>
    public static async Task<IHost> CleanTestDataAsync(this IHost host, bool dryRun = true)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var manager = services.GetRequiredService<SeedDataManager>();
        var logger = services.GetRequiredService<ILogger<SeedDataManager>>();

        try
        {
            logger.LogInformation("Starting test data cleanup (dry run: {DryRun})", dryRun);

            var result = await manager.CleanTestDataAsync(dryRun);

            if (result.IsSuccess)
            {
                logger.LogInformation("Test data cleanup completed: {Summary}", result.GetSummary());
            }
            else
            {
                logger.LogError("Test data cleanup failed: {Summary}", result.GetSummary());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during test data cleanup");
            throw;
        }

        return host;
    }

    /// <summary>
    /// Gets seed data status for the current environment
    /// </summary>
    public static async Task<SeedStatus> GetSeedStatusAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var manager = services.GetRequiredService<SeedDataManager>();
        var environment = services.GetRequiredService<IHostEnvironment>();

        return await manager.GetSeedStatusAsync(environment.EnvironmentName);
    }
}

/// <summary>
/// Configuration options for seed data
/// </summary>
public class SeedDataOptions
{
    /// <summary>
    /// Whether seed data is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Environments where seeding is enabled
    /// </summary>
    public string[] EnabledEnvironments { get; set; } = { "Development", "Production" };

    /// <summary>
    /// Whether to auto-seed on application startup
    /// </summary>
    public bool AutoSeedOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to require confirmation for production seeding
    /// </summary>
    public bool RequireProductionConfirmation { get; set; } = true;

    /// <summary>
    /// Whether to backup before seeding in production
    /// </summary>
    public bool BackupInProduction { get; set; } = true;

    /// <summary>
    /// Maximum number of existing records before preventing seeding
    /// </summary>
    public int MaxExistingRecords { get; set; } = 0;

    /// <summary>
    /// Whether to validate data after seeding
    /// </summary>
    public bool ValidateAfterSeeding { get; set; } = true;

    /// <summary>
    /// Whether to create audit logs for seeding operations
    /// </summary>
    public bool CreateAuditLogs { get; set; } = true;

    /// <summary>
    /// Custom settings for different environments
    /// </summary>
    public Dictionary<string, object> EnvironmentSettings { get; set; } = new();
}