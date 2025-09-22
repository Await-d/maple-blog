using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using MapleBlog.Infrastructure.Data.Seeders.Core;
using MapleBlog.Application.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.API.Controllers.Admin;

/// <summary>
/// Admin controller for managing seed data operations
/// </summary>
[ApiController]
[Route("api/admin/seeddata")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class SeedDataController : ControllerBase
{
    private readonly SeedDataManager _seedDataManager;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SeedDataController> _logger;
    private readonly IAuditLogService _auditLogService;

    public SeedDataController(
        SeedDataManager seedDataManager,
        IHostEnvironment environment,
        ILogger<SeedDataController> logger,
        IAuditLogService auditLogService)
    {
        _seedDataManager = seedDataManager;
        _environment = environment;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Gets the current seed data status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<SeedStatus>> GetStatus()
    {
        try
        {
            var status = await _seedDataManager.GetSeedStatusAsync(_environment.EnvironmentName);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seed data status");
            return StatusCode(500, new { error = "Failed to get seed data status", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets available seed data providers
    /// </summary>
    [HttpGet("providers")]
    public ActionResult<IEnumerable<object>> GetProviders()
    {
        try
        {
            var providers = _seedDataManager.GetAvailableProviders()
                .Select(p => new
                {
                    Environment = p.Environment,
                    Priority = p.Priority,
                    Type = p.GetType().Name,
                    CanProvideFor = p.CanProvideFor(_environment.EnvironmentName)
                });

            return Ok(providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seed data providers");
            return StatusCode(500, new { error = "Failed to get seed data providers", message = ex.Message });
        }
    }

    /// <summary>
    /// Validates the environment for seeding
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<SeedValidationResult>> ValidateEnvironment([FromBody] ValidateEnvironmentRequest request)
    {
        try
        {
            var environment = string.IsNullOrEmpty(request.Environment) ? _environment.EnvironmentName : request.Environment;
            var provider = _seedDataManager.GetAvailableProviders()
                .FirstOrDefault(p => p.CanProvideFor(environment));

            if (provider == null)
            {
                return BadRequest(new { error = $"No provider found for environment: {environment}" });
            }

            var context = HttpContext.RequestServices.GetRequiredService<BlogDbContext>();
            var validation = await provider.ValidateEnvironmentAsync(context);

            await _auditLogService.LogUserActionAsync(
                null,
                User.Identity?.Name,
                "Validate",
                "SeedData",
                environment,
                $"Environment validation for {environment}",
                new { environment, isValid = validation.IsValid, errors = validation.Errors.Count });

            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating environment for seeding");
            return StatusCode(500, new { error = "Failed to validate environment", message = ex.Message });
        }
    }

    /// <summary>
    /// Seeds data for the specified environment
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult<SeedResult>> SeedData([FromBody] SeedDataRequest request)
    {
        try
        {
            var environment = string.IsNullOrEmpty(request.Environment) ? _environment.EnvironmentName : request.Environment;

            // Additional safety check for production
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase) && !request.ConfirmProduction)
            {
                return BadRequest(new { error = "Production seeding requires explicit confirmation" });
            }

            _logger.LogWarning("Initiating seed data operation for environment: {Environment} by user: {User}",
                environment, User.Identity?.Name);

            var result = await _seedDataManager.SeedAsync(environment, request.ForceSeeding);

            await _auditLogService.LogUserActionAsync(
                null,
                User.Identity?.Name,
                "Seed",
                "SeedData",
                environment,
                $"Seed data operation for {environment}",
                new
                {
                    environment,
                    success = result.IsSuccess,
                    duration = result.Duration,
                    created = result.TotalCreated,
                    skipped = result.TotalSkipped,
                    user = User.Identity?.Name
                });

            if (result.IsSuccess)
            {
                _logger.LogInformation("Seed data operation completed successfully: {Summary}", result.GetSummary());
                return Ok(result);
            }
            else
            {
                _logger.LogError("Seed data operation failed: {Summary}", result.GetSummary());
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during seed data operation");
            await _auditLogService.LogUserActionAsync(
                null,
                User.Identity?.Name,
                "SeedError",
                "SeedData",
                null,
                $"Seed data operation failed",
                new { error = ex.Message, user = User.Identity?.Name });

            return StatusCode(500, new { error = "Seed data operation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Cleans test data from the database
    /// </summary>
    [HttpPost("clean-test-data")]
    public async Task<ActionResult<CleanupResult>> CleanTestData([FromBody] CleanTestDataRequest request)
    {
        try
        {
            _logger.LogWarning("Initiating test data cleanup (dry run: {DryRun}) by user: {User}",
                request.DryRun, User.Identity?.Name);

            var result = await _seedDataManager.CleanTestDataAsync(request.DryRun);

            await _auditLogService.LogUserActionAsync(
                null,
                User.Identity?.Name,
                "CleanTestData",
                "SeedData",
                null,
                $"Test data cleanup operation",
                new
                {
                    dryRun = request.DryRun,
                    success = result.IsSuccess,
                    found = result.TotalFound,
                    removed = result.TotalRemoved,
                    user = User.Identity?.Name
                });

            if (result.IsSuccess)
            {
                _logger.LogInformation("Test data cleanup completed: {Summary}", result.GetSummary());
                return Ok(result);
            }
            else
            {
                _logger.LogError("Test data cleanup failed: {Summary}", result.GetSummary());
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during test data cleanup");
            await _auditLogService.LogUserActionAsync(
                null,
                User.Identity?.Name,
                "CleanTestDataError",
                "SeedData",
                null,
                $"Test data cleanup failed",
                new { error = ex.Message, user = User.Identity?.Name });

            return StatusCode(500, new { error = "Test data cleanup failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets seed data operation history
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<object>>> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // This would typically query audit logs for seed data operations
            // For now, return a placeholder response
            var history = new[]
            {
                new
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Operation = "Seed",
                    Environment = _environment.EnvironmentName,
                    User = "admin",
                    Success = true,
                    Summary = "Seeded 15 records successfully"
                }
            };

            return Ok(new
            {
                data = history,
                pagination = new
                {
                    page,
                    pageSize,
                    total = history.Length,
                    totalPages = 1
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seed data history");
            return StatusCode(500, new { error = "Failed to get seed data history", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets environment information
    /// </summary>
    [HttpGet("environment")]
    public ActionResult<object> GetEnvironmentInfo()
    {
        try
        {
            return Ok(new
            {
                Name = _environment.EnvironmentName,
                IsDevelopment = _environment.IsDevelopment(),
                IsProduction = _environment.IsProduction(),
                IsStaging = _environment.IsStaging(),
                ContentRootPath = _environment.ContentRootPath,
                ApplicationName = _environment.ApplicationName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting environment information");
            return StatusCode(500, new { error = "Failed to get environment information", message = ex.Message });
        }
    }
}

/// <summary>
/// Request model for environment validation
/// </summary>
public class ValidateEnvironmentRequest
{
    public string? Environment { get; set; }
}

/// <summary>
/// Request model for seeding data
/// </summary>
public class SeedDataRequest
{
    public string? Environment { get; set; }
    public bool ForceSeeding { get; set; } = false;
    public bool ConfirmProduction { get; set; } = false;
}

/// <summary>
/// Request model for cleaning test data
/// </summary>
public class CleanTestDataRequest
{
    public bool DryRun { get; set; } = true;
}