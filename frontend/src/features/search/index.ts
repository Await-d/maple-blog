/**
 * Search Feature Index
 * Central exports for search-related functionality
 */

// Components
export { SearchBox } from '@/components/search/SearchBox';
export { SearchSuggestions } from '@/components/search/SearchSuggestions';
export { SearchResults } from '@/components/search/SearchResults';
export type { SearchFilters } from '@/components/search/SearchFilters';
export { AdvancedSearch } from '@/components/search/AdvancedSearch';
export { default as SearchResultCard } from '@/components/search/SearchResultCard';

// Services and API
export { searchApi } from '@/services/search/searchApi';
export { archiveApi } from '@/services/search/archiveApi';

// Store
export { useSearchStore } from '@/stores/searchStore';

// Types (re-export from global types)
export type {
  SearchResult,
  SearchQuery,
  SearchFilters as SearchFiltersType,
  SearchSuggestion,
  SearchCategory,
  AdvancedSearchOptions,
  SearchStats
} from '@/types/search';

// Search feature configuration
export const searchFeature = {
  name: 'search',
  version: '1.0.0',
  description: 'Advanced search and filtering system',
  components: {
    SearchBox: () => import('@/components/search/SearchBox').then(m => ({ default: m.SearchBox })),
    SearchSuggestions: () => import('@/components/search/SearchSuggestions').then(m => ({ default: m.SearchSuggestions })),
    SearchResults: () => import('@/components/search/SearchResults').then(m => ({ default: m.SearchResults })),
    SearchFilters: () => import('@/components/search/SearchFilters').then(m => ({ default: m.SearchFilters })),
    AdvancedSearch: () => import('@/components/search/AdvancedSearch').then(m => ({ default: m.AdvancedSearch })),
    SearchResultCard: () => import('@/components/search/SearchResultCard').then(m => ({ default: m.default }))
  },
  services: {
    searchApi: () => import('@/services/search/searchApi').then(m => m.searchApi),
    archiveApi: () => import('@/services/search/archiveApi').then(m => m.archiveApi)
  },
  stores: {
    useSearchStore: () => import('@/stores/searchStore').then(m => m.useSearchStore)
  }
} as const;

// Search configuration
export const searchConfig = {
  // Search behavior settings
  debounceDelay: 300, // milliseconds
  minQueryLength: 2,
  maxSuggestions: 8,
  maxResults: 50,
  
  // Search categories
  categories: [
    { id: 'posts', name: 'Blog Posts', weight: 1.0 },
    { id: 'categories', name: 'Categories', weight: 0.8 },
    { id: 'tags', name: 'Tags', weight: 0.6 },
    { id: 'authors', name: 'Authors', weight: 0.7 },
    { id: 'comments', name: 'Comments', weight: 0.4 }
  ],
  
  // Search filters
  filters: {
    dateRanges: [
      { id: 'today', name: 'Today', days: 1 },
      { id: 'week', name: 'This Week', days: 7 },
      { id: 'month', name: 'This Month', days: 30 },
      { id: 'quarter', name: 'Last 3 Months', days: 90 },
      { id: 'year', name: 'This Year', days: 365 }
    ],
    sortOptions: [
      { id: 'relevance', name: 'Relevance', field: '_score', order: 'desc' },
      { id: 'date_desc', name: 'Newest First', field: 'publishedAt', order: 'desc' },
      { id: 'date_asc', name: 'Oldest First', field: 'publishedAt', order: 'asc' },
      { id: 'views_desc', name: 'Most Viewed', field: 'viewsCount', order: 'desc' },
      { id: 'likes_desc', name: 'Most Liked', field: 'likesCount', order: 'desc' },
      { id: 'comments_desc', name: 'Most Commented', field: 'commentsCount', order: 'desc' }
    ],
    contentTypes: [
      { id: 'article', name: 'Articles' },
      { id: 'tutorial', name: 'Tutorials' },
      { id: 'guide', name: 'Guides' },
      { id: 'news', name: 'News' },
      { id: 'opinion', name: 'Opinion' }
    ]
  },
  
  // Advanced search options
  advanced: {
    operators: {
      and: 'AND',
      or: 'OR',
      not: 'NOT',
      phrase: '"..."',
      wildcard: '*',
      fuzzy: '~'
    },
    fields: [
      { id: 'title', name: 'Title', boost: 2.0 },
      { id: 'content', name: 'Content', boost: 1.0 },
      { id: 'excerpt', name: 'Excerpt', boost: 1.5 },
      { id: 'tags', name: 'Tags', boost: 1.2 },
      { id: 'category', name: 'Category', boost: 1.1 },
      { id: 'author', name: 'Author', boost: 0.8 }
    ],
    highlighting: {
      preTag: '<mark class="search-highlight">',
      postTag: '</mark>',
      maxFragments: 3,
      fragmentSize: 150
    }
  },
  
  // Analytics tracking
  analytics: {
    trackQuery: true,
    trackResults: true,
    trackClicks: true,
    trackNoResults: true,
    trackFilters: true
  },
  
  // Performance settings
  performance: {
    cacheResults: true,
    cacheDuration: 5 * 60 * 1000, // 5 minutes
    prefetchSuggestions: true,
    lazyLoadResults: true,
    virtualization: {
      enabled: true,
      itemHeight: 120,
      overscan: 5
    }
  }
} as const;

// Search utilities
export const searchUtils = {
  /**
   * Highlight search terms in text
   */
  highlightText: (text: string, query: string, className = 'search-highlight'): string => {
    if (!query.trim()) return text;
    
    const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
    return text.replace(regex, `<mark class="${className}">$1</mark>`);
  },
  
  /**
   * Parse search query for advanced operators
   */
  parseQuery: (query: string) => {
    const parsed = {
      terms: [] as string[],
      phrases: [] as string[],
      exclude: [] as string[],
      operators: [] as string[]
    };
    
    // Extract phrases (quoted text)
    const phraseRegex = /"([^"]*)"/g;
    let match;
    while ((match = phraseRegex.exec(query)) !== null) {
      parsed.phrases.push(match[1]);
      query = query.replace(match[0], '').trim();
    }
    
    // Extract exclusions (NOT operator)
    const excludeRegex = /(?:^|\s)-(\w+)/g;
    while ((match = excludeRegex.exec(query)) !== null) {
      parsed.exclude.push(match[1]);
      query = query.replace(match[0], '').trim();
    }
    
    // Split remaining terms
    parsed.terms = query.split(/\s+/).filter(term => term.length > 0);
    
    return parsed;
  },
  
  /**
   * Build search URL with parameters
   */
  buildSearchUrl: (baseUrl: string, params: Record<string, unknown>): string => {
    const searchParams = new URLSearchParams();
    
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        if (Array.isArray(value)) {
          value.forEach(v => searchParams.append(key, v.toString()));
        } else {
          searchParams.set(key, value.toString());
        }
      }
    });
    
    return `${baseUrl}?${searchParams.toString()}`;
  },
  
  /**
   * Get search result snippet
   */
  getSnippet: (content: string, query: string, maxLength = 200): string => {
    if (!query.trim()) return content.substring(0, maxLength) + (content.length > maxLength ? '...' : '');
    
    const queryLower = query.toLowerCase();
    const contentLower = content.toLowerCase();
    const index = contentLower.indexOf(queryLower);
    
    if (index === -1) {
      return content.substring(0, maxLength) + (content.length > maxLength ? '...' : '');
    }
    
    const start = Math.max(0, index - Math.floor((maxLength - query.length) / 2));
    const end = Math.min(content.length, start + maxLength);
    
    let snippet = content.substring(start, end);
    
    if (start > 0) snippet = '...' + snippet;
    if (end < content.length) snippet = snippet + '...';
    
    return snippet;
  }
};

export default searchFeature;