using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 权限实体
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// 权限名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 显示名称（用于UI展示）
    /// </summary>
    [StringLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 权限分类
    /// </summary>
    [StringLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// 资源名称（如 Posts, Users, Comments）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// 操作名称（如 Create, Read, Update, Delete）
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 权限作用域
    /// </summary>
    public PermissionScope Scope { get; set; } = PermissionScope.Own;

    /// <summary>
    /// 是否为系统权限（系统权限不可删除）
    /// </summary>
    public bool IsSystemPermission { get; set; } = false;

    // 导航属性

    /// <summary>
    /// 角色权限关联
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // 业务方法

    /// <summary>
    /// 获取权限的完整名称（Resource.Action格式）
    /// </summary>
    /// <returns>完整权限名称</returns>
    public string GetFullName()
    {
        return $"{Resource}.{Action}";
    }

    /// <summary>
    /// 检查是否匹配指定的资源和操作
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <returns>是否匹配</returns>
    public bool Matches(string resource, string action)
    {
        return string.Equals(Resource, resource, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Action, action, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 设置资源和操作（自动更新权限名称）
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="scope">权限作用域</param>
    public void SetResourceAction(string resource, string action, PermissionScope scope = PermissionScope.Own)
    {
        Resource = resource?.Trim() ?? string.Empty;
        Action = action?.Trim() ?? string.Empty;
        Scope = scope;
        Name = GetFullName();
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查是否匹配指定的资源、操作和作用域
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="scope">权限作用域</param>
    /// <returns>是否匹配</returns>
    public bool Matches(string resource, string action, PermissionScope scope)
    {
        return string.Equals(Resource, resource, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Action, action, StringComparison.OrdinalIgnoreCase) &&
               Scope == scope;
    }

    /// <summary>
    /// 检查权限是否涵盖指定的作用域（权限继承）
    /// </summary>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="requiredScope">所需的作用域</param>
    /// <returns>是否涵盖</returns>
    public bool Covers(string resource, string action, PermissionScope requiredScope)
    {
        if (!string.Equals(Resource, resource, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Action, action, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Global权限涵盖所有作用域
        if (Scope == PermissionScope.Global)
            return true;

        // Department权限涵盖Own
        if (Scope == PermissionScope.Department && requiredScope == PermissionScope.Own)
            return true;

        // 精确匹配
        return Scope == requiredScope;
    }
}