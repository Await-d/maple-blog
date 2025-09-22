using Microsoft.AspNetCore.Http;
using MapleBlog.Application.DTOs.File;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Service for managing user avatars
    /// </summary>
    public interface IAvatarService
    {
        /// <summary>
        /// Uploads a new avatar for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="avatarFile">Avatar image file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Avatar upload result</returns>
        Task<AvatarUploadResultDto> UploadAvatarAsync(Guid userId, IFormFile avatarFile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user's current avatar
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="size">Requested size (small, medium, large, original)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Avatar information</returns>
        Task<AvatarDto?> GetUserAvatarAsync(Guid userId, AvatarSize size = AvatarSize.Medium, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets avatar file stream
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="size">Avatar size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Avatar file stream</returns>
        Task<FileStreamResultDto?> GetAvatarStreamAsync(Guid userId, AvatarSize size = AvatarSize.Medium, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes user's avatar
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteAvatarAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates default avatar for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="userName">User name for avatar generation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Default avatar information</returns>
        Task<AvatarDto> GenerateDefaultAvatarAsync(Guid userId, string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crops and resizes avatar
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cropRequest">Crop parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cropped avatar result</returns>
        Task<AvatarUploadResultDto> CropAvatarAsync(Guid userId, AvatarCropRequestDto cropRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets avatar URL for user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="size">Avatar size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Avatar URL</returns>
        Task<string> GetAvatarUrlAsync(Guid userId, AvatarSize size = AvatarSize.Medium, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates avatar file
        /// </summary>
        /// <param name="file">Avatar file</param>
        /// <returns>Validation result</returns>
        Task<AvatarValidationResultDto> ValidateAvatarAsync(IFormFile file);

        /// <summary>
        /// Gets avatar statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Avatar usage statistics</returns>
        Task<AvatarStatsDto> GetAvatarStatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes avatar thumbnails for all sizes
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="originalAvatarPath">Path to original avatar</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if processed successfully</returns>
        Task<bool> ProcessAvatarThumbnailsAsync(Guid userId, string originalAvatarPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleans up old avatar files for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of files cleaned up</returns>
        Task<int> CleanupOldAvatarsAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Avatar size options
    /// </summary>
    public enum AvatarSize
    {
        /// <summary>
        /// Small avatar (32x32)
        /// </summary>
        Small = 32,

        /// <summary>
        /// Medium avatar (64x64)
        /// </summary>
        Medium = 64,

        /// <summary>
        /// Large avatar (128x128)
        /// </summary>
        Large = 128,

        /// <summary>
        /// Extra large avatar (256x256)
        /// </summary>
        ExtraLarge = 256,

        /// <summary>
        /// Original uploaded size
        /// </summary>
        Original = 0
    }

    /// <summary>
    /// Avatar upload result
    /// </summary>
    public class AvatarUploadResultDto
    {
        /// <summary>
        /// Whether upload was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Avatar information if successful
        /// </summary>
        public AvatarDto? Avatar { get; set; }

        /// <summary>
        /// URLs for different sizes
        /// </summary>
        public Dictionary<AvatarSize, string> Urls { get; set; } = new();

        /// <summary>
        /// Creates successful result
        /// </summary>
        public static AvatarUploadResultDto CreateSuccess(AvatarDto avatar, Dictionary<AvatarSize, string> urls)
        {
            return new AvatarUploadResultDto
            {
                Success = true,
                Avatar = avatar,
                Urls = urls
            };
        }

        /// <summary>
        /// Creates error result
        /// </summary>
        public static AvatarUploadResultDto Error(string errorMessage)
        {
            return new AvatarUploadResultDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Avatar information
    /// </summary>
    public class AvatarDto
    {
        /// <summary>
        /// User ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Avatar file ID
        /// </summary>
        public Guid? FileId { get; set; }

        /// <summary>
        /// Avatar URL
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Original filename
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Upload date
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Image width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Image height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a default generated avatar
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Available sizes
        /// </summary>
        public Dictionary<AvatarSize, string> AvailableSizes { get; set; } = new();
    }

    /// <summary>
    /// Avatar crop request
    /// </summary>
    public class AvatarCropRequestDto
    {
        /// <summary>
        /// X coordinate of crop area
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y coordinate of crop area
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Width of crop area
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of crop area
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Rotation angle in degrees
        /// </summary>
        public int Rotation { get; set; }

        /// <summary>
        /// Scale factor
        /// </summary>
        public double Scale { get; set; } = 1.0;
    }

    /// <summary>
    /// Avatar validation result
    /// </summary>
    public class AvatarValidationResultDto
    {
        /// <summary>
        /// Whether avatar is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Detected image format
        /// </summary>
        public string? ImageFormat { get; set; }

        /// <summary>
        /// Image dimensions
        /// </summary>
        public (int Width, int Height)? Dimensions { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public long FileSize { get; set; }
    }

    /// <summary>
    /// Avatar usage statistics
    /// </summary>
    public class AvatarStatsDto
    {
        /// <summary>
        /// Total users with avatars
        /// </summary>
        public int UsersWithAvatars { get; set; }

        /// <summary>
        /// Users with default avatars
        /// </summary>
        public int UsersWithDefaultAvatars { get; set; }

        /// <summary>
        /// Users with custom avatars
        /// </summary>
        public int UsersWithCustomAvatars { get; set; }

        /// <summary>
        /// Total avatar storage used
        /// </summary>
        public long TotalStorageUsed { get; set; }

        /// <summary>
        /// Average avatar file size
        /// </summary>
        public long AverageFileSize { get; set; }

        /// <summary>
        /// Most common avatar formats
        /// </summary>
        public Dictionary<string, int> FormatDistribution { get; set; } = new();

        /// <summary>
        /// Recent avatar uploads
        /// </summary>
        public int RecentUploads { get; set; }
    }
}