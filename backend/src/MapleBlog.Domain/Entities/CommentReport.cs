using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 评论举报实体
/// </summary>
public class CommentReport : BaseEntity
{
    /// <summary>
    /// 评论ID
    /// </summary>
    [Required]
    public Guid CommentId { get; set; }

    /// <summary>
    /// 举报者ID
    /// </summary>
    [Required]
    public Guid ReporterId { get; set; }

    /// <summary>
    /// 举报原因
    /// </summary>
    [Required]
    public CommentReportReason Reason { get; set; }

    /// <summary>
    /// 举报详细描述
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 举报状态
    /// </summary>
    public CommentReportStatus Status { get; set; } = CommentReportStatus.Pending;

    // 处理信息

    /// <summary>
    /// 审查时间
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// 审查者ID
    /// </summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>
    /// 处理结果
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// 处理动作
    /// </summary>
    public CommentReportAction? Action { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    // 导航属性

    /// <summary>
    /// 评论
    /// </summary>
    public virtual Comment? Comment { get; set; }

    /// <summary>
    /// 举报者
    /// </summary>
    public virtual User? Reporter { get; set; }

    /// <summary>
    /// 审查者
    /// </summary>
    public virtual User? Reviewer { get; set; }

    /// <summary>
    /// 处理者用户（与Reviewer相同，用于兼容性）
    /// </summary>
    public virtual User? ProcessedByUser => Reviewer;

    // 业务方法

    /// <summary>
    /// 处理举报
    /// </summary>
    /// <param name="reviewerId">审查者ID</param>
    /// <param name="status">审查状态</param>
    /// <param name="resolution">处理结果</param>
    /// <param name="action">处理动作</param>
    public void Process(Guid reviewerId, CommentReportStatus status, string? resolution = null, CommentReportAction? action = null)
    {
        Status = status;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
        Resolution = resolution;
        Action = action;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查举报是否已处理
    /// </summary>
    /// <returns>是否已处理</returns>
    public bool IsProcessed()
    {
        return Status != CommentReportStatus.Pending;
    }

    /// <summary>
    /// 获取举报原因的显示文本
    /// </summary>
    /// <returns>显示文本</returns>
    public string GetReasonDisplayText()
    {
        return Reason switch
        {
            CommentReportReason.Spam => "垃圾信息",
            CommentReportReason.Harassment => "侮辱或骚扰",
            CommentReportReason.HateSpeech => "仇恨言论",
            CommentReportReason.InappropriateContent => "不当内容",
            CommentReportReason.Misinformation => "虚假信息",
            CommentReportReason.CopyrightViolation => "版权侵犯",
            CommentReportReason.Other => "其他",
            _ => "未知原因"
        };
    }

    /// <summary>
    /// 检查举报是否紧急处理
    /// </summary>
    /// <returns>是否紧急</returns>
    public bool IsUrgent()
    {
        return Reason == CommentReportReason.HateSpeech ||
               Reason == CommentReportReason.Harassment;
    }
}