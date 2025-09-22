using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.API.Hubs;

namespace MapleBlog.API.Controllers;

/// <summary>
/// 评论控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ICommentNotificationService _notificationService;
    private readonly IHubContext<CommentHub> _commentHub;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(
        ICommentService commentService,
        ICommentNotificationService notificationService,
        IHubContext<CommentHub> commentHub,
        ILogger<CommentsController> logger)
    {
        _commentService = commentService;
        _notificationService = notificationService;
        _commentHub = commentHub;
        _logger = logger;
    }

    #region 评论CRUD操作

    /// <summary>
    /// 创建评论
    /// </summary>
    /// <param name="request">评论创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的评论</returns>
    /// <response code="201">评论创建成功</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="401">未授权</response>
    /// <response code="429">请求频率过高</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CommentCreateDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // 设置客户端信息
            var clientInfo = new CommentClientInfoDto
            {
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                Referer = Request.Headers.Referer.ToString()
            };

            var requestWithClientInfo = request with { ClientInfo = clientInfo };

            var comment = await _commentService.CreateCommentAsync(requestWithClientInfo, userId.Value, cancellationToken);

            // 异步处理通知和实时推送
            _ = Task.Run(async () =>
            {
                try
                {
                    // 发送通知
                    await _notificationService.HandleCommentCreatedAsync(comment.Id, cancellationToken);

                    // 实时推送
                    await BroadcastCommentCreated(comment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing comment notifications for comment {CommentId}", comment.Id);
                }
            }, cancellationToken);

            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating comment: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating comment: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating comment for user {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "创建评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的评论</returns>
    /// <response code="200">评论更新成功</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权限操作</response>
    /// <response code="404">评论不存在</response>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> UpdateComment(Guid id, [FromBody] CommentUpdateDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var isAdmin = IsCurrentUserAdmin();
            var comment = await _commentService.UpdateCommentAsync(id, request, userId.Value, isAdmin, cancellationToken);

            // 实时推送更新
            await BroadcastCommentUpdated(comment);

            return Ok(comment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Comment {CommentId} not found for update", id);
            return NotFound(new { message = "评论不存在" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} unauthorized to update comment {CommentId}", GetCurrentUserId(), id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return StatusCode(500, new { message = "更新评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    /// <response code="204">评论删除成功</response>
    /// <response code="401">未授权</response>
    /// <response code="403">无权限操作</response>
    /// <response code="404">评论不存在</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var isAdmin = IsCurrentUserAdmin();
            var deleted = await _commentService.DeleteCommentAsync(id, userId.Value, isAdmin, cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "评论不存在或已被删除" });
            }

            // 异步处理通知和实时推送
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.HandleCommentDeletedAsync(id, userId.Value, cancellationToken);
                    await BroadcastCommentDeleted(id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing comment deletion notifications for comment {CommentId}", id);
                }
            }, cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} unauthorized to delete comment {CommentId}", GetCurrentUserId(), id);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return StatusCode(500, new { message = "删除评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取单个评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论详情</returns>
    /// <response code="200">获取成功</response>
    /// <response code="404">评论不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> GetComment(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var comment = await _commentService.GetCommentAsync(id, userId, cancellationToken);

            if (comment == null)
            {
                return NotFound(new { message = "评论不存在" });
            }

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment {CommentId}", id);
            return StatusCode(500, new { message = "获取评论时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 评论列表查询

    /// <summary>
    /// 获取评论列表
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论分页结果</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">查询参数无效</response>
    [HttpGet]
    [ProducesResponseType(typeof(CommentPagedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentPagedResultDto>> GetComments([FromQuery] CommentQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var result = await _commentService.GetCommentsAsync(query, userId, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for post {PostId}", query.PostId);
            return StatusCode(500, new { message = "获取评论列表时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取文章的评论树
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="maxDepth">最大深度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论树结构</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">参数无效</response>
    [HttpGet("tree/{postId}")]
    [ProducesResponseType(typeof(IList<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IList<CommentDto>>> GetCommentTree(Guid postId, [FromQuery] int maxDepth = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            if (maxDepth < 1 || maxDepth > 10)
            {
                return BadRequest(new { message = "最大深度必须在1-10之间" });
            }

            var userId = GetCurrentUserId();
            var commentTree = await _commentService.GetCommentTreeAsync(postId, userId, maxDepth, cancellationToken);

            return Ok(commentTree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment tree for post {PostId}", postId);
            return StatusCode(500, new { message = "获取评论树时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取用户的评论列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户评论分页结果</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">参数无效</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(CommentPagedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentPagedResultDto>> GetUserComments(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "页码必须大于0，每页大小必须在1-100之间" });
            }

            var result = await _commentService.GetUserCommentsAsync(userId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments for user {UserId}", userId);
            return StatusCode(500, new { message = "获取用户评论时发生错误，请稍后重试" });
        }
    }

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
    /// <response code="200">搜索成功</response>
    /// <response code="400">参数无效</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(CommentPagedResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentPagedResultDto>> SearchComments(
        [FromQuery] string keyword,
        [FromQuery] Guid? postId = null,
        [FromQuery] Guid? authorId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                return BadRequest(new { message = "关键词长度不能少于2个字符" });
            }

            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "页码必须大于0，每页大小必须在1-100之间" });
            }

            var result = await _commentService.SearchCommentsAsync(keyword, postId, authorId, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching comments with keyword '{Keyword}'", keyword);
            return StatusCode(500, new { message = "搜索评论时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 评论互动

    /// <summary>
    /// 点赞评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>点赞结果</returns>
    /// <response code="200">点赞成功</response>
    /// <response code="400">重复点赞或其他错误</response>
    /// <response code="401">未授权</response>
    /// <response code="404">评论不存在</response>
    [HttpPost("{id}/like")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LikeComment(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _commentService.LikeCommentAsync(id, userId.Value, GetClientIpAddress(), cancellationToken);

            if (!success)
            {
                return BadRequest(new { message = "点赞失败，可能是重复点赞或评论不存在" });
            }

            // 异步处理通知和实时推送
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.HandleCommentLikedAsync(id, userId.Value, cancellationToken);
                    await BroadcastCommentLiked(id, userId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing comment like notifications for comment {CommentId}", id);
                }
            }, cancellationToken);

            return Ok(new { message = "点赞成功" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid like operation for comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return StatusCode(500, new { message = "点赞时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 取消点赞评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>取消点赞结果</returns>
    /// <response code="200">取消点赞成功</response>
    /// <response code="400">未点赞或其他错误</response>
    /// <response code="401">未授权</response>
    /// <response code="404">评论不存在</response>
    [HttpDelete("{id}/like")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlikeComment(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _commentService.UnlikeCommentAsync(id, userId.Value, cancellationToken);

            if (!success)
            {
                return BadRequest(new { message = "取消点赞失败，可能是未点赞或评论不存在" });
            }

            // 实时推送取消点赞
            await BroadcastCommentUnliked(id, userId.Value);

            return Ok(new { message = "取消点赞成功" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid unlike operation for comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unliking comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return StatusCode(500, new { message = "取消点赞时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 举报评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="request">举报请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报结果</returns>
    /// <response code="200">举报成功</response>
    /// <response code="400">举报失败或参数无效</response>
    /// <response code="401">未授权</response>
    /// <response code="404">评论不存在</response>
    [HttpPost("{id}/report")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportComment(Guid id, [FromBody] CommentReportRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _commentService.ReportCommentAsync(id, userId.Value, request, GetClientIpAddress(), cancellationToken);

            if (!success)
            {
                return BadRequest(new { message = "举报失败，可能是重复举报或评论不存在" });
            }

            return Ok(new { message = "举报成功，我们会尽快处理" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid report operation for comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting comment {CommentId} by user {UserId}", id, GetCurrentUserId());
            return StatusCode(500, new { message = "举报时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 统计信息

    /// <summary>
    /// 获取文章评论统计
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论统计信息</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">参数无效</response>
    [HttpGet("stats/post/{postId}")]
    [ProducesResponseType(typeof(CommentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentStatsDto>> GetCommentStats(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _commentService.GetCommentStatsAsync(postId, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment stats for post {PostId}", postId);
            return StatusCode(500, new { message = "获取评论统计时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取用户评论统计
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户评论统计</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">参数无效</response>
    [HttpGet("stats/user/{userId}")]
    [ProducesResponseType(typeof(UserCommentStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserCommentStatsDto>> GetUserCommentStats(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _commentService.GetUserCommentStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment stats for user {UserId}", userId);
            return StatusCode(500, new { message = "获取用户评论统计时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取热门评论
    /// </summary>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="timeRange">时间范围（天数）</param>
    /// <param name="limit">数量限制</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热门评论列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">参数无效</response>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(IList<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IList<CommentDto>>> GetPopularComments(
        [FromQuery] Guid? postId = null,
        [FromQuery] int timeRange = 7,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (timeRange < 1 || timeRange > 365)
            {
                return BadRequest(new { message = "时间范围必须在1-365天之间" });
            }

            if (limit < 1 || limit > 100)
            {
                return BadRequest(new { message = "数量限制必须在1-100之间" });
            }

            var comments = await _commentService.GetPopularCommentsAsync(postId, timeRange, limit, cancellationToken);
            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular comments");
            return StatusCode(500, new { message = "获取热门评论时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 检查当前用户是否为管理员
    /// </summary>
    private bool IsCurrentUserAdmin()
    {
        return User.IsInRole("Admin") || User.IsInRole("Moderator");
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// 广播评论创建事件
    /// </summary>
    private async Task BroadcastCommentCreated(CommentDto comment)
    {
        try
        {
            await _commentHub.Clients.Group($"post_{comment.PostId}")
                .SendAsync("CommentCreated", comment);

            _logger.LogDebug("Broadcasted comment created event for comment {CommentId}", comment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment created event for comment {CommentId}", comment.Id);
        }
    }

    /// <summary>
    /// 广播评论更新事件
    /// </summary>
    private async Task BroadcastCommentUpdated(CommentDto comment)
    {
        try
        {
            await _commentHub.Clients.Group($"post_{comment.PostId}")
                .SendAsync("CommentUpdated", comment);

            _logger.LogDebug("Broadcasted comment updated event for comment {CommentId}", comment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment updated event for comment {CommentId}", comment.Id);
        }
    }

    /// <summary>
    /// 广播评论删除事件
    /// </summary>
    private async Task BroadcastCommentDeleted(Guid commentId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentDeleted", new { commentId });

            _logger.LogDebug("Broadcasted comment deleted event for comment {CommentId}", commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment deleted event for comment {CommentId}", commentId);
        }
    }

    /// <summary>
    /// 广播评论点赞事件
    /// </summary>
    private async Task BroadcastCommentLiked(Guid commentId, Guid userId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentLiked", new { commentId, userId });

            _logger.LogDebug("Broadcasted comment liked event for comment {CommentId}", commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment liked event for comment {CommentId}", commentId);
        }
    }

    /// <summary>
    /// 广播评论取消点赞事件
    /// </summary>
    private async Task BroadcastCommentUnliked(Guid commentId, Guid userId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentUnliked", new { commentId, userId });

            _logger.LogDebug("Broadcasted comment unliked event for comment {CommentId}", commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment unliked event for comment {CommentId}", commentId);
        }
    }

    #endregion
}