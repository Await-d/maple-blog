/**
 * FeaturedPosts component - Display featured posts in elegant layouts
 * Features: Featured badges, highlighted design, responsive layouts
 */

import React, { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Star,
  Calendar as _Calendar,
  Eye,
  MessageCircle,
  Clock,
  User,
  Tag,
  Heart,
  Bookmark,
  Share2,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  Award,
  Zap,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { useFeaturedPosts } from '../../services/home/homeApi';
import { useIsMobile } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { PostSummary } from '../../types/home';

interface FeaturedPostsProps {
  className?: string;
  title?: string;
  count?: number;
  layout?: 'showcase' | 'carousel' | 'grid';
  showControls?: boolean;
  autoScroll?: boolean;
}

interface FeaturedPostCardProps {
  post: PostSummary;
  featured?: boolean;
  size?: 'small' | 'medium' | 'large';
  orientation?: 'horizontal' | 'vertical';
  showActions?: boolean;
  className?: string;
  style?: React.CSSProperties;
}

const FeaturedPostCard: React.FC<FeaturedPostCardProps> = ({
  post,
  featured = false,
  size = 'medium',
  orientation = 'vertical',
  showActions = true,
  className,
  style,
}) => {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);
  const [isLiked, setIsLiked] = useState(false);
  const [isBookmarked, setIsBookmarked] = useState(false);

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('zh-CN', {
      month: 'short',
      day: 'numeric',
    });
  };

  const handleImageLoad = () => setImageLoaded(true);
  const handleImageError = () => setImageError(true);

  const handleLike = (e: React.MouseEvent) => {
    e.preventDefault();
    setIsLiked(!isLiked);
  };

  const handleBookmark = (e: React.MouseEvent) => {
    e.preventDefault();
    setIsBookmarked(!isBookmarked);
  };

  const handleShare = (e: React.MouseEvent) => {
    e.preventDefault();
    if (navigator.share) {
      navigator.share({
        title: post.title,
        url: `/post/${post.slug}`,
      });
    }
  };

  const sizeClasses = {
    small: 'h-48',
    medium: 'h-64 sm:h-80',
    large: 'h-80 sm:h-96 lg:h-[32rem]',
  };

  const cardClasses = cn(
    'group relative overflow-hidden rounded-xl transition-all duration-500 hover:shadow-2xl',
    {
      'bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700': !post.featuredImageUrl,
      'transform hover:scale-[1.02]': size === 'large',
    },
    className
  );

  if (orientation === 'horizontal') {
    return (
      <article className={cn(cardClasses, 'flex', size === 'large' ? 'h-64' : 'h-48')} style={style}>
        <Link to={`/post/${post.slug}`} className="flex w-full">
          {/* Image */}
          {post.featuredImageUrl && !imageError && (
            <div className="relative w-2/5 bg-gray-200 dark:bg-gray-700">
              <img
                src={post.featuredImageUrl}
                alt={post.title}
                className={cn(
                  'w-full h-full object-cover transition-all duration-500 group-hover:scale-110',
                  !imageLoaded && 'opacity-0'
                )}
                onLoad={handleImageLoad}
                onError={handleImageError}
                loading="lazy"
              />
              {!imageLoaded && (
                <div className="absolute inset-0 bg-gray-200 dark:bg-gray-700 animate-pulse" />
              )}

              {/* Featured Badge */}
              {featured && (
                <div className="absolute top-3 left-3">
                  <span className="inline-flex items-center space-x-1 px-3 py-1.5 bg-gradient-to-r from-orange-500 to-red-500 text-white text-sm font-medium rounded-full shadow-lg">
                    <Star size={14} fill="currentColor" />
                    <span>精选</span>
                  </span>
                </div>
              )}

              {/* Quick Actions Overlay */}
              {showActions && (
                <div className="absolute inset-0 bg-black/20 opacity-0 group-hover:opacity-100 transition-opacity duration-300 flex items-center justify-center">
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                      onClick={handleLike}
                    >
                      <Heart size={16} className={isLiked ? 'fill-current text-red-500' : ''} />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                      onClick={handleBookmark}
                    >
                      <Bookmark size={16} className={isBookmarked ? 'fill-current' : ''} />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                      onClick={handleShare}
                    >
                      <Share2 size={16} />
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Content */}
          <div className="flex-1 p-6 flex flex-col justify-between">
            {/* Category & Tags */}
            <div className="flex items-center space-x-2 mb-3">
              {post.category && (
                <span className="inline-flex items-center px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-xs font-medium rounded">
                  <Tag size={10} className="mr-1" />
                  {post.category.name}
                </span>
              )}
              {post.readingTime && (
                <span className="text-xs text-gray-500 dark:text-gray-400 flex items-center space-x-1">
                  <Clock size={10} />
                  <span>{post.readingTime} 分钟</span>
                </span>
              )}
            </div>

            {/* Title */}
            <h3 className="font-bold text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors line-clamp-3 mb-3 text-lg">
              {post.title}
            </h3>

            {/* Summary */}
            {post.summary && (
              <p className="text-gray-600 dark:text-gray-400 line-clamp-3 mb-4 flex-1">
                {post.summary}
              </p>
            )}

            {/* Meta */}
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3 text-sm text-gray-500 dark:text-gray-400">
                <span className="flex items-center space-x-1">
                  <User size={14} />
                  <span className="truncate max-w-20">{post.author.displayName || post.author.userName}</span>
                </span>
                <span>{formatDate(post.publishedAt)}</span>
              </div>
              <div className="flex items-center space-x-3 text-sm text-gray-500 dark:text-gray-400">
                <span className="flex items-center space-x-1">
                  <Eye size={14} />
                  <span>{post.viewCount.toLocaleString()}</span>
                </span>
                {post.commentCount > 0 && (
                  <span className="flex items-center space-x-1">
                    <MessageCircle size={14} />
                    <span>{post.commentCount}</span>
                  </span>
                )}
              </div>
            </div>
          </div>
        </Link>
      </article>
    );
  }

  return (
    <article className={cn(cardClasses, sizeClasses[size])} style={style}>
      <Link to={`/post/${post.slug}`} className="block h-full">
        {/* Image Container */}
        <div className="relative h-2/3 bg-gray-200 dark:bg-gray-700">
          {post.featuredImageUrl && !imageError ? (
            <>
              <img
                src={post.featuredImageUrl}
                alt={post.title}
                className={cn(
                  'w-full h-full object-cover transition-all duration-500 group-hover:scale-110',
                  !imageLoaded && 'opacity-0'
                )}
                onLoad={handleImageLoad}
                onError={handleImageError}
                loading="lazy"
              />
              {!imageLoaded && (
                <div className="absolute inset-0 bg-gray-200 dark:bg-gray-700 animate-pulse" />
              )}
            </>
          ) : (
            <div className="w-full h-full bg-gradient-to-br from-orange-500 via-red-500 to-pink-500 flex items-center justify-center">
              <Star size={48} className="text-white/20" />
            </div>
          )}

          {/* Overlay Gradient */}
          <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />

          {/* Featured Badge */}
          {featured && (
            <div className="absolute top-4 left-4">
              <span className="inline-flex items-center space-x-2 px-3 py-2 bg-gradient-to-r from-orange-500 to-red-500 text-white font-medium rounded-full shadow-lg backdrop-blur-sm">
                <Award size={16} fill="currentColor" />
                <span>精选文章</span>
              </span>
            </div>
          )}

          {/* Reading Time */}
          {post.readingTime && (
            <div className="absolute top-4 right-4">
              <span className="inline-flex items-center space-x-1 px-2 py-1 bg-black/50 backdrop-blur-sm text-white text-sm rounded">
                <Clock size={12} />
                <span>{post.readingTime}min</span>
              </span>
            </div>
          )}

          {/* Quick Actions */}
          {showActions && (
            <div className="absolute bottom-4 right-4 flex items-center space-x-2 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
              <Button
                variant="ghost"
                size="sm"
                className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                onClick={handleLike}
              >
                <Heart size={16} className={isLiked ? 'fill-current text-red-500' : ''} />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                onClick={handleBookmark}
              >
                <Bookmark size={16} className={isBookmarked ? 'fill-current' : ''} />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="bg-white/20 backdrop-blur-sm hover:bg-white/30 text-white border-0 p-2"
                onClick={handleShare}
              >
                <Share2 size={16} />
              </Button>
            </div>
          )}
        </div>

        {/* Content */}
        <div className="h-1/3 p-4 flex flex-col justify-between">
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
          <h3 className="font-bold text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors line-clamp-2 mb-2">
            {post.title}
          </h3>

          {/* Meta */}
          <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
            <div className="flex items-center space-x-2">
              <User size={12} />
              <span className="truncate max-w-20">{post.author.displayName || post.author.userName}</span>
              <span>•</span>
              <span>{formatDate(post.publishedAt)}</span>
            </div>
            <div className="flex items-center space-x-2">
              <Eye size={12} />
              <span>{post.viewCount > 999 ? `${Math.floor(post.viewCount / 1000)}k` : post.viewCount}</span>
            </div>
          </div>
        </div>
      </Link>
    </article>
  );
};

export const FeaturedPosts: React.FC<FeaturedPostsProps> = ({
  className,
  title = '精选文章',
  count = 6,
  layout = 'showcase',
  showControls = true,
  autoScroll = false,
}) => {
  const isMobile = useIsMobile();
  const scrollRef = useRef<HTMLDivElement>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(true);

  // API data
  const { data: posts, isLoading, error, refetch } = useFeaturedPosts(count);

  // Auto scroll for carousel
  useEffect(() => {
    if (autoScroll && layout === 'carousel' && posts && posts.length > 1) {
      const interval = setInterval(() => {
        if (scrollRef.current) {
          const { scrollLeft, scrollWidth, clientWidth } = scrollRef.current;
          if (scrollLeft + clientWidth >= scrollWidth - 10) {
            scrollRef.current.scrollTo({ left: 0, behavior: 'smooth' });
          } else {
            scrollRef.current.scrollBy({ left: 300, behavior: 'smooth' });
          }
        }
      }, 5000);

      return () => clearInterval(interval);
    }
  }, [autoScroll, layout, posts]);

  // Update scroll buttons state
  const updateScrollButtons = () => {
    if (scrollRef.current) {
      const { scrollLeft, scrollWidth, clientWidth } = scrollRef.current;
      setCanScrollLeft(scrollLeft > 0);
      setCanScrollRight(scrollLeft + clientWidth < scrollWidth - 10);
    }
  };

  useEffect(() => {
    const scrollElement = scrollRef.current;
    if (scrollElement) {
      scrollElement.addEventListener('scroll', updateScrollButtons);
      updateScrollButtons();
      return () => scrollElement.removeEventListener('scroll', updateScrollButtons);
    }
  }, [posts]);

  const scrollLeft = () => {
    scrollRef.current?.scrollBy({ left: -300, behavior: 'smooth' });
  };

  const scrollRight = () => {
    scrollRef.current?.scrollBy({ left: 300, behavior: 'smooth' });
  };

  if (isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <div className="h-8 w-32 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        </div>
        <div className={cn(
          'grid gap-6',
          {
            'grid-cols-1 lg:grid-cols-2': layout === 'showcase',
            'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3': layout === 'grid',
          }
        )}>
          {Array.from({ length: layout === 'carousel' ? 3 : count }, (_, index) => (
            <div key={index} className="animate-pulse">
              <div className="bg-gray-200 dark:bg-gray-700 rounded-xl h-64 mb-4" />
              <div className="space-y-2">
                <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4" />
                <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
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
          <Star size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            暂无精选文章
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error ? '加载失败，请稍后重试' : '等待编辑推荐优质内容'}
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

  const [primaryPost, ...otherPosts] = posts;

  if (layout === 'showcase' && posts.length > 0) {
    return (
      <section className={cn('space-y-6', className)} role="region" aria-label={title}>
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <Zap className="text-orange-500" size={24} />
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              {title}
            </h2>
            <span className="px-2 py-1 bg-gradient-to-r from-orange-500 to-red-500 text-white text-sm font-medium rounded">
              编辑精选
            </span>
          </div>
          <Link to="/featured">
            <Button variant="ghost" size="sm" className="text-orange-600 hover:text-orange-700">
              查看全部
              <ExternalLink size={14} className="ml-1" />
            </Button>
          </Link>
        </div>

        {/* Showcase Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Primary Featured Post */}
          <FeaturedPostCard
            post={primaryPost}
            featured
            size="large"
            orientation={isMobile ? 'vertical' : 'horizontal'}
          />

          {/* Secondary Posts */}
          <div className="space-y-4">
            {otherPosts.slice(0, 3).map((post, index) => (
              <FeaturedPostCard
                key={post.id}
                post={post}
                size="small"
                orientation="horizontal"
                showActions={false}
                className="animate-fade-in"
                style={{ animationDelay: `${(index + 1) * 150}ms` } as React.CSSProperties}
              />
            ))}
          </div>
        </div>
      </section>
    );
  }

  if (layout === 'carousel') {
    return (
      <section className={cn('space-y-6', className)} role="region" aria-label={title}>
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <Star className="text-orange-500" size={24} />
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              {title}
            </h2>
          </div>
          {showControls && (
            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={scrollLeft}
                disabled={!canScrollLeft}
                className="p-2"
              >
                <ChevronLeft size={16} />
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={scrollRight}
                disabled={!canScrollRight}
                className="p-2"
              >
                <ChevronRight size={16} />
              </Button>
            </div>
          )}
        </div>

        {/* Carousel */}
        <div
          ref={scrollRef}
          className="flex space-x-6 overflow-x-auto scrollbar-hide pb-4"
          style={{ scrollSnapType: 'x mandatory' }}
        >
          {posts.map((post, index) => (
            <div
              key={post.id}
              className="flex-shrink-0 w-80"
              style={{ scrollSnapAlign: 'start' }}
            >
              <FeaturedPostCard
                post={post}
                featured={index === 0}
                size="medium"
                className="animate-slide-in-right"
                style={{ animationDelay: `${index * 100}ms` } as React.CSSProperties}
              />
            </div>
          ))}
        </div>
      </section>
    );
  }

  // Grid Layout
  return (
    <section className={cn('space-y-6', className)} role="region" aria-label={title}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Star className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
      </div>

      {/* Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {posts.map((post, index) => (
          <FeaturedPostCard
            key={post.id}
            post={post}
            featured={index < 2}
            size="medium"
            className="animate-fade-in-up"
            style={{ animationDelay: `${index * 150}ms` } as React.CSSProperties}
          />
        ))}
      </div>
    </section>
  );
};

/**
 * Usage:
 * <FeaturedPosts /> - Default showcase layout
 * <FeaturedPosts layout="carousel" autoScroll /> - Auto-scrolling carousel
 * <FeaturedPosts layout="grid" count={9} /> - 3x3 grid layout
 *
 * Features:
 * - Multiple responsive layouts (showcase, carousel, grid)
 * - Featured post highlighting with badges
 * - Auto-scrolling carousel option
 * - Interactive quick actions (like, bookmark, share)
 * - Smooth animations and transitions
 * - Loading states with skeletons
 * - Error handling with retry
 * - Image lazy loading with fallbacks
 * - Responsive design optimized for all devices
 * - Accessibility support with proper ARIA labels
 * - Integration with featured posts API
 * - Beautiful gradient overlays and effects
 */

export default FeaturedPosts;