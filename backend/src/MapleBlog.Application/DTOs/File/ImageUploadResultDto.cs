namespace MapleBlog.Application.DTOs.File;

/// <summary>
/// Image upload result data transfer object with additional image-specific properties
/// </summary>
public class ImageUploadResultDto
{
    /// <summary>
    /// Whether the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// File information if upload succeeded
    /// </summary>
    public FileInfoDto? FileInfo { get; set; }

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Thumbnail URL (if generated)
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Medium size image URL (if generated)
    /// </summary>
    public string? MediumUrl { get; set; }

    /// <summary>
    /// Large size image URL (if generated)
    /// </summary>
    public string? LargeUrl { get; set; }

    /// <summary>
    /// Image format (JPEG, PNG, WebP, etc.)
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Whether image was optimized during upload
    /// </summary>
    public bool WasOptimized { get; set; }

    /// <summary>
    /// Original file size before optimization
    /// </summary>
    public long? OriginalSize { get; set; }

    /// <summary>
    /// Optimized file size (if optimization occurred)
    /// </summary>
    public long? OptimizedSize => FileInfo?.Size;

    /// <summary>
    /// Compression ratio (if optimization occurred)
    /// </summary>
    public double? CompressionRatio
    {
        get
        {
            if (OriginalSize.HasValue && OptimizedSize.HasValue && OriginalSize > 0)
            {
                return (double)OptimizedSize.Value / OriginalSize.Value;
            }
            return null;
        }
    }

    /// <summary>
    /// File unique identifier
    /// </summary>
    public Guid? FileId => FileInfo?.Id;

    /// <summary>
    /// Original image URL
    /// </summary>
    public string? ImageUrl => FileInfo?.Url;

    /// <summary>
    /// Original filename
    /// </summary>
    public string? FileName => FileInfo?.FileName;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTimeOffset? UploadedAt => FileInfo?.UploadedAt;

    /// <summary>
    /// Associated post ID (if uploaded for a specific post)
    /// </summary>
    public Guid? PostId { get; set; }

    /// <summary>
    /// Create a successful image upload result
    /// </summary>
    /// <param name="fileInfo">File information</param>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <param name="format">Image format</param>
    /// <returns>Success result</returns>
    public static ImageUploadResultDto CreateSuccess(FileInfoDto fileInfo, int? width = null, int? height = null, string? format = null)
    {
        return new ImageUploadResultDto
        {
            Success = true,
            FileInfo = fileInfo,
            Width = width,
            Height = height,
            Format = format
        };
    }

    /// <summary>
    /// Create a failed image upload result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Error result</returns>
    public static ImageUploadResultDto Error(string errorMessage)
    {
        return new ImageUploadResultDto
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}