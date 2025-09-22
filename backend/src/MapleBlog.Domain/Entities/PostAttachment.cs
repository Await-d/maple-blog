using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 文章附件实体
/// </summary>
public class PostAttachment : BaseEntity
{
    /// <summary>
    /// 文章ID
    /// </summary>
    [Required]
    public Guid PostId { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 内容类型
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件路径
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件URL
    /// </summary>
    [StringLength(500)]
    public string? FileUrl { get; set; }

    /// <summary>
    /// 图片宽度
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// 图片高度
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// 替代文本
    /// </summary>
    [StringLength(255)]
    public string? Alt { get; set; }

    /// <summary>
    /// 图片说明
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    // 导航属性

    /// <summary>
    /// 文章
    /// </summary>
    public virtual Post? Post { get; set; }

    // 业务方法

    /// <summary>
    /// 检查是否为图片文件
    /// </summary>
    /// <returns>是否为图片</returns>
    public bool IsImage()
    {
        return ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取文件大小的友好显示
    /// </summary>
    /// <returns>文件大小字符串</returns>
    public string GetFileSizeDisplay()
    {
        const int byteConversion = 1024;
        double bytes = FileSize;

        if (bytes >= Math.Pow(byteConversion, 3))
        {
            return $"{bytes / Math.Pow(byteConversion, 3):F2} GB";
        }
        if (bytes >= Math.Pow(byteConversion, 2))
        {
            return $"{bytes / Math.Pow(byteConversion, 2):F2} MB";
        }
        if (bytes >= byteConversion)
        {
            return $"{bytes / byteConversion:F2} KB";
        }

        return $"{bytes} B";
    }

    /// <summary>
    /// 设置图片尺寸
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public void SetImageDimensions(int? width, int? height)
    {
        Width = width;
        Height = height;
        UpdateAuditFields();
    }
}