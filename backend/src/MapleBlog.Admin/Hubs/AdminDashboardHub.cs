using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MapleBlog.Admin.Services;
using MapleBlog.Domain.DTOs;
using System;
using System.Threading.Tasks;

namespace MapleBlog.Admin.Hubs
{
    /// <summary>
    /// SignalR hub for real-time dashboard updates
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminDashboardHub : Hub
    {
        private readonly ISystemMonitorService _systemMonitorService;
        private readonly IDashboardService _dashboardService;
        private readonly IAnalyticsService _analyticsService;

        public AdminDashboardHub(
            ISystemMonitorService systemMonitorService,
            IDashboardService dashboardService,
            IAnalyticsService analyticsService)
        {
            _systemMonitorService = systemMonitorService;
            _dashboardService = dashboardService;
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Subscribe to real-time system metrics
        /// </summary>
        /// <returns>Task indicating subscription completion</returns>
        public async Task SubscribeToSystemMetrics()
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Setup periodic updates for system metrics
            while (true)
            {
                try
                {
                    var metrics = await _systemMonitorService.GetCurrentSystemMetricsAsync();
                    await Clients.Caller.SendAsync("ReceiveSystemMetrics", metrics);

                    // Update every 5 seconds
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    // Log or handle the exception
                    await Clients.Caller.SendAsync("ErrorReceivingMetrics", ex.Message);
                    break;
                }
            }
        }

        /// <summary>
        /// Subscribe to real-time dashboard summary
        /// </summary>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <returns>Task indicating subscription completion</returns>
        public async Task SubscribeToDashboardSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            try
            {
                var summary = await _dashboardService.GetDashboardSummaryAsync(startDate.Value, endDate.Value);
                await Clients.Caller.SendAsync("ReceiveDashboardSummary", summary);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ErrorReceivingSummary", ex.Message);
            }
        }

        /// <summary>
        /// Subscribe to real-time analytics for a specific metric type
        /// </summary>
        /// <param name="metricType">Type of metric to receive</param>
        /// <param name="startDate">Start date for metrics</param>
        /// <param name="endDate">End date for metrics</param>
        /// <returns>Task indicating subscription completion</returns>
        public async Task SubscribeToAnalytics(string metricType, DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddMonths(-1);
            endDate ??= DateTime.UtcNow;

            // Setup periodic updates for analytics
            while (true)
            {
                try
                {
                    var analytics = await _analyticsService.GetTimeSeriesMetricsAsync(metricType, startDate.Value, endDate.Value);
                    await Clients.Caller.SendAsync("ReceiveAnalytics", analytics);

                    // Update every 10 seconds
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    await Clients.Caller.SendAsync("ErrorReceivingAnalytics", ex.Message);
                    break;
                }
            }
        }

        /// <summary>
        /// Broadcast system events to all connected admin clients
        /// </summary>
        /// <param name="systemEvent">System event to broadcast</param>
        /// <returns>Task indicating broadcast completion</returns>
        public async Task BroadcastSystemEvent(SystemEventDto systemEvent)
        {
            await Clients.All.SendAsync("ReceiveSystemEvent", systemEvent);
        }

        /// <summary>
        /// Client connection handler
        /// </summary>
        /// <returns>Task indicating connection completion</returns>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _dashboardService.LogDashboardConnectionAsync(userId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Client disconnection handler
        /// </summary>
        /// <param name="exception">Exception that caused disconnection, if any</param>
        /// <returns>Task indicating disconnection completion</returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _dashboardService.LogDashboardDisconnectionAsync(userId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}