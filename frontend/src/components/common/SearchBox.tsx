// @ts-nocheck
/**
 * SearchBox component - Advanced search with suggestions and history
 * Features: Real-time suggestions, search history, keyboard navigation, debounced search
 */

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, X, Clock, TrendingUp as _TrendingUp, Hash, User, FileText } from 'lucide-react';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { useTagStats, useActiveAuthors } from '../../services/home/homeApi';
import { cn } from '../../utils/cn';

interface SearchBoxProps {
  className?: string;
  onClose?: () => void;
  autoFocus?: boolean;
  placeholder?: string;
  size?: 'sm' | 'md' | 'lg';
}

interface SearchSuggestion {
  id: string;
  type: 'post' | 'category' | 'tag' | 'author' | 'history';
  title: string;
  subtitle?: string;
  url: string;
  icon?: React.ReactNode;
}

export const SearchBox: React.FC<SearchBoxProps> = ({
  className,
  onClose,
  autoFocus = false,
  placeholder = '搜索文章、分类、标签...',
  size: _size = 'md',
}) => {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);

  // State
  const [query, setQuery] = useState('');
  const [suggestions, setSuggestions] = useState<SearchSuggestion[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const [isLoading, setIsLoading] = useState(false);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [searchHistory, setSearchHistory] = useState<string[]>([]);

  // Data from API
  const { data: tags } = useTagStats(20, 2);
  const { data: authors } = useActiveAuthors(10);

  // Load search history from localStorage
  useEffect(() => {
    const history = JSON.parse(localStorage.getItem('searchHistory') || '[]');
    setSearchHistory(history);
  }, []);

  // Auto focus
  useEffect(() => {
    if (autoFocus && inputRef.current) {
      inputRef.current.focus();
    }
  }, [autoFocus]);

  // Generate suggestions based on query
  const generateSuggestions = useCallback(async (searchQuery: string): Promise<SearchSuggestion[]> => {
    if (!searchQuery.trim()) {
      // Show recent searches and popular content when no query
      const historySuggestions: SearchSuggestion[] = searchHistory.slice(0, 5).map((term, index) => ({
        id: `history-${index}`,
        type: 'history',
        title: term,
        url: `/search?q=${encodeURIComponent(term)}`,
        icon: <Clock size={14} />,
      }));

      const popularTags: SearchSuggestion[] = (tags || []).slice(0, 3).map(tag => ({
        id: `tag-${tag.id}`,
        type: 'tag',
        title: tag.name,
        subtitle: `${tag.postCount} 篇文章`,
        url: `/tag/${tag.slug}`,
        icon: <Hash size={14} />,
      }));

      return [...historySuggestions, ...popularTags];
    }

    const searchLower = searchQuery.toLowerCase();
    const results: SearchSuggestion[] = [];

    // Search in tags
    if (tags) {
      const matchingTags = tags
        .filter(tag => tag.name.toLowerCase().includes(searchLower))
        .slice(0, 3)
        .map(tag => ({
          id: `tag-${tag.id}`,
          type: 'tag' as const,
          title: tag.name,
          subtitle: `${tag.postCount} 篇文章`,
          url: `/tag/${tag.slug}`,
          icon: <Hash size={14} />,
        }));
      results.push(...matchingTags);
    }

    // Search in authors
    if (authors) {
      const matchingAuthors = authors
        .filter(author =>
          author.displayName?.toLowerCase().includes(searchLower) ||
          author.userName.toLowerCase().includes(searchLower)
        )
        .slice(0, 2)
        .map(author => ({
          id: `author-${author.id}`,
          type: 'author' as const,
          title: author.displayName || author.userName,
          subtitle: `${author.postCount} 篇文章`,
          url: `/author/${author.userName}`,
          icon: <User size={14} />,
        }));
      results.push(...matchingAuthors);
    }

    // Add direct search option
    results.push({
      id: 'search-all',
      type: 'post',
      title: `搜索 "${searchQuery}"`,
      subtitle: '在所有文章中搜索',
      url: `/search?q=${encodeURIComponent(searchQuery)}`,
      icon: <Search size={14} />,
    });

    return results;
  }, [tags, authors, searchHistory]);

  // Debounced search
  useEffect(() => {
    const timeoutId = setTimeout(async () => {
      if (query.trim()) {
        setIsLoading(true);
        try {
          const results = await generateSuggestions(query);
          setSuggestions(results);
          setShowSuggestions(true);
          setSelectedIndex(-1);
        } catch (error) {
          console.error('Search suggestions failed:', error);
          setSuggestions([]);
        } finally {
          setIsLoading(false);
        }
      } else {
        const results = await generateSuggestions('');
        setSuggestions(results);
        setShowSuggestions(results.length > 0);
        setSelectedIndex(-1);
      }
    }, 300);

    return () => clearTimeout(timeoutId);
  }, [query, generateSuggestions]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!showSuggestions || suggestions.length === 0) return;

      switch (event.key) {
        case 'ArrowDown':
          event.preventDefault();
          setSelectedIndex(prev => (prev + 1) % suggestions.length);
          break;
        case 'ArrowUp':
          event.preventDefault();
          setSelectedIndex(prev => prev <= 0 ? suggestions.length - 1 : prev - 1);
          break;
        case 'Enter':
          event.preventDefault();
          if (selectedIndex >= 0 && selectedIndex < suggestions.length) {
            handleSuggestionClick(suggestions[selectedIndex]);
          } else if (query.trim()) {
            handleSearch(query);
          }
          break;
        case 'Escape':
          setShowSuggestions(false);
          if (onClose) onClose();
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [showSuggestions, suggestions, selectedIndex, query, onClose]);

  const saveToHistory = useCallback((term: string) => {
    const newHistory = [term, ...searchHistory.filter(h => h !== term)].slice(0, 10);
    setSearchHistory(newHistory);
    localStorage.setItem('searchHistory', JSON.stringify(newHistory));
  }, [searchHistory]);

  const handleSearch = useCallback((searchTerm: string) => {
    if (!searchTerm.trim()) return;

    saveToHistory(searchTerm.trim());
    navigate(`/search?q=${encodeURIComponent(searchTerm.trim())}`);
    setShowSuggestions(false);
    if (onClose) onClose();
  }, [navigate, onClose, saveToHistory]);

  const handleSuggestionClick = useCallback((suggestion: SearchSuggestion) => {
    if (suggestion.type === 'post' || suggestion.type === 'history') {
      saveToHistory(query.trim() || suggestion.title);
    }
    navigate(suggestion.url);
    setShowSuggestions(false);
    if (onClose) onClose();
  }, [query, navigate, onClose, saveToHistory]);

  // Close suggestions when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        suggestionsRef.current &&
        !suggestionsRef.current.contains(event.target as Node) &&
        inputRef.current &&
        !inputRef.current.contains(event.target as Node)
      ) {
        setShowSuggestions(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const clearHistory = () => {
    setSearchHistory([]);
    localStorage.removeItem('searchHistory');
    setSuggestions([]);
  };

  const handleInputChange = (value: string) => {
    setQuery(value);
    if (!value.trim()) {
      setShowSuggestions(false);
    }
  };

  const handleInputFocus = () => {
    if (suggestions.length > 0) {
      setShowSuggestions(true);
    }
  };

  return (
    <div className={cn('relative', className)}>
      <div className="relative">
        <Input
          ref={inputRef}
          type="text"
          value={query}
          onChange={(e) => handleInputChange(e.target.value)}
          onFocus={handleInputFocus}
          placeholder={placeholder}
          className="pr-10"
          aria-label="搜索"
          aria-expanded={showSuggestions}
          aria-autocomplete="list"
          role="combobox"
        />

        <div className="absolute right-3 top-1/2 transform -translate-y-1/2 flex items-center space-x-1">
          {isLoading && (
            <div className="w-4 h-4 border-2 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
          )}
          {query && !isLoading && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setQuery('')}
              className="p-0 h-4 w-4"
              aria-label="清除搜索"
            >
              <X size={14} />
            </Button>
          )}
          <Search size={16} className="text-gray-400" />
        </div>
      </div>

      {/* Search Suggestions */}
      {showSuggestions && suggestions.length > 0 && (
        <div
          ref={suggestionsRef}
          className="absolute top-full mt-1 w-full bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-2 z-50 max-h-80 overflow-y-auto"
          role="listbox"
        >
          {!query.trim() && searchHistory.length > 0 && (
            <div className="flex items-center justify-between px-3 py-2">
              <span className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
                最近搜索
              </span>
              <Button
                variant="ghost"
                size="sm"
                onClick={clearHistory}
                className="text-xs text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300 p-1"
              >
                清除
              </Button>
            </div>
          )}

          {suggestions.map((suggestion, index) => (
            <button
              key={suggestion.id}
              onClick={() => handleSuggestionClick(suggestion)}
              className={cn(
                'flex items-center space-x-3 w-full px-3 py-2 text-left transition-colors',
                index === selectedIndex
                  ? 'bg-orange-50 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300'
                  : 'text-gray-700 hover:bg-gray-50 dark:text-gray-300 dark:hover:bg-gray-700'
              )}
              role="option"
              aria-selected={index === selectedIndex}
            >
              <span className="text-gray-400 dark:text-gray-500">
                {suggestion.icon}
              </span>
              <div className="flex-1 min-w-0">
                <div className="text-sm font-medium truncate">
                  {suggestion.title}
                </div>
                {suggestion.subtitle && (
                  <div className="text-xs text-gray-500 dark:text-gray-400 truncate">
                    {suggestion.subtitle}
                  </div>
                )}
              </div>
              {suggestion.type === 'post' && (
                <span className="text-gray-400 dark:text-gray-500">
                  <FileText size={14} />
                </span>
              )}
            </button>
          ))}

          {!query.trim() && suggestions.length > 0 && (
            <div className="border-t border-gray-200 dark:border-gray-700 mt-2 pt-2 px-3">
              <div className="text-xs text-gray-500 dark:text-gray-400">
                提示：使用 Ctrl+K 快速打开搜索
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

/**
 * Usage:
 * <SearchBox /> - Basic search box
 * <SearchBox autoFocus onClose={() => setOpen(false)} /> - In modal/dropdown
 * <SearchBox size="lg" placeholder="搜索..." /> - Custom size and placeholder
 *
 * Features:
 * - Real-time search suggestions with debouncing
 * - Search history with localStorage persistence
 * - Keyboard navigation (arrows, enter, escape)
 * - Tag and author suggestions from API data
 * - Loading states and error handling
 * - Click outside to close
 * - Accessibility support with ARIA attributes
 * - Mobile-friendly responsive design
 * - Integration with routing for search navigation
 */