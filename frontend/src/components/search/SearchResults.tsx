/**
 * SearchResults Component
 * 搜索结果展示组件 - 显示搜索结果列表和分页
 */

import React, { useState, useEffect, useCallback } from 'react';
import {
  Search,
  SortAsc,
  SortDesc,
  Grid,
  List,
  Loader,
  AlertCircle,
  RefreshCw,
  Eye,
  Clock,
  TrendingUp,
} from 'lucide-react';
import { useSearchStore } from '@/stores/searchStore';
import { SearchResult, SortOption } from '@/types/search';
import SearchResultCard from './SearchResultCard';
import { searchApi } from '@/services/search/searchApi';

interface SearchResultsProps {
  className?: string;
  showStats?: boolean;
  showViewToggle?: boolean;
  showSortOptions?: boolean;
  onResultClick?: (result: SearchResult) => void;
  virtualScrolling?: boolean;
}

function SearchResults({
  className = '',
  showStats = true,
  showViewToggle = true,
  showSortOptions = true,
  onResultClick,
  virtualScrolling = false,
}: SearchResultsProps) {
  const {
    query,
    results,
    totalCount,
    loading,
    error,
    hasMore,
    page: _page,
    sortBy,
    sortDirection,
    searchTime,
    resultStats,
    setSortBy,
    loadMore,
    retry,
  } = useSearchStore();

  const [viewMode, setViewMode] = useState<'list' | 'grid'>('list');
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  // 处理结果点击
  const handleResultClick = useCallback(
    async (result: SearchResult, clickPosition: number) => {
      // 记录点击分析
      try {
        await searchApi.recordSearchAnalytics(
          query,
          totalCount,
          result.id,
          clickPosition
        );
      } catch (error) {
        console.warn('Failed to record search analytics:', error);
      }

      onResultClick?.(result);
    },
    [query, totalCount, onResultClick]
  );

  // 处理排序变化
  const handleSortChange = (newSortBy: SortOption) => {
    const newDirection = sortBy === newSortBy && sortDirection === 'desc' ? 'asc' : 'desc';
    setSortBy(newSortBy, newDirection);
  };

  // 加载更多结果
  const handleLoadMore = useCallback(async () => {
    if (isLoadingMore || !hasMore) return;

    setIsLoadingMore(true);
    try {
      await loadMore();
    } finally {
      setIsLoadingMore(false);
    }
  }, [isLoadingMore, hasMore, loadMore]);

  // 无限滚动
  useEffect(() => {
    if (!virtualScrolling || !hasMore) return;

    const handleScroll = () => {
      const { scrollTop, scrollHeight, clientHeight } = document.documentElement;

      if (scrollTop + clientHeight >= scrollHeight - 1000 && !loading && !isLoadingMore) {
        handleLoadMore();
      }
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => window.removeEventListener('scroll', handleScroll);
  }, [virtualScrolling, hasMore, loading, isLoadingMore, handleLoadMore]);

  // 排序选项
  const sortOptions: Array<{ value: SortOption; label: string; icon?: React.ComponentType<{ className?: string }> }> = [
    { value: 'relevance', label: '相关性', icon: TrendingUp },
    { value: 'publishedAt', label: '发布时间', icon: Clock },
    { value: 'viewCount', label: '浏览量', icon: Eye },
    { value: 'title', label: '标题', icon: SortAsc },
  ];

  // 渲染统计信息
  const renderStats = () => {
    if (!showStats || (!query && results.length === 0)) return null;

    return (
      <div className="flex flex-col sm:flex-row sm:items-center justify-between py-4 border-b border-gray-200">
        <div className="flex items-center space-x-4 text-sm text-gray-600">
          {query && (
            <div className="flex items-center space-x-2">
              <Search className="h-4 w-4" />
              <span>
                搜索 &quot;<strong className="text-gray-900">{query}</strong>&quot;
                找到 <strong className="text-gray-900">{totalCount.toLocaleString()}</strong> 个结果
              </span>
            </div>
          )}

          {resultStats && (
            <div className="flex items-center space-x-4 text-xs text-gray-500">
              <span>耗时 {resultStats.took}ms</span>
              <span>最高分 {resultStats.maxScore.toFixed(2)}</span>
            </div>
          )}
        </div>

        {/* 视图和排序控制 */}
        <div className="flex items-center space-x-2 mt-2 sm:mt-0">
          {/* 视图切换 */}
          {showViewToggle && (
            <div className="flex items-center bg-gray-100 rounded-lg p-1">
              <button
                onClick={() => setViewMode('list')}
                className={`p-1 rounded ${
                  viewMode === 'list'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
                title="列表视图"
              >
                <List className="h-4 w-4" />
              </button>
              <button
                onClick={() => setViewMode('grid')}
                className={`p-1 rounded ${
                  viewMode === 'grid'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
                title="网格视图"
              >
                <Grid className="h-4 w-4" />
              </button>
            </div>
          )}

          {/* 排序选择 */}
          {showSortOptions && (
            <div className="relative">
              <select
                value={sortBy}
                onChange={(e) => handleSortChange(e.target.value as SortOption)}
                className="appearance-none bg-white border border-gray-300 rounded-md px-3 py-1 pr-8 text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
              >
                {sortOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>

              <button
                onClick={() => setSortBy(sortBy, sortDirection === 'desc' ? 'asc' : 'desc')}
                className="absolute right-2 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
                title={`${sortDirection === 'desc' ? '降序' : '升序'}排列`}
              >
                {sortDirection === 'desc' ? (
                  <SortDesc className="h-4 w-4" />
                ) : (
                  <SortAsc className="h-4 w-4" />
                )}
              </button>
            </div>
          )}
        </div>
      </div>
    );
  };

  // 渲染加载状态
  const renderLoading = () => (
    <div className="flex items-center justify-center py-12">
      <div className="flex items-center space-x-3">
        <Loader className="h-6 w-6 animate-spin text-blue-500" />
        <span className="text-gray-600">搜索中...</span>
      </div>
    </div>
  );

  // 渲染错误状态
  const renderError = () => (
    <div className="flex flex-col items-center justify-center py-12">
      <AlertCircle className="h-12 w-12 text-red-500 mb-4" />
      <h3 className="text-lg font-medium text-gray-900 mb-2">搜索失败</h3>
      <p className="text-gray-600 mb-4 text-center max-w-md">
        {error || '搜索时发生错误，请稍后重试'}
      </p>
      <button
        onClick={retry}
        className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
      >
        <RefreshCw className="h-4 w-4" />
        <span>重试</span>
      </button>
    </div>
  );

  // 渲染空状态
  const renderEmpty = () => {
    if (!query) {
      return (
        <div className="flex flex-col items-center justify-center py-12">
          <Search className="h-12 w-12 text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">开始搜索</h3>
          <p className="text-gray-600 text-center max-w-md">
            输入关键词开始搜索文章、分类、标签或作者
          </p>
        </div>
      );
    }

    return (
      <div className="flex flex-col items-center justify-center py-12">
        <Search className="h-12 w-12 text-gray-400 mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">没有找到相关结果</h3>
        <p className="text-gray-600 text-center max-w-md mb-4">
          试试不同的关键词或调整筛选条件
        </p>
        <div className="text-sm text-gray-500">
          <p>搜索建议：</p>
          <ul className="mt-2 space-y-1 text-left">
            <li>• 检查拼写是否正确</li>
            <li>• 尝试使用更通用的关键词</li>
            <li>• 减少筛选条件</li>
            <li>• 使用同义词或相关词汇</li>
          </ul>
        </div>
      </div>
    );
  };

  // 渲染结果列表
  const renderResults = () => {
    if (results.length === 0) return renderEmpty();

    const gridClasses = viewMode === 'grid'
      ? 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6'
      : 'space-y-4';

    return (
      <div className={gridClasses}>
        {results.map((result, index) => (
          <SearchResultCard
            key={result.id}
            result={result}
            query={query}
            viewMode={viewMode}
            onClick={() => handleResultClick(result, index + 1)}
            showScore={sortBy === 'relevance'}
          />
        ))}
      </div>
    );
  };

  // 渲染加载更多按钮
  const renderLoadMore = () => {
    if (!hasMore || loading) return null;

    return (
      <div className="flex justify-center py-8">
        <button
          onClick={handleLoadMore}
          disabled={isLoadingMore}
          className="flex items-center space-x-2 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
        >
          {isLoadingMore ? (
            <Loader className="h-4 w-4 animate-spin" />
          ) : (
            <RefreshCw className="h-4 w-4" />
          )}
          <span>{isLoadingMore ? '加载中...' : '加载更多'}</span>
        </button>
      </div>
    );
  };

  return (
    <div className={`search-results ${className}`}>
      {renderStats()}

      <div className="mt-6">
        {loading && results.length === 0 && renderLoading()}
        {error && results.length === 0 && renderError()}
        {!loading && !error && renderResults()}

        {/* 加载更多结果指示器 */}
        {isLoadingMore && (
          <div className="flex items-center justify-center py-4">
            <Loader className="h-5 w-5 animate-spin text-blue-500 mr-2" />
            <span className="text-gray-600">加载更多结果...</span>
          </div>
        )}

        {/* 加载更多按钮（非无限滚动模式） */}
        {!virtualScrolling && renderLoadMore()}
      </div>

      {/* 结果统计 */}
      {results.length > 0 && (
        <div className="mt-8 pt-4 border-t border-gray-200 text-center text-sm text-gray-500">
          显示 {results.length} / {totalCount.toLocaleString()} 个结果
          {searchTime > 0 && (
            <span className="ml-4">搜索耗时 {Math.round(searchTime)}ms</span>
          )}
        </div>
      )}
    </div>
  );
}

// Export both named and default exports
export { SearchResults };
export default SearchResults;