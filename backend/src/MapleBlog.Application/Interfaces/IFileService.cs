using Microsoft.AspNetCore.Http;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.File;

namespace MapleBlog.Application.Interfaces;

public interface IFileService
{
    Task<FileUploadResultDto> UploadFileAsync(FileUploadDto fileUploadDto);
    Task<FileUploadResultDto> UploadFileAsync(IFormFile file, string? folder, Guid currentUserId, CancellationToken cancellationToken);
    Task<ImageUploadResultDto> UploadImageAsync(IFormFile image, Guid? postId, Guid currentUserId, CancellationToken cancellationToken);
    Task<FileDto> GetFileAsync(Guid fileId);
    Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, CancellationToken cancellationToken);
    Task<IEnumerable<FileDto>> GetFilesByUserAsync(string userId);
    Task<PagedResultDto<FileInfoDto>> GetUserFilesAsync(Guid userId, int pageNumber, int pageSize, string? fileType, CancellationToken cancellationToken);
    Task<PagedResultDto<FileInfoDto>> GetAllFilesAsync(int pageNumber, int pageSize, string? fileType, Guid? uploadedBy, CancellationToken cancellationToken);
    Task<FileStorageStatsDto> GetStorageStatsAsync(CancellationToken cancellationToken);
    Task<FileStreamResultDto?> GetFileStreamAsync(Guid fileId, CancellationToken cancellationToken);
    Task<bool> DeleteFileAsync(Guid fileId, CancellationToken cancellationToken);
    Task<bool> DeleteFileAsync(Guid fileId, string userId);
    Task<FileDto> UpdateFileMetadataAsync(Guid fileId, UpdateFileMetadataDto updateDto);
    Task<bool> ValidateFileAsync(FileUploadDto fileUploadDto);
    Task<string> GetFileUrlAsync(Guid fileId);
    Task<long> GetUserStorageUsageAsync(string userId);
    Task<IEnumerable<FileDto>> GetFilesByTypeAsync(string fileType);
    Task<bool> CleanupOrphanedFilesAsync();
}

/// <summary>
/// File stream result for downloads
/// </summary>
public class FileStreamResultDto
{
    public Stream Stream { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}