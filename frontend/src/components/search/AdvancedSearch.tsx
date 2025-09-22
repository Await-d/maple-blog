// @ts-nocheck
/**
 * AdvancedSearch Component
 * 高级搜索组件 - 提供多维度筛选和高级搜索选项
 */

import React, { useState, useEffect } from 'react';
import {
  ChevronDown,
  ChevronUp,
  Calendar,
  User,
  Folder,
  Hash,
  Filter,
  X,
  Search,
  RefreshCw,
} from 'lucide-react';
import { useSearchStore } from '@/stores/searchStore';
import { searchApi } from '@/services/search/searchApi';
import { SearchFilters, SortOption } from '@/types/search';
import SearchFiltersComponent from './SearchFilters';

interface AdvancedSearchProps {
  className?: string;
  onSearch?: () => void;
  showToggle?: boolean;
  defaultExpanded?: boolean;
}

interface FilterOptions {
  categories: Array<{ name: string; count: number }>;
  tags: Array<{ name: string; count: number }>;
  authors: Array<{ name: string; count: number }>;
  years: Array<{ year: number; count: number }>;
}

export default function AdvancedSearch({
  className = '',
  onSearch,
  showToggle = true,
  defaultExpanded = false,
}: AdvancedSearchProps) {
  const {
    query,
    filters,
    sortBy,
    sortDirection,
    showAdvanced,
    setFilters,
    updateFilter,
    clearFilters,
    setSortBy,
    toggleAdvanced,
    search,
  } = useSearchStore();

  const [expanded, setExpanded] = useState(defaultExpanded || showAdvanced);
  const [loading, setLoading] = useState(false);
  const [filterOptions, setFilterOptions] = useState<FilterOptions>({
    categories: [],
    tags: [],
    authors: [],
    years: [],
  });

  // 同步展开状态
  useEffect(() => {
    setExpanded(showAdvanced);
  }, [showAdvanced]);

  // 加载筛选器选项
  useEffect(() => {
    const loadFilterOptions = async () => {
      try {
        setLoading(true);
        const data = await searchApi.getFilterData();
        setFilterOptions(data);
      } catch (error) {
        console.error('Failed to load filter options:', error);
      } finally {
        setLoading(false);
      }
    };

    if (expanded) {
      loadFilterOptions();
    }
  }, [expanded]);

  // 处理筛选器变化
  const handleFilterChange = (key: keyof SearchFilters, value: any) => {
    updateFilter(key, value);
  };

  // 处理排序变化
  const handleSortChange = (newSortBy: SortOption, newDirection?: 'asc' | 'desc') => {
    setSortBy(newSortBy, newDirection || sortDirection);
  };

  // 应用筛选器
  const handleApplyFilters = async () => {
    await search();
    onSearch?.();
  };

  // 重置筛选器
  const handleResetFilters = () => {
    clearFilters();
  };

  // 切换展开状态
  const toggleExpanded = () => {
    if (showToggle) {
      toggleAdvanced();
    } else {
      setExpanded(!expanded);
    }
  };

  // 获取活跃筛选器数量
  const getActiveFiltersCount = (): number => {
    let count = 0;
    if (filters.categories?.length) count++;
    if (filters.tags?.length) count++;
    if (filters.authors?.length) count++;
    if (filters.dateFrom || filters.dateTo) count++;
    if (filters.contentType?.length) count++;
    if (filters.status?.length) count++;
    return count;
  };

  const activeFiltersCount = getActiveFiltersCount();

  // 排序选项
  const sortOptions: Array<{ value: SortOption; label: string; desc?: string }> = [
    { value: 'relevance', label: '相关性', desc: '按搜索相关性排序' },
    { value: 'publishedAt', label: '发布时间', desc: '按发布时间排序' },
    { value: 'updatedAt', label: '更新时间', desc: '按更新时间排序' },
    { value: 'title', label: '标题', desc: '按标题字母顺序排序' },
    { value: 'viewCount', label: '浏览量', desc: '按浏览量排序' },
    { value: 'likeCount', label: '点赞数', desc: '按点赞数排序' },
    { value: 'commentCount', label: '评论数', desc: '按评论数排序' },
  ];

  return (
    <div className={`bg-white border border-gray-200 rounded-lg ${className}`}>
      {/* 标题栏 */}
      <div
        className="flex items-center justify-between p-4 cursor-pointer"
        onClick={toggleExpanded}
      >
        <div className="flex items-center space-x-2">
          <Filter className="h-5 w-5 text-gray-500" />
          <span className="font-medium text-gray-900">高级搜索</span>
          {activeFiltersCount > 0 && (
            <span className="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded-full">
              {activeFiltersCount} 个筛选器
            </span>
          )}
        </div>
        <div className="flex items-center space-x-2">
          {activeFiltersCount > 0 && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleResetFilters();
              }}
              className="text-gray-400 hover:text-gray-600 p-1 rounded"
              title="清除所有筛选器"
            >
              <X className="h-4 w-4" />
            </button>
          )}
          {showToggle && (
            <div className="text-gray-400">
              {expanded ? (
                <ChevronUp className="h-5 w-5" />
              ) : (
                <ChevronDown className="h-5 w-5" />
              )}
            </div>
          )}
        </div>
      </div>

      {/* 高级搜索内容 */}
      {expanded && (
        <div className="border-t border-gray-200 p-4 space-y-6">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <RefreshCw className="h-5 w-5 animate-spin text-gray-400 mr-2" />
              <span className="text-sm text-gray-500">加载筛选选项...</span>
            </div>
          ) : (
            <>
              {/* 搜索筛选器组件 */}
              <SearchFiltersComponent
                filters={filters}
                filterOptions={filterOptions}
                onChange={handleFilterChange}
              />

              {/* 排序选项 */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium text-gray-900 flex items-center">
                  <Search className="h-4 w-4 mr-2 text-gray-500" />
                  排序方式
                </h4>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  {sortOptions.map((option) => (
                    <label
                      key={option.value}
                      className={`
                        relative flex items-center p-3 border rounded-lg cursor-pointer
                        hover:bg-gray-50 transition-colors
                        ${sortBy === option.value
                          ? 'border-blue-500 bg-blue-50 text-blue-900'
                          : 'border-gray-200 text-gray-900'
                        }
                      `}
                    >
                      <input
                        type="radio"
                        name="sortBy"
                        value={option.value}
                        checked={sortBy === option.value}
                        onChange={() => handleSortChange(option.value)}
                        className="sr-only"
                      />
                      <div className="flex-1">
                        <div className="font-medium">{option.label}</div>
                        {option.desc && (
                          <div className="text-xs text-gray-500 mt-1">{option.desc}</div>
                        )}
                      </div>

                      {/* 排序方向 */}
                      {sortBy === option.value && (
                        <div className="ml-2 flex space-x-1">
                          <button
                            type="button"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleSortChange(option.value, 'desc');
                            }}
                            className={`
                              px-2 py-1 text-xs rounded
                              ${sortDirection === 'desc'
                                ? 'bg-blue-600 text-white'
                                : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                              }
                            `}
                          >
                            降序
                          </button>
                          <button
                            type="button"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleSortChange(option.value, 'asc');
                            }}
                            className={`
                              px-2 py-1 text-xs rounded
                              ${sortDirection === 'asc'
                                ? 'bg-blue-600 text-white'
                                : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                              }
                            `}
                          >
                            升序
                          </button>
                        </div>
                      )}
                    </label>
                  ))}
                </div>
              </div>

              {/* 操作按钮 */}
              <div className="flex flex-col sm:flex-row gap-3 pt-4 border-t border-gray-100">
                <button
                  onClick={handleApplyFilters}
                  disabled={!query.trim()}
                  className={`
                    flex-1 px-4 py-2 rounded-lg font-medium transition-colors
                    flex items-center justify-center space-x-2
                    ${query.trim()
                      ? 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
                      : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                    }
                  `}
                >
                  <Search className="h-4 w-4" />
                  <span>应用筛选器搜索</span>
                </button>

                <button
                  onClick={handleResetFilters}
                  disabled={activeFiltersCount === 0}
                  className={`
                    px-4 py-2 rounded-lg font-medium transition-colors
                    flex items-center justify-center space-x-2
                    ${activeFiltersCount > 0
                      ? 'bg-gray-100 text-gray-700 hover:bg-gray-200 focus:ring-2 focus:ring-gray-500 focus:ring-offset-2'
                      : 'bg-gray-50 text-gray-400 cursor-not-allowed'
                    }
                  `}
                >
                  <X className="h-4 w-4" />
                  <span>重置筛选器</span>
                </button>
              </div>

              {/* 筛选器摘要 */}
              {activeFiltersCount > 0 && (
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
                  <div className="text-sm font-medium text-blue-900 mb-2">
                    已应用的筛选器：
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {filters.categories?.map((category) => (
                      <span
                        key={`category-${category}`}
                        className="inline-flex items-center bg-purple-100 text-purple-800 text-xs font-medium px-2.5 py-0.5 rounded-full"
                      >
                        <Folder className="h-3 w-3 mr-1" />
                        {category}
                        <button
                          onClick={() => {
                            const newCategories = filters.categories?.filter(c => c !== category);
                            handleFilterChange('categories', newCategories);
                          }}
                          className="ml-1 text-purple-600 hover:text-purple-800"
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </span>
                    ))}

                    {filters.tags?.map((tag) => (
                      <span
                        key={`tag-${tag}`}
                        className="inline-flex items-center bg-orange-100 text-orange-800 text-xs font-medium px-2.5 py-0.5 rounded-full"
                      >
                        <Hash className="h-3 w-3 mr-1" />
                        {tag}
                        <button
                          onClick={() => {
                            const newTags = filters.tags?.filter(t => t !== tag);
                            handleFilterChange('tags', newTags);
                          }}
                          className="ml-1 text-orange-600 hover:text-orange-800"
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </span>
                    ))}

                    {filters.authors?.map((author) => (
                      <span
                        key={`author-${author}`}
                        className="inline-flex items-center bg-green-100 text-green-800 text-xs font-medium px-2.5 py-0.5 rounded-full"
                      >
                        <User className="h-3 w-3 mr-1" />
                        {author}
                        <button
                          onClick={() => {
                            const newAuthors = filters.authors?.filter(a => a !== author);
                            handleFilterChange('authors', newAuthors);
                          }}
                          className="ml-1 text-green-600 hover:text-green-800"
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </span>
                    ))}

                    {(filters.dateFrom || filters.dateTo) && (
                      <span className="inline-flex items-center bg-blue-100 text-blue-800 text-xs font-medium px-2.5 py-0.5 rounded-full">
                        <Calendar className="h-3 w-3 mr-1" />
                        {filters.dateFrom && filters.dateTo
                          ? `${filters.dateFrom} - ${filters.dateTo}`
                          : filters.dateFrom
                          ? `从 ${filters.dateFrom}`
                          : `到 ${filters.dateTo}`
                        }
                        <button
                          onClick={() => {
                            handleFilterChange('dateFrom', undefined);
                            handleFilterChange('dateTo', undefined);
                          }}
                          className="ml-1 text-blue-600 hover:text-blue-800"
                        >
                          <X className="h-3 w-3" />
                        </button>
                      </span>
                    )}
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}