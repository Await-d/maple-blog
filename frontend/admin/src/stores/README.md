# Admin Dashboard State Management

This directory contains the comprehensive state management system for the admin dashboard, built with **Zustand** and following enterprise-level patterns.

## Architecture Overview

The state management system is organized into specialized stores, each handling specific domain concerns:

- **`adminStore.ts`** - Main admin store for global UI state, user info, permissions, notifications
- **`dashboardStore.ts`** - Dashboard-specific state for metrics, charts, and real-time data
- **`userManagementStore.ts`** - User management operations, pagination, and bulk actions
- **`permissionStore.ts`** - Permission and role management state
- **`realTimeStore.ts`** - WebSocket connections and real-time data streams
- **`index.ts`** - Centralized exports and cross-store utilities

## Key Features

### 🏗️ Module-Specific Architecture
Each store handles a specific domain with clear boundaries and responsibilities.

### 💾 State Persistence
Important state is automatically persisted to localStorage using Zustand's persist middleware.

### 🔄 Real-Time Updates
Built-in WebSocket integration for live data updates and notifications.

### 🎯 Selective Subscriptions
Optimized selectors prevent unnecessary re-renders using shallow equality checks.

### 🛠️ Development Tools
Comprehensive debugging utilities and DevTools integration.

### 📊 Performance Monitoring
Built-in performance metrics and monitoring capabilities.

## Usage Examples

### Basic Store Usage

```typescript
import { useUser, useNotifications, useAdminStore } from '@/stores';

function Header() {
  const user = useUser();
  const notifications = useNotifications();
  const { addNotification, setPageTitle } = useAdminStore();

  const handleNotify = () => {
    addNotification({
      type: 'success',
      title: 'Operation completed',
      description: 'Data has been saved successfully',
    });
  };

  return (
    <div>
      <h1>Welcome, {user?.displayName}</h1>
      <Badge count={notifications.length} />
    </div>
  );
}
```

### Dashboard State Management

```typescript
import {
  useDashboardStore,
  useStats,
  useRealTimeData
} from '@/stores';

function DashboardOverview() {
  const stats = useStats();
  const realTimeData = useRealTimeData();
  const { startRefresh, setAutoRefresh } = useDashboardStore();

  useEffect(() => {
    setAutoRefresh(true);
    startRefresh();
  }, []);

  return (
    <div>
      <StatsCard data={stats} />
      <RealTimeMetrics data={realTimeData} />
    </div>
  );
}
```

### User Management with Bulk Operations

```typescript
import {
  useUsers,
  useSelectedUsers,
  useUserManagementStore,
  useBulkOperationProgress
} from '@/stores';

function UserManagement() {
  const users = useUsers();
  const selectedUsers = useSelectedUsers();
  const progress = useBulkOperationProgress();
  const {
    selectAllUsers,
    clearSelection,
    startBulkOperation
  } = useUserManagementStore();

  const handleBulkAction = async () => {
    startBulkOperation(selectedUsers.length);
    // Perform bulk operation...
  };

  return (
    <div>
      <DataTable
        data={users}
        selectedRows={selectedUsers}
        onSelectAll={selectAllUsers}
      />
      {progress.isRunning && (
        <BulkProgress progress={progress} />
      )}
    </div>
  );
}
```

### Permission-Based UI

```typescript
import { useHasPermission, useFeatureEnabled } from '@/stores';

function AdminPanel() {
  const canManageUsers = useHasPermission('users.manage');
  const analyticsEnabled = useFeatureEnabled('advancedAnalytics');

  return (
    <div>
      {canManageUsers && <UserManagementPanel />}
      {analyticsEnabled && <AdvancedAnalytics />}
    </div>
  );
}
```

### Real-Time Data Subscription

```typescript
import { useRealTimeChannel, useRealTimeEvent } from '@/stores';

function LiveDashboard() {
  const userId = 'current-user-id';

  // Subscribe to real-time channel
  useRealTimeChannel('dashboard', userId);

  // Handle real-time events
  useRealTimeEvent('metrics_update', (data) => {
    console.log('New metrics:', data);
  });

  return <Dashboard />;
}
```

## Store Configuration

### Persistence Configuration

Stores are configured with selective persistence:

```typescript
{
  name: 'store-name',
  partialize: (state) => ({
    // Only persist specific state slices
    theme: state.theme,
    settings: state.settings,
  }),
}
```

### Development Tools

In development mode, stores include:

- **Redux DevTools** integration
- **State change logging**
- **Performance monitoring**
- **Integrity validation**

## Performance Optimizations

### Selective Subscriptions

```typescript
// ✅ Good - Subscribe to specific state slice
const userName = useAdminStore(state => state.user?.name);

// ❌ Avoid - Subscribe to entire state
const state = useAdminStore();
```

### Computed Selectors

```typescript
// Pre-computed selectors for expensive operations
export const useUserStats = () => {
  return useUserManagementStore((state) => {
    // Expensive computation cached by Zustand
    return computeUserStatistics(state.users);
  });
};
```

### Immer Integration

All stores use Immer for immutable updates:

```typescript
set((state) => {
  // Direct mutation - Immer handles immutability
  state.users.push(newUser);
  state.pagination.total += 1;
});
```

## Error Handling

### State Validation

```typescript
// Built-in state integrity checks
const issues = storeDebug.validateStoreIntegrity();
if (issues.length > 0) {
  console.warn('State integrity issues:', issues);
}
```

### Error Boundaries

```typescript
// Stores handle errors gracefully
set((state) => {
  state.error = 'Operation failed';
  state.loading = false;
});
```

## Testing

### Mock Store State

```typescript
import { stores } from '@/stores';

// Mock store state for testing
beforeEach(() => {
  stores.admin.setState({
    user: mockUser,
    permissions: mockPermissions,
  });
});
```

### Test Selectors

```typescript
import { renderHook } from '@testing-library/react';
import { useHasPermission } from '@/stores';

test('permission check works correctly', () => {
  const { result } = renderHook(() =>
    useHasPermission('users.manage')
  );

  expect(result.current).toBe(true);
});
```

## Migration and Updates

### Store Version Management

When store structure changes:

1. Update the store interface
2. Provide migration logic in persist config
3. Test with existing persisted data

### Breaking Changes

For breaking changes:

1. Increment store version
2. Clear persisted state if needed
3. Provide migration path

## Best Practices

### ✅ Do's

- Use specific selectors to prevent unnecessary re-renders
- Leverage computed selectors for expensive operations
- Keep store actions simple and focused
- Use TypeScript for type safety
- Test store logic independently

### ❌ Don'ts

- Subscribe to entire store state unnecessarily
- Perform side effects in selectors
- Store derived state that can be computed
- Mutate state directly (outside Immer)
- Mix domain concerns in a single store

## Debugging

### Development Console

```typescript
// Log all store states
storeDebug.logAllStores();

// Monitor specific store changes
const unsubscribe = storeDebug.monitorStoreChanges('admin');

// Validate store integrity
storeDebug.validateStoreIntegrity();
```

### Performance Monitoring

```typescript
// Measure store operation performance
const perf = storeUtils.measureStorePerformance();
// ... perform operations
perf.end('bulk_user_update');
```

## Contributing

When adding new stores or features:

1. Follow the established patterns
2. Add appropriate TypeScript types
3. Include selectors and computed values
4. Add debugging utilities
5. Update documentation
6. Write tests

## Related Documentation

- [Zustand Documentation](https://github.com/pmndrs/zustand)
- [Immer Documentation](https://immerjs.github.io/immer/)
- [React Performance Patterns](https://kentcdodds.com/blog/optimize-react-re-renders)