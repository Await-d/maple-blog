namespace MapleBlog.Domain.Entities;

/// <summary>
/// 用户角色关联实体
/// </summary>
public class UserRole
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 分配时间
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 分配者ID
    /// </summary>
    public Guid? AssignedBy { get; set; }

    /// <summary>
    /// 到期时间（可为空表示永不过期）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    // 导航属性

    /// <summary>
    /// 用户
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    public virtual Role? Role { get; set; }

    /// <summary>
    /// 分配者
    /// </summary>
    public virtual User? Assigner { get; set; }

    // 业务方法

    /// <summary>
    /// 检查角色是否有效（未过期且激活）
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        return IsActive && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// 停用角色
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// 激活角色
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// 设置过期时间
    /// </summary>
    /// <param name="expiresAt">过期时间</param>
    public void SetExpiration(DateTime? expiresAt)
    {
        ExpiresAt = expiresAt;
    }
}