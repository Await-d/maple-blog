// @ts-nocheck
// Home page data types matching the backend DTOs

/**
 * Home page data aggregation type
 */
export interface HomePageData {
  /** Featured posts for hero section */
  featuredPosts: PostSummary[];
  /** Latest published posts */
  latestPosts: PostSummary[];
  /** Most popular posts by views */
  popularPosts: PostSummary[];
  /** Categories with post counts */
  categories: CategorySummary[];
  /** Popular tags with usage counts */
  popularTags: TagSummary[];
  /** Website statistics */
  siteStats: SiteStats;
  /** Personalized recommendations (for authenticated users) */
  recommendedPosts?: PostSummary[];
  /** Active authors with recent activity */
  activeAuthors: AuthorSummary[];
  /** Timestamp when this data was generated */
  generatedAt: string;
  /** Cache expiry time for this data */
  expiresAt: string;
}

/**
 * Post summary type for lists and cards
 */
export interface PostSummary {
  id: string;
  title: string;
  slug: string;
  summary?: string;
  /** Featured image URL */
  featuredImageUrl?: string;
  /** Open Graph image URL */
  ogImageUrl?: string;
  publishedAt: string;
  updatedAt: string;
  viewCount: number;
  likeCount: number;
  commentCount: number;
  readingTime?: number;
  isFeatured: boolean;
  isSticky: boolean;
  /** Category information */
  category?: CategorySummary;
  /** Author information */
  author: AuthorSummary;
  /** Tags associated with this post */
  tags: TagSummary[];
}

/**
 * Category summary type
 */
export interface CategorySummary {
  id: string;
  name: string;
  slug: string;
  description?: string;
  /** Category color for UI theming */
  color?: string;
  /** Icon name or URL */
  icon?: string;
  /** Number of published posts in this category */
  postCount: number;
  /** Parent category for hierarchical organization */
  parentId?: string;
  updatedAt: string;
}

/**
 * Tag summary type
 */
export interface TagSummary {
  id: string;
  name: string;
  slug: string;
  description?: string;
  /** Tag color for UI theming */
  color?: string;
  /** Number of posts using this tag */
  postCount: number;
  /** Tag usage frequency (for tag cloud sizing) */
  usageFrequency: number;
  updatedAt: string;
}

/**
 * Author summary type
 */
export interface AuthorSummary {
  id: string;
  userName: string;
  displayName?: string;
  firstName?: string;
  lastName?: string;
  /** Avatar image URL */
  avatar?: string;
  /** Author bio/description */
  bio?: string;
  /** Number of published posts */
  postCount: number;
  /** Total views across all author's posts */
  totalViews: number;
  /** Date of most recent post */
  lastPostDate?: string;
  updatedAt: string;
}

/**
 * Site statistics type
 */
export interface SiteStats {
  /** Total number of published posts */
  totalPosts: number;
  /** Total number of categories */
  totalCategories: number;
  /** Total number of tags */
  totalTags: number;
  /** Total number of registered users */
  totalUsers: number;
  /** Total number of active authors (users with posts) */
  totalAuthors: number;
  /** Total views across all posts */
  totalViews: number;
  /** Total likes across all posts */
  totalLikes: number;
  /** Total comments across all posts */
  totalComments: number;
  /** Number of posts published this month */
  postsThisMonth: number;
  /** Number of posts published this week */
  postsThisWeek: number;
  /** Number of posts published today */
  postsToday: number;
  /** Date of the most recent post */
  lastPostDate?: string;
  /** Average posts per month */
  averagePostsPerMonth: number;
  /** Total reading time across all posts (minutes) */
  totalReadingTime: number;
  /** Timestamp when these stats were calculated */
  calculatedAt: string;
}

/**
 * Personalization preferences for the home page
 */
export interface PersonalizationSettings {
  /** User's preferred categories */
  preferredCategories: string[];
  /** User's preferred tags */
  preferredTags: string[];
  /** User's followed authors */
  followedAuthors: string[];
  /** Theme preference (light, dark, auto) */
  theme: 'light' | 'dark' | 'auto';
  /** Layout preference (grid, list, cards) */
  layout: 'grid' | 'list' | 'cards';
  /** Number of posts to show per page */
  postsPerPage: number;
  /** Show reading time estimates */
  showReadingTime: boolean;
  /** Show post summaries in lists */
  showSummaries: boolean;
  /** Language preference */
  language: string;
  updatedAt: string;
}

// UI State Types

/**
 * Home page UI state
 */
export interface HomePageState {
  /** Loading states for different sections */
  loading: {
    homePage: boolean;
    featuredPosts: boolean;
    latestPosts: boolean;
    popularPosts: boolean;
    recommendations: boolean;
    stats: boolean;
    categories: boolean;
    tags: boolean;
    authors: boolean;
  };
  /** Error states for different sections */
  errors: {
    homePage?: string;
    featuredPosts?: string;
    latestPosts?: string;
    popularPosts?: string;
    recommendations?: string;
    stats?: string;
    categories?: string;
    tags?: string;
    authors?: string;
  };
  /** Current layout mode */
  layoutMode: 'grid' | 'list' | 'cards';
  /** Whether sidebar is collapsed on mobile */
  sidebarCollapsed: boolean;
  /** Current theme */
  theme: 'light' | 'dark' | 'auto';
  /** User personalization settings */
  personalization?: PersonalizationSettings;
  /** Last refresh timestamp */
  lastRefresh?: string;
}

/**
 * Responsive breakpoint state
 */
export interface ResponsiveState {
  /** Current screen size category */
  breakpoint: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl';
  /** Whether currently mobile viewport */
  isMobile: boolean;
  /** Whether currently tablet viewport */
  isTablet: boolean;
  /** Whether currently desktop viewport */
  isDesktop: boolean;
  /** Current viewport width */
  width: number;
  /** Current viewport height */
  height: number;
}

/**
 * Component-specific UI states
 */
export interface ComponentStates {
  /** Hero section state */
  heroSection: {
    currentSlide: number;
    autoPlay: boolean;
    isPlaying: boolean;
  };
  /** Tag cloud state */
  tagCloud: {
    selectedTags: string[];
    filterMode: 'include' | 'exclude';
    sortBy: 'name' | 'count' | 'recent';
  };
  /** Category grid state */
  categoryGrid: {
    selectedCategory?: string;
    showHierarchy: boolean;
    expandedCategories: string[];
  };
  /** Post list state */
  postList: {
    sortBy: 'date' | 'popularity' | 'title';
    sortOrder: 'asc' | 'desc';
    filterBy?: {
      category?: string;
      tag?: string;
      author?: string;
    };
  };
}

// API Response Types

/**
 * API response wrapper for home page data
 */
export interface HomePageDataResponse {
  data: HomePageData;
  success: boolean;
  message?: string;
  timestamp: string;
}

/**
 * API response wrapper for lists
 */
export interface ListResponse<T> {
  data: T[];
  success: boolean;
  message?: string;
  timestamp: string;
  pagination?: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
}

/**
 * API error response
 */
export interface ApiErrorResponse {
  success: false;
  message: string;
  errors?: Record<string, string[]>;
  timestamp: string;
}

// User Interaction Types

/**
 * User interaction tracking
 */
export interface UserInteraction {
  postId: string;
  interactionType: 'view' | 'like' | 'comment' | 'share' | 'bookmark';
  duration?: number; // in seconds for view interactions
  timestamp: string;
}

/**
 * Recommendation feedback
 */
export interface RecommendationFeedback {
  postId: string;
  feedback: 'relevant' | 'not_relevant' | 'already_read' | 'not_interested';
  reason?: string;
}

// Performance and Analytics Types

/**
 * Performance metrics for home page
 */
export interface HomePageMetrics {
  /** Time to first byte */
  ttfb: number;
  /** First contentful paint */
  fcp: number;
  /** Largest contentful paint */
  lcp: number;
  /** First input delay */
  fid: number;
  /** Cumulative layout shift */
  cls: number;
  /** Bundle size metrics */
  bundleSize: {
    total: number;
    gzipped: number;
    critical: number;
  };
  /** Cache hit rates */
  cacheHitRates: {
    homePage: number;
    images: number;
    api: number;
  };
}

/**
 * User engagement metrics
 */
export interface EngagementMetrics {
  /** Time spent on page */
  timeOnPage: number;
  /** Scroll depth percentage */
  scrollDepth: number;
  /** Number of interactions */
  interactions: number;
  /** Bounce rate indicator */
  bounced: boolean;
  /** Content engagement score */
  engagementScore: number;
}

// Search and Filter Types

/**
 * Search configuration for home page
 */
export interface SearchConfig {
  /** Search query */
  query: string;
  /** Search filters */
  filters: {
    categories?: string[];
    tags?: string[];
    authors?: string[];
    dateRange?: {
      start: string;
      end: string;
    };
    contentType?: string[];
  };
  /** Search sorting */
  sort: {
    field: 'relevance' | 'date' | 'popularity' | 'title';
    order: 'asc' | 'desc';
  };
  /** Search results pagination */
  pagination: {
    page: number;
    pageSize: number;
  };
}

/**
 * Search results
 */
export interface SearchResults {
  posts: PostSummary[];
  total: number;
  facets: {
    categories: Array<{ id: string; name: string; count: number }>;
    tags: Array<{ id: string; name: string; count: number }>;
    authors: Array<{ id: string; name: string; count: number }>;
  };
  suggestions?: string[];
  executionTime: number;
}

// Accessibility Types

/**
 * Accessibility preferences
 */
export interface AccessibilitySettings {
  /** Reduced motion preference */
  reduceMotion: boolean;
  /** High contrast mode */
  highContrast: boolean;
  /** Font size multiplier */
  fontSizeMultiplier: number;
  /** Screen reader optimizations */
  screenReaderOptimized: boolean;
  /** Keyboard navigation enhanced */
  enhancedKeyboardNav: boolean;
}

// Export utility types
export type HomePageSection = keyof HomePageData;
export type LoadingState = keyof HomePageState['loading'];
export type ErrorState = keyof HomePageState['errors'];
export type ThemeMode = PersonalizationSettings['theme'];
export type LayoutMode = PersonalizationSettings['layout'];
export type InteractionType = UserInteraction['interactionType'];
export type FeedbackType = RecommendationFeedback['feedback'];
export type SortField = SearchConfig['sort']['field'];
export type SortOrder = SearchConfig['sort']['order'];