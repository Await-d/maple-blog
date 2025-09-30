import { createRoot } from 'react-dom/client';
import { createElement } from 'react';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastOptions {
  duration?: number; // Auto-dismiss time in ms, 0 = no auto-dismiss
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left' | 'top-center' | 'bottom-center';
  closable?: boolean;
  actionText?: string;
  onAction?: () => void;
  onClose?: () => void;
  className?: string;
  maxWidth?: number;
  showIcon?: boolean;
  persistent?: boolean; // Survives page navigation
}

export interface Toast {
  id: string;
  message: string;
  type: ToastType;
  options: Required<ToastOptions>;
  timestamp: Date;
  visible: boolean;
  dismissed: boolean;
}

import type { Root } from 'react-dom/client';

class ToastNotificationService {
  private toasts: Map<string, Toast> = new Map();
  private container: HTMLDivElement | null = null;
  private root: Root | null = null;
  private readonly maxToasts = 5;
  private nextId = 1;

  constructor() {
    this.initializeContainer();
  }

  /**
   * Show a toast notification
   */
  show(
    message: string,
    type: ToastType = 'info',
    options: ToastOptions = {}
  ): string {
    const defaultOptions: Required<ToastOptions> = {
      duration: type === 'error' ? 6000 : 4000,
      position: 'top-right',
      closable: true,
      actionText: '',
      onAction: () => { /* noop - action handler is optional */ },
      onClose: () => { /* noop - close handler is optional */ },
      className: '',
      maxWidth: 400,
      showIcon: true,
      persistent: false
    };

    const toastOptions = { ...defaultOptions, ...options };
    const id = `toast_${this.nextId++}`;

    const toast: Toast = {
      id,
      message,
      type,
      options: toastOptions,
      timestamp: new Date(),
      visible: true,
      dismissed: false
    };

    this.addToast(toast);
    return id;
  }

  /**
   * Convenience methods for different toast types
   */
  success(message: string, options?: ToastOptions): string {
    return this.show(message, 'success', options);
  }

  error(message: string, options?: ToastOptions): string {
    return this.show(message, 'error', options);
  }

  warning(message: string, options?: ToastOptions): string {
    return this.show(message, 'warning', options);
  }

  info(message: string, options?: ToastOptions): string {
    return this.show(message, 'info', options);
  }

  /**
   * Dismiss a specific toast
   */
  dismiss(id: string): void {
    const toast = this.toasts.get(id);
    if (toast && !toast.dismissed) {
      toast.dismissed = true;
      toast.visible = false;
      toast.options.onClose();

      // Remove after animation
      setTimeout(() => {
        this.toasts.delete(id);
        this.render();
      }, 300);

      this.render();
    }
  }

  /**
   * Dismiss all toasts
   */
  dismissAll(): void {
    Array.from(this.toasts.keys()).forEach(id => this.dismiss(id));
  }

  /**
   * Get all active toasts
   */
  getToasts(): Toast[] {
    return Array.from(this.toasts.values()).filter(toast => !toast.dismissed);
  }

  /**
   * Clear all toasts (immediate removal)
   */
  clear(): void {
    this.toasts.clear();
    this.render();
  }

  /**
   * Update toast position for all active toasts
   */
  setPosition(position: ToastOptions['position']): void {
    this.toasts.forEach(toast => {
      if (!toast.dismissed) {
        toast.options.position = position || 'top-right';
      }
    });
    this.render();
  }

  private addToast(toast: Toast): void {
    this.toasts.set(toast.id, toast);

    // Ensure we don't exceed max toasts
    if (this.toasts.size > this.maxToasts) {
      const oldestId = Array.from(this.toasts.keys())[0];
      this.dismiss(oldestId);
    }

    // Auto-dismiss if duration is set
    if (toast.options.duration > 0) {
      setTimeout(() => {
        this.dismiss(toast.id);
      }, toast.options.duration);
    }

    this.render();
  }

  private initializeContainer(): void {
    if (typeof document === 'undefined') return;

    this.container = document.createElement('div');
    this.container.id = 'toast-container';
    this.container.className = 'toast-container';
    this.container.style.cssText = `
      position: fixed;
      z-index: 10000;
      pointer-events: none;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    `;

    document.body.appendChild(this.container);
    this.root = createRoot(this.container);
  }

  private render(): void {
    if (!this.root) return;

    const toastsByPosition = this.groupToastsByPosition();
    const toastContainers = Object.entries(toastsByPosition).map(([position, toasts]) =>
      createElement(ToastPositionContainer, {
        key: position,
        position: position as ToastOptions['position'],
        toasts,
        onDismiss: (id: string) => this.dismiss(id)
      })
    );

    this.root.render(createElement('div', {}, ...toastContainers));
  }

  private groupToastsByPosition(): Record<string, Toast[]> {
    const groups: Record<string, Toast[]> = {};

    this.toasts.forEach(toast => {
      if (!toast.dismissed) {
        const position = toast.options.position;
        if (!groups[position]) {
          groups[position] = [];
        }
        groups[position].push(toast);
      }
    });

    return groups;
  }
}

// React Components for rendering toasts
interface ToastPositionContainerProps {
  position: ToastOptions['position'];
  toasts: Toast[];
  onDismiss: (id: string) => void;
}

function ToastPositionContainer({ position, toasts, onDismiss }: ToastPositionContainerProps) {
  const positionStyles = getPositionStyles(position);

  return createElement('div', {
    className: `toast-position-${position}`,
    style: {
      position: 'absolute',
      display: 'flex',
      flexDirection: position?.includes('top') ? 'column' : 'column-reverse',
      gap: '8px',
      maxWidth: '100vw',
      padding: '16px',
      boxSizing: 'border-box',
      pointerEvents: 'none',
      ...positionStyles
    }
  }, toasts.map(toast =>
    createElement(ToastComponent, {
      key: toast.id,
      toast,
      onDismiss
    })
  ));
}

interface ToastComponentProps {
  toast: Toast;
  onDismiss: (id: string) => void;
}

function ToastComponent({ toast, onDismiss }: ToastComponentProps) {
  const { message, type, options, visible } = toast;

  const handleAction = () => {
    if (options.onAction) {
      options.onAction();
    }
  };

  const handleClose = () => {
    onDismiss(toast.id);
  };

  const typeColors = {
    success: { bg: '#10B981', icon: '✓' },
    error: { bg: '#EF4444', icon: '✕' },
    warning: { bg: '#F59E0B', icon: '⚠' },
    info: { bg: '#3B82F6', icon: 'ℹ' }
  };

  const color = typeColors[type];

  return createElement('div', {
    className: `toast toast-${type} ${options.className}`,
    style: {
      display: 'flex',
      alignItems: 'flex-start',
      gap: '12px',
      padding: '12px 16px',
      backgroundColor: 'white',
      border: `1px solid ${color.bg}`,
      borderLeft: `4px solid ${color.bg}`,
      borderRadius: '6px',
      boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
      maxWidth: `${options.maxWidth}px`,
      minWidth: '300px',
      pointerEvents: 'auto',
      transform: visible ? 'translateY(0)' : 'translateY(-100%)',
      opacity: visible ? 1 : 0,
      transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
      fontSize: '14px',
      lineHeight: '1.5'
    }
  }, [
    // Icon
    options.showIcon && createElement('div', {
      key: 'icon',
      style: {
        color: color.bg,
        fontWeight: 'bold',
        fontSize: '16px',
        flexShrink: 0,
        marginTop: '1px'
      }
    }, color.icon),

    // Content
    createElement('div', {
      key: 'content',
      style: { flex: 1, minWidth: 0 }
    }, [
      createElement('div', {
        key: 'message',
        style: {
          color: '#374151',
          wordWrap: 'break-word'
        }
      }, message),

      // Action button
      options.actionText && createElement('button', {
        key: 'action',
        onClick: handleAction,
        style: {
          marginTop: '8px',
          padding: '4px 8px',
          backgroundColor: color.bg,
          color: 'white',
          border: 'none',
          borderRadius: '4px',
          fontSize: '12px',
          cursor: 'pointer',
          fontWeight: '500'
        }
      }, options.actionText)
    ]),

    // Close button
    options.closable && createElement('button', {
      key: 'close',
      onClick: handleClose,
      style: {
        background: 'none',
        border: 'none',
        color: '#9CA3AF',
        cursor: 'pointer',
        fontSize: '18px',
        lineHeight: 1,
        padding: '0',
        flexShrink: 0
      }
    }, '×')
  ]);
}

function getPositionStyles(position: ToastOptions['position']): React.CSSProperties {
  const styles: React.CSSProperties = {};

  switch (position) {
    case 'top-right':
      styles.top = 0;
      styles.right = 0;
      break;
    case 'top-left':
      styles.top = 0;
      styles.left = 0;
      break;
    case 'top-center':
      styles.top = 0;
      styles.left = '50%';
      styles.transform = 'translateX(-50%)';
      break;
    case 'bottom-right':
      styles.bottom = 0;
      styles.right = 0;
      break;
    case 'bottom-left':
      styles.bottom = 0;
      styles.left = 0;
      break;
    case 'bottom-center':
      styles.bottom = 0;
      styles.left = '50%';
      styles.transform = 'translateX(-50%)';
      break;
  }

  return styles;
}

// Create singleton instance
export const toast = new ToastNotificationService();

// Default export for convenience
export default toast;