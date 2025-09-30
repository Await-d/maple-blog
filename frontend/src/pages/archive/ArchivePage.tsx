/**
 * ArchivePage - 完整的文章归档页面
 * 多视图模式：时间线、日历、分类、标签
 * 实时数据集成，支持搜索和筛选
 */

import React, { useState, useEffect, useMemo } from 'react';
import { DocumentHead } from '@/components/common/DocumentHead';
import { useArchiveStore } from '@/stores/searchStore';
import { createLogger } from '@/utils/logger';
import {
  Archive,
  Calendar,
  Tag,
  Folder,
  BarChart3,
  Search,
  Filter,
  GitCommit,
  TrendingUp,
  FileText,
  Clock,
  RefreshCw,
  ChevronDown,
  BookOpen,
  Hash,
} from 'lucide-react';

// 导入归档组件
import TimelineArchive from '@/components/archive/TimelineArchive';
import ArchiveCalendar from '@/components/archive/ArchiveCalendar';
import CategoryArchive from '@/components/archive/CategoryArchive';
import TagCloud from '@/components/archive/TagCloud';

type ArchiveViewType = 'timeline' | 'calendar' | 'category' | 'tag';

interface ArchiveStats {
  totalPosts: number;
  totalCategories: number;
  totalTags: number;
  totalAuthors: number;
  activeMonths: number;
  averagePostsPerMonth: number;
  firstPostDate: string;
  lastPostDate: string;
}

interface ViewModeButton {
  id: ArchiveViewType;
  label: string;
  icon: React.ReactNode;
  description: string;
  color: string;
}

export const ArchivePage: React.FC = () => {
  // Initialize logger for this component
  const log = createLogger('ArchivePage');
  
  const {
    currentView,
    loading,
    error,
    timelineArchive,
    calendarArchive,
    categoryTree,
    tagCloud,
    setCurrentView,
    loadTimelineArchive,
    loadCalendarArchive,
    loadCategoryTree,
    loadTagCloud,
  } = useArchiveStore();

  const [searchTerm, setSearchTerm] = useState('');
  const [showFilters, setShowFilters] = useState(false);
  const [selectedYear, setSelectedYear] = useState<number>(new Date().getFullYear());
  const [refreshing, setRefreshing] = useState(false);

  // 视图模式配置
  const viewModes: ViewModeButton[] = [
    {
      id: 'timeline',
      label: '时间线',
      icon: <GitCommit className="w-5 h-5" />,
      description: '按时间顺序查看文章',
      color: 'blue',
    },
    {
      id: 'calendar',
      label: '日历视图',
      icon: <Calendar className="w-5 h-5" />,
      description: '日历形式查看发布记录',
      color: 'green',
    },
    {
      id: 'category',
      label: '分类归档',
      icon: <Folder className="w-5 h-5" />,
      description: '按分类组织文章',
      color: 'purple',
    },
    {
      id: 'tag',
      label: '标签云',
      icon: <Tag className="w-5 h-5" />,
      description: '标签云形式展示',
      color: 'orange',
    },
  ];

  // 计算统计数据
  const stats: ArchiveStats = useMemo(() => {
    const defaultStats: ArchiveStats = {
      totalPosts: 0,
      totalCategories: 0,
      totalTags: 0,
      totalAuthors: 0,
      activeMonths: 0,
      averagePostsPerMonth: 0,
      firstPostDate: '',
      lastPostDate: '',
    };

    // 从不同视图的数据中汇总统计信息
    if (timelineArchive) {
      defaultStats.totalPosts = timelineArchive.totalCount;
      defaultStats.firstPostDate = timelineArchive.dateRange.from;
      defaultStats.lastPostDate = timelineArchive.dateRange.to;
      defaultStats.activeMonths = timelineArchive.years.reduce(
        (sum, year) => sum + year.months.length, 0
      );
      defaultStats.averagePostsPerMonth = defaultStats.activeMonths > 0
        ? defaultStats.totalPosts / defaultStats.activeMonths
        : 0;
    }

    if (categoryTree) {
      const countCategories = (categories: import('@/types/search').CategoryTreeNode[]): number => {
        return categories.reduce(
          (count, cat) => count + 1 + countCategories(cat.children || []), 0
        );
      };
      defaultStats.totalCategories = countCategories(categoryTree.categories);
    }

    if (tagCloud) {
      defaultStats.totalTags = tagCloud.tags.length;
    }

    return defaultStats;
  }, [timelineArchive, categoryTree, tagCloud]);

  // 初始化数据加载
  useEffect(() => {
    const initializeData = async () => {
      log.info('Initializing archive data loading', 'initializeData', { selectedYear });
      
      try {
        log.startTimer('archiveDataLoad');
        
        // 并行加载所有视图的数据
        await Promise.all([
          loadTimelineArchive(),
          loadCalendarArchive(selectedYear),
          loadCategoryTree(),
          loadTagCloud(),
        ]);
        
        log.endTimer('archiveDataLoad');
        log.info('Archive data initialization completed successfully', 'initializeData');
      } catch (error) {
        log.endTimer('archiveDataLoad');
        log.error('Failed to load archive data during initialization', 'initializeData', {
          selectedYear,
          errorMessage: (error as Error).message
        }, error as Error);
      }
    };

    initializeData();
  }, [loadTimelineArchive, loadCalendarArchive, loadCategoryTree, loadTagCloud, selectedYear, log]);

  // 处理视图切换
  const handleViewChange = async (viewType: ArchiveViewType) => {
    setCurrentView(viewType);

    // 确保切换时加载对应的数据
    switch (viewType) {
      case 'timeline':
        if (!timelineArchive) {
          await loadTimelineArchive();
        }
        break;
      case 'calendar':
        if (!calendarArchive || calendarArchive.year !== selectedYear) {
          await loadCalendarArchive(selectedYear);
        }
        break;
      case 'category':
        if (!categoryTree) {
          await loadCategoryTree();
        }
        break;
      case 'tag':
        if (!tagCloud) {
          await loadTagCloud();
        }
        break;
    }
  };

  // 刷新数据
  const handleRefresh = async () => {
    log.logUserAction('refresh', currentView, { currentView, selectedYear });
    setRefreshing(true);
    
    try {
      log.startTimer(`${currentView}Refresh`);
      
      switch (currentView) {
        case 'timeline':
          await loadTimelineArchive();
          break;
        case 'calendar':
          await loadCalendarArchive(selectedYear);
          break;
        case 'category':
          await loadCategoryTree();
          break;
        case 'tag':
          await loadTagCloud();
          break;
      }
      
      log.endTimer(`${currentView}Refresh`);
      log.info(`${currentView} archive data refreshed successfully`, 'handleRefresh', { currentView });
    } catch (error) {
      log.endTimer(`${currentView}Refresh`);
      log.error('Failed to refresh archive data', 'handleRefresh', {
        currentView,
        selectedYear,
        errorMessage: (error as Error).message
      }, error as Error);
    } finally {
      setRefreshing(false);
    }
  };

  // 渲染统计面板
  const renderStatsPanel = () => (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 p-6 mb-8">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white flex items-center">
          <BarChart3 className="w-6 h-6 mr-2 text-blue-600 dark:text-blue-400" />
          归档统计概览
        </h2>
        <button
          onClick={handleRefresh}
          disabled={refreshing}
          className="flex items-center px-3 py-2 text-sm text-gray-600 dark:text-gray-300 hover:text-gray-800 dark:hover:text-white border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <RefreshCw className={`w-4 h-4 mr-1 ${refreshing ? 'animate-spin' : ''}`} />
          {refreshing ? '更新中...' : '刷新'}
        </button>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
        <div className="text-center">
          <div className="flex items-center justify-center w-12 h-12 bg-blue-100 dark:bg-blue-900 rounded-lg mx-auto mb-3">
            <FileText className="w-6 h-6 text-blue-600 dark:text-blue-400" />
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-white mb-1">
            {stats.totalPosts.toLocaleString()}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400">总文章数</div>
        </div>

        <div className="text-center">
          <div className="flex items-center justify-center w-12 h-12 bg-green-100 dark:bg-green-900 rounded-lg mx-auto mb-3">
            <Folder className="w-6 h-6 text-green-600 dark:text-green-400" />
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-white mb-1">
            {stats.totalCategories}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400">分类数</div>
        </div>

        <div className="text-center">
          <div className="flex items-center justify-center w-12 h-12 bg-purple-100 dark:bg-purple-900 rounded-lg mx-auto mb-3">
            <Hash className="w-6 h-6 text-purple-600 dark:text-purple-400" />
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-white mb-1">
            {stats.totalTags}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400">标签数</div>
        </div>

        <div className="text-center">
          <div className="flex items-center justify-center w-12 h-12 bg-orange-100 dark:bg-orange-900 rounded-lg mx-auto mb-3">
            <TrendingUp className="w-6 h-6 text-orange-600 dark:text-orange-400" />
          </div>
          <div className="text-2xl font-bold text-gray-900 dark:text-white mb-1">
            {stats.averagePostsPerMonth.toFixed(1)}
          </div>
          <div className="text-sm text-gray-600 dark:text-gray-400">平均/月</div>
        </div>
      </div>

      {stats.firstPostDate && stats.lastPostDate && (
        <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-center space-x-6 text-sm text-gray-600 dark:text-gray-400">
            <span className="flex items-center">
              <Clock className="w-4 h-4 mr-1" />
              首篇文章: {new Date(stats.firstPostDate).toLocaleDateString()}
            </span>
            <span className="flex items-center">
              <BookOpen className="w-4 h-4 mr-1" />
              最新文章: {new Date(stats.lastPostDate).toLocaleDateString()}
            </span>
            <span className="flex items-center">
              <Calendar className="w-4 h-4 mr-1" />
              活跃月份: {stats.activeMonths} 个月
            </span>
          </div>
        </div>
      )}
    </div>
  );

  // 渲染视图切换器
  const renderViewSwitcher = () => (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 p-6 mb-8">
      <div className="flex flex-col lg:flex-row lg:items-center justify-between space-y-4 lg:space-y-0">
        {/* 视图模式选择 */}
        <div className="grid grid-cols-2 lg:flex lg:space-x-2 gap-2 lg:gap-0">
          {viewModes.map((mode) => {
            const isActive = currentView === mode.id;
            const colorClasses = {
              blue: isActive
                ? 'bg-blue-100 border-blue-300 text-blue-700 dark:bg-blue-900 dark:border-blue-600 dark:text-blue-300'
                : 'bg-white border-gray-300 text-gray-700 hover:bg-blue-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-600',
              green: isActive
                ? 'bg-green-100 border-green-300 text-green-700 dark:bg-green-900 dark:border-green-600 dark:text-green-300'
                : 'bg-white border-gray-300 text-gray-700 hover:bg-green-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-600',
              purple: isActive
                ? 'bg-purple-100 border-purple-300 text-purple-700 dark:bg-purple-900 dark:border-purple-600 dark:text-purple-300'
                : 'bg-white border-gray-300 text-gray-700 hover:bg-purple-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-600',
              orange: isActive
                ? 'bg-orange-100 border-orange-300 text-orange-700 dark:bg-orange-900 dark:border-orange-600 dark:text-orange-300'
                : 'bg-white border-gray-300 text-gray-700 hover:bg-orange-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-600',
            };

            return (
              <button
                key={mode.id}
                onClick={() => handleViewChange(mode.id)}
                className={`
                  flex items-center justify-center px-4 py-3 rounded-lg border-2 transition-all duration-200
                  ${colorClasses[mode.color as keyof typeof colorClasses]}
                  ${isActive ? 'shadow-md' : 'hover:shadow-sm'}
                `}
                title={mode.description}
              >
                {mode.icon}
                <span className="ml-2 font-medium">{mode.label}</span>
              </button>
            );
          })}
        </div>

        {/* 搜索和筛选控件 */}
        <div className="flex items-center space-x-3">
          {/* 搜索框 */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <input
              type="text"
              placeholder="搜索归档..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 pr-4 py-2 w-64 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* 年份选择器（仅日历视图显示） */}
          {currentView === 'calendar' && (
            <select
              value={selectedYear}
              onChange={(e) => setSelectedYear(Number(e.target.value))}
              className="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              {Array.from({ length: 10 }, (_, i) => {
                const year = new Date().getFullYear() - i;
                return (
                  <option key={year} value={year}>
                    {year} 年
                  </option>
                );
              })}
            </select>
          )}

          {/* 筛选按钮 */}
          <button
            onClick={() => setShowFilters(!showFilters)}
            className={`
              flex items-center px-4 py-2 rounded-lg border transition-colors
              ${showFilters
                ? 'bg-blue-100 border-blue-300 text-blue-700 dark:bg-blue-900 dark:border-blue-600 dark:text-blue-300'
                : 'bg-white border-gray-300 text-gray-700 hover:bg-gray-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-600'
              }
            `}
          >
            <Filter className="w-4 h-4 mr-2" />
            筛选
            <ChevronDown className={`w-4 h-4 ml-1 transition-transform ${showFilters ? 'rotate-180' : ''}`} />
          </button>
        </div>
      </div>

      {/* 扩展筛选面板 */}
      {showFilters && (
        <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                日期范围
              </label>
              <div className="flex space-x-2">
                <input
                  type="date"
                  className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <input
                  type="date"
                  className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                内容类型
              </label>
              <div className="space-y-2">
                {['文章', '页面', '草稿'].map((type) => (
                  <label key={type} className="flex items-center">
                    <input
                      type="checkbox"
                      className="mr-2 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                    <span className="text-sm text-gray-700 dark:text-gray-300">{type}</span>
                  </label>
                ))}
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                排序方式
              </label>
              <select className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option>发布时间（新到旧）</option>
                <option>发布时间（旧到新）</option>
                <option>标题（A-Z）</option>
                <option>标题（Z-A）</option>
                <option>阅读量</option>
                <option>评论数</option>
              </select>
            </div>
          </div>
        </div>
      )}
    </div>
  );

  // 渲染当前视图内容
  const renderCurrentView = () => {
    if (loading) {
      return (
        <div className="flex items-center justify-center py-24">
          <div className="flex items-center space-x-3">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            <span className="text-lg text-gray-600 dark:text-gray-400">
              加载{viewModes.find(m => m.id === currentView)?.label}数据...
            </span>
          </div>
        </div>
      );
    }

    if (error) {
      return (
        <div className="text-center py-24">
          <div className="text-red-500 mb-4">
            <Archive className="h-16 w-16 mx-auto mb-4" />
            <h3 className="text-xl font-semibold mb-2">加载失败</h3>
            <p className="text-gray-600 dark:text-gray-400 mb-6">{error}</p>
          </div>
          <button
            onClick={handleRefresh}
            className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            重新加载
          </button>
        </div>
      );
    }

    switch (currentView) {
      case 'timeline':
        return (
          <TimelineArchive
            className="archive-view-content"
            showStats={true}
            showFilters={true}
            viewMode="timeline"
          />
        );

      case 'calendar':
        return (
          <ArchiveCalendar
            className="archive-view-content"
            initialYear={selectedYear}
            showHeatmap={true}
            showStats={true}
            onDateClick={(date, posts) => {
              log.logUserAction('dateClick', 'calendar', { 
                date, 
                postCount: posts?.length || 0,
                currentView 
              });
            }}
          />
        );

      case 'category':
        return (
          <CategoryArchive
            className="archive-view-content"
            showSearch={true}
            showStats={true}
            viewMode="tree"
            onCategoryClick={(category) => {
              log.logUserAction('categoryClick', 'category', { 
                categoryName: category?.name || 'unknown',
                categoryId: category?.id || 'unknown',
                currentView 
              });
            }}
            onPostClick={(post) => {
              window.open(`/posts/${post.slug}`, '_blank');
            }}
          />
        );

      case 'tag':
        return (
          <TagCloud
            className="archive-view-content"
            colorScheme="rainbow"
            showSearch={true}
            showStats={true}
            layout="cloud"
            maxTags={100}
            onTagClick={(tag) => {
              log.logUserAction('tagClick', 'tagCloud', {
                tagName: tag?.name || 'unknown',
                tagSlug: tag?.slug || 'unknown',
                currentView
              });
            }}
          />
        );

      default:
        return (
          <div className="text-center py-24">
            <Archive className="h-16 w-16 text-gray-400 mx-auto mb-4" />
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
              未知视图类型
            </h3>
            <p className="text-gray-600 dark:text-gray-400">
              请选择一个有效的归档视图
            </p>
          </div>
        );
    }
  };

  return (
    <>
      <DocumentHead
        title="文章归档 - Maple Blog"
        description="浏览和探索所有发布的文章。支持多种视图模式：时间线、日历、分类和标签归档。"
        keywords="文章归档,博客归档,时间线,日历视图,分类,标签,搜索"
        ogTitle="文章归档 - Maple Blog"
        ogDescription="按时间、分类和标签浏览历史文章归档"
      />

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="max-w-7xl mx-auto">
            {/* 页面标题 */}
            <header className="mb-8">
              <div className="text-center">
                <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4 flex items-center justify-center">
                  <Archive className="w-10 h-10 mr-3 text-blue-600 dark:text-blue-400" />
                  文章归档
                </h1>
                <p className="text-xl text-gray-600 dark:text-gray-400 max-w-3xl mx-auto">
                  探索和发现所有发布的内容。支持多种视图模式，让您以最适合的方式浏览文章历史。
                </p>
              </div>
            </header>

            {/* 统计面板 */}
            {renderStatsPanel()}

            {/* 视图切换和控制面板 */}
            {renderViewSwitcher()}

            {/* 当前视图内容 */}
            <main className="archive-main">
              {renderCurrentView()}
            </main>
          </div>
        </div>
      </div>
    </>
  );
};

export default ArchivePage;