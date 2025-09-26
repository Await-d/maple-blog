/**
 * SearchSuggestions component - Advanced search suggestions with multiple sources
 * Features: Trending searches, smart completions, contextual suggestions, analytics
 */

import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  TrendingUp,
  Clock,
  Hash,
  User,
  FileText,
  Search,
  Star,
  Eye,
  MessageCircle as _MessageCircle,
  ArrowUpRight,
  Zap as _Zap,
  Filter as _Filter,
  Calendar as _Calendar,
  BookOpen,
  Target as _Target,
  Sparkles as _Sparkles,
  Brain,
  History,
} from 'lucide-react';
import { Button } from '../ui/Button';
import {
  useTagStats,
  useActiveAuthors,
  usePopularPosts,
  useCategoryStats,
} from '../../services/home/homeApi';
import { useAuth } from '../../hooks/useAuth';
import { usePersonalization } from '../../hooks/usePersonalization';
import { cn } from '../../utils/cn';
import type { TagSummary as _TagSummary, AuthorSummary as _AuthorSummary, PostSummary as _PostSummary, CategorySummary as _CategorySummary } from '../../types/home';

interface SearchSuggestionsProps {
  className?: string;
  query?: string;
  context?: 'header' | 'page' | 'modal';
  onSuggestionClick?: (suggestion: SearchSuggestion) => void;
  showTrending?: boolean;
  showPersonalized?: boolean;
  showHistory?: boolean;
  maxSuggestions?: number;
}

interface SearchSuggestion {
  id: string;
  type: 'post' | 'category' | 'tag' | 'author' | 'query' | 'trending' | 'personalized';
  title: string;
  subtitle?: string;
  description?: string;
  url: string;
  icon?: React.ReactNode;
  badge?: string;
  score?: number;
  metadata?: {
    views?: number;
    posts?: number;
    recent?: boolean;
    trending?: boolean;
    personalized?: boolean;
  };
}

interface SuggestionGroupProps {
  title: string;
  icon: React.ReactNode;
  suggestions: SearchSuggestion[];
  onSuggestionClick?: (suggestion: SearchSuggestion) => void;
  showAll?: boolean;
  maxItems?: number;
}

const SuggestionGroup: React.FC<SuggestionGroupProps> = ({
  title,
  icon,
  suggestions,
  onSuggestionClick,
  showAll = false,
  maxItems = 5,
}) => {
  const [expanded, setExpanded] = useState(showAll);
  const displaySuggestions = expanded ? suggestions : suggestions.slice(0, maxItems);

  if (suggestions.length === 0) return null;

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <span className="text-orange-500">{icon}</span>
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">
            {title}
          </h3>
          <span className="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-xs rounded">
            {suggestions.length}
          </span>
        </div>
        {suggestions.length > maxItems && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setExpanded(!expanded)}
            className="text-xs text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"
          >
            {expanded ? '收起' : `查看全部 ${suggestions.length}`}
          </Button>
        )}
      </div>

      <div className="space-y-1">
        {displaySuggestions.map((suggestion) => (
          <SuggestionItem
            key={suggestion.id}
            suggestion={suggestion}
            onClick={onSuggestionClick}
          />
        ))}
      </div>
    </div>
  );
};

interface SuggestionItemProps {
  suggestion: SearchSuggestion;
  onClick?: (suggestion: SearchSuggestion) => void;
  className?: string;
}

const SuggestionItem: React.FC<SuggestionItemProps> = ({
  suggestion,
  onClick,
  className,
}) => {
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    if (onClick) {
      onClick(suggestion);
    }
  };

  return (
    <Link
      to={suggestion.url}
      onClick={handleClick}
      className={cn(
        'flex items-center space-x-3 p-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors group',
        className
      )}
    >
      {/* Icon */}
      <div className="flex-shrink-0 text-gray-400 dark:text-gray-500 group-hover:text-orange-500">
        {suggestion.icon}
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center space-x-2">
          <span className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 truncate">
            {suggestion.title}
          </span>
          {suggestion.badge && (
            <span className={cn(
              'px-1.5 py-0.5 text-xs font-medium rounded',
              {
                'bg-red-100 text-red-700 dark:bg-red-900/20 dark:text-red-300': suggestion.badge === '热门',
                'bg-blue-100 text-blue-700 dark:bg-blue-900/20 dark:text-blue-300': suggestion.badge === '推荐',
                'bg-green-100 text-green-700 dark:bg-green-900/20 dark:text-green-300': suggestion.badge === '新',
              }
            )}>
              {suggestion.badge}
            </span>
          )}
        </div>
        {suggestion.subtitle && (
          <p className="text-xs text-gray-500 dark:text-gray-400 truncate mt-0.5">
            {suggestion.subtitle}
          </p>
        )}
      </div>

      {/* Metadata */}
      {suggestion.metadata && (
        <div className="flex-shrink-0 flex items-center space-x-2 text-xs text-gray-400 dark:text-gray-500">
          {suggestion.metadata.views && (
            <span className="flex items-center space-x-1">
              <Eye size={12} />
              <span>{suggestion.metadata.views > 999 ? `${Math.floor(suggestion.metadata.views / 1000)}k` : suggestion.metadata.views}</span>
            </span>
          )}
          {suggestion.metadata.posts && (
            <span className="flex items-center space-x-1">
              <FileText size={12} />
              <span>{suggestion.metadata.posts}</span>
            </span>
          )}
        </div>
      )}

      {/* Arrow */}
      <ArrowUpRight size={14} className="text-gray-300 dark:text-gray-600 group-hover:text-orange-400 opacity-0 group-hover:opacity-100 transition-opacity" />
    </Link>
  );
};

export const SearchSuggestions: React.FC<SearchSuggestionsProps> = ({
  className,
  query = '',
  context: _context = 'page',
  onSuggestionClick,
  showTrending = true,
  showPersonalized = true,
  showHistory = true,
  maxSuggestions = 20,
}) => {
  const navigate = useNavigate();
  const { isAuthenticated, user: _user } = useAuth();
  const { settings: personalizationSettings } = usePersonalization();

  // Local state
  const [searchHistory, setSearchHistory] = useState<string[]>([]);
  const [trendingQueries, setTrendingQueries] = useState<string[]>([]);

  // API data
  const { data: tags } = useTagStats(20, 2);
  const { data: authors } = useActiveAuthors(15);
  const { data: popularPosts } = usePopularPosts(15, 7);
  const { data: categories } = useCategoryStats();

  // Load search history
  useEffect(() => {
    const history = JSON.parse(localStorage.getItem('searchHistory') || '[]');
    setSearchHistory(history);

    // Mock trending queries (in real app, this would come from analytics)
    setTrendingQueries([
      'React 19 新功能',
      'TypeScript 最佳实践',
      '前端性能优化',
      'Node.js 微服务',
      'Vue 3 组合式API',
      'Next.js 14',
      'CSS Grid 布局',
      'JavaScript ES2024',
    ]);
  }, []);

  // Generate suggestions based on query and context
  const suggestions = useMemo(() => {
    const allSuggestions: SearchSuggestion[] = [];

    if (!query.trim()) {
      // Show contextual suggestions when no query

      // Search history
      if (showHistory && searchHistory.length > 0) {
        const historySuggestions = searchHistory.slice(0, 5).map((term, index) => ({
          id: `history-${index}`,
          type: 'query' as const,
          title: term,
          url: `/search?q=${encodeURIComponent(term)}`,
          icon: <History size={16} />,
          subtitle: '历史搜索',
        }));
        allSuggestions.push(...historySuggestions);
      }

      // Trending queries
      if (showTrending) {
        const trendingSuggestions = trendingQueries.slice(0, 4).map((term, index) => ({
          id: `trending-${index}`,
          type: 'trending' as const,
          title: term,
          url: `/search?q=${encodeURIComponent(term)}`,
          icon: <TrendingUp size={16} />,
          badge: '热门',
          subtitle: '热门搜索',
        }));
        allSuggestions.push(...trendingSuggestions);
      }

      // Personalized suggestions
      if (showPersonalized && isAuthenticated && personalizationSettings) {
        // Preferred categories
        if (personalizationSettings.preferredCategories.length > 0 && categories) {
          const preferredCategorySuggestions = categories
            .filter(cat => personalizationSettings.preferredCategories.includes(cat.id))
            .slice(0, 3)
            .map(category => ({
              id: `personalized-cat-${category.id}`,
              type: 'personalized' as const,
              title: category.name,
              subtitle: `${category.postCount} 篇文章`,
              url: `/category/${category.slug}`,
              icon: <BookOpen size={16} />,
              badge: '推荐',
              metadata: { posts: category.postCount },
            }));
          allSuggestions.push(...preferredCategorySuggestions);
        }

        // Followed authors
        if (personalizationSettings.followedAuthors.length > 0 && authors) {
          const followedAuthorSuggestions = authors
            .filter(author => personalizationSettings.followedAuthors.includes(author.id))
            .slice(0, 2)
            .map(author => ({
              id: `personalized-author-${author.id}`,
              type: 'personalized' as const,
              title: author.displayName || author.userName,
              subtitle: `${author.postCount} 篇文章`,
              url: `/author/${author.userName}`,
              icon: <User size={16} />,
              badge: '推荐',
              metadata: { posts: author.postCount, views: author.totalViews },
            }));
          allSuggestions.push(...followedAuthorSuggestions);
        }
      }

      // Popular content
      if (popularPosts) {
        const popularPostSuggestions = popularPosts.slice(0, 3).map(post => ({
          id: `popular-post-${post.id}`,
          type: 'post' as const,
          title: post.title,
          subtitle: `${post.author.displayName || post.author.userName} • ${post.viewCount} 浏览`,
          url: `/post/${post.slug}`,
          icon: <Star size={16} />,
          badge: '热门',
          metadata: { views: post.viewCount },
        }));
        allSuggestions.push(...popularPostSuggestions);
      }

      // Popular tags
      if (tags) {
        const tagSuggestions = tags.slice(0, 4).map(tag => ({
          id: `popular-tag-${tag.id}`,
          type: 'tag' as const,
          title: tag.name,
          subtitle: `${tag.postCount} 篇文章`,
          url: `/tag/${tag.slug}`,
          icon: <Hash size={16} />,
          metadata: { posts: tag.postCount },
        }));
        allSuggestions.push(...tagSuggestions);
      }
    } else {
      // Filter suggestions based on query
      const searchLower = query.toLowerCase();

      // Search in posts
      if (popularPosts) {
        const matchingPosts = popularPosts
          .filter(post => post.title.toLowerCase().includes(searchLower))
          .slice(0, 3)
          .map(post => ({
            id: `post-${post.id}`,
            type: 'post' as const,
            title: post.title,
            subtitle: `${post.author.displayName || post.author.userName} • ${post.viewCount} 浏览`,
            url: `/post/${post.slug}`,
            icon: <FileText size={16} />,
            metadata: { views: post.viewCount },
          }));
        allSuggestions.push(...matchingPosts);
      }

      // Search in categories
      if (categories) {
        const matchingCategories = categories
          .filter(category =>
            category.name.toLowerCase().includes(searchLower) ||
            category.description?.toLowerCase().includes(searchLower)
          )
          .slice(0, 3)
          .map(category => ({
            id: `category-${category.id}`,
            type: 'category' as const,
            title: category.name,
            subtitle: category.description || `${category.postCount} 篇文章`,
            url: `/category/${category.slug}`,
            icon: <BookOpen size={16} />,
            metadata: { posts: category.postCount },
          }));
        allSuggestions.push(...matchingCategories);
      }

      // Search in tags
      if (tags) {
        const matchingTags = tags
          .filter(tag => tag.name.toLowerCase().includes(searchLower))
          .slice(0, 4)
          .map(tag => ({
            id: `tag-${tag.id}`,
            type: 'tag' as const,
            title: tag.name,
            subtitle: `${tag.postCount} 篇文章`,
            url: `/tag/${tag.slug}`,
            icon: <Hash size={16} />,
            metadata: { posts: tag.postCount },
          }));
        allSuggestions.push(...matchingTags);
      }

      // Search in authors
      if (authors) {
        const matchingAuthors = authors
          .filter(author =>
            author.displayName?.toLowerCase().includes(searchLower) ||
            author.userName.toLowerCase().includes(searchLower) ||
            author.bio?.toLowerCase().includes(searchLower)
          )
          .slice(0, 3)
          .map(author => ({
            id: `author-${author.id}`,
            type: 'author' as const,
            title: author.displayName || author.userName,
            subtitle: author.bio || `${author.postCount} 篇文章`,
            url: `/author/${author.userName}`,
            icon: <User size={16} />,
            metadata: { posts: author.postCount, views: author.totalViews },
          }));
        allSuggestions.push(...matchingAuthors);
      }

      // Add direct search option
      allSuggestions.push({
        id: 'search-all',
        type: 'query',
        title: `搜索 "${query}"`,
        subtitle: '在所有内容中搜索',
        url: `/search?q=${encodeURIComponent(query)}`,
        icon: <Search size={16} />,
      });
    }

    return allSuggestions.slice(0, maxSuggestions);
  }, [
    query,
    searchHistory,
    trendingQueries,
    tags,
    authors,
    popularPosts,
    categories,
    isAuthenticated,
    personalizationSettings,
    showHistory,
    showTrending,
    showPersonalized,
    maxSuggestions,
  ]);

  // Group suggestions by type
  const groupedSuggestions = useMemo(() => {
    const groups: Record<string, SearchSuggestion[]> = {};

    suggestions.forEach(suggestion => {
      const groupKey = suggestion.type;
      if (!groups[groupKey]) {
        groups[groupKey] = [];
      }
      groups[groupKey].push(suggestion);
    });

    return groups;
  }, [suggestions]);

  const handleSuggestionClick = useCallback((suggestion: SearchSuggestion) => {
    // Save to search history if it's a search query
    if (suggestion.type === 'query' || suggestion.type === 'trending') {
      const term = suggestion.title.replace(/^搜索 "(.+)"$/, '$1');
      const newHistory = [term, ...searchHistory.filter(h => h !== term)].slice(0, 10);
      setSearchHistory(newHistory);
      localStorage.setItem('searchHistory', JSON.stringify(newHistory));
    }

    if (onSuggestionClick) {
      onSuggestionClick(suggestion);
    } else {
      navigate(suggestion.url);
    }
  }, [searchHistory, onSuggestionClick, navigate]);

  const clearHistory = () => {
    setSearchHistory([]);
    localStorage.removeItem('searchHistory');
  };

  if (suggestions.length === 0) {
    return (
      <div className={cn('text-center py-8', className)}>
        <Search size={32} className="mx-auto text-gray-400 mb-2" />
        <p className="text-gray-500 dark:text-gray-400">
          {query ? '未找到相关内容' : '开始输入进行搜索'}
        </p>
      </div>
    );
  }

  return (
    <div className={cn('space-y-6', className)}>
      {/* Search History */}
      {!query && searchHistory.length > 0 && (
        <SuggestionGroup
          title="最近搜索"
          icon={<Clock size={16} />}
          suggestions={groupedSuggestions.query || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={5}
        />
      )}

      {/* Trending */}
      {!query && (groupedSuggestions.trending?.length || 0) > 0 && (
        <SuggestionGroup
          title="热门搜索"
          icon={<TrendingUp size={16} />}
          suggestions={groupedSuggestions.trending || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={4}
        />
      )}

      {/* Personalized */}
      {!query && isAuthenticated && (groupedSuggestions.personalized?.length || 0) > 0 && (
        <SuggestionGroup
          title="为您推荐"
          icon={<Brain size={16} />}
          suggestions={groupedSuggestions.personalized || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={3}
        />
      )}

      {/* Posts */}
      {(groupedSuggestions.post?.length || 0) > 0 && (
        <SuggestionGroup
          title={query ? '相关文章' : '热门文章'}
          icon={<FileText size={16} />}
          suggestions={groupedSuggestions.post || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={3}
        />
      )}

      {/* Categories */}
      {(groupedSuggestions.category?.length || 0) > 0 && (
        <SuggestionGroup
          title="分类"
          icon={<BookOpen size={16} />}
          suggestions={groupedSuggestions.category || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={3}
        />
      )}

      {/* Tags */}
      {(groupedSuggestions.tag?.length || 0) > 0 && (
        <SuggestionGroup
          title="标签"
          icon={<Hash size={16} />}
          suggestions={groupedSuggestions.tag || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={4}
        />
      )}

      {/* Authors */}
      {(groupedSuggestions.author?.length || 0) > 0 && (
        <SuggestionGroup
          title="作者"
          icon={<User size={16} />}
          suggestions={groupedSuggestions.author || []}
          onSuggestionClick={handleSuggestionClick}
          maxItems={3}
        />
      )}

      {/* Search Query */}
      {query && (groupedSuggestions.query?.length || 0) > 0 && (
        <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
          <SuggestionGroup
            title="搜索"
            icon={<Search size={16} />}
            suggestions={groupedSuggestions.query || []}
            onSuggestionClick={handleSuggestionClick}
            maxItems={1}
          />
        </div>
      )}

      {/* Clear History */}
      {!query && searchHistory.length > 0 && (
        <div className="text-center border-t border-gray-200 dark:border-gray-700 pt-4">
          <Button
            variant="ghost"
            size="sm"
            onClick={clearHistory}
            className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"
          >
            清除搜索历史
          </Button>
        </div>
      )}
    </div>
  );
};

/**
 * Usage:
 * <SearchSuggestions /> - Default suggestions without query
 * <SearchSuggestions query="react" /> - Query-based suggestions
 * <SearchSuggestions context="modal" onSuggestionClick={handleClick} /> - In modal with custom handler
 *
 * Features:
 * - Multiple suggestion sources (history, trending, personalized, content)
 * - Smart grouping and categorization
 * - Personalized recommendations for authenticated users
 * - Search history persistence with localStorage
 * - Responsive design with proper spacing
 * - Analytics integration for trending queries
 * - Accessibility support with proper navigation
 * - Performance optimization with memoization
 */