/**
 * TagCloud component - Interactive tag cloud with visual weighting
 * Features: Size-based frequency, color themes, filtering, animations
 */

import React, { useState, useMemo, useRef, useEffect as _useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Tag,
  TrendingUp as _TrendingUp,
  Filter,
  X,
  RefreshCw,
  Hash,
  Plus,
  Minus,
  Search,
  SortAsc,
  SortDesc as _SortDesc,
  Clock,
  BarChart3,
  Shuffle as _Shuffle,
  Eye as _Eye,
  EyeOff as _EyeOff,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { useTagStats } from '../../services/home/homeApi';
import { useHomeStore, useIsMobile } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { TagSummary } from '../../types/home';

interface TagCloudProps {
  className?: string;
  title?: string;
  maxTags?: number;
  minUsage?: number;
  showControls?: boolean;
  interactive?: boolean;
  colorScheme?: 'default' | 'warm' | 'cool' | 'rainbow';
  shape?: 'rectangle' | 'circle' | 'organic';
}

interface TagItemProps {
  tag: TagSummary;
  size: number;
  color: string;
  isSelected?: boolean;
  isHighlighted?: boolean;
  onClick?: () => void;
  className?: string;
  style?: React.CSSProperties;
}

const TagItem: React.FC<TagItemProps> = ({
  tag,
  size,
  color,
  isSelected = false,
  isHighlighted = false,
  onClick,
  className,
  style,
}) => {
  const [isHovered, setIsHovered] = useState(false);

  return (
    <Link
      to={`/tag/${tag.slug}`}
      onClick={onClick}
      className={cn(
        'inline-block m-1 px-3 py-1 rounded-full transition-all duration-300 hover:scale-110 cursor-pointer',
        {
          'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300 ring-2 ring-orange-500': isSelected,
          'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700': !isSelected && !isHighlighted,
          'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300': isHighlighted && !isSelected,
          'transform hover:-translate-y-1 shadow-lg': isHovered,
        },
        className
      )}
      style={{
        fontSize: `${size}px`,
        backgroundColor: !isSelected && !isHighlighted ? color : undefined,
        color: !isSelected && !isHighlighted ? 'white' : undefined,
        fontWeight: Math.max(400, Math.min(700, 400 + tag.usageFrequency * 100)),
        ...style,
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      title={`${tag.name} - ${tag.postCount} 篇文章`}
    >
      #{tag.name}
    </Link>
  );
};

const getColorScheme = (scheme: string, index: number, total: number): string[] => {
  switch (scheme) {
    case 'warm':
      return ['#f97316', '#f59e0b', '#dc2626', '#ea580c', '#c2410c'];
    case 'cool':
      return ['#3b82f6', '#06b6d4', '#8b5cf6', '#10b981', '#6366f1'];
    case 'rainbow': {
      const hue = (index / total) * 360;
      return [`hsl(${hue}, 70%, 60%)`];
    }
    default:
      return ['#6b7280', '#9ca3af', '#4b5563', '#374151', '#1f2937'];
  }
};

export const TagCloud: React.FC<TagCloudProps> = ({
  className,
  title = '热门标签',
  maxTags = 50,
  minUsage = 1,
  showControls = true,
  interactive = true,
  colorScheme = 'default',
  shape = 'rectangle',
}) => {
  const isMobile = useIsMobile();
  const cloudRef = useRef<HTMLDivElement>(null);

  const {
    components: { tagCloud },
    selectTags,
    toggleTag,
    setTagFilterMode,
    setTagSortBy,
  } = useHomeStore();

  // Local state
  const [searchQuery, setSearchQuery] = useState('');
  const [isShuffling, setIsShuffling] = useState(false);
  const [showFilters, setShowFilters] = useState(false);

  // API data
  const { data: tags, isLoading, error, refetch } = useTagStats(maxTags, minUsage);

  // Process tags with search and sorting
  const processedTags = useMemo(() => {
    if (!tags) return [];

    let filtered = tags;

    // Apply search filter
    if (searchQuery.trim()) {
      const query = searchQuery.toLowerCase();
      filtered = filtered.filter(tag =>
        tag.name.toLowerCase().includes(query) ||
        tag.description?.toLowerCase().includes(query)
      );
    }

    // Apply sorting
    const sorted = [...filtered].sort((a, b) => {
      switch (tagCloud.sortBy) {
        case 'name':
          return a.name.localeCompare(b.name, 'zh-CN');
        case 'count':
          return b.postCount - a.postCount;
        case 'recent':
          return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime();
        default:
          return b.postCount - a.postCount;
      }
    });

    return sorted;
  }, [tags, searchQuery, tagCloud.sortBy]);

  // Calculate tag sizes and colors
  const tagStyles = useMemo(() => {
    if (!processedTags.length) return [];

    const maxCount = Math.max(...processedTags.map(t => t.postCount));
    const minCount = Math.min(...processedTags.map(t => t.postCount));
    const countRange = maxCount - minCount || 1;

    const minSize = isMobile ? 12 : 14;
    const maxSize = isMobile ? 24 : 32;
    const sizeRange = maxSize - minSize;

    const colors = getColorScheme(colorScheme, 0, processedTags.length);

    return processedTags.map((tag, index) => {
      const frequency = (tag.postCount - minCount) / countRange;
      const size = minSize + (frequency * sizeRange);
      const colorIndex = index % colors.length;
      const color = colors[colorIndex];

      return {
        tag,
        size,
        color,
        frequency,
      };
    });
  }, [processedTags, colorScheme, isMobile]);

  // Handle tag selection
  const handleTagClick = (tagId: string) => {
    if (interactive) {
      toggleTag(tagId);
    }
  };

  // Handle shuffle animation
  const handleShuffle = () => {
    setIsShuffling(true);
    refetch();
    setTimeout(() => setIsShuffling(false), 1000);
  };

  // Filter selected/highlighted tags
  const getTagState = (tagId: string) => {
    const isSelected = tagCloud.selectedTags.includes(tagId);
    const isHighlighted = Boolean(
      searchQuery.trim() &&
      processedTags.find(t => t.id === tagId)?.name.toLowerCase().includes(searchQuery.toLowerCase())
    );
    return { isSelected, isHighlighted };
  };

  if (isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <div className="h-8 w-32 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          {showControls && (
            <div className="flex items-center space-x-2">
              {[1, 2, 3].map((i) => (
                <div key={i} className="w-8 h-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
              ))}
            </div>
          )}
        </div>
        <div className="min-h-48 flex items-center justify-center">
          <div className="flex flex-wrap gap-2">
            {Array.from({ length: 20 }, (_, index) => (
              <div
                key={index}
                className="h-6 bg-gray-200 dark:bg-gray-700 rounded-full animate-pulse"
                style={{
                  width: `${60 + (index % 5) * 20}px`,
                }}
              />
            ))}
          </div>
        </div>
      </section>
    );
  }

  if (error || !tags?.length) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
        <div className="text-center py-12">
          <Tag size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            暂无标签
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error ? '加载失败，请稍后重试' : '等待创建文章标签'}
          </p>
          {error && (
            <Button onClick={() => refetch()} variant="outline" size="sm">
              重新加载
            </Button>
          )}
        </div>
      </section>
    );
  }

  return (
    <section className={cn('space-y-6', className)} role="region" aria-label={title}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Hash className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
          <span className="px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-sm font-medium rounded">
            {processedTags.length}
          </span>
        </div>

        {/* Controls */}
        {showControls && (
          <div className="flex items-center space-x-2">
            {/* Search */}
            <div className="relative">
              <Input
                type="text"
                placeholder="搜索标签..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-32 sm:w-40 h-8 text-sm"
              />
              <Search size={14} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400" />
            </div>

            {/* Filter Toggle */}
            <Button
              variant={showFilters ? 'primary' : 'outline'}
              size="sm"
              onClick={() => setShowFilters(!showFilters)}
              className="p-2"
              aria-label="筛选选项"
            >
              <Filter size={16} />
            </Button>

            {/* Shuffle */}
            <Button
              variant="outline"
              size="sm"
              onClick={handleShuffle}
              disabled={isShuffling}
              className="p-2"
              aria-label="随机排序"
            >
              <RefreshCw size={16} className={isShuffling ? 'animate-spin' : ''} />
            </Button>
          </div>
        )}
      </div>

      {/* Filters Panel */}
      {showFilters && (
        <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {/* Sort Options */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                排序方式
              </label>
              <div className="flex items-center space-x-2">
                {[
                  { value: 'count', label: '使用次数', icon: BarChart3 },
                  { value: 'name', label: '名称', icon: SortAsc },
                  { value: 'recent', label: '最新', icon: Clock },
                ].map(({ value, label, icon: Icon }) => (
                  <Button
                    key={value}
                    variant={tagCloud.sortBy === value ? 'primary' : 'outline'}
                    size="sm"
                    onClick={() => setTagSortBy(value as 'count' | 'name' | 'recent')}
                    className="flex items-center space-x-1"
                  >
                    <Icon size={14} />
                    <span className="hidden sm:inline">{label}</span>
                  </Button>
                ))}
              </div>
            </div>

            {/* Filter Mode */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                筛选模式
              </label>
              <div className="flex items-center space-x-2">
                <Button
                  variant={tagCloud.filterMode === 'include' ? 'primary' : 'outline'}
                  size="sm"
                  onClick={() => setTagFilterMode('include')}
                  className="flex items-center space-x-1"
                >
                  <Plus size={14} />
                  <span>包含</span>
                </Button>
                <Button
                  variant={tagCloud.filterMode === 'exclude' ? 'primary' : 'outline'}
                  size="sm"
                  onClick={() => setTagFilterMode('exclude')}
                  className="flex items-center space-x-1"
                >
                  <Minus size={14} />
                  <span>排除</span>
                </Button>
              </div>
            </div>

            {/* Selected Tags */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                已选择 ({tagCloud.selectedTags.length})
              </label>
              {tagCloud.selectedTags.length > 0 ? (
                <div className="flex items-center space-x-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => selectTags([])}
                    className="flex items-center space-x-1"
                  >
                    <X size={14} />
                    <span>清空</span>
                  </Button>
                  <span className="text-sm text-gray-500 dark:text-gray-400">
                    {tagCloud.filterMode === 'include' ? '显示包含' : '排除包含'}这些标签的文章
                  </span>
                </div>
              ) : (
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  点击标签进行筛选
                </p>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Tag Cloud */}
      <div
        ref={cloudRef}
        className={cn(
          'min-h-48 p-6 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 transition-all duration-500',
          {
            'text-center': shape === 'circle',
            'rounded-full': shape === 'circle',
            'transform hover:shadow-lg': interactive,
          },
          isShuffling && 'animate-pulse'
        )}
        style={{
          clipPath: shape === 'organic' ? 'polygon(0% 0%, 100% 0%, 100% 75%, 75% 100%, 0% 100%)' : undefined,
        }}
      >
        {tagStyles.length > 0 ? (
          <div className={cn(
            'flex flex-wrap items-center',
            shape === 'circle' ? 'justify-center' : 'justify-start'
          )}>
            {tagStyles.map(({ tag, size, color, frequency: _frequency }, index) => {
              const { isSelected, isHighlighted } = getTagState(tag.id);
              return (
                <TagItem
                  key={tag.id}
                  tag={tag}
                  size={size}
                  color={color}
                  isSelected={isSelected}
                  isHighlighted={isHighlighted}
                  onClick={() => handleTagClick(tag.id)}
                  className="animate-fade-in"
                  style={{ animationDelay: `${index * 50}ms` } as React.CSSProperties}
                />
              );
            })}
          </div>
        ) : (
          <div className="flex items-center justify-center h-32 text-gray-500 dark:text-gray-400">
            <div className="text-center">
              <Search size={32} className="mx-auto mb-2" />
              <p>没有找到匹配的标签</p>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSearchQuery('')}
                className="mt-2"
              >
                清除搜索
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* Statistics */}
      <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
        <span>
          显示 {processedTags.length} 个标签
          {searchQuery && ` • 搜索: "${searchQuery}"`}
        </span>
        <div className="flex items-center space-x-4">
          {tagCloud.selectedTags.length > 0 && (
            <span>
              已选择 {tagCloud.selectedTags.length} 个标签
            </span>
          )}
          <Link to="/tags">
            <Button variant="ghost" size="sm" className="text-orange-600 hover:text-orange-700">
              查看全部标签
            </Button>
          </Link>
        </div>
      </div>
    </section>
  );
};

/**
 * Usage:
 * <TagCloud /> - Default tag cloud
 * <TagCloud colorScheme="rainbow" shape="circle" /> - Rainbow colored circular cloud
 * <TagCloud maxTags={30} minUsage={5} /> - Limited high-usage tags
 * <TagCloud interactive={false} /> - Non-interactive display only
 *
 * Features:
 * - Visual tag weighting based on usage frequency
 * - Multiple color schemes (default, warm, cool, rainbow)
 * - Different shapes (rectangle, circle, organic)
 * - Interactive tag selection with filtering
 * - Real-time search with highlighting
 * - Multiple sorting options (count, name, recent)
 * - Responsive design with mobile optimization
 * - Smooth animations and hover effects
 * - Tag frequency visualization
 * - Accessibility support with ARIA labels
 * - Integration with home store for state persistence
 * - Loading states and error handling
 */

export default TagCloud;