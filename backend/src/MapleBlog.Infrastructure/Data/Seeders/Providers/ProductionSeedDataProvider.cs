using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Infrastructure.Data.Seeders.Core;

namespace MapleBlog.Infrastructure.Data.Seeders.Providers;

/// <summary>
/// Production environment seed data provider - provides minimal essential data for production
/// </summary>
public class ProductionSeedDataProvider : BaseSeedDataProvider
{
    private readonly IConfiguration _configuration;

    public ProductionSeedDataProvider(ILogger<ProductionSeedDataProvider> logger, IConfiguration configuration)
        : base(logger, "Production")
    {
        _configuration = configuration;
    }

    public override int Priority => 200; // Higher priority than base

    public override async Task<SeedDataConfiguration> GetSeedDataConfigurationAsync()
    {
        return new SeedDataConfiguration
        {
            AllowSeeding = true,
            RequireConfirmation = true,
            BackupBeforeSeeding = true,
            ClearExistingData = false,
            MaxExistingRecords = 0, // Don't seed if any data exists
            CreateAuditLogs = true,
            ValidateAfterSeeding = true,
            ValidationRules = new List<string>
            {
                "NoTestData",
                "SecurePasswords",
                "ProductionEmails",
                "MinimalData"
            },
            CustomSettings = new Dictionary<string, object>
            {
                { "RequireEnvVars", true },
                { "ValidateSSL", true },
                { "StrictValidation", true }
            }
        };
    }

    protected override async Task PerformCustomValidationAsync(BlogDbContext context, SeedValidationResult result)
    {
        // Ensure no test data exists
        var testUsers = await context.Users
            .Where(u => u.Email.Value.Contains("example.com") ||
                       u.Email.Value.Contains("test.com") ||
                       u.UserName.Contains("test") ||
                       u.UserName.Contains("demo"))
            .CountAsync();

        if (testUsers > 0)
        {
            result.IsValid = false;
            result.Errors.Add($"Found {testUsers} test users in production database. Clean test data before seeding.");
        }

        // Validate required environment variables
        var requiredEnvVars = new[] { "ADMIN_EMAIL", "ADMIN_PASSWORD" };
        foreach (var envVar in requiredEnvVars)
        {
            var value = System.Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(value))
            {
                result.IsValid = false;
                result.Errors.Add($"Required environment variable {envVar} is not set");
            }
        }

        // Validate admin email format
        var adminEmail = System.Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        if (!string.IsNullOrEmpty(adminEmail))
        {
            if (adminEmail.Contains("example.com") || adminEmail.Contains("test.com"))
            {
                result.IsValid = false;
                result.Errors.Add("Admin email must not use test domains (example.com, test.com)");
            }
        }

        // Validate admin password strength
        var adminPassword = System.Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (!string.IsNullOrEmpty(adminPassword))
        {
            if (adminPassword.Length < 12)
            {
                result.IsValid = false;
                result.Errors.Add("Admin password must be at least 12 characters long");
            }

            if (!HasComplexPassword(adminPassword))
            {
                result.IsValid = false;
                result.Errors.Add("Admin password must contain uppercase, lowercase, numbers, and special characters");
            }
        }
    }

    public override async Task<IEnumerable<User>> GetUsersAsync()
    {
        var users = new List<User>();

        // Create system user
        var systemUser = CreateSystemUser(
            "system",
            "system@maple-blog.internal",
            "System User",
            "Internal system user for automated operations"
        );
        systemUser.PasswordHash = CreatePasswordHash(Guid.NewGuid().ToString()); // Random password
        systemUser.IsActive = false; // System user is inactive by default
        users.Add(systemUser);

        // Create admin user from environment variables
        var adminEmail = System.Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = System.Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        var adminName = System.Environment.GetEnvironmentVariable("ADMIN_NAME") ?? "Administrator";

        if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
        {
            var adminUser = CreateSystemUser(
                "admin",
                adminEmail,
                adminName,
                "System administrator"
            );
            adminUser.PasswordHash = CreatePasswordHash(adminPassword);
            users.Add(adminUser);

            Logger.LogInformation("Created production admin user with email: {Email}", adminEmail);
        }
        else
        {
            Logger.LogWarning("Admin user not created - missing ADMIN_EMAIL or ADMIN_PASSWORD environment variables");
        }

        return users;
    }

    public override async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        // Minimal essential categories for production
        return new List<Category>
        {
            CreateCategory("General", "General content and announcements", "#6B7280"),
            CreateCategory("Documentation", "Documentation and guides", "#3B82F6"),
            CreateCategory("Updates", "System updates and news", "#10B981")
        };
    }

    public override async Task<IEnumerable<Tag>> GetTagsAsync()
    {
        // Essential tags for production
        return new List<Tag>
        {
            CreateTag("Important", "Important content", "#EF4444"),
            CreateTag("Documentation", "Documentation content", "#3B82F6"),
            CreateTag("Update", "System updates", "#10B981"),
            CreateTag("Guide", "User guides", "#F59E0B")
        };
    }

    public override async Task<IEnumerable<Post>> GetPostsAsync()
    {
        // No default posts in production - let users create their own content
        return Enumerable.Empty<Post>();
    }

    public override async Task<IEnumerable<SystemConfiguration>> GetSystemConfigurationsAsync()
    {
        var baseConfigs = (await base.GetDefaultSystemConfigurationsAsync()).ToList();

        // Override/add production-specific configurations
        var productionConfigs = new List<SystemConfiguration>
        {
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Environment.Mode",
                Value = "Production",
                Description = "Current environment mode",
                Category = "System",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Security.RequireHttps",
                Value = "true",
                Description = "Require HTTPS connections",
                Category = "Security",
                IsPublic = false,
                DataType = "boolean"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Security.StrictCors",
                Value = "true",
                Description = "Enable strict CORS policy",
                Category = "Security",
                IsPublic = false,
                DataType = "boolean"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Logging.Level",
                Value = "Warning",
                Description = "Default logging level for production",
                Category = "System",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Cache.Enabled",
                Value = "true",
                Description = "Enable caching in production",
                Category = "Performance",
                IsPublic = false,
                DataType = "boolean"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Content.DefaultLanguage",
                Value = "en-US",
                Description = "Default content language",
                Category = "Content",
                IsPublic = true,
                DataType = "string"
            }
        };

        // Update site name from configuration if available
        var siteName = _configuration.GetValue<string>("Site:Name");
        if (!string.IsNullOrEmpty(siteName))
        {
            var siteNameConfig = baseConfigs.FirstOrDefault(c => c.Key == "Site.Name");
            if (siteNameConfig != null)
            {
                siteNameConfig.Value = siteName;
            }
        }

        return baseConfigs.Concat(productionConfigs);
    }

    /// <summary>
    /// Validates password complexity
    /// </summary>
    private bool HasComplexPassword(string password)
    {
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }
}