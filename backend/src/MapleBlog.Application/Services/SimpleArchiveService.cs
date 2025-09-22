using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Archive;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Services;

/// <summary>
/// 简单归档服务实现
/// 满足IArchiveService接口要求的基本实现
/// </summary>
public class SimpleArchiveService : IArchiveService
{
    private readonly ILogger<SimpleArchiveService> _logger;
    private readonly IPostRepository _postRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUserRepository _userRepository;
    private readonly DbContext _context;
    private readonly IMemoryCache _cache;

    public SimpleArchiveService(
        ILogger<SimpleArchiveService> logger,
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IUserRepository userRepository,
        DbContext context,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<ArchiveDto> GetArchiveByMonthAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation("Getting archive for {Year}/{Month}", year, month);

            var cacheKey = $"archive_month_{year}_{month}";
            if (_cache.TryGetValue(cacheKey, out ArchiveDto cachedArchive))
            {
                return cachedArchive;
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           p.PublishedAt >= startDate &&
                           p.PublishedAt < endDate)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();

            var archiveItems = posts.Select(p => new ArchiveItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                ReadingTime = CalculateReadingTime(p.Content)
            }).ToList();

            var archive = new ArchiveDto
            {
                Year = year,
                Month = month,
                PostCount = posts.Count,
                Items = archiveItems
            };

            // 缓存结果（1小时）
            _cache.Set(cacheKey, archive, TimeSpan.FromHours(1));

            return archive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive for {Year}/{Month}", year, month);
            return new ArchiveDto
            {
                Year = year,
                Month = month,
                PostCount = 0,
                Items = new List<ArchiveItemDto>()
            };
        }
    }

    public async Task<ArchiveDto> GetArchiveByYearAsync(int year)
    {
        try
        {
            _logger.LogInformation("Getting archive for year {Year}", year);

            var cacheKey = $"archive_year_{year}";
            if (_cache.TryGetValue(cacheKey, out ArchiveDto cachedArchive))
            {
                return cachedArchive;
            }

            var startDate = new DateTime(year, 1, 1);
            var endDate = new DateTime(year + 1, 1, 1);

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           p.PublishedAt >= startDate &&
                           p.PublishedAt < endDate)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();

            var archiveItems = posts.Select(p => new ArchiveItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                ReadingTime = CalculateReadingTime(p.Content)
            }).ToList();

            var archive = new ArchiveDto
            {
                Year = year,
                Month = null,
                PostCount = posts.Count,
                Items = archiveItems
            };

            // 缓存结果（2小时）
            _cache.Set(cacheKey, archive, TimeSpan.FromHours(2));

            return archive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive for year {Year}", year);
            return new ArchiveDto
            {
                Year = year,
                Month = null,
                PostCount = 0,
                Items = new List<ArchiveItemDto>()
            };
        }
    }

    public async Task<IEnumerable<ArchiveItemDto>> GetArchiveTimelineAsync()
    {
        try
        {
            _logger.LogInformation("Getting archive timeline");

            var cacheKey = "archive_timeline";
            if (_cache.TryGetValue(cacheKey, out List<ArchiveItemDto> cachedTimeline))
            {
                return cachedTimeline;
            }

            // 获取所有已发布的文章，按发布日期排序
            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .OrderByDescending(p => p.PublishedAt)
                .Take(100) // 限制数量以提高性能
                .ToListAsync();

            var timeline = posts.Select(p => new ArchiveItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                ReadingTime = CalculateReadingTime(p.Content)
            }).ToList();

            // 缓存结果（30分钟）
            _cache.Set(cacheKey, timeline, TimeSpan.FromMinutes(30));

            return timeline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive timeline");
            return new List<ArchiveItemDto>();
        }
    }

    public async Task<IEnumerable<PostListDto>> GetPostsByArchivePeriodAsync(int year, int month)
    {
        try
        {
            _logger.LogInformation("Getting posts for {Year}/{Month}", year, month);

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           p.PublishedAt >= startDate &&
                           p.PublishedAt < endDate)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();

            return posts.Select(p => new PostListDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt,
                AuthorId = p.AuthorId,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "Uncategorized",
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                IsPublished = p.IsPublished,
                IsFeatured = p.IsFeatured
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for {Year}/{Month}", year, month);
            return new List<PostListDto>();
        }
    }

    public async Task<ArchiveStatsDto> GetArchiveStatsAsync()
    {
        try
        {
            _logger.LogInformation("Getting archive statistics");

            var cacheKey = "archive_stats";
            if (_cache.TryGetValue(cacheKey, out ArchiveStatsDto cachedStats))
            {
                return cachedStats;
            }

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .ToListAsync();

            if (!posts.Any())
            {
                return new ArchiveStatsDto
                {
                    TotalPosts = 0,
                    TotalYears = 0,
                    FirstPostDate = null,
                    LastPostDate = null,
                    TopMonths = new List<ArchiveItemDto>()
                };
            }

            var publishedDates = posts.Select(p => p.PublishedAt ?? p.CreatedAt).ToList();
            var firstPostDate = publishedDates.Min();
            var lastPostDate = publishedDates.Max();

            // 计算跨越的年份数
            var totalYears = lastPostDate.Year - firstPostDate.Year + 1;

            // 获取最热门的月份（按文章数量排序）
            var monthlyStats = posts
                .GroupBy(p => new { Year = (p.PublishedAt ?? p.CreatedAt).Year, Month = (p.PublishedAt ?? p.CreatedAt).Month })
                .Select(g => new ArchiveItemDto
                {
                    Id = Guid.NewGuid(),
                    Title = $"{g.Key.Year}年{g.Key.Month}月",
                    PublishedAt = new DateTime(g.Key.Year, g.Key.Month, 1),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderByDescending(x => x.ViewCount)
                .Take(5)
                .ToList();

            var stats = new ArchiveStatsDto
            {
                TotalPosts = posts.Count,
                TotalYears = totalYears,
                FirstPostDate = firstPostDate,
                LastPostDate = lastPostDate,
                TopMonths = monthlyStats
            };

            // 缓存结果（4小时）
            _cache.Set(cacheKey, stats, TimeSpan.FromHours(4));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive statistics");
            return new ArchiveStatsDto
            {
                TotalPosts = 0,
                TotalYears = 0,
                FirstPostDate = null,
                LastPostDate = null,
                TopMonths = new List<ArchiveItemDto>()
            };
        }
    }

    public async Task<IEnumerable<ArchiveItemDto>> GetPopularArchivePeriodsAsync(int count = 10)
    {
        try
        {
            _logger.LogInformation("Getting popular archive periods, count: {Count}", count);

            var cacheKey = $"popular_archive_periods_{count}";
            if (_cache.TryGetValue(cacheKey, out List<ArchiveItemDto> cachedPeriods))
            {
                return cachedPeriods;
            }

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .ToListAsync();

            // 按月份统计文章数量和视图数
            var monthlyStats = posts
                .GroupBy(p => new { Year = (p.PublishedAt ?? p.CreatedAt).Year, Month = (p.PublishedAt ?? p.CreatedAt).Month })
                .Select(g => new ArchiveItemDto
                {
                    Id = Guid.NewGuid(),
                    Title = $"{g.Key.Year}年{g.Key.Month}月 ({g.Count()}篇文章)",
                    Summary = $"共{g.Count()}篇文章，总视图数{g.Sum(p => p.ViewCount)}",
                    PublishedAt = new DateTime(g.Key.Year, g.Key.Month, 1),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount),
                    ReadingTime = g.Count() // 使用文章数作为指标
                })
                .OrderByDescending(x => x.ViewCount) // 按视图数排序
                .ThenByDescending(x => x.ReadingTime) // 再按文章数排序
                .Take(count)
                .ToList();

            // 缓存结果（2小时）
            _cache.Set(cacheKey, monthlyStats, TimeSpan.FromHours(2));

            return monthlyStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular archive periods");
            return new List<ArchiveItemDto>();
        }
    }

    public async Task<bool> RefreshArchiveCacheAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing archive cache");

            // 清理所有归档相关的缓存
            var cacheKeys = new[]
            {
                "archive_stats",
                "archive_timeline"
            };

            foreach (var key in cacheKeys)
            {
                _cache.Remove(key);
            }

            // 清理按月份和年份的缓存（简化处理，实际应该使用更精确的键名管理）
            // 这里只是示意性地清理几个常用的缓存键
            var currentYear = DateTime.UtcNow.Year;
            for (int year = currentYear - 2; year <= currentYear; year++)
            {
                _cache.Remove($"archive_year_{year}");
                for (int month = 1; month <= 12; month++)
                {
                    _cache.Remove($"archive_month_{year}_{month}");
                }
            }

            // 清理热门归档缓存
            for (int count = 5; count <= 20; count += 5)
            {
                _cache.Remove($"popular_archive_periods_{count}");
            }

            _logger.LogInformation("Archive cache refreshed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing archive cache");
            return false;
        }
    }

    public async Task<IEnumerable<ArchiveItemDto>> SearchArchiveAsync(string searchTerm)
    {
        try
        {
            _logger.LogInformation("Searching archive with term: {SearchTerm}", searchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<ArchiveItemDto>();
            }

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           (p.Title.Contains(searchTerm) ||
                            p.Content.Contains(searchTerm) ||
                            (p.Summary != null && p.Summary.Contains(searchTerm))))
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.PublishedAt)
                .Take(50) // 限制搜索结果数量
                .ToListAsync();

            return posts.Select(p => new ArchiveItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                ReadingTime = CalculateReadingTime(p.Content)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching archive with term: {SearchTerm}", searchTerm);
            return new List<ArchiveItemDto>();
        }
    }

    public async Task<CategoryArchiveResponse> GetCategoryArchiveAsync(CategoryArchiveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting category archive for {CategoryId}", request.CategoryId);

            // 获取分类信息
            var category = await _context.Set<Category>()
                .Include(c => c.Parent)
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                return new CategoryArchiveResponse
                {
                    CategoryId = request.CategoryId,
                    CategoryName = "Category Not Found",
                    CategoryDescription = "The requested category was not found.",
                    Items = new List<ArchiveItem>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0,
                    ChildCategories = new List<CategoryInfo>(),
                    Statistics = new MapleBlog.Application.DTOs.Archive.CategoryStatistics()
                };
            }

            // 获取该分类下的文章（分页）
            var query = _context.Set<Post>()
                .Where(p => p.IsPublished && p.CategoryId == request.CategoryId)
                .Include(p => p.Author)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag);

            var totalCount = await query.CountAsync(cancellationToken);
            var posts = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var archiveItems = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ReadingTimeMinutes = CalculateReadingTime(p.Content)
            }).ToList();

            // 获取子分类
            var childCategories = category.Children?.Select(c => new CategoryInfo
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description ?? string.Empty,
                PostCount = c.PostCount,
                Slug = c.Slug
            }).ToList() ?? new List<CategoryInfo>();

            // 统计信息
            var statistics = new MapleBlog.Application.DTOs.Archive.CategoryStatistics
            {
                TotalPosts = totalCount,
                TotalViews = posts.Sum(p => p.ViewCount),
                TotalComments = posts.Sum(p => p.CommentCount),
                AvgViewsPerPost = totalCount > 0 ? (double)posts.Sum(p => p.ViewCount) / totalCount : 0,
                LastPostDate = posts.FirstOrDefault()?.PublishedAt
            };

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new CategoryArchiveResponse
            {
                CategoryId = request.CategoryId,
                CategoryName = category.Name,
                CategoryDescription = category.Description ?? string.Empty,
                Items = archiveItems,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                ChildCategories = childCategories,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category archive for {CategoryId}", request.CategoryId);
            return new CategoryArchiveResponse
            {
                CategoryId = request.CategoryId,
                CategoryName = "Error",
                CategoryDescription = "An error occurred while retrieving the category archive.",
                Items = new List<ArchiveItem>(),
                TotalCount = 0,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                ChildCategories = new List<CategoryInfo>(),
                Statistics = new MapleBlog.Application.DTOs.Archive.CategoryStatistics()
            };
        }
    }

    public async Task<TagArchiveResponse> GetTagArchiveAsync(TagArchiveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting tag archive for {TagId}", request.TagId);

            // 获取标签信息
            var tag = await _context.Set<Tag>()
                .FirstOrDefaultAsync(t => t.Id == request.TagId, cancellationToken);

            if (tag == null)
            {
                return new TagArchiveResponse
                {
                    TagId = request.TagId,
                    TagName = "Tag Not Found",
                    TagDescription = "The requested tag was not found.",
                    Items = new List<ArchiveItem>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0,
                    RelatedTags = new List<TagInfo>(),
                    Statistics = new MapleBlog.Application.DTOs.Archive.TagStatistics()
                };
            }

            // 获取该标签下的文章（分页）
            var query = _context.Set<Post>()
                .Where(p => p.IsPublished && p.PostTags.Any(pt => pt.TagId == request.TagId))
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag);

            var totalCount = await query.CountAsync(cancellationToken);
            var posts = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var archiveItems = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ReadingTimeMinutes = CalculateReadingTime(p.Content)
            }).ToList();

            // 获取相关标签（与该标签共同出现的其他标签）
            var relatedTagIds = await _context.Set<PostTag>()
                .Where(pt => _context.Set<PostTag>()
                    .Where(pt2 => pt2.TagId == request.TagId)
                    .Select(pt2 => pt2.PostId)
                    .Contains(pt.PostId) && pt.TagId != request.TagId)
                .GroupBy(pt => pt.TagId)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            var relatedTags = await _context.Set<Tag>()
                .Where(t => relatedTagIds.Contains(t.Id))
                .Select(t => new TagInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description ?? string.Empty,
                    PostCount = t.PostCount,
                    Slug = t.Slug
                })
                .ToListAsync(cancellationToken);

            // 统计信息
            var statistics = new MapleBlog.Application.DTOs.Archive.TagStatistics
            {
                TotalPosts = totalCount,
                TotalViews = posts.Sum(p => p.ViewCount),
                TotalComments = posts.Sum(p => p.CommentCount),
                AvgViewsPerPost = totalCount > 0 ? (double)posts.Sum(p => p.ViewCount) / totalCount : 0,
                LastPostDate = posts.FirstOrDefault()?.PublishedAt
            };

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new TagArchiveResponse
            {
                TagId = request.TagId,
                TagName = tag.Name,
                TagDescription = tag.Description ?? string.Empty,
                Items = archiveItems,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                RelatedTags = relatedTags,
                Statistics = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag archive for {TagId}", request.TagId);
            return new TagArchiveResponse
            {
                TagId = request.TagId,
                TagName = "Error",
                TagDescription = "An error occurred while retrieving the tag archive.",
                Items = new List<ArchiveItem>(),
                TotalCount = 0,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                RelatedTags = new List<TagInfo>(),
                Statistics = new MapleBlog.Application.DTOs.Archive.TagStatistics()
            };
        }
    }

    public async Task<ArchiveNavigationResponse> GetArchiveNavigationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting archive navigation");

            var cacheKey = "archive_navigation";
            if (_cache.TryGetValue(cacheKey, out ArchiveNavigationResponse cachedNavigation))
            {
                return cachedNavigation;
            }

            // 获取时间导航（按年月统计）
            var timeNavigation = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .GroupBy(p => new { Year = (p.PublishedAt ?? p.CreatedAt).Year, Month = (p.PublishedAt ?? p.CreatedAt).Month })
                .Select(g => new TimeNavigationItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    PostCount = g.Count(),
                    DisplayName = $"{g.Key.Year}年{g.Key.Month}月",
                    Url = $"/archive/{g.Key.Year}/{g.Key.Month}"
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(24) // 显示最近24个月
                .ToListAsync(cancellationToken);

            // 获取分类导航
            var categoryNavigation = await _context.Set<Category>()
                .Where(c => c.PostCount > 0)
                .Select(c => new CategoryNavigationItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    PostCount = c.PostCount,
                    Url = $"/archive/category/{c.Slug}",
                    Description = c.Description ?? string.Empty
                })
                .OrderByDescending(c => c.PostCount)
                .Take(20) // 显示最多20个分类
                .ToListAsync(cancellationToken);

            // 获取标签导航
            var tagNavigation = await _context.Set<Tag>()
                .Where(t => t.PostCount > 0)
                .Select(t => new TagNavigationItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    PostCount = t.PostCount,
                    Url = $"/archive/tag/{t.Slug}",
                    Description = t.Description ?? string.Empty
                })
                .OrderByDescending(t => t.PostCount)
                .Take(30) // 显示最多30个标签
                .ToListAsync(cancellationToken);

            var navigation = new ArchiveNavigationResponse
            {
                TimeNavigation = timeNavigation,
                CategoryNavigation = categoryNavigation,
                TagNavigation = tagNavigation
            };

            // 缓存结果（1小时）
            _cache.Set(cacheKey, navigation, TimeSpan.FromHours(1));

            return navigation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive navigation");
            return new ArchiveNavigationResponse
            {
                TimeNavigation = new List<TimeNavigationItem>(),
                CategoryNavigation = new List<CategoryNavigationItem>(),
                TagNavigation = new List<TagNavigationItem>()
            };
        }
    }

    public async Task<TimeArchiveResponse> GetTimeArchiveAsync(TimeArchiveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting time archive for {ArchiveType} {Year}/{Month}",
                request.ArchiveType, request.Year, request.Month);

            DateTime startDate, endDate;

            // 根据归档类型设置时间范围
            switch (request.ArchiveType)
            {
                case ArchiveType.Month:
                    if (!request.Month.HasValue || !request.Year.HasValue)
                    {
                        throw new ArgumentException("Year and Month are required for monthly archive");
                    }
                    startDate = new DateTime(request.Year.Value, request.Month.Value, 1);
                    endDate = startDate.AddMonths(1);
                    break;
                case ArchiveType.Year:
                    if (!request.Year.HasValue)
                    {
                        throw new ArgumentException("Year is required for yearly archive");
                    }
                    startDate = new DateTime(request.Year.Value, 1, 1);
                    endDate = new DateTime(request.Year.Value + 1, 1, 1);
                    break;
                default:
                    throw new ArgumentException($"Unsupported archive type: {request.ArchiveType}");
            }

            // 获取文章（分页）
            var query = _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           p.PublishedAt >= startDate &&
                           p.PublishedAt < endDate)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag);

            var totalCount = await query.CountAsync(cancellationToken);
            var posts = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var archiveItems = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ReadingTimeMinutes = CalculateReadingTime(p.Content)
            }).ToList();

            // 生成时间轴（按日期分组）
            var timeline = posts
                .GroupBy(p => (p.PublishedAt ?? p.CreatedAt).Date)
                .Select(g => new TimelineItem
                {
                    Date = g.Key,
                    PostCount = g.Count(),
                    DisplayDate = g.Key.ToString("yyyy-MM-dd"),
                    Posts = g.Select(p => new TimelinePost
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Slug = p.Slug,
                        ViewCount = p.ViewCount
                    }).ToList()
                })
                .OrderByDescending(x => x.Date)
                .ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new TimeArchiveResponse
            {
                ArchiveType = request.ArchiveType,
                Year = request.Year,
                Month = request.Month,
                Items = archiveItems,
                Timeline = timeline,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time archive for {ArchiveType} {Year}/{Month}",
                request.ArchiveType, request.Year, request.Month);
            return new TimeArchiveResponse
            {
                ArchiveType = request.ArchiveType,
                Year = request.Year,
                Month = request.Month,
                Items = new List<ArchiveItem>(),
                Timeline = new List<TimelineItem>(),
                TotalCount = 0,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0
            };
        }
    }

    public async Task<ArchiveStatsResponse> GetArchiveStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting archive statistics");

            var cacheKey = "archive_stats_response";
            if (_cache.TryGetValue(cacheKey, out ArchiveStatsResponse cachedStats))
            {
                return cachedStats;
            }

            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .ToListAsync(cancellationToken);

            // 年度统计
            var yearlyStats = posts
                .GroupBy(p => (p.PublishedAt ?? p.CreatedAt).Year)
                .Select(g => new YearlyStatItem
                {
                    Year = g.Key,
                    PostCount = g.Count(),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderByDescending(x => x.Year)
                .ToList();

            // 月度统计
            var monthlyStats = posts
                .GroupBy(p => new { Year = (p.PublishedAt ?? p.CreatedAt).Year, Month = (p.PublishedAt ?? p.CreatedAt).Month })
                .Select(g => new MonthlyStatItem
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    PostCount = g.Count(),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Take(12) // 最近12个月
                .ToList();

            // 分类统计
            var categoryStats = posts
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category)
                .Select(g => new CategoryStatItem
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    PostCount = g.Count(),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderByDescending(x => x.PostCount)
                .Take(10)
                .ToList();

            // 标签统计
            var tagStats = posts
                .SelectMany(p => p.PostTags ?? new List<PostTag>())
                .Where(pt => pt.Tag != null)
                .GroupBy(pt => pt.Tag)
                .Select(g => new TagStatItem
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    PostCount = g.Count(),
                    ViewCount = g.Sum(pt => pt.Post?.ViewCount ?? 0),
                    CommentCount = g.Sum(pt => pt.Post?.CommentCount ?? 0)
                })
                .OrderByDescending(x => x.PostCount)
                .Take(15)
                .ToList();

            // 作者统计
            var authorStats = posts
                .Where(p => p.Author != null)
                .GroupBy(p => p.Author)
                .Select(g => new AuthorStatItem
                {
                    Id = g.Key.Id,
                    Name = g.Key.UserName,
                    PostCount = g.Count(),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderByDescending(x => x.PostCount)
                .Take(10)
                .ToList();

            // 趋势数据（最近12个月的文章发布趋势）
            var trendData = posts
                .Where(p => (p.PublishedAt ?? p.CreatedAt) >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(p => new { Year = (p.PublishedAt ?? p.CreatedAt).Year, Month = (p.PublishedAt ?? p.CreatedAt).Month })
                .Select(g => new TrendDataItem
                {
                    Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                    PostCount = g.Count(),
                    ViewCount = g.Sum(p => p.ViewCount),
                    CommentCount = g.Sum(p => p.CommentCount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            var response = new ArchiveStatsResponse
            {
                TotalPosts = posts.Count,
                YearlyStats = yearlyStats,
                MonthlyStats = monthlyStats,
                CategoryStats = categoryStats,
                TagStats = tagStats,
                AuthorStats = authorStats,
                TrendData = trendData
            };

            // 缓存结果（4小时）
            _cache.Set(cacheKey, response, TimeSpan.FromHours(4));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive statistics");
            return new ArchiveStatsResponse
            {
                TotalPosts = 0,
                YearlyStats = new List<YearlyStatItem>(),
                MonthlyStats = new List<MonthlyStatItem>(),
                CategoryStats = new List<CategoryStatItem>(),
                TagStats = new List<TagStatItem>(),
                AuthorStats = new List<AuthorStatItem>(),
                TrendData = new List<TrendDataItem>()
            };
        }
    }

    public async Task<ArchiveSearchResponse> SearchArchiveAsync(ArchiveSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching archive with query: {Query}", request.Query);

            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return new ArchiveSearchResponse
                {
                    Query = request.Query ?? string.Empty,
                    Items = new List<ArchiveItem>(),
                    TotalCount = 0,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };
            }

            var query = _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           (p.Title.Contains(request.Query) ||
                            p.Content.Contains(request.Query) ||
                            (p.Summary != null && p.Summary.Contains(request.Query))))
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag);

            var totalCount = await query.CountAsync(cancellationToken);
            var posts = await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var archiveItems = posts.Select(p => new ArchiveItem
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary ?? string.Empty,
                PublishedAt = p.PublishedAt ?? p.CreatedAt,
                AuthorName = p.Author?.UserName ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Uncategorized",
                ViewCount = p.ViewCount,
                CommentCount = p.CommentCount,
                TagNames = p.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                ReadingTimeMinutes = CalculateReadingTime(p.Content),
                // 高亮匹配的关键词
                Highlights = ExtractSearchHighlights(p, request.Query)
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new ArchiveSearchResponse
            {
                Query = request.Query,
                Items = archiveItems,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching archive with query: {Query}", request.Query);
            return new ArchiveSearchResponse
            {
                Query = request.Query ?? string.Empty,
                Items = new List<ArchiveItem>(),
                TotalCount = 0,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0
            };
        }
    }

    /// <summary>
    /// 计算文章阅读时间（分钟）
    /// </summary>
    /// <param name="content">文章内容</param>
    /// <returns>阅读时间（分钟）</returns>
    private int CalculateReadingTime(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // 估算阅读速度：中文每分钟300字，英文每分钟200词
        var wordCount = content.Length;
        var readingTimeMinutes = Math.Max(1, (int)Math.Ceiling(wordCount / 300.0));
        return readingTimeMinutes;
    }

    /// <summary>
    /// 提取搜索高亮片段
    /// </summary>
    /// <param name="post">文章</param>
    /// <param name="query">搜索词</param>
    /// <returns>高亮片段列表</returns>
    private List<string> ExtractSearchHighlights(Post post, string query)
    {
        var highlights = new List<string>();
        var normalizedQuery = query.ToLowerInvariant();

        // 从标题提取高亮
        if (post.Title.ToLowerInvariant().Contains(normalizedQuery))
        {
            highlights.Add(post.Title);
        }

        // 从摘要提取高亮
        if (!string.IsNullOrEmpty(post.Summary) && post.Summary.ToLowerInvariant().Contains(normalizedQuery))
        {
            highlights.Add(post.Summary.Length > 100 ? post.Summary.Substring(0, 100) + "..." : post.Summary);
        }

        // 从内容提取高亮片段
        var content = post.Content.ToLowerInvariant();
        var queryIndex = content.IndexOf(normalizedQuery);
        if (queryIndex >= 0)
        {
            var start = Math.Max(0, queryIndex - 50);
            var length = Math.Min(100, post.Content.Length - start);
            var snippet = post.Content.Substring(start, length);
            if (start > 0) snippet = "..." + snippet;
            if (start + length < post.Content.Length) snippet += "...";
            highlights.Add(snippet);
        }

        return highlights.Take(3).ToList(); // 最多返回3个高亮片段
    }
}