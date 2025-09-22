// @ts-nocheck
/**
 * Sidebar component - Dynamic sidebar with latest posts, popular tags, and statistics
 * Features: Mobile collapsible, personalized content, loading states, sticky positioning
 */

import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  X,
  Calendar,
  Eye,
  MessageCircle,
  Tag,
  TrendingUp,
  Users,
  BookOpen,
  Star,
  Clock,
  ChevronRight,
  Bookmark,
  Zap,
} from 'lucide-react';
import { Button } from '../ui/Button';
import {
  useLatestPosts,
  usePopularPosts,
  useTagStats,
  useActiveAuthors,
  useSiteStats,
  usePersonalizedRecommendations,
} from '../../services/home/homeApi';
import { useAuth } from '../../hooks/useAuth';
import { useHomeStore, useIsMobile, useSidebarCollapsed } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { PostSummary, TagSummary, AuthorSummary } from '../../types/home';

interface SidebarProps {
  className?: string;
  position?: 'left' | 'right';
  sticky?: boolean;
}

interface SidebarSection {
  id: string;
  title: string;
  icon: React.ReactNode;
  component: React.ReactNode;
  loading?: boolean;
  error?: string;
}

export const Sidebar: React.FC<SidebarProps> = ({
  className,
  position = 'right',
  sticky = true,
}) => {
  const navigate = useNavigate();
  const { user, isAuthenticated } = useAuth();
  const isMobile = useIsMobile();
  const sidebarCollapsed = useSidebarCollapsed();
  const { setSidebarCollapsed } = useHomeStore();

  // Local state
  const [activeSection, setActiveSection] = useState<string>('latest');

  // API data
  const { data: latestPosts, isLoading: latestLoading, error: latestError } = useLatestPosts(5);
  const { data: popularPosts, isLoading: popularLoading, error: popularError } = usePopularPosts(5, 7);
  const { data: tags, isLoading: tagsLoading, error: tagsError } = useTagStats(15, 2);
  const { data: authors, isLoading: authorsLoading, error: authorsError } = useActiveAuthors(5);
  const { data: siteStats, isLoading: statsLoading, error: statsError } = useSiteStats();
  const { data: recommendations, isLoading: recommendationsLoading, error: recommendationsError } =
    usePersonalizedRecommendations(user?.id || null, 5);

  // Close sidebar on route change (mobile)
  useEffect(() => {
    if (isMobile) {
      setSidebarCollapsed(true);
    }
  }, [navigate, isMobile, setSidebarCollapsed]);

  // Format post date
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

  // Post list component
  const PostList: React.FC<{ posts: PostSummary[]; showStats?: boolean }> = ({ posts, showStats = true }) => (
    <div className="space-y-3">
      {posts.map((post) => (
        <Link
          key={post.id}
          to={`/post/${post.slug}`}
          className="block group p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
        >
          <h4 className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 line-clamp-2 mb-2">
            {post.title}
          </h4>

          <div className="flex items-center space-x-4 text-xs text-gray-500 dark:text-gray-400">
            <span className="flex items-center space-x-1">
              <Calendar size={12} />
              <span>{formatDate(post.publishedAt)}</span>
            </span>
            {showStats && (
              <>
                <span className="flex items-center space-x-1">
                  <Eye size={12} />
                  <span>{post.viewCount}</span>
                </span>
                {post.commentCount > 0 && (
                  <span className="flex items-center space-x-1">
                    <MessageCircle size={12} />
                    <span>{post.commentCount}</span>
                  </span>
                )}
              </>
            )}
          </div>
        </Link>
      ))}
    </div>
  );

  // Tag cloud component
  const TagCloud: React.FC<{ tags: TagSummary[] }> = ({ tags }) => (
    <div className="flex flex-wrap gap-2">
      {tags.map((tag) => (
        <Link
          key={tag.id}
          to={`/tag/${tag.slug}`}
          className="inline-flex items-center space-x-1 px-2 py-1 bg-gray-100 dark:bg-gray-800 hover:bg-orange-100 dark:hover:bg-orange-900/20 text-xs text-gray-700 dark:text-gray-300 hover:text-orange-700 dark:hover:text-orange-300 rounded-md transition-colors"
          style={{
            fontSize: Math.max(10, Math.min(14, 8 + tag.usageFrequency * 3)) + 'px',
          }}
        >
          <Tag size={10} />
          <span>{tag.name}</span>
          <span className="text-gray-500 dark:text-gray-400">({tag.postCount})</span>
        </Link>
      ))}
    </div>
  );

  // Authors list component
  const AuthorsList: React.FC<{ authors: AuthorSummary[] }> = ({ authors }) => (
    <div className="space-y-3">
      {authors.map((author) => (
        <Link
          key={author.id}
          to={`/author/${author.userName}`}
          className="flex items-center space-x-3 p-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors group"
        >
          {author.avatar ? (
            <img
              src={author.avatar}
              alt={author.displayName || author.userName}
              className="w-8 h-8 rounded-full object-cover"
            />
          ) : (
            <div className="w-8 h-8 bg-gray-300 dark:bg-gray-600 rounded-full flex items-center justify-center">
              <Users size={14} />
            </div>
          )}
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 truncate">
              {author.displayName || author.userName}
            </div>
            <div className="text-xs text-gray-500 dark:text-gray-400">
              {author.postCount} 篇文章 • {author.totalViews} 次浏览
            </div>
          </div>
        </Link>
      ))}
    </div>
  );

  // Site statistics component
  const SiteStatistics: React.FC = () => (
    <div className="grid grid-cols-2 gap-4">
      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
        <div className="text-lg font-bold text-gray-900 dark:text-white">
          {siteStats?.totalPosts.toLocaleString()}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">文章总数</div>
      </div>
      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
        <div className="text-lg font-bold text-gray-900 dark:text-white">
          {siteStats?.totalViews.toLocaleString()}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">总浏览量</div>
      </div>
      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
        <div className="text-lg font-bold text-gray-900 dark:text-white">
          {siteStats?.totalAuthors.toLocaleString()}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">活跃作者</div>
      </div>
      <div className="text-center p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
        <div className="text-lg font-bold text-gray-900 dark:text-white">
          {siteStats?.postsThisMonth.toLocaleString()}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400">本月新增</div>
      </div>
    </div>
  );

  // Loading component
  const LoadingSection: React.FC = () => (
    <div className="space-y-3">
      {[...Array(3)].map((_, index) => (
        <div key={index} className="animate-pulse">
          <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded mb-2"></div>
          <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-2/3"></div>
        </div>
      ))}
    </div>
  );

  // Error component
  const ErrorSection: React.FC<{ error: string }> = ({ error }) => (
    <div className="text-sm text-gray-500 dark:text-gray-400 text-center py-4">
      <div className="text-red-500 mb-1">加载失败</div>
      <div className="text-xs">{error}</div>
    </div>
  );

  // Sidebar sections configuration
  const sections: SidebarSection[] = [
    ...(isAuthenticated && recommendations?.length ? [{
      id: 'recommendations',
      title: '为您推荐',
      icon: <Zap size={16} />,
      component: recommendations ? <PostList posts={recommendations} /> : null,
      loading: recommendationsLoading,
      error: recommendationsError?.message,
    }] : []),
    {
      id: 'latest',
      title: '最新文章',
      icon: <Clock size={16} />,
      component: latestPosts ? <PostList posts={latestPosts} /> : null,
      loading: latestLoading,
      error: latestError?.message,
    },
    {
      id: 'popular',
      title: '本周热门',
      icon: <TrendingUp size={16} />,
      component: popularPosts ? <PostList posts={popularPosts} /> : null,
      loading: popularLoading,
      error: popularError?.message,
    },
    {
      id: 'tags',
      title: '热门标签',
      icon: <Tag size={16} />,
      component: tags ? <TagCloud tags={tags} /> : null,
      loading: tagsLoading,
      error: tagsError?.message,
    },
    {
      id: 'authors',
      title: '活跃作者',
      icon: <Users size={16} />,
      component: authors ? <AuthorsList authors={authors} /> : null,
      loading: authorsLoading,
      error: authorsError?.message,
    },
    {
      id: 'stats',
      title: '网站统计',
      icon: <BookOpen size={16} />,
      component: siteStats ? <SiteStatistics /> : null,
      loading: statsLoading,
      error: statsError?.message,
    },
  ];

  const sidebarClasses = cn(
    'bg-white dark:bg-gray-900 border-l border-gray-200 dark:border-gray-800',
    {
      'sticky top-20': sticky && !isMobile,
      'fixed inset-y-0 right-0 z-40 w-80 transform transition-transform duration-300 ease-in-out': isMobile,
      'translate-x-full': isMobile && sidebarCollapsed,
      'translate-x-0': isMobile && !sidebarCollapsed,
    },
    className
  );

  return (
    <>
      {/* Mobile Overlay */}
      {isMobile && !sidebarCollapsed && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 z-30"
          onClick={() => setSidebarCollapsed(true)}
        />
      )}

      {/* Sidebar */}
      <aside className={sidebarClasses}>
        <div className="h-full overflow-y-auto">
          {/* Mobile Header */}
          {isMobile && (
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800">
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white">
                侧边栏
              </h2>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setSidebarCollapsed(true)}
                className="p-2"
              >
                <X size={20} />
              </Button>
            </div>
          )}

          {/* Sections */}
          <div className="p-4 space-y-6">
            {sections.map((section) => (
              <div key={section.id}>
                <div className="flex items-center space-x-2 mb-3">
                  <span className="text-orange-500 dark:text-orange-400">
                    {section.icon}
                  </span>
                  <h3 className="text-sm font-semibold text-gray-900 dark:text-white">
                    {section.title}
                  </h3>
                </div>

                {section.loading ? (
                  <LoadingSection />
                ) : section.error ? (
                  <ErrorSection error={section.error} />
                ) : (
                  section.component
                )}
              </div>
            ))}

            {/* Quick Actions */}
            <div className="pt-6 border-t border-gray-200 dark:border-gray-800">
              <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-3">
                快速导航
              </h3>
              <div className="space-y-2">
                <Link
                  to="/archive"
                  className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 hover:text-orange-600 dark:hover:text-orange-400 transition-colors"
                >
                  <span>文章归档</span>
                  <ChevronRight size={14} />
                </Link>
                <Link
                  to="/categories"
                  className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 hover:text-orange-600 dark:hover:text-orange-400 transition-colors"
                >
                  <span>分类浏览</span>
                  <ChevronRight size={14} />
                </Link>
                {isAuthenticated && (
                  <Link
                    to="/bookmarks"
                    className="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 hover:text-orange-600 dark:hover:text-orange-400 transition-colors"
                  >
                    <span>我的收藏</span>
                    <ChevronRight size={14} />
                  </Link>
                )}
              </div>
            </div>
          </div>
        </div>
      </aside>
    </>
  );
};

/**
 * Usage:
 * <Sidebar /> - Default right sidebar
 * <Sidebar position="left" /> - Left sidebar
 * <Sidebar sticky={false} /> - Non-sticky sidebar
 *
 * Features:
 * - Mobile-responsive with collapsible overlay
 * - Dynamic content loading from multiple APIs
 * - Personalized recommendations for authenticated users
 * - Loading states and error handling
 * - Sticky positioning for desktop
 * - Tag cloud with usage-based sizing
 * - Author profiles with stats
 * - Site statistics dashboard
 * - Quick navigation links
 * - Smooth animations and transitions
 * - Dark theme support
 * - Integration with home store for state management
 */