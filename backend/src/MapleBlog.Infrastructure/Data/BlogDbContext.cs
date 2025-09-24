using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using System.Reflection;
using System.Text.Json;

namespace MapleBlog.Infrastructure.Data;

/// <summary>
/// Maple Blog 数据库上下文
/// </summary>
public class BlogDbContext : DbContext
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    // 用户认证系统
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Domain.Entities.UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // 内容管理系统
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostTag> PostTags { get; set; }
    public DbSet<PostAttachment> PostAttachments { get; set; }
    public DbSet<PostRevision> PostRevisions { get; set; }

    // 评论系统
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentLike> CommentLikes { get; set; }
    public DbSet<CommentReport> CommentReports { get; set; }

    // 搜索归档
    public DbSet<SearchIndex> SearchIndexes { get; set; }
    public DbSet<SearchQuery> SearchQueries { get; set; }
    public DbSet<PopularSearch> PopularSearches { get; set; }

    // 文件系统
    public DbSet<Domain.Entities.File> Files { get; set; }

    // 安全与认证
    public DbSet<LoginHistory> LoginHistories { get; set; }

    // 管理后台
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    // 数据权限系统
    public DbSet<DataPermissionRule> DataPermissionRules { get; set; }
    public DbSet<TemporaryPermission> TemporaryPermissions { get; set; }

    // 存储配额系统
    public DbSet<StorageQuotaConfiguration> StorageQuotaConfigurations { get; set; }
    // Configuration entities
    public DbSet<Domain.Entities.Configuration> Configurations { get; set; }
    public DbSet<ConfigurationVersion> ConfigurationVersions { get; set; }
    public DbSet<ConfigurationBackup> ConfigurationBackups { get; set; } = null!;
    public DbSet<ConfigurationTemplate> ConfigurationTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 应用所有配置
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // 配置软删除全局过滤器
        ConfigureSoftDeleteFilter(modelBuilder);

        // 配置枚举转换
        ConfigureEnumConversions(modelBuilder);

        // 配置JSON字段
        ConfigureJsonFields(modelBuilder);

        // 设置默认的字符串长度
        ConfigureStringDefaults(modelBuilder);
    }

    /// <summary>
    /// 配置软删除全局过滤器
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void ConfigureSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(GenerateSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    /// <summary>
    /// 生成软删除过滤器表达式
    /// </summary>
    /// <param name="type">实体类型</param>
    /// <returns>过滤器表达式</returns>
    private static System.Linq.Expressions.LambdaExpression GenerateSoftDeleteFilter(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        var condition = System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(condition, parameter);
    }

    /// <summary>
    /// 配置枚举转换
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void ConfigureEnumConversions(ModelBuilder modelBuilder)
    {
        // 配置Post状态枚举
        modelBuilder.Entity<Post>()
            .Property(p => p.Status)
            .HasConversion<string>();

        // 配置Comment状态枚举
        modelBuilder.Entity<Comment>()
            .Property(c => c.Status)
            .HasConversion<string>();

        // 配置审计日志动作枚举
        modelBuilder.Entity<AuditLog>()
            .Property(al => al.Action)
            .HasConversion<string>();

        // 配置日志风险级别枚举
        modelBuilder.Entity<AuditLog>()
            .Property(al => al.RiskLevel)
            .HasConversion<string>();

        // 配置通知类型枚举
        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>();

        // 配置通知优先级枚举
        modelBuilder.Entity<Notification>()
            .Property(n => n.Priority)
            .HasConversion<string>();

        // 配置登录历史枚举
        modelBuilder.Entity<LoginHistory>()
            .Property(lh => lh.Result)
            .HasConversion<string>();

        modelBuilder.Entity<LoginHistory>()
            .Property(lh => lh.LoginType)
            .HasConversion<string>();
    }

    /// <summary>
    /// 配置JSON字段
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void ConfigureJsonFields(ModelBuilder modelBuilder)
    {
        // 忽略 List<object> 和其他泛型类型，防止被EF Core意外映射为实体
        modelBuilder.Ignore<List<object>>();
        modelBuilder.Ignore<Dictionary<string, object>>();

        // 根据数据库提供程序配置JSON字段
        if (false) // Database.IsNpgsql() removed due to compatibility
        {
            // PostgreSQL 使用 JSONB
            modelBuilder.Entity<SearchQuery>()
                .Property(e => e.Filters)
                .HasColumnType("jsonb");

            modelBuilder.Entity<SearchQuery>()
                .Property(e => e.ClickedResults)
                .HasColumnType("jsonb");

            modelBuilder.Entity<Notification>()
                .Property(e => e.Data)
                .HasColumnType("jsonb");
        }
        else if (Database.IsSqlServer())
        {
            // SQL Server 使用 NVARCHAR(MAX)
            modelBuilder.Entity<SearchQuery>()
                .Property(e => e.Filters)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            modelBuilder.Entity<SearchQuery>()
                .Property(e => e.ClickedResults)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<object>>(v, (JsonSerializerOptions?)null));

            modelBuilder.Entity<Notification>()
                .Property(e => e.Data)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v);
        }
        else
        {
            // SQLite 和其他数据库使用 TEXT
            modelBuilder.Entity<SearchQuery>()
                .Property(e => e.Filters)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            modelBuilder.Entity<SearchQuery>()
                .Property(e => e.ClickedResults)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<object>>(v, (JsonSerializerOptions?)null));
        }
    }

    /// <summary>
    /// 配置字符串默认设置
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private void ConfigureStringDefaults(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    // 设置默认字符串长度
                    if (property.GetMaxLength() == null)
                    {
                        property.SetMaxLength(500);
                    }

                    // 对于某些特殊字段设置更长的长度
                    var propertyName = property.Name.ToLowerInvariant();
                    if (propertyName.Contains("content") || propertyName.Contains("description") ||
                        propertyName.Contains("text") || propertyName == "bio")
                    {
                        property.SetMaxLength(null); // 无长度限制
                    }
                }
            }
        }
    }

    /// <summary>
    /// 保存更改前的预处理
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 更新审计字段
        UpdateAuditFields();

        // 保存更改
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 更新审计字段
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                // 在实际应用中，应该从当前用户上下文中获取用户ID
                // entry.Entity.CreatedBy = GetCurrentUserId();
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                // entry.Entity.UpdatedBy = GetCurrentUserId();
            }
        }
    }

    /// <summary>
    /// 忽略软删除过滤器
    /// </summary>
    /// <returns>禁用软删除过滤器的DbContext</returns>
    public IQueryable<T> IgnoreSoftDeleteFilter<T>() where T : BaseEntity
    {
        return Set<T>().IgnoreQueryFilters();
    }

    /// <summary>
    /// 硬删除实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体</param>
    /// <returns>删除的实体</returns>
    public EntityEntry<T> HardDelete<T>(T entity) where T : BaseEntity
    {
        return Set<T>().Remove(entity);
    }

    /// <summary>
    /// 软删除实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="deletedBy">删除者ID</param>
    /// <returns>任务</returns>
    public Task SoftDeleteAsync<T>(T entity, Guid? deletedBy = null) where T : BaseEntity
    {
        entity.SoftDelete(deletedBy);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 恢复软删除的实体
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体</param>
    /// <param name="restoredBy">恢复者ID</param>
    /// <returns>任务</returns>
    public Task RestoreAsync<T>(T entity, Guid? restoredBy = null) where T : BaseEntity
    {
        entity.Restore(restoredBy);
        return Task.CompletedTask;
    }
}