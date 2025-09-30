using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Seeders.Core;

/// <summary>
/// Interface for seed data providers that can provide environment-specific seed data
/// </summary>
public interface ISeedDataProvider
{
    /// <summary>
    /// Gets the environment this provider supports
    /// </summary>
    string Environment { get; }

    /// <summary>
    /// Gets the priority of this provider (higher priority wins in case of conflicts)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Checks if this provider can handle the specified environment
    /// </summary>
    bool CanProvideFor(string environment);

    /// <summary>
    /// Gets the seed data configuration for this environment
    /// </summary>
    Task<SeedDataConfiguration> GetSeedDataConfigurationAsync();

    /// <summary>
    /// Validates that the environment is suitable for seeding
    /// </summary>
    Task<SeedValidationResult> ValidateEnvironmentAsync(BlogDbContext context);

    /// <summary>
    /// Gets the roles to be seeded
    /// </summary>
    Task<IEnumerable<Role>> GetRolesAsync();

    /// <summary>
    /// Gets the permissions to be seeded
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsAsync();

    /// <summary>
    /// Gets default role-permission assignments
    /// </summary>
    Task<IEnumerable<RolePermissionAssignment>> GetRolePermissionAssignmentsAsync();

    /// <summary>
    /// Gets default user-role assignments
    /// </summary>
    Task<IEnumerable<UserRoleAssignment>> GetUserRoleAssignmentsAsync();

    /// <summary>
    /// Gets the users to be seeded
    /// </summary>
    Task<IEnumerable<User>> GetUsersAsync();

    /// <summary>
    /// Gets the categories to be seeded
    /// </summary>
    Task<IEnumerable<Category>> GetCategoriesAsync();

    /// <summary>
    /// Gets the tags to be seeded
    /// </summary>
    Task<IEnumerable<Tag>> GetTagsAsync();

    /// <summary>
    /// Gets the posts to be seeded
    /// </summary>
    Task<IEnumerable<Post>> GetPostsAsync();

    /// <summary>
    /// Gets the system configurations to be seeded
    /// </summary>
    Task<IEnumerable<SystemConfiguration>> GetSystemConfigurationsAsync();

    /// <summary>
    /// Gets any additional custom seed data
    /// </summary>
    Task<object[]> GetCustomSeedDataAsync();
}

/// <summary>
/// Seed data configuration
/// </summary>
public class SeedDataConfiguration
{
    /// <summary>
    /// Whether to allow seeding in this environment
    /// </summary>
    public bool AllowSeeding { get; set; } = true;

    /// <summary>
    /// Whether to require confirmation before seeding
    /// </summary>
    public bool RequireConfirmation { get; set; } = true;

    /// <summary>
    /// Whether to backup existing data before seeding
    /// </summary>
    public bool BackupBeforeSeeding { get; set; } = true;

    /// <summary>
    /// Whether to clear existing data before seeding
    /// </summary>
    public bool ClearExistingData { get; set; } = false;

    /// <summary>
    /// Maximum number of existing records allowed before preventing seeding
    /// </summary>
    public int MaxExistingRecords { get; set; } = 0;

    /// <summary>
    /// Whether to create audit logs for seeding operations
    /// </summary>
    public bool CreateAuditLogs { get; set; } = true;

    /// <summary>
    /// Whether to validate data integrity after seeding
    /// </summary>
    public bool ValidateAfterSeeding { get; set; } = true;

    /// <summary>
    /// Custom validation rules
    /// </summary>
    public List<string> ValidationRules { get; set; } = new();

    /// <summary>
    /// Custom environment-specific settings
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Result of seed validation
/// </summary>
public class SeedValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation messages
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Warnings that don't prevent seeding
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Errors that prevent seeding
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Additional context information
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static SeedValidationResult Success(string? message = null)
    {
        var result = new SeedValidationResult { IsValid = true };
        if (!string.IsNullOrEmpty(message))
            result.Messages.Add(message);
        return result;
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static SeedValidationResult Failure(string error)
    {
        return new SeedValidationResult
        {
            IsValid = false,
            Errors = { error }
        };
    }

    /// <summary>
    /// Creates a warning validation result
    /// </summary>
    public static SeedValidationResult Warning(string warning)
    {
        return new SeedValidationResult
        {
            IsValid = true,
            Warnings = { warning }
        };
    }
}