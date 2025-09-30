/**
 * 评论实时通信服务 (SignalR WebSocket)
 * 处理评论相关的实时事件和通知
 */

import * as signalR from '@microsoft/signalr';
import type {
  CommentSocketEvents,
  TypingUser as _TypingUser,
  CommentNotification as _CommentNotification,
  CommentConnectionStatus,
} from '../types/comment';
import { getAuthToken } from '../utils/auth';
import { logger } from './loggingService';
import { errorReporter } from './errorReporting';

export class CommentSocketService {
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 1000; // 初始延迟1秒
  private eventListeners = new Map<string, Set<(...args: unknown[]) => void>>();
  private isConnecting = false;
  private currentPostId: string | null = null;
  private typingTimeouts = new Map<string, NodeJS.Timeout>();
  private status: CommentConnectionStatus = { status: 'disconnected' };

  private notifyStatus(status: CommentConnectionStatus, log?: { level: 'debug' | 'info' | 'warn' | 'error'; message: string; context?: Record<string, unknown>; error?: Error }): void {
    this.status = status;

    if (log) {
      const { level, message, context, error } = log;
      const logContext = {
        component: 'CommentSocket',
        action: status.status,
        ...context,
      };

      switch (level) {
        case 'debug':
          logger.debug(message, logContext);
          break;
        case 'info':
          logger.info(message, logContext);
          break;
        case 'warn':
          logger.warn(message, logContext, error);
          break;
        case 'error':
          logger.error(message, logContext, error);
          break;
      }
    }

    this.emit('ConnectionStatusChanged', status);
  }

  /**
   * 初始化WebSocket连接
   */
  async connect(): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected || this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    this.notifyStatus({ status: 'connecting' }, {
      level: 'info',
      message: '正在连接评论实时通信通道',
      context: { attempts: this.reconnectAttempts },
    });

    try {
      // 创建连接
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/comment', {
          accessTokenFactory: () => getAuthToken() || '',
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // 指数退避策略
            const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            const attempt = retryContext.previousRetryCount + 1;
            this.notifyStatus({ status: 'reconnecting', attempt }, {
              level: 'warn',
              message: '评论实时通道正在重连',
              context: { attempt, delay },
            });
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // 绑定事件处理器
      this.bindEventHandlers();

      // 启动连接
      await this.connection.start();

      this.reconnectAttempts = 0;
      this.notifyStatus({ status: 'connected' }, {
        level: 'info',
        message: '评论实时通道连接成功',
      });

      // 如果有当前文章ID，自动加入文章组
      if (this.currentPostId) {
        await this.joinPostGroup(this.currentPostId);
      }

    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      this.notifyStatus({ status: 'disconnected', reason: err.message }, {
        level: 'error',
        message: '评论实时通道连接失败',
        error: err,
      });

      errorReporter.captureError(err, {
        component: 'CommentSocket',
        action: 'connect',
        handled: true,
      });

      this.scheduleReconnect(err);
    } finally {
      this.isConnecting = false;
    }
  }

  /**
   * 断开连接
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        this.notifyStatus({ status: 'disconnected', reason: 'manual' }, {
          level: 'info',
          message: '已断开评论实时通道',
        });
      } catch (error) {
        const err = error instanceof Error ? error : new Error(String(error));
        this.notifyStatus({ status: 'disconnected', reason: err.message }, {
          level: 'warn',
          message: '断开评论实时通道时发生异常',
          error: err,
        });
        errorReporter.captureError(err, {
          component: 'CommentSocket',
          action: 'disconnect',
          handled: true,
        });
      }
      this.connection = null;
    }
    this.eventListeners.clear();
    this.typingTimeouts.forEach(timeout => clearTimeout(timeout));
    this.typingTimeouts.clear();
    this.reconnectAttempts = 0;
  }

  /**
   * 绑定所有事件处理器
   */
  private bindEventHandlers(): void {
    if (!this.connection) return;

    // 评论CRUD事件
    this.connection.on('CommentCreated', (comment) => {
      this.emit('CommentCreated', comment);
    });

    this.connection.on('CommentUpdated', (comment) => {
      this.emit('CommentUpdated', comment);
    });

    this.connection.on('CommentDeleted', (data) => {
      this.emit('CommentDeleted', data);
    });

    // 评论互动事件
    this.connection.on('CommentLiked', (data) => {
      this.emit('CommentLiked', data);
    });

    this.connection.on('CommentUnliked', (data) => {
      this.emit('CommentUnliked', data);
    });

    // 审核事件
    this.connection.on('CommentApproved', (data) => {
      this.emit('CommentApproved', data);
    });

    this.connection.on('CommentRejected', (data) => {
      this.emit('CommentRejected', data);
    });

    this.connection.on('CommentHidden', (data) => {
      this.emit('CommentHidden', data);
    });

    this.connection.on('CommentRestored', (data) => {
      this.emit('CommentRestored', data);
    });

    this.connection.on('CommentMarkedAsSpam', (data) => {
      this.emit('CommentMarkedAsSpam', data);
    });

    // 输入状态事件
    this.connection.on('UserStartedTyping', (data) => {
      this.emit('UserStartedTyping', data);
      this.handleTypingStart(data);
    });

    this.connection.on('UserStoppedTyping', (data) => {
      this.emit('UserStoppedTyping', data);
      this.handleTypingStop(data);
    });

    // 统计和状态事件
    this.connection.on('CommentStats', (stats) => {
      this.emit('CommentStats', stats);
    });

    this.connection.on('OnlineUserCount', (data) => {
      this.emit('OnlineUserCount', data);
    });

    // 通知事件
    this.connection.on('NewNotification', (notification) => {
      this.emit('NewNotification', notification);
    });

    this.connection.on('UnreadNotificationCount', (count) => {
      this.emit('UnreadNotificationCount', count);
    });

    this.connection.on('RecentNotifications', (notifications) => {
      this.emit('RecentNotifications', notifications);
    });

    this.connection.on('NotificationMarkedAsRead', (notificationId) => {
      this.emit('NotificationMarkedAsRead', notificationId);
    });

    // 管理员事件
    this.connection.on('ModerationStats', (stats) => {
      this.emit('ModerationStats', stats);
    });

    this.connection.on('CommentsModerated', (data) => {
      this.emit('CommentsModerated', data);
    });

    // 系统事件
    this.connection.on('Error', (message) => {
      const error = new Error(message);
      logger.error('评论实时通道收到错误事件', {
        component: 'CommentSocket',
        action: 'serverError',
      }, error);
      errorReporter.captureError(error, {
        component: 'CommentSocket',
        action: 'serverError',
        handled: true,
      });

      this.emit('Error', message);
    });

    this.connection.on('JoinedPostGroup', (postId) => {
      logger.info('已加入评论组', {
        component: 'CommentSocket',
        action: 'joinPostGroup',
        postId,
      });
      this.emit('JoinedPostGroup', postId);
    });

    this.connection.on('LeftPostGroup', (postId) => {
      logger.info('已离开评论组', {
        component: 'CommentSocket',
        action: 'leavePostGroup',
        postId,
      });
      this.emit('LeftPostGroup', postId);
    });

    this.connection.on('JoinedModerationGroup', () => {
      logger.info('已加入评论审核频道', {
        component: 'CommentSocket',
        action: 'joinModerationGroup',
      });
      this.emit('JoinedModerationGroup');
    });

    this.connection.on('LeftModerationGroup', () => {
      logger.info('已离开评论审核频道', {
        component: 'CommentSocket',
        action: 'leaveModerationGroup',
      });
      this.emit('LeftModerationGroup');
    });

    // 连接状态事件
    this.connection.onreconnecting((error) => {
      const attempt = this.reconnectAttempts + 1;
      const err = error instanceof Error ? error : error ? new Error(String(error)) : undefined;
      this.notifyStatus({ status: 'reconnecting', attempt }, {
        level: 'warn',
        message: '评论实时通道连接中断，正在尝试重连',
        context: { attempt },
        error: err,
      });
    });

    this.connection.onreconnected(() => {
      this.reconnectAttempts = 0;
      this.notifyStatus({ status: 'connected' }, {
        level: 'info',
        message: '评论实时通道重连成功',
      });
      // 重新加入文章组
      if (this.currentPostId) {
        this.joinPostGroup(this.currentPostId);
      }
    });

    this.connection.onclose((error) => {
      const err = error instanceof Error ? error : error ? new Error(String(error)) : undefined;
      this.notifyStatus({ status: 'disconnected', reason: err?.message }, {
        level: 'warn',
        message: '评论实时通道连接已关闭',
        error: err,
      });

      this.scheduleReconnect(err);
    });
  }

  /**
   * 加入文章评论组
   */
  async joinPostGroup(postId: string): Promise<boolean> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      this.currentPostId = postId;
      await this.connect();
      return false;
    }

    try {
      const success = await this.connection.invoke('JoinPostGroup', postId);
      if (success) {
        this.currentPostId = postId;
      }
      return success;
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.error('加入评论组失败', {
        component: 'CommentSocket',
        action: 'joinPostGroup',
        postId,
      }, err);
      errorReporter.captureError(err, {
        component: 'CommentSocket',
        action: 'joinPostGroup',
        handled: true,
        extra: { postId },
      });
      return false;
    }
  }

  /**
   * 离开文章评论组
   */
  async leavePostGroup(postId: string): Promise<boolean> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return false;
    }

    try {
      const success = await this.connection.invoke('LeavePostGroup', postId);
      if (success && this.currentPostId === postId) {
        this.currentPostId = null;
      }
      return success;
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('离开评论组失败', {
        component: 'CommentSocket',
        action: 'leavePostGroup',
        postId,
      }, err);
      errorReporter.captureError(err, {
        component: 'CommentSocket',
        action: 'leavePostGroup',
        handled: true,
        extra: { postId },
      });
      return false;
    }
  }

  /**
   * 通知开始输入
   */
  async startTyping(postId: string, parentId?: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('StartTyping', postId, parentId);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('发送正在输入状态失败', {
        component: 'CommentSocket',
        action: 'startTyping',
        postId,
        parentId,
      }, err);
    }
  }

  /**
   * 通知停止输入
   */
  async stopTyping(postId: string, parentId?: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('StopTyping', postId, parentId);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('发送停止输入状态失败', {
        component: 'CommentSocket',
        action: 'stopTyping',
        postId,
        parentId,
      }, err);
    }
  }

  /**
   * 获取评论统计
   */
  async getCommentStats(postId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('GetCommentStats', postId);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('获取评论统计失败', {
        component: 'CommentSocket',
        action: 'getCommentStats',
        postId,
      }, err);
    }
  }

  /**
   * 获取在线用户数
   */
  async getOnlineUserCount(postId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('GetOnlineUserCount', postId);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('获取在线评论人数失败', {
        component: 'CommentSocket',
        action: 'getOnlineUserCount',
        postId,
      }, err);
    }
  }

  /**
   * 标记通知为已读
   */
  async markNotificationAsRead(notificationId: string): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('MarkNotificationAsRead', notificationId);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('标记评论通知为已读失败', {
        component: 'CommentSocket',
        action: 'markNotificationAsRead',
        notificationId,
      }, err);
    }
  }

  /**
   * 获取未读通知数量
   */
  async getUnreadNotificationCount(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('GetUnreadNotificationCount');
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('获取未读评论通知数量失败', {
        component: 'CommentSocket',
        action: 'getUnreadNotificationCount',
      }, err);
    }
  }

  /**
   * 获取最新通知
   */
  async getRecentNotifications(limit: number = 10): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('GetRecentNotifications', limit);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('获取最新评论通知失败', {
        component: 'CommentSocket',
        action: 'getRecentNotifications',
        limit,
      }, err);
    }
  }

  /**
   * 加入审核组（管理员）
   */
  async joinModerationGroup(): Promise<boolean> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return false;
    }

    try {
      const result = await this.connection.invoke('JoinModerationGroup');
      return result;
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.error('加入评论审核频道失败', {
        component: 'CommentSocket',
        action: 'joinModerationGroup',
      }, err);
      errorReporter.captureError(err, {
        component: 'CommentSocket',
        action: 'joinModerationGroup',
        handled: true,
      });
      return false;
    }
  }

  /**
   * 离开审核组（管理员）
   */
  async leaveModerationGroup(): Promise<boolean> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return false;
    }

    try {
      const result = await this.connection.invoke('LeaveModerationGroup');
      return result;
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('离开评论审核频道失败', {
        component: 'CommentSocket',
        action: 'leaveModerationGroup',
      }, err);
      errorReporter.captureError(err, {
        component: 'CommentSocket',
        action: 'leaveModerationGroup',
        handled: true,
      });
      return false;
    }
  }

  /**
   * 获取审核统计（管理员）
   */
  async getModerationStats(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke('GetModerationStats');
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.warn('获取评论审核统计失败', {
        component: 'CommentSocket',
        action: 'getModerationStats',
      }, err);
    }
  }

  /**
   * 添加事件监听器
   */
  on<K extends keyof CommentSocketEvents>(
    event: K,
    listener: CommentSocketEvents[K]
  ): () => void {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, new Set());
    }
    this.eventListeners.get(event)!.add(listener as (...args: unknown[]) => void);
    return () => this.off(event, listener);
  }

  /**
   * 移除事件监听器
   */
  off<K extends keyof CommentSocketEvents>(
    event: K,
    listener: CommentSocketEvents[K]
  ): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      listeners.delete(listener as (...args: unknown[]) => void);
      if (listeners.size === 0) {
        this.eventListeners.delete(event);
      }
    }
  }

  /**
   * 触发事件
   */
  private emit<K extends keyof CommentSocketEvents>(
    event: K,
    ...args: Parameters<CommentSocketEvents[K]>
  ): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      listeners.forEach(listener => {
        try {
          (listener as (...args: unknown[]) => void)(...args);
        } catch (error) {
          const err = error instanceof Error ? error : new Error(String(error));
          logger.error('评论实时事件监听器执行失败', {
            component: 'CommentSocket',
            action: 'emit',
            event,
          }, err);
          errorReporter.captureError(err, {
            component: 'CommentSocket',
            action: 'emit',
            handled: true,
            extra: { event },
          });
        }
      });
    }
  }

  /**
   * 处理用户开始输入
   */
  private handleTypingStart(data: { userId: string; postId: string; parentId?: string }): void {
    const key = `${data.userId}_${data.postId}_${data.parentId || 'root'}`;

    // 清除之前的超时
    if (this.typingTimeouts.has(key)) {
      clearTimeout(this.typingTimeouts.get(key)!);
    }

    // 设置新的超时，10秒后自动停止输入状态
    const timeout = setTimeout(() => {
      this.emit('UserStoppedTyping', data);
      this.typingTimeouts.delete(key);
    }, 10000);

    this.typingTimeouts.set(key, timeout);
  }

  /**
   * 处理用户停止输入
   */
  private handleTypingStop(data: { userId: string; postId: string; parentId?: string }): void {
    const key = `${data.userId}_${data.postId}_${data.parentId || 'root'}`;

    if (this.typingTimeouts.has(key)) {
      clearTimeout(this.typingTimeouts.get(key)!);
      this.typingTimeouts.delete(key);
    }
  }

  /**
   * 调度重连
   */
  private scheduleReconnect(error?: Error): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      const reason = error?.message || '已达到最大重连次数';
      this.notifyStatus({ status: 'failed', attempts: this.reconnectAttempts, reason }, {
        level: 'error',
        message: '评论实时通道多次重连失败，停止尝试',
        error,
      });

      if (error) {
        errorReporter.captureError(error, {
          component: 'CommentSocket',
          action: 'scheduleReconnect',
          handled: true,
          extra: { attempts: this.reconnectAttempts },
        });
      }
      return;
    }

    const attempt = this.reconnectAttempts + 1;
    const delay = Math.min(this.reconnectDelay * Math.pow(2, this.reconnectAttempts), 30000);

    this.notifyStatus({ status: 'reconnecting', attempt }, {
      level: 'warn',
      message: '计划重连评论实时通道',
      context: { attempt, delay },
      error,
    });

    setTimeout(() => {
      this.reconnectAttempts = attempt;
      this.connect();
    }, delay);
  }

  /**
   * 获取连接状态
   */
  get connectionState(): signalR.HubConnectionState {
    return this.connection?.state || signalR.HubConnectionState.Disconnected;
  }

  get connectionStatus(): CommentConnectionStatus {
    return this.status;
  }

  /**
   * 检查是否已连接
   */
  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// 导出单例实例
export const commentSocket = new CommentSocketService();

// 导出类型化的事件监听器辅助函数
export const useCommentSocket = () => {
  return {
    connect: () => commentSocket.connect(),
    disconnect: () => commentSocket.disconnect(),
    joinPostGroup: (postId: string) => commentSocket.joinPostGroup(postId),
    leavePostGroup: (postId: string) => commentSocket.leavePostGroup(postId),
    startTyping: (postId: string, parentId?: string) => commentSocket.startTyping(postId, parentId),
    stopTyping: (postId: string, parentId?: string) => commentSocket.stopTyping(postId, parentId),
    markNotificationAsRead: (notificationId: string) => commentSocket.markNotificationAsRead(notificationId),
    getRecentNotifications: (limit?: number) => commentSocket.getRecentNotifications(limit),
    getUnreadNotificationCount: () => commentSocket.getUnreadNotificationCount(),
    on: <K extends keyof CommentSocketEvents>(event: K, listener: CommentSocketEvents[K]) =>
      commentSocket.on(event, listener),
    off: <K extends keyof CommentSocketEvents>(event: K, listener: CommentSocketEvents[K]) => {
      commentSocket.off(event, listener);
    },
    isConnected: commentSocket.isConnected,
    connectionState: commentSocket.connectionState,
    connectionStatus: commentSocket.connectionStatus,
  };
};
