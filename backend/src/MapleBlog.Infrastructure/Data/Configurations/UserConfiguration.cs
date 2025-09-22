using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 用户实体配置
/// </summary>
public class UserConfiguration : BaseEntityConfiguration<User>
{
    protected override void ConfigureEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // 基本属性
        ConfigureStringProperty(builder.Property(u => u.UserName), maxLength: 50, isRequired: true);

        // Email value object configuration - convert to string
        builder.Property(u => u.Email)
            .HasConversion(v => v.Value, v => Email.Create(v))
            .HasMaxLength(255)
            .IsRequired();
        ConfigureStringProperty(builder.Property(u => u.PasswordHash), maxLength: 255, isRequired: true);
        ConfigureStringProperty(builder.Property(u => u.SecurityStamp), maxLength: 255, isRequired: true);

        // 可选属性
        ConfigureStringProperty(builder.Property(u => u.PhoneNumber), maxLength: 20);
        ConfigureStringProperty(builder.Property(u => u.DisplayName), maxLength: 100);
        ConfigureStringProperty(builder.Property(u => u.FirstName), maxLength: 50);
        ConfigureStringProperty(builder.Property(u => u.LastName), maxLength: 50);
        ConfigureStringProperty(builder.Property(u => u.AvatarUrl), maxLength: 500);
        ConfigureStringProperty(builder.Property(u => u.Website), maxLength: 255);
        ConfigureStringProperty(builder.Property(u => u.Location), maxLength: 100);
        ConfigureStringProperty(builder.Property(u => u.Gender), maxLength: 10);

        // 大文本字段
        builder.Property(u => u.Bio)
            .HasMaxLength(1000);

        // 布尔值默认值
        builder.Property(u => u.EmailConfirmed)
            .HasDefaultValue(false);

        builder.Property(u => u.PhoneNumberConfirmed)
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorEnabled)
            .HasDefaultValue(false);

        builder.Property(u => u.LockoutEnabled)
            .HasDefaultValue(true);

        builder.Property(u => u.AccessFailedCount)
            .HasDefaultValue(0);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.IsVerified)
            .HasDefaultValue(false);

        // 唯一索引
        ConfigureIndex(builder, new[] { nameof(User.UserName) }, isUnique: true,
            GetFilterForSoftDelete());

        ConfigureIndex(builder, new[] { nameof(User.Email) }, isUnique: true,
            GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(User.IsActive), nameof(User.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(User.LastLoginAt) });

        // 关系配置 - 为博客相关实体配置导航属性
        // 注意：由于User实体没有显式的集合导航属性，这些关系主要在目标实体中配置
        // 但我们在此处配置反向关系以确保完整性

        // 用户角色关系 - 使用枚举，无需额外配置

        // 博客实体关系 - 这些将由Post和Comment配置中的反向关系处理
        // 用户作为作者的文章
        // 配置在PostConfiguration中：Posts -> Author (User)

        // 用户的评论
        // 配置在CommentConfiguration中：Comments -> Author (User)

        // 用户的评论点赞
        // 配置在CommentLikeConfiguration中：CommentLikes -> User

        // 用户举报的评论
        // 配置在CommentReportConfiguration中：CommentReports -> Reporter (User)

        // 审核相关关系
        // 配置在CommentConfiguration中：Comments -> Moderator (User)
        // 配置在CommentReportConfiguration中：CommentReports -> Reviewer (User)

        // 搜索查询关系
        // 配置在SearchQueryConfiguration中：SearchQueries -> User (如果存在)

        // 审计日志关系
        // 配置在AuditLogConfiguration中：AuditLogs -> User (如果存在)

        // 通知关系
        // 配置在NotificationConfiguration中：Notifications -> User (如果存在)
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