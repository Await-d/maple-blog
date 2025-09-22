using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 角色实体配置
/// </summary>
public class RoleConfiguration : BaseEntityConfiguration<Role>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        // 基本属性
        ConfigureStringProperty(builder.Property(r => r.Name), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(r => r.NormalizedName), maxLength: 50, isRequired: true);

        // 大文本字段
        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        // 默认值
        builder.Property(r => r.IsSystemRole)
            .HasDefaultValue(false);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        // 唯一索引
        ConfigureIndex(builder, new[] { nameof(Role.Name) }, isUnique: true,
            GetFilterForSoftDelete());

        ConfigureIndex(builder, new[] { nameof(Role.NormalizedName) }, isUnique: true,
            GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Role.IsActive) });
        ConfigureIndex(builder, new[] { nameof(Role.IsSystemRole) });

        // 关系配置
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
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