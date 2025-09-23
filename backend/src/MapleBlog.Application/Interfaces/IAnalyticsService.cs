using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task<Dictionary<string, object>> GetDashboardAnalyticsAsync();
        Task<Dictionary<string, object>> GetUserAnalyticsAsync(string userId);
        Task<Dictionary<string, object>> GetContentAnalyticsAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, object>> GetSystemAnalyticsAsync();
        Task TrackEventAsync(string eventName, Dictionary<string, object> properties);
        Task TrackPageViewAsync(string pageName, string userId = null);
        Task<Dictionary<string, object>> GetRealTimeAnalyticsAsync();
        Task<TimeSeriesMetricsDto> GetTimeSeriesMetricsAsync(string metricName, DateTime startDate, DateTime endDate);
    }
}