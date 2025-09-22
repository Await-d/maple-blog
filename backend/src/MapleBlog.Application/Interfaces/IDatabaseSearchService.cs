using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 数据库搜索服务接口
/// </summary>
public interface IDatabaseSearchService
{
    /// <summary>
    /// 搜索文章
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>搜索结果</returns>
    Task<(IEnumerable<Post> Posts, int TotalCount)> SearchPostsAsync(string query, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 搜索用户
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>搜索结果</returns>
    Task<(IEnumerable<User> Users, int TotalCount)> SearchUsersAsync(string query, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 搜索评论
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>搜索结果</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchCommentsAsync(string query, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 全文搜索
    /// </summary>
    /// <param name="query">搜索关键词</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>混合搜索结果</returns>
    Task<object> GlobalSearchAsync(string query, int pageNumber = 1, int pageSize = 10);
}