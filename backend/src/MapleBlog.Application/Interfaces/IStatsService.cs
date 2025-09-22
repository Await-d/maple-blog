using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Statistics service interface for calculating and caching website metrics
    /// </summary>
    public interface IStatsService
    {
        /// <summary>
        /// Gets comprehensive site statistics with caching
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Site statistics DTO</returns>
        Task<SiteStatsDto> GetSiteStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets author statistics for the authors widget
        /// </summary>
        /// <param name="count">Number of authors to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Active author summaries</returns>
        Task<IReadOnlyList<AuthorSummaryDto>> GetActiveAuthorsAsync(int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets category statistics with post counts
        /// </summary>
        /// <param name="includeEmpty">Whether to include categories with no posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category summaries with statistics</returns>
        Task<IReadOnlyList<CategorySummaryDto>> GetCategoryStatsAsync(bool includeEmpty = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tag statistics for tag cloud generation
        /// </summary>
        /// <param name="count">Maximum number of tags to return</param>
        /// <param name="minUsage">Minimum usage count to include</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag summaries with usage statistics</returns>
        Task<IReadOnlyList<TagSummaryDto>> GetTagStatsAsync(int count = 50, int minUsage = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets post engagement statistics for trending analysis
        /// </summary>
        /// <param name="daysBack">Number of days to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Post engagement statistics</returns>
        Task<IReadOnlyList<DTOs.PostSummaryDto>> GetTrendingPostsAsync(int daysBack = 7, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh all cached statistics (background job)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task RefreshCachedStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Increment view count for a post (real-time)
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task IncrementPostViewAsync(Guid postId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increment like count for a post (real-time)
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task IncrementPostLikeAsync(Guid postId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update comment count for a post (real-time)
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="delta">Change in comment count (can be negative)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task UpdatePostCommentCountAsync(Guid postId, int delta, CancellationToken cancellationToken = default);

        // 扩展方法支持管理后台仪表盘

        /// <summary>
        /// 获取总页面浏览量
        /// </summary>
        /// <returns>总浏览量</returns>
        Task<long> GetTotalPageViewsAsync();

        /// <summary>
        /// 获取指定时间范围内的页面浏览量
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>浏览量</returns>
        Task<long> GetPageViewsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取独立访客数
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>独立访客数</returns>
        Task<int> GetUniqueVisitorsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取小时统计数据
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>小时统计数据</returns>
        Task<IEnumerable<DTOs.Admin.HourlyStatsDto>> GetHourlyStatsAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 获取日访问统计
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>日访问统计</returns>
        Task<IEnumerable<DTOs.Admin.DailyVisitStatsDto>> GetDailyVisitStatsAsync(DateTime startDate, DateTime endDate);
    }
}