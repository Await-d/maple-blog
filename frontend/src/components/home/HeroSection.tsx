/**
 * HeroSection component - Main hero section with featured posts carousel
 * Features: Auto-play carousel, parallax effects, responsive design, touch gestures
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Link } from 'react-router-dom';
import {
  ChevronLeft,
  ChevronRight,
  Play,
  Pause,
  Calendar,
  Eye,
  MessageCircle,
  Clock,
  User,
  Tag,
  ExternalLink,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { useFeaturedPosts } from '../../services/home/homeApi';
import { useHomeStore, useIsMobile, useAccessibilitySettings } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { PostSummary } from '../../types/home';

interface HeroSectionProps {
  className?: string;
  height?: 'sm' | 'md' | 'lg' | 'xl';
  showControls?: boolean;
  autoPlay?: boolean;
  interval?: number;
}

interface SlideIndicatorProps {
  total: number;
  current: number;
  onSelect: (index: number) => void;
  className?: string;
}

const SlideIndicator: React.FC<SlideIndicatorProps> = ({
  total,
  current,
  onSelect,
  className,
}) => (
  <div className={cn('flex items-center justify-center space-x-2', className)}>
    {Array.from({ length: total }, (_, index) => (
      <button
        key={index}
        onClick={() => onSelect(index)}
        className={cn(
          'w-2 h-2 rounded-full transition-all duration-300',
          index === current
            ? 'bg-white w-8'
            : 'bg-white/50 hover:bg-white/75'
        )}
        aria-label={`切换到第 ${index + 1} 张幻灯片`}
      />
    ))}
  </div>
);

interface PostSlideProps {
  post: PostSummary;
  isActive: boolean;
  onReadMore: () => void;
  className?: string;
}

const PostSlide: React.FC<PostSlideProps> = ({
  post,
  isActive,
  onReadMore,
  className,
}) => {
  const formatDate = (dateString: string): string => {
    try {
      return new Date(dateString).toLocaleDateString('zh-CN', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      });
    } catch {
      return '未知日期';
    }
  };

  return (
    <div className={cn('relative h-full w-full overflow-hidden', className)}>
      {/* Background Image with Parallax */}
      <div className="absolute inset-0">
        {post.featuredImageUrl ? (
          <div className="relative h-full w-full">
            <img
              src={post.featuredImageUrl}
              alt={post.title}
              className="h-full w-full object-cover transition-transform duration-700 ease-out hover:scale-105"
              loading="lazy"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-black/70 via-black/50 to-transparent" />
          </div>
        ) : (
          <div className="h-full w-full bg-gradient-to-br from-orange-500 via-red-500 to-pink-500">
            <div className="absolute inset-0 bg-black/30" />
          </div>
        )}
      </div>

      {/* Content */}
      <div className="relative z-10 flex h-full items-center">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl">
            {/* Category and Reading Time */}
            <div className="flex items-center space-x-4 mb-4">
              {post.category && (
                <Link
                  to={`/category/${post.category.slug}`}
                  className="inline-flex items-center space-x-1 px-3 py-1 bg-orange-500/20 backdrop-blur-sm rounded-full text-orange-200 hover:bg-orange-500/30 transition-colors"
                >
                  <Tag size={14} />
                  <span className="text-sm font-medium">{post.category.name}</span>
                </Link>
              )}
              {post.readingTime && (
                <div className="flex items-center space-x-1 text-white/80">
                  <Clock size={14} />
                  <span className="text-sm">{post.readingTime} 分钟阅读</span>
                </div>
              )}
            </div>

            {/* Title */}
            <h1 className="text-3xl sm:text-4xl lg:text-5xl font-bold text-white mb-4 leading-tight">
              {post.title}
            </h1>

            {/* Summary */}
            {post.summary && (
              <p className="text-lg text-white/90 mb-6 line-clamp-3 leading-relaxed">
                {post.summary}
              </p>
            )}

            {/* Meta Information */}
            <div className="flex flex-wrap items-center space-x-6 mb-8 text-white/80">
              <div className="flex items-center space-x-2">
                <User size={16} />
                <Link
                  to={`/author/${post.author?.userName || 'unknown'}`}
                  className="hover:text-white transition-colors"
                >
                  {post.author?.displayName || post.author?.userName || '未知作者'}
                </Link>
              </div>
              <div className="flex items-center space-x-2">
                <Calendar size={16} />
                <span>{formatDate(post.publishedAt || '')}</span>
              </div>
              <div className="flex items-center space-x-2">
                <Eye size={16} />
                <span>{(post.viewCount || 0).toLocaleString()} 次浏览</span>
              </div>
              {(post.commentCount || 0) > 0 && (
                <div className="flex items-center space-x-2">
                  <MessageCircle size={16} />
                  <span>{post.commentCount || 0} 评论</span>
                </div>
              )}
            </div>

            {/* Action Buttons */}
            <div className="flex items-center space-x-4">
              <Button
                as={Link}
                to={`/post/${post.slug || 'unknown'}`}
                variant="primary"
                size="lg"
                className="bg-white/20 backdrop-blur-sm border-white/30 text-white hover:bg-white/30"
                onClick={onReadMore}
              >
                阅读全文
              </Button>
              <Button
                variant="ghost"
                size="lg"
                className="text-white border-white/30 hover:bg-white/10"
                onClick={() => {
                  if (navigator.share && post.title && post.slug) {
                    navigator.share({
                      title: post.title,
                      url: `/post/${post.slug}`,
                    });
                  }
                }}
              >
                <ExternalLink size={16} className="mr-2" />
                分享
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Animated Overlay */}
      <div
        className={cn(
          'absolute inset-0 bg-black/20 transition-opacity duration-500',
          isActive ? 'opacity-0' : 'opacity-100'
        )}
      />
    </div>
  );
};

export const HeroSection: React.FC<HeroSectionProps> = ({
  className,
  height = 'lg',
  showControls = true,
  autoPlay: initialAutoPlay = true,
  interval = 5000,
}) => {
  const isMobile = useIsMobile();
  const accessibility = useAccessibilitySettings();
  const {
    components: { heroSection },
    setHeroSlide,
    setHeroAutoPlay,
    toggleHeroPlayback,
  } = useHomeStore();

  // Local state
  const [touchStart, setTouchStart] = useState<number | null>(null);
  const [touchEnd, setTouchEnd] = useState<number | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  // API data with error handling
  const { data: featuredPosts, isLoading, error } = useFeaturedPosts(5);

  // Ensure posts is always an array and handle edge cases
  const posts = Array.isArray(featuredPosts) ? featuredPosts : [];
  const safeCurrentSlide = Math.max(0, Math.min(heroSection.currentSlide, posts.length - 1));
  const currentSlide = posts.length > 0 ? safeCurrentSlide : 0;
  const autoPlay = heroSection.autoPlay && !accessibility.reduceMotion;
  const isPlaying = heroSection.isPlaying && !accessibility.reduceMotion;

  // Auto-play functionality
  useEffect(() => {
    if (autoPlay && isPlaying && posts.length > 1) {
      intervalRef.current = setInterval(() => {
        setHeroSlide((currentSlide + 1) % posts.length);
      }, interval);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [autoPlay, isPlaying, posts.length, currentSlide, interval, setHeroSlide]);

  // Initialize auto-play setting
  useEffect(() => {
    if (initialAutoPlay !== undefined) {
      setHeroAutoPlay(initialAutoPlay);
    }
  }, [initialAutoPlay, setHeroAutoPlay]);

  // Navigation functions
  const goToSlide = useCallback((index: number) => {
    setHeroSlide(index);
  }, [setHeroSlide]);

  const goToPrevious = useCallback(() => {
    const newIndex = currentSlide === 0 ? posts.length - 1 : currentSlide - 1;
    setHeroSlide(newIndex);
  }, [currentSlide, posts.length, setHeroSlide]);

  const goToNext = useCallback(() => {
    const newIndex = (currentSlide + 1) % posts.length;
    setHeroSlide(newIndex);
  }, [currentSlide, posts.length, setHeroSlide]);

  // Touch handlers for mobile swipe
  const handleTouchStart = (e: React.TouchEvent) => {
    setTouchEnd(null);
    setTouchStart(e.targetTouches[0].clientX);
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    setTouchEnd(e.targetTouches[0].clientX);
  };

  const handleTouchEnd = () => {
    if (!touchStart || !touchEnd) return;

    const distance = touchStart - touchEnd;
    const isLeftSwipe = distance > 50;
    const isRightSwipe = distance < -50;

    if (isLeftSwipe && posts.length > 1) {
      goToNext();
    }
    if (isRightSwipe && posts.length > 1) {
      goToPrevious();
    }
  };

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (posts.length <= 1) return;

      switch (event.key) {
        case 'ArrowLeft':
          event.preventDefault();
          goToPrevious();
          break;
        case 'ArrowRight':
          event.preventDefault();
          goToNext();
          break;
        case ' ':
        case 'Enter':
          event.preventDefault();
          toggleHeroPlayback();
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [posts.length, goToPrevious, goToNext, toggleHeroPlayback]);

  // Height classes
  const heightClasses = {
    sm: 'h-64 sm:h-80',
    md: 'h-80 sm:h-96',
    lg: 'h-96 sm:h-[32rem]',
    xl: 'h-[32rem] sm:h-screen',
  };

  if (isLoading) {
    return (
      <section className={cn('relative overflow-hidden bg-gray-900', heightClasses[height], className)}>
        <div className="absolute inset-0 bg-gradient-to-br from-orange-500 via-red-500 to-pink-500 animate-pulse" />
        <div className="relative z-10 flex h-full items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-2 border-white border-t-transparent" />
        </div>
      </section>
    );
  }

  if (error || !posts.length) {
    return (
      <section className={cn('relative overflow-hidden bg-gray-900', heightClasses[height], className)}>
        <div className="absolute inset-0 bg-gradient-to-br from-orange-500 via-red-500 to-pink-500" />
        <div className="relative z-10 flex h-full items-center justify-center">
          <div className="text-center text-white">
            <h2 className="text-2xl font-bold mb-2">暂无特色文章</h2>
            <p className="text-white/80">请稍后再试，或浏览其他内容。</p>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section
      className={cn('relative overflow-hidden', heightClasses[height], className)}
      role="region"
      aria-label="特色文章轮播"
      onTouchStart={isMobile ? handleTouchStart : undefined}
      onTouchMove={isMobile ? handleTouchMove : undefined}
      onTouchEnd={isMobile ? handleTouchEnd : undefined}
    >
      {/* Slides */}
      <div className="relative h-full w-full">
        {posts.map((post, index) => {
          // Ensure post object has required properties
          if (!post || !post.id || !post.title) {
            return null;
          }

          return (
            <div
              key={post.id}
              className={cn(
                'absolute inset-0 transition-opacity duration-1000 ease-in-out',
                index === currentSlide ? 'opacity-100 z-10' : 'opacity-0 z-0'
              )}
              role="tabpanel"
              aria-label={`幻灯片 ${index + 1}: ${post.title}`}
              aria-hidden={index !== currentSlide}
            >
              <PostSlide
                post={post}
                isActive={index === currentSlide}
                onReadMore={() => {
                  // Track interaction if needed
                }}
              />
            </div>
          );
        })}
      </div>

      {/* Controls */}
      {showControls && posts.length > 1 && (
        <>
          {/* Navigation Arrows */}
          <Button
            variant="ghost"
            size="sm"
            onClick={goToPrevious}
            className="absolute left-4 top-1/2 -translate-y-1/2 z-20 bg-black/20 backdrop-blur-sm border-white/20 text-white hover:bg-black/30 p-3"
            aria-label="上一张"
          >
            <ChevronLeft size={20} />
          </Button>

          <Button
            variant="ghost"
            size="sm"
            onClick={goToNext}
            className="absolute right-4 top-1/2 -translate-y-1/2 z-20 bg-black/20 backdrop-blur-sm border-white/20 text-white hover:bg-black/30 p-3"
            aria-label="下一张"
          >
            <ChevronRight size={20} />
          </Button>

          {/* Bottom Controls */}
          <div className="absolute bottom-6 left-1/2 -translate-x-1/2 z-20 flex items-center space-x-4">
            {/* Slide Indicators */}
            <SlideIndicator
              total={posts.length}
              current={currentSlide}
              onSelect={goToSlide}
            />

            {/* Play/Pause Button */}
            {!accessibility.reduceMotion && (
              <Button
                variant="ghost"
                size="sm"
                onClick={toggleHeroPlayback}
                className="bg-black/20 backdrop-blur-sm border-white/20 text-white hover:bg-black/30 p-2"
                aria-label={isPlaying ? '暂停播放' : '开始播放'}
              >
                {isPlaying ? <Pause size={16} /> : <Play size={16} />}
              </Button>
            )}
          </div>
        </>
      )}

      {/* Progress Bar */}
      {autoPlay && isPlaying && posts.length > 1 && (
        <div className="absolute bottom-0 left-0 right-0 z-20">
          <div className="h-1 bg-white/20">
            <div
              className="h-full bg-white transition-all duration-100 ease-linear"
              style={{
                width: `${((Date.now() % interval) / interval) * 100}%`,
              }}
            />
          </div>
        </div>
      )}

      {/* Screen Reader Navigation */}
      <div className="sr-only">
        <h2>特色文章轮播</h2>
        <p>当前显示第 {currentSlide + 1} 张，共 {posts.length} 张幻灯片</p>
        <p>使用左右箭头键导航，空格键暂停或播放</p>
      </div>
    </section>
  );
};

/**
 * Usage:
 * <HeroSection /> - Default hero section with featured posts
 * <HeroSection height="xl" /> - Full-screen hero section
 * <HeroSection autoPlay={false} /> - Manual navigation only
 * <HeroSection showControls={false} /> - No visible controls
 *
 * Features:
 * - Responsive design with mobile touch gestures
 * - Auto-play carousel with pause/play controls
 * - Keyboard navigation support (arrow keys, spacebar)
 * - Accessibility features with reduced motion support
 * - Parallax background images
 * - Smooth transitions and animations
 * - Post metadata display
 * - Share functionality
 * - Loading and error states
 * - Progress indicator
 * - Screen reader support
 * - Integration with home store for state management
 */

export default HeroSection;