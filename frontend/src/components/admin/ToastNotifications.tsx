/**
 * Toast Notifications Component
 * Displays toast notifications for admin operations
 * Integrates with the notification service
 */

import React from 'react';
import { X, CheckCircle, AlertTriangle, Info, AlertCircle } from 'lucide-react';
import { useNotifications, ToastNotification, ToastType } from '../../services/admin/notificationService';

// Toast Icon Component
const ToastIcon: React.FC<{ type: ToastType }> = ({ type }) => {
  const iconProps = { className: 'h-5 w-5 mr-3 flex-shrink-0' };

  switch (type) {
    case 'success':
      return <CheckCircle {...iconProps} className={`${iconProps.className} text-green-600`} />;
    case 'error':
      return <AlertCircle {...iconProps} className={`${iconProps.className} text-red-600`} />;
    case 'warning':
      return <AlertTriangle {...iconProps} className={`${iconProps.className} text-yellow-600`} />;
    case 'info':
      return <Info {...iconProps} className={`${iconProps.className} text-blue-600`} />;
    default:
      return <Info {...iconProps} className={`${iconProps.className} text-gray-600`} />;
  }
};

// Individual Toast Component
const Toast: React.FC<{
  notification: ToastNotification;
  onRemove: (id: string) => void;
}> = ({ notification, onRemove }) => {
  const getToastStyles = (type: ToastType): string => {
    const baseStyles = 'relative flex items-start p-4 mb-3 rounded-lg shadow-lg border-l-4 transition-all duration-300 ease-in-out transform translate-x-0 max-w-md w-full';

    switch (type) {
      case 'success':
        return `${baseStyles} bg-green-50 border-green-400 text-green-800`;
      case 'error':
        return `${baseStyles} bg-red-50 border-red-400 text-red-800`;
      case 'warning':
        return `${baseStyles} bg-yellow-50 border-yellow-400 text-yellow-800`;
      case 'info':
        return `${baseStyles} bg-blue-50 border-blue-400 text-blue-800`;
      default:
        return `${baseStyles} bg-gray-50 border-gray-400 text-gray-800`;
    }
  };

  return (
    <div className={getToastStyles(notification.type)}>
      <ToastIcon type={notification.type} />
      <div className="flex-1 min-w-0">
        <div className="text-sm font-medium">{notification.title}</div>
        {notification.message && (
          <div className="mt-1 text-sm opacity-90">{notification.message}</div>
        )}
        {notification.actions && notification.actions.length > 0 && (
          <div className="mt-3 flex space-x-2">
            {notification.actions.map((action, index) => (
              <button
                key={index}
                onClick={() => {
                  action.action();
                  onRemove(notification.id);
                }}
                className="text-xs font-medium underline hover:no-underline focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-current rounded"
              >
                {action.label}
              </button>
            ))}
          </div>
        )}
      </div>
      <button
        onClick={() => onRemove(notification.id)}
        className="ml-4 flex-shrink-0 text-current opacity-60 hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-current rounded p-1 transition-opacity"
        aria-label="Dismiss notification"
      >
        <X className="h-4 w-4" />
      </button>
    </div>
  );
};

// Toast Container Component
const ToastNotifications: React.FC = () => {
  const { notifications, remove } = useNotifications();

  if (notifications.length === 0) return null;

  return (
    <div
      className="fixed top-4 right-4 z-50 space-y-2"
      aria-live="polite"
      aria-label="Notifications"
    >
      {notifications.map((notification) => (
        <Toast
          key={notification.id}
          notification={notification}
          onRemove={remove}
        />
      ))}
    </div>
  );
};

// Toast Provider Component (wrapper for app)
export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
    <>
      {children}
      <ToastNotifications />
    </>
  );
};

// Export both the default and named export
export { ToastNotifications };
export default ToastNotifications;