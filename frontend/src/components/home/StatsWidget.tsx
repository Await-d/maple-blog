// @ts-nocheck
/**
 * StatsWidget component - Display site statistics with animated counters
 * Features: Animated counting, charts, growth indicators, responsive design
 */

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { Link } from 'react-router-dom';
import {
  BarChart3,
  TrendingUp,
  TrendingDown,
  Users,
  FileText,
  Eye,
  MessageCircle,
  Heart as _Heart,
  BookOpen,
  Calendar,
  Clock,
  Target as _Target,
  Award,
  Zap,
  Activity,
  PieChart,
  LineChart,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { useSiteStats } from '../../services/home/homeApi';
import { useIsMobile, useAccessibilitySettings } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { SiteStats as _SiteStats } from '../../types/home';

interface StatsWidgetProps {
  className?: string;
  title?: string;
  layout?: 'grid' | 'horizontal' | 'vertical' | 'compact';
  showGrowth?: boolean;
  showCharts?: boolean;
  animated?: boolean;
  period?: 'today' | 'week' | 'month' | 'all';
}

interface StatItemProps {
  icon: React.ReactNode;
  label: string;
  value: number;
  suffix?: string;
  growth?: number;
  color?: string;
  animated?: boolean;
  delay?: number;
  className?: string;
}

interface ChartData {
  label: string;
  value: number;
  color: string;
}

const StatItem: React.FC<StatItemProps> = ({
  icon,
  label,
  value,
  suffix = '',
  growth,
  color = 'gray',
  animated = true,
  delay = 0,
  className,
}) => {
  const [displayValue, setDisplayValue] = useState(0);
  const [hasAnimated, setHasAnimated] = useState(false);
  const accessibility = useAccessibilitySettings();
  const elementRef = useRef<HTMLDivElement>(null);

  // Animate counter
  const animateValue = useCallback(() => {
    const duration = 2000; // 2 seconds
    const steps = 60;
    const stepValue = value / steps;
    const stepDelay = duration / steps;

    let currentStep = 0;

    const timer = setInterval(() => {
      currentStep++;
      const currentValue = Math.min(stepValue * currentStep, value);
      setDisplayValue(Math.floor(currentValue));

      if (currentStep >= steps) {
        setDisplayValue(value);
        clearInterval(timer);
      }
    }, stepDelay);

    setTimeout(() => {
      if (timer) clearInterval(timer);
      setDisplayValue(value);
    }, duration + 100);
  }, [value]);

  // Intersection Observer for animation trigger
  useEffect(() => {
    if (!animated || accessibility.reduceMotion || hasAnimated) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setHasAnimated(true);
          animateValue();
        }
      },
      { threshold: 0.1 }
    );

    if (elementRef.current) {
      observer.observe(elementRef.current);
    }

    return () => observer.disconnect();
  }, [animated, accessibility.reduceMotion, hasAnimated, animateValue]);

  // Format large numbers
  const formatValue = (num: number): string => {
    if (num >= 1000000) {
      return (num / 1000000).toFixed(1) + 'M';
    }
    if (num >= 1000) {
      return (num / 1000).toFixed(1) + 'K';
    }
    return num.toLocaleString();
  };

  const colorClasses = {
    gray: 'text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800',
    blue: 'text-blue-600 dark:text-blue-400 bg-blue-100 dark:bg-blue-900/20',
    green: 'text-green-600 dark:text-green-400 bg-green-100 dark:bg-green-900/20',
    orange: 'text-orange-600 dark:text-orange-400 bg-orange-100 dark:bg-orange-900/20',
    red: 'text-red-600 dark:text-red-400 bg-red-100 dark:bg-red-900/20',
    purple: 'text-purple-600 dark:text-purple-400 bg-purple-100 dark:bg-purple-900/20',
  };

  return (
    <div
      ref={elementRef}
      className={cn(
        'relative p-6 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 transition-all duration-300 hover:shadow-lg dark:hover:shadow-2xl group',
        className
      )}
      style={{ animationDelay: `${delay}ms` }}
    >
      {/* Background Pattern */}
      <div className="absolute inset-0 opacity-5 bg-gradient-to-br from-current to-transparent rounded-xl" />

      <div className="relative">
        {/* Icon */}
        <div className={cn(
          'w-12 h-12 rounded-lg flex items-center justify-center mb-4 transition-transform group-hover:scale-110',
          colorClasses[color as keyof typeof colorClasses] || colorClasses.gray
        )}>
          {icon}
        </div>

        {/* Value */}
        <div className="mb-2">
          <span className="text-3xl font-bold text-gray-900 dark:text-white">
            {formatValue(displayValue)}
          </span>
          {suffix && (
            <span className="text-lg text-gray-500 dark:text-gray-400 ml-1">
              {suffix}
            </span>
          )}
        </div>

        {/* Label */}
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
            {label}
          </span>

          {/* Growth Indicator */}
          {growth !== undefined && (
            <div className={cn(
              'flex items-center space-x-1 px-2 py-1 rounded-full text-xs font-medium',
              growth > 0
                ? 'bg-green-100 dark:bg-green-900/20 text-green-700 dark:text-green-300'
                : growth < 0
                  ? 'bg-red-100 dark:bg-red-900/20 text-red-700 dark:text-red-300'
                  : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400'
            )}>
              {growth > 0 ? (
                <TrendingUp size={12} />
              ) : growth < 0 ? (
                <TrendingDown size={12} />
              ) : (
                <Activity size={12} />
              )}
              <span>
                {growth > 0 ? '+' : ''}{growth.toFixed(1)}%
              </span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

// Mini chart component for visual representation
const MiniChart: React.FC<{ data: ChartData[]; type?: 'pie' | 'bar' }> = ({
  data,
  type = 'pie',
}) => {
  if (type === 'bar') {
    const maxValue = Math.max(...data.map(d => d.value));
    return (
      <div className="flex items-end space-x-1 h-16">
        {data.map((item, index) => (
          <div
            key={index}
            className="flex-1 bg-orange-200 dark:bg-orange-900/30 rounded-t"
            style={{
              height: `${(item.value / maxValue) * 100}%`,
              backgroundColor: item.color,
            }}
            title={`${item.label}: ${item.value}`}
          />
        ))}
      </div>
    );
  }

  // Simple pie chart representation
  const total = data.reduce((sum, item) => sum + item.value, 0);
  let currentAngle = 0;

  return (
    <div className="relative w-16 h-16">
      <svg className="w-full h-full transform -rotate-90">
        {data.map((item, index) => {
          const percentage = item.value / total;
          const strokeDasharray = `${percentage * 100} ${100 - percentage * 100}`;
          const strokeDashoffset = -currentAngle;
          currentAngle += percentage * 100;

          return (
            <circle
              key={index}
              cx="32"
              cy="32"
              r="28"
              fill="none"
              stroke={item.color}
              strokeWidth="8"
              strokeDasharray={strokeDasharray}
              strokeDashoffset={strokeDashoffset}
              className="transition-all duration-1000"
              style={{ transformOrigin: 'center' }}
            />
          );
        })}
      </svg>
    </div>
  );
};

export const StatsWidget: React.FC<StatsWidgetProps> = ({
  className,
  title = '网站统计',
  layout = 'grid',
  showGrowth = true,
  showCharts = true,
  animated = true,
  period: _period = 'all',
}) => {
  const _isMobile = useIsMobile();
  const accessibility = useAccessibilitySettings();

  // API data
  const { data: stats, isLoading, error, refetch } = useSiteStats();

  // Mock growth data (in real app, this would come from API)
  const growthData = {
    posts: 12.5,
    users: 8.3,
    views: 15.7,
    comments: 5.2,
  };

  if (isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="h-8 w-32 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div className={cn(
          'grid gap-6',
          {
            'grid-cols-2 sm:grid-cols-4': layout === 'grid',
            'grid-cols-1': layout === 'vertical',
            'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4': layout === 'horizontal',
            'grid-cols-2': layout === 'compact',
          }
        )}>
          {Array.from({ length: 8 }, (_, index) => (
            <div
              key={index}
              className="bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse"
              style={{ height: layout === 'compact' ? '120px' : '160px' }}
            />
          ))}
        </div>
      </section>
    );
  }

  if (error || !stats) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
        <div className="text-center py-12">
          <BarChart3 size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            统计数据加载失败
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            无法获取网站统计信息
          </p>
          <Button onClick={() => refetch()} variant="outline" size="sm">
            重新加载
          </Button>
        </div>
      </section>
    );
  }

  const statItems = [
    {
      icon: <FileText size={24} />,
      label: '文章总数',
      value: stats.totalPosts,
      growth: showGrowth ? growthData.posts : undefined,
      color: 'blue',
    },
    {
      icon: <Eye size={24} />,
      label: '总浏览量',
      value: stats.totalViews,
      growth: showGrowth ? growthData.views : undefined,
      color: 'green',
    },
    {
      icon: <Users size={24} />,
      label: '注册用户',
      value: stats.totalUsers,
      growth: showGrowth ? growthData.users : undefined,
      color: 'orange',
    },
    {
      icon: <MessageCircle size={24} />,
      label: '评论总数',
      value: stats.totalComments,
      growth: showGrowth ? growthData.comments : undefined,
      color: 'purple',
    },
    {
      icon: <BookOpen size={24} />,
      label: '分类数量',
      value: stats.totalCategories,
      color: 'red',
    },
    {
      icon: <Award size={24} />,
      label: '活跃作者',
      value: stats.totalAuthors,
      color: 'green',
    },
    {
      icon: <Calendar size={24} />,
      label: '本月文章',
      value: stats.postsThisMonth,
      color: 'blue',
    },
    {
      icon: <Clock size={24} />,
      label: '阅读时长',
      value: stats.totalReadingTime,
      suffix: '分钟',
      color: 'orange',
    },
  ];

  // Chart data for visualization
  const chartData: ChartData[] = [
    { label: '文章', value: stats.totalPosts, color: '#3b82f6' },
    { label: '评论', value: stats.totalComments, color: '#8b5cf6' },
    { label: '分类', value: stats.totalCategories, color: '#ef4444' },
    { label: '作者', value: stats.totalAuthors, color: '#10b981' },
  ];

  return (
    <section className={cn('space-y-6', className)} role="region" aria-label={title}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <BarChart3 className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
          {stats.calculatedAt && (
            <span className="px-2 py-1 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-sm rounded">
              {new Date(stats.calculatedAt).toLocaleString('zh-CN')} 更新
            </span>
          )}
        </div>

        {/* Quick Actions */}
        <div className="flex items-center space-x-2">
          <Link to="/analytics">
            <Button variant="outline" size="sm" className="text-orange-600 hover:text-orange-700">
              详细分析
            </Button>
          </Link>
        </div>
      </div>

      {/* Stats Grid */}
      <div className={cn(
        'grid gap-4 sm:gap-6',
        {
          'grid-cols-2 sm:grid-cols-4': layout === 'grid',
          'grid-cols-1 space-y-4': layout === 'vertical',
          'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4': layout === 'horizontal',
          'grid-cols-2 gap-4': layout === 'compact',
        }
      )}>
        {statItems.map((item, index) => (
          <StatItem
            key={item.label}
            icon={item.icon}
            label={item.label}
            value={item.value}
            suffix={item.suffix}
            growth={item.growth}
            color={item.color}
            animated={animated && !accessibility.reduceMotion}
            delay={index * 100}
            className="animate-fade-in-up"
          />
        ))}
      </div>

      {/* Charts Section */}
      {showCharts && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Content Distribution */}
          <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                内容分布
              </h3>
              <PieChart size={20} className="text-gray-400" />
            </div>
            <div className="flex items-center space-x-6">
              <MiniChart data={chartData} type="pie" />
              <div className="space-y-2 flex-1">
                {chartData.map((item, index) => (
                  <div key={index} className="flex items-center space-x-2">
                    <div
                      className="w-3 h-3 rounded-full"
                      style={{ backgroundColor: item.color }}
                    />
                    <span className="text-sm text-gray-600 dark:text-gray-400">
                      {item.label}: {item.value.toLocaleString()}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Activity Overview */}
          <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                活跃度概览
              </h3>
              <LineChart size={20} className="text-gray-400" />
            </div>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-600 dark:text-gray-400">本周文章</span>
                <span className="font-semibold text-gray-900 dark:text-white">
                  {stats.postsThisWeek}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-600 dark:text-gray-400">今日文章</span>
                <span className="font-semibold text-gray-900 dark:text-white">
                  {stats.postsToday}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-600 dark:text-gray-400">月均文章</span>
                <span className="font-semibold text-gray-900 dark:text-white">
                  {stats.averagePostsPerMonth.toFixed(1)}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-gray-600 dark:text-gray-400">最近更新</span>
                <span className="font-semibold text-gray-900 dark:text-white">
                  {stats.lastPostDate
                    ? new Date(stats.lastPostDate).toLocaleDateString('zh-CN')
                    : '暂无'
                  }
                </span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Quick Insights */}
      <div className="bg-gradient-to-r from-orange-50 to-red-50 dark:from-orange-950/20 dark:to-red-950/20 rounded-xl p-6 border border-orange-200 dark:border-orange-800">
        <div className="flex items-center space-x-3 mb-4">
          <Zap className="text-orange-500" size={20} />
          <h3 className="font-semibold text-gray-900 dark:text-white">
            数据洞察
          </h3>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
          <div className="text-gray-600 dark:text-gray-400">
            平均每篇文章获得 <span className="font-semibold text-orange-600">
              {stats.totalPosts > 0 ? Math.round(stats.totalViews / stats.totalPosts) : 0}
            </span> 次浏览
          </div>
          <div className="text-gray-600 dark:text-gray-400">
            平均每个用户发表 <span className="font-semibold text-orange-600">
              {stats.totalAuthors > 0 ? Math.round(stats.totalPosts / stats.totalAuthors) : 0}
            </span> 篇文章
          </div>
          <div className="text-gray-600 dark:text-gray-400">
            评论参与率达 <span className="font-semibold text-orange-600">
              {stats.totalPosts > 0 ? ((stats.totalComments / stats.totalPosts) * 100).toFixed(1) : 0}%
            </span>
          </div>
        </div>
      </div>
    </section>
  );
};

/**
 * Usage:
 * <StatsWidget /> - Default grid layout with growth indicators
 * <StatsWidget layout="horizontal" showCharts={false} /> - Horizontal layout without charts
 * <StatsWidget layout="compact" animated={false} /> - Compact layout without animations
 *
 * Features:
 * - Multiple responsive layouts (grid, horizontal, vertical, compact)
 * - Animated counter with intersection observer
 * - Growth indicators with trend arrows
 * - Mini charts for data visualization
 * - Real-time statistics from API
 * - Loading states and error handling
 * - Accessibility support with reduced motion
 * - Data insights and analysis
 * - Mobile-friendly responsive design
 * - Customizable appearance and behavior
 */

export default StatsWidget;