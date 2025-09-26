/* eslint-disable react-refresh/only-export-components */
import React from 'react';
import { Card, Statistic, Skeleton, Tooltip, Badge } from 'antd';
import {
  ArrowUpOutlined,
  ArrowDownOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons';
import classNames from 'classnames';
import type { ReactNode } from 'react';

export interface StatCardProps {
  title: string;
  value: number | string;
  precision?: number;
  suffix?: string;
  prefix?: ReactNode;
  icon?: ReactNode;
  trend?: {
    value: number;
    isPositive?: boolean;
    label?: string;
  };
  extra?: ReactNode;
  loading?: boolean;
  size?: 'small' | 'default' | 'large';
  status?: 'default' | 'success' | 'warning' | 'error';
  tooltip?: string;
  className?: string;
  style?: React.CSSProperties;
  onClick?: () => void;
  formatter?: (value: number | string) => ReactNode;
}

type BadgeStatus = 'success' | 'processing' | 'default' | 'error' | 'warning';

const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  precision = 0,
  suffix,
  prefix,
  icon,
  trend,
  extra,
  loading = false,
  size = 'default',
  status = 'default',
  tooltip,
  className,
  style,
  onClick,
  formatter,
}) => {
  // Status color mapping
  const statusColors = {
    default: '#1890ff',
    success: '#52c41a',
    warning: '#faad14',
    error: '#ff4d4f',
  };

  // Render trend indicator
  const renderTrend = () => {
    if (!trend) return null;

    const { value: trendValue, isPositive, label } = trend;
    const isUp = isPositive !== undefined ? isPositive : trendValue > 0;
    const color = isUp ? '#52c41a' : '#ff4d4f';
    const TrendIcon = isUp ? ArrowUpOutlined : ArrowDownOutlined;

    return (
      <div
        className="flex items-center gap-1 text-sm"
        style={{ color }}
      >
        <TrendIcon />
        <span>{Math.abs(trendValue)}%</span>
        {label && <span className="text-gray-500 ml-1">{label}</span>}
      </div>
    );
  };

  // Render card header
  const renderHeader = () => (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2">
        <span className="text-gray-600 font-medium">{title}</span>
        {tooltip && (
          <Tooltip title={tooltip}>
            <InfoCircleOutlined className="text-gray-400 text-xs" />
          </Tooltip>
        )}
      </div>
      {extra}
    </div>
  );

  // Render status indicator
  const renderStatus = () => {
    if (status === 'default') return null;

    const badgeStatus: BadgeStatus = status === 'error' ? 'error' : status as BadgeStatus;

    return (
      <Badge
        status={badgeStatus}
        className="absolute top-3 right-3"
      />
    );
  };

  // Card content
  const cardContent = (
    <div className="relative">
      {renderStatus()}

      <div className="mb-3">
        {renderHeader()}
      </div>

      <div className="flex items-end justify-between">
        <div className="flex items-center gap-3">
          {icon && (
            <div
              className="p-3 rounded-lg bg-opacity-10"
              style={{
                backgroundColor: statusColors[status],
                color: statusColors[status],
              }}
            >
              {icon}
            </div>
          )}

          <div>
            {loading ? (
              <Skeleton.Input
                active
                size={size === 'large' ? 'large' : 'default'}
                style={{ width: 120 }}
              />
            ) : (
              <Statistic
                value={value}
                precision={precision}
                suffix={suffix}
                prefix={prefix}
                {...(formatter && { formatter })}
                valueStyle={{
                  fontSize: size === 'large' ? '2rem' : size === 'small' ? '1.25rem' : '1.5rem',
                  fontWeight: 600,
                  color: statusColors[status],
                }}
              />
            )}
          </div>
        </div>

        {trend && !loading && (
          <div className="text-right">
            {renderTrend()}
          </div>
        )}
      </div>
    </div>
  );

  const cardProps = {
    className: classNames(
      'stat-card',
      `stat-card--${size}`,
      `stat-card--${status}`,
      {
        'cursor-pointer hover:shadow-md transition-shadow': onClick,
        'stat-card--loading': loading,
      },
      className
    ),
    style: {
      minHeight: size === 'large' ? 160 : size === 'small' ? 100 : 120,
      ...style,
    },
    onClick,
    hoverable: !!onClick,
    bordered: false,
  };

  return (
    <Card {...cardProps}>
      {cardContent}
    </Card>
  );
};

// Pre-defined stat card variants
export const StatCardVariants = {
  // User statistics card
  UserStats: (props: Omit<StatCardProps, 'icon'>) => (
    <StatCard
      {...props}
      icon={<div className="text-2xl">👥</div>}
      status="success"
    />
  ),

  // Content statistics card
  ContentStats: (props: Omit<StatCardProps, 'icon'>) => (
    <StatCard
      {...props}
      icon={<div className="text-2xl">📝</div>}
      status="default"
    />
  ),

  // System statistics card
  SystemStats: (props: Omit<StatCardProps, 'icon'>) => (
    <StatCard
      {...props}
      icon={<div className="text-2xl">⚡</div>}
      status="warning"
    />
  ),

  // Performance statistics card
  PerformanceStats: (props: Omit<StatCardProps, 'icon'>) => (
    <StatCard
      {...props}
      icon={<div className="text-2xl">📊</div>}
      status="success"
    />
  ),
};

// Predefined formatters
export const StatCardFormatters = {
  // Number formatter with K/M/B suffixes
  number: (value: number | string): ReactNode => {
    const num = typeof value === 'string' ? parseFloat(value) : value;
    if (num >= 1000000000) {
      return `${(num / 1000000000).toFixed(1)}B`;
    }
    if (num >= 1000000) {
      return `${(num / 1000000).toFixed(1)}M`;
    }
    if (num >= 1000) {
      return `${(num / 1000).toFixed(1)}K`;
    }
    return num.toString();
  },

  // Percentage formatter
  percentage: (value: number | string): ReactNode => {
    const num = typeof value === 'string' ? parseFloat(value) : value;
    return `${num.toFixed(1)}%`;
  },

  // Currency formatter
  currency: (value: number | string): ReactNode => {
    const num = typeof value === 'string' ? parseFloat(value) : value;
    return new Intl.NumberFormat('zh-CN', {
      style: 'currency',
      currency: 'CNY',
    }).format(num);
  },

  // Duration formatter (seconds to human readable)
  duration: (value: number | string): ReactNode => {
    const seconds = typeof value === 'string' ? parseFloat(value) : value;
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = Math.floor(seconds % 60);

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    }
    if (minutes > 0) {
      return `${minutes}m ${remainingSeconds}s`;
    }
    return `${remainingSeconds}s`;
  },

  // File size formatter
  fileSize: (value: number | string): ReactNode => {
    const bytes = typeof value === 'string' ? parseFloat(value) : value;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    if (bytes === 0) return '0 B';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`;
  },
};

export default StatCard;