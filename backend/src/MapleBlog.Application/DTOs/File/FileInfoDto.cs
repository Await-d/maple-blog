using System.IO;

namespace MapleBlog.Application.DTOs.File;

/// <summary>
/// File information data transfer object
/// </summary>
public class FileInfoDto
{
    /// <summary>
    /// File unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Stored filename (may be different from original)
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// File content type (MIME type)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Relative path to the file
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Full URL to access the file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// ID of user who uploaded the file
    /// </summary>
    public Guid UploadedBy { get; set; }

    /// <summary>
    /// Name of user who uploaded the file
    /// </summary>
    public string UploadedByName { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Optional folder/category the file is stored in
    /// </summary>
    public string? Folder { get; set; }

    /// <summary>
    /// Optional description of the file
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Alternative text for images (accessibility)
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Whether the file is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// File extension
    /// </summary>
    public string Extension => System.IO.Path.GetExtension(FileName).ToLowerInvariant();

    /// <summary>
    /// Whether the file is an image
    /// </summary>
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Human-readable file size
    /// </summary>
    public string FormattedSize
    {
        get
        {
            string[] sizes = ["B", "KB", "MB", "GB"];
            double len = Size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}