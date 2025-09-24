namespace MapleBlog.Application.DTOs.File;

public class FileUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public Stream FileStream { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    
    /// <summary>
    /// 目标目录
    /// </summary>
    public string? Directory { get; set; }
    
    /// <summary>
    /// 标签列表
    /// </summary>
    public List<string>? Tags { get; set; }
    
    /// <summary>
    /// 访问级别
    /// </summary>
    public string? AccessLevel { get; set; }
}


public class FileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class UpdateFileMetadataDto
{
    public string? Description { get; set; }
    public string? Category { get; set; }
}