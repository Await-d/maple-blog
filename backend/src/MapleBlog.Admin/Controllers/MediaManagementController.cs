using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.File;
using MapleBlog.Application.DTOs.Image;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using System.Security.Cryptography;
using System.Text.Json;
using System.Linq.Expressions;
using MapleBlog.Admin.DTOs;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// 媒体管理控制器 - 提供全面的媒体文件管理功能
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    public class MediaManagementController : BaseAdminController
    {
        private readonly Application.Interfaces.IFileService _fileService;
        private readonly Application.Interfaces.IImageProcessingService _imageProcessingService;
        private readonly IFileRepository _fileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IStorageQuotaService _storageQuotaService;
        private readonly IFileAccessControlService _fileAccessControlService;
        private readonly IMemoryCache _memoryCache;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public MediaManagementController(
            ILogger<MediaManagementController> logger,
            IPermissionService permissionService,
            IAuditLogService auditLogService,
            Application.Interfaces.IFileService fileService,
            Application.Interfaces.IImageProcessingService imageProcessingService,
            IFileRepository fileRepository,
            IUserRepository userRepository,
            IStorageQuotaService storageQuotaService,
            IFileAccessControlService fileAccessControlService,
            IMemoryCache memoryCache,
            IMapper mapper,
            IConfiguration configuration)
            : base(logger, permissionService, auditLogService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _storageQuotaService = storageQuotaService ?? throw new ArgumentNullException(nameof(storageQuotaService));
            _fileAccessControlService = fileAccessControlService ?? throw new ArgumentNullException(nameof(fileAccessControlService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region 媒体库概览和统计

        /// <summary>
        /// 获取媒体库概览
        /// </summary>
        /// <returns>媒体库概览数据</returns>
        [HttpGet("overview")]
        public async Task<IActionResult> GetMediaOverview()
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var cacheKey = "media_overview";
                if (_memoryCache.TryGetValue(cacheKey, out MediaLibraryOverviewDto? cachedOverview))
                {
                    return Success(cachedOverview);
                }

                var overview = await GetMediaOverviewDataAsync();
                
                _memoryCache.Set(cacheKey, overview, TimeSpan.FromMinutes(5));

                await LogAuditAsync("VIEW", "MediaOverview", description: "查看媒体库概览");

                return Success(overview);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取媒体库概览");
            }
        }

        /// <summary>
        /// 获取存储统计信息
        /// </summary>
        /// <returns>存储统计数据</returns>
        [HttpGet("storage-statistics")]
        public async Task<IActionResult> GetStorageStatistics()
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var cacheKey = "storage_statistics";
                if (_memoryCache.TryGetValue(cacheKey, out StorageStatisticsDto? cachedStats))
                {
                    return Success(cachedStats);
                }

                var statistics = await GetStorageStatisticsDataAsync();
                
                _memoryCache.Set(cacheKey, statistics, TimeSpan.FromMinutes(10));

                await LogAuditAsync("VIEW", "StorageStatistics", description: "查看存储统计");

                return Success(statistics);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取存储统计");
            }
        }

        #endregion

        #region 文件搜索和筛选

        /// <summary>
        /// 搜索媒体文件
        /// </summary>
        /// <param name="request">搜索请求</param>
        /// <returns>搜索结果</returns>
        [HttpPost("search")]
        public async Task<IActionResult> SearchMedia([FromBody] MediaSearchRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var searchResults = await SearchMediaFilesAsync(request);

                await LogAuditAsync("SEARCH", "Media", description: $"搜索媒体: {request.SearchTerm}");

                return SuccessWithPagination(
                    searchResults.Items,
                    searchResults.TotalCount,
                    request.PageNumber,
                    request.PageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "媒体搜索");
            }
        }

        /// <summary>
        /// 获取媒体文件列表
        /// </summary>
        /// <param name="directory">目录筛选</param>
        /// <param name="contentType">内容类型筛选</param>
        /// <param name="userId">用户筛选</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="minSize">最小文件大小</param>
        /// <param name="maxSize">最大文件大小</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="sortBy">排序字段</param>
        /// <param name="sortDirection">排序方向</param>
        /// <returns>媒体文件列表</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetMediaList(
            [FromQuery] string? directory = null,
            [FromQuery] string? contentType = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] long? minSize = null,
            [FromQuery] long? maxSize = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var searchRequest = new MediaSearchRequestDto
                {
                    Directory = directory,
                    ContentTypes = string.IsNullOrEmpty(contentType) ? new List<string>() : new[] { contentType },
                    UserIds = userId.HasValue ? new[] { userId.Value } : new List<Guid>(),
                    UploadDateRange = startDate.HasValue && endDate.HasValue
                        ? new DateRangeDto { StartDate = startDate.Value, EndDate = endDate.Value }
                        : null,
                    MinFileSize = minSize,
                    MaxFileSize = maxSize,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                };

                var result = await SearchMediaFilesAsync(searchRequest);

                return SuccessWithPagination(
                    result.Items,
                    result.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取媒体列表");
            }
        }

        #endregion

        #region 文件上传和处理

        /// <summary>
        /// 上传单个文件
        /// </summary>
        /// <param name="file">文件</param>
        /// <param name="directory">目录</param>
        /// <param name="description">描述</param>
        /// <param name="tags">标签</param>
        /// <param name="accessLevel">访问级别</param>
        /// <returns>上传结果</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            IFormFile file,
            [FromForm] string directory = "general",
            [FromForm] string? description = null,
            [FromForm] string? tags = null,
            [FromForm] FileAccessLevel accessLevel = FileAccessLevel.Public)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.upload");
                if (permissionCheck != null) return permissionCheck;

                if (file == null || file.Length == 0)
                {
                    return Error("请选择要上传的文件");
                }

                // 检查用户存储配额
                var quotaCheck = await _storageQuotaService.CheckUploadPermissionAsync(
                    CurrentUserId!.Value, file.Length);
                if (!quotaCheck.CanUpload)
                {
                    return Error($"存储配额不足: {quotaCheck.Message}");
                }

                var uploadDto = new FileUploadDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    FileStream = file.OpenReadStream(),
                    Directory = directory,
                    Description = description,
                    Tags = tags,
                    AccessLevel = accessLevel
                };

                var result = await _fileService.UploadFileAsync(uploadDto);
                if (!result.Success)
                {
                    return Error(result.ErrorMessage ?? "文件上传失败");
                }

                await LogAuditAsync("UPLOAD", "Media", result.FileId?.ToString(),
                    $"上传文件: {file.FileName} ({file.Length} bytes)");

                return Success(result, "文件上传成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "上传文件");
            }
        }

        /// <summary>
        /// 批量上传文件
        /// </summary>
        /// <param name="files">文件列表</param>
        /// <param name="directory">目录</param>
        /// <param name="accessLevel">访问级别</param>
        /// <returns>批量上传结果</returns>
        [HttpPost("batch-upload")]
        public async Task<IActionResult> BatchUploadFiles(
            IFormFileCollection files,
            [FromForm] string directory = "general",
            [FromForm] FileAccessLevel accessLevel = FileAccessLevel.Public)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.upload");
                if (permissionCheck != null) return permissionCheck;

                if (files == null || files.Count == 0)
                {
                    return Error("请选择要上传的文件");
                }

                var totalSize = files.Sum(f => f.Length);
                var quotaCheck = await _storageQuotaService.CheckUploadPermissionAsync(
                    CurrentUserId!.Value, totalSize);
                if (!quotaCheck.CanUpload)
                {
                    return Error($"存储配额不足: {quotaCheck.Message}");
                }

                var results = new List<FileUploadResultDto>();
                var successCount = 0;
                var failCount = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var uploadDto = new FileUploadDto
                        {
                            FileName = file.FileName,
                            ContentType = file.ContentType,
                            Size = file.Length,
                            FileStream = file.OpenReadStream(),
                            Directory = directory,
                            AccessLevel = accessLevel
                        };

                        var result = await _fileService.UploadFileAsync(uploadDto);
                        results.Add(result);

                        if (result.Success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "批量上传文件时发生错误: {FileName}", file.FileName);
                        results.Add(FileUploadResultDto.Error($"上传失败: {ex.Message}"));
                        failCount++;
                    }
                }

                await LogAuditAsync("BATCH_UPLOAD", "Media", null,
                    $"批量上传文件: {files.Count} 个文件，成功 {successCount} 个，失败 {failCount} 个");

                return Success(new
                {
                    TotalFiles = files.Count,
                    SuccessCount = successCount,
                    FailCount = failCount,
                    Results = results
                }, $"批量上传完成: 成功 {successCount} 个，失败 {failCount} 个");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量上传文件");
            }
        }

        #endregion

        #region 图片处理

        /// <summary>
        /// 调整图片大小
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="request">调整大小请求</param>
        /// <returns>处理结果</returns>
        [HttpPost("{fileId:guid}/resize")]
        public async Task<IActionResult> ResizeImage(Guid fileId, [FromBody] ImageResizeRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFoundResult("文件", fileId);
                }

                if (!file.ContentType.StartsWith("image/"))
                {
                    return Error("只能调整图片文件的大小");
                }

                var result = await _imageProcessingService.ResizeImageAsync(
                    file.FilePath, request.Width, request.Height, request.Quality);

                await LogAuditAsync("RESIZE", "Media", fileId.ToString(),
                    $"调整图片大小: {request.Width}x{request.Height}");

                return Success(result, "图片大小调整成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "调整图片大小");
            }
        }

        /// <summary>
        /// 生成图片缩略图
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="request">缩略图请求</param>
        /// <returns>缩略图结果</returns>
        [HttpPost("{fileId:guid}/thumbnail")]
        public async Task<IActionResult> GenerateThumbnail(Guid fileId, [FromBody] ThumbnailGenerationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFoundResult("文件", fileId);
                }

                if (!file.ContentType.StartsWith("image/"))
                {
                    return Error("只能为图片文件生成缩略图");
                }

                var result = await _imageProcessingService.GenerateThumbnailAsync(
                    file.FilePath, request.Size, request.Size, request.Quality);

                await LogAuditAsync("THUMBNAIL", "Media", fileId.ToString(),
                    $"生成缩略图: {request.Size}x{request.Size}");

                return Success(result, "缩略图生成成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "生成缩略图");
            }
        }

        /// <summary>
        /// 图片格式转换
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="request">格式转换请求</param>
        /// <returns>转换结果</returns>
        [HttpPost("{fileId:guid}/convert")]
        public async Task<IActionResult> ConvertImageFormat(Guid fileId, [FromBody] ImageConversionRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFoundResult("文件", fileId);
                }

                if (!file.ContentType.StartsWith("image/"))
                {
                    return Error("只能转换图片文件格式");
                }

                var result = await _imageProcessingService.ConvertImageFormatAsync(
                    file.FilePath, request.TargetFormat, request.Quality);

                await LogAuditAsync("CONVERT", "Media", fileId.ToString(),
                    $"转换图片格式: {file.Extension} -> {request.TargetFormat}");

                return Success(result, "图片格式转换成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "转换图片格式");
            }
        }

        /// <summary>
        /// 批量图片处理
        /// </summary>
        /// <param name="request">批量处理请求</param>
        /// <returns>批量处理结果</returns>
        [HttpPost("batch-image-processing")]
        public async Task<IActionResult> BatchProcessImages([FromBody] BatchImageProcessingRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await ProcessImagesBatchAsync(request);

                await LogAuditAsync("BATCH_IMAGE_PROCESS", "Media", null,
                    $"批量图片处理: {request.FileIds.Count()} 个文件");

                return Success(result, $"批量图片处理完成: 成功 {result.SuccessCount} 个，失败 {result.FailCount} 个");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量图片处理");
            }
        }

        #endregion

        #region 文件管理

        /// <summary>
        /// 获取文件详情
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文件详情</returns>
        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> GetFileDetails(Guid fileId)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFoundResult("文件", fileId);
                }

                var fileDetails = await GetFileDetailsDataAsync(file);

                await LogAuditAsync("VIEW", "Media", fileId.ToString(), "查看文件详情");

                return Success(fileDetails);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取文件详情");
            }
        }

        /// <summary>
        /// 更新文件信息
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="request">更新请求</param>
        /// <returns>更新结果</returns>
        [HttpPut("{fileId:guid}")]
        public async Task<IActionResult> UpdateFileInfo(Guid fileId, [FromBody] UpdateFileInfoRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFoundResult("文件", fileId);
                }

                // 检查权限
                if (file.UserId != CurrentUserId && !await HasPermissionAsync("media.edit.all"))
                {
                    return Forbid("只能编辑自己上传的文件");
                }

                var oldFile = _mapper.Map<File>(file);

                // 更新文件信息
                file.Description = request.Description;
                file.Tags = request.Tags;
                file.AccessLevel = request.AccessLevel;
                file.UpdatedAt = DateTime.UtcNow;

                await _fileRepository.UpdateAsync(file);

                await LogAuditAsync("UPDATE", "Media", fileId.ToString(),
                    "更新文件信息", oldFile, file);

                return Success(_mapper.Map<FileInfoDto>(file), "文件信息更新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新文件信息");
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="permanent">是否永久删除</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(Guid fileId, [FromQuery] bool permanent = false)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync(permanent ? "media.delete.permanent" : "media.delete");
                if (permissionCheck != null) return permissionCheck;

                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return NotFoundResult("文件", fileId);
                }

                // 检查权限
                if (file.UserId != CurrentUserId && !await HasPermissionAsync("media.delete.all"))
                {
                    return Forbid("只能删除自己上传的文件");
                }

                // 检查文件是否被引用
                if (file.IsInUse && file.ReferenceCount > 0)
                {
                    return Error($"文件正在被 {file.ReferenceCount} 个地方引用，无法删除");
                }

                var result = await DeleteFileAsync(file, permanent);

                await LogAuditAsync("DELETE", "Media", fileId.ToString(),
                    $"{(permanent ? "永久" : "软")}删除文件: {file.OriginalFileName}");

                return Success(result, $"文件{(permanent ? "永久" : "")}删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除文件");
            }
        }

        /// <summary>
        /// 批量删除文件
        /// </summary>
        /// <param name="request">批量删除请求</param>
        /// <returns>批量删除结果</returns>
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDeleteFiles([FromBody] BatchFileOperationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.delete");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await BatchDeleteFilesAsync(request.FileIds, request.Permanent);

                await LogAuditAsync("BATCH_DELETE", "Media", null,
                    $"批量删除文件: {request.FileIds.Count()} 个文件");

                return Success(result, $"批量删除完成: 成功 {result.SuccessCount} 个，失败 {result.FailCount} 个");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量删除文件");
            }
        }

        /// <summary>
        /// 移动文件到不同目录
        /// </summary>
        /// <param name="request">移动文件请求</param>
        /// <returns>移动结果</returns>
        [HttpPost("move")]
        public async Task<IActionResult> MoveFiles([FromBody] MoveFilesRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await MoveFilesAsync(request.FileIds, request.TargetDirectory);

                await LogAuditAsync("MOVE", "Media", null,
                    $"移动文件: {request.FileIds.Count()} 个文件 -> {request.TargetDirectory}");

                return Success(result, $"文件移动完成: 成功 {result.SuccessCount} 个，失败 {result.FailCount} 个");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "移动文件");
            }
        }

        #endregion

        #region 重复文件管理

        /// <summary>
        /// 检测重复文件
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>重复文件检测结果</returns>
        [HttpGet("duplicate-detection")]
        public async Task<IActionResult> DetectDuplicateFiles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var duplicates = await DetectDuplicateFilesAsync(pageNumber, pageSize);

                return SuccessWithPagination(
                    duplicates.Items,
                    duplicates.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "重复文件检测");
            }
        }

        /// <summary>
        /// 合并重复文件
        /// </summary>
        /// <param name="request">合并重复文件请求</param>
        /// <returns>合并结果</returns>
        [HttpPost("merge-duplicates")]
        public async Task<IActionResult> MergeDuplicateFiles([FromBody] MergeDuplicateFilesRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.edit");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await MergeDuplicateFilesAsync(request);

                await LogAuditAsync("MERGE_DUPLICATES", "Media", null,
                    $"合并重复文件: {request.DuplicateGroups.Count()} 组");

                return Success(result, $"重复文件合并完成: 成功 {result.MergedCount} 组");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "合并重复文件");
            }
        }

        #endregion

        #region 存储管理

        /// <summary>
        /// 清理未使用的文件
        /// </summary>
        /// <param name="daysOld">清理多少天前的文件</param>
        /// <param name="dryRun">是否为试运行</param>
        /// <returns>清理结果</returns>
        [HttpPost("cleanup-unused")]
        public async Task<IActionResult> CleanupUnusedFiles(
            [FromQuery] int daysOld = 30,
            [FromQuery] bool dryRun = true)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.cleanup");
                if (permissionCheck != null) return permissionCheck;

                var result = await CleanupUnusedFilesAsync(daysOld, dryRun);

                await LogAuditAsync("CLEANUP", "Media", null,
                    $"清理未使用文件: {daysOld} 天前的文件，{(dryRun ? "试运行" : "实际删除")}");

                return Success(result, dryRun ? "清理预览完成" : "清理完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "清理未使用文件");
            }
        }

        /// <summary>
        /// 获取存储使用分析
        /// </summary>
        /// <returns>存储使用分析</returns>
        [HttpGet("storage-analysis")]
        public async Task<IActionResult> GetStorageAnalysis()
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("media.read");
                if (permissionCheck != null) return permissionCheck;

                var cacheKey = "storage_analysis";
                if (_memoryCache.TryGetValue(cacheKey, out StorageAnalysisDto? cachedAnalysis))
                {
                    return Success(cachedAnalysis);
                }

                var analysis = await GetStorageAnalysisDataAsync();
                
                _memoryCache.Set(cacheKey, analysis, TimeSpan.FromMinutes(15));

                return Success(analysis);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取存储使用分析");
            }
        }

        #endregion

        #region 私有辅助方法

        private async Task<MediaLibraryOverviewDto> GetMediaOverviewDataAsync()
        {
            // 实现媒体库概览数据获取逻辑
            var totalFiles = await _fileRepository.CountAsync();
            var totalSize = await _fileRepository.GetTotalFileSizeAsync();
            var todayUploads = await _fileRepository.CountTodayUploadsAsync();
            var imageCount = await _fileRepository.CountByContentTypeAsync("image");
            var documentCount = await _fileRepository.CountByContentTypeAsync("document");
            var videoCount = await _fileRepository.CountByContentTypeAsync("video");

            return new MediaLibraryOverviewDto
            {
                TotalFiles = totalFiles,
                TotalSize = totalSize,
                TodayUploads = todayUploads,
                ImageCount = imageCount,
                DocumentCount = documentCount,
                VideoCount = videoCount,
                AvailableSpace = await GetAvailableSpaceAsync(),
                RecentUploads = await GetRecentUploadsAsync(10)
            };
        }

        private async Task<StorageStatisticsDto> GetStorageStatisticsDataAsync()
        {
            // 实现存储统计数据获取逻辑
            var statistics = new StorageStatisticsDto
            {
                TotalSize = await _fileRepository.GetTotalFileSizeAsync(),
                UsedSpace = await _fileRepository.GetUsedSpaceAsync(),
                AvailableSpace = await GetAvailableSpaceAsync(),
                FileTypeDistribution = await GetFileTypeDistributionAsync(),
                DirectoryDistribution = await GetDirectoryDistributionAsync(),
                UserStorageUsage = await GetTopUserStorageUsageAsync(10),
                MonthlyUploadTrends = await GetMonthlyUploadTrendsAsync(12)
            };

            return statistics;
        }

        private async Task<PagedResultDto<MediaFileDto>> SearchMediaFilesAsync(MediaSearchRequestDto request)
        {
            // 实现媒体文件搜索逻辑
            var query = _fileRepository.GetQueryable();

            // 应用搜索条件
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(f => f.OriginalFileName.Contains(request.SearchTerm) ||
                                        f.Description!.Contains(request.SearchTerm) ||
                                        f.Tags!.Contains(request.SearchTerm));
            }

            if (!string.IsNullOrEmpty(request.Directory))
            {
                query = query.Where(f => f.Directory == request.Directory);
            }

            if (request.ContentTypes?.Any() == true)
            {
                query = query.Where(f => request.ContentTypes.Contains(f.ContentType));
            }

            if (request.UserIds?.Any() == true)
            {
                query = query.Where(f => request.UserIds.Contains(f.UserId));
            }

            if (request.UploadDateRange != null)
            {
                query = query.Where(f => f.CreatedAt >= request.UploadDateRange.StartDate &&
                                        f.CreatedAt <= request.UploadDateRange.EndDate);
            }

            if (request.MinFileSize.HasValue)
            {
                query = query.Where(f => f.FileSize >= request.MinFileSize);
            }

            if (request.MaxFileSize.HasValue)
            {
                query = query.Where(f => f.FileSize <= request.MaxFileSize);
            }

            // 应用排序
            query = request.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(GetSortExpression(request.SortBy))
                : query.OrderBy(GetSortExpression(request.SortBy));

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var mediaFiles = items.Select(file => new MediaFileDto
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                Directory = file.Directory,
                Description = file.Description,
                Tags = file.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(),
                AccessLevel = file.AccessLevel.ToString(),
                ImageWidth = file.ImageWidth,
                ImageHeight = file.ImageHeight,
                UploadedAt = file.CreatedAt,
                IsInUse = file.IsInUse,
                ReferenceCount = file.ReferenceCount,
                AccessCount = file.AccessCount,
                LastAccessedAt = file.LastAccessedAt,
                UploaderName = file.User?.Username ?? "Unknown"
            }).ToList();

            return new PagedResultDto<MediaFileDto>
            {
                Items = mediaFiles,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        private static Expression<Func<MapleBlog.Domain.Entities.File, object>> GetSortExpression(string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "filename" => f => f.OriginalFileName,
                "size" => f => f.FileSize,
                "contenttype" => f => f.ContentType,
                "directory" => f => f.Directory,
                "accesscount" => f => f.AccessCount,
                "lastaccessedat" => f => f.LastAccessedAt ?? DateTime.MinValue,
                _ => f => f.CreatedAt
            };
        }

        // 其他私有辅助方法的实现...
        // 由于篇幅限制，这里省略具体实现细节

        #endregion
    }

    #region DTO类定义

    /// <summary>
    /// 媒体库概览DTO
    /// </summary>
    public class MediaLibraryOverviewDto
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int TodayUploads { get; set; }
        public int ImageCount { get; set; }
        public int DocumentCount { get; set; }
        public int VideoCount { get; set; }
        public long AvailableSpace { get; set; }
        public IEnumerable<RecentUploadDto> RecentUploads { get; set; } = new List<RecentUploadDto>();
    }

    /// <summary>
    /// 存储统计DTO
    /// </summary>
    public class StorageStatisticsDto
    {
        public long TotalSize { get; set; }
        public long UsedSpace { get; set; }
        public long AvailableSpace { get; set; }
        public IEnumerable<FileTypeDistributionDto> FileTypeDistribution { get; set; } = new List<FileTypeDistributionDto>();
        public IEnumerable<DirectoryDistributionDto> DirectoryDistribution { get; set; } = new List<DirectoryDistributionDto>();
        public IEnumerable<UserStorageUsageDto> UserStorageUsage { get; set; } = new List<UserStorageUsageDto>();
        public IEnumerable<MonthlyUploadTrendDto> MonthlyUploadTrends { get; set; } = new List<MonthlyUploadTrendDto>();
    }

    /// <summary>
    /// 媒体搜索请求DTO
    /// </summary>
    public class MediaSearchRequestDto
    {
        public string? SearchTerm { get; set; }
        public string? Directory { get; set; }
        public IEnumerable<string>? ContentTypes { get; set; }
        public IEnumerable<Guid>? UserIds { get; set; }
        public Application.DTOs.Admin.DateRangeDto? UploadDateRange { get; set; }
        public long? MinFileSize { get; set; }
        public long? MaxFileSize { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
    }

    /// <summary>
    /// 图片调整大小请求DTO
    /// </summary>
    public class ImageResizeRequestDto
    {
        [Range(1, 10000)]
        public int Width { get; set; }

        [Range(1, 10000)]
        public int Height { get; set; }

        [Range(1, 100)]
        public int Quality { get; set; } = 85;
    }

    /// <summary>
    /// 缩略图生成请求DTO
    /// </summary>
    public class ThumbnailGenerationRequestDto
    {
        [Range(50, 1000)]
        public int Size { get; set; } = 150;

        [Range(1, 100)]
        public int Quality { get; set; } = 80;
    }

    /// <summary>
    /// 图片转换请求DTO
    /// </summary>
    public class ImageConversionRequestDto
    {
        [Required]
        public string TargetFormat { get; set; } = string.Empty; // jpg, png, webp

        [Range(1, 100)]
        public int Quality { get; set; } = 85;
    }

    /// <summary>
    /// 批量图片处理请求DTO
    /// </summary>
    public class BatchImageProcessingRequestDto
    {
        [Required]
        public IEnumerable<Guid> FileIds { get; set; } = new List<Guid>();

        [Required]
        public string Operation { get; set; } = string.Empty; // resize, thumbnail, convert

        public ImageResizeRequestDto? ResizeOptions { get; set; }
        public ThumbnailGenerationRequestDto? ThumbnailOptions { get; set; }
        public ImageConversionRequestDto? ConversionOptions { get; set; }
    }

    /// <summary>
    /// 更新文件信息请求DTO
    /// </summary>
    public class UpdateFileInfoRequestDto
    {
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public FileAccessLevel AccessLevel { get; set; }
    }

    /// <summary>
    /// 批量文件操作请求DTO
    /// </summary>
    public class BatchFileOperationRequestDto
    {
        [Required]
        public IEnumerable<Guid> FileIds { get; set; } = new List<Guid>();

        public bool Permanent { get; set; } = false;
    }

    /// <summary>
    /// 移动文件请求DTO
    /// </summary>
    public class MoveFilesRequestDto
    {
        [Required]
        public IEnumerable<Guid> FileIds { get; set; } = new List<Guid>();

        [Required]
        public string TargetDirectory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 合并重复文件请求DTO
    /// </summary>
    public class MergeDuplicateFilesRequestDto
    {
        [Required]
        public IEnumerable<DuplicateFileGroupDto> DuplicateGroups { get; set; } = new List<DuplicateFileGroupDto>();
    }

    /// <summary>
    /// 重复文件组DTO
    /// </summary>
    public class DuplicateFileGroupDto
    {
        public Guid KeepFileId { get; set; }
        public IEnumerable<Guid> RemoveFileIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 存储分析DTO
    /// </summary>
    public class StorageAnalysisDto
    {
        public long TotalStorage { get; set; }
        public long UsedStorage { get; set; }
        public double UsagePercentage { get; set; }
        public IEnumerable<LargestFilesDto> LargestFiles { get; set; } = new List<LargestFilesDto>();
        public IEnumerable<UnusedFilesDto> UnusedFiles { get; set; } = new List<UnusedFilesDto>();
        public IEnumerable<DirectoryUsageDto> DirectoryUsage { get; set; } = new List<DirectoryUsageDto>();
    }

    // 其他相关DTO类...

    #endregion
}