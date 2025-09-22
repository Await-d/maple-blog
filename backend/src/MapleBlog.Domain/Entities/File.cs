using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// Represents a file uploaded to the system
    /// </summary>
    public class File : BaseEntity
    {
        /// <summary>
        /// Original filename as uploaded by the user
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// System-generated filename
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File extension (with dot, e.g., ".jpg")
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// MIME content type
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Relative file path in storage
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Directory where file is stored (e.g., "posts", "avatars", "documents")
        /// </summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// SHA256 hash of the file content for deduplication
        /// </summary>
        public string? FileHash { get; set; }

        /// <summary>
        /// User who uploaded the file
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Whether the file is currently in use (referenced by posts, etc.)
        /// </summary>
        public bool IsInUse { get; set; } = false;

        /// <summary>
        /// Number of times this file is referenced
        /// </summary>
        public int ReferenceCount { get; set; } = 0;

        /// <summary>
        /// Tags associated with this file for organization
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Optional description or alt text for the file
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether the file is publicly accessible
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Access level for the file
        /// </summary>
        public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Public;

        /// <summary>
        /// File metadata as JSON string for additional properties
        /// </summary>
        public string? MetadataJson { get; set; }

        /// <summary>
        /// For images: width in pixels
        /// </summary>
        public int? ImageWidth { get; set; }

        /// <summary>
        /// For images: height in pixels
        /// </summary>
        public int? ImageHeight { get; set; }

        /// <summary>
        /// IP address from which the file was uploaded
        /// </summary>
        public string? UploadIpAddress { get; set; }

        /// <summary>
        /// User agent from which the file was uploaded
        /// </summary>
        public string? UploadUserAgent { get; set; }

        /// <summary>
        /// When the file was last accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// Number of times the file has been downloaded/accessed
        /// </summary>
        public int AccessCount { get; set; } = 0;
    }

    /// <summary>
    /// File access levels
    /// </summary>
    public enum FileAccessLevel
    {
        /// <summary>
        /// Public access - anyone can view
        /// </summary>
        Public = 0,

        /// <summary>
        /// Authenticated users only
        /// </summary>
        Authenticated = 1,

        /// <summary>
        /// Owner only
        /// </summary>
        Private = 2,

        /// <summary>
        /// Administrators only
        /// </summary>
        Admin = 3
    }
}