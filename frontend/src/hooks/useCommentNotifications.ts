/**
 * 评论通知系统 Hook
 * 管理实时通知、未读数量、通知权限等
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import { useCommentSocket } from '../services/commentSocket';
import { CommentNotification, CommentNotificationType } from '../types/comment';
import { useAuth } from './useAuth';

interface NotificationState {
  notifications: CommentNotification[];
  unreadCount: number;
  loading: boolean;
  permission: NotificationPermission;
  soundEnabled: boolean;
}

export const useCommentNotifications = () => {
  const { user: _user, isAuthenticated } = useAuth();
  const commentSocket = useCommentSocket();
  const [state, setState] = useState<NotificationState>({
    notifications: [],
    unreadCount: 0,
    loading: false,
    permission: 'default',
    soundEnabled: true
  });

  const soundRef = useRef<HTMLAudioElement | null>(null);
  const vibrationTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // 初始化通知音效
  useEffect(() => {
    soundRef.current = new Audio('/sounds/notification.mp3');
    soundRef.current.volume = 0.3;
  }, []);

  // 请求通知权限
  const requestNotificationPermission = useCallback(async () => {
    if (!('Notification' in window)) {
      console.warn('This browser does not support notifications');
      return false;
    }

    try {
      const permission = await Notification.requestPermission();
      setState(prev => ({ ...prev, permission }));
      return permission === 'granted';
    } catch (error) {
      console.error('Error requesting notification permission:', error);
      return false;
    }
  }, []);

  // 显示系统通知
  const showSystemNotification = useCallback((notification: CommentNotification) => {
    if (state.permission !== 'granted') return;

    // 检查页面是否在前台
    if (document.visibilityState === 'visible') return;

    const title = getNotificationTitle(notification.type);
    const options: NotificationOptions = {
      body: notification.content,
      icon: notification.triggeredByUser?.avatarUrl || '/icons/comment-icon.png',
      badge: '/icons/badge.png',
      tag: `comment-${notification.id}`,
      data: {
        url: notification.url,
        notificationId: notification.id
      },
      requireInteraction: false,
      silent: false
    };

    const systemNotification = new Notification(title, options);

    systemNotification.onclick = () => {
      window.focus();
      window.location.href = notification.url;
      systemNotification.close();
    };

    // 自动关闭
    setTimeout(() => {
      systemNotification.close();
    }, 5000);
  }, [state.permission]);

  // 播放通知音效
  const playNotificationSound = useCallback(() => {
    if (!state.soundEnabled || !soundRef.current) return;

    try {
      soundRef.current.currentTime = 0;
      soundRef.current.play().catch(error => {
        console.warn('Could not play notification sound:', error);
      });
    } catch (error) {
      console.warn('Notification sound error:', error);
    }
  }, [state.soundEnabled]);

  // 触发震动
  const triggerVibration = useCallback(() => {
    if ('vibrate' in navigator && navigator.vibrate) {
      navigator.vibrate([100, 50, 100]);
    }
  }, []);

  // 处理新通知
  const handleNewNotification = useCallback((notification: CommentNotification) => {
    setState(prev => ({
      ...prev,
      notifications: [notification, ...prev.notifications.slice(0, 49)], // 最多保留50条
      unreadCount: prev.unreadCount + 1
    }));

    // 显示系统通知
    showSystemNotification(notification);

    // 音效提示
    playNotificationSound();

    // 震动提示（移动设备）
    triggerVibration();

    // 页面标题闪烁提示
    if (document.visibilityState !== 'visible') {
      const originalTitle = document.title;
      let flashCount = 0;

      const flashInterval = setInterval(() => {
        document.title = flashCount % 2 === 0 ? '🔔 新消息' : originalTitle;
        flashCount++;

        if (flashCount >= 6) {
          clearInterval(flashInterval);
          document.title = originalTitle;
        }
      }, 1000);

      // 页面获得焦点时停止闪烁
      const handleVisibilityChange = () => {
        if (document.visibilityState === 'visible') {
          clearInterval(flashInterval);
          document.title = originalTitle;
          document.removeEventListener('visibilitychange', handleVisibilityChange);
        }
      };

      document.addEventListener('visibilitychange', handleVisibilityChange);
    }
  }, [showSystemNotification, playNotificationSound, triggerVibration]);

  // 标记通知为已读
  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      await commentSocket.markNotificationAsRead(notificationId);

      setState(prev => ({
        ...prev,
        notifications: prev.notifications.map(n =>
          n.id === notificationId ? { ...n, isRead: true } : n
        ),
        unreadCount: Math.max(0, prev.unreadCount - 1)
      }));
    } catch (error) {
      console.error('Error marking notification as read:', error);
    }
  }, [commentSocket]);

  // 标记所有通知为已读
  const markAllAsRead = useCallback(async () => {
    const unreadNotifications = state.notifications.filter(n => !n.isRead);

    try {
      await Promise.all(
        unreadNotifications.map(n => commentSocket.markNotificationAsRead(n.id))
      );

      setState(prev => ({
        ...prev,
        notifications: prev.notifications.map(n => ({ ...n, isRead: true })),
        unreadCount: 0
      }));
    } catch (error) {
      console.error('Error marking all notifications as read:', error);
    }
  }, [state.notifications, commentSocket]);

  // 删除通知
  const removeNotification = useCallback((notificationId: string) => {
    setState(prev => {
      const notification = prev.notifications.find(n => n.id === notificationId);
      const wasUnread = notification && !notification.isRead;

      return {
        ...prev,
        notifications: prev.notifications.filter(n => n.id !== notificationId),
        unreadCount: wasUnread ? Math.max(0, prev.unreadCount - 1) : prev.unreadCount
      };
    });
  }, []);

  // 清空所有通知
  const clearAllNotifications = useCallback(() => {
    setState(prev => ({
      ...prev,
      notifications: [],
      unreadCount: 0
    }));
  }, []);

  // 切换音效
  const toggleSound = useCallback(() => {
    setState(prev => ({ ...prev, soundEnabled: !prev.soundEnabled }));
  }, []);

  // 获取最近通知
  const loadRecentNotifications = useCallback(async (limit: number = 20) => {
    if (!isAuthenticated || !commentSocket.isConnected) return;

    setState(prev => ({ ...prev, loading: true }));

    try {
      await commentSocket.getRecentNotifications(limit);
      await commentSocket.getUnreadNotificationCount();
    } catch (error) {
      console.error('Error loading recent notifications:', error);
    } finally {
      setState(prev => ({ ...prev, loading: false }));
    }
  }, [isAuthenticated, commentSocket]);

  // 初始化和清理
  useEffect(() => {
    if (!isAuthenticated) {
      setState(prev => ({
        ...prev,
        notifications: [],
        unreadCount: 0
      }));
      return;
    }

    // 获取通知权限状态
    if ('Notification' in window) {
      setState(prev => ({ ...prev, permission: Notification.permission }));
    }

    // 连接WebSocket并绑定事件
    if (commentSocket.isConnected) {
      // 绑定通知事件
      commentSocket.on('NewNotification', handleNewNotification);
      commentSocket.on('UnreadNotificationCount', (count: number) => {
        setState(prev => ({ ...prev, unreadCount: count }));
      });
      commentSocket.on('RecentNotifications', (notifications: CommentNotification[]) => {
        setState(prev => ({ ...prev, notifications }));
      });
      commentSocket.on('NotificationMarkedAsRead', (notificationId: string) => {
        setState(prev => ({
          ...prev,
          notifications: prev.notifications.map(n =>
            n.id === notificationId ? { ...n, isRead: true } : n
          )
        }));
      });

      // 加载初始数据
      loadRecentNotifications();
    }

    return () => {
      if (vibrationTimeoutRef.current) {
        clearTimeout(vibrationTimeoutRef.current);
        vibrationTimeoutRef.current = null;
      }
    };
  }, [isAuthenticated, commentSocket, handleNewNotification, loadRecentNotifications]);

  // 页面可见性变化处理
  useEffect(() => {
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible' && state.unreadCount > 0) {
        // 页面重新获得焦点时，可以考虑自动标记通知为已读
        // 这里暂时不自动标记，让用户手动操作
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => document.removeEventListener('visibilitychange', handleVisibilityChange);
  }, [state.unreadCount]);

  return {
    // 状态
    notifications: state.notifications,
    unreadCount: state.unreadCount,
    loading: state.loading,
    permission: state.permission,
    soundEnabled: state.soundEnabled,

    // 操作方法
    requestNotificationPermission,
    markAsRead,
    markAllAsRead,
    removeNotification,
    clearAllNotifications,
    toggleSound,
    loadRecentNotifications,

    // 工具方法
    hasUnread: state.unreadCount > 0,
    isSupported: 'Notification' in window,
    canShowNotifications: state.permission === 'granted'
  };
};

// 获取通知类型对应的标题
const getNotificationTitle = (type: CommentNotificationType): string => {
  switch (type) {
    case CommentNotificationType.CommentReply:
      return '📝 新的回复';
    case CommentNotificationType.CommentMention:
      return '👋 有人提到了你';
    case CommentNotificationType.CommentLiked:
      return '❤️ 收到点赞';
    case CommentNotificationType.CommentApproved:
      return '✅ 评论已通过';
    case CommentNotificationType.CommentRejected:
      return '❌ 评论被拒绝';
    default:
      return '🔔 新通知';
  }
};

export default useCommentNotifications;