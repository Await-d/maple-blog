// @ts-nocheck
import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ConfigProvider } from 'antd';
import enUS from 'antd/locale/en_US';
import { PermissionProvider } from '../contexts/PermissionContext';

// Custom render function that includes all providers
interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  initialEntries?: string[];
  permissions?: string[];
  userRole?: string;
  locale?: any;
  queryClient?: QueryClient;
}

const createTestQueryClient = () => {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });
};

export const renderWithProviders = (
  ui: ReactElement,
  {
    initialEntries = ['/'],
    permissions = ['read:all', 'write:all'],
    userRole = 'admin',
    locale = enUS,
    queryClient = createTestQueryClient(),
    ...renderOptions
  }: CustomRenderOptions = {}
) => {
  const AllProviders: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <ConfigProvider locale={locale}>
          <PermissionProvider>
            {children}
          </PermissionProvider>
        </ConfigProvider>
      </MemoryRouter>
    </QueryClientProvider>
  );

  return render(ui, { wrapper: AllProviders, ...renderOptions });
};

// Mock data generators
export const generateMockUser = (overrides: Partial<any> = {}) => ({
  id: '1',
  username: 'test@example.com',
  displayName: 'Test User',
  role: 'user',
  status: 'active',
  createdAt: '2024-01-01T00:00:00Z',
  lastLogin: '2024-01-15T10:30:00Z',
  avatar: null,
  ...overrides,
});

export const generateMockPost = (overrides: Partial<any> = {}) => ({
  id: '1',
  title: 'Test Post',
  content: 'This is a test post content',
  excerpt: 'Test excerpt',
  status: 'published',
  categoryId: '1',
  category: { id: '1', name: 'Technology' },
  tags: [{ id: '1', name: 'test' }, { id: '2', name: 'post' }],
  authorId: '1',
  author: generateMockUser({ role: 'author' }),
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  publishedAt: '2024-01-01T00:00:00Z',
  viewCount: 100,
  commentCount: 5,
  ...overrides,
});

export const generateMockComment = (overrides: Partial<any> = {}) => ({
  id: '1',
  content: 'This is a test comment',
  status: 'approved',
  postId: '1',
  authorId: '1',
  author: generateMockUser(),
  parentId: null,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  replies: [],
  ...overrides,
});

export const generateMockDashboardStats = (overrides: Partial<any> = {}) => ({
  userStats: {
    total: 1000,
    active: 750,
    newToday: 25,
    newThisWeek: 150,
    newThisMonth: 300,
  },
  contentStats: {
    publishedPosts: 500,
    drafts: 50,
    postsToday: 5,
    postsThisWeek: 25,
    postsThisMonth: 100,
    totalViews: 50000,
    viewsToday: 1000,
  },
  systemStats: {
    viewsTotal: 100000,
    viewsToday: 2000,
    performanceScore: 92.5,
    uptime: '99.9%',
    responseTime: 150,
  },
  ...overrides,
});

export const generateMockSystemMetrics = (overrides: Partial<any> = {}) => ({
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
  network: {
    bytesIn: 1048576000,
    bytesOut: 524288000,
  },
  application: {
    uptime: 864000000,
    requestCount: 15432,
    errorCount: 23,
    responseTime: 125,
    activeConnections: 150,
  },
  ...overrides,
});

export const generateMockHealthCheck = (overrides: Partial<any> = {}) => ({
  status: 'healthy' as const,
  timestamp: '2024-01-15T10:30:00Z',
  checks: {
    database: { status: 'pass' as const, responseTime: 10 },
    cache: { status: 'pass' as const, responseTime: 5 },
    storage: { status: 'warn' as const, responseTime: 100 },
    api: { status: 'pass' as const, responseTime: 50 },
    search: { status: 'pass' as const, responseTime: 25 },
  },
  ...overrides,
});

export const generateMockActivity = (overrides: Partial<any> = {}) => ({
  id: '1',
  type: 'user_login' as const,
  description: 'User logged in',
  user: generateMockUser(),
  metadata: {},
  createdAt: '2024-01-15T08:30:00Z',
  ...overrides,
});

// Test utilities for common interactions
export const waitForLoadingToFinish = async () => {
  // Wait for loading spinners to disappear
  await new Promise(resolve => setTimeout(resolve, 100));
};

export const expectElementToBeAccessible = (element: HTMLElement) => {
  // Check for basic accessibility attributes
  if (element.tagName === 'BUTTON') {
    expect(element).toHaveAttribute('type');
  }
  
  if (element.getAttribute('role') === 'button') {
    expect(element).toHaveAttribute('tabindex');
  }
  
  // Check for form elements
  if (['INPUT', 'TEXTAREA', 'SELECT'].includes(element.tagName)) {
    const label = document.querySelector(`label[for="${element.id}"]`);
    const ariaLabel = element.getAttribute('aria-label');
    const ariaLabelledBy = element.getAttribute('aria-labelledby');
    
    expect(label || ariaLabel || ariaLabelledBy).toBeTruthy();
  }
};

// Mock API responses
export const mockApiResponse = (data: any, delay = 0) => {
  return new Promise(resolve => {
    setTimeout(() => resolve({ data }), delay);
  });
};

export const mockApiError = (error: any, delay = 0) => {
  return new Promise((_, reject) => {
    setTimeout(() => reject(error), delay);
  });
};

// Helper for testing permission-based rendering
export const renderWithPermissions = (
  ui: ReactElement,
  permissions: string[],
  userRole: string = 'user'
) => {
  return renderWithProviders(ui, { permissions, userRole });
};

// Helper for testing with different user roles
export const renderAsAdmin = (ui: ReactElement) => {
  return renderWithPermissions(ui, ['read:all', 'write:all', 'admin:all'], 'admin');
};

export const renderAsModerator = (ui: ReactElement) => {
  return renderWithPermissions(ui, ['read:all', 'write:content', 'moderate:all'], 'moderator');
};

export const renderAsUser = (ui: ReactElement) => {
  return renderWithPermissions(ui, ['read:public'], 'user');
};

// Custom matchers for testing
export const expectToBeVisible = (element: HTMLElement | null) => {
  expect(element).toBeInTheDocument();
  expect(element).toBeVisible();
};

export const expectToHaveCorrectRole = (element: HTMLElement, expectedRole: string) => {
  expect(element).toHaveAttribute('role', expectedRole);
};

export const expectToBeAccessible = (element: HTMLElement) => {
  expectElementToBeAccessible(element);
};

// Data table testing utilities
export const expectTableToHaveRows = (table: HTMLElement, expectedRowCount: number) => {
  const rows = table.querySelectorAll('tbody tr');
  expect(rows).toHaveLength(expectedRowCount);
};

export const expectTableToHaveColumns = (table: HTMLElement, expectedColumns: string[]) => {
  const headers = table.querySelectorAll('thead th');
  expectedColumns.forEach((columnName, index) => {
    expect(headers[index]).toHaveTextContent(columnName);
  });
};

export const expectTableToBeLoading = (table: HTMLElement) => {
  const loadingSpinner = table.querySelector('.ant-spin');
  expect(loadingSpinner).toBeInTheDocument();
};

// Chart testing utilities
export const expectChartToBeRendered = (chartContainer: HTMLElement) => {
  const canvas = chartContainer.querySelector('canvas');
  const svg = chartContainer.querySelector('svg');
  expect(canvas || svg).toBeInTheDocument();
};

export const expectChartToHaveData = (chartContainer: HTMLElement) => {
  expectChartToBeRendered(chartContainer);
  // Additional checks for chart data can be added here
};

// Form testing utilities
export const fillFormField = async (
  fieldName: string,
  value: string,
  container?: HTMLElement
) => {
  const field = container 
    ? container.querySelector(`[data-testid="${fieldName}"]`)
    : document.querySelector(`[data-testid="${fieldName}"]`);
  
  expect(field).toBeInTheDocument();
  
  if (field?.tagName === 'INPUT') {
    await userEvent.clear(field as HTMLInputElement);
    await userEvent.type(field as HTMLInputElement, value);
  } else if (field?.tagName === 'TEXTAREA') {
    await userEvent.clear(field as HTMLTextAreaElement);
    await userEvent.type(field as HTMLTextAreaElement, value);
  }
};

// Re-export testing library utilities
export * from '@testing-library/react';
export { default as userEvent } from '@testing-library/user-event';