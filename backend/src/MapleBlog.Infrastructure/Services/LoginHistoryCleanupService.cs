using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Background service for cleaning up old login history records
/// </summary>
public class LoginHistoryCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoginHistoryCleanupService> _logger;
    private readonly LoginHistoryCleanupSettings _settings;
    private readonly TimeSpan _interval;

    public LoginHistoryCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LoginHistoryCleanupService> logger,
        IOptions<LoginHistoryCleanupSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings.Value;
        _interval = TimeSpan.FromHours(_settings.CleanupIntervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Login History Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login history cleanup");
                // Wait a shorter interval before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        _logger.LogInformation("Login History Cleanup Service stopped");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var loginTrackingService = scope.ServiceProvider.GetRequiredService<ILoginTrackingService>();

        _logger.LogInformation("Starting login history cleanup");

        var retentionPeriod = TimeSpan.FromDays(_settings.RetentionDays);

        try
        {
            await loginTrackingService.CleanupOldRecordsAsync(retentionPeriod, cancellationToken);
            _logger.LogInformation("Login history cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform login history cleanup");
            throw;
        }
    }
}

/// <summary>
/// Configuration settings for login history cleanup
/// </summary>
public class LoginHistoryCleanupSettings
{
    /// <summary>
    /// Number of days to retain login history records
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Interval in hours between cleanup operations
    /// </summary>
    public double CleanupIntervalHours { get; set; } = 24.0;

    /// <summary>
    /// Whether the cleanup service is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}