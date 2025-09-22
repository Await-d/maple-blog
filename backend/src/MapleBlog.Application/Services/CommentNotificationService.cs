using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapleBlog.Application.Services;

/// <summary>
/// 评论通知服务实现
/// </summary>
public class CommentNotificationService : ICommentNotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentNotificationService> _logger;
    private readonly IMemoryCache _memoryCache;

    // 通知频率限制（分钟）
    private const int DefaultNotificationFrequencyLimit = 5;

    // 通知过期时间（天）
    private const int DefaultNotificationExpirationDays = 30;

    public CommentNotificationService(
        INotificationRepository notificationRepository,
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        IMapper mapper,
        ILogger<CommentNotificationService> logger,
        IMemoryCache memoryCache)
    {
        _notificationRepository = notificationRepository;
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _mapper = mapper;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    #region 通知创建和发送

    /// <summary>
    /// 创建通知
    /// </summary>
    public async Task<Guid> CreateNotificationAsync(CommentNotificationCreateDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = new Notification
            {
                Title = request.Title,
                Content = request.Content,
                Url = request.Url,
                ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(DefaultNotificationExpirationDays),
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
            };

            // 为每个接收者创建通知副本
            var notifications = request.RecipientIds.Select(recipientId => new Notification
            {
                RecipientId = recipientId,
                SenderId = request.SenderId,
                Title = notification.Title,
                Content = notification.Content,
                Url = notification.Url,
                ExpiresAt = notification.ExpiresAt,
                Metadata = notification.Metadata,
                Type = MapToNotificationType(request.Type)
            }).ToList();

            await _notificationRepository.AddRangeAsync(notifications);
            await _notificationRepository.SaveChangesAsync();

            // 如果需要立即发送，触发实时推送
            if (request.SendImmediately)
            {
                var pushDto = new CommentNotificationPushDto
                {
                    Notification = await MapToNotificationDtoAsync(notifications.First()),
                    PushType = NotificationPushType.New,
                    TargetUserIds = request.RecipientIds,
                    Channels = [NotificationChannel.Web]
                };

                await SendRealtimeNotificationAsync(pushDto, cancellationToken);
            }

            _logger.LogInformation("Created notification for {RecipientCount} recipients", request.RecipientIds.Count);

            return notifications.First().Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification");
            throw;
        }
    }

    /// <summary>
    /// 批量创建通知
    /// </summary>
    public async Task<IList<Guid>> CreateNotificationsBatchAsync(IList<CommentNotificationCreateDto> requests, CancellationToken cancellationToken = default)
    {
        try
        {
            var notificationIds = new List<Guid>();

            foreach (var request in requests)
            {
                var notificationId = await CreateNotificationAsync(request, cancellationToken);
                notificationIds.Add(notificationId);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notifications batch");
            throw;
        }
    }

    /// <summary>
    /// 发送实时通知
    /// </summary>
    public async Task<bool> SendRealtimeNotificationAsync(CommentNotificationPushDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该通过SignalR发送实时通知
            // 暂时记录日志模拟
            _logger.LogInformation("Sending realtime notification to {UserCount} users: {Title}",
                notification.TargetUserIds.Count, notification.Notification.Title);

            await Task.Delay(10, cancellationToken); // 模拟异步操作

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending realtime notification");
            return false;
        }
    }

    /// <summary>
    /// 发送评论回复通知
    /// </summary>
    public async Task<bool> SendReplyNotificationAsync(Guid commentId, Guid replyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            var reply = await _commentRepository.GetByIdWithDetailsAsync(replyId);

            if (comment == null || reply == null || comment.AuthorId == reply.AuthorId)
            {
                return false; // 不给自己发通知
            }

            // 检查用户通知设置
            if (!await IsNotificationEnabledAsync(comment.AuthorId, CommentNotificationType.NewReply, cancellationToken: cancellationToken))
            {
                return false;
            }

            // 检查频率限制
            if (!await CheckNotificationFrequencyAsync(comment.AuthorId, CommentNotificationType.NewReply))
            {
                return false;
            }

            var content = GenerateReplyNotificationContent(comment, reply);

            var createRequest = new CommentNotificationCreateDto
            {
                RecipientIds = [comment.AuthorId],
                SenderId = reply.AuthorId,
                CommentId = replyId,
                Type = CommentNotificationType.NewReply,
                Title = content.Title,
                Content = content.Content,
                Url = GenerateCommentUrl(reply),
                SendImmediately = true
            };

            await CreateNotificationAsync(createRequest, cancellationToken);

            _logger.LogInformation("Sent reply notification for comment {CommentId} to user {UserId}",
                commentId, comment.AuthorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reply notification for comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// 发送提及通知
    /// </summary>
    public async Task<bool> SendMentionNotificationAsync(Guid commentId, IList<Guid> mentionedUserIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null || !mentionedUserIds.Any())
            {
                return false;
            }

            // 过滤掉作者自己
            var targetUserIds = mentionedUserIds.Where(id => id != comment.AuthorId).ToList();
            if (!targetUserIds.Any())
            {
                return false;
            }

            // 检查每个用户的通知设置
            var enabledUserIds = new List<Guid>();
            foreach (var userId in targetUserIds)
            {
                if (await IsNotificationEnabledAsync(userId, CommentNotificationType.Mention, cancellationToken: cancellationToken) &&
                    await CheckNotificationFrequencyAsync(userId, CommentNotificationType.Mention))
                {
                    enabledUserIds.Add(userId);
                }
            }

            if (!enabledUserIds.Any())
            {
                return false;
            }

            var content = GenerateMentionNotificationContent(comment);

            var createRequest = new CommentNotificationCreateDto
            {
                RecipientIds = enabledUserIds,
                SenderId = comment.AuthorId,
                CommentId = commentId,
                Type = CommentNotificationType.Mention,
                Title = content.Title,
                Content = content.Content,
                Url = GenerateCommentUrl(comment),
                SendImmediately = true
            };

            await CreateNotificationAsync(createRequest, cancellationToken);

            _logger.LogInformation("Sent mention notifications for comment {CommentId} to {UserCount} users",
                commentId, enabledUserIds.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mention notification for comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// 发送点赞通知
    /// </summary>
    public async Task<bool> SendLikeNotificationAsync(Guid commentId, Guid likerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null || comment.AuthorId == likerId)
            {
                return false; // 不给自己发通知
            }

            // 检查用户通知设置
            if (!await IsNotificationEnabledAsync(comment.AuthorId, CommentNotificationType.CommentLiked, cancellationToken: cancellationToken))
            {
                return false;
            }

            // 点赞通知通常有更严格的频率限制
            if (!await CheckNotificationFrequencyAsync(comment.AuthorId, CommentNotificationType.CommentLiked, 15))
            {
                return false;
            }

            var content = await GenerateLikeNotificationContentAsync(comment, likerId);

            var createRequest = new CommentNotificationCreateDto
            {
                RecipientIds = [comment.AuthorId],
                SenderId = likerId,
                CommentId = commentId,
                Type = CommentNotificationType.CommentLiked,
                Title = content.Title,
                Content = content.Content,
                Url = GenerateCommentUrl(comment),
                SendImmediately = true
            };

            await CreateNotificationAsync(createRequest, cancellationToken);

            _logger.LogInformation("Sent like notification for comment {CommentId} to user {UserId}",
                commentId, comment.AuthorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending like notification for comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// 发送审核通知
    /// </summary>
    public async Task<bool> SendModerationNotificationAsync(Guid commentId, Guid moderatorId, bool approved, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            var notificationType = approved
                ? CommentNotificationType.CommentApproved
                : CommentNotificationType.CommentRejected;

            // 检查用户通知设置
            if (!await IsNotificationEnabledAsync(comment.AuthorId, notificationType, cancellationToken: cancellationToken))
            {
                return false;
            }

            var content = GenerateModerationNotificationContent(comment, approved, reason);

            var createRequest = new CommentNotificationCreateDto
            {
                RecipientIds = [comment.AuthorId],
                SenderId = moderatorId,
                CommentId = commentId,
                Type = notificationType,
                Title = content.Title,
                Content = content.Content,
                Url = GenerateCommentUrl(comment),
                SendImmediately = true
            };

            await CreateNotificationAsync(createRequest, cancellationToken);

            _logger.LogInformation("Sent moderation notification for comment {CommentId} to user {UserId}, approved: {Approved}",
                commentId, comment.AuthorId, approved);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending moderation notification for comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// 发送新评论通知（给文章作者）
    /// </summary>
    public async Task<bool> SendNewCommentNotificationAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment?.Post == null)
            {
                return false;
            }

            var postAuthorId = comment.Post.AuthorId;
            if (postAuthorId == comment.AuthorId)
            {
                return false; // 不给自己发通知
            }

            // 检查用户通知设置
            if (!await IsNotificationEnabledAsync(postAuthorId, CommentNotificationType.NewCommentOnPost, cancellationToken: cancellationToken))
            {
                return false;
            }

            // 检查频率限制
            if (!await CheckNotificationFrequencyAsync(postAuthorId, CommentNotificationType.NewCommentOnPost))
            {
                return false;
            }

            var content = GenerateNewCommentNotificationContent(comment);

            var createRequest = new CommentNotificationCreateDto
            {
                RecipientIds = [postAuthorId],
                SenderId = comment.AuthorId,
                CommentId = commentId,
                Type = CommentNotificationType.NewCommentOnPost,
                Title = content.Title,
                Content = content.Content,
                Url = GenerateCommentUrl(comment),
                SendImmediately = true
            };

            await CreateNotificationAsync(createRequest, cancellationToken);

            _logger.LogInformation("Sent new comment notification for comment {CommentId} to post author {UserId}",
                commentId, postAuthorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new comment notification for comment {CommentId}", commentId);
            return false;
        }
    }

    #endregion

    #region 通知查询

    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    public async Task<CommentPagedResultDto<CommentNotificationDto>> GetUserNotificationsAsync(Guid userId, CommentNotificationQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _notificationRepository.GetQueryable()
                .Where(n => n.RecipientId == userId && !n.IsDeleted);

            // 应用过滤条件
            if (query.TypeFilter.Any())
            {
                var mappedTypes = query.TypeFilter.Select(t => MapToNotificationType(t)).ToList();
                queryable = queryable.Where(n => mappedTypes.Contains(n.Type));
            }

            if (query.UnreadOnly)
            {
                queryable = queryable.Where(n => !n.IsRead);
            }

            if (query.StartDate.HasValue)
            {
                queryable = queryable.Where(n => n.CreatedAt >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                queryable = queryable.Where(n => n.CreatedAt <= query.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                queryable = queryable.Where(n => n.Title.Contains(query.Keyword) ||
                                                n.Content.Contains(query.Keyword));
            }

            // 排序
            queryable = query.SortDirection == SortDirection.Asc
                ? queryable.OrderBy(n => n.CreatedAt)
                : queryable.OrderByDescending(n => n.CreatedAt);

            // 分页
            var totalCount = await queryable.CountAsync(cancellationToken);
            var notifications = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(n => n.Sender)
                .ToListAsync(cancellationToken);

            var notificationDtos = await Task.WhenAll(
                notifications.Select(MapToNotificationDtoAsync));

            return new CommentPagedResultDto<CommentNotificationDto>
            {
                Items = notificationDtos,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                CurrentPage = query.Page,
                PageSize = query.PageSize,
                HasNextPage = query.Page * query.PageSize < totalCount,
                HasPreviousPage = query.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取单个通知
    /// </summary>
    public async Task<CommentNotificationDto?> GetNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetQueryable()
                .Where(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted)
                .Include(n => n.Sender)
                .FirstOrDefaultAsync(cancellationToken);

            return notification != null ? await MapToNotificationDtoAsync(notification) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification {NotificationId} for user {UserId}",
                notificationId, userId);
            return null;
        }
    }

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"unread_count:{userId}";
            var cached = _memoryCache.Get<int?>(cacheKey);
            if (cached.HasValue)
            {
                return cached.Value;
            }

            var count = await _notificationRepository.GetQueryable()
                .Where(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted)
                .CountAsync(cancellationToken);

            // 缓存1分钟
            _memoryCache.Set(cacheKey, count, TimeSpan.FromMinutes(1));

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// 获取通知统计信息
    /// </summary>
    public async Task<CommentNotificationStatsDto> GetNotificationStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"notification_stats:{userId}";
            var cached = _memoryCache.Get<CommentNotificationStatsDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var stats = await CalculateNotificationStatsAsync(userId, cancellationToken);

            // 缓存5分钟
            _memoryCache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification stats for user {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region 通知操作

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetQueryable()
                .Where(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (notification == null || notification.IsRead)
            {
                return false;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdateAuditFields(userId);

            await _notificationRepository.SaveChangesAsync();

            // 清理缓存
            ClearUserNotificationCache(userId);

            _logger.LogDebug("Marked notification {NotificationId} as read for user {UserId}",
                notificationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    /// <summary>
    /// 标记通知为未读
    /// </summary>
    public async Task<bool> MarkAsUnreadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetQueryable()
                .Where(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (notification == null || !notification.IsRead)
            {
                return false;
            }

            notification.IsRead = false;
            notification.ReadAt = null;
            notification.UpdateAuditFields(userId);

            await _notificationRepository.SaveChangesAsync();

            // 清理缓存
            ClearUserNotificationCache(userId);

            _logger.LogDebug("Marked notification {NotificationId} as unread for user {UserId}",
                notificationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as unread for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    /// <summary>
    /// 批量操作通知
    /// </summary>
    public async Task<NotificationBatchOperationResult> BatchOperateNotificationsAsync(Guid userId, CommentNotificationBatchActionDto request, CancellationToken cancellationToken = default)
    {
        var result = new NotificationBatchOperationResult
        {
            ProcessedCount = request.NotificationIds.Count
        };

        var details = new Dictionary<Guid, string>();
        var successCount = 0;

        foreach (var notificationId in request.NotificationIds)
        {
            try
            {
                var success = request.Action switch
                {
                    NotificationBatchAction.MarkAsRead => await MarkAsReadAsync(notificationId, userId, cancellationToken),
                    NotificationBatchAction.MarkAsUnread => await MarkAsUnreadAsync(notificationId, userId, cancellationToken),
                    NotificationBatchAction.Delete => await DeleteNotificationAsync(notificationId, userId, cancellationToken),
                    NotificationBatchAction.Archive => await ArchiveNotificationAsync(notificationId, userId, cancellationToken),
                    _ => false
                };

                if (success)
                {
                    successCount++;
                    details[notificationId] = "操作成功";
                }
                else
                {
                    details[notificationId] = "操作失败";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch operation for notification {NotificationId}", notificationId);
                details[notificationId] = ex.Message;
            }
        }

        return result with
        {
            Success = successCount > 0,
            SuccessCount = successCount,
            FailedCount = result.ProcessedCount - successCount,
            Details = details
        };
    }

    /// <summary>
    /// 标记所有通知为已读
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _notificationRepository.GetQueryable()
                .Where(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted)
                .ToListAsync(cancellationToken);

            var count = 0;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdateAuditFields(userId);
                count++;
            }

            await _notificationRepository.SaveChangesAsync();

            // 清理缓存
            ClearUserNotificationCache(userId);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", count, userId);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// 删除通知
    /// </summary>
    public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _notificationRepository.GetQueryable()
                .Where(n => n.Id == notificationId && n.RecipientId == userId && !n.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (notification == null)
            {
                return false;
            }

            notification.SoftDelete(userId);
            await _notificationRepository.SaveChangesAsync();

            // 清理缓存
            ClearUserNotificationCache(userId);

            _logger.LogDebug("Deleted notification {NotificationId} for user {UserId}",
                notificationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    /// <summary>
    /// 清理过期通知
    /// </summary>
    public async Task<int> CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredNotifications = await _notificationRepository.GetQueryable()
                .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt < DateTime.UtcNow && !n.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var notification in expiredNotifications)
            {
                notification.SoftDelete();
            }

            await _notificationRepository.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count);

            return expiredNotifications.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired notifications");
            return 0;
        }
    }

    #endregion

    #region 通知设置

    /// <summary>
    /// 获取用户通知设置
    /// </summary>
    public async Task<CommentNotificationSettingsDto> GetNotificationSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该从用户设置表查询，暂时返回默认设置
            await Task.Delay(1, cancellationToken);

            return new CommentNotificationSettingsDto
            {
                UserId = userId,
                EnableEmailNotifications = true,
                EnableBrowserNotifications = true,
                EnableMobileNotifications = false,
                NotificationFrequencyLimit = DefaultNotificationFrequencyLimit
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings for user {UserId}", userId);
            return new CommentNotificationSettingsDto { UserId = userId };
        }
    }

    /// <summary>
    /// 更新用户通知设置
    /// </summary>
    public async Task<bool> UpdateNotificationSettingsAsync(Guid userId, CommentNotificationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该保存到用户设置表，暂时记录日志
            await Task.Delay(1, cancellationToken);

            _logger.LogInformation("Updated notification settings for user {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查用户是否启用了特定类型的通知
    /// </summary>
    public async Task<bool> IsNotificationEnabledAsync(Guid userId, CommentNotificationType notificationType, NotificationChannel channel = NotificationChannel.Web, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await GetNotificationSettingsAsync(userId, cancellationToken);

            // 检查通道是否启用
            var channelEnabled = channel switch
            {
                NotificationChannel.Web => true, // Web通知总是启用
                NotificationChannel.Email => settings.EnableEmailNotifications,
                NotificationChannel.Mobile => settings.EnableMobileNotifications,
                _ => true
            };

            if (!channelEnabled)
            {
                return false;
            }

            // 检查特定通知类型设置
            return notificationType switch
            {
                CommentNotificationType.NewReply => settings.NewReplySettings.Enabled,
                CommentNotificationType.Mention => settings.MentionSettings.Enabled,
                CommentNotificationType.CommentLiked => settings.LikeSettings.Enabled,
                CommentNotificationType.CommentApproved or
                CommentNotificationType.CommentRejected => settings.ModerationSettings.Enabled,
                _ => true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking notification enabled for user {UserId}", userId);
            return true; // 默认启用
        }
    }

    /// <summary>
    /// 检查是否在安静时间内
    /// </summary>
    public async Task<bool> IsInQuietTimeAsync(Guid userId, DateTime currentTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await GetNotificationSettingsAsync(userId, cancellationToken);

            if (!settings.QuietTimeStart.HasValue || !settings.QuietTimeEnd.HasValue)
            {
                return false; // 没有设置安静时间
            }

            var currentTimeOfDay = currentTime.TimeOfDay;
            var quietStart = settings.QuietTimeStart.Value;
            var quietEnd = settings.QuietTimeEnd.Value;

            // 处理跨天的情况
            if (quietStart <= quietEnd)
            {
                return currentTimeOfDay >= quietStart && currentTimeOfDay <= quietEnd;
            }
            else
            {
                return currentTimeOfDay >= quietStart || currentTimeOfDay <= quietEnd;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking quiet time for user {UserId}", userId);
            return false;
        }
    }

    #endregion

    #region 模板和内容生成 - 简化实现

    /// <summary>
    /// 生成通知内容
    /// </summary>
    public async Task<NotificationContent> GenerateNotificationContentAsync(CommentNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        return type switch
        {
            CommentNotificationType.NewReply => new NotificationContent
            {
                Title = "收到新的回复",
                Content = "您的评论收到了一条新的回复"
            },
            CommentNotificationType.Mention => new NotificationContent
            {
                Title = "有人提到了您",
                Content = "您在一条评论中被提及"
            },
            CommentNotificationType.CommentLiked => new NotificationContent
            {
                Title = "评论获得点赞",
                Content = "您的评论获得了一个赞"
            },
            _ => new NotificationContent
            {
                Title = "新通知",
                Content = "您有一条新通知"
            }
        };
    }

    /// <summary>
    /// 获取通知模板
    /// </summary>
    public async Task<NotificationTemplate?> GetNotificationTemplateAsync(CommentNotificationType type, NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        // 这里应该从数据库查询模板，暂时返回null
        return null;
    }

    /// <summary>
    /// 渲染通知内容
    /// </summary>
    public async Task<string> RenderNotificationContentAsync(NotificationTemplate template, object data, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        // 这里应该使用模板引擎渲染内容
        return template.ContentTemplate;
    }

    #endregion

    #region 通知分发 - 简化实现

    /// <summary>
    /// 分发通知到各个渠道
    /// </summary>
    public async Task<NotificationDistributionResult> DistributeNotificationAsync(CommentNotificationDto notification, NotificationChannel[] channels, CancellationToken cancellationToken = default)
    {
        var result = new NotificationDistributionResult
        {
            TotalChannels = channels.Length
        };

        var channelResults = new Dictionary<NotificationChannel, bool>();
        var errorMessages = new Dictionary<NotificationChannel, string>();
        var successCount = 0;

        foreach (var channel in channels)
        {
            try
            {
                var success = channel switch
                {
                    NotificationChannel.Web => true, // Web通知已经通过创建到数据库完成
                    NotificationChannel.Email => await SendEmailNotificationAsync(notification, cancellationToken),
                    NotificationChannel.Mobile => await SendMobileNotificationAsync(notification, cancellationToken),
                    _ => false
                };

                channelResults[channel] = success;
                if (success)
                {
                    successCount++;
                }
            }
            catch (Exception ex)
            {
                channelResults[channel] = false;
                errorMessages[channel] = ex.Message;
                _logger.LogError(ex, "Error distributing notification to channel {Channel}", channel);
            }
        }

        return result with
        {
            Success = successCount > 0,
            SuccessChannels = successCount,
            FailedChannels = result.TotalChannels - successCount,
            ChannelResults = channelResults,
            ErrorMessages = errorMessages
        };
    }

    /// <summary>
    /// 发送邮件通知
    /// </summary>
    public async Task<bool> SendEmailNotificationAsync(CommentNotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(10, cancellationToken); // 模拟邮件发送
            _logger.LogDebug("Sent email notification to user {UserId}: {Title}",
                notification.RecipientId, notification.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notification");
            return false;
        }
    }

    /// <summary>
    /// 发送移动推送通知
    /// </summary>
    public async Task<bool> SendMobileNotificationAsync(CommentNotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(10, cancellationToken); // 模拟推送
            _logger.LogDebug("Sent mobile notification to user {UserId}: {Title}",
                notification.RecipientId, notification.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mobile notification");
            return false;
        }
    }

    /// <summary>
    /// 发送浏览器推送通知
    /// </summary>
    public async Task<bool> SendBrowserNotificationAsync(CommentNotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(10, cancellationToken); // 模拟浏览器推送
            _logger.LogDebug("Sent browser notification to user {UserId}: {Title}",
                notification.RecipientId, notification.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending browser notification");
            return false;
        }
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 处理评论创建事件
    /// </summary>
    public async Task<bool> HandleCommentCreatedAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            var tasks = new List<Task<bool>>();

            // 发送给文章作者的通知
            tasks.Add(SendNewCommentNotificationAsync(commentId, cancellationToken));

            // 如果是回复，发送回复通知
            if (comment.ParentId.HasValue)
            {
                tasks.Add(SendReplyNotificationAsync(comment.ParentId.Value, commentId, cancellationToken));
            }

            // 发送提及通知
            var mentionedUserIds = await ExtractMentionedUsersAsync(comment.Content.RawContent);
            if (mentionedUserIds.Any())
            {
                tasks.Add(SendMentionNotificationAsync(commentId, mentionedUserIds, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            return results.Any(r => r);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling comment created event for comment {CommentId}", commentId);
            return false;
        }
    }

    /// <summary>
    /// 处理评论点赞事件
    /// </summary>
    public async Task<bool> HandleCommentLikedAsync(Guid commentId, Guid likerId, CancellationToken cancellationToken = default)
    {
        return await SendLikeNotificationAsync(commentId, likerId, cancellationToken);
    }

    /// <summary>
    /// 处理评论审核事件
    /// </summary>
    public async Task<bool> HandleCommentModeratedAsync(Guid commentId, Guid moderatorId, bool approved, string? reason = null, CancellationToken cancellationToken = default)
    {
        return await SendModerationNotificationAsync(commentId, moderatorId, approved, reason, cancellationToken);
    }

    /// <summary>
    /// 处理评论删除事件
    /// </summary>
    public async Task<bool> HandleCommentDeletedAsync(Guid commentId, Guid deleterId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null || comment.AuthorId == deleterId)
            {
                return false; // 自己删除不发通知
            }

            var content = new NotificationContent
            {
                Title = "评论被删除",
                Content = "您的评论已被管理员删除"
            };

            var createRequest = new CommentNotificationCreateDto
            {
                RecipientIds = [comment.AuthorId],
                SenderId = deleterId,
                CommentId = commentId,
                Type = CommentNotificationType.CommentDeleted,
                Title = content.Title,
                Content = content.Content,
                SendImmediately = true
            };

            await CreateNotificationAsync(createRequest, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling comment deleted event for comment {CommentId}", commentId);
            return false;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 映射到通知DTO
    /// </summary>
    private async Task<CommentNotificationDto> MapToNotificationDtoAsync(Notification notification)
    {
        await Task.Delay(1); // 模拟异步操作

        // 获取评论信息
        CommentNotificationCommentDto? commentDto = null;
        if (!string.IsNullOrEmpty(notification.Metadata))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Metadata);
                if (metadata != null && metadata.TryGetValue("commentId", out var commentIdObj))
                {
                    if (Guid.TryParse(commentIdObj.ToString(), out var commentId))
                    {
                        var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
                        if (comment?.Post != null)
                        {
                            commentDto = new CommentNotificationCommentDto
                            {
                                Id = comment.Id,
                                PostId = comment.PostId,
                                PostTitle = comment.Post.Title,
                                PostSlug = comment.Post.Slug,
                                Content = comment.Content.RawContent.Length > 100
                                    ? comment.Content.RawContent[..100] + "..."
                                    : comment.Content.RawContent,
                                ParentId = comment.ParentId,
                                Status = comment.Status,
                                CreatedAt = comment.CreatedAt
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing notification metadata for notification {NotificationId}",
                    notification.Id);
            }
        }

        return new CommentNotificationDto
        {
            Id = notification.Id,
            RecipientId = notification.RecipientId,
            Sender = notification.Sender != null ? _mapper.Map<CommentAuthorDto>(notification.Sender) : null,
            Comment = commentDto,
            Type = (CommentNotificationType)notification.Type,
            Title = notification.Title,
            Content = notification.Content,
            Url = notification.Url,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt,
            ExpiresAt = notification.ExpiresAt,
            Metadata = notification.Metadata
        };
    }

    /// <summary>
    /// 检查通知频率限制
    /// </summary>
    private async Task<bool> CheckNotificationFrequencyAsync(Guid userId, CommentNotificationType type, int? customLimitMinutes = null)
    {
        try
        {
            var settings = await GetNotificationSettingsAsync(userId);
            var limitMinutes = customLimitMinutes ?? settings.NotificationFrequencyLimit;

            var cutoffTime = DateTime.UtcNow.AddMinutes(-limitMinutes);
            var recentCount = await _notificationRepository.GetQueryable()
                .Where(n => n.RecipientId == userId &&
                           n.Type == MapToNotificationType(type) &&
                           n.CreatedAt > cutoffTime &&
                           !n.IsDeleted)
                .CountAsync();

            return recentCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking notification frequency for user {UserId}", userId);
            return true; // 出错时允许发送
        }
    }

    /// <summary>
    /// 生成回复通知内容
    /// </summary>
    private NotificationContent GenerateReplyNotificationContent(Comment comment, Comment reply)
    {
        var replyAuthor = reply.Author?.DisplayName ?? "某位用户";
        var postTitle = comment.Post?.Title ?? "文章";

        return new NotificationContent
        {
            Title = "收到新的回复",
            Content = $"{replyAuthor} 在《{postTitle}》中回复了您的评论"
        };
    }

    /// <summary>
    /// 生成提及通知内容
    /// </summary>
    private NotificationContent GenerateMentionNotificationContent(Comment comment)
    {
        var author = comment.Author?.DisplayName ?? "某位用户";
        var postTitle = comment.Post?.Title ?? "文章";

        return new NotificationContent
        {
            Title = "有人提到了您",
            Content = $"{author} 在《{postTitle}》的评论中提到了您"
        };
    }

    /// <summary>
    /// 生成点赞通知内容
    /// </summary>
    private async Task<NotificationContent> GenerateLikeNotificationContentAsync(Comment comment, Guid likerId)
    {
        // 这里应该查询用户信息，暂时使用默认文本
        await Task.Delay(1);

        var postTitle = comment.Post?.Title ?? "文章";

        return new NotificationContent
        {
            Title = "评论获得点赞",
            Content = $"您在《{postTitle}》中的评论获得了一个赞"
        };
    }

    /// <summary>
    /// 生成审核通知内容
    /// </summary>
    private NotificationContent GenerateModerationNotificationContent(Comment comment, bool approved, string? reason)
    {
        var postTitle = comment.Post?.Title ?? "文章";
        var status = approved ? "通过审核" : "未通过审核";
        var content = $"您在《{postTitle}》中的评论已{status}";

        if (!approved && !string.IsNullOrWhiteSpace(reason))
        {
            content += $"，原因：{reason}";
        }

        return new NotificationContent
        {
            Title = approved ? "评论审核通过" : "评论审核未通过",
            Content = content
        };
    }

    /// <summary>
    /// 生成新评论通知内容
    /// </summary>
    private NotificationContent GenerateNewCommentNotificationContent(Comment comment)
    {
        var author = comment.Author?.DisplayName ?? "某位用户";
        var postTitle = comment.Post?.Title ?? "您的文章";

        return new NotificationContent
        {
            Title = "收到新评论",
            Content = $"{author} 在《{postTitle}》中发表了评论"
        };
    }

    /// <summary>
    /// 生成评论URL
    /// </summary>
    private string GenerateCommentUrl(Comment comment)
    {
        var postSlug = comment.Post?.Slug ?? "unknown";
        return $"/posts/{postSlug}#comment-{comment.Id}";
    }

    /// <summary>
    /// 提取内容中提及的用户
    /// </summary>
    private async Task<IList<Guid>> ExtractMentionedUsersAsync(string content)
    {
        try
        {
            var mentionPattern = @"@(\w+)";
            var matches = Regex.Matches(content, mentionPattern);
            var usernames = matches.Select(m => m.Groups[1].Value).Distinct().ToList();

            // 这里应该根据用户名查找用户ID，暂时返回空列表
            await Task.Delay(1);
            return new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting mentioned users");
            return new List<Guid>();
        }
    }

    /// <summary>
    /// 归档通知
    /// </summary>
    private async Task<bool> ArchiveNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该实现归档逻辑，暂时等同于标记已读
            return await MarkAsReadAsync(notificationId, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving notification {NotificationId}", notificationId);
            return false;
        }
    }

    /// <summary>
    /// 计算通知统计信息
    /// </summary>
    private async Task<CommentNotificationStatsDto> CalculateNotificationStatsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var totalCount = await _notificationRepository.GetQueryable()
            .Where(n => n.RecipientId == userId && !n.IsDeleted)
            .CountAsync(cancellationToken);

        var unreadCount = await _notificationRepository.GetQueryable()
            .Where(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted)
            .CountAsync(cancellationToken);

        var todayCount = await _notificationRepository.GetQueryable()
            .Where(n => n.RecipientId == userId && n.CreatedAt >= today && !n.IsDeleted)
            .CountAsync(cancellationToken);

        // 按类型统计
        var typeCounts = new Dictionary<CommentNotificationType, int>();
        var unreadTypeCounts = new Dictionary<CommentNotificationType, int>();

        foreach (CommentNotificationType type in Enum.GetValues<CommentNotificationType>())
        {
            var mappedType = MapToNotificationType(type);

            typeCounts[type] = await _notificationRepository.GetQueryable()
                .Where(n => n.RecipientId == userId && n.Type == mappedType && !n.IsDeleted)
                .CountAsync(cancellationToken);

            unreadTypeCounts[type] = await _notificationRepository.GetQueryable()
                .Where(n => n.RecipientId == userId && n.Type == mappedType && !n.IsRead && !n.IsDeleted)
                .CountAsync(cancellationToken);
        }

        return new CommentNotificationStatsDto
        {
            UserId = userId,
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            TodayCount = todayCount,
            TypeCounts = typeCounts,
            UnreadTypeCounts = unreadTypeCounts
        };
    }

    /// <summary>
    /// 清理用户通知缓存
    /// </summary>
    private void ClearUserNotificationCache(Guid userId)
    {
        _memoryCache.Remove($"unread_count:{userId}");
        _memoryCache.Remove($"notification_stats:{userId}");
    }

    /// <summary>
    /// 将应用层通知类型映射到领域层通知类型
    /// </summary>
    private static MapleBlog.Domain.Enums.NotificationType MapToNotificationType(CommentNotificationType commentType)
    {
        return commentType switch
        {
            CommentNotificationType.NewReply => MapleBlog.Domain.Enums.NotificationType.CommentReply,
            CommentNotificationType.Mention => MapleBlog.Domain.Enums.NotificationType.CommentReply,
            CommentNotificationType.CommentLiked => MapleBlog.Domain.Enums.NotificationType.CommentLiked,
            CommentNotificationType.CommentApproved => MapleBlog.Domain.Enums.NotificationType.CommentReply,
            CommentNotificationType.CommentRejected => MapleBlog.Domain.Enums.NotificationType.CommentReply,
            _ => MapleBlog.Domain.Enums.NotificationType.CommentReply
        };
    }

    #endregion
}