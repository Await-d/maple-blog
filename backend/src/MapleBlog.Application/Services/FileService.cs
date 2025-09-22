using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.File;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using System.Security.Cryptography;

namespace MapleBlog.Application.Services;

public class FileService : Application.Interfaces.IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRepository<PostAttachment> _attachmentRepository;
    private readonly string _uploadPath;
    private readonly string _baseUrl;
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;

    public FileService(
        ILogger<FileService> logger,
        IConfiguration configuration,
        IRepository<PostAttachment> attachmentRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _attachmentRepository = attachmentRepository;

        // 配置文件上传设置
        _uploadPath = _configuration.GetValue<string>("FileUpload:UploadPath") ?? "uploads";
        _baseUrl = _configuration.GetValue<string>("FileUpload:BaseUrl") ?? "/files";
        _maxFileSize = _configuration.GetValue<long?>("FileUpload:MaxFileSize") ?? 10L * 1024 * 1024; // 10MB
        _allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".zip" };

        // 确保上传目录存在
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<FileUploadResultDto> UploadFileAsync(FileUploadDto fileUploadDto)
    {
        try
        {
            _logger.LogInformation("开始上传文件: {FileName}, 大小: {Size}", fileUploadDto.FileName, fileUploadDto.Size);

            // 验证文件
            var validationResult = await ValidateFileAsync(fileUploadDto);
            if (!validationResult)
            {
                return FileUploadResultDto.Error("文件验证失败");
            }

            var fileId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(fileUploadDto.FileName).ToLowerInvariant();
            var uniqueFileName = $"{fileId}{fileExtension}";

            // 按日期创建子目录
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var uploadDir = Path.Combine(_uploadPath, dateFolder);

            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var filePath = Path.Combine(uploadDir, uniqueFileName);
            var fileUrl = $"{_baseUrl}/{dateFolder}/{uniqueFileName}".Replace("\\", "/");

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await fileUploadDto.FileStream.CopyToAsync(fileStream);
            }

            // 创建附件记录
            var attachment = new PostAttachment
            {
                Id = fileId,
                FileName = uniqueFileName,
                OriginalFileName = fileUploadDto.FileName,
                ContentType = fileUploadDto.ContentType,
                FileSize = fileUploadDto.Size,
                FilePath = filePath,
                FileUrl = fileUrl,
                Caption = fileUploadDto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 如果是图片，获取尺寸信息
            if (IsImageFile(fileUploadDto.ContentType))
            {
                var dimensions = await GetImageDimensionsAsync(filePath);
                attachment.Width = dimensions.Width;
                attachment.Height = dimensions.Height;
            }

            await _attachmentRepository.AddAsync(attachment);
            await _attachmentRepository.SaveChangesAsync();

            _logger.LogInformation("文件上传成功: {FileId}, 路径: {FilePath}", fileId, filePath);

            var fileInfoDto = new FileInfoDto
            {
                Id = fileId,
                FileName = fileUploadDto.FileName,
                Url = fileUrl,
                Size = fileUploadDto.Size,
                ContentType = fileUploadDto.ContentType,
                UploadedAt = DateTimeOffset.UtcNow,
                Description = fileUploadDto.Description,
                Folder = fileUploadDto.Category
            };

            return FileUploadResultDto.CreateSuccess(fileInfoDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件时发生错误: {FileName}", fileUploadDto.FileName);
            return FileUploadResultDto.Error(ex.Message);
        }
    }

    public async Task<FileUploadResultDto> UploadFileAsync(IFormFile file, string? folder, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return FileUploadResultDto.Error("No file provided");
        }

        using var stream = file.OpenReadStream();
        var uploadDto = new FileUploadDto
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            FileStream = stream,
            Category = folder,
            Description = null
        };

        return await UploadFileAsync(uploadDto);
    }

    public async Task<ImageUploadResultDto> UploadImageAsync(IFormFile image, Guid? postId, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (image == null || image.Length == 0)
        {
            return ImageUploadResultDto.Error("No image provided");
        }

        // Validate it's actually an image
        if (!image.ContentType.StartsWith("image/"))
        {
            return ImageUploadResultDto.Error("File is not an image");
        }

        using var stream = image.OpenReadStream();
        var uploadDto = new FileUploadDto
        {
            FileName = image.FileName,
            ContentType = image.ContentType,
            Size = image.Length,
            FileStream = stream,
            Category = "images",
            Description = null
        };

        var result = await UploadFileAsync(uploadDto);

        // Convert to ImageUploadResultDto
        if (result.Success && result.FileInfo != null)
        {
            return ImageUploadResultDto.CreateSuccess(result.FileInfo);
        }
        else
        {
            return ImageUploadResultDto.Error(result.ErrorMessage ?? "Image upload failed");
        }
    }

    public async Task<FileDto> GetFileAsync(Guid fileId)
    {
        try
        {
            var attachment = await _attachmentRepository.GetByIdAsync(fileId);
            if (attachment == null)
            {
                throw new FileNotFoundException($"文件不存在: {fileId}");
            }

            return new FileDto
            {
                Id = attachment.Id,
                FileName = attachment.OriginalFileName,
                ContentType = attachment.ContentType,
                Size = attachment.FileSize,
                Url = attachment.FileUrl ?? string.Empty,
                Description = attachment.Caption,
                Category = "attachment",
                CreatedAt = attachment.CreatedAt,
                UserId = "system" // 由于PostAttachment没有直接的UserId，这里使用默认值
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息时发生错误: {FileId}", fileId);
            throw;
        }
    }

    public async Task<IEnumerable<FileDto>> GetFilesByUserAsync(string userId)
    {
        try
        {
            // 由于PostAttachment不直接关联用户，这里通过Post关联查询
            // 在实际应用中可能需要修改数据模型来支持这个功能
            var attachments = await _attachmentRepository.GetAllAsync();

            return attachments.Select(a => new FileDto
            {
                Id = a.Id,
                FileName = a.OriginalFileName,
                ContentType = a.ContentType,
                Size = a.FileSize,
                Url = a.FileUrl ?? string.Empty,
                Description = a.Caption,
                Category = "attachment",
                CreatedAt = a.CreatedAt,
                UserId = userId
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户文件列表时发生错误: {UserId}", userId);
            return new List<FileDto>();
        }
    }

    public async Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await _attachmentRepository.GetByIdAsync(fileId);
            if (attachment == null)
            {
                return null;
            }

            return new FileInfoDto
            {
                Id = attachment.Id,
                FileName = attachment.OriginalFileName,
                Url = attachment.FileUrl ?? string.Empty,
                Size = attachment.FileSize,
                ContentType = attachment.ContentType,
                UploadedAt = attachment.CreatedAt,
                Description = attachment.Caption
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件信息时发生错误: {FileId}", fileId);
            return null;
        }
    }

    public async Task<PagedResultDto<FileInfoDto>> GetUserFilesAsync(Guid userId, int pageNumber, int pageSize, string? fileType, CancellationToken cancellationToken)
    {
        try
        {
            var attachments = await _attachmentRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(fileType))
            {
                attachments = attachments.Where(a => a.ContentType.StartsWith(fileType)).ToList();
            }

            var totalCount = attachments.Count();
            var items = attachments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new FileInfoDto
                {
                    Id = a.Id,
                    FileName = a.OriginalFileName,
                    Url = a.FileUrl ?? string.Empty,
                    Size = a.FileSize,
                    ContentType = a.ContentType,
                    UploadedAt = a.CreatedAt,
                    Description = a.Caption
                })
                .ToList();

            return new PagedResultDto<FileInfoDto>
            {
                Items = items,
                TotalItems = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户文件列表时发生错误: {UserId}", userId);
            return PagedResultDto<FileInfoDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<PagedResultDto<FileInfoDto>> GetAllFilesAsync(int pageNumber, int pageSize, string? fileType, Guid? uploadedBy, CancellationToken cancellationToken)
    {
        try
        {
            var attachments = await _attachmentRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(fileType))
            {
                attachments = attachments.Where(a => a.ContentType.StartsWith(fileType)).ToList();
            }

            var totalCount = attachments.Count();
            var items = attachments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new FileInfoDto
                {
                    Id = a.Id,
                    FileName = a.OriginalFileName,
                    Url = a.FileUrl ?? string.Empty,
                    Size = a.FileSize,
                    ContentType = a.ContentType,
                    UploadedAt = a.CreatedAt,
                    Description = a.Caption
                })
                .ToList();

            return new PagedResultDto<FileInfoDto>
            {
                Items = items,
                TotalItems = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有文件列表时发生错误");
            return PagedResultDto<FileInfoDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<FileStreamResultDto?> GetFileStreamAsync(Guid fileId, CancellationToken cancellationToken)
    {
        try
        {
            var attachment = await _attachmentRepository.GetByIdAsync(fileId);
            if (attachment == null || !System.IO.File.Exists(attachment.FilePath))
            {
                return null;
            }

            var stream = new FileStream(attachment.FilePath, FileMode.Open, FileAccess.Read);
            return new FileStreamResultDto
            {
                Stream = stream,
                ContentType = attachment.ContentType,
                FileName = attachment.OriginalFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件流时发生错误: {FileId}", fileId);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        return await DeleteFileAsync(fileId, "system");
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, string userId)
    {
        try
        {
            var attachment = await _attachmentRepository.GetByIdAsync(fileId);
            if (attachment == null)
            {
                _logger.LogWarning("尝试删除不存在的文件: {FileId}", fileId);
                return false;
            }

            // 删除物理文件
            if (System.IO.File.Exists(attachment.FilePath))
            {
                System.IO.File.Delete(attachment.FilePath);
                _logger.LogInformation("已删除物理文件: {FilePath}", attachment.FilePath);
            }

            // 删除数据库记录
            _attachmentRepository.Remove(attachment);
            await _attachmentRepository.SaveChangesAsync();

            _logger.LogInformation("文件删除成功: {FileId}, 用户: {UserId}", fileId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件时发生错误: {FileId}, 用户: {UserId}", fileId, userId);
            return false;
        }
    }

    public async Task<FileDto> UpdateFileMetadataAsync(Guid fileId, UpdateFileMetadataDto updateDto)
    {
        try
        {
            var attachment = await _attachmentRepository.GetByIdAsync(fileId);
            if (attachment == null)
            {
                throw new FileNotFoundException($"文件不存在: {fileId}");
            }

            // 更新元数据
            if (!string.IsNullOrEmpty(updateDto.Description))
            {
                attachment.Caption = updateDto.Description;
            }

            if (!string.IsNullOrEmpty(updateDto.Category))
            {
                // 可以在这里添加分类逻辑，目前PostAttachment没有分类字段
            }

            attachment.UpdatedAt = DateTime.UtcNow;
            _attachmentRepository.Update(attachment);
            await _attachmentRepository.SaveChangesAsync();

            _logger.LogInformation("文件元数据更新成功: {FileId}", fileId);
            return await GetFileAsync(fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新文件元数据时发生错误: {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> ValidateFileAsync(FileUploadDto fileUploadDto)
    {
        try
        {
            // 检查文件大小
            if (fileUploadDto.Size > _maxFileSize)
            {
                _logger.LogWarning("文件大小超出限制: {Size}/{MaxSize}", fileUploadDto.Size, _maxFileSize);
                return false;
            }

            // 检查文件扩展名
            var extension = Path.GetExtension(fileUploadDto.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("文件类型不被允许: {Extension}", extension);
                return false;
            }

            // 检查文件名
            if (string.IsNullOrWhiteSpace(fileUploadDto.FileName) || fileUploadDto.FileName.Length > 255)
            {
                _logger.LogWarning("文件名无效: {FileName}", fileUploadDto.FileName);
                return false;
            }

            // 检查内容类型
            if (string.IsNullOrWhiteSpace(fileUploadDto.ContentType))
            {
                _logger.LogWarning("内容类型无效: {ContentType}", fileUploadDto.ContentType);
                return false;
            }

            // 检查文件流
            if (fileUploadDto.FileStream == null || !fileUploadDto.FileStream.CanRead)
            {
                _logger.LogWarning("文件流无效");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证文件时发生错误: {FileName}", fileUploadDto.FileName);
            return false;
        }
    }

    public async Task<string> GetFileUrlAsync(Guid fileId)
    {
        try
        {
            var attachment = await _attachmentRepository.GetByIdAsync(fileId);
            if (attachment?.FileUrl != null)
            {
                return attachment.FileUrl;
            }

            _logger.LogWarning("文件URL不存在: {FileId}", fileId);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文件URL时发生错误: {FileId}", fileId);
            return string.Empty;
        }
    }

    public async Task<long> GetUserStorageUsageAsync(string userId)
    {
        try
        {
            // 由于PostAttachment不直接关联用户，这里返回所有附件的总大小
            // 在实际应用中可能需要通过Post关联查询特定用户的文件
            var attachments = await _attachmentRepository.GetAllAsync();
            var totalSize = attachments.Sum(a => a.FileSize);

            _logger.LogInformation("用户 {UserId} 的存储使用量: {TotalSize} 字节", userId, totalSize);
            return totalSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户存储使用量时发生错误: {UserId}", userId);
            return 0;
        }
    }

    public async Task<IEnumerable<FileDto>> GetFilesByTypeAsync(string fileType)
    {
        try
        {
            var attachments = await _attachmentRepository.GetAllAsync();
            var filteredAttachments = attachments.Where(a =>
                a.ContentType.StartsWith(fileType, StringComparison.OrdinalIgnoreCase));

            return filteredAttachments.Select(a => new FileDto
            {
                Id = a.Id,
                FileName = a.OriginalFileName,
                ContentType = a.ContentType,
                Size = a.FileSize,
                Url = a.FileUrl ?? string.Empty,
                Description = a.Caption,
                Category = "attachment",
                CreatedAt = a.CreatedAt,
                UserId = "system"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取文件列表时发生错误: {FileType}", fileType);
            return new List<FileDto>();
        }
    }

    public async Task<bool> CleanupOrphanedFilesAsync()
    {
        try
        {
            _logger.LogInformation("开始清理孤立文件");

            var allAttachments = await _attachmentRepository.GetAllAsync();
            var cleanedCount = 0;

            foreach (var attachment in allAttachments)
            {
                // 检查物理文件是否存在
                if (!System.IO.File.Exists(attachment.FilePath))
                {
                    _logger.LogWarning("发现孤立的数据库记录，物理文件不存在: {FilePath}", attachment.FilePath);
                    _attachmentRepository.Remove(attachment);
                    cleanedCount++;
                }
                // 检查是否是孤立的附件（没有关联到任何Post）
                else if (attachment.PostId == null || attachment.PostId == Guid.Empty)
                {
                    // 检查文件创建时间，如果超过7天且没有关联到Post，则删除
                    if (attachment.CreatedAt < DateTime.UtcNow.AddDays(-7))
                    {
                        _logger.LogInformation("删除超过7天的孤立文件: {FilePath}", attachment.FilePath);

                        if (System.IO.File.Exists(attachment.FilePath))
                        {
                            System.IO.File.Delete(attachment.FilePath);
                        }

                        _attachmentRepository.Remove(attachment);
                        cleanedCount++;
                    }
                }
            }

            if (cleanedCount > 0)
            {
                await _attachmentRepository.SaveChangesAsync();
            }

            // 清理空的上传目录
            CleanupEmptyDirectories(_uploadPath);

            _logger.LogInformation("孤立文件清理完成，清理了 {CleanedCount} 个文件", cleanedCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理孤立文件时发生错误");
            return false;
        }
    }

    private bool IsImageFile(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(int? Width, int? Height)> GetImageDimensionsAsync(string filePath)
    {
        try
        {
            // 简单的图片尺寸获取逻辑，在实际应用中可能需要使用专门的图像处理库
            // 这里返回默认值，可以后续使用 ImageSharp 或其他库来实现
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取图片尺寸失败: {FilePath}", filePath);
            return (null, null);
        }
    }

    private void CleanupEmptyDirectories(string rootPath)
    {
        try
        {
            foreach (var directory in Directory.GetDirectories(rootPath))
            {
                CleanupEmptyDirectories(directory);

                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    _logger.LogInformation("删除空目录: {Directory}", directory);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理空目录时发生错误: {RootPath}", rootPath);
        }
    }

    public async Task<FileStorageStatsDto> GetStorageStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var attachments = await _attachmentRepository.GetAllAsync();

            var totalFiles = attachments.Count();
            var totalSize = attachments.Sum(a => a.FileSize);
            var imageFiles = attachments.Count(a => a.ContentType.StartsWith("image/"));
            var documentFiles = attachments.Count(a => !a.ContentType.StartsWith("image/"));

            return new FileStorageStatsDto
            {
                TotalFiles = totalFiles,
                TotalStorageUsed = totalSize,
                ImageFiles = imageFiles,
                DocumentFiles = documentFiles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取存储统计信息时发生错误");
            return new FileStorageStatsDto();
        }
    }
}