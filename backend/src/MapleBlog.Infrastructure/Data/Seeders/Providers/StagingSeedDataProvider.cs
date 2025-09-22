using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Enums;
using MapleBlog.Infrastructure.Data.Seeders.Core;

namespace MapleBlog.Infrastructure.Data.Seeders.Providers;

/// <summary>
/// Staging environment seed data provider - provides production-like data for staging/testing
/// </summary>
public class StagingSeedDataProvider : BaseSeedDataProvider
{
    private readonly IConfiguration _configuration;

    public StagingSeedDataProvider(ILogger<StagingSeedDataProvider> logger, IConfiguration configuration)
        : base(logger, "Staging")
    {
        _configuration = configuration;
    }

    public override int Priority => 150; // Between development and production

    public override async Task<SeedDataConfiguration> GetSeedDataConfigurationAsync()
    {
        return new SeedDataConfiguration
        {
            AllowSeeding = true,
            RequireConfirmation = true,
            BackupBeforeSeeding = true,
            ClearExistingData = false,
            MaxExistingRecords = 100, // Allow some existing data
            CreateAuditLogs = true,
            ValidateAfterSeeding = true,
            ValidationRules = new List<string>
            {
                "NoProductionData",
                "SecurePasswords",
                "StagingEmails",
                "ProductionLikeData"
            },
            CustomSettings = new Dictionary<string, object>
            {
                { "RequireEnvVars", true },
                { "AllowTestAccounts", true },
                { "ProductionLikeStructure", true }
            }
        };
    }

    protected override async Task PerformCustomValidationAsync(BlogDbContext context, SeedValidationResult result)
    {
        // Ensure no production data exists
        var productionUsers = await context.Users
            .Where(u => !u.Email.Value.Contains("staging") &&
                       !u.Email.Value.Contains("test") &&
                       !u.Email.Value.Contains("example") &&
                       u.Email.Value.Contains("@"))
            .CountAsync();

        if (productionUsers > 10) // Allow some users but not too many
        {
            result.Warnings.Add($"Found {productionUsers} potential production users in staging database");
        }

        // Validate required environment variables for staging
        var adminEmail = System.Environment.GetEnvironmentVariable("STAGING_ADMIN_EMAIL");
        var adminPassword = System.Environment.GetEnvironmentVariable("STAGING_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(adminEmail))
        {
            result.Warnings.Add("STAGING_ADMIN_EMAIL environment variable not set, using default");
        }

        if (string.IsNullOrEmpty(adminPassword))
        {
            result.Warnings.Add("STAGING_ADMIN_PASSWORD environment variable not set, using default");
        }

        // Validate staging email domains
        if (!string.IsNullOrEmpty(adminEmail))
        {
            var allowedDomains = new[] { "staging", "test", "example.com" };
            if (!allowedDomains.Any(domain => adminEmail.Contains(domain)))
            {
                result.Warnings.Add("Staging admin email should use staging/test domain");
            }
        }
    }

    public override async Task<IEnumerable<User>> GetUsersAsync()
    {
        var users = new List<User>();

        // Create system user
        var systemUser = CreateSystemUser(
            "system",
            "system@staging.maple-blog.com",
            "System User",
            "Internal system user for automated operations"
        );
        systemUser.PasswordHash = CreatePasswordHash(Guid.NewGuid().ToString());
        systemUser.IsActive = false;
        users.Add(systemUser);

        // Create admin user from environment or use default
        var adminEmail = System.Environment.GetEnvironmentVariable("STAGING_ADMIN_EMAIL") ?? "admin@staging.maple-blog.com";
        var adminPassword = System.Environment.GetEnvironmentVariable("STAGING_ADMIN_PASSWORD") ?? "StagingAdmin123!";
        var adminName = System.Environment.GetEnvironmentVariable("STAGING_ADMIN_NAME") ?? "Staging Administrator";

        var adminUser = CreateSystemUser(
            "admin",
            adminEmail,
            adminName,
            "Staging environment administrator"
        );
        adminUser.PasswordHash = CreatePasswordHash(adminPassword);
        users.Add(adminUser);

        // Create test users for staging validation
        var testUsers = new[]
        {
            new { Username = "author_test", Email = "author@staging.maple-blog.com", DisplayName = "Test Author", Bio = "Content author for staging tests" },
            new { Username = "user_test", Email = "user@staging.maple-blog.com", DisplayName = "Test User", Bio = "Regular user for staging tests" },
            new { Username = "moderator_test", Email = "moderator@staging.maple-blog.com", DisplayName = "Test Moderator", Bio = "Content moderator for staging tests" },
        };

        foreach (var testUser in testUsers)
        {
            var user = CreateSystemUser(
                testUser.Username,
                testUser.Email,
                testUser.DisplayName,
                testUser.Bio
            );
            user.PasswordHash = CreatePasswordHash("TestUser123!");
            users.Add(user);
        }

        Logger.LogInformation("Created {Count} staging users", users.Count);

        return users;
    }

    public override async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        // Production-like categories but clearly marked as staging
        return new List<Category>
        {
            CreateCategory("General", "General content and announcements", "#6B7280"),
            CreateCategory("Documentation", "Documentation and guides", "#3B82F6"),
            CreateCategory("Updates", "System updates and news", "#10B981"),
            CreateCategory("Testing", "Testing and QA related content", "#EC4899"),
            CreateCategory("Staging Notes", "Staging environment specific content", "#F59E0B"),
            CreateCategory("Beta Features", "Beta features and previews", "#8B5CF6")
        };
    }

    public override async Task<IEnumerable<Tag>> GetTagsAsync()
    {
        // Mix of production and testing tags
        return new List<Tag>
        {
            CreateTag("Important", "Important content", "#EF4444"),
            CreateTag("Documentation", "Documentation content", "#3B82F6"),
            CreateTag("Update", "System updates", "#10B981"),
            CreateTag("Guide", "User guides", "#F59E0B"),
            CreateTag("Testing", "Test content", "#EC4899"),
            CreateTag("Staging", "Staging environment", "#8B5CF6"),
            CreateTag("Beta", "Beta features", "#6366F1"),
            CreateTag("Preview", "Preview content", "#84CC16"),
            CreateTag("QA", "Quality assurance", "#F97316"),
            CreateTag("Performance", "Performance testing", "#EF4444")
        };
    }

    public override async Task<IEnumerable<Post>> GetPostsAsync()
    {
        var posts = new List<Post>();

        // Staging welcome post
        var welcomePost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Welcome to Staging Environment",
            Slug = "welcome-to-staging-environment",
            Summary = "This is the staging environment for Maple Blog. Use this environment for testing and validation before production deployment.",
            Content = CreateStagingWelcomeContent(),
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-7),
            IsFeatured = true,
            IsSticky = true,
            AllowComments = true,
            MetaTitle = "Staging Environment - Maple Blog",
            MetaDescription = "Staging environment for testing and validation",
            ReadingTime = 3,
            WordCount = 500,
            ViewCount = 25,
            LikeCount = 3,
            CommentCount = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow.AddDays(-7)
        };
        posts.Add(welcomePost);

        // Test post for validation
        var testPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Staging Test Post - Do Not Delete",
            Slug = "staging-test-post-do-not-delete",
            Summary = "This is a test post used for staging validation. Please do not delete this post as it is used for automated testing.",
            Content = CreateStagingTestContent(),
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-3),
            AllowComments = true,
            MetaTitle = "Staging Test Post",
            MetaDescription = "Test post for staging environment validation",
            ReadingTime = 2,
            WordCount = 300,
            ViewCount = 10,
            LikeCount = 0,
            CommentCount = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };
        posts.Add(testPost);

        // Draft post for testing publishing workflow
        var draftPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Draft Post for Publishing Tests",
            Slug = "draft-post-for-publishing-tests",
            Summary = "This draft post is used for testing the publishing workflow in staging environment.",
            Content = "This is a draft post content for testing publishing workflows.",
            Status = PostStatus.Draft,
            AllowComments = true,
            MetaTitle = "Draft Post for Testing",
            MetaDescription = "Draft post for testing publishing workflow",
            ReadingTime = 1,
            WordCount = 100,
            ViewCount = 0,
            LikeCount = 0,
            CommentCount = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        posts.Add(draftPost);

        Logger.LogInformation("Created {Count} staging posts", posts.Count);

        return posts;
    }

    public override async Task<IEnumerable<SystemConfiguration>> GetSystemConfigurationsAsync()
    {
        var baseConfigs = (await base.GetDefaultSystemConfigurationsAsync()).ToList();

        // Add staging-specific configurations
        var stagingConfigs = new List<SystemConfiguration>
        {
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Environment.Mode",
                Value = "Staging",
                Description = "Current environment mode",
                Category = "System",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Testing.Enabled",
                Value = "true",
                Description = "Enable testing features",
                Category = "Testing",
                IsPublic = false,
                DataType = "boolean"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Logging.Level",
                Value = "Information",
                Description = "Default logging level for staging",
                Category = "System",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Cache.Enabled",
                Value = "true",
                Description = "Enable caching in staging",
                Category = "Performance",
                IsPublic = false,
                DataType = "boolean"
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
                Key = "Testing.AutomatedTestsEnabled",
                Value = "true",
                Description = "Enable automated testing endpoints",
                Category = "Testing",
                IsPublic = false,
                DataType = "boolean"
            }
        };

        // Update site name for staging
        var siteNameConfig = baseConfigs.FirstOrDefault(c => c.Key == "Site.Name");
        if (siteNameConfig != null)
        {
            siteNameConfig.Value = "Maple Blog (Staging)";
        }

        return baseConfigs.Concat(stagingConfigs);
    }

    #region Content Generation Methods

    private string CreateStagingWelcomeContent()
    {
        return @"# Welcome to Staging Environment

**⚠️ This is a staging environment for testing purposes**

This environment is used for:

## Testing & Validation
- Feature testing before production deployment
- Integration testing with external services
- Performance and load testing
- User acceptance testing (UAT)

## Environment Details
- **Environment**: Staging
- **Database**: Staging database (isolated from production)
- **Cache**: Redis staging instance
- **Logs**: Centralized logging with staging tags

## Important Notes
- Data in this environment may be reset periodically
- Do not store important data here
- Use test accounts for all operations
- Report any issues to the development team

## Test Accounts
- Admin: admin@staging.maple-blog.com
- Author: author@staging.maple-blog.com
- User: user@staging.maple-blog.com

All test accounts use the password: `TestUser123!`

## Next Steps
1. Verify core functionality
2. Test user workflows
3. Validate integrations
4. Check performance metrics

Happy testing! 🧪";
    }

    private string CreateStagingTestContent()
    {
        return @"# Staging Test Post

**This is a test post for staging validation - DO NOT DELETE**

This post is used for automated testing and validation in the staging environment.

## Test Scenarios Covered
- Post creation and publishing
- Content rendering
- SEO metadata
- Comment functionality
- Social sharing
- Search indexing

## Technical Details
- **Created**: Automatically via seed data
- **Status**: Published
- **Categories**: Testing
- **Tags**: Testing, Staging

## Validation Checklist
- [ ] Post displays correctly
- [ ] Comments can be added
- [ ] Social sharing works
- [ ] SEO metadata is present
- [ ] Search indexing functions

If you can see this post and all functionality works correctly, the staging environment is functioning properly.";
    }

    #endregion
}