using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Archive;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Constants;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MapleBlog.Application.Services;

/// <summary>
/// 归档应用服务
/// 提供内容归档和浏览的核心功能，包括时间归档、分类归档和统计分析
/// </summary>
public class AdvancedArchiveService : IAdvancedArchiveService
{
    private readonly ILogger<AdvancedArchiveService> _logger;
    private readonly DbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly JsonSerializerOptions _jsonOptions;

    // 缓存配置
    private readonly int _archiveCacheMinutes;
    private readonly int _statsCacheMinutes;
    private readonly bool _enableCaching;

    public AdvancedArchiveService(
        ILogger<AdvancedArchiveService> logger,
        DbContext context,
        IDistributedCache cache,
        IConfiguration configuration,
        IPostRepository postRepository,
        IUserRepository userRepository)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _configuration = configuration;
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // 读取配置
        _archiveCacheMinutes = _configuration.GetValue<int>("Archive:CacheMinutes", 30);
        _statsCacheMinutes = _configuration.GetValue<int>("Archive:StatsCacheMinutes", 60);
        _enableCaching = _configuration.GetValue<bool>("Archive:EnableCaching", true);
    }

    /// <summary>
    /// 获取时间归档
    /// </summary>
    public async Task<TimeArchiveResponse> GetTimeArchiveAsync(TimeArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"archive:time:{request.ArchiveType}:{request.Year}:{request.Month}:{request.Page}:{request.PageSize}";

        try
        {
            // 尝试从缓存获取
            if (_enableCaching)
            {
                var cachedResult = await GetFromCacheAsync<TimeArchiveResponse>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            var response = new TimeArchiveResponse
            {
                ArchiveType = request.ArchiveType,
                Year = request.Year,
                Month = request.Month
            };

            var query = _context.Set<Post>()
                .Where(p => p.IsPublished);

            // 应用时间过滤
            query = ApplyTimeFilter(query, request);

            // 获取总数
            var totalCount = await query.LongCountAsync(cancellationToken);

            // 获取归档项目
            var posts = await query
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            response.Items = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? TruncateContent(p.Content, 200),
                AuthorName = p.Author?.DisplayName ?? p.Author?.UserName ?? "匿名",
                CategoryName = p.Category?.Name ?? "未分类",
                PublishedAt = p.PublishedAt,
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount
            }).ToList();

            response.TotalCount = totalCount;
            response.CurrentPage = request.Page;
            response.PageSize = request.PageSize;
            response.TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // 获取时间轴数据
            response.Timeline = await GetTimelineDataAsync(request.ArchiveType, request.Year, request.Month, cancellationToken);

            // 缓存结果
            if (_enableCaching)
            {
                await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(_archiveCacheMinutes), cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time archive for {ArchiveType} {Year}/{Month}", request.ArchiveType, request.Year, request.Month);
            return new TimeArchiveResponse
            {
                ArchiveType = request.ArchiveType,
                Year = request.Year,
                Month = request.Month,
                Items = new List<ArchiveItem>()
            };
        }
    }

    /// <summary>
    /// 获取分类归档
    /// </summary>
    public async Task<CategoryArchiveResponse> GetCategoryArchiveAsync(CategoryArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"archive:category:{request.CategoryId}:{request.IncludeChildren}:{request.Page}:{request.PageSize}";

        try
        {
            // 尝试从缓存获取
            if (_enableCaching)
            {
                var cachedResult = await GetFromCacheAsync<CategoryArchiveResponse>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            var category = await _context.Set<Category>()
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return new CategoryArchiveResponse { CategoryId = request.CategoryId };
            }

            var response = new CategoryArchiveResponse
            {
                CategoryId = request.CategoryId,
                CategoryName = category.Name,
                CategorySlug = category.Slug,
                CategoryDescription = category.Description,
                ParentCategory = category.Parent != null ? new CategoryInfo
                {
                    Id = category.Parent.Id,
                    Name = category.Parent.Name,
                    Slug = category.Parent.Slug
                } : null
            };

            // 构建分类查询
            var categoryIds = new List<Guid> { request.CategoryId };

            // 如果包含子分类
            if (request.IncludeChildren && category.Children?.Any() == true)
            {
                categoryIds.AddRange(await GetAllChildCategoryIds(request.CategoryId, cancellationToken));
                response.ChildCategories = category.Children.Select(c => new CategoryInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    PostCount = _context.Set<Post>().Count(p => p.CategoryId == c.Id && p.IsPublished)
                }).ToList();
            }

            var query = _context.Set<Post>()
                .Where(p => p.IsPublished && p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value));

            // 获取总数
            var totalCount = await query.LongCountAsync(cancellationToken);

            // 获取归档项目
            var posts = await query
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            response.Items = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? TruncateContent(p.Content, 200),
                AuthorName = p.Author?.DisplayName ?? p.Author?.UserName ?? "匿名",
                CategoryName = p.Category?.Name ?? "未分类",
                PublishedAt = p.PublishedAt,
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount
            }).ToList();

            response.TotalCount = totalCount;
            response.CurrentPage = request.Page;
            response.PageSize = request.PageSize;
            response.TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // 获取分类统计
            response.Statistics = await GetCategoryStatisticsAsync(request.CategoryId, request.IncludeChildren, cancellationToken);

            // 缓存结果
            if (_enableCaching)
            {
                await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(_archiveCacheMinutes), cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category archive for category {CategoryId}", request.CategoryId);
            return new CategoryArchiveResponse { CategoryId = request.CategoryId };
        }
    }

    /// <summary>
    /// 获取标签归档
    /// </summary>
    public async Task<TagArchiveResponse> GetTagArchiveAsync(TagArchiveRequest request, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"archive:tag:{request.TagId}:{request.Page}:{request.PageSize}";

        try
        {
            // 尝试从缓存获取
            if (_enableCaching)
            {
                var cachedResult = await GetFromCacheAsync<TagArchiveResponse>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            var tag = await _context.Set<Tag>()
                .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);

            if (tag == null)
            {
                return new TagArchiveResponse { TagId = request.TagId };
            }

            var response = new TagArchiveResponse
            {
                TagId = request.TagId,
                TagName = tag.Name,
                TagSlug = tag.Slug,
                TagDescription = tag.Description
            };

            var query = _context.Set<Post>()
                .Where(p => p.IsPublished && p.Tags!.Any(t => t.Id == request.TagId));

            // 获取总数
            var totalCount = await query.LongCountAsync(cancellationToken);

            // 获取归档项目
            var posts = await query
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            response.Items = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? TruncateContent(p.Content, 200),
                AuthorName = p.Author?.DisplayName ?? p.Author?.UserName ?? "匿名",
                CategoryName = p.Category?.Name ?? "未分类",
                PublishedAt = p.PublishedAt,
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount
            }).ToList();

            response.TotalCount = totalCount;
            response.CurrentPage = request.Page;
            response.PageSize = request.PageSize;
            response.TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // 获取相关标签
            response.RelatedTags = await GetRelatedTagsAsync(request.TagId, 10, cancellationToken);

            // 获取标签统计
            response.Statistics = await GetTagStatisticsAsync(request.TagId, cancellationToken);

            // 缓存结果
            if (_enableCaching)
            {
                await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(_archiveCacheMinutes), cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag archive for tag {TagId}", request.TagId);
            return new TagArchiveResponse { TagId = request.TagId };
        }
    }

    /// <summary>
    /// 获取归档统计
    /// </summary>
    public async Task<ArchiveStatsResponse> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "archive:stats:overview";

        try
        {
            // 尝试从缓存获取
            if (_enableCaching)
            {
                var cachedResult = await GetFromCacheAsync<ArchiveStatsResponse>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            var response = new ArchiveStatsResponse();

            // 总体统计
            var totalPosts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .LongCountAsync(cancellationToken);

            response.TotalPosts = totalPosts;

            // 年度统计
            response.YearlyStats = await GetYearlyStatsAsync(cancellationToken);

            // 月度统计
            response.MonthlyStats = await GetMonthlyStatsAsync(cancellationToken);

            // 分类统计
            response.CategoryStats = await GetCategoryStatsAsync(cancellationToken);

            // 标签统计
            response.TagStats = await GetTagStatsAsync(cancellationToken);

            // 作者统计
            response.AuthorStats = await GetAuthorStatsAsync(cancellationToken);

            // 趋势数据
            response.TrendData = await GetTrendDataAsync(cancellationToken);

            // 缓存结果
            if (_enableCaching)
            {
                await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(_statsCacheMinutes), cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive statistics");
            return new ArchiveStatsResponse();
        }
    }

    /// <summary>
    /// 获取归档导航
    /// </summary>
    public async Task<ArchiveNavigationResponse> GetArchiveNavigationAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "archive:navigation";

        try
        {
            // 尝试从缓存获取
            if (_enableCaching)
            {
                var cachedResult = await GetFromCacheAsync<ArchiveNavigationResponse>(cacheKey, cancellationToken);
                if (cachedResult != null)
                {
                    return cachedResult;
                }
            }

            var response = new ArchiveNavigationResponse();

            // 时间导航
            response.TimeNavigation = await GetTimeNavigationAsync(cancellationToken);

            // 分类导航
            response.CategoryNavigation = await GetCategoryNavigationAsync(cancellationToken);

            // 标签导航
            response.TagNavigation = await GetTagNavigationAsync(cancellationToken);

            // 缓存结果
            if (_enableCaching)
            {
                await SetCacheAsync(cacheKey, response, TimeSpan.FromMinutes(_archiveCacheMinutes), cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive navigation");
            return new ArchiveNavigationResponse();
        }
    }

    /// <summary>
    /// 搜索归档内容
    /// </summary>
    public async Task<ArchiveSearchResponse> SearchArchiveAsync(ArchiveSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<Post>()
                .Where(p => p.IsPublished);

            // 应用搜索条件
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                query = query.Where(p =>
                    p.Title.Contains(request.Query) ||
                    p.Content.Contains(request.Query) ||
                    p.Summary!.Contains(request.Query));
            }

            // 应用过滤条件
            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId);
            }

            if (request.TagIds?.Any() == true)
            {
                query = query.Where(p => p.Tags!.Any(t => request.TagIds.Contains(t.Id)));
            }

            if (request.AuthorId.HasValue)
            {
                query = query.Where(p => p.AuthorId == request.AuthorId);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where(p => p.PublishedAt >= request.StartDate);
            }

            if (request.EndDate.HasValue)
            {
                query = query.Where(p => p.PublishedAt <= request.EndDate);
            }

            // 排序
            query = request.SortBy?.ToLowerInvariant() switch
            {
                "title" => request.SortDirection == "asc"
                    ? query.OrderBy(p => p.Title)
                    : query.OrderByDescending(p => p.Title),
                "views" => request.SortDirection == "asc"
                    ? query.OrderBy(p => p.ViewCount)
                    : query.OrderByDescending(p => p.ViewCount),
                "comments" => request.SortDirection == "asc"
                    ? query.OrderBy(p => p.CommentCount)
                    : query.OrderByDescending(p => p.CommentCount),
                _ => query.OrderByDescending(p => p.PublishedAt)
            };

            // 获取总数
            var totalCount = await query.LongCountAsync(cancellationToken);

            // 获取结果
            var posts = await query
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var response = new ArchiveSearchResponse
            {
                Query = request.Query,
                Items = posts.Select(p => new ArchiveItem
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Summary = p.Summary ?? TruncateContent(p.Content, 200),
                    AuthorName = p.Author?.DisplayName ?? p.Author?.UserName ?? "匿名",
                    CategoryName = p.Category?.Name ?? "未分类",
                    TagNames = p.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                    PublishedAt = p.PublishedAt,
                    ViewCount = p.ViewCount,
                    CommentCount = p.CommentCount
                }).ToList(),
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching archive with query: {Query}", request.Query);
            return new ArchiveSearchResponse { Query = request.Query ?? "" };
        }
    }

    #region 私有方法

    /// <summary>
    /// 应用时间过滤
    /// </summary>
    private IQueryable<Post> ApplyTimeFilter(IQueryable<Post> query, TimeArchiveRequest request)
    {
        switch (request.ArchiveType)
        {
            case ArchiveType.Year:
                if (request.Year.HasValue)
                {
                    query = query.Where(p => p.PublishedAt!.Value.Year == request.Year.Value);
                }
                break;

            case ArchiveType.Month:
                if (request.Year.HasValue && request.Month.HasValue)
                {
                    query = query.Where(p =>
                        p.PublishedAt!.Value.Year == request.Year.Value &&
                        p.PublishedAt!.Value.Month == request.Month.Value);
                }
                else if (request.Year.HasValue)
                {
                    query = query.Where(p => p.PublishedAt!.Value.Year == request.Year.Value);
                }
                break;

            case ArchiveType.Day:
                if (request.Year.HasValue && request.Month.HasValue && request.Day.HasValue)
                {
                    query = query.Where(p =>
                        p.PublishedAt!.Value.Year == request.Year.Value &&
                        p.PublishedAt!.Value.Month == request.Month.Value &&
                        p.PublishedAt!.Value.Day == request.Day.Value);
                }
                break;
        }

        return query;
    }

    /// <summary>
    /// 获取时间轴数据
    /// </summary>
    private async Task<List<TimelineItem>> GetTimelineDataAsync(ArchiveType archiveType, int? year, int? month, CancellationToken cancellationToken)
    {
        var timeline = new List<TimelineItem>();

        try
        {
            switch (archiveType)
            {
                case ArchiveType.Year:
                    // 获取所有年份的文章数量
                    var yearlyData = await _context.Set<Post>()
                        .Where(p => p.IsPublished && p.PublishedAt.HasValue)
                        .GroupBy(p => p.PublishedAt!.Value.Year)
                        .Select(g => new TimelineItem
                        {
                            Year = g.Key,
                            PostCount = g.Count(),
                            ViewCount = g.Sum(p => p.ViewCount),
                            CommentCount = g.Sum(p => p.CommentCount)
                        })
                        .OrderByDescending(t => t.Year)
                        .ToListAsync(cancellationToken);

                    timeline.AddRange(yearlyData);
                    break;

                case ArchiveType.Month:
                    if (year.HasValue)
                    {
                        // 获取指定年份的月度数据
                        var monthlyData = await _context.Set<Post>()
                            .Where(p => p.IsPublished && p.PublishedAt.HasValue && p.PublishedAt!.Value.Year == year.Value)
                            .GroupBy(p => p.PublishedAt!.Value.Month)
                            .Select(g => new TimelineItem
                            {
                                Year = year.Value,
                                Month = g.Key,
                                PostCount = g.Count(),
                                ViewCount = g.Sum(p => p.ViewCount),
                                CommentCount = g.Sum(p => p.CommentCount)
                            })
                            .OrderByDescending(t => t.Month)
                            .ToListAsync(cancellationToken);

                        timeline.AddRange(monthlyData);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timeline data for {ArchiveType}", archiveType);
        }

        return timeline;
    }

    /// <summary>
    /// 获取所有子分类ID
    /// </summary>
    private async Task<List<Guid>> GetAllChildCategoryIds(Guid categoryId, CancellationToken cancellationToken)
    {
        var childIds = new List<Guid>();

        var children = await _context.Set<Category>()
            .Where(c => c.ParentId == categoryId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var childId in children)
        {
            childIds.Add(childId);
            var grandChildren = await GetAllChildCategoryIds(childId, cancellationToken);
            childIds.AddRange(grandChildren);
        }

        return childIds;
    }

    /// <summary>
    /// 获取分类统计
    /// </summary>
    private async Task<DTOs.Archive.CategoryStatistics> GetCategoryStatisticsAsync(Guid categoryId, bool includeChildren, CancellationToken cancellationToken)
    {
        try
        {
            var categoryIds = new List<Guid> { categoryId };

            if (includeChildren)
            {
                categoryIds.AddRange(await GetAllChildCategoryIds(categoryId, cancellationToken));
            }

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished && p.CategoryId.HasValue && categoryIds.Contains(p.CategoryId.Value))
                .ToListAsync(cancellationToken);

            return new DTOs.Archive.CategoryStatistics
            {
                TotalPosts = posts.Count,
                TotalViews = posts.Sum(p => p.ViewCount),
                TotalComments = posts.Sum(p => p.CommentCount),
                AverageViews = posts.Count > 0 ? (int)posts.Average(p => p.ViewCount) : 0,
                LastPostDate = posts.Max(p => p.PublishedAt),
                FirstPostDate = posts.Min(p => p.PublishedAt)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category statistics for {CategoryId}", categoryId);
            return new DTOs.Archive.CategoryStatistics();
        }
    }

    /// <summary>
    /// 获取相关标签
    /// </summary>
    private async Task<List<TagInfo>> GetRelatedTagsAsync(Guid tagId, int count, CancellationToken cancellationToken)
    {
        try
        {
            // 找到与当前标签一起使用的其他标签
            var relatedTags = await _context.Set<PostTag>()
                .Where(pt => pt.Post.Tags!.Any(t => t.Id == tagId) && pt.TagId != tagId)
                .GroupBy(pt => pt.TagId)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => new { TagId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var tagInfos = new List<TagInfo>();
            foreach (var relatedTag in relatedTags)
            {
                var tag = await _context.Set<Tag>()
                    .FirstOrDefaultAsync(t => t.Id == relatedTag.TagId, cancellationToken);

                if (tag != null)
                {
                    tagInfos.Add(new TagInfo
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        Slug = tag.Slug,
                        PostCount = relatedTag.Count
                    });
                }
            }

            return tagInfos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related tags for {TagId}", tagId);
            return new List<TagInfo>();
        }
    }

    /// <summary>
    /// 获取标签统计
    /// </summary>
    private async Task<DTOs.Archive.TagStatistics> GetTagStatisticsAsync(Guid tagId, CancellationToken cancellationToken)
    {
        try
        {
            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished && p.Tags!.Any(t => t.Id == tagId))
                .ToListAsync(cancellationToken);

            return new DTOs.Archive.TagStatistics
            {
                TotalPosts = posts.Count,
                TotalViews = posts.Sum(p => p.ViewCount),
                TotalComments = posts.Sum(p => p.CommentCount),
                AverageViews = posts.Count > 0 ? (int)posts.Average(p => p.ViewCount) : 0,
                LastPostDate = posts.Max(p => p.PublishedAt),
                FirstPostDate = posts.Min(p => p.PublishedAt)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag statistics for {TagId}", tagId);
            return new DTOs.Archive.TagStatistics();
        }
    }

    /// <summary>
    /// 获取年度统计
    /// </summary>
    private async Task<List<YearlyStatItem>> GetYearlyStatsAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Post>()
            .Where(p => p.IsPublished && p.PublishedAt.HasValue)
            .GroupBy(p => p.PublishedAt!.Value.Year)
            .Select(g => new YearlyStatItem
            {
                Year = g.Key,
                PostCount = g.Count(),
                ViewCount = g.Sum(p => p.ViewCount),
                CommentCount = g.Sum(p => p.CommentCount)
            })
            .OrderByDescending(s => s.Year)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取月度统计
    /// </summary>
    private async Task<List<MonthlyStatItem>> GetMonthlyStatsAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Post>()
            .Where(p => p.IsPublished && p.PublishedAt.HasValue)
            .GroupBy(p => new { p.PublishedAt!.Value.Year, p.PublishedAt!.Value.Month })
            .Select(g => new MonthlyStatItem
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                PostCount = g.Count(),
                ViewCount = g.Sum(p => p.ViewCount),
                CommentCount = g.Sum(p => p.CommentCount)
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .Take(24) // 最近24个月
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取分类统计
    /// </summary>
    private async Task<List<CategoryStatItem>> GetCategoryStatsAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Category>()
            .Where(c => c.Posts.Any(p => p.IsPublished))
            .Select(c => new CategoryStatItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                PostCount = c.Posts.Count(p => p.IsPublished),
                ViewCount = c.Posts.Where(p => p.IsPublished).Sum(p => p.ViewCount),
                CommentCount = c.Posts.Where(p => p.IsPublished).Sum(p => p.CommentCount)
            })
            .OrderByDescending(s => s.PostCount)
            .Take(20) // 前20个分类
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取标签统计
    /// </summary>
    private async Task<List<TagStatItem>> GetTagStatsAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Tag>()
            .Where(t => t.Posts.Any(p => p.IsPublished))
            .Select(t => new TagStatItem
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                PostCount = t.Posts.Count(p => p.IsPublished),
                ViewCount = t.Posts.Where(p => p.IsPublished).Sum(p => p.ViewCount),
                CommentCount = t.Posts.Where(p => p.IsPublished).Sum(p => p.CommentCount)
            })
            .OrderByDescending(s => s.PostCount)
            .Take(50) // 前50个标签
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取作者统计
    /// </summary>
    private async Task<List<AuthorStatItem>> GetAuthorStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("开始获取作者统计数据");

            // 获取所有已发布的文章
            var posts = await _postRepository.GetQueryable()
                .Where(p => p.Status == PostStatus.Published && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            if (!posts.Any())
            {
                _logger.LogInformation("没有找到已发布的文章");
                return new List<AuthorStatItem>();
            }

            // 按作者分组统计
            var authorStats = posts
                .GroupBy(p => p.AuthorId)
                .Select(g => new
                {
                    AuthorId = g.Key,
                    PostCount = g.Count(),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderByDescending(x => x.PostCount)
                .Take(10)
                .ToList();

            // 获取作者信息
            var result = new List<AuthorStatItem>();
            foreach (var stat in authorStats)
            {
                try
                {
                    // 尝试获取作者用户信息
                    var user = await _userRepository.GetByIdAsync(stat.AuthorId, cancellationToken);

                    result.Add(new AuthorStatItem
                    {
                        Id = user?.Id ?? stat.AuthorId,
                        Name = user?.UserName ?? $"作者_{stat.AuthorId.ToString()[..8]}", // 使用用户名或截断的ID
                        PostCount = stat.PostCount,
                        ViewCount = stat.ViewCount,
                        CommentCount = stat.CommentCount
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取作者信息失败，作者ID: {AuthorId}", stat.AuthorId);

                    // 即使获取用户信息失败，也保留统计数据
                    result.Add(new AuthorStatItem
                    {
                        Id = stat.AuthorId,
                        Name = $"未知作者_{stat.AuthorId.ToString()[..8]}",
                        PostCount = stat.PostCount,
                        ViewCount = stat.ViewCount,
                        CommentCount = stat.CommentCount
                    });
                }
            }

            _logger.LogDebug("作者统计数据获取完成，共 {Count} 位作者", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取作者统计数据时发生错误");
            return new List<AuthorStatItem>();
        }
    }

    /// <summary>
    /// 获取趋势数据
    /// </summary>
    private async Task<List<TrendDataItem>> GetTrendDataAsync(CancellationToken cancellationToken)
    {
        var last12Months = new List<TrendDataItem>();
        var now = DateTime.UtcNow;

        for (int i = 11; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var year = date.Year;
            var month = date.Month;

            var postCount = await _context.Set<Post>()
                .CountAsync(p => p.IsPublished &&
                    p.PublishedAt.HasValue &&
                    p.PublishedAt!.Value.Year == year &&
                    p.PublishedAt!.Value.Month == month, cancellationToken);

            last12Months.Add(new TrendDataItem
            {
                Year = year,
                Month = month,
                PostCount = postCount,
                Label = $"{year}-{month:D2}"
            });
        }

        return last12Months;
    }

    /// <summary>
    /// 获取时间导航
    /// </summary>
    private async Task<List<TimeNavigationItem>> GetTimeNavigationAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Post>()
            .Where(p => p.IsPublished && p.PublishedAt.HasValue)
            .GroupBy(p => p.PublishedAt!.Value.Year)
            .Select(g => new TimeNavigationItem
            {
                Year = g.Key,
                PostCount = g.Count(),
                Months = g.GroupBy(p => p.PublishedAt!.Value.Month)
                    .Select(m => new MonthNavigationItem
                    {
                        Month = m.Key,
                        PostCount = m.Count()
                    })
                    .OrderByDescending(m => m.Month)
                    .ToList()
            })
            .OrderByDescending(t => t.Year)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取分类导航
    /// </summary>
    private async Task<List<CategoryNavigationItem>> GetCategoryNavigationAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Category>()
            .Where(c => c.ParentId == null && c.Posts.Any(p => p.IsPublished))
            .Select(c => new CategoryNavigationItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                PostCount = c.Posts.Count(p => p.IsPublished),
                Children = c.Children
                    .Where(child => child.Posts.Any(p => p.IsPublished))
                    .Select(child => new CategoryNavigationItem
                    {
                        Id = child.Id,
                        Name = child.Name,
                        Slug = child.Slug,
                        PostCount = child.Posts.Count(p => p.IsPublished)
                    })
                    .OrderByDescending(child => child.PostCount)
                    .ToList()
            })
            .OrderByDescending(c => c.PostCount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取标签导航
    /// </summary>
    private async Task<List<TagNavigationItem>> GetTagNavigationAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Tag>()
            .Where(t => t.Posts.Any(p => p.IsPublished))
            .Select(t => new TagNavigationItem
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                PostCount = t.Posts.Count(p => p.IsPublished)
            })
            .OrderByDescending(t => t.PostCount)
            .Take(100) // 限制标签数量
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 从缓存获取数据
    /// </summary>
    private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data from cache with key: {Key}", key);
        }

        return null;
    }

    /// <summary>
    /// 设置缓存数据
    /// </summary>
    private async Task SetCacheAsync<T>(string key, T data, TimeSpan expiration, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
            await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache with key: {Key}", key);
        }
    }

    /// <summary>
    /// 截断内容
    /// </summary>
    private static string TruncateContent(string? content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length <= maxLength)
        {
            return content ?? string.Empty;
        }

        return content.Substring(0, maxLength) + "...";
    }

    #endregion
}

/// <summary>
/// 高级归档服务接口
/// </summary>
public interface IAdvancedArchiveService
{
    /// <summary>
    /// 获取时间归档
    /// </summary>
    Task<TimeArchiveResponse> GetTimeArchiveAsync(TimeArchiveRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分类归档
    /// </summary>
    Task<CategoryArchiveResponse> GetCategoryArchiveAsync(CategoryArchiveRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取标签归档
    /// </summary>
    Task<TagArchiveResponse> GetTagArchiveAsync(TagArchiveRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取归档统计
    /// </summary>
    Task<ArchiveStatsResponse> GetArchiveStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取归档导航
    /// </summary>
    Task<ArchiveNavigationResponse> GetArchiveNavigationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索归档内容
    /// </summary>
    Task<ArchiveSearchResponse> SearchArchiveAsync(ArchiveSearchRequest request, CancellationToken cancellationToken = default);
}

