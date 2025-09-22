using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Admin.Services;
using MapleBlog.Admin.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// Reports controller for comprehensive report generation and management
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin,Analyst,ReportViewer")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;
        private readonly AnalyticsService _analyticsService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ReportsController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _reportStoragePath;

        public ReportsController(
            ReportService reportService,
            AnalyticsService analyticsService,
            IDistributedCache cache,
            ILogger<ReportsController> logger,
            IWebHostEnvironment environment)
        {
            _reportService = reportService;
            _analyticsService = analyticsService;
            _cache = cache;
            _logger = logger;
            _environment = environment;
            _reportStoragePath = Path.Combine(_environment.ContentRootPath, "Reports");

            // Ensure reports directory exists
            if (!Directory.Exists(_reportStoragePath))
            {
                Directory.CreateDirectory(_reportStoragePath);
            }
        }

        /// <summary>
        /// Generate a comprehensive analytics report
        /// </summary>
        /// <param name="request">Report generation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated report</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateReport(
            [FromBody] ReportGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null || request.Query == null)
                {
                    return BadRequest(new { error = "Report request and query are required" });
                }

                if (request.Query.StartDate > request.Query.EndDate)
                {
                    return BadRequest(new { error = "Start date must be before end date" });
                }

                _logger.LogInformation("Generating {ReportType} report for period {StartDate} to {EndDate}",
                    request.ReportType, request.Query.StartDate, request.Query.EndDate);

                var report = await _reportService.GenerateComprehensiveReportAsync(
                    request.ReportType,
                    request.Query,
                    request.ExportOptions ?? new ExportOptionsDto(),
                    cancellationToken);

                // Store report metadata for retrieval
                await StoreReportMetadataAsync(report, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new { error = "An error occurred while generating the report" });
            }
        }

        /// <summary>
        /// Generate multiple reports in batch
        /// </summary>
        /// <param name="request">Batch report request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of generated reports</returns>
        [HttpPost("generate-batch")]
        [ProducesResponseType(typeof(List<AnalyticsReportDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateBatchReports(
            [FromBody] BatchReportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null || !request.ReportTypes.Any())
                {
                    return BadRequest(new { error = "Batch request with report types is required" });
                }

                _logger.LogInformation("Generating batch of {Count} reports", request.ReportTypes.Count);

                var reports = await _reportService.GenerateBatchReportsAsync(
                    request.ReportTypes,
                    request.Query,
                    cancellationToken);

                // Store metadata for each report
                foreach (var report in reports.Where(r => r.Status == ReportStatus.Completed))
                {
                    await StoreReportMetadataAsync(report, cancellationToken);
                }

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch reports");
                return StatusCode(500, new { error = "An error occurred while generating batch reports" });
            }
        }

        /// <summary>
        /// Generate scheduled reports (daily, weekly, monthly)
        /// </summary>
        /// <param name="scheduleType">Schedule type</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated scheduled report</returns>
        [HttpPost("generate-scheduled/{scheduleType}")]
        [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateScheduledReport(
            ScheduledReportType scheduleType,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating scheduled {ScheduleType} report", scheduleType);

                var report = await _reportService.GenerateScheduledReportAsync(scheduleType, cancellationToken);

                await StoreReportMetadataAsync(report, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating scheduled report");
                return StatusCode(500, new { error = "An error occurred while generating scheduled report" });
            }
        }

        /// <summary>
        /// Generate custom report with user-defined parameters
        /// </summary>
        /// <param name="request">Custom report request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated custom report</returns>
        [HttpPost("generate-custom")]
        [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateCustomReport(
            [FromBody] CustomReportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.ReportName))
                {
                    return BadRequest(new { error = "Custom report request with name is required" });
                }

                _logger.LogInformation("Generating custom report: {ReportName}", request.ReportName);

                var report = await _reportService.GenerateCustomReportAsync(request, cancellationToken);

                await StoreReportMetadataAsync(report, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom report");
                return StatusCode(500, new { error = "An error occurred while generating custom report" });
            }
        }

        /// <summary>
        /// Generate comparison report between two periods
        /// </summary>
        /// <param name="request">Comparison report request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comparison report</returns>
        [HttpPost("generate-comparison")]
        [ProducesResponseType(typeof(ComparisonReportDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateComparisonReport(
            [FromBody] ComparisonReportRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { error = "Comparison report request is required" });
                }

                _logger.LogInformation("Generating comparison report: {ReportName}", request.ReportName);

                var report = await _reportService.GenerateComparisonReportAsync(request, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating comparison report");
                return StatusCode(500, new { error = "An error occurred while generating comparison report" });
            }
        }

        /// <summary>
        /// Generate real-time report with current metrics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Real-time report</returns>
        [HttpGet("realtime")]
        [ProducesResponseType(typeof(RealtimeReportDto), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRealtimeReport(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating real-time report");

                // Check cache first for real-time data
                var cacheKey = "report:realtime";
                var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (!string.IsNullOrEmpty(cached))
                {
                    var cachedReport = JsonSerializer.Deserialize<RealtimeReportDto>(cached);
                    return Ok(cachedReport);
                }

                var report = await _reportService.GenerateRealtimeReportAsync(cancellationToken);

                // Cache for 30 seconds
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(report), cacheOptions, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating real-time report");
                return StatusCode(500, new { error = "An error occurred while generating real-time report" });
            }
        }

        /// <summary>
        /// Generate trend analysis report
        /// </summary>
        /// <param name="request">Trend analysis request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Trend analysis report</returns>
        [HttpPost("trend-analysis")]
        [ProducesResponseType(typeof(TrendAnalysisReportDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateTrendAnalysisReport(
            [FromBody] TrendAnalysisRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.MetricName))
                {
                    return BadRequest(new { error = "Trend analysis request with metric name is required" });
                }

                _logger.LogInformation("Generating trend analysis for metric: {MetricName}", request.MetricName);

                var report = await _reportService.GenerateTrendAnalysisReportAsync(request, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating trend analysis report");
                return StatusCode(500, new { error = "An error occurred while generating trend analysis report" });
            }
        }

        /// <summary>
        /// Export an existing report to specified format
        /// </summary>
        /// <param name="reportId">Report ID</param>
        /// <param name="format">Export format (csv, excel, pdf, json)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File download result</returns>
        [HttpGet("export/{reportId}")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ExportReport(
            Guid reportId,
            [FromQuery] string format = "pdf",
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Exporting report {ReportId} to {Format}", reportId, format);

                // Retrieve report from cache or storage
                var report = await GetStoredReportAsync(reportId, cancellationToken);

                if (report == null)
                {
                    return NotFound(new { error = "Report not found" });
                }

                var exportOptions = new ExportOptionsDto
                {
                    Format = Enum.Parse<ExportFormat>(format, true),
                    IncludeCharts = true,
                    IncludeRawData = false
                };

                var fileName = await _reportService.ExportReportAsync(report, exportOptions, cancellationToken);
                var filePath = Path.Combine(_reportStoragePath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "Export file not found" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, cancellationToken);
                var contentType = GetContentType(format);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                return StatusCode(500, new { error = "An error occurred while exporting report" });
            }
        }

        /// <summary>
        /// Get list of available reports
        /// </summary>
        /// <param name="startDate">Filter by start date</param>
        /// <param name="endDate">Filter by end date</param>
        /// <param name="reportType">Filter by report type</param>
        /// <param name="status">Filter by status</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of reports</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ReportMetadataDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetReports(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] ReportType? reportType,
            [FromQuery] ReportStatus? status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching reports list");

                // Get reports metadata from storage
                var reports = await GetStoredReportsMetadataAsync(
                    startDate,
                    endDate,
                    reportType,
                    status,
                    cancellationToken);

                // Apply pagination
                var totalCount = reports.Count;
                var pagedReports = reports
                    .OrderByDescending(r => r.GeneratedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PagedResult<ReportMetadataDto>
                {
                    Items = pagedReports,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports list");
                return StatusCode(500, new { error = "An error occurred while fetching reports" });
            }
        }

        /// <summary>
        /// Get a specific report by ID
        /// </summary>
        /// <param name="reportId">Report ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Report details</returns>
        [HttpGet("{reportId}")]
        [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching report {ReportId}", reportId);

                var report = await GetStoredReportAsync(reportId, cancellationToken);

                if (report == null)
                {
                    return NotFound(new { error = "Report not found" });
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching report");
                return StatusCode(500, new { error = "An error occurred while fetching report" });
            }
        }

        /// <summary>
        /// Delete a report
        /// </summary>
        /// <param name="reportId">Report ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("{reportId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting report {ReportId}", reportId);

                var deleted = await DeleteStoredReportAsync(reportId, cancellationToken);

                if (!deleted)
                {
                    return NotFound(new { error = "Report not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report");
                return StatusCode(500, new { error = "An error occurred while deleting report" });
            }
        }

        /// <summary>
        /// Schedule a report for automatic generation
        /// </summary>
        /// <param name="schedule">Schedule configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Schedule creation result</returns>
        [HttpPost("schedule")]
        [ProducesResponseType(typeof(ReportScheduleDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ScheduleReport(
            [FromBody] ReportScheduleRequest schedule,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (schedule == null)
                {
                    return BadRequest(new { error = "Schedule configuration is required" });
                }

                _logger.LogInformation("Creating report schedule: {ScheduleName}", schedule.Name);

                var scheduleDto = new ReportScheduleDto
                {
                    Id = Guid.NewGuid(),
                    Name = schedule.Name,
                    ReportType = schedule.ReportType,
                    Schedule = schedule.CronExpression,
                    Query = schedule.Query,
                    Recipients = schedule.Recipients,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    NextRunTime = CalculateNextRunTime(schedule.CronExpression)
                };

                // Store schedule in database/cache
                await StoreReportScheduleAsync(scheduleDto, cancellationToken);

                return Ok(scheduleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling report");
                return StatusCode(500, new { error = "An error occurred while scheduling report" });
            }
        }

        /// <summary>
        /// Get report templates for quick report generation
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of report templates</returns>
        [HttpGet("templates")]
        [ProducesResponseType(typeof(List<ReportTemplateDto>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetReportTemplates(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching report templates");

                var templates = await GetAvailableTemplatesAsync(cancellationToken);

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching report templates");
                return StatusCode(500, new { error = "An error occurred while fetching templates" });
            }
        }

        /// <summary>
        /// Generate report from template
        /// </summary>
        /// <param name="templateId">Template ID</param>
        /// <param name="parameters">Template parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated report</returns>
        [HttpPost("templates/{templateId}/generate")]
        [ProducesResponseType(typeof(AnalyticsReportDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GenerateFromTemplate(
            Guid templateId,
            [FromBody] Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating report from template {TemplateId}", templateId);

                var template = await GetReportTemplateAsync(templateId, cancellationToken);

                if (template == null)
                {
                    return NotFound(new { error = "Template not found" });
                }

                // Apply parameters to template query
                var query = ApplyParametersToQuery(template.DefaultQuery, parameters);

                var report = await _analyticsService.GenerateReportAsync(
                    template.ReportType,
                    query,
                    cancellationToken);

                await StoreReportMetadataAsync(report, cancellationToken);

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report from template");
                return StatusCode(500, new { error = "An error occurred while generating report from template" });
            }
        }

        // Private helper methods

        private async Task StoreReportMetadataAsync(AnalyticsReportDto report, CancellationToken cancellationToken)
        {
            var metadata = new ReportMetadataDto
            {
                Id = report.Id,
                Name = report.Name,
                Description = report.Description,
                Type = report.Type,
                Status = report.Status,
                GeneratedAt = report.GeneratedAt,
                GenerationTimeMs = report.GenerationTimeMs,
                RecordCount = report.RecordCount,
                ExportFormats = new List<string>(),
                FileSize = 0
            };

            var cacheKey = $"report:metadata:{report.Id}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(metadata), cacheOptions, cancellationToken);
        }

        private async Task<AnalyticsReportDto?> GetStoredReportAsync(Guid reportId, CancellationToken cancellationToken)
        {
            var cacheKey = $"report:data:{reportId}";
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);

            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<AnalyticsReportDto>(cached);
            }

            // If not in cache, try to load from file
            var filePath = Path.Combine(_reportStoragePath, $"{reportId}.json");
            if (System.IO.File.Exists(filePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize<AnalyticsReportDto>(json);
            }

            return null;
        }

        private async Task<List<ReportMetadataDto>> GetStoredReportsMetadataAsync(
            DateTime? startDate,
            DateTime? endDate,
            ReportType? reportType,
            ReportStatus? status,
            CancellationToken cancellationToken)
        {
            // In a real implementation, this would query a database
            // For now, return mock data
            var reports = new List<ReportMetadataDto>();

            for (int i = 0; i < 50; i++)
            {
                reports.Add(new ReportMetadataDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"Report {i + 1}",
                    Type = (ReportType)(i % 5),
                    Status = ReportStatus.Completed,
                    GeneratedAt = DateTime.UtcNow.AddDays(-i),
                    RecordCount = Random.Shared.Next(100, 10000),
                    FileSize = Random.Shared.Next(1000, 1000000)
                });
            }

            // Apply filters
            if (startDate.HasValue)
                reports = reports.Where(r => r.GeneratedAt >= startDate.Value).ToList();

            if (endDate.HasValue)
                reports = reports.Where(r => r.GeneratedAt <= endDate.Value).ToList();

            if (reportType.HasValue)
                reports = reports.Where(r => r.Type == reportType.Value).ToList();

            if (status.HasValue)
                reports = reports.Where(r => r.Status == status.Value).ToList();

            await Task.Delay(1, cancellationToken); // Placeholder for async
            return reports;
        }

        private async Task<bool> DeleteStoredReportAsync(Guid reportId, CancellationToken cancellationToken)
        {
            // Remove from cache
            var metadataCacheKey = $"report:metadata:{reportId}";
            var dataCacheKey = $"report:data:{reportId}";

            await _cache.RemoveAsync(metadataCacheKey, cancellationToken);
            await _cache.RemoveAsync(dataCacheKey, cancellationToken);

            // Remove file if exists
            var filePath = Path.Combine(_reportStoragePath, $"{reportId}.json");
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return true;
            }

            return false;
        }

        private async Task StoreReportScheduleAsync(ReportScheduleDto schedule, CancellationToken cancellationToken)
        {
            var cacheKey = $"report:schedule:{schedule.Id}";
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(schedule), cacheOptions, cancellationToken);
        }

        private async Task<List<ReportTemplateDto>> GetAvailableTemplatesAsync(CancellationToken cancellationToken)
        {
            // Return predefined templates
            var templates = new List<ReportTemplateDto>
            {
                new ReportTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Weekly User Activity",
                    Description = "User activity and engagement metrics for the past week",
                    Category = "User Analytics",
                    ReportType = ReportType.UserBehavior,
                    DefaultQuery = new AnalyticsQueryDto
                    {
                        StartDate = DateTime.UtcNow.AddDays(-7),
                        EndDate = DateTime.UtcNow,
                        TimeGranularity = TimeGranularity.Day
                    }
                },
                new ReportTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Monthly Content Performance",
                    Description = "Content performance metrics for the past month",
                    Category = "Content Analytics",
                    ReportType = ReportType.ContentPerformance,
                    DefaultQuery = new AnalyticsQueryDto
                    {
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow,
                        TimeGranularity = TimeGranularity.Week
                    }
                },
                new ReportTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Traffic Source Analysis",
                    Description = "Breakdown of traffic sources and channels",
                    Category = "Traffic Analytics",
                    ReportType = ReportType.TrafficAnalysis,
                    DefaultQuery = new AnalyticsQueryDto
                    {
                        StartDate = DateTime.UtcNow.AddDays(-30),
                        EndDate = DateTime.UtcNow,
                        TimeGranularity = TimeGranularity.Day
                    }
                },
                new ReportTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Conversion Funnel",
                    Description = "User conversion funnel analysis",
                    Category = "Conversion Analytics",
                    ReportType = ReportType.ConversionFunnel,
                    DefaultQuery = new AnalyticsQueryDto
                    {
                        StartDate = DateTime.UtcNow.AddDays(-30),
                        EndDate = DateTime.UtcNow
                    }
                },
                new ReportTemplateDto
                {
                    Id = Guid.NewGuid(),
                    Name = "SEO Performance",
                    Description = "SEO metrics and keyword performance",
                    Category = "SEO Analytics",
                    ReportType = ReportType.SeoPerformance,
                    DefaultQuery = new AnalyticsQueryDto
                    {
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow,
                        TimeGranularity = TimeGranularity.Week
                    }
                }
            };

            await Task.Delay(1, cancellationToken); // Placeholder for async
            return templates;
        }

        private async Task<ReportTemplateDto?> GetReportTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            var templates = await GetAvailableTemplatesAsync(cancellationToken);
            return templates.FirstOrDefault(t => t.Id == templateId);
        }

        private AnalyticsQueryDto ApplyParametersToQuery(AnalyticsQueryDto baseQuery, Dictionary<string, object> parameters)
        {
            var query = new AnalyticsQueryDto
            {
                StartDate = baseQuery.StartDate,
                EndDate = baseQuery.EndDate,
                TimeGranularity = baseQuery.TimeGranularity,
                Dimensions = new List<string>(baseQuery.Dimensions),
                Metrics = new List<string>(baseQuery.Metrics),
                Filters = new Dictionary<string, object>(baseQuery.Filters),
                Aggregations = new Dictionary<string, string>(baseQuery.Aggregations)
            };

            // Apply parameters
            foreach (var param in parameters)
            {
                switch (param.Key.ToLower())
                {
                    case "startdate":
                        if (DateTime.TryParse(param.Value.ToString(), out var startDate))
                            query.StartDate = startDate;
                        break;

                    case "enddate":
                        if (DateTime.TryParse(param.Value.ToString(), out var endDate))
                            query.EndDate = endDate;
                        break;

                    case "timegranularity":
                        if (Enum.TryParse<TimeGranularity>(param.Value.ToString(), out var granularity))
                            query.TimeGranularity = granularity;
                        break;

                    default:
                        // Add to filters
                        query.Filters[param.Key] = param.Value;
                        break;
                }
            }

            return query;
        }

        private string GetContentType(string format)
        {
            return format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" or "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                "json" => "application/json",
                _ => "application/octet-stream"
            };
        }

        private DateTime CalculateNextRunTime(string cronExpression)
        {
            // Simplified implementation - in production, use a proper cron parser
            return DateTime.UtcNow.AddDays(1);
        }
    }

    // Additional DTOs for ReportsController

    public class ReportGenerationRequest
    {
        public ReportType ReportType { get; set; }
        public AnalyticsQueryDto Query { get; set; } = new();
        public ExportOptionsDto? ExportOptions { get; set; }
    }

    public class BatchReportRequest
    {
        public List<ReportType> ReportTypes { get; set; } = new();
        public AnalyticsQueryDto Query { get; set; } = new();
    }

    public class ReportMetadataDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime GeneratedAt { get; set; }
        public long GenerationTimeMs { get; set; }
        public int RecordCount { get; set; }
        public List<string> ExportFormats { get; set; } = new();
        public long FileSize { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ReportScheduleRequest
    {
        public string Name { get; set; } = string.Empty;
        public ReportType ReportType { get; set; }
        public string CronExpression { get; set; } = string.Empty;
        public AnalyticsQueryDto Query { get; set; } = new();
        public List<string> Recipients { get; set; } = new();
    }

    public class ReportScheduleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ReportType ReportType { get; set; }
        public string Schedule { get; set; } = string.Empty;
        public AnalyticsQueryDto Query { get; set; } = new();
        public List<string> Recipients { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
    }

    public class ReportTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ReportType ReportType { get; set; }
        public AnalyticsQueryDto DefaultQuery { get; set; } = new();
        public Dictionary<string, string> Parameters { get; set; } = new();
    }
}