using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.API.Hubs;

/// <summary>
/// 评论实时通信中心
/// </summary>
[Authorize]
public class CommentHub : Hub
{
    private readonly ICommentService _commentService;
    private readonly ICommentNotificationService _notificationService;
    private readonly ILogger<CommentHub> _logger;

    public CommentHub(
        ICommentService commentService,
        ICommentNotificationService notificationService,
        ILogger<CommentHub> logger)
    {
        _commentService = commentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    #region 连接管理

    /// <summary>
    /// 客户端连接时调用
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetCurrentUserId();
            var connectionId = Context.ConnectionId;

            if (userId.HasValue)
            {
                // 加入用户组，用于发送个人通知
                await Groups.AddToGroupAsync(connectionId, $"user_{userId.Value}");

                // 如果是管理员或审核员，加入审核组
                if (IsCurrentUserModerator())
                {
                    await Groups.AddToGroupAsync(connectionId, "moderators");
                }

                _logger.LogDebug("User {UserId} connected to CommentHub with connection {ConnectionId}",
                    userId.Value, connectionId);
            }
            else
            {
                _logger.LogWarning("Anonymous user connected to CommentHub with connection {ConnectionId}",
                    connectionId);
            }

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            throw;
        }
    }

    /// <summary>
    /// 客户端断开连接时调用
    /// </summary>
    /// <param name="exception">断开连接的异常（如果有）</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userId = GetCurrentUserId();
            var connectionId = Context.ConnectionId;

            if (exception != null)
            {
                _logger.LogWarning(exception, "User {UserId} disconnected from CommentHub with exception, connection {ConnectionId}",
                    userId, connectionId);
            }
            else
            {
                _logger.LogDebug("User {UserId} disconnected from CommentHub, connection {ConnectionId}",
                    userId, connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
        }
    }

    #endregion

    #region 文章评论组管理

    /// <summary>
    /// 加入文章评论组
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <returns>是否成功</returns>
    [HubMethodName("JoinPostGroup")]
    public async Task<bool> JoinPostGroup(string postId)
    {
        try
        {
            if (!Guid.TryParse(postId, out var parsedPostId))
            {
                await Clients.Caller.SendAsync("Error", "无效的文章ID");
                return false;
            }

            var groupName = $"post_{parsedPostId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} joined post group {GroupName}, connection {ConnectionId}",
                userId, groupName, Context.ConnectionId);

            await Clients.Caller.SendAsync("JoinedPostGroup", postId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining post group for post {PostId}", postId);
            await Clients.Caller.SendAsync("Error", "加入文章组失败");
            return false;
        }
    }

    /// <summary>
    /// 离开文章评论组
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <returns>是否成功</returns>
    [HubMethodName("LeavePostGroup")]
    public async Task<bool> LeavePostGroup(string postId)
    {
        try
        {
            if (!Guid.TryParse(postId, out var parsedPostId))
            {
                await Clients.Caller.SendAsync("Error", "无效的文章ID");
                return false;
            }

            var groupName = $"post_{parsedPostId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} left post group {GroupName}, connection {ConnectionId}",
                userId, groupName, Context.ConnectionId);

            await Clients.Caller.SendAsync("LeftPostGroup", postId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving post group for post {PostId}", postId);
            await Clients.Caller.SendAsync("Error", "离开文章组失败");
            return false;
        }
    }

    #endregion

    #region 评论实时操作

    /// <summary>
    /// 用户开始输入评论
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="parentId">父评论ID（如果是回复）</param>
    [HubMethodName("StartTyping")]
    public async Task StartTyping(string postId, string? parentId = null)
    {
        try
        {
            if (!Guid.TryParse(postId, out var parsedPostId))
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return;
            }

            var userName = GetCurrentUserName();
            var groupName = $"post_{parsedPostId}";

            var typingInfo = new
            {
                userId = userId.Value,
                userName = userName,
                postId = parsedPostId,
                parentId = !string.IsNullOrEmpty(parentId) && Guid.TryParse(parentId, out var pId) ? pId : (Guid?)null,
                timestamp = DateTime.UtcNow
            };

            // 通知同一文章组的其他用户（除了自己）
            await Clients.GroupExcept(groupName, Context.ConnectionId)
                .SendAsync("UserStartedTyping", typingInfo);

            _logger.LogDebug("User {UserId} started typing in post {PostId}", userId.Value, parsedPostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StartTyping for post {PostId}", postId);
        }
    }

    /// <summary>
    /// 用户停止输入评论
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="parentId">父评论ID（如果是回复）</param>
    [HubMethodName("StopTyping")]
    public async Task StopTyping(string postId, string? parentId = null)
    {
        try
        {
            if (!Guid.TryParse(postId, out var parsedPostId))
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return;
            }

            var groupName = $"post_{parsedPostId}";

            var typingInfo = new
            {
                userId = userId.Value,
                postId = parsedPostId,
                parentId = !string.IsNullOrEmpty(parentId) && Guid.TryParse(parentId, out var pId) ? pId : (Guid?)null
            };

            // 通知同一文章组的其他用户（除了自己）
            await Clients.GroupExcept(groupName, Context.ConnectionId)
                .SendAsync("UserStoppedTyping", typingInfo);

            _logger.LogDebug("User {UserId} stopped typing in post {PostId}", userId.Value, parsedPostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StopTyping for post {PostId}", postId);
        }
    }

    /// <summary>
    /// 获取文章评论统计
    /// </summary>
    /// <param name="postId">文章ID</param>
    [HubMethodName("GetCommentStats")]
    public async Task GetCommentStats(string postId)
    {
        try
        {
            if (!Guid.TryParse(postId, out var parsedPostId))
            {
                await Clients.Caller.SendAsync("Error", "无效的文章ID");
                return;
            }

            var stats = await _commentService.GetCommentStatsAsync(parsedPostId);
            await Clients.Caller.SendAsync("CommentStats", stats);

            _logger.LogDebug("Sent comment stats for post {PostId} to user {UserId}",
                parsedPostId, GetCurrentUserId());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment stats for post {PostId}", postId);
            await Clients.Caller.SendAsync("Error", "获取评论统计失败");
        }
    }

    /// <summary>
    /// 获取实时在线用户数
    /// </summary>
    /// <param name="postId">文章ID</param>
    [HubMethodName("GetOnlineUserCount")]
    public async Task GetOnlineUserCount(string postId)
    {
        try
        {
            if (!Guid.TryParse(postId, out var parsedPostId))
            {
                await Clients.Caller.SendAsync("Error", "无效的文章ID");
                return;
            }

            // 这里应该从缓存或数据库获取实时在线用户数
            // 暂时返回模拟数据
            var onlineCount = new Random().Next(1, 50);

            await Clients.Caller.SendAsync("OnlineUserCount", new { postId = parsedPostId, count = onlineCount });

            _logger.LogDebug("Sent online user count for post {PostId}: {Count}", parsedPostId, onlineCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online user count for post {PostId}", postId);
            await Clients.Caller.SendAsync("Error", "获取在线用户数失败");
        }
    }

    #endregion

    #region 通知管理

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    [HubMethodName("MarkNotificationAsRead")]
    public async Task MarkNotificationAsRead(string notificationId)
    {
        try
        {
            if (!Guid.TryParse(notificationId, out var parsedNotificationId))
            {
                await Clients.Caller.SendAsync("Error", "无效的通知ID");
                return;
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "用户未授权");
                return;
            }

            var success = await _notificationService.MarkAsReadAsync(parsedNotificationId, userId.Value);
            if (success)
            {
                await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);

                // 发送更新的未读数量
                var unreadCount = await _notificationService.GetUnreadCountAsync(userId.Value);
                await Clients.Caller.SendAsync("UnreadNotificationCount", unreadCount);

                _logger.LogDebug("Marked notification {NotificationId} as read for user {UserId}",
                    parsedNotificationId, userId.Value);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "标记通知失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read", notificationId);
            await Clients.Caller.SendAsync("Error", "标记通知失败");
        }
    }

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    [HubMethodName("GetUnreadNotificationCount")]
    public async Task GetUnreadNotificationCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "用户未授权");
                return;
            }

            var count = await _notificationService.GetUnreadCountAsync(userId.Value);
            await Clients.Caller.SendAsync("UnreadNotificationCount", count);

            _logger.LogDebug("Sent unread notification count {Count} to user {UserId}", count, userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count for user {UserId}", GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "获取未读通知数失败");
        }
    }

    /// <summary>
    /// 获取最新通知
    /// </summary>
    /// <param name="limit">数量限制</param>
    [HubMethodName("GetRecentNotifications")]
    public async Task GetRecentNotifications(int limit = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "用户未授权");
                return;
            }

            if (limit < 1 || limit > 50)
            {
                limit = 10;
            }

            var query = new CommentNotificationQueryDto
            {
                UserId = userId.Value,
                Page = 1,
                PageSize = limit,
                SortDirection = SortDirection.Desc
            };

            var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value, query);
            await Clients.Caller.SendAsync("RecentNotifications", notifications.Items);

            _logger.LogDebug("Sent {Count} recent notifications to user {UserId}",
                notifications.Items.Count, userId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent notifications for user {UserId}", GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "获取最新通知失败");
        }
    }

    #endregion

    #region 管理员功能

    /// <summary>
    /// 获取审核队列统计（仅管理员）
    /// </summary>
    [HubMethodName("GetModerationStats")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task GetModerationStats()
    {
        try
        {
            // 这里应该调用审核服务获取统计信息
            // 暂时返回模拟数据
            var stats = new
            {
                pendingCount = 5,
                reportedCount = 2,
                todayCount = 15,
                weeklyApprovalRate = 0.85
            };

            await Clients.Caller.SendAsync("ModerationStats", stats);

            _logger.LogDebug("Sent moderation stats to moderator {UserId}", GetCurrentUserId());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation stats for user {UserId}", GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "获取审核统计失败");
        }
    }

    /// <summary>
    /// 加入审核组（仅管理员）
    /// </summary>
    [HubMethodName("JoinModerationGroup")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<bool> JoinModerationGroup()
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "moderators");
            await Clients.Caller.SendAsync("JoinedModerationGroup");

            _logger.LogDebug("User {UserId} joined moderation group", GetCurrentUserId());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining moderation group for user {UserId}", GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "加入审核组失败");
            return false;
        }
    }

    /// <summary>
    /// 离开审核组（仅管理员）
    /// </summary>
    [HubMethodName("LeaveModerationGroup")]
    [Authorize(Roles = "Admin,Moderator")]
    public async Task<bool> LeaveModerationGroup()
    {
        try
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "moderators");
            await Clients.Caller.SendAsync("LeftModerationGroup");

            _logger.LogDebug("User {UserId} left moderation group", GetCurrentUserId());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving moderation group for user {UserId}", GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "离开审核组失败");
            return false;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 获取当前用户名
    /// </summary>
    private string GetCurrentUserName()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
    }

    /// <summary>
    /// 检查当前用户是否为审核员
    /// </summary>
    private bool IsCurrentUserModerator()
    {
        return Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Moderator") == true;
    }

    #endregion
}

/// <summary>
/// 强类型的SignalR客户端接口
/// </summary>
public interface ICommentHubClient
{
    /// <summary>
    /// 新评论创建
    /// </summary>
    Task CommentCreated(CommentDto comment);

    /// <summary>
    /// 评论更新
    /// </summary>
    Task CommentUpdated(CommentDto comment);

    /// <summary>
    /// 评论删除
    /// </summary>
    Task CommentDeleted(object commentInfo);

    /// <summary>
    /// 评论被点赞
    /// </summary>
    Task CommentLiked(object likeInfo);

    /// <summary>
    /// 评论取消点赞
    /// </summary>
    Task CommentUnliked(object unlikeInfo);

    /// <summary>
    /// 评论被批准
    /// </summary>
    Task CommentApproved(object approvalInfo);

    /// <summary>
    /// 评论被拒绝
    /// </summary>
    Task CommentRejected(object rejectionInfo);

    /// <summary>
    /// 评论被隐藏
    /// </summary>
    Task CommentHidden(object hideInfo);

    /// <summary>
    /// 评论被恢复
    /// </summary>
    Task CommentRestored(object restoreInfo);

    /// <summary>
    /// 评论被标记为垃圾信息
    /// </summary>
    Task CommentMarkedAsSpam(object spamInfo);

    /// <summary>
    /// 用户开始输入
    /// </summary>
    Task UserStartedTyping(object typingInfo);

    /// <summary>
    /// 用户停止输入
    /// </summary>
    Task UserStoppedTyping(object typingInfo);

    /// <summary>
    /// 评论统计信息
    /// </summary>
    Task CommentStats(CommentStatsDto stats);

    /// <summary>
    /// 在线用户数
    /// </summary>
    Task OnlineUserCount(object countInfo);

    /// <summary>
    /// 新通知
    /// </summary>
    Task NewNotification(CommentNotificationDto notification);

    /// <summary>
    /// 未读通知数量
    /// </summary>
    Task UnreadNotificationCount(int count);

    /// <summary>
    /// 最新通知列表
    /// </summary>
    Task RecentNotifications(IList<CommentNotificationDto> notifications);

    /// <summary>
    /// 通知标记为已读
    /// </summary>
    Task NotificationMarkedAsRead(string notificationId);

    /// <summary>
    /// 审核统计信息
    /// </summary>
    Task ModerationStats(object stats);

    /// <summary>
    /// 评论批量审核完成
    /// </summary>
    Task CommentsModerated(object moderationInfo);

    /// <summary>
    /// 错误消息
    /// </summary>
    Task Error(string message);

    /// <summary>
    /// 已加入文章组
    /// </summary>
    Task JoinedPostGroup(string postId);

    /// <summary>
    /// 已离开文章组
    /// </summary>
    Task LeftPostGroup(string postId);

    /// <summary>
    /// 已加入审核组
    /// </summary>
    Task JoinedModerationGroup();

    /// <summary>
    /// 已离开审核组
    /// </summary>
    Task LeftModerationGroup();
}