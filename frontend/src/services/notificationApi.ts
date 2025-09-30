/**
 * Notification API Service
 * Comprehensive notification settings management
 */

import { apiClient } from './api/client';
import type {
  NotificationSettings,
  NotificationStats,
  NotificationTemplate,
  NotificationSettingsResponse,
  NotificationStatsResponse,
  UpdateNotificationSettingsRequest,
  TestNotificationRequest,
  NotificationChannel,
  NotificationCategory
} from '../types/notifications';

/**
 * Notification API service
 */
export const notificationApi = {
  /**
   * Get current user's notification settings
   */
  async getSettings(): Promise<NotificationSettings> {
    const response = await apiClient.get<NotificationSettingsResponse>('/users/me/notifications/settings');
    return response.data.settings;
  },

  /**
   * Update notification settings
   */
  async updateSettings(settings: Partial<NotificationSettings>): Promise<NotificationSettings> {
    const response = await apiClient.put<NotificationSettingsResponse>(
      '/users/me/notifications/settings',
      { settings } as UpdateNotificationSettingsRequest
    );
    return response.data.settings;
  },

  /**
   * Reset notification settings to defaults
   */
  async resetToDefaults(): Promise<NotificationSettings> {
    const response = await apiClient.post<NotificationSettingsResponse>('/users/me/notifications/settings/reset');
    return response.data.settings;
  },

  /**
   * Get notification statistics
   */
  async getStats(): Promise<NotificationStats> {
    const response = await apiClient.get<NotificationStatsResponse>('/users/me/notifications/stats');
    return response.data.stats;
  },

  /**
   * Request browser notification permission
   */
  async requestBrowserPermission(): Promise<NotificationPermission> {
    if (!('Notification' in window)) {
      throw new Error('Browser notifications not supported');
    }

    try {
      const permission = await Notification.requestPermission();

      // Update settings on server
      await this.updateSettings({
        push: {
          browser: {
            enabled: permission === 'granted',
            permission: permission
          }
        }
      } as Partial<NotificationSettings>);

      return permission;
    } catch (error) {
      console.error('Error requesting notification permission:', error);
      throw error;
    }
  },

  /**
   * Test notification delivery
   */
  async testNotification(request: TestNotificationRequest): Promise<void> {
    await apiClient.post('/users/me/notifications/test', request);
  },

  /**
   * Get available notification templates
   */
  async getTemplates(): Promise<NotificationTemplate[]> {
    const response = await apiClient.get<{ templates: NotificationTemplate[] }>('/notifications/templates');
    return response.data.templates;
  },

  /**
   * Update notification template
   */
  async updateTemplate(templateId: string, updates: Partial<NotificationTemplate>): Promise<NotificationTemplate> {
    const response = await apiClient.put<{ template: NotificationTemplate }>(`/notifications/templates/${templateId}`, updates);
    return response.data.template;
  },

  /**
   * Get notification history
   */
  async getHistory(params?: {
    limit?: number;
    offset?: number;
    category?: NotificationCategory;
    channel?: NotificationChannel;
    startDate?: string;
    endDate?: string;
  }): Promise<{
    notifications: Array<{
      id: string;
      category: NotificationCategory;
      channel: NotificationChannel;
      subject: string;
      body: string;
      status: 'sent' | 'delivered' | 'read' | 'failed';
      sentAt: string;
      readAt?: string;
    }>;
    total: number;
  }> {
    const response = await apiClient.get('/users/me/notifications/history', { params });
    return response.data as {
      notifications: {
        id: string;
        category: NotificationCategory;
        channel: NotificationChannel;
        subject: string;
        body: string;
        status: 'sent' | 'delivered' | 'read' | 'failed';
        sentAt: string;
        readAt?: string;
      }[];
      total: number;
    };
  },

  /**
   * Mark notification as read
   */
  async markAsRead(notificationId: string): Promise<void> {
    await apiClient.post(`/users/me/notifications/${notificationId}/read`);
  },

  /**
   * Mark all notifications as read
   */
  async markAllAsRead(): Promise<void> {
    await apiClient.post('/users/me/notifications/read-all');
  },

  /**
   * Delete notification
   */
  async deleteNotification(notificationId: string): Promise<void> {
    await apiClient.delete(`/users/me/notifications/${notificationId}`);
  },

  /**
   * Unsubscribe from email notifications
   */
  async unsubscribeEmail(token: string, category?: NotificationCategory): Promise<void> {
    await apiClient.post('/notifications/unsubscribe', { token, category });
  },

  /**
   * Subscribe to push notifications
   */
  async subscribePush(subscription: PushSubscription): Promise<void> {
    await apiClient.post('/users/me/notifications/push/subscribe', {
      subscription: {
        endpoint: subscription.endpoint,
        keys: {
          p256dh: this.arrayBufferToBase64(subscription.getKey('p256dh')!),
          auth: this.arrayBufferToBase64(subscription.getKey('auth')!)
        }
      }
    });
  },

  /**
   * Unsubscribe from push notifications
   */
  async unsubscribePush(): Promise<void> {
    await apiClient.post('/users/me/notifications/push/unsubscribe');
  },

  /**
   * Update do not disturb schedule
   */
  async updateDoNotDisturbSchedule(scheduleId: string, schedule: {
    name: string;
    enabled: boolean;
    startTime: string;
    endTime: string;
    days: number[];
    timezone: string;
    allowUrgent: boolean;
  }): Promise<void> {
    await apiClient.put(`/users/me/notifications/dnd-schedules/${scheduleId}`, schedule);
  },

  /**
   * Create do not disturb schedule
   */
  async createDoNotDisturbSchedule(schedule: {
    name: string;
    enabled: boolean;
    startTime: string;
    endTime: string;
    days: number[];
    timezone: string;
    allowUrgent: boolean;
  }): Promise<{ id: string }> {
    const response = await apiClient.post<{ id: string }>('/users/me/notifications/dnd-schedules', schedule);
    return response.data;
  },

  /**
   * Delete do not disturb schedule
   */
  async deleteDoNotDisturbSchedule(scheduleId: string): Promise<void> {
    await apiClient.delete(`/users/me/notifications/dnd-schedules/${scheduleId}`);
  },

  /**
   * Utility method to convert ArrayBuffer to Base64
   */
  arrayBufferToBase64(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    bytes.forEach(byte => binary += String.fromCharCode(byte));
    return window.btoa(binary);
  }
};

/**
 * Notification utilities
 */
export const notificationUtils = {
  /**
   * Check if browser supports notifications
   */
  isSupported(): boolean {
    return 'Notification' in window;
  },

  /**
   * Check if service worker is available for push notifications
   */
  isServiceWorkerSupported(): boolean {
    return 'serviceWorker' in navigator && 'PushManager' in window;
  },

  /**
   * Get current notification permission
   */
  getPermission(): NotificationPermission {
    if (!this.isSupported()) return 'denied';
    return Notification.permission;
  },

  /**
   * Show browser notification (for testing)
   */
  showBrowserNotification(title: string, options?: NotificationOptions): Notification | null {
    if (this.getPermission() !== 'granted') return null;

    return new Notification(title, {
      icon: '/icons/notification.png',
      badge: '/icons/badge.png',
      ...options
    });
  },

  /**
   * Validate time format (HH:mm)
   */
  validateTimeFormat(time: string): boolean {
    return /^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$/.test(time);
  },

  /**
   * Convert 24h time to 12h format
   */
  formatTime12h(time24: string): string {
    const [hours, minutes] = time24.split(':').map(Number);
    const period = hours >= 12 ? 'PM' : 'AM';
    const hours12 = hours % 12 || 12;
    return `${hours12}:${minutes.toString().padStart(2, '0')} ${period}`;
  },

  /**
   * Convert 12h time to 24h format
   */
  formatTime24h(time12: string): string {
    const [time, period] = time12.split(' ');
    const [hours, minutes] = time.split(':').map(Number);
    let hours24 = hours;

    if (period === 'PM' && hours !== 12) {
      hours24 += 12;
    } else if (period === 'AM' && hours === 12) {
      hours24 = 0;
    }

    return `${hours24.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
  },

  /**
   * Get day names for schedule UI
   */
  getDayNames(locale: string = 'en'): string[] {
    const formatter = new Intl.DateTimeFormat(locale, { weekday: 'long' });
    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(2023, 0, i + 1); // Start from Sunday
      return formatter.format(date);
    });
  },

  /**
   * Check if current time is within do not disturb schedule
   */
  isInDoNotDisturbPeriod(schedules: Array<{
    enabled: boolean;
    startTime: string;
    endTime: string;
    days: number[];
    timezone: string;
  }>): boolean {
    const now = new Date();
    const currentDay = now.getDay();
    const currentTime = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}`;

    return schedules.some(schedule => {
      if (!schedule.enabled || !schedule.days.includes(currentDay)) {
        return false;
      }

      const start = schedule.startTime;
      const end = schedule.endTime;

      // Handle overnight schedules (end time is next day)
      if (start > end) {
        return currentTime >= start || currentTime <= end;
      } else {
        return currentTime >= start && currentTime <= end;
      }
    });
  }
};

export default notificationApi;