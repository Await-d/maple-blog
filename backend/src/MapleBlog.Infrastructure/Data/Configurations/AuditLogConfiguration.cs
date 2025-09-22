using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 审计日志实体配置
/// </summary>
public class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // 基本字符串属性配置
        ConfigureStringProperty(builder.Property(al => al.UserName), maxLength: 100);
        ConfigureStringProperty(builder.Property(al => al.UserEmail), maxLength: 320);
        ConfigureStringProperty(builder.Property(al => al.Action), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(al => al.ResourceType), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(al => al.ResourceId), maxLength: 100);
        ConfigureStringProperty(builder.Property(al => al.ResourceName), maxLength: 200);
        ConfigureStringProperty(builder.Property(al => al.IpAddress), maxLength: 45);
        ConfigureStringProperty(builder.Property(al => al.UserAgent), maxLength: 500);
        ConfigureStringProperty(builder.Property(al => al.RequestPath), maxLength: 500);
        ConfigureStringProperty(builder.Property(al => al.HttpMethod), maxLength: 10);
        ConfigureStringProperty(builder.Property(al => al.Result), maxLength: 20, isRequired: true);
        ConfigureStringProperty(builder.Property(al => al.RiskLevel), maxLength: 20, isRequired: true);
        ConfigureStringProperty(builder.Property(al => al.Category), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(al => al.SessionId), maxLength: 100);

        // 大文本字段 - 无长度限制
        builder.Property(al => al.Description);

        builder.Property(al => al.OldValues);

        builder.Property(al => al.NewValues);

        builder.Property(al => al.ErrorMessage);

        builder.Property(al => al.AdditionalData);

        // 可选的外键属性
        builder.Property(al => al.UserId)
            .IsRequired(false);

        builder.Property(al => al.CorrelationId)
            .IsRequired(false);

        // 数值属性
        builder.Property(al => al.StatusCode)
            .IsRequired(false);

        builder.Property(al => al.Duration)
            .IsRequired(false);

        // 布尔值默认值
        builder.Property(al => al.IsSensitive)
            .HasDefaultValue(false);

        // 默认值配置
        builder.Property(al => al.Result)
            .HasDefaultValue("Success");

        builder.Property(al => al.RiskLevel)
            .HasDefaultValue("Low");

        // 性能索引配置
        ConfigureIndex(builder, new[] { nameof(AuditLog.UserId) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.Action) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.ResourceType) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.ResourceId) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.IpAddress) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.Result) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.RiskLevel) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.Category) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.IsSensitive) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.CorrelationId) });

        // 复合索引 - 提高查询性能
        ConfigureIndex(builder, new[] { nameof(AuditLog.UserId), nameof(AuditLog.CreatedAt) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.Action), nameof(AuditLog.ResourceType) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.CreatedAt), nameof(AuditLog.RiskLevel) });
        ConfigureIndex(builder, new[] { nameof(AuditLog.IsSensitive), nameof(AuditLog.CreatedAt) });

        // 关系配置
        // 与 User 的多对一关系（可选）
        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// 重写软删除配置 - 审计日志不使用软删除
    /// </summary>
    /// <param name="builder">实体构建器</param>
    protected override void ConfigureSoftDelete(EntityTypeBuilder<AuditLog> builder)
    {
        // 审计日志通常不使用软删除，而是通过数据保留策略进行管理
        // 但保留基础字段以符合 BaseEntity 要求
        base.ConfigureSoftDelete(builder);

        // 移除软删除查询过滤器，让审计日志始终可见
        // 注意：这可能需要在上下文级别处理
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