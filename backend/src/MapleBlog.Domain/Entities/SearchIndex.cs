using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 搜索索引实体
/// </summary>
public class SearchIndex
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 实体类型
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    [StringLength(500)]
    public string? Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 搜索向量（PostgreSQL全文搜索）
    /// </summary>
    public string? SearchVector { get; set; }

    /// <summary>
    /// 关键词（逗号分隔）
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// 语言
    /// </summary>
    [StringLength(10)]
    public string Language { get; set; } = "zh-CN";

    // 权重配置

    /// <summary>
    /// 标题权重
    /// </summary>
    public decimal TitleWeight { get; set; } = 1.0m;

    /// <summary>
    /// 内容权重
    /// </summary>
    public decimal ContentWeight { get; set; } = 0.5m;

    /// <summary>
    /// 关键词权重
    /// </summary>
    public decimal KeywordWeight { get; set; } = 0.8m;

    // 状态信息

    /// <summary>
    /// 索引时间
    /// </summary>
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    // 业务方法

    /// <summary>
    /// 更新索引
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="keywords">关键词</param>
    public void UpdateIndex(string? title, string? content, string? keywords = null)
    {
        Title = title;
        Content = content;
        Keywords = keywords;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置权重
    /// </summary>
    /// <param name="titleWeight">标题权重</param>
    /// <param name="contentWeight">内容权重</param>
    /// <param name="keywordWeight">关键词权重</param>
    public void SetWeights(decimal titleWeight, decimal contentWeight, decimal keywordWeight)
    {
        TitleWeight = titleWeight;
        ContentWeight = contentWeight;
        KeywordWeight = keywordWeight;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 停用索引
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 激活索引
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更新内容（UpdateIndex的别名，为了兼容性）
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="keywords">关键词</param>
    /// <param name="language">语言</param>
    public void UpdateContent(string? title, string? content, string? keywords = null, string? language = null)
    {
        UpdateIndex(title, content, keywords);
        if (!string.IsNullOrEmpty(language))
        {
            Language = language;
        }
    }

    /// <summary>
    /// 标记为激活（Activate的别名，为了兼容性）
    /// </summary>
    public void MarkAsActive()
    {
        Activate();
    }

    /// <summary>
    /// 创建搜索索引
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="title">标题</param>
    /// <param name="content">内容</param>
    /// <param name="keywords">关键词</param>
    /// <param name="language">语言</param>
    /// <returns>搜索索引</returns>
    public static SearchIndex Create(string entityType, Guid entityId, string? title, string? content, string? keywords = null, string language = "zh-CN")
    {
        return new SearchIndex
        {
            EntityType = entityType,
            EntityId = entityId,
            Title = title,
            Content = content,
            Keywords = keywords,
            Language = language
        };
    }
}