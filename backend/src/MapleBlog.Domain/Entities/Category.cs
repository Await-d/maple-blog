using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 分类实体
/// </summary>
public class Category : BaseEntity
{
    /// <summary>
    /// 分类名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL友好的标识符
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 分类描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 父分类ID（用于层级分类）
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 层级路径（用于高效查询层级结构）
    /// </summary>
    [StringLength(500)]
    public string? TreePath { get; set; }

    /// <summary>
    /// 层级等级（0表示根分类）
    /// </summary>
    public int Level { get; set; } = 0;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// 排序顺序（别名，为了兼容现有代码）
    /// </summary>
    public int SortOrder
    {
        get => DisplayOrder;
        set => DisplayOrder = value;
    }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 文章数量（冗余字段，用于性能优化）
    /// </summary>
    public int PostCount { get; set; } = 0;

    // SEO字段

    /// <summary>
    /// SEO标题
    /// </summary>
    [StringLength(200)]
    public string? MetaTitle { get; set; }

    /// <summary>
    /// SEO描述
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// SEO关键词
    /// </summary>
    [StringLength(500)]
    public string? MetaKeywords { get; set; }

    // 样式配置

    /// <summary>
    /// 分类颜色（十六进制颜色值）
    /// </summary>
    [StringLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// 分类图标
    /// </summary>
    [StringLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// 封面图片URL
    /// </summary>
    [StringLength(500)]
    public string? CoverImageUrl { get; set; }

    // 导航属性

    /// <summary>
    /// 父分类
    /// </summary>
    public virtual Category? Parent { get; set; }

    /// <summary>
    /// 子分类列表
    /// </summary>
    public virtual ICollection<Category> Children { get; set; } = new List<Category>();

    /// <summary>
    /// 分类下的文章
    /// </summary>
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    // 业务方法

    /// <summary>
    /// 设置分类名称和Slug
    /// </summary>
    /// <param name="name">分类名称</param>
    /// <param name="slug">URL标识符</param>
    public void SetNameAndSlug(string name, string? slug = null)
    {
        Name = name?.Trim() ?? string.Empty;
        Slug = slug?.Trim() ?? GenerateSlug(Name);
        DisplayName = Name; // Set DisplayName same as Name by default
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置父分类（更新层级信息）
    /// </summary>
    /// <param name="parent">父分类</param>
    public void SetParent(Category? parent)
    {
        if (parent != null && parent.Id == Id)
            throw new InvalidOperationException("不能设置自己为父分类");

        Parent = parent;
        ParentId = parent?.Id;

        if (parent == null)
        {
            Level = 0;
            TreePath = $"/{Id}/";
        }
        else
        {
            Level = parent.Level + 1;
            TreePath = $"{parent.TreePath}{Id}/";
        }

        UpdateAuditFields();
    }

    /// <summary>
    /// 获取所有祖先分类的ID
    /// </summary>
    /// <returns>祖先分类ID列表</returns>
    public IEnumerable<Guid> GetAncestorIds()
    {
        if (string.IsNullOrEmpty(TreePath))
            return Enumerable.Empty<Guid>();

        return TreePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
                      .Where(id => Guid.TryParse(id, out _) && Guid.Parse(id) != Id)
                      .Select(Guid.Parse);
    }

    /// <summary>
    /// 检查是否为根分类
    /// </summary>
    /// <returns>是否为根分类</returns>
    public bool IsRoot()
    {
        return ParentId == null;
    }

    /// <summary>
    /// 检查是否为叶子分类（没有子分类）
    /// </summary>
    /// <returns>是否为叶子分类</returns>
    public bool IsLeaf()
    {
        return !Children.Any();
    }

    /// <summary>
    /// 增加文章数量
    /// </summary>
    /// <param name="count">增加数量</param>
    public void IncreasePostCount(int count = 1)
    {
        PostCount += count;
        UpdateAuditFields();
    }

    /// <summary>
    /// 减少文章数量
    /// </summary>
    /// <param name="count">减少数量</param>
    public void DecreasePostCount(int count = 1)
    {
        PostCount = Math.Max(0, PostCount - count);
        UpdateAuditFields();
    }

    /// <summary>
    /// 更新排序顺序
    /// </summary>
    /// <param name="sortOrder">新的排序顺序</param>
    public void UpdateSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        UpdateAuditFields();
    }

    /// <summary>
    /// 生成URL友好的Slug
    /// </summary>
    /// <param name="name">分类名称</param>
    /// <returns>URL标识符</returns>
    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // 简单的Slug生成逻辑，实际项目中可以使用更复杂的实现
        return name.ToLowerInvariant()
                  .Replace(" ", "-")
                  .Replace("_", "-")
                  .Trim('-');
    }
}