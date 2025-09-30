/**
 * Search and Archive Type Definitions
 * 搜索和归档功能的TypeScript类型定义
 */

// 基础搜索类型
export interface SearchQuery {
  text: string;
  filters?: SearchFilters;
  sortBy?: SortOption;
  sortDirection?: 'asc' | 'desc';
  advanced?: boolean;
}

export interface SearchCategory {
  id: string;
  name: string;
  slug: string;
  count: number;
  description?: string;
  color?: string;
}

export interface AdvancedSearchOptions {
  exactPhrase?: string;
  anyWords?: string[];
  excludeWords?: string[];
  titleOnly?: boolean;
  contentOnly?: boolean;
  authorName?: string;
  dateRange?: DateRange;
  minReadingTime?: number;
  maxReadingTime?: number;
}

export interface SearchStats {
  totalResults: number;
  searchTime: number;
  topCategories: { name: string; count: number }[];
  topTags: { name: string; count: number }[];
  averageRelevanceScore: number;
}

export interface SearchRequest {
  query: string;
  filters?: SearchFilters;
  page?: number;
  pageSize?: number;
  sortBy?: SortOption;
  sortDirection?: 'asc' | 'desc';
}

export interface SearchFilters {
  categories?: string[];
  tags?: string[];
  authors?: string[];
  dateFrom?: string;
  dateTo?: string;
  contentType?: ContentType[];
  status?: PostStatus[];
}

export interface SearchResponse {
  results: SearchResult[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
  took: number; // 搜索耗时（毫秒）
  suggestions?: SearchSuggestion[];
  facets?: SearchFacets;
}

export interface SearchResult {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  content?: string;
  author: Author;
  categories: Category[];
  tags: Tag[];
  publishedAt: string;
  updatedAt: string;
  readingTime: number;
  viewCount: number;
  likeCount: number;
  commentCount: number;
  thumbnailUrl?: string;
  highlights?: SearchHighlight[];
  score: number; // 搜索相关性评分
}

export interface SearchHighlight {
  field: string;
  fragments: string[];
}

export interface SearchSuggestion {
  text: string;
  highlighted: string;
  score: number;
  type: 'query' | 'category' | 'tag' | 'author';
}

export interface SearchFacets {
  categories: FacetItem[];
  tags: FacetItem[];
  authors: FacetItem[];
  years: FacetItem[];
  months: FacetItem[];
}

export interface FacetItem {
  name: string;
  count: number;
  selected: boolean;
}

// 搜索配置和选项
export type SortOption =
  | 'relevance'
  | 'publishedAt'
  | 'updatedAt'
  | 'title'
  | 'viewCount'
  | 'likeCount'
  | 'commentCount';

export type ContentType = 'post' | 'page' | 'draft';
export type PostStatus = 'published' | 'draft' | 'archived';

// 搜索历史和建议
export interface SearchHistory {
  id: string;
  query: string;
  timestamp: string;
  resultCount: number;
  filters?: SearchFilters;
}

export interface SearchAutoComplete {
  query: string;
  suggestions: AutoCompleteSuggestion[];
  loading: boolean;
}

export interface AutoCompleteSuggestion {
  text: string;
  type: 'query' | 'category' | 'tag' | 'author' | 'title';
  count?: number;
  icon?: string;
}

// 归档类型
export interface ArchiveRequest {
  type: ArchiveType;
  groupBy?: ArchiveGroupBy;
  dateRange?: DateRange;
  categories?: string[];
  tags?: string[];
}

export interface ArchiveResponse {
  items: ArchiveItem[];
  totalCount: number;
  grouping: ArchiveGroupBy;
  dateRange: DateRange;
  statistics: ArchiveStatistics;
}

export interface ArchiveItem {
  key: string; // 分组键值（如年份、月份、分类名）
  label: string; // 显示标签
  count: number;
  posts: ArchivePost[];
  children?: ArchiveItem[]; // 支持层级结构
}

export interface ArchivePost {
  id: string;
  title: string;
  slug: string;
  excerpt?: string;
  author: Author;
  publishedAt: string;
  categories: Category[];
  tags: Tag[];
  readingTime: number;
  viewCount: number;
  thumbnailUrl?: string;
}

export interface ArchiveStatistics {
  totalPosts: number;
  totalCategories: number;
  totalTags: number;
  totalAuthors: number;
  dateRange: DateRange;
  mostActiveMonth: string;
  averagePostsPerMonth: number;
  categoryDistribution: { name: string; count: number }[];
  tagDistribution: { name: string; count: number }[];
}

export type ArchiveType = 'date' | 'category' | 'tag' | 'author';
export type ArchiveGroupBy = 'year' | 'month' | 'day' | 'category' | 'tag' | 'author';

export interface DateRange {
  from: string;
  to: string;
}

// 时间轴归档
export interface TimelineArchive {
  years: TimelineYear[];
  totalCount: number;
  dateRange: DateRange;
}

export interface TimelineYear {
  year: number;
  count: number;
  months: TimelineMonth[];
}

export interface TimelineMonth {
  month: number;
  monthName: string;
  count: number;
  posts: ArchivePost[];
}

// 日历视图
export interface CalendarArchive {
  year: number;
  months: CalendarMonth[];
  totalCount: number;
}

export interface CalendarMonth {
  month: number;
  monthName: string;
  days: CalendarDay[];
}

export interface CalendarDay {
  day: number;
  date: string;
  count: number;
  posts: ArchivePost[];
  isToday: boolean;
  isCurrentMonth: boolean;
}

// 标签云
export interface TagCloud {
  tags: TagCloudItem[];
  maxCount: number;
  minCount: number;
}

export interface TagCloudItem {
  name: string;
  slug: string;
  count: number;
  weight: number; // 0-1，用于控制显示大小
  color?: string;
}

// 分类树
export interface CategoryTree {
  categories: CategoryTreeNode[];
  totalCount: number;
}

export interface CategoryTreeNode {
  id: string;
  name: string;
  slug: string;
  count: number;
  description?: string;
  color?: string;
  children: CategoryTreeNode[];
  posts?: ArchivePost[];
  level: number;
}

// 搜索分析
export interface SearchAnalytics {
  topQueries: QueryAnalytics[];
  queryTrends: QueryTrend[];
  noResultQueries: string[];
  averageSearchTime: number;
  totalSearches: number;
  uniqueSearchers: number;
}

export interface QueryAnalytics {
  query: string;
  count: number;
  avgResultCount: number;
  avgClickPosition: number;
  lastSearchedAt: string;
}

export interface QueryTrend {
  date: string;
  count: number;
  queries: string[];
}

// 搜索状态管理
export interface SearchState {
  // 搜索状态
  query: string;
  filters: SearchFilters;
  results: SearchResult[];
  suggestions: SearchSuggestion[];
  autoComplete: AutoCompleteSuggestion[];
  history: SearchHistory[];

  // 分页和排序
  page: number;
  pageSize: number;
  totalCount: number;
  hasMore: boolean;
  sortBy: SortOption;
  sortDirection: 'asc' | 'desc';

  // UI状态
  loading: boolean;
  error: string | null;
  showAdvanced: boolean;
  showHistory: boolean;

  // 性能统计
  searchTime: number;
  resultStats?: {
    took: number;
    totalHits: number;
    maxScore: number;
  };
}

export interface ArchiveState {
  // 归档数据
  timelineArchive?: TimelineArchive;
  calendarArchive?: CalendarArchive;
  categoryTree?: CategoryTree;
  tagCloud?: TagCloud;
  categoryPosts: Record<string, ArchivePost[]>;

  // 当前视图
  currentView: 'timeline' | 'calendar' | 'category' | 'tag';
  selectedYear?: number;
  selectedMonth?: number;
  selectedCategory?: string;
  selectedTag?: string;

  // UI状态
  loading: boolean;
  error: string | null;
}

// 通用实体类型
export interface Author {
  id: string;
  username: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  bio?: string;
  website?: string;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  parentId?: string;
  parent?: Category;
  children?: Category[];
  postCount: number;
}

export interface Tag {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  postCount: number;
}

// API响应类型
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// 搜索配置
export interface SearchConfig {
  maxSuggestions: number;
  debounceMs: number;
  minQueryLength: number;
  maxHistoryItems: number;
  enableHighlight: boolean;
  highlightTags: {
    pre: string;
    post: string;
  };
  facetLimits: {
    categories: number;
    tags: number;
    authors: number;
  };
}

// 默认配置
export const DEFAULT_SEARCH_CONFIG: SearchConfig = {
  maxSuggestions: 10,
  debounceMs: 300,
  minQueryLength: 2,
  maxHistoryItems: 20,
  enableHighlight: true,
  highlightTags: {
    pre: '<mark>',
    post: '</mark>'
  },
  facetLimits: {
    categories: 20,
    tags: 30,
    authors: 10
  }
};

export const DEFAULT_PAGE_SIZE = 10;
export const DEFAULT_SORT_OPTION: SortOption = 'relevance';
