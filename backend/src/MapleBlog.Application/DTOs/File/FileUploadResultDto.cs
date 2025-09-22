namespace MapleBlog.Application.DTOs.File;

/// <summary>
/// File upload result data transfer object
/// </summary>
public class FileUploadResultDto
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
    /// File unique identifier
    /// </summary>
    public Guid? FileId => FileInfo?.Id;

    /// <summary>
    /// File URL if upload succeeded
    /// </summary>
    public string? FileUrl => FileInfo?.Url;

    /// <summary>
    /// Original filename
    /// </summary>
    public string? FileName => FileInfo?.FileName;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSize => FileInfo?.Size;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTimeOffset? UploadedAt => FileInfo?.UploadedAt;

    /// <summary>
    /// Create a successful upload result
    /// </summary>
    /// <param name="fileInfo">File information</param>
    /// <returns>Success result</returns>
    public static FileUploadResultDto CreateSuccess(FileInfoDto fileInfo)
    {
        return new FileUploadResultDto
        {
            Success = true,
            FileInfo = fileInfo
        };
    }

    /// <summary>
    /// Create a failed upload result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Error result</returns>
    public static FileUploadResultDto Error(string errorMessage)
    {
        return new FileUploadResultDto
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}