// @ts-nocheck
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect, useRef } from 'react';
import { message } from 'antd';
import { useDashboardStore } from '@/stores/dashboardStore';
import { useRealTimeStore } from '@/stores/realTimeStore';
import { ApiService } from '@/services/api';
import type { DashboardStats, SystemMetrics, HealthCheck, Activity } from '@/types';
import type { LineChartData } from '@/components/charts/LineChart';

// Dashboard query keys
const DASHBOARD_QUERY_KEYS = {
  stats: 'dashboard-stats',
  metrics: 'system-metrics',
  healthCheck: 'health-check',
  activities: 'recent-activities',
} as const;

// Custom hook for dashboard data management
export const useDashboard = () => {
  const queryClient = useQueryClient();
  const refreshIntervalRef = useRef<NodeJS.Timeout | null>(null);

  const {
    stats,
    metrics,
    healthCheck,
    activities,
    autoRefresh,
    refreshInterval,
    isRefreshing,
    setStats,
    setMetrics,
    setHealthCheck,
    setActivities,
    startRefresh,
    endRefresh,
  } = useDashboardStore();

  const { isConnected } = useRealTimeStore();

  // Fetch dashboard statistics
  const {
    data: statsData,
    isLoading: statsLoading,
    error: statsError,
    refetch: refetchStats,
  } = useQuery({
    queryKey: [DASHBOARD_QUERY_KEYS.stats],
    queryFn: () => ApiService.get<DashboardStats>('/dashboard/stats'),
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: autoRefresh ? refreshInterval * 1000 : false,
  });

  // Fetch system metrics
  const {
    data: metricsData,
    isLoading: metricsLoading,
    error: metricsError,
    refetch: refetchMetrics,
  } = useQuery({
    queryKey: [DASHBOARD_QUERY_KEYS.metrics],
    queryFn: () => ApiService.get<SystemMetrics>('/dashboard/metrics'),
    staleTime: 1 * 60 * 1000, // 1 minute
    refetchInterval: autoRefresh ? refreshInterval * 1000 : false,
  });

  // Fetch health check status
  const {
    data: healthData,
    isLoading: healthLoading,
    error: healthError,
    refetch: refetchHealth,
  } = useQuery({
    queryKey: [DASHBOARD_QUERY_KEYS.healthCheck],
    queryFn: () => ApiService.get<HealthCheck>('/dashboard/health'),
    staleTime: 30 * 1000, // 30 seconds
    refetchInterval: autoRefresh ? Math.min(refreshInterval, 30) * 1000 : false,
  });

  // Fetch recent activities
  const {
    data: activitiesData,
    isLoading: activitiesLoading,
    error: activitiesError,
    refetch: refetchActivities,
  } = useQuery({
    queryKey: [DASHBOARD_QUERY_KEYS.activities],
    queryFn: () => ApiService.get<Activity[]>('/dashboard/activities', { limit: 20 }),
    staleTime: 2 * 60 * 1000, // 2 minutes
    refetchInterval: autoRefresh ? refreshInterval * 1000 : false,
  });

  // Sync data to store when queries succeed
  useEffect(() => {
    if (statsData) {
      setStats(statsData);
    }
  }, [statsData, setStats]);

  useEffect(() => {
    if (metricsData) {
      setMetrics(metricsData);
    }
  }, [metricsData, setMetrics]);

  useEffect(() => {
    if (healthData) {
      setHealthCheck(healthData);
    }
  }, [healthData, setHealthCheck]);

  useEffect(() => {
    if (activitiesData) {
      setActivities(activitiesData);
    }
  }, [activitiesData, setActivities]);

  // Refresh all dashboard data
  const refreshAll = useCallback(async () => {
    if (isRefreshing) return;

    try {
      startRefresh();
      await Promise.all([
        refetchStats(),
        refetchMetrics(),
        refetchHealth(),
        refetchActivities(),
      ]);
      message.success('仪表盘数据已更新');
    } catch (error) {
      console.error('Failed to refresh dashboard data:', error);
      message.error('数据更新失败');
    } finally {
      endRefresh();
    }
  }, [
    isRefreshing,
    startRefresh,
    endRefresh,
    refetchStats,
    refetchMetrics,
    refetchHealth,
    refetchActivities,
  ]);

  // Invalidate specific query
  const invalidateQuery = useCallback((queryKey: string) => {
    queryClient.invalidateQueries({ queryKey: [queryKey] });
  }, [queryClient]);

  // Setup auto refresh
  useEffect(() => {
    if (autoRefresh && refreshInterval > 0) {
      refreshIntervalRef.current = setInterval(() => {
        if (!isRefreshing) {
          refreshAll();
        }
      }, refreshInterval * 1000);
    } else {
      if (refreshIntervalRef.current) {
        clearInterval(refreshIntervalRef.current);
        refreshIntervalRef.current = null;
      }
    }

    return () => {
      if (refreshIntervalRef.current) {
        clearInterval(refreshIntervalRef.current);
      }
    };
  }, [autoRefresh, refreshInterval, refreshAll, isRefreshing]);

  // Calculate loading states
  const isLoading = statsLoading || metricsLoading || healthLoading || activitiesLoading;
  const hasErrors = statsError || metricsError || healthError || activitiesError;

  // Get chart data for different visualizations
  const getChartData = useCallback((type: 'traffic' | 'performance' | 'users' | 'content') => {
    if (!stats) return null;

    switch (type) {
      case 'traffic':
        return {
          labels: ['今日', '昨日', '本周', '本月'],
          datasets: [
            {
              label: '页面浏览量',
              data: [
                stats.systemStats.viewsToday,
                stats.systemStats.viewsToday * 0.8,
                stats.systemStats.viewsToday * 5.2,
                stats.systemStats.viewsToday * 25.6,
              ],
              borderColor: '#1890ff',
              backgroundColor: 'rgba(24, 144, 255, 0.1)',
            },
          ],
        };

      case 'performance':
        return {
          labels: ['CPU', '内存', '磁盘', '网络'],
          datasets: [
            {
              label: '使用率 (%)',
              data: metrics ? [
                metrics.cpu.usage,
                metrics.memory.usage,
                metrics.disk.usage,
                (metrics.network.bytesIn + metrics.network.bytesOut) / 1024 / 1024 / 100, // Normalized
              ] : [0, 0, 0, 0],
              backgroundColor: [
                '#ff4d4f',
                '#faad14',
                '#52c41a',
                '#1890ff',
              ],
            },
          ],
        };

      case 'users':
        return {
          labels: ['活跃用户', '新用户', '总用户'],
          datasets: [
            {
              label: '用户统计',
              data: [
                stats.userStats.active,
                stats.userStats.newToday,
                stats.userStats.total,
              ],
              backgroundColor: ['#52c41a', '#1890ff', '#722ed1'],
            },
          ],
        };

      case 'content':
        return {
          labels: ['已发布', '草稿', '今日新增'],
          datasets: [
            {
              label: '内容统计',
              data: [
                stats.contentStats.publishedPosts,
                stats.contentStats.drafts,
                stats.contentStats.postsToday,
              ],
              backgroundColor: ['#52c41a', '#faad14', '#1890ff'],
            },
          ],
        };

      default:
        return null;
    }
  }, [stats, metrics]);

  // Get real-time data trends
  const getTrendData = useCallback(() => {
    if (!stats) return null;

    return {
      userTrend: stats.userStats.trend,
      contentTrend: stats.contentStats.trend,
      systemPerformance: stats.systemStats.performanceScore,
    };
  }, [stats]);

  // Export dashboard data
  const exportData = useCallback(async (format: 'json' | 'csv' | 'excel' = 'json') => {
    try {
      const response = await ApiService.get(`/dashboard/export?format=${format}`, {}, {
        responseType: 'blob',
      });

      // Create download link
      const blob = new Blob([response]);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `dashboard-export-${new Date().toISOString().split('T')[0]}.${format}`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      message.success('数据导出成功');
    } catch (error) {
      console.error('Export failed:', error);
      message.error('数据导出失败');
    }
  }, []);

  return {
    // Data
    stats: stats || statsData,
    metrics: metrics || metricsData,
    healthCheck: healthCheck || healthData,
    activities: activities || activitiesData,

    // Loading states
    isLoading,
    isRefreshing,
    hasErrors,

    // Connection status
    isConnected,

    // Actions
    refreshAll,
    invalidateQuery,
    exportData,

    // Chart data helpers
    getChartData,
    getTrendData,

    // Individual refresh functions
    refetchStats,
    refetchMetrics,
    refetchHealth,
    refetchActivities,
  };
};

// Hook for dashboard configuration
export const useDashboardConfig = () => {
  const {
    autoRefresh,
    refreshInterval,
    layout,
    chartConfigs,
    setAutoRefresh,
    setRefreshInterval,
    updateLayout,
    setChartConfig,
    removeChartConfig,
    saveLayoutPreset,
    loadLayoutPreset,
  } = useDashboardStore();

  const updateRefreshSettings = useCallback((settings: {
    autoRefresh?: boolean;
    refreshInterval?: number;
  }) => {
    if (settings.autoRefresh !== undefined) {
      setAutoRefresh(settings.autoRefresh);
    }
    if (settings.refreshInterval !== undefined) {
      setRefreshInterval(settings.refreshInterval);
    }
  }, [setAutoRefresh, setRefreshInterval]);

  const resetLayout = useCallback(() => {
    updateLayout({
      statsOrder: ['users', 'content', 'system', 'performance'],
      chartsOrder: ['traffic', 'performance', 'users', 'content'],
      widgetSizes: {
        statsCard: { width: 280, height: 120 },
        chartWidget: { width: 400, height: 300 },
        activityFeed: { width: 350, height: 400 },
      },
    });
  }, [updateLayout]);

  return {
    // Config state
    autoRefresh,
    refreshInterval,
    layout,
    chartConfigs,

    // Actions
    updateRefreshSettings,
    updateLayout,
    resetLayout,
    setChartConfig,
    removeChartConfig,
    saveLayoutPreset,
    loadLayoutPreset,
  };
};

export default useDashboard;