using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 文章附件实体配置
/// </summary>
public class PostAttachmentConfiguration : BaseEntityConfiguration<PostAttachment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.ToTable("PostAttachments");

        // 基本属性
        builder.Property(pa => pa.PostId)
            .IsRequired();

        ConfigureStringProperty(builder.Property(pa => pa.FileName), maxLength: 255, isRequired: true);
        ConfigureStringProperty(builder.Property(pa => pa.OriginalFileName), maxLength: 255, isRequired: true);
        ConfigureStringProperty(builder.Property(pa => pa.ContentType), maxLength: 100, isRequired: true);
        ConfigureStringProperty(builder.Property(pa => pa.FilePath), maxLength: 500, isRequired: true);
        ConfigureStringProperty(builder.Property(pa => pa.FileUrl), maxLength: 500);

        // 数值属性
        builder.Property(pa => pa.FileSize)
            .IsRequired();

        builder.Property(pa => pa.Width);
        builder.Property(pa => pa.Height);

        builder.Property(pa => pa.DisplayOrder)
            .HasDefaultValue(0);

        // 描述字段
        builder.Property(pa => pa.Caption)
            .HasMaxLength(500);

        builder.Property(pa => pa.Alt)
            .HasMaxLength(255);

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(PostAttachment.PostId) });
        ConfigureIndex(builder, new[] { nameof(PostAttachment.ContentType) });
        ConfigureIndex(builder, new[] { nameof(PostAttachment.DisplayOrder) });
        ConfigureIndex(builder, new[] { nameof(PostAttachment.CreatedAt) });

        // 关系配置 - 在PostConfiguration中已配置
        // builder.HasOne(pa => pa.Post)
        //     .WithMany(p => p.PostAttachments)
        //     .HasForeignKey(pa => pa.PostId)
        //     .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>
/// 文章版本实体配置
/// </summary>
public class PostRevisionConfiguration : BaseEntityConfiguration<PostRevision>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PostRevision> builder)
    {
        builder.ToTable("PostRevisions");

        // 基本属性
        builder.Property(pr => pr.PostId)
            .IsRequired();

        builder.Property(pr => pr.RevisionNumber)
            .IsRequired();

        ConfigureStringProperty(builder.Property(pr => pr.Title), maxLength: 200, isRequired: true);

        builder.Property(pr => pr.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(pr => pr.Summary)
            .HasMaxLength(1000);

        ConfigureStringProperty(builder.Property(pr => pr.ChangeReason), maxLength: 500);

        builder.Property(pr => pr.IsMajorRevision)
            .HasDefaultValue(false);

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(PostRevision.PostId), nameof(PostRevision.RevisionNumber) });
        ConfigureIndex(builder, new[] { nameof(PostRevision.PostId), nameof(PostRevision.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(PostRevision.CreatedAt) });

        // 唯一约束
        builder.HasIndex(pr => new { pr.PostId, pr.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("IX_PostRevisions_PostId_RevisionNumber_Unique");

        // 关系配置 - 在PostConfiguration中已配置
        // builder.HasOne(pr => pr.Post)
        //     .WithMany(p => p.PostRevisions)
        //     .HasForeignKey(pr => pr.PostId)
        //     .OnDelete(DeleteBehavior.Cascade);
    }
}