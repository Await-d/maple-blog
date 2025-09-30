using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations;

/// <summary>
/// 基础实体配置
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        // 主键配置
        builder.HasKey(e => e.Id);

        // 根据数据库提供程序配置主键
        ConfigurePrimaryKey(builder);

        // 审计字段配置
        ConfigureAuditFields(builder);

        // 软删除配置
        ConfigureSoftDelete(builder);

        // 版本控制配置
        ConfigureConcurrency(builder);

        // 自定义配置
        ConfigureEntity(builder);
    }

    /// <summary>
    /// 配置主键
    /// </summary>
    /// <param name="builder">实体构建器</param>
    protected virtual void ConfigurePrimaryKey(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        // 根据数据库提供程序设置默认值
        var databaseProvider = GetDatabaseProvider();
        switch (databaseProvider)
        {
            case DatabaseProvider.PostgreSQL:
                builder.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");
                break;
            case DatabaseProvider.SqlServer:
                builder.Property(e => e.Id)
                    .HasDefaultValueSql("NEWID()");
                break;
            case DatabaseProvider.MySQL:
                // MySQL 8.0+ 支持 UUID() 函数
                builder.Property(e => e.Id)
                    .HasConversion<string>()
                    .HasDefaultValueSql("(UUID())");
                break;
            case DatabaseProvider.SQLite:
            default:
                // SQLite 存储为字符串
                builder.Property(e => e.Id)
                    .HasConversion<string>();
                break;
        }
    }

    /// <summary>
    /// 配置审计字段
    /// </summary>
    /// <param name="builder">实体构建器</param>
    protected virtual void ConfigureAuditFields(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql(GetCurrentTimestampSql());

        builder.Property(e => e.CreatedBy)
            .IsRequired(false);

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.UpdatedBy)
            .IsRequired(false);
    }

    /// <summary>
    /// 配置软删除
    /// </summary>
    /// <param name="builder">实体构建器</param>
    protected virtual void ConfigureSoftDelete(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);

        builder.Property(e => e.DeletedBy)
            .IsRequired(false);

        // 软删除过滤器
        builder.HasQueryFilter(e => !e.IsDeleted);

        // 软删除索引
        var databaseProvider = GetDatabaseProvider();
        if (databaseProvider == DatabaseProvider.SqlServer)
        {
            builder.HasIndex(e => e.IsDeleted)
                .HasFilter("[IsDeleted] = 0");
        }
        else if (databaseProvider == DatabaseProvider.PostgreSQL)
        {
            builder.HasIndex(e => e.IsDeleted)
                .HasFilter("\"IsDeleted\" = FALSE");
        }
        else
        {
            builder.HasIndex(e => e.IsDeleted);
        }
    }

    /// <summary>
    /// 配置并发控制
    /// </summary>
    /// <param name="builder">实体构建器</param>
    protected virtual void ConfigureConcurrency(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.Version)
            .IsRowVersion()
            .IsRequired(false);
    }

    /// <summary>
    /// 配置实体特定设置（子类重写）
    /// </summary>
    /// <param name="builder">实体构建器</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);

    /// <summary>
    /// 获取当前时间戳SQL
    /// </summary>
    /// <returns>SQL语句</returns>
    protected virtual string GetCurrentTimestampSql()
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
    protected virtual DatabaseProvider GetDatabaseProvider()
    {
        // TODO: 后续可通过配置或上下文解析真实提供程序
        return DatabaseProvider.SQLite;
    }

    /// <summary>
    /// 配置字符串属性
    /// </summary>
    /// <param name="builder">属性构建器</param>
    /// <param name="maxLength">最大长度</param>
    /// <param name="isRequired">是否必需</param>
    /// <param name="isUnicode">是否Unicode</param>
    protected void ConfigureStringProperty(
        PropertyBuilder<string> builder,
        int? maxLength = null,
        bool isRequired = false,
        bool isUnicode = true)
    {
        builder.IsRequired(isRequired);

        if (maxLength.HasValue)
        {
            builder.HasMaxLength(maxLength.Value);
        }

        builder.IsUnicode(isUnicode);
    }

    /// <summary>
    /// 配置索引
    /// </summary>
    /// <param name="builder">实体构建器</param>
    /// <param name="propertyNames">属性名称</param>
    /// <param name="isUnique">是否唯一</param>
    /// <param name="filter">过滤器</param>
    protected void ConfigureIndex(
        EntityTypeBuilder<T> builder,
        string[] propertyNames,
        bool isUnique = false,
        string? filter = null)
    {
        var indexBuilder = builder.HasIndex(propertyNames);

        if (isUnique)
        {
            indexBuilder.IsUnique();
        }

        if (!string.IsNullOrEmpty(filter))
        {
            indexBuilder.HasFilter(filter);
        }
    }
}

/// <summary>
/// 数据库提供程序枚举
/// </summary>
public enum DatabaseProvider
{
    SQLite,
    PostgreSQL,
    SqlServer,
    MySQL,
    Oracle
}
