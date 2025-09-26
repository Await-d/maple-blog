/**
 * 评论状态徽章组件
 * 显示评论的审核状态
 */

import React from 'react';
import { CommentStatus } from '../../../types/comment';

interface StatusBadgeProps {
  status: CommentStatus;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
}

const StatusBadge: React.FC<StatusBadgeProps> = ({
  status,
  className = '',
  size = 'sm'
}) => {
  const getStatusConfig = (status: CommentStatus) => {
    switch (status) {
      case CommentStatus.Approved:
        return {
          label: '已批准',
          icon: '✅',
          className: 'bg-green-100 text-green-800 dark:bg-green-900/20 dark:text-green-400'
        };
      case CommentStatus.Pending:
        return {
          label: '待审核',
          icon: '⏳',
          className: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-400'
        };
      case CommentStatus.Rejected:
        return {
          label: '已拒绝',
          icon: '❌',
          className: 'bg-red-100 text-red-800 dark:bg-red-900/20 dark:text-red-400'
        };
      case CommentStatus.Hidden:
        return {
          label: '已隐藏',
          icon: '👁️‍🗨️',
          className: 'bg-gray-100 text-gray-800 dark:bg-gray-900/20 dark:text-gray-400'
        };
      case CommentStatus.Spam:
        return {
          label: '垃圾信息',
          icon: '🚫',
          className: 'bg-red-100 text-red-800 dark:bg-red-900/20 dark:text-red-400'
        };
      default:
        return {
          label: '未知',
          icon: '❓',
          className: 'bg-gray-100 text-gray-800 dark:bg-gray-900/20 dark:text-gray-400'
        };
    }
  };

  const getSizeClasses = (size: string) => {
    switch (size) {
      case 'sm':
        return 'text-xs px-2 py-0.5';
      case 'md':
        return 'text-sm px-2.5 py-1';
      case 'lg':
        return 'text-base px-3 py-1.5';
      default:
        return 'text-xs px-2 py-0.5';
    }
  };

  const config = getStatusConfig(status);
  const sizeClasses = getSizeClasses(size);

  // 对于已批准状态，通常不需要显示徽章
  if (status === CommentStatus.Approved) {
    return null;
  }

  return (
    <span
      className={`
        inline-flex items-center font-medium rounded-full
        ${config.className}
        ${sizeClasses}
        ${className}
      `}
      title={`评论状态: ${config.label}`}
    >
      <span className="mr-1" role="img" aria-label={config.label}>
        {config.icon}
      </span>
      {config.label}
    </span>
  );
};

export default StatusBadge;