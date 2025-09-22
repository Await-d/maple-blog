namespace MapleBlog.Application.DTOs;

/// <summary>
/// Comprehensive tag analytics data transfer object for admin dashboard
/// </summary>
public class TagAnalyticsDto
{
    /// <summary>
    /// Total number of tags in the system
    /// </summary>
    public int TotalTags { get; set; }

    /// <summary>
    /// Number of active tags (used in last 30 days)
    /// </summary>
    public int ActiveTags { get; set; }

    /// <summary>
    /// Number of inactive tags (not used in last 30 days)
    /// </summary>
    public int InactiveTags { get; set; }

    /// <summary>
    /// Number of unused tags (never used in any post)
    /// </summary>
    public int UnusedTags { get; set; }

    /// <summary>
    /// Average tags per post across all posts
    /// </summary>
    public double AverageTagsPerPost { get; set; }

    /// <summary>
    /// Most used tags (top 20)
    /// </summary>
    public List<TagUsageInfo> MostUsedTags { get; set; } = new();

    /// <summary>
    /// Least used tags (bottom 20, excluding unused)
    /// </summary>
    public List<TagUsageInfo> LeastUsedTags { get; set; } = new();

    /// <summary>
    /// Recently created tags (last 30 days)
    /// </summary>
    public List<TagUsageInfo> RecentlyCreatedTags { get; set; } = new();

    /// <summary>
    /// Tags with highest growth in usage (last 30 days vs previous 30 days)
    /// </summary>
    public List<TagGrowthInfo> FastestGrowingTags { get; set; } = new();

    /// <summary>
    /// Tags with declining usage
    /// </summary>
    public List<TagGrowthInfo> DecliningTags { get; set; } = new();

    /// <summary>
    /// Tag creation trends over the last 12 months
    /// </summary>
    public Dictionary<string, int> TagCreationTrends { get; set; } = new();

    /// <summary>
    /// Tag usage trends over the last 12 months
    /// </summary>
    public Dictionary<string, int> TagUsageTrends { get; set; } = new();

    /// <summary>
    /// Distribution of tag usage (how many tags are used X times)
    /// </summary>
    public Dictionary<string, int> UsageDistribution { get; set; } = new();

    /// <summary>
    /// Tag co-occurrence analysis (most common tag combinations)
    /// </summary>
    public List<TagCombinationInfo> CommonTagCombinations { get; set; } = new();

    /// <summary>
    /// Authors who create the most new tags
    /// </summary>
    public List<TagCreatorInfo> TopTagCreators { get; set; } = new();

    /// <summary>
    /// Average tag lifespan (time between creation and last use)
    /// </summary>
    public double AverageTagLifespanDays { get; set; }

    /// <summary>
    /// Percentage of tags that become inactive within 90 days
    /// </summary>
    public double TagAbandonmentRate { get; set; }

    /// <summary>
    /// Suggested tags for cleanup (unused or rarely used)
    /// </summary>
    public List<TagCleanupSuggestion> CleanupSuggestions { get; set; } = new();

    /// <summary>
    /// Overall tag health score (0-100)
    /// </summary>
    public int TagHealthScore { get; set; }

    /// <summary>
    /// Last analysis update timestamp
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Basic tag usage information
/// </summary>
public class TagUsageInfo
{
    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Tag color
    /// </summary>
    public string? Color { get; set; }
}

/// <summary>
/// Tag growth information
/// </summary>
public class TagGrowthInfo
{
    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Current usage count
    /// </summary>
    public int CurrentUsage { get; set; }

    /// <summary>
    /// Previous period usage count
    /// </summary>
    public int PreviousUsage { get; set; }

    /// <summary>
    /// Growth percentage
    /// </summary>
    public double GrowthPercentage { get; set; }

    /// <summary>
    /// Absolute growth (current - previous)
    /// </summary>
    public int AbsoluteGrowth => CurrentUsage - PreviousUsage;
}

/// <summary>
/// Information about common tag combinations
/// </summary>
public class TagCombinationInfo
{
    /// <summary>
    /// Tags in this combination
    /// </summary>
    public List<string> TagNames { get; set; } = new();

    /// <summary>
    /// Number of posts using this combination
    /// </summary>
    public int CombinationCount { get; set; }

    /// <summary>
    /// Percentage of posts that use this combination
    /// </summary>
    public double CombinationPercentage { get; set; }

    /// <summary>
    /// Posts that use this tag combination
    /// </summary>
    public List<Guid> Posts { get; set; } = new();
}

/// <summary>
/// Information about users who create many tags
/// </summary>
public class TagCreatorInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Number of tags created
    /// </summary>
    public int TagsCreated { get; set; }

    /// <summary>
    /// Average usage of created tags
    /// </summary>
    public double AverageTagUsage { get; set; }

    /// <summary>
    /// Most recent tag creation date
    /// </summary>
    public DateTimeOffset? LastTagCreated { get; set; }

    /// <summary>
    /// Total usage across all created tags
    /// </summary>
    public int TotalUsage { get; set; }
}

/// <summary>
/// Suggestion for tag cleanup
/// </summary>
public class TagCleanupSuggestion
{
    /// <summary>
    /// Tag ID
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// Tag name
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Type of cleanup suggested (string format for compatibility)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Type of cleanup suggested
    /// </summary>
    public TagCleanupType CleanupType { get; set; }

    /// <summary>
    /// Reason for the suggestion
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Description of the cleanup suggestion
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tag IDs involved in this suggestion
    /// </summary>
    public List<Guid> TagIds { get; set; } = new();

    /// <summary>
    /// Suggested action
    /// </summary>
    public string SuggestedAction { get; set; } = string.Empty;

    /// <summary>
    /// Alternative tags to merge with (if applicable)
    /// </summary>
    public List<string> MergeCandidates { get; set; } = new();

    /// <summary>
    /// Priority level (1-5, where 5 is highest priority)
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// Types of tag cleanup operations
/// </summary>
public enum TagCleanupType
{
    /// <summary>
    /// Delete unused tag
    /// </summary>
    Delete = 1,

    /// <summary>
    /// Merge similar tags
    /// </summary>
    Merge = 2,

    /// <summary>
    /// Rename inconsistent tag
    /// </summary>
    Rename = 3,

    /// <summary>
    /// Archive rarely used tag
    /// </summary>
    Archive = 4
}