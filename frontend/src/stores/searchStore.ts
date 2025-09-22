// @ts-nocheck
import { create } from 'zustand';
import { persist, subscribeWithSelector } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import {
  SearchState,
  ArchiveState,
  SearchRequest,
  SearchResponse,
  SearchResult,
  SearchHistory,
  AutoCompleteSuggestion,
  SearchFilters,
  SortOption,
  TimelineArchive,
  CalendarArchive,
  CategoryTree,
  TagCloud,
  DEFAULT_PAGE_SIZE,
  DEFAULT_SORT_OPTION,
  DEFAULT_SEARCH_CONFIG,
} from '@/types/search';

// 搜索状态接口
interface SearchStore extends SearchState {
  // 搜索操作
  setQuery: (query: string) => void;
  setFilters: (filters: SearchFilters) => void;
  updateFilter: (key: keyof SearchFilters, value: any) => void;
  clearFilters: () => void;
  setSortBy: (sortBy: SortOption, direction?: 'asc' | 'desc') => void;
  setPage: (page: number) => void;

  // 搜索执行
  search: (request?: Partial<SearchRequest>) => Promise<SearchResponse | null>;
  loadMore: () => Promise<void>;
  retry: () => Promise<void>;

  // 自动完成和建议
  loadAutoComplete: (query: string) => Promise<void>;
  clearAutoComplete: () => void;

  // 搜索历史
  addToHistory: (query: string, resultCount: number, filters?: SearchFilters) => void;
  removeFromHistory: (id: string) => void;
  clearHistory: () => void;

  // UI状态
  toggleAdvanced: () => void;
  toggleHistory: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;

  // 重置
  reset: () => void;
  resetResults: () => void;
}

// 归档状态接口
interface ArchiveStore extends ArchiveState {
  // 数据加载
  loadTimelineArchive: (year?: number) => Promise<void>;
  loadCalendarArchive: (year: number) => Promise<void>;
  loadCategoryTree: () => Promise<void>;
  loadTagCloud: () => Promise<void>;

  // 视图切换
  setCurrentView: (view: 'timeline' | 'calendar' | 'category' | 'tag') => void;
  setSelectedYear: (year?: number) => void;
  setSelectedMonth: (month?: number) => void;
  setSelectedCategory: (category?: string) => void;
  setSelectedTag: (tag?: string) => void;

  // UI状态
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;

  // 重置
  reset: () => void;
}

// 初始搜索状态
const initialSearchState: SearchState = {
  query: '',
  filters: {},
  results: [],
  suggestions: [],
  autoComplete: [],
  history: [],

  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  totalCount: 0,
  hasMore: false,
  sortBy: DEFAULT_SORT_OPTION,
  sortDirection: 'desc',

  loading: false,
  error: null,
  showAdvanced: false,
  showHistory: false,

  searchTime: 0,
};

// 初始归档状态
const initialArchiveState: ArchiveState = {
  currentView: 'timeline',
  loading: false,
  error: null,
};

// 创建搜索状态管理
export const useSearchStore = create<SearchStore>()(
  subscribeWithSelector(
    persist(
      immer((set, get) => ({
        ...initialSearchState,

        // 基础设置
        setQuery: (query: string) => {
          set((state) => {
            state.query = query;
            if (query.length === 0) {
              state.results = [];
              state.suggestions = [];
              state.autoComplete = [];
              state.totalCount = 0;
              state.hasMore = false;
              state.page = 1;
            }
          });
        },

        setFilters: (filters: SearchFilters) => {
          set((state) => {
            state.filters = filters;
            state.page = 1; // 重置页码
          });
        },

        updateFilter: (key: keyof SearchFilters, value: any) => {
          set((state) => {
            if (value === undefined || value === null ||
                (Array.isArray(value) && value.length === 0)) {
              delete state.filters[key];
            } else {
              state.filters[key] = value;
            }
            state.page = 1; // 重置页码
          });
        },

        clearFilters: () => {
          set((state) => {
            state.filters = {};
            state.page = 1;
          });
        },

        setSortBy: (sortBy: SortOption, direction: 'asc' | 'desc' = 'desc') => {
          set((state) => {
            state.sortBy = sortBy;
            state.sortDirection = direction;
            state.page = 1; // 重置页码
          });
        },

        setPage: (page: number) => {
          set((state) => {
            state.page = page;
          });
        },

        // 搜索执行
        search: async (request?: Partial<SearchRequest>) => {
          const state = get();
          const searchRequest: SearchRequest = {
            query: state.query,
            filters: state.filters,
            page: state.page,
            pageSize: state.pageSize,
            sortBy: state.sortBy,
            sortDirection: state.sortDirection,
            ...request,
          };

          // 如果查询为空，直接返回
          if (!searchRequest.query?.trim()) {
            set((draft) => {
              draft.results = [];
              draft.suggestions = [];
              draft.totalCount = 0;
              draft.hasMore = false;
              draft.error = null;
            });
            return null;
          }

          set((draft) => {
            draft.loading = true;
            draft.error = null;
            if (searchRequest.page === 1) {
              draft.results = [];
            }
          });

          try {
            const startTime = performance.now();

            // 这里应该调用实际的API服务
            // 目前使用模拟数据
            const response = await mockSearchApi(searchRequest);

            const endTime = performance.now();
            const searchTime = endTime - startTime;

            set((draft) => {
              if (searchRequest.page === 1) {
                draft.results = response.results;
              } else {
                draft.results.push(...response.results);
              }

              draft.suggestions = response.suggestions || [];
              draft.totalCount = response.totalCount;
              draft.hasMore = response.hasMore;
              draft.searchTime = searchTime;
              draft.resultStats = {
                took: response.took,
                totalHits: response.totalCount,
                maxScore: Math.max(...response.results.map(r => r.score)),
              };
              draft.loading = false;
            });

            // 添加到搜索历史
            if (searchRequest.page === 1 && searchRequest.query) {
              get().addToHistory(
                searchRequest.query,
                response.totalCount,
                searchRequest.filters
              );
            }

            return response;
          } catch (error) {
            set((draft) => {
              draft.loading = false;
              draft.error = error instanceof Error ? error.message : '搜索失败';
            });
            return null;
          }
        },

        loadMore: async () => {
          const state = get();
          if (!state.hasMore || state.loading) return;

          await state.search({ page: state.page + 1 });
          set((draft) => {
            draft.page += 1;
          });
        },

        retry: async () => {
          const state = get();
          await state.search();
        },

        // 自动完成
        loadAutoComplete: async (query: string) => {
          if (query.length < DEFAULT_SEARCH_CONFIG.minQueryLength) {
            set((draft) => {
              draft.autoComplete = [];
            });
            return;
          }

          try {
            // 模拟API调用
            const suggestions = await mockAutoCompleteApi(query);

            set((draft) => {
              draft.autoComplete = suggestions;
            });
          } catch (error) {
            console.error('Auto-complete failed:', error);
            set((draft) => {
              draft.autoComplete = [];
            });
          }
        },

        clearAutoComplete: () => {
          set((draft) => {
            draft.autoComplete = [];
          });
        },

        // 搜索历史
        addToHistory: (query: string, resultCount: number, filters?: SearchFilters) => {
          set((draft) => {
            const existingIndex = draft.history.findIndex((h: any) => h.query === query);
            const historyItem: SearchHistory = {
              id: Date.now().toString(),
              query,
              timestamp: new Date().toISOString(),
              resultCount,
              filters,
            };

            if (existingIndex >= 0) {
              // 更新已存在的项目
              draft.history[existingIndex] = historyItem;
            } else {
              // 添加新项目
              draft.history.unshift(historyItem);
              // 限制历史记录数量
              if (draft.history.length > DEFAULT_SEARCH_CONFIG.maxHistoryItems) {
                draft.history = draft.history.slice(0, DEFAULT_SEARCH_CONFIG.maxHistoryItems);
              }
            }
          });
        },

        removeFromHistory: (id: string) => {
          set((draft) => {
            draft.history = draft.history.filter((h: any) => h.id !== id);
          });
        },

        clearHistory: () => {
          set((draft) => {
            draft.history = [];
          });
        },

        // UI状态
        toggleAdvanced: () => {
          set((draft) => {
            draft.showAdvanced = !draft.showAdvanced;
          });
        },

        toggleHistory: () => {
          set((draft) => {
            draft.showHistory = !draft.showHistory;
          });
        },

        setLoading: (loading: boolean) => {
          set((draft) => {
            draft.loading = loading;
          });
        },

        setError: (error: string | null) => {
          set((draft) => {
            draft.error = error;
          });
        },

        // 重置
        reset: () => {
          set((draft) => {
            Object.assign(draft, initialSearchState);
          });
        },

        resetResults: () => {
          set((draft) => {
            draft.results = [];
            draft.suggestions = [];
            draft.autoComplete = [];
            draft.totalCount = 0;
            draft.hasMore = false;
            draft.page = 1;
            draft.error = null;
          });
        },
      })),
      {
        name: 'maple-blog-search',
        partialize: (state) => ({
          history: state.history,
          filters: state.filters,
          sortBy: state.sortBy,
          sortDirection: state.sortDirection,
          pageSize: state.pageSize,
        }),
      }
    )
  )
);

// 创建归档状态管理
export const useArchiveStore = create<ArchiveStore>()(
  subscribeWithSelector(
    immer((set, get) => ({
      ...initialArchiveState,

      // 数据加载
      loadTimelineArchive: async (year?: number) => {
        set((draft) => {
          draft.loading = true;
          draft.error = null;
        });

        try {
          // 模拟API调用
          const timeline = await mockTimelineArchiveApi(year);

          set((draft) => {
            draft.timelineArchive = timeline;
            draft.loading = false;
          });
        } catch (error) {
          set((draft) => {
            draft.loading = false;
            draft.error = error instanceof Error ? error.message : '加载时间线归档失败';
          });
        }
      },

      loadCalendarArchive: async (year: number) => {
        set((draft) => {
          draft.loading = true;
          draft.error = null;
        });

        try {
          // 模拟API调用
          const calendar = await mockCalendarArchiveApi(year);

          set((draft) => {
            draft.calendarArchive = calendar;
            draft.loading = false;
          });
        } catch (error) {
          set((draft) => {
            draft.loading = false;
            draft.error = error instanceof Error ? error.message : '加载日历归档失败';
          });
        }
      },

      loadCategoryTree: async () => {
        set((draft) => {
          draft.loading = true;
          draft.error = null;
        });

        try {
          // 模拟API调用
          const categoryTree = await mockCategoryTreeApi();

          set((draft) => {
            draft.categoryTree = categoryTree;
            draft.loading = false;
          });
        } catch (error) {
          set((draft) => {
            draft.loading = false;
            draft.error = error instanceof Error ? error.message : '加载分类树失败';
          });
        }
      },

      loadTagCloud: async () => {
        set((draft) => {
          draft.loading = true;
          draft.error = null;
        });

        try {
          // 模拟API调用
          const tagCloud = await mockTagCloudApi();

          set((draft) => {
            draft.tagCloud = tagCloud;
            draft.loading = false;
          });
        } catch (error) {
          set((draft) => {
            draft.loading = false;
            draft.error = error instanceof Error ? error.message : '加载标签云失败';
          });
        }
      },

      // 视图切换
      setCurrentView: (view) => {
        set((draft) => {
          draft.currentView = view;
        });
      },

      setSelectedYear: (year) => {
        set((draft) => {
          draft.selectedYear = year;
          draft.selectedMonth = undefined; // 清除月份选择
        });
      },

      setSelectedMonth: (month) => {
        set((draft) => {
          draft.selectedMonth = month;
        });
      },

      setSelectedCategory: (category) => {
        set((draft) => {
          draft.selectedCategory = category;
        });
      },

      setSelectedTag: (tag) => {
        set((draft) => {
          draft.selectedTag = tag;
        });
      },

      // UI状态
      setLoading: (loading) => {
        set((draft) => {
          draft.loading = loading;
        });
      },

      setError: (error) => {
        set((draft) => {
          draft.error = error;
        });
      },

      // 重置
      reset: () => {
        set((draft) => {
          Object.assign(draft, initialArchiveState);
        });
      },
    }))
  )
);

// 模拟API函数（实际项目中应该替换为真实的API调用）
async function mockSearchApi(request: SearchRequest): Promise<SearchResponse> {
  // 模拟网络延迟
  await new Promise(resolve => setTimeout(resolve, 200));

  return {
    results: [],
    totalCount: 0,
    page: request.page || 1,
    pageSize: request.pageSize || DEFAULT_PAGE_SIZE,
    hasMore: false,
    took: 150,
    suggestions: [],
  };
}

async function mockAutoCompleteApi(query: string): Promise<AutoCompleteSuggestion[]> {
  await new Promise(resolve => setTimeout(resolve, 100));
  return [];
}

async function mockTimelineArchiveApi(year?: number): Promise<TimelineArchive> {
  await new Promise(resolve => setTimeout(resolve, 300));
  return {
    years: [],
    totalCount: 0,
    dateRange: { from: '', to: '' },
  };
}

async function mockCalendarArchiveApi(year: number): Promise<CalendarArchive> {
  await new Promise(resolve => setTimeout(resolve, 300));
  return {
    year,
    months: [],
    totalCount: 0,
  };
}

async function mockCategoryTreeApi(): Promise<CategoryTree> {
  await new Promise(resolve => setTimeout(resolve, 300));
  return {
    categories: [],
    totalCount: 0,
  };
}

async function mockTagCloudApi(): Promise<TagCloud> {
  await new Promise(resolve => setTimeout(resolve, 300));
  return {
    tags: [],
    maxCount: 0,
    minCount: 0,
  };
}

// 导出搜索相关的selector钩子
export const useSearchQuery = () => useSearchStore(state => state.query);
export const useSearchResults = () => useSearchStore(state => state.results);
export const useSearchLoading = () => useSearchStore(state => state.loading);
export const useSearchError = () => useSearchStore(state => state.error);
export const useSearchFilters = () => useSearchStore(state => state.filters);
export const useSearchHistory = () => useSearchStore(state => state.history);
export const useAutoComplete = () => useSearchStore(state => state.autoComplete);

// 导出归档相关的selector钩子
export const useArchiveCurrentView = () => useArchiveStore(state => state.currentView);
export const useArchiveLoading = () => useArchiveStore(state => state.loading);
export const useTimelineArchive = () => useArchiveStore(state => state.timelineArchive);
export const useCalendarArchive = () => useArchiveStore(state => state.calendarArchive);
export const useCategoryTree = () => useArchiveStore(state => state.categoryTree);
export const useTagCloud = () => useArchiveStore(state => state.tagCloud);