using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 用户角色关联实体配置
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        // 复合主键
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // 属性配置
        builder.Property(ur => ur.AssignedAt)
            .IsRequired()
            .HasDefaultValueSql(GetCurrentTimestampSql());

        builder.Property(ur => ur.AssignedBy)
            .IsRequired(false);

        builder.Property(ur => ur.ExpiresAt)
            .IsRequired(false);

        builder.Property(ur => ur.IsActive)
            .HasDefaultValue(true);

        // 索引
        builder.HasIndex(ur => ur.UserId);
        builder.HasIndex(ur => ur.RoleId);
        builder.HasIndex(ur => new { ur.UserId, ur.IsActive, ur.ExpiresAt });
        builder.HasIndex(ur => ur.AssignedBy);

        // 关系配置
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Assigner)
            .WithMany()
            .HasForeignKey(ur => ur.AssignedBy)
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