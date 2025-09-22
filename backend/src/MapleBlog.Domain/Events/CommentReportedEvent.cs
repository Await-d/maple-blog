using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Events;

/// <summary>
/// 评论举报事件
/// </summary>
public record CommentReportedEvent : DomainEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public override string EventName => "CommentReported";

    /// <summary>
    /// 举报记录ID
    /// </summary>
    public Guid ReportId { get; init; }

    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// 举报者ID
    /// </summary>
    public Guid ReporterId { get; init; }

    /// <summary>
    /// 评论作者ID
    /// </summary>
    public Guid CommentAuthorId { get; init; }

    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// 举报原因
    /// </summary>
    public CommentReportReason Reason { get; init; }

    /// <summary>
    /// 举报描述
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 举报后该评论的总举报数
    /// </summary>
    public int NewReportCount { get; init; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// 举报者信息
    /// </summary>
    public ReportUser Reporter { get; init; }

    /// <summary>
    /// 被举报的评论信息
    /// </summary>
    public ReportedComment Comment { get; init; }

    /// <summary>
    /// 是否为重复举报（同一用户对同一评论的再次举报）
    /// </summary>
    public bool IsDuplicateReport { get; init; }

    /// <summary>
    /// 是否触发了自动审核阈值
    /// </summary>
    public bool TriggeredAutoModeration { get; init; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="reportId">举报记录ID</param>
    /// <param name="commentId">评论ID</param>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="commentAuthorId">评论作者ID</param>
    /// <param name="postId">文章ID</param>
    /// <param name="reason">举报原因</param>
    /// <param name="description">举报描述</param>
    /// <param name="newReportCount">新的举报总数</param>
    /// <param name="reporter">举报者信息</param>
    /// <param name="comment">评论信息</param>
    /// <param name="isDuplicateReport">是否为重复举报</param>
    /// <param name="triggeredAutoModeration">是否触发自动审核</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    public CommentReportedEvent(
        Guid reportId,
        Guid commentId,
        Guid reporterId,
        Guid commentAuthorId,
        Guid postId,
        CommentReportReason reason,
        string? description,
        int newReportCount,
        ReportUser reporter,
        ReportedComment comment,
        bool isDuplicateReport,
        bool triggeredAutoModeration,
        string? ipAddress = null,
        string? userAgent = null)
    {
        ReportId = reportId;
        CommentId = commentId;
        ReporterId = reporterId;
        CommentAuthorId = commentAuthorId;
        PostId = postId;
        Reason = reason;
        Description = description;
        NewReportCount = newReportCount;
        Reporter = reporter;
        Comment = comment;
        IsDuplicateReport = isDuplicateReport;
        TriggeredAutoModeration = triggeredAutoModeration;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// 从评论举报实体创建事件
    /// </summary>
    /// <param name="commentReport">评论举报实体</param>
    /// <param name="newReportCount">新的举报总数</param>
    /// <param name="isDuplicateReport">是否为重复举报</param>
    /// <param name="triggeredAutoModeration">是否触发自动审核</param>
    /// <returns>评论举报事件</returns>
    public static CommentReportedEvent FromCommentReport(
        CommentReport commentReport,
        int newReportCount,
        bool isDuplicateReport = false,
        bool triggeredAutoModeration = false)
    {
        var reporter = new ReportUser(
            commentReport.ReporterId,
            commentReport.Reporter?.UserName ?? "匿名用户",
            commentReport.Reporter?.Email,
            commentReport.Reporter?.AvatarUrl
        );

        var comment = new ReportedComment(
            commentReport.CommentId,
            commentReport.Comment?.Content?.ProcessedContent ?? string.Empty,
            commentReport.Comment?.RawContent ?? string.Empty,
            commentReport.Comment?.AuthorId ?? Guid.Empty,
            commentReport.Comment?.PostId ?? Guid.Empty,
            commentReport.Comment?.ParentId,
            commentReport.Comment?.Level ?? 0,
            commentReport.Comment?.Status ?? Enums.CommentStatus.Published
        );

        var reason = Enum.TryParse<CommentReportReason>(commentReport.Reason.ToString(), true, out var parsedReason)
            ? parsedReason
            : CommentReportReason.Other;

        return new CommentReportedEvent(
            reportId: commentReport.Id,
            commentId: commentReport.CommentId,
            reporterId: commentReport.ReporterId,
            commentAuthorId: comment.AuthorId,
            postId: comment.PostId,
            reason: reason,
            description: commentReport.Description,
            newReportCount: newReportCount,
            reporter: reporter,
            comment: comment,
            isDuplicateReport: isDuplicateReport,
            triggeredAutoModeration: triggeredAutoModeration,
            ipAddress: commentReport.IpAddress,
            userAgent: commentReport.UserAgent
        );
    }

    /// <summary>
    /// 检查是否需要立即审核
    /// </summary>
    /// <param name="urgentThreshold">紧急审核阈值</param>
    /// <returns>是否需要立即审核</returns>
    public bool RequiresImmediateReview(int urgentThreshold = 5)
    {
        return NewReportCount >= urgentThreshold ||
               Reason == CommentReportReason.HateSpeech ||
               Reason == CommentReportReason.Harassment;
    }

    /// <summary>
    /// 检查是否为严重举报
    /// </summary>
    /// <returns>是否为严重举报</returns>
    public bool IsSeriousReport()
    {
        return Reason is CommentReportReason.HateSpeech or
                        CommentReportReason.Harassment or
                        CommentReportReason.InappropriateContent;
    }

    /// <summary>
    /// 获取举报优先级
    /// </summary>
    /// <returns>优先级（1-5，5为最高）</returns>
    public int GetPriority()
    {
        return Reason switch
        {
            CommentReportReason.HateSpeech => 5,
            CommentReportReason.Harassment => 5,
            CommentReportReason.InappropriateContent => 4,
            CommentReportReason.Misinformation => 3,
            CommentReportReason.Spam => 2,
            CommentReportReason.CopyrightViolation => 3,
            CommentReportReason.Other => 1,
            _ => 1
        };
    }

    /// <summary>
    /// 获取举报原因的显示文本
    /// </summary>
    /// <returns>原因文本</returns>
    public string GetReasonText()
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
}

/// <summary>
/// 举报用户信息
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Username">用户名</param>
/// <param name="Email">邮箱</param>
/// <param name="AvatarUrl">头像URL</param>
public record ReportUser(
    Guid UserId,
    string Username,
    string? Email = null,
    string? AvatarUrl = null
);

/// <summary>
/// 被举报的评论信息
/// </summary>
/// <param name="CommentId">评论ID</param>
/// <param name="Content">处理后的评论内容</param>
/// <param name="RawContent">原始评论内容</param>
/// <param name="AuthorId">作者ID</param>
/// <param name="PostId">文章ID</param>
/// <param name="ParentId">父评论ID</param>
/// <param name="Level">评论层级</param>
/// <param name="Status">评论状态</param>
public record ReportedComment(
    Guid CommentId,
    string Content,
    string RawContent,
    Guid AuthorId,
    Guid PostId,
    Guid? ParentId,
    int Level,
    Enums.CommentStatus Status
);