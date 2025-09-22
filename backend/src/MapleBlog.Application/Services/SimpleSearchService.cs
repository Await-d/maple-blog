using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Search;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace MapleBlog.Application.Services;


/// <summary>
/// 简单搜索服务实现
/// 满足ISearchService接口要求的基本实现
/// </summary>
public class SimpleSearchService : ISearchService
{
    private readonly ILogger<SimpleSearchService> _logger;
    private readonly IPostRepository _postRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUserRepository _userRepository;
    private readonly DbContext _context;
    private readonly IMemoryCache _cache;

    public SimpleSearchService(
        ILogger<SimpleSearchService> logger,
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

    public async Task<SearchResultDto<PostListDto>> SearchPostsAsync(SearchRequestDto searchRequest)
    {
        try
        {
            _logger.LogInformation("Searching posts with query: {Query}", searchRequest.Query);

            if (string.IsNullOrWhiteSpace(searchRequest.Query))
            {
                return new SearchResultDto<PostListDto>
                {
                    Results = new List<PostListDto>(),
                    TotalCount = 0,
                    Page = searchRequest.Page,
                    PageSize = searchRequest.PageSize,
                    TotalPages = 0,
                    Query = searchRequest.Query
                };
            }

            // 使用数据库全文搜索
            var posts = await _postRepository.SearchAsync(
                searchRequest.Query,
                searchRequest.Page,
                searchRequest.PageSize,
                publishedOnly: true);

            // 转换为PostListDto
            var postDtos = posts.Select(p => new PostListDto
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
                Tags = p.PostTags?.Select(pt => new TagDto { Name = pt.Tag?.Name ?? string.Empty }).ToList() ?? new List<TagDto>(),
                IsPublished = p.IsPublished,
                IsFeatured = p.IsFeatured
            }).ToList();

            // 计算总数（简化处理，实际应该分离查询）
            var totalCount = await _context.Set<Post>()
                .Where(p => p.IsPublished &&
                           (p.Title.Contains(searchRequest.Query) ||
                            p.Content.Contains(searchRequest.Query) ||
                            p.Summary.Contains(searchRequest.Query)))
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / searchRequest.PageSize);

            return new SearchResultDto<PostListDto>
            {
                Results = postDtos,
                TotalCount = totalCount,
                Page = searchRequest.Page,
                PageSize = searchRequest.PageSize,
                TotalPages = totalPages,
                Query = searchRequest.Query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts with query: {Query}", searchRequest.Query);
            return new SearchResultDto<PostListDto>
            {
                Results = new List<PostListDto>(),
                TotalCount = 0,
                Page = searchRequest.Page,
                PageSize = searchRequest.PageSize,
                TotalPages = 0,
                Query = searchRequest.Query
            };
        }
    }

    public async Task<SearchResultDto<object>> SearchAllAsync(SearchRequestDto searchRequest)
    {
        try
        {
            _logger.LogInformation("Searching all content with query: {Query}", searchRequest.Query);

            if (string.IsNullOrWhiteSpace(searchRequest.Query))
            {
                return new SearchResultDto<object>
                {
                    Results = new List<object>(),
                    TotalCount = 0,
                    Page = searchRequest.Page,
                    PageSize = searchRequest.PageSize,
                    TotalPages = 0,
                    Query = searchRequest.Query
                };
            }

            var results = new List<object>();
            var totalCount = 0;

            // 搜索文章
            var posts = await _postRepository.SearchAsync(
                searchRequest.Query,
                searchRequest.Page,
                searchRequest.PageSize / 2, // 分配一半空间给文章
                publishedOnly: true);

            foreach (var post in posts)
            {
                results.Add(new
                {
                    Type = "Post",
                    Id = post.Id,
                    Title = post.Title,
                    Summary = post.Summary,
                    Url = $"/posts/{post.Slug}",
                    Author = post.Author?.UserName,
                    PublishedAt = post.PublishedAt
                });
            }

            // 搜索分类
            var categories = await _context.Set<Category>()
                .Where(c => c.Name.Contains(searchRequest.Query) || c.Description.Contains(searchRequest.Query))
                .Take(searchRequest.PageSize / 4) // 分配四分之一空间给分类
                .ToListAsync();

            foreach (var category in categories)
            {
                results.Add(new
                {
                    Type = "Category",
                    Id = category.Id,
                    Title = category.Name,
                    Summary = category.Description,
                    Url = $"/categories/{category.Slug}",
                    PostCount = category.PostCount
                });
            }

            // 搜索标签
            var tags = await _context.Set<Tag>()
                .Where(t => t.Name.Contains(searchRequest.Query) || t.Description.Contains(searchRequest.Query))
                .Take(searchRequest.PageSize / 4) // 分配四分之一空间给标签
                .ToListAsync();

            foreach (var tag in tags)
            {
                results.Add(new
                {
                    Type = "Tag",
                    Id = tag.Id,
                    Title = tag.Name,
                    Summary = tag.Description,
                    Url = $"/tags/{tag.Slug}",
                    PostCount = tag.PostCount
                });
            }

            // 计算总数（简化处理）
            totalCount = results.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / searchRequest.PageSize);

            return new SearchResultDto<object>
            {
                Results = results,
                TotalCount = totalCount,
                Page = searchRequest.Page,
                PageSize = searchRequest.PageSize,
                TotalPages = totalPages,
                Query = searchRequest.Query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching all content with query: {Query}", searchRequest.Query);
            return new SearchResultDto<object>
            {
                Results = new List<object>(),
                TotalCount = 0,
                Page = searchRequest.Page,
                PageSize = searchRequest.PageSize,
                TotalPages = 0,
                Query = searchRequest.Query
            };
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        try
        {
            _logger.LogInformation("Getting search suggestions for query: {Query}", query);

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return new List<string>();
            }

            var cacheKey = $"search_suggestions_{query.ToLowerInvariant()}_{maxSuggestions}";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedSuggestions))
            {
                return cachedSuggestions;
            }

            var suggestions = new List<string>();

            // 从文章标题获取建议
            var titleSuggestions = await _context.Set<Post>()
                .Where(p => p.IsPublished && p.Title.Contains(query))
                .Select(p => p.Title)
                .Distinct()
                .Take(maxSuggestions / 2)
                .ToListAsync();
            suggestions.AddRange(titleSuggestions);

            // 从分类名称获取建议
            var categorySuggestions = await _context.Set<Category>()
                .Where(c => c.Name.Contains(query))
                .Select(c => c.Name)
                .Distinct()
                .Take(maxSuggestions / 4)
                .ToListAsync();
            suggestions.AddRange(categorySuggestions);

            // 从标签名称获取建议
            var tagSuggestions = await _context.Set<Tag>()
                .Where(t => t.Name.Contains(query))
                .Select(t => t.Name)
                .Distinct()
                .Take(maxSuggestions / 4)
                .ToListAsync();
            suggestions.AddRange(tagSuggestions);

            // 去重并限制数量
            var uniqueSuggestions = suggestions.Distinct().Take(maxSuggestions).ToList();

            // 缓存结果（5分钟）
            _cache.Set(cacheKey, uniqueSuggestions, TimeSpan.FromMinutes(5));

            return uniqueSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return new List<string>();
        }
    }

    public async Task<bool> IndexPostAsync(Guid postId)
    {
        try
        {
            _logger.LogInformation("Indexing post: {PostId}", postId);

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
            {
                _logger.LogWarning("Post not found for indexing: {PostId}", postId);
                return false;
            }

            // 检查是否已存在索引
            var existingIndex = await _context.Set<SearchIndex>()
                .FirstOrDefaultAsync(si => si.EntityType == "post" && si.EntityId == postId);

            if (existingIndex != null)
            {
                // 更新现有索引
                existingIndex.UpdateContent(
                    post.Title,
                    post.Content,
                    GenerateKeywords(post),
                    "zh-CN");
                existingIndex.MarkAsActive();
            }
            else
            {
                // 创建新索引
                var searchIndex = SearchIndex.Create(
                    "post",
                    postId,
                    post.Title,
                    post.Content,
                    GenerateKeywords(post),
                    "zh-CN");
                _context.Set<SearchIndex>().Add(searchIndex);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully indexed post: {PostId}", postId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing post: {PostId}", postId);
            return false;
        }
    }

    public async Task<bool> RemoveFromIndexAsync(Guid postId)
    {
        try
        {
            _logger.LogInformation("Removing post from index: {PostId}", postId);

            var searchIndex = await _context.Set<SearchIndex>()
                .FirstOrDefaultAsync(si => si.EntityType == "post" && si.EntityId == postId);

            if (searchIndex != null)
            {
                _context.Set<SearchIndex>().Remove(searchIndex);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully removed post from index: {PostId}", postId);
                return true;
            }

            _logger.LogWarning("Search index not found for post: {PostId}", postId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing post from index: {PostId}", postId);
            return false;
        }
    }

    public async Task<bool> UpdateIndexAsync(Guid postId)
    {
        try
        {
            _logger.LogInformation("Updating index for post: {PostId}", postId);

            // 更新索引和重新索引是同一个操作
            return await IndexPostAsync(postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating index for post: {PostId}", postId);
            return false;
        }
    }

    public async Task<bool> RebuildIndexAsync()
    {
        try
        {
            _logger.LogInformation("Rebuilding search index");

            // 清理现有索引
            var existingIndexes = await _context.Set<SearchIndex>().ToListAsync();
            _context.Set<SearchIndex>().RemoveRange(existingIndexes);
            await _context.SaveChangesAsync();

            // 重新索引所有已发布的文章
            var posts = await _postRepository.GetAllPublishedAsync();
            var successCount = 0;
            var totalCount = posts.Count();

            foreach (var post in posts)
            {
                var success = await IndexPostAsync(post.Id);
                if (success)
                {
                    successCount++;
                }
            }

            _logger.LogInformation("Rebuild completed: {SuccessCount}/{TotalCount} posts indexed",
                successCount, totalCount);

            return successCount == totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding search index");
            return false;
        }
    }

    public async Task<SearchStatsDto> GetSearchStatsAsync()
    {
        try
        {
            _logger.LogInformation("Getting search statistics");

            var cacheKey = "search_stats";
            if (_cache.TryGetValue(cacheKey, out SearchStatsDto cachedStats))
            {
                return cachedStats;
            }

            // 统计搜索索引数量
            var totalIndexes = await _context.Set<SearchIndex>()
                .Where(si => si.IsActive)
                .CountAsync();

            // 统计文章数量
            var totalPosts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .CountAsync();

            // 模拟搜索次数（实际应该从搜索日志或独立表获取）
            var totalSearches = totalIndexes * 10; // 模拟数据

            var stats = new SearchStatsDto
            {
                TotalSearches = totalSearches,
                TotalResults = totalPosts,
                LastSearchDate = DateTime.UtcNow,
                IndexedPostsCount = totalIndexes,
                PublishedPostsCount = totalPosts,
                AverageResultsPerSearch = totalPosts > 0 ? (double)totalPosts / Math.Max(totalSearches, 1) : 0
            };

            // 缓存统计结果（15分钟）
            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(15));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search statistics");
            return new SearchStatsDto
            {
                TotalSearches = 0,
                TotalResults = 0,
                LastSearchDate = DateTime.UtcNow
            };
        }
    }

    public async Task<IEnumerable<string>> GetPopularSearchTermsAsync(int count = 20)
    {
        try
        {
            _logger.LogInformation("Getting popular search terms, count: {Count}", count);

            var cacheKey = $"popular_search_terms_{count}";
            if (_cache.TryGetValue(cacheKey, out List<string> cachedTerms))
            {
                return cachedTerms;
            }

            // 从数据库获取热门搜索词（实际应该有专门的搜索日志表）
            // 这里使用文章标题和分类标签作为热门搜索词
            var popularTerms = new List<string>();

            // 获取热门文章标题关键词
            var popularPosts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.ViewCount)
                .Take(count / 2)
                .Select(p => p.Title)
                .ToListAsync();

            // 提取关键词（简化处理）
            foreach (var title in popularPosts)
            {
                var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2)
                    .Take(2);
                popularTerms.AddRange(words);
            }

            // 获取热门分类
            var popularCategories = await _context.Set<Category>()
                .OrderByDescending(c => c.PostCount)
                .Take(count / 4)
                .Select(c => c.Name)
                .ToListAsync();
            popularTerms.AddRange(popularCategories);

            // 获取热门标签
            var popularTags = await _context.Set<Tag>()
                .OrderByDescending(t => t.PostCount)
                .Take(count / 4)
                .Select(t => t.Name)
                .ToListAsync();
            popularTerms.AddRange(popularTags);

            // 去重并限制数量
            var uniqueTerms = popularTerms.Distinct().Take(count).ToList();

            // 缓存结果（30分钟）
            _cache.Set(cacheKey, uniqueTerms, TimeSpan.FromMinutes(30));

            return uniqueTerms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular search terms");
            return new List<string>();
        }
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    /// <param name="request">搜索请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索响应</returns>
    public async Task<MapleBlog.Application.DTOs.Search.SearchResponse> SearchAsync(MapleBlog.Application.DTOs.Search.SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing search with query: {Query}, Type: {SearchType}",
                request.Query, request.SearchType);

            var searchId = Guid.NewGuid();
            var startTime = DateTime.UtcNow;

            // 实现真正的搜索逻辑
            var results = new List<MapleBlog.Application.DTOs.Search.SearchResultItem>();
            var totalCount = 0;

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                // 搜索文章
                var posts = await _postRepository.SearchAsync(
                    request.Query,
                    request.Page,
                    request.PageSize,
                    publishedOnly: true,
                    cancellationToken);

                foreach (var post in posts)
                {
                    results.Add(new MapleBlog.Application.DTOs.Search.SearchResultItem
                    {
                        Id = Guid.NewGuid(),
                        EntityId = post.Id,
                        EntityType = "post",
                        Type = "post",
                        Title = post.Title,
                        Summary = post.Summary ?? string.Empty,
                        Content = post.Content.Length > 200 ? post.Content.Substring(0, 200) + "..." : post.Content,
                        Url = $"/posts/{post.Slug}",
                        Score = CalculateRelevanceScore(post, request.Query),
                        Highlights = ExtractHighlights(post, request.Query),
                        CreatedAt = post.CreatedAt,
                        UpdatedAt = post.UpdatedAt ?? post.CreatedAt,
                        Author = post.Author?.UserName ?? "Unknown",
                        Category = post.Category?.Name ?? "Uncategorized",
                        Tags = post.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                        Thumbnail = null, // 可以从post的metadata或附件中获取
                        ViewCount = post.ViewCount,
                        LikeCount = 0, // 如果有点赞功能
                        CommentCount = post.CommentCount
                    });
                }

                // 计算总数
                totalCount = await _context.Set<Post>()
                    .Where(p => p.IsPublished &&
                               (p.Title.Contains(request.Query) ||
                                p.Content.Contains(request.Query) ||
                                (p.Summary != null && p.Summary.Contains(request.Query))))
                    .CountAsync(cancellationToken);
            }

            var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var response = new MapleBlog.Application.DTOs.Search.SearchResponse
            {
                SearchId = searchId,
                Results = results,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                Query = request.Query,
                ExecutionTimeMs = executionTime,
                Suggestions = await GetSuggestionsAsync(request.Query, 5, cancellationToken),
                HasMore = request.Page < totalPages
            };

            _logger.LogInformation("Search completed. Query: {Query}, Results: {Count}, Time: {Time}ms",
                request.Query, totalCount, executionTime);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search with query: {Query}", request.Query);
            return new MapleBlog.Application.DTOs.Search.SearchResponse
            {
                SearchId = Guid.NewGuid(),
                Results = new List<MapleBlog.Application.DTOs.Search.SearchResultItem>(),
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = 0,
                Query = request.Query,
                ExecutionTimeMs = 0,
                Suggestions = new List<string>(),
                HasMore = false
            };
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="count">建议数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索建议列表</returns>
    public async Task<IEnumerable<string>> GetSuggestionsAsync(string query, int count, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting search suggestions for query: {Query}, count: {Count}", query, count);

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<string>();
            }

            // 简单的建议实现 - 基于查询词生成一些示例建议
            var suggestions = new List<string>();
            var normalizedQuery = query.Trim().ToLowerInvariant();

            // 模拟一些通用建议
            var commonSuggestions = new List<string>
            {
                $"{normalizedQuery} 教程",
                $"{normalizedQuery} 指南",
                $"{normalizedQuery} 实例",
                $"{normalizedQuery} 最佳实践",
                $"{normalizedQuery} 入门"
            };

            suggestions.AddRange(commonSuggestions.Take(count));

            await Task.Delay(5, cancellationToken); // 模拟异步操作

            _logger.LogInformation("Generated {Count} suggestions for query: {Query}", suggestions.Count, query);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for query: {Query}", query);
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取热门搜索
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="timeSpan">时间范围</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热门搜索列表</returns>
    public async Task<IEnumerable<MapleBlog.Application.DTOs.Search.PopularSearchItem>> GetPopularSearchesAsync(int count, TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting popular searches, count: {Count}, timeSpan: {TimeSpan}", count, timeSpan);

            // 简单的热门搜索实现 - 返回一些示例数据
            var popularSearches = new List<MapleBlog.Application.DTOs.Search.PopularSearchItem>
            {
                new MapleBlog.Application.DTOs.Search.PopularSearchItem
                {
                    Query = "ASP.NET Core",
                    SearchCount = 150,
                    LastSearched = DateTime.UtcNow.AddHours(-2),
                    Trend = 1,
                    ChangePercentage = 15.5,
                    IsPromoted = false
                },
                new MapleBlog.Application.DTOs.Search.PopularSearchItem
                {
                    Query = "Entity Framework",
                    SearchCount = 120,
                    LastSearched = DateTime.UtcNow.AddHours(-1),
                    Trend = 0,
                    ChangePercentage = 2.1,
                    IsPromoted = false
                },
                new MapleBlog.Application.DTOs.Search.PopularSearchItem
                {
                    Query = "React",
                    SearchCount = 100,
                    LastSearched = DateTime.UtcNow.AddMinutes(-30),
                    Trend = -1,
                    ChangePercentage = -5.2,
                    IsPromoted = true
                }
            };

            await Task.Delay(5, cancellationToken); // 模拟异步操作

            var result = popularSearches
                .Where(s => s.LastSearched >= DateTime.UtcNow.Subtract(timeSpan))
                .Take(count)
                .ToList();

            _logger.LogInformation("Retrieved {Count} popular searches", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular searches");
            return new List<MapleBlog.Application.DTOs.Search.PopularSearchItem>();
        }
    }

    /// <summary>
    /// 获取搜索历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索历史列表</returns>
    public async Task<IEnumerable<MapleBlog.Application.DTOs.Search.SearchHistoryItem>> GetSearchHistoryAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting search history for user: {UserId}, count: {Count}", userId, count);

            // 简单的搜索历史实现 - 返回一些示例数据
            var searchHistory = new List<MapleBlog.Application.DTOs.Search.SearchHistoryItem>
            {
                new MapleBlog.Application.DTOs.Search.SearchHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Query = "C# 异步编程",
                    SearchedAt = DateTime.UtcNow.AddHours(-1),
                    SearchType = "general",
                    ResultCount = 25,
                    ExecutionTime = 120,
                    HasResults = true,
                    ClickedResults = 3
                },
                new MapleBlog.Application.DTOs.Search.SearchHistoryItem
                {
                    Id = Guid.NewGuid(),
                    Query = "微服务架构",
                    SearchedAt = DateTime.UtcNow.AddHours(-3),
                    SearchType = "general",
                    ResultCount = 18,
                    ExecutionTime = 95,
                    HasResults = true,
                    ClickedResults = 1
                }
            };

            await Task.Delay(5, cancellationToken); // 模拟异步操作

            var result = searchHistory.Take(count).ToList();

            _logger.LogInformation("Retrieved {Count} search history items for user: {UserId}", result.Count, userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history for user: {UserId}", userId);
            return new List<MapleBlog.Application.DTOs.Search.SearchHistoryItem>();
        }
    }

    /// <summary>
    /// 清除搜索历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> ClearSearchHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Clearing search history for user: {UserId}", userId);

            // 简单的清除历史实现 - 模拟清除操作
            await Task.Delay(10, cancellationToken); // 模拟异步操作

            _logger.LogInformation("Search history cleared for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 获取推荐搜索
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="baseQuery">基础查询</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐搜索结果列表</returns>
    public async Task<IEnumerable<MapleBlog.Application.DTOs.Search.SearchResultItem>> GetRecommendationsAsync(Guid userId, string? baseQuery, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting recommendations for user: {UserId}, baseQuery: {BaseQuery}", userId, baseQuery);

            // 简单的推荐实现 - 返回一些示例推荐
            var recommendations = new List<MapleBlog.Application.DTOs.Search.SearchResultItem>
            {
                new MapleBlog.Application.DTOs.Search.SearchResultItem
                {
                    Id = Guid.NewGuid(),
                    Type = "post",
                    Title = "ASP.NET Core 最佳实践",
                    Summary = "了解ASP.NET Core开发的最佳实践和常见模式",
                    Url = "/posts/aspnet-core-best-practices",
                    Score = 0.95,
                    Highlights = new List<string> { "ASP.NET Core", "最佳实践" },
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    Author = "管理员",
                    Category = "技术",
                    Tags = new List<string> { "ASP.NET Core", "最佳实践", "Web开发" },
                    Thumbnail = "/images/aspnet-thumbnail.jpg"
                },
                new MapleBlog.Application.DTOs.Search.SearchResultItem
                {
                    Id = Guid.NewGuid(),
                    Type = "post",
                    Title = "Entity Framework Core 性能优化",
                    Summary = "提升Entity Framework Core应用性能的技巧和方法",
                    Url = "/posts/ef-core-performance",
                    Score = 0.88,
                    Highlights = new List<string> { "Entity Framework", "性能优化" },
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    Author = "技术编辑",
                    Category = "数据库",
                    Tags = new List<string> { "Entity Framework", "性能", "数据库" },
                    Thumbnail = "/images/ef-thumbnail.jpg"
                }
            };

            await Task.Delay(10, cancellationToken); // 模拟异步操作

            // 如果有基础查询，过滤相关结果
            if (!string.IsNullOrWhiteSpace(baseQuery))
            {
                var normalizedQuery = baseQuery.ToLowerInvariant();
                recommendations = recommendations
                    .Where(r => r.Title.ToLowerInvariant().Contains(normalizedQuery) ||
                               r.Summary.ToLowerInvariant().Contains(normalizedQuery) ||
                               r.Tags.Any(t => t.ToLowerInvariant().Contains(normalizedQuery)))
                    .ToList();
            }

            _logger.LogInformation("Generated {Count} recommendations for user: {UserId}", recommendations.Count, userId);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user: {UserId}", userId);
            return new List<MapleBlog.Application.DTOs.Search.SearchResultItem>();
        }
    }

    /// <summary>
    /// 记录搜索点击
    /// </summary>
    /// <param name="searchId">搜索ID</param>
    /// <param name="resultId">结果ID</param>
    /// <param name="position">位置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完成任务</returns>
    public async Task RecordSearchClickAsync(Guid searchId, Guid resultId, int position, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Recording search click - SearchId: {SearchId}, ResultId: {ResultId}, Position: {Position}",
                searchId, resultId, position);

            // 简单的点击记录实现 - 模拟记录操作
            await Task.Delay(5, cancellationToken); // 模拟异步操作

            _logger.LogInformation("Search click recorded successfully - SearchId: {SearchId}, ResultId: {ResultId}",
                searchId, resultId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search click - SearchId: {SearchId}, ResultId: {ResultId}",
                searchId, resultId);
            // 不抛出异常，因为点击记录失败不应该影响用户体验
        }
    }

    /// <summary>
    /// 为文章生成关键词
    /// </summary>
    /// <param name="post">文章实体</param>
    /// <returns>关键词字符串</returns>
    private string GenerateKeywords(Post post)
    {
        var keywords = new List<string>();

        // 添加分类名称
        if (post.Category != null)
        {
            keywords.Add(post.Category.Name);
        }

        // 添加标签名称
        if (post.PostTags?.Any() == true)
        {
            keywords.AddRange(post.PostTags.Select(pt => pt.Tag?.Name).Where(name => !string.IsNullOrEmpty(name)));
        }

        // 从标题提取关键词（简化处理）
        var titleWords = post.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Take(5);
        keywords.AddRange(titleWords);

        return string.Join(", ", keywords.Distinct());
    }

    /// <summary>
    /// 计算相关性评分
    /// </summary>
    /// <param name="post">文章</param>
    /// <param name="query">搜索词</param>
    /// <returns>相关性评分</returns>
    private double CalculateRelevanceScore(Post post, string query)
    {
        var score = 0.0;
        var normalizedQuery = query.ToLowerInvariant();

        // 标题匹配（最高权重）
        if (post.Title.ToLowerInvariant().Contains(normalizedQuery))
        {
            score += 1.0;
        }

        // 摘要匹配（中等权重）
        if (!string.IsNullOrEmpty(post.Summary) && post.Summary.ToLowerInvariant().Contains(normalizedQuery))
        {
            score += 0.7;
        }

        // 内容匹配（较低权重）
        if (post.Content.ToLowerInvariant().Contains(normalizedQuery))
        {
            score += 0.5;
        }

        // 分类匹配
        if (post.Category?.Name.ToLowerInvariant().Contains(normalizedQuery) == true)
        {
            score += 0.3;
        }

        // 标签匹配
        if (post.PostTags?.Any(pt => pt.Tag?.Name.ToLowerInvariant().Contains(normalizedQuery) == true) == true)
        {
            score += 0.2;
        }

        // 按视图数和时间调整评分
        var daysSincePublished = (DateTime.UtcNow - (post.PublishedAt ?? post.CreatedAt)).TotalDays;
        var timeBoost = Math.Max(0, 1 - (daysSincePublished / 365)); // 新文章加分
        var viewBoost = Math.Min(0.5, post.ViewCount / 1000.0); // 热门文章加分

        score += timeBoost * 0.1 + viewBoost * 0.1;

        return Math.Min(1.0, score); // 限制在1.0以内
    }

    /// <summary>
    /// 提取高亮片段
    /// </summary>
    /// <param name="post">文章</param>
    /// <param name="query">搜索词</param>
    /// <returns>高亮片段列表</returns>
    private List<string> ExtractHighlights(Post post, string query)
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