/**
 * 通知徽章组件
 * 显示未读通知数量和通知列表
 */

import React, { useState, useRef, useEffect } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';
import { useCommentNotifications } from '../../../hooks/useCommentNotifications';
import { CommentNotification, CommentNotificationType } from '../../../types/comment';
import UserAvatar from '../../../components/common/UserAvatar';

interface NotificationBadgeProps {
  className?: string;
  position?: 'left' | 'right';
  maxItems?: number;
  compact?: boolean;
}

const NotificationBadge: React.FC<NotificationBadgeProps> = ({
  className = '',
  position = 'right',
  maxItems = 10,
  compact = false
}) => {
  const {
    notifications,
    unreadCount,
    loading,
    permission: _permission,
    soundEnabled,
    hasUnread,
    markAsRead,
    markAllAsRead,
    removeNotification,
    clearAllNotifications,
    toggleSound,
    requestNotificationPermission,
    canShowNotifications
  } = useCommentNotifications();

  const [isOpen, setIsOpen] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // 点击外部关闭
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
        setShowSettings(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [isOpen]);

  // 处理通知点击
  const handleNotificationClick = async (notification: CommentNotification) => {
    // 标记为已读
    if (!notification.isRead) {
      await markAsRead(notification.id);
    }

    // 跳转到对应页面
    if (notification.url) {
      window.location.href = notification.url;
    }

    setIsOpen(false);
  };

  // 获取通知图标
  const getNotificationIcon = (type: CommentNotificationType) => {
    switch (type) {
      case CommentNotificationType.CommentReply:
        return '💬';
      case CommentNotificationType.CommentMention:
        return '👋';
      case CommentNotificationType.CommentLiked:
        return '❤️';
      case CommentNotificationType.CommentApproved:
        return '✅';
      case CommentNotificationType.CommentRejected:
        return '❌';
      default:
        return '🔔';
    }
  };

  // 获取通知颜色
  const getNotificationColor = (type: CommentNotificationType) => {
    switch (type) {
      case CommentNotificationType.CommentReply:
        return 'text-blue-600 dark:text-blue-400';
      case CommentNotificationType.CommentMention:
        return 'text-green-600 dark:text-green-400';
      case CommentNotificationType.CommentLiked:
        return 'text-red-600 dark:text-red-400';
      case CommentNotificationType.CommentApproved:
        return 'text-green-600 dark:text-green-400';
      case CommentNotificationType.CommentRejected:
        return 'text-red-600 dark:text-red-400';
      default:
        return 'text-gray-600 dark:text-gray-400';
    }
  };

  const displayNotifications = notifications.slice(0, maxItems);

  return (
    <div className={`notification-badge relative ${className}`} ref={dropdownRef}>
      {/* 通知按钮 */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className={`
          relative p-2 rounded-lg transition-colors duration-200
          ${hasUnread
            ? 'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20'
            : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
          }
          ${compact ? 'p-1.5' : 'p-2'}
        `}
        title={`通知 ${unreadCount > 0 ? `(${unreadCount} 未读)` : ''}`}
      >
        <svg
          className={`${compact ? 'w-5 h-5' : 'w-6 h-6'} ${hasUnread ? 'animate-pulse' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M15 17h5l-5 5v-5zM11 3.055A9.001 9.001 0 1020.945 13H11V3.055z"
          />
        </svg>

        {/* 未读数量徽章 */}
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 inline-flex items-center justify-center px-2 py-1 text-xs font-bold leading-none text-white bg-red-500 rounded-full min-w-[1.25rem]">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* 通知下拉面板 */}
      {isOpen && (
        <div
          className={`
            absolute z-50 mt-2 w-96 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg
            ${position === 'left' ? 'left-0' : 'right-0'}
          `}
        >
          {/* 头部 */}
          <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
              通知
              {unreadCount > 0 && (
                <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900/20 dark:text-red-400">
                  {unreadCount} 未读
                </span>
              )}
            </h3>

            <div className="flex items-center space-x-2">
              {/* 设置按钮 */}
              <button
                onClick={() => setShowSettings(!showSettings)}
                className="p-1 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 rounded"
                title="通知设置"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              </button>

              {/* 全部已读按钮 */}
              {unreadCount > 0 && (
                <button
                  onClick={markAllAsRead}
                  className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-200"
                  title="全部已读"
                >
                  全部已读
                </button>
              )}
            </div>
          </div>

          {/* 设置面板 */}
          {showSettings && (
            <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-750">
              <div className="space-y-3">
                {/* 系统通知权限 */}
                {!canShowNotifications && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-700 dark:text-gray-300">
                      系统通知
                    </span>
                    <button
                      onClick={requestNotificationPermission}
                      className="px-3 py-1 text-xs bg-blue-600 hover:bg-blue-700 text-white rounded transition-colors"
                    >
                      开启
                    </button>
                  </div>
                )}

                {/* 音效开关 */}
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-700 dark:text-gray-300">
                    提示音效
                  </span>
                  <button
                    onClick={toggleSound}
                    className={`
                      relative inline-flex h-5 w-9 items-center rounded-full transition-colors
                      ${soundEnabled ? 'bg-blue-600' : 'bg-gray-300 dark:bg-gray-600'}
                    `}
                  >
                    <span
                      className={`
                        inline-block h-3 w-3 transform rounded-full bg-white transition-transform
                        ${soundEnabled ? 'translate-x-5' : 'translate-x-1'}
                      `}
                    />
                  </button>
                </div>

                {/* 清空通知 */}
                {notifications.length > 0 && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-700 dark:text-gray-300">
                      清空通知
                    </span>
                    <button
                      onClick={clearAllNotifications}
                      className="px-3 py-1 text-xs bg-red-600 hover:bg-red-700 text-white rounded transition-colors"
                    >
                      清空
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* 通知列表 */}
          <div className="max-h-96 overflow-y-auto">
            {loading && (
              <div className="flex items-center justify-center py-8">
                <svg className="w-6 h-6 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              </div>
            )}

            {!loading && displayNotifications.length === 0 && (
              <div className="flex flex-col items-center justify-center py-8 text-gray-500 dark:text-gray-400">
                <svg className="w-12 h-12 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M15 17h5l-5 5v-5zM11 3.055A9.001 9.001 0 1020.945 13H11V3.055z" />
                </svg>
                <p className="text-sm">暂无通知</p>
              </div>
            )}

            {!loading && displayNotifications.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onClick={() => handleNotificationClick(notification)}
                onRemove={() => removeNotification(notification.id)}
                getIcon={getNotificationIcon}
                getColor={getNotificationColor}
              />
            ))}
          </div>

          {/* 底部 */}
          {displayNotifications.length > 0 && (
            <div className="px-4 py-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-750">
              <button
                onClick={() => {
                  // 跳转到完整的通知页面
                  window.location.href = '/notifications';
                  setIsOpen(false);
                }}
                className="w-full text-center text-sm text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-200 transition-colors"
              >
                查看所有通知
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

// 通知项组件
interface NotificationItemProps {
  notification: CommentNotification;
  onClick: () => void;
  onRemove: () => void;
  getIcon: (type: CommentNotificationType) => string;
  getColor: (type: CommentNotificationType) => string;
}

const NotificationItem: React.FC<NotificationItemProps> = ({
  notification,
  onClick,
  onRemove,
  getIcon,
  getColor
}) => {
  const [showActions, setShowActions] = useState(false);

  const timeAgo = formatDistanceToNow(new Date(notification.createdAt), {
    addSuffix: true,
    locale: zhCN
  });

  return (
    <div
      className={`
        relative px-4 py-3 border-b border-gray-100 dark:border-gray-700 cursor-pointer
        hover:bg-gray-50 dark:hover:bg-gray-750 transition-colors
        ${!notification.isRead ? 'bg-blue-50 dark:bg-blue-900/10' : ''}
      `}
      onClick={onClick}
      onMouseEnter={() => setShowActions(true)}
      onMouseLeave={() => setShowActions(false)}
    >
      <div className="flex items-start space-x-3">
        {/* 通知图标 */}
        <div className={`flex-shrink-0 text-lg ${getColor(notification.type)}`}>
          {getIcon(notification.type)}
        </div>

        {/* 通知内容 */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center space-x-2 mb-1">
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
              {notification.title}
            </p>
            {!notification.isRead && (
              <div className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0"></div>
            )}
          </div>

          <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
            {notification.content}
          </p>

          <div className="flex items-center space-x-2 mt-1">
            {notification.triggeredByUser && (
              <UserAvatar
                user={notification.triggeredByUser}
                size="xs"
                showStatus={false}
              />
            )}
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {timeAgo}
            </span>
          </div>
        </div>

        {/* 操作按钮 */}
        {showActions && (
          <button
            onClick={(e) => {
              e.stopPropagation();
              onRemove();
            }}
            className="flex-shrink-0 p-1 text-gray-400 hover:text-red-500 transition-colors"
            title="删除通知"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
    </div>
  );
};

export default NotificationBadge;