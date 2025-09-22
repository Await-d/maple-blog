using System;
using System.Collections.Generic;

namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// Represents a file sharing record in the system
    /// </summary>
    public class FileShare : BaseEntity
    {
        /// <summary>
        /// Unique identifier for the share
        /// </summary>
        public string ShareId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the file being shared
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// ID of the user who created the share
        /// </summary>
        public Guid SharedById { get; set; }

        /// <summary>
        /// ID of the user the file is shared with (null for public shares)
        /// </summary>
        public Guid? SharedWithId { get; set; }

        /// <summary>
        /// Email of the user the file is shared with (for external shares)
        /// </summary>
        public string? SharedWithEmail { get; set; }

        /// <summary>
        /// Permission level granted for this share
        /// </summary>
        public FilePermission Permission { get; set; }

        /// <summary>
        /// Optional expiration date for the share
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Number of times the share has been accessed
        /// </summary>
        public int AccessCount { get; set; } = 0;

        /// <summary>
        /// Last time the share was accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        /// <summary>
        /// Whether the share is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether the share requires authentication to access
        /// </summary>
        public bool RequiresAuthentication { get; set; } = true;

        /// <summary>
        /// Optional password for accessing the share (hashed)
        /// </summary>
        public string? PasswordHash { get; set; }

        /// <summary>
        /// Optional message included with the share
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Maximum number of times the share can be accessed
        /// </summary>
        public int? MaxAccessCount { get; set; }

        /// <summary>
        /// Whether email notification was sent for this share
        /// </summary>
        public bool NotificationSent { get; set; } = false;

        /// <summary>
        /// Date when the share was revoked (if applicable)
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// ID of the user who revoked the share (if applicable)
        /// </summary>
        public Guid? RevokedById { get; set; }

        /// <summary>
        /// Reason for revoking the share
        /// </summary>
        public string? RevocationReason { get; set; }

        // Navigation properties
        public virtual File? File { get; set; }
        public virtual User? SharedBy { get; set; }
        public virtual User? SharedWith { get; set; }
        public virtual User? RevokedBy { get; set; }
        public virtual ICollection<FileAccessLog> AccessLogs { get; set; } = new List<FileAccessLog>();
    }

    /// <summary>
    /// File permission levels for sharing
    /// </summary>
    public enum FilePermission
    {
        Read = 1,
        Write = 2,
        Delete = 4,
        Share = 8,
        Admin = 15 // All permissions
    }

    /// <summary>
    /// Represents a file access log entry
    /// </summary>
    public class FileAccessLog : BaseEntity
    {
        /// <summary>
        /// ID of the file that was accessed
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// ID of the file share (if accessed through a share)
        /// </summary>
        public Guid? FileShareId { get; set; }

        /// <summary>
        /// ID of the user who accessed the file
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Type of access operation
        /// </summary>
        public FileAccessType AccessType { get; set; }

        /// <summary>
        /// IP address from which the file was accessed
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string of the client
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether the access was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if access failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional metadata about the access (JSON)
        /// </summary>
        public string? Metadata { get; set; }

        // Navigation properties
        public virtual File? File { get; set; }
        public virtual FileShare? FileShare { get; set; }
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Types of file access operations
    /// </summary>
    public enum FileAccessType
    {
        View = 1,
        Download = 2,
        Edit = 3,
        Delete = 4,
        Share = 5,
        PermissionChange = 6
    }
}