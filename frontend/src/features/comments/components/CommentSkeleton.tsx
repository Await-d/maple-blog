// @ts-nocheck
/**
 * 评论骨架屏组件
 * 用于加载时显示占位内容
 */

import React from 'react';

interface CommentSkeletonProps {
  depth?: number;
  className?: string;
}

const CommentSkeleton: React.FC<CommentSkeletonProps> = ({
  depth = 0,
  className = ''
}) => {
  const indentStyle = {
    marginLeft: `${depth * 24}px`
  };

  return (
    <div
      className={`animate-pulse bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${className}`}
      style={indentStyle}
    >
      {/* 用户头像和信息 */}
      <div className="flex items-start space-x-3 mb-3">
        <div className="w-8 h-8 bg-gray-300 dark:bg-gray-600 rounded-full"></div>
        <div className="flex-1">
          <div className="flex items-center space-x-2 mb-1">
            <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-20"></div>
            <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-12"></div>
          </div>
          <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-16"></div>
        </div>
      </div>

      {/* 评论内容 */}
      <div className="space-y-2 mb-3">
        <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-full"></div>
        <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-4/5"></div>
        <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-3/5"></div>
      </div>

      {/* 操作按钮 */}
      <div className="flex items-center space-x-4">
        <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded w-12"></div>
        <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded w-12"></div>
        <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded w-12"></div>
      </div>
    </div>
  );
};

export default CommentSkeleton;