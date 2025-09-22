using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Infrastructure.Data.Seeders.Core;

namespace MapleBlog.Infrastructure.Data.Seeders.Validation;

/// <summary>
/// Comprehensive validator for seed data integrity and consistency
/// </summary>
public class SeedDataValidator
{
    private readonly BlogDbContext _context;
    private readonly ILogger<SeedDataValidator> _logger;

    public SeedDataValidator(BlogDbContext context, ILogger<SeedDataValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Performs comprehensive validation of seeded data
    /// </summary>
    public async Task<ValidationReport> ValidateAllAsync(string environment)
    {
        var report = new ValidationReport
        {
            Environment = environment,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting comprehensive seed data validation for environment: {Environment}", environment);

            // Core entity validations
            await ValidateUsersAsync(report);
            await ValidateRolesAsync(report);
            await ValidatePermissionsAsync(report);
            await ValidateCategoriesAsync(report);
            await ValidateTagsAsync(report);
            await ValidatePostsAsync(report);
            await ValidateSystemConfigurationsAsync(report);

            // Relationship validations
            await ValidateRelationshipsAsync(report);

            // Security validations
            await ValidateSecurityAsync(report, environment);

            // Data consistency validations
            await ValidateDataConsistencyAsync(report);

            // Environment-specific validations
            await ValidateEnvironmentSpecificAsync(report, environment);

            report.EndTime = DateTime.UtcNow;
            report.IsValid = report.Errors.Count == 0;

            _logger.LogInformation("Validation completed for {Environment}. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                environment, report.IsValid, report.Errors.Count, report.Warnings.Count);

            return report;
        }
        catch (Exception ex)
        {
            report.EndTime = DateTime.UtcNow;
            report.IsValid = false;
            report.Errors.Add($"Critical validation error: {ex.Message}");
            _logger.LogError(ex, "Critical error during seed data validation");
            return report;
        }
    }

    private async Task ValidateUsersAsync(ValidationReport report)
    {
        var section = "Users";

        try
        {
            var users = await _context.Users.ToListAsync();
            report.EntityCounts[section] = users.Count;

            if (users.Count == 0)
            {
                report.Errors.Add("No users found in database");
                return;
            }

            // Check for admin user
            var adminUsers = users.Where(u => u.UserName.ToLower().Contains("admin")).ToList();
            if (!adminUsers.Any())
            {
                report.Errors.Add("No admin user found");
            }

            // Check for system user
            var systemUsers = users.Where(u => u.UserName.ToLower() == "system").ToList();
            if (!systemUsers.Any())
            {
                report.Warnings.Add("No system user found");
            }

            // Validate user data integrity
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.UserName))
                {
                    report.Errors.Add($"User {user.Id} has empty username");
                }

                if (string.IsNullOrEmpty(user.Email?.Value))
                {
                    report.Errors.Add($"User {user.UserName} has empty email");
                }

                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    report.Errors.Add($"User {user.UserName} has empty password hash");
                }

                if (user.PasswordHash?.Length < 20)
                {
                    report.Warnings.Add($"User {user.UserName} has suspiciously short password hash");
                }
            }

            // Check for duplicate usernames
            var duplicateUsernames = users
                .GroupBy(u => u.UserName.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateUsernames)
            {
                report.Errors.Add($"Duplicate username found: {duplicate}");
            }

            // Check for duplicate emails
            var duplicateEmails = users
                .GroupBy(u => u.Email.Value.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateEmails)
            {
                report.Errors.Add($"Duplicate email found: {duplicate}");
            }

            _logger.LogDebug("User validation completed. Found {Count} users", users.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating users: {ex.Message}");
            _logger.LogError(ex, "Error during user validation");
        }
    }

    private async Task ValidateRolesAsync(ValidationReport report)
    {
        var section = "Roles";

        try
        {
            var roles = await _context.Roles.ToListAsync();
            report.EntityCounts[section] = roles.Count;

            if (roles.Count == 0)
            {
                report.Errors.Add("No roles found in database");
                return;
            }

            // Check for essential roles
            var essentialRoles = new[] { "Admin", "User", "Author" };
            foreach (var essentialRole in essentialRoles)
            {
                if (!roles.Any(r => r.Name.Equals(essentialRole, StringComparison.OrdinalIgnoreCase)))
                {
                    report.Errors.Add($"Essential role missing: {essentialRole}");
                }
            }

            // Validate role data integrity
            foreach (var role in roles)
            {
                if (string.IsNullOrEmpty(role.Name))
                {
                    report.Errors.Add($"Role {role.Id} has empty name");
                }

                if (string.IsNullOrEmpty(role.NormalizedName))
                {
                    report.Errors.Add($"Role {role.Name} has empty normalized name");
                }

                if (role.NormalizedName != role.Name.ToUpperInvariant())
                {
                    report.Warnings.Add($"Role {role.Name} has incorrect normalized name");
                }
            }

            // Check for duplicate role names
            var duplicateRoles = roles
                .GroupBy(r => r.NormalizedName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateRoles)
            {
                report.Errors.Add($"Duplicate role found: {duplicate}");
            }

            _logger.LogDebug("Role validation completed. Found {Count} roles", roles.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating roles: {ex.Message}");
            _logger.LogError(ex, "Error during role validation");
        }
    }

    private async Task ValidatePermissionsAsync(ValidationReport report)
    {
        var section = "Permissions";

        try
        {
            var permissions = await _context.Permissions.ToListAsync();
            report.EntityCounts[section] = permissions.Count;

            if (permissions.Count == 0)
            {
                report.Warnings.Add("No permissions found in database");
                return;
            }

            // Check for essential permissions
            var essentialPermissions = new[]
            {
                "Users.Read", "Users.Create", "Users.Update", "Users.Delete",
                "Posts.Read", "Posts.Create", "Posts.Update", "Posts.Delete",
                "System.FullAccess"
            };

            foreach (var essentialPermission in essentialPermissions)
            {
                if (!permissions.Any(p => p.Name.Equals(essentialPermission, StringComparison.OrdinalIgnoreCase)))
                {
                    report.Warnings.Add($"Recommended permission missing: {essentialPermission}");
                }
            }

            // Validate permission data integrity
            foreach (var permission in permissions)
            {
                if (string.IsNullOrEmpty(permission.Name))
                {
                    report.Errors.Add($"Permission {permission.Id} has empty name");
                }

                if (string.IsNullOrEmpty(permission.Category))
                {
                    report.Warnings.Add($"Permission {permission.Name} has empty category");
                }
            }

            // Check for duplicate permissions
            var duplicatePermissions = permissions
                .GroupBy(p => p.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicatePermissions)
            {
                report.Errors.Add($"Duplicate permission found: {duplicate}");
            }

            _logger.LogDebug("Permission validation completed. Found {Count} permissions", permissions.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating permissions: {ex.Message}");
            _logger.LogError(ex, "Error during permission validation");
        }
    }

    private async Task ValidateCategoriesAsync(ValidationReport report)
    {
        var section = "Categories";

        try
        {
            var categories = await _context.Categories.ToListAsync();
            report.EntityCounts[section] = categories.Count;

            if (categories.Count == 0)
            {
                report.Warnings.Add("No categories found in database");
                return;
            }

            // Validate category data integrity
            foreach (var category in categories)
            {
                if (string.IsNullOrEmpty(category.Name))
                {
                    report.Errors.Add($"Category {category.Id} has empty name");
                }

                if (string.IsNullOrEmpty(category.Slug))
                {
                    report.Errors.Add($"Category {category.Name} has empty slug");
                }

                if (category.Slug?.Contains(" ") == true)
                {
                    report.Warnings.Add($"Category {category.Name} slug contains spaces");
                }
            }

            // Check for duplicate slugs
            var duplicateSlugs = categories
                .GroupBy(c => c.Slug?.ToLowerInvariant())
                .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                .Select(g => g.Key);

            foreach (var duplicate in duplicateSlugs)
            {
                report.Errors.Add($"Duplicate category slug found: {duplicate}");
            }

            _logger.LogDebug("Category validation completed. Found {Count} categories", categories.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating categories: {ex.Message}");
            _logger.LogError(ex, "Error during category validation");
        }
    }

    private async Task ValidateTagsAsync(ValidationReport report)
    {
        var section = "Tags";

        try
        {
            var tags = await _context.Tags.ToListAsync();
            report.EntityCounts[section] = tags.Count;

            if (tags.Count == 0)
            {
                report.Warnings.Add("No tags found in database");
                return;
            }

            // Validate tag data integrity
            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag.Name))
                {
                    report.Errors.Add($"Tag {tag.Id} has empty name");
                }

                if (string.IsNullOrEmpty(tag.Slug))
                {
                    report.Errors.Add($"Tag {tag.Name} has empty slug");
                }

                if (tag.Slug?.Contains(" ") == true)
                {
                    report.Warnings.Add($"Tag {tag.Name} slug contains spaces");
                }
            }

            // Check for duplicate slugs
            var duplicateSlugs = tags
                .GroupBy(t => t.Slug?.ToLowerInvariant())
                .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                .Select(g => g.Key);

            foreach (var duplicate in duplicateSlugs)
            {
                report.Errors.Add($"Duplicate tag slug found: {duplicate}");
            }

            _logger.LogDebug("Tag validation completed. Found {Count} tags", tags.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating tags: {ex.Message}");
            _logger.LogError(ex, "Error during tag validation");
        }
    }

    private async Task ValidatePostsAsync(ValidationReport report)
    {
        var section = "Posts";

        try
        {
            var posts = await _context.Posts.Include(p => p.Category).ToListAsync();
            report.EntityCounts[section] = posts.Count;

            // Validate post data integrity
            foreach (var post in posts)
            {
                if (string.IsNullOrEmpty(post.Title))
                {
                    report.Errors.Add($"Post {post.Id} has empty title");
                }

                if (string.IsNullOrEmpty(post.Slug))
                {
                    report.Errors.Add($"Post {post.Title} has empty slug");
                }

                if (post.Slug?.Contains(" ") == true)
                {
                    report.Warnings.Add($"Post {post.Title} slug contains spaces");
                }

                if (string.IsNullOrEmpty(post.Content))
                {
                    report.Warnings.Add($"Post {post.Title} has empty content");
                }

                if (post.CategoryId.HasValue && post.Category == null)
                {
                    report.Errors.Add($"Post {post.Title} references non-existent category");
                }
            }

            // Check for duplicate slugs
            var duplicateSlugs = posts
                .GroupBy(p => p.Slug?.ToLowerInvariant())
                .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                .Select(g => g.Key);

            foreach (var duplicate in duplicateSlugs)
            {
                report.Errors.Add($"Duplicate post slug found: {duplicate}");
            }

            _logger.LogDebug("Post validation completed. Found {Count} posts", posts.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating posts: {ex.Message}");
            _logger.LogError(ex, "Error during post validation");
        }
    }

    private async Task ValidateSystemConfigurationsAsync(ValidationReport report)
    {
        var section = "SystemConfigurations";

        try
        {
            var configs = await _context.SystemConfigurations.ToListAsync();
            report.EntityCounts[section] = configs.Count;

            if (configs.Count == 0)
            {
                report.Warnings.Add("No system configurations found in database");
                return;
            }

            // Check for essential configurations
            var essentialConfigs = new[]
            {
                "Site.Name", "Site.Description", "Environment.Mode"
            };

            foreach (var essentialConfig in essentialConfigs)
            {
                if (!configs.Any(c => c.Key.Equals(essentialConfig, StringComparison.OrdinalIgnoreCase)))
                {
                    report.Warnings.Add($"Recommended configuration missing: {essentialConfig}");
                }
            }

            // Validate configuration data integrity
            foreach (var config in configs)
            {
                if (string.IsNullOrEmpty(config.Key))
                {
                    report.Errors.Add($"Configuration {config.Id} has empty key");
                }

                if (string.IsNullOrEmpty(config.Value))
                {
                    report.Warnings.Add($"Configuration {config.Key} has empty value");
                }
            }

            // Check for duplicate keys
            var duplicateKeys = configs
                .GroupBy(c => c.Key?.ToLowerInvariant())
                .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                .Select(g => g.Key);

            foreach (var duplicate in duplicateKeys)
            {
                report.Errors.Add($"Duplicate configuration key found: {duplicate}");
            }

            _logger.LogDebug("System configuration validation completed. Found {Count} configurations", configs.Count);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating system configurations: {ex.Message}");
            _logger.LogError(ex, "Error during system configuration validation");
        }
    }

    private async Task ValidateRelationshipsAsync(ValidationReport report)
    {
        try
        {
            // Validate user-role relationships
            var userRoles = await _context.UserRoles.Include(ur => ur.User).Include(ur => ur.Role).ToListAsync();
            foreach (var userRole in userRoles)
            {
                if (userRole.User == null)
                {
                    report.Errors.Add($"UserRole (UserId: {userRole.UserId}, RoleId: {userRole.RoleId}) references non-existent user");
                }

                if (userRole.Role == null)
                {
                    report.Errors.Add($"UserRole (UserId: {userRole.UserId}, RoleId: {userRole.RoleId}) references non-existent role");
                }
            }

            // Validate role-permission relationships
            var rolePermissions = await _context.RolePermissions.Include(rp => rp.Role).Include(rp => rp.Permission).ToListAsync();
            foreach (var rolePermission in rolePermissions)
            {
                if (rolePermission.Role == null)
                {
                    report.Errors.Add($"RolePermission (RoleId: {rolePermission.RoleId}, PermissionId: {rolePermission.PermissionId}) references non-existent role");
                }

                if (rolePermission.Permission == null)
                {
                    report.Errors.Add($"RolePermission (RoleId: {rolePermission.RoleId}, PermissionId: {rolePermission.PermissionId}) references non-existent permission");
                }
            }

            _logger.LogDebug("Relationship validation completed");
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating relationships: {ex.Message}");
            _logger.LogError(ex, "Error during relationship validation");
        }
    }

    private async Task ValidateSecurityAsync(ValidationReport report, string environment)
    {
        try
        {
            // Check for insecure passwords in production
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                var users = await _context.Users.ToListAsync();

                foreach (var user in users)
                {
                    // Check for test emails in production
                    if (user.Email.Value.Contains("example.com") ||
                        user.Email.Value.Contains("test.com") ||
                        user.Email.Value.Contains("localhost"))
                    {
                        report.Errors.Add($"Test email found in production: {user.Email.Value}");
                    }

                    // Check for test usernames in production
                    if (user.UserName.ToLower().Contains("test") ||
                        user.UserName.ToLower().Contains("demo") ||
                        user.UserName.ToLower().Contains("sample"))
                    {
                        report.Errors.Add($"Test username found in production: {user.UserName}");
                    }
                }
            }

            // Validate admin user exists and has proper role
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN");
            if (adminRole != null)
            {
                var adminUserExists = await _context.UserRoles
                    .AnyAsync(ur => ur.RoleId == adminRole.Id);

                if (!adminUserExists)
                {
                    report.Errors.Add("No user assigned to admin role");
                }
            }

            _logger.LogDebug("Security validation completed for environment: {Environment}", environment);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating security: {ex.Message}");
            _logger.LogError(ex, "Error during security validation");
        }
    }

    private async Task ValidateDataConsistencyAsync(ValidationReport report)
    {
        try
        {
            // Validate category post counts
            var categories = await _context.Categories.ToListAsync();
            foreach (var category in categories)
            {
                var actualPostCount = await _context.Posts.CountAsync(p => p.CategoryId == category.Id);
                if (category.PostCount != actualPostCount)
                {
                    report.Warnings.Add($"Category {category.Name} post count mismatch. Stored: {category.PostCount}, Actual: {actualPostCount}");
                }
            }

            // Validate tag usage counts
            var tags = await _context.Tags.ToListAsync();
            foreach (var tag in tags)
            {
                var actualUsageCount = await _context.PostTags.CountAsync(pt => pt.TagId == tag.Id);
                if (tag.UsageCount != actualUsageCount)
                {
                    report.Warnings.Add($"Tag {tag.Name} usage count mismatch. Stored: {tag.UsageCount}, Actual: {actualUsageCount}");
                }
            }

            _logger.LogDebug("Data consistency validation completed");
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error validating data consistency: {ex.Message}");
            _logger.LogError(ex, "Error during data consistency validation");
        }
    }

    private async Task ValidateEnvironmentSpecificAsync(ValidationReport report, string environment)
    {
        try
        {
            switch (environment.ToLowerInvariant())
            {
                case "production":
                    await ValidateProductionSpecificAsync(report);
                    break;
                case "staging":
                    await ValidateStagingSpecificAsync(report);
                    break;
                case "development":
                    await ValidateDevelopmentSpecificAsync(report);
                    break;
            }

            _logger.LogDebug("Environment-specific validation completed for: {Environment}", environment);
        }
        catch (Exception ex)
        {
            report.Errors.Add($"Error in environment-specific validation: {ex.Message}");
            _logger.LogError(ex, "Error during environment-specific validation");
        }
    }

    private async Task ValidateProductionSpecificAsync(ValidationReport report)
    {
        // Ensure no test data exists
        var testDataCount = await _context.Users
            .CountAsync(u => u.Email.Value.Contains("example.com") ||
                            u.Email.Value.Contains("test.com") ||
                            u.UserName.Contains("test"));

        if (testDataCount > 0)
        {
            report.Errors.Add($"Found {testDataCount} test users in production environment");
        }

        // Check for proper SSL configuration
        var httpsConfig = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == "Security.RequireHttps");

        if (httpsConfig?.Value?.ToLower() != "true")
        {
            report.Warnings.Add("HTTPS not enforced in production environment");
        }
    }

    private async Task ValidateStagingSpecificAsync(ValidationReport report)
    {
        // Ensure staging is properly identified
        var envConfig = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == "Environment.Mode");

        if (envConfig?.Value != "Staging")
        {
            report.Warnings.Add("Environment mode not set to Staging");
        }

        // Check for test data presence (expected in staging)
        var testUsers = await _context.Users
            .CountAsync(u => u.Email.Value.Contains("staging") || u.Email.Value.Contains("test"));

        if (testUsers == 0)
        {
            report.Warnings.Add("No test users found in staging environment");
        }
    }

    private async Task ValidateDevelopmentSpecificAsync(ValidationReport report)
    {
        // Check for development configurations
        var debugConfig = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == "Debug.Enabled");

        if (debugConfig?.Value?.ToLower() != "true")
        {
            report.Warnings.Add("Debug mode not enabled in development environment");
        }

        // Ensure sample data exists
        var samplePosts = await _context.Posts.CountAsync();
        if (samplePosts == 0)
        {
            report.Warnings.Add("No sample posts found in development environment");
        }
    }
}

/// <summary>
/// Comprehensive validation report
/// </summary>
public class ValidationReport
{
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Information { get; set; } = new();
    public Dictionary<string, int> EntityCounts { get; set; } = new();

    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    public bool HasIssues => Errors.Any() || Warnings.Any();

    /// <summary>
    /// Gets a summary of the validation report
    /// </summary>
    public string GetSummary()
    {
        if (!IsValid)
        {
            return $"Validation failed for {Environment}: {Errors.Count} errors, {Warnings.Count} warnings";
        }

        return $"Validation passed for {Environment} in {Duration:mm\\:ss}. {Warnings.Count} warnings found.";
    }

    /// <summary>
    /// Gets detailed validation statistics
    /// </summary>
    public object GetStatistics()
    {
        return new
        {
            Environment,
            Duration = Duration.ToString(@"mm\:ss"),
            IsValid,
            ErrorCount = Errors.Count,
            WarningCount = Warnings.Count,
            InformationCount = Information.Count,
            EntityCounts,
            Summary = GetSummary()
        };
    }
}