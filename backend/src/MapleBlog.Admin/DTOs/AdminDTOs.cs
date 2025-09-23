using System;
using System.Collections.Generic;

namespace MapleBlog.Admin.DTOs
{
    public class DashboardConfigDto
    {
        public string WidgetLayout { get; set; }
        public Dictionary<string, object> Settings { get; set; }
        public List<string> EnabledWidgets { get; set; }
        public int RefreshInterval { get; set; }
    }

    public class SystemEventDto
    {
        public Guid Id { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class UserStorageUsageDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public long UsedStorage { get; set; }
        public long TotalStorage { get; set; }
        public int FileCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class UnusedFilesDto
    {
        public List<FileInfo> Files { get; set; }
        public long TotalSize { get; set; }
        public int TotalCount { get; set; }
        
        public class FileInfo
        {
            public Guid Id { get; set; }
            public string FileName { get; set; }
            public long Size { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Path { get; set; }
        }
    }

    public class DirectoryUsageDto
    {
        public string Path { get; set; }
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public int SubdirectoryCount { get; set; }
        public List<DirectoryUsageDto> Subdirectories { get; set; }
    }

    public class Configuration
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TotalPosts { get; set; }
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public int TotalViews { get; set; }
        public double AvgResponseTime { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class BasicSystemMetricsDto
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public long DiskUsage { get; set; }
        public int ActiveConnections { get; set; }
        public double RequestsPerSecond { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TimeSeriesMetricsDto
    {
        public List<MetricPoint> DataPoints { get; set; }
        public string MetricName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public class MetricPoint
        {
            public DateTime Timestamp { get; set; }
            public double Value { get; set; }
        }
    }

    public class LargestFilesDto
    {
        public List<FileInfo> Files { get; set; }
        public long TotalSize { get; set; }
        
        public class FileInfo
        {
            public Guid Id { get; set; }
            public string FileName { get; set; }
            public long Size { get; set; }
            public string Path { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }

    public class RecentUploadDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public long Size { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public string FileType { get; set; }
    }

    public class FileTypeDistributionDto
    {
        public Dictionary<string, int> Distribution { get; set; }
        public int TotalFiles { get; set; }
    }

    public class DirectoryDistributionDto
    {
        public Dictionary<string, long> Distribution { get; set; }
        public long TotalSize { get; set; }
    }

    public class MonthlyUploadTrendDto
    {
        public List<MonthData> Data { get; set; }
        
        public class MonthData
        {
            public string Month { get; set; }
            public int FileCount { get; set; }
            public long TotalSize { get; set; }
        }
    }



    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PerformanceMetrics
    {
        public double CpuUsagePercentage { get; set; }
        public double MemoryUsagePercentage { get; set; }
        public long AvailableMemoryMB { get; set; }
        public double DiskUsagePercentage { get; set; }
        public long NetworkBytesReceived { get; set; }
        public long NetworkBytesSent { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}