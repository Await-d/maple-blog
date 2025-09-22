// @ts-nocheck
/**
 * TagCloud Component
 * 标签云组件 - 以标签云形式展示所有标签
 */

import { useState, useEffect, useMemo } from 'react';
import {
  Hash,
  Search,
  BarChart3,
  TrendingUp,
  FileText,
  Grid,
  List,
  Shuffle,
} from 'lucide-react';
import { useArchiveStore } from '@/stores/searchStore';
import { TagCloudItem, ArchivePost } from '@/types/search';
import { archiveApi } from '@/services/search/archiveApi';

interface TagCloudProps {
  className?: string;
  minFontSize?: number;
  maxFontSize?: number;
  colorScheme?: 'blue' | 'rainbow' | 'green' | 'purple';
  showSearch?: boolean;
  showStats?: boolean;
  onTagClick?: (tag: TagCloudItem) => void;
  maxTags?: number;
  layout?: 'cloud' | 'list' | 'grid';
}

interface TagItemProps {
  tag: TagCloudItem;
  minSize: number;
  maxSize: number;
  colorScheme: string;
  layout: string;
  onClick: (tag: TagCloudItem) => void;
}

function TagItem({ tag, minSize, maxSize, colorScheme, layout, onClick }: TagItemProps) {
  // 计算字体大小
  const fontSize = minSize + (tag.weight * (maxSize - minSize));

  // 颜色方案
  const colorSchemes = {
    blue: [
      'text-blue-600 hover:text-blue-800 bg-blue-50 hover:bg-blue-100',
      'text-blue-700 hover:text-blue-900 bg-blue-100 hover:bg-blue-200',
      'text-blue-800 hover:text-blue-900 bg-blue-200 hover:bg-blue-300',
      'text-blue-900 hover:text-blue-900 bg-blue-300 hover:bg-blue-400',
    ],
    rainbow: [
      'text-red-600 hover:text-red-800 bg-red-50 hover:bg-red-100',
      'text-green-600 hover:text-green-800 bg-green-50 hover:bg-green-100',
      'text-blue-600 hover:text-blue-800 bg-blue-50 hover:bg-blue-100',
      'text-purple-600 hover:text-purple-800 bg-purple-50 hover:bg-purple-100',
      'text-orange-600 hover:text-orange-800 bg-orange-50 hover:bg-orange-100',
      'text-pink-600 hover:text-pink-800 bg-pink-50 hover:bg-pink-100',
    ],
    green: [
      'text-green-600 hover:text-green-800 bg-green-50 hover:bg-green-100',
      'text-green-700 hover:text-green-900 bg-green-100 hover:bg-green-200',
      'text-green-800 hover:text-green-900 bg-green-200 hover:bg-green-300',
      'text-green-900 hover:text-green-900 bg-green-300 hover:bg-green-400',
    ],
    purple: [
      'text-purple-600 hover:text-purple-800 bg-purple-50 hover:bg-purple-100',
      'text-purple-700 hover:text-purple-900 bg-purple-100 hover:bg-purple-200',
      'text-purple-800 hover:text-purple-900 bg-purple-200 hover:bg-purple-300',
      'text-purple-900 hover:text-purple-900 bg-purple-300 hover:bg-purple-400',
    ],
  };

  const colors = colorSchemes[colorScheme as keyof typeof colorSchemes] || colorSchemes.blue;
  const colorIndex = Math.floor(tag.weight * (colors.length - 1));
  const colorClass = colors[colorIndex];

  if (layout === 'list') {
    return (
      <div
        onClick={() => onClick(tag)}
        className="flex items-center justify-between p-3 bg-white border border-gray-200 rounded-lg hover:border-blue-300 hover:shadow-sm cursor-pointer transition-all"
      >
        <div className="flex items-center space-x-3">
          <Hash className="h-5 w-5 text-gray-400" />
          <div>
            <h3 className="font-medium text-gray-900 hover:text-blue-600 transition-colors">
              {tag.name}
            </h3>
            <p className="text-sm text-gray-600">
              {tag.count} 篇文章
            </p>
          </div>
        </div>
        <div className="text-right">
          <div className="text-sm text-gray-500">
            权重: {Math.round(tag.weight * 100)}%
          </div>
        </div>
      </div>
    );
  }

  if (layout === 'grid') {
    return (
      <div
        onClick={() => onClick(tag)}
        className="group cursor-pointer bg-white border border-gray-200 rounded-lg p-4 hover:border-blue-300 hover:shadow-md transition-all duration-200"
      >
        <div className="flex items-center justify-center mb-3">
          <div className="p-3 bg-blue-100 rounded-full group-hover:bg-blue-200 transition-colors">
            <Hash className="h-6 w-6 text-blue-600" />
          </div>
        </div>
        <div className="text-center">
          <h3 className="font-semibold text-gray-900 group-hover:text-blue-600 transition-colors truncate">
            {tag.name}
          </h3>
          <p className="text-sm text-gray-600 mt-1">
            {tag.count} 篇文章
          </p>
          <div className="mt-2">
            <div
              className="bg-gray-200 rounded-full h-2"
              title={`权重: ${Math.round(tag.weight * 100)}%`}
            >
              <div
                className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                style={{ width: `${tag.weight * 100}%` }}
              />
            </div>
          </div>
        </div>
      </div>
    );
  }

  // 默认云状布局
  return (
    <button
      onClick={() => onClick(tag)}
      className={`
        inline-block px-3 py-1 m-1 rounded-full font-medium transition-all duration-200
        hover:scale-110 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2
        ${colorClass}
      `}
      style={{
        fontSize: `${fontSize}px`,
        lineHeight: `${fontSize + 4}px`,
      }}
      title={`${tag.name} - ${tag.count} 篇文章`}
    >
      #{tag.name}
    </button>
  );
}

export default function TagCloud({
  className = '',
  minFontSize = 12,
  maxFontSize = 24,
  colorScheme = 'rainbow',
  showSearch = true,
  showStats = true,
  onTagClick,
  maxTags = 100,
  layout: initialLayout = 'cloud',
}: TagCloudProps) {
  const { tagCloud, loading, error, loadTagCloud } = useArchiveStore();
  const [searchTerm, setSearchTerm] = useState('');
  const [layout, setLayout] = useState<'cloud' | 'list' | 'grid'>(initialLayout);
  const [sortBy, setSortBy] = useState<'name' | 'count' | 'random'>('count');
  const [selectedTags, setSelectedTags] = useState<Set<string>>(new Set());
  const [tagPosts, setTagPosts] = useState<Record<string, ArchivePost[]>>({});

  // 加载标签云数据
  useEffect(() => {
    loadTagCloud();
  }, [loadTagCloud]);

  // 过滤和排序标签
  const processedTags = useMemo(() => {
    if (!tagCloud) return [];

    let tags = tagCloud.tags;

    // 搜索过滤
    if (searchTerm) {
      tags = tags.filter(tag =>
        tag.name.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    // 排序
    tags = [...tags].sort((a, b) => {
      switch (sortBy) {
        case 'name':
          return a.name.localeCompare(b.name);
        case 'random':
          return Math.random() - 0.5;
        default: // count
          return b.count - a.count;
      }
    });

    // 限制数量
    return tags.slice(0, maxTags);
  }, [tagCloud, searchTerm, sortBy, maxTags]);

  // 统计信息
  const stats = useMemo(() => {
    if (!tagCloud) return { totalTags: 0, totalPosts: 0, avgPostsPerTag: 0 };

    const totalTags = tagCloud.tags.length;
    const totalPosts = tagCloud.tags.reduce((sum, tag) => sum + tag.count, 0);
    const avgPostsPerTag = totalTags > 0 ? totalPosts / totalTags : 0;

    return { totalTags, totalPosts, avgPostsPerTag };
  }, [tagCloud]);

  // 处理标签点击
  const handleTagClick = async (tag: TagCloudItem) => {
    // 加载标签相关的文章
    if (!tagPosts[tag.slug]) {
      try {
        const response = await archiveApi.getTagPosts(tag.slug, 1, 10);
        setTagPosts(prev => ({
          ...prev,
          [tag.slug]: response.data,
        }));
      } catch (error) {
        console.error('Failed to load tag posts:', error);
      }
    }

    // 切换标签选择状态
    setSelectedTags(prev => {
      const newSet = new Set(prev);
      if (newSet.has(tag.slug)) {
        newSet.delete(tag.slug);
      } else {
        newSet.add(tag.slug);
      }
      return newSet;
    });

    onTagClick?.(tag);
  };

  // 随机打乱标签顺序
  const shuffleTags = () => {
    setSortBy('random');
  };

  if (loading) {
    return (
      <div className={`flex items-center justify-center py-12 ${className}`}>
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500" />
          <span className="text-gray-600">加载标签云...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-red-500 mb-4">
          <Hash className="h-12 w-12 mx-auto mb-2" />
          <p>加载标签云失败</p>
        </div>
        <p className="text-gray-600 mb-4">{error}</p>
        <button
          onClick={() => loadTagCloud()}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          重试
        </button>
      </div>
    );
  }

  if (!tagCloud || processedTags.length === 0) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <Hash className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {searchTerm ? '没有找到匹配的标签' : '暂无标签'}
        </h3>
        <p className="text-gray-600">
          {searchTerm ? '试试调整搜索关键词' : '还没有创建任何标签'}
        </p>
      </div>
    );
  }

  return (
    <div className={`tag-cloud ${className}`}>
      {/* 控制面板 */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between space-y-4 lg:space-y-0">
          {/* 统计信息 */}
          {showStats && (
            <div className="flex items-center space-x-6 text-sm text-gray-600">
              <span className="flex items-center">
                <Hash className="h-4 w-4 mr-1" />
                {stats.totalTags} 个标签
              </span>
              <span className="flex items-center">
                <FileText className="h-4 w-4 mr-1" />
                {stats.totalPosts} 篇文章
              </span>
              <span className="flex items-center">
                <BarChart3 className="h-4 w-4 mr-1" />
                平均 {stats.avgPostsPerTag.toFixed(1)} 篇/标签
              </span>
              <span className="flex items-center">
                <TrendingUp className="h-4 w-4 mr-1" />
                显示前 {processedTags.length} 个
              </span>
            </div>
          )}

          {/* 控制区域 */}
          <div className="flex flex-col sm:flex-row sm:items-center space-y-2 sm:space-y-0 sm:space-x-4">
            {/* 搜索框 */}
            {showSearch && (
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                <input
                  type="text"
                  placeholder="搜索标签..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            )}

            {/* 排序选择 */}
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as 'name' | 'count' | 'random')}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="count">按使用频率排序</option>
              <option value="name">按名称排序</option>
              <option value="random">随机排序</option>
            </select>

            {/* 随机按钮 */}
            <button
              onClick={shuffleTags}
              className="flex items-center px-3 py-2 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              <Shuffle className="h-4 w-4 mr-1" />
              打乱
            </button>

            {/* 布局切换 */}
            <div className="flex bg-gray-100 rounded-lg p-1">
              <button
                onClick={() => setLayout('cloud')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  layout === 'cloud'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
                title="标签云"
              >
                <Hash className="h-4 w-4" />
              </button>
              <button
                onClick={() => setLayout('list')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  layout === 'list'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
                title="列表视图"
              >
                <List className="h-4 w-4" />
              </button>
              <button
                onClick={() => setLayout('grid')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  layout === 'grid'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
                title="网格视图"
              >
                <Grid className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* 标签云展示 */}
      <div className="bg-white border border-gray-200 rounded-lg p-6">
        {layout === 'cloud' && (
          <div className="text-center leading-relaxed">
            {processedTags.map((tag) => (
              <TagItem
                key={tag.slug}
                tag={tag}
                minSize={minFontSize}
                maxSize={maxFontSize}
                colorScheme={colorScheme}
                layout={layout}
                onClick={handleTagClick}
              />
            ))}
          </div>
        )}

        {layout === 'list' && (
          <div className="space-y-3">
            {processedTags.map((tag) => (
              <TagItem
                key={tag.slug}
                tag={tag}
                minSize={minFontSize}
                maxSize={maxFontSize}
                colorScheme={colorScheme}
                layout={layout}
                onClick={handleTagClick}
              />
            ))}
          </div>
        )}

        {layout === 'grid' && (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {processedTags.map((tag) => (
              <TagItem
                key={tag.slug}
                tag={tag}
                minSize={minFontSize}
                maxSize={maxFontSize}
                colorScheme={colorScheme}
                layout={layout}
                onClick={handleTagClick}
              />
            ))}
          </div>
        )}
      </div>

      {/* 选中的标签信息 */}
      {selectedTags.size > 0 && (
        <div className="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center justify-between mb-3">
            <h3 className="text-lg font-medium text-blue-900">
              已选中 {selectedTags.size} 个标签
            </h3>
            <button
              onClick={() => setSelectedTags(new Set())}
              className="text-blue-600 hover:text-blue-800 text-sm"
            >
              清除选择
            </button>
          </div>

          <div className="space-y-2">
            {Array.from(selectedTags).map((tagSlug) => {
              const tag = processedTags.find(t => t.slug === tagSlug);
              const posts = tagPosts[tagSlug] || [];

              return (
                tag && (
                  <div key={tagSlug} className="bg-white rounded-lg p-3 border border-blue-200">
                    <div className="flex items-center justify-between mb-2">
                      <h4 className="font-medium text-gray-900 flex items-center">
                        <Hash className="h-4 w-4 mr-1 text-blue-500" />
                        {tag.name}
                      </h4>
                      <span className="text-sm text-gray-600">
                        {tag.count} 篇文章
                      </span>
                    </div>

                    {posts.length > 0 && (
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                        {posts.slice(0, 4).map((post) => (
                          <div
                            key={post.id}
                            className="text-sm text-gray-700 hover:text-blue-600 cursor-pointer truncate"
                            onClick={() => window.open(`/posts/${post.slug}`, '_blank')}
                          >
                            • {post.title}
                          </div>
                        ))}
                        {tag.count > 4 && (
                          <div className="text-sm text-gray-500">
                            +{tag.count - 4} 更多文章...
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                )
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}