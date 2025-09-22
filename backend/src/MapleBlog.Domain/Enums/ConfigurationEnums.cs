namespace MapleBlog.Domain.Enums;

/// <summary>
/// 配置变更类型
/// </summary>
public enum ConfigurationChangeType
{
    /// <summary>
    /// 创建
    /// </summary>
    Create = 1,

    /// <summary>
    /// 更新
    /// </summary>
    Update = 2,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 3,

    /// <summary>
    /// 恢复
    /// </summary>
    Restore = 4,

    /// <summary>
    /// 回滚
    /// </summary>
    Rollback = 5,

    /// <summary>
    /// 导入
    /// </summary>
    Import = 6,

    /// <summary>
    /// 导出
    /// </summary>
    Export = 7
}

/// <summary>
/// 配置审批状态
/// </summary>
public enum ConfigurationApprovalStatus
{
    /// <summary>
    /// 待审批
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已审批
    /// </summary>
    Approved = 2,

    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已撤回
    /// </summary>
    Withdrawn = 4,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 5
}

/// <summary>
/// 配置重要性级别
/// </summary>
public enum ConfigurationCriticality
{
    /// <summary>
    /// 低 - 无需审批
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中 - 需要审批
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高 - 需要高级审批
    /// </summary>
    High = 3,

    /// <summary>
    /// 关键 - 需要多级审批
    /// </summary>
    Critical = 4
}

/// <summary>
/// 配置数据类型
/// </summary>
public enum ConfigurationDataType
{
    /// <summary>
    /// 字符串
    /// </summary>
    String = 1,

    /// <summary>
    /// 整数
    /// </summary>
    Integer = 2,

    /// <summary>
    /// 小数
    /// </summary>
    Decimal = 3,

    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// 日期时间
    /// </summary>
    DateTime = 5,

    /// <summary>
    /// JSON对象
    /// </summary>
    Json = 6,

    /// <summary>
    /// 数组
    /// </summary>
    Array = 7,

    /// <summary>
    /// 文件路径
    /// </summary>
    FilePath = 8,

    /// <summary>
    /// URL
    /// </summary>
    Url = 9,

    /// <summary>
    /// 邮箱
    /// </summary>
    Email = 10,

    /// <summary>
    /// 密码 (加密)
    /// </summary>
    Password = 11,

    /// <summary>
    /// 密钥 (加密)
    /// </summary>
    SecretKey = 12
}

/// <summary>
/// 配置模板类型
/// </summary>
public enum ConfigurationTemplateType
{
    /// <summary>
    /// 站点基础配置
    /// </summary>
    SiteBasic = 1,

    /// <summary>
    /// 数据库配置
    /// </summary>
    Database = 2,

    /// <summary>
    /// 缓存配置
    /// </summary>
    Cache = 3,

    /// <summary>
    /// 邮件配置
    /// </summary>
    Email = 4,

    /// <summary>
    /// 文件存储配置
    /// </summary>
    FileStorage = 5,

    /// <summary>
    /// 第三方集成配置
    /// </summary>
    ThirdPartyIntegration = 6,

    /// <summary>
    /// 安全配置
    /// </summary>
    Security = 7,

    /// <summary>
    /// 监控配置
    /// </summary>
    Monitoring = 8,

    /// <summary>
    /// 功能开关
    /// </summary>
    FeatureFlag = 9,

    /// <summary>
    /// 自定义配置
    /// </summary>
    Custom = 10
}