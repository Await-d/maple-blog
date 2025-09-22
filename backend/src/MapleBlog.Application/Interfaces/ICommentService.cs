using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 评论服务接口
/// </summary>
public interface ICommentService
{
    #region 基础CRUD操作

    /// <summary>
    /// 创建评论
    /// </summary>
    /// <param name="request">创建评论请求</param>
    /// <param name="authorId">作者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的评论</returns>
    Task<CommentDto> CreateCommentAsync(CommentCreateDto request, Guid authorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="request">更新评论请求</param>
    /// <param name="userId">用户ID</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的评论</returns>
    Task<CommentDto> UpdateCommentAsync(Guid commentId, CommentUpdateDto request, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除</returns>
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">当前用户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论详情</returns>
    Task<CommentDto?> GetCommentAsync(Guid commentId, Guid? userId = null, CancellationToken cancellationToken = default);

    #endregion

    #region 评论列表查询

    /// <summary>
    /// 获取评论列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="userId">当前用户ID（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论分页结果</returns>
    Task<CommentPagedResultDto> GetCommentsAsync(CommentQueryDto query, Guid? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文章的评论树
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="userId">当前用户ID（可选）</param>
    /// <param name="maxDepth">最大深度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论树结构</returns>
    Task<IList<CommentDto>> GetCommentTreeAsync(Guid postId, Guid? userId = null, int maxDepth = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的评论列表
    /// </summary>
    /// <param name="authorId">作者ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论分页结果</returns>
    Task<CommentPagedResultDto> GetUserCommentsAsync(Guid authorId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索评论
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="authorId">作者ID（可选）</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<CommentPagedResultDto> SearchCommentsAsync(string keyword, Guid? postId = null, Guid? authorId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    #endregion

    #region 评论互动

    /// <summary>
    /// 点赞评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功点赞</returns>
    Task<bool> LikeCommentAsync(Guid commentId, Guid userId, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消点赞评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功取消点赞</returns>
    Task<bool> UnlikeCommentAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 举报评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="request">举报请求</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功举报</returns>
    Task<bool> ReportCommentAsync(Guid commentId, Guid reporterId, CommentReportRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);

    #endregion

    #region 统计信息

    /// <summary>
    /// 获取文章评论统计
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论统计信息</returns>
    Task<CommentStatsDto> GetCommentStatsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户评论统计
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户评论统计</returns>
    Task<UserCommentStatsDto> GetUserCommentStatsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取热门评论
    /// </summary>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="timeRange">时间范围（天数）</param>
    /// <param name="limit">数量限制</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热门评论列表</returns>
    Task<IList<CommentDto>> GetPopularCommentsAsync(Guid? postId = null, int timeRange = 7, int limit = 10, CancellationToken cancellationToken = default);

    #endregion

    #region 缓存管理

    /// <summary>
    /// 刷新评论缓存
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cascadeRefresh">是否级联刷新相关缓存</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功刷新</returns>
    Task<bool> RefreshCommentCacheAsync(Guid commentId, bool cascadeRefresh = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 预热文章评论缓存
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>预热的评论数量</returns>
    Task<int> WarmupPostCommentCacheAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的缓存数量</returns>
    Task<int> CleanupExpiredCacheAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 内容处理

    /// <summary>
    /// 渲染评论内容
    /// </summary>
    /// <param name="rawContent">原始内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染后的HTML内容</returns>
    Task<string> RenderCommentContentAsync(string rawContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提取内容中的提及用户
    /// </summary>
    /// <param name="content">评论内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被提及的用户ID列表</returns>
    Task<IList<Guid>> ExtractMentionedUsersAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查内容是否需要审核
    /// </summary>
    /// <param name="content">评论内容</param>
    /// <param name="authorId">作者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否需要审核</returns>
    Task<bool> ShouldModerationAsync(string content, Guid authorId, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// 用户评论统计
/// </summary>
public record UserCommentStatsDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 总评论数
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// 获得的总点赞数
    /// </summary>
    public int TotalLikes { get; init; }

    /// <summary>
    /// 获得的总回复数
    /// </summary>
    public int TotalReplies { get; init; }

    /// <summary>
    /// 平均点赞数
    /// </summary>
    public double AverageLikes { get; init; }

    /// <summary>
    /// 最受欢迎的评论
    /// </summary>
    public CommentDto? MostPopularComment { get; init; }

    /// <summary>
    /// 最近评论时间
    /// </summary>
    public DateTime? LastCommentAt { get; init; }

    /// <summary>
    /// 评论活跃度（最近30天）
    /// </summary>
    public int RecentActivity { get; init; }

    /// <summary>
    /// 按状态分组的评论数
    /// </summary>
    public IDictionary<CommentStatus, int> CommentsByStatus { get; init; } = new Dictionary<CommentStatus, int>();
}