using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using System.Text.RegularExpressions;

namespace MapleBlog.Application.Services;

/// <summary>
/// 评论服务实现
/// </summary>
public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICommentLikeRepository _commentLikeRepository;
    private readonly ICommentReportRepository _commentReportRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUserInteractionRepository _userInteractionRepository;
    private readonly ICommentCacheService _cacheService;
    private readonly IAIContentModerationService _moderationService;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentService> _logger;
    private readonly IMemoryCache _memoryCache;

    private const int MaxCommentDepth = 10;
    private const int CommentContentMaxLength = 2000;

    public CommentService(
        ICommentRepository commentRepository,
        ICommentLikeRepository commentLikeRepository,
        ICommentReportRepository commentReportRepository,
        IPostRepository postRepository,
        IUserInteractionRepository userInteractionRepository,
        ICommentCacheService cacheService,
        IAIContentModerationService moderationService,
        IMapper mapper,
        ILogger<CommentService> logger,
        IMemoryCache memoryCache)
    {
        _commentRepository = commentRepository;
        _commentLikeRepository = commentLikeRepository;
        _commentReportRepository = commentReportRepository;
        _postRepository = postRepository;
        _userInteractionRepository = userInteractionRepository;
        _cacheService = cacheService;
        _moderationService = moderationService;
        _mapper = mapper;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    #region 基础CRUD操作

    /// <summary>
    /// 创建评论
    /// </summary>
    public async Task<CommentDto> CreateCommentAsync(CommentCreateDto request, Guid authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 验证文章存在
            var post = await _postRepository.GetByIdAsync(request.PostId);
            if (post == null || !post.IsPublished)
            {
                throw new InvalidOperationException("文章不存在或不允许评论");
            }

            // 验证父评论（如果是回复）
            Comment? parentComment = null;
            if (request.ParentId.HasValue)
            {
                parentComment = await _commentRepository.GetByIdAsync(request.ParentId.Value);
                if (parentComment == null || parentComment.PostId != request.PostId || !parentComment.IsPubliclyVisible())
                {
                    throw new InvalidOperationException("父评论不存在或不可回复");
                }

                // 检查回复深度
                if (parentComment.GetDepth() >= MaxCommentDepth)
                {
                    throw new InvalidOperationException($"回复深度不能超过{MaxCommentDepth}层");
                }
            }

            // 创建评论内容值对象
            var content = CommentContent.Create(request.Content);

            // 创建评论实体
            var comment = new Comment
            {
                PostId = request.PostId,
                AuthorId = authorId,
                ParentId = request.ParentId,
                Content = content,
                ThreadPath = parentComment?.ThreadPath.CreateChildPath(Guid.NewGuid()) ??
                            ThreadPath.CreateRoot(Guid.NewGuid()),
                Status = CommentStatus.Pending,
                IpAddress = request.ClientInfo?.IpAddress,
                UserAgent = request.ClientInfo?.UserAgent
            };

            // AI内容审核
            var moderationResult = await _moderationService.ModerateCommentAsync(
                content, authorId, request.ClientInfo?.IpAddress, cancellationToken);

            comment.SetAIModerationResult(
                moderationResult.IsApproved ? ModerationResult.Approved : ModerationResult.RequiresHumanReview,
                moderationResult.Confidence,
                moderationResult.SensitiveWords.Any());

            // 保存评论
            await _commentRepository.AddAsync(comment);
            await _commentRepository.SaveChangesAsync();

            // 更新父评论的回复计数
            if (parentComment != null)
            {
                // 这里应该更新父评论及其所有祖先的回复数
                await UpdateParentReplyCountAsync(parentComment, 1);
            }

            // 清理相关缓存
            await _cacheService.RemovePostCommentsAsync(request.PostId);

            // 映射到DTO
            var commentDto = await MapToCommentDtoAsync(comment, authorId);

            _logger.LogInformation("User {AuthorId} created comment {CommentId} on post {PostId}",
                authorId, comment.Id, request.PostId);

            return commentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for user {AuthorId} on post {PostId}",
                authorId, request.PostId);
            throw;
        }
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    public async Task<CommentDto> UpdateCommentAsync(Guid commentId, CommentUpdateDto request, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                throw new InvalidOperationException("评论不存在");
            }

            // 检查权限
            if (!comment.CanBeEditedBy(userId, isAdmin))
            {
                throw new UnauthorizedAccessException("没有权限编辑此评论");
            }

            // 更新内容
            var newContent = CommentContent.Create(request.Content);
            comment.Content = newContent;

            // 重新进行AI审核
            var moderationResult = await _moderationService.ModerateCommentAsync(
                newContent, comment.AuthorId, comment.IpAddress, cancellationToken);

            comment.SetAIModerationResult(
                moderationResult.IsApproved ? ModerationResult.Approved : ModerationResult.RequiresHumanReview,
                moderationResult.Confidence,
                moderationResult.SensitiveWords.Any());

            // 如果是管理员编辑且原状态不是已批准，则直接批准
            if (isAdmin && comment.Status != CommentStatus.Approved)
            {
                comment.Status = CommentStatus.Approved;
                comment.ModeratedAt = DateTime.UtcNow;
                comment.ModeratedBy = userId;
            }

            comment.UpdateAuditFields(userId);
            await _commentRepository.SaveChangesAsync();

            // 清理缓存
            await _cacheService.RemoveCommentAsync(commentId);
            await _cacheService.RemovePostCommentsAsync(comment.PostId);

            var commentDto = await MapToCommentDtoAsync(comment, userId);

            _logger.LogInformation("Comment {CommentId} updated by user {UserId}", commentId, userId);

            return commentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId} by user {UserId}", commentId, userId);
            throw;
        }
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted)
            {
                return false;
            }

            // 检查权限
            if (!comment.CanBeDeletedBy(userId, isAdmin))
            {
                throw new UnauthorizedAccessException("没有权限删除此评论");
            }

            // 软删除评论
            comment.SoftDelete(userId);

            // 递归软删除所有子评论
            await SoftDeleteChildCommentsAsync(commentId, userId);

            // 更新父评论的回复计数
            if (comment.ParentId.HasValue)
            {
                var parentComment = await _commentRepository.GetByIdAsync(comment.ParentId.Value);
                if (parentComment != null)
                {
                    await UpdateParentReplyCountAsync(parentComment, -1);
                }
            }

            await _commentRepository.SaveChangesAsync();

            // 清理缓存
            await _cacheService.RemoveCommentAsync(commentId);
            await _cacheService.RemovePostCommentsAsync(comment.PostId);

            _logger.LogInformation("Comment {CommentId} deleted by user {UserId}", commentId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId} by user {UserId}", commentId, userId);
            throw;
        }
    }

    /// <summary>
    /// 获取单个评论
    /// </summary>
    public async Task<CommentDto?> GetCommentAsync(Guid commentId, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 先尝试从缓存获取
            var cached = await _cacheService.GetCommentAsync(commentId);
            if (cached != null)
            {
                return await MapToCommentDtoAsync(cached, userId);
            }

            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null || comment.IsDeleted)
            {
                return null;
            }

            // 缓存评论
            await _cacheService.SetCommentAsync(comment);

            return await MapToCommentDtoAsync(comment, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment {CommentId}", commentId);
            return null;
        }
    }

    #endregion

    #region 评论列表查询

    /// <summary>
    /// 获取评论列表
    /// </summary>
    public async Task<CommentPagedResultDto> GetCommentsAsync(CommentQueryDto query, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 尝试从缓存获取
            var cacheKey = $"comments:post:{query.PostId}:page:{query.Page}:size:{query.PageSize}:sort:{query.SortOrder}";
            if (query.ParentId.HasValue)
            {
                cacheKey += $":parent:{query.ParentId}";
            }

            var cached = await _cacheService.GetCommentPageAsync(cacheKey);
            if (cached != null && cached is CommentPagedResultDto cachedResult)
            {
                return await EnrichCommentPageAsync(cachedResult, userId);
            }

            // 从数据库查询
            var queryable = await _commentRepository.GetCommentsQueryableAsync(query.PostId);

            // 应用排序
            queryable = ApplyCommentSorting(queryable, query.SortOrder);

            // 分页
            var totalCount = await queryable.CountAsync(cancellationToken);
            var comments = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var result = new CommentPagedResultDto
            {
                Comments = await Task.WhenAll(comments.Select(c => MapToCommentDtoAsync(c, userId))),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                CurrentPage = query.Page,
                PageSize = query.PageSize,
                HasNextPage = query.Page * query.PageSize < totalCount,
                HasPreviousPage = query.Page > 1
            };

            // 缓存结果
            await _cacheService.SetCommentPageAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", query.PostId);
            throw;
        }
    }

    /// <summary>
    /// 获取文章的评论树
    /// </summary>
    public async Task<IList<CommentDto>> GetCommentTreeAsync(Guid postId, Guid? userId = null, int maxDepth = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"comment_tree:post:{postId}:depth:{maxDepth}";
            var cached = _memoryCache.Get<IList<CommentDto>>(cacheKey);
            if (cached != null)
            {
                return await EnrichCommentTreeAsync(cached, userId);
            }

            // 获取所有根评论
            var rootCommentsResult = await _commentRepository.GetRootCommentsAsync(postId);
            var rootComments = rootCommentsResult.Comments.Where(c => c.Status == CommentStatus.Approved);

            var commentTree = new List<CommentDto>();

            foreach (var rootComment in rootComments)
            {
                var commentDto = await BuildCommentTreeAsync(rootComment, userId, maxDepth);
                commentTree.Add(commentDto);
            }

            // 缓存结果
            _memoryCache.Set(cacheKey, commentTree, TimeSpan.FromMinutes(10));

            return commentTree;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment tree for post {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// 获取用户的评论列表
    /// </summary>
    public async Task<CommentPagedResultDto> GetUserCommentsAsync(Guid authorId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = await _commentRepository.GetUserCommentsQueryableAsync(authorId);

            var totalCount = await queryable.CountAsync(cancellationToken);
            var comments = await queryable
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new CommentPagedResultDto
            {
                Comments = await Task.WhenAll(comments.Select(c => MapToCommentDtoAsync(c, authorId))),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for user {AuthorId}", authorId);
            throw;
        }
    }

    /// <summary>
    /// 搜索评论
    /// </summary>
    public async Task<CommentPagedResultDto> SearchCommentsAsync(string keyword, Guid? postId = null, Guid? authorId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchResult = await _commentRepository.SearchCommentsAsync(keyword, postId, authorId, null, page, pageSize, cancellationToken);

            var totalCount = searchResult.TotalCount;
            var comments = searchResult.Comments.ToList();

            return new CommentPagedResultDto
            {
                Comments = await Task.WhenAll(comments.Select(c => MapToCommentDtoAsync(c))),
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching comments with keyword '{Keyword}'", keyword);
            throw;
        }
    }

    #endregion

    #region 评论互动

    /// <summary>
    /// 点赞评论
    /// </summary>
    public async Task<bool> LikeCommentAsync(Guid commentId, Guid userId, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null || !comment.IsPubliclyVisible())
            {
                return false;
            }

            // 检查是否已经点赞
            var existingLike = await _commentLikeRepository.GetUserLikeAsync(commentId, userId);
            if (existingLike != null && !existingLike.IsDeleted)
            {
                return false; // 已经点赞过
            }

            var like = comment.Like(userId, ipAddress);
            await _commentLikeRepository.AddAsync(like);
            await _commentRepository.SaveChangesAsync();

            // 记录用户互动
            await _userInteractionRepository.RecordInteractionAsync(userId, commentId, "comment_like");

            // 清理缓存
            await _cacheService.RemoveCommentAsync(commentId);

            _logger.LogInformation("User {UserId} liked comment {CommentId}", userId, commentId);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false; // 已经点赞过
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking comment {CommentId} by user {UserId}", commentId, userId);
            throw;
        }
    }

    /// <summary>
    /// 取消点赞评论
    /// </summary>
    public async Task<bool> UnlikeCommentAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            comment.Unlike(userId);
            await _commentRepository.SaveChangesAsync();

            // 清理缓存
            await _cacheService.RemoveCommentAsync(commentId);

            _logger.LogInformation("User {UserId} unliked comment {CommentId}", userId, commentId);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false; // 没有点赞过
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unliking comment {CommentId} by user {UserId}", commentId, userId);
            throw;
        }
    }

    /// <summary>
    /// 举报评论
    /// </summary>
    public async Task<bool> ReportCommentAsync(Guid commentId, Guid reporterId, CommentReportRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null || !comment.IsPubliclyVisible())
            {
                return false;
            }

            var report = comment.Report(reporterId, request.Reason, request.Description, ipAddress);
            await _commentReportRepository.AddAsync(report);
            await _commentRepository.SaveChangesAsync();

            _logger.LogInformation("User {ReporterId} reported comment {CommentId} for {Reason}",
                reporterId, commentId, request.Reason);

            return true;
        }
        catch (InvalidOperationException)
        {
            return false; // 已经举报过
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting comment {CommentId} by user {ReporterId}",
                commentId, reporterId);
            throw;
        }
    }

    #endregion

    #region 统计信息

    /// <summary>
    /// 获取文章评论统计
    /// </summary>
    public async Task<CommentStatsDto> GetCommentStatsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"comment_stats:post:{postId}";
            var cached = _memoryCache.Get<CommentStatsDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var stats = await _commentRepository.GetPostCommentStatsAsync(postId);

            // 缓存统计信息
            _memoryCache.Set(cacheKey, stats, TimeSpan.FromMinutes(15));

            return new CommentStatsDto
            {
                PostId = postId,
                TotalCount = stats.TotalComments,
                RootCommentCount = stats.RootComments,
                ReplyCount = stats.TotalComments - stats.RootComments,
                ParticipantCount = 0 // 需要计算参与用户数
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment stats for post {PostId}", postId);
            throw;
        }
    }

    /// <summary>
    /// 获取用户评论统计
    /// </summary>
    public async Task<UserCommentStatsDto> GetUserCommentStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _commentRepository.GetUserCommentStatsAsync(userId);
            return new UserCommentStatsDto
            {
                UserId = userId,
                TotalComments = stats.TotalComments,
                TotalLikes = 0, // 需要计算总点赞数
                TotalReplies = 0 // 需要计算总回复数
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment stats for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取热门评论
    /// </summary>
    public async Task<IList<CommentDto>> GetPopularCommentsAsync(Guid? postId = null, int timeRange = 7, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _commentRepository.GetPopularCommentsAsync(postId, timeRange, limit);
            return await Task.WhenAll(result.Comments.Select(c => MapToCommentDtoAsync(c)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular comments for post {PostId}", postId);
            throw;
        }
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 刷新评论缓存
    /// </summary>
    public async Task<bool> RefreshCommentCacheAsync(Guid commentId, bool cascadeRefresh = true, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveCommentAsync(commentId);

            if (cascadeRefresh)
            {
                var comment = await _commentRepository.GetByIdAsync(commentId);
                if (comment != null)
                {
                    await _cacheService.RemovePostCommentsAsync(comment.PostId);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache for comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// 预热文章评论缓存
    /// </summary>
    public async Task<int> WarmupPostCommentCacheAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comments = await _commentRepository.GetPostCommentsAsync(postId, cancellationToken);

            var tasks = comments.Select(async comment =>
            {
                await _cacheService.SetCommentAsync(comment);
            });

            await Task.WhenAll(tasks);

            return comments.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up cache for post {PostId}", postId);
            return 0;
        }
    }

    /// <summary>
    /// 清理过期缓存
    /// </summary>
    public async Task<int> CleanupExpiredCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.CleanupExpiredCacheAsync(cancellationToken);
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired cache");
            return 0;
        }
    }

    #endregion

    #region 内容处理

    /// <summary>
    /// 渲染评论内容
    /// </summary>
    public async Task<string> RenderCommentContentAsync(string rawContent, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该实现Markdown渲染、HTML清理、表情符号转换等
            await Task.Delay(1, cancellationToken); // 模拟异步操作

            // 简单的处理：HTML转义 + 换行转换
            var rendered = System.Web.HttpUtility.HtmlEncode(rawContent);
            rendered = rendered.Replace("\n", "<br>");

            // 处理@提及
            rendered = ProcessMentions(rendered);

            return rendered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering comment content");
            return System.Web.HttpUtility.HtmlEncode(rawContent);
        }
    }

    /// <summary>
    /// 提取内容中的提及用户
    /// </summary>
    public async Task<IList<Guid>> ExtractMentionedUsersAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var mentionPattern = @"@(\w+)";
            var matches = Regex.Matches(content, mentionPattern);
            var usernames = matches.Select(m => m.Groups[1].Value).Distinct();

            // 根据用户名查找用户ID
            var userIds = new List<Guid>();
            foreach (var username in usernames)
            {
                // 这里应该查询用户服务获取用户ID
                // 暂时跳过实现
                await Task.Delay(1, cancellationToken);
            }

            return userIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting mentioned users from content");
            return new List<Guid>();
        }
    }

    /// <summary>
    /// 检查内容是否需要审核
    /// </summary>
    public async Task<bool> ShouldModerationAsync(string content, Guid authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var moderationResult = await _moderationService.ModerateContentAsync(content, cancellationToken);
            return await _moderationService.RequiresHumanReviewAsync(content, moderationResult, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if content requires moderation");
            return true; // 保守处理，需要审核
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 映射评论实体到DTO
    /// </summary>
    private async Task<CommentDto> MapToCommentDtoAsync(Comment comment, Guid? currentUserId = null)
    {
        var dto = _mapper.Map<CommentDto>(comment);

        // 渲染内容
        dto = dto with { RenderedContent = await RenderCommentContentAsync(comment.Content.RawContent) };

        // 设置权限信息
        if (currentUserId.HasValue)
        {
            dto = dto with
            {
                IsLiked = comment.Likes.Any(l => l.UserId == currentUserId && !l.IsDeleted),
                CanEdit = comment.CanBeEditedBy(currentUserId.Value),
                CanDelete = comment.CanBeDeletedBy(currentUserId.Value)
            };
        }

        return dto;
    }

    /// <summary>
    /// 递归构建评论树
    /// </summary>
    private async Task<CommentDto> BuildCommentTreeAsync(Comment comment, Guid? userId, int maxDepth, int currentDepth = 0)
    {
        var commentDto = await MapToCommentDtoAsync(comment, userId);

        if (currentDepth < maxDepth && comment.Replies.Any())
        {
            var replyTasks = comment.Replies
                .Where(r => r.IsPubliclyVisible())
                .OrderBy(r => r.CreatedAt)
                .Select(r => BuildCommentTreeAsync(r, userId, maxDepth, currentDepth + 1));

            var replies = await Task.WhenAll(replyTasks);
            commentDto = commentDto with { Replies = replies.ToList() };
        }

        return commentDto;
    }

    /// <summary>
    /// 应用评论排序
    /// </summary>
    private IQueryable<Comment> ApplyCommentSorting(IQueryable<Comment> queryable, CommentSortOrder sortOrder)
    {
        return sortOrder switch
        {
            CommentSortOrder.CreatedAtAsc => queryable.OrderBy(c => c.CreatedAt),
            CommentSortOrder.CreatedAtDesc => queryable.OrderByDescending(c => c.CreatedAt),
            CommentSortOrder.LikeCountDesc => queryable.OrderByDescending(c => c.LikeCount).ThenByDescending(c => c.CreatedAt),
            CommentSortOrder.ReplyCountDesc => queryable.OrderByDescending(c => c.ReplyCount).ThenByDescending(c => c.CreatedAt),
            CommentSortOrder.HotScore => queryable.OrderByDescending(c =>
                (c.LikeCount * 2 + c.ReplyCount) *
                Math.Exp(-Math.Abs((DateTime.UtcNow - c.CreatedAt).TotalHours) / 24.0)
            ).ThenByDescending(c => c.CreatedAt),
            _ => queryable.OrderByDescending(c => c.CreatedAt)
        };
    }

    /// <summary>
    /// 更新父评论回复计数
    /// </summary>
    private async Task UpdateParentReplyCountAsync(Comment parentComment, int delta)
    {
        var current = parentComment;
        while (current != null)
        {
            if (delta > 0)
            {
                current.ReplyCount++;
            }
            else if (current.ReplyCount > 0)
            {
                current.ReplyCount--;
            }

            current.UpdateAuditFields();

            if (current.ParentId.HasValue)
            {
                current = await _commentRepository.GetByIdAsync(current.ParentId.Value);
            }
            else
            {
                current = null;
            }
        }
    }

    /// <summary>
    /// 软删除子评论
    /// </summary>
    private async Task SoftDeleteChildCommentsAsync(Guid parentId, Guid userId)
    {
        var childComments = await _commentRepository.GetChildCommentsAsync(parentId);

        foreach (var child in childComments.Where(c => !c.IsDeleted))
        {
            child.SoftDelete(userId);
            await SoftDeleteChildCommentsAsync(child.Id, userId);
        }
    }

    /// <summary>
    /// 处理@提及
    /// </summary>
    private string ProcessMentions(string content)
    {
        var mentionPattern = @"@(\w+)";
        return Regex.Replace(content, mentionPattern, "<span class=\"mention\">@$1</span>");
    }

    /// <summary>
    /// 丰富评论页面数据
    /// </summary>
    private async Task<CommentPagedResultDto> EnrichCommentPageAsync(CommentPagedResultDto page, Guid? userId)
    {
        if (userId.HasValue)
        {
            var enrichedComments = await Task.WhenAll(
                page.Comments.Select(async c =>
                {
                    var comment = await _commentRepository.GetByIdAsync(c.Id);
                    return comment != null ? await MapToCommentDtoAsync(comment, userId) : c;
                })
            );

            return page with { Comments = enrichedComments };
        }

        return page;
    }

    /// <summary>
    /// 丰富评论树数据
    /// </summary>
    private async Task<IList<CommentDto>> EnrichCommentTreeAsync(IList<CommentDto> tree, Guid? userId)
    {
        if (userId.HasValue)
        {
            var enrichedComments = await Task.WhenAll(
                tree.Select(async c =>
                {
                    var comment = await _commentRepository.GetByIdAsync(c.Id);
                    return comment != null ? await MapToCommentDtoAsync(comment, userId) : c;
                })
            );

            return enrichedComments;
        }

        return tree;
    }

    #endregion
}