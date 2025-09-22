// @ts-nocheck
/**
 * TimelineArchive Component
 * 时间轴归档组件 - 按年月展示文章时间线
 */

import { useState, useEffect } from 'react';
import {
  Calendar,
  ChevronDown,
  ChevronUp,
  Clock,
  FileText,
  TrendingUp,
  BarChart,
} from 'lucide-react';
import { useArchiveStore } from '@/stores/searchStore';
import { TimelineYear, TimelineMonth, ArchivePost } from '@/types/search';
import { formatDistanceToNow } from 'date-fns';
import { zhCN } from 'date-fns/locale';

interface TimelineArchiveProps {
  className?: string;
  showStats?: boolean;
  showFilters?: boolean;
  initialYear?: number;
  viewMode?: 'timeline' | 'compact';
}

interface TimelinePostProps {
  post: ArchivePost;
  onClick?: (post: ArchivePost) => void;
}

function TimelinePost({ post, onClick }: TimelinePostProps) {
  const handleClick = () => {
    onClick?.(post);
  };

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

  return (
    <article
      className="group cursor-pointer p-4 border border-gray-200 rounded-lg hover:border-blue-300 hover:shadow-md transition-all duration-200 bg-white"
      onClick={handleClick}
    >
      {/* 缩略图 */}
      {post.thumbnailUrl && (
        <div className="w-full h-48 mb-4">
          <img
            src={post.thumbnailUrl}
            alt={post.title}
            className="w-full h-full object-cover rounded-lg bg-gray-100"
            loading="lazy"
          />
        </div>
      )}

      {/* 文章信息 */}
      <div className="space-y-3">
        <h3 className="text-lg font-semibold text-gray-900 group-hover:text-blue-600 leading-tight line-clamp-2">
          {post.title}
        </h3>

        {post.excerpt && (
          <p className="text-gray-600 text-sm leading-relaxed line-clamp-3">
            {post.excerpt}
          </p>
        )}

        {/* 元信息 */}
        <div className="flex items-center justify-between text-xs text-gray-500">
          <div className="flex items-center space-x-4">
            <span className="flex items-center">
              <Calendar className="h-3 w-3 mr-1" />
              {formatDate(post.publishedAt)}
            </span>
            <span className="flex items-center">
              <Clock className="h-3 w-3 mr-1" />
              {post.readingTime} 分钟阅读
            </span>
            <span className="flex items-center">
              <TrendingUp className="h-3 w-3 mr-1" />
              {post.viewCount} 浏览
            </span>
          </div>

          {/* 作者 */}
          <div className="flex items-center space-x-1">
            {post.author.avatarUrl ? (
              <img
                src={post.author.avatarUrl}
                alt={post.author.displayName}
                className="h-4 w-4 rounded-full"
              />
            ) : (
              <div className="h-4 w-4 rounded-full bg-gray-300" />
            )}
            <span>{post.author.displayName}</span>
          </div>
        </div>

        {/* 分类和标签 */}
        <div className="flex flex-wrap items-center gap-2">
          {post.categories.slice(0, 2).map((category) => (
            <span
              key={category.id}
              className="px-2 py-1 text-xs font-medium bg-purple-100 text-purple-800 rounded-full"
            >
              {category.name}
            </span>
          ))}
          {post.tags.slice(0, 3).map((tag) => (
            <span
              key={tag.id}
              className="px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded-full"
            >
              #{tag.name}
            </span>
          ))}
        </div>
      </div>
    </article>
  );
}

interface TimelineMonthProps {
  year: number;
  month: TimelineMonth;
  expanded: boolean;
  onToggle: () => void;
  onPostClick?: (post: ArchivePost) => void;
  viewMode: 'timeline' | 'compact';
}

function TimelineMonthComponent({
  year,
  month,
  expanded,
  onToggle,
  onPostClick,
  viewMode,
}: TimelineMonthProps) {
  const monthNames = [
    '一月', '二月', '三月', '四月', '五月', '六月',
    '七月', '八月', '九月', '十月', '十一月', '十二月'
  ];

  return (
    <div className="relative">
      {/* 时间线连接线 */}
      {viewMode === 'timeline' && (
        <div className="absolute left-4 top-12 bottom-0 w-0.5 bg-gray-300" />
      )}

      {/* 月份标题 */}
      <button
        onClick={onToggle}
        className="flex items-center justify-between w-full p-4 bg-gray-50 hover:bg-gray-100 rounded-lg transition-colors mb-4"
      >
        <div className="flex items-center space-x-3">
          {viewMode === 'timeline' && (
            <div className="relative">
              <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center text-white text-sm font-medium z-10">
                {month.month}
              </div>
            </div>
          )}
          <div className="text-left">
            <h3 className="text-lg font-semibold text-gray-900">
              {monthNames[month.month - 1]} {year}
            </h3>
            <p className="text-sm text-gray-600">
              {month.count} 篇文章
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-2">
          <span className="text-sm text-gray-500">
            {expanded ? '收起' : '展开'}
          </span>
          {expanded ? (
            <ChevronUp className="h-5 w-5 text-gray-400" />
          ) : (
            <ChevronDown className="h-5 w-5 text-gray-400" />
          )}
        </div>
      </button>

      {/* 文章列表 */}
      {expanded && (
        <div className={`
          ${viewMode === 'timeline' ? 'ml-12' : 'ml-0'}
          space-y-4 mb-8
        `}>
          {month.posts.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {month.posts.map((post) => (
                <TimelinePost
                  key={post.id}
                  post={post}
                  onClick={onPostClick}
                />
              ))}
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500">
              该月份暂无文章
            </div>
          )}
        </div>
      )}
    </div>
  );
}

interface TimelineYearProps {
  year: TimelineYear;
  expandedMonths: Set<string>;
  onMonthToggle: (yearMonth: string) => void;
  onPostClick?: (post: ArchivePost) => void;
  viewMode: 'timeline' | 'compact';
}

function TimelineYearComponent({
  year,
  expandedMonths,
  onMonthToggle,
  onPostClick,
  viewMode,
}: TimelineYearProps) {
  return (
    <div className="mb-12">
      {/* 年份标题 */}
      <div className="sticky top-0 z-10 bg-white border-b border-gray-200 pb-4 mb-6">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 flex items-center">
            <Calendar className="h-6 w-6 mr-2 text-blue-500" />
            {year.year} 年
          </h2>
          <div className="flex items-center space-x-4 text-sm text-gray-600">
            <span className="flex items-center">
              <FileText className="h-4 w-4 mr-1" />
              {year.count} 篇文章
            </span>
            <span className="flex items-center">
              <BarChart className="h-4 w-4 mr-1" />
              {year.months.length} 个月
            </span>
          </div>
        </div>
      </div>

      {/* 月份列表 */}
      <div className="space-y-6">
        {year.months.map((month) => {
          const yearMonth = `${year.year}-${month.month}`;
          return (
            <TimelineMonthComponent
              key={yearMonth}
              year={year.year}
              month={month}
              expanded={expandedMonths.has(yearMonth)}
              onToggle={() => onMonthToggle(yearMonth)}
              onPostClick={onPostClick}
              viewMode={viewMode}
            />
          );
        })}
      </div>
    </div>
  );
}

export default function TimelineArchive({
  className = '',
  showStats = true,
  showFilters = true,
  initialYear,
  viewMode: initialViewMode = 'timeline',
}: TimelineArchiveProps) {
  const { timelineArchive, loading, error, loadTimelineArchive } = useArchiveStore();
  const [expandedMonths, setExpandedMonths] = useState<Set<string>>(new Set());
  const [_selectedYear, _setSelectedYear] = useState<number | undefined>(initialYear);
  const [viewMode, setViewMode] = useState<'timeline' | 'compact'>(initialViewMode);
  const [sortOrder, setSortOrder] = useState<'desc' | 'asc'>('desc');

  // 加载时间线数据
  useEffect(() => {
    loadTimelineArchive(_selectedYear);
  }, [_selectedYear, loadTimelineArchive]);

  // 处理月份展开/收起
  const handleMonthToggle = (yearMonth: string) => {
    setExpandedMonths(prev => {
      const newSet = new Set(prev);
      if (newSet.has(yearMonth)) {
        newSet.delete(yearMonth);
      } else {
        newSet.add(yearMonth);
      }
      return newSet;
    });
  };

  // 全部展开
  const expandAll = () => {
    if (!timelineArchive) return;
    const allMonths = new Set<string>();
    timelineArchive.years.forEach(year => {
      year.months.forEach(month => {
        allMonths.add(`${year.year}-${month.month}`);
      });
    });
    setExpandedMonths(allMonths);
  };

  // 全部收起
  const collapseAll = () => {
    setExpandedMonths(new Set());
  };

  // 处理文章点击
  const handlePostClick = (post: ArchivePost) => {
    window.open(`/posts/${post.slug}`, '_blank');
  };

  // 排序后的年份数据
  const sortedYears = timelineArchive?.years.sort((a, b) => {
    return sortOrder === 'desc' ? b.year - a.year : a.year - b.year;
  }) || [];

  if (loading) {
    return (
      <div className={`flex items-center justify-center py-12 ${className}`}>
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500" />
          <span className="text-gray-600">加载归档数据...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-red-500 mb-4">
          <Calendar className="h-12 w-12 mx-auto mb-2" />
          <p>加载归档失败</p>
        </div>
        <p className="text-gray-600 mb-4">{error}</p>
        <button
          onClick={() => loadTimelineArchive(_selectedYear)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          重试
        </button>
      </div>
    );
  }

  if (!timelineArchive || sortedYears.length === 0) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <Calendar className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">暂无归档内容</h3>
        <p className="text-gray-600">还没有发布任何文章</p>
      </div>
    );
  }

  return (
    <div className={`timeline-archive ${className}`}>
      {/* 控制面板 */}
      {(showStats || showFilters) && (
        <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between space-y-4 sm:space-y-0">
            {/* 统计信息 */}
            {showStats && (
              <div className="flex items-center space-x-6 text-sm text-gray-600">
                <span className="flex items-center">
                  <FileText className="h-4 w-4 mr-1" />
                  总计 {timelineArchive.totalCount} 篇文章
                </span>
                <span className="flex items-center">
                  <Calendar className="h-4 w-4 mr-1" />
                  跨越 {sortedYears.length} 年
                </span>
                <span className="flex items-center">
                  <Clock className="h-4 w-4 mr-1" />
                  {timelineArchive.dateRange.from} - {timelineArchive.dateRange.to}
                </span>
              </div>
            )}

            {/* 控制按钮 */}
            {showFilters && (
              <div className="flex items-center space-x-2">
                {/* 视图切换 */}
                <div className="flex bg-gray-100 rounded-lg p-1">
                  <button
                    onClick={() => setViewMode('timeline')}
                    className={`px-3 py-1 rounded text-sm transition-colors ${
                      viewMode === 'timeline'
                        ? 'bg-white text-gray-900 shadow-sm'
                        : 'text-gray-600 hover:text-gray-900'
                    }`}
                  >
                    时间轴
                  </button>
                  <button
                    onClick={() => setViewMode('compact')}
                    className={`px-3 py-1 rounded text-sm transition-colors ${
                      viewMode === 'compact'
                        ? 'bg-white text-gray-900 shadow-sm'
                        : 'text-gray-600 hover:text-gray-900'
                    }`}
                  >
                    紧凑模式
                  </button>
                </div>

                {/* 排序按钮 */}
                <button
                  onClick={() => setSortOrder(sortOrder === 'desc' ? 'asc' : 'desc')}
                  className="px-3 py-1 bg-gray-100 hover:bg-gray-200 rounded-lg text-sm transition-colors"
                  title={`当前${sortOrder === 'desc' ? '降序' : '升序'}排列，点击切换`}
                >
                  {sortOrder === 'desc' ? '最新' : '最早'}
                </button>

                {/* 展开/收起按钮 */}
                <div className="flex space-x-1">
                  <button
                    onClick={expandAll}
                    className="px-3 py-1 text-sm text-blue-600 hover:text-blue-700 transition-colors"
                  >
                    全部展开
                  </button>
                  <button
                    onClick={collapseAll}
                    className="px-3 py-1 text-sm text-gray-600 hover:text-gray-700 transition-colors"
                  >
                    全部收起
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* 时间线 */}
      <div className="space-y-8">
        {sortedYears.map((year) => (
          <TimelineYearComponent
            key={year.year}
            year={year}
            expandedMonths={expandedMonths}
            onMonthToggle={handleMonthToggle}
            onPostClick={handlePostClick}
            viewMode={viewMode}
          />
        ))}
      </div>
    </div>
  );
}