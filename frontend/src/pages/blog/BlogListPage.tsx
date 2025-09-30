/**
 * BlogListPage - 完整功能的博客文章列表页面
 * 提供搜索、筛选、排序、分页、多种布局模式等完整功能
 */

import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Search,
  Filter,
  Grid3X3,
  List,
  LayoutGrid,
  RefreshCw,
  Tag,
  Folder,
  ChevronDown,
  X,
} from 'lucide-react';
import { Helmet } from '@/components/common/DocumentHead';
import PostList from '@/components/blog/PostList';
import FeaturedPosts from '@/components/home/FeaturedPosts';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { useBlogPostQueries } from '@/services/blog/blogApi';
import { useDebounce } from '@/hooks/useDebounce';
import { cn } from '@/utils/cn';
import type { PostSearchParams } from '@/types/blog';
import { PostStatus } from '@/types/blog';

type ViewMode = 'grid' | 'list' | 'compact';
type SortOption = 'publishedAt' | 'updatedAt' | 'title' | 'viewCount' | 'likeCount' | 'commentCount';

interface FilterState {
  categories: string[];
  tags: string[];
  authors: string[];
  status: PostStatus[];
  dateFrom?: string;
  dateTo?: string;
  isFeatured?: boolean;
  isSticky?: boolean;
}

export const BlogListPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  // UI State
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<SortOption>('publishedAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [showFilters, setShowFilters] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(12);

  // Filter State
  const [filters, setFilters] = useState<FilterState>({
    categories: [],
    tags: [],
    authors: [],
    status: [PostStatus.Published],
  });

  // Debounced search query
  const debouncedSearchQuery = useDebounce(searchQuery, 300);

  // Build search parameters
  const searchParameters: PostSearchParams = useMemo(() => ({
    query: debouncedSearchQuery,
    page: currentPage,
    pageSize,
    sortBy: sortBy as 'title' | 'publishedAt' | 'createdAt' | 'updatedAt' | 'viewCount' | 'likeCount',
    sortOrder,
    status: filters.status,
    categoryId: filters.categories[0], // For simplicity, take first category
    tagIds: filters.tags,
    authorId: filters.authors[0], // For simplicity, take first author
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    isFeatured: filters.isFeatured,
    isSticky: filters.isSticky,
  }), [
    debouncedSearchQuery,
    currentPage,
    pageSize,
    sortBy,
    sortOrder,
    filters,
  ]);

  // API queries
  const { usePostsList } = useBlogPostQueries();
  const {
    data: postsResponse,
    isLoading,
    error,
    refetch,
  } = usePostsList(searchParameters, true);

  // Initialize from URL parameters
  useEffect(() => {
    const query = searchParams.get('q') || '';
    const view = (searchParams.get('view') as ViewMode) || 'grid';
    const sort = (searchParams.get('sort') as SortOption) || 'publishedAt';
    const order = (searchParams.get('order') as 'asc' | 'desc') || 'desc';
    const page = parseInt(searchParams.get('page') || '1', 10);
    const size = parseInt(searchParams.get('size') || '12', 10);

    setSearchQuery(query);
    setViewMode(view);
    setSortBy(sort);
    setSortOrder(order);
    setCurrentPage(page);
    setPageSize(size);
  }, [searchParams]);

  // Update URL when parameters change
  const updateURLParams = useCallback((updates: Record<string, string | number | undefined>) => {
    const newParams = new URLSearchParams(searchParams);

    Object.entries(updates).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        newParams.set(key, value.toString());
      } else {
        newParams.delete(key);
      }
    });

    setSearchParams(newParams);
  }, [searchParams, setSearchParams]);

  // Handle search
  const handleSearch = useCallback((query: string) => {
    setSearchQuery(query);
    setCurrentPage(1);
    updateURLParams({ q: query, page: 1 });
  }, [updateURLParams]);

  // Handle view mode change
  const handleViewModeChange = useCallback((mode: ViewMode) => {
    setViewMode(mode);
    updateURLParams({ view: mode });
  }, [updateURLParams]);

  // Handle sort change
  const handleSortChange = useCallback((newSortBy: SortOption, newSortOrder?: 'asc' | 'desc') => {
    setSortBy(newSortBy);
    if (newSortOrder) setSortOrder(newSortOrder);
    setCurrentPage(1);
    updateURLParams({ sort: newSortBy, order: newSortOrder || sortOrder, page: 1 });
  }, [sortOrder, updateURLParams]);

  // Handle pagination
  const handlePageChange = useCallback((page: number) => {
    setCurrentPage(page);
    updateURLParams({ page });
    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, [updateURLParams]);

  // Handle filter change
  const handleFilterChange = useCallback((filterType: keyof FilterState, value: string[] | PostStatus[] | string | boolean | undefined) => {
    setFilters(prev => ({ ...prev, [filterType]: value }));
    setCurrentPage(1);
  }, []);

  // Clear all filters
  const handleClearFilters = useCallback(() => {
    setFilters({
      categories: [],
      tags: [],
      authors: [],
      status: [PostStatus.Published],
    });
    setSearchQuery('');
    setCurrentPage(1);
    updateURLParams({ q: undefined, page: 1 });
  }, [updateURLParams]);

  // Get active filters count
  const activeFiltersCount = useMemo(() => {
    let count = 0;
    if (filters.categories.length > 0) count++;
    if (filters.tags.length > 0) count++;
    if (filters.authors.length > 0) count++;
    if (filters.dateFrom || filters.dateTo) count++;
    if (filters.isFeatured !== undefined) count++;
    if (filters.isSticky !== undefined) count++;
    return count;
  }, [filters]);

  // Get posts data with transformation for PostList component
  const postsData = (postsResponse?.items || []).map(post => ({
    ...post,
    authorName: post.author?.displayName || post.author?.userName || 'Unknown Author',
    status: post.status, // Keep as PostStatus enum
    tags: post.tags // Keep as Tag[]
  }));
  const totalCount = postsResponse?.totalCount || 0;
  const totalPages = postsResponse?.totalPages || 1;
  const hasNextPage = postsResponse?.hasNextPage || false;
  const hasPreviousPage = postsResponse?.hasPreviousPage || false;

  return (
    <>
      <Helmet>
        <title>{searchQuery ? `搜索: ${searchQuery}` : '博客文章'} - Maple Blog</title>
        <meta
          name="description"
          content={searchQuery
            ? `搜索结果: ${searchQuery} - 发现相关的博客文章和见解`
            : '浏览所有博客文章，发现有趣的内容和见解。支持搜索、筛选和多种浏览模式。'
          }
        />
        <meta name="keywords" content="博客,文章,搜索,筛选,分类,标签" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-7xl mx-auto">
            {/* Page Header */}
            <div className="mb-8">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <div>
                  <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                    {searchQuery ? `搜索: ${searchQuery}` : '博客文章'}
                  </h1>
                  <p className="text-lg text-gray-600 dark:text-gray-400">
                    {searchQuery
                      ? `找到 ${totalCount} 篇相关文章`
                      : `共 ${totalCount} 篇文章，探索优质内容`
                    }
                  </p>
                </div>

                {/* Quick Actions */}
                <div className="flex items-center space-x-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => refetch()}
                    disabled={isLoading}
                  >
                    <RefreshCw className={cn('w-4 h-4 mr-2', isLoading && 'animate-spin')} />
                    刷新
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowFilters(!showFilters)}
                  >
                    <Filter className="w-4 h-4 mr-2" />
                    筛选 {activeFiltersCount > 0 && `(${activeFiltersCount})`}
                  </Button>
                </div>
              </div>
            </div>

            {/* Search and Controls Bar */}
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-4 mb-6">
              <div className="flex flex-col lg:flex-row gap-4">
                {/* Search Input */}
                <div className="flex-1">
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                    <Input
                      type="text"
                      placeholder="搜索文章标题、内容或标签..."
                      value={searchQuery}
                      onChange={(e) => handleSearch(e.target.value)}
                      className="pl-10 pr-4"
                    />
                    {searchQuery && (
                      <button
                        onClick={() => handleSearch('')}
                        className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                </div>

                {/* View Mode Switcher */}
                <div className="flex items-center bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
                  <button
                    onClick={() => handleViewModeChange('grid')}
                    className={cn(
                      'p-2 rounded-md transition-colors',
                      viewMode === 'grid'
                        ? 'bg-white dark:bg-gray-700 text-blue-600 shadow-sm'
                        : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                    )}
                    title="网格视图"
                  >
                    <Grid3X3 className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleViewModeChange('list')}
                    className={cn(
                      'p-2 rounded-md transition-colors',
                      viewMode === 'list'
                        ? 'bg-white dark:bg-gray-700 text-blue-600 shadow-sm'
                        : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                    )}
                    title="列表视图"
                  >
                    <List className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleViewModeChange('compact')}
                    className={cn(
                      'p-2 rounded-md transition-colors',
                      viewMode === 'compact'
                        ? 'bg-white dark:bg-gray-700 text-blue-600 shadow-sm'
                        : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                    )}
                    title="紧凑视图"
                  >
                    <LayoutGrid className="w-4 h-4" />
                  </button>
                </div>

                {/* Sort Dropdown */}
                <div className="relative">
                  <select
                    value={`${sortBy}-${sortOrder}`}
                    onChange={(e) => {
                      const [newSortBy, newSortOrder] = e.target.value.split('-') as [SortOption, 'asc' | 'desc'];
                      handleSortChange(newSortBy, newSortOrder);
                    }}
                    className="appearance-none bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md px-4 py-2 pr-8 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="publishedAt-desc">最新发布</option>
                    <option value="publishedAt-asc">最早发布</option>
                    <option value="updatedAt-desc">最近更新</option>
                    <option value="viewCount-desc">最多浏览</option>
                    <option value="likeCount-desc">最多点赞</option>
                    <option value="commentCount-desc">最多评论</option>
                    <option value="title-asc">标题 A-Z</option>
                    <option value="title-desc">标题 Z-A</option>
                  </select>
                  <ChevronDown className="absolute right-2 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                </div>
              </div>

              {/* Active Filters Display */}
              {activeFiltersCount > 0 && (
                <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-sm text-gray-600 dark:text-gray-400">已应用筛选:</span>

                    {filters.categories.map(category => (
                      <span key={category} className="inline-flex items-center bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 text-xs px-2 py-1 rounded-full">
                        <Folder className="w-3 h-3 mr-1" />
                        {category}
                        <button
                          onClick={() => handleFilterChange('categories', filters.categories.filter(c => c !== category))}
                          className="ml-1 hover:text-blue-600"
                        >
                          <X className="w-3 h-3" />
                        </button>
                      </span>
                    ))}

                    {filters.tags.map(tag => (
                      <span key={tag} className="inline-flex items-center bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 text-xs px-2 py-1 rounded-full">
                        <Tag className="w-3 h-3 mr-1" />
                        {tag}
                        <button
                          onClick={() => handleFilterChange('tags', filters.tags.filter(t => t !== tag))}
                          className="ml-1 hover:text-green-600"
                        >
                          <X className="w-3 h-3" />
                        </button>
                      </span>
                    ))}

                    <button
                      onClick={handleClearFilters}
                      className="text-xs text-gray-500 hover:text-gray-700 underline"
                    >
                      清除所有筛选
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Advanced Filters Panel */}
            {showFilters && (
              <div className="mb-6">
                <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                    {/* Status Filter */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        发布状态
                      </label>
                      <select
                        multiple
                        value={filters.status.map(s => s.toString())}
                        onChange={(e) => {
                          const values = Array.from(e.target.selectedOptions, option => parseInt(option.value) as PostStatus);
                          handleFilterChange('status', values);
                        }}
                        className="w-full border border-gray-300 dark:border-gray-600 rounded-md px-3 py-2 bg-white dark:bg-gray-800 text-sm"
                      >
                        <option value={PostStatus.Published}>已发布</option>
                        <option value={PostStatus.Draft}>草稿</option>
                        <option value={PostStatus.Archived}>已归档</option>
                      </select>
                    </div>

                    {/* Date Range Filter */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        发布日期
                      </label>
                      <div className="space-y-2">
                        <input
                          type="date"
                          value={filters.dateFrom || ''}
                          onChange={(e) => handleFilterChange('dateFrom', e.target.value || undefined)}
                          className="w-full border border-gray-300 dark:border-gray-600 rounded-md px-3 py-2 bg-white dark:bg-gray-800 text-sm"
                          placeholder="开始日期"
                        />
                        <input
                          type="date"
                          value={filters.dateTo || ''}
                          onChange={(e) => handleFilterChange('dateTo', e.target.value || undefined)}
                          className="w-full border border-gray-300 dark:border-gray-600 rounded-md px-3 py-2 bg-white dark:bg-gray-800 text-sm"
                          placeholder="结束日期"
                        />
                      </div>
                    </div>

                    {/* Special Filters */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        特殊筛选
                      </label>
                      <div className="space-y-2">
                        <label className="flex items-center">
                          <input
                            type="checkbox"
                            checked={filters.isFeatured === true}
                            onChange={(e) => handleFilterChange('isFeatured', e.target.checked ? true : undefined)}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                          <span className="ml-2 text-sm text-gray-700 dark:text-gray-300">精选文章</span>
                        </label>
                        <label className="flex items-center">
                          <input
                            type="checkbox"
                            checked={filters.isSticky === true}
                            onChange={(e) => handleFilterChange('isSticky', e.target.checked ? true : undefined)}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                          <span className="ml-2 text-sm text-gray-700 dark:text-gray-300">置顶文章</span>
                        </label>
                      </div>
                    </div>

                    {/* Page Size */}
                    <div>
                      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                        每页显示
                      </label>
                      <select
                        value={pageSize}
                        onChange={(e) => {
                          const newSize = parseInt(e.target.value, 10);
                          setPageSize(newSize);
                          setCurrentPage(1);
                          updateURLParams({ size: newSize, page: 1 });
                        }}
                        className="w-full border border-gray-300 dark:border-gray-600 rounded-md px-3 py-2 bg-white dark:bg-gray-800 text-sm"
                      >
                        <option value="6">6 篇</option>
                        <option value="12">12 篇</option>
                        <option value="24">24 篇</option>
                        <option value="48">48 篇</option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Featured Posts Section (only on first page without search) */}
            {!searchQuery && currentPage === 1 && (
              <div className="mb-8">
                <FeaturedPosts
                  count={4}
                  layout="grid"
                  title="编辑推荐"
                  className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6"
                />
              </div>
            )}

            {/* Main Content */}
            <div className="space-y-6">
              {error ? (
                <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-8 text-center">
                  <div className="text-red-600 dark:text-red-400 mb-4">
                    <Search className="w-12 h-12 mx-auto mb-4" />
                    <h3 className="text-lg font-medium mb-2">加载失败</h3>
                    <p className="text-sm">{error instanceof Error ? error.message : '获取文章列表时出现错误'}</p>
                  </div>
                  <Button onClick={() => refetch()} variant="outline" size="sm">
                    <RefreshCw className="w-4 h-4 mr-2" />
                    重新加载
                  </Button>
                </div>
              ) : postsData.length === 0 && !isLoading ? (
                <div className="bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 rounded-lg p-12 text-center">
                  <Search className="w-16 h-16 mx-auto text-gray-400 mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                    {searchQuery ? '没有找到相关文章' : '暂无文章'}
                  </h3>
                  <p className="text-gray-500 dark:text-gray-400 mb-6">
                    {searchQuery
                      ? '尝试使用其他关键词或调整筛选条件'
                      : '还没有发布任何文章，请稍后再来查看'
                    }
                  </p>
                  {searchQuery && (
                    <div className="space-x-4">
                      <Button onClick={() => handleSearch('')} variant="outline">
                        查看所有文章
                      </Button>
                      <Button onClick={handleClearFilters} variant="ghost">
                        清除筛选条件
                      </Button>
                    </div>
                  )}
                </div>
              ) : (
                <>
                  {/* Posts List */}
                  <PostList
                    posts={postsData}
                    loading={isLoading}
                    error={error && typeof error === 'object' && 'message' in error ? (error as Error).message : null}
                    layout={viewMode}
                    showAuthor
                    showCategory
                    showTags
                    showStats
                    showActions
                    className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6"
                    data-testid="blog-posts-list"
                  />

                  {/* Pagination */}
                  {totalPages > 1 && (
                    <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
                      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                        <div className="text-sm text-gray-600 dark:text-gray-400">
                          显示第 {(currentPage - 1) * pageSize + 1} - {Math.min(currentPage * pageSize, totalCount)} 条，共 {totalCount} 条结果
                        </div>

                        <div className="flex items-center space-x-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handlePageChange(currentPage - 1)}
                            disabled={!hasPreviousPage || isLoading}
                          >
                            上一页
                          </Button>

                          <div className="flex items-center space-x-1">
                            {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                              let pageNumber;
                              if (totalPages <= 5) {
                                pageNumber = i + 1;
                              } else if (currentPage <= 3) {
                                pageNumber = i + 1;
                              } else if (currentPage >= totalPages - 2) {
                                pageNumber = totalPages - 4 + i;
                              } else {
                                pageNumber = currentPage - 2 + i;
                              }

                              return (
                                <Button
                                  key={pageNumber}
                                  variant={currentPage === pageNumber ? 'default' : 'ghost'}
                                  size="sm"
                                  onClick={() => handlePageChange(pageNumber)}
                                  disabled={isLoading}
                                  className="min-w-[36px]"
                                >
                                  {pageNumber}
                                </Button>
                              );
                            })}
                          </div>

                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => handlePageChange(currentPage + 1)}
                            disabled={!hasNextPage || isLoading}
                          >
                            下一页
                          </Button>
                        </div>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default BlogListPage;