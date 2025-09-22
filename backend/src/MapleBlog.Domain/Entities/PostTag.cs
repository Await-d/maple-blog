namespace MapleBlog.Domain.Entities;

/// <summary>
/// 文章标签关联实体
/// </summary>
public class PostTag
{
    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; set; }

    /// <summary>
    /// 标签ID
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// 分配者用户ID（为了兼容现有代码）
    /// </summary>
    public Guid? AssignedByUserId
    {
        get => CreatedBy;
        set => CreatedBy = value;
    }

    /// <summary>
    /// 标签顺序（在文章中的显示顺序）
    /// </summary>
    public int TagOrder { get; set; } = 0;

    // 构造函数

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public PostTag()
    {
    }

    /// <summary>
    /// 构造函数，用于创建文章标签关联
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="tagId">标签ID</param>
    /// <param name="createdBy">创建者ID</param>
    public PostTag(Guid postId, Guid tagId, Guid? createdBy = null)
    {
        PostId = postId;
        TagId = tagId;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    // 导航属性

    /// <summary>
    /// 文章
    /// </summary>
    public virtual Post? Post { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public virtual Tag? Tag { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public virtual User? Creator { get; set; }

    /// <summary>
    /// 分配者用户（别名，为了兼容现有代码）
    /// </summary>
    public virtual User? AssignedByUser => Creator;
}