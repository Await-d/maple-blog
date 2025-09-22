using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Infrastructure.Data.Seeders.Core;

/// <summary>
/// Manages seed data operations with environment awareness, validation, and auditing
/// </summary>
public class SeedDataManager
{
    private readonly BlogDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeedDataManager> _logger;
    private readonly List<ISeedDataProvider> _providers;

    public SeedDataManager(
        BlogDbContext context,
        IServiceProvider serviceProvider,
        ILogger<SeedDataManager> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _providers = new List<ISeedDataProvider>();
    }

    /// <summary>
    /// Registers a seed data provider
    /// </summary>
    public void RegisterProvider(ISeedDataProvider provider)
    {
        _providers.Add(provider);
        _logger.LogDebug("Registered seed data provider: {Provider} for environment: {Environment}",
            provider.GetType().Name, provider.Environment);
    }

    /// <summary>
    /// Seeds data for the specified environment
    /// </summary>
    public async Task<SeedResult> SeedAsync(string environment, bool forceSeeding = false, CancellationToken cancellationToken = default)
    {
        var result = new SeedResult { Environment = environment, StartTime = DateTime.UtcNow };

        try
        {
            _logger.LogInformation("Starting seed data process for environment: {Environment}", environment);

            // Find appropriate provider
            var provider = GetProviderForEnvironment(environment);
            if (provider == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"No seed data provider found for environment: {environment}";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            _logger.LogInformation("Using seed data provider: {Provider}", provider.GetType().Name);

            // Get configuration
            var config = await provider.GetSeedDataConfigurationAsync();
            result.Configuration = config;

            // Validate environment
            var validation = await provider.ValidateEnvironmentAsync(_context);
            result.ValidationResult = validation;

            if (!validation.IsValid && !forceSeeding)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Environment validation failed";
                _logger.LogError("Environment validation failed: {Errors}", string.Join(", ", validation.Errors));
                return result;
            }

            // Log warnings
            foreach (var warning in validation.Warnings)
            {
                _logger.LogWarning("Validation warning: {Warning}", warning);
            }

            // Check if confirmation is required
            if (config.RequireConfirmation && !forceSeeding)
            {
                result.RequiresConfirmation = true;
                result.IsSuccess = false;
                result.ErrorMessage = "Confirmation required for seeding operation";
                _logger.LogWarning("Seeding requires confirmation. Use forceSeeding parameter to proceed.");
                return result;
            }

            // Create backup if required
            if (config.BackupBeforeSeeding)
            {
                await CreateBackupAsync(result);
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Seed data
                await SeedDataAsync(provider, result, cancellationToken);

                // Validate after seeding
                if (config.ValidateAfterSeeding)
                {
                    await ValidateSeededDataAsync(result);
                }

                // Create audit logs
                if (config.CreateAuditLogs)
                {
                    await CreateAuditLogsAsync(result);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Seed data transaction committed successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error during seeding, transaction rolled back");
                throw;
            }

            result.IsSuccess = true;
            result.EndTime = DateTime.UtcNow;
            _logger.LogInformation("Seed data process completed successfully in {Duration}",
                result.EndTime - result.StartTime);

            return result;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "Seed data process failed");
            return result;
        }
    }

    /// <summary>
    /// Gets available seed data providers
    /// </summary>
    public IEnumerable<ISeedDataProvider> GetAvailableProviders()
    {
        return _providers.OrderByDescending(p => p.Priority);
    }

    /// <summary>
    /// Gets seed data status for environment
    /// </summary>
    public async Task<SeedStatus> GetSeedStatusAsync(string environment)
    {
        var status = new SeedStatus
        {
            Environment = environment,
            CheckTime = DateTime.UtcNow
        };

        try
        {
            // Check database connectivity
            status.CanConnectToDatabase = await _context.Database.CanConnectAsync();

            if (!status.CanConnectToDatabase)
            {
                status.Issues.Add("Cannot connect to database");
                return status;
            }

            // Check existing data
            status.ExistingUsers = await _context.Users.CountAsync();
            status.ExistingPosts = await _context.Posts.CountAsync();
            status.ExistingCategories = await _context.Categories.CountAsync();
            status.ExistingTags = await _context.Tags.CountAsync();
            status.ExistingRoles = await _context.Roles.CountAsync();

            // Check for test data
            var testUsers = await _context.Users
                .Where(u => u.Email.Value.Contains("example.com") ||
                           u.Email.Value.Contains("test.com") ||
                           u.UserName.Contains("test"))
                .CountAsync();

            if (testUsers > 0)
            {
                status.HasTestData = true;
                status.Issues.Add($"Found {testUsers} test users");
            }

            // Check for required data
            var hasAdminRole = await _context.Roles.AnyAsync(r => r.NormalizedName == "ADMIN");
            if (!hasAdminRole)
            {
                status.Issues.Add("Missing admin role");
            }

            var hasAdminUser = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId,
                     (u, ur) => new { User = u, UserRole = ur })
                .Join(_context.Roles, x => x.UserRole.RoleId, r => r.Id,
                     (x, r) => new { x.User, Role = r })
                .AnyAsync(x => x.Role.NormalizedName == "ADMIN");

            if (!hasAdminUser)
            {
                status.Issues.Add("No admin user found");
            }

            status.IsHealthy = status.Issues.Count == 0;

        }
        catch (Exception ex)
        {
            status.Issues.Add($"Error checking status: {ex.Message}");
            _logger.LogError(ex, "Error checking seed status for environment: {Environment}", environment);
        }

        return status;
    }

    /// <summary>
    /// Cleans test data from the database
    /// </summary>
    public async Task<CleanupResult> CleanTestDataAsync(bool dryRun = true)
    {
        var result = new CleanupResult { StartTime = DateTime.UtcNow, IsDryRun = dryRun };

        try
        {
            // Find test users
            var testUsers = await _context.Users
                .Where(u => u.Email.Value.Contains("example.com") ||
                           u.Email.Value.Contains("test.com") ||
                           u.UserName.Contains("test") ||
                           u.UserName.Contains("demo"))
                .ToListAsync();

            result.TestUsersFound = testUsers.Count;

            // Find test posts
            var testPosts = await _context.Posts
                .Where(p => p.Title.Contains("test") ||
                           p.Title.Contains("demo") ||
                           p.Title.Contains("sample"))
                .ToListAsync();

            result.TestPostsFound = testPosts.Count;

            if (!dryRun)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Remove test posts
                    if (testPosts.Any())
                    {
                        _context.Posts.RemoveRange(testPosts);
                        result.TestPostsRemoved = testPosts.Count;
                    }

                    // Remove test users
                    if (testUsers.Any())
                    {
                        _context.Users.RemoveRange(testUsers);
                        result.TestUsersRemoved = testUsers.Count;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Cleaned {UserCount} test users and {PostCount} test posts",
                        result.TestUsersRemoved, result.TestPostsRemoved);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            result.IsSuccess = true;
            result.EndTime = DateTime.UtcNow;

        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.UtcNow;
            _logger.LogError(ex, "Error cleaning test data");
        }

        return result;
    }

    #region Private Methods

    private ISeedDataProvider? GetProviderForEnvironment(string environment)
    {
        return _providers
            .Where(p => p.CanProvideFor(environment))
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();
    }

    private async Task SeedDataAsync(ISeedDataProvider provider, SeedResult result, CancellationToken cancellationToken)
    {
        // Seed roles
        var roles = await provider.GetRolesAsync();
        if (roles.Any())
        {
            await SeedRolesAsync(roles, result);
        }

        // Seed permissions
        var permissions = await provider.GetPermissionsAsync();
        if (permissions.Any())
        {
            await SeedPermissionsAsync(permissions, result);
        }

        // Seed users
        var users = await provider.GetUsersAsync();
        if (users.Any())
        {
            await SeedUsersAsync(users, result);
        }

        // Seed categories
        var categories = await provider.GetCategoriesAsync();
        if (categories.Any())
        {
            await SeedCategoriesAsync(categories, result);
        }

        // Seed tags
        var tags = await provider.GetTagsAsync();
        if (tags.Any())
        {
            await SeedTagsAsync(tags, result);
        }

        // Seed posts
        var posts = await provider.GetPostsAsync();
        if (posts.Any())
        {
            await SeedPostsAsync(posts, result);
        }

        // Seed system configurations
        var configs = await provider.GetSystemConfigurationsAsync();
        if (configs.Any())
        {
            await SeedSystemConfigurationsAsync(configs, result);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(IEnumerable<Role> roles, SeedResult result)
    {
        foreach (var role in roles)
        {
            var existing = await _context.Roles
                .FirstOrDefaultAsync(r => r.NormalizedName == role.NormalizedName);

            if (existing == null)
            {
                await _context.Roles.AddAsync(role);
                result.RolesCreated++;
                _logger.LogDebug("Created role: {RoleName}", role.Name);
            }
            else
            {
                result.RolesSkipped++;
                _logger.LogDebug("Skipped existing role: {RoleName}", role.Name);
            }
        }
    }

    private async Task SeedPermissionsAsync(IEnumerable<Permission> permissions, SeedResult result)
    {
        foreach (var permission in permissions)
        {
            var existing = await _context.Permissions
                .FirstOrDefaultAsync(p => p.Name == permission.Name);

            if (existing == null)
            {
                await _context.Permissions.AddAsync(permission);
                result.PermissionsCreated++;
                _logger.LogDebug("Created permission: {PermissionName}", permission.Name);
            }
            else
            {
                result.PermissionsSkipped++;
                _logger.LogDebug("Skipped existing permission: {PermissionName}", permission.Name);
            }
        }
    }

    private async Task SeedUsersAsync(IEnumerable<User> users, SeedResult result)
    {
        foreach (var user in users)
        {
            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == user.UserName || u.Email.Value == user.Email.Value);

            if (existing == null)
            {
                await _context.Users.AddAsync(user);
                result.UsersCreated++;
                _logger.LogDebug("Created user: {UserName}", user.UserName);
            }
            else
            {
                result.UsersSkipped++;
                _logger.LogDebug("Skipped existing user: {UserName}", user.UserName);
            }
        }
    }

    private async Task SeedCategoriesAsync(IEnumerable<Category> categories, SeedResult result)
    {
        foreach (var category in categories)
        {
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug == category.Slug);

            if (existing == null)
            {
                await _context.Categories.AddAsync(category);
                result.CategoriesCreated++;
                _logger.LogDebug("Created category: {CategoryName}", category.Name);
            }
            else
            {
                result.CategoriesSkipped++;
                _logger.LogDebug("Skipped existing category: {CategoryName}", category.Name);
            }
        }
    }

    private async Task SeedTagsAsync(IEnumerable<Tag> tags, SeedResult result)
    {
        foreach (var tag in tags)
        {
            var existing = await _context.Tags
                .FirstOrDefaultAsync(t => t.Slug == tag.Slug);

            if (existing == null)
            {
                await _context.Tags.AddAsync(tag);
                result.TagsCreated++;
                _logger.LogDebug("Created tag: {TagName}", tag.Name);
            }
            else
            {
                result.TagsSkipped++;
                _logger.LogDebug("Skipped existing tag: {TagName}", tag.Name);
            }
        }
    }

    private async Task SeedPostsAsync(IEnumerable<Post> posts, SeedResult result)
    {
        foreach (var post in posts)
        {
            var existing = await _context.Posts
                .FirstOrDefaultAsync(p => p.Slug == post.Slug);

            if (existing == null)
            {
                await _context.Posts.AddAsync(post);
                result.PostsCreated++;
                _logger.LogDebug("Created post: {PostTitle}", post.Title);
            }
            else
            {
                result.PostsSkipped++;
                _logger.LogDebug("Skipped existing post: {PostTitle}", post.Title);
            }
        }
    }

    private async Task SeedSystemConfigurationsAsync(IEnumerable<SystemConfiguration> configs, SeedResult result)
    {
        foreach (var config in configs)
        {
            var existing = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == config.Key);

            if (existing == null)
            {
                await _context.SystemConfigurations.AddAsync(config);
                result.ConfigurationsCreated++;
                _logger.LogDebug("Created configuration: {ConfigKey}", config.Key);
            }
            else
            {
                result.ConfigurationsSkipped++;
                _logger.LogDebug("Skipped existing configuration: {ConfigKey}", config.Key);
            }
        }
    }

    private async Task CreateBackupAsync(SeedResult result)
    {
        // Implementation would depend on specific backup requirements
        // This is a placeholder for backup functionality
        _logger.LogInformation("Backup functionality not implemented");
    }

    private async Task ValidateSeededDataAsync(SeedResult result)
    {
        // Validate that essential data exists
        var roleCount = await _context.Roles.CountAsync();
        var userCount = await _context.Users.CountAsync();
        var permissionCount = await _context.Permissions.CountAsync();

        if (roleCount == 0)
            result.ValidationErrors.Add("No roles found after seeding");

        if (userCount == 0)
            result.ValidationErrors.Add("No users found after seeding");

        if (permissionCount == 0)
            result.ValidationErrors.Add("No permissions found after seeding");

        _logger.LogDebug("Post-seed validation completed. Roles: {RoleCount}, Users: {UserCount}, Permissions: {PermissionCount}",
            roleCount, userCount, permissionCount);
    }

    private async Task CreateAuditLogsAsync(SeedResult result)
    {
        try
        {
            var auditService = _serviceProvider.GetService<IAuditLogService>();
            if (auditService != null)
            {
                var auditLog = new AuditLog
                {
                    ResourceType = "SeedData",
                    Action = "Completed",
                    Description = $"Seeded data for environment: {result.Environment}",
                    AdditionalData = System.Text.Json.JsonSerializer.Serialize(result)
                };
                await auditService.LogAsync(auditLog);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create audit log for seeding operation");
        }
    }

    #endregion
}