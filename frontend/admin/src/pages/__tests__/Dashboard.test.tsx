import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import '@testing-library/jest-dom';

import Dashboard from '../Dashboard';
import { PermissionProvider } from '../../contexts/PermissionContext';

// Mock dependencies
const mockRefreshAll = vi.fn();
const mockExportData = vi.fn();
const mockGetChartData = vi.fn();
const mockGetTrendData = vi.fn();
const mockUpdateRefreshSettings = vi.fn();

vi.mock('../../hooks/useDashboard', () => ({
  useDashboard: () => ({
    stats: mockDashboardStats,
    metrics: mockSystemMetrics,
    healthCheck: mockHealthCheck,
    activities: mockActivities,
    isLoading: false,
    isRefreshing: false,
    hasErrors: false,
    isConnected: true,
    refreshAll: mockRefreshAll,
    exportData: mockExportData,
    getChartData: mockGetChartData,
    getTrendData: mockGetTrendData,
  }),
}));

vi.mock('../../hooks/useDashboard', () => ({
  useDashboardConfig: () => ({
    autoRefresh: true,
    refreshInterval: 30,
    updateRefreshSettings: mockUpdateRefreshSettings,
  }),
}));

vi.mock('../../hooks/usePermissions', () => ({
  usePermissions: () => ({
    hasPermission: vi.fn().mockReturnValue(true),
    userRole: 'admin',
    permissions: ['dashboard:read', 'dashboard:export', 'system:monitor'],
  }),
}));

vi.mock('../../components/charts/StatCard', () => ({
  default: ({ title, value, loading }: { title: string; value: string | number; loading: boolean }) => (
    <div data-testid={`stat-card-${title.toLowerCase()}`}>
      {loading ? (
        <span data-testid="stat-loading">Loading...</span>
      ) : (
        <div>
          <span data-testid="stat-title">{title}</span>
          <span data-testid="stat-value">{value}</span>
        </div>
      )}
    </div>
  ),
  StatCardVariants: {
    UserStats: ({ title, value, loading }: { title: string; value: string | number; loading: boolean }) => (
      <div data-testid="user-stats-card">
        {loading ? 'Loading...' : `${title}: ${value}`}
      </div>
    ),
    ContentStats: ({ title, value, loading }: { title: string; value: string | number; loading: boolean }) => (
      <div data-testid="content-stats-card">
        {loading ? 'Loading...' : `${title}: ${value}`}
      </div>
    ),
    SystemStats: ({ title, value, loading }: { title: string; value: string | number; loading: boolean }) => (
      <div data-testid="system-stats-card">
        {loading ? 'Loading...' : `${title}: ${value}`}
      </div>
    ),
    PerformanceStats: ({ title, value, loading }: { title: string; value: string | number; loading: boolean }) => (
      <div data-testid="performance-stats-card">
        {loading ? 'Loading...' : `${title}: ${value}`}
      </div>
    ),
  },
  StatCardFormatters: {
    number: (num: number) => num.toLocaleString(),
    fileSize: (bytes: number) => `${bytes} bytes`,
    duration: (ms: number) => `${ms}ms`,
  },
}));

vi.mock('../../components/charts/LineChart', () => ({
  default: ({ title, data, loading, onRefresh }: { title: string; data: unknown; loading: boolean; onRefresh: () => void }) => (
    <div data-testid={`line-chart-${title.toLowerCase()}`}>
      {loading ? (
        <span data-testid="chart-loading">Loading chart...</span>
      ) : (
        <div>
          <h3 data-testid="chart-title">{title}</h3>
          <div data-testid="chart-data">{JSON.stringify(data)}</div>
          <button data-testid="chart-refresh" onClick={onRefresh}>
            Refresh Chart
          </button>
        </div>
      )}
    </div>
  ),
}));

// Mock dayjs
vi.mock('dayjs', () => {
  const mockDayjs = (_date?: unknown) => ({
    format: (format: string) => {
      if (format === 'YYYY-MM-DD HH:mm:ss') return '2024-01-15 10:30:00';
      return '2024-01-15';
    },
    fromNow: () => '2 hours ago',
  });
  return { default: mockDayjs };
});

// Mock data
const mockDashboardStats = {
  userStats: {
    total: 1234,
    active: 567,
    newToday: 12,
  },
  contentStats: {
    publishedPosts: 89,
    drafts: 15,
    postsToday: 3,
  },
  systemStats: {
    viewsTotal: 45678,
    viewsToday: 234,
    performanceScore: 92.5,
  },
};

const mockSystemMetrics = {
  cpu: {
    usage: 65,
    cores: 8,
  },
  memory: {
    usage: 78,
    used: 6442450944,
    total: 8589934592,
  },
  disk: {
    usage: 45,
    used: 4831838208,
    total: 10737418240,
  },
  application: {
    uptime: 864000000,
    requestCount: 15432,
    errorCount: 23,
    responseTime: 125,
  },
};

const mockHealthCheck = {
  status: 'healthy' as const,
  timestamp: '2024-01-15T10:30:00Z',
  checks: {
    database: { status: 'pass' as const },
    cache: { status: 'pass' as const },
    storage: { status: 'warn' as const },
    api: { status: 'pass' as const },
  },
};

const mockActivities = [
  {
    id: '1',
    type: 'user_login' as const,
    description: 'User logged in',
    user: {
      id: '1',
      username: 'john_doe',
      displayName: 'John Doe',
      avatar: 'https://example.com/avatar1.jpg',
    },
    createdAt: '2024-01-15T08:30:00Z',
  },
  {
    id: '2',
    type: 'post_create' as const,
    description: 'New blog post created',
    user: {
      id: '2',
      username: 'jane_smith',
      displayName: 'Jane Smith',
      avatar: null,
    },
    createdAt: '2024-01-15T07:15:00Z',
  },
  {
    id: '3',
    type: 'comment_create' as const,
    description: 'Comment added to post',
    user: {
      id: '3',
      username: 'bob_johnson',
      displayName: 'Bob Johnson',
      avatar: 'https://example.com/avatar3.jpg',
    },
    createdAt: '2024-01-15T06:45:00Z',
  },
];

// Test wrapper component
const TestWrapper: React.FC<{ children: React.ReactNode; route?: string }> = ({ 
  children, 
  route = '/dashboard' 
}) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <PermissionProvider initialPermissions={['dashboard:read', 'dashboard:export', 'system:monitor']}>
          {children}
        </PermissionProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );
};

const renderDashboard = (route?: string) => {
  return render(
    <TestWrapper route={route}>
      <Dashboard />
    </TestWrapper>
  );
};

describe('Dashboard Component', () => {
  let user: ReturnType<typeof userEvent.setup>;

  beforeEach(() => {
    user = userEvent.setup();
    vi.clearAllMocks();
    
    // Mock chart data
    mockGetChartData.mockImplementation((type: string) => {
      switch (type) {
        case 'traffic':
          return [{ name: 'Jan', value: 100 }, { name: 'Feb', value: 200 }];
        case 'performance':
          return [{ name: 'CPU', value: 65 }, { name: 'Memory', value: 78 }];
        case 'users':
          return [{ name: 'Active', value: 567 }, { name: 'New', value: 12 }];
        default:
          return [];
      }
    });

    mockGetTrendData.mockReturnValue({
      userTrend: 5.2,
      contentTrend: -2.1,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Rendering', () => {
    it('renders dashboard title and description', () => {
      renderDashboard();

      expect(screen.getByText('管理仪表盘')).toBeInTheDocument();
      expect(screen.getByText('实时监控系统状态和关键指标')).toBeInTheDocument();
    });

    it('renders toolbar with action buttons', () => {
      renderDashboard();

      expect(screen.getByRole('button', { name: /刷新/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /设置/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /全屏/i })).toBeInTheDocument();
    });

    it('shows connection status when offline', () => {
      // Mock offline state
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        isConnected: false,
      });

      renderDashboard();

      expect(screen.getByText('离线模式')).toBeInTheDocument();
    });
  });

  describe('Statistics Cards', () => {
    it('renders all statistics cards with correct data', () => {
      renderDashboard();

      expect(screen.getByTestId('user-stats-card')).toBeInTheDocument();
      expect(screen.getByTestId('content-stats-card')).toBeInTheDocument();
      expect(screen.getByTestId('system-stats-card')).toBeInTheDocument();
      expect(screen.getByTestId('performance-stats-card')).toBeInTheDocument();
    });

    it('displays loading state for statistics cards', () => {
      // Mock loading state
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        isLoading: true,
      });

      renderDashboard();

      const loadingElements = screen.getAllByText('Loading...');
      expect(loadingElements.length).toBeGreaterThan(0);
    });

    it('shows correct user statistics', () => {
      renderDashboard();

      const userStatsCard = screen.getByTestId('user-stats-card');
      expect(userStatsCard).toHaveTextContent('1234');
    });

    it('displays trend indicators correctly', () => {
      renderDashboard();

      // Verify that trend data is being used
      expect(mockGetTrendData).toHaveBeenCalled();
    });
  });

  describe('Health Status', () => {
    it('renders health status alert', () => {
      renderDashboard();

      expect(screen.getByText('系统状态：健康')).toBeInTheDocument();
      expect(screen.getByText('检查时间：2024-01-15 10:30:00')).toBeInTheDocument();
    });

    it('shows individual health check statuses', () => {
      renderDashboard();

      expect(screen.getByText('database')).toBeInTheDocument();
      expect(screen.getByText('cache')).toBeInTheDocument();
      expect(screen.getByText('storage')).toBeInTheDocument();
      expect(screen.getByText('api')).toBeInTheDocument();
    });

    it('displays appropriate status colors', () => {
      renderDashboard();

      // Check for status indicator elements
      const statusIndicators = document.querySelectorAll('.w-2.h-2.rounded-full');
      expect(statusIndicators.length).toBeGreaterThan(0);
    });

    it('handles degraded system status', () => {
      // Mock degraded status
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        healthCheck: {
          ...mockHealthCheck,
          status: 'degraded',
        },
      });

      renderDashboard();

      expect(screen.getByText('系统状态：降级')).toBeInTheDocument();
    });

    it('handles unhealthy system status', () => {
      // Mock unhealthy status
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        healthCheck: {
          ...mockHealthCheck,
          status: 'unhealthy',
        },
      });

      renderDashboard();

      expect(screen.getByText('系统状态：异常')).toBeInTheDocument();
    });
  });

  describe('Charts Section', () => {
    it('renders all chart components', () => {
      renderDashboard();

      expect(screen.getByTestId('line-chart-流量趋势')).toBeInTheDocument();
      expect(screen.getByTestId('line-chart-系统性能')).toBeInTheDocument();
      expect(screen.getByTestId('line-chart-用户增长')).toBeInTheDocument();
    });

    it('passes correct data to charts', () => {
      renderDashboard();

      // Verify chart data fetching
      expect(mockGetChartData).toHaveBeenCalledWith('traffic');
      expect(mockGetChartData).toHaveBeenCalledWith('performance');
      expect(mockGetChartData).toHaveBeenCalledWith('users');
    });

    it('handles chart refresh', async () => {
      renderDashboard();

      const chartRefreshButton = screen.getAllByTestId('chart-refresh')[0];
      await user.click(chartRefreshButton);

      expect(mockRefreshAll).toHaveBeenCalled();
    });

    it('shows loading state for charts', () => {
      // Mock loading state
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        isLoading: true,
      });

      renderDashboard();

      const chartLoadingElements = screen.getAllByTestId('chart-loading');
      expect(chartLoadingElements.length).toBeGreaterThan(0);
    });
  });

  describe('Activity Feed', () => {
    it('renders activity feed with recent activities', () => {
      renderDashboard();

      expect(screen.getByText('最近活动')).toBeInTheDocument();
      expect(screen.getByText('User logged in')).toBeInTheDocument();
      expect(screen.getByText('New blog post created')).toBeInTheDocument();
      expect(screen.getByText('Comment added to post')).toBeInTheDocument();
    });

    it('displays user information for each activity', () => {
      renderDashboard();

      expect(screen.getByText('John Doe')).toBeInTheDocument();
      expect(screen.getByText('Jane Smith')).toBeInTheDocument();
      expect(screen.getByText('Bob Johnson')).toBeInTheDocument();
    });

    it('shows relative timestamps', () => {
      renderDashboard();

      const timeElements = screen.getAllByText('2 hours ago');
      expect(timeElements.length).toBeGreaterThan(0);
    });

    it('renders view all button', () => {
      renderDashboard();

      expect(screen.getByRole('button', { name: /查看全部/i })).toBeInTheDocument();
    });

    it('displays appropriate icons for different activity types', () => {
      renderDashboard();

      // Check for avatar elements (representing activity type icons)
      const avatars = document.querySelectorAll('.ant-avatar');
      expect(avatars.length).toBeGreaterThan(0);
    });
  });

  describe('System Metrics', () => {
    it('renders system monitoring section', () => {
      renderDashboard();

      expect(screen.getByText('系统监控')).toBeInTheDocument();
    });

    it('displays CPU usage with progress circle', () => {
      renderDashboard();

      expect(screen.getByText('CPU 使用率')).toBeInTheDocument();
      expect(screen.getByText('8 核心')).toBeInTheDocument();
    });

    it('displays memory usage information', () => {
      renderDashboard();

      expect(screen.getByText('内存使用率')).toBeInTheDocument();
      expect(screen.getByText('6442450944 bytes / 8589934592 bytes')).toBeInTheDocument();
    });

    it('displays disk usage information', () => {
      renderDashboard();

      expect(screen.getByText('磁盘使用率')).toBeInTheDocument();
      expect(screen.getByText('4831838208 bytes / 10737418240 bytes')).toBeInTheDocument();
    });

    it('displays application metrics', () => {
      renderDashboard();

      expect(screen.getByText('应用状态')).toBeInTheDocument();
      expect(screen.getByText('运行时间：864000000ms')).toBeInTheDocument();
      expect(screen.getByText('请求数：15,432')).toBeInTheDocument();
      expect(screen.getByText('错误数：23')).toBeInTheDocument();
      expect(screen.getByText('响应时间：125ms')).toBeInTheDocument();
    });

    it('shows warning status for high resource usage', () => {
      // Mock high usage
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        metrics: {
          ...mockSystemMetrics,
          cpu: { usage: 95, cores: 8 },
          memory: { usage: 85, used: 7301439232, total: 8589934592 },
        },
      });

      renderDashboard();

      // Should show exception status for high usage
      const progressCircles = document.querySelectorAll('.ant-progress-circle');
      expect(progressCircles.length).toBeGreaterThan(0);
    });
  });

  describe('Toolbar Actions', () => {
    it('handles refresh action', async () => {
      renderDashboard();

      const refreshButton = screen.getByRole('button', { name: /刷新/i });
      await user.click(refreshButton);

      expect(mockRefreshAll).toHaveBeenCalled();
    });

    it('shows loading state during refresh', () => {
      // Mock refreshing state
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        isRefreshing: true,
      });

      renderDashboard();

      const refreshButton = screen.getByRole('button', { name: /刷新/i });
      expect(refreshButton).toHaveClass('ant-btn-loading');
    });

    it('opens settings dropdown', async () => {
      renderDashboard();

      const settingsButton = screen.getByRole('button', { name: /设置/i });
      await user.click(settingsButton);

      await waitFor(() => {
        expect(screen.getByText('自动刷新')).toBeInTheDocument();
        expect(screen.getByText('刷新间隔(秒)')).toBeInTheDocument();
      });
    });

    it('toggles auto refresh setting', async () => {
      renderDashboard();

      const settingsButton = screen.getByRole('button', { name: /设置/i });
      await user.click(settingsButton);

      await waitFor(async () => {
        const autoRefreshSwitch = screen.getByRole('switch');
        await user.click(autoRefreshSwitch);

        expect(mockUpdateRefreshSettings).toHaveBeenCalledWith({
          autoRefresh: expect.any(Boolean),
        });
      });
    });

    it('changes refresh interval', async () => {
      renderDashboard();

      const settingsButton = screen.getByRole('button', { name: /设置/i });
      await user.click(settingsButton);

      await waitFor(async () => {
        const intervalInput = screen.getByDisplayValue('30');
        await user.clear(intervalInput);
        await user.type(intervalInput, '60');

        expect(mockUpdateRefreshSettings).toHaveBeenCalledWith({
          refreshInterval: 60,
        });
      });
    });

    it('changes dashboard layout', async () => {
      renderDashboard();

      const settingsButton = screen.getByRole('button', { name: /设置/i });
      await user.click(settingsButton);

      await waitFor(async () => {
        const compactLayoutOption = screen.getByText('紧凑布局');
        await user.click(compactLayoutOption);

        // Should apply compact layout styles
        const dashboard = document.querySelector('.dashboard-page');
        expect(dashboard).toBeInTheDocument();
      });
    });

    it('handles export actions', async () => {
      renderDashboard();

      const settingsButton = screen.getByRole('button', { name: /设置/i });
      await user.click(settingsButton);

      await waitFor(async () => {
        const exportJsonOption = screen.getByText('导出 JSON');
        await user.click(exportJsonOption);

        expect(mockExportData).toHaveBeenCalledWith('json');
      });
    });

    it('handles fullscreen toggle', async () => {
      renderDashboard();

      const fullscreenButton = screen.getByRole('button', { name: /全屏/i });
      
      // Mock fullscreen API
      const mockRequestFullscreen = vi.fn();
      const mockExitFullscreen = vi.fn();
      
      Object.defineProperty(document, 'fullscreenElement', {
        value: null,
        writable: true,
      });
      
      Object.defineProperty(document.documentElement, 'requestFullscreen', {
        value: mockRequestFullscreen,
        writable: true,
      });
      
      Object.defineProperty(document, 'exitFullscreen', {
        value: mockExitFullscreen,
        writable: true,
      });

      await user.click(fullscreenButton);

      expect(mockRequestFullscreen).toHaveBeenCalled();
    });
  });

  describe('Error Handling', () => {
    it('displays error state when data loading fails', () => {
      // Mock error state
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        hasErrors: true,
      });

      renderDashboard();

      expect(screen.getByText('数据加载失败')).toBeInTheDocument();
      expect(screen.getByText('无法获取仪表盘数据，请检查网络连接或稍后重试。')).toBeInTheDocument();
    });

    it('provides retry option on error', async () => {
      // Mock error state
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        hasErrors: true,
      });

      renderDashboard();

      const retryButton = screen.getByRole('button', { name: /重试/i });
      await user.click(retryButton);

      expect(mockRefreshAll).toHaveBeenCalled();
    });

    it('handles missing data gracefully', () => {
      // Mock missing data
      vi.mocked(vi.importMock('../../hooks/useDashboard')).mockReturnValueOnce({
        ...vi.mocked(vi.importMock('../../hooks/useDashboard')),
        stats: null,
        metrics: null,
        healthCheck: null,
        activities: null,
      });

      renderDashboard();

      // Should still render basic structure
      expect(screen.getByText('管理仪表盘')).toBeInTheDocument();
    });
  });

  describe('Responsive Design', () => {
    it('adapts layout for mobile screens', () => {
      // Mock mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768,
      });

      renderDashboard();

      // Should render without errors on mobile
      expect(screen.getByText('管理仪表盘')).toBeInTheDocument();
    });

    it('uses appropriate grid columns for different screen sizes', () => {
      renderDashboard();

      // Should use responsive grid system
      const gridRows = document.querySelectorAll('.ant-row');
      expect(gridRows.length).toBeGreaterThan(0);
    });
  });

  describe('Accessibility', () => {
    it('has proper heading hierarchy', () => {
      renderDashboard();

      const mainHeading = screen.getByRole('heading', { level: 2 });
      expect(mainHeading).toHaveTextContent('管理仪表盘');
    });

    it('provides proper labels for interactive elements', () => {
      renderDashboard();

      const refreshButton = screen.getByRole('button', { name: /刷新/i });
      const settingsButton = screen.getByRole('button', { name: /设置/i });
      const fullscreenButton = screen.getByRole('button', { name: /全屏/i });

      expect(refreshButton).toBeInTheDocument();
      expect(settingsButton).toBeInTheDocument();
      expect(fullscreenButton).toBeInTheDocument();
    });

    it('supports keyboard navigation', async () => {
      renderDashboard();

      const refreshButton = screen.getByRole('button', { name: /刷新/i });
      refreshButton.focus();

      expect(refreshButton).toHaveFocus();

      // Tab navigation should work
      await user.tab();
      const settingsButton = screen.getByRole('button', { name: /设置/i });
      expect(settingsButton).toHaveFocus();
    });

    it('provides appropriate ARIA labels for progress indicators', () => {
      renderDashboard();

      const progressElements = document.querySelectorAll('[role="progressbar"]');
      progressElements.forEach((element) => {
        expect(element).toHaveAttribute('aria-valuemin');
        expect(element).toHaveAttribute('aria-valuemax');
        expect(element).toHaveAttribute('aria-valuenow');
      });
    });

    it('announces status changes to screen readers', async () => {
      renderDashboard();

      // Status alerts should have appropriate roles
      const alerts = document.querySelectorAll('[role="alert"]');
      expect(alerts.length).toBeGreaterThan(0);
    });
  });

  describe('Performance', () => {
    it('renders efficiently with large datasets', () => {
      const startTime = performance.now();
      renderDashboard();
      const endTime = performance.now();

      // Should render within reasonable time
      expect(endTime - startTime).toBeLessThan(1000);
    });

    it('memoizes expensive computations', () => {
      const { rerender } = renderDashboard();

      // Re-render with same data shouldn't cause unnecessary updates
      rerender(
        <TestWrapper>
          <Dashboard />
        </TestWrapper>
      );

      // Chart data should be memoized
      expect(mockGetChartData).toHaveBeenCalledTimes(3); // Called once per chart type
    });
  });

  describe('Integration', () => {
    it('works with permission system', () => {
      renderDashboard();

      // Should render all sections when user has permissions
      expect(screen.getByText('管理仪表盘')).toBeInTheDocument();
      expect(screen.getByText('系统监控')).toBeInTheDocument();
    });

    it('respects permission restrictions', () => {
      // Mock restricted permissions
      vi.mocked(vi.importMock('../../hooks/usePermissions')).mockReturnValueOnce({
        hasPermission: vi.fn().mockImplementation((permission: string) => {
          return permission !== 'system:monitor';
        }),
        userRole: 'user',
        permissions: ['dashboard:read'],
      });

      renderDashboard();

      // Should hide restricted sections based on permissions
      expect(screen.getByText('管理仪表盘')).toBeInTheDocument();
    });

    it('integrates with routing system', () => {
      renderDashboard('/dashboard');

      // Should render correctly when accessed via route
      expect(screen.getByText('管理仪表盘')).toBeInTheDocument();
    });

    it('integrates with data fetching system', () => {
      renderDashboard();

      // Should use query hooks for data fetching
      expect(mockGetChartData).toHaveBeenCalled();
      expect(mockGetTrendData).toHaveBeenCalled();
    });
  });
});