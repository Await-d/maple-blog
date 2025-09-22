using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 评论仓储接口
/// 提供评论的数据访问和复杂查询功能
/// </summary>
public interface ICommentRepository : IRepository<Comment>
{
    /// <summary>
    /// 根据文章ID获取评论列表
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="includeReplies">是否包含回复</param>
    /// <param name="onlyApproved">是否只返回已审核的评论</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论列表</returns>
    Task<IEnumerable<Comment>> GetByPostIdAsync(
        Guid postId,
        bool includeReplies = true,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取文章评论
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="onlyApproved">是否只返回已审核的评论</param>
    /// <param name="sortBy">排序方式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页评论结果</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetPagedByPostIdAsync(
        Guid postId,
        int pageNumber = 1,
        int pageSize = 20,
        bool onlyApproved = true,
        CommentSortBy sortBy = CommentSortBy.CreatedAtDesc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定评论的直接回复
    /// </summary>
    /// <param name="parentId">父评论ID</param>
    /// <param name="onlyApproved">是否只返回已审核的评论</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回复列表</returns>
    Task<IEnumerable<Comment>> GetRepliesAsync(
        Guid parentId,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定评论的所有后代回复（递归）
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="maxDepth">最大深度</param>
    /// <param name="onlyApproved">是否只返回已审核的评论</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有后代回复</returns>
    Task<IEnumerable<Comment>> GetDescendantsAsync(
        Guid commentId,
        int? maxDepth = null,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据线程路径获取评论
    /// </summary>
    /// <param name="threadPath">线程路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论</returns>
    Task<Comment?> GetByThreadPathAsync(
        ThreadPath threadPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的评论列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="onlyApproved">是否只返回已审核的评论</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户评论列表</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待审核的评论列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>待审核评论列表</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetPendingModerationAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取被举报的评论列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="minReportCount">最小举报数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被举报评论列表</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetReportedCommentsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        int minReportCount = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索评论
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <param name="status">评论状态（可选）</param>
    /// <param name="dateFrom">开始日期（可选）</param>
    /// <param name="dateTo">结束日期（可选）</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchAsync(
        string keyword,
        Guid? postId = null,
        Guid? userId = null,
        CommentStatus? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论的完整线程
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整线程（从根到叶子）</returns>
    Task<IEnumerable<Comment>> GetThreadAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取热门评论
    /// </summary>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="days">统计天数</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热门评论列表</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetPopularCommentsAsync(
        Guid? postId = null,
        int days = 7,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最新评论
    /// </summary>
    /// <param name="count">获取数量</param>
    /// <param name="onlyApproved">是否只返回已审核的评论</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新评论列表</returns>
    Task<IEnumerable<Comment>> GetLatestCommentsAsync(
        int count = 10,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计评论数据
    /// </summary>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <param name="dateFrom">开始日期（可选）</param>
    /// <param name="dateTo">结束日期（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计结果</returns>
    Task<CommentStatistics> GetStatisticsAsync(
        Guid? postId = null,
        Guid? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新评论状态
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="status">新状态</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="reason">原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新数量</returns>
    Task<int> BatchUpdateStatusAsync(
        IEnumerable<Guid> commentIds,
        CommentStatus status,
        Guid moderatorId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建评论的线程路径
    /// </summary>
    /// <param name="postId">文章ID（可选，为空则重建所有）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重建的评论数量</returns>
    Task<int> RebuildThreadPathsAsync(
        Guid? postId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否对评论有权限
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="action">操作类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有权限</returns>
    Task<bool> CheckPermissionAsync(
        Guid commentId,
        Guid userId,
        CommentAction action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取评论（包含详细信息）
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论详细信息</returns>
    Task<Comment?> GetByIdWithDetailsAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论查询接口
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论查询接口</returns>
    Task<IQueryable<Comment>> GetCommentsQueryableAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID列表获取评论
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论列表</returns>
    Task<IEnumerable<Comment>> GetByIdsAsync(IEnumerable<Guid> commentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取根级评论（非回复）
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>根级评论</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> GetRootCommentsAsync(Guid postId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户评论查询接口
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户评论查询接口</returns>
    Task<IQueryable<Comment>> GetUserCommentsQueryableAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索评论
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="userId">用户ID（可选）</param>
    /// <param name="status">状态（可选）</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchCommentsAsync(
        string keyword,
        Guid? postId = null,
        Guid? userId = null,
        CommentStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文章评论统计
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论统计</returns>
    Task<CommentStatistics> GetPostCommentStatsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户评论统计
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户评论统计</returns>
    Task<CommentStatistics> GetUserCommentStatsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取查询接口
    /// </summary>
    /// <returns>评论查询接口</returns>
    IQueryable<Comment> GetQueryable();

    /// <summary>
    /// 获取文章评论
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论列表</returns>
    Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取子评论
    /// </summary>
    /// <param name="parentId">父评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子评论列表</returns>
    Task<IEnumerable<Comment>> GetChildCommentsAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按状态统计评论数量
    /// </summary>
    /// <param name="status">评论状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数量</returns>
    Task<int> CountByStatusAsync(CommentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计被举报评论数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数量</returns>
    Task<int> CountReportedCommentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按日期统计评论数量
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数量</returns>
    Task<int> CountByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取周度审核统计
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核统计</returns>
    Task<object> GetWeeklyModerationStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取AI审核统计
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI审核统计</returns>
    Task<object> GetAIModerationStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户审核统计
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户审核统计</returns>
    Task<object> GetUserModerationStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}


/// <summary>
/// 评论统计信息
/// </summary>
public record CommentStatistics
{
    /// <summary>
    /// 总评论数
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// 已发布评论数
    /// </summary>
    public int PublishedComments { get; init; }

    /// <summary>
    /// 待审核评论数
    /// </summary>
    public int PendingComments { get; init; }

    /// <summary>
    /// 垃圾评论数
    /// </summary>
    public int SpamComments { get; init; }

    /// <summary>
    /// 已隐藏评论数
    /// </summary>
    public int HiddenComments { get; init; }

    /// <summary>
    /// 根评论数
    /// </summary>
    public int RootComments { get; init; }

    /// <summary>
    /// 回复数
    /// </summary>
    public int ReplyComments { get; init; }

    /// <summary>
    /// 平均每篇文章评论数
    /// </summary>
    public double AverageCommentsPerPost { get; init; }

    /// <summary>
    /// 平均评论长度
    /// </summary>
    public double AverageCommentLength { get; init; }

    /// <summary>
    /// 最深嵌套层级
    /// </summary>
    public int MaxNestingLevel { get; init; }

    /// <summary>
    /// 活跃评论者数量
    /// </summary>
    public int ActiveCommenters { get; init; }

    /// <summary>
    /// 总点赞数
    /// </summary>
    public int TotalLikes { get; init; }

    /// <summary>
    /// 总举报数
    /// </summary>
    public int TotalReports { get; init; }
}