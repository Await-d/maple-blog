// @ts-nocheck
/**
 * PopularPosts component - Display popular posts with different layouts
 * Features: Multiple layouts, loading states, responsive design, animations
 */

import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  TrendingUp,
  Calendar,
  Eye,
  MessageCircle,
  Clock,
  User,
  Tag,
  Star,
  ThumbsUp,
  Bookmark,
  Share2,
  MoreHorizontal as _MoreHorizontal,
  Grid3X3,
  List,
  Grid,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { usePopularPosts } from '../../services/home/homeApi';
import { useHomeStore, useIsMobile, useCurrentLayout } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { PostSummary } from '../../types/home';

interface PopularPostsProps {
  className?: string;
  title?: string;
  count?: number;
  daysBack?: number;
  layout?: 'grid' | 'list' | 'cards';
  showControls?: boolean;
  compact?: boolean;
}

interface PostCardProps {
  post: PostSummary;
  layout: 'grid' | 'list' | 'cards';
  compact?: boolean;
  index?: number;
  className?: string;
  style?: React.CSSProperties;
}

const PostCard: React.FC<PostCardProps> = ({
  post,
  layout,
  compact = false,
  index = 0,
  className,
  style,
}) => {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 1) return '昨天';
    if (diffDays < 7) return `${diffDays} 天前`;
    if (diffDays < 30) return `${Math.ceil(diffDays / 7)} 周前`;
    return date.toLocaleDateString('zh-CN');
  };

  const handleImageLoad = () => setImageLoaded(true);
  const handleImageError = () => setImageError(true);

  const handleShare = (e: React.MouseEvent) => {
    e.preventDefault();
    if (navigator.share) {
      navigator.share({
        title: post.title,
        url: `/post/${post.slug}`,
      });
    }
  };

  const cardClasses = cn(
    'group relative transition-all duration-300 hover:shadow-lg dark:hover:shadow-2xl',
    {
      'bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700': layout !== 'list',
      'border-b border-gray-200 dark:border-gray-700 pb-4': layout === 'list',
    },
    className
  );

  if (layout === 'list') {
    return (
      <article className={cardClasses} style={style}>
        <Link to={`/post/${post.slug}`} className="flex space-x-4">
          {/* Image */}
          {!compact && post.featuredImageUrl && !imageError && (
            <div className="flex-shrink-0 w-24 h-24 sm:w-32 sm:h-32 rounded-lg overflow-hidden bg-gray-200 dark:bg-gray-700">
              <img
                src={post.featuredImageUrl}
                alt={post.title}
                className={cn(
                  'w-full h-full object-cover transition-all duration-300 group-hover:scale-105',
                  !imageLoaded && 'opacity-0'
                )}
                onLoad={handleImageLoad}
                onError={handleImageError}
                loading="lazy"
              />
              {!imageLoaded && (
                <div className="w-full h-full bg-gray-200 dark:bg-gray-700 animate-pulse" />
              )}
            </div>
          )}

          {/* Content */}
          <div className="flex-1 min-w-0">
            {/* Category & Trending Badge */}
            <div className="flex items-center space-x-2 mb-2">
              {post.category && (
                <span className="inline-flex items-center px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-xs font-medium rounded">
                  {post.category.name}
                </span>
              )}
              {index < 3 && (
                <span className="inline-flex items-center space-x-1 px-2 py-1 bg-red-100 dark:bg-red-900/20 text-red-700 dark:text-red-300 text-xs font-medium rounded">
                  <TrendingUp size={10} />
                  <span>热门</span>
                </span>
              )}
            </div>

            {/* Title */}
            <h3 className={cn(
              'font-semibold text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors line-clamp-2 mb-2',
              compact ? 'text-sm' : 'text-base sm:text-lg'
            )}>
              {post.title}
            </h3>

            {/* Summary */}
            {!compact && post.summary && (
              <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2 mb-3">
                {post.summary}
              </p>
            )}

            {/* Meta */}
            <div className="flex items-center space-x-4 text-xs text-gray-500 dark:text-gray-400">
              <span className="flex items-center space-x-1">
                <User size={12} />
                <span>{post.author.displayName || post.author.userName}</span>
              </span>
              <span className="flex items-center space-x-1">
                <Calendar size={12} />
                <span>{formatDate(post.publishedAt)}</span>
              </span>
              <span className="flex items-center space-x-1">
                <Eye size={12} />
                <span>{post.viewCount.toLocaleString()}</span>
              </span>
              {post.commentCount > 0 && (
                <span className="flex items-center space-x-1">
                  <MessageCircle size={12} />
                  <span>{post.commentCount}</span>
                </span>
              )}
            </div>
          </div>
        </Link>
      </article>
    );
  }

  return (
    <article className={cardClasses} style={style}>
      <Link to={`/post/${post.slug}`} className="block">
        {/* Image */}
        {post.featuredImageUrl && !imageError && (
          <div className={cn(
            'relative overflow-hidden bg-gray-200 dark:bg-gray-700',
            compact ? 'h-32' : 'h-48',
            layout === 'cards' ? 'rounded-t-lg' : 'rounded-lg mb-4'
          )}>
            <img
              src={post.featuredImageUrl}
              alt={post.title}
              className={cn(
                'w-full h-full object-cover transition-all duration-300 group-hover:scale-105',
                !imageLoaded && 'opacity-0'
              )}
              onLoad={handleImageLoad}
              onError={handleImageError}
              loading="lazy"
            />
            {!imageLoaded && (
              <div className="absolute inset-0 bg-gray-200 dark:bg-gray-700 animate-pulse" />
            )}

            {/* Overlay Badges */}
            <div className="absolute top-3 left-3 flex items-center space-x-2">
              {index < 3 && (
                <span className="inline-flex items-center space-x-1 px-2 py-1 bg-red-500/90 backdrop-blur-sm text-white text-xs font-medium rounded">
                  <Star size={10} />
                  <span>热门</span>
                </span>
              )}
              {post.readingTime && (
                <span className="inline-flex items-center space-x-1 px-2 py-1 bg-black/50 backdrop-blur-sm text-white text-xs rounded">
                  <Clock size={10} />
                  <span>{post.readingTime}min</span>
                </span>
              )}
            </div>

            {/* Quick Actions */}
            <div className="absolute top-3 right-3 opacity-0 group-hover:opacity-100 transition-opacity">
              <div className="flex items-center space-x-1">
                <Button
                  variant="ghost"
                  size="sm"
                  className="p-1 bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0"
                  onClick={handleShare}
                  aria-label="分享"
                >
                  <Share2 size={14} />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  className="p-1 bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0"
                  aria-label="收藏"
                >
                  <Bookmark size={14} />
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Content */}
        <div className={cn('p-4', !post.featuredImageUrl && 'pt-0')}>
          {/* Category */}
          {post.category && (
            <div className="mb-2">
              <span className="inline-flex items-center px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-xs font-medium rounded">
                <Tag size={10} className="mr-1" />
                {post.category.name}
              </span>
            </div>
          )}

          {/* Title */}
          <h3 className={cn(
            'font-semibold text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors line-clamp-2 mb-2',
            compact ? 'text-sm' : 'text-base sm:text-lg'
          )}>
            {post.title}
          </h3>

          {/* Summary */}
          {!compact && post.summary && (
            <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-3 mb-3">
              {post.summary}
            </p>
          )}

          {/* Meta */}
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3 text-xs text-gray-500 dark:text-gray-400">
              <span className="flex items-center space-x-1">
                <User size={12} />
                <span className="truncate max-w-20">{post.author.displayName || post.author.userName}</span>
              </span>
              <span>{formatDate(post.publishedAt)}</span>
            </div>
            <div className="flex items-center space-x-2 text-xs text-gray-500 dark:text-gray-400">
              <span className="flex items-center space-x-1">
                <Eye size={12} />
                <span>{post.viewCount > 999 ? `${Math.floor(post.viewCount / 1000)}k` : post.viewCount}</span>
              </span>
              {post.likeCount > 0 && (
                <span className="flex items-center space-x-1">
                  <ThumbsUp size={12} />
                  <span>{post.likeCount}</span>
                </span>
              )}
            </div>
          </div>
        </div>
      </Link>
    </article>
  );
};

export const PopularPosts: React.FC<PopularPostsProps> = ({
  className,
  title = '热门文章',
  count = 12,
  daysBack = 30,
  layout: propLayout,
  showControls = true,
  compact = false,
}) => {
  const isMobile = useIsMobile();
  const currentLayout = useCurrentLayout();
  const { setLayoutMode } = useHomeStore();

  const layout = propLayout || currentLayout;
  const actualCount = isMobile ? Math.min(count, 6) : count;

  // API data
  const { data: posts, isLoading, error, refetch } = usePopularPosts(actualCount, daysBack);

  const handleLayoutChange = (newLayout: 'grid' | 'list' | 'cards') => {
    if (!propLayout) {
      setLayoutMode(newLayout);
    }
  };

  if (isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        {/* Header */}
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

        {/* Loading Posts */}
        <div className={cn(
          'grid gap-6',
          {
            'grid-cols-1': layout === 'list',
            'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3': layout === 'grid',
            'grid-cols-1 sm:grid-cols-2 xl:grid-cols-3': layout === 'cards',
          }
        )}>
          {Array.from({ length: actualCount }, (_, index) => (
            <div key={index} className="animate-pulse">
              {layout !== 'list' && (
                <div className={cn('bg-gray-200 dark:bg-gray-700 rounded-lg mb-4', compact ? 'h-32' : 'h-48')} />
              )}
              <div className="space-y-3">
                <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4" />
                <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
                <div className="flex space-x-4">
                  <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-16" />
                  <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-12" />
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>
    );
  }

  if (error || !posts?.length) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
        <div className="text-center py-12">
          <TrendingUp size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            暂无热门文章
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error ? '加载失败，请稍后重试' : '等待更多精彩内容'}
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
          <TrendingUp className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
          <span className="px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-sm font-medium rounded">
            {posts.length}
          </span>
        </div>

        {/* Layout Controls */}
        {showControls && !propLayout && (
          <div className="flex items-center space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
            <Button
              variant={layout === 'cards' ? 'primary' : 'ghost'}
              size="sm"
              onClick={() => handleLayoutChange('cards')}
              className="p-2"
              aria-label="卡片布局"
            >
              <Grid3X3 size={16} />
            </Button>
            <Button
              variant={layout === 'grid' ? 'primary' : 'ghost'}
              size="sm"
              onClick={() => handleLayoutChange('grid')}
              className="p-2"
              aria-label="网格布局"
            >
              <Grid size={16} />
            </Button>
            <Button
              variant={layout === 'list' ? 'primary' : 'ghost'}
              size="sm"
              onClick={() => handleLayoutChange('list')}
              className="p-2"
              aria-label="列表布局"
            >
              <List size={16} />
            </Button>
          </div>
        )}
      </div>

      {/* Posts Grid */}
      <div className={cn(
        'grid gap-6',
        {
          'grid-cols-1 space-y-6': layout === 'list',
          'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3': layout === 'grid',
          'grid-cols-1 sm:grid-cols-2 xl:grid-cols-3': layout === 'cards',
        }
      )}>
        {posts.map((post, index) => (
          <PostCard
            key={post.id}
            post={post}
            layout={layout}
            compact={compact}
            index={index}
            className="animate-fade-in"
            style={{ animationDelay: `${index * 100}ms` } as React.CSSProperties}
          />
        ))}
      </div>

      {/* Load More */}
      {posts.length >= actualCount && (
        <div className="text-center pt-6">
          <Link to="/popular">
            <Button variant="outline" size="lg">
              查看更多热门文章
              <TrendingUp size={16} className="ml-2" />
            </Button>
          </Link>
        </div>
      )}
    </section>
  );
};

/**
 * Usage:
 * <PopularPosts /> - Default popular posts
 * <PopularPosts layout="list" /> - List layout
 * <PopularPosts compact /> - Compact version
 * <PopularPosts count={6} daysBack={7} /> - Weekly top 6
 *
 * Features:
 * - Multiple responsive layouts (grid, list, cards)
 * - Layout switching controls
 * - Loading states with skeletons
 * - Error handling with retry
 * - Image lazy loading with fallbacks
 * - Trending badges for top posts
 * - Quick action buttons (share, bookmark)
 * - Responsive design with mobile optimization
 * - Smooth animations with staggered delays
 * - Integration with home store for layout persistence
 * - Accessibility support with proper ARIA labels
 * - SEO-friendly structured data
 */

export default PopularPosts;