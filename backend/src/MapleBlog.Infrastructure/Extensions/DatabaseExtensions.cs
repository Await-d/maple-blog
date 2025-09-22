using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Infrastructure.Data.Seeders;
using MapleBlog.Infrastructure.Data.Seeders.Core;
using MapleBlog.Infrastructure.Data.Seeders.Extensions;

namespace MapleBlog.Infrastructure.Extensions;

/// <summary>
/// Extension methods for database setup and seeding
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending migrations and seeds data if needed
    /// </summary>
    /// <param name="host">The application host</param>
    /// <param name="seedData">Whether to seed sample data (typically only in development)</param>
    /// <returns>The host for chaining</returns>
    public static async Task<IHost> InitializeDatabaseAsync(this IHost host, bool seedData = false)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<BlogDbContext>>();

        try
        {
            var context = services.GetRequiredService<BlogDbContext>();

            // Apply any pending migrations
            logger.LogInformation("Checking for pending database migrations...");

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                    pendingMigrations.Count(),
                    string.Join(", ", pendingMigrations));

                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogInformation("Database is up to date.");
            }

            // Use new seed data system if requested
            if (seedData)
            {
                logger.LogInformation("Initializing modern seed data system...");
                var environment = services.GetRequiredService<IHostEnvironment>();
                var seedDataManager = services.GetService<SeedDataManager>();

                if (seedDataManager != null)
                {
                    var result = await seedDataManager.SeedAsync(environment.EnvironmentName, false);
                    if (result.IsSuccess)
                    {
                        logger.LogInformation("Seed data completed: {Summary}", result.GetSummary());
                    }
                    else
                    {
                        logger.LogWarning("Seed data not completed: {Summary}", result.GetSummary());
                    }
                }
                else
                {
                    logger.LogWarning("SeedDataManager not registered. Please call AddSeedDataServices() in your DI configuration.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }

        return host;
    }

    /// <summary>
    /// Ensures the database is created and applies any pending migrations
    /// </summary>
    /// <param name="services">Service collection</param>
    public static async Task EnsureDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogDbContext>>();

        try
        {
            logger.LogInformation("Ensuring database exists...");

            // Create database if it doesn't exist
            var created = await context.Database.EnsureCreatedAsync();

            if (created)
            {
                logger.LogInformation("Database created successfully.");
            }
            else
            {
                // Apply migrations if database already exists
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying pending migrations...");
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Migrations applied successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring database exists.");
            throw;
        }
    }

    /// <summary>
    /// Drops and recreates the database (development only)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="seedData">Whether to seed data after recreation</param>
    public static async Task RecreateAndSeedDatabaseAsync(this IServiceProvider services, bool seedData = true)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlogDbContext>>();

        try
        {
            logger.LogWarning("Dropping and recreating database...");

            // Drop and create database
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            logger.LogInformation("Database recreated successfully.");

            if (seedData)
            {
                var seeder = new BlogContentSeeder(context, scope.ServiceProvider.GetRequiredService<ILogger<BlogContentSeeder>>());
                await seeder.SeedAsync();
                logger.LogInformation("Database seeded with sample data.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recreating database.");
            throw;
        }
    }

    /// <summary>
    /// Gets database health information
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <returns>Database health status</returns>
    public static async Task<DatabaseHealthInfo> GetDatabaseHealthAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        try
        {
            // Test connection and get basic stats
            var canConnect = await context.Database.CanConnectAsync();

            if (!canConnect)
            {
                return new DatabaseHealthInfo
                {
                    IsHealthy = false,
                    ConnectionString = context.Database.GetConnectionString() ?? "Unknown",
                    Provider = context.Database.ProviderName ?? "Unknown",
                    Error = "Cannot connect to database"
                };
            }

            var postCount = await context.Posts.CountAsync();
            var categoryCount = await context.Categories.CountAsync();
            var tagCount = await context.Tags.CountAsync();
            var userCount = await context.Users.CountAsync();

            return new DatabaseHealthInfo
            {
                IsHealthy = true,
                ConnectionString = MaskConnectionString(context.Database.GetConnectionString()),
                Provider = context.Database.ProviderName ?? "Unknown",
                PostCount = postCount,
                CategoryCount = categoryCount,
                TagCount = tagCount,
                UserCount = userCount,
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new DatabaseHealthInfo
            {
                IsHealthy = false,
                ConnectionString = MaskConnectionString(context.Database.GetConnectionString()),
                Provider = context.Database.ProviderName ?? "Unknown",
                Error = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Masks sensitive information in connection strings for logging
    /// </summary>
    private static string? MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // Simple masking - in production, use a more sophisticated approach
        var patterns = new[]
        {
            @"Password\s*=\s*[^;]+",
            @"PWD\s*=\s*[^;]+",
            @"User\s*ID\s*=\s*[^;]+",
            @"UID\s*=\s*[^;]+"
        };

        var masked = connectionString;
        foreach (var pattern in patterns)
        {
            masked = System.Text.RegularExpressions.Regex.Replace(
                masked, pattern,
                match => match.Value.Split('=')[0] + "=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return masked;
    }
}

/// <summary>
/// Database health information
/// </summary>
public class DatabaseHealthInfo
{
    public bool IsHealthy { get; set; }
    public string? ConnectionString { get; set; }
    public string? Provider { get; set; }
    public int PostCount { get; set; }
    public int CategoryCount { get; set; }
    public int TagCount { get; set; }
    public int UserCount { get; set; }
    public string? Error { get; set; }
    public DateTime LastChecked { get; set; }
}