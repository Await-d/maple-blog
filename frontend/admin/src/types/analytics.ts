// @ts-nocheck
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
    value: any;
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
  customDimensions?: Record<string, any>;
}

// Dashboard Widget
export interface DashboardWidget {
  id: string;
  type: 'metric' | 'chart' | 'table' | 'map' | 'custom';
  title: string;
  metric?: string;
  visualization?: string;
  data?: any;
  config?: Record<string, any>;
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