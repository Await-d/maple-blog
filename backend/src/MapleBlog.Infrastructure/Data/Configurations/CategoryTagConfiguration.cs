using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 分类实体配置
/// </summary>
public class CategoryConfiguration : BaseEntityConfiguration<Category>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        // 基本属性
        ConfigureStringProperty(builder.Property(c => c.Name), maxLength: 100, isRequired: true);
        ConfigureStringProperty(builder.Property(c => c.Slug), maxLength: 100, isRequired: true);
        ConfigureStringProperty(builder.Property(c => c.TreePath), maxLength: 500);

        // SEO字段
        ConfigureStringProperty(builder.Property(c => c.MetaTitle), maxLength: 200);
        builder.Property(c => c.MetaDescription).HasMaxLength(1000);
        ConfigureStringProperty(builder.Property(c => c.MetaKeywords), maxLength: 500);

        // 样式字段
        ConfigureStringProperty(builder.Property(c => c.Color), maxLength: 7);
        ConfigureStringProperty(builder.Property(c => c.Icon), maxLength: 50);
        ConfigureStringProperty(builder.Property(c => c.CoverImageUrl), maxLength: 500);

        // 大文本字段
        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        // 默认值
        builder.Property(c => c.Level)
            .HasDefaultValue(0);

        builder.Property(c => c.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.Property(c => c.PostCount)
            .HasDefaultValue(0);

        // 唯一索引
        ConfigureIndex(builder, new[] { nameof(Category.Slug) }, isUnique: true,
            GetFilterForSoftDelete());

        ConfigureIndex(builder, new[] { nameof(Category.Name) }, isUnique: true,
            GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Category.ParentId) });
        ConfigureIndex(builder, new[] { nameof(Category.TreePath) });
        ConfigureIndex(builder, new[] { nameof(Category.Level), nameof(Category.DisplayOrder) });
        ConfigureIndex(builder, new[] { nameof(Category.IsActive) });

        // 关系配置
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Posts)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
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

/// <summary>
/// 标签实体配置
/// </summary>
public class TagConfiguration : BaseEntityConfiguration<Tag>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        // 基本属性
        ConfigureStringProperty(builder.Property(t => t.Name), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(t => t.Slug), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(t => t.Color), maxLength: 7);

        // 大文本字段
        builder.Property(t => t.Description)
            .HasMaxLength(500);

        // 默认值
        builder.Property(t => t.UsageCount)
            .HasDefaultValue(0);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        // 唯一索引
        ConfigureIndex(builder, new[] { nameof(Tag.Name) }, isUnique: true,
            GetFilterForSoftDelete());

        ConfigureIndex(builder, new[] { nameof(Tag.Slug) }, isUnique: true,
            GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Tag.UsageCount) });
        ConfigureIndex(builder, new[] { nameof(Tag.IsActive) });

        // 关系配置
        builder.HasMany(t => t.PostTags)
            .WithOne(pt => pt.Tag)
            .HasForeignKey(pt => pt.TagId)
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

/// <summary>
/// 文章标签关联配置
/// </summary>
public class PostTagConfiguration : IEntityTypeConfiguration<PostTag>
{
    public void Configure(EntityTypeBuilder<PostTag> builder)
    {
        builder.ToTable("PostTags");

        // 复合主键
        builder.HasKey(pt => new { pt.PostId, pt.TagId });

        // 属性配置
        builder.Property(pt => pt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql(GetCurrentTimestampSql());

        // 索引
        builder.HasIndex(pt => pt.PostId);
        builder.HasIndex(pt => pt.TagId);
        builder.HasIndex(pt => pt.CreatedAt);

        // 关系配置
        builder.HasOne(pt => pt.Post)
            .WithMany(p => p.PostTags)
            .HasForeignKey(pt => pt.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Tag)
            .WithMany(t => t.PostTags)
            .HasForeignKey(pt => pt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pt => pt.Creator)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// 获取当前时间戳SQL
    /// </summary>
    /// <returns>SQL语句</returns>
    private string GetCurrentTimestampSql()
    {
        // 根据数据库提供程序返回适当的SQL
        // 这里使用通用的CURRENT_TIMESTAMP，在实际部署时会根据具体数据库调整
        return "CURRENT_TIMESTAMP";
    }
}