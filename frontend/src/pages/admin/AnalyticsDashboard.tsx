/**
 * AnalyticsDashboard - Comprehensive admin analytics dashboard
 * Provides detailed insights into site performance, user behavior, and content metrics
 */

import React, { useState, useEffect, useCallback } from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { LineChart, BarChart, PieChart } from '@/components/charts';
import { createLogger, reportError } from '@/utils/logger';
import { analyticsService, TIME_PERIODS } from '@/services/analyticsService';
import { 
  AnalyticsData, 
  AnalyticsFilters, 
  TimePeriod, 
  AnalyticsAlert,
  GoalTracking,
  LineChartData,
  ChartDataPoint
} from '@/types/analytics';
import {
  BarChart3,
  Users,
  Eye,
  Clock,
  TrendingUp,
  TrendingDown,
  Globe,
  Download,
  RefreshCw,
  Calendar,
  AlertTriangle,
  CheckCircle,
  Info,
  Target,
  Search,
  FileText,
  MessageCircle
} from 'lucide-react';

export const AnalyticsDashboard: React.FC = () => {
  // Initialize logger for this component
  const log = createLogger('AnalyticsDashboard');
  
  const [data, setData] = useState<AnalyticsData | null>(null);
  const [alerts, setAlerts] = useState<AnalyticsAlert[]>([]);
  const [goals, setGoals] = useState<GoalTracking[]>([]);
  const [filters, setFilters] = useState<AnalyticsFilters>({
    period: '30d'
  });
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const [autoRefresh, setAutoRefresh] = useState(false);

  // Load analytics data
  const loadData = useCallback(async (showRefreshing = false) => {
    const action = showRefreshing ? 'refreshAnalyticsData' : 'loadAnalyticsData';
    log.info(`Starting analytics data ${showRefreshing ? 'refresh' : 'load'}`, action, {
      filters,
      showRefreshing
    });

    try {
      if (showRefreshing) setRefreshing(true);
      else setLoading(true);

      log.startTimer('analyticsDataLoad');

      const [analyticsData, alertsData, goalsData] = await Promise.all([
        analyticsService.getAnalyticsData(filters),
        analyticsService.getAlerts(),
        analyticsService.getGoals()
      ]);

      setData(analyticsData);
      setAlerts(alertsData);
      setGoals(goalsData);

      log.endTimer('analyticsDataLoad');
      log.info(`Analytics data ${showRefreshing ? 'refresh' : 'load'} completed successfully`, action, {
        dataPointsLoaded: analyticsData?.traffic?.length || 0,
        alertsCount: alertsData?.length || 0,
        goalsCount: goalsData?.length || 0
      });
    } catch (error) {
      log.endTimer('analyticsDataLoad');
      log.error(`Failed to ${showRefreshing ? 'refresh' : 'load'} analytics data`, action, {
        filters,
        showRefreshing,
        errorMessage: (error as Error).message
      }, error as Error);

      // Report critical analytics loading errors
      await reportError(error as Error, {
        component: 'AnalyticsDashboard',
        action,
        extra: { filters, showRefreshing }
      });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [filters, log]);

  // Handle period change
  const handlePeriodChange = (period: TimePeriod['value']) => {
    setFilters(prev => ({ ...prev, period }));
  };

  // Handle export
  const handleExport = async (format: 'pdf' | 'csv' | 'excel') => {
    try {
      const blob = await analyticsService.exportData(format, filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `analytics-${filters.period}.${format}`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      log.error(`Analytics export failed for format ${format}`, 'handleExport', {
        format,
        filters,
        errorMessage: (error as Error).message
      }, error as Error);
      
      // Report export failures
      await reportError(error as Error, {
        component: 'AnalyticsDashboard',
        action: 'handleExport',
        extra: { format, filters }
      });
    }
  };

  // Auto refresh effect
  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (autoRefresh) {
      interval = setInterval(() => {
        loadData(true);
      }, 30000); // Refresh every 30 seconds
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [autoRefresh, loadData]);

  // Load data when filters change
  useEffect(() => {
    loadData();
  }, [loadData]);

  // Prepare chart data
  const trafficChartData: LineChartData[] = data ? [
    {
      name: 'Page Views',
      data: data.traffic.map(d => ({ x: d.date, y: d.views })),
      color: '#3B82F6'
    },
    {
      name: 'Unique Visitors',
      data: data.traffic.map(d => ({ x: d.date, y: d.visitors })),
      color: '#10B981'
    },
    {
      name: 'Sessions',
      data: data.traffic.map(d => ({ x: d.date, y: d.sessions })),
      color: '#F59E0B'
    }
  ] : [];

  const topPostsChartData: ChartDataPoint[] = data ? 
    data.topPosts.slice(0, 6).map(post => ({
      name: post.title.length > 20 ? post.title.substring(0, 20) + '...' : post.title,
      value: post.views
    })) : [];

  const sourceChartData: ChartDataPoint[] = data ? 
    data.sources.map(source => ({
      name: source.source,
      value: source.visits,
      percentage: source.percentage
    })) : [];

  const deviceChartData: ChartDataPoint[] = data ? 
    data.devices.map(device => ({
      name: device.type,
      value: device.visits,
      percentage: device.percentage
    })) : [];

  const formatDuration = (seconds: number) => {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}m ${remainingSeconds}s`;
  };

  const getIcon = (alertType: AnalyticsAlert['type']) => {
    switch (alertType) {
      case 'success': return <CheckCircle className="w-4 h-4" />;
      case 'warning': return <AlertTriangle className="w-4 h-4" />;
      case 'error': return <AlertTriangle className="w-4 h-4" />;
      case 'info': return <Info className="w-4 h-4" />;
    }
  };

  const getAlertColor = (alertType: AnalyticsAlert['type']) => {
    switch (alertType) {
      case 'success': return 'text-green-600 dark:text-green-400';
      case 'warning': return 'text-yellow-600 dark:text-yellow-400';
      case 'error': return 'text-red-600 dark:text-red-400';
      case 'info': return 'text-blue-600 dark:text-blue-400';
    }
  };

  const getGoalColor = (status: GoalTracking['status']) => {
    switch (status) {
      case 'exceeded': return 'text-green-600 dark:text-green-400';
      case 'on-track': return 'text-blue-600 dark:text-blue-400';
      case 'behind': return 'text-red-600 dark:text-red-400';
    }
  };

  if (loading && !data) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-400">Loading analytics data...</p>
        </div>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center">
        <p className="text-red-600 dark:text-red-400">Failed to load analytics data</p>
      </div>
    );
  }

  return (
    <>
      <Helmet>
        <title>Analytics Dashboard - Maple Blog Admin</title>
        <meta name="description" content="Comprehensive analytics dashboard for site performance monitoring and insights." />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-7xl mx-auto">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between mb-8">
              <div className="mb-4 lg:mb-0">
                <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2 flex items-center">
                  <BarChart3 className="w-8 h-8 mr-3 text-blue-600 dark:text-blue-400" />
                  Analytics Dashboard
                </h1>
                <p className="text-lg text-gray-600 dark:text-gray-400">
                  Comprehensive insights into site performance and user behavior
                </p>
              </div>

              <div className="flex flex-col sm:flex-row gap-4">
                {/* Time Period Selector */}
                <div className="flex items-center gap-2">
                  <Calendar className="w-5 h-5 text-gray-500" />
                  <select
                    value={filters.period}
                    onChange={(e) => handlePeriodChange(e.target.value as TimePeriod['value'])}
                    className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  >
                    {TIME_PERIODS.filter(p => p.value !== 'custom').map(period => (
                      <option key={period.value} value={period.value}>
                        {period.label}
                      </option>
                    ))}
                  </select>
                </div>

                {/* Controls */}
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setAutoRefresh(!autoRefresh)}
                    className={`px-3 py-2 rounded-md transition-colors ${
                      autoRefresh 
                        ? 'bg-blue-600 text-white' 
                        : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600'
                    }`}
                  >
                    <RefreshCw className={`w-4 h-4 ${autoRefresh ? 'animate-spin' : ''}`} />
                  </button>

                  <button
                    onClick={() => loadData(true)}
                    disabled={refreshing}
                    className="px-3 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-md hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors disabled:opacity-50"
                  >
                    <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
                  </button>

                  <div className="relative group">
                    <button className="px-3 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors flex items-center gap-2">
                      <Download className="w-4 h-4" />
                      Export
                    </button>
                    <div className="absolute right-0 mt-2 w-32 bg-white dark:bg-gray-800 rounded-md shadow-lg border border-gray-200 dark:border-gray-700 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                      <button
                        onClick={() => handleExport('csv')}
                        className="block w-full px-4 py-2 text-left text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 first:rounded-t-md"
                      >
                        CSV
                      </button>
                      <button
                        onClick={() => handleExport('excel')}
                        className="block w-full px-4 py-2 text-left text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                      >
                        Excel
                      </button>
                      <button
                        onClick={() => handleExport('pdf')}
                        className="block w-full px-4 py-2 text-left text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 last:rounded-b-md"
                      >
                        PDF
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Alerts */}
            {alerts.filter(a => !a.isRead).length > 0 && (
              <div className="mb-6">
                <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                    Recent Alerts
                  </h3>
                  <div className="space-y-2">
                    {alerts.filter(a => !a.isRead).slice(0, 3).map(alert => (
                      <div key={alert.id} className="flex items-center gap-3 p-2 rounded-md bg-gray-50 dark:bg-gray-700">
                        <div className={getAlertColor(alert.type)}>
                          {getIcon(alert.type)}
                        </div>
                        <div className="flex-1">
                          <p className="text-sm font-medium text-gray-900 dark:text-white">
                            {alert.title}
                          </p>
                          <p className="text-xs text-gray-600 dark:text-gray-400">
                            {alert.message}
                          </p>
                        </div>
                        <span className="text-xs text-gray-500 dark:text-gray-500">
                          {new Date(alert.timestamp).toLocaleTimeString()}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* KPI Cards */}
            <div className="grid grid-cols-2 lg:grid-cols-4 xl:grid-cols-6 gap-4 mb-8">
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Total Views</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">
                      {data.overview.totalViews.toLocaleString()}
                    </p>
                  </div>
                  <div className="p-2 bg-blue-100 dark:bg-blue-900 rounded-lg">
                    <Eye className="w-6 h-6 text-blue-600 dark:text-blue-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Visitors</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">
                      {data.overview.uniqueVisitors.toLocaleString()}
                    </p>
                  </div>
                  <div className="p-2 bg-green-100 dark:bg-green-900 rounded-lg">
                    <Users className="w-6 h-6 text-green-600 dark:text-green-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Avg Session</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">
                      {formatDuration(data.overview.avgSessionDuration)}
                    </p>
                  </div>
                  <div className="p-2 bg-purple-100 dark:bg-purple-900 rounded-lg">
                    <Clock className="w-6 h-6 text-purple-600 dark:text-purple-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Bounce Rate</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">
                      {data.overview.bounceRate}%
                    </p>
                  </div>
                  <div className="p-2 bg-orange-100 dark:bg-orange-900 rounded-lg">
                    {data.overview.bounceRate > 60 ? 
                      <TrendingDown className="w-6 h-6 text-orange-600 dark:text-orange-400" /> :
                      <TrendingUp className="w-6 h-6 text-orange-600 dark:text-orange-400" />
                    }
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Total Posts</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">
                      {data.overview.totalPosts}
                    </p>
                  </div>
                  <div className="p-2 bg-indigo-100 dark:bg-indigo-900 rounded-lg">
                    <FileText className="w-6 h-6 text-indigo-600 dark:text-indigo-400" />
                  </div>
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">Comments</p>
                    <p className="text-2xl font-bold text-gray-900 dark:text-white">
                      {data.overview.totalComments}
                    </p>
                  </div>
                  <div className="p-2 bg-pink-100 dark:bg-pink-900 rounded-lg">
                    <MessageCircle className="w-6 h-6 text-pink-600 dark:text-pink-400" />
                  </div>
                </div>
              </div>
            </div>

            {/* Goals Progress */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 mb-8">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4 flex items-center">
                <Target className="w-5 h-5 mr-2 text-blue-600 dark:text-blue-400" />
                Goal Progress
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                {goals.map(goal => (
                  <div key={goal.id} className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                    <div className="flex items-center justify-between mb-2">
                      <h4 className="text-sm font-medium text-gray-900 dark:text-white">
                        {goal.name}
                      </h4>
                      <span className={`text-sm font-semibold ${getGoalColor(goal.status)}`}>
                        {goal.percentage.toFixed(1)}%
                      </span>
                    </div>
                    <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2 mb-2">
                      <div
                        className={`h-2 rounded-full transition-all duration-300 ${
                          goal.status === 'exceeded' ? 'bg-green-600' :
                          goal.status === 'on-track' ? 'bg-blue-600' : 'bg-red-600'
                        }`}
                        style={{ width: `${Math.min(goal.percentage, 100)}%` }}
                      />
                    </div>
                    <div className="flex justify-between text-xs text-gray-600 dark:text-gray-400">
                      <span>{goal.current.toLocaleString()}</span>
                      <span>{goal.target.toLocaleString()}</span>
                    </div>
                    <p className="text-xs text-gray-500 dark:text-gray-500 mt-1">
                      {goal.period}
                    </p>
                  </div>
                ))}
              </div>
            </div>

            {/* Main Charts Grid */}
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-8">
              {/* Traffic Trends */}
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Traffic Trends
                </h3>
                <div className="h-80">
                  <LineChart 
                    data={trafficChartData}
                    height={320}
                    className="w-full"
                  />
                </div>
              </div>

              {/* Top Performing Posts */}
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Top Performing Posts
                </h3>
                <div className="h-80">
                  <BarChart 
                    data={topPostsChartData}
                    height={320}
                    orientation="horizontal"
                    className="w-full"
                  />
                </div>
              </div>
            </div>

            {/* Secondary Charts Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6 mb-8">
              {/* Traffic Sources */}
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Traffic Sources
                </h3>
                <PieChart 
                  data={sourceChartData}
                  width={280}
                  height={300}
                  className="mx-auto"
                />
              </div>

              {/* Device Breakdown */}
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Device Breakdown
                </h3>
                <PieChart 
                  data={deviceChartData}
                  width={280}
                  height={300}
                  innerRadius={60}
                  className="mx-auto"
                />
              </div>

              {/* Geographic Distribution */}
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4 flex items-center">
                  <Globe className="w-5 h-5 mr-2 text-blue-600 dark:text-blue-400" />
                  Top Countries
                </h3>
                <div className="space-y-3">
                  {data.geographic.slice(0, 6).map((country, i) => (
                    <div key={country.code} className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-gray-900 dark:text-white">
                          {i + 1}. {country.country}
                        </span>
                      </div>
                      <div className="text-right">
                        <p className="text-sm font-semibold text-gray-900 dark:text-white">
                          {country.visits.toLocaleString()}
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-500">
                          {country.percentage.toFixed(1)}%
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            {/* Content Performance Table */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 mb-8">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Content Performance by Category
              </h3>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700">
                      <th className="text-left py-3 px-4 text-sm font-semibold text-gray-900 dark:text-white">
                        Category
                      </th>
                      <th className="text-right py-3 px-4 text-sm font-semibold text-gray-900 dark:text-white">
                        Posts
                      </th>
                      <th className="text-right py-3 px-4 text-sm font-semibold text-gray-900 dark:text-white">
                        Total Views
                      </th>
                      <th className="text-right py-3 px-4 text-sm font-semibold text-gray-900 dark:text-white">
                        Avg Views
                      </th>
                      <th className="text-right py-3 px-4 text-sm font-semibold text-gray-900 dark:text-white">
                        Engagement
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.contentPerformance.map((category, i) => (
                      <tr key={i} className="border-b border-gray-100 dark:border-gray-700">
                        <td className="py-3 px-4 text-sm text-gray-900 dark:text-white">
                          {category.category}
                        </td>
                        <td className="py-3 px-4 text-sm text-gray-900 dark:text-white text-right">
                          {category.posts}
                        </td>
                        <td className="py-3 px-4 text-sm text-gray-900 dark:text-white text-right">
                          {category.totalViews.toLocaleString()}
                        </td>
                        <td className="py-3 px-4 text-sm text-gray-900 dark:text-white text-right">
                          {category.avgViews.toLocaleString()}
                        </td>
                        <td className="py-3 px-4 text-sm text-gray-900 dark:text-white text-right">
                          <div className="flex items-center justify-end gap-2">
                            <span>{category.engagement}%</span>
                            <div className="w-12 bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                              <div
                                className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                                style={{ width: `${category.engagement}%` }}
                              />
                            </div>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Search Queries */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4 flex items-center">
                <Search className="w-5 h-5 mr-2 text-blue-600 dark:text-blue-400" />
                Popular Search Queries
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {data.searchQueries.map((query, i) => (
                  <div key={i} className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                    <p className="text-sm font-medium text-gray-900 dark:text-white mb-2">
                      &ldquo;{query.query}&rdquo;
                    </p>
                    <div className="flex justify-between text-xs text-gray-600 dark:text-gray-400">
                      <span>{query.count} searches</span>
                      <span>{query.clickThrough}% CTR</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default AnalyticsDashboard;