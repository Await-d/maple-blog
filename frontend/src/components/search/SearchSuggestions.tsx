/**
 * SearchSuggestions Component
 * 搜索建议组件 - 显示自动完成建议、搜索历史、热门搜索
 */

import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  Search,
  Clock,
  TrendingUp,
  Hash,
  Folder,
  User,
  FileText,
  X,
  ArrowUpRight,
  Loader,
} from 'lucide-react';
import {
  AutoCompleteSuggestion,
  SearchHistory,
  DEFAULT_SEARCH_CONFIG,
} from '@/types/search';
import { useSearchStore } from '@/stores/searchStore';
import { searchApi } from '@/services/search/searchApi';

interface SearchSuggestionsProps {
  query: string;
  suggestions: AutoCompleteSuggestion[];
  history: SearchHistory[];
  loading?: boolean;
  onSelect: (suggestion: string) => void;
  onClose: () => void;
}

interface SuggestionGroup {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  items: AutoCompleteSuggestion[];
  color: string;
}

export default function SearchSuggestions({
  query,
  suggestions,
  history,
  loading = false,
  onSelect,
  onClose,
}: SearchSuggestionsProps) {
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const [popularQueries, setPopularQueries] = useState<string[]>([]);
  const [enhancedSuggestions, setEnhancedSuggestions] = useState<{
    queries: AutoCompleteSuggestion[];
    categories: AutoCompleteSuggestion[];
    tags: AutoCompleteSuggestion[];
    authors: AutoCompleteSuggestion[];
    posts: AutoCompleteSuggestion[];
  }>({ queries: [], categories: [], tags: [], authors: [], posts: [] });

  const { removeFromHistory } = useSearchStore();
  const containerRef = useRef<HTMLDivElement>(null);

  // 加载热门搜索
  useEffect(() => {
    const loadPopularQueries = async () => {
      try {
        const popular = await searchApi.getPopularQueries(5);
        setPopularQueries(popular);
      } catch (error) {
        console.warn('Failed to load popular queries:', error);
      }
    };

    loadPopularQueries();
  }, []);

  // 加载增强建议
  useEffect(() => {
    const loadEnhancedSuggestions = async () => {
      if (query && query.length >= DEFAULT_SEARCH_CONFIG.minQueryLength) {
        try {
          const enhanced = await searchApi.getEnhancedSuggestions(query);
          setEnhancedSuggestions(enhanced);
        } catch (error) {
          console.warn('Failed to load enhanced suggestions:', error);
        }
      }
    };

    loadEnhancedSuggestions();
  }, [query]);

  // 获取所有可选项
  const getAllSelectableItems = useCallback((): { text: string; type: string }[] => {
    const items: { text: string; type: string }[] = [];

    // 添加历史记录
    if (!query && history.length > 0) {
      history.slice(0, 5).forEach(h => {
        items.push({ text: h.query, type: 'history' });
      });
    }

    // 添加热门搜索
    if (!query && popularQueries.length > 0) {
      popularQueries.forEach(q => {
        items.push({ text: q, type: 'popular' });
      });
    }

    // 添加搜索建议
    suggestions.forEach(s => {
      items.push({ text: s.text, type: s.type });
    });

    // 添加增强建议
    Object.entries(enhancedSuggestions).forEach(([type, items]) => {
      items.forEach(item => {
        items.push({ text: item.text, type: type as 'query' | 'category' | 'tag' | 'author' | 'title' });
      });
    });

    return items;
  }, [query, history, popularQueries, suggestions, enhancedSuggestions]);

  // 键盘导航
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const allItems = getAllSelectableItems();

      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          setSelectedIndex(prev => (prev + 1) % allItems.length);
          break;
        case 'ArrowUp':
          e.preventDefault();
          setSelectedIndex(prev => (prev - 1 + allItems.length) % allItems.length);
          break;
        case 'Enter':
          e.preventDefault();
          if (selectedIndex >= 0 && allItems[selectedIndex]) {
            onSelect(allItems[selectedIndex].text);
          }
          break;
        case 'Escape':
          e.preventDefault();
          onClose();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [selectedIndex, onSelect, onClose, getAllSelectableItems]);

  // 构建建议分组
  const buildSuggestionGroups = (): SuggestionGroup[] => {
    const groups: SuggestionGroup[] = [];

    // 查询建议
    if (enhancedSuggestions.queries.length > 0) {
      groups.push({
        title: '搜索建议',
        icon: Search,
        items: enhancedSuggestions.queries,
        color: 'text-blue-500',
      });
    }

    // 文章建议
    if (enhancedSuggestions.posts.length > 0) {
      groups.push({
        title: '相关文章',
        icon: FileText,
        items: enhancedSuggestions.posts,
        color: 'text-green-500',
      });
    }

    // 分类建议
    if (enhancedSuggestions.categories.length > 0) {
      groups.push({
        title: '分类',
        icon: Folder,
        items: enhancedSuggestions.categories,
        color: 'text-purple-500',
      });
    }

    // 标签建议
    if (enhancedSuggestions.tags.length > 0) {
      groups.push({
        title: '标签',
        icon: Hash,
        items: enhancedSuggestions.tags,
        color: 'text-orange-500',
      });
    }

    // 作者建议
    if (enhancedSuggestions.authors.length > 0) {
      groups.push({
        title: '作者',
        icon: User,
        items: enhancedSuggestions.authors,
        color: 'text-indigo-500',
      });
    }

    return groups;
  };

  const suggestionGroups = buildSuggestionGroups();
  const showHistory = !query && history.length > 0;
  const showPopular = !query && popularQueries.length > 0;

  // 高亮匹配文本
  const highlightMatch = (text: string, query: string): React.ReactNode => {
    if (!query) return text;

    const regex = new RegExp(`(${query})`, 'gi');
    const parts = text.split(regex);

    return parts.map((part, index) => {
      if (regex.test(part)) {
        return (
          <mark key={index} className="bg-yellow-200 text-yellow-900 rounded">
            {part}
          </mark>
        );
      }
      return part;
    });
  };

  // 处理历史记录删除
  const handleRemoveHistory = (e: React.MouseEvent, historyId: string) => {
    e.stopPropagation();
    removeFromHistory(historyId);
  };

  if (loading) {
    return (
      <div className="absolute top-full left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-lg z-50 mt-1">
        <div className="flex items-center justify-center py-8">
          <Loader className="h-5 w-5 animate-spin text-gray-400" />
          <span className="ml-2 text-sm text-gray-500">搜索中...</span>
        </div>
      </div>
    );
  }

  const hasContent = showHistory || showPopular || suggestionGroups.length > 0;

  if (!hasContent) {
    return null;
  }

  return (
    <div
      ref={containerRef}
      className="absolute top-full left-0 right-0 bg-white border border-gray-200 rounded-lg shadow-lg z-50 mt-1 max-h-96 overflow-y-auto"
    >
      {/* 搜索历史 */}
      {showHistory && (
        <div className="p-2 border-b border-gray-100">
          <div className="flex items-center text-xs font-medium text-gray-500 uppercase tracking-wide mb-2 px-2">
            <Clock className="h-3 w-3 mr-1" />
            搜索历史
          </div>
          <div className="space-y-1">
            {history.slice(0, 5).map((historyItem, index) => (
              <button
                key={historyItem.id}
                onClick={() => onSelect(historyItem.query)}
                className={`
                  w-full text-left px-3 py-2 rounded-md text-sm
                  hover:bg-gray-50 flex items-center justify-between group
                  ${selectedIndex === index ? 'bg-blue-50 text-blue-700' : 'text-gray-700'}
                `}
              >
                <div className="flex items-center min-w-0 flex-1">
                  <Clock className="h-4 w-4 text-gray-400 mr-2 flex-shrink-0" />
                  <span className="truncate">{historyItem.query}</span>
                  {historyItem.resultCount > 0 && (
                    <span className="ml-2 text-xs text-gray-400">
                      {historyItem.resultCount} 个结果
                    </span>
                  )}
                </div>
                <button
                  onClick={(e) => handleRemoveHistory(e, historyItem.id)}
                  className="opacity-0 group-hover:opacity-100 p-1 text-gray-400 hover:text-gray-600"
                  aria-label="删除历史记录"
                >
                  <X className="h-3 w-3" />
                </button>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* 热门搜索 */}
      {showPopular && (
        <div className="p-2 border-b border-gray-100">
          <div className="flex items-center text-xs font-medium text-gray-500 uppercase tracking-wide mb-2 px-2">
            <TrendingUp className="h-3 w-3 mr-1" />
            热门搜索
          </div>
          <div className="space-y-1">
            {popularQueries.map((popularQuery, index) => (
              <button
                key={popularQuery}
                onClick={() => onSelect(popularQuery)}
                className={`
                  w-full text-left px-3 py-2 rounded-md text-sm
                  hover:bg-gray-50 flex items-center
                  ${selectedIndex === (showHistory ? history.length : 0) + index
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-700'
                  }
                `}
              >
                <TrendingUp className="h-4 w-4 text-red-400 mr-2 flex-shrink-0" />
                <span className="truncate">{popularQuery}</span>
                <ArrowUpRight className="h-3 w-3 text-gray-400 ml-auto opacity-0 group-hover:opacity-100" />
              </button>
            ))}
          </div>
        </div>
      )}

      {/* 搜索建议分组 */}
      {suggestionGroups.map((group, groupIndex) => (
        <div key={group.title} className="p-2 border-b border-gray-100 last:border-b-0">
          <div className="flex items-center text-xs font-medium text-gray-500 uppercase tracking-wide mb-2 px-2">
            <group.icon className={`h-3 w-3 mr-1 ${group.color}`} />
            {group.title}
          </div>
          <div className="space-y-1">
            {group.items.map((item, index) => {
              const globalIndex =
                (showHistory ? Math.min(5, history.length) : 0) +
                (showPopular ? popularQueries.length : 0) +
                suggestionGroups.slice(0, groupIndex).reduce((acc, g) => acc + g.items.length, 0) +
                index;

              return (
                <button
                  key={`${item.type}-${item.text}`}
                  onClick={() => onSelect(item.text)}
                  className={`
                    w-full text-left px-3 py-2 rounded-md text-sm
                    hover:bg-gray-50 flex items-center group
                    ${selectedIndex === globalIndex ? 'bg-blue-50 text-blue-700' : 'text-gray-700'}
                  `}
                >
                  <group.icon className={`h-4 w-4 mr-2 flex-shrink-0 ${group.color}`} />
                  <div className="min-w-0 flex-1">
                    <div className="truncate">
                      {highlightMatch(item.text, query)}
                    </div>
                    {item.count && (
                      <div className="text-xs text-gray-400">
                        {item.count} 个结果
                      </div>
                    )}
                  </div>
                  <ArrowUpRight className="h-3 w-3 text-gray-400 ml-2 opacity-0 group-hover:opacity-100 flex-shrink-0" />
                </button>
              );
            })}
          </div>
        </div>
      ))}

      {/* 快捷键提示 */}
      <div className="p-3 bg-gray-50 text-xs text-gray-500 border-t">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <span className="flex items-center">
              <kbd className="px-1.5 py-0.5 bg-white border border-gray-300 rounded text-xs">
                ↑↓
              </kbd>
              <span className="ml-1">选择</span>
            </span>
            <span className="flex items-center">
              <kbd className="px-1.5 py-0.5 bg-white border border-gray-300 rounded text-xs">
                Enter
              </kbd>
              <span className="ml-1">搜索</span>
            </span>
            <span className="flex items-center">
              <kbd className="px-1.5 py-0.5 bg-white border border-gray-300 rounded text-xs">
                Esc
              </kbd>
              <span className="ml-1">关闭</span>
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}