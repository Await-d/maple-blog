/**
 * Toast Notification Service for Admin Dashboard
 * Provides consistent user feedback across admin operations
 */

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface ToastNotification {
  id: string;
  type: ToastType;
  title: string;
  message?: string;
  duration?: number;
  timestamp: Date;
  actions?: Array<{
    label: string;
    action: () => void;
  }>;
}

class NotificationService {
  private notifications: ToastNotification[] = [];
  private listeners: Array<(notifications: ToastNotification[]) => void> = [];
  private nextId = 1;

  // Add a new notification
  show(type: ToastType, title: string, message?: string, options?: {
    duration?: number;
    actions?: Array<{ label: string; action: () => void }>;
  }): string {
    const id = `toast-${this.nextId++}`;
    const notification: ToastNotification = {
      id,
      type,
      title,
      message,
      duration: options?.duration ?? this.getDefaultDuration(type),
      timestamp: new Date(),
      actions: options?.actions,
    };

    this.notifications.push(notification);
    this.notifyListeners();

    // Auto-remove after duration
    if (notification.duration && notification.duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, notification.duration);
    }

    return id;
  }

  // Remove a notification by ID
  remove(id: string): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
    this.notifyListeners();
  }

  // Clear all notifications
  clear(): void {
    this.notifications = [];
    this.notifyListeners();
  }

  // Get all notifications
  getAll(): ToastNotification[] {
    return [...this.notifications];
  }

  // Subscribe to notification changes
  subscribe(listener: (notifications: ToastNotification[]) => void): () => void {
    this.listeners.push(listener);
    return () => {
      this.listeners = this.listeners.filter(l => l !== listener);
    };
  }

  // Convenience methods for different types
  success(title: string, message?: string, duration?: number): string {
    return this.show('success', title, message, { duration });
  }

  error(title: string, message?: string, duration?: number): string {
    return this.show('error', title, message, { duration: duration ?? 8000 });
  }

  warning(title: string, message?: string, duration?: number): string {
    return this.show('warning', title, message, { duration });
  }

  info(title: string, message?: string, duration?: number): string {
    return this.show('info', title, message, { duration });
  }

  // Show operation result notification
  showOperationResult(
    operation: string,
    success: boolean,
    details?: { count?: number; errors?: string[] }
  ): string {
    if (success) {
      const countText = details?.count ? ` (${details.count} items)` : '';
      return this.success(
        `${operation} completed successfully`,
        `Operation completed${countText}`
      );
    } else {
      const errorText = details?.errors?.length
        ? details.errors.join(', ')
        : 'Please try again';
      return this.error(
        `${operation} failed`,
        errorText
      );
    }
  }

  // Show bulk operation result
  showBulkOperationResult(
    operation: string,
    result: { processedCount: number; failedCount: number; errors?: string[] }
  ): string {
    const { processedCount, failedCount, errors } = result;

    if (failedCount === 0) {
      return this.success(
        `Bulk ${operation} completed`,
        `Successfully processed ${processedCount} items`
      );
    } else if (processedCount === 0) {
      const errorText = errors?.length ? errors.join(', ') : 'All operations failed';
      return this.error(
        `Bulk ${operation} failed`,
        errorText
      );
    } else {
      const errorText = errors?.length ? ` Errors: ${errors.join(', ')}` : '';
      return this.warning(
        `Bulk ${operation} partially completed`,
        `${processedCount} succeeded, ${failedCount} failed.${errorText}`
      );
    }
  }

  // Show confirmation with actions
  showConfirmation(
    title: string,
    message: string,
    onConfirm: () => void,
    onCancel?: () => void
  ): string {
    return this.show('warning', title, message, {
      duration: 0, // Don't auto-dismiss
      actions: [
        {
          label: 'Confirm',
          action: () => {
            onConfirm();
          }
        },
        {
          label: 'Cancel',
          action: () => {
            onCancel?.();
          }
        }
      ]
    });
  }

  // Show network error
  showNetworkError(): string {
    return this.error(
      'Network Error',
      'Unable to connect to server. Please check your connection and try again.',
      10000
    );
  }

  // Show validation error
  showValidationError(errors: Record<string, string[]>): string {
    const errorMessages = Object.entries(errors)
      .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
      .join('\n');

    return this.error(
      'Validation Error',
      errorMessages,
      8000
    );
  }

  private getDefaultDuration(type: ToastType): number {
    switch (type) {
      case 'success': return 4000;
      case 'info': return 5000;
      case 'warning': return 6000;
      case 'error': return 8000;
      default: return 5000;
    }
  }

  private notifyListeners(): void {
    this.listeners.forEach(listener => {
      listener([...this.notifications]);
    });
  }
}

// Create singleton instance
export const notificationService = new NotificationService();

// React hook for notifications
import { useState, useEffect } from 'react';

export const useNotifications = () => {
  const [notifications, setNotifications] = useState<ToastNotification[]>([]);

  useEffect(() => {
    const unsubscribe = notificationService.subscribe(setNotifications);
    setNotifications(notificationService.getAll());
    return unsubscribe;
  }, []);

  return {
    notifications,
    show: notificationService.show.bind(notificationService),
    success: notificationService.success.bind(notificationService),
    error: notificationService.error.bind(notificationService),
    warning: notificationService.warning.bind(notificationService),
    info: notificationService.info.bind(notificationService),
    remove: notificationService.remove.bind(notificationService),
    clear: notificationService.clear.bind(notificationService),
    showOperationResult: notificationService.showOperationResult.bind(notificationService),
    showBulkOperationResult: notificationService.showBulkOperationResult.bind(notificationService),
    showConfirmation: notificationService.showConfirmation.bind(notificationService),
    showNetworkError: notificationService.showNetworkError.bind(notificationService),
    showValidationError: notificationService.showValidationError.bind(notificationService),
  };
};

// Export service for direct usage
export { NotificationService };
export default notificationService;