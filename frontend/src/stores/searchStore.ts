import { create } from 'zustand';
import { persist, subscribeWithSelector } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import {
  SearchState,
  ArchiveState,
  SearchRequest,
  SearchResponse,
  SearchHistory,
  SearchFilters,
  SortOption,
  CategoryTreeNode,
  ContentType,
  PostStatus,
  DEFAULT_PAGE_SIZE,
  DEFAULT_SORT_OPTION,
  DEFAULT_SEARCH_CONFIG,
} from '@/types/search';
import { archiveApi } from '@/services/search/archiveApi';
import { searchApi } from '@/services/search/searchApi';

// 搜索状态接口
interface SearchStore extends SearchState {
  // 搜索操作
  setQuery: (_query: string) => void;
  setFilters: (filters: SearchFilters) => void;
  updateFilter: (key: keyof SearchFilters, value: unknown) => void;
  clearFilters: () => void;
  setSortBy: (sortBy: SortOption, direction?: 'asc' | 'desc') => void;
  setPage: (page: number) => void;

  // 搜索执行
  search: (request?: Partial<SearchRequest>) => Promise<SearchResponse | null>;
  loadMore: () => Promise<void>;
  retry: () => Promise<void>;

  // 自动完成和建议
  loadAutoComplete: (_query: string) => Promise<void>;
  clearAutoComplete: () => void;

  // 搜索历史
  addToHistory: (_query: string, resultCount: number, filters?: SearchFilters) => void;
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
  loadCalendarArchive: (_year: number) => Promise<void>;
  loadCategoryTree: () => Promise<void>;
  loadTagCloud: () => Promise<void>;
  loadCategoryPosts: (categorySlug: string, page?: number, pageSize?: number) => Promise<void>;

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
  categoryPosts: {},
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

        updateFilter: (key: keyof SearchFilters, value: unknown) => {
          set((state) => {
            if (value === undefined || value === null ||
                (Array.isArray(value) && value.length === 0)) {
              delete state.filters[key];
            } else {
              // Type-safe assignment for filter values
              switch (key) {
                case 'categories':
                case 'tags':
                case 'authors':
                  state.filters[key] = value as string[];
                  break;
                case 'dateFrom':
                case 'dateTo':
                  state.filters[key] = value as string;
                  break;
                case 'contentType':
                  state.filters[key] = value as ContentType[];
                  break;
                case 'status':
                  state.filters[key] = value as PostStatus[];
                  break;
                default:
                  (state.filters as Record<string, unknown>)[key] = value;
              }
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

            // 调用真实的搜索API服务
            const response = await searchApi.search(searchRequest);

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
            // 调用真实的自动完成API
            const suggestions = await searchApi.getAutoComplete(query);

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
            const existingIndex = draft.history.findIndex((h: SearchHistory) => h.query === query);
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
            draft.history = draft.history.filter((h: SearchHistory) => h.id !== id);
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
    immer((set, _get) => ({
      ...initialArchiveState,

      // 数据加载
      loadTimelineArchive: async (year?: number) => {
        set((draft) => {
          draft.loading = true;
          draft.error = null;
        });

        try {
          // 调用真实的时间轴归档API
          const timeline = await archiveApi.getTimelineArchive(year);

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
          // 调用真实的日历归档API
          const calendar = await archiveApi.getCalendarArchive(year);

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
          const categoryTree = await archiveApi.getCategoryTree();
          const categoriesWithLevels = applyCategoryLevels(categoryTree.categories);

          set((draft) => {
            draft.categoryTree = {
              categories: categoriesWithLevels,
              totalCount: categoryTree.totalCount,
            };
            draft.categoryPosts = {};
            draft.loading = false;
          });
        } catch (error) {
          const message = error instanceof Error ? error.message : '加载分类树失败';
          set((draft) => {
            draft.loading = false;
            draft.error = message;
          });
        }
      },

      loadTagCloud: async () => {
        set((draft) => {
          draft.loading = true;
          draft.error = null;
        });

        try {
          const tagCloud = await archiveApi.getTagCloud();

          set((draft) => {
            draft.tagCloud = tagCloud;
            draft.loading = false;
          });
        } catch (error) {
          const message = error instanceof Error ? error.message : '加载标签云失败';
          set((draft) => {
            draft.loading = false;
            draft.error = message;
          });
        }
      },

      loadCategoryPosts: async (categorySlug: string, page = 1, pageSize = 20) => {
        try {
          const response = await archiveApi.getCategoryPosts(categorySlug, page, pageSize);

          set((draft) => {
            draft.categoryPosts[categorySlug] = response.data;

            if (draft.categoryTree) {
              updateCategoryNode(draft.categoryTree.categories, categorySlug, (node) => {
                node.posts = response.data;
              });
            }
          });
        } catch (error) {
          const message = error instanceof Error ? error.message : '加载分类文章失败';
          set((draft) => {
            draft.error = message;
          });
          throw error;
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
        set(() => ({
          ...initialArchiveState,
          categoryPosts: {},
        }));
      },
    }))
  )
);

function applyCategoryLevels(categories: CategoryTreeNode[], level = 0): CategoryTreeNode[] {
  return categories.map((category) => ({
    ...category,
    level,
    children: applyCategoryLevels(category.children || [], level + 1),
    posts: category.posts ? [...category.posts] : category.posts,
  }));
}

function updateCategoryNode(
  categories: CategoryTreeNode[],
  slug: string,
  updater: (node: CategoryTreeNode) => void,
): boolean {
  for (const category of categories) {
    if (category.slug === slug) {
      updater(category);
      return true;
    }
    if (updateCategoryNode(category.children || [], slug, updater)) {
      return true;
    }
  }
  return false;
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
