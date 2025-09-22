// @ts-nocheck
import {
  useQuery,
  useMutation,
  useQueryClient,
  UseQueryOptions,
  UseMutationOptions,
  QueryKey,
} from '@tanstack/react-query';
import type {
  HomePageData,
  PostSummary,
  CategorySummary,
  TagSummary,
  AuthorSummary,
  SiteStats,
  UserInteraction,
  HomePageDataResponse,
  ListResponse,
  ApiErrorResponse,
} from '../../types/home';
import { apiClient } from '../api/client';

// API endpoints
const HOME_ENDPOINTS = {
  HOME_PAGE: '/api/home',
  FEATURED_POSTS: '/api/home/featured',
  LATEST_POSTS: '/api/home/latest',
  POPULAR_POSTS: '/api/home/popular',
  RECOMMENDATIONS: '/api/home/recommendations',
  STATS: '/api/home/stats',
  CATEGORIES: '/api/home/categories',
  TAGS: '/api/home/tags',
  AUTHORS: '/api/home/authors',
  INTERACTION: '/api/home/interaction',
  REFRESH_CACHE: '/api/home/refresh-cache',
} as const;

// Query keys for consistent caching
export const HOME_QUERY_KEYS = {
  homePage: () => ['home', 'page'] as const,
  homePagePersonalized: (userId: string) => ['home', 'page', 'personalized', userId] as const,
  featuredPosts: (count: number) => ['home', 'featured', count] as const,
  latestPosts: (count: number) => ['home', 'latest', count] as const,
  popularPosts: (count: number, daysBack: number) => ['home', 'popular', count, daysBack] as const,
  recommendations: (userId: string, count: number) => ['home', 'recommendations', userId, count] as const,
  stats: () => ['home', 'stats'] as const,
  categories: (includeEmpty: boolean) => ['home', 'categories', includeEmpty] as const,
  tags: (count: number, minUsage: number) => ['home', 'tags', count, minUsage] as const,
  authors: (count: number) => ['home', 'authors', count] as const,
} as const;

// Cache times (in milliseconds)
const CACHE_TIMES = {
  homePage: 15 * 60 * 1000, // 15 minutes
  homePagePersonalized: 10 * 60 * 1000, // 10 minutes
  featuredPosts: 60 * 60 * 1000, // 1 hour
  latestPosts: 5 * 60 * 1000, // 5 minutes
  popularPosts: 30 * 60 * 1000, // 30 minutes
  recommendations: 10 * 60 * 1000, // 10 minutes
  stats: 30 * 60 * 1000, // 30 minutes
  categories: 60 * 60 * 1000, // 1 hour
  tags: 30 * 60 * 1000, // 30 minutes
  authors: 15 * 60 * 1000, // 15 minutes
} as const;

// API service functions
class HomeApiService {
  /**
   * Get comprehensive home page data
   */
  static async getHomePageData(): Promise<HomePageData> {
    const response = await apiClient.get<HomePageDataResponse>(HOME_ENDPOINTS.HOME_PAGE);
    return response.data.data;
  }

  /**
   * Get featured posts
   */
  static async getFeaturedPosts(count: number = 5): Promise<PostSummary[]> {
    const response = await apiClient.get<ListResponse<PostSummary>>(
      HOME_ENDPOINTS.FEATURED_POSTS,
      { params: { count } }
    );
    return response.data.data;
  }

  /**
   * Get latest published posts
   */
  static async getLatestPosts(count: number = 10): Promise<PostSummary[]> {
    const response = await apiClient.get<ListResponse<PostSummary>>(
      HOME_ENDPOINTS.LATEST_POSTS,
      { params: { count } }
    );
    return response.data.data;
  }

  /**
   * Get popular posts by views
   */
  static async getPopularPosts(count: number = 10, daysBack: number = 30): Promise<PostSummary[]> {
    const response = await apiClient.get<ListResponse<PostSummary>>(
      HOME_ENDPOINTS.POPULAR_POSTS,
      { params: { count, daysBack } }
    );
    return response.data.data;
  }

  /**
   * Get personalized recommendations
   */
  static async getPersonalizedRecommendations(count: number = 10): Promise<PostSummary[]> {
    const response = await apiClient.get<ListResponse<PostSummary>>(
      HOME_ENDPOINTS.RECOMMENDATIONS,
      { params: { count } }
    );
    return response.data.data;
  }

  /**
   * Get website statistics
   */
  static async getSiteStats(): Promise<SiteStats> {
    const response = await apiClient.get<{ data: SiteStats }>(HOME_ENDPOINTS.STATS);
    return response.data.data;
  }

  /**
   * Get category statistics
   */
  static async getCategoryStats(includeEmpty: boolean = false): Promise<CategorySummary[]> {
    const response = await apiClient.get<ListResponse<CategorySummary>>(
      HOME_ENDPOINTS.CATEGORIES,
      { params: { includeEmpty } }
    );
    return response.data.data;
  }

  /**
   * Get tag statistics
   */
  static async getTagStats(count: number = 50, minUsage: number = 1): Promise<TagSummary[]> {
    const response = await apiClient.get<ListResponse<TagSummary>>(
      HOME_ENDPOINTS.TAGS,
      { params: { count, minUsage } }
    );
    return response.data.data;
  }

  /**
   * Get active authors
   */
  static async getActiveAuthors(count: number = 10): Promise<AuthorSummary[]> {
    const response = await apiClient.get<ListResponse<AuthorSummary>>(
      HOME_ENDPOINTS.AUTHORS,
      { params: { count } }
    );
    return response.data.data;
  }

  /**
   * Record user interaction
   */
  static async recordInteraction(interaction: UserInteraction): Promise<void> {
    await apiClient.post(HOME_ENDPOINTS.INTERACTION, {
      postId: interaction.postId,
      interactionType: interaction.interactionType,
      duration: interaction.duration,
    });
  }

  /**
   * Refresh home page cache (admin only)
   */
  static async refreshCache(): Promise<void> {
    await apiClient.post(HOME_ENDPOINTS.REFRESH_CACHE);
  }
}

// Custom hooks for home page data fetching

/**
 * Hook to fetch home page data with intelligent caching
 */
export function useHomePageData(
  options?: Omit<UseQueryOptions<HomePageData, ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.homePage(),
    queryFn: HomeApiService.getHomePageData,
    staleTime: CACHE_TIMES.homePage,
    gcTime: CACHE_TIMES.homePage * 2,
    retry: 2,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    refetchOnWindowFocus: false,
    refetchOnReconnect: 'always',
    ...options,
  });
}

/**
 * Hook to fetch featured posts
 */
export function useFeaturedPosts(
  count: number = 5,
  options?: Omit<UseQueryOptions<PostSummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.featuredPosts(count),
    queryFn: () => HomeApiService.getFeaturedPosts(count),
    staleTime: CACHE_TIMES.featuredPosts,
    gcTime: CACHE_TIMES.featuredPosts * 2,
    retry: 2,
    enabled: count > 0 && count <= 10,
    ...options,
  });
}

/**
 * Hook to fetch latest posts
 */
export function useLatestPosts(
  count: number = 10,
  options?: Omit<UseQueryOptions<PostSummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.latestPosts(count),
    queryFn: () => HomeApiService.getLatestPosts(count),
    staleTime: CACHE_TIMES.latestPosts,
    gcTime: CACHE_TIMES.latestPosts * 2,
    retry: 2,
    enabled: count > 0 && count <= 20,
    refetchInterval: 5 * 60 * 1000, // Refetch every 5 minutes for latest content
    ...options,
  });
}

/**
 * Hook to fetch popular posts
 */
export function usePopularPosts(
  count: number = 10,
  daysBack: number = 30,
  options?: Omit<UseQueryOptions<PostSummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.popularPosts(count, daysBack),
    queryFn: () => HomeApiService.getPopularPosts(count, daysBack),
    staleTime: CACHE_TIMES.popularPosts,
    gcTime: CACHE_TIMES.popularPosts * 2,
    retry: 2,
    enabled: count > 0 && count <= 20 && daysBack > 0 && daysBack <= 365,
    ...options,
  });
}

/**
 * Hook to fetch personalized recommendations (requires authentication)
 */
export function usePersonalizedRecommendations(
  userId: string | null,
  count: number = 10,
  options?: Omit<UseQueryOptions<PostSummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: userId ? HOME_QUERY_KEYS.recommendations(userId, count) : [],
    queryFn: () => HomeApiService.getPersonalizedRecommendations(count),
    staleTime: CACHE_TIMES.recommendations,
    gcTime: CACHE_TIMES.recommendations * 2,
    retry: 2,
    enabled: !!userId && count > 0 && count <= 20,
    ...options,
  });
}

/**
 * Hook to fetch site statistics
 */
export function useSiteStats(
  options?: Omit<UseQueryOptions<SiteStats, ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.stats(),
    queryFn: HomeApiService.getSiteStats,
    staleTime: CACHE_TIMES.stats,
    gcTime: CACHE_TIMES.stats * 2,
    retry: 2,
    ...options,
  });
}

/**
 * Hook to fetch category statistics
 */
export function useCategoryStats(
  includeEmpty: boolean = false,
  options?: Omit<UseQueryOptions<CategorySummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.categories(includeEmpty),
    queryFn: () => HomeApiService.getCategoryStats(includeEmpty),
    staleTime: CACHE_TIMES.categories,
    gcTime: CACHE_TIMES.categories * 2,
    retry: 2,
    ...options,
  });
}

/**
 * Hook to fetch tag statistics
 */
export function useTagStats(
  count: number = 50,
  minUsage: number = 1,
  options?: Omit<UseQueryOptions<TagSummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.tags(count, minUsage),
    queryFn: () => HomeApiService.getTagStats(count, minUsage),
    staleTime: CACHE_TIMES.tags,
    gcTime: CACHE_TIMES.tags * 2,
    retry: 2,
    enabled: count > 0 && count <= 100 && minUsage >= 1,
    ...options,
  });
}

/**
 * Hook to fetch active authors
 */
export function useActiveAuthors(
  count: number = 10,
  options?: Omit<UseQueryOptions<AuthorSummary[], ApiErrorResponse>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: HOME_QUERY_KEYS.authors(count),
    queryFn: () => HomeApiService.getActiveAuthors(count),
    staleTime: CACHE_TIMES.authors,
    gcTime: CACHE_TIMES.authors * 2,
    retry: 2,
    enabled: count > 0 && count <= 20,
    ...options,
  });
}

// Mutation hooks for user interactions

/**
 * Hook to record user interactions
 */
export function useRecordInteraction(
  options?: UseMutationOptions<void, ApiErrorResponse, UserInteraction>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: HomeApiService.recordInteraction,
    onSuccess: (_, variables) => {
      // Invalidate related queries to update recommendations
      const userId = localStorage.getItem('userId'); // Adjust based on your auth implementation
      if (userId) {
        queryClient.invalidateQueries({
          queryKey: HOME_QUERY_KEYS.recommendations(userId, 10),
        });
      }

      // Update local post data if available
      queryClient.setQueriesData<PostSummary[]>(
        { predicate: (query) => query.queryKey.includes('home') },
        (oldData) => {
          if (!oldData) return oldData;
          return oldData.map(post =>
            post.id === variables.postId
              ? { ...post, viewCount: post.viewCount + (variables.interactionType === 'view' ? 1 : 0) }
              : post
          );
        }
      );
    },
    ...options,
  });
}

/**
 * Hook to refresh home page cache (admin only)
 */
export function useRefreshCache(
  options?: UseMutationOptions<void, ApiErrorResponse, void>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: HomeApiService.refreshCache,
    onSuccess: () => {
      // Invalidate all home-related queries
      queryClient.invalidateQueries({ queryKey: ['home'] });
    },
    ...options,
  });
}

// Utility functions for cache management

/**
 * Prefetch home page data for faster loading
 */
export function prefetchHomePageData(queryClient: ReturnType<typeof useQueryClient>) {
  return queryClient.prefetchQuery({
    queryKey: HOME_QUERY_KEYS.homePage(),
    queryFn: HomeApiService.getHomePageData,
    staleTime: CACHE_TIMES.homePage,
  });
}

/**
 * Prefetch featured posts
 */
export function prefetchFeaturedPosts(
  queryClient: ReturnType<typeof useQueryClient>,
  count: number = 5
) {
  return queryClient.prefetchQuery({
    queryKey: HOME_QUERY_KEYS.featuredPosts(count),
    queryFn: () => HomeApiService.getFeaturedPosts(count),
    staleTime: CACHE_TIMES.featuredPosts,
  });
}

/**
 * Invalidate all home-related caches
 */
export function invalidateHomeCaches(queryClient: ReturnType<typeof useQueryClient>) {
  return queryClient.invalidateQueries({ queryKey: ['home'] });
}

/**
 * Clear all home-related caches
 */
export function clearHomeCaches(queryClient: ReturnType<typeof useQueryClient>) {
  return queryClient.removeQueries({ queryKey: ['home'] });
}

/**
 * Get cached home page data without triggering a request
 */
export function getCachedHomePageData(
  queryClient: ReturnType<typeof useQueryClient>
): HomePageData | undefined {
  return queryClient.getQueryData(HOME_QUERY_KEYS.homePage());
}

/**
 * Update cached post data optimistically
 */
export function updateCachedPostData(
  queryClient: ReturnType<typeof useQueryClient>,
  postId: string,
  updates: Partial<PostSummary>
) {
  queryClient.setQueriesData<PostSummary[]>(
    { predicate: (query) => query.queryKey.includes('home') && Array.isArray(query.queryKey) },
    (oldData) => {
      if (!oldData) return oldData;
      return oldData.map(post =>
        post.id === postId ? { ...post, ...updates } : post
      );
    }
  );
}

// Error boundary helpers

/**
 * Check if error is a network error
 */
export function isNetworkError(error: unknown): error is ApiErrorResponse {
  return (
    typeof error === 'object' &&
    error !== null &&
    'success' in error &&
    error.success === false
  );
}

/**
 * Get user-friendly error message
 */
export function getErrorMessage(error: unknown, fallback: string = 'An unexpected error occurred'): string {
  if (isNetworkError(error)) {
    return error.message || fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

export { HomeApiService };