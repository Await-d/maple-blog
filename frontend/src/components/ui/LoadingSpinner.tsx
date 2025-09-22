// @ts-nocheck
/**
 * LoadingSpinner - 通用加载动画组件
 * 提供多种尺寸和样式的加载动画
 */

import React from 'react';
import { cn } from '../../utils/cn';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl';
  color?: 'primary' | 'secondary' | 'white' | 'gray';
  className?: string;
  label?: string;
}

const sizeClasses = {
  sm: 'w-4 h-4',
  md: 'w-6 h-6',
  lg: 'w-8 h-8',
  xl: 'w-12 h-12',
};

const colorClasses = {
  primary: 'border-blue-600 border-t-transparent',
  secondary: 'border-gray-600 border-t-transparent',
  white: 'border-white border-t-transparent',
  gray: 'border-gray-300 border-t-gray-600',
};

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  color = 'primary',
  className,
  label,
}) => {
  return (
    <div
      className={cn('flex items-center justify-center', className)}
      role="status"
      aria-label={label || '加载中'}
    >
      <div
        className={cn(
          'animate-spin rounded-full border-2',
          sizeClasses[size],
          colorClasses[color]
        )}
      />
      {label && (
        <span className="ml-2 text-sm text-gray-600 dark:text-gray-400">
          {label}
        </span>
      )}
    </div>
  );
};

export default LoadingSpinner;