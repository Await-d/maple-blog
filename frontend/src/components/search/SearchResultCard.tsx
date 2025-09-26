/**
 * SearchResultCard Component
 * 搜索结果卡片组件 - 显示单个搜索结果的详细信息
 */

import React, { useMemo } from 'react';
import {
  Calendar,
  User,
  Eye,
  Heart,
  MessageCircle,
  Clock,
  Folder,
  Hash,
  ExternalLink,
  Star,
  TrendingUp,
} from 'lucide-react';
import { SearchResult } from '@/types/search';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';

interface SearchResultCardProps {
  result: SearchResult;
  query: string;
  viewMode: 'list' | 'grid';
  onClick: () => void;
  showScore?: boolean;
  showThumbnail?: boolean;
  showExcerpt?: boolean;
  showStats?: boolean;
  showHighlights?: boolean;
  className?: string;
}

export default function SearchResultCard({
  result,
  query,
  viewMode,
  onClick,
  showScore = false,
  showThumbnail = true,
  showExcerpt = true,
  showStats = true,
  showHighlights = true,
  className = '',
}: SearchResultCardProps) {
  // 高亮搜索关键词
  const highlightText = useMemo(() => {
    const highlight = (text: string, query: string): string => {
      if (!query || !text) return text;

      const keywords = query.split(/\s+/).filter(Boolean);
      let highlightedText = text;

      keywords.forEach((keyword) => {
        const regex = new RegExp(`(${keyword})`, 'gi');
        highlightedText = highlightedText.replace(regex, '<mark class="bg-yellow-200 text-yellow-900 rounded px-1">$1</mark>');
      });

      return highlightedText;
    };

    return {
      title: highlight(result.title, query),
      excerpt: highlight(result.excerpt || '', query),
    };
  }, [result.title, result.excerpt, query]);

  // 格式化时间
  const formatDate = (dateString: string) => {
    try {
      return formatDistanceToNow(new Date(dateString), {
        addSuffix: true,
        locale: zhCN,
      });
    } catch {
      return '未知时间';
    }
  };

  // 计算阅读时间
  const readingTimeText = useMemo(() => {
    const minutes = result.readingTime;
    if (minutes < 1) return '< 1 分钟阅读';
    return `${Math.round(minutes)} 分钟阅读`;
  }, [result.readingTime]);

  // 获取相关性评分颜色
  const getScoreColor = (score: number) => {
    if (score >= 0.8) return 'text-green-600 bg-green-100';
    if (score >= 0.6) return 'text-yellow-600 bg-yellow-100';
    if (score >= 0.4) return 'text-orange-600 bg-orange-100';
    return 'text-red-600 bg-red-100';
  };

  // 处理卡片点击
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    onClick();
  };

  // 渲染缩略图
  const renderThumbnail = () => {
    if (!showThumbnail || !result.thumbnailUrl) return null;

    return (
      <div className={`flex-shrink-0 ${viewMode === 'grid' ? 'w-full h-48 mb-4' : 'w-32 h-24 mr-4'}`}>
        <img
          src={result.thumbnailUrl}
          alt={result.title}
          className="w-full h-full object-cover rounded-lg bg-gray-100"
          loading="lazy"
          onError={(e) => {
            (e.target as HTMLImageElement).style.display = 'none';
          }}
        />
      </div>
    );
  };

  // 渲染高亮片段
  const renderHighlights = () => {
    if (!showHighlights || !result.highlights?.length) return null;

    return (
      <div className="mt-3 space-y-2">
        {result.highlights.slice(0, 2).map((highlight, index) => (
          <div key={index} className="text-sm">
            <div className="text-xs text-gray-500 mb-1 capitalize">
              {highlight.field === 'content' ? '内容' : highlight.field}
            </div>
            <div className="text-gray-700 leading-relaxed">
              {highlight.fragments.slice(0, 1).map((fragment, fragIndex) => (
                <div
                  key={fragIndex}
                  dangerouslySetInnerHTML={{ __html: fragment }}
                  className="prose-sm max-w-none"
                />
              ))}
            </div>
          </div>
        ))}
      </div>
    );
  };

  // 渲染统计信息
  const renderStats = () => {
    if (!showStats) return null;

    return (
      <div className="flex items-center space-x-4 text-xs text-gray-500">
        <span className="flex items-center">
          <Eye className="h-3 w-3 mr-1" />
          {result.viewCount.toLocaleString()}
        </span>
        <span className="flex items-center">
          <Heart className="h-3 w-3 mr-1" />
          {result.likeCount.toLocaleString()}
        </span>
        <span className="flex items-center">
          <MessageCircle className="h-3 w-3 mr-1" />
          {result.commentCount.toLocaleString()}
        </span>
        <span className="flex items-center">
          <Clock className="h-3 w-3 mr-1" />
          {readingTimeText}
        </span>
      </div>
    );
  };

  // 渲染分类和标签
  const renderCategoriesAndTags = () => (
    <div className="flex flex-wrap items-center gap-2 mt-3">
      {result.categories.slice(0, 2).map((category) => (
        <span
          key={category.id}
          className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800"
        >
          <Folder className="h-3 w-3 mr-1" />
          {category.name}
        </span>
      ))}
      {result.tags.slice(0, 3).map((tag) => (
        <span
          key={tag.id}
          className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
        >
          <Hash className="h-3 w-3 mr-1" />
          {tag.name}
        </span>
      ))}
    </div>
  );

  // 渲染作者信息
  const renderAuthor = () => (
    <div className="flex items-center space-x-2">
      {result.author.avatarUrl ? (
        <img
          src={result.author.avatarUrl}
          alt={result.author.displayName}
          className="h-6 w-6 rounded-full bg-gray-200"
          loading="lazy"
        />
      ) : (
        <div className="h-6 w-6 rounded-full bg-gray-200 flex items-center justify-center">
          <User className="h-3 w-3 text-gray-500" />
        </div>
      )}
      <span className="text-sm text-gray-700">{result.author.displayName}</span>
    </div>
  );

  // 列表视图样式
  if (viewMode === 'list') {
    return (
      <article
        className={`
          group cursor-pointer bg-white border border-gray-200 rounded-lg p-6
          hover:border-blue-300 hover:shadow-md transition-all duration-200
          ${className}
        `}
        onClick={handleClick}
      >
        <div className="flex">
          {renderThumbnail()}

          <div className="flex-1 min-w-0">
            {/* 标题和评分 */}
            <div className="flex items-start justify-between mb-2">
              <h3
                className="text-xl font-semibold text-gray-900 group-hover:text-blue-600 leading-tight"
                dangerouslySetInnerHTML={{ __html: highlightText.title }}
              />
              <div className="flex items-center space-x-2 ml-4">
                {showScore && (
                  <span className={`
                    px-2 py-1 text-xs font-medium rounded-full
                    ${getScoreColor(result.score)}
                  `}>
                    <Star className="h-3 w-3 mr-1 inline" />
                    {(result.score * 100).toFixed(0)}%
                  </span>
                )}
                <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-500 opacity-0 group-hover:opacity-100 transition-opacity" />
              </div>
            </div>

            {/* 摘要 */}
            {showExcerpt && result.excerpt && (
              <p
                className="text-gray-600 leading-relaxed mb-3 line-clamp-2"
                dangerouslySetInnerHTML={{ __html: highlightText.excerpt }}
              />
            )}

            {/* 高亮片段 */}
            {renderHighlights()}

            {/* 元信息 */}
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mt-4 space-y-2 sm:space-y-0">
              <div className="flex items-center space-x-4 text-sm text-gray-500">
                {renderAuthor()}
                <span className="flex items-center">
                  <Calendar className="h-4 w-4 mr-1" />
                  {formatDate(result.publishedAt)}
                </span>
              </div>

              {renderStats()}
            </div>

            {/* 分类和标签 */}
            {renderCategoriesAndTags()}
          </div>
        </div>
      </article>
    );
  }

  // 网格视图样式
  return (
    <article
      className={`
        group cursor-pointer bg-white border border-gray-200 rounded-lg overflow-hidden
        hover:border-blue-300 hover:shadow-lg transition-all duration-200
        ${className}
      `}
      onClick={handleClick}
    >
      {renderThumbnail()}

      <div className="p-4">
        {/* 评分 */}
        {showScore && (
          <div className="flex justify-between items-start mb-2">
            <span className={`
              px-2 py-1 text-xs font-medium rounded-full
              ${getScoreColor(result.score)}
            `}>
              <TrendingUp className="h-3 w-3 mr-1 inline" />
              {(result.score * 100).toFixed(0)}%
            </span>
            <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-500 opacity-0 group-hover:opacity-100 transition-opacity" />
          </div>
        )}

        {/* 标题 */}
        <h3
          className="text-lg font-semibold text-gray-900 group-hover:text-blue-600 leading-tight mb-2 line-clamp-2"
          dangerouslySetInnerHTML={{ __html: highlightText.title }}
        />

        {/* 摘要 */}
        {showExcerpt && result.excerpt && (
          <p
            className="text-gray-600 text-sm leading-relaxed mb-3 line-clamp-3"
            dangerouslySetInnerHTML={{ __html: highlightText.excerpt }}
          />
        )}

        {/* 高亮片段 */}
        {renderHighlights()}

        {/* 作者和时间 */}
        <div className="flex items-center justify-between mb-3 text-sm text-gray-500">
          {renderAuthor()}
          <span className="flex items-center">
            <Calendar className="h-3 w-3 mr-1" />
            {formatDate(result.publishedAt)}
          </span>
        </div>

        {/* 统计信息 */}
        {renderStats()}

        {/* 分类和标签 */}
        {renderCategoriesAndTags()}
      </div>
    </article>
  );
}

// 添加必要的CSS类（需要添加到全局样式中）
const styles = `
  .line-clamp-1 {
    display: -webkit-box;
    -webkit-line-clamp: 1;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .line-clamp-2 {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }

  .line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
  }
`;

// 如果使用样式组件，可以导出样式
export const SearchResultCardStyles = styles;