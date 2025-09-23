using System;
using System.Collections.Generic;

namespace MapleBlog.Application.DTOs.Admin
{
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
}