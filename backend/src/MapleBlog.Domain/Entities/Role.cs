using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 角色实体
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标准化的角色名称（用于查询优化）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 显示名称（用于UI展示）
    /// </summary>
    [StringLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 是否为系统角色（系统角色不可删除）
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性

    /// <summary>
    /// 角色与用户的关联
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// 角色权限
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // 业务方法

    /// <summary>
    /// 设置角色名称（自动设置标准化名称）
    /// </summary>
    /// <param name="name">角色名称</param>
    public void SetName(string name)
    {
        Name = name?.Trim() ?? string.Empty;
        NormalizedName = Name.ToUpperInvariant();
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查角色是否拥有指定权限
    /// </summary>
    /// <param name="permissionName">权限名称</param>
    /// <returns>是否拥有权限</returns>
    public bool HasPermission(string permissionName)
    {
        return RolePermissions.Any(rp => rp.Permission != null &&
                                        string.Equals(rp.Permission.Name, permissionName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    /// <returns>权限列表</returns>
    public IEnumerable<Permission> GetPermissions()
    {
        return RolePermissions
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!)
            .ToList();
    }

    /// <summary>
    /// 获取角色的用户数量
    /// </summary>
    /// <returns>用户数量</returns>
    public int GetUserCount()
    {
        return UserRoles.Count(ur => ur.IsActive &&
                                    (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow));
    }
}