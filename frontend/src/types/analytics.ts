/**
 * Analytics data types and interfaces for the admin dashboard
 */

export interface AnalyticsOverview {
  totalViews: number;
  uniqueVisitors: number;
  avgSessionDuration: number;
  bounceRate: number;
  newUsers: number;
  returningUsers: number;
  totalPosts: number;
  totalComments: number;
  totalCategories: number;
  totalTags: number;
}

export interface TrafficData {
  date: string;
  views: number;
  visitors: number;
  sessions: number;
  bounceRate: number;
}

export interface TopPost {
  id: string;
  title: string;
  slug: string;
  views: number;
  engagement: number;
  comments: number;
  shares: number;
  averageReadTime: number;
}

export interface TrafficSource {
  source: string;
  visits: number;
  percentage: number;
  bounceRate: number;
  avgSessionDuration: number;
}

export interface DeviceData {
  type: 'Desktop' | 'Mobile' | 'Tablet';
  visits: number;
  percentage: number;
  bounceRate: number;
  avgSessionDuration: number;
}

export interface GeographicData {
  country: string;
  code: string;
  visits: number;
  percentage: number;
  newUsers: number;
}

export interface SearchQuery {
  query: string;
  count: number;
  results: number;
  clickThrough: number;
}

export interface UserActivity {
  date: string;
  newRegistrations: number;
  activeUsers: number;
  postsCreated: number;
  commentsPosted: number;
}

export interface ContentPerformance {
  category: string;
  posts: number;
  totalViews: number;
  avgViews: number;
  engagement: number;
}

export interface AnalyticsData {
  overview: AnalyticsOverview;
  traffic: TrafficData[];
  topPosts: TopPost[];
  sources: TrafficSource[];
  devices: DeviceData[];
  geographic: GeographicData[];
  searchQueries: SearchQuery[];
  userActivity: UserActivity[];
  contentPerformance: ContentPerformance[];
}

export interface TimePeriod {
  label: string;
  value: '7d' | '30d' | '90d' | '1y' | 'custom';
  days: number;
}

export interface DateRange {
  start: Date;
  end: Date;
}

export interface AnalyticsFilters {
  period: TimePeriod['value'];
  dateRange?: DateRange;
  category?: string;
  source?: string;
  device?: string;
  country?: string;
}

export interface ChartDataPoint {
  name: string;
  value: number;
  percentage?: number;
  color?: string;
}

export interface LineChartData {
  name: string;
  data: { x: string; y: number }[];
  color?: string;
}

export interface ExportOptions {
  format: 'pdf' | 'csv' | 'excel';
  dateRange: DateRange;
  sections: string[];
}

export interface AnalyticsAlert {
  id: string;
  type: 'warning' | 'error' | 'info' | 'success';
  title: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
}

export interface GoalTracking {
  id: string;
  name: string;
  target: number;
  current: number;
  percentage: number;
  period: string;
  status: 'on-track' | 'behind' | 'exceeded';
}