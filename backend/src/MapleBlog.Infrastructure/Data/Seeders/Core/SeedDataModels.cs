namespace MapleBlog.Infrastructure.Data.Seeders.Core;

/// <summary>
/// Result of a seed data operation
/// </summary>
public class SeedResult
{
    public string Environment { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public bool RequiresConfirmation { get; set; }

    // Configuration used
    public SeedDataConfiguration? Configuration { get; set; }

    // Validation results
    public SeedValidationResult? ValidationResult { get; set; }

    // Statistics
    public int RolesCreated { get; set; }
    public int RolesSkipped { get; set; }
    public int PermissionsCreated { get; set; }
    public int PermissionsSkipped { get; set; }
    public int UsersCreated { get; set; }
    public int UsersSkipped { get; set; }
    public int CategoriesCreated { get; set; }
    public int CategoriesSkipped { get; set; }
    public int TagsCreated { get; set; }
    public int TagsSkipped { get; set; }
    public int PostsCreated { get; set; }
    public int PostsSkipped { get; set; }
    public int ConfigurationsCreated { get; set; }
    public int ConfigurationsSkipped { get; set; }

    // Validation
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();

    // Summary properties
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    public int TotalCreated => RolesCreated + PermissionsCreated + UsersCreated +
                              CategoriesCreated + TagsCreated + PostsCreated + ConfigurationsCreated;
    public int TotalSkipped => RolesSkipped + PermissionsSkipped + UsersSkipped +
                              CategoriesSkipped + TagsSkipped + PostsSkipped + ConfigurationsSkipped;
    public bool HasValidationIssues => ValidationErrors.Any() || ValidationWarnings.Any();

    /// <summary>
    /// Gets a summary of the seeding operation
    /// </summary>
    public string GetSummary()
    {
        if (!IsSuccess)
        {
            return $"Seeding failed for {Environment}: {ErrorMessage}";
        }

        return $"Seeding completed for {Environment} in {Duration:mm\\:ss}. " +
               $"Created: {TotalCreated}, Skipped: {TotalSkipped}";
    }

    /// <summary>
    /// Gets detailed statistics
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            { "Environment", Environment },
            { "Duration", Duration.ToString(@"mm\:ss") },
            { "Success", IsSuccess },
            { "RequiredConfirmation", RequiresConfirmation },
            { "TotalCreated", TotalCreated },
            { "TotalSkipped", TotalSkipped },
            { "Roles", new { Created = RolesCreated, Skipped = RolesSkipped } },
            { "Permissions", new { Created = PermissionsCreated, Skipped = PermissionsSkipped } },
            { "Users", new { Created = UsersCreated, Skipped = UsersSkipped } },
            { "Categories", new { Created = CategoriesCreated, Skipped = CategoriesSkipped } },
            { "Tags", new { Created = TagsCreated, Skipped = TagsSkipped } },
            { "Posts", new { Created = PostsCreated, Skipped = PostsSkipped } },
            { "Configurations", new { Created = ConfigurationsCreated, Skipped = ConfigurationsSkipped } },
            { "ValidationErrors", ValidationErrors },
            { "ValidationWarnings", ValidationWarnings }
        };
    }
}

/// <summary>
/// Status of seed data in an environment
/// </summary>
public class SeedStatus
{
    public string Environment { get; set; } = string.Empty;
    public DateTime CheckTime { get; set; }
    public bool IsHealthy { get; set; }
    public bool CanConnectToDatabase { get; set; }
    public bool HasTestData { get; set; }

    // Data counts
    public int ExistingUsers { get; set; }
    public int ExistingPosts { get; set; }
    public int ExistingCategories { get; set; }
    public int ExistingTags { get; set; }
    public int ExistingRoles { get; set; }

    // Issues found
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets a summary of the status
    /// </summary>
    public string GetSummary()
    {
        if (!CanConnectToDatabase)
        {
            return "Cannot connect to database";
        }

        if (!IsHealthy)
        {
            return $"Environment has {Issues.Count} issues: {string.Join(", ", Issues)}";
        }

        var totalRecords = ExistingUsers + ExistingPosts + ExistingCategories + ExistingTags + ExistingRoles;
        return $"Environment is healthy. Total records: {totalRecords}";
    }
}

/// <summary>
/// Result of cleaning test data
/// </summary>
public class CleanupResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsDryRun { get; set; }

    // Test data found
    public int TestUsersFound { get; set; }
    public int TestPostsFound { get; set; }
    public int TestCategoriesFound { get; set; }
    public int TestTagsFound { get; set; }

    // Test data removed (only in actual cleanup, not dry run)
    public int TestUsersRemoved { get; set; }
    public int TestPostsRemoved { get; set; }
    public int TestCategoriesRemoved { get; set; }
    public int TestTagsRemoved { get; set; }

    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
    public int TotalFound => TestUsersFound + TestPostsFound + TestCategoriesFound + TestTagsFound;
    public int TotalRemoved => TestUsersRemoved + TestPostsRemoved + TestCategoriesRemoved + TestTagsRemoved;

    /// <summary>
    /// Gets a summary of the cleanup operation
    /// </summary>
    public string GetSummary()
    {
        if (!IsSuccess)
        {
            return $"Cleanup failed: {ErrorMessage}";
        }

        if (IsDryRun)
        {
            return $"Dry run completed. Found {TotalFound} test records that would be removed.";
        }

        return $"Cleanup completed in {Duration:mm\\:ss}. Removed {TotalRemoved} test records.";
    }
}

/// <summary>
/// Seed data version information
/// </summary>
public class SeedDataVersion
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public string? PreRelease { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();

    public string Version => PreRelease != null
        ? $"{Major}.{Minor}.{Patch}-{PreRelease}"
        : $"{Major}.{Minor}.{Patch}";

    public override string ToString() => Version;
}

/// <summary>
/// Seed data migration information
/// </summary>
public class SeedDataMigration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SeedDataVersion FromVersion { get; set; } = new();
    public SeedDataVersion ToVersion { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsApplied { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? AppliedBy { get; set; }
    public List<string> Operations { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets a summary of the migration
    /// </summary>
    public string GetSummary()
    {
        var status = IsApplied ? "Applied" : "Pending";
        return $"{Name} ({FromVersion} → {ToVersion}) - {status}";
    }
}

/// <summary>
/// Seed data event for auditing
/// </summary>
public class SeedDataEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Environment { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Creates a seed event
    /// </summary>
    public static SeedDataEvent Create(string environment, string operation, bool isSuccess, TimeSpan duration, string user = "System")
    {
        return new SeedDataEvent
        {
            Environment = environment,
            Operation = operation,
            IsSuccess = isSuccess,
            Duration = duration,
            User = user
        };
    }
}