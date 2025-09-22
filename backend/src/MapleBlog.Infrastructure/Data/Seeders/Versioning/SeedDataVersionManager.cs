using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MapleBlog.Domain.Entities;
using MapleBlog.Infrastructure.Data.Seeders.Core;

namespace MapleBlog.Infrastructure.Data.Seeders.Versioning;

/// <summary>
/// Manages seed data versioning and migrations
/// </summary>
public class SeedDataVersionManager
{
    private readonly BlogDbContext _context;
    private readonly ILogger<SeedDataVersionManager> _logger;
    private const string SEED_VERSION_KEY = "SeedData.Version";
    private const string SEED_MIGRATION_PREFIX = "SeedData.Migration.";

    public SeedDataVersionManager(BlogDbContext context, ILogger<SeedDataVersionManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current seed data version
    /// </summary>
    public async Task<SeedDataVersion?> GetCurrentVersionAsync()
    {
        try
        {
            var versionConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == SEED_VERSION_KEY);

            if (versionConfig == null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<SeedDataVersion>(versionConfig.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current seed data version");
            return null;
        }
    }

    /// <summary>
    /// Sets the current seed data version
    /// </summary>
    public async Task SetCurrentVersionAsync(SeedDataVersion version)
    {
        try
        {
            var versionJson = JsonSerializer.Serialize(version, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var versionConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == SEED_VERSION_KEY);

            if (versionConfig == null)
            {
                versionConfig = new SystemConfiguration
                {
                    Id = Guid.NewGuid(),
                    Key = SEED_VERSION_KEY,
                    Value = versionJson,
                    Description = "Current seed data version",
                    Category = "SeedData",
                    IsPublic = false,
                    DataType = "json",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.SystemConfigurations.AddAsync(versionConfig);
            }
            else
            {
                versionConfig.Value = versionJson;
                versionConfig.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Seed data version updated to {Version}", version.Version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting seed data version");
            throw;
        }
    }

    /// <summary>
    /// Gets available migrations
    /// </summary>
    public async Task<IEnumerable<SeedDataMigration>> GetAvailableMigrationsAsync()
    {
        try
        {
            var migrationConfigs = await _context.SystemConfigurations
                .Where(c => c.Key.StartsWith(SEED_MIGRATION_PREFIX))
                .ToListAsync();

            var migrations = new List<SeedDataMigration>();

            foreach (var config in migrationConfigs)
            {
                try
                {
                    var migration = JsonSerializer.Deserialize<SeedDataMigration>(config.Value);
                    if (migration != null)
                    {
                        migrations.Add(migration);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize migration: {Key}", config.Key);
                }
            }

            return migrations.OrderBy(m => m.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available migrations");
            return Enumerable.Empty<SeedDataMigration>();
        }
    }

    /// <summary>
    /// Gets pending migrations
    /// </summary>
    public async Task<IEnumerable<SeedDataMigration>> GetPendingMigrationsAsync()
    {
        var currentVersion = await GetCurrentVersionAsync();
        var allMigrations = await GetAvailableMigrationsAsync();

        return allMigrations
            .Where(m => !m.IsApplied && (currentVersion == null || IsVersionNewer(m.ToVersion, currentVersion)))
            .OrderBy(m => m.CreatedAt);
    }

    /// <summary>
    /// Applies a migration
    /// </summary>
    public async Task<MigrationResult> ApplyMigrationAsync(string migrationId, string appliedBy = "System")
    {
        var result = new MigrationResult
        {
            MigrationId = migrationId,
            StartTime = DateTime.UtcNow,
            AppliedBy = appliedBy
        };

        try
        {
            var migrations = await GetAvailableMigrationsAsync();
            var migration = migrations.FirstOrDefault(m => m.Id == migrationId);

            if (migration == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Migration not found";
                return result;
            }

            if (migration.IsApplied)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Migration already applied";
                return result;
            }

            _logger.LogInformation("Applying seed data migration: {MigrationName}", migration.Name);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Execute migration operations
                await ExecuteMigrationOperationsAsync(migration, result);

                // Mark migration as applied
                migration.IsApplied = true;
                migration.AppliedAt = DateTime.UtcNow;
                migration.AppliedBy = appliedBy;

                await SaveMigrationAsync(migration);

                // Update current version
                await SetCurrentVersionAsync(migration.ToVersion);

                await transaction.CommitAsync();

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation("Migration {MigrationName} applied successfully in {Duration}",
                    migration.Name, result.Duration);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            result.EndTime = DateTime.UtcNow;

            _logger.LogError(ex, "Error applying migration: {MigrationId}", migrationId);
            return result;
        }
    }

    /// <summary>
    /// Creates a new migration
    /// </summary>
    public async Task<SeedDataMigration> CreateMigrationAsync(
        string name,
        string description,
        SeedDataVersion fromVersion,
        SeedDataVersion toVersion,
        List<string> operations)
    {
        var migration = new SeedDataMigration
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Operations = operations,
            CreatedAt = DateTime.UtcNow
        };

        await SaveMigrationAsync(migration);

        _logger.LogInformation("Created seed data migration: {MigrationName} ({FromVersion} → {ToVersion})",
            name, fromVersion.Version, toVersion.Version);

        return migration;
    }

    /// <summary>
    /// Initializes versioning system
    /// </summary>
    public async Task InitializeVersioningAsync(string environment)
    {
        try
        {
            var currentVersion = await GetCurrentVersionAsync();

            if (currentVersion == null)
            {
                // Create initial version
                var initialVersion = new SeedDataVersion
                {
                    Major = 1,
                    Minor = 0,
                    Patch = 0,
                    Environment = environment,
                    Description = "Initial seed data version",
                    CreatedAt = DateTime.UtcNow
                };

                await SetCurrentVersionAsync(initialVersion);

                _logger.LogInformation("Initialized seed data versioning with version {Version} for environment {Environment}",
                    initialVersion.Version, environment);
            }
            else
            {
                _logger.LogInformation("Seed data versioning already initialized with version {Version}",
                    currentVersion.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing seed data versioning");
            throw;
        }
    }

    /// <summary>
    /// Gets version history
    /// </summary>
    public async Task<IEnumerable<object>> GetVersionHistoryAsync()
    {
        try
        {
            var appliedMigrations = await GetAvailableMigrationsAsync();

            var history = appliedMigrations
                .Where(m => m.IsApplied)
                .OrderByDescending(m => m.AppliedAt)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Description,
                    FromVersion = m.FromVersion.Version,
                    ToVersion = m.ToVersion.Version,
                    m.AppliedAt,
                    m.AppliedBy,
                    Operations = m.Operations.Count
                });

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history");
            return Enumerable.Empty<object>();
        }
    }

    /// <summary>
    /// Validates migration chain
    /// </summary>
    public async Task<ValidationResult> ValidateMigrationChainAsync()
    {
        var result = new ValidationResult { IsValid = true };

        try
        {
            var currentVersion = await GetCurrentVersionAsync();
            var migrations = await GetAvailableMigrationsAsync();

            if (currentVersion == null)
            {
                result.Issues.Add("No current version found");
                return result;
            }

            // Check for broken migration chain
            var appliedMigrations = migrations.Where(m => m.IsApplied).OrderBy(m => m.AppliedAt);
            var pendingMigrations = migrations.Where(m => !m.IsApplied).OrderBy(m => m.CreatedAt);

            // Validate applied migrations form a continuous chain
            SeedDataVersion? expectedVersion = null;
            foreach (var migration in appliedMigrations)
            {
                if (expectedVersion != null && !AreVersionsEqual(migration.FromVersion, expectedVersion))
                {
                    result.IsValid = false;
                    result.Issues.Add($"Migration chain broken at {migration.Name}: expected from version {expectedVersion.Version}, got {migration.FromVersion.Version}");
                }
                expectedVersion = migration.ToVersion;
            }

            // Check if current version matches last applied migration
            if (expectedVersion != null && !AreVersionsEqual(currentVersion, expectedVersion))
            {
                result.IsValid = false;
                result.Issues.Add($"Current version {currentVersion.Version} doesn't match last applied migration version {expectedVersion.Version}");
            }

            // Validate pending migrations can be applied
            foreach (var migration in pendingMigrations)
            {
                if (expectedVersion != null && !AreVersionsEqual(migration.FromVersion, expectedVersion))
                {
                    result.Issues.Add($"Pending migration {migration.Name} has incorrect from version: expected {expectedVersion?.Version}, got {migration.FromVersion.Version}");
                }
                expectedVersion = migration.ToVersion;
            }

            if (result.Issues.Any() && result.IsValid)
            {
                result.IsValid = false;
            }

            _logger.LogDebug("Migration chain validation completed. Valid: {IsValid}, Issues: {IssueCount}",
                result.IsValid, result.Issues.Count);

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Issues.Add($"Error validating migration chain: {ex.Message}");
            _logger.LogError(ex, "Error validating migration chain");
            return result;
        }
    }

    #region Private Methods

    private async Task SaveMigrationAsync(SeedDataMigration migration)
    {
        var migrationJson = JsonSerializer.Serialize(migration, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var configKey = $"{SEED_MIGRATION_PREFIX}{migration.Id}";
        var migrationConfig = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == configKey);

        if (migrationConfig == null)
        {
            migrationConfig = new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = configKey,
                Value = migrationJson,
                Description = $"Seed data migration: {migration.Name}",
                Category = "SeedData",
                IsPublic = false,
                DataType = "json",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.SystemConfigurations.AddAsync(migrationConfig);
        }
        else
        {
            migrationConfig.Value = migrationJson;
            migrationConfig.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    private async Task ExecuteMigrationOperationsAsync(SeedDataMigration migration, MigrationResult result)
    {
        foreach (var operation in migration.Operations)
        {
            try
            {
                await ExecuteOperationAsync(operation, migration.Parameters);
                result.OperationsExecuted++;
            }
            catch (Exception ex)
            {
                result.OperationErrors.Add($"Operation '{operation}' failed: {ex.Message}");
                throw;
            }
        }
    }

    private async Task ExecuteOperationAsync(string operation, Dictionary<string, object> parameters)
    {
        // This is a simplified implementation - in a real system, you would have
        // more sophisticated operation execution with proper parsing and validation

        _logger.LogDebug("Executing migration operation: {Operation}", operation);

        switch (operation.ToUpperInvariant())
        {
            case "ADD_CONFIGURATION":
                await AddConfigurationOperation(parameters);
                break;
            case "UPDATE_CONFIGURATION":
                await UpdateConfigurationOperation(parameters);
                break;
            case "DELETE_CONFIGURATION":
                await DeleteConfigurationOperation(parameters);
                break;
            case "ADD_ROLE":
                await AddRoleOperation(parameters);
                break;
            case "ADD_PERMISSION":
                await AddPermissionOperation(parameters);
                break;
            case "CLEANUP_TEST_DATA":
                await CleanupTestDataOperation(parameters);
                break;
            default:
                throw new NotSupportedException($"Operation not supported: {operation}");
        }
    }

    private async Task AddConfigurationOperation(Dictionary<string, object> parameters)
    {
        var key = parameters["key"].ToString();
        var value = parameters["value"].ToString();
        var description = parameters.GetValueOrDefault("description")?.ToString() ?? "";
        var category = parameters.GetValueOrDefault("category")?.ToString() ?? "General";

        var existing = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

        if (existing == null)
        {
            var config = new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = key!,
                Value = value!,
                Description = description,
                Category = category,
                IsPublic = false,
                DataType = "string",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.SystemConfigurations.AddAsync(config);
        }
    }

    private async Task UpdateConfigurationOperation(Dictionary<string, object> parameters)
    {
        var key = parameters["key"].ToString();
        var value = parameters["value"].ToString();

        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

        if (config != null)
        {
            config.Value = value!;
            config.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task DeleteConfigurationOperation(Dictionary<string, object> parameters)
    {
        var key = parameters["key"].ToString();

        var config = await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Key == key);

        if (config != null)
        {
            _context.SystemConfigurations.Remove(config);
        }
    }

    private async Task AddRoleOperation(Dictionary<string, object> parameters)
    {
        var name = parameters["name"].ToString();
        var description = parameters.GetValueOrDefault("description")?.ToString() ?? "";

        var existing = await _context.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == name!.ToUpperInvariant());

        if (existing == null)
        {
            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = name!,
                NormalizedName = name!.ToUpperInvariant(),
                Description = description,
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Roles.AddAsync(role);
        }
    }

    private async Task AddPermissionOperation(Dictionary<string, object> parameters)
    {
        var name = parameters["name"].ToString();
        var description = parameters.GetValueOrDefault("description")?.ToString() ?? "";
        var category = parameters.GetValueOrDefault("category")?.ToString() ?? "General";

        var existing = await _context.Permissions
            .FirstOrDefaultAsync(p => p.Name == name);

        if (existing == null)
        {
            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = name!,
                Description = description,
                Category = category,
                IsSystemPermission = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Permissions.AddAsync(permission);
        }
    }

    private async Task CleanupTestDataOperation(Dictionary<string, object> parameters)
    {
        // Remove test users
        var testUsers = await _context.Users
            .Where(u => u.Email.Value.Contains("test.com") ||
                       u.Email.Value.Contains("example.com") ||
                       u.UserName.Contains("test"))
            .ToListAsync();

        if (testUsers.Any())
        {
            _context.Users.RemoveRange(testUsers);
        }

        // Remove test posts
        var testPosts = await _context.Posts
            .Where(p => p.Title.Contains("test") || p.Title.Contains("demo"))
            .ToListAsync();

        if (testPosts.Any())
        {
            _context.Posts.RemoveRange(testPosts);
        }
    }

    private bool IsVersionNewer(SeedDataVersion version1, SeedDataVersion version2)
    {
        if (version1.Major > version2.Major) return true;
        if (version1.Major < version2.Major) return false;

        if (version1.Minor > version2.Minor) return true;
        if (version1.Minor < version2.Minor) return false;

        if (version1.Patch > version2.Patch) return true;
        if (version1.Patch < version2.Patch) return false;

        return false; // Versions are equal
    }

    private bool AreVersionsEqual(SeedDataVersion version1, SeedDataVersion version2)
    {
        return version1.Major == version2.Major &&
               version1.Minor == version2.Minor &&
               version1.Patch == version2.Patch &&
               version1.PreRelease == version2.PreRelease;
    }

    #endregion
}

/// <summary>
/// Result of a migration operation
/// </summary>
public class MigrationResult
{
    public string MigrationId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
    public int OperationsExecuted { get; set; }
    public List<string> OperationErrors { get; set; } = new();

    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;

    public string GetSummary()
    {
        if (!IsSuccess)
        {
            return $"Migration {MigrationId} failed: {ErrorMessage}";
        }

        return $"Migration {MigrationId} applied successfully in {Duration:mm\\:ss}. {OperationsExecuted} operations executed.";
    }
}

/// <summary>
/// Result of validation operations
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();

    public string GetSummary()
    {
        if (IsValid)
        {
            return "Validation passed";
        }

        return $"Validation failed: {Issues.Count} issues found";
    }
}