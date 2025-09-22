using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// Service for managing file permissions and access control
    /// </summary>
    public class FilePermissionService : IFilePermissionService
    {
        private readonly ILogger<FilePermissionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IUserContextService _userContextService;

        // File size limits by role (in bytes)
        private static readonly Dictionary<UserRoleEnum, long> RoleFileSizeLimits = new()
        {
            { UserRoleEnum.Guest, 0 }, // No upload for guests
            { UserRoleEnum.User, 10 * 1024 * 1024 }, // 10MB
            { UserRoleEnum.Author, 50 * 1024 * 1024 }, // 50MB
            { UserRoleEnum.Moderator, 100 * 1024 * 1024 }, // 100MB
            { UserRoleEnum.Admin, long.MaxValue } // No limit
        };

        // Storage quotas by role (in bytes)
        private static readonly Dictionary<UserRoleEnum, long> RoleStorageQuotas = new()
        {
            { UserRoleEnum.Guest, 0 }, // No storage for guests
            { UserRoleEnum.User, 100 * 1024 * 1024 }, // 100MB
            { UserRoleEnum.Author, 500 * 1024 * 1024 }, // 500MB
            { UserRoleEnum.Moderator, 1024 * 1024 * 1024 }, // 1GB
            { UserRoleEnum.Admin, long.MaxValue } // No limit
        };

        // Allowed file extensions by role
        private static readonly Dictionary<UserRoleEnum, string[]> RoleAllowedExtensions = new()
        {
            { UserRoleEnum.Guest, Array.Empty<string>() },
            { UserRoleEnum.User, new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".docx", ".doc" } },
            { UserRoleEnum.Author, new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf", ".txt", ".docx", ".doc", ".md", ".zip" } },
            { UserRoleEnum.Moderator, new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf", ".txt", ".docx", ".doc", ".md", ".zip", ".mp4", ".mp3" } },
            { UserRoleEnum.Admin, new[] { "*" } } // All extensions
        };

        public FilePermissionService(
            ILogger<FilePermissionService> logger,
            IConfiguration configuration,
            IRepository<User> userRepository,
            IFileRepository fileRepository,
            IUserContextService userContextService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        }

        public async Task<bool> CanUploadFileAsync(Guid userId, long fileSize, string fileType, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for file upload permission check: {UserId}", userId);
                    return false;
                }

                var userRole = user.Role;

                // Check if user can upload at all
                if (userRole == UserRoleEnum.Guest)
                {
                    _logger.LogWarning("Guest user attempted to upload file: {UserId}", userId);
                    return false;
                }

                // Check file size limit
                var maxFileSize = RoleFileSizeLimits[userRole];
                if (fileSize > maxFileSize)
                {
                    _logger.LogWarning("File size {FileSize} exceeds limit {MaxSize} for user {UserId} with role {Role}",
                        fileSize, maxFileSize, userId, userRole);
                    return false;
                }

                // Check file type
                var extension = Path.GetExtension(fileType)?.ToLowerInvariant() ?? "";
                var allowedExtensions = RoleAllowedExtensions[userRole];
                if (!allowedExtensions.Contains("*") && !allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("File type {FileType} not allowed for user {UserId} with role {Role}",
                        fileType, userId, userRole);
                    return false;
                }

                // Check storage quota
                var currentUsage = await _fileRepository.GetUserStorageUsageAsync(userId, cancellationToken);
                var storageQuota = RoleStorageQuotas[userRole];
                if (currentUsage + fileSize > storageQuota)
                {
                    _logger.LogWarning("Storage quota exceeded for user {UserId}. Current: {Current}, Additional: {Additional}, Quota: {Quota}",
                        userId, currentUsage, fileSize, storageQuota);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file upload permission for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> CanAccessFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found for access permission check: {FileId}", fileId);
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for file access permission check: {UserId}", userId);
                    return false;
                }

                return await ValidateFileAccessAsync(userId, file.AccessLevel, file.UserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file access permission for user {UserId}, file {FileId}", userId, fileId);
                return false;
            }
        }

        public async Task<bool> CanDeleteFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found for delete permission check: {FileId}", fileId);
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User not found for file delete permission check: {UserId}", userId);
                    return false;
                }

                var userRole = user.Role;

                // Admin can delete any file
                if (userRole == UserRoleEnum.Admin)
                {
                    return true;
                }

                // Moderator can delete most files except admin files
                if (userRole == UserRoleEnum.Moderator && file.AccessLevel != FileAccessLevel.Admin)
                {
                    return true;
                }

                // Users can only delete their own files
                if (file.UserId == userId)
                {
                    return true;
                }

                _logger.LogWarning("User {UserId} with role {Role} attempted to delete file {FileId} owned by {OwnerId}",
                    userId, userRole, fileId, file.UserId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file delete permission for user {UserId}, file {FileId}", userId, fileId);
                return false;
            }
        }

        public async Task<bool> CanUpdateFileAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return false;
                }

                var userRole = user.Role;

                // Admin can update any file
                if (userRole == UserRoleEnum.Admin)
                {
                    return true;
                }

                // Moderator can update most files except admin files
                if (userRole == UserRoleEnum.Moderator && file.AccessLevel != FileAccessLevel.Admin)
                {
                    return true;
                }

                // Users can only update their own files
                return file.UserId == userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file update permission for user {UserId}, file {FileId}", userId, fileId);
                return false;
            }
        }

        public async Task<bool> CanManageUserFilesAsync(Guid managerId, Guid targetUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var manager = await _userRepository.GetByIdAsync(managerId, cancellationToken);
                if (manager == null)
                {
                    return false;
                }

                var managerRoleString = manager.Role.ToString();

                // Admin can manage any user's files
                if (managerRoleString == SystemRole.Admin)
                {
                    return true;
                }

                // Moderator can manage files for non-admin users
                if (managerRoleString == SystemRole.Moderator)
                {
                    var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
                    if (targetUser != null)
                    {
                        var targetRole = targetUser.Role;
                        return targetRole != UserRoleEnum.Admin;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file management permission for manager {ManagerId}, target {TargetUserId}", managerId, targetUserId);
                return false;
            }
        }

        public async Task<long> GetMaxFileSizeAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return 0;
                }

                var userRole = user.Role;
                return RoleFileSizeLimits.GetValueOrDefault(userRole, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max file size for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<IEnumerable<string>> GetAllowedFileTypesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return Array.Empty<string>();
                }

                var userRole = user.Role;
                return RoleAllowedExtensions.GetValueOrDefault(userRole, Array.Empty<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting allowed file types for user {UserId}", userId);
                return Array.Empty<string>();
            }
        }

        public async Task<bool> ValidateFileAccessAsync(Guid userId, FileAccessLevel accessLevel, Guid fileOwner, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return false;
                }

                var userRole = user.Role;

                return accessLevel switch
                {
                    FileAccessLevel.Public => true,
                    FileAccessLevel.Authenticated => userRole != UserRoleEnum.Guest,
                    FileAccessLevel.Private => userId == fileOwner || userRole == UserRoleEnum.Admin || userRole == UserRoleEnum.Moderator,
                    FileAccessLevel.Admin => userRole == UserRoleEnum.Admin,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file access for user {UserId}, access level {AccessLevel}", userId, accessLevel);
                return false;
            }
        }

        public async Task<FilePermissions> GetUserFilePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return new FilePermissions();
                }

                var userRole = user.Role;

                return new FilePermissions
                {
                    CanUpload = userRole != UserRoleEnum.Guest,
                    CanDeleteOwn = userRole != UserRoleEnum.Guest,
                    CanDeleteOthers = userRole == UserRoleEnum.Admin || userRole == UserRoleEnum.Moderator,
                    CanAccessPrivate = userRole == UserRoleEnum.Admin || userRole == UserRoleEnum.Moderator,
                    CanAccessAdmin = userRole == UserRoleEnum.Admin,
                    CanManageOthers = userRole == UserRoleEnum.Admin || userRole == UserRoleEnum.Moderator,
                    MaxFileSize = RoleFileSizeLimits.GetValueOrDefault(userRole, 0),
                    StorageQuota = RoleStorageQuotas.GetValueOrDefault(userRole, 0),
                    AllowedExtensions = RoleAllowedExtensions.GetValueOrDefault(userRole, Array.Empty<string>()),
                    AllowedContentTypes = GetAllowedContentTypes(userRole)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file permissions for user {UserId}", userId);
                return new FilePermissions();
            }
        }

        public async Task LogFileAccessAsync(Guid userId, Guid fileId, string action, bool success, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // For now, we'll just log to the application logger
                // In a production system, you might want to store this in a dedicated audit table
                _logger.LogInformation("File access: User {UserId} performed {Action} on file {FileId}. Success: {Success}. IP: {IP}, UserAgent: {UserAgent}",
                    userId, action, fileId, success, ipAddress ?? "Unknown", userAgent ?? "Unknown");

                // TODO: Implement proper audit logging to database if required
                // This could involve creating a FileAuditLog entity and repository
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging file access for user {UserId}, file {FileId}, action {Action}", userId, fileId, action);
            }
        }

        private static IEnumerable<string> GetAllowedContentTypes(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.Guest => Array.Empty<string>(),
                UserRoleEnum.User => new[]
                {
                    "image/jpeg", "image/png", "image/gif",
                    "application/pdf", "text/plain",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/msword"
                },
                UserRoleEnum.Author => new[]
                {
                    "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
                    "application/pdf", "text/plain", "text/markdown",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/msword", "application/zip"
                },
                UserRoleEnum.Moderator => new[]
                {
                    "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
                    "application/pdf", "text/plain", "text/markdown",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/msword", "application/zip",
                    "video/mp4", "audio/mpeg"
                },
                UserRoleEnum.Admin => new[] { "*/*" },
                _ => Array.Empty<string>()
            };
        }
    }
}