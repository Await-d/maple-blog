using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 评论聚合根 - 管理评论系统的核心业务逻辑
/// </summary>
public class Comment : BaseEntity
{
    /// <summary>
    /// 关联文章ID
    /// </summary>
    [Required]
    public Guid PostId { get; set; }

    /// <summary>
    /// 评论作者ID
    /// </summary>
    [Required]
    public Guid AuthorId { get; set; }

    /// <summary>
    /// 父评论ID（用于回复功能）
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 线程路径（用于层次结构管理）
    /// </summary>
    [Required]
    public ThreadPath ThreadPath { get; set; } = null!;

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    public CommentContent Content { get; set; } = null!;

    /// <summary>
    /// 评论状态
    /// </summary>
    public CommentStatus Status { get; set; } = CommentStatus.Pending;

    /// <summary>
    /// 审核时间
    /// </summary>
    public DateTime? ModeratedAt { get; set; }

    /// <summary>
    /// 审核者ID
    /// </summary>
    public Guid? ModeratedBy { get; set; }

    /// <summary>
    /// 审核备注
    /// </summary>
    [StringLength(500)]
    public string? ModerationNote { get; set; }

    /// <summary>
    /// 审核备注列表（支持多条备注记录）
    /// </summary>
    public string? ModerationNotes { get; set; }

    // 统计数据

    /// <summary>
    /// 点赞数
    /// </summary>
    public int LikeCount { get; set; } = 0;

    /// <summary>
    /// 回复数
    /// </summary>
    public int ReplyCount { get; set; } = 0;

    /// <summary>
    /// 举报数
    /// </summary>
    public int ReportCount { get; set; } = 0;

    // 内容质量和审核相关

    /// <summary>
    /// 内容质量评分
    /// </summary>
    public CommentQuality Quality { get; set; } = CommentQuality.Unknown;

    /// <summary>
    /// AI审核置信度 (0.0-1.0)
    /// </summary>
    public double? AIModerationScore { get; set; }

    /// <summary>
    /// AI审核结果
    /// </summary>
    public ModerationResult? AIModerationResult { get; set; }

    /// <summary>
    /// 是否包含敏感词
    /// </summary>
    public bool ContainsSensitiveWords { get; set; } = false;

    /// <summary>
    /// 是否经过AI审核
    /// </summary>
    public bool AIModerated { get; set; } = false;

    /// <summary>
    /// 是否已批准（快捷属性）
    /// </summary>
    public bool IsApproved => Status == CommentStatus.Approved;

    /// <summary>
    /// 审核原因
    /// </summary>
    [StringLength(1000)]
    public string? ModerationReason { get; set; }

    // 层次结构和内容相关

    /// <summary>
    /// 评论层级深度
    /// </summary>
    public int Level => ThreadPath?.Depth ?? 0;

    /// <summary>
    /// 原始内容（未处理的文本）
    /// </summary>
    [StringLength(10000)]
    public string? RawContent { get; set; }

    /// <summary>
    /// 内容格式类型
    /// </summary>
    public ContentFormatType ContentType { get; set; } = ContentFormatType.Markdown;

    // 作者信息缓存（用于提高查询性能）

    /// <summary>
    /// 作者姓名（冗余存储以提高性能）
    /// </summary>
    [StringLength(100)]
    public string? AuthorName { get; set; }

    /// <summary>
    /// 作者邮箱（冗余存储以提高性能）
    /// </summary>
    [StringLength(256)]
    public string? AuthorEmail { get; set; }

    /// <summary>
    /// 作者头像URL（冗余存储以提高性能）
    /// </summary>
    [StringLength(500)]
    public string? AuthorAvatarUrl { get; set; }

    // IP和用户代理信息（用于反垃圾和审计）

    /// <summary>
    /// 发布者IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理字符串
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    // 导航属性

    /// <summary>
    /// 关联的文章
    /// </summary>
    public virtual Post? Post { get; set; }

    /// <summary>
    /// 评论作者
    /// </summary>
    public virtual User? Author { get; set; }

    /// <summary>
    /// 父评论
    /// </summary>
    public virtual Comment? Parent { get; set; }

    /// <summary>
    /// 子评论列表
    /// </summary>
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

    /// <summary>
    /// 子评论别名（为了兼容现有代码）
    /// </summary>
    public virtual ICollection<Comment> Children => Replies;

    /// <summary>
    /// 评论点赞记录
    /// </summary>
    public virtual ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();

    /// <summary>
    /// 评论举报记录
    /// </summary>
    public virtual ICollection<CommentReport> Reports { get; set; } = new List<CommentReport>();

    /// <summary>
    /// 审核者
    /// </summary>
    public virtual User? Moderator { get; set; }

    // 业务方法

    /// <summary>
    /// 添加回复
    /// </summary>
    /// <param name="replyContent">回复内容</param>
    /// <param name="authorId">回复者ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <returns>回复评论实例</returns>
    public Comment Reply(CommentContent replyContent, Guid authorId, string? ipAddress = null, string? userAgent = null)
    {
        var reply = new Comment
        {
            PostId = this.PostId,
            AuthorId = authorId,
            ParentId = this.Id,
            ThreadPath = this.ThreadPath.CreateChildPath(Guid.NewGuid()),
            Content = replyContent,
            Status = CommentStatus.Pending,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        // 增加回复计数
        IncreaseReplyCount();

        return reply;
    }

    /// <summary>
    /// 点赞评论
    /// </summary>
    /// <param name="userId">点赞用户ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>点赞记录</returns>
    public CommentLike Like(Guid userId, string? ipAddress = null)
    {
        // 检查是否已经点赞
        var existingLike = Likes.FirstOrDefault(l => l.UserId == userId && !l.IsDeleted);
        if (existingLike != null)
        {
            throw new InvalidOperationException("用户已经点赞过此评论");
        }

        var like = new CommentLike
        {
            CommentId = this.Id,
            UserId = userId,
            IpAddress = ipAddress
        };

        // 增加点赞计数
        IncreaseLikeCount();

        return like;
    }

    /// <summary>
    /// 取消点赞
    /// </summary>
    /// <param name="userId">取消点赞的用户ID</param>
    public void Unlike(Guid userId)
    {
        var like = Likes.FirstOrDefault(l => l.UserId == userId && !l.IsDeleted);
        if (like == null)
        {
            throw new InvalidOperationException("用户尚未点赞此评论");
        }

        like.SoftDelete(userId);
        DecreaseLikeCount();
        UpdateAuditFields(userId);
    }

    /// <summary>
    /// 举报评论
    /// </summary>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="reason">举报原因</param>
    /// <param name="description">举报描述</param>
    /// <param name="ipAddress">IP地址</param>
    /// <returns>举报记录</returns>
    public CommentReport Report(Guid reporterId, CommentReportReason reason, string description, string? ipAddress = null)
    {
        // 检查是否已经举报过
        var existingReport = Reports.FirstOrDefault(r => r.ReporterId == reporterId && !r.IsDeleted);
        if (existingReport != null)
        {
            throw new InvalidOperationException("用户已经举报过此评论");
        }

        var report = new CommentReport
        {
            CommentId = this.Id,
            ReporterId = reporterId,
            Reason = reason,
            Description = description,
            Status = CommentReportStatus.Pending,
            IpAddress = ipAddress
        };

        // 增加举报计数
        IncreaseReportCount();

        return report;
    }

    /// <summary>
    /// 审核评论
    /// </summary>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="action">审核动作</param>
    /// <param name="note">审核备注</param>
    public void Moderate(Guid moderatorId, ModerationAction action, string? note = null)
    {
        Status = action switch
        {
            ModerationAction.Approve => CommentStatus.Approved,
            ModerationAction.Hide => CommentStatus.Hidden,
            ModerationAction.MarkAsSpam => CommentStatus.Spam,
            ModerationAction.Delete => CommentStatus.Deleted,
            ModerationAction.Restore => CommentStatus.Approved,
            ModerationAction.Review => CommentStatus.Pending,
            _ => Status
        };

        ModeratedAt = DateTime.UtcNow;
        ModeratedBy = moderatorId;
        ModerationNote = note;
        UpdateAuditFields(moderatorId);

        // 如果是删除操作，执行软删除
        if (action == ModerationAction.Delete)
        {
            SoftDelete(moderatorId);
        }
        else if (action == ModerationAction.Restore && IsDeleted)
        {
            Restore(moderatorId);
        }
    }

    /// <summary>
    /// 设置AI审核结果
    /// </summary>
    /// <param name="result">审核结果</param>
    /// <param name="score">置信度分数</param>
    /// <param name="containsSensitiveWords">是否包含敏感词</param>
    public void SetAIModerationResult(ModerationResult result, double score, bool containsSensitiveWords = false)
    {
        AIModerationResult = result;
        AIModerationScore = Math.Max(0.0, Math.Min(1.0, score)); // 确保分数在0.0-1.0之间
        ContainsSensitiveWords = containsSensitiveWords;

        // 根据AI审核结果自动设置状态
        if (result == ModerationResult.Approved)
        {
            Status = CommentStatus.Approved;
        }
        else if (result is ModerationResult.RejectedSpam or ModerationResult.RejectedInappropriate
                 or ModerationResult.RejectedHateSpeech or ModerationResult.RejectedSensitiveWords)
        {
            Status = CommentStatus.Rejected;
        }
        // RequiresHumanReview 保持 Pending 状态

        UpdateAuditFields();
    }

    /// <summary>
    /// 设置内容质量评分
    /// </summary>
    /// <param name="quality">质量等级</param>
    public void SetQuality(CommentQuality quality)
    {
        Quality = quality;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查评论是否可以被公开显示
    /// </summary>
    /// <returns>是否可公开显示</returns>
    public bool IsPubliclyVisible()
    {
        return Status == CommentStatus.Approved && !IsDeleted;
    }

    /// <summary>
    /// 检查用户是否可以编辑此评论
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <returns>是否可以编辑</returns>
    public bool CanBeEditedBy(Guid userId, bool isAdmin = false)
    {
        if (IsDeleted || Status == CommentStatus.Deleted)
            return false;

        // 管理员可以编辑任何评论
        if (isAdmin)
            return true;

        // 作者可以在一定时间内编辑自己的评论
        if (AuthorId == userId)
        {
            var editWindowMinutes = 30; // 30分钟编辑窗口
            return CreatedAt.AddMinutes(editWindowMinutes) > DateTime.UtcNow;
        }

        return false;
    }

    /// <summary>
    /// 检查用户是否可以删除此评论
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="isAdmin">是否为管理员</param>
    /// <returns>是否可以删除</returns>
    public bool CanBeDeletedBy(Guid userId, bool isAdmin = false)
    {
        if (IsDeleted)
            return false;

        // 管理员和审核者可以删除任何评论
        if (isAdmin)
            return true;

        // 作者可以删除自己的评论
        return AuthorId == userId;
    }

    /// <summary>
    /// 获取层级深度
    /// </summary>
    /// <returns>层级深度</returns>
    public int GetDepth()
    {
        return ThreadPath.Depth;
    }

    /// <summary>
    /// 检查是否为根评论（非回复）
    /// </summary>
    /// <returns>是否为根评论</returns>
    public bool IsRootComment()
    {
        return ParentId == null;
    }

    /// <summary>
    /// 增加点赞数
    /// </summary>
    public void IncreaseLikeCount()
    {
        LikeCount++;
        UpdateAuditFields();
    }

    /// <summary>
    /// 减少点赞数
    /// </summary>
    public void DecreaseLikeCount()
    {
        LikeCount = Math.Max(0, LikeCount - 1);
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加回复数
    /// </summary>
    private void IncreaseReplyCount()
    {
        ReplyCount++;
        UpdateAuditFields();
    }

    /// <summary>
    /// 减少回复数
    /// </summary>
    private void DecreaseReplyCount()
    {
        ReplyCount = Math.Max(0, ReplyCount - 1);
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加举报数
    /// </summary>
    public void IncreaseReportCount()
    {
        ReportCount++;
        UpdateAuditFields();
    }

    /// <summary>
    /// 减少举报数
    /// </summary>
    private void DecreaseReportCount()
    {
        ReportCount = Math.Max(0, ReportCount - 1);
        UpdateAuditFields();
    }
}