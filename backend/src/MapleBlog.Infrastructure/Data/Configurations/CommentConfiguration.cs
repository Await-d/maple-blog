using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;
using System.Text.Json;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 评论实体配置
/// </summary>
public class CommentConfiguration : BaseEntityConfiguration<Comment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        // 值对象配置 - CommentContent
        builder.OwnsOne(c => c.Content, contentBuilder =>
        {
            contentBuilder.Property(cc => cc.RawContent)
                .HasColumnName("RawContent")
                .IsRequired()
                .HasMaxLength(10000);

            contentBuilder.Property(cc => cc.ProcessedContent)
                .HasColumnName("ProcessedContent")
                .IsRequired()
                .HasMaxLength(15000);

            contentBuilder.Property(cc => cc.ContentType)
                .HasColumnName("ContentType")
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("markdown");

            contentBuilder.Property(cc => cc.ContainsSensitiveContent)
                .HasColumnName("ContainsSensitiveContent")
                .HasDefaultValue(false);

            contentBuilder.Property(cc => cc.Summary)
                .HasColumnName("Summary")
                .IsRequired()
                .HasMaxLength(200);
        });

        // 值对象配置 - ThreadPath
        builder.OwnsOne(c => c.ThreadPath, pathBuilder =>
        {
            pathBuilder.Property(tp => tp.Path)
                .HasColumnName("ThreadPath")
                .IsRequired()
                .HasMaxLength(500);

            pathBuilder.Property(tp => tp.Depth)
                .HasColumnName("Depth")
                .HasDefaultValue(0);

            pathBuilder.Property(tp => tp.RootId)
                .HasColumnName("RootId")
                .IsRequired();

            pathBuilder.Property(tp => tp.ParentId)
                .HasColumnName("ParentThreadId");

            pathBuilder.Property(tp => tp.CurrentId)
                .HasColumnName("CurrentThreadId")
                .IsRequired();

            // 将NodeIds存储为JSON
            pathBuilder.Property(tp => tp.NodeIds)
                .HasColumnName("NodeIds")
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                );
        });

        // 基本属性
        builder.Property(c => c.PostId)
            .IsRequired();

        builder.Property(c => c.AuthorId)
            .IsRequired();

        builder.Property(c => c.ParentId);

        // 状态和审核相关
        builder.Property(c => c.Status)
            .IsRequired()
            .HasDefaultValue(CommentStatus.Pending)
            .HasConversion<string>();

        builder.Property(c => c.ModeratedAt);
        builder.Property(c => c.ModeratedBy);

        builder.Property(c => c.ModerationNote)
            .HasMaxLength(500);

        // 统计数据
        builder.Property(c => c.LikeCount)
            .HasDefaultValue(0);

        builder.Property(c => c.ReplyCount)
            .HasDefaultValue(0);

        builder.Property(c => c.ReportCount)
            .HasDefaultValue(0);

        // 内容质量和AI审核
        builder.Property(c => c.Quality)
            .HasDefaultValue(CommentQuality.Unknown)
            .HasConversion<string>();

        builder.Property(c => c.AIModerationScore);

        builder.Property(c => c.AIModerationResult)
            .HasConversion<string>();

        builder.Property(c => c.ContainsSensitiveWords)
            .HasDefaultValue(false);

        // IP和用户代理信息
        builder.Property(c => c.IpAddress)
            .HasMaxLength(45);

        builder.Property(c => c.UserAgent)
            .HasMaxLength(500);

        // 高性能索引配置
        ConfigureCommentIndexes(builder);

        // 关系配置
        ConfigureCommentRelations(builder);
    }

    /// <summary>
    /// 配置评论索引
    /// </summary>
    private void ConfigureCommentIndexes(EntityTypeBuilder<Comment> builder)
    {
        // 基本查询索引
        ConfigureIndex(builder, new[] { nameof(Comment.PostId), nameof(Comment.Status) });
        ConfigureIndex(builder, new[] { nameof(Comment.AuthorId), nameof(Comment.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(Comment.ParentId) });

        // ThreadPath相关索引 - 暂时注释，因为值对象索引配置有问题
        // builder.HasIndex("ThreadPath_Path")
        //     .HasDatabaseName("IX_Comments_ThreadPath");

        // builder.HasIndex("ThreadPath_RootId")
        //     .HasDatabaseName("IX_Comments_RootId");

        // builder.HasIndex("ThreadPath_Depth")
        //     .HasDatabaseName("IX_Comments_Depth");

        // 复合索引用于复杂查询
        ConfigureIndex(builder, new[] { nameof(Comment.PostId), nameof(Comment.Status), nameof(Comment.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(Comment.Status), nameof(Comment.CreatedAt) });

        // 审核相关索引
        ConfigureIndex(builder, new[] { nameof(Comment.Status), nameof(Comment.ReportCount) });
        ConfigureIndex(builder, new[] { nameof(Comment.ModeratedBy), nameof(Comment.ModeratedAt) });

        // 统计索引
        ConfigureIndex(builder, new[] { nameof(Comment.LikeCount), nameof(Comment.CreatedAt) });

        // 全文搜索索引（如果支持）- 暂时注释，因为值对象属性索引有问题
        // ConfigureFullTextSearch(builder);
    }

    /// <summary>
    /// 配置全文搜索
    /// </summary>
    private void ConfigureFullTextSearch(EntityTypeBuilder<Comment> builder)
    {
        var databaseProvider = GetDatabaseProvider();

        if (databaseProvider == DatabaseProvider.PostgreSQL)
        {
            // PostgreSQL全文搜索
            builder.HasIndex("ProcessedContent");
                // .HasMethod("GIN") - PostgreSQL specific, removed
                // .IsTsVectorExpressionIndex("to_tsvector('chinese', \"ProcessedContent\")"); - PostgreSQL specific
        }
        else if (databaseProvider == DatabaseProvider.SqlServer)
        {
            // SQL Server全文搜索 - 需要在迁移中手动添加
            // builder.HasIndex("ProcessedContent")
            //     .HasDatabaseName("IX_Comments_ProcessedContent_FullText");
        }
    }

    /// <summary>
    /// 配置评论关系
    /// </summary>
    private void ConfigureCommentRelations(EntityTypeBuilder<Comment> builder)
    {
        // 与Post的关系
        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // 与User的关系
        builder.HasOne(c => c.Author)
            .WithMany() // User实体通过反向关系访问Comments，无需显式导航属性
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // 父子评论关系
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // 审核者关系
        builder.HasOne(c => c.Moderator)
            .WithMany()
            .HasForeignKey(c => c.ModeratedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // 与CommentLike的关系
        builder.HasMany(c => c.Likes)
            .WithOne(cl => cl.Comment)
            .HasForeignKey(cl => cl.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // 与CommentReport的关系
        builder.HasMany(c => c.Reports)
            .WithOne(cr => cr.Comment)
            .HasForeignKey(cr => cr.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// 评论点赞配置
/// </summary>
public class CommentLikeConfiguration : BaseEntityConfiguration<CommentLike>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CommentLike> builder)
    {
        builder.ToTable("CommentLikes");

        // 基本属性
        builder.Property(cl => cl.CommentId)
            .IsRequired();

        builder.Property(cl => cl.UserId)
            .IsRequired();

        // IP和用户代理信息
        builder.Property(cl => cl.IpAddress)
            .HasMaxLength(45);

        builder.Property(cl => cl.UserAgent)
            .HasMaxLength(500);

        // 唯一约束 - 防止重复点赞
        builder.HasIndex(cl => new { cl.CommentId, cl.UserId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0") // 只对未删除的记录应用唯一约束
            .HasDatabaseName("IX_CommentLikes_CommentId_UserId_Unique");

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(CommentLike.CommentId) });
        ConfigureIndex(builder, new[] { nameof(CommentLike.UserId), nameof(CommentLike.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(CommentLike.CreatedAt) });

        // 关系配置
        builder.HasOne(cl => cl.User)
            .WithMany() // User实体通过反向关系访问CommentLikes，无需显式导航属性
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment关系已在CommentConfiguration中配置
    }
}

/// <summary>
/// 评论举报配置
/// </summary>
public class CommentReportConfiguration : BaseEntityConfiguration<CommentReport>
{
    protected override void ConfigureEntity(EntityTypeBuilder<CommentReport> builder)
    {
        builder.ToTable("CommentReports");

        // 基本属性
        builder.Property(cr => cr.CommentId)
            .IsRequired();

        builder.Property(cr => cr.ReporterId)
            .IsRequired();

        builder.Property(cr => cr.Reason)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(cr => cr.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(cr => cr.Status)
            .HasDefaultValue(CommentReportStatus.Pending)
            .HasConversion<string>();

        // 处理信息
        builder.Property(cr => cr.ReviewedAt);
        builder.Property(cr => cr.ReviewedBy);

        builder.Property(cr => cr.Resolution)
            .HasMaxLength(2000);

        builder.Property(cr => cr.Action)
            .HasConversion<string>();

        // IP和用户代理信息
        builder.Property(cr => cr.IpAddress)
            .HasMaxLength(45);

        builder.Property(cr => cr.UserAgent)
            .HasMaxLength(500);

        // 唯一约束 - 防止重复举报
        builder.HasIndex(cr => new { cr.CommentId, cr.ReporterId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_CommentReports_CommentId_ReporterId_Unique");

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(CommentReport.Status), nameof(CommentReport.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(CommentReport.CommentId) });
        ConfigureIndex(builder, new[] { nameof(CommentReport.ReporterId) });
        ConfigureIndex(builder, new[] { nameof(CommentReport.Reason), nameof(CommentReport.Status) });
        ConfigureIndex(builder, new[] { nameof(CommentReport.ReviewedBy), nameof(CommentReport.ReviewedAt) });

        // 关系配置
        builder.HasOne(cr => cr.Reporter)
            .WithMany() // User实体通过反向关系访问CommentReports，无需显式导航属性
            .HasForeignKey(cr => cr.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.Reviewer)
            .WithMany()
            .HasForeignKey(cr => cr.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Comment关系已在CommentConfiguration中配置
    }
}