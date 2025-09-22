using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 角色权限关联实体配置
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        // 外键属性配置
        builder.Property(rp => rp.RoleId)
            .IsRequired();

        builder.Property(rp => rp.PermissionId)
            .IsRequired();

        builder.Property(rp => rp.GrantedBy)
            .IsRequired(false);

        // 时间字段配置
        builder.Property(rp => rp.GrantedAt)
            .IsRequired()
            .HasDefaultValueSql(GetCurrentTimestampSql());

        builder.Property(rp => rp.ExpiresAt)
            .IsRequired(false);

        // 布尔字段配置
        builder.Property(rp => rp.IsTemporary)
            .HasDefaultValue(false);

        builder.Property(rp => rp.IsActive)
            .HasDefaultValue(true);

        // 复合主键配置（角色ID + 权限ID）
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // 性能索引
        builder.HasIndex(rp => rp.RoleId);
        builder.HasIndex(rp => rp.PermissionId);
        builder.HasIndex(rp => rp.IsActive);
        builder.HasIndex(rp => rp.IsTemporary);
        builder.HasIndex(rp => rp.ExpiresAt);

        // 关系配置已在 Role 和 Permission 的配置中定义
        // 这里确保外键约束正确配置
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 与授予者的关系
        builder.HasOne(rp => rp.Granter)
            .WithMany()
            .HasForeignKey(rp => rp.GrantedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }

    /// <summary>
    /// 获取当前时间戳SQL
    /// </summary>
    /// <returns>SQL语句</returns>
    private string GetCurrentTimestampSql()
    {
        return GetDatabaseProvider() switch
        {
            DatabaseProvider.SQLite => "datetime('now')",
            DatabaseProvider.PostgreSQL => "CURRENT_TIMESTAMP",
            DatabaseProvider.MySQL => "CURRENT_TIMESTAMP(6)",
            DatabaseProvider.SqlServer => "GETUTCDATE()",
            DatabaseProvider.Oracle => "SYSTIMESTAMP",
            _ => "CURRENT_TIMESTAMP"
        };
    }

    /// <summary>
    /// 获取数据库提供程序
    /// </summary>
    /// <returns>数据库提供程序</returns>
    private DatabaseProvider GetDatabaseProvider()
    {
        // 简化实现，实际项目中应该从配置中读取
        return DatabaseProvider.SQLite;
    }
}