// Analytics specific types

// Time ranges for analytics
export type TimeRange = 'today' | 'yesterday' | 'week' | 'month' | 'quarter' | 'year' | 'custom';

export interface TimeRangeOption {
  label: string;
  value: TimeRange;
  days?: number;
}

// Analytics Overview
export interface AnalyticsOverview {
  period: {
    start: string;
    end: string;
  };
  metrics: {
    totalVisits: number;
    uniqueVisitors: number;
    pageViews: number;
    avgSessionDuration: number;
    bounceRate: number;
    conversionRate: number;
  };
  trends: {
    visitsChange: number;
    visitorsChange: number;
    pageViewsChange: number;
    durationChange: number;
  };
  topMetrics: {
    peakHour: string;
    peakDay: string;
    topDevice: string;
    topBrowser: string;
    topCountry: string;
  };
}

// Traffic Analytics
export interface TrafficData {
  date: string;
  visits: number;
  uniqueVisitors: number;
  pageViews: number;
  bounceRate: number;
  avgDuration: number;
}

export interface TrafficSource {
  source: string;
  visits: number;
  percentage: number;
  trend: number;
  icon?: string;
  color?: string;
}

export interface DeviceAnalytics {
  device: string;
  sessions: number;
  percentage: number;
  bounceRate: number;
  avgDuration: number;
}

export interface GeographicData {
  country: string;
  region?: string;
  city?: string;
  visits: number;
  percentage: number;
  avgDuration: number;
  bounceRate: number;
  coordinates?: [number, number];
}

// User Analytics
export interface UserBehavior {
  metric: string;
  value: number;
  change: number;
  details?: {
    new: number;
    returning: number;
    engaged: number;
  };
}

export interface UserFlow {
  page: string;
  entries: number;
  exits: number;
  dropoffs: number;
  conversions: number;
  avgTime: number;
}

export interface UserSegment {
  name: string;
  count: number;
  percentage: number;
  characteristics: {
    avgAge?: number;
    topInterests?: string[];
    avgSessionsPerUser?: number;
    avgValuePerUser?: number;
  };
  trend: number;
}

export interface UserActivity {
  hour: number;
  monday: number;
  tuesday: number;
  wednesday: number;
  thursday: number;
  friday: number;
  saturday: number;
  sunday: number;
}

// Content Analytics
export interface ContentMetrics {
  contentId: string;
  title: string;
  type: 'post' | 'page' | 'media';
  author: string;
  publishDate: string;
  views: number;
  uniqueViews: number;
  avgTimeOnPage: number;
  bounceRate: number;
  shares: number;
  comments: number;
  likes: number;
  conversionRate: number;
  engagementScore: number;
  trend: number;
}

export interface ContentPerformance {
  period: string;
  topContent: ContentMetrics[];
  risingContent: ContentMetrics[];
  decliningContent: ContentMetrics[];
  categories: CategoryPerformance[];
  tags: TagPerformance[];
}

export interface CategoryPerformance {
  category: string;
  posts: number;
  totalViews: number;
  avgViews: number;
  engagementRate: number;
  trend: number;
}

export interface TagPerformance {
  tag: string;
  posts: number;
  views: number;
  engagement: number;
  trend: number;
}

export interface AuthorPerformance {
  authorId: string;
  authorName: string;
  avatar?: string;
  posts: number;
  totalViews: number;
  avgViews: number;
  totalEngagement: number;
  avgEngagement: number;
  topPost: string;
  followers?: number;
  trend: number;
}

// Engagement Analytics
export interface EngagementMetrics {
  totalInteractions: number;
  likes: number;
  comments: number;
  shares: number;
  saves: number;
  engagementRate: number;
  viralityScore: number;
}

export interface SocialMetrics {
  platform: 'facebook' | 'twitter' | 'linkedin' | 'instagram' | 'pinterest' | 'reddit';
  shares: number;
  clicks: number;
  conversions: number;
  engagement: number;
}

// Conversion Analytics
export interface ConversionFunnel {
  stage: string;
  users: number;
  dropoff: number;
  conversionRate: number;
  avgTimeToNext: number;
}

export interface GoalMetrics {
  goalId: string;
  goalName: string;
  type: 'pageview' | 'event' | 'duration' | 'custom';
  completions: number;
  conversionRate: number;
  value: number;
  trend: number;
}

// Real-time Analytics
export interface RealTimeData {
  activeUsers: number;
  activeSessions: number;
  pageViewsPerMinute: number;
  topPages: Array<{
    path: string;
    users: number;
    percentage: number;
  }>;
  topReferrers: Array<{
    source: string;
    users: number;
  }>;
  topLocations: Array<{
    location: string;
    users: number;
  }>;
  recentEvents: Array<{
    timestamp: string;
    event: string;
    user?: string;
    details?: string;
  }>;
}

// SEO Analytics
export interface SEOMetrics {
  organicTraffic: number;
  keywords: Array<{
    keyword: string;
    impressions: number;
    clicks: number;
    ctr: number;
    position: number;
    trend: number;
  }>;
  backlinks: number;
  domainAuthority: number;
  pageSpeed: {
    mobile: number;
    desktop: number;
  };
  crawlErrors: number;
}

// Performance Analytics
export interface PerformanceMetrics {
  loadTime: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  totalBlockingTime: number;
  cumulativeLayoutShift: number;
  firstInputDelay: number;
  timeToInteractive: number;
  serverResponseTime: number;
  coreWebVitals: {
    lcp: 'good' | 'needs-improvement' | 'poor';
    fid: 'good' | 'needs-improvement' | 'poor';
    cls: 'good' | 'needs-improvement' | 'poor';
  };
}

// Revenue Analytics
export interface RevenueMetrics {
  totalRevenue: number;
  transactions: number;
  avgOrderValue: number;
  conversionRate: number;
  revenuePerUser: number;
  refundRate: number;
  growth: number;
  forecast: number;
}

// Custom Reports
export interface CustomReport {
  id: string;
  name: string;
  description?: string;
  metrics: string[];
  dimensions: string[];
  filters?: Array<{
    field: string;
    operator: 'equals' | 'contains' | 'greater' | 'less' | 'between';
    value: unknown;
  }>;
  timeRange: TimeRange;
  visualization: 'table' | 'line' | 'bar' | 'pie' | 'map' | 'heatmap';
  schedule?: 'daily' | 'weekly' | 'monthly';
  recipients?: string[];
  createdBy: string;
  createdAt: string;
  lastRun?: string;
}

// Analytics Filters
export interface AnalyticsFilter {
  timeRange: TimeRange;
  startDate?: string;
  endDate?: string;
  segment?: string;
  device?: string;
  source?: string;
  country?: string;
  contentType?: string;
  author?: string;
  category?: string;
  tags?: string[];
  customDimensions?: Record<string, unknown>;
}

// Dashboard Widget
export interface DashboardWidget {
  id: string;
  type: 'metric' | 'chart' | 'table' | 'map' | 'custom';
  title: string;
  metric?: string;
  visualization?: string;
  data?: unknown;
  config?: Record<string, unknown>;
  position: {
    x: number;
    y: number;
    w: number;
    h: number;
  };
  refreshInterval?: number;
}

// Export formats
export type ExportFormat = 'csv' | 'excel' | 'pdf' | 'json';

export interface ExportOptions {
  format: ExportFormat;
  dateRange: {
    start: string;
    end: string;
  };
  metrics?: string[];
  dimensions?: string[];
  filters?: AnalyticsFilter;
  includeCharts?: boolean;
  includeRawData?: boolean;
  compression?: boolean;
}

// Analytics Alerts
export interface AnalyticsAlert {
  id: string;
  name: string;
  metric: string;
  condition: 'above' | 'below' | 'equals' | 'change';
  threshold: number;
  frequency: 'realtime' | 'hourly' | 'daily' | 'weekly';
  recipients: string[];
  isActive: boolean;
  lastTriggered?: string;
  createdAt: string;
}

// Additional Analytics Types

// User Cohort Analytics
export interface UserCohortData {
  cohortDate: string;
  userCount: number;
  periods: Array<{
    period: number;
    retainedUsers: number;
    retentionRate: number;
  }>;
}

// User Retention Analytics
export interface UserRetentionData {
  period: string;
  totalUsers: number;
  returnedUsers: number;
  retentionRate: number;
  segments: Array<{
    segment: string;
    retentionRate: number;
  }>;
}

// Content Trends
export interface ContentTrendsData {
  period: string;
  trendingTopics: Array<{
    topic: string;
    posts: number;
    views: number;
    growth: number;
  }>;
  contentTypes: Array<{
    type: string;
    count: number;
    engagement: number;
  }>;
  keywords: Array<{
    keyword: string;
    mentions: number;
    sentiment: 'positive' | 'negative' | 'neutral';
  }>;
}

// Attribution Analytics
export interface AttributionData {
  model: 'first-click' | 'last-click' | 'linear' | 'time-decay' | 'position';
  channels: Array<{
    channel: string;
    conversions: number;
    attributedValue: number;
    percentage: number;
  }>;
  paths: Array<{
    path: string[];
    conversions: number;
    value: number;
  }>;
}

// Search Console Data
export interface SearchConsoleData {
  period: {
    start: string;
    end: string;
  };
  performance: {
    totalClicks: number;
    totalImpressions: number;
    averageCtr: number;
    averagePosition: number;
  };
  queries: Array<{
    query: string;
    clicks: number;
    impressions: number;
    ctr: number;
    position: number;
  }>;
  pages: Array<{
    page: string;
    clicks: number;
    impressions: number;
    ctr: number;
    position: number;
  }>;
}

// Page Rankings
export interface PageRankingsData {
  url: string;
  keywords: Array<{
    keyword: string;
    position: number;
    previousPosition?: number;
    searchVolume: number;
    difficulty: number;
  }>;
  competitorAnalysis: Array<{
    competitor: string;
    url: string;
    position: number;
    strengthScore: number;
  }>;
}

// Page Speed Data
export interface PageSpeedData {
  url: string;
  device: 'mobile' | 'desktop';
  score: number;
  metrics: {
    firstContentfulPaint: number;
    speedIndex: number;
    largestContentfulPaint: number;
    timeToInteractive: number;
    totalBlockingTime: number;
    cumulativeLayoutShift: number;
  };
  opportunities: Array<{
    id: string;
    title: string;
    description: string;
    savings: number;
    impact: 'low' | 'medium' | 'high';
  }>;
}

// Core Web Vitals
export interface CoreWebVitalsData {
  period: string;
  metrics: {
    lcp: {
      value: number;
      rating: 'good' | 'needs-improvement' | 'poor';
      percentile75: number;
    };
    fid: {
      value: number;
      rating: 'good' | 'needs-improvement' | 'poor';
      percentile75: number;
    };
    cls: {
      value: number;
      rating: 'good' | 'needs-improvement' | 'poor';
      percentile75: number;
    };
  };
  urls: Array<{
    url: string;
    lcp: number;
    fid: number;
    cls: number;
  }>;
}

// Transaction Data
export interface TransactionData {
  transactionId: string;
  date: string;
  amount: number;
  currency: string;
  items: Array<{
    itemId: string;
    name: string;
    category: string;
    quantity: number;
    price: number;
  }>;
  customer: {
    id: string;
    type: 'new' | 'returning';
    segment: string;
  };
  source: string;
  campaign?: string;
}

// Product Performance
export interface ProductPerformanceData {
  productId: string;
  name: string;
  category: string;
  revenue: number;
  units: number;
  averagePrice: number;
  profitMargin: number;
  conversionRate: number;
  cartAdditions: number;
  wishlistAdditions: number;
  reviews: {
    count: number;
    averageRating: number;
  };
}

// Custom Report Results
export interface CustomReportResult {
  reportId: string;
  generatedAt: string;
  data: Array<Record<string, unknown>>;
  summary: {
    totalRows: number;
    totalValue?: number;
    averageValue?: number;
  };
  metadata: {
    dimensions: string[];
    metrics: string[];
    filters: Record<string, unknown>;
  };
}

// Comparison Results
export interface ComparisonResult {
  type: 'time' | 'segment';
  baseline: {
    label: string;
    period?: {
      start: string;
      end: string;
    };
    data: Record<string, number>;
  };
  comparison: {
    label: string;
    period?: {
      start: string;
      end: string;
    };
    data: Record<string, number>;
  };
  changes: Record<string, {
    absolute: number;
    percentage: number;
    significance: 'significant' | 'not-significant';
  }>;
}

// Forecast Data
export interface ForecastData {
  metric: string;
  historical: Array<{
    date: string;
    value: number;
  }>;
  forecast: Array<{
    date: string;
    value: number;
    confidenceInterval: {
      lower: number;
      upper: number;
    };
  }>;
  accuracy: {
    mape: number; // Mean Absolute Percentage Error
    rmse: number; // Root Mean Square Error
  };
  factors: Array<{
    name: string;
    impact: number;
    confidence: number;
  }>;
}

// Anomaly Data
export interface AnomalyData {
  metric: string;
  anomalies: Array<{
    date: string;
    expected: number;
    actual: number;
    deviation: number;
    severity: 'low' | 'medium' | 'high';
    factors?: string[];
  }>;
  patterns: Array<{
    type: 'seasonal' | 'trend' | 'outlier';
    description: string;
    confidence: number;
  }>;
}

// Trend Analysis
export interface TrendAnalysisData {
  metric: string;
  period: string;
  trend: {
    direction: 'up' | 'down' | 'stable';
    strength: number;
    significance: number;
  };
  breakpoints: Array<{
    date: string;
    type: 'increase' | 'decrease' | 'plateau';
    confidence: number;
  }>;
  seasonality: {
    detected: boolean;
    pattern?: 'weekly' | 'monthly' | 'quarterly' | 'yearly';
    strength?: number;
  };
}