import { create } from 'zustand';
import { devtools, persist } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type {
  DashboardStats,
  Activity,
  SystemMetrics,
  HealthCheck,
  ChartConfig
} from '@/types';

export interface DashboardState {
  // Dashboard data
  stats: DashboardStats | null;
  metrics: SystemMetrics | null;
  healthCheck: HealthCheck | null;
  activities: Activity[];

  // Chart configurations
  chartConfigs: Record<string, ChartConfig>;

  // UI state
  refreshInterval: number; // in seconds
  autoRefresh: boolean;
  lastRefresh: string | null;
  isRefreshing: boolean;

  // Dashboard layout
  layout: {
    statsOrder: string[];
    chartsOrder: string[];
    widgetSizes: Record<string, { width: number; height: number }>;
  };

  // Real-time data
  realTimeData: {
    activeUsers: number;
    systemLoad: number;
    requestsPerMinute: number;
    errorRate: number;
  };

  // Actions
  setStats: (stats: DashboardStats) => void;
  setMetrics: (metrics: SystemMetrics) => void;
  setHealthCheck: (healthCheck: HealthCheck) => void;
  setActivities: (activities: Activity[]) => void;
  addActivity: (activity: Activity) => void;

  setChartConfig: (chartId: string, config: ChartConfig) => void;
  removeChartConfig: (chartId: string) => void;

  setRefreshInterval: (interval: number) => void;
  setAutoRefresh: (enabled: boolean) => void;
  startRefresh: () => void;
  endRefresh: () => void;

  updateLayout: (layout: Partial<DashboardState['layout']>) => void;
  updateRealTimeData: (data: Partial<DashboardState['realTimeData']>) => void;

  // Dashboard customization
  saveLayoutPreset: (name: string, layout: DashboardState['layout']) => void;
  loadLayoutPreset: (name: string) => void;

  reset: () => void;
}

const initialState = {
  stats: null,
  metrics: null,
  healthCheck: null,
  activities: [],

  chartConfigs: {},

  refreshInterval: 30,
  autoRefresh: true,
  lastRefresh: null,
  isRefreshing: false,

  layout: {
    statsOrder: ['users', 'content', 'system', 'performance'],
    chartsOrder: ['traffic', 'performance', 'users', 'content'],
    widgetSizes: {
      statsCard: { width: 280, height: 120 },
      chartWidget: { width: 400, height: 300 },
      activityFeed: { width: 350, height: 400 },
    },
  },

  realTimeData: {
    activeUsers: 0,
    systemLoad: 0,
    requestsPerMinute: 0,
    errorRate: 0,
  },
};

export const useDashboardStore = create<DashboardState>()(
  devtools(
    persist(
      immer((set, _get) => ({
        ...initialState,

        setStats: (stats) => {
          set((state) => {
            state.stats = stats;
            state.lastRefresh = new Date().toISOString();
          });
        },

        setMetrics: (metrics) => {
          set((state) => {
            state.metrics = metrics;
          });
        },

        setHealthCheck: (healthCheck) => {
          set((state) => {
            state.healthCheck = healthCheck;
          });
        },

        setActivities: (activities) => {
          set((state) => {
            state.activities = activities;
          });
        },

        addActivity: (activity) => {
          set((state) => {
            state.activities.unshift(activity);
            // Keep only the latest 50 activities
            if (state.activities.length > 50) {
              state.activities = state.activities.slice(0, 50);
            }
          });
        },

        setChartConfig: (chartId, config) => {
          set((state) => {
            state.chartConfigs[chartId] = config;
          });
        },

        removeChartConfig: (chartId) => {
          set((state) => {
            delete state.chartConfigs[chartId];
          });
        },

        setRefreshInterval: (interval) => {
          set((state) => {
            state.refreshInterval = interval;
          });
        },

        setAutoRefresh: (enabled) => {
          set((state) => {
            state.autoRefresh = enabled;
          });
        },

        startRefresh: () => {
          set((state) => {
            state.isRefreshing = true;
          });
        },

        endRefresh: () => {
          set((state) => {
            state.isRefreshing = false;
            state.lastRefresh = new Date().toISOString();
          });
        },

        updateLayout: (newLayout) => {
          set((state) => {
            state.layout = { ...state.layout, ...newLayout };
          });
        },

        updateRealTimeData: (data) => {
          set((state) => {
            state.realTimeData = { ...state.realTimeData, ...data };
          });
        },

        saveLayoutPreset: (name, layout) => {
          // This could be extended to save presets to backend
          console.log(`Saving layout preset: ${name}`, layout);
        },

        loadLayoutPreset: (name) => {
          // This could be extended to load presets from backend
          console.log(`Loading layout preset: ${name}`);
        },

        reset: () => {
          set(() => initialState);
        },
      })),
      {
        name: 'dashboard-store',
        partialize: (state) => ({
          refreshInterval: state.refreshInterval,
          autoRefresh: state.autoRefresh,
          layout: state.layout,
          chartConfigs: state.chartConfigs,
        }),
      }
    ),
    {
      name: 'dashboard-store',
      enabled: process.env.NODE_ENV === 'development',
    }
  )
);

// Selectors
export const useStats = () => useDashboardStore((state) => state.stats);
export const useMetrics = () => useDashboardStore((state) => state.metrics);
export const useHealthCheck = () => useDashboardStore((state) => state.healthCheck);
export const useActivities = () => useDashboardStore((state) => state.activities);
export const useChartConfigs = () => useDashboardStore((state) => state.chartConfigs);
export const useRefreshState = () => useDashboardStore((state) => ({
  interval: state.refreshInterval,
  autoRefresh: state.autoRefresh,
  isRefreshing: state.isRefreshing,
  lastRefresh: state.lastRefresh,
}));
export const useDashboardLayout = () => useDashboardStore((state) => state.layout);
export const useRealTimeData = () => useDashboardStore((state) => state.realTimeData);

// Computed selectors
export const useSystemStatus = () => {
  return useDashboardStore((state) => {
    if (!state.healthCheck) return 'unknown';

    const { status } = state.healthCheck;
    return status;
  });
};

export const usePerformanceScore = () => {
  return useDashboardStore((state) => {
    if (!state.stats) return 0;

    return state.stats.systemStats.performanceScore;
  });
};

export const useTrendData = () => {
  return useDashboardStore((state) => {
    if (!state.stats) return null;

    return {
      userTrend: state.stats.userStats.trend,
      contentTrend: state.stats.contentStats.trend,
    };
  });
};