/**
 * Skeleton Loader Components
 * 
 * Accessible skeleton loading components that respect user preferences
 * for reduced motion and provide proper ARIA labels.
 */

import React from 'react';
import { cn } from '../../utils/cn';

// ============================================================================
// BASE SKELETON COMPONENT
// ============================================================================

export interface SkeletonProps {
  className?: string;
  width?: string | number;
  height?: string | number;
  variant?: 'rectangular' | 'circular' | 'text' | 'rounded';
  animation?: 'pulse' | 'wave' | 'none';
  children?: React.ReactNode;
  'aria-label'?: string;
}

export const Skeleton: React.FC<SkeletonProps> = ({
  className,
  width,
  height,
  variant = 'rectangular',
  animation = 'pulse',
  children,
  'aria-label': ariaLabel,
}) => {
  const style: React.CSSProperties = {};
  
  if (width !== undefined) {
    style.width = typeof width === 'number' ? `${width}px` : width;
  }
  
  if (height !== undefined) {
    style.height = typeof height === 'number' ? `${height}px` : height;
  }

  const baseClasses = 'bg-gray-200 dark:bg-gray-700';
  
  const variantClasses = {
    rectangular: '',
    circular: 'rounded-full',
    text: 'rounded-sm',
    rounded: 'rounded-lg',
  };

  const animationClasses = {
    pulse: 'animate-pulse',
    wave: 'animate-shimmer bg-gradient-to-r from-gray-200 via-gray-300 to-gray-200 dark:from-gray-700 dark:via-gray-600 dark:to-gray-700',
    none: '',
  };

  return (
    <div
      className={cn(
        baseClasses,
        variantClasses[variant],
        animationClasses[animation],
        className
      )}
      style={style}
      role="status"
      aria-label={ariaLabel || 'Loading content...'}
      aria-live="polite"
    >
      {children}
      <span className="sr-only">Loading...</span>
    </div>
  );
};

// ============================================================================
// USER TABLE SKELETON
// ============================================================================

export interface UserTableSkeletonProps {
  rows?: number;
  showActions?: boolean;
  className?: string;
}

export const UserTableSkeleton: React.FC<UserTableSkeletonProps> = ({
  rows = 5,
  showActions = true,
  className,
}) => {
  return (
    <div className={cn('space-y-4', className)} role="status" aria-label="Loading user list">
      {/* Table Header Skeleton */}
      <div className="flex items-center space-x-4 p-4 border-b">
        <Skeleton width={20} height={20} variant="rectangular" />
        <Skeleton width={120} height={16} variant="text" />
        <Skeleton width={80} height={16} variant="text" />
        <Skeleton width={100} height={16} variant="text" />
        <Skeleton width={120} height={16} variant="text" />
        <Skeleton width={140} height={16} variant="text" />
        <Skeleton width={100} height={16} variant="text" />
        {showActions && <Skeleton width={80} height={16} variant="text" />}
      </div>

      {/* Table Rows Skeleton */}
      {Array.from({ length: rows }).map((_, index) => (
        <div
          key={index}
          className="flex items-center space-x-4 p-4 hover:bg-gray-50 dark:hover:bg-gray-800/50"
        >
          {/* Checkbox */}
          <Skeleton width={20} height={20} variant="rectangular" />

          {/* User Info */}
          <div className="flex items-center space-x-3 flex-1">
            <Skeleton width={40} height={40} variant="circular" />
            <div className="space-y-2">
              <Skeleton width={160} height={16} variant="text" />
              <Skeleton width={120} height={14} variant="text" />
              <Skeleton width={180} height={12} variant="text" />
            </div>
          </div>

          {/* Role Badge */}
          <Skeleton width={60} height={24} variant="rounded" />

          {/* Status Badge */}
          <div className="flex items-center space-x-2">
            <Skeleton width={70} height={24} variant="rounded" />
            <Skeleton width={16} height={16} variant="circular" />
          </div>

          {/* Join Date */}
          <div className="space-y-1">
            <Skeleton width={100} height={14} variant="text" />
            <Skeleton width={60} height={12} variant="text" />
          </div>

          {/* Last Login */}
          <div className="space-y-1">
            <Skeleton width={120} height={14} variant="text" />
            <Skeleton width={80} height={12} variant="text" />
          </div>

          {/* Content Stats */}
          <div className="space-y-1">
            <Skeleton width={60} height={14} variant="text" />
            <Skeleton width={80} height={12} variant="text" />
          </div>

          {/* Actions */}
          {showActions && (
            <div className="flex items-center space-x-1">
              <Skeleton width={32} height={32} variant="rectangular" />
              <Skeleton width={32} height={32} variant="rectangular" />
              <Skeleton width={32} height={32} variant="rectangular" />
              <Skeleton width={32} height={32} variant="rectangular" />
            </div>
          )}
        </div>
      ))}

      <span className="sr-only">Loading user data, please wait...</span>
    </div>
  );
};

// ============================================================================
// USER CARD SKELETON
// ============================================================================

export interface UserCardSkeletonProps {
  count?: number;
  className?: string;
}

export const UserCardSkeleton: React.FC<UserCardSkeletonProps> = ({
  count = 8,
  className,
}) => {
  return (
    <div 
      className={cn('grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4', className)}
      role="status" 
      aria-label="Loading user cards"
    >
      {Array.from({ length: count }).map((_, index) => (
        <div key={index} className="border rounded-lg p-4 space-y-4">
          {/* Header with Avatar and Info */}
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <Skeleton width={48} height={48} variant="circular" />
              <div className="space-y-2">
                <Skeleton width={120} height={16} variant="text" />
                <Skeleton width={80} height={14} variant="text" />
              </div>
            </div>
            <Skeleton width={20} height={20} variant="rectangular" />
          </div>

          {/* Badges */}
          <div className="flex items-center justify-between">
            <Skeleton width={60} height={24} variant="rounded" />
            <Skeleton width={70} height={24} variant="rounded" />
          </div>

          {/* Stats */}
          <div className="space-y-3">
            <div className="flex justify-between">
              <Skeleton width={50} height={12} variant="text" />
              <Skeleton width={80} height={12} variant="text" />
            </div>
            <div className="flex justify-between">
              <Skeleton width={70} height={12} variant="text" />
              <Skeleton width={60} height={12} variant="text" />
            </div>
            <div className="flex justify-between">
              <Skeleton width={40} height={12} variant="text" />
              <Skeleton width={30} height={12} variant="text" />
            </div>
            <div className="flex justify-between">
              <Skeleton width={65} height={12} variant="text" />
              <Skeleton width={45} height={12} variant="text" />
            </div>
          </div>

          {/* Status Indicators */}
          <div className="flex items-center justify-center space-x-2">
            <Skeleton width={16} height={16} variant="circular" />
            <Skeleton width={16} height={16} variant="circular" />
          </div>

          {/* Action Buttons */}
          <div className="flex justify-center space-x-2">
            <Skeleton width={32} height={32} variant="rectangular" />
            <Skeleton width={32} height={32} variant="rectangular" />
            <Skeleton width={32} height={32} variant="rectangular" />
            <Skeleton width={32} height={32} variant="rectangular" />
          </div>
        </div>
      ))}

      <span className="sr-only">Loading user cards, please wait...</span>
    </div>
  );
};

// ============================================================================
// STATS CARDS SKELETON
// ============================================================================

export interface StatsCardSkeletonProps {
  count?: number;
  className?: string;
}

export const StatsCardSkeleton: React.FC<StatsCardSkeletonProps> = ({
  count = 4,
  className,
}) => {
  return (
    <div 
      className={cn('grid gap-4 md:grid-cols-2 lg:grid-cols-4', className)}
      role="status" 
      aria-label="Loading statistics"
    >
      {Array.from({ length: count }).map((_, index) => (
        <div key={index} className="border rounded-lg p-6">
          <div className="flex items-center justify-between space-y-0 pb-2">
            <Skeleton width={120} height={16} variant="text" />
            <Skeleton width={20} height={20} variant="rectangular" />
          </div>
          <div className="space-y-2">
            <Skeleton width={80} height={32} variant="text" />
            <Skeleton width={100} height={12} variant="text" />
          </div>
        </div>
      ))}

      <span className="sr-only">Loading statistics, please wait...</span>
    </div>
  );
};

// ============================================================================
// FILTER SECTION SKELETON
// ============================================================================

export interface FilterSkeletonProps {
  showFilters?: boolean;
  className?: string;
}

export const FilterSkeleton: React.FC<FilterSkeletonProps> = ({
  showFilters = true,
  className,
}) => {
  return (
    <div className={cn('border rounded-lg', className)} role="status" aria-label="Loading filters">
      {/* Header */}
      <div className="p-6">
        <div className="flex items-center justify-between">
          <Skeleton width={200} height={20} variant="text" />
          <div className="flex items-center space-x-2">
            <Skeleton width={80} height={36} variant="rounded" />
            <Skeleton width={100} height={36} variant="rounded" />
          </div>
        </div>
      </div>

      {/* Filters */}
      {showFilters && (
        <div className="px-6 pb-6">
          <div className="grid gap-4 md:grid-cols-3 lg:grid-cols-4">
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
            <Skeleton width="100%" height={40} variant="rounded" />
          </div>
        </div>
      )}

      <span className="sr-only">Loading filter options, please wait...</span>
    </div>
  );
};

// ============================================================================
// PAGINATION SKELETON
// ============================================================================

export interface PaginationSkeletonProps {
  className?: string;
}

export const PaginationSkeleton: React.FC<PaginationSkeletonProps> = ({
  className,
}) => {
  return (
    <div 
      className={cn('flex items-center justify-between mt-6 pt-6 border-t', className)}
      role="status" 
      aria-label="Loading pagination"
    >
      <Skeleton width={200} height={16} variant="text" />
      <div className="flex items-center space-x-2">
        <Skeleton width={80} height={36} variant="rounded" />
        <div className="flex items-center space-x-1">
          <Skeleton width={36} height={36} variant="rounded" />
          <Skeleton width={36} height={36} variant="rounded" />
          <Skeleton width={36} height={36} variant="rounded" />
          <Skeleton width={36} height={36} variant="rounded" />
          <Skeleton width={36} height={36} variant="rounded" />
        </div>
        <Skeleton width={64} height={36} variant="rounded" />
      </div>

      <span className="sr-only">Loading pagination controls, please wait...</span>
    </div>
  );
};

// ============================================================================
// FULL PAGE USER MANAGEMENT SKELETON
// ============================================================================

export interface UserManagementSkeletonProps {
  viewMode?: 'table' | 'card';
  showFilters?: boolean;
  className?: string;
}

export const UserManagementSkeleton: React.FC<UserManagementSkeletonProps> = ({
  viewMode = 'table',
  showFilters = true,
  className,
}) => {
  return (
    <div className={cn('space-y-6', className)} role="status" aria-label="Loading user management">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="space-y-2">
          <Skeleton width={200} height={32} variant="text" />
          <Skeleton width={300} height={16} variant="text" />
        </div>
        <div className="flex items-center space-x-2">
          <Skeleton width={80} height={36} variant="rounded" />
          <Skeleton width={100} height={36} variant="rounded" />
        </div>
      </div>

      {/* Stats Cards */}
      <StatsCardSkeleton count={4} />

      {/* Filters */}
      <FilterSkeleton showFilters={showFilters} />

      {/* User List */}
      <div className="border rounded-lg">
        <div className="p-6">
          <div className="flex items-center justify-between mb-6">
            <Skeleton width={150} height={24} variant="text" />
            <Skeleton width={100} height={16} variant="text" />
          </div>
          
          {viewMode === 'table' ? (
            <UserTableSkeleton rows={10} />
          ) : (
            <UserCardSkeleton count={12} />
          )}
        </div>

        {/* Pagination */}
        <PaginationSkeleton />
      </div>

      <span className="sr-only">Loading user management interface, please wait...</span>
    </div>
  );
};

// ============================================================================
// EXPORT DEFAULT
// ============================================================================

export default Skeleton;