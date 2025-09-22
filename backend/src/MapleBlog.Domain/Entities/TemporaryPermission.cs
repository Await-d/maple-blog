using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 临时权限实体
/// 用于管理临时授权和权限委派
/// </summary>
public class TemporaryPermission : BaseEntity
{
    /// <summary>
    /// 被授权用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 被授权用户
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// 资源类型
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 资源ID
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public DataOperation Operation { get; set; }

    /// <summary>
    /// 权限生效时间
    /// </summary>
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 权限过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 授权者ID
    /// </summary>
    public Guid GrantedBy { get; set; }

    /// <summary>
    /// 授权者
    /// </summary>
    public User GrantedByUser { get; set; } = null!;

    /// <summary>
    /// 委派来源用户ID（用于权限委派）
    /// </summary>
    public Guid? DelegatedFrom { get; set; }

    /// <summary>
    /// 委派来源用户
    /// </summary>
    public User? DelegatedFromUser { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    public TemporaryPermissionType Type { get; set; } = TemporaryPermissionType.Temporary;

    /// <summary>
    /// 授权原因
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 撤销者ID
    /// </summary>
    public Guid? RevokedBy { get; set; }

    /// <summary>
    /// 撤销者
    /// </summary>
    public User? RevokedByUser { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    public string? RevokeReason { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 使用次数限制（0表示无限制）
    /// </summary>
    public int UsageLimit { get; set; } = 0;

    /// <summary>
    /// 已使用次数
    /// </summary>
    public int UsedCount { get; set; } = 0;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 检查权限是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsValid()
    {
        if (IsRevoked || !IsActive)
            return false;

        var now = DateTime.UtcNow;

        if (now < EffectiveFrom || now > ExpiresAt)
            return false;

        if (UsageLimit > 0 && UsedCount >= UsageLimit)
            return false;

        return true;
    }

    /// <summary>
    /// 记录权限使用
    /// </summary>
    public void RecordUsage()
    {
        UsedCount++;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 撤销权限
    /// </summary>
    /// <param name="revokedBy">撤销者ID</param>
    /// <param name="reason">撤销原因</param>
    public void Revoke(Guid revokedBy, string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedBy = revokedBy;
        RevokeReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// 临时权限类型
/// </summary>
public enum TemporaryPermissionType
{
    /// <summary>
    /// 临时授权
    /// </summary>
    Temporary = 1,

    /// <summary>
    /// 权限委派
    /// </summary>
    Delegated = 2,

    /// <summary>
    /// 紧急访问
    /// </summary>
    Emergency = 3,

    /// <summary>
    /// 审批后临时权限
    /// </summary>
    Approved = 4
}