import { create } from 'zustand';
import { subscribeWithSelector, persist, createJSONStorage } from 'zustand/middleware';
import { immer } from 'zustand/middleware/immer';
import type {
  HomePageState,
  ResponsiveState,
  ComponentStates,
  PersonalizationSettings,
  HomePageMetrics,
  AccessibilitySettings,
  ThemeMode,
  LayoutMode,
} from '../types/home';

/**
 * Home page store interface with all state and actions
 */
interface HomeStore extends HomePageState {
  // State
  responsive: ResponsiveState;
  components: ComponentStates;
  metrics: HomePageMetrics | null;
  accessibility: AccessibilitySettings;

  // UI Actions
  setLoading: (section: keyof HomePageState['loading'], isLoading: boolean) => void;
  setError: (section: keyof HomePageState['errors'], error?: string) => void;
  clearErrors: () => void;
  setLayoutMode: (mode: LayoutMode) => void;
  setSidebarCollapsed: (collapsed: boolean) => void;
  setTheme: (theme: ThemeMode) => void;
  toggleTheme: () => void;
  setLastRefresh: (timestamp?: string) => void;

  // Responsive Actions
  updateResponsiveState: (state: Partial<ResponsiveState>) => void;
  setBreakpoint: (breakpoint: ResponsiveState['breakpoint']) => void;

  // Component State Actions
  setHeroSlide: (slide: number) => void;
  setHeroAutoPlay: (autoPlay: boolean) => void;
  toggleHeroPlayback: () => void;
  selectTags: (tags: string[]) => void;
  toggleTag: (tag: string) => void;
  setTagFilterMode: (mode: 'include' | 'exclude') => void;
  setTagSortBy: (sortBy: 'name' | 'count' | 'recent') => void;
  selectCategory: (categoryId?: string) => void;
  toggleCategoryHierarchy: () => void;
  expandCategory: (categoryId: string) => void;
  collapseCategory: (categoryId: string) => void;
  setPostListSort: (sortBy: 'date' | 'popularity' | 'title', order: 'asc' | 'desc') => void;
  setPostListFilter: (filter: ComponentStates['postList']['filterBy']) => void;

  // Personalization Actions
  updatePersonalization: (settings: Partial<PersonalizationSettings>) => void;
  addPreferredCategory: (categoryId: string) => void;
  removePreferredCategory: (categoryId: string) => void;
  addPreferredTag: (tagId: string) => void;
  removePreferredTag: (tagId: string) => void;
  followAuthor: (authorId: string) => void;
  unfollowAuthor: (authorId: string) => void;
  setPostsPerPage: (count: number) => void;
  toggleReadingTime: () => void;
  toggleSummaries: () => void;
  setLanguage: (language: string) => void;

  // Accessibility Actions
  updateAccessibility: (settings: Partial<AccessibilitySettings>) => void;
  toggleReduceMotion: () => void;
  toggleHighContrast: () => void;
  setFontSizeMultiplier: (multiplier: number) => void;
  toggleScreenReaderOptimized: () => void;
  toggleEnhancedKeyboardNav: () => void;

  // Performance Actions
  updateMetrics: (metrics: Partial<HomePageMetrics>) => void;
  recordPerformanceMetric: (metric: keyof HomePageMetrics, value: number) => void;

  // Reset Actions
  resetComponentStates: () => void;
  resetPersonalization: () => void;
  resetAccessibility: () => void;
  resetAll: () => void;
}

// Default states
const defaultHomePageState: HomePageState = {
  loading: {
    homePage: false,
    featuredPosts: false,
    latestPosts: false,
    popularPosts: false,
    recommendations: false,
    stats: false,
    categories: false,
    tags: false,
    authors: false,
  },
  errors: {},
  layoutMode: 'cards',
  sidebarCollapsed: false,
  theme: 'auto',
  personalization: undefined,
  lastRefresh: undefined,
};

const defaultResponsiveState: ResponsiveState = {
  breakpoint: 'lg',
  isMobile: false,
  isTablet: false,
  isDesktop: true,
  width: 1024,
  height: 768,
};

const defaultComponentStates: ComponentStates = {
  heroSection: {
    currentSlide: 0,
    autoPlay: true,
    isPlaying: true,
  },
  tagCloud: {
    selectedTags: [],
    filterMode: 'include',
    sortBy: 'count',
  },
  categoryGrid: {
    selectedCategory: undefined,
    showHierarchy: true,
    expandedCategories: [],
  },
  postList: {
    sortBy: 'date',
    sortOrder: 'desc',
    filterBy: undefined,
  },
};

const defaultPersonalization: PersonalizationSettings = {
  preferredCategories: [],
  preferredTags: [],
  followedAuthors: [],
  theme: 'auto',
  layout: 'cards',
  postsPerPage: 10,
  showReadingTime: true,
  showSummaries: true,
  language: 'zh-CN',
  updatedAt: new Date().toISOString(),
};

const defaultAccessibilitySettings: AccessibilitySettings = {
  reduceMotion: false,
  highContrast: false,
  fontSizeMultiplier: 1.0,
  screenReaderOptimized: false,
  enhancedKeyboardNav: false,
};

/**
 * Home page store with persistent settings and responsive state management
 */
export const useHomeStore = create<HomeStore>()(
  subscribeWithSelector(
    persist(
      immer((set, _get) => ({
        // Initial state
        ...defaultHomePageState,
        responsive: defaultResponsiveState,
        components: defaultComponentStates,
        metrics: null,
        accessibility: defaultAccessibilitySettings,

        // UI Actions
        setLoading: (section, isLoading) =>
          set((state) => {
            state.loading[section] = isLoading;
          }),

        setError: (section, error) =>
          set((state) => {
            if (error) {
              state.errors[section] = error;
            } else {
              delete state.errors[section];
            }
          }),

        clearErrors: () =>
          set((state) => {
            state.errors = {};
          }),

        setLayoutMode: (mode) =>
          set((state) => {
            state.layoutMode = mode;
            if (state.personalization) {
              state.personalization.layout = mode;
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        setSidebarCollapsed: (collapsed) =>
          set((state) => {
            state.sidebarCollapsed = collapsed;
          }),

        setTheme: (theme) =>
          set((state) => {
            state.theme = theme;
            if (state.personalization) {
              state.personalization.theme = theme;
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        toggleTheme: () =>
          set((state) => {
            const currentTheme = state.theme;
            const newTheme: ThemeMode = currentTheme === 'light' ? 'dark' : currentTheme === 'dark' ? 'auto' : 'light';
            state.theme = newTheme;
            if (state.personalization) {
              state.personalization.theme = newTheme;
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        setLastRefresh: (timestamp) =>
          set((state) => {
            state.lastRefresh = timestamp || new Date().toISOString();
          }),

        // Responsive Actions
        updateResponsiveState: (newState) =>
          set((state) => {
            Object.assign(state.responsive, newState);
          }),

        setBreakpoint: (breakpoint) =>
          set((state) => {
            state.responsive.breakpoint = breakpoint;
            state.responsive.isMobile = ['xs', 'sm'].includes(breakpoint);
            state.responsive.isTablet = breakpoint === 'md';
            state.responsive.isDesktop = ['lg', 'xl', '2xl'].includes(breakpoint);
          }),

        // Component State Actions
        setHeroSlide: (slide) =>
          set((state) => {
            if (state.components.heroSection) {
              state.components.heroSection.currentSlide = slide;
            }
          }),

        setHeroAutoPlay: (autoPlay) =>
          set((state) => {
            if (state.components.heroSection) {
              state.components.heroSection.autoPlay = autoPlay;
              state.components.heroSection.isPlaying = autoPlay;
            }
          }),

        toggleHeroPlayback: () =>
          set((state) => {
            if (state.components.heroSection) {
              state.components.heroSection.isPlaying = !state.components.heroSection.isPlaying;
            }
          }),

        selectTags: (tags) =>
          set((state) => {
            state.components.tagCloud.selectedTags = tags;
          }),

        toggleTag: (tag) =>
          set((state) => {
            const currentTags = state.components.tagCloud.selectedTags;
            if (currentTags.includes(tag)) {
              state.components.tagCloud.selectedTags = currentTags.filter((t: string) => t !== tag);
            } else {
              state.components.tagCloud.selectedTags = [...currentTags, tag];
            }
          }),

        setTagFilterMode: (mode) =>
          set((state) => {
            state.components.tagCloud.filterMode = mode;
          }),

        setTagSortBy: (sortBy) =>
          set((state) => {
            state.components.tagCloud.sortBy = sortBy;
          }),

        selectCategory: (categoryId) =>
          set((state) => {
            state.components.categoryGrid.selectedCategory = categoryId;
          }),

        toggleCategoryHierarchy: () =>
          set((state) => {
            state.components.categoryGrid.showHierarchy = !state.components.categoryGrid.showHierarchy;
          }),

        expandCategory: (categoryId) =>
          set((state) => {
            if (!state.components.categoryGrid.expandedCategories.includes(categoryId)) {
              state.components.categoryGrid.expandedCategories.push(categoryId);
            }
          }),

        collapseCategory: (categoryId) =>
          set((state) => {
            state.components.categoryGrid.expandedCategories =
              state.components.categoryGrid.expandedCategories.filter((id: string) => id !== categoryId);
          }),

        setPostListSort: (sortBy, order) =>
          set((state) => {
            state.components.postList.sortBy = sortBy;
            state.components.postList.sortOrder = order;
          }),

        setPostListFilter: (filter) =>
          set((state) => {
            state.components.postList.filterBy = filter;
          }),

        // Personalization Actions
        updatePersonalization: (settings) =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            Object.assign(state.personalization, settings);
            state.personalization.updatedAt = new Date().toISOString();
          }),

        addPreferredCategory: (categoryId) =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            if (!state.personalization.preferredCategories.includes(categoryId)) {
              state.personalization.preferredCategories.push(categoryId);
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        removePreferredCategory: (categoryId) =>
          set((state) => {
            if (state.personalization) {
              state.personalization.preferredCategories =
                state.personalization.preferredCategories.filter((id: string) => id !== categoryId);
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        addPreferredTag: (tagId) =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            if (!state.personalization.preferredTags.includes(tagId)) {
              state.personalization.preferredTags.push(tagId);
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        removePreferredTag: (tagId) =>
          set((state) => {
            if (state.personalization) {
              state.personalization.preferredTags =
                state.personalization.preferredTags.filter((id: string) => id !== tagId);
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        followAuthor: (authorId) =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            if (!state.personalization.followedAuthors.includes(authorId)) {
              state.personalization.followedAuthors.push(authorId);
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        unfollowAuthor: (authorId) =>
          set((state) => {
            if (state.personalization) {
              state.personalization.followedAuthors =
                state.personalization.followedAuthors.filter((id: string) => id !== authorId);
              state.personalization.updatedAt = new Date().toISOString();
            }
          }),

        setPostsPerPage: (count) =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            state.personalization.postsPerPage = Math.max(5, Math.min(50, count));
            state.personalization.updatedAt = new Date().toISOString();
          }),

        toggleReadingTime: () =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            state.personalization.showReadingTime = !state.personalization.showReadingTime;
            state.personalization.updatedAt = new Date().toISOString();
          }),

        toggleSummaries: () =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            state.personalization.showSummaries = !state.personalization.showSummaries;
            state.personalization.updatedAt = new Date().toISOString();
          }),

        setLanguage: (language) =>
          set((state) => {
            if (!state.personalization) {
              state.personalization = { ...defaultPersonalization };
            }
            state.personalization.language = language;
            state.personalization.updatedAt = new Date().toISOString();
          }),

        // Accessibility Actions
        updateAccessibility: (settings) =>
          set((state) => {
            Object.assign(state.accessibility, settings);
          }),

        toggleReduceMotion: () =>
          set((state) => {
            state.accessibility.reduceMotion = !state.accessibility.reduceMotion;
          }),

        toggleHighContrast: () =>
          set((state) => {
            state.accessibility.highContrast = !state.accessibility.highContrast;
          }),

        setFontSizeMultiplier: (multiplier) =>
          set((state) => {
            state.accessibility.fontSizeMultiplier = Math.max(0.8, Math.min(2.0, multiplier));
          }),

        toggleScreenReaderOptimized: () =>
          set((state) => {
            state.accessibility.screenReaderOptimized = !state.accessibility.screenReaderOptimized;
          }),

        toggleEnhancedKeyboardNav: () =>
          set((state) => {
            state.accessibility.enhancedKeyboardNav = !state.accessibility.enhancedKeyboardNav;
          }),

        // Performance Actions
        updateMetrics: (metrics) =>
          set((state) => {
            if (state.metrics) {
              Object.assign(state.metrics, metrics);
            } else {
              state.metrics = metrics as HomePageMetrics;
            }
          }),

        recordPerformanceMetric: (metric, value) =>
          set((state) => {
            if (!state.metrics) {
              state.metrics = {} as HomePageMetrics;
            }
            // Type-safe metric assignment
            if (state.metrics && typeof state.metrics === 'object') {
              (state.metrics as Record<string, unknown>)[metric] = value;
            }
          }),

        // Reset Actions
        resetComponentStates: () =>
          set((state) => {
            state.components = { ...defaultComponentStates };
          }),

        resetPersonalization: () =>
          set((state) => {
            state.personalization = { ...defaultPersonalization };
          }),

        resetAccessibility: () =>
          set((state) => {
            state.accessibility = { ...defaultAccessibilitySettings };
          }),

        resetAll: () =>
          set((state) => {
            Object.assign(state, {
              ...defaultHomePageState,
              responsive: defaultResponsiveState,
              components: defaultComponentStates,
              metrics: null,
              accessibility: defaultAccessibilitySettings,
            });
          }),
      })),
      {
        name: 'home-store',
        storage: createJSONStorage(() => localStorage),
        partialize: (state) => ({
          // Only persist user preferences, not transient UI state
          layoutMode: state.layoutMode,
          theme: state.theme,
          personalization: state.personalization,
          accessibility: state.accessibility,
          components: {
            // Persist some component preferences
            tagCloud: {
              filterMode: state.components.tagCloud.filterMode,
              sortBy: state.components.tagCloud.sortBy,
            },
            categoryGrid: {
              showHierarchy: state.components.categoryGrid.showHierarchy,
            },
            postList: {
              sortBy: state.components.postList.sortBy,
              sortOrder: state.components.postList.sortOrder,
            },
          },
        }),
        version: 1,
        migrate: (persistedState: unknown, version: number) => {
          // Handle store migrations if needed
          if (version === 0) {
            // Migrate from version 0 to 1
            return {
              ...(persistedState as Record<string, unknown>),
              accessibility: defaultAccessibilitySettings,
            };
          }
          return persistedState;
        },
      }
    )
  )
);

// Selectors for common state combinations
export const useHomePageLoading = () => useHomeStore((state) => state.loading);
export const useHomePageErrors = () => useHomeStore((state) => state.errors);
export const useResponsiveState = () => useHomeStore((state) => state.responsive);
export const useComponentStates = () => useHomeStore((state) => state.components);
export const usePersonalization = () => useHomeStore((state) => state.personalization);
export const useAccessibilitySettings = () => useHomeStore((state) => state.accessibility);
export const useHomePageMetrics = () => useHomeStore((state) => state.metrics);

// Derived selectors
export const useIsMobile = () => useHomeStore((state) => state.responsive.isMobile);
export const useIsTablet = () => useHomeStore((state) => state.responsive.isTablet);
export const useIsDesktop = () => useHomeStore((state) => state.responsive.isDesktop);
export const useCurrentTheme = () => useHomeStore((state) => state.theme);
export const useCurrentLayout = () => useHomeStore((state) => state.layoutMode);
export const useSidebarCollapsed = () => useHomeStore((state) => state.sidebarCollapsed);

// Action selectors
export const useHomePageActions = () => useHomeStore((state) => ({
  setLoading: state.setLoading,
  setError: state.setError,
  clearErrors: state.clearErrors,
  setLayoutMode: state.setLayoutMode,
  setSidebarCollapsed: state.setSidebarCollapsed,
  setTheme: state.setTheme,
  toggleTheme: state.toggleTheme,
  setLastRefresh: state.setLastRefresh,
}));

export const useResponsiveActions = () => useHomeStore((state) => ({
  updateResponsiveState: state.updateResponsiveState,
  setBreakpoint: state.setBreakpoint,
}));

export const usePersonalizationActions = () => useHomeStore((state) => ({
  updatePersonalization: state.updatePersonalization,
  addPreferredCategory: state.addPreferredCategory,
  removePreferredCategory: state.removePreferredCategory,
  addPreferredTag: state.addPreferredTag,
  removePreferredTag: state.removePreferredTag,
  followAuthor: state.followAuthor,
  unfollowAuthor: state.unfollowAuthor,
  setPostsPerPage: state.setPostsPerPage,
  toggleReadingTime: state.toggleReadingTime,
  toggleSummaries: state.toggleSummaries,
  setLanguage: state.setLanguage,
}));

export const useAccessibilityActions = () => useHomeStore((state) => ({
  updateAccessibility: state.updateAccessibility,
  toggleReduceMotion: state.toggleReduceMotion,
  toggleHighContrast: state.toggleHighContrast,
  setFontSizeMultiplier: state.setFontSizeMultiplier,
  toggleScreenReaderOptimized: state.toggleScreenReaderOptimized,
  toggleEnhancedKeyboardNav: state.toggleEnhancedKeyboardNav,
}));