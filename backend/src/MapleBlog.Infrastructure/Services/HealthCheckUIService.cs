using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 健康检查UI服务，提供简单的HTML界面查看健康状态
/// </summary>
public static class HealthCheckUIService
{
    /// <summary>
    /// 生成健康检查仪表板HTML
    /// </summary>
    public static async Task WriteHealthCheckUI(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/html; charset=utf-8";

        var html = GenerateHealthCheckHTML(report);
        await context.Response.WriteAsync(html);
    }

    private static string GenerateHealthCheckHTML(HealthReport report)
    {
        var overallStatus = report.Status.ToString();
        var statusColor = GetStatusColor(report.Status);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("    <title>Maple Blog - Health Check Dashboard</title>");
        html.AppendLine("    <style>");
        html.AppendLine(GetCSS());
        html.AppendLine("    </style>");
        html.AppendLine("    <script>");
        html.AppendLine(GetJavaScript());
        html.AppendLine("    </script>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        html.AppendLine("        <header>");
        html.AppendLine("            <h1>🍁 Maple Blog Health Dashboard</h1>");
        html.AppendLine($"            <div class=\"overall-status {statusColor.ToLower()}\">");
        html.AppendLine($"                <span class=\"status-indicator\"></span>");
        html.AppendLine($"                <span>Overall Status: {overallStatus}</span>");
        html.AppendLine("            </div>");
        html.AppendLine($"            <div class=\"timestamp\">Last Updated: {timestamp}</div>");
        html.AppendLine("        </header>");

        html.AppendLine("        <div class=\"stats\">");
        html.AppendLine($"            <div class=\"stat-card\">");
        html.AppendLine($"                <h3>Total Duration</h3>");
        html.AppendLine($"                <span class=\"stat-value\">{report.TotalDuration.TotalMilliseconds:F2}ms</span>");
        html.AppendLine("            </div>");
        html.AppendLine($"            <div class=\"stat-card\">");
        html.AppendLine($"                <h3>Checks</h3>");
        html.AppendLine($"                <span class=\"stat-value\">{report.Entries.Count}</span>");
        html.AppendLine("            </div>");
        html.AppendLine($"            <div class=\"stat-card\">");
        html.AppendLine($"                <h3>Healthy</h3>");
        html.AppendLine($"                <span class=\"stat-value\">{report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy)}</span>");
        html.AppendLine("            </div>");
        html.AppendLine($"            <div class=\"stat-card\">");
        html.AppendLine($"                <h3>Degraded</h3>");
        html.AppendLine($"                <span class=\"stat-value\">{report.Entries.Count(e => e.Value.Status == HealthStatus.Degraded)}</span>");
        html.AppendLine("            </div>");
        html.AppendLine($"            <div class=\"stat-card\">");
        html.AppendLine($"                <h3>Unhealthy</h3>");
        html.AppendLine($"                <span class=\"stat-value\">{report.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)}</span>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // Filter buttons
        html.AppendLine("        <div class=\"filters\">");
        html.AppendLine("            <button class=\"filter-btn active\" onclick=\"filterChecks('all')\">All</button>");
        html.AppendLine("            <button class=\"filter-btn\" onclick=\"filterChecks('healthy')\">Healthy</button>");
        html.AppendLine("            <button class=\"filter-btn\" onclick=\"filterChecks('degraded')\">Degraded</button>");
        html.AppendLine("            <button class=\"filter-btn\" onclick=\"filterChecks('unhealthy')\">Unhealthy</button>");
        html.AppendLine("            <button class=\"filter-btn\" onclick=\"filterChecks('redis')\">Redis</button>");
        html.AppendLine("            <button class=\"filter-btn\" onclick=\"filterChecks('database')\">Database</button>");
        html.AppendLine("        </div>");

        html.AppendLine("        <div class=\"health-checks\">");

        foreach (var entry in report.Entries.OrderBy(e => e.Key))
        {
            var checkStatus = entry.Value.Status.ToString();
            var checkStatusColor = GetStatusColor(entry.Value.Status);
            var tags = string.Join(", ", entry.Value.Tags);
            var checkClasses = string.Join(" ", entry.Value.Tags.Select(t => $"tag-{t}"));

            html.AppendLine($"            <div class=\"health-check {checkStatusColor.ToLower()} {checkClasses}\" data-status=\"{checkStatus.ToLower()}\" data-tags=\"{tags.ToLower()}\">");
            html.AppendLine("                <div class=\"check-header\">");
            html.AppendLine($"                    <h3>{entry.Key}</h3>");
            html.AppendLine($"                    <span class=\"status-badge {checkStatusColor.ToLower()}\">{checkStatus}</span>");
            html.AppendLine("                </div>");

            html.AppendLine("                <div class=\"check-details\">");
            if (!string.IsNullOrEmpty(entry.Value.Description))
            {
                html.AppendLine($"                    <p><strong>Description:</strong> {entry.Value.Description}</p>");
            }
            html.AppendLine($"                    <p><strong>Duration:</strong> {entry.Value.Duration.TotalMilliseconds:F2}ms</p>");

            if (entry.Value.Tags.Any())
            {
                html.AppendLine($"                    <p><strong>Tags:</strong> {tags}</p>");
            }

            if (entry.Value.Exception != null)
            {
                html.AppendLine($"                    <div class=\"error-details\">");
                html.AppendLine($"                        <strong>Error:</strong> {entry.Value.Exception.Message}");
                html.AppendLine("                    </div>");
            }

            if (entry.Value.Data.Count > 0)
            {
                html.AppendLine("                    <div class=\"data-section\">");
                html.AppendLine("                        <button class=\"toggle-data\" onclick=\"toggleData(this)\">Show Details</button>");
                html.AppendLine("                        <div class=\"data-content\" style=\"display: none;\">");
                html.AppendLine("                            <table class=\"data-table\">");

                foreach (var data in entry.Value.Data)
                {
                    var value = data.Value?.ToString() ?? "null";
                    if (value.Length > 100)
                    {
                        value = value.Substring(0, 97) + "...";
                    }
                    html.AppendLine($"                                <tr><td>{data.Key}</td><td>{value}</td></tr>");
                }

                html.AppendLine("                            </table>");
                html.AppendLine("                        </div>");
                html.AppendLine("                    </div>");
            }
            html.AppendLine("                </div>");
            html.AppendLine("            </div>");
        }

        html.AppendLine("        </div>");

        // System Information
        html.AppendLine("        <div class=\"system-info\">");
        html.AppendLine("            <h2>System Information</h2>");
        html.AppendLine("            <div class=\"info-grid\">");
        html.AppendLine($"                <div class=\"info-item\"><label>Machine Name:</label><span>{Environment.MachineName}</span></div>");
        html.AppendLine($"                <div class=\"info-item\"><label>OS Version:</label><span>{Environment.OSVersion}</span></div>");
        html.AppendLine($"                <div class=\"info-item\"><label>Processor Count:</label><span>{Environment.ProcessorCount}</span></div>");
        html.AppendLine($"                <div class=\"info-item\"><label>Working Set:</label><span>{Environment.WorkingSet / 1024 / 1024:F2} MB</span></div>");
        html.AppendLine($"                <div class=\"info-item\"><label>GC Memory:</label><span>{GC.GetTotalMemory(false) / 1024 / 1024:F2} MB</span></div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        html.AppendLine("        <footer>");
        html.AppendLine("            <button onclick=\"refreshPage()\" class=\"refresh-btn\">🔄 Refresh</button>");
        html.AppendLine("            <a href=\"/health\" class=\"api-link\">JSON API</a>");
        html.AppendLine("            <a href=\"/health/detailed\" class=\"api-link\">Detailed JSON</a>");
        html.AppendLine($"            <span class=\"version\">Maple Blog v1.0</span>");
        html.AppendLine("        </footer>");
        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string GetStatusColor(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => "Green",
            HealthStatus.Degraded => "Orange",
            HealthStatus.Unhealthy => "Red",
            _ => "Gray"
        };
    }

    private static string GetCSS()
    {
        return @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            color: #333;
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
        }

        header {
            background: white;
            border-radius: 15px;
            padding: 30px;
            margin-bottom: 30px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.1);
            text-align: center;
        }

        h1 {
            color: #2c3e50;
            margin-bottom: 20px;
            font-size: 2.5em;
        }

        .overall-status {
            display: inline-flex;
            align-items: center;
            gap: 10px;
            padding: 15px 25px;
            border-radius: 25px;
            font-weight: 600;
            font-size: 1.2em;
            margin-bottom: 15px;
        }

        .status-indicator {
            width: 12px;
            height: 12px;
            border-radius: 50%;
        }

        .green { background-color: #d4edda; color: #155724; }
        .green .status-indicator { background-color: #28a745; }
        .orange { background-color: #fff3cd; color: #856404; }
        .orange .status-indicator { background-color: #ffc107; }
        .red { background-color: #f8d7da; color: #721c24; }
        .red .status-indicator { background-color: #dc3545; }

        .timestamp {
            color: #6c757d;
            font-size: 0.9em;
        }

        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }

        .stat-card {
            background: white;
            padding: 25px;
            border-radius: 15px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.08);
        }

        .stat-card h3 {
            color: #6c757d;
            font-size: 0.9em;
            margin-bottom: 10px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .stat-value {
            font-size: 2em;
            font-weight: bold;
            color: #2c3e50;
        }

        .filters {
            display: flex;
            gap: 10px;
            margin-bottom: 30px;
            flex-wrap: wrap;
        }

        .filter-btn {
            padding: 12px 20px;
            border: none;
            border-radius: 25px;
            background: white;
            color: #333;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 2px 5px rgba(0,0,0,0.1);
        }

        .filter-btn:hover, .filter-btn.active {
            background: #007bff;
            color: white;
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(0,123,255,0.3);
        }

        .health-checks {
            display: grid;
            gap: 20px;
            margin-bottom: 30px;
        }

        .health-check {
            background: white;
            border-radius: 15px;
            padding: 25px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.08);
            transition: all 0.3s ease;
        }

        .health-check:hover {
            transform: translateY(-5px);
            box-shadow: 0 10px 25px rgba(0,0,0,0.15);
        }

        .check-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
        }

        .check-header h3 {
            color: #2c3e50;
            font-size: 1.3em;
        }

        .status-badge {
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 0.8em;
            font-weight: bold;
            text-transform: uppercase;
        }

        .check-details p {
            margin-bottom: 8px;
            line-height: 1.6;
        }

        .error-details {
            background: #f8d7da;
            border: 1px solid #f5c6cb;
            border-radius: 8px;
            padding: 15px;
            margin-top: 15px;
            color: #721c24;
        }

        .data-section {
            margin-top: 15px;
        }

        .toggle-data {
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 5px;
            padding: 8px 15px;
            cursor: pointer;
            font-size: 0.9em;
        }

        .toggle-data:hover {
            background: #e9ecef;
        }

        .data-table {
            width: 100%;
            margin-top: 10px;
            border-collapse: collapse;
        }

        .data-table td {
            padding: 8px 12px;
            border-bottom: 1px solid #dee2e6;
            font-size: 0.9em;
        }

        .data-table td:first-child {
            font-weight: 600;
            background: #f8f9fa;
            width: 30%;
        }

        .system-info {
            background: white;
            border-radius: 15px;
            padding: 25px;
            margin-bottom: 30px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.08);
        }

        .system-info h2 {
            color: #2c3e50;
            margin-bottom: 20px;
        }

        .info-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 15px;
        }

        .info-item {
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #eee;
        }

        .info-item label {
            font-weight: 600;
            color: #6c757d;
        }

        footer {
            background: white;
            border-radius: 15px;
            padding: 20px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.08);
            display: flex;
            justify-content: center;
            align-items: center;
            gap: 20px;
            flex-wrap: wrap;
        }

        .refresh-btn {
            background: #28a745;
            color: white;
            border: none;
            padding: 12px 20px;
            border-radius: 25px;
            cursor: pointer;
            font-weight: 600;
            transition: all 0.3s ease;
        }

        .refresh-btn:hover {
            background: #218838;
            transform: translateY(-2px);
        }

        .api-link {
            background: #007bff;
            color: white;
            text-decoration: none;
            padding: 12px 20px;
            border-radius: 25px;
            font-weight: 600;
            transition: all 0.3s ease;
        }

        .api-link:hover {
            background: #0056b3;
            transform: translateY(-2px);
        }

        .version {
            color: #6c757d;
            font-size: 0.9em;
        }

        .hidden {
            display: none !important;
        }

        @media (max-width: 768px) {
            .container {
                padding: 10px;
            }

            .check-header {
                flex-direction: column;
                align-items: flex-start;
                gap: 10px;
            }

            .filters {
                justify-content: center;
            }

            footer {
                flex-direction: column;
                gap: 15px;
            }
        }
        ";
    }

    private static string GetJavaScript()
    {
        return @"
        function refreshPage() {
            window.location.reload();
        }

        function toggleData(button) {
            const content = button.nextElementSibling;
            if (content.style.display === 'none') {
                content.style.display = 'block';
                button.textContent = 'Hide Details';
            } else {
                content.style.display = 'none';
                button.textContent = 'Show Details';
            }
        }

        function filterChecks(filter) {
            const checks = document.querySelectorAll('.health-check');
            const buttons = document.querySelectorAll('.filter-btn');

            // Update active button
            buttons.forEach(btn => btn.classList.remove('active'));
            event.target.classList.add('active');

            checks.forEach(check => {
                const status = check.dataset.status;
                const tags = check.dataset.tags;

                let show = false;

                switch(filter) {
                    case 'all':
                        show = true;
                        break;
                    case 'healthy':
                        show = status === 'healthy';
                        break;
                    case 'degraded':
                        show = status === 'degraded';
                        break;
                    case 'unhealthy':
                        show = status === 'unhealthy';
                        break;
                    case 'redis':
                        show = tags.includes('redis');
                        break;
                    case 'database':
                        show = tags.includes('database') || tags.includes('db');
                        break;
                }

                check.style.display = show ? 'block' : 'none';
            });
        }

        // Auto-refresh every 30 seconds
        setInterval(refreshPage, 30000);
        ";
    }
}