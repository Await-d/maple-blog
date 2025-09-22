using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Moderation;
using MapleBlog.Application.Interfaces;
using MapleBlog.API.Hubs;
using MapleBlog.Infrastructure.Services;

namespace MapleBlog.API.Controllers;

/// <summary>
/// 评论审核管理控制器
/// </summary>
[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin,Moderator")]
[Produces("application/json")]
public class CommentModerationController : ControllerBase
{
    private readonly ICommentModerationService _moderationService;
    private readonly ICommentNotificationService _notificationService;
    private readonly IHubContext<CommentHub> _commentHub;
    private readonly ILogger<CommentModerationController> _logger;

    public CommentModerationController(
        ICommentModerationService moderationService,
        ICommentNotificationService notificationService,
        IHubContext<CommentHub> commentHub,
        ILogger<CommentModerationController> logger)
    {
        _moderationService = moderationService;
        _notificationService = notificationService;
        _commentHub = commentHub;
        _logger = logger;
    }

    #region 审核队列管理

    /// <summary>
    /// 获取审核队列
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核队列分页结果</returns>
    /// <response code="200">获取成功</response>
    /// <response code="400">查询参数无效</response>
    /// <response code="401">未授权</response>
    /// <response code="403">权限不足</response>
    [HttpGet("moderation-queue")]
    [ProducesResponseType(typeof(CommentPagedResultDto<CommentModerationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommentPagedResultDto<CommentModerationDto>>> GetModerationQueue(
        [FromQuery] CommentModerationQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _moderationService.GetModerationQueueAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation queue");
            return StatusCode(500, new { message = "获取审核队列时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取单个待审核评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核评论详情</returns>
    /// <response code="200">获取成功</response>
    /// <response code="404">评论不存在</response>
    [HttpGet("moderation/{commentId}")]
    [ProducesResponseType(typeof(CommentModerationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentModerationDto>> GetModerationComment(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _moderationService.GetModerationCommentAsync(commentId, cancellationToken);
            if (comment == null)
            {
                return NotFound(new { message = "评论不存在" });
            }

            return Ok(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation comment {CommentId}", commentId);
            return StatusCode(500, new { message = "获取审核评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取审核统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核统计</returns>
    /// <response code="200">获取成功</response>
    [HttpGet("moderation-stats")]
    [ProducesResponseType(typeof(CommentModerationStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentModerationStatsDto>> GetModerationStats(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _moderationService.GetModerationStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation stats");
            return StatusCode(500, new { message = "获取审核统计时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 审核操作

    /// <summary>
    /// 执行批量审核操作
    /// </summary>
    /// <param name="request">审核操作请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <response code="200">操作成功</response>
    /// <response code="400">请求参数无效</response>
    /// <response code="401">未授权</response>
    /// <response code="403">权限不足</response>
    [HttpPost("moderate")]
    [ProducesResponseType(typeof(ModerationOperationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModerationOperationResult>> ModerateComments(
        [FromBody] CommentModerationActionDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var moderatorId = GetCurrentUserId();
            if (!moderatorId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _moderationService.ModerateCommentsAsync(request, moderatorId.Value, cancellationToken);

            // 异步广播审核结果
            _ = Task.Run(async () =>
            {
                try
                {
                    await BroadcastModerationResult(request.CommentIds, request.Action);

                    // 发送通知
                    foreach (var commentId in request.CommentIds)
                    {
                        var approved = request.Action == Domain.Enums.ModerationAction.Approve;
                        await _notificationService.HandleCommentModeratedAsync(
                            commentId, moderatorId.Value, approved, request.Note, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing moderation notifications");
                }
            }, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating comments");
            return StatusCode(500, new { message = "审核操作时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 批准评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <response code="200">操作成功</response>
    /// <response code="404">评论不存在</response>
    [HttpPost("{commentId}/approve")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveComment(Guid commentId, [FromBody] string? note = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var moderatorId = GetCurrentUserId();
            if (!moderatorId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _moderationService.ApproveCommentAsync(commentId, moderatorId.Value, note, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "评论不存在或无法操作" });
            }

            // 异步处理通知和广播
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.HandleCommentModeratedAsync(commentId, moderatorId.Value, true, note, cancellationToken);
                    await BroadcastCommentApproved(commentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing comment approval notifications for comment {CommentId}", commentId);
                }
            }, cancellationToken);

            return Ok(new { message = "评论已批准" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving comment {CommentId}", commentId);
            return StatusCode(500, new { message = "批准评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 拒绝评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="reason">拒绝原因</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    /// <response code="200">操作成功</response>
    /// <response code="400">参数无效</response>
    /// <response code="404">评论不存在</response>
    [HttpPost("{commentId}/reject")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectComment(
        Guid commentId,
        [FromBody] RejectCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var moderatorId = GetCurrentUserId();
            if (!moderatorId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _moderationService.RejectCommentAsync(commentId, moderatorId.Value, request.Reason, request.Note, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "评论不存在或无法操作" });
            }

            // 异步处理通知和广播
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.HandleCommentModeratedAsync(commentId, moderatorId.Value, false, request.Note, cancellationToken);
                    await BroadcastCommentRejected(commentId, request.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing comment rejection notifications for comment {CommentId}", commentId);
                }
            }, cancellationToken);

            return Ok(new { message = "评论已拒绝" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting comment {CommentId}", commentId);
            return StatusCode(500, new { message = "拒绝评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 隐藏评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("{commentId}/hide")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HideComment(Guid commentId, [FromBody] string? note = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var moderatorId = GetCurrentUserId();
            if (!moderatorId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _moderationService.HideCommentAsync(commentId, moderatorId.Value, note, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "评论不存在或无法操作" });
            }

            await BroadcastCommentHidden(commentId);

            return Ok(new { message = "评论已隐藏" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding comment {CommentId}", commentId);
            return StatusCode(500, new { message = "隐藏评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 恢复评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("{commentId}/restore")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreComment(Guid commentId, [FromBody] string? note = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var moderatorId = GetCurrentUserId();
            if (!moderatorId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _moderationService.RestoreCommentAsync(commentId, moderatorId.Value, note, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "评论不存在或无法操作" });
            }

            await BroadcastCommentRestored(commentId);

            return Ok(new { message = "评论已恢复" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring comment {CommentId}", commentId);
            return StatusCode(500, new { message = "恢复评论时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 标记为垃圾信息
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("{commentId}/spam")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsSpam(Guid commentId, [FromBody] string? note = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var moderatorId = GetCurrentUserId();
            if (!moderatorId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _moderationService.MarkAsSpamAsync(commentId, moderatorId.Value, note, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "评论不存在或无法操作" });
            }

            await BroadcastCommentMarkedAsSpam(commentId);

            return Ok(new { message = "评论已标记为垃圾信息" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking comment {CommentId} as spam", commentId);
            return StatusCode(500, new { message = "标记垃圾信息时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 举报管理

    /// <summary>
    /// 获取举报列表
    /// </summary>
    /// <param name="status">举报状态过滤</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报分页结果</returns>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(CommentPagedResultDto<CommentReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentPagedResultDto<CommentReportDto>>> GetReports(
        [FromQuery] Domain.Enums.CommentReportStatus[] status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "页码必须大于0，每页大小必须在1-100之间" });
            }

            if (!status.Any())
            {
                status = [Domain.Enums.CommentReportStatus.Pending];
            }

            var result = await _moderationService.GetReportsAsync(status, page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports");
            return StatusCode(500, new { message = "获取举报列表时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取单个举报详情
    /// </summary>
    /// <param name="reportId">举报ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报详情</returns>
    [HttpGet("reports/{reportId}")]
    [ProducesResponseType(typeof(CommentReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentReportDto>> GetReport(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _moderationService.GetReportAsync(reportId, cancellationToken);
            if (report == null)
            {
                return NotFound(new { message = "举报不存在" });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", reportId);
            return StatusCode(500, new { message = "获取举报详情时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 处理举报
    /// </summary>
    /// <param name="request">处理举报请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    [HttpPost("reports/process")]
    [ProducesResponseType(typeof(ReportProcessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportProcessResult>> ProcessReports(
        [FromBody] CommentReportProcessDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var processorId = GetCurrentUserId();
            if (!processorId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _moderationService.ProcessReportsAsync(request, processorId.Value, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reports");
            return StatusCode(500, new { message = "处理举报时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region AI审核

    /// <summary>
    /// 重新进行AI审核
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI审核结果</returns>
    [HttpPost("{commentId}/ai-moderate")]
    [ProducesResponseType(typeof(AIContentModerationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AIContentModerationResult>> RerunAIModeration(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _moderationService.RerunAIModerationAsync(commentId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound(new { message = "评论不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rerunning AI moderation for comment {CommentId}", commentId);
            return StatusCode(500, new { message = "AI审核时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 批量AI审核
    /// </summary>
    /// <param name="request">批量审核请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量审核结果</returns>
    [HttpPost("batch-ai-moderate")]
    [ProducesResponseType(typeof(BatchModerationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchModerationResult>> BatchAIModeration(
        [FromBody] BatchAIModerationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.CommentIds.Count > 50)
            {
                return BadRequest(new { message = "一次最多只能处理50条评论" });
            }

            var result = await _moderationService.BatchAIModerationAsync(request.CommentIds, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch AI moderation");
            return StatusCode(500, new { message = "批量AI审核时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取AI审核设置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核设置</returns>
    [HttpGet("moderation-settings")]
    [ProducesResponseType(typeof(ModerationSettings), StatusCodes.Status200OK)]
    public async Task<ActionResult<ModerationSettings>> GetModerationSettings(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _moderationService.GetModerationSettingsAsync(cancellationToken);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation settings");
            return StatusCode(500, new { message = "获取审核设置时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 更新AI审核设置
    /// </summary>
    /// <param name="settings">审核设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新结果</returns>
    [HttpPut("moderation-settings")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateModerationSettings(
        [FromBody] ModerationSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _moderationService.UpdateModerationSettingsAsync(settings, cancellationToken);
            if (!success)
            {
                return StatusCode(500, new { message = "更新设置失败" });
            }

            return Ok(new { message = "审核设置已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating moderation settings");
            return StatusCode(500, new { message = "更新审核设置时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 用户管理

    /// <summary>
    /// 获取用户审核统计
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户审核统计</returns>
    [HttpGet("users/{userId}/stats")]
    [ProducesResponseType(typeof(UserModerationStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserModerationStatsDto>> GetUserModerationStats(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _moderationService.GetUserModerationStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user moderation stats for {UserId}", userId);
            return StatusCode(500, new { message = "获取用户审核统计时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 设置用户信任度
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">设置信任度请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>设置结果</returns>
    [HttpPost("users/{userId}/trust-score")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetUserTrustScore(
        Guid userId,
        [FromBody] SetTrustScoreRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var operatorId = GetCurrentUserId();
            if (!operatorId.HasValue)
            {
                return Unauthorized();
            }

            var success = await _moderationService.SetUserTrustScoreAsync(
                userId, request.TrustScore, request.Reason, operatorId.Value, cancellationToken);

            if (!success)
            {
                return StatusCode(500, new { message = "设置信任度失败" });
            }

            return Ok(new { message = "用户信任度已更新" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting trust score for user {UserId}", userId);
            return StatusCode(500, new { message = "设置用户信任度时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取用户信任度历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>信任度历史</returns>
    [HttpGet("users/{userId}/trust-score-history")]
    [ProducesResponseType(typeof(IList<UserTrustScoreHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<UserTrustScoreHistoryDto>>> GetUserTrustScoreHistory(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _moderationService.GetUserTrustScoreHistoryAsync(userId, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trust score history for user {UserId}", userId);
            return StatusCode(500, new { message = "获取信任度历史时发生错误，请稍后重试" });
        }
    }

    #endregion

    #region 审核历史

    /// <summary>
    /// 获取评论审核历史
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核历史</returns>
    [HttpGet("{commentId}/history")]
    [ProducesResponseType(typeof(IList<CommentModerationHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IList<CommentModerationHistoryDto>>> GetModerationHistory(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var history = await _moderationService.GetModerationHistoryAsync(commentId, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation history for comment {CommentId}", commentId);
            return StatusCode(500, new { message = "获取审核历史时发生错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取审核者操作历史
    /// </summary>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作历史</returns>
    [HttpGet("moderators/{moderatorId}/history")]
    [ProducesResponseType(typeof(CommentPagedResultDto<CommentModerationHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentPagedResultDto<CommentModerationHistoryDto>>> GetModeratorHistory(
        Guid moderatorId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "页码必须大于0，每页大小必须在1-100之间" });
            }

            var history = await _moderationService.GetModeratorHistoryAsync(
                moderatorId, startDate, endDate, page, pageSize, cancellationToken);

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderator history for {ModeratorId}", moderatorId);
            return StatusCode(500, new { message = "获取审核者历史时发生错误，请稍后重试" });
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
    /// 广播审核结果
    /// </summary>
    private async Task BroadcastModerationResult(IList<Guid> commentIds, Domain.Enums.ModerationAction action)
    {
        try
        {
            await _commentHub.Clients.Group("moderators")
                .SendAsync("CommentsModerated", new { commentIds, action });

            _logger.LogDebug("Broadcasted moderation result for {Count} comments with action {Action}",
                commentIds.Count, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting moderation result");
        }
    }

    /// <summary>
    /// 广播评论批准事件
    /// </summary>
    private async Task BroadcastCommentApproved(Guid commentId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentApproved", new { commentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment approved event for comment {CommentId}", commentId);
        }
    }

    /// <summary>
    /// 广播评论拒绝事件
    /// </summary>
    private async Task BroadcastCommentRejected(Guid commentId, Domain.Enums.ModerationAction reason)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentRejected", new { commentId, reason });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment rejected event for comment {CommentId}", commentId);
        }
    }

    /// <summary>
    /// 广播评论隐藏事件
    /// </summary>
    private async Task BroadcastCommentHidden(Guid commentId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentHidden", new { commentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment hidden event for comment {CommentId}", commentId);
        }
    }

    /// <summary>
    /// 广播评论恢复事件
    /// </summary>
    private async Task BroadcastCommentRestored(Guid commentId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentRestored", new { commentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment restored event for comment {CommentId}", commentId);
        }
    }

    /// <summary>
    /// 广播评论标记为垃圾信息事件
    /// </summary>
    private async Task BroadcastCommentMarkedAsSpam(Guid commentId)
    {
        try
        {
            await _commentHub.Clients.All.SendAsync("CommentMarkedAsSpam", new { commentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting comment spam event for comment {CommentId}", commentId);
        }
    }

    #endregion
}

/// <summary>
/// 拒绝评论请求
/// </summary>
public record RejectCommentRequest
{
    /// <summary>
    /// 拒绝原因
    /// </summary>
    public Domain.Enums.ModerationAction Reason { get; init; }

    /// <summary>
    /// 审核备注
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// 批量AI审核请求
/// </summary>
public record BatchAIModerationRequest
{
    /// <summary>
    /// 评论ID列表
    /// </summary>
    public IList<Guid> CommentIds { get; init; } = new List<Guid>();
}

/// <summary>
/// 设置信任度请求
/// </summary>
public record SetTrustScoreRequest
{
    /// <summary>
    /// 信任度评分 (0.0-1.0)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "信任度必须在0.0-1.0之间")]
    public double TrustScore { get; init; }

    /// <summary>
    /// 设置原因
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "原因不能超过500字符")]
    public string Reason { get; init; } = string.Empty;
}