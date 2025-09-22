using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Service for managing file permissions and access control
    /// </summary>
    public interface IFilePermissionService
    {
        /// <summary>
        /// Checks if a user can upload files
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileSize">Size of file to upload</param>
        /// <param name="fileType">Type of file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user can upload, false otherwise</returns>
        Task<bool> CanUploadFileAsync(Guid userId, long fileSize, string fileType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user can access a specific file
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user can access, false otherwise</returns>
        Task<bool> CanAccessFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user can delete a specific file
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user can delete, false otherwise</returns>
        Task<bool> CanDeleteFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user can update file metadata
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user can update, false otherwise</returns>
        Task<bool> CanUpdateFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user can manage files for another user (admin/moderator function)
        /// </summary>
        /// <param name="managerId">Manager user ID</param>
        /// <param name="targetUserId">Target user ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user can manage, false otherwise</returns>
        Task<bool> CanManageUserFilesAsync(Guid managerId, Guid targetUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the maximum file size a user can upload
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum file size in bytes</returns>
        Task<long> GetMaxFileSizeAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the allowed file types for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of allowed file extensions</returns>
        Task<IEnumerable<string>> GetAllowedFileTypesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates file access level for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="accessLevel">File access level</param>
        /// <param name="fileOwner">File owner ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if access is allowed, false otherwise</returns>
        Task<bool> ValidateFileAccessAsync(Guid userId, FileAccessLevel accessLevel, Guid fileOwner, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user's file permissions based on their role
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File permissions for the user</returns>
        Task<FilePermissions> GetUserFilePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs file access for audit purposes
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="action">Action performed</param>
        /// <param name="success">Whether action was successful</param>
        /// <param name="ipAddress">Client IP address</param>
        /// <param name="userAgent">User agent</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task LogFileAccessAsync(Guid userId, Guid fileId, string action, bool success, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// File permissions for a user
    /// </summary>
    public class FilePermissions
    {
        /// <summary>
        /// Can upload files
        /// </summary>
        public bool CanUpload { get; set; }

        /// <summary>
        /// Can delete own files
        /// </summary>
        public bool CanDeleteOwn { get; set; }

        /// <summary>
        /// Can delete other users' files
        /// </summary>
        public bool CanDeleteOthers { get; set; }

        /// <summary>
        /// Can access private files
        /// </summary>
        public bool CanAccessPrivate { get; set; }

        /// <summary>
        /// Can access admin-only files
        /// </summary>
        public bool CanAccessAdmin { get; set; }

        /// <summary>
        /// Can manage files for other users
        /// </summary>
        public bool CanManageOthers { get; set; }

        /// <summary>
        /// Maximum file size in bytes
        /// </summary>
        public long MaxFileSize { get; set; }

        /// <summary>
        /// Storage quota in bytes
        /// </summary>
        public long StorageQuota { get; set; }

        /// <summary>
        /// Allowed file extensions
        /// </summary>
        public IEnumerable<string> AllowedExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Allowed content types
        /// </summary>
        public IEnumerable<string> AllowedContentTypes { get; set; } = new List<string>();
    }
}