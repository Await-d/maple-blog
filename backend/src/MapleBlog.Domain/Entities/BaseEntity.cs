using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 基础实体类，包含审计字段和软删除支持
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// 版本号（用于乐观并发控制）
    /// </summary>
    [Timestamp]
    public byte[]? Version { get; set; }

    /// <summary>
    /// 是否已删除（软删除标记）
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 删除者ID
    /// </summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// 标记实体为已删除
    /// </summary>
    /// <param name="deletedBy">删除者ID</param>
    public virtual void SoftDelete(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }

    /// <summary>
    /// 恢复已删除的实体
    /// </summary>
    /// <param name="restoredBy">恢复者ID</param>
    public virtual void Restore(Guid? restoredBy = null)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = restoredBy;
    }

    /// <summary>
    /// 更新实体的审计字段
    /// </summary>
    /// <param name="updatedBy">更新者ID</param>
    public virtual void UpdateAuditFields(Guid? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}

/// <summary>
/// 可审计实体接口
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>
/// 软删除实体接口
/// </summary>
public interface ISoftDeletableEntity
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
    void SoftDelete(Guid? deletedBy = null);
    void Restore(Guid? restoredBy = null);
}

/// <summary>
/// 版本控制实体接口
/// </summary>
public interface IVersionedEntity
{
    byte[]? Version { get; set; }
}