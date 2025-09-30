using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Infrastructure.Data.Seeders.Core;

public record RolePermissionAssignment(string RoleName, string PermissionName, Guid? GrantedBy = null);

public record UserRoleAssignment(string UserName, string RoleName, Guid? AssignedBy = null);


/// <summary>
/// Base implementation for seed data providers with common functionality
/// </summary>
public abstract class BaseSeedDataProvider : ISeedDataProvider
{
    protected readonly ILogger Logger;
    protected readonly string EnvironmentName;

    protected BaseSeedDataProvider(ILogger logger, string environmentName)
    {
        Logger = logger;
        EnvironmentName = environmentName;
    }

    /// <summary>
    /// Gets the environment this provider supports
    /// </summary>
    public virtual string Environment => EnvironmentName;

    /// <summary>
    /// Gets the priority of this provider
    /// </summary>
    public virtual int Priority => 100;

    /// <summary>
    /// Checks if this provider can handle the specified environment
    /// </summary>
    public virtual bool CanProvideFor(string environment)
    {
        return string.Equals(Environment, environment, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the seed data configuration for this environment
    /// </summary>
    public abstract Task<SeedDataConfiguration> GetSeedDataConfigurationAsync();

    /// <summary>
    /// Validates that the environment is suitable for seeding
    /// </summary>
    public virtual async Task<SeedValidationResult> ValidateEnvironmentAsync(BlogDbContext context)
    {
        var config = await GetSeedDataConfigurationAsync();
        var result = new SeedValidationResult { IsValid = true };

        // Check if seeding is allowed
        if (!config.AllowSeeding)
        {
            result.IsValid = false;
            result.Errors.Add($"Seeding is not allowed in {Environment} environment");
            return result;
        }

        // Check existing data limits
        var existingUsers = await context.Users.CountAsync();
        var existingPosts = await context.Posts.CountAsync();
        var totalRecords = existingUsers + existingPosts;

        if (totalRecords > config.MaxExistingRecords)
        {
            result.IsValid = false;
            result.Errors.Add($"Environment has {totalRecords} existing records, exceeding limit of {config.MaxExistingRecords}");
            return result;
        }

        // Add warnings if data exists
        if (totalRecords > 0)
        {
            result.Warnings.Add($"Environment contains {totalRecords} existing records");
        }

        // Environment-specific validations
        await PerformCustomValidationAsync(context, result);

        return result;
    }

    /// <summary>
    /// Performs custom validation specific to the provider
    /// </summary>
    protected virtual Task PerformCustomValidationAsync(BlogDbContext context, SeedValidationResult result)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the roles to be seeded
    /// </summary>
    public virtual async Task<IEnumerable<Role>> GetRolesAsync()
    {
        return await GetDefaultRolesAsync();
    }

    /// <summary>
    /// Gets the permissions to be seeded
    /// </summary>
    public virtual async Task<IEnumerable<Permission>> GetPermissionsAsync()
    {
        return await GetDefaultPermissionsAsync();
    }

    /// <summary>
    /// Gets role-permission assignments to seed
    /// </summary>
    public virtual Task<IEnumerable<RolePermissionAssignment>> GetRolePermissionAssignmentsAsync()
    {
        return Task.FromResult<IEnumerable<RolePermissionAssignment>>(Array.Empty<RolePermissionAssignment>());
    }

    /// <summary>
    /// Gets user-role assignments to seed
    /// </summary>
    public virtual Task<IEnumerable<UserRoleAssignment>> GetUserRoleAssignmentsAsync()
    {
        return Task.FromResult<IEnumerable<UserRoleAssignment>>(Array.Empty<UserRoleAssignment>());
    }

    /// <summary>
    /// Gets the users to be seeded
    /// </summary>
    public abstract Task<IEnumerable<User>> GetUsersAsync();

    /// <summary>
    /// Gets the categories to be seeded
    /// </summary>
    public abstract Task<IEnumerable<Category>> GetCategoriesAsync();

    /// <summary>
    /// Gets the tags to be seeded
    /// </summary>
    public abstract Task<IEnumerable<Tag>> GetTagsAsync();

    /// <summary>
    /// Gets the posts to be seeded
    /// </summary>
    public abstract Task<IEnumerable<Post>> GetPostsAsync();

    /// <summary>
    /// Gets the system configurations to be seeded
    /// </summary>
    public virtual async Task<IEnumerable<SystemConfiguration>> GetSystemConfigurationsAsync()
    {
        return await GetDefaultSystemConfigurationsAsync();
    }

    /// <summary>
    /// Gets any additional custom seed data
    /// </summary>
    public virtual Task<object[]> GetCustomSeedDataAsync()
    {
        return Task.FromResult(Array.Empty<object>());
    }

    #region Protected Helper Methods

    /// <summary>
    /// Gets default system roles
    /// </summary>
    protected virtual Task<IEnumerable<Role>> GetDefaultRolesAsync()
    {
        var roles = new List<Role>
        {
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "SuperAdmin",
                NormalizedName = "SUPERADMIN",
                Description = "Super administrator with full system access",
                IsSystemRole = true,
                IsActive = true
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "Administrator with system management privileges",
                IsSystemRole = true,
                IsActive = true
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Author",
                NormalizedName = "AUTHOR",
                Description = "Content author with publishing privileges",
                IsSystemRole = true,
                IsActive = true
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user with basic privileges",
                IsSystemRole = true,
                IsActive = true
            }
        };

        return Task.FromResult(roles.AsEnumerable());
    }

    /// <summary>
    /// Gets default system permissions
    /// </summary>
    protected virtual Task<IEnumerable<Permission>> GetDefaultPermissionsAsync()
    {
        var permissions = new List<Permission>
        {
            // System Management
            new Permission { Id = Guid.NewGuid(), Name = "System.FullAccess", Description = "Full system access", Category = "System", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "System.ViewLogs", Description = "View system logs", Category = "System", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "System.ManageConfig", Description = "Manage system configuration", Category = "System", IsSystemPermission = true },

            // User Management
            new Permission { Id = Guid.NewGuid(), Name = "Users.Create", Description = "Create users", Category = "Users", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Users.Read", Description = "View users", Category = "Users", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Users.Update", Description = "Update users", Category = "Users", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Users.Delete", Description = "Delete users", Category = "Users", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Users.ManageRoles", Description = "Manage user roles", Category = "Users", IsSystemPermission = true },

            // Role Management
            new Permission { Id = Guid.NewGuid(), Name = "Roles.Create", Description = "Create roles", Category = "Roles", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Roles.Read", Description = "View roles", Category = "Roles", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Roles.Update", Description = "Update roles", Category = "Roles", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Roles.Delete", Description = "Delete roles", Category = "Roles", IsSystemPermission = true },

            // Content Management
            new Permission { Id = Guid.NewGuid(), Name = "Posts.Create", Description = "Create posts", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Posts.Read", Description = "View posts", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Posts.Update", Description = "Update posts", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Posts.Delete", Description = "Delete posts", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Posts.Publish", Description = "Publish posts", Category = "Content", IsSystemPermission = true },

            // Category Management
            new Permission { Id = Guid.NewGuid(), Name = "Categories.Create", Description = "Create categories", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Categories.Read", Description = "View categories", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Categories.Update", Description = "Update categories", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Categories.Delete", Description = "Delete categories", Category = "Content", IsSystemPermission = true },

            // Tag Management
            new Permission { Id = Guid.NewGuid(), Name = "Tags.Create", Description = "Create tags", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Tags.Read", Description = "View tags", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Tags.Update", Description = "Update tags", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Tags.Delete", Description = "Delete tags", Category = "Content", IsSystemPermission = true },

            // Comment Management
            new Permission { Id = Guid.NewGuid(), Name = "Comments.Create", Description = "Create comments", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Comments.Read", Description = "View comments", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Comments.Update", Description = "Update comments", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Comments.Delete", Description = "Delete comments", Category = "Content", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Comments.Moderate", Description = "Moderate comments", Category = "Content", IsSystemPermission = true },

            // File Management
            new Permission { Id = Guid.NewGuid(), Name = "Files.Upload", Description = "Upload files", Category = "Files", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Files.Download", Description = "Download files", Category = "Files", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Files.Delete", Description = "Delete files", Category = "Files", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid(), Name = "Files.Manage", Description = "Manage file system", Category = "Files", IsSystemPermission = true }
        };

        var normalized = permissions
            .Select(permission =>
            {
                if (string.IsNullOrWhiteSpace(permission.Resource) || string.IsNullOrWhiteSpace(permission.Action))
                {
                    var parts = permission.Name.Split('.', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        permission.Resource = parts[0];
                        permission.Action = parts[1];
                    }
                    else
                    {
                        permission.Resource = permission.Name;
                        permission.Action = "Execute";
                    }
                }

                permission.Name = $"{permission.Resource}.{permission.Action}";
                return permission;
            })
            .GroupBy(p => new { p.Resource, p.Action, p.Scope })
            .Select(g => g.First())
            .ToList();

        return Task.FromResult(normalized.AsEnumerable());
    }

    /// <summary>
    /// Gets default system configurations
    /// </summary>
    protected virtual Task<IEnumerable<SystemConfiguration>> GetDefaultSystemConfigurationsAsync()
    {
        var configs = new List<SystemConfiguration>
        {
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Site.Name",
                Value = "Maple Blog",
                Description = "Website name",
                Category = "General",
                IsPublic = true,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Site.Description",
                Value = "A modern blog platform built with ASP.NET Core and React",
                Description = "Website description",
                Category = "General",
                IsPublic = true,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Comments.RequireApproval",
                Value = "true",
                Description = "Whether comments require approval",
                Category = "Content",
                IsPublic = false,
                DataType = "boolean"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Posts.DefaultStatus",
                Value = "Draft",
                Description = "Default status for new posts",
                Category = "Content",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Security.MaxLoginAttempts",
                Value = "5",
                Description = "Maximum login attempts before lockout",
                Category = "Security",
                IsPublic = false,
                DataType = "integer"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Security.AccountLockoutMinutes",
                Value = "30",
                Description = "Account lockout duration in minutes",
                Category = "Security",
                IsPublic = false,
                DataType = "integer"
            }
        };

        return Task.FromResult(configs.AsEnumerable());
    }

    /// <summary>
    /// Creates a secure password hash
    /// </summary>
    protected string CreatePasswordHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    /// <summary>
    /// Creates a system user with secure defaults
    /// </summary>
    protected User CreateSystemUser(string userName, string email, string displayName, string? bio = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = Email.Create(email),
            DisplayName = displayName,
            Bio = bio ?? $"System user: {displayName}",
            EmailConfirmed = true,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a basic category
    /// </summary>
    protected Category CreateCategory(string name, string description, string color = "#6B7280")
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = CreateSlug(name),
            Description = description,
            Color = color,
            IsActive = true,
            PostCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a basic tag
    /// </summary>
    protected Tag CreateTag(string name, string description, string color = "#6B7280")
    {
        return new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = CreateSlug(name),
            Description = description,
            Color = color,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a URL-friendly slug from a string
    /// </summary>
    protected string CreateSlug(string input)
    {
        return input.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Trim('-');
    }

    #endregion
}
