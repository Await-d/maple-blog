using Microsoft.AspNetCore.Http;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Service interface for file operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Uploads a file and returns the file path
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="subDirectory">Subdirectory to store the file (e.g., "posts", "avatars")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File path or URL</returns>
        Task<FileUploadResult> UploadFileAsync(
            IFormFile file,
            string subDirectory = "general",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads multiple files
        /// </summary>
        /// <param name="files">Files to upload</param>
        /// <param name="subDirectory">Subdirectory to store the files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of upload results</returns>
        Task<IReadOnlyList<FileUploadResult>> UploadFilesAsync(
            IEnumerable<IFormFile> files,
            string subDirectory = "general",
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filePath">Path or URL of the file to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully deleted</returns>
        Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple files
        /// </summary>
        /// <param name="filePaths">Paths or URLs of the files to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of successfully deleted files</returns>
        Task<int> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        /// <param name="filePath">Path or URL of the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if file exists</returns>
        Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information
        /// </summary>
        /// <param name="filePath">Path or URL of the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File information</returns>
        Task<FileInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies a file to a new location
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully copied</returns>
        Task<bool> CopyFileAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves a file to a new location
        /// </summary>
        /// <param name="sourcePath">Source file path</param>
        /// <param name="destinationPath">Destination file path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successfully moved</returns>
        Task<bool> MoveFileAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a downloadable stream for a file
        /// </summary>
        /// <param name="filePath">Path or URL of the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File stream and content type</returns>
        Task<FileDownloadResult?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates file upload constraints
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="options">Validation options</param>
        /// <returns>Validation result</returns>
        FileValidationResult ValidateFile(IFormFile file, FileValidationOptions? options = null);

        /// <summary>
        /// Cleans up orphaned files (files not referenced in the database)
        /// </summary>
        /// <param name="olderThanDays">Delete files older than specified days</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of cleaned up files</returns>
        Task<int> CleanupOrphanedFilesAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage usage statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage statistics</returns>
        Task<StorageStatistics> GetStorageStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads file with user and folder parameters (for backward compatibility)
        /// </summary>
        /// <param name="file">File to upload</param>
        /// <param name="userId">User ID</param>
        /// <param name="folder">Folder name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, Guid userId, string folder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads an image file
        /// </summary>
        /// <param name="file">Image file to upload</param>
        /// <param name="subDirectory">Subdirectory to store the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result</returns>
        Task<FileUploadResult> UploadImageAsync(IFormFile file, string subDirectory = "images", CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets files for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user files</returns>
        Task<IEnumerable<FileMetadata>> GetUserFilesAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all files in the system
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all files</returns>
        Task<IEnumerable<FileMetadata>> GetAllFilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information by ID
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File information</returns>
        Task<FileMetadata?> GetFileInfoAsync(Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file by ID with user validation
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="fileName">File name for validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteFileAsync(Guid fileId, Guid userId, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage statistics with alternative name
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage statistics</returns>
        Task<StorageStatistics> GetStorageStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file stream by file ID
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File stream result</returns>
        Task<FileDownloadResult?> GetFileStreamAsync(Guid fileId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Image processing service interface
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>
        /// Resizes an image to specified dimensions
        /// </summary>
        /// <param name="sourceStream">Source image stream</param>
        /// <param name="width">Target width</param>
        /// <param name="height">Target height</param>
        /// <param name="maintainAspectRatio">Whether to maintain aspect ratio</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Resized image stream</returns>
        Task<Stream> ResizeImageAsync(
            Stream sourceStream,
            int width,
            int height,
            bool maintainAspectRatio = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates multiple thumbnail sizes for an image
        /// </summary>
        /// <param name="sourceStream">Source image stream</param>
        /// <param name="sizes">Thumbnail sizes to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of size to thumbnail stream</returns>
        Task<IDictionary<ThumbnailSize, Stream>> CreateThumbnailsAsync(
            Stream sourceStream,
            IEnumerable<ThumbnailSize> sizes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes an image for web delivery
        /// </summary>
        /// <param name="sourceStream">Source image stream</param>
        /// <param name="quality">Compression quality (1-100)</param>
        /// <param name="format">Target format</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimized image stream</returns>
        Task<Stream> OptimizeImageAsync(
            Stream sourceStream,
            int quality = 85,
            ImageFormat format = ImageFormat.Jpeg,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a watermark to an image
        /// </summary>
        /// <param name="sourceStream">Source image stream</param>
        /// <param name="watermarkStream">Watermark image stream</param>
        /// <param name="position">Watermark position</param>
        /// <param name="opacity">Watermark opacity (0.0-1.0)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Watermarked image stream</returns>
        Task<Stream> AddWatermarkAsync(
            Stream sourceStream,
            Stream watermarkStream,
            WatermarkPosition position = WatermarkPosition.BottomRight,
            float opacity = 0.5f,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets image metadata
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Image metadata</returns>
        Task<ImageMetadata> GetImageMetadataAsync(Stream imageStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a stream contains a valid image
        /// </summary>
        /// <param name="stream">Stream to validate</param>
        /// <param name="allowedFormats">Allowed image formats</param>
        /// <returns>True if valid image</returns>
        bool IsValidImage(Stream stream, IEnumerable<ImageFormat>? allowedFormats = null);

        /// <summary>
        /// Converts image format
        /// </summary>
        /// <param name="sourceStream">Source image stream</param>
        /// <param name="targetFormat">Target image format</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Converted image stream</returns>
        Task<Stream> ConvertFormatAsync(
            Stream sourceStream,
            ImageFormat targetFormat,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// File upload result
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; init; }
        public string? FilePath { get; init; }
        public string? FileName { get; init; }
        public long FileSize { get; init; }
        public string? ContentType { get; init; }
        public string? Error { get; init; }
        public FileMetadata? Metadata { get; init; }
    }

    /// <summary>
    /// File download result
    /// </summary>
    public class FileDownloadResult
    {
        public Stream Stream { get; init; } = null!;
        public string ContentType { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public long? ContentLength { get; init; }
    }

    /// <summary>
    /// File validation result
    /// </summary>
    public class FileValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = new List<string>();
    }

    /// <summary>
    /// File validation options
    /// </summary>
    public class FileValidationOptions
    {
        public long MaxFileSize { get; init; } = 10 * 1024 * 1024; // 10MB
        public IReadOnlyList<string> AllowedExtensions { get; init; } = new List<string>();
        public IReadOnlyList<string> AllowedContentTypes { get; init; } = new List<string>();
        public bool RequireImageFormat { get; init; } = false;
        public bool ScanForMalware { get; init; } = false;
    }

    /// <summary>
    /// File metadata
    /// </summary>
    public class FileMetadata
    {
        public string OriginalName { get; init; } = string.Empty;
        public string Extension { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public long Size { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? Hash { get; init; }
        public IDictionary<string, object> AdditionalData { get; init; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Storage statistics
    /// </summary>
    public class StorageStatistics
    {
        public long TotalSize { get; init; }
        public int TotalFiles { get; init; }
        public IDictionary<string, long> SizeByDirectory { get; init; } = new Dictionary<string, long>();
        public IDictionary<string, int> FileCountByType { get; init; } = new Dictionary<string, int>();
        public DateTime LastUpdated { get; init; }
    }

    /// <summary>
    /// Thumbnail size specification
    /// </summary>
    public class ThumbnailSize
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool MaintainAspectRatio { get; init; } = true;

        public static ThumbnailSize Small => new() { Width = 150, Height = 150, Name = "small" };
        public static ThumbnailSize Medium => new() { Width = 300, Height = 300, Name = "medium" };
        public static ThumbnailSize Large => new() { Width = 600, Height = 600, Name = "large" };
    }

    /// <summary>
    /// Image metadata
    /// </summary>
    public class ImageMetadata
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public ImageFormat Format { get; init; }
        public string? ColorSpace { get; init; }
        public int BitsPerPixel { get; init; }
        public bool HasTransparency { get; init; }
        public IDictionary<string, object> ExifData { get; init; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Supported image formats
    /// </summary>
    public enum ImageFormat
    {
        Jpeg,
        Png,
        Gif,
        Webp,
        Bmp,
        Tiff
    }

    /// <summary>
    /// Watermark position options
    /// </summary>
    public enum WatermarkPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
}