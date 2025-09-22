namespace MapleBlog.Domain.Enums;

/// <summary>
/// 数据操作类型
/// 定义对数据可以执行的各种操作
/// </summary>
public enum DataOperation
{
    /// <summary>
    /// 创建新数据
    /// </summary>
    Create = 1,

    /// <summary>
    /// 读取/查看数据
    /// </summary>
    Read = 2,

    /// <summary>
    /// 更新/修改数据
    /// </summary>
    Update = 3,

    /// <summary>
    /// 删除数据
    /// </summary>
    Delete = 4,

    /// <summary>
    /// 列表查询
    /// </summary>
    List = 5,

    /// <summary>
    /// 导出数据
    /// </summary>
    Export = 6,

    /// <summary>
    /// 审核/审批
    /// </summary>
    Approve = 7,

    /// <summary>
    /// 发布/上线
    /// </summary>
    Publish = 8,

    /// <summary>
    /// 归档
    /// </summary>
    Archive = 9,

    /// <summary>
    /// 管理权限
    /// </summary>
    Manage = 10
}