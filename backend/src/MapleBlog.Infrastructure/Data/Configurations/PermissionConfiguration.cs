using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 权限实体配置
/// </summary>
public class PermissionConfiguration : BaseEntityConfiguration<Permission>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        // 基本属性配置
        ConfigureStringProperty(builder.Property(p => p.Name), maxLength: 100, isRequired: true);
        ConfigureStringProperty(builder.Property(p => p.Description), maxLength: 500);
        ConfigureStringProperty(builder.Property(p => p.Resource), maxLength: 50, isRequired: true);
        ConfigureStringProperty(builder.Property(p => p.Action), maxLength: 50, isRequired: true);

        // 枚举字段配置
        builder.Property(p => p.Scope)
            .HasConversion<string>()
            .HasDefaultValue(PermissionScope.Own);

        // 布尔值默认值
        builder.Property(p => p.IsSystemPermission)
            .HasDefaultValue(false);

        // 索引配置
        // 权限名称唯一索引（在软删除过滤条件下）
        ConfigureIndex(builder, new[] { nameof(Permission.Name) }, isUnique: true,
            GetFilterForSoftDelete());

        // 资源、操作和作用域的组合唯一索引（确保同一资源的同一操作的同一作用域只有一个权限）
        ConfigureIndex(builder, new[] { nameof(Permission.Resource), nameof(Permission.Action), nameof(Permission.Scope) },
            isUnique: true, GetFilterForSoftDelete());

        // 性能索引
        ConfigureIndex(builder, new[] { nameof(Permission.Resource) });
        ConfigureIndex(builder, new[] { nameof(Permission.Action) });
        ConfigureIndex(builder, new[] { nameof(Permission.Scope) });
        ConfigureIndex(builder, new[] { nameof(Permission.IsSystemPermission) });

        // 关系配置
        // 与 RolePermission 的一对多关系
        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
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