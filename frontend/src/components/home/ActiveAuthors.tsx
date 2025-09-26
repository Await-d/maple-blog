/**
 * ActiveAuthors component - Display active authors with stats and rankings
 * Features: Author rankings, follower counts, recent activity, social links
 */

import React, { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import {
  Users,
  TrendingUp,
  Star,
  Crown,
  Medal,
  Award,
  User,
  Eye,
  FileText,
  Calendar,
  MapPin as _MapPin,
  ExternalLink as _ExternalLink,
  Github,
  Twitter,
  Linkedin,
  Globe,
  Heart as _Heart,
  MessageCircle as _MessageCircle,
  BookOpen as _BookOpen,
  Zap as _Zap,
  Filter as _Filter,
  MoreHorizontal,
  UserPlus,
  Grid,
  List,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { useActiveAuthors } from '../../services/home/homeApi';
import { useIsMobile, usePersonalizationActions } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { AuthorSummary } from '../../types/home';

interface ActiveAuthorsProps {
  className?: string;
  title?: string;
  count?: number;
  layout?: 'grid' | 'list' | 'ranking' | 'compact';
  showFollowButton?: boolean;
  showStats?: boolean;
  showSocial?: boolean;
}

interface AuthorCardProps {
  author: AuthorSummary;
  rank?: number;
  layout: 'grid' | 'list' | 'ranking' | 'compact';
  showFollowButton?: boolean;
  showStats?: boolean;
  showSocial?: boolean;
  isFollowing?: boolean;
  onFollow?: (authorId: string) => void;
  className?: string;
  style?: React.CSSProperties;
}

// Mock social media data (in real app, this would come from user profile)
const getSocialLinks = (userName: string) => [
  { platform: 'github', url: `https://github.com/${userName}`, icon: Github },
  { platform: 'twitter', url: `https://twitter.com/${userName}`, icon: Twitter },
  { platform: 'linkedin', url: `https://linkedin.com/in/${userName}`, icon: Linkedin },
  { platform: 'website', url: `https://${userName}.dev`, icon: Globe },
];

const getRankIcon = (rank: number) => {
  switch (rank) {
    case 1:
      return <Crown size={20} className="text-yellow-500" />;
    case 2:
      return <Medal size={20} className="text-gray-400" />;
    case 3:
      return <Award size={20} className="text-orange-500" />;
    default:
      return <Star size={16} className="text-gray-400" />;
  }
};

const getBadgeColor = (rank: number) => {
  if (rank <= 3) return 'from-yellow-400 to-orange-500';
  if (rank <= 10) return 'from-blue-400 to-purple-500';
  return 'from-gray-400 to-gray-600';
};

const AuthorCard: React.FC<AuthorCardProps> = ({
  author,
  rank,
  layout,
  showFollowButton = true,
  showStats = true,
  showSocial = false,
  isFollowing = false,
  onFollow,
  className,
  style,
}) => {
  const [imageLoaded, setImageLoaded] = useState(false);
  const [imageError, setImageError] = useState(false);
  const [socialVisible, setSocialVisible] = useState(false);

  const socialLinks = getSocialLinks(author.userName);

  const formatDate = (dateString?: string): string => {
    if (!dateString) return '暂未发布';
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

  if (layout === 'ranking') {
    return (
      <div
        className={cn(
          'flex items-center space-x-4 p-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 hover:shadow-md transition-all duration-200 group',
          className
        )}
        style={style}
      >
        {/* Rank */}
        <div className="flex-shrink-0 flex items-center justify-center w-12 h-12">
          {rank && rank <= 3 ? (
            <div className={cn(
              'w-10 h-10 rounded-full bg-gradient-to-r flex items-center justify-center text-white font-bold shadow-lg',
              getBadgeColor(rank)
            )}>
              {rank}
            </div>
          ) : (
            <div className="w-10 h-10 rounded-full bg-gray-100 dark:bg-gray-700 flex items-center justify-center text-gray-600 dark:text-gray-400 font-bold">
              {rank}
            </div>
          )}
        </div>

        {/* Avatar */}
        <Link
          to={`/author/${author.userName}`}
          className="flex-shrink-0"
        >
          {author.avatar && !imageError ? (
            <img
              src={author.avatar}
              alt={author.displayName || author.userName}
              className={cn(
                'w-12 h-12 rounded-full object-cover ring-2 ring-gray-200 dark:ring-gray-700 transition-all duration-200 group-hover:ring-orange-500',
                !imageLoaded && 'opacity-0'
              )}
              onLoad={handleImageLoad}
              onError={handleImageError}
            />
          ) : (
            <div className="w-12 h-12 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center ring-2 ring-gray-200 dark:ring-gray-700 group-hover:ring-orange-500 transition-all duration-200">
              <User size={20} className="text-gray-600 dark:text-gray-400" />
            </div>
          )}
        </Link>

        {/* Info */}
        <div className="flex-1 min-w-0">
          <Link
            to={`/author/${author.userName}`}
            className="block group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors"
          >
            <h3 className="font-semibold text-gray-900 dark:text-white truncate">
              {author.displayName || author.userName}
            </h3>
            {author.bio && (
              <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-1 mt-1">
                {author.bio}
              </p>
            )}
          </Link>
        </div>

        {/* Stats */}
        {showStats && (
          <div className="flex items-center space-x-4 text-sm text-gray-500 dark:text-gray-400">
            <div className="flex items-center space-x-1">
              <FileText size={14} />
              <span>{author.postCount}</span>
            </div>
            <div className="flex items-center space-x-1">
              <Eye size={14} />
              <span>{author.totalViews > 999 ? `${Math.floor(author.totalViews / 1000)}k` : author.totalViews}</span>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex-shrink-0">
          {showFollowButton && onFollow && (
            <Button
              variant={isFollowing ? 'outline' : 'primary'}
              size="sm"
              onClick={() => onFollow(author.id)}
              className="min-w-20"
            >
              {isFollowing ? '已关注' : '关注'}
            </Button>
          )}
        </div>
      </div>
    );
  }

  if (layout === 'list') {
    return (
      <div
        className={cn(
          'flex items-center space-x-4 p-4 hover:bg-gray-50 dark:hover:bg-gray-800 rounded-lg transition-colors group',
          className
        )}
        style={style}
      >
        {/* Avatar */}
        <Link to={`/author/${author.userName}`} className="flex-shrink-0">
          {author.avatar && !imageError ? (
            <img
              src={author.avatar}
              alt={author.displayName || author.userName}
              className={cn(
                'w-12 h-12 rounded-full object-cover transition-all duration-200 group-hover:scale-105',
                !imageLoaded && 'opacity-0'
              )}
              onLoad={handleImageLoad}
              onError={handleImageError}
            />
          ) : (
            <div className="w-12 h-12 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center group-hover:scale-105 transition-transform">
              <User size={20} className="text-gray-600 dark:text-gray-400" />
            </div>
          )}
        </Link>

        {/* Content */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between">
            <Link
              to={`/author/${author.userName}`}
              className="group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors"
            >
              <h3 className="font-semibold text-gray-900 dark:text-white">
                {author.displayName || author.userName}
              </h3>
            </Link>
            {rank && (
              <div className="flex items-center space-x-1">
                {getRankIcon(rank)}
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  #{rank}
                </span>
              </div>
            )}
          </div>

          {author.bio && (
            <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-1 mt-1">
              {author.bio}
            </p>
          )}

          {showStats && (
            <div className="flex items-center space-x-4 mt-2 text-xs text-gray-500 dark:text-gray-400">
              <span className="flex items-center space-x-1">
                <FileText size={12} />
                <span>{author.postCount} 篇文章</span>
              </span>
              <span className="flex items-center space-x-1">
                <Eye size={12} />
                <span>{author.totalViews.toLocaleString()} 浏览</span>
              </span>
              <span className="flex items-center space-x-1">
                <Calendar size={12} />
                <span>最近 {formatDate(author.lastPostDate)}</span>
              </span>
            </div>
          )}
        </div>
      </div>
    );
  }

  if (layout === 'compact') {
    return (
      <Link
        to={`/author/${author.userName}`}
        className={cn(
          'block p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 hover:shadow-md hover:-translate-y-1 transition-all duration-200 group',
          className
        )}
      >
        <div className="flex items-center space-x-3">
          {/* Avatar */}
          {author.avatar && !imageError ? (
            <img
              src={author.avatar}
              alt={author.displayName || author.userName}
              className={cn(
                'w-10 h-10 rounded-full object-cover',
                !imageLoaded && 'opacity-0'
              )}
              onLoad={handleImageLoad}
              onError={handleImageError}
            />
          ) : (
            <div className="w-10 h-10 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center">
              <User size={16} className="text-gray-600 dark:text-gray-400" />
            </div>
          )}

          {/* Info */}
          <div className="flex-1 min-w-0">
            <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors truncate">
              {author.displayName || author.userName}
            </h4>
            <div className="flex items-center space-x-2 text-xs text-gray-500 dark:text-gray-400">
              <span>{author.postCount} 篇</span>
              <span>•</span>
              <span>{author.totalViews > 999 ? `${Math.floor(author.totalViews / 1000)}k` : author.totalViews} 览</span>
            </div>
          </div>
        </div>
      </Link>
    );
  }

  // Grid Layout (Default)
  return (
    <div
      className={cn(
        'p-6 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 hover:shadow-lg dark:hover:shadow-2xl transition-all duration-300 group hover:-translate-y-1',
        className
      )}
      style={style}
    >
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-2">
          {rank && getRankIcon(rank)}
          {rank && rank <= 10 && (
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
              #{rank}
            </span>
          )}
        </div>
        <div className="flex items-center space-x-1">
          {showSocial && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setSocialVisible(!socialVisible)}
              className="p-1"
            >
              <MoreHorizontal size={16} />
            </Button>
          )}
        </div>
      </div>

      {/* Avatar & Info */}
      <Link to={`/author/${author.userName}`} className="block text-center mb-4">
        {author.avatar && !imageError ? (
          <img
            src={author.avatar}
            alt={author.displayName || author.userName}
            className={cn(
              'w-16 h-16 rounded-full object-cover mx-auto mb-3 ring-2 ring-gray-200 dark:ring-gray-700 group-hover:ring-orange-500 transition-all duration-200',
              !imageLoaded && 'opacity-0'
            )}
            onLoad={handleImageLoad}
            onError={handleImageError}
          />
        ) : (
          <div className="w-16 h-16 rounded-full bg-gray-300 dark:bg-gray-600 flex items-center justify-center mx-auto mb-3 ring-2 ring-gray-200 dark:ring-gray-700 group-hover:ring-orange-500 transition-all duration-200">
            <User size={28} className="text-gray-600 dark:text-gray-400" />
          </div>
        )}

        <h3 className="font-semibold text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors mb-1">
          {author.displayName || author.userName}
        </h3>

        {author.bio && (
          <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2">
            {author.bio}
          </p>
        )}
      </Link>

      {/* Stats */}
      {showStats && (
        <div className="grid grid-cols-2 gap-4 mb-4">
          <div className="text-center">
            <div className="text-lg font-bold text-gray-900 dark:text-white">
              {author.postCount}
            </div>
            <div className="text-xs text-gray-500 dark:text-gray-400">文章</div>
          </div>
          <div className="text-center">
            <div className="text-lg font-bold text-gray-900 dark:text-white">
              {author.totalViews > 999
                ? `${(author.totalViews / 1000).toFixed(1)}k`
                : author.totalViews
              }
            </div>
            <div className="text-xs text-gray-500 dark:text-gray-400">浏览</div>
          </div>
        </div>
      )}

      {/* Social Links */}
      {showSocial && socialVisible && (
        <div className="flex items-center justify-center space-x-2 mb-4">
          {socialLinks.slice(0, 4).map(({ platform, url, icon: Icon }) => (
            <a
              key={platform}
              href={url}
              target="_blank"
              rel="noopener noreferrer"
              className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
            >
              <Icon size={16} />
            </a>
          ))}
        </div>
      )}

      {/* Actions */}
      <div className="space-y-2">
        {showFollowButton && onFollow && (
          <Button
            variant={isFollowing ? 'outline' : 'primary'}
            size="sm"
            onClick={() => onFollow(author.id)}
            className="w-full"
          >
            <UserPlus size={14} className="mr-2" />
            {isFollowing ? '已关注' : '关注'}
          </Button>
        )}

        <div className="text-center text-xs text-gray-500 dark:text-gray-400">
          最近活动: {formatDate(author.lastPostDate)}
        </div>
      </div>
    </div>
  );
};

export const ActiveAuthors: React.FC<ActiveAuthorsProps> = ({
  className,
  title = '活跃作者',
  count = 12,
  layout = 'grid',
  showFollowButton = true,
  showStats = true,
  showSocial = false,
}) => {
  const _isMobile = useIsMobile();
  const { followAuthor, unfollowAuthor } = usePersonalizationActions();

  // Local state
  const [currentLayout, setCurrentLayout] = useState(layout);
  const [followingAuthors, setFollowingAuthors] = useState<Set<string>>(new Set());

  // API data
  const { data: authors, isLoading, error, refetch } = useActiveAuthors(count);

  // Sort authors by activity score (post count + views)
  const rankedAuthors = useMemo(() => {
    if (!authors) return [];
    return [...authors].sort((a, b) => {
      const scoreA = a.postCount * 100 + a.totalViews;
      const scoreB = b.postCount * 100 + b.totalViews;
      return scoreB - scoreA;
    });
  }, [authors]);

  const handleFollow = (authorId: string) => {
    if (followingAuthors.has(authorId)) {
      setFollowingAuthors(prev => {
        const next = new Set(prev);
        next.delete(authorId);
        return next;
      });
      unfollowAuthor(authorId);
    } else {
      setFollowingAuthors(prev => new Set(prev).add(authorId));
      followAuthor(authorId);
    }
  };

  if (isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <div className="h-8 w-32 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          <div className="flex items-center space-x-2">
            <div className="w-8 h-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
            <div className="w-8 h-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          </div>
        </div>
        <div className={cn(
          'grid gap-4',
          {
            'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4': currentLayout === 'grid',
            'grid-cols-1': currentLayout === 'list' || currentLayout === 'ranking',
            'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6': currentLayout === 'compact',
          }
        )}>
          {Array.from({ length: count }, (_, index) => (
            <div
              key={index}
              className="bg-gray-200 dark:bg-gray-700 rounded-xl animate-pulse"
              style={{ height: currentLayout === 'compact' ? '120px' : '240px' }}
            />
          ))}
        </div>
      </section>
    );
  }

  if (error || !authors?.length) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
        <div className="text-center py-12">
          <Users size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            暂无活跃作者
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error ? '加载失败，请稍后重试' : '等待作者发表文章'}
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
          <Users className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
          <span className="px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-sm font-medium rounded">
            {authors.length}
          </span>
        </div>

        {/* Layout Controls */}
        <div className="flex items-center space-x-2">
          <div className="flex items-center space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
            <Button
              variant={currentLayout === 'grid' ? 'primary' : 'ghost'}
              size="sm"
              onClick={() => setCurrentLayout('grid')}
              className="p-2"
              aria-label="网格布局"
            >
              <Grid size={16} />
            </Button>
            <Button
              variant={currentLayout === 'list' ? 'primary' : 'ghost'}
              size="sm"
              onClick={() => setCurrentLayout('list')}
              className="p-2"
              aria-label="列表布局"
            >
              <List size={16} />
            </Button>
            <Button
              variant={currentLayout === 'ranking' ? 'primary' : 'ghost'}
              size="sm"
              onClick={() => setCurrentLayout('ranking')}
              className="p-2"
              aria-label="排行布局"
            >
              <TrendingUp size={16} />
            </Button>
          </div>
        </div>
      </div>

      {/* Authors */}
      <div className={cn(
        'grid gap-4',
        {
          'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4': currentLayout === 'grid',
          'grid-cols-1': currentLayout === 'list' || currentLayout === 'ranking',
          'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6': currentLayout === 'compact',
        }
      )}>
        {rankedAuthors.map((author, index) => (
          <AuthorCard
            key={author.id}
            author={author}
            rank={index + 1}
            layout={currentLayout}
            showFollowButton={showFollowButton}
            showStats={showStats}
            showSocial={showSocial}
            isFollowing={followingAuthors.has(author.id)}
            onFollow={handleFollow}
            className="animate-fade-in"
            style={{ animationDelay: `${index * 100}ms` } as React.CSSProperties}
          />
        ))}
      </div>

      {/* All Authors Link */}
      <div className="text-center pt-4">
        <Link to="/authors">
          <Button variant="outline" size="lg">
            查看所有作者
            <Users size={16} className="ml-2" />
          </Button>
        </Link>
      </div>
    </section>
  );
};

/**
 * Usage:
 * <ActiveAuthors /> - Default grid layout with all features
 * <ActiveAuthors layout="ranking" count={10} /> - Top 10 ranking list
 * <ActiveAuthors layout="compact" showSocial={false} /> - Compact cards without social
 *
 * Features:
 * - Multiple responsive layouts (grid, list, ranking, compact)
 * - Author ranking system with badges and icons
 * - Follow/unfollow functionality with state management
 * - Social media links integration
 * - Comprehensive author statistics
 * - Recent activity tracking
 * - Loading states and error handling
 * - Responsive design with mobile optimization
 * - Smooth animations and hover effects
 * - Integration with personalization store
 * - Accessibility support with ARIA labels
 */

export default ActiveAuthors;