namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 首页缓存服务接口
/// </summary>
public interface IHomepageCacheService
{
    /// <summary>
    /// 获取首页缓存数据
    /// </summary>
    /// <returns>首页数据</returns>
    Task<object?> GetHomepageDataAsync();

    /// <summary>
    /// 设置首页缓存数据
    /// </summary>
    /// <param name="data">首页数据</param>
    /// <param name="expiration">过期时间</param>
    Task SetHomepageDataAsync(object data, TimeSpan? expiration = null);

    /// <summary>
    /// 清除首页缓存
    /// </summary>
    Task ClearHomepageCacheAsync();

    /// <summary>
    /// 获取热门文章缓存
    /// </summary>
    /// <returns>热门文章</returns>
    Task<object?> GetPopularPostsAsync();

    /// <summary>
    /// 设置热门文章缓存
    /// </summary>
    /// <param name="posts">热门文章</param>
    /// <param name="expiration">过期时间</param>
    Task SetPopularPostsAsync(object posts, TimeSpan? expiration = null);

    /// <summary>
    /// 获取最新文章缓存
    /// </summary>
    /// <returns>最新文章</returns>
    Task<object?> GetLatestPostsAsync();

    /// <summary>
    /// 设置最新文章缓存
    /// </summary>
    /// <param name="posts">最新文章</param>
    /// <param name="expiration">过期时间</param>
    Task SetLatestPostsAsync(object posts, TimeSpan? expiration = null);
}