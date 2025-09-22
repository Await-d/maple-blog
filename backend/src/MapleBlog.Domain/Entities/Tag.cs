using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 标签实体
/// </summary>
public class Tag : BaseEntity
{
    /// <summary>
    /// 标签名称
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL友好的标识符
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 标签描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// SEO元描述
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// 标签颜色（十六进制颜色值）
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// 使用次数（冗余字段，用于性能优化）
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 使用次数别名（为了兼容现有代码）
    /// </summary>
    public int UseCount
    {
        get => UsageCount;
        set => UsageCount = value;
    }

    /// <summary>
    /// 文章数量别名（为了兼容现有代码）
    /// </summary>
    public int PostCount
    {
        get => UsageCount;
        set => UsageCount = value;
    }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 导航属性

    /// <summary>
    /// 文章标签关联
    /// </summary>
    public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();

    // 计算属性

    /// <summary>
    /// 使用该标签的所有文章（计算属性）
    /// </summary>
    public IEnumerable<Post> Posts => PostTags.Where(pt => pt.Post != null).Select(pt => pt.Post!);

    // 业务方法

    /// <summary>
    /// 设置标签名称和Slug
    /// </summary>
    /// <param name="name">标签名称</param>
    /// <param name="slug">URL标识符</param>
    public void SetNameAndSlug(string name, string? slug = null)
    {
        Name = name?.Trim() ?? string.Empty;
        Slug = slug?.Trim() ?? GenerateSlug(Name);
        DisplayName = Name; // Set DisplayName same as Name by default
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加使用次数
    /// </summary>
    /// <param name="count">增加数量</param>
    public void IncreaseUsageCount(int count = 1)
    {
        UsageCount += count;
        UpdateAuditFields();
    }

    /// <summary>
    /// 减少使用次数
    /// </summary>
    /// <param name="count">减少数量</param>
    public void DecreaseUsageCount(int count = 1)
    {
        UsageCount = Math.Max(0, UsageCount - count);
        UpdateAuditFields();
    }

    /// <summary>
    /// 获取使用该标签的所有文章
    /// </summary>
    /// <returns>文章列表</returns>
    public IEnumerable<Post> GetPosts()
    {
        return PostTags.Where(pt => pt.Post != null).Select(pt => pt.Post!);
    }

    /// <summary>
    /// 激活标签
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdateAuditFields();
    }

    /// <summary>
    /// 停用标签
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdateAuditFields();
    }

    /// <summary>
    /// 更新使用次数
    /// </summary>
    /// <param name="newCount">新的使用次数</param>
    public void UpdateUseCount(int newCount)
    {
        UsageCount = Math.Max(0, newCount);
        UpdateAuditFields();
    }

    /// <summary>
    /// 生成URL友好的Slug
    /// </summary>
    /// <param name="name">标签名称</param>
    /// <returns>URL标识符</returns>
    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.ToLowerInvariant()
                  .Replace(" ", "-")
                  .Replace("_", "-")
                  .Trim('-');
    }
}