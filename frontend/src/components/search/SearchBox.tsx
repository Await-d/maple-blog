/**
 * SearchBox Component
 * 智能搜索框组件 - 支持实时搜索建议、历史记录、快捷键
 */

import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Search, X, Clock, TrendingUp, Mic, MicOff, Loader } from 'lucide-react';
import { useSearchStore } from '@/stores/searchStore';
import { searchApi } from '@/services/search/searchApi';
import { useDebounce } from '@/hooks/useDebounce';
import { DEFAULT_SEARCH_CONFIG } from '@/types/search';
import SearchSuggestions from './SearchSuggestions';

interface SearchBoxProps {
  placeholder?: string;
  className?: string;
  showFilters?: boolean;
  onSearch?: (query: string) => void;
  onFocus?: () => void;
  onBlur?: () => void;
  disabled?: boolean;
  variant?: 'default' | 'compact' | 'hero';
}

export default function SearchBox({
  placeholder = '搜索文章、分类、标签...',
  className = '',
  showFilters = false,
  onSearch,
  onFocus,
  onBlur,
  disabled = false,
  variant = 'default',
}: SearchBoxProps) {
  // 状态管理
  const {
    query,
    autoComplete,
    history,
    loading,
    showHistory,
    setQuery,
    loadAutoComplete,
    clearAutoComplete,
    toggleHistory,
    search,
  } = useSearchStore();

  // 本地状态
  const [localQuery, setLocalQuery] = useState(query);
  const [isFocused, setIsFocused] = useState(false);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [isRecording, setIsRecording] = useState(false);
  const [speechSupported, setSpeechSupported] = useState(false);

  // refs
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);
  const _mediaRecorderRef = useRef<MediaRecorder | null>(null);

  // 防抖处理搜索建议
  const debouncedQuery = useDebounce(localQuery, DEFAULT_SEARCH_CONFIG.debounceMs);

  // 样式变体
  const variants = {
    default: 'w-full max-w-2xl',
    compact: 'w-full max-w-md',
    hero: 'w-full max-w-4xl',
  };

  // 检查语音搜索支持
  useEffect(() => {
    setSpeechSupported('webkitSpeechRecognition' in window || 'SpeechRecognition' in window);
  }, []);

  // 加载自动完成建议
  useEffect(() => {
    if (
      debouncedQuery &&
      debouncedQuery.length >= DEFAULT_SEARCH_CONFIG.minQueryLength &&
      isFocused
    ) {
      loadAutoComplete(debouncedQuery);
    } else {
      clearAutoComplete();
    }
  }, [debouncedQuery, isFocused, loadAutoComplete, clearAutoComplete]);

  // 键盘快捷键
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Ctrl/Cmd + K 聚焦搜索框
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        inputRef.current?.focus();
      }

      // Escape 键清除搜索或失焦
      if (e.key === 'Escape') {
        if (isFocused) {
          inputRef.current?.blur();
          setShowSuggestions(false);
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isFocused]);

  // 点击外部关闭建议
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

  // 处理输入变化
  const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setLocalQuery(value);
    setQuery(value);

    if (value.trim() || showHistory) {
      setShowSuggestions(true);
    }
  }, [setQuery, showHistory]);

  // 处理搜索提交
  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();

      if (!localQuery.trim()) {
        return;
      }

      setShowSuggestions(false);
      await search({ query: localQuery.trim() });
      onSearch?.(localQuery.trim());

      // 记录搜索分析
      searchApi.recordSearchAnalytics(localQuery.trim(), 0);
    },
    [localQuery, search, onSearch]
  );

  // 处理建议选择
  const handleSuggestionSelect = useCallback(
    async (suggestion: string) => {
      setLocalQuery(suggestion);
      setQuery(suggestion);
      setShowSuggestions(false);

      await search({ query: suggestion });
      onSearch?.(suggestion);

      inputRef.current?.blur();
    },
    [setQuery, search, onSearch]
  );

  // 处理输入框聚焦
  const handleFocus = useCallback(() => {
    setIsFocused(true);
    if (localQuery.trim() || history.length > 0) {
      setShowSuggestions(true);
    }
    onFocus?.();
  }, [localQuery, history.length, onFocus]);

  // 处理输入框失焦
  const handleBlur = useCallback(() => {
    // 延迟失焦，允许点击建议
    setTimeout(() => {
      setIsFocused(false);
      onBlur?.();
    }, 200);
  }, [onBlur]);

  // 清除搜索
  const handleClear = useCallback(() => {
    setLocalQuery('');
    setQuery('');
    clearAutoComplete();
    setShowSuggestions(false);
    inputRef.current?.focus();
  }, [setQuery, clearAutoComplete]);

  // 语音搜索
  const handleVoiceSearch = useCallback(async () => {
    if (!speechSupported) return;

    try {
      setIsRecording(true);

      // 这里应该实现语音识别
      // 简化版本，实际项目中需要集成 Web Speech API
      const SpeechRecognition = window.webkitSpeechRecognition || window.SpeechRecognition;
      const recognition = new SpeechRecognition();

      recognition.continuous = false;
      recognition.interimResults = false;
      recognition.lang = 'zh-CN';

      recognition.onresult = (event: SpeechRecognitionEvent) => {
        const transcript = event.results[0][0].transcript;
        setLocalQuery(transcript);
        setQuery(transcript);
        handleSubmit({ preventDefault: () => { /* no-op */ } } as React.FormEvent);
      };

      recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
        console.error('Speech recognition error:', event.error);
        setIsRecording(false);
      };

      recognition.onend = () => {
        setIsRecording(false);
      };

      recognition.start();
    } catch (error) {
      console.error('Voice search error:', error);
      setIsRecording(false);
    }
  }, [speechSupported, setQuery, handleSubmit]);

  // 渲染搜索建议
  const shouldShowSuggestions = showSuggestions && isFocused && (
    autoComplete.length > 0 ||
    history.length > 0 ||
    localQuery.length >= DEFAULT_SEARCH_CONFIG.minQueryLength
  );

  return (
    <div className={`relative ${variants[variant]} ${className}`}>
      <form onSubmit={handleSubmit} className="relative">
        <div className="relative">
          {/* 搜索图标 */}
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <Search className="h-5 w-5 text-gray-400" />
          </div>

          {/* 搜索输入框 */}
          <input
            ref={inputRef}
            type="text"
            value={localQuery}
            onChange={handleInputChange}
            onFocus={handleFocus}
            onBlur={handleBlur}
            disabled={disabled}
            placeholder={placeholder}
            className={`
              block w-full pl-10 pr-12 py-3 border border-gray-300 rounded-lg
              text-sm placeholder-gray-500 bg-white
              focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
              disabled:bg-gray-50 disabled:text-gray-500
              ${variant === 'hero' ? 'text-lg py-4' : ''}
              transition-all duration-200
            `}
            autoComplete="off"
            aria-label="搜索"
            role="searchbox"
            aria-expanded={shouldShowSuggestions}
            aria-haspopup="listbox"
          />

          {/* 右侧操作按钮 */}
          <div className="absolute inset-y-0 right-0 flex items-center pr-2 space-x-1">
            {/* 清除按钮 */}
            {localQuery && (
              <button
                type="button"
                onClick={handleClear}
                className="p-1 text-gray-400 hover:text-gray-600 rounded"
                aria-label="清除搜索"
              >
                <X className="h-4 w-4" />
              </button>
            )}

            {/* 语音搜索按钮 */}
            {speechSupported && (
              <button
                type="button"
                onClick={handleVoiceSearch}
                disabled={isRecording}
                className={`
                  p-1 rounded transition-colors
                  ${isRecording
                    ? 'text-red-500 hover:text-red-600'
                    : 'text-gray-400 hover:text-gray-600'
                  }
                `}
                aria-label={isRecording ? '录音中...' : '语音搜索'}
              >
                {isRecording ? (
                  <MicOff className="h-4 w-4 animate-pulse" />
                ) : (
                  <Mic className="h-4 w-4" />
                )}
              </button>
            )}

            {/* 加载指示器 */}
            {loading && (
              <div className="p-1">
                <Loader className="h-4 w-4 animate-spin text-blue-500" />
              </div>
            )}
          </div>
        </div>

        {/* 快捷键提示 */}
        {!isFocused && !disabled && (
          <div className="absolute inset-y-0 right-0 flex items-center pr-3">
            <kbd className="hidden sm:inline-flex items-center px-2 py-1 border border-gray-200 rounded text-xs text-gray-500 bg-gray-50">
              <span className="mr-1">⌘</span>K
            </kbd>
          </div>
        )}
      </form>

      {/* 搜索建议 */}
      {shouldShowSuggestions && (
        <div ref={suggestionsRef}>
          <SearchSuggestions
            query={localQuery}
            suggestions={autoComplete}
            history={history}
            loading={loading}
            onSelect={handleSuggestionSelect}
            onClose={() => setShowSuggestions(false)}
          />
        </div>
      )}

      {/* 搜索过滤器快捷按钮 */}
      {showFilters && (
        <div className="mt-2 flex items-center space-x-2">
          <button
            type="button"
            onClick={toggleHistory}
            className="text-xs text-gray-500 hover:text-gray-700 flex items-center space-x-1"
          >
            <Clock className="h-3 w-3" />
            <span>历史记录</span>
          </button>
          <button
            type="button"
            className="text-xs text-gray-500 hover:text-gray-700 flex items-center space-x-1"
          >
            <TrendingUp className="h-3 w-3" />
            <span>热门搜索</span>
          </button>
        </div>
      )}
    </div>
  );
}

// 为 TypeScript 添加 SpeechRecognition 类型声明
