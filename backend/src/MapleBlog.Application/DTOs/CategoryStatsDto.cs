namespace MapleBlog.Application.DTOs;

/// <summary>
/// Category statistics data transfer object
/// </summary>
public class CategoryStatsDto
{
    /// <summary>
    /// Category ID
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Category slug
    /// </summary>
    public string CategorySlug { get; set; } = string.Empty;

    /// <summary>
    /// Total number of posts in this category
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// Number of published posts in this category
    /// </summary>
    public int PublishedPostCount { get; set; }

    /// <summary>
    /// Number of draft posts in this category
    /// </summary>
    public int DraftPostCount { get; set; }

    /// <summary>
    /// Total number of comments on posts in this category
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Total number of views for posts in this category
    /// </summary>
    public int TotalViews { get; set; }

    /// <summary>
    /// Average views per post in this category
    /// </summary>
    public double AverageViewsPerPost => PostCount > 0 ? (double)TotalViews / PostCount : 0;

    /// <summary>
    /// Most recent post date in this category
    /// </summary>
    public DateTimeOffset? LastPostDate { get; set; }

    /// <summary>
    /// Most recent post title
    /// </summary>
    public string? LastPostTitle { get; set; }

    /// <summary>
    /// Number of child categories
    /// </summary>
    public int ChildCategoryCount { get; set; }

    /// <summary>
    /// Child categories with their basic info
    /// </summary>
    public List<CategoryBasicInfo> ChildCategories { get; set; } = new();

    /// <summary>
    /// Most popular posts in this category (top 5)
    /// </summary>
    public List<PostBasicInfo> PopularPosts { get; set; } = new();

    /// <summary>
    /// Recent posts in this category (last 5)
    /// </summary>
    public List<PostBasicInfo> RecentPosts { get; set; } = new();

    /// <summary>
    /// Monthly post creation statistics (last 12 months)
    /// </summary>
    public Dictionary<string, int> MonthlyPostCounts { get; set; } = new();

    /// <summary>
    /// Post engagement rate (comments per post)
    /// </summary>
    public double EngagementRate => PostCount > 0 ? (double)CommentCount / PostCount : 0;

    /// <summary>
    /// Whether this category is actively being used (has posts in last 30 days)
    /// </summary>
    public bool IsActive => LastPostDate?.Date >= DateTime.UtcNow.AddDays(-30).Date;

    /// <summary>
    /// Category hierarchy depth
    /// </summary>
    public int HierarchyDepth { get; set; }

    /// <summary>
    /// Parent category information (if applicable)
    /// </summary>
    public CategoryBasicInfo? ParentCategory { get; set; }
}

/// <summary>
/// Basic category information for reference in stats
/// </summary>
public class CategoryBasicInfo
{
    /// <summary>
    /// Category ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Category description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of posts in this category
    /// </summary>
    public int PostCount { get; set; }
}

/// <summary>
/// Basic post information for reference in category stats
/// </summary>
public class PostBasicInfo
{
    /// <summary>
    /// Post ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Post title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Post slug
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Post publication date
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Number of views
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Number of comments
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;
}