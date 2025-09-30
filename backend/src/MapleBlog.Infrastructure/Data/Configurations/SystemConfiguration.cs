using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 系统配置表配置
/// </summary>
public class SystemConfigurationConfiguration : BaseEntityConfiguration<Domain.Entities.SystemConfiguration>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Domain.Entities.SystemConfiguration> builder)
    {
        builder.ToTable("SystemConfigurations");

        // 基本属性
        ConfigureStringProperty(builder.Property(sc => sc.Section), maxLength: 100, isRequired: true);
        ConfigureStringProperty(builder.Property(sc => sc.Key), maxLength: 100, isRequired: true);
        ConfigureStringProperty(builder.Property(sc => sc.DataType), maxLength: 50);

        // 大文本字段
        builder.Property(sc => sc.Value);

        builder.Property(sc => sc.Description)
            .HasMaxLength(1000);

        // 默认值
        builder.Property(sc => sc.DataType)
            .HasDefaultValue("string");

        builder.Property(sc => sc.IsSystem)
            .HasDefaultValue(false);

        builder.Property(sc => sc.IsEncrypted)
            .HasDefaultValue(false);

        builder.Property(sc => sc.DisplayOrder)
            .HasDefaultValue(0);

        // 唯一索引
        ConfigureIndex(builder, new[] { nameof(Domain.Entities.SystemConfiguration.Section), nameof(Domain.Entities.SystemConfiguration.Key) },
            isUnique: true, GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Domain.Entities.SystemConfiguration.Section) });
        ConfigureIndex(builder, new[] { nameof(Domain.Entities.SystemConfiguration.IsSystem) });
        ConfigureIndex(builder, new[] { nameof(Domain.Entities.SystemConfiguration.DisplayOrder) });
    }

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
/// 通知配置
/// </summary>
public class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        // 基本属性
        ConfigureStringProperty(builder.Property(n => n.Title), maxLength: 200, isRequired: true);
        ConfigureStringProperty(builder.Property(n => n.RelatedEntityType), maxLength: 50);

        // 大文本字段
        builder.Property(n => n.Content)
            .HasMaxLength(2000);

        builder.Property(n => n.Data);

        // 枚举转换
        builder.Property(n => n.Type)
            .HasConversion<string>();

        builder.Property(n => n.Priority)
            .HasConversion<string>();

        // 默认值
        builder.Property(n => n.IsRead)
            .HasDefaultValue(false);

        builder.Property(n => n.Priority)
            .HasDefaultValue(NotificationPriority.Normal);

        builder.Property(n => n.SendEmail)
            .HasDefaultValue(false);

        builder.Property(n => n.SendPush)
            .HasDefaultValue(false);

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Notification.UserId), nameof(Notification.IsRead) });
        ConfigureIndex(builder, new[] { nameof(Notification.UserId), nameof(Notification.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(Notification.Type), nameof(Notification.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(Notification.RelatedEntityType), nameof(Notification.RelatedEntityId) });
        ConfigureIndex(builder, new[] { nameof(Notification.ExpiresAt) });

        // 关系配置在User中已定义
    }
}
