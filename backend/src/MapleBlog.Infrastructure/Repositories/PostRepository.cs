using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Post repository implementation with Entity Framework Core
    /// </summary>
    public class PostRepository : BlogBaseRepository<Post>, IPostRepository
    {
        public PostRepository(BlogDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Post>> GetPostsWithDetailsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            return await GetQueryWithDetails()
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            return await GetQueryWithDetails()
                .FirstOrDefaultAsync(p => p.Slug == slug.ToLowerInvariant(), cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetByAuthorAsync(
            Guid authorId,
            int pageNumber = 1,
            int pageSize = 10,
            PostStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.AuthorId == authorId);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetByCategoryAsync(
            Guid categoryId,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.CategoryId == categoryId);

            if (publishedOnly)
                query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            return await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetByTagAsync(
            Guid tagId,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.PostTags.Any(pt => pt.TagId == tagId));

            if (publishedOnly)
                query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            return await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetByTagsAsync(
            IEnumerable<Guid> tagIds,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default)
        {
            var tagIdList = tagIds.ToList();
            if (!tagIdList.Any())
                return new List<Post>();

            var query = GetQueryWithDetails()
                .Where(p => tagIdList.All(tagId => p.PostTags.Any(pt => pt.TagId == tagId)));

            if (publishedOnly)
                query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            return await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> SearchAsync(
            string searchTerm,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Post>();

            var query = GetQueryWithDetails()
                .Where(p =>
                    EF.Functions.Like(p.Title, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.Content, $"%{searchTerm}%") ||
                    EF.Functions.Like(p.Summary, $"%{searchTerm}%"));

            if (publishedOnly)
                query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            return await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetPublishedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            bool orderDescending = true,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            query = orderDescending
                ? query.OrderByDescending(p => p.PublishedAt)
                : query.OrderBy(p => p.PublishedAt);

            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetFeaturedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.IsFeatured);

            if (publishedOnly)
                query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            return await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetMostPopularAsync(
            int pageNumber = 1,
            int pageSize = 10,
            int? daysBack = null,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            if (daysBack.HasValue)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysBack.Value);
                query = query.Where(p => p.PublishedAt >= cutoffDate);
            }

            return await query
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.LikeCount)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetRecentAsync(
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails();

            if (publishedOnly)
                query = query.Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetRelatedAsync(
            Guid postId,
            int count = 5,
            CancellationToken cancellationToken = default)
        {
            var currentPost = await _dbSet
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

            if (currentPost == null)
                return new List<Post>();

            var query = GetQueryWithDetails()
                .Where(p => p.Id != postId && p.Status == PostStatus.Published);

            // Priority 1: Same category
            if (currentPost.CategoryId.HasValue)
            {
                var sameCategoryPosts = await query
                    .Where(p => p.CategoryId == currentPost.CategoryId)
                    .OrderByDescending(p => p.PublishedAt)
                    .Take(count)
                    .ToListAsync(cancellationToken);

                if (sameCategoryPosts.Count >= count)
                    return sameCategoryPosts;

                count -= sameCategoryPosts.Count;
                query = query.Where(p => p.CategoryId != currentPost.CategoryId);
            }

            // Priority 2: Shared tags
            var currentTagIds = currentPost.PostTags.Select(pt => pt.TagId).ToList();
            if (currentTagIds.Any())
            {
                var sharedTagsPosts = await query
                    .Where(p => p.PostTags.Any(pt => currentTagIds.Contains(pt.TagId)))
                    .OrderByDescending(p => p.PostTags.Count(pt => currentTagIds.Contains(pt.TagId)))
                    .ThenByDescending(p => p.PublishedAt)
                    .Take(count)
                    .ToListAsync(cancellationToken);

                return sharedTagsPosts;
            }

            // Fallback: Recent posts
            return await query
                .OrderByDescending(p => p.PublishedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Post>> GetScheduledForPublicationAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(p => p.Status == PostStatus.Scheduled &&
                           p.ScheduledAt.HasValue &&
                           p.ScheduledAt <= now)
                .ToListAsync(cancellationToken);
        }

        public async Task<PostStatistics> GetStatisticsAsync(Guid? authorId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();

            if (authorId.HasValue)
                query = query.Where(p => p.AuthorId == authorId.Value);

            var totalPosts = await query.CountAsync(cancellationToken);
            var publishedPosts = await query.CountAsync(p => p.Status == PostStatus.Published, cancellationToken);
            var draftPosts = await query.CountAsync(p => p.Status == PostStatus.Draft, cancellationToken);
            var privatePosts = await query.CountAsync(p => p.Status == PostStatus.Private, cancellationToken);
            var scheduledPosts = await query.CountAsync(p => p.Status == PostStatus.Scheduled, cancellationToken);
            var archivedPosts = await query.CountAsync(p => p.Status == PostStatus.Archived, cancellationToken);
            var featuredPosts = await query.CountAsync(p => p.IsFeatured, cancellationToken);

            var totalViews = await query.SumAsync(p => p.ViewCount, cancellationToken);
            var totalLikes = await query.SumAsync(p => p.LikeCount, cancellationToken);
            var totalComments = await query.SumAsync(p => p.CommentCount, cancellationToken);

            var lastPostDate = await query
                .Where(p => p.Status == PostStatus.Published)
                .MaxAsync(p => (DateTime?)p.PublishedAt, cancellationToken);

            var mostViewedPost = await query
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.ViewCount)
                .Include(p => p.Author)
                .FirstOrDefaultAsync(cancellationToken);

            var mostLikedPost = await query
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.LikeCount)
                .Include(p => p.Author)
                .FirstOrDefaultAsync(cancellationToken);

            return new PostStatistics
            {
                TotalPosts = totalPosts,
                PublishedPosts = publishedPosts,
                DraftPosts = draftPosts,
                PrivatePosts = privatePosts,
                ScheduledPosts = scheduledPosts,
                ArchivedPosts = archivedPosts,
                FeaturedPosts = featuredPosts,
                TotalViews = totalViews,
                TotalLikes = totalLikes,
                TotalComments = totalComments,
                LastPostDate = lastPostDate,
                MostViewedPost = mostViewedPost,
                MostLikedPost = mostLikedPost
            };
        }

        public async Task<IReadOnlyDictionary<DateTime, IReadOnlyList<Post>>> GetArchiveAsync(
            int? year = null,
            int? month = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Post> query = _dbSet
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue)
                .Include(p => p.Author);

            if (year.HasValue)
                query = query.Where(p => p.PublishedAt!.Value.Year == year.Value);

            if (month.HasValue)
                query = query.Where(p => p.PublishedAt!.Value.Month == month.Value);

            var posts = await query
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync(cancellationToken);

            return posts
                .GroupBy(p => new DateTime(p.PublishedAt!.Value.Year, p.PublishedAt!.Value.Month, 1))
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Post>)g.ToList());
        }

        public async Task<bool> IsSlugAvailableAsync(
            string slug,
            Guid? excludePostId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var query = _dbSet.Where(p => p.Slug == slug.ToLowerInvariant());

            if (excludePostId.HasValue)
                query = query.Where(p => p.Id != excludePostId.Value);

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<int> BulkUpdateStatusAsync(
            IEnumerable<Guid> postIds,
            PostStatus status,
            CancellationToken cancellationToken = default)
        {
            var postIdList = postIds.ToList();
            if (!postIdList.Any())
                return 0;

            var posts = await _dbSet
                .Where(p => postIdList.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var post in posts)
            {
                switch (status)
                {
                    case PostStatus.Published:
                        post.Publish();
                        break;
                    case PostStatus.Draft:
                        post.Unpublish();
                        break;
                    case PostStatus.Private:
                        post.SetPrivate();
                        break;
                    case PostStatus.Archived:
                        post.Archive();
                        break;
                }
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets a query with commonly included related data
        /// </summary>
        private IQueryable<Post> GetQueryWithDetails()
        {
            return _dbSet
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
                .AsSplitQuery(); // Use split queries for performance with multiple includes
        }

        /// <summary>
        /// 获取第一篇发布的文章
        /// </summary>
        public async Task<Post?> GetFirstPublishedAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue)
                .OrderBy(p => p.PublishedAt)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 获取所有文章的总阅读时间
        /// </summary>
        public async Task<int> GetTotalReadingTimeAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.Status == PostStatus.Published && p.ReadingTime.HasValue)
                .SumAsync(p => p.ReadingTime ?? 0, cancellationToken);
        }

        /// <summary>
        /// 获取作者的总浏览量
        /// </summary>
        public async Task<int> GetAuthorTotalViewsAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.AuthorId == authorId && p.Status == PostStatus.Published)
                .SumAsync(p => p.ViewCount, cancellationToken);
        }

        /// <summary>
        /// 获取作者最后一篇文章的发布日期
        /// </summary>
        public async Task<DateTime?> GetAuthorLastPostDateAsync(Guid authorId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.AuthorId == authorId && p.Status == PostStatus.Published && p.PublishedAt.HasValue)
                .MaxAsync(p => (DateTime?)p.PublishedAt, cancellationToken);
        }

        /// <summary>
        /// 获取所有已发布的文章
        /// </summary>
        public async Task<IEnumerable<Post>> GetAllPublishedAsync(CancellationToken cancellationToken = default)
        {
            return await GetQueryWithDetails()
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取最受欢迎的文章（按浏览量排序）
        /// </summary>
        public async Task<IEnumerable<Post>> GetMostViewedPostsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await GetQueryWithDetails()
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue)
                .OrderByDescending(p => p.ViewCount)
                .ThenByDescending(p => p.PublishedAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetTotalViewsAsync(System.Linq.Expressions.Expression<Func<Post, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.AsQueryable();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            return await query.SumAsync(p => (long)p.ViewCount, cancellationToken);
        }

        public async Task<IEnumerable<Post>> GetPublishedPostsAsync(int pageNumber, int pageSize, Guid? categoryId = null, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var query = GetQueryWithDetails()
                .Where(p => p.Status == PostStatus.Published && p.PublishedAt.HasValue);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PublishedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.PublishedAt <= endDate.Value);
            }

            return await query
                .OrderByDescending(p => p.PublishedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }
    }
}