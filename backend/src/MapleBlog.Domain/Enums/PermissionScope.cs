namespace MapleBlog.Domain.Enums;

/// <summary>
/// 权限作用域
/// 定义用户可以访问的数据范围层级
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// 无权限
    /// </summary>
    None = 0,

    /// <summary>
    /// 仅自己的数据
    /// </summary>
    Own = 1,

    /// <summary>
    /// 部门/分组数据
    /// </summary>
    Department = 2,

    /// <summary>
    /// 组织/机构数据
    /// </summary>
    Organization = 3,

    /// <summary>
    /// 公开数据
    /// </summary>
    Public = 4,

    /// <summary>
    /// 所有数据（全局权限）
    /// </summary>
    Global = 5
}

/// <summary>
/// 数据权限范围
/// 更细粒度的权限范围控制
/// </summary>
public enum DataPermissionScope
{
    /// <summary>
    /// 无访问权限
    /// </summary>
    None = 0,

    /// <summary>
    /// 仅自己创建的数据
    /// </summary>
    Own = 1,

    /// <summary>
    /// 部门内数据
    /// </summary>
    Department = 2,

    /// <summary>
    /// 组织内数据
    /// </summary>
    Organization = 3,

    /// <summary>
    /// 全局数据访问
    /// </summary>
    Global = 4,

    /// <summary>
    /// 自定义条件
    /// </summary>
    Custom = 5
}