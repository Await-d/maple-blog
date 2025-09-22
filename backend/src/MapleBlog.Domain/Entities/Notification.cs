using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 通知实体
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// 用户ID（接收者）
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 接收者ID（与UserId相同，用于兼容性）
    /// </summary>
    public Guid RecipientId
    {
        get => UserId;
        set => UserId = value;
    }

    /// <summary>
    /// 发送者ID
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 通知链接
    /// </summary>
    [StringLength(500)]
    public string? Url { get; set; }

    /// <summary>
    /// 额外数据（JSON格式）
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 元数据（与Data相同，用于兼容性）
    /// </summary>
    public string? Metadata
    {
        get => Data;
        set => Data = value;
    }

    // 关联信息

    /// <summary>
    /// 相关实体类型
    /// </summary>
    [StringLength(50)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// 相关实体ID
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    // 状态信息

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// 阅读时间
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    // 发送渠道

    /// <summary>
    /// 是否发送邮件
    /// </summary>
    public bool SendEmail { get; set; } = false;

    /// <summary>
    /// 邮件发送时间
    /// </summary>
    public DateTime? EmailSentAt { get; set; }

    /// <summary>
    /// 是否发送推送
    /// </summary>
    public bool SendPush { get; set; } = false;

    /// <summary>
    /// 推送发送时间
    /// </summary>
    public DateTime? PushSentAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // 导航属性

    /// <summary>
    /// 用户（接收者）
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// 发送者
    /// </summary>
    public virtual User? Sender { get; set; }

    // 业务方法

    /// <summary>
    /// 标记为已读
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
            UpdateAuditFields();
        }
    }

    /// <summary>
    /// 标记为未读
    /// </summary>
    public void MarkAsUnread()
    {
        if (IsRead)
        {
            IsRead = false;
            ReadAt = null;
            UpdateAuditFields();
        }
    }

    /// <summary>
    /// 设置发送渠道
    /// </summary>
    /// <param name="sendEmail">是否发送邮件</param>
    /// <param name="sendPush">是否发送推送</param>
    public void SetDeliveryChannels(bool sendEmail = false, bool sendPush = false)
    {
        SendEmail = sendEmail;
        SendPush = sendPush;
        UpdateAuditFields();
    }

    /// <summary>
    /// 标记邮件已发送
    /// </summary>
    public void MarkEmailSent()
    {
        EmailSentAt = DateTime.UtcNow;
        UpdateAuditFields();
    }

    /// <summary>
    /// 标记推送已发送
    /// </summary>
    public void MarkPushSent()
    {
        PushSentAt = DateTime.UtcNow;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查通知是否已过期
    /// </summary>
    /// <returns>是否已过期</returns>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// 创建通知
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="type">通知类型</param>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="priority">优先级</param>
    /// <returns>通知</returns>
    public static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string? content = null,
        NotificationPriority priority = NotificationPriority.Normal)
    {
        return new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            Priority = priority
        };
    }
}