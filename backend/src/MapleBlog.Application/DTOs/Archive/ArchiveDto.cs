using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.Archive;

public class ArchiveDto
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public int PostCount { get; set; }
    public IEnumerable<ArchiveItemDto> Items { get; set; } = new List<ArchiveItemDto>();
}

public class ArchiveItemDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PostCount { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    // Additional properties needed by SimpleArchiveService
    /// <summary>
    /// Post unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Post title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Post summary/excerpt
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Publication date and time
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Category name
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Associated tag names
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Tag names (alias for compatibility)
    /// </summary>
    public List<string> TagNames
    {
        get => Tags;
        set => Tags = value;
    }

    /// <summary>
    /// Estimated reading time in minutes
    /// </summary>
    public int ReadingTimeMinutes { get; set; }

    /// <summary>
    /// Reading time (alias for compatibility)
    /// </summary>
    public int ReadingTime
    {
        get => ReadingTimeMinutes;
        set => ReadingTimeMinutes = value;
    }

    /// <summary>
    /// View count
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Comment count
    /// </summary>
    public int CommentCount { get; set; }
}

public class ArchiveStatsDto
{
    public int TotalPosts { get; set; }
    public int TotalYears { get; set; }
    public DateTime? FirstPostDate { get; set; }
    public DateTime? LastPostDate { get; set; }
    public IEnumerable<ArchiveItemDto> TopMonths { get; set; } = new List<ArchiveItemDto>();
}

// 新增的DTO类型
/// <summary>
/// 分类归档请求
/// </summary>
public class CategoryArchiveRequest
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// 是否包含子分类
    /// </summary>
    public bool IncludeChildren { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 页面大小
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 分类归档响应
/// </summary>
public class CategoryArchiveResponse
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategorySlug { get; set; }
    public string? CategoryDescription { get; set; }
    public CategoryInfo? ParentCategory { get; set; }
    public List<CategoryInfo> ChildCategories { get; set; } = new();
    public List<ArchiveItem> Items { get; set; } = new();
    public CategoryStatistics Statistics { get; set; } = new();
    public long TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    // 为兼容性保留的属性
    public IEnumerable<PostListDto> Posts => Items.Select(item => new PostListDto
    {
        Id = item.Id,
        Title = item.Title,
        Slug = item.Slug,
        Summary = item.Summary,
        PublishedAt = item.PublishedAt,
        Author = new PostAuthorDto { DisplayName = item.AuthorName },
        Category = new CategoryDto { Name = item.CategoryName },
        Tags = item.TagNames.Select(name => new TagDto { Name = name }).ToList(),
        Stats = new PostStatsDto { ViewCount = item.ViewCount, CommentCount = item.CommentCount }
    });
    public int Page => CurrentPage;
}

/// <summary>
/// 标签归档请求
/// </summary>
public class TagArchiveRequest
{
    /// <summary>
    /// 标签ID
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 页面大小
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 标签归档响应
/// </summary>
public class TagArchiveResponse
{
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? TagSlug { get; set; }
    public string? TagDescription { get; set; }
    public List<TagInfo> RelatedTags { get; set; } = new();
    public List<ArchiveItem> Items { get; set; } = new();
    public TagStatistics Statistics { get; set; } = new();
    public long TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    // 为兼容性保留的属性
    public IEnumerable<PostListDto> Posts => Items.Select(item => new PostListDto
    {
        Id = item.Id,
        Title = item.Title,
        Slug = item.Slug,
        Summary = item.Summary,
        PublishedAt = item.PublishedAt,
        Author = new PostAuthorDto { DisplayName = item.AuthorName },
        Category = new CategoryDto { Name = item.CategoryName },
        Tags = item.TagNames.Select(name => new TagDto { Name = name }).ToList(),
        Stats = new PostStatsDto { ViewCount = item.ViewCount, CommentCount = item.CommentCount }
    });
    public int Page => CurrentPage;
}

/// <summary>
/// 归档导航响应
/// </summary>
public class ArchiveNavigationResponse
{
    public List<TimeNavigationItem> TimeNavigation { get; set; } = new();
    public List<CategoryNavigationItem> CategoryNavigation { get; set; } = new();
    public List<TagNavigationItem> TagNavigation { get; set; } = new();

    // 为兼容性保留的属性
    public IEnumerable<YearArchiveItem> YearlyArchives =>
        TimeNavigation.Select(t => new YearArchiveItem { Year = t.Year, PostCount = t.PostCount });

    public IEnumerable<CategoryArchiveItem> CategoryArchives =>
        CategoryNavigation.Select(c => new CategoryArchiveItem
        {
            CategoryId = c.Id,
            CategoryName = c.Name,
            PostCount = c.PostCount
        });

    public IEnumerable<TagArchiveItem> TagArchives =>
        TagNavigation.Select(t => new TagArchiveItem
        {
            TagId = t.Id,
            TagName = t.Name,
            PostCount = t.PostCount
        });

    public int TotalPostCount => TimeNavigation.Sum(t => t.PostCount);
}

/// <summary>
/// 年份归档项
/// </summary>
public class YearArchiveItem
{
    /// <summary>
    /// 年份
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 月份归档
    /// </summary>
    public IEnumerable<MonthArchiveItem> MonthlyArchives { get; set; } = new List<MonthArchiveItem>();
}

/// <summary>
/// 月份归档项
/// </summary>
public class MonthArchiveItem
{
    /// <summary>
    /// 月份
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 月份名称
    /// </summary>
    public string MonthName { get; set; } = string.Empty;
}

/// <summary>
/// 分类归档项
/// </summary>
public class CategoryArchiveItem
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 子分类
    /// </summary>
    public IEnumerable<CategoryArchiveItem> Children { get; set; } = new List<CategoryArchiveItem>();
}

/// <summary>
/// 标签归档项
/// </summary>
public class TagArchiveItem
{
    /// <summary>
    /// 标签ID
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 标签颜色
    /// </summary>
    public string TagColor { get; set; } = string.Empty;
}

// AdvancedArchiveService需要的额外DTO类型

/// <summary>
/// 时间归档请求
/// </summary>
public class TimeArchiveRequest
{
    public ArchiveType ArchiveType { get; set; } = ArchiveType.Month;
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 归档搜索请求
/// </summary>
public class ArchiveSearchRequest
{
    public string? Query { get; set; }
    public Guid? CategoryId { get; set; }
    public List<Guid>? TagIds { get; set; }
    public Guid? AuthorId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SortBy { get; set; } = "date";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 时间归档响应
/// </summary>
public class TimeArchiveResponse
{
    public ArchiveType ArchiveType { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public List<ArchiveItem> Items { get; set; } = new();
    public List<TimelineItem> Timeline { get; set; } = new();
    public long TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// 归档统计响应
/// </summary>
public class ArchiveStatsResponse
{
    public long TotalPosts { get; set; }
    public List<YearlyStatItem> YearlyStats { get; set; } = new();
    public List<MonthlyStatItem> MonthlyStats { get; set; } = new();
    public List<CategoryStatItem> CategoryStats { get; set; } = new();
    public List<TagStatItem> TagStats { get; set; } = new();
    public List<AuthorStatItem> AuthorStats { get; set; } = new();
    public List<TrendDataItem> TrendData { get; set; } = new();
}

/// <summary>
/// 归档搜索响应
/// </summary>
public class ArchiveSearchResponse
{
    public string Query { get; set; } = string.Empty;
    public List<ArchiveItem> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// 归档项
/// </summary>
public class ArchiveItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public List<string> TagNames { get; set; } = new();
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }

    // Additional properties needed by SimpleArchiveService
    public List<string> Tags { get; set; } = new();
    public int ReadingTimeMinutes { get; set; }
    public List<string> Highlights { get; set; } = new();
}

// 统计相关的DTO
public class TimelineItem
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public int PostCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }

    /// <summary>
    /// Timeline date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Display date string
    /// </summary>
    public string DisplayDate { get; set; } = string.Empty;

    /// <summary>
    /// Posts for this timeline item
    /// </summary>
    public List<TimelinePost> Posts { get; set; } = new();
}

/// <summary>
/// Timeline post item
/// </summary>
public class TimelinePost
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int ViewCount { get; set; }
}

public class CategoryInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int PostCount { get; set; }
}

public class TagInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int PostCount { get; set; }
}

public class CategoryStatistics
{
    public int TotalPosts { get; set; }
    public int TotalViews { get; set; }
    public int TotalComments { get; set; }
    public int AverageViews { get; set; }
    public double AvgViewsPerPost { get; set; }
    public DateTime? LastPostDate { get; set; }
    public DateTime? FirstPostDate { get; set; }
}

public class TagStatistics
{
    public int TotalPosts { get; set; }
    public int TotalViews { get; set; }
    public int TotalComments { get; set; }
    public int AverageViews { get; set; }
    public double AvgViewsPerPost { get; set; }
    public DateTime? LastPostDate { get; set; }
    public DateTime? FirstPostDate { get; set; }
}

public class YearlyStatItem
{
    public int Year { get; set; }
    public int PostCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
}

public class MonthlyStatItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PostCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
}

public class CategoryStatItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int PostCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
}

public class TagStatItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int PostCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
}

public class AuthorStatItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public int ViewCount { get; set; }
    public int CommentCount { get; set; }
}

public class TrendDataItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PostCount { get; set; }
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Trend date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// View count for this period
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Comment count for this period
    /// </summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Day of the month (optional)
    /// </summary>
    public int? Day { get; set; }
}

public class TimeNavigationItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public List<MonthNavigationItem> Months { get; set; } = new();
}

public class MonthNavigationItem
{
    public int Month { get; set; }
    public int PostCount { get; set; }
}

public class CategoryNavigationItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public List<CategoryNavigationItem> Children { get; set; } = new();
}

public class TagNavigationItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public int PostCount { get; set; }
}