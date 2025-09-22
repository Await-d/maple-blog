// @ts-nocheck
// Main admin store exports
import { useAdminStore } from './adminStore';
export {
  useAdminStore,
  type AdminState,
  // Basic selectors
  useUser,
  usePermissions,
  useCollapsed,
  useTheme,
  useLoading,
  useNotifications,
  // Extended selectors
  usePageTitle,
  useBreadcrumbs,
  useSessionState,
  useFeatures,
  useSettings,
  useConnectionStatus,
  usePerformanceMetrics,
  // Permission hooks
  useHasPermission,
  useHasAllPermissions,
  useHasRole,
  useFeatureEnabled,
  // Computed selectors
  useUnreadNotifications,
  useIsSessionActive,
  useConnectionHealth,
  usePerformanceStatus,
  useStoreSubscription,
} from './adminStore';

// Dashboard store exports
import { useDashboardStore } from './dashboardStore';
export {
  useDashboardStore,
  type DashboardState,
  // Dashboard selectors
  useStats,
  useMetrics,
  useHealthCheck,
  useActivities,
  useChartConfigs,
  useRefreshState,
  useDashboardLayout,
  useRealTimeData,
  // Computed dashboard selectors
  useSystemStatus,
  usePerformanceScore,
  useTrendData,
} from './dashboardStore';

// User management store exports
import { useUserManagementStore } from './userManagementStore';
export {
  useUserManagementStore,
  type UserManagementState,
  // User management selectors
  useUsers,
  useSelectedUsers,
  useCurrentUser,
  usePagination,
  useQuery,
  useUserManagementLoading,
  useUserManagementError,
  useBulkOperationProgress,
  useUserForm,
  useRoleAssignment,
  useUserDetails,
  // Computed user management selectors
  useSelectedUsersCount,
  useHasSelectedUsers,
  useUserStats,
} from './userManagementStore';

// Permission store exports
import { usePermissionStore } from './permissionStore';
export {
  usePermissionStore,
  type PermissionState,
  // Permission selectors
  usePermissions as usePermissionsList,
  useRoles,
  useCurrentRole,
  useSelectedPermissions,
  useSelectedRoles,
  useRolePermissionMatrix,
  usePermissionCategories,
  usePermissionLoading,
  usePermissionError,
  usePermissionForm,
  useRoleForm,
  useRolePermissionAssignment,
  useUserRoleAssignment,
  // Computed permission selectors
  usePermissionStats,
  useRoleStats,
} from './permissionStore';

// Real-time store exports
import { useRealTimeStore } from './realTimeStore';
export {
  useRealTimeStore,
  type RealTimeState,
  // Real-time selectors
  useRealTimeConnection,
  useLiveActivities,
  useLiveMetrics,
  useLiveNotifications,
  // Real-time hooks
  useRealTimeChannel,
  useRealTimeEvent,
} from './realTimeStore';

// Store utility types
export interface StoreHydration {
  adminStore: boolean;
  dashboardStore: boolean;
  userManagementStore: boolean;
  permissionStore: boolean;
  realTimeStore: boolean;
}

// Cross-store communication utilities
export const storeUtils = {
  // Reset all stores
  resetAllStores: () => {
    useAdminStore.getState().reset();
    useDashboardStore.getState().reset();
    useUserManagementStore.getState().reset();
    usePermissionStore.getState().reset();
    useRealTimeStore.getState().reset();
  },

  // Check if stores are hydrated from persistence
  getHydrationStatus: (): StoreHydration => {
    return {
      adminStore: !!useAdminStore.getState().user,
      dashboardStore: !!useDashboardStore.getState().stats,
      userManagementStore: useUserManagementStore.getState().users.length > 0,
      permissionStore: usePermissionStore.getState().permissions.length > 0,
      realTimeStore: useRealTimeStore.getState().isConnected,
    };
  },

  // Subscribe to cross-store updates
  subscribeToStoreUpdates: (callback: (storeName: string, state: any) => void) => {
    const unsubscribers = [
      useAdminStore.subscribe(
        (state: any) => state.user,
        (user: any) => callback('admin', { user })
      ),
      useDashboardStore.subscribe(
        (state: any) => state.stats,
        (stats: any) => callback('dashboard', { stats })
      ),
      useUserManagementStore.subscribe(
        (state: any) => state.users,
        (users: any) => callback('userManagement', { users })
      ),
      usePermissionStore.subscribe(
        (state: any) => state.permissions,
        (permissions: any) => callback('permission', { permissions })
      ),
    ];

    // Return cleanup function
    return () => {
      unsubscribers.forEach(unsub => unsub());
    };
  },

  // Sync user permissions across stores
  syncUserPermissions: (user: any) => {
    const adminStore = useAdminStore.getState();
    const permissionStore = usePermissionStore.getState();

    if (user) {
      const permissions = user.roles.flatMap((role: any) =>
        role.permissions.map((permission: any) => permission.code)
      );

      adminStore.setPermissions(permissions);

      // Update permission store with user's roles if needed
      const userRoles = user.roles;
      const existingRoles = permissionStore.roles;

      userRoles.forEach((userRole: any) => {
        const existingRole = existingRoles.find((r: any) => r.id === userRole.id);
        if (!existingRole) {
          permissionStore.addRole(userRole);
        }
      });
    }
  },

  // Performance monitoring across stores
  measureStorePerformance: () => {
    const startTime = performance.now();

    return {
      end: (operation: string) => {
        const endTime = performance.now();
        const duration = endTime - startTime;

        useAdminStore.getState().updatePerformanceMetrics({
          renderTime: duration,
        });

        if (process.env.NODE_ENV === 'development') {
          console.log(`Store operation "${operation}" took ${duration.toFixed(2)}ms`);
        }
      },
    };
  },
};

// Store debugging utilities (development only)
export const storeDebug = process.env.NODE_ENV === 'development' ? {
  // Log current state of all stores
  logAllStores: () => {
    console.group('Store States');
    console.log('Admin Store:', useAdminStore.getState());
    console.log('Dashboard Store:', useDashboardStore.getState());
    console.log('User Management Store:', useUserManagementStore.getState());
    console.log('Permission Store:', usePermissionStore.getState());
    console.groupEnd();
  },

  // Monitor store changes
  monitorStoreChanges: (storeName?: string) => {
    const stores = {
      admin: useAdminStore,
      dashboard: useDashboardStore,
      userManagement: useUserManagementStore,
      permission: usePermissionStore,
    };

    const targetStores = storeName ? [storeName] : Object.keys(stores);

    const unsubscribers = targetStores.map(name => {
      const store = stores[name as keyof typeof stores];
      return store.subscribe(
        (state: any) => state,
        (newState: any, previousState: any) => {
          console.log(`Store "${name}" changed:`, {
            previous: previousState,
            current: newState,
          });
        }
      );
    });

    return () => unsubscribers.forEach(unsub => unsub());
  },

  // Validate store state integrity
  validateStoreIntegrity: () => {
    const issues: string[] = [];

    // Check admin store
    const adminState = useAdminStore.getState();
    if (adminState.user && adminState.permissions.length === 0) {
      issues.push('Admin store: User exists but no permissions set');
    }

    // Check user management store
    const userMgmtState = useUserManagementStore.getState();
    if (userMgmtState.selectedUsers.length > userMgmtState.users.length) {
      issues.push('User management store: More selected users than total users');
    }

    // Check permission store
    const permissionState = usePermissionStore.getState();
    if (permissionState.roles.length > 0 && permissionState.permissions.length === 0) {
      issues.push('Permission store: Roles exist but no permissions defined');
    }

    if (issues.length > 0) {
      console.warn('Store integrity issues found:', issues);
    } else {
      console.log('All stores are in valid state');
    }

    return issues;
  },
} : {};

// Export store instances for direct access (use sparingly)
export const stores = {
  admin: useAdminStore,
  dashboard: useDashboardStore,
  userManagement: useUserManagementStore,
  permission: usePermissionStore,
  realTime: useRealTimeStore,
};