using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.File;
using MapleBlog.Application.Interfaces;
using System.Security.Claims;

namespace MapleBlog.API.Controllers;

/// <summary>
/// File management API controller for handling uploads and media
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FileController> _logger;

    public FileController(IFileService fileService, ILogger<FileController> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Upload a single file
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="folder">Target folder (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file information</returns>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(10_000_000)] // 10MB limit
    [ProducesResponseType(typeof(FileUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<FileUploadResultDto>> UploadFile(
        IFormFile file,
        [FromForm] string? folder = null,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Validate file size
        if (file.Length > 10_000_000) // 10MB
        {
            return BadRequest("File size exceeds 10MB limit");
        }

        // Validate file type
        var allowedTypes = new[]
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp",
            "application/pdf", "text/plain", "text/markdown",
            "application/zip", "application/x-zip-compressed"
        };

        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest($"File type {file.ContentType} is not allowed");
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _fileService.UploadFileAsync(file, folder, currentUserId, cancellationToken);

            _logger.LogInformation("File uploaded successfully: {FileName} by user {UserId}",
                file.FileName, currentUserId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid file upload request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    /// <param name="files">Files to upload</param>
    /// <param name="folder">Target folder (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload results for all files</returns>
    [HttpPost("upload/multiple")]
    [Authorize]
    [RequestSizeLimit(50_000_000)] // 50MB total limit
    [ProducesResponseType(typeof(IEnumerable<FileUploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<IEnumerable<FileUploadResultDto>>> UploadMultipleFiles(
        IFormFileCollection files,
        [FromForm] string? folder = null,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        if (files.Count > 10)
        {
            return BadRequest("Cannot upload more than 10 files at once");
        }

        var totalSize = files.Sum(f => f.Length);
        if (totalSize > 50_000_000) // 50MB total
        {
            return BadRequest("Total file size exceeds 50MB limit");
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var results = new List<FileUploadResultDto>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var result = await _fileService.UploadFileAsync(file, folder, currentUserId, cancellationToken);
                    results.Add(result);
                }
            }

            _logger.LogInformation("Multiple files uploaded successfully: {FileCount} files by user {UserId}",
                results.Count, currentUserId);

            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid multiple file upload request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Upload an image specifically for use in articles
    /// </summary>
    /// <param name="image">Image file to upload</param>
    /// <param name="postId">Associated post ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with optimized image information</returns>
    [HttpPost("upload/image")]
    [Authorize(Roles = "Admin,Author")]
    [RequestSizeLimit(5_000_000)] // 5MB limit for images
    [ProducesResponseType(typeof(ImageUploadResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ImageUploadResultDto>> UploadImage(
        IFormFile image,
        [FromForm] Guid? postId = null,
        CancellationToken cancellationToken = default)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("No image provided");
        }

        // Validate image type
        var allowedImageTypes = new[]
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
        };

        if (!allowedImageTypes.Contains(image.ContentType.ToLowerInvariant()))
        {
            return BadRequest($"File type {image.ContentType} is not a supported image format");
        }

        if (image.Length > 5_000_000) // 5MB
        {
            return BadRequest("Image size exceeds 5MB limit");
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _fileService.UploadImageAsync(image, postId, currentUserId, cancellationToken);

            _logger.LogInformation("Image uploaded successfully: {FileName} by user {UserId}",
                image.FileName, currentUserId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid image upload request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image {FileName}", image.FileName);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get file information by ID
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File information</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FileInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileInfoDto>> GetFileInfo(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileInfo = await _fileService.GetFileInfoAsync(id, cancellationToken);

            if (fileInfo == null)
                return NotFound();

            return Ok(fileInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info for {FileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get files uploaded by the current user
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="fileType">Filter by file type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of user's files</returns>
    [HttpGet("my-files")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResultDto<FileInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResultDto<FileInfoDto>>> GetMyFiles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? fileType = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _fileService.GetUserFilesAsync(
                currentUserId, pageNumber, pageSize, fileType, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Check if file exists and user has permission
            var fileInfo = await _fileService.GetFileInfoAsync(id, cancellationToken);
            if (fileInfo == null)
                return NotFound();

            if (!isAdmin && fileInfo.UploadedBy != currentUserId)
                return Forbid();

            var deleted = await _fileService.DeleteFileAsync(id, cancellationToken);

            if (!deleted)
                return NotFound();

            _logger.LogInformation("File deleted: {FileId} by user {UserId}", id, currentUserId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all files (admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="fileType">Filter by file type</param>
    /// <param name="uploadedBy">Filter by uploader user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of all files</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResultDto<FileInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<FileInfoDto>>> GetAllFiles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? fileType = null,
        [FromQuery] Guid? uploadedBy = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Invalid pagination parameters");
        }

        try
        {
            var result = await _fileService.GetAllFilesAsync(
                pageNumber, pageSize, fileType, uploadedBy, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all files");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get file storage statistics (admin only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Storage statistics</returns>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FileStorageStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FileStorageStatsDto>> GetStorageStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _fileService.GetStorageStatsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage stats");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Download a file
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileStream = await _fileService.GetFileStreamAsync(id, cancellationToken);

            if (fileStream == null)
                return NotFound();

            return File(fileStream.Stream, fileStream.ContentType, fileStream.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}