import '@testing-library/jest-dom';
import { vi } from 'vitest';

// Mock ResizeObserver
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}));

// Mock IntersectionObserver
global.IntersectionObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}));

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // deprecated
    removeListener: vi.fn(), // deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Mock getComputedStyle
Object.defineProperty(window, 'getComputedStyle', {
  value: () => ({
    getPropertyValue: () => '',
  }),
});

// Mock scrollTo
Object.defineProperty(window, 'scrollTo', {
  value: vi.fn(),
});

// Mock fullscreen API
Object.defineProperty(document, 'fullscreenElement', {
  value: null,
  writable: true,
});

Object.defineProperty(document, 'exitFullscreen', {
  value: vi.fn(),
  writable: true,
});

Object.defineProperty(document.documentElement, 'requestFullscreen', {
  value: vi.fn(),
  writable: true,
});

// Mock console methods in test environment
global.console = {
  ...console,
  // Suppress console.warn and console.error in tests unless needed
  warn: vi.fn(),
  error: vi.fn(),
};

// Mock antd message
vi.mock('antd', async () => {
  const actual = await vi.importActual('antd');
  return {
    ...actual,
    message: {
      success: vi.fn(),
      error: vi.fn(),
      warning: vi.fn(),
      info: vi.fn(),
      loading: vi.fn(),
    },
  };
});

// Mock zustand stores
vi.mock('../stores/adminStore', () => ({
  useAdminStore: () => ({
    user: {
      id: '1',
      username: 'admin@test.com',
      role: 'admin',
      permissions: ['read:all', 'write:all'],
    },
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
    updateUser: vi.fn(),
  }),
}));

vi.mock('../stores/dashboardStore', () => ({
  useDashboardStore: () => ({
    stats: {
      userStats: { total: 100, active: 80, newToday: 5 },
      contentStats: { publishedPosts: 50, drafts: 10, postsToday: 2 },
      systemStats: { viewsTotal: 10000, viewsToday: 500, performanceScore: 95 },
    },
    metrics: {
      cpu: { usage: 60, cores: 8 },
      memory: { usage: 70, used: 4000000000, total: 8000000000 },
      disk: { usage: 50, used: 250000000000, total: 500000000000 },
      application: { uptime: 86400000, requestCount: 10000, errorCount: 5, responseTime: 100 },
    },
    isLoading: false,
    refreshData: vi.fn(),
    exportData: vi.fn(),
  }),
}));

// Setup fake timers for testing
beforeEach(() => {
  vi.useFakeTimers();
});

afterEach(() => {
  vi.runOnlyPendingTimers();
  vi.useRealTimers();
  vi.clearAllMocks();
});