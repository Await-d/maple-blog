using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Service for advanced file access control and ownership management
    /// </summary>
    public interface IFileAccessControlService
    {
        /// <summary>
        /// Changes file ownership
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="newOwnerId">New owner user ID</param>
        /// <param name="currentUserId">Current user performing the action</param>
        /// <param name="reason">Reason for ownership change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if ownership changed successfully</returns>
        Task<bool> ChangeFileOwnershipAsync(Guid fileId, Guid newOwnerId, Guid currentUserId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shares a file with another user
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="targetUserId">User to share with</param>
        /// <param name="permission">Permission level to grant</param>
        /// <param name="expiresAt">Optional expiration date</param>
        /// <param name="shareUserId">User performing the share</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Share ID if successful</returns>
        Task<Guid?> ShareFileAsync(Guid fileId, Guid targetUserId, FileSharePermission permission, DateTime? expiresAt, Guid shareUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes file sharing
        /// </summary>
        /// <param name="shareId">Share ID</param>
        /// <param name="currentUserId">User revoking the share</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if revoked successfully</returns>
        Task<bool> RevokeFileShareAsync(Guid shareId, Guid currentUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets files shared with a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of shared files</returns>
        Task<IEnumerable<SharedFileDto>> GetSharedFilesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets files shared by a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of files shared by user</returns>
        Task<IEnumerable<SharedFileDto>> GetFilesSharedByUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a temporary access link for a file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="expiresAt">Expiration time</param>
        /// <param name="maxDownloads">Maximum number of downloads allowed</param>
        /// <param name="createdBy">User creating the link</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Temporary access token</returns>
        Task<string?> CreateTemporaryAccessLinkAsync(Guid fileId, DateTime expiresAt, int maxDownloads, Guid createdBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a temporary access link
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File ID if valid, null otherwise</returns>
        Task<Guid?> ValidateTemporaryAccessLinkAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records file download through temporary link
        /// </summary>
        /// <param name="token">Access token</param>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="userAgent">User agent</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RecordTemporaryAccessAsync(string token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file access history
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="days">Number of days to look back</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Access history</returns>
        Task<IEnumerable<FileAccessLogDto>> GetFileAccessHistoryAsync(Guid fileId, int days = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets file access level
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="accessLevel">New access level</param>
        /// <param name="currentUserId">User making the change</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> SetFileAccessLevelAsync(Guid fileId, FileAccessLevel accessLevel, Guid currentUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if user can perform specific action on file
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="action">Action to perform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if action is allowed</returns>
        Task<bool> CanPerformActionAsync(Guid userId, Guid fileId, FileAction action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user's effective permissions for a file
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Effective file permissions</returns>
        Task<FileEffectivePermissions> GetEffectivePermissionsAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// File sharing permission levels
    /// </summary>
    public enum FileSharePermission
    {
        /// <summary>
        /// Read-only access
        /// </summary>
        Read = 1,

        /// <summary>
        /// Read and download access
        /// </summary>
        Download = 2,

        /// <summary>
        /// Read, download, and update metadata
        /// </summary>
        Edit = 3,

        /// <summary>
        /// Full control including delete
        /// </summary>
        FullControl = 4
    }

    /// <summary>
    /// File actions for permission checking
    /// </summary>
    public enum FileAction
    {
        Read,
        Download,
        Update,
        Delete,
        Share,
        ChangeOwnership,
        ChangeAccessLevel,
        ManagePermissions
    }

    /// <summary>
    /// Shared file information
    /// </summary>
    public class SharedFileDto
    {
        /// <summary>
        /// Share ID
        /// </summary>
        public Guid ShareId { get; set; }

        /// <summary>
        /// File ID
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File owner
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Owner name
        /// </summary>
        public string OwnerName { get; set; } = string.Empty;

        /// <summary>
        /// Shared by user ID
        /// </summary>
        public Guid SharedById { get; set; }

        /// <summary>
        /// Shared by user name
        /// </summary>
        public string SharedByName { get; set; } = string.Empty;

        /// <summary>
        /// Shared with user
        /// </summary>
        public Guid? SharedWithId { get; set; }

        /// <summary>
        /// Shared with user name
        /// </summary>
        public string SharedWithName { get; set; } = string.Empty;

        /// <summary>
        /// Permission level
        /// </summary>
        public string Permission { get; set; } = string.Empty;

        /// <summary>
        /// Share date
        /// </summary>
        public DateTime SharedAt { get; set; }

        /// <summary>
        /// Expiration date
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether share is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Number of times the file has been accessed
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Last time the file was accessed
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }
    }

    /// <summary>
    /// File access log entry
    /// </summary>
    public class FileAccessLogDto
    {
        /// <summary>
        /// Access date and time
        /// </summary>
        public DateTime AccessedAt { get; set; }

        /// <summary>
        /// User ID (null for anonymous access)
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// User name
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Action performed
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// IP address
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Whether action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Access method (direct, shared, temporary link)
        /// </summary>
        public string AccessMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Effective file permissions for a user
    /// </summary>
    public class FileEffectivePermissions
    {
        /// <summary>
        /// Can read file metadata
        /// </summary>
        public bool CanRead { get; set; }

        /// <summary>
        /// Can download file
        /// </summary>
        public bool CanDownload { get; set; }

        /// <summary>
        /// Can update file metadata
        /// </summary>
        public bool CanUpdate { get; set; }

        /// <summary>
        /// Can delete file
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Can share file
        /// </summary>
        public bool CanShare { get; set; }

        /// <summary>
        /// Can change file ownership
        /// </summary>
        public bool CanChangeOwnership { get; set; }

        /// <summary>
        /// Can change access level
        /// </summary>
        public bool CanChangeAccessLevel { get; set; }

        /// <summary>
        /// Source of permissions (owner, role, shared, admin)
        /// </summary>
        public string PermissionSource { get; set; } = string.Empty;

        /// <summary>
        /// Whether user is the file owner
        /// </summary>
        public bool IsOwner { get; set; }
    }
}