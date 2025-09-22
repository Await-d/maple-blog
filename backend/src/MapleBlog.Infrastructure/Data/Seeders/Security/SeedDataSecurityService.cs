using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using MapleBlog.Domain.Entities;
using MapleBlog.Application.Interfaces;
using MapleBlog.Infrastructure.Data.Seeders.Core;

namespace MapleBlog.Infrastructure.Data.Seeders.Security;

/// <summary>
/// Security service for seed data operations with comprehensive audit logging
/// </summary>
public class SeedDataSecurityService
{
    private readonly BlogDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedDataSecurityService> _logger;
    private readonly IAuditLogService _auditLogService;

    public SeedDataSecurityService(
        BlogDbContext context,
        IConfiguration configuration,
        ILogger<SeedDataSecurityService> logger,
        IAuditLogService auditLogService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Performs comprehensive security validation for seed data operations
    /// </summary>
    public async Task<SecurityValidationResult> ValidateSecurityAsync(string environment, string operation, string userId = "System")
    {
        var result = new SecurityValidationResult
        {
            Environment = environment,
            Operation = operation,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            await LogSecurityEventAsync("SecurityValidation", "Started", environment, operation, userId);

            // Validate environment security
            await ValidateEnvironmentSecurityAsync(result);

            // Validate user permissions
            await ValidateUserPermissionsAsync(result);

            // Validate operation security
            await ValidateOperationSecurityAsync(result);

            // Check for suspicious patterns
            await CheckSuspiciousPatternsAsync(result);

            // Validate data integrity requirements
            await ValidateDataIntegrityAsync(result);

            // Environment-specific security checks
            await PerformEnvironmentSpecificSecurityChecksAsync(result);

            result.IsValid = result.SecurityIssues.Count == 0;

            await LogSecurityEventAsync("SecurityValidation",
                result.IsValid ? "Passed" : "Failed",
                environment, operation, userId, result);

            return result;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.SecurityIssues.Add($"Security validation failed: {ex.Message}");

            await LogSecurityEventAsync("SecurityValidation", "Error", environment, operation, userId, ex);

            _logger.LogError(ex, "Error during security validation for operation {Operation} in environment {Environment}",
                operation, environment);

            return result;
        }
    }

    /// <summary>
    /// Creates secure audit trail for seed data operations
    /// </summary>
    public async Task CreateSecureAuditTrailAsync(SeedResult seedResult, string userId = "System")
    {
        try
        {
            var auditData = new
            {
                Environment = seedResult.Environment,
                Operation = "SeedData",
                Success = seedResult.IsSuccess,
                Duration = seedResult.Duration,
                Statistics = new
                {
                    TotalCreated = seedResult.TotalCreated,
                    TotalSkipped = seedResult.TotalSkipped,
                    RolesCreated = seedResult.RolesCreated,
                    UsersCreated = seedResult.UsersCreated,
                    CategoriesCreated = seedResult.CategoriesCreated,
                    TagsCreated = seedResult.TagsCreated,
                    PostsCreated = seedResult.PostsCreated,
                    ConfigurationsCreated = seedResult.ConfigurationsCreated
                },
                ValidationErrors = seedResult.ValidationErrors,
                ValidationWarnings = seedResult.ValidationWarnings,
                SecurityHash = GenerateSecurityHash(seedResult)
            };

            var auditLog = new AuditLog
            {
                Action = seedResult.IsSuccess ? "Completed" : "Failed",
                ResourceType = "SeedData",
                Description = $"Seed data operation for {seedResult.Environment}",
                AdditionalData = System.Text.Json.JsonSerializer.Serialize(auditData),
                Category = "SeedDataSecurity"
            };

            if (Guid.TryParse(userId, out var userGuid))
            {
                auditLog.UserId = userGuid;
            }

            await _auditLogService.LogAsync(auditLog);

            // Create additional security log entry
            await LogSecurityEventAsync("SeedDataOperation",
                seedResult.IsSuccess ? "Completed" : "Failed",
                seedResult.Environment, "SeedData", userId, auditData);

            _logger.LogInformation("Secure audit trail created for seed operation in {Environment} by user {UserId}",
                seedResult.Environment, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create secure audit trail for seed operation");
            // Don't throw - audit failure shouldn't break the main operation
        }
    }

    /// <summary>
    /// Validates that user has sufficient permissions for the operation
    /// </summary>
    public async Task<bool> ValidateUserPermissionsAsync(string userId, string operation, string environment)
    {
        try
        {
            if (userId == "System")
            {
                // System operations are allowed but logged
                await LogSecurityEventAsync("PermissionCheck", "SystemUser", environment, operation, userId);
                return true;
            }

            // Check if user exists and is active
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null || !user.IsActive)
            {
                await LogSecurityEventAsync("PermissionCheck", "UserNotFound", environment, operation, userId);
                return false;
            }

            // Check user roles and permissions
            var hasPermission = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id && ur.IsActive)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
                .Join(_context.RolePermissions, r => r.Id, rp => rp.RoleId, (r, rp) => rp)
                .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
                .AnyAsync(p => p.Name == "System.FullAccess" || p.Name == "System.ManageConfig");

            if (!hasPermission)
            {
                await LogSecurityEventAsync("PermissionCheck", "InsufficientPermissions", environment, operation, userId);
                return false;
            }

            await LogSecurityEventAsync("PermissionCheck", "Granted", environment, operation, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user permissions for {UserId}", userId);
            await LogSecurityEventAsync("PermissionCheck", "Error", environment, operation, userId, ex);
            return false;
        }
    }

    /// <summary>
    /// Monitors for suspicious activity patterns
    /// </summary>
    public async Task<SuspiciousActivityReport> MonitorSuspiciousActivityAsync(TimeSpan timeWindow)
    {
        var report = new SuspiciousActivityReport
        {
            TimeWindow = timeWindow,
            CheckTime = DateTime.UtcNow
        };

        try
        {
            var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);

            // Check for multiple failed operations from same user
            var failedOperations = await _context.AuditLogs
                .Where(al => al.Timestamp >= cutoffTime &&
                           al.Action.Contains("Failed") &&
                           al.ResourceType == "SeedData")
                .GroupBy(al => al.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .Where(x => x.Count >= 3)
                .ToListAsync();

            foreach (var failure in failedOperations)
            {
                report.SuspiciousActivities.Add($"User {failure.UserId} had {failure.Count} failed seed operations");
            }

            // Check for operations outside business hours
            var afterHoursOperations = await _context.AuditLogs
                .Where(al => al.Timestamp >= cutoffTime &&
                           al.ResourceType == "SeedData" &&
                           (al.Timestamp.Hour < 6 || al.Timestamp.Hour > 22))
                .CountAsync();

            if (afterHoursOperations > 0)
            {
                report.SuspiciousActivities.Add($"{afterHoursOperations} seed operations performed outside business hours");
            }

            // Check for rapid successive operations
            var rapidOperations = await _context.AuditLogs
                .Where(al => al.Timestamp >= cutoffTime && al.ResourceType == "SeedData")
                .GroupBy(al => al.UserId)
                .Where(g => g.Count() > 10)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var rapid in rapidOperations)
            {
                report.SuspiciousActivities.Add($"User {rapid.UserId} performed {rapid.Count} operations in {timeWindow.TotalMinutes} minutes");
            }

            // Check for production environment operations
            var productionOperations = await _context.AuditLogs
                .Where(al => al.Timestamp >= cutoffTime &&
                           al.ResourceType == "SeedData" &&
                           al.Details.Contains("Production"))
                .CountAsync();

            if (productionOperations > 0)
            {
                report.SuspiciousActivities.Add($"{productionOperations} seed operations performed in production environment");
            }

            report.HasSuspiciousActivity = report.SuspiciousActivities.Any();

            if (report.HasSuspiciousActivity)
            {
                await LogSecurityEventAsync("SuspiciousActivity", "Detected", "System", "Monitor", "System", report);
            }

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring suspicious activity");
            report.SuspiciousActivities.Add($"Error monitoring suspicious activity: {ex.Message}");
            return report;
        }
    }

    /// <summary>
    /// Encrypts sensitive seed data configuration
    /// </summary>
    public string EncryptSensitiveData(string data)
    {
        try
        {
            var key = GetEncryptionKey();
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting sensitive data");
            throw;
        }
    }

    /// <summary>
    /// Decrypts sensitive seed data configuration
    /// </summary>
    public string DecryptSensitiveData(string encryptedData)
    {
        try
        {
            var key = GetEncryptionKey();
            var data = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.Key = key;

            var iv = new byte[aes.IV.Length];
            var encrypted = new byte[data.Length - iv.Length];

            Array.Copy(data, 0, iv, 0, iv.Length);
            Array.Copy(data, iv.Length, encrypted, 0, encrypted.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting sensitive data");
            throw;
        }
    }

    #region Private Methods

    private async Task ValidateEnvironmentSecurityAsync(SecurityValidationResult result)
    {
        // Check for production environment safeguards
        if (result.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            // Ensure HTTPS is enforced
            var httpsRequired = _configuration.GetValue<bool>("Security:RequireHttpsMetadata", false);
            if (!httpsRequired)
            {
                result.SecurityIssues.Add("HTTPS not enforced in production environment");
            }

            // Check for secure connection strings
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("TrustServerCertificate=true"))
            {
                result.SecurityWarnings.Add("Database connection allows untrusted certificates in production");
            }

            // Ensure sensitive data is properly secured
            var jwtKey = _configuration.GetValue<string>("JwtSettings:SecretKey");
            if (!string.IsNullOrEmpty(jwtKey) && jwtKey.Length < 32)
            {
                result.SecurityIssues.Add("JWT secret key is too short for production use");
            }
        }
    }

    private async Task ValidateUserPermissionsAsync(SecurityValidationResult result)
    {
        // Check if operation requires elevated permissions
        if (result.Operation.Contains("Seed") || result.Operation.Contains("Clean"))
        {
            var hasValidUser = await ValidateUserPermissionsAsync(result.UserId, result.Operation, result.Environment);
            if (!hasValidUser)
            {
                result.SecurityIssues.Add($"User {result.UserId} lacks sufficient permissions for operation {result.Operation}");
            }
        }
    }

    private async Task ValidateOperationSecurityAsync(SecurityValidationResult result)
    {
        // Check for high-risk operations
        var highRiskOperations = new[] { "Clean", "Delete", "Reset", "Drop" };
        if (highRiskOperations.Any(op => result.Operation.Contains(op, StringComparison.OrdinalIgnoreCase)))
        {
            result.SecurityWarnings.Add($"High-risk operation detected: {result.Operation}");

            // Require additional confirmation for destructive operations
            if (result.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                result.RequiresAdditionalConfirmation = true;
            }
        }
    }

    private async Task CheckSuspiciousPatternsAsync(SecurityValidationResult result)
    {
        // Check for recent failed operations from same user
        Guid? userGuid = null;
        if (Guid.TryParse(result.UserId, out var parsedGuid))
        {
            userGuid = parsedGuid;
        }

        var recentFailures = await _context.AuditLogs
            .Where(al => al.UserId == userGuid &&
                        al.Timestamp >= DateTime.UtcNow.AddHours(-1) &&
                        al.Action.Contains("Failed"))
            .CountAsync();

        if (recentFailures >= 3)
        {
            result.SecurityIssues.Add($"User has {recentFailures} recent failed operations");
        }

        // Check for operations during off-hours
        var currentHour = DateTime.UtcNow.Hour;
        if (currentHour < 6 || currentHour > 22)
        {
            result.SecurityWarnings.Add("Operation requested outside normal business hours");
        }
    }

    private async Task ValidateDataIntegrityAsync(SecurityValidationResult result)
    {
        // Check database integrity
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                result.SecurityIssues.Add("Cannot establish secure database connection");
            }

            // Check for database corruption indicators
            var userCount = await _context.Users.CountAsync();
            var roleCount = await _context.Roles.CountAsync();

            if (userCount == 0 && roleCount == 0)
            {
                result.SecurityWarnings.Add("Database appears to be empty - potential integrity issue");
            }
        }
        catch (Exception ex)
        {
            result.SecurityIssues.Add($"Database integrity check failed: {ex.Message}");
        }
    }

    private async Task PerformEnvironmentSpecificSecurityChecksAsync(SecurityValidationResult result)
    {
        switch (result.Environment.ToLowerInvariant())
        {
            case "production":
                await ValidateProductionSecurityAsync(result);
                break;
            case "staging":
                await ValidateStagingSecurityAsync(result);
                break;
            case "development":
                await ValidateDevelopmentSecurityAsync(result);
                break;
        }
    }

    private async Task ValidateProductionSecurityAsync(SecurityValidationResult result)
    {
        // Check for test data in production
        var testUsers = await _context.Users
            .Where(u => u.Email.Value.Contains("test.com") ||
                       u.Email.Value.Contains("example.com") ||
                       u.UserName.Contains("test"))
            .CountAsync();

        if (testUsers > 0)
        {
            result.SecurityIssues.Add($"Found {testUsers} test users in production environment");
        }

        // Validate admin account security
        var adminUsers = await _context.Users
            .Where(u => u.UserName.ToLower().Contains("admin"))
            .ToListAsync();

        foreach (var admin in adminUsers)
        {
            if (admin.Email.Value.Contains("example.com") || admin.Email.Value.Contains("test.com"))
            {
                result.SecurityIssues.Add($"Admin user {admin.UserName} has test email in production");
            }
        }
    }

    private async Task ValidateStagingSecurityAsync(SecurityValidationResult result)
    {
        // Ensure staging is properly isolated
        var siteName = await _context.SystemConfigurations
            .Where(c => c.Key == "Site.Name")
            .FirstOrDefaultAsync();

        if (siteName != null && !siteName.Value.Contains("Staging"))
        {
            result.SecurityWarnings.Add("Site name doesn't indicate staging environment");
        }
    }

    private async Task ValidateDevelopmentSecurityAsync(SecurityValidationResult result)
    {
        // Check for production-like configurations in development
        var httpsRequired = _configuration.GetValue<bool>("Security:RequireHttpsMetadata", false);
        if (httpsRequired)
        {
            result.SecurityWarnings.Add("HTTPS enforcement enabled in development environment");
        }
    }

    private async Task LogSecurityEventAsync(string category, string action, string environment, string operation, string userId, object? data = null)
    {
        try
        {
            var eventData = new
            {
                Category = category,
                Environment = environment,
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                UserAgent = "SeedDataSystem",
                IpAddress = "Internal",
                Data = data
            };

            var auditLog = new AuditLog
            {
                Action = action,
                ResourceType = "SeedDataSecurity",
                Description = $"{category} for {operation} in {environment}",
                AdditionalData = System.Text.Json.JsonSerializer.Serialize(eventData),
                Category = category
            };

            if (Guid.TryParse(userId, out var userGuid))
            {
                auditLog.UserId = userGuid;
            }

            await _auditLogService.LogAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
        }
    }

    private string GenerateSecurityHash(object data)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashBytes);
        }
        catch
        {
            return "HASH_GENERATION_FAILED";
        }
    }

    private byte[] GetEncryptionKey()
    {
        var keyString = _configuration.GetValue<string>("SeedData:EncryptionKey")
                       ?? _configuration.GetValue<string>("JwtSettings:SecretKey")
                       ?? "DefaultKeyForDevelopmentOnlyNotForProduction";

        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
    }

    #endregion
}

/// <summary>
/// Result of security validation
/// </summary>
public class SecurityValidationResult
{
    public string Environment { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsValid { get; set; }
    public List<string> SecurityIssues { get; set; } = new();
    public List<string> SecurityWarnings { get; set; } = new();
    public bool RequiresAdditionalConfirmation { get; set; }

    public string GetSummary()
    {
        if (!IsValid)
        {
            return $"Security validation failed: {SecurityIssues.Count} issues, {SecurityWarnings.Count} warnings";
        }

        return $"Security validation passed with {SecurityWarnings.Count} warnings";
    }
}

/// <summary>
/// Report of suspicious activity monitoring
/// </summary>
public class SuspiciousActivityReport
{
    public TimeSpan TimeWindow { get; set; }
    public DateTime CheckTime { get; set; }
    public bool HasSuspiciousActivity { get; set; }
    public List<string> SuspiciousActivities { get; set; } = new();

    public string GetSummary()
    {
        if (!HasSuspiciousActivity)
        {
            return $"No suspicious activity detected in the last {TimeWindow.TotalHours:F1} hours";
        }

        return $"Detected {SuspiciousActivities.Count} suspicious activities in the last {TimeWindow.TotalHours:F1} hours";
    }
}