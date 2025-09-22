namespace MapleBlog.Domain.Enums;

/// <summary>
/// 搜索类型
/// </summary>
public enum SearchType
{
    /// <summary>
    /// 全文搜索
    /// </summary>
    FullText,

    /// <summary>
    /// 精确匹配
    /// </summary>
    Exact,

    /// <summary>
    /// 模糊搜索
    /// </summary>
    Fuzzy,

    /// <summary>
    /// 前缀搜索
    /// </summary>
    Prefix,

    /// <summary>
    /// 短语匹配
    /// </summary>
    Phrase,

    /// <summary>
    /// 通配符搜索
    /// </summary>
    Wildcard
}

/// <summary>
/// 搜索排序类型
/// </summary>
public enum SearchSortType
{
    /// <summary>
    /// 相关性
    /// </summary>
    Relevance,

    /// <summary>
    /// 日期
    /// </summary>
    Date,

    /// <summary>
    /// 标题
    /// </summary>
    Title,

    /// <summary>
    /// 热度
    /// </summary>
    Popularity,

    /// <summary>
    /// 评论数
    /// </summary>
    CommentCount,

    /// <summary>
    /// 浏览数
    /// </summary>
    ViewCount
}

/// <summary>
/// 搜索状态
/// </summary>
public enum SearchStatus
{
    /// <summary>
    /// 成功
    /// </summary>
    Success,

    /// <summary>
    /// 失败
    /// </summary>
    Failed,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout,

    /// <summary>
    /// 无结果
    /// </summary>
    NoResults,

    /// <summary>
    /// 查询错误
    /// </summary>
    QueryError
}

/// <summary>
/// 内容类型
/// </summary>
public enum ContentType
{
    /// <summary>
    /// 文章
    /// </summary>
    Post,

    /// <summary>
    /// 评论
    /// </summary>
    Comment,

    /// <summary>
    /// 用户
    /// </summary>
    User,

    /// <summary>
    /// 分类
    /// </summary>
    Category,

    /// <summary>
    /// 标签
    /// </summary>
    Tag,

    /// <summary>
    /// 页面
    /// </summary>
    Page,

    /// <summary>
    /// 附件
    /// </summary>
    Attachment
}

/// <summary>
/// 索引状态
/// </summary>
public enum IndexStatus
{
    /// <summary>
    /// 待索引
    /// </summary>
    Pending,

    /// <summary>
    /// 索引中
    /// </summary>
    Indexing,

    /// <summary>
    /// 已索引
    /// </summary>
    Indexed,

    /// <summary>
    /// 索引失败
    /// </summary>
    Failed,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted,

    /// <summary>
    /// 需要更新
    /// </summary>
    NeedUpdate
}

/// <summary>
/// 归档类型
/// </summary>
public enum ArchiveType
{
    /// <summary>
    /// 按年归档
    /// </summary>
    Year,

    /// <summary>
    /// 按月归档
    /// </summary>
    Month,

    /// <summary>
    /// 按周归档
    /// </summary>
    Week,

    /// <summary>
    /// 按日归档
    /// </summary>
    Day,

    /// <summary>
    /// 按分类归档
    /// </summary>
    Category,

    /// <summary>
    /// 按标签归档
    /// </summary>
    Tag,

    /// <summary>
    /// 按作者归档
    /// </summary>
    Author
}