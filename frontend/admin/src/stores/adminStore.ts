// @ts-nocheck
import { create } from 'zustand';
import { devtools, persist, subscribeWithSelector } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type { User, Notification, GlobalState } from '@/types';

export interface AdminState extends GlobalState {
  // Extended UI state
  pageTitle: string;
  breadcrumbs: Array<{ title: string; path?: string }>;

  // Session management
  sessionTimeout: number;
  lastActivity: string;
  isSessionExpired: boolean;

  // Feature flags
  features: Record<string, boolean>;

  // Application settings
  settings: {
    language: string;
    timezone: string;
    dateFormat: string;
    currency: string;
    pageSize: number;
    autoSave: boolean;
    soundNotifications: boolean;
  };

  // Real-time connection state
  connectionStatus: 'connected' | 'disconnected' | 'connecting' | 'error';

  // Performance monitoring
  performanceMetrics: {
    pageLoadTime: number;
    apiResponseTime: number;
    renderTime: number;
  };

  // Actions
  setUser: (user: User | null) => void;
  setPermissions: (permissions: string[]) => void;
  setCollapsed: (collapsed: boolean) => void;
  setTheme: (theme: 'light' | 'dark') => void;
  setLoading: (loading: boolean) => void;

  // Notification management
  addNotification: (notification: Omit<Notification, 'id' | 'createdAt'>) => void;
  removeNotification: (id: string) => void;
  clearNotifications: () => void;
  markNotificationAsRead: (id: string) => void;
  markAllNotificationsAsRead: () => void;

  // Navigation
  setPageTitle: (title: string) => void;
  setBreadcrumbs: (breadcrumbs: Array<{ title: string; path?: string }>) => void;

  // Session management
  updateLastActivity: () => void;
  setSessionExpired: (expired: boolean) => void;
  extendSession: () => void;

  // Feature flags
  enableFeature: (feature: string) => void;
  disableFeature: (feature: string) => void;
  isFeatureEnabled: (feature: string) => boolean;

  // Settings
  updateSettings: (settings: Partial<AdminState['settings']>) => void;

  // Connection status
  setConnectionStatus: (status: AdminState['connectionStatus']) => void;

  // Performance monitoring
  updatePerformanceMetrics: (metrics: Partial<AdminState['performanceMetrics']>) => void;

  // Utility actions
  logout: () => void;
  reset: () => void;
}

const initialState = {
  // Base GlobalState
  user: null,
  permissions: [],
  collapsed: false,
  theme: 'light' as const,
  loading: false,
  notifications: [],

  // Extended UI state
  pageTitle: 'Admin Dashboard',
  breadcrumbs: [],

  // Session management
  sessionTimeout: 30 * 60 * 1000, // 30 minutes in milliseconds
  lastActivity: new Date().toISOString(),
  isSessionExpired: false,

  // Feature flags
  features: {
    realTimeUpdates: true,
    advancedAnalytics: true,
    bulkOperations: true,
    exportData: true,
    auditLogs: true,
    systemMonitoring: true,
  },

  // Application settings
  settings: {
    language: 'en',
    timezone: 'UTC',
    dateFormat: 'YYYY-MM-DD',
    currency: 'USD',
    pageSize: 20,
    autoSave: true,
    soundNotifications: false,
  },

  // Real-time connection state
  connectionStatus: 'disconnected' as const,

  // Performance monitoring
  performanceMetrics: {
    pageLoadTime: 0,
    apiResponseTime: 0,
    renderTime: 0,
  },
};

export const useAdminStore = create<AdminState>()(
  devtools(
    persist(
      subscribeWithSelector(
        immer((set, get) => ({
          ...initialState,

          setUser: (user) => {
            set((state) => {
              state.user = user;
              if (user) {
                state.permissions = user.roles.flatMap(role =>
                  role.permissions.map(permission => permission.code)
                );
                state.lastActivity = new Date().toISOString();
                state.isSessionExpired = false;
              } else {
                state.permissions = [];
              }
            });
          },

          setPermissions: (permissions) => {
            set((state) => {
              state.permissions = permissions;
            });
          },

          setCollapsed: (collapsed) => {
            set((state) => {
              state.collapsed = collapsed;
            });
          },

          setTheme: (theme) => {
            set((state) => {
              state.theme = theme;
            });

            // Update HTML data-theme attribute
            document.documentElement.setAttribute('data-theme', theme);
          },

          setLoading: (loading) => {
            set((state) => {
              state.loading = loading;
            });
          },

          // Enhanced notification management
          addNotification: (notification) => {
            const id = `notification_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            const newNotification: Notification = {
              ...notification,
              id,
              createdAt: new Date().toISOString(),
            };

            set((state) => {
              state.notifications.unshift(newNotification);

              // Limit notification count
              if (state.notifications.length > 10) {
                state.notifications = state.notifications.slice(0, 10);
              }
            });

            // Auto-remove notification
            if (notification.duration !== 0) {
              setTimeout(() => {
                get().removeNotification(id);
              }, notification.duration || 4500);
            }

            // Play sound if enabled
            const { settings } = get();
            if (settings.soundNotifications && notification.type === 'error') {
              // Could integrate with audio API here
              console.log('🔊 Playing notification sound');
            }
          },

          removeNotification: (id) => {
            set((state) => {
              state.notifications = state.notifications.filter(n => n.id !== id);
            });
          },

          clearNotifications: () => {
            set((state) => {
              state.notifications = [];
            });
          },

          markNotificationAsRead: (id) => {
            set((state) => {
              const notification = state.notifications.find(n => n.id === id);
              if (notification) {
                // Could add a 'read' property to notifications type
                console.log(`Marking notification ${id} as read`);
              }
            });
          },

          markAllNotificationsAsRead: () => {
            set((state) => {
              // Could add bulk read functionality
              console.log('Marking all notifications as read');
            });
          },

          // Navigation
          setPageTitle: (title) => {
            set((state) => {
              state.pageTitle = title;
            });
            // Update document title
            document.title = `${title} - Admin Dashboard`;
          },

          setBreadcrumbs: (breadcrumbs) => {
            set((state) => {
              state.breadcrumbs = breadcrumbs;
            });
          },

          // Session management
          updateLastActivity: () => {
            set((state) => {
              state.lastActivity = new Date().toISOString();
              state.isSessionExpired = false;
            });
          },

          setSessionExpired: (expired) => {
            set((state) => {
              state.isSessionExpired = expired;
              if (expired) {
                state.connectionStatus = 'error';
              }
            });
          },

          extendSession: () => {
            set((state) => {
              state.lastActivity = new Date().toISOString();
              state.isSessionExpired = false;
            });
          },

          // Feature flags
          enableFeature: (feature) => {
            set((state) => {
              state.features[feature] = true;
            });
          },

          disableFeature: (feature) => {
            set((state) => {
              state.features[feature] = false;
            });
          },

          isFeatureEnabled: (feature) => {
            const state = get();
            return state.features[feature] || false;
          },

          // Settings
          updateSettings: (newSettings) => {
            set((state) => {
              state.settings = { ...state.settings, ...newSettings };
            });
          },

          // Connection status
          setConnectionStatus: (status) => {
            set((state) => {
              state.connectionStatus = status;
            });

            // Add connection status notification
            if (status === 'disconnected') {
              get().addNotification({
                type: 'warning',
                title: 'Connection Lost',
                description: 'Real-time updates are unavailable',
                duration: 0,
              });
            } else if (status === 'connected') {
              get().addNotification({
                type: 'success',
                title: 'Connected',
                description: 'Real-time updates are active',
                duration: 3000,
              });
            }
          },

          // Performance monitoring
          updatePerformanceMetrics: (metrics) => {
            set((state) => {
              state.performanceMetrics = { ...state.performanceMetrics, ...metrics };
            });
          },

          logout: () => {
            set((state) => {
              state.user = null;
              state.permissions = [];
              state.notifications = [];
              state.isSessionExpired = false;
              state.connectionStatus = 'disconnected';
            });

            // Clear other stores if needed
            // This could be extended to reset other stores
          },

          reset: () => {
            set(() => initialState);
          },
        }))
      ),
      {
        name: 'admin-store',
        partialize: (state) => ({
          collapsed: state.collapsed,
          theme: state.theme,
          settings: state.settings,
          features: state.features,
        }),
      }
    ),
    {
      name: 'admin-store',
      enabled: process.env.NODE_ENV === 'development',
    }
  )
);

// Basic selectors
export const useUser = () => useAdminStore((state) => state.user);
export const usePermissions = () => useAdminStore((state) => state.permissions);
export const useCollapsed = () => useAdminStore((state) => state.collapsed);
export const useTheme = () => useAdminStore((state) => state.theme);
export const useLoading = () => useAdminStore((state) => state.loading);
export const useNotifications = () => useAdminStore((state) => state.notifications);

// Extended selectors
export const usePageTitle = () => useAdminStore((state) => state.pageTitle);
export const useBreadcrumbs = () => useAdminStore((state) => state.breadcrumbs);
export const useSessionState = () => useAdminStore((state) => ({
  timeout: state.sessionTimeout,
  lastActivity: state.lastActivity,
  isExpired: state.isSessionExpired,
}));
export const useFeatures = () => useAdminStore((state) => state.features);
export const useSettings = () => useAdminStore((state) => state.settings);
export const useConnectionStatus = () => useAdminStore((state) => state.connectionStatus);
export const usePerformanceMetrics = () => useAdminStore((state) => state.performanceMetrics);

// Permission checking hooks
export const useHasPermission = (permission: string | string[]) => {
  const permissions = usePermissions();

  if (Array.isArray(permission)) {
    return permission.some(p => permissions.includes(p));
  }

  return permissions.includes(permission);
};

export const useHasAllPermissions = (permissions: string[]) => {
  const userPermissions = usePermissions();
  return permissions.every(p => userPermissions.includes(p));
};

// Role checking hooks
export const useHasRole = (role: string | string[]) => {
  const user = useUser();

  if (!user) return false;

  const userRoles = user.roles.map(r => r.name);

  if (Array.isArray(role)) {
    return role.some(r => userRoles.includes(r));
  }

  return userRoles.includes(role);
};

// Feature flag hooks
export const useFeatureEnabled = (feature: string) => {
  return useAdminStore((state) => state.features[feature] || false);
};

// Computed selectors
export const useUnreadNotifications = () => {
  return useAdminStore((state) =>
    state.notifications.filter(n => n.type === 'error' || n.type === 'warning').length
  );
};

export const useIsSessionActive = () => {
  return useAdminStore((state) => {
    if (!state.user || state.isSessionExpired) return false;

    const now = new Date().getTime();
    const lastActivity = new Date(state.lastActivity).getTime();
    const timeDiff = now - lastActivity;

    return timeDiff < state.sessionTimeout;
  });
};

export const useConnectionHealth = () => {
  return useAdminStore((state) => ({
    status: state.connectionStatus,
    isConnected: state.connectionStatus === 'connected',
    isConnecting: state.connectionStatus === 'connecting',
    hasError: state.connectionStatus === 'error',
  }));
};

export const usePerformanceStatus = () => {
  return useAdminStore((state) => {
    const { pageLoadTime, apiResponseTime } = state.performanceMetrics;

    let status: 'excellent' | 'good' | 'fair' | 'poor' = 'excellent';

    if (pageLoadTime > 3000 || apiResponseTime > 1000) {
      status = 'poor';
    } else if (pageLoadTime > 2000 || apiResponseTime > 500) {
      status = 'fair';
    } else if (pageLoadTime > 1000 || apiResponseTime > 200) {
      status = 'good';
    }

    return {
      status,
      metrics: state.performanceMetrics,
    };
  });
};

// Store subscription hooks for real-time updates
export const useStoreSubscription = () => {
  const store = useAdminStore;

  return {
    // Subscribe to user changes
    subscribeToUser: (callback: (user: User | null) => void) => {
      return store.subscribe(
        (state) => state.user,
        callback,
        { equalityFn: (a, b) => a?.id === b?.id }
      );
    },

    // Subscribe to connection status changes
    subscribeToConnection: (callback: (status: string) => void) => {
      return store.subscribe(
        (state) => state.connectionStatus,
        callback
      );
    },

    // Subscribe to notification changes
    subscribeToNotifications: (callback: (notifications: Notification[]) => void) => {
      return store.subscribe(
        (state) => state.notifications,
        callback,
        { equalityFn: (a, b) => a.length === b.length }
      );
    },
  };
};