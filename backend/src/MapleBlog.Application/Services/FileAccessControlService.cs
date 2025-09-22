using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// Service for advanced file access control and ownership management
    /// </summary>
    public class FileAccessControlService : IFileAccessControlService
    {
        private readonly ILogger<FileAccessControlService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRepository<User> _userRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFilePermissionService _filePermissionService;
        private readonly IUserContextService _userContextService;
        private readonly IFileShareRepository _fileShareRepository;
        private readonly IFileAccessLogRepository _fileAccessLogRepository;

        // In-memory storage for temporary access links (in production, use Redis or database)
        private static readonly Dictionary<string, TemporaryAccessLink> _temporaryLinks = new();

        public FileAccessControlService(
            ILogger<FileAccessControlService> logger,
            IConfiguration configuration,
            IRepository<User> userRepository,
            IFileRepository fileRepository,
            IFilePermissionService filePermissionService,
            IUserContextService userContextService,
            IFileShareRepository fileShareRepository,
            IFileAccessLogRepository fileAccessLogRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _filePermissionService = filePermissionService ?? throw new ArgumentNullException(nameof(filePermissionService));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
            _fileShareRepository = fileShareRepository ?? throw new ArgumentNullException(nameof(fileShareRepository));
            _fileAccessLogRepository = fileAccessLogRepository ?? throw new ArgumentNullException(nameof(fileAccessLogRepository));
        }

        public async Task<bool> ChangeFileOwnershipAsync(Guid fileId, Guid newOwnerId, Guid currentUserId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found for ownership change: {FileId}", fileId);
                    return false;
                }

                var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
                if (currentUser == null)
                {
                    _logger.LogWarning("Current user not found for ownership change: {UserId}", currentUserId);
                    return false;
                }

                var newOwner = await _userRepository.GetByIdAsync(newOwnerId, cancellationToken);
                if (newOwner == null)
                {
                    _logger.LogWarning("New owner not found: {UserId}", newOwnerId);
                    return false;
                }

                // Check if current user can change ownership
                var canChangeOwnership = await CanPerformActionAsync(currentUserId, fileId, FileAction.ChangeOwnership, cancellationToken);
                if (!canChangeOwnership)
                {
                    _logger.LogWarning("User {UserId} not authorized to change ownership of file {FileId}", currentUserId, fileId);
                    return false;
                }

                var oldOwnerId = file.UserId;
                file.UserId = newOwnerId;
                file.UpdatedAt = DateTime.UtcNow;
                file.UpdatedBy = currentUserId;

                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("File ownership changed: File {FileId} from user {OldOwner} to user {NewOwner} by {CurrentUser}. Reason: {Reason}",
                    fileId, oldOwnerId, newOwnerId, currentUserId, reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing file ownership for file {FileId}", fileId);
                return false;
            }
        }

        public async Task<Guid?> ShareFileAsync(Guid fileId, Guid targetUserId, FileSharePermission permission, DateTime? expiresAt, Guid shareUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found for sharing: {FileId}", fileId);
                    return null;
                }

                var canShare = await CanPerformActionAsync(shareUserId, fileId, FileAction.Share, cancellationToken);
                if (!canShare)
                {
                    _logger.LogWarning("User {UserId} not authorized to share file {FileId}", shareUserId, fileId);
                    return null;
                }

                var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
                if (targetUser == null)
                {
                    _logger.LogWarning("Target user not found for file sharing: {UserId}", targetUserId);
                    return null;
                }

                // Create FileShare entity
                var shareId = GenerateShareId();
                var fileShare = new Domain.Entities.FileShare
                {
                    Id = Guid.NewGuid(),
                    ShareId = shareId,
                    FileId = fileId,
                    SharedById = shareUserId,
                    SharedWithId = targetUserId,
                    Permission = (FilePermission)permission,
                    IsActive = true,
                    RequiresAuthentication = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _fileShareRepository.AddAsync(fileShare, cancellationToken);

                _logger.LogInformation("File shared: File {FileId} shared with user {TargetUserId} by {ShareUserId} with permission {Permission}. ShareId: {ShareId}",
                    fileId, targetUserId, shareUserId, permission, shareId);

                return fileShare.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing file {FileId} with user {TargetUserId}", fileId, targetUserId);
                return null;
            }
        }

        public async Task<bool> RevokeFileShareAsync(Guid shareId, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Look up FileShare entity, verify permissions, and revoke
                var fileShare = await _fileShareRepository.GetByIdAsync(shareId, cancellationToken);
                if (fileShare == null)
                {
                    _logger.LogWarning("File share not found for revocation: {ShareId}", shareId);
                    return false;
                }

                // Check if current user has permission to revoke the share
                if (fileShare.SharedById != currentUserId)
                {
                    var canManage = await CanPerformActionAsync(currentUserId, fileShare.FileId, FileAction.ManagePermissions, cancellationToken);
                    if (!canManage)
                    {
                        _logger.LogWarning("User {UserId} not authorized to revoke share {ShareId}", currentUserId, shareId);
                        return false;
                    }
                }

                await _fileShareRepository.RevokeShareAsync(shareId, currentUserId, "User revoked share", cancellationToken);
                _logger.LogInformation("File share revoked: ShareId {ShareId} by user {UserId}", shareId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking file share {ShareId}", shareId);
                return false;
            }
        }

        public async Task<IEnumerable<SharedFileDto>> GetSharedFilesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting files shared with user {UserId}", userId);

                // Query FileShare entities where SharedWithId = userId
                var shares = await _fileShareRepository.GetSharedWithUserAsync(userId, cancellationToken);

                var sharedFiles = new List<SharedFileDto>();
                foreach (var share in shares)
                {
                    if (share.File != null)
                    {
                        sharedFiles.Add(new SharedFileDto
                        {
                            ShareId = share.Id,
                            FileId = share.FileId,
                            FileName = share.File.OriginalFileName,
                            FileSize = share.File.FileSize,
                            SharedById = share.SharedById,
                            SharedByName = share.SharedBy?.UserName ?? "Unknown",
                            Permission = share.Permission.ToString(),
                            SharedAt = share.CreatedAt,
                            ExpiresAt = share.ExpiresAt
                        });
                    }
                }

                return sharedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared files for user {UserId}", userId);
                return new List<SharedFileDto>();
            }
        }

        public async Task<IEnumerable<SharedFileDto>> GetFilesSharedByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting files shared by user {UserId}", userId);

                // Query FileShare entities where SharedById = userId
                var shares = await _fileShareRepository.GetSharedByUserAsync(userId, cancellationToken);

                var sharedFiles = new List<SharedFileDto>();
                foreach (var share in shares)
                {
                    if (share.File != null)
                    {
                        sharedFiles.Add(new SharedFileDto
                        {
                            ShareId = share.Id,
                            FileId = share.FileId,
                            FileName = share.File.OriginalFileName,
                            FileSize = share.File.FileSize,
                            SharedWithId = share.SharedWithId,
                            SharedWithName = share.SharedWith?.UserName ?? share.SharedWithEmail ?? "Anonymous",
                            Permission = share.Permission.ToString(),
                            SharedAt = share.CreatedAt,
                            ExpiresAt = share.ExpiresAt,
                            AccessCount = share.AccessCount,
                            LastAccessedAt = share.LastAccessedAt
                        });
                    }
                }

                return sharedFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files shared by user {UserId}", userId);
                return new List<SharedFileDto>();
            }
        }

        public async Task<string?> CreateTemporaryAccessLinkAsync(Guid fileId, DateTime expiresAt, int maxDownloads, Guid createdBy, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found for temporary link creation: {FileId}", fileId);
                    return null;
                }

                var canShare = await CanPerformActionAsync(createdBy, fileId, FileAction.Share, cancellationToken);
                if (!canShare)
                {
                    _logger.LogWarning("User {UserId} not authorized to create temporary link for file {FileId}", createdBy, fileId);
                    return null;
                }

                // Generate secure token
                var token = GenerateSecureToken();

                var tempLink = new TemporaryAccessLink
                {
                    Token = token,
                    FileId = fileId,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    MaxDownloads = maxDownloads,
                    DownloadCount = 0,
                    IsActive = true
                };

                _temporaryLinks[token] = tempLink;

                _logger.LogInformation("Temporary access link created for file {FileId} by user {UserId}. Token: {Token}, Expires: {ExpiresAt}",
                    fileId, createdBy, token, expiresAt);

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating temporary access link for file {FileId}", fileId);
                return null;
            }
        }

        public async Task<Guid?> ValidateTemporaryAccessLinkAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_temporaryLinks.TryGetValue(token, out var tempLink))
                {
                    _logger.LogWarning("Temporary access token not found: {Token}", token);
                    return null;
                }

                if (!tempLink.IsActive)
                {
                    _logger.LogWarning("Temporary access token is inactive: {Token}", token);
                    return null;
                }

                if (tempLink.ExpiresAt <= DateTime.UtcNow)
                {
                    tempLink.IsActive = false;
                    _logger.LogWarning("Temporary access token has expired: {Token}", token);
                    return null;
                }

                if (tempLink.DownloadCount >= tempLink.MaxDownloads)
                {
                    tempLink.IsActive = false;
                    _logger.LogWarning("Temporary access token download limit reached: {Token}", token);
                    return null;
                }

                return tempLink.FileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating temporary access token: {Token}", token);
                return null;
            }
        }

        public async Task RecordTemporaryAccessAsync(string token, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_temporaryLinks.TryGetValue(token, out var tempLink))
                {
                    tempLink.DownloadCount++;
                    tempLink.LastAccessedAt = DateTime.UtcNow;

                    _logger.LogInformation("Temporary access recorded: Token {Token}, File {FileId}, Downloads: {DownloadCount}/{MaxDownloads}, IP: {IP}",
                        token, tempLink.FileId, tempLink.DownloadCount, tempLink.MaxDownloads, ipAddress ?? "Unknown");

                    if (tempLink.DownloadCount >= tempLink.MaxDownloads)
                    {
                        tempLink.IsActive = false;
                        _logger.LogInformation("Temporary access token deactivated due to download limit: {Token}", token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording temporary access for token: {Token}", token);
            }
        }

        public async Task<IEnumerable<FileAccessLogDto>> GetFileAccessHistoryAsync(Guid fileId, int days = 30, CancellationToken cancellationToken = default)
        {
            try
            {
                // For now, return simulated access history
                // In a production system, you would query FileAccessLog entities
                var history = new List<FileAccessLogDto>();

                _logger.LogDebug("Getting access history for file {FileId} for last {Days} days", fileId, days);

                // TODO: Query FileAccessLog entities for the file
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access history for file {FileId}", fileId);
                return new List<FileAccessLogDto>();
            }
        }

        public async Task<bool> SetFileAccessLevelAsync(Guid fileId, FileAccessLevel accessLevel, Guid currentUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found for access level change: {FileId}", fileId);
                    return false;
                }

                var canChangeAccessLevel = await CanPerformActionAsync(currentUserId, fileId, FileAction.ChangeAccessLevel, cancellationToken);
                if (!canChangeAccessLevel)
                {
                    _logger.LogWarning("User {UserId} not authorized to change access level of file {FileId}", currentUserId, fileId);
                    return false;
                }

                var oldAccessLevel = file.AccessLevel;
                file.AccessLevel = accessLevel;
                file.UpdatedAt = DateTime.UtcNow;
                file.UpdatedBy = currentUserId;

                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("File access level changed: File {FileId} from {OldLevel} to {NewLevel} by user {UserId}",
                    fileId, oldAccessLevel, accessLevel, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting access level for file {FileId}", fileId);
                return false;
            }
        }

        public async Task<bool> CanPerformActionAsync(Guid userId, Guid fileId, FileAction action, CancellationToken cancellationToken = default)
        {
            try
            {
                var effectivePermissions = await GetEffectivePermissionsAsync(userId, fileId, cancellationToken);

                return action switch
                {
                    FileAction.Read => effectivePermissions.CanRead,
                    FileAction.Download => effectivePermissions.CanDownload,
                    FileAction.Update => effectivePermissions.CanUpdate,
                    FileAction.Delete => effectivePermissions.CanDelete,
                    FileAction.Share => effectivePermissions.CanShare,
                    FileAction.ChangeOwnership => effectivePermissions.CanChangeOwnership,
                    FileAction.ChangeAccessLevel => effectivePermissions.CanChangeAccessLevel,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} can perform action {Action} on file {FileId}", userId, action, fileId);
                return false;
            }
        }

        public async Task<FileEffectivePermissions> GetEffectivePermissionsAsync(Guid userId, Guid fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    return new FileEffectivePermissions();
                }

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    return new FileEffectivePermissions();
                }

                var userRoleString = user.Role.ToString();
                var isOwner = file.UserId == userId;

                // Check basic file access first
                var canAccess = await _filePermissionService.ValidateFileAccessAsync(userId, file.AccessLevel, file.UserId, cancellationToken);
                if (!canAccess)
                {
                    return new FileEffectivePermissions();
                }

                var permissions = new FileEffectivePermissions
                {
                    IsOwner = isOwner,
                    CanRead = true, // If we got here, user can read
                    CanDownload = true // Basic download permission
                };

                // Determine permissions based on role and ownership
                if (userRoleString == SystemRole.Admin)
                {
                    permissions.CanUpdate = true;
                    permissions.CanDelete = true;
                    permissions.CanShare = true;
                    permissions.CanChangeOwnership = true;
                    permissions.CanChangeAccessLevel = true;
                    permissions.PermissionSource = "admin";
                }
                else if (userRoleString == SystemRole.Moderator)
                {
                    permissions.CanUpdate = file.AccessLevel != FileAccessLevel.Admin;
                    permissions.CanDelete = file.AccessLevel != FileAccessLevel.Admin;
                    permissions.CanShare = true;
                    permissions.CanChangeOwnership = file.AccessLevel != FileAccessLevel.Admin;
                    permissions.CanChangeAccessLevel = file.AccessLevel != FileAccessLevel.Admin;
                    permissions.PermissionSource = "moderator";
                }
                else if (isOwner)
                {
                    permissions.CanUpdate = true;
                    permissions.CanDelete = true;
                    permissions.CanShare = true;
                    permissions.CanChangeOwnership = false; // Owner can't transfer ownership without admin
                    permissions.CanChangeAccessLevel = file.AccessLevel != FileAccessLevel.Admin;
                    permissions.PermissionSource = "owner";
                }
                else
                {
                    // Regular user accessing shared file
                    permissions.CanUpdate = false;
                    permissions.CanDelete = false;
                    permissions.CanShare = false;
                    permissions.CanChangeOwnership = false;
                    permissions.CanChangeAccessLevel = false;
                    permissions.PermissionSource = "shared";
                }

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting effective permissions for user {UserId}, file {FileId}", userId, fileId);
                return new FileEffectivePermissions();
            }
        }

        private static string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private string GenerateShareId()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[12];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("/", "-").Replace("+", "_").TrimEnd('=');
        }

        /// <summary>
        /// Temporary access link data structure
        /// </summary>
        private class TemporaryAccessLink
        {
            public string Token { get; set; } = string.Empty;
            public Guid FileId { get; set; }
            public Guid CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public int MaxDownloads { get; set; }
            public int DownloadCount { get; set; }
            public DateTime? LastAccessedAt { get; set; }
            public bool IsActive { get; set; }
        }
    }
}