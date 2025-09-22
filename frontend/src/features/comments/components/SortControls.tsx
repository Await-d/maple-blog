// @ts-nocheck
/**
 * 评论排序控制组件
 * 提供排序和筛选功能
 */

import React, { useState } from 'react';
import { CommentSortOrder, CommentStatus } from '../../../types/comment';

interface SortControlsProps {
  currentSort: CommentSortOrder;
  onSortChange: (sortOrder: CommentSortOrder) => void;
  statusFilter: CommentStatus[];
  onStatusFilterChange: (statusFilter: CommentStatus[]) => void;
  className?: string;
  showStatusFilter?: boolean;
}

const SortControls: React.FC<SortControlsProps> = ({
  currentSort,
  onSortChange,
  statusFilter,
  onStatusFilterChange,
  className = '',
  showStatusFilter = false
}) => {
  const [showFilters, setShowFilters] = useState(false);

  const sortOptions = [
    { value: CommentSortOrder.CreatedAtDesc, label: '最新优先', icon: '⏰' },
    { value: CommentSortOrder.CreatedAtAsc, label: '最早优先', icon: '📅' },
    { value: CommentSortOrder.LikeCountDesc, label: '最多点赞', icon: '👍' },
    { value: CommentSortOrder.ReplyCountDesc, label: '最多回复', icon: '💬' },
    { value: CommentSortOrder.HotScore, label: '热度排序', icon: '🔥' }
  ];

  const statusOptions = [
    { value: CommentStatus.Approved, label: '已批准', color: 'green' },
    { value: CommentStatus.Pending, label: '待审核', color: 'yellow' },
    { value: CommentStatus.Hidden, label: '已隐藏', color: 'gray' },
    { value: CommentStatus.Rejected, label: '已拒绝', color: 'red' }
  ];

  const handleStatusToggle = (status: CommentStatus) => {
    const newFilter = statusFilter.includes(status)
      ? statusFilter.filter(s => s !== status)
      : [...statusFilter, status];

    onStatusFilterChange(newFilter);
  };

  const getSortIcon = (sortOrder: CommentSortOrder) => {
    const option = sortOptions.find(opt => opt.value === sortOrder);
    return option?.icon || '📝';
  };

  const getSortLabel = (sortOrder: CommentSortOrder) => {
    const option = sortOptions.find(opt => opt.value === sortOrder);
    return option?.label || '排序';
  };

  return (
    <div className={`sort-controls ${className}`}>
      <div className="flex items-center justify-between">
        {/* 排序选择器 */}
        <div className="flex items-center space-x-4">
          <div className="relative">
            <button
              onClick={() => setShowFilters(!showFilters)}
              className="flex items-center space-x-2 px-3 py-2 text-sm font-medium text-gray-700
                         dark:text-gray-300 bg-white dark:bg-gray-800 border border-gray-300
                         dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700
                         transition-colors duration-200"
            >
              <span>{getSortIcon(currentSort)}</span>
              <span>{getSortLabel(currentSort)}</span>
              <svg
                className={`w-4 h-4 transition-transform duration-200 ${
                  showFilters ? 'rotate-180' : ''
                }`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M19 9l-7 7-7-7"
                />
              </svg>
            </button>

            {/* 下拉菜单 */}
            {showFilters && (
              <div className="absolute top-full left-0 mt-2 w-48 bg-white dark:bg-gray-800
                              border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg
                              z-10 py-2">
                {sortOptions.map(option => (
                  <button
                    key={option.value}
                    onClick={() => {
                      onSortChange(option.value);
                      setShowFilters(false);
                    }}
                    className={`
                      w-full text-left px-4 py-2 text-sm flex items-center space-x-3
                      hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors
                      ${currentSort === option.value
                        ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400'
                        : 'text-gray-700 dark:text-gray-300'
                      }
                    `}
                  >
                    <span>{option.icon}</span>
                    <span>{option.label}</span>
                    {currentSort === option.value && (
                      <svg className="w-4 h-4 ml-auto" fill="currentColor" viewBox="0 0 20 20">
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                    )}
                  </button>
                ))}

                {/* 状态筛选 */}
                {showStatusFilter && (
                  <>
                    <div className="border-t border-gray-200 dark:border-gray-700 my-2"></div>
                    <div className="px-4 py-2">
                      <div className="text-xs font-medium text-gray-500 dark:text-gray-400 mb-2">
                        评论状态筛选
                      </div>
                      {statusOptions.map(status => (
                        <label
                          key={status.value}
                          className="flex items-center space-x-2 py-1 cursor-pointer"
                        >
                          <input
                            type="checkbox"
                            checked={statusFilter.includes(status.value)}
                            onChange={() => handleStatusToggle(status.value)}
                            className="rounded text-blue-600 focus:ring-blue-500"
                          />
                          <span className="text-sm text-gray-700 dark:text-gray-300">
                            {status.label}
                          </span>
                        </label>
                      ))}
                    </div>
                  </>
                )}
              </div>
            )}
          </div>

          {/* 快速排序按钮 */}
          <div className="hidden sm:flex items-center space-x-2">
            {sortOptions.slice(0, 3).map(option => (
              <button
                key={option.value}
                onClick={() => onSortChange(option.value)}
                className={`
                  px-3 py-1.5 text-xs font-medium rounded-full transition-colors duration-200
                  ${currentSort === option.value
                    ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-300 dark:hover:bg-gray-600'
                  }
                `}
                title={option.label}
              >
                <span className="mr-1">{option.icon}</span>
                {option.label}
              </button>
            ))}
          </div>
        </div>

        {/* 活跃状态筛选标签 */}
        {statusFilter.length > 0 && statusFilter.length < statusOptions.length && (
          <div className="flex items-center space-x-2">
            <span className="text-xs text-gray-500 dark:text-gray-400">筛选:</span>
            {statusFilter.map(status => {
              const statusInfo = statusOptions.find(s => s.value === status);
              return (
                <span
                  key={status}
                  className={`
                    inline-flex items-center px-2 py-1 text-xs font-medium rounded-full
                    ${statusInfo?.color === 'green' ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : ''}
                    ${statusInfo?.color === 'yellow' ? 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200' : ''}
                    ${statusInfo?.color === 'gray' ? 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200' : ''}
                    ${statusInfo?.color === 'red' ? 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200' : ''}
                  `}
                >
                  {statusInfo?.label}
                  <button
                    onClick={() => handleStatusToggle(status)}
                    className="ml-1 hover:bg-black hover:bg-opacity-20 rounded-full p-0.5"
                  >
                    <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                      <path
                        fillRule="evenodd"
                        d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                        clipRule="evenodd"
                      />
                    </svg>
                  </button>
                </span>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};

export default SortControls;