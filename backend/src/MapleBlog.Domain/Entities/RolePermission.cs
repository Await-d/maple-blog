namespace MapleBlog.Domain.Entities;

/// <summary>
/// 角色权限关联实体
/// </summary>
public class RolePermission
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// 授予时间
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 授予者ID
    /// </summary>
    public Guid? GrantedBy { get; set; }

    /// <summary>
    /// 是否为临时权限
    /// </summary>
    public bool IsTemporary { get; set; } = false;

    /// <summary>
    /// 过期时间（仅临时权限有效）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性

    /// <summary>
    /// 角色
    /// </summary>
    public virtual Role? Role { get; set; }

    /// <summary>
    /// 权限
    /// </summary>
    public virtual Permission? Permission { get; set; }

    /// <summary>
    /// 授予者
    /// </summary>
    public virtual User? Granter { get; set; }

    // 业务方法

    /// <summary>
    /// 检查权限是否有效（未过期且激活）
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        if (!IsActive)
            return false;

        if (IsTemporary && ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// 设置临时权限
    /// </summary>
    /// <param name="expiresAt">过期时间</param>
    public void SetTemporary(DateTime expiresAt)
    {
        IsTemporary = true;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// 撤销权限
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
    }

    /// <summary>
    /// 激活权限
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}