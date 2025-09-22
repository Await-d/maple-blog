using MapleBlog.Application.DTOs;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 评论通知服务接口
/// </summary>
public interface ICommentNotificationService
{
    #region 通知创建和发送

    /// <summary>
    /// 创建通知
    /// </summary>
    /// <param name="request">创建通知请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的通知ID</returns>
    Task<Guid> CreateNotificationAsync(CommentNotificationCreateDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量创建通知
    /// </summary>
    /// <param name="requests">批量创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的通知ID列表</returns>
    Task<IList<Guid>> CreateNotificationsBatchAsync(IList<CommentNotificationCreateDto> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送实时通知
    /// </summary>
    /// <param name="notification">通知数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送是否成功</returns>
    Task<bool> SendRealtimeNotificationAsync(CommentNotificationPushDto notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送评论回复通知
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="replyId">回复ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendReplyNotificationAsync(Guid commentId, Guid replyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送提及通知
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="mentionedUserIds">被提及的用户ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendMentionNotificationAsync(Guid commentId, IList<Guid> mentionedUserIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送点赞通知
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="likerId">点赞者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendLikeNotificationAsync(Guid commentId, Guid likerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送审核通知
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="approved">是否通过审核</param>
    /// <param name="reason">审核原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendModerationNotificationAsync(Guid commentId, Guid moderatorId, bool approved, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送新评论通知（给文章作者）
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendNewCommentNotificationAsync(Guid commentId, CancellationToken cancellationToken = default);

    #endregion

    #region 通知查询

    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知分页结果</returns>
    Task<CommentPagedResultDto<CommentNotificationDto>> GetUserNotificationsAsync(Guid userId, CommentNotificationQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个通知
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知详情</returns>
    Task<CommentNotificationDto?> GetNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>未读通知数量</returns>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取通知统计信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知统计</returns>
    Task<CommentNotificationStatsDto> GetNotificationStatsAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region 通知操作

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记通知为未读
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> MarkAsUnreadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量操作通知
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">批量操作请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<NotificationBatchOperationResult> BatchOperateNotificationsAsync(Guid userId, CommentNotificationBatchActionDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记所有通知为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标记的通知数量</returns>
    Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除通知
    /// </summary>
    /// <param name="notificationId">通知ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期通知
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的通知数量</returns>
    Task<int> CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 通知设置

    /// <summary>
    /// 获取用户通知设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知设置</returns>
    Task<CommentNotificationSettingsDto> GetNotificationSettingsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户通知设置
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="settings">通知设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateNotificationSettingsAsync(Guid userId, CommentNotificationSettingsDto settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否启用了特定类型的通知
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="notificationType">通知类型</param>
    /// <param name="channel">通知渠道</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否启用</returns>
    Task<bool> IsNotificationEnabledAsync(Guid userId, CommentNotificationType notificationType, NotificationChannel channel = NotificationChannel.Web, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否在安静时间内
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="currentTime">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在安静时间内</returns>
    Task<bool> IsInQuietTimeAsync(Guid userId, DateTime currentTime, CancellationToken cancellationToken = default);

    #endregion

    #region 模板和内容生成

    /// <summary>
    /// 生成通知内容
    /// </summary>
    /// <param name="type">通知类型</param>
    /// <param name="data">通知数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的通知内容</returns>
    Task<NotificationContent> GenerateNotificationContentAsync(CommentNotificationType type, object data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取通知模板
    /// </summary>
    /// <param name="type">通知类型</param>
    /// <param name="channel">通知渠道</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知模板</returns>
    Task<NotificationTemplate?> GetNotificationTemplateAsync(CommentNotificationType type, NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 渲染通知内容
    /// </summary>
    /// <param name="template">通知模板</param>
    /// <param name="data">数据对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>渲染后的内容</returns>
    Task<string> RenderNotificationContentAsync(NotificationTemplate template, object data, CancellationToken cancellationToken = default);

    #endregion

    #region 通知分发

    /// <summary>
    /// 分发通知到各个渠道
    /// </summary>
    /// <param name="notification">通知数据</param>
    /// <param name="channels">目标渠道</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分发结果</returns>
    Task<NotificationDistributionResult> DistributeNotificationAsync(CommentNotificationDto notification, NotificationChannel[] channels, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送邮件通知
    /// </summary>
    /// <param name="notification">通知数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendEmailNotificationAsync(CommentNotificationDto notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送移动推送通知
    /// </summary>
    /// <param name="notification">通知数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendMobileNotificationAsync(CommentNotificationDto notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送浏览器推送通知
    /// </summary>
    /// <param name="notification">通知数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SendBrowserNotificationAsync(CommentNotificationDto notification, CancellationToken cancellationToken = default);

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理评论创建事件
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> HandleCommentCreatedAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理评论点赞事件
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="likerId">点赞者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> HandleCommentLikedAsync(Guid commentId, Guid likerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理评论审核事件
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="approved">是否通过</param>
    /// <param name="reason">审核原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> HandleCommentModeratedAsync(Guid commentId, Guid moderatorId, bool approved, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理评论删除事件
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="deleterId">删除者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> HandleCommentDeletedAsync(Guid commentId, Guid deleterId, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// 通知批量操作结果
/// </summary>
public record NotificationBatchOperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 处理的通知数量
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// 成功处理的数量
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失败的数量
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 详细结果
    /// </summary>
    public IDictionary<Guid, string> Details { get; init; } = new Dictionary<Guid, string>();
}

/// <summary>
/// 通知内容
/// </summary>
public record NotificationContent
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// HTML内容
    /// </summary>
    public string? HtmlContent { get; init; }

    /// <summary>
    /// 链接URL
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 图标URL
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// 通知模板
/// </summary>
public record NotificationTemplate
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public CommentNotificationType Type { get; init; }

    /// <summary>
    /// 通知渠道
    /// </summary>
    public NotificationChannel Channel { get; init; }

    /// <summary>
    /// 标题模板
    /// </summary>
    public string TitleTemplate { get; init; } = string.Empty;

    /// <summary>
    /// 内容模板
    /// </summary>
    public string ContentTemplate { get; init; } = string.Empty;

    /// <summary>
    /// HTML内容模板
    /// </summary>
    public string? HtmlTemplate { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// 通知分发结果
/// </summary>
public record NotificationDistributionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 分发的渠道数量
    /// </summary>
    public int TotalChannels { get; init; }

    /// <summary>
    /// 成功的渠道数量
    /// </summary>
    public int SuccessChannels { get; init; }

    /// <summary>
    /// 失败的渠道数量
    /// </summary>
    public int FailedChannels { get; init; }

    /// <summary>
    /// 各渠道的分发结果
    /// </summary>
    public IDictionary<NotificationChannel, bool> ChannelResults { get; init; } = new Dictionary<NotificationChannel, bool>();

    /// <summary>
    /// 错误消息
    /// </summary>
    public IDictionary<NotificationChannel, string> ErrorMessages { get; init; } = new Dictionary<NotificationChannel, string>();
}