using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.File;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace MapleBlog.Infrastructure.Services
{
    /// <summary>
    /// File service implementation for local file system storage
    /// </summary>
    public class FileService : Application.Interfaces.IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly IFileRepository _fileRepository;
        private readonly IUserContextService _userContextService;
        private readonly string _uploadPath;
        private readonly string _baseUrl;

        private static readonly Dictionary<string, string> ContentTypeMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".webp", "image/webp" },
            { ".bmp", "image/bmp" },
            { ".tiff", "image/tiff" },
            { ".svg", "image/svg+xml" },
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".txt", "text/plain" },
            { ".md", "text/markdown" },
            { ".zip", "application/zip" },
            { ".rar", "application/x-rar-compressed" }
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff", ".svg"
        };

        private static readonly HashSet<string> DangerousExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".msi", ".dll"
        };

        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt", ".md", ".rtf", ".odt", ".xls", ".xlsx", ".ppt", ".pptx"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".m4v"
        };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a"
        };

        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz"
        };

        public FileService(
            IConfiguration configuration,
            ILogger<FileService> logger,
            IFileRepository fileRepository,
            IUserContextService userContextService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));

            _uploadPath = _configuration["FileStorage:UploadPath"] ?? "uploads";
            _baseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";

            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<FileUploadResult> UploadFileAsync(
            IFormFile file,
            string subDirectory = "general",
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var uploadId = Guid.NewGuid().ToString("N")[..8];

            _logger.LogInformation("Starting file upload {UploadId}: {FileName} ({FileSize} bytes) to {SubDirectory}",
                uploadId, file?.FileName, file?.Length, subDirectory);

            if (file == null || file.Length == 0)
            {
                stopwatch.Stop();
                _logger.LogWarning("File upload {UploadId} failed: File is null or empty after {ElapsedMs}ms",
                    uploadId, stopwatch.ElapsedMilliseconds);

                return new FileUploadResult
                {
                    Success = false,
                    Error = "File is null or empty"
                };
            }

            // Validate file
            var validationOptions = GetDefaultValidationOptions();
            var validationResult = ValidateFile(file, validationOptions);
            if (!validationResult.IsValid)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Error = string.Join(", ", validationResult.Errors)
                };
            }

            // Get current user for quota checking
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("File upload attempted without authenticated user context");
                // Use the admin user ID from seed data as fallback
                currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
            }

            // Check storage quota before upload
            var quotaCheck = await CheckUserStorageQuotaAsync(currentUserId.Value, file.Length, cancellationToken);
            if (!quotaCheck)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Error = "Storage quota exceeded. Please delete some files before uploading new ones."
                };
            }

            try
            {
                var subDirectoryPath = Path.Combine(_uploadPath, SanitizeDirectoryName(subDirectory));
                Directory.CreateDirectory(subDirectoryPath);

                var fileName = await GenerateUniqueFileName(file.FileName, subDirectoryPath, cancellationToken);
                var filePath = Path.Combine(subDirectoryPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                // Perform security scan
                var securityScan = await ScanFileForSecurityThreatsAsync(filePath, file.ContentType, cancellationToken);
                if (!securityScan.IsSafe)
                {
                    // Delete the uploaded file if it's unsafe
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete unsafe file: {FilePath}", filePath);
                    }

                    return new FileUploadResult
                    {
                        Success = false,
                        Error = $"File failed security scan: {string.Join(", ", securityScan.Threats)}"
                    };
                }

                // Log security scan results if there are warnings
                if (securityScan.Threats.Any())
                {
                    _logger.LogWarning("File security scan found warnings for {FilePath}: {Threats}",
                        filePath, string.Join(", ", securityScan.Threats));
                }

                var fileHash = await CalculateFileHashAsync(filePath, cancellationToken);
                var relativeFilePath = Path.Combine(subDirectory, fileName).Replace('\\', '/');
                var publicUrl = $"{_baseUrl.TrimEnd('/')}/{relativeFilePath}";

                // Check for duplicate files (deduplication)
                var (isDuplicate, existingFile) = await CheckForDuplicateFileAsync(fileHash, currentUserId.Value, cancellationToken);
                if (isDuplicate && existingFile != null)
                {
                    // Delete the just-uploaded file since we found a duplicate
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete duplicate file: {FilePath}", filePath);
                    }

                    return new FileUploadResult
                    {
                        Success = true,
                        FilePath = $"{_baseUrl.TrimEnd('/')}/{existingFile.FilePath}",
                        FileName = existingFile.FileName,
                        FileSize = existingFile.FileSize,
                        ContentType = existingFile.ContentType,
                        Metadata = new FileMetadata
                        {
                            OriginalName = existingFile.OriginalFileName,
                            Extension = existingFile.Extension,
                            ContentType = existingFile.ContentType,
                            Size = existingFile.FileSize,
                            CreatedAt = existingFile.CreatedAt,
                            Hash = existingFile.FileHash,
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["FileId"] = existingFile.Id,
                                ["UserId"] = currentUserId.Value,
                                ["IsDeduplication"] = true,
                                ["DuplicateCount"] = existingFile.ReferenceCount + 1
                            }
                        }
                    };
                }

                // Extract image dimensions if it's an image
                int? imageWidth = null;
                int? imageHeight = null;
                if (IsImageFile(fileName))
                {
                    try
                    {
                        var dimensions = await GetImageDimensionsAsync(filePath, cancellationToken);
                        imageWidth = dimensions.Width;
                        imageHeight = dimensions.Height;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not extract image dimensions for file: {FileName}", fileName);
                    }
                }

                // Create file entity for database storage
                var fileEntity = new MapleBlog.Domain.Entities.File
                {
                    Id = Guid.NewGuid(),
                    OriginalFileName = file.FileName,
                    FileName = fileName,
                    Extension = Path.GetExtension(file.FileName),
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FilePath = relativeFilePath,
                    Directory = subDirectory,
                    FileHash = fileHash,
                    UserId = currentUserId.Value,
                    IsInUse = true,
                    ReferenceCount = 1,
                    IsPublic = true,
                    AccessLevel = FileAccessLevel.Public,
                    ImageWidth = imageWidth,
                    ImageHeight = imageHeight,
                    UploadIpAddress = _userContextService.GetClientIpAddress(),
                    UploadUserAgent = _userContextService.GetUserAgent(),
                    AccessCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                // Save to database
                var savedFile = await _fileRepository.AddAsync(fileEntity, cancellationToken);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                var metadata = new FileMetadata
                {
                    OriginalName = file.FileName,
                    Extension = Path.GetExtension(file.FileName),
                    ContentType = file.ContentType,
                    Size = file.Length,
                    CreatedAt = DateTime.UtcNow,
                    Hash = fileHash,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["FileId"] = savedFile.Id,
                        ["UserId"] = currentUserId.Value,
                        ["ImageWidth"] = imageWidth,
                        ["ImageHeight"] = imageHeight
                    }
                };

                stopwatch.Stop();
                _logger.LogInformation("File upload {UploadId} completed successfully: {FileName} -> {FilePath} (ID: {FileId}) in {ElapsedMs}ms",
                    uploadId, file.FileName, publicUrl, savedFile.Id, stopwatch.ElapsedMilliseconds);

                return new FileUploadResult
                {
                    Success = true,
                    FilePath = publicUrl,
                    FileName = fileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    Metadata = metadata
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "File upload {UploadId} failed for {FileName} after {ElapsedMs}ms",
                    uploadId, file.FileName, stopwatch.ElapsedMilliseconds);

                return new FileUploadResult
                {
                    Success = false,
                    Error = "Failed to upload file"
                };
            }
        }

        public async Task<IReadOnlyList<FileUploadResult>> UploadFilesAsync(
            IEnumerable<IFormFile> files,
            string subDirectory = "general",
            CancellationToken cancellationToken = default)
        {
            var results = new List<FileUploadResult>();

            foreach (var file in files)
            {
                var result = await UploadFileAsync(file, subDirectory, cancellationToken);
                results.Add(result);
            }

            return results;
        }

        public async Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var physicalPath = GetPhysicalPath(filePath);
                if (!System.IO.File.Exists(physicalPath))
                    return false;

                System.IO.File.Delete(physicalPath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<int> DeleteFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            var deletedCount = 0;

            foreach (var filePath in filePaths)
            {
                if (await DeleteFileAsync(filePath, cancellationToken))
                {
                    deletedCount++;
                }
            }

            return deletedCount;
        }

        public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var physicalPath = GetPhysicalPath(filePath);
            return System.IO.File.Exists(physicalPath);
        }

        public async Task<FileInfo?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var physicalPath = GetPhysicalPath(filePath);
            if (!System.IO.File.Exists(physicalPath))
                return null;

            var fileInfo = new System.IO.FileInfo(physicalPath);
            return fileInfo;
        }

        public async Task<bool> CopyFileAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sourcePhysicalPath = GetPhysicalPath(sourcePath);
                var destinationPhysicalPath = GetPhysicalPath(destinationPath);

                if (!System.IO.File.Exists(sourcePhysicalPath))
                    return false;

                var destinationDirectory = Path.GetDirectoryName(destinationPhysicalPath);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory!);
                }

                System.IO.File.Copy(sourcePhysicalPath, destinationPhysicalPath, true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourcePath, destinationPath);
                return false;
            }
        }

        public async Task<bool> MoveFileAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sourcePhysicalPath = GetPhysicalPath(sourcePath);
                var destinationPhysicalPath = GetPhysicalPath(destinationPath);

                if (!System.IO.File.Exists(sourcePhysicalPath))
                    return false;

                var destinationDirectory = Path.GetDirectoryName(destinationPhysicalPath);
                if (!Directory.Exists(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory!);
                }

                System.IO.File.Move(sourcePhysicalPath, destinationPhysicalPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file from {Source} to {Destination}", sourcePath, destinationPath);
                return false;
            }
        }

        public async Task<FileDownloadResult?> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var physicalPath = GetPhysicalPath(filePath);
            if (!System.IO.File.Exists(physicalPath))
                return null;

            try
            {
                var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                var fileName = Path.GetFileName(physicalPath);
                var extension = Path.GetExtension(fileName);
                var contentType = ContentTypeMapping.TryGetValue(extension, out var type) ? type : "application/octet-stream";
                var fileInfo = new System.IO.FileInfo(physicalPath);

                return new FileDownloadResult
                {
                    Stream = stream,
                    ContentType = contentType,
                    FileName = fileName,
                    ContentLength = fileInfo.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file stream for: {FilePath}", filePath);
                return null;
            }
        }

        public FileValidationResult ValidateFile(IFormFile file, FileValidationOptions? options = null)
        {
            options ??= GetDefaultValidationOptions();
            var errors = new List<string>();

            // Check file size
            if (file.Length > options.MaxFileSize)
            {
                errors.Add($"File size ({file.Length:N0} bytes) exceeds maximum allowed size ({options.MaxFileSize:N0} bytes)");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                errors.Add("File must have an extension");
            }
            else
            {
                // Check against dangerous extensions
                if (DangerousExtensions.Contains(extension))
                {
                    errors.Add($"File extension '{extension}' is not allowed for security reasons");
                }

                // Check allowed extensions if specified
                if (options.AllowedExtensions.Any() && !options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", options.AllowedExtensions)}");
                }
            }

            // Check content type
            if (options.AllowedContentTypes.Any() && !options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"Content type '{file.ContentType}' is not allowed. Allowed types: {string.Join(", ", options.AllowedContentTypes)}");
            }

            // Check if image format is required
            if (options.RequireImageFormat && !ImageExtensions.Contains(extension))
            {
                errors.Add("File must be an image");
            }

            // Validate file name
            if (HasInvalidFileNameCharacters(file.FileName))
            {
                errors.Add("File name contains invalid characters");
            }

            return new FileValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        public async Task<int> CleanupOrphanedFilesAsync(int olderThanDays = 30, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            var deletedCount = 0;
            var totalChecked = 0;
            var errors = 0;

            try
            {
                _logger.LogInformation("Starting orphaned files cleanup for files older than {CutoffDate}", cutoffDate);

                // Get orphaned files from database
                var orphanedFiles = await _fileRepository.GetOrphanedFilesAsync(olderThanDays, cancellationToken);
                totalChecked = orphanedFiles.Count;

                _logger.LogInformation("Found {Count} potentially orphaned files in database", orphanedFiles.Count);

                foreach (var fileRecord in orphanedFiles)
                {
                    try
                    {
                        var physicalPath = Path.Combine(_uploadPath, fileRecord.FilePath);

                        // Check if physical file exists
                        if (System.IO.File.Exists(physicalPath))
                        {
                            // Delete physical file
                            System.IO.File.Delete(physicalPath);
                            _logger.LogDebug("Deleted physical file: {FilePath}", physicalPath);
                        }

                        // Remove from database
                        _fileRepository.Remove(fileRecord);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        _logger.LogWarning(ex, "Failed to cleanup orphaned file: {FileId} ({FilePath})",
                            fileRecord.Id, fileRecord.FilePath);
                    }
                }

                // Save database changes
                if (deletedCount > 0)
                {
                    await _fileRepository.SaveChangesAsync(cancellationToken);
                }

                stopwatch.Stop();
                _logger.LogInformation("Orphaned files cleanup completed. Checked: {TotalChecked}, Deleted: {DeletedCount}, Errors: {Errors}, Duration: {ElapsedMs}ms",
                    totalChecked, deletedCount, errors, stopwatch.ElapsedMilliseconds);

                return deletedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Critical error during orphaned file cleanup after {ElapsedMs}ms. Checked: {TotalChecked}, Deleted: {DeletedCount}",
                    stopwatch.ElapsedMilliseconds, totalChecked, deletedCount);

                return deletedCount;
            }
        }

        public async Task<StorageStatistics> GetStorageStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var totalSize = 0L;
                var totalFiles = 0;
                var sizeByDirectory = new Dictionary<string, long>();
                var fileCountByType = new Dictionary<string, int>();

                var allFiles = Directory.GetFiles(_uploadPath, "*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    var directory = Path.GetDirectoryName(file)?.Replace(_uploadPath, "").Trim(Path.DirectorySeparatorChar) ?? "root";
                    var extension = Path.GetExtension(file).ToLowerInvariant();

                    totalSize += fileInfo.Length;
                    totalFiles++;

                    // Track size by directory
                    if (sizeByDirectory.ContainsKey(directory))
                        sizeByDirectory[directory] += fileInfo.Length;
                    else
                        sizeByDirectory[directory] = fileInfo.Length;

                    // Track file count by type
                    if (fileCountByType.ContainsKey(extension))
                        fileCountByType[extension]++;
                    else
                        fileCountByType[extension] = 1;
                }

                return new StorageStatistics
                {
                    TotalSize = totalSize,
                    TotalFiles = totalFiles,
                    SizeByDirectory = sizeByDirectory,
                    FileCountByType = fileCountByType,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating storage statistics");
                return new StorageStatistics { LastUpdated = DateTime.UtcNow };
            }
        }

        /// <summary>
        /// Gets default file validation options from configuration
        /// </summary>
        private FileValidationOptions GetDefaultValidationOptions()
        {
            var maxFileSize = _configuration.GetValue<long>("FileStorage:MaxFileSize", 10 * 1024 * 1024); // 10MB
            var allowedExtensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();

            return new FileValidationOptions
            {
                MaxFileSize = maxFileSize,
                AllowedExtensions = allowedExtensions,
                RequireImageFormat = false,
                ScanForMalware = false
            };
        }

        /// <summary>
        /// Generates a unique file name to prevent conflicts
        /// </summary>
        private async Task<string> GenerateUniqueFileName(string originalFileName, string directoryPath, CancellationToken cancellationToken)
        {
            var sanitizedName = SanitizeFileName(Path.GetFileNameWithoutExtension(originalFileName));
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];

            var fileName = $"{timestamp}_{uniqueId}_{sanitizedName}{extension}";

            // Ensure the file name is not too long
            if (fileName.Length > 255)
            {
                var maxNameLength = 255 - extension.Length - timestamp.Length - uniqueId.Length - 2; // 2 for underscores
                sanitizedName = sanitizedName.Substring(0, Math.Min(sanitizedName.Length, maxNameLength));
                fileName = $"{timestamp}_{uniqueId}_{sanitizedName}{extension}";
            }

            // Final check for uniqueness
            var filePath = Path.Combine(directoryPath, fileName);
            var counter = 1;
            while (System.IO.File.Exists(filePath))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                fileName = $"{nameWithoutExt}_{counter}{extension}";
                filePath = Path.Combine(directoryPath, fileName);
                counter++;
            }

            return fileName;
        }

        /// <summary>
        /// Sanitizes a file name by removing invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var regex = new Regex($"[{Regex.Escape(new string(invalidChars))}]");
            var sanitized = regex.Replace(fileName, "_");

            // Also remove other potentially problematic characters
            sanitized = Regex.Replace(sanitized, @"[^\w\-_\.]", "_");
            sanitized = Regex.Replace(sanitized, @"_+", "_").Trim('_');

            return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
        }

        /// <summary>
        /// Sanitizes a directory name
        /// </summary>
        private string SanitizeDirectoryName(string directoryName)
        {
            var invalidChars = Path.GetInvalidPathChars();
            var regex = new Regex($"[{Regex.Escape(new string(invalidChars))}]");
            var sanitized = regex.Replace(directoryName, "_");

            return string.IsNullOrEmpty(sanitized) ? "general" : sanitized;
        }

        /// <summary>
        /// Checks if file name has invalid characters
        /// </summary>
        private bool HasInvalidFileNameCharacters(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return fileName.IndexOfAny(invalidChars) >= 0;
        }

        /// <summary>
        /// Performs basic file security scanning
        /// </summary>
        private async Task<FileSecurityScanResult> ScanFileForSecurityThreatsAsync(string filePath, string contentType, CancellationToken cancellationToken)
        {
            var result = new FileSecurityScanResult
            {
                IsSafe = true,
                Threats = new List<string>(),
                ScanTimestamp = DateTime.UtcNow
            };

            try
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                // Check for dangerous file extensions
                if (DangerousExtensions.Contains(extension))
                {
                    result.IsSafe = false;
                    result.Threats.Add($"Potentially dangerous file extension: {extension}");
                }

                // Check for suspicious file size patterns
                if (fileInfo.Length == 0)
                {
                    result.IsSafe = false;
                    result.Threats.Add("Empty file detected");
                }
                else if (fileInfo.Length > 500 * 1024 * 1024) // 500MB
                {
                    result.Threats.Add("Unusually large file size");
                    // Don't mark as unsafe, just note the warning
                }

                // Check for content type mismatch
                if (!string.IsNullOrEmpty(contentType))
                {
                    var expectedContentType = ContentTypeMapping.GetValueOrDefault(extension, "application/octet-stream");
                    if (!contentType.Equals(expectedContentType, StringComparison.OrdinalIgnoreCase) &&
                        !contentType.StartsWith("application/octet-stream"))
                    {
                        result.Threats.Add($"Content type mismatch: expected {expectedContentType}, got {contentType}");
                    }
                }

                // Basic file header validation for common formats
                await ValidateFileHeaderAsync(filePath, extension, result, cancellationToken);

                // Check for embedded executable content (basic scan)
                await ScanForEmbeddedExecutablesAsync(filePath, result, cancellationToken);

                _logger.LogDebug("Security scan completed for file: {FilePath}. Safe: {IsSafe}, Threats: {ThreatCount}",
                    filePath, result.IsSafe, result.Threats.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security scan for file: {FilePath}", filePath);
                result.IsSafe = false;
                result.Threats.Add("Security scan failed - marking as unsafe");
                return result;
            }
        }

        /// <summary>
        /// Validates file headers against expected signatures
        /// </summary>
        private async Task ValidateFileHeaderAsync(string filePath, string extension, FileSecurityScanResult result, CancellationToken cancellationToken)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                var headerBytes = new byte[16]; // Read first 16 bytes
                var bytesRead = await stream.ReadAsync(headerBytes, 0, 16, cancellationToken);

                if (bytesRead == 0) return;

                // Check common file signatures
                var headerHex = Convert.ToHexString(headerBytes[..bytesRead]).ToLowerInvariant();

                switch (extension)
                {
                    case ".pdf":
                        if (!headerHex.StartsWith("255044462d")) // %PDF-
                        {
                            result.Threats.Add("Invalid PDF file header");
                        }
                        break;
                    case ".jpg" or ".jpeg":
                        if (!headerHex.StartsWith("ffd8ff"))
                        {
                            result.Threats.Add("Invalid JPEG file header");
                        }
                        break;
                    case ".png":
                        if (!headerHex.StartsWith("89504e47"))
                        {
                            result.Threats.Add("Invalid PNG file header");
                        }
                        break;
                    case ".gif":
                        if (!headerHex.StartsWith("474946"))
                        {
                            result.Threats.Add("Invalid GIF file header");
                        }
                        break;
                    case ".zip":
                        if (!headerHex.StartsWith("504b0304") && !headerHex.StartsWith("504b0506"))
                        {
                            result.Threats.Add("Invalid ZIP file header");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not validate file header for: {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Scans for embedded executable content (basic implementation)
        /// </summary>
        private async Task ScanForEmbeddedExecutablesAsync(string filePath, FileSecurityScanResult result, CancellationToken cancellationToken)
        {
            try
            {
                var fileSize = new System.IO.FileInfo(filePath).Length;

                // Skip scanning very large files for performance
                if (fileSize > 50 * 1024 * 1024) // 50MB
                {
                    return;
                }

                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                var buffer = new byte[8192];
                var suspiciousPatterns = new byte[][]
                {
                    System.Text.Encoding.ASCII.GetBytes("MZ"), // PE executable header
                    System.Text.Encoding.ASCII.GetBytes("\x7fELF"), // ELF header
                    System.Text.Encoding.ASCII.GetBytes("#!/bin/"), // Unix shebang
                    System.Text.Encoding.ASCII.GetBytes("<script"), // JavaScript
                    System.Text.Encoding.ASCII.GetBytes("javascript:"), // JavaScript URL
                };

                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    foreach (var pattern in suspiciousPatterns)
                    {
                        if (ContainsPattern(buffer, bytesRead, pattern))
                        {
                            var patternName = System.Text.Encoding.ASCII.GetString(pattern);
                            result.Threats.Add($"Potentially embedded executable content detected: {patternName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not scan for embedded executables in: {FilePath}", filePath);
            }
        }

        /// <summary>
        /// Checks if buffer contains a specific byte pattern
        /// </summary>
        private static bool ContainsPattern(byte[] buffer, int length, byte[] pattern)
        {
            for (int i = 0; i <= length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return true;
            }
            return false;
        }

        /// <summary>
        /// File security scan result
        /// </summary>
        private class FileSecurityScanResult
        {
            public bool IsSafe { get; set; }
            public List<string> Threats { get; set; } = new();
            public DateTime ScanTimestamp { get; set; }
        }

        /// <summary>
        /// Gets the physical file path from a public file path
        /// </summary>
        private string GetPhysicalPath(string filePath)
        {
            // Remove base URL if present
            if (filePath.StartsWith(_baseUrl))
            {
                filePath = filePath.Substring(_baseUrl.Length).TrimStart('/');
            }

            return Path.Combine(_uploadPath, filePath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Calculates SHA-256 hash of a file with optimized performance
        /// </summary>
        private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            const int bufferSize = 8192; // 8KB buffer for better performance
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read, bufferSize);
            using var sha256 = SHA256.Create();

            var buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, bufferSize, cancellationToken)) > 0)
            {
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }

        /// <summary>
        /// Calculates hash from stream for uploaded files
        /// </summary>
        private async Task<string> CalculateFileHashAsync(Stream stream, CancellationToken cancellationToken)
        {
            const int bufferSize = 8192;
            using var sha256 = SHA256.Create();

            var buffer = new byte[bufferSize];
            int bytesRead;
            var originalPosition = stream.Position;
            stream.Position = 0; // Reset to beginning

            while ((bytesRead = await stream.ReadAsync(buffer, 0, bufferSize, cancellationToken)) > 0)
            {
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            stream.Position = originalPosition; // Restore original position

            return Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }

        /// <summary>
        /// Performs file deduplication check and management
        /// </summary>
        private async Task<(bool IsDuplicate, Domain.Entities.File? ExistingFile)> CheckForDuplicateFileAsync(
            string fileHash,
            Guid currentUserId,
            CancellationToken cancellationToken)
        {
            try
            {
                var existingFile = await _fileRepository.GetByHashAsync(fileHash, cancellationToken);
                if (existingFile != null)
                {
                    // Increment reference count for deduplication
                    await _fileRepository.UpdateReferenceCountAsync(existingFile.Id, 1, cancellationToken);

                    _logger.LogInformation("Duplicate file detected, linking to existing file: {FileId} (hash: {FileHash})",
                        existingFile.Id, fileHash);

                    return (true, existingFile);
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicate file with hash: {FileHash}", fileHash);
                return (false, null);
            }
        }

        /// <summary>
        /// Uploads file with user and folder parameters (for backward compatibility)
        /// </summary>
        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, Guid userId, string folder, CancellationToken cancellationToken = default)
        {
            // Set user context for this operation
            _userContextService.SetUserContext(userId, $"User-{userId}", "User");

            try
            {
                var result = await UploadFileAsync(file, folder, cancellationToken);

                if (result.Success && result.Metadata != null)
                {
                    result.Metadata.AdditionalData["RequestedUserId"] = userId;
                    _logger.LogDebug("File uploaded for user {UserId} to folder {Folder}", userId, folder);
                }

                return result;
            }
            finally
            {
                _userContextService.ClearUserContext();
            }
        }

        /// <summary>
        /// Uploads an image file with validation
        /// </summary>
        public async Task<FileUploadResult> UploadImageAsync(IFormFile file, string subDirectory = "images", CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Error = "File is null or empty"
                };
            }

            // Create image-specific validation options
            var validationOptions = new FileValidationOptions
            {
                MaxFileSize = _configuration.GetValue<long>("FileStorage:MaxImageSize", 5 * 1024 * 1024), // 5MB for images
                AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" },
                AllowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/svg+xml" },
                RequireImageFormat = true
            };

            // Validate as image
            var validationResult = ValidateFile(file, validationOptions);
            if (!validationResult.IsValid)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Error = string.Join(", ", validationResult.Errors)
                };
            }

            return await UploadFileAsync(file, subDirectory, cancellationToken);
        }

        /// <summary>
        /// Gets files for a specific user (stub implementation)
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> GetUserFilesAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                var userFiles = await _fileRepository.GetByUserIdAsync(userId, page, pageSize, cancellationToken);

                var fileMetadataList = userFiles.Select(file => new FileMetadata
                {
                    OriginalName = file.OriginalFileName,
                    Extension = file.Extension,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    CreatedAt = file.CreatedAt,
                    Hash = file.FileHash,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["FileId"] = file.Id,
                        ["FilePath"] = file.FilePath,
                        ["Directory"] = file.Directory,
                        ["IsInUse"] = file.IsInUse,
                        ["ReferenceCount"] = file.ReferenceCount,
                        ["AccessCount"] = file.AccessCount,
                        ["LastAccessedAt"] = file.LastAccessedAt,
                        ["ImageWidth"] = file.ImageWidth,
                        ["ImageHeight"] = file.ImageHeight,
                        ["Description"] = file.Description,
                        ["Tags"] = file.Tags
                    }
                }).ToList();

                _logger.LogInformation("Retrieved {Count} files for user {UserId}", fileMetadataList.Count, userId);
                return fileMetadataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for user {UserId}", userId);
                return new List<FileMetadata>();
            }
        }

        /// <summary>
        /// Gets all files in the system from database
        /// </summary>
        public async Task<IEnumerable<FileMetadata>> GetAllFilesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                var files = await _fileRepository.GetAllAsync(cancellationToken);
                var pagedFiles = files
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                var fileMetadataList = pagedFiles.Select(file => new FileMetadata
                {
                    OriginalName = file.OriginalFileName,
                    Extension = file.Extension,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    CreatedAt = file.CreatedAt,
                    Hash = file.FileHash,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["FileId"] = file.Id,
                        ["FilePath"] = file.FilePath,
                        ["Directory"] = file.Directory,
                        ["IsInUse"] = file.IsInUse,
                        ["ReferenceCount"] = file.ReferenceCount,
                        ["AccessCount"] = file.AccessCount,
                        ["LastAccessedAt"] = file.LastAccessedAt,
                        ["ImageWidth"] = file.ImageWidth,
                        ["ImageHeight"] = file.ImageHeight,
                        ["Description"] = file.Description,
                        ["Tags"] = file.Tags,
                        ["UserId"] = file.UserId
                    }
                }).ToList();

                _logger.LogInformation("Retrieved {Count} files from database (page {Page}, size {PageSize})",
                    fileMetadataList.Count, page, pageSize);

                return fileMetadataList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all files from database");
                return new List<FileMetadata>();
            }
        }

        /// <summary>
        /// Gets file information by ID (with correct return type)
        /// </summary>
        public async Task<FileInfoDto?> GetFileInfoAsync(Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found with ID {FileId}", fileId);
                    return null;
                }

                // Record file access
                await _fileRepository.RecordFileAccessAsync(fileId, cancellationToken);

                return new FileInfoDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    StoredFileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Path = file.FilePath,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    UploadedBy = file.UserId,
                    UploadedAt = file.CreatedAt,
                    Folder = file.Directory,
                    Description = file.Description,
                    IsPublic = file.IsPublic
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file info for ID {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// Deletes a file by ID with user validation
        /// </summary>
        public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId, string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found with ID {FileId}", fileId);
                    return false;
                }

                // Validate user ownership
                if (file.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} is not authorized to delete file {FileId}", userId, fileId);
                    return false;
                }

                // Validate filename matches (additional security check)
                if (!string.IsNullOrEmpty(fileName) && file.OriginalFileName != fileName)
                {
                    _logger.LogWarning("Filename mismatch for file {FileId}. Expected: {ExpectedFileName}, Actual: {ActualFileName}",
                        fileId, fileName, file.OriginalFileName);
                    return false;
                }

                // Check reference count before deletion
                if (file.ReferenceCount > 1)
                {
                    // Decrement reference count instead of deleting
                    await _fileRepository.UpdateReferenceCountAsync(fileId, -1, cancellationToken);
                    _logger.LogInformation("Decremented reference count for file {FileId} (references: {ReferenceCount})",
                        fileId, file.ReferenceCount - 1);
                    return true;
                }

                // Delete physical file
                var fullPath = Path.Combine(_uploadPath, file.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Delete from database
                _fileRepository.Remove(file);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted file {FileId} ({FileName}) for user {UserId}",
                    fileId, file.OriginalFileName, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId} for user {UserId}", fileId, userId);
                return false;
            }
        }

        /// <summary>
        /// Gets storage statistics with comprehensive file categorization and time-based analysis
        /// </summary>
        public async Task<FileStorageStatsDto> GetStorageStatsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Get all files from database
                var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
                var activeFiles = allFiles.Where(f => !f.IsDeleted).ToList();

                // Time calculations
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
                var monthStart = new DateTime(now.Year, now.Month, 1);

                // Basic statistics
                var totalFiles = activeFiles.Count;
                var totalSize = activeFiles.Sum(f => f.FileSize);

                // File type categorization
                var imageFiles = activeFiles.Where(f => IsImageExtension(f.Extension)).ToList();
                var documentFiles = activeFiles.Where(f => IsDocumentExtension(f.Extension)).ToList();
                var otherFiles = activeFiles.Where(f => !IsImageExtension(f.Extension) && !IsDocumentExtension(f.Extension)).ToList();

                // Time-based statistics
                var filesToday = activeFiles.Count(f => f.CreatedAt >= todayStart);
                var filesThisWeek = activeFiles.Count(f => f.CreatedAt >= weekStart);
                var filesThisMonth = activeFiles.Count(f => f.CreatedAt >= monthStart);

                // File type distribution
                var fileTypeDistribution = activeFiles
                    .GroupBy(f => GetFileTypeCategory(f.Extension, f.ContentType))
                    .ToDictionary(g => g.Key, g => g.Count());

                // Monthly upload statistics (last 12 months)
                var monthlyUploads = new Dictionary<string, int>();
                for (int i = 0; i < 12; i++)
                {
                    var month = now.AddMonths(-i);
                    var monthKey = month.ToString("yyyy-MM");
                    var monthStart1 = new DateTime(month.Year, month.Month, 1);
                    var monthEnd = monthStart1.AddMonths(1);
                    var count = activeFiles.Count(f => f.CreatedAt >= monthStart1 && f.CreatedAt < monthEnd);
                    monthlyUploads[monthKey] = count;
                }

                // User storage statistics (top 10 users by storage)
                var storageByUser = activeFiles
                    .GroupBy(f => f.UserId)
                    .Select(g => new StorageByUserDto
                    {
                        UserId = g.Key,
                        UserName = $"User-{g.Key}", // TODO: Get actual username from UserService
                        FileCount = g.Count(),
                        StorageUsed = g.Sum(f => f.FileSize)
                    })
                    .OrderByDescending(u => u.StorageUsed)
                    .Take(10)
                    .ToDictionary(u => u.UserName, u => u);

                // File size statistics
                var fileSizes = activeFiles.Select(f => f.FileSize).Where(s => s > 0).ToList();
                var largestFileSize = fileSizes.Any() ? fileSizes.Max() : 0;
                var smallestFileSize = fileSizes.Any() ? fileSizes.Min() : 0;

                // Most common file type
                var mostCommonFileType = fileTypeDistribution
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault().Key ?? "Unknown";

                var result = new FileStorageStatsDto
                {
                    TotalFiles = totalFiles,
                    TotalStorageUsed = totalSize,

                    ImageFiles = imageFiles.Count,
                    ImageStorageUsed = imageFiles.Sum(f => f.FileSize),

                    DocumentFiles = documentFiles.Count,
                    DocumentStorageUsed = documentFiles.Sum(f => f.FileSize),

                    OtherFiles = otherFiles.Count,
                    OtherStorageUsed = otherFiles.Sum(f => f.FileSize),

                    FilesToday = filesToday,
                    FilesThisWeek = filesThisWeek,
                    FilesThisMonth = filesThisMonth,

                    LargestFileSize = largestFileSize,
                    SmallestFileSize = smallestFileSize,
                    MostCommonFileType = mostCommonFileType,

                    FileTypeDistribution = fileTypeDistribution,
                    MonthlyUploads = monthlyUploads,
                    StorageByUser = storageByUser
                };

                _logger.LogInformation("Generated storage statistics: {TotalFiles} files, {TotalSize} bytes total",
                    totalFiles, totalSize);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving comprehensive storage statistics");
                return new FileStorageStatsDto();
            }
        }

        /// <summary>
        /// Gets file stream for download (with correct return type)
        /// </summary>
        public async Task<FileStreamResultDto?> GetFileStreamAsync(Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found with ID {FileId}", fileId);
                    return null;
                }

                var fullPath = Path.Combine(_uploadPath, file.FilePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogWarning("Physical file not found at path {FilePath} for file ID {FileId}", fullPath, fileId);
                    return null;
                }

                // Record file access
                await _fileRepository.RecordFileAccessAsync(fileId, cancellationToken);

                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                return new FileStreamResultDto
                {
                    Stream = fileStream,
                    ContentType = file.ContentType,
                    FileName = file.OriginalFileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file stream for ID {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// Checks if a file is an image based on extension
        /// </summary>
        private static bool IsImageFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return ImageExtensions.Contains(extension);
        }

        /// <summary>
        /// Checks if extension is an image type
        /// </summary>
        private static bool IsImageExtension(string extension)
        {
            return !string.IsNullOrEmpty(extension) && ImageExtensions.Contains(extension);
        }

        /// <summary>
        /// Checks if extension is a document type
        /// </summary>
        private static bool IsDocumentExtension(string extension)
        {
            return !string.IsNullOrEmpty(extension) && DocumentExtensions.Contains(extension);
        }

        /// <summary>
        /// Gets file type category for statistics
        /// </summary>
        private static string GetFileTypeCategory(string extension, string contentType)
        {
            if (string.IsNullOrEmpty(extension))
                return "Unknown";

            extension = extension.ToLowerInvariant();

            if (ImageExtensions.Contains(extension))
                return "Images";
            if (DocumentExtensions.Contains(extension))
                return "Documents";
            if (VideoExtensions.Contains(extension))
                return "Videos";
            if (AudioExtensions.Contains(extension))
                return "Audio";
            if (ArchiveExtensions.Contains(extension))
                return "Archives";

            // Fallback to content type analysis
            if (!string.IsNullOrEmpty(contentType))
            {
                var lowerContentType = contentType.ToLowerInvariant();
                if (lowerContentType.StartsWith("image/"))
                    return "Images";
                if (lowerContentType.StartsWith("video/"))
                    return "Videos";
                if (lowerContentType.StartsWith("audio/"))
                    return "Audio";
                if (lowerContentType.Contains("pdf") || lowerContentType.Contains("document"))
                    return "Documents";
            }

            return "Other";
        }

        /// <summary>
        /// Gets image dimensions asynchronously using ImageSharp
        /// </summary>
        private static async Task<(int Width, int Height)> GetImageDimensionsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                using var image = await Image.LoadAsync(fileStream, cancellationToken);

                return (image.Width, image.Height);
            }
            catch (Exception)
            {
                // If we can't read dimensions (not an image or corrupted), return default values
                return (0, 0);
            }
        }

        /// <summary>
        /// Gets image metadata including dimensions and other properties
        /// </summary>
        private static async Task<Dictionary<string, object>> GetImageMetadataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var metadata = new Dictionary<string, object>();

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, System.IO.FileShare.Read);
                using var image = await Image.LoadAsync(fileStream, cancellationToken);

                metadata["Width"] = image.Width;
                metadata["Height"] = image.Height;
                metadata["Format"] = image.Metadata.DecodedImageFormat?.Name ?? "Unknown";
                metadata["PixelType"] = image.PixelType.ToString();

                // Add EXIF data if available
                if (image.Metadata.ExifProfile != null)
                {
                    metadata["HasExif"] = true;
                    // Add commonly used EXIF properties
                    try
                    {
                        var exif = image.Metadata.ExifProfile;
                        if (exif.TryGetValue(ExifTag.Make, out var make))
                            metadata["CameraMake"] = make.Value;
                        if (exif.TryGetValue(ExifTag.Model, out var model))
                            metadata["CameraModel"] = model.Value;
                        if (exif.TryGetValue(ExifTag.DateTime, out var dateTime))
                            metadata["DateTaken"] = dateTime.Value;
                    }
                    catch
                    {
                        // Ignore EXIF parsing errors
                    }
                }

                return metadata;
            }
            catch (Exception)
            {
                // If we can't read metadata, return basic info
                metadata["Width"] = 0;
                metadata["Height"] = 0;
                metadata["Format"] = "Unknown";
                return metadata;
            }
        }

        #region Additional IFileService Interface Implementation

        /// <summary>
        /// Uploads a file using FileUploadDto
        /// </summary>
        public async Task<FileUploadResultDto> UploadFileAsync(FileUploadDto fileUploadDto)
        {
            try
            {
                if (fileUploadDto?.FileStream == null)
                {
                    return FileUploadResultDto.Error("File stream is null");
                }

                // Create a temporary IFormFile from the stream and metadata
                var formFile = new StreamFormFile(
                    fileUploadDto.FileStream,
                    fileUploadDto.FileName,
                    fileUploadDto.ContentType,
                    fileUploadDto.Size);

                // Use existing upload logic
                var result = await UploadFileAsync(formFile, fileUploadDto.Category ?? "general");

                if (result.Success && result.Metadata != null)
                {
                    var fileInfo = new FileInfoDto
                    {
                        Id = (Guid)result.Metadata.AdditionalData["FileId"],
                        FileName = result.Metadata.OriginalName,
                        StoredFileName = result.FileName,
                        ContentType = result.ContentType,
                        Size = result.FileSize,
                        Path = result.FilePath,
                        Url = result.FilePath,
                        UploadedBy = (Guid)result.Metadata.AdditionalData["UserId"],
                        UploadedAt = result.Metadata.CreatedAt,
                        Folder = fileUploadDto.Category,
                        Description = fileUploadDto.Description
                    };

                    return FileUploadResultDto.CreateSuccess(fileInfo);
                }

                return FileUploadResultDto.Error(result.Error ?? "Upload failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file from FileUploadDto");
                return FileUploadResultDto.Error("File upload failed");
            }
        }

        /// <summary>
        /// Uploads a file with specific folder and user context
        /// </summary>
        public async Task<FileUploadResultDto> UploadFileAsync(IFormFile file, string? folder, Guid currentUserId, CancellationToken cancellationToken)
        {
            try
            {
                // Set user context for this operation
                _userContextService.SetUserContext(currentUserId, $"User-{currentUserId}", "User");

                var result = await UploadFileAsync(file, folder ?? "general", cancellationToken);

                if (result.Success && result.Metadata != null)
                {
                    var fileInfo = new FileInfoDto
                    {
                        Id = (Guid)result.Metadata.AdditionalData["FileId"],
                        FileName = result.Metadata.OriginalName,
                        StoredFileName = result.FileName,
                        ContentType = result.ContentType,
                        Size = result.FileSize,
                        Path = result.FilePath,
                        Url = result.FilePath,
                        UploadedBy = currentUserId,
                        UploadedAt = result.Metadata.CreatedAt,
                        Folder = folder
                    };

                    return FileUploadResultDto.CreateSuccess(fileInfo);
                }

                return FileUploadResultDto.Error(result.Error ?? "Upload failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for user {UserId}", currentUserId);
                return FileUploadResultDto.Error("File upload failed");
            }
            finally
            {
                _userContextService.ClearUserContext();
            }
        }

        /// <summary>
        /// Uploads an image with post association
        /// </summary>
        public async Task<ImageUploadResultDto> UploadImageAsync(IFormFile image, Guid? postId, Guid currentUserId, CancellationToken cancellationToken)
        {
            try
            {
                // Set user context for this operation
                _userContextService.SetUserContext(currentUserId, $"User-{currentUserId}", "User");

                var result = await UploadImageAsync(image, "images", cancellationToken);

                if (result.Success && result.Metadata != null)
                {
                    var fileInfo = new FileInfoDto
                    {
                        Id = (Guid)result.Metadata.AdditionalData["FileId"],
                        FileName = result.Metadata.OriginalName,
                        StoredFileName = result.FileName,
                        ContentType = result.ContentType,
                        Size = result.FileSize,
                        Path = result.FilePath,
                        Url = result.FilePath,
                        UploadedBy = currentUserId,
                        UploadedAt = result.Metadata.CreatedAt,
                        Folder = "images"
                    };

                    var imageResult = ImageUploadResultDto.CreateSuccess(
                        fileInfo,
                        result.Metadata.AdditionalData.TryGetValue("ImageWidth", out var width) ? (int?)width : null,
                        result.Metadata.AdditionalData.TryGetValue("ImageHeight", out var height) ? (int?)height : null,
                        Path.GetExtension(image.FileName)?.TrimStart('.').ToUpperInvariant()
                    );

                    imageResult.PostId = postId;
                    return imageResult;
                }

                return ImageUploadResultDto.Error(result.Error ?? "Image upload failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for user {UserId}", currentUserId);
                return ImageUploadResultDto.Error("Image upload failed");
            }
            finally
            {
                _userContextService.ClearUserContext();
            }
        }

        /// <summary>
        /// Gets a file by ID and returns as FileDto
        /// </summary>
        public async Task<FileDto> GetFileAsync(Guid fileId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    throw new FileNotFoundException($"File with ID {fileId} not found");
                }

                return new FileDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    Description = file.Description,
                    Category = file.Directory,
                    CreatedAt = file.CreatedAt,
                    UserId = file.UserId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Gets files by user ID (string version)
        /// </summary>
        public async Task<IEnumerable<FileDto>> GetFilesByUserAsync(string userId)
        {
            try
            {
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                    return new List<FileDto>();
                }

                var files = await _fileRepository.GetAllByUserIdAsync(userGuid);

                return files.Select(file => new FileDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    Description = file.Description,
                    Category = file.Directory,
                    CreatedAt = file.CreatedAt,
                    UserId = file.UserId.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for user {UserId}", userId);
                return new List<FileDto>();
            }
        }

        /// <summary>
        /// Gets user files with pagination and filtering
        /// </summary>
        public async Task<PagedResultDto<FileInfoDto>> GetUserFilesAsync(Guid userId, int pageNumber, int pageSize, string? fileType, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Get total count for pagination
                var totalCount = await _fileRepository.GetUserFileCountAsync(userId, cancellationToken);

                if (totalCount == 0)
                {
                    _logger.LogDebug("No files found for user {UserId}", userId);
                    return new PagedResultDto<FileInfoDto>
                    {
                        Items = new List<FileInfoDto>(),
                        TotalItems = 0,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalPages = 0
                    };
                }

                // Get paginated files
                var files = await _fileRepository.GetByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);

                // Apply file type filter if specified
                if (!string.IsNullOrEmpty(fileType))
                {
                    files = files.Where(f => GetFileTypeCategory(f.Extension, f.ContentType)
                        .Equals(fileType, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var fileInfos = files.Select(file => new FileInfoDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    StoredFileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Path = file.FilePath,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    UploadedBy = file.UserId,
                    UploadedAt = file.CreatedAt,
                    Folder = file.Directory,
                    Description = file.Description,
                    IsPublic = file.IsPublic
                }).ToList();

                var result = new PagedResultDto<FileInfoDto>
                {
                    Items = fileInfos,
                    TotalItems = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {Count} files for user {UserId} (page {PageNumber}/{TotalPages}) in {ElapsedMs}ms",
                    fileInfos.Count, userId, pageNumber, result.TotalPages, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error retrieving user files for {UserId} after {ElapsedMs}ms",
                    userId, stopwatch.ElapsedMilliseconds);

                return new PagedResultDto<FileInfoDto>
                {
                    Items = new List<FileInfoDto>(),
                    TotalItems = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// Gets all files with pagination and filtering
        /// </summary>
        public async Task<PagedResultDto<FileInfoDto>> GetAllFilesAsync(int pageNumber, int pageSize, string? fileType, Guid? uploadedBy, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Get total count for pagination
                var totalCount = await _fileRepository.GetTotalCountAsync(cancellationToken);

                if (totalCount == 0)
                {
                    _logger.LogDebug("No files found in system");
                    return new PagedResultDto<FileInfoDto>
                    {
                        Items = new List<FileInfoDto>(),
                        TotalItems = 0,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalPages = 0
                    };
                }

                // Get files with basic pagination
                IReadOnlyList<Domain.Entities.File> files;

                if (uploadedBy.HasValue)
                {
                    files = await _fileRepository.GetByUserIdAsync(uploadedBy.Value, pageNumber, pageSize, cancellationToken);
                    totalCount = await _fileRepository.GetUserFileCountAsync(uploadedBy.Value, cancellationToken);
                }
                else
                {
                    // For all files, we need to implement database-level pagination
                    var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
                    var filteredFiles = allFiles.Where(f => !f.IsDeleted).ToList();

                    // Apply file type filter if specified
                    if (!string.IsNullOrEmpty(fileType))
                    {
                        filteredFiles = filteredFiles.Where(f => GetFileTypeCategory(f.Extension, f.ContentType)
                            .Equals(fileType, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    totalCount = filteredFiles.Count;
                    files = filteredFiles
                        .OrderByDescending(f => f.CreatedAt)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }

                var fileInfos = files.Select(file => new FileInfoDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    StoredFileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Path = file.FilePath,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    UploadedBy = file.UserId,
                    UploadedAt = file.CreatedAt,
                    Folder = file.Directory,
                    Description = file.Description,
                    IsPublic = file.IsPublic
                }).ToList();

                var result = new PagedResultDto<FileInfoDto>
                {
                    Items = fileInfos,
                    TotalItems = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                stopwatch.Stop();
                _logger.LogInformation("Retrieved {Count} files (page {PageNumber}/{TotalPages}, filter: {FileType}, user: {UserId}) in {ElapsedMs}ms",
                    fileInfos.Count, pageNumber, result.TotalPages, fileType ?? "none", uploadedBy?.ToString() ?? "all", stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error retrieving all files after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return new PagedResultDto<FileInfoDto>
                {
                    Items = new List<FileInfoDto>(),
                    TotalItems = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }
        }

        /// <summary>
        /// Deletes a file by ID
        /// </summary>
        public async Task<bool> DeleteFileAsync(Guid fileId, CancellationToken cancellationToken)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken);
                if (file == null)
                {
                    _logger.LogWarning("File not found with ID {FileId}", fileId);
                    return false;
                }

                // Delete physical file
                var fullPath = Path.Combine(_uploadPath, file.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // Delete from database
                _fileRepository.Remove(file);
                await _fileRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted file {FileId} ({FileName})", fileId, file.OriginalFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId}", fileId);
                return false;
            }
        }

        /// <summary>
        /// Deletes a file by ID with user validation
        /// </summary>
        public async Task<bool> DeleteFileAsync(Guid fileId, string userId)
        {
            try
            {
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                    return false;
                }

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    _logger.LogWarning("File not found with ID {FileId}", fileId);
                    return false;
                }

                // Check user ownership
                if (file.UserId != userGuid)
                {
                    _logger.LogWarning("User {UserId} is not authorized to delete file {FileId}", userId, fileId);
                    return false;
                }

                return await DeleteFileAsync(fileId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId} for user {UserId}", fileId, userId);
                return false;
            }
        }

        /// <summary>
        /// Updates file metadata
        /// </summary>
        public async Task<FileDto> UpdateFileMetadataAsync(Guid fileId, UpdateFileMetadataDto updateDto)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    throw new FileNotFoundException($"File with ID {fileId} not found");
                }

                // Update metadata
                if (updateDto.Description != null)
                {
                    file.Description = updateDto.Description;
                }

                if (updateDto.Category != null)
                {
                    file.Directory = updateDto.Category;
                }

                _fileRepository.Update(file);
                await _fileRepository.SaveChangesAsync();

                _logger.LogInformation("Updated metadata for file {FileId}", fileId);

                return new FileDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    Description = file.Description,
                    Category = file.Directory,
                    CreatedAt = file.CreatedAt,
                    UserId = file.UserId.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file metadata for {FileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Validates a file upload DTO
        /// </summary>
        public async Task<bool> ValidateFileAsync(FileUploadDto fileUploadDto)
        {
            try
            {
                if (fileUploadDto?.FileStream == null)
                {
                    return false;
                }

                // Create a temporary IFormFile for validation
                var formFile = new StreamFormFile(
                    fileUploadDto.FileStream,
                    fileUploadDto.FileName,
                    fileUploadDto.ContentType,
                    fileUploadDto.Size);

                var validationResult = ValidateFile(formFile);
                return validationResult.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file upload DTO");
                return false;
            }
        }

        /// <summary>
        /// Gets file URL by ID
        /// </summary>
        public async Task<string> GetFileUrlAsync(Guid fileId)
        {
            try
            {
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    throw new FileNotFoundException($"File with ID {fileId} not found");
                }

                return $"{_baseUrl.TrimEnd('/')}/{file.FilePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file URL for {FileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Gets user storage usage with quota management
        /// </summary>
        public async Task<long> GetUserStorageUsageAsync(string userId)
        {
            try
            {
                if (!Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                    return 0;
                }

                var storageUsed = await _fileRepository.GetUserStorageUsageAsync(userGuid);
                _logger.LogDebug("User {UserId} storage usage: {StorageUsed} bytes", userId, storageUsed);

                return storageUsed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage usage for user {UserId}", userId);
                return 0;
            }
        }

        /// <summary>
        /// Checks if user has exceeded storage quota
        /// </summary>
        public async Task<bool> CheckUserStorageQuotaAsync(Guid userId, long additionalSize = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUsage = await _fileRepository.GetUserStorageUsageAsync(userId, cancellationToken);
                var maxQuota = GetUserStorageQuota(userId);

                var totalUsage = currentUsage + additionalSize;
                var withinQuota = totalUsage <= maxQuota;

                if (!withinQuota)
                {
                    _logger.LogWarning("User {UserId} exceeds storage quota. Current: {CurrentUsage}, Additional: {AdditionalSize}, Max: {MaxQuota}",
                        userId, currentUsage, additionalSize, maxQuota);
                }

                return withinQuota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking storage quota for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Gets user storage quota from configuration
        /// </summary>
        private long GetUserStorageQuota(Guid userId)
        {
            // Default quota from configuration (e.g., 100MB per user)
            var defaultQuota = _configuration.GetValue<long>("FileStorage:DefaultUserQuota", 100 * 1024 * 1024);

            // TODO: In a real implementation, you might have different quotas for different user roles
            // or custom quotas stored in a database table
            return defaultQuota;
        }

        /// <summary>
        /// Gets user storage quota info
        /// </summary>
        public async Task<UserStorageQuotaDto> GetUserStorageQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var currentUsage = await _fileRepository.GetUserStorageUsageAsync(userId, cancellationToken);
                var maxQuota = GetUserStorageQuota(userId);
                var fileCount = await _fileRepository.GetUserFileCountAsync(userId, cancellationToken);

                return new UserStorageQuotaDto
                {
                    UserId = userId,
                    CurrentUsage = currentUsage,
                    MaxQuota = maxQuota,
                    FileCount = fileCount,
                    AvailableSpace = Math.Max(0, maxQuota - currentUsage),
                    UsagePercentage = maxQuota > 0 ? (double)currentUsage / maxQuota * 100 : 0,
                    IsQuotaExceeded = currentUsage > maxQuota
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage quota info for user {UserId}", userId);
                return new UserStorageQuotaDto
                {
                    UserId = userId,
                    CurrentUsage = 0,
                    MaxQuota = GetUserStorageQuota(userId),
                    FileCount = 0,
                    AvailableSpace = GetUserStorageQuota(userId),
                    UsagePercentage = 0,
                    IsQuotaExceeded = false
                };
            }
        }

        /// <summary>
        /// Gets files by type
        /// </summary>
        public async Task<IEnumerable<FileDto>> GetFilesByTypeAsync(string fileType)
        {
            try
            {
                var files = await _fileRepository.GetByContentTypeAsync(fileType);

                return files.Select(file => new FileDto
                {
                    Id = file.Id,
                    FileName = file.OriginalFileName,
                    ContentType = file.ContentType,
                    Size = file.FileSize,
                    Url = $"{_baseUrl.TrimEnd('/')}/{file.FilePath}",
                    Description = file.Description,
                    Category = file.Directory,
                    CreatedAt = file.CreatedAt,
                    UserId = file.UserId.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files by type {FileType}", fileType);
                return new List<FileDto>();
            }
        }

        /// <summary>
        /// Cleans up orphaned files
        /// </summary>
        public async Task<bool> CleanupOrphanedFilesAsync()
        {
            try
            {
                var deletedCount = await CleanupOrphanedFilesAsync(30);
                _logger.LogInformation("Cleanup completed. {DeletedCount} orphaned files removed", deletedCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned file cleanup");
                return false;
            }
        }

        #endregion

        /// <summary>
        /// Helper class to create IFormFile from stream
        /// </summary>
        private class StreamFormFile : IFormFile
        {
            private readonly Stream _stream;

            public StreamFormFile(Stream stream, string fileName, string contentType, long length)
            {
                _stream = stream;
                FileName = fileName;
                ContentType = contentType;
                Length = length;
            }

            public string ContentType { get; }
            public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length { get; }
            public string Name => "file";
            public string FileName { get; }

            public void CopyTo(Stream target) => _stream.CopyTo(target);
            public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => _stream.CopyToAsync(target, cancellationToken);
            public Stream OpenReadStream() => _stream;
        }
    }
}