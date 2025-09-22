using System.Text;
using MapleBlog.Domain.Entities;
using MapleBlog.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 数据库搜索服务实现
/// </summary>
public class DatabaseSearchService : IDatabaseSearchService
{
    private readonly ILogger<DatabaseSearchService> _logger;
    private readonly DbContext _context;

    public DatabaseSearchService(
        ILogger<DatabaseSearchService> logger,
        DbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 搜索文章
    /// </summary>
    public async Task<(IEnumerable<Post> Posts, int TotalCount)> SearchPostsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (new List<Post>(), 0);
            }

            var searchTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var postsQuery = _context.Set<Post>().Where(p => p.IsPublished);

            // 搜索标题和内容
            foreach (var term in searchTerms)
            {
                var searchTerm = term;
                postsQuery = postsQuery.Where(p =>
                    p.Title.Contains(searchTerm) ||
                    p.Content.Contains(searchTerm) ||
                    (p.Summary != null && p.Summary.Contains(searchTerm)));
            }

            var totalCount = await postsQuery.CountAsync();
            var posts = await postsQuery
                .Include(p => p.Category)
                .Include(p => p.Tags)
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (posts, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts with query: {Query}", query);
            return (new List<Post>(), 0);
        }
    }

    /// <summary>
    /// 搜索用户
    /// </summary>
    public async Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (new List<User>(), 0);
            }

            var searchTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var usersQuery = _context.Set<User>().Where(u => u.IsActive);

            // 搜索用户名、邮箱、显示名称
            foreach (var term in searchTerms)
            {
                var searchTerm = term;
                usersQuery = usersQuery.Where(u =>
                    u.UserName.Contains(searchTerm) ||
                    u.Email.Value.Contains(searchTerm) ||
                    (u.DisplayName != null && u.DisplayName.Contains(searchTerm)));
            }

            var totalCount = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderBy(u => u.UserName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query: {Query}", query);
            return (new List<User>(), 0);
        }
    }

    /// <summary>
    /// 搜索评论
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchCommentsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return (new List<Comment>(), 0);
            }

            var searchTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commentsQuery = _context.Set<Comment>().Where(c => c.IsApproved);

            // 搜索评论内容
            foreach (var term in searchTerms)
            {
                var searchTerm = term;
                commentsQuery = commentsQuery.Where(c =>
                    c.Content.RawContent.Contains(searchTerm) ||
                    c.Content.ProcessedContent.Contains(searchTerm));
            }

            var totalCount = await commentsQuery.CountAsync();
            var comments = await commentsQuery
                .Include(c => c.Post)
                .Include(c => c.Author)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (comments, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching comments with query: {Query}", query);
            return (new List<Comment>(), 0);
        }
    }

    /// <summary>
    /// 全文搜索
    /// </summary>
    public async Task<object> GlobalSearchAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new
                {
                    Posts = new List<Post>(),
                    Users = new List<User>(),
                    Comments = new List<Comment>(),
                    TotalCount = 0
                };
            }

            // 限制每个类型的结果数量
            var limitPerType = Math.Max(1, pageSize / 3);

            var (posts, postsCount) = await SearchPostsAsync(query, 1, limitPerType);
            var (users, usersCount) = await SearchUsersAsync(query, 1, limitPerType);
            var (comments, commentsCount) = await SearchCommentsAsync(query, 1, limitPerType);

            return new
            {
                Posts = posts,
                Users = users,
                Comments = comments,
                TotalCount = postsCount + usersCount + commentsCount,
                Counts = new
                {
                    Posts = postsCount,
                    Users = usersCount,
                    Comments = commentsCount
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing global search with query: {Query}", query);
            return new
            {
                Posts = new List<Post>(),
                Users = new List<User>(),
                Comments = new List<Comment>(),
                TotalCount = 0
            };
        }
    }

}