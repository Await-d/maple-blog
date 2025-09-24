/**
 * Analytics service for fetching dashboard data
 * Currently uses realistic mock data, can be replaced with actual API calls
 */

import { 
  AnalyticsData, 
  AnalyticsFilters, 
  TimePeriod,
  AnalyticsOverview,
  TrafficData,
  UserActivity,
  TopPost,
  AnalyticsAlert,
  GoalTracking
} from '@/types/analytics';

// Time periods configuration
export const TIME_PERIODS: TimePeriod[] = [
  { label: 'Last 7 days', value: '7d', days: 7 },
  { label: 'Last 30 days', value: '30d', days: 30 },
  { label: 'Last 90 days', value: '90d', days: 90 },
  { label: 'Last year', value: '1y', days: 365 },
  { label: 'Custom range', value: 'custom', days: 0 }
];

// Generate realistic mock data
const generateTrafficData = (days: number): TrafficData[] => {
  const data: TrafficData[] = [];
  const today = new Date();
  
  for (let i = days - 1; i >= 0; i--) {
    const date = new Date(today);
    date.setDate(date.getDate() - i);
    
    // Generate realistic traffic patterns with weekly cycles
    const dayOfWeek = date.getDay();
    const isWeekend = dayOfWeek === 0 || dayOfWeek === 6;
    const baseSessions = isWeekend ? 150 : 250;
    const variance = Math.random() * 0.4 + 0.8; // 80-120% variance
    
    const sessions = Math.round(baseSessions * variance);
    const visitors = Math.round(sessions * (Math.random() * 0.3 + 0.6)); // 60-90% of sessions
    const views = Math.round(sessions * (Math.random() * 0.8 + 1.2)); // 120-200% of sessions
    const bounceRate = Math.round((Math.random() * 20 + 40) * 10) / 10; // 40-60%
    
    data.push({
      date: date.toISOString().split('T')[0],
      views,
      visitors,
      sessions,
      bounceRate
    });
  }
  
  return data;
};

const generateTopPosts = (): TopPost[] => {
  const posts = [
    'Getting Started with React 19',
    'Advanced TypeScript Patterns',
    'Building Scalable APIs with .NET Core',
    'Modern CSS Techniques',
    'Performance Optimization Tips',
    'Understanding Clean Architecture',
    'Database Design Best Practices',
    'Frontend Testing Strategies',
    'DevOps for Beginners',
    'Microservices Architecture Guide'
  ];
  
  return posts.slice(0, 8).map((title, i) => ({
    id: `post-${i + 1}`,
    title,
    slug: title.toLowerCase().replace(/\s+/g, '-'),
    views: Math.round((1000 - i * 100) * (Math.random() * 0.4 + 0.8)),
    engagement: Math.round((85 - i * 5) * (Math.random() * 0.2 + 0.9)),
    comments: Math.round((50 - i * 4) * (Math.random() * 0.6 + 0.7)),
    shares: Math.round((30 - i * 2) * (Math.random() * 0.5 + 0.75)),
    averageReadTime: Math.round((8 - i * 0.5) * (Math.random() * 0.3 + 0.85))
  }));
};

const generateUserActivity = (days: number): UserActivity[] => {
  const data: UserActivity[] = [];
  const today = new Date();
  
  for (let i = days - 1; i >= 0; i--) {
    const date = new Date(today);
    date.setDate(date.getDate() - i);
    
    data.push({
      date: date.toISOString().split('T')[0],
      newRegistrations: Math.round(Math.random() * 15 + 5),
      activeUsers: Math.round(Math.random() * 100 + 80),
      postsCreated: Math.round(Math.random() * 8 + 2),
      commentsPosted: Math.round(Math.random() * 25 + 15)
    });
  }
  
  return data;
};

const mockAnalyticsData: AnalyticsData = {
  overview: {
    totalViews: 45678,
    uniqueVisitors: 12543,
    avgSessionDuration: 245,
    bounceRate: 52.3,
    newUsers: 3456,
    returningUsers: 9087,
    totalPosts: 127,
    totalComments: 892,
    totalCategories: 12,
    totalTags: 45
  },
  traffic: generateTrafficData(30),
  topPosts: generateTopPosts(),
  sources: [
    { source: 'Organic Search', visits: 18432, percentage: 45.2, bounceRate: 48.5, avgSessionDuration: 185 },
    { source: 'Direct', visits: 12876, percentage: 31.6, bounceRate: 42.1, avgSessionDuration: 220 },
    { source: 'Social Media', visits: 5432, percentage: 13.3, bounceRate: 65.7, avgSessionDuration: 95 },
    { source: 'Referral', visits: 2987, percentage: 7.3, bounceRate: 55.4, avgSessionDuration: 165 },
    { source: 'Email', visits: 1054, percentage: 2.6, bounceRate: 38.2, avgSessionDuration: 280 }
  ],
  devices: [
    { type: 'Desktop', visits: 24567, percentage: 60.3, bounceRate: 48.2, avgSessionDuration: 245 },
    { type: 'Mobile', visits: 13456, percentage: 33.0, bounceRate: 58.7, avgSessionDuration: 165 },
    { type: 'Tablet', visits: 2734, percentage: 6.7, bounceRate: 52.1, avgSessionDuration: 198 }
  ],
  geographic: [
    { country: 'United States', code: 'US', visits: 15432, percentage: 37.8, newUsers: 1234 },
    { country: 'United Kingdom', code: 'GB', visits: 6789, percentage: 16.6, newUsers: 567 },
    { country: 'Germany', code: 'DE', visits: 4321, percentage: 10.6, newUsers: 345 },
    { country: 'France', code: 'FR', visits: 3456, percentage: 8.5, newUsers: 289 },
    { country: 'Canada', code: 'CA', visits: 2987, percentage: 7.3, newUsers: 234 },
    { country: 'Australia', code: 'AU', visits: 2134, percentage: 5.2, newUsers: 178 },
    { country: 'Japan', code: 'JP', visits: 1876, percentage: 4.6, newUsers: 156 },
    { country: 'India', code: 'IN', visits: 1543, percentage: 3.8, newUsers: 123 },
    { country: 'Brazil', code: 'BR', visits: 1234, percentage: 3.0, newUsers: 98 },
    { country: 'Others', code: 'XX', visits: 1005, percentage: 2.5, newUsers: 87 }
  ],
  searchQueries: [
    { query: 'react hooks', count: 234, results: 12, clickThrough: 78 },
    { query: 'typescript tutorial', count: 189, results: 8, clickThrough: 65 },
    { query: '.net core api', count: 156, results: 15, clickThrough: 89 },
    { query: 'css grid layout', count: 134, results: 6, clickThrough: 54 },
    { query: 'clean architecture', count: 123, results: 9, clickThrough: 71 }
  ],
  userActivity: generateUserActivity(30),
  contentPerformance: [
    { category: 'Technology', posts: 45, totalViews: 18432, avgViews: 410, engagement: 78 },
    { category: 'Programming', posts: 38, totalViews: 15678, avgViews: 413, engagement: 82 },
    { category: 'Web Development', posts: 32, totalViews: 12345, avgViews: 386, engagement: 75 },
    { category: 'DevOps', posts: 12, totalViews: 5432, avgViews: 453, engagement: 85 }
  ]
};

// Analytics alerts
const mockAlerts: AnalyticsAlert[] = [
  {
    id: '1',
    type: 'success',
    title: 'Traffic Milestone',
    message: 'Site reached 50,000 monthly page views!',
    timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000),
    isRead: false
  },
  {
    id: '2',
    type: 'warning',
    title: 'High Bounce Rate',
    message: 'Mobile bounce rate increased to 65% this week.',
    timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000),
    isRead: false
  },
  {
    id: '3',
    type: 'info',
    title: 'New Popular Content',
    message: '"Advanced TypeScript Patterns" is trending.',
    timestamp: new Date(Date.now() - 24 * 60 * 60 * 1000),
    isRead: true
  }
];

// Goal tracking
const mockGoals: GoalTracking[] = [
  {
    id: '1',
    name: 'Monthly Page Views',
    target: 50000,
    current: 45678,
    percentage: 91.4,
    period: 'This Month',
    status: 'on-track'
  },
  {
    id: '2',
    name: 'New Subscribers',
    target: 1000,
    current: 1234,
    percentage: 123.4,
    period: 'This Quarter',
    status: 'exceeded'
  },
  {
    id: '3',
    name: 'Avg Session Duration',
    target: 180,
    current: 245,
    percentage: 136.1,
    period: 'This Month',
    status: 'exceeded'
  },
  {
    id: '4',
    name: 'Content Publishing',
    target: 20,
    current: 15,
    percentage: 75.0,
    period: 'This Month',
    status: 'behind'
  }
];

class AnalyticsService {
  // Simulate network delay
  private delay(ms: number = 500): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  async getAnalyticsData(filters: AnalyticsFilters): Promise<AnalyticsData> {
    await this.delay();
    
    // In a real implementation, this would make an API call with filters
    const data = { ...mockAnalyticsData };
    
    // Simulate filtering by regenerating traffic data for different periods
    if (filters.period !== '30d') {
      const days = TIME_PERIODS.find(p => p.value === filters.period)?.days || 30;
      data.traffic = generateTrafficData(days);
      data.userActivity = generateUserActivity(days);
      
      // Adjust overview stats based on period
      const multiplier = days / 30;
      data.overview = {
        ...data.overview,
        totalViews: Math.round(data.overview.totalViews * multiplier),
        uniqueVisitors: Math.round(data.overview.uniqueVisitors * multiplier),
        newUsers: Math.round(data.overview.newUsers * multiplier),
        returningUsers: Math.round(data.overview.returningUsers * multiplier)
      };
    }
    
    return data;
  }

  async getAnalyticsOverview(filters: AnalyticsFilters): Promise<AnalyticsOverview> {
    await this.delay(200);
    const data = await this.getAnalyticsData(filters);
    return data.overview;
  }

  async getTrafficData(filters: AnalyticsFilters): Promise<TrafficData[]> {
    await this.delay(300);
    const data = await this.getAnalyticsData(filters);
    return data.traffic;
  }

  async getTopPosts(filters: AnalyticsFilters): Promise<TopPost[]> {
    await this.delay(250);
    const data = await this.getAnalyticsData(filters);
    return data.topPosts;
  }

  async getAlerts(): Promise<AnalyticsAlert[]> {
    await this.delay(150);
    return [...mockAlerts];
  }

  async getGoals(): Promise<GoalTracking[]> {
    await this.delay(200);
    return [...mockGoals];
  }

  async markAlertAsRead(alertId: string): Promise<void> {
    await this.delay(100);
    const alert = mockAlerts.find(a => a.id === alertId);
    if (alert) {
      alert.isRead = true;
    }
  }

  async exportData(format: 'pdf' | 'csv' | 'excel', filters: AnalyticsFilters): Promise<Blob> {
    await this.delay(1000);
    
    // In a real implementation, this would generate actual export files
    const data = await this.getAnalyticsData(filters);
    const exportContent = JSON.stringify(data, null, 2);
    
    return new Blob([exportContent], { 
      type: format === 'csv' ? 'text/csv' : 'application/json' 
    });
  }
}

export const analyticsService = new AnalyticsService();