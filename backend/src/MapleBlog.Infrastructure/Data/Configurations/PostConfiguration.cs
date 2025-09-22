using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 文章实体配置
/// </summary>
public class PostConfiguration : BaseEntityConfiguration<Post>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        // 基本属性
        ConfigureStringProperty(builder.Property(p => p.Title), maxLength: 200, isRequired: true);
        ConfigureStringProperty(builder.Property(p => p.Slug), maxLength: 200, isRequired: true);
        ConfigureStringProperty(builder.Property(p => p.ContentType), maxLength: 20, isRequired: true);
        ConfigureStringProperty(builder.Property(p => p.Language), maxLength: 10);

        // 大文本字段
        builder.Property(p => p.Summary)
            .HasMaxLength(1000);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // 无长度限制

        // SEO字段
        ConfigureStringProperty(builder.Property(p => p.MetaTitle), maxLength: 200);
        builder.Property(p => p.MetaDescription).HasMaxLength(1000);
        ConfigureStringProperty(builder.Property(p => p.MetaKeywords), maxLength: 500);
        ConfigureStringProperty(builder.Property(p => p.CanonicalUrl), maxLength: 500);

        // 社交媒体字段
        ConfigureStringProperty(builder.Property(p => p.OgTitle), maxLength: 200);
        builder.Property(p => p.OgDescription).HasMaxLength(1000);
        ConfigureStringProperty(builder.Property(p => p.OgImageUrl), maxLength: 500);

        // 默认值
        builder.Property(p => p.ContentType)
            .HasDefaultValue("markdown");

        builder.Property(p => p.Status)
            .HasDefaultValue(PostStatus.Draft)
            .HasConversion<string>();

        builder.Property(p => p.ViewCount)
            .HasDefaultValue(0);

        builder.Property(p => p.LikeCount)
            .HasDefaultValue(0);

        builder.Property(p => p.CommentCount)
            .HasDefaultValue(0);

        builder.Property(p => p.ShareCount)
            .HasDefaultValue(0);

        builder.Property(p => p.AllowComments)
            .HasDefaultValue(true);

        builder.Property(p => p.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(p => p.IsSticky)
            .HasDefaultValue(false);

        builder.Property(p => p.Language)
            .HasDefaultValue("zh-CN");

        // 唯一索引
        ConfigureIndex(builder, new[] { nameof(Post.Slug) }, isUnique: true,
            GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Post.Status), nameof(Post.PublishedAt) });
        ConfigureIndex(builder, new[] { nameof(Post.CategoryId), nameof(Post.Status) });
        ConfigureIndex(builder, new[] { nameof(Post.AuthorId), nameof(Post.Status) });
        ConfigureIndex(builder, new[] { nameof(Post.IsFeatured) });
        ConfigureIndex(builder, new[] { nameof(Post.IsSticky) });
        ConfigureIndex(builder, new[] { nameof(Post.ViewCount) });
        ConfigureIndex(builder, new[] { nameof(Post.PublishedAt) });
        ConfigureIndex(builder, new[] { nameof(Post.Status), nameof(Post.IsFeatured), nameof(Post.PublishedAt) });

        // 关系配置
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Author)
            .WithMany() // User实体通过反向关系访问Posts，无需显式导航属性
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.PostTags)
            .WithOne(pt => pt.Post)
            .HasForeignKey(pt => pt.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PostAttachments)
            .WithOne(pa => pa.Post)
            .HasForeignKey(pa => pa.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PostRevisions)
            .WithOne(pr => pr.Post)
            .HasForeignKey(pr => pr.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// 获取软删除过滤器
    /// </summary>
    /// <returns>过滤器字符串</returns>
    private string GetFilterForSoftDelete()
    {
        return GetDatabaseProvider() switch
        {
            DatabaseProvider.SqlServer => "[IsDeleted] = 0",
            DatabaseProvider.PostgreSQL => "\"IsDeleted\" = FALSE",
            DatabaseProvider.MySQL => "`IsDeleted` = FALSE",
            _ => "IsDeleted = 0"
        };
    }
}