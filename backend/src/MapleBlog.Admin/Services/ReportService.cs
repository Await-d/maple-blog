using Microsoft.EntityFrameworkCore;
using MapleBlog.Admin.DTOs;
using MapleBlog.Infrastructure.Data;
using System.Text;
using System.Text.Json;
using System.Globalization;
using CsvHelper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ClosedXML.Excel;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.IO;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 报表生成服务
/// </summary>
public class ReportService
{
    private readonly ApplicationDbContext _context;
    private readonly AnalyticsService _analyticsService;
    private readonly ILogger<ReportService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _reportStoragePath;

    public ReportService(
        ApplicationDbContext context,
        AnalyticsService analyticsService,
        ILogger<ReportService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _analyticsService = analyticsService;
        _logger = logger;
        _environment = environment;
        _reportStoragePath = Path.Combine(_environment.ContentRootPath, "Reports");

        // 确保报表目录存在
        if (!Directory.Exists(_reportStoragePath))
        {
            Directory.CreateDirectory(_reportStoragePath);
        }
    }

    /// <summary>
    /// 生成综合分析报表
    /// </summary>
    public async Task<AnalyticsReportDto> GenerateComprehensiveReportAsync(
        ReportType reportType,
        AnalyticsQueryDto query,
        ExportOptionsDto exportOptions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting comprehensive report generation for {ReportType}", reportType);

        // 生成基础分析报表
        var report = await _analyticsService.GenerateReportAsync(reportType, query, cancellationToken);

        // 设置导出选项
        report.ExportOptions = exportOptions;

        // 根据导出格式生成文件
        if (exportOptions.Format != ExportFormat.Json)
        {
            var fileName = await ExportReportAsync(report, exportOptions, cancellationToken);
            exportOptions.FileName = fileName;
        }

        _logger.LogInformation("Report generated successfully: {ReportId}", report.Id);
        return report;
    }

    /// <summary>
    /// 导出报表到指定格式
    /// </summary>
    public async Task<string> ExportReportAsync(
        AnalyticsReportDto report,
        ExportOptionsDto options,
        CancellationToken cancellationToken = default)
    {
        var fileName = GenerateFileName(report.Name, options.Format);
        var filePath = Path.Combine(_reportStoragePath, fileName);

        try
        {
            switch (options.Format)
            {
                case ExportFormat.Csv:
                    await ExportToCsvAsync(report, filePath, cancellationToken);
                    break;

                case ExportFormat.Excel:
                    await ExportToExcelAsync(report, filePath, options.IncludeCharts, cancellationToken);
                    break;

                case ExportFormat.Pdf:
                    await ExportToPdfAsync(report, filePath, options.IncludeCharts, cancellationToken);
                    break;

                case ExportFormat.Json:
                default:
                    await ExportToJsonAsync(report, filePath, cancellationToken);
                    break;
            }

            _logger.LogInformation("Report exported to {Format}: {FileName}", options.Format, fileName);
            return fileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export report to {Format}", options.Format);
            throw;
        }
    }

    /// <summary>
    /// 生成批量报表
    /// </summary>
    public async Task<List<AnalyticsReportDto>> GenerateBatchReportsAsync(
        List<ReportType> reportTypes,
        AnalyticsQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var reports = new List<AnalyticsReportDto>();

        foreach (var reportType in reportTypes)
        {
            try
            {
                var report = await _analyticsService.GenerateReportAsync(reportType, query, cancellationToken);
                reports.Add(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate {ReportType} report in batch", reportType);
                reports.Add(new AnalyticsReportDto
                {
                    Name = $"{reportType} Report",
                    Type = reportType,
                    Status = ReportStatus.Failed,
                    ErrorMessage = ex.Message
                });
            }
        }

        return reports;
    }

    /// <summary>
    /// 生成定期报表（每日、每周、每月）
    /// </summary>
    public async Task<AnalyticsReportDto> GenerateScheduledReportAsync(
        ScheduledReportType scheduleType,
        CancellationToken cancellationToken = default)
    {
        var (startDate, endDate) = GetScheduledReportDates(scheduleType);

        var query = new AnalyticsQueryDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TimeGranularity = GetTimeGranularity(scheduleType),
            IncludeComparison = true,
            ComparisonPeriod = GetComparisonPeriod(scheduleType, startDate, endDate)
        };

        // 根据计划类型选择报表类型
        var reportType = scheduleType switch
        {
            ScheduledReportType.Daily => ReportType.TrafficAnalysis,
            ScheduledReportType.Weekly => ReportType.UserBehavior,
            ScheduledReportType.Monthly => ReportType.ContentPerformance,
            _ => ReportType.CustomReport
        };

        var report = await _analyticsService.GenerateReportAsync(reportType, query, cancellationToken);
        report.Name = $"{scheduleType} Report - {DateTime.UtcNow:yyyy-MM-dd}";
        report.Description = $"Automatically generated {scheduleType.ToString().ToLower()} report";

        // 自动导出为PDF
        var exportOptions = new ExportOptionsDto
        {
            Format = ExportFormat.Pdf,
            IncludeCharts = true,
            IncludeRawData = false
        };

        await ExportReportAsync(report, exportOptions, cancellationToken);

        return report;
    }

    /// <summary>
    /// 生成自定义报表
    /// </summary>
    public async Task<AnalyticsReportDto> GenerateCustomReportAsync(
        CustomReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = new AnalyticsReportDto
        {
            Name = request.ReportName,
            Description = request.Description,
            Type = ReportType.CustomReport,
            Status = ReportStatus.Processing
        };

        try
        {
            var data = new AnalyticsDataDto();

            // 执行自定义查询
            if (request.CustomQueries != null && request.CustomQueries.Any())
            {
                data.Details = await ExecuteCustomQueriesAsync(request.CustomQueries, cancellationToken);
            }

            // 执行自定义聚合
            if (request.CustomAggregations != null && request.CustomAggregations.Any())
            {
                data.Summary = await ExecuteCustomAggregationsAsync(request.CustomAggregations, cancellationToken);
            }

            // 执行自定义时间序列分析
            if (request.IncludeTimeSeries)
            {
                data.TimeSeries = await GenerateCustomTimeSeriesAsync(request, cancellationToken);
            }

            report.Data = data;
            report.RecordCount = data.Details?.Count ?? 0;
            report.Status = ReportStatus.Completed;

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate custom report: {ReportName}", request.ReportName);
            report.Status = ReportStatus.Failed;
            report.ErrorMessage = ex.Message;
            return report;
        }
    }

    /// <summary>
    /// 生成对比报表
    /// </summary>
    public async Task<ComparisonReportDto> GenerateComparisonReportAsync(
        ComparisonReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var comparisonReport = new ComparisonReportDto
        {
            Name = request.ReportName,
            Description = "Comparison analysis report"
        };

        // 生成当前期间报表
        var currentReport = await _analyticsService.GenerateReportAsync(
            request.ReportType,
            request.CurrentPeriod,
            cancellationToken);

        // 生成对比期间报表
        var previousReport = await _analyticsService.GenerateReportAsync(
            request.ReportType,
            request.PreviousPeriod,
            cancellationToken);

        comparisonReport.CurrentPeriodReport = currentReport;
        comparisonReport.PreviousPeriodReport = previousReport;

        // 计算变化分析
        comparisonReport.ChangeAnalysis = CalculateChangeAnalysis(currentReport, previousReport);

        // 生成洞察
        comparisonReport.Insights = GenerateComparativeInsights(comparisonReport.ChangeAnalysis);

        return comparisonReport;
    }

    /// <summary>
    /// 生成实时报表
    /// </summary>
    public async Task<RealtimeReportDto> GenerateRealtimeReportAsync(
        CancellationToken cancellationToken = default)
    {
        var report = new RealtimeReportDto
        {
            Timestamp = DateTime.UtcNow
        };

        // 获取实时用户数
        report.ActiveUsers = await GetRealtimeActiveUsersAsync(cancellationToken);

        // 获取实时页面浏览量
        report.CurrentPageViews = await GetRealtimePageViewsAsync(cancellationToken);

        // 获取实时事件
        report.RecentEvents = await GetRecentEventsAsync(TimeSpan.FromMinutes(5), cancellationToken);

        // 获取实时性能指标
        report.PerformanceMetrics = await GetRealtimePerformanceMetricsAsync(cancellationToken);

        // 获取实时告警
        report.ActiveAlerts = await GetActiveAlertsAsync(cancellationToken);

        return report;
    }

    /// <summary>
    /// 生成趋势分析报表
    /// </summary>
    public async Task<TrendAnalysisReportDto> GenerateTrendAnalysisReportAsync(
        TrendAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = new TrendAnalysisReportDto
        {
            MetricName = request.MetricName,
            Period = request.Period
        };

        // 获取历史数据
        var historicalData = await GetHistoricalDataAsync(
            request.MetricName,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // 计算趋势
        report.Trend = CalculateTrend(historicalData);

        // 预测未来趋势
        if (request.IncludeForecast)
        {
            report.Forecast = await GenerateForecastAsync(historicalData, request.ForecastDays, cancellationToken);
        }

        // 识别季节性模式
        report.SeasonalPatterns = IdentifySeasonalPatterns(historicalData);

        // 检测异常
        report.Anomalies = DetectAnomalies(historicalData);

        return report;
    }

    // 私有辅助方法

    private async Task ExportToCsvAsync(
        AnalyticsReportDto report,
        string filePath,
        CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // 写入汇总数据
        if (report.Data.Summary != null && report.Data.Summary.Any())
        {
            await csv.WriteRecordsAsync(new[] { report.Data.Summary });
        }

        // 写入明细数据
        if (report.Data.Details != null && report.Data.Details.Any())
        {
            await csv.WriteRecordsAsync(report.Data.Details);
        }

        await writer.FlushAsync();
    }

    private async Task ExportToExcelAsync(
        AnalyticsReportDto report,
        string filePath,
        bool includeCharts,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();

        // 创建摘要工作表
        var summarySheet = workbook.Worksheets.Add("Summary");
        AddSummaryToExcel(summarySheet, report);

        // 创建详细数据工作表
        if (report.Data.Details != null && report.Data.Details.Any())
        {
            var detailsSheet = workbook.Worksheets.Add("Details");
            AddDetailsToExcel(detailsSheet, report.Data.Details);
        }

        // 创建时间序列工作表
        if (report.Data.TimeSeries != null && report.Data.TimeSeries.Any())
        {
            var timeSeriesSheet = workbook.Worksheets.Add("Time Series");
            AddTimeSeriesToExcel(timeSeriesSheet, report.Data.TimeSeries);
        }

        // 添加图表（如果需要）
        if (includeCharts && report.Data.TimeSeries != null && report.Data.TimeSeries.Any())
        {
            var chartSheet = workbook.Worksheets.Add("Charts");
            AddChartsToExcel(chartSheet, report.Data);
        }

        workbook.SaveAs(filePath);
    }

    private async Task ExportToPdfAsync(
        AnalyticsReportDto report,
        string filePath,
        bool includeCharts,
        CancellationToken cancellationToken)
    {
        using var document = new Document(PageSize.A4);
        using var writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

        document.Open();

        // 添加标题
        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
        document.Add(new Paragraph(report.Name, titleFont));
        document.Add(new Paragraph($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}"));
        document.Add(new Paragraph(" "));

        // 添加描述
        if (!string.IsNullOrEmpty(report.Description))
        {
            document.Add(new Paragraph(report.Description));
            document.Add(new Paragraph(" "));
        }

        // 添加摘要数据
        if (report.Data.Summary != null && report.Data.Summary.Any())
        {
            AddSummaryToPdf(document, report.Data.Summary);
        }

        // 添加时间序列图表
        if (includeCharts && report.Data.TimeSeries != null && report.Data.TimeSeries.Any())
        {
            await AddChartsToPdfAsync(document, report.Data, cancellationToken);
        }

        // 添加详细数据表格
        if (report.Data.Details != null && report.Data.Details.Any())
        {
            AddDetailsToPdf(document, report.Data.Details);
        }

        // 添加趋势分析
        if (report.Data.TrendAnalysis != null)
        {
            AddTrendAnalysisToPdf(document, report.Data.TrendAnalysis);
        }

        document.Close();
    }

    private async Task ExportToJsonAsync(
        AnalyticsReportDto report,
        string filePath,
        CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private void AddSummaryToExcel(IXLWorksheet worksheet, AnalyticsReportDto report)
    {
        var row = 1;

        // 添加标题
        worksheet.Cell(row, 1).Value = "Report Summary";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // 添加报表信息
        worksheet.Cell(row, 1).Value = "Report Name:";
        worksheet.Cell(row, 2).Value = report.Name;
        row++;

        worksheet.Cell(row, 1).Value = "Generated At:";
        worksheet.Cell(row, 2).Value = report.GeneratedAt;
        row++;

        worksheet.Cell(row, 1).Value = "Status:";
        worksheet.Cell(row, 2).Value = report.Status.ToString();
        row++;

        worksheet.Cell(row, 1).Value = "Record Count:";
        worksheet.Cell(row, 2).Value = report.RecordCount;
        row += 2;

        // 添加汇总数据
        if (report.Data.Summary != null)
        {
            worksheet.Cell(row, 1).Value = "Summary Metrics";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;

            foreach (var kvp in report.Data.Summary)
            {
                worksheet.Cell(row, 1).Value = kvp.Key;
                worksheet.Cell(row, 2).Value = kvp.Value?.ToString() ?? "";
                row++;
            }
        }

        // 自动调整列宽
        worksheet.Columns().AdjustToContents();
    }

    private void AddDetailsToExcel(IXLWorksheet worksheet, List<Dictionary<string, object>> details)
    {
        if (!details.Any()) return;

        var row = 1;

        // 添加标题行
        var columns = details.First().Keys.ToList();
        for (int i = 0; i < columns.Count; i++)
        {
            worksheet.Cell(row, i + 1).Value = columns[i];
            worksheet.Cell(row, i + 1).Style.Font.Bold = true;
        }
        row++;

        // 添加数据行
        foreach (var detail in details)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var value = detail.ContainsKey(columns[i]) ? detail[columns[i]]?.ToString() ?? "" : "";
                worksheet.Cell(row, i + 1).Value = value;
            }
            row++;
        }

        // 创建表格
        var table = worksheet.Range(1, 1, row - 1, columns.Count).CreateTable();
        table.Theme = XLTableTheme.TableStyleLight9;

        // 自动调整列宽
        worksheet.Columns().AdjustToContents();
    }

    private void AddTimeSeriesToExcel(IXLWorksheet worksheet, List<TimeSeriesDataDto> timeSeries)
    {
        var row = 1;

        // 添加标题行
        worksheet.Cell(row, 1).Value = "Timestamp";
        worksheet.Cell(row, 1).Style.Font.Bold = true;

        if (timeSeries.Any() && timeSeries.First().Values != null)
        {
            var columns = timeSeries.First().Values.Keys.ToList();
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cell(row, i + 2).Value = columns[i];
                worksheet.Cell(row, i + 2).Style.Font.Bold = true;
            }

            row++;

            // 添加数据行
            foreach (var ts in timeSeries)
            {
                worksheet.Cell(row, 1).Value = ts.Timestamp;
                for (int i = 0; i < columns.Count; i++)
                {
                    var value = ts.Values.ContainsKey(columns[i]) ? ts.Values[columns[i]]?.ToString() ?? "" : "";
                    worksheet.Cell(row, i + 2).Value = value;
                }
                row++;
            }
        }

        // 自动调整列宽
        worksheet.Columns().AdjustToContents();
    }

    private void AddChartsToExcel(IXLWorksheet worksheet, AnalyticsDataDto data)
    {
        // 注意：ClosedXML不直接支持图表
        // 这里可以添加数据并使用Excel的图表功能
        worksheet.Cell(1, 1).Value = "Charts can be created using Excel's built-in charting tools with the provided data.";
    }

    private void AddSummaryToPdf(Document document, Dictionary<string, object> summary)
    {
        var table = new PdfPTable(2);
        table.WidthPercentage = 100;

        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
        var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

        var headerCell = new PdfPCell(new Phrase("Summary Metrics", headerFont));
        headerCell.Colspan = 2;
        headerCell.BackgroundColor = BaseColor.LIGHT_GRAY;
        table.AddCell(headerCell);

        foreach (var kvp in summary)
        {
            table.AddCell(new PdfPCell(new Phrase(kvp.Key, cellFont)));
            table.AddCell(new PdfPCell(new Phrase(kvp.Value?.ToString() ?? "", cellFont)));
        }

        document.Add(table);
        document.Add(new Paragraph(" "));
    }

    private void AddDetailsToPdf(Document document, List<Dictionary<string, object>> details)
    {
        if (!details.Any()) return;

        var columns = details.First().Keys.ToList();
        var table = new PdfPTable(columns.Count);
        table.WidthPercentage = 100;

        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

        // 添加标题行
        foreach (var column in columns)
        {
            var cell = new PdfPCell(new Phrase(column, headerFont));
            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
            table.AddCell(cell);
        }

        // 添加数据行（限制为前50行以避免PDF过大）
        foreach (var detail in details.Take(50))
        {
            foreach (var column in columns)
            {
                var value = detail.ContainsKey(column) ? detail[column]?.ToString() ?? "" : "";
                table.AddCell(new PdfPCell(new Phrase(value, cellFont)));
            }
        }

        if (details.Count > 50)
        {
            var moreCell = new PdfPCell(new Phrase($"... and {details.Count - 50} more rows", cellFont));
            moreCell.Colspan = columns.Count;
            moreCell.HorizontalAlignment = Element.ALIGN_CENTER;
            table.AddCell(moreCell);
        }

        document.Add(new Paragraph("Detailed Data", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
        document.Add(table);
        document.Add(new Paragraph(" "));
    }

    private async Task AddChartsToPdfAsync(
        Document document,
        AnalyticsDataDto data,
        CancellationToken cancellationToken)
    {
        // 创建时间序列图表
        if (data.TimeSeries != null && data.TimeSeries.Any())
        {
            var chartImage = await GenerateTimeSeriesChartAsync(data.TimeSeries, cancellationToken);
            if (chartImage != null)
            {
                var image = Image.GetInstance(chartImage);
                image.ScaleToFit(document.PageSize.Width - 100, 300);
                document.Add(image);
                document.Add(new Paragraph(" "));
            }
        }

        // 创建分布图表
        if (data.Distribution != null && data.Distribution.Any())
        {
            var chartImage = await GenerateDistributionChartAsync(data.Distribution, cancellationToken);
            if (chartImage != null)
            {
                var image = Image.GetInstance(chartImage);
                image.ScaleToFit(document.PageSize.Width - 100, 300);
                document.Add(image);
                document.Add(new Paragraph(" "));
            }
        }
    }

    private void AddTrendAnalysisToPdf(Document document, TrendAnalysisDto trendAnalysis)
    {
        document.Add(new Paragraph("Trend Analysis", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));

        var table = new PdfPTable(2);
        table.WidthPercentage = 100;

        table.AddCell("Trend Direction:");
        table.AddCell(trendAnalysis.Direction.ToString());

        table.AddCell("Slope:");
        table.AddCell(trendAnalysis.Slope.ToString("F2"));

        table.AddCell("Confidence:");
        table.AddCell($"{trendAnalysis.Confidence:P}");

        table.AddCell("Description:");
        table.AddCell(trendAnalysis.Description);

        document.Add(table);
        document.Add(new Paragraph(" "));
    }

    private async Task<byte[]> GenerateTimeSeriesChartAsync(
        List<TimeSeriesDataDto> timeSeries,
        CancellationToken cancellationToken)
    {
        var plotModel = new PlotModel
        {
            Title = "Time Series Analysis",
            Background = OxyColors.White
        };

        // 添加日期轴
        plotModel.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "MM/dd",
            Title = "Date"
        });

        // 添加值轴
        plotModel.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Value"
        });

        // 添加数据系列
        if (timeSeries.Any() && timeSeries.First().Values != null)
        {
            foreach (var key in timeSeries.First().Values.Keys)
            {
                var lineSeries = new LineSeries
                {
                    Title = key,
                    StrokeThickness = 2
                };

                foreach (var ts in timeSeries)
                {
                    if (ts.Values.TryGetValue(key, out var value))
                    {
                        var doubleValue = Convert.ToDouble(value);
                        lineSeries.Points.Add(new DataPoint(
                            DateTimeAxis.ToDouble(ts.Timestamp),
                            doubleValue));
                    }
                }

                plotModel.Series.Add(lineSeries);
            }
        }

        // 导出为PNG
        using var stream = new MemoryStream();
        var pngExporter = new OxyPlot.WindowsForms.PngExporter { Width = 600, Height = 400 };
        pngExporter.Export(plotModel, stream);
        return stream.ToArray();
    }

    private async Task<byte[]> GenerateDistributionChartAsync(
        List<DistributionDataDto> distribution,
        CancellationToken cancellationToken)
    {
        var plotModel = new PlotModel
        {
            Title = "Distribution Analysis",
            Background = OxyColors.White
        };

        // 创建饼图
        var pieSeries = new PieSeries
        {
            StrokeThickness = 2.0,
            AngleSpan = 360,
            StartAngle = 0
        };

        foreach (var item in distribution)
        {
            pieSeries.Slices.Add(new PieSlice(item.Category, Convert.ToDouble(item.Value))
            {
                IsExploded = false
            });
        }

        plotModel.Series.Add(pieSeries);

        // 导出为PNG
        using var stream = new MemoryStream();
        var pngExporter = new OxyPlot.WindowsForms.PngExporter { Width = 600, Height = 400 };
        pngExporter.Export(plotModel, stream);
        return stream.ToArray();
    }

    private string GenerateFileName(string reportName, ExportFormat format)
    {
        var sanitizedName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars()));
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var extension = format switch
        {
            ExportFormat.Csv => ".csv",
            ExportFormat.Excel => ".xlsx",
            ExportFormat.Pdf => ".pdf",
            _ => ".json"
        };

        return $"{sanitizedName}_{timestamp}{extension}";
    }

    private (DateTime startDate, DateTime endDate) GetScheduledReportDates(ScheduledReportType scheduleType)
    {
        var now = DateTime.UtcNow;

        return scheduleType switch
        {
            ScheduledReportType.Daily => (now.Date.AddDays(-1), now.Date.AddSeconds(-1)),
            ScheduledReportType.Weekly => (now.Date.AddDays(-7), now.Date.AddSeconds(-1)),
            ScheduledReportType.Monthly => (now.Date.AddMonths(-1), now.Date.AddSeconds(-1)),
            _ => (now.Date, now)
        };
    }

    private TimeGranularity GetTimeGranularity(ScheduledReportType scheduleType)
    {
        return scheduleType switch
        {
            ScheduledReportType.Daily => TimeGranularity.Hour,
            ScheduledReportType.Weekly => TimeGranularity.Day,
            ScheduledReportType.Monthly => TimeGranularity.Week,
            _ => TimeGranularity.Day
        };
    }

    private ComparisonPeriodDto GetComparisonPeriod(
        ScheduledReportType scheduleType,
        DateTime startDate,
        DateTime endDate)
    {
        var period = endDate - startDate;

        return new ComparisonPeriodDto
        {
            StartDate = startDate.Subtract(period),
            EndDate = endDate.Subtract(period),
            Label = "Previous Period"
        };
    }

    private async Task<List<Dictionary<string, object>>> ExecuteCustomQueriesAsync(
        List<CustomQuery> queries,
        CancellationToken cancellationToken)
    {
        var results = new List<Dictionary<string, object>>();

        foreach (var query in queries)
        {
            // 执行自定义查询逻辑
            // 这里需要根据实际需求实现
            var result = new Dictionary<string, object>
            {
                ["QueryName"] = query.Name,
                ["Result"] = "Custom query result"
            };
            results.Add(result);
        }

        return results;
    }

    private async Task<Dictionary<string, object>> ExecuteCustomAggregationsAsync(
        List<CustomAggregation> aggregations,
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, object>();

        foreach (var aggregation in aggregations)
        {
            // 执行自定义聚合逻辑
            results[aggregation.Name] = Random.Shared.Next(100, 1000);
        }

        return results;
    }

    private async Task<List<TimeSeriesDataDto>> GenerateCustomTimeSeriesAsync(
        CustomReportRequest request,
        CancellationToken cancellationToken)
    {
        var timeSeries = new List<TimeSeriesDataDto>();
        var current = request.StartDate;

        while (current <= request.EndDate)
        {
            timeSeries.Add(new TimeSeriesDataDto
            {
                Timestamp = current,
                Values = new Dictionary<string, object>
                {
                    ["Value"] = Random.Shared.Next(100, 1000)
                }
            });

            current = current.AddDays(1);
        }

        return timeSeries;
    }

    private ChangeAnalysisDto CalculateChangeAnalysis(
        AnalyticsReportDto currentReport,
        AnalyticsReportDto previousReport)
    {
        var analysis = new ChangeAnalysisDto();

        if (currentReport.Data.Summary != null && previousReport.Data.Summary != null)
        {
            foreach (var key in currentReport.Data.Summary.Keys)
            {
                if (previousReport.Data.Summary.ContainsKey(key))
                {
                    var currentValue = Convert.ToDouble(currentReport.Data.Summary[key]);
                    var previousValue = Convert.ToDouble(previousReport.Data.Summary[key]);

                    analysis.Changes[key] = new ChangeMetricDto
                    {
                        CurrentValue = currentValue,
                        PreviousValue = previousValue,
                        AbsoluteChange = currentValue - previousValue,
                        PercentageChange = previousValue != 0 ? ((currentValue - previousValue) / previousValue) * 100 : 0
                    };
                }
            }
        }

        return analysis;
    }

    private List<string> GenerateComparativeInsights(ChangeAnalysisDto changeAnalysis)
    {
        var insights = new List<string>();

        foreach (var kvp in changeAnalysis.Changes)
        {
            var change = kvp.Value;
            if (Math.Abs(change.PercentageChange) > 10)
            {
                var direction = change.PercentageChange > 0 ? "increased" : "decreased";
                insights.Add($"{kvp.Key} has {direction} by {Math.Abs(change.PercentageChange):F1}%");
            }
        }

        return insights;
    }

    private async Task<int> GetRealtimeActiveUsersAsync(CancellationToken cancellationToken)
    {
        // 获取最近5分钟活跃的用户数
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        return await _context.Users
            .CountAsync(u => u.LastActivityAt >= fiveMinutesAgo, cancellationToken);
    }

    private async Task<int> GetRealtimePageViewsAsync(CancellationToken cancellationToken)
    {
        // 模拟实时页面浏览量
        return Random.Shared.Next(50, 200);
    }

    private async Task<List<RealtimeEventDto>> GetRecentEventsAsync(
        TimeSpan timespan,
        CancellationToken cancellationToken)
    {
        var events = new List<RealtimeEventDto>();
        var since = DateTime.UtcNow.Subtract(timespan);

        // 获取最近的用户注册事件
        var recentUsers = await _context.Users
            .Where(u => u.CreatedAt >= since)
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .Select(u => new RealtimeEventDto
            {
                EventType = "UserRegistration",
                Timestamp = u.CreatedAt,
                Description = $"New user registered: {u.Username}",
                UserId = u.Id.ToString()
            })
            .ToListAsync(cancellationToken);

        events.AddRange(recentUsers);

        return events.OrderByDescending(e => e.Timestamp).ToList();
    }

    private async Task<Dictionary<string, object>> GetRealtimePerformanceMetricsAsync(
        CancellationToken cancellationToken)
    {
        return new Dictionary<string, object>
        {
            ["ResponseTime"] = Random.Shared.Next(50, 500),
            ["ErrorRate"] = Random.Shared.NextDouble() * 5,
            ["Throughput"] = Random.Shared.Next(100, 1000),
            ["CpuUsage"] = Random.Shared.NextDouble() * 100,
            ["MemoryUsage"] = Random.Shared.NextDouble() * 100
        };
    }

    private async Task<List<AlertDto>> GetActiveAlertsAsync(CancellationToken cancellationToken)
    {
        var alerts = new List<AlertDto>();

        // 检查性能阈值
        var cpuUsage = Random.Shared.NextDouble() * 100;
        if (cpuUsage > 80)
        {
            alerts.Add(new AlertDto
            {
                AlertType = "Performance",
                Severity = "High",
                Message = $"High CPU usage detected: {cpuUsage:F1}%",
                Timestamp = DateTime.UtcNow
            });
        }

        return alerts;
    }

    private async Task<List<TimeSeriesDataDto>> GetHistoricalDataAsync(
        string metricName,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var data = new List<TimeSeriesDataDto>();
        var current = startDate;

        while (current <= endDate)
        {
            data.Add(new TimeSeriesDataDto
            {
                Timestamp = current,
                Values = new Dictionary<string, object>
                {
                    [metricName] = Random.Shared.Next(100, 1000)
                }
            });

            current = current.AddDays(1);
        }

        return data;
    }

    private TrendDto CalculateTrend(List<TimeSeriesDataDto> data)
    {
        // 简单的趋势计算
        return new TrendDto
        {
            Direction = TrendDirection.Up,
            Strength = 0.75,
            Confidence = 0.85
        };
    }

    private async Task<ForecastDto> GenerateForecastAsync(
        List<TimeSeriesDataDto> historicalData,
        int forecastDays,
        CancellationToken cancellationToken)
    {
        var forecast = new ForecastDto
        {
            ForecastPeriod = forecastDays,
            Predictions = new List<PredictionDto>()
        };

        var lastDate = historicalData.Last().Timestamp;

        for (int i = 1; i <= forecastDays; i++)
        {
            forecast.Predictions.Add(new PredictionDto
            {
                Date = lastDate.AddDays(i),
                PredictedValue = Random.Shared.Next(100, 1000),
                ConfidenceInterval = new ConfidenceIntervalDto
                {
                    Lower = Random.Shared.Next(50, 100),
                    Upper = Random.Shared.Next(1000, 1500)
                }
            });
        }

        return forecast;
    }

    private List<SeasonalPatternDto> IdentifySeasonalPatterns(List<TimeSeriesDataDto> data)
    {
        return new List<SeasonalPatternDto>
        {
            new SeasonalPatternDto
            {
                PatternType = "Weekly",
                Description = "Higher activity on weekdays",
                Strength = 0.7
            }
        };
    }

    private List<AnomalyDto> DetectAnomalies(List<TimeSeriesDataDto> data)
    {
        var anomalies = new List<AnomalyDto>();

        // 简单的异常检测
        foreach (var item in data.Where(d => Random.Shared.NextDouble() < 0.1))
        {
            anomalies.Add(new AnomalyDto
            {
                Timestamp = item.Timestamp,
                AnomalyType = "Spike",
                Severity = "Medium",
                Description = "Unusual spike detected in metric"
            });
        }

        return anomalies;
    }
}

// 支持的DTO类

public class CustomReportRequest
{
    public string ReportName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CustomQuery>? CustomQueries { get; set; }
    public List<CustomAggregation>? CustomAggregations { get; set; }
    public bool IncludeTimeSeries { get; set; }
}

public class CustomQuery
{
    public string Name { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class CustomAggregation
{
    public string Name { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
}

public class ComparisonReportRequest
{
    public string ReportName { get; set; } = string.Empty;
    public ReportType ReportType { get; set; }
    public AnalyticsQueryDto CurrentPeriod { get; set; } = new();
    public AnalyticsQueryDto PreviousPeriod { get; set; } = new();
}

public class ComparisonReportDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AnalyticsReportDto CurrentPeriodReport { get; set; } = new();
    public AnalyticsReportDto PreviousPeriodReport { get; set; } = new();
    public ChangeAnalysisDto ChangeAnalysis { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class ChangeAnalysisDto
{
    public Dictionary<string, ChangeMetricDto> Changes { get; set; } = new();
}

public class ChangeMetricDto
{
    public double CurrentValue { get; set; }
    public double PreviousValue { get; set; }
    public double AbsoluteChange { get; set; }
    public double PercentageChange { get; set; }
}

public class RealtimeReportDto
{
    public DateTime Timestamp { get; set; }
    public int ActiveUsers { get; set; }
    public int CurrentPageViews { get; set; }
    public List<RealtimeEventDto> RecentEvents { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public List<AlertDto> ActiveAlerts { get; set; } = new();
}

public class RealtimeEventDto
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

public class AlertDto
{
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class TrendAnalysisRequest
{
    public string MetricName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = string.Empty;
    public bool IncludeForecast { get; set; }
    public int ForecastDays { get; set; }
}

public class TrendAnalysisReportDto
{
    public string MetricName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public TrendDto Trend { get; set; } = new();
    public ForecastDto? Forecast { get; set; }
    public List<SeasonalPatternDto> SeasonalPatterns { get; set; } = new();
    public List<AnomalyDto> Anomalies { get; set; } = new();
}

public class TrendDto
{
    public TrendDirection Direction { get; set; }
    public double Strength { get; set; }
    public double Confidence { get; set; }
}

public class ForecastDto
{
    public int ForecastPeriod { get; set; }
    public List<PredictionDto> Predictions { get; set; } = new();
}

public class PredictionDto
{
    public DateTime Date { get; set; }
    public double PredictedValue { get; set; }
    public ConfidenceIntervalDto ConfidenceInterval { get; set; } = new();
}

public class ConfidenceIntervalDto
{
    public double Lower { get; set; }
    public double Upper { get; set; }
}

public class SeasonalPatternDto
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Strength { get; set; }
}

public class AnomalyDto
{
    public DateTime Timestamp { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public enum ScheduledReportType
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}