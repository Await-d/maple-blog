using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 数据权限规则实体
/// 定义用户对特定资源的数据访问权限规则
/// </summary>
public class DataPermissionRule : BaseEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户实体
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 角色ID（可选，用于角色级权限）
    /// </summary>
    public Guid? RoleId { get; set; }

    /// <summary>
    /// 角色实体
    /// </summary>
    public Role? Role { get; set; }

    /// <summary>
    /// 资源类型（如：Posts, Users, Comments等）
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 具体资源ID（可选，用于特定资源权限）
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// 数据操作类型
    /// </summary>
    public DataOperation Operation { get; set; }

    /// <summary>
    /// 权限范围
    /// </summary>
    public DataPermissionScope Scope { get; set; }

    /// <summary>
    /// 权限条件表达式（JSON格式）
    /// 用于复杂的条件判断，如：{"CreatedBy": "{UserId}", "IsPublished": true}
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// 是否允许访问
    /// </summary>
    public bool IsAllowed { get; set; } = true;

    /// <summary>
    /// 优先级（数字越大优先级越高）
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 规则生效时间
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// 规则失效时间
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 是否是临时权限
    /// </summary>
    public bool IsTemporary { get; set; } = false;

    /// <summary>
    /// 授权者ID（用于临时权限和委派权限）
    /// </summary>
    public Guid? GrantedBy { get; set; }

    /// <summary>
    /// 授权者实体
    /// </summary>
    public User? GrantedByUser { get; set; }

    /// <summary>
    /// 权限来源类型
    /// </summary>
    public PermissionSource Source { get; set; } = PermissionSource.Direct;

    /// <summary>
    /// 备注说明
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// 检查规则是否生效
    /// </summary>
    /// <returns>是否生效</returns>
    public bool IsEffective()
    {
        var now = DateTime.UtcNow;

        if (EffectiveFrom.HasValue && now < EffectiveFrom.Value)
            return false;

        if (EffectiveTo.HasValue && now > EffectiveTo.Value)
            return false;

        return IsActive;
    }

    /// <summary>
    /// 检查是否匹配指定的资源和操作
    /// </summary>
    /// <param name="resourceType">资源类型</param>
    /// <param name="operation">操作类型</param>
    /// <param name="resourceId">资源ID</param>
    /// <returns>是否匹配</returns>
    public bool Matches(string resourceType, DataOperation operation, Guid? resourceId = null)
    {
        if (!IsEffective())
            return false;

        if (!string.Equals(ResourceType, resourceType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Operation != operation)
            return false;

        // 如果规则指定了具体资源ID，则必须匹配
        if (ResourceId.HasValue && ResourceId.Value != resourceId)
            return false;

        return true;
    }
}

/// <summary>
/// 权限来源类型
/// </summary>
public enum PermissionSource
{
    /// <summary>
    /// 直接分配
    /// </summary>
    Direct = 1,

    /// <summary>
    /// 角色继承
    /// </summary>
    Role = 2,

    /// <summary>
    /// 临时授权
    /// </summary>
    Temporary = 3,

    /// <summary>
    /// 权限委派
    /// </summary>
    Delegated = 4,

    /// <summary>
    /// 系统默认
    /// </summary>
    System = 5
}