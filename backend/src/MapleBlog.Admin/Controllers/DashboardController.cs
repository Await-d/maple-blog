using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.Application.Services;
using MapleBlog.Admin.Services;
using MapleBlog.Domain.DTOs;
using System;
using System.Threading.Tasks;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// Admin dashboard controller providing real-time metrics and statistics
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ISystemMonitorService _systemMonitorService;
        private readonly IAnalyticsService _analyticsService;

        public DashboardController(
            IDashboardService dashboardService,
            ISystemMonitorService systemMonitorService,
            IAnalyticsService analyticsService)
        {
            _dashboardService = dashboardService;
            _systemMonitorService = systemMonitorService;
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Get overall dashboard summary statistics
        /// </summary>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <returns>Dashboard summary statistics</returns>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(DashboardSummaryDto), 200)]
        public async Task<IActionResult> GetDashboardSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            var summary = await _dashboardService.GetDashboardSummaryAsync(startDate.Value, endDate.Value);
            return Ok(summary);
        }

        /// <summary>
        /// Get real-time system performance metrics
        /// </summary>
        /// <returns>System performance metrics</returns>
        [HttpGet("system-metrics")]
        [ProducesResponseType(typeof(SystemMetricsDto), 200)]
        public async Task<IActionResult> GetSystemMetrics()
        {
            var metrics = await _systemMonitorService.GetCurrentSystemMetricsAsync();
            return Ok(metrics);
        }

        /// <summary>
        /// Get time-series analytics for specified metrics
        /// </summary>
        /// <param name="metricType">Type of metric to retrieve</param>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <returns>Time-series metrics</returns>
        [HttpGet("analytics")]
        [ProducesResponseType(typeof(TimeSeriesMetricsDto), 200)]
        public async Task<IActionResult> GetAnalytics(
            [FromQuery] string metricType,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            var analytics = await _analyticsService.GetTimeSeriesMetricsAsync(metricType, startDate.Value, endDate.Value);
            return Ok(analytics);
        }

        /// <summary>
        /// Get custom dashboard configuration for the current user
        /// </summary>
        /// <returns>User's dashboard configuration</returns>
        [HttpGet("config")]
        [ProducesResponseType(typeof(DashboardConfigDto), 200)]
        public async Task<IActionResult> GetDashboardConfig()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var config = await _dashboardService.GetUserDashboardConfigAsync(userId);
            return Ok(config);
        }

        /// <summary>
        /// Update custom dashboard configuration
        /// </summary>
        /// <param name="config">Dashboard configuration to update</param>
        /// <returns>Updated dashboard configuration</returns>
        [HttpPut("config")]
        [ProducesResponseType(typeof(DashboardConfigDto), 200)]
        public async Task<IActionResult> UpdateDashboardConfig([FromBody] DashboardConfigDto config)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var updatedConfig = await _dashboardService.UpdateUserDashboardConfigAsync(userId, config);
            return Ok(updatedConfig);
        }

        /// <summary>
        /// Get recent system events and notifications
        /// </summary>
        /// <param name="limit">Number of recent events to retrieve</param>
        /// <returns>List of recent system events</returns>
        [HttpGet("events")]
        [ProducesResponseType(typeof(IEnumerable<SystemEventDto>), 200)]
        public async Task<IActionResult> GetRecentEvents([FromQuery] int limit = 20)
        {
            var events = await _systemMonitorService.GetRecentSystemEventsAsync(limit);
            return Ok(events);
        }
    }
}