import { ApiService } from './api';
import type {
  AnalyticsOverview,
  TrafficData,
  TrafficSource,
  DeviceAnalytics,
  GeographicData,
  UserBehavior,
  UserFlow,
  UserSegment,
  UserActivity,
  ContentMetrics,
  ContentPerformance,
  AuthorPerformance,
  EngagementMetrics,
  ConversionFunnel,
  GoalMetrics,
  RealTimeData,
  SEOMetrics,
  PerformanceMetrics,
  RevenueMetrics,
  CustomReport,
  AnalyticsFilter,
  ExportOptions,
  AnalyticsAlert,
  TimeRange,
  UserCohortData,
  UserRetentionData,
  ContentTrendsData,
  AttributionData,
  SearchConsoleData,
  PageRankingsData,
  PageSpeedData,
  CoreWebVitalsData,
  TransactionData,
  ProductPerformanceData,
  CustomReportResult,
  ComparisonResult,
  ForecastData,
  AnomalyData,
  TrendAnalysisData
} from '@/types/analytics';

class AnalyticsService {
  private baseUrl = '/analytics';

  // Overview Analytics
  async getOverview(filter?: AnalyticsFilter): Promise<AnalyticsOverview> {
    return ApiService.get(`${this.baseUrl}/overview`, filter);
  }

  // Traffic Analytics
  async getTrafficData(filter?: AnalyticsFilter): Promise<TrafficData[]> {
    return ApiService.get(`${this.baseUrl}/traffic`, filter);
  }

  async getTrafficSources(filter?: AnalyticsFilter): Promise<TrafficSource[]> {
    return ApiService.get(`${this.baseUrl}/traffic/sources`, filter);
  }

  async getDeviceAnalytics(filter?: AnalyticsFilter): Promise<DeviceAnalytics[]> {
    return ApiService.get(`${this.baseUrl}/devices`, filter);
  }

  async getGeographicData(filter?: AnalyticsFilter): Promise<GeographicData[]> {
    return ApiService.get(`${this.baseUrl}/geographic`, filter);
  }

  // User Analytics
  async getUserBehavior(filter?: AnalyticsFilter): Promise<UserBehavior[]> {
    return ApiService.get(`${this.baseUrl}/users/behavior`, filter);
  }

  async getUserFlow(filter?: AnalyticsFilter): Promise<UserFlow[]> {
    return ApiService.get(`${this.baseUrl}/users/flow`, filter);
  }

  async getUserSegments(filter?: AnalyticsFilter): Promise<UserSegment[]> {
    return ApiService.get(`${this.baseUrl}/users/segments`, filter);
  }

  async getUserActivity(filter?: AnalyticsFilter): Promise<UserActivity[]> {
    return ApiService.get(`${this.baseUrl}/users/activity`, filter);
  }

  async getUserCohorts(filter?: AnalyticsFilter): Promise<UserCohortData[]> {
    return ApiService.get(`${this.baseUrl}/users/cohorts`, filter);
  }

  async getUserRetention(filter?: AnalyticsFilter): Promise<UserRetentionData> {
    return ApiService.get(`${this.baseUrl}/users/retention`, filter);
  }

  // Content Analytics
  async getContentMetrics(filter?: AnalyticsFilter): Promise<ContentMetrics[]> {
    return ApiService.get(`${this.baseUrl}/content/metrics`, filter);
  }

  async getContentPerformance(filter?: AnalyticsFilter): Promise<ContentPerformance> {
    return ApiService.get(`${this.baseUrl}/content/performance`, filter);
  }

  async getAuthorPerformance(filter?: AnalyticsFilter): Promise<AuthorPerformance[]> {
    return ApiService.get(`${this.baseUrl}/content/authors`, filter);
  }

  async getEngagementMetrics(filter?: AnalyticsFilter): Promise<EngagementMetrics> {
    return ApiService.get(`${this.baseUrl}/content/engagement`, filter);
  }

  async getContentTrends(filter?: AnalyticsFilter): Promise<ContentTrendsData> {
    return ApiService.get(`${this.baseUrl}/content/trends`, filter);
  }

  async getSocialMetrics(filter?: AnalyticsFilter): Promise<EngagementMetrics> {
    return ApiService.get(`${this.baseUrl}/content/social`, filter);
  }

  // Conversion Analytics
  async getConversionFunnel(funnelId: string, filter?: AnalyticsFilter): Promise<ConversionFunnel[]> {
    return ApiService.get(`${this.baseUrl}/conversions/funnel/${funnelId}`, filter);
  }

  async getGoalMetrics(filter?: AnalyticsFilter): Promise<GoalMetrics[]> {
    return ApiService.get(`${this.baseUrl}/conversions/goals`, filter);
  }

  async getAttributionData(filter?: AnalyticsFilter): Promise<AttributionData> {
    return ApiService.get(`${this.baseUrl}/conversions/attribution`, filter);
  }

  // Real-time Analytics
  async getRealTimeData(): Promise<RealTimeData> {
    return ApiService.get(`${this.baseUrl}/realtime`);
  }

  async subscribeToRealTime(callback: (data: RealTimeData) => void): () => void {
    // WebSocket subscription implementation
    const ws = new WebSocket(`${process.env.VITE_WS_URL}/analytics/realtime`);

    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      callback(data);
    };

    return () => ws.close();
  }

  // SEO Analytics
  async getSEOMetrics(filter?: AnalyticsFilter): Promise<SEOMetrics> {
    return ApiService.get(`${this.baseUrl}/seo/metrics`, filter);
  }

  async getSearchConsoleData(filter?: AnalyticsFilter): Promise<SearchConsoleData> {
    return ApiService.get(`${this.baseUrl}/seo/search-console`, filter);
  }

  async getPageRankings(filter?: AnalyticsFilter): Promise<PageRankingsData[]> {
    return ApiService.get(`${this.baseUrl}/seo/rankings`, filter);
  }

  // Performance Analytics
  async getPerformanceMetrics(filter?: AnalyticsFilter): Promise<PerformanceMetrics> {
    return ApiService.get(`${this.baseUrl}/performance/metrics`, filter);
  }

  async getPageSpeedData(url?: string): Promise<PageSpeedData> {
    return ApiService.get(`${this.baseUrl}/performance/pagespeed`, { url });
  }

  async getCoreWebVitals(filter?: AnalyticsFilter): Promise<CoreWebVitalsData> {
    return ApiService.get(`${this.baseUrl}/performance/web-vitals`, filter);
  }

  // Revenue Analytics
  async getRevenueMetrics(filter?: AnalyticsFilter): Promise<RevenueMetrics> {
    return ApiService.get(`${this.baseUrl}/revenue/metrics`, filter);
  }

  async getTransactionData(filter?: AnalyticsFilter): Promise<TransactionData[]> {
    return ApiService.get(`${this.baseUrl}/revenue/transactions`, filter);
  }

  async getProductPerformance(filter?: AnalyticsFilter): Promise<ProductPerformanceData[]> {
    return ApiService.get(`${this.baseUrl}/revenue/products`, filter);
  }

  // Custom Reports
  async getCustomReports(): Promise<CustomReport[]> {
    return ApiService.get(`${this.baseUrl}/reports`);
  }

  async getCustomReport(reportId: string): Promise<CustomReport> {
    return ApiService.get(`${this.baseUrl}/reports/${reportId}`);
  }

  async createCustomReport(report: Omit<CustomReport, 'id' | 'createdAt'>): Promise<CustomReport> {
    return ApiService.post(`${this.baseUrl}/reports`, report);
  }

  async updateCustomReport(reportId: string, updates: Partial<CustomReport>): Promise<CustomReport> {
    return ApiService.put(`${this.baseUrl}/reports/${reportId}`, updates);
  }

  async deleteCustomReport(reportId: string): Promise<void> {
    return ApiService.delete(`${this.baseUrl}/reports/${reportId}`);
  }

  async runCustomReport(reportId: string, filter?: AnalyticsFilter): Promise<CustomReportResult> {
    return ApiService.post(`${this.baseUrl}/reports/${reportId}/run`, filter);
  }

  // Data Export
  async exportData(options: ExportOptions): Promise<Blob> {
    const response = await ApiService.post(`${this.baseUrl}/export`, options, {
      responseType: 'blob'
    });
    return response as unknown as Blob;
  }

  async scheduleExport(options: ExportOptions & { schedule: string }): Promise<void> {
    return ApiService.post(`${this.baseUrl}/export/schedule`, options);
  }

  // Analytics Alerts
  async getAlerts(): Promise<AnalyticsAlert[]> {
    return ApiService.get(`${this.baseUrl}/alerts`);
  }

  async createAlert(alert: Omit<AnalyticsAlert, 'id' | 'createdAt'>): Promise<AnalyticsAlert> {
    return ApiService.post(`${this.baseUrl}/alerts`, alert);
  }

  async updateAlert(alertId: string, updates: Partial<AnalyticsAlert>): Promise<AnalyticsAlert> {
    return ApiService.put(`${this.baseUrl}/alerts/${alertId}`, updates);
  }

  async deleteAlert(alertId: string): Promise<void> {
    return ApiService.delete(`${this.baseUrl}/alerts/${alertId}`);
  }

  async testAlert(alertId: string): Promise<void> {
    return ApiService.post(`${this.baseUrl}/alerts/${alertId}/test`);
  }

  // Comparison Analytics
  async compareTimeRanges(
    range1: { start: string; end: string },
    range2: { start: string; end: string },
    metrics: string[]
  ): Promise<ComparisonResult> {
    return ApiService.post(`${this.baseUrl}/compare/time`, {
      range1,
      range2,
      metrics
    });
  }

  async compareSegments(
    segments: string[],
    metrics: string[],
    filter?: AnalyticsFilter
  ): Promise<ComparisonResult> {
    return ApiService.post(`${this.baseUrl}/compare/segments`, {
      segments,
      metrics,
      ...filter
    });
  }

  // Predictive Analytics
  async getForecast(
    metric: string,
    period: number,
    filter?: AnalyticsFilter
  ): Promise<ForecastData> {
    return ApiService.post(`${this.baseUrl}/predict/forecast`, {
      metric,
      period,
      ...filter
    });
  }

  async getAnomalies(
    metrics: string[],
    sensitivity: 'low' | 'medium' | 'high',
    filter?: AnalyticsFilter
  ): Promise<AnomalyData[]> {
    return ApiService.post(`${this.baseUrl}/predict/anomalies`, {
      metrics,
      sensitivity,
      ...filter
    });
  }

  async getTrendAnalysis(
    metric: string,
    filter?: AnalyticsFilter
  ): Promise<TrendAnalysisData> {
    return ApiService.get(`${this.baseUrl}/predict/trends`, {
      metric,
      ...filter
    });
  }

  // Helper Methods
  getTimeRangePresets(): Array<{ label: string; value: TimeRange; days?: number }> {
    return [
      { label: '今天', value: 'today', days: 1 },
      { label: '昨天', value: 'yesterday', days: 1 },
      { label: '最近7天', value: 'week', days: 7 },
      { label: '最近30天', value: 'month', days: 30 },
      { label: '最近90天', value: 'quarter', days: 90 },
      { label: '最近365天', value: 'year', days: 365 },
      { label: '自定义', value: 'custom' }
    ];
  }

  formatMetricValue(value: number, type: string): string {
    switch (type) {
      case 'currency':
        return new Intl.NumberFormat('zh-CN', {
          style: 'currency',
          currency: 'CNY'
        }).format(value);
      case 'percentage':
        return `${(value * 100).toFixed(2)}%`;
      case 'duration': {
        const minutes = Math.floor(value / 60);
        const seconds = value % 60;
        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
      }
      case 'number':
        return new Intl.NumberFormat('zh-CN').format(value);
      default:
        return value.toString();
    }
  }

  calculateGrowthRate(current: number, previous: number): number {
    if (previous === 0) return current > 0 ? 100 : 0;
    return ((current - previous) / previous) * 100;
  }
}

export const analyticsService = new AnalyticsService();