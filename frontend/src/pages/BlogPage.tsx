/**
 * BlogPage - Main blog listing page with advanced filtering and search
 * Implements modern React patterns with performance optimizations
 */

import React, { useCallback, useMemo } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Helmet } from '@/components/common/DocumentHead';
import { useLogger } from '@/utils/logger';
import type { LogContext } from '@/services/loggingService';
import blogApi from '@/services/blog/blogApi';
import { analytics } from '@/services/analytics';
import { PostList } from '@/components/blog/PostList';
import { SearchFilters } from '@/components/search/SearchFilters';
import { SearchBox } from '@/components/search/SearchBox';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/Button';
import { useAuth } from '@/hooks/useAuth';
import { usePersonalization } from '@/hooks/usePersonalization';
import { useCategoryQueries } from '@/services/blog/categoryApi';
import { useTagQueries } from '@/services/blog/tagApi';
import type { BlogPost } from '@/types/blog';

interface BlogPageProps {
  className?: string;
}

interface BlogPageFilters {
  category?: string;
  tag?: string;
  search?: string;
  sortBy?: 'date' | 'views' | 'likes' | 'comments';
  sortOrder?: 'asc' | 'desc';
  page?: number;
}

const POSTS_PER_PAGE = 12;

export const BlogPage: React.FC<BlogPageProps> = ({ className = '' }) => {
  const logger = useLogger('BlogPage');
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { user } = useAuth();
  const { preferences, updatePreferences } = usePersonalization();

  // Parse URL parameters
  const filters = useMemo((): BlogPageFilters => ({
    category: searchParams.get('category') || undefined,
    tag: searchParams.get('tag') || undefined,
    search: searchParams.get('search') || undefined,
    sortBy: (searchParams.get('sortBy') as BlogPageFilters['sortBy']) || 'date',
    sortOrder: (searchParams.get('sortOrder') as BlogPageFilters['sortOrder']) || 'desc',
    page: parseInt(searchParams.get('page') || '1', 10)
  }), [searchParams]);


  // Log page view
  React.useEffect(() => {
    logger.logUserAction('page_view', 'blog_page', {
      filters: filters as unknown as LogContext['filters'],
      user_id: user?.id
    });

    analytics.track('page_view', 'BlogInterface');
  }, [logger, filters, user]);

  // Fetch blog posts
  const {
    data: postsData,
    isLoading: postsLoading,
    error: postsError,
    refetch: refetchPosts
  } = useQuery({
    queryKey: ['blog-posts', filters],
    queryFn: async () => {
      logger.startTimer('fetch_posts');
      
      try {
        const result = await blogApi.getPosts({
          query: filters.search,
          categoryId: selectedCategory?.id,
          tagIds: selectedTag ? [selectedTag.id] : undefined,
          sortBy: filters.sortBy === 'date' ? 'publishedAt' : 
                  filters.sortBy === 'views' ? 'viewCount' :
                  filters.sortBy === 'comments' ? 'createdAt' :
                  filters.sortBy === 'likes' ? 'likeCount' : 
                  filters.sortBy,
          sortOrder: filters.sortOrder,
          page: filters.page,
          pageSize: POSTS_PER_PAGE
        });
        
        logger.endTimer('fetch_posts');

        return result;
      } catch (error) {
        logger.logApiError('GET', '/api/posts', error as Error, { filters: filters as unknown as LogContext['filters'] });
        throw error;
      }
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    retry: 3
  });

  const { useCategoriesList } = useCategoryQueries();
  const { data: categoryListResponse } = useCategoriesList(
    {
      page: 1,
      pageSize: 100,
      sortBy: 'sortOrder',
      sortOrder: 'asc',
      isActive: true,
    },
    true,
  );
  const categories = React.useMemo(() => categoryListResponse?.items ?? [], [categoryListResponse?.items]);

  const { usePopularTags } = useTagQueries();
  const { data: popularTags = [] } = usePopularTags(30, true);

  const selectedCategory = useMemo(
    () => categories.find(cat => cat.slug === filters.category || cat.id === filters.category),
    [categories, filters.category],
  );

  const selectedTag = useMemo(
    () => popularTags.find(tag => tag.slug === filters.tag || tag.id === filters.tag),
    [popularTags, filters.tag],
  );

  const categoryFilterOptions = useMemo(
    () =>
      categories.map(cat => ({
        name: cat.name,
        value: cat.slug,
        count: cat.postCount,
      })),
    [categories],
  );

  const tagFilterOptions = useMemo(
    () =>
      popularTags.map(tag => ({
        name: tag.name,
        value: tag.slug,
        count: tag.postCount,
      })),
    [popularTags],
  );

  // Update URL parameters
  const updateFilters = useCallback((newFilters: Partial<BlogPageFilters>) => {
    const updatedParams = new URLSearchParams(searchParams);
    
    Object.entries(newFilters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        updatedParams.set(key, value.toString());
      } else {
        updatedParams.delete(key);
      }
    });

    // Reset to page 1 when changing filters (except page itself)
    if (!newFilters.page && Object.keys(newFilters).length > 0) {
      updatedParams.set('page', '1');
    }

    setSearchParams(updatedParams);

    logger.logUserAction('filter_change', 'blog_filters', {
      old_filters: filters as unknown as LogContext['old_filters'],
      new_filters: newFilters as unknown as LogContext['new_filters']
    });

    // Update user preferences if logged in
    if (user && (newFilters.sortBy || newFilters.sortOrder)) {
      updatePreferences({
        preferredCategories: preferences?.preferredCategories || [],
        preferredTags: preferences?.preferredTags || []
      });
    }
  }, [searchParams, setSearchParams, filters, logger, user, updatePreferences, preferences?.preferredCategories, preferences?.preferredTags]);

  // Handle search
  const handleSearch = useCallback((searchTerm: string) => {
    updateFilters({ search: searchTerm, page: 1 });
    
    if (searchTerm) {
      analytics.track('search', 'BlogInterface');
    }
  }, [updateFilters]);

  // Handle category selection
  const handleCategorySelect = useCallback((categorySlug?: string) => {
    updateFilters({ category: categorySlug || undefined, page: 1 });

    if (categorySlug) {
      analytics.track('category_filter', 'BlogInterface');
    }
  }, [updateFilters]);

  // Handle tag selection
  const handleTagSelect = useCallback((tagSlug?: string) => {
    updateFilters({ tag: tagSlug || undefined, page: 1 });

    if (tagSlug) {
      analytics.track('tag_filter', 'BlogInterface');
    }
  }, [updateFilters]);

  // Handle pagination
  const handlePageChange = useCallback((page: number) => {
    updateFilters({ page });

    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });

    analytics.track('pagination', 'BlogInterface');
  }, [updateFilters]);

  // Clear all filters
  const clearFilters = useCallback(() => {
    setSearchParams(new URLSearchParams());
    logger.logUserAction('clear_filters', 'blog_filters');
    analytics.track('clear_filters', 'BlogInterface');
  }, [setSearchParams, logger]);

  // Handle post interaction
  const handlePostClick = useCallback((post: BlogPost) => {
    logger.logUserAction('post_click', 'blog_post_link', {
      post_id: post.id,
      post_title: post.title,
      post_category: post.category?.name,
      click_source: 'blog_list'
    });

    analytics.track('post_view', 'BlogInterface');

    navigate(`/posts/${post.slug}`);
  }, [logger, navigate]);

  // Handle error retry
  const handleRetry = useCallback(() => {
    logger.logUserAction('retry_fetch', 'error_recovery');
    refetchPosts();
  }, [logger, refetchPosts]);

  // SEO metadata
  const seoTitle = useMemo(() => {
    const parts = ['Blog'];
    if (filters.category) {
      const category = categories.find(cat => cat.slug === filters.category);
      if (category) parts.unshift(category.name);
    }
    if (filters.search) {
      parts.unshift(`Search: ${filters.search}`);
    }
    return parts.join(' | ') + ' | Maple Blog';
  }, [filters, categories]);

  const seoDescription = useMemo(() => {
    if (filters.search) {
      return `Search results for "${filters.search}" in our blog. Find articles, tutorials, and insights.`;
    }
    if (filters.category) {
      const category = categories.find(cat => cat.slug === filters.category);
      return category 
        ? `Browse ${category.name} articles and posts. ${category.description || 'Discover insights and tutorials.'}`
        : 'Browse blog articles and posts in this category.';
    }
    return 'Explore our collection of articles, tutorials, and insights on web development, technology, and more.';
  }, [filters, categories]);

  // Render loading state
  if (postsLoading && !postsData) {
    return (
      <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
        <Helmet>
          <title>Loading Blog | Maple Blog</title>
        </Helmet>
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center justify-center min-h-[50vh]">
            <LoadingSpinner size="lg" />
          </div>
        </div>
      </div>
    );
  }

  // Render error state
  if (postsError && !postsData) {
    return (
      <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
        <Helmet>
          <title>Error Loading Blog | Maple Blog</title>
        </Helmet>
        <div className="container mx-auto px-4 py-8">
          <div className="flex flex-col items-center justify-center min-h-[50vh] text-center">
            <Alert className="mb-6 max-w-md">
              <h3 className="font-semibold mb-2">Unable to Load Blog Posts</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                We&apos;re having trouble loading the blog posts. This might be a temporary issue.
              </p>
              <Button onClick={handleRetry} className="w-full">
                Try Again
              </Button>
            </Alert>
          </div>
        </div>
      </div>
    );
  }

  const totalPages = postsData ? Math.ceil(postsData.totalCount / POSTS_PER_PAGE) : 0;
  const hasActiveFilters = filters.category || filters.tag || filters.search;

  return (
    <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
      <Helmet>
        <title>{seoTitle}</title>
        <meta name="description" content={seoDescription} />
        {filters.page && filters.page > 1 && (
          <link rel="prev" href={`/blog?${new URLSearchParams({ ...filters, page: (filters.page - 1).toString() })}`} />
        )}
        {filters.page && filters.page < totalPages && (
          <link rel="next" href={`/blog?${new URLSearchParams({ ...filters, page: (filters.page + 1).toString() })}`} />
        )}
      </Helmet>

      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl md:text-4xl font-bold text-gray-900 dark:text-white mb-4">
            {filters.category 
              ? categories.find(cat => cat.slug === filters.category)?.name || 'Category'
              : filters.search 
                ? `Search: ${filters.search}`
                : 'Blog'
            }
          </h1>
          
          {filters.search && (
            <p className="text-gray-600 dark:text-gray-400">
              {postsData?.totalCount || 0} result{postsData?.totalCount !== 1 ? 's' : ''} found
            </p>
          )}
        </div>

        {/* Search and Filters */}
        <div className="mb-8 space-y-6">
          <div className="flex flex-col lg:flex-row gap-4">
            <div className="flex-1">
              <SearchBox
                onSearch={handleSearch}
                placeholder="Search articles, tutorials, and insights..."
                className="w-full"
              />
            </div>
            
            {hasActiveFilters && (
              <Button
                variant="outline"
                onClick={clearFilters}
                className="lg:w-auto"
              >
                Clear Filters
              </Button>
            )}
          </div>

          <SearchFilters
            filters={{
              categories: filters.category ? [filters.category] : [],
              tags: filters.tag ? [filters.tag] : [],
              authors: [],
              dateFrom: undefined,
              dateTo: undefined,
              contentType: [],
              status: []
            }}
            filterOptions={{
              categories: categoryFilterOptions,
              tags: tagFilterOptions,
              authors: [],
              years: []
            }}
            onChange={(key, value) => {
              if (key === 'categories') {
                if (Array.isArray(value) && value.length > 0) {
                  handleCategorySelect(value[0]);
                } else if (Array.isArray(value) && value.length === 0) {
                  handleCategorySelect(undefined);
                }
              } else if (key === 'tags') {
                if (Array.isArray(value) && value.length > 0) {
                  handleTagSelect(value[0]);
                } else if (Array.isArray(value) && value.length === 0) {
                  handleTagSelect(undefined);
                }
              }
            }}
          />
        </div>

        {/* Posts List */}
        <PostList
          posts={postsData?.items || []}
          loading={postsLoading}
          error={postsError?.message || null}
          onPostClick={handlePostClick}
          onRetry={handleRetry}
          currentPage={filters.page || 1}
          totalPages={totalPages}
          onPageChange={handlePageChange}
          emptyStateMessage={
            hasActiveFilters 
              ? 'No posts match your current filters. Try adjusting your search criteria.'
              : 'No blog posts available yet. Check back soon!'
          }
        />
      </div>
    </div>
  );
};

export default BlogPage;
