/**
 * ArchiveCalendar Component
 * 归档日历组件 - 以日历形式展示文章发布分布
 */

import { useState, useEffect, useMemo } from 'react';
import {
  Calendar,
  ChevronLeft,
  ChevronRight,
  FileText,
  TrendingUp,
  Eye,
  Clock,
} from 'lucide-react';
import { useArchiveStore } from '@/stores/searchStore';
import { CalendarDay, CalendarMonth, ArchivePost } from '@/types/search';

interface ArchiveCalendarProps {
  className?: string;
  initialYear?: number;
  onDateClick?: (date: string, posts: ArchivePost[]) => void;
  showHeatmap?: boolean;
  showStats?: boolean;
}

interface CalendarCellProps {
  day: CalendarDay;
  maxCount: number;
  onClick: (day: CalendarDay) => void;
  showHeatmap: boolean;
}

function CalendarCell({ day, maxCount, onClick, showHeatmap }: CalendarCellProps) {
  // 根据文章数量计算热力图颜色强度
  const getHeatmapIntensity = (count: number, max: number): number => {
    if (count === 0) return 0;
    if (max === 0) return 0;
    return Math.min(Math.ceil((count / max) * 4), 4);
  };

  const intensity = getHeatmapIntensity(day.count, maxCount);

  // 热力图颜色类名
  const heatmapColors = [
    'bg-gray-100 text-gray-500', // 0 篇
    'bg-green-100 text-green-800', // 1-25%
    'bg-green-200 text-green-900', // 26-50%
    'bg-green-300 text-green-900', // 51-75%
    'bg-green-400 text-green-900', // 76-100%
  ];

  const baseClasses = `
    relative w-full h-10 border border-gray-200 flex items-center justify-center text-sm
    cursor-pointer transition-all duration-200 hover:border-blue-300 hover:z-10
    ${day.isCurrentMonth ? 'font-medium' : 'text-gray-400'}
    ${day.isToday ? 'ring-2 ring-blue-500' : ''}
    ${showHeatmap ? heatmapColors[intensity] : 'bg-white hover:bg-gray-50'}
  `;

  return (
    <div
      className={baseClasses}
      onClick={() => onClick(day)}
      title={`${day.date}: ${day.count} 篇文章`}
    >
      <span className="relative z-10">{day.day}</span>

      {/* 文章数量指示器 */}
      {day.count > 0 && !showHeatmap && (
        <div className="absolute top-1 right-1 w-2 h-2 bg-blue-500 rounded-full" />
      )}

      {/* 今天标记 */}
      {day.isToday && (
        <div className="absolute bottom-1 left-1/2 transform -translate-x-1/2 w-1 h-1 bg-blue-500 rounded-full" />
      )}

      {/* hover 时显示文章数量 */}
      {day.count > 0 && (
        <div className="absolute -top-8 left-1/2 transform -translate-x-1/2 bg-gray-900 text-white text-xs px-2 py-1 rounded opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-20">
          {day.count} 篇
        </div>
      )}
    </div>
  );
}

interface MonthViewProps {
  month: CalendarMonth;
  year: number;
  maxCount: number;
  onDayClick: (day: CalendarDay) => void;
  showHeatmap: boolean;
}

function MonthView({ month, year, maxCount, onDayClick, showHeatmap }: MonthViewProps) {
  const weekdays = ['日', '一', '二', '三', '四', '五', '六'];

  // 生成日历网格
  const generateCalendarGrid = (): (CalendarDay | null)[][] => {
    const firstDay = new Date(year, month.month - 1, 1);
    const _lastDay = new Date(year, month.month, 0);
    const startDate = new Date(firstDay);
    startDate.setDate(startDate.getDate() - firstDay.getDay()); // 调整到周日开始

    const weeks: (CalendarDay | null)[][] = [];
    let currentWeek: (CalendarDay | null)[] = [];

    for (let i = 0; i < 42; i++) { // 6周 × 7天
      const currentDate = new Date(startDate);
      currentDate.setDate(startDate.getDate() + i);

      if (currentDate.getMonth() === month.month - 1) {
        // 当前月份的日期
        const dayData = month.days.find(d => d.day === currentDate.getDate());
        currentWeek.push(dayData || {
          day: currentDate.getDate(),
          date: currentDate.toISOString().split('T')[0],
          count: 0,
          posts: [],
          isToday: false,
          isCurrentMonth: true,
        });
      } else {
        // 其他月份的日期
        currentWeek.push({
          day: currentDate.getDate(),
          date: currentDate.toISOString().split('T')[0],
          count: 0,
          posts: [],
          isToday: false,
          isCurrentMonth: false,
        });
      }

      if (currentWeek.length === 7) {
        weeks.push(currentWeek);
        currentWeek = [];
      }
    }

    return weeks;
  };

  const calendarGrid = generateCalendarGrid();

  return (
    <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
      {/* 月份标题 */}
      <div className="bg-gray-50 px-4 py-3 border-b border-gray-200">
        <h3 className="text-lg font-semibold text-gray-900">
          {month.monthName} {year}
        </h3>
        <p className="text-sm text-gray-600">
          {month.days.reduce((sum, day) => sum + day.count, 0)} 篇文章
        </p>
      </div>

      {/* 星期标题 */}
      <div className="grid grid-cols-7 border-b border-gray-200">
        {weekdays.map((weekday) => (
          <div
            key={weekday}
            className="h-8 flex items-center justify-center text-sm font-medium text-gray-500 bg-gray-50"
          >
            {weekday}
          </div>
        ))}
      </div>

      {/* 日历网格 */}
      <div className="grid grid-cols-7 gap-0">
        {calendarGrid.flat().map((day, index) => (
          day ? (
            <div key={`${day.date}-${index}`} className="group">
              <CalendarCell
                day={day}
                maxCount={maxCount}
                onClick={onDayClick}
                showHeatmap={showHeatmap}
              />
            </div>
          ) : (
            <div key={`empty-${index}`} className="h-10" />
          )
        ))}
      </div>
    </div>
  );
}

export default function ArchiveCalendar({
  className = '',
  initialYear = new Date().getFullYear(),
  onDateClick,
  showHeatmap = true,
  showStats = true,
}: ArchiveCalendarProps) {
  const { calendarArchive, loading, error, loadCalendarArchive } = useArchiveStore();
  const [currentYear, setCurrentYear] = useState(initialYear);
  const [selectedDay, setSelectedDay] = useState<CalendarDay | null>(null);
  const [showPostsModal, setShowPostsModal] = useState(false);
  const [showHeatmapState, setShowHeatmap] = useState<boolean>(showHeatmap ?? true);

  // 加载日历数据
  useEffect(() => {
    loadCalendarArchive(currentYear);
  }, [currentYear, loadCalendarArchive]);

  // 计算最大文章数（用于热力图）
  const maxCount = useMemo(() => {
    if (!calendarArchive) return 0;
    return Math.max(
      ...calendarArchive.months.flatMap(month =>
        month.days.map(day => day.count)
      )
    );
  }, [calendarArchive]);

  // 计算统计信息
  const stats = useMemo(() => {
    if (!calendarArchive) return { totalPosts: 0, activeDays: 0, avgPerDay: 0 };

    const totalPosts = calendarArchive.totalCount;
    const activeDays = calendarArchive.months.reduce(
      (sum, month) => sum + month.days.filter(day => day.count > 0).length,
      0
    );
    const avgPerDay = activeDays > 0 ? totalPosts / activeDays : 0;

    return { totalPosts, activeDays, avgPerDay };
  }, [calendarArchive]);

  // 处理日期点击
  const handleDayClick = (day: CalendarDay) => {
    if (day.count === 0) return;

    setSelectedDay(day);
    setShowPostsModal(true);
    onDateClick?.(day.date, day.posts);
  };

  // 切换年份
  const handleYearChange = (delta: number) => {
    setCurrentYear(prev => prev + delta);
  };

  if (loading) {
    return (
      <div className={`flex items-center justify-center py-12 ${className}`}>
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500" />
          <span className="text-gray-600">加载日历数据...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-red-500 mb-4">
          <Calendar className="h-12 w-12 mx-auto mb-2" />
          <p>加载日历失败</p>
        </div>
        <p className="text-gray-600 mb-4">{error}</p>
        <button
          onClick={() => loadCalendarArchive(currentYear)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          重试
        </button>
      </div>
    );
  }

  if (!calendarArchive) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <Calendar className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">暂无日历数据</h3>
        <p className="text-gray-600">该年份还没有发布任何文章</p>
      </div>
    );
  }

  return (
    <div className={`archive-calendar ${className}`}>
      {/* 控制面板 */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between space-y-4 sm:space-y-0">
          {/* 年份控制 */}
          <div className="flex items-center space-x-4">
            <button
              onClick={() => handleYearChange(-1)}
              className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              title="上一年"
            >
              <ChevronLeft className="h-5 w-5" />
            </button>

            <h2 className="text-xl font-bold text-gray-900">
              {currentYear} 年度日历
            </h2>

            <button
              onClick={() => handleYearChange(1)}
              disabled={currentYear >= new Date().getFullYear()}
              className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              title="下一年"
            >
              <ChevronRight className="h-5 w-5" />
            </button>
          </div>

          {/* 统计信息和控制 */}
          <div className="flex items-center space-x-4">
            {showStats && (
              <div className="flex items-center space-x-4 text-sm text-gray-600">
                <span className="flex items-center">
                  <FileText className="h-4 w-4 mr-1" />
                  {stats.totalPosts} 篇文章
                </span>
                <span className="flex items-center">
                  <TrendingUp className="h-4 w-4 mr-1" />
                  {stats.activeDays} 活跃天数
                </span>
                <span className="flex items-center">
                  <Eye className="h-4 w-4 mr-1" />
                  平均 {stats.avgPerDay.toFixed(1)} 篇/天
                </span>
              </div>
            )}

            {/* 热力图切换 */}
            <div className="flex items-center space-x-2">
              <label className="text-sm text-gray-600">热力图</label>
              <button
                onClick={() => setShowHeatmap(!showHeatmapState)}
                className={`
                  relative w-12 h-6 rounded-full transition-colors duration-200
                  ${showHeatmapState ? 'bg-blue-500' : 'bg-gray-300'}
                `}
              >
                <div
                  className={`
                    absolute top-1 left-1 w-4 h-4 bg-white rounded-full transition-transform duration-200
                    ${showHeatmapState ? 'translate-x-6' : 'translate-x-0'}
                  `}
                />
              </button>
            </div>
          </div>
        </div>

        {/* 热力图图例 */}
        {showHeatmapState && (
          <div className="mt-4 pt-4 border-t border-gray-200">
            <div className="flex items-center justify-between text-sm text-gray-600">
              <span>文章发布热力图</span>
              <div className="flex items-center space-x-2">
                <span>少</span>
                <div className="flex space-x-1">
                  <div className="w-3 h-3 bg-gray-100 border border-gray-300" />
                  <div className="w-3 h-3 bg-green-100 border border-gray-300" />
                  <div className="w-3 h-3 bg-green-200 border border-gray-300" />
                  <div className="w-3 h-3 bg-green-300 border border-gray-300" />
                  <div className="w-3 h-3 bg-green-400 border border-gray-300" />
                </div>
                <span>多</span>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* 月份日历 */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {calendarArchive.months.map((month) => (
          <MonthView
            key={month.month}
            month={month}
            year={currentYear}
            maxCount={maxCount}
            onDayClick={handleDayClick}
            showHeatmap={showHeatmapState}
          />
        ))}
      </div>

      {/* 文章详情模态框 */}
      {showPostsModal && selectedDay && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg max-w-2xl w-full max-h-[80vh] overflow-hidden">
            {/* 模态框标题 */}
            <div className="px-6 py-4 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900">
                  {selectedDay.date} 发布的文章
                </h3>
                <button
                  onClick={() => setShowPostsModal(false)}
                  className="text-gray-400 hover:text-gray-600"
                >
                  ×
                </button>
              </div>
              <p className="text-sm text-gray-600 mt-1">
                共 {selectedDay.count} 篇文章
              </p>
            </div>

            {/* 文章列表 */}
            <div className="max-h-96 overflow-y-auto">
              {selectedDay.posts.map((post) => (
                <div
                  key={post.id}
                  className="px-6 py-4 border-b border-gray-100 last:border-b-0 hover:bg-gray-50 cursor-pointer"
                  onClick={() => {
                    window.open(`/posts/${post.slug}`, '_blank');
                    setShowPostsModal(false);
                  }}
                >
                  <h4 className="font-medium text-gray-900 mb-2 line-clamp-2">
                    {post.title}
                  </h4>
                  {post.excerpt && (
                    <p className="text-sm text-gray-600 mb-3 line-clamp-2">
                      {post.excerpt}
                    </p>
                  )}
                  <div className="flex items-center justify-between text-xs text-gray-500">
                    <div className="flex items-center space-x-4">
                      <span className="flex items-center">
                        <Clock className="h-3 w-3 mr-1" />
                        {post.readingTime} 分钟阅读
                      </span>
                      <span className="flex items-center">
                        <Eye className="h-3 w-3 mr-1" />
                        {post.viewCount} 浏览
                      </span>
                    </div>
                    <span>{post.author.displayName}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}