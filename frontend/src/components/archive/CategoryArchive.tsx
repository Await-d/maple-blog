// @ts-nocheck
/**
 * CategoryArchive Component
 * 分类归档组件 - 展示层级分类结构和相关文章
 */

import { useState, useEffect, useMemo } from 'react';
import {
  Folder,
  FolderOpen,
  ChevronRight,
  ChevronDown,
  FileText,
  Search,
  Grid,
  List,
  BarChart3,
  Eye,
  Calendar,
} from 'lucide-react';
import { useArchiveStore } from '@/stores/searchStore';
import { CategoryTreeNode, ArchivePost } from '@/types/search';
import { archiveApi } from '@/services/search/archiveApi';

interface CategoryArchiveProps {
  className?: string;
  showSearch?: boolean;
  showStats?: boolean;
  initialExpandedCategories?: Set<string>;
  onCategoryClick?: (category: CategoryTreeNode) => void;
  onPostClick?: (post: ArchivePost) => void;
  viewMode?: 'tree' | 'grid' | 'list';
}

interface CategoryNodeProps {
  category: CategoryTreeNode;
  expandedCategories: Set<string>;
  onToggle: (categoryId: string) => void;
  onCategoryClick?: (category: CategoryTreeNode) => void;
  onPostClick?: (post: ArchivePost) => void;
  level: number;
  showPosts: boolean;
}

function CategoryNode({
  category,
  expandedCategories,
  onToggle,
  onCategoryClick,
  onPostClick,
  level,
  showPosts,
}: CategoryNodeProps) {
  const isExpanded = expandedCategories.has(category.id);
  const hasChildren = category.children.length > 0;
  const hasPosts = category.posts && category.posts.length > 0;

  const indentClass = `ml-${level * 4}`;

  return (
    <div className="category-node">
      {/* 分类节点 */}
      <div
        className={`
          flex items-center p-3 rounded-lg cursor-pointer group
          hover:bg-gray-50 transition-colors border-l-4
          ${category.color ? `border-l-${category.color}-500` : 'border-l-blue-500'}
          ${indentClass}
        `}
        onClick={() => onCategoryClick?.(category)}
      >
        {/* 展开/折叠按钮 */}
        <button
          onClick={(e) => {
            e.stopPropagation();
            onToggle(category.id);
          }}
          className="mr-2 p-1 rounded hover:bg-gray-100 transition-colors"
          disabled={!hasChildren && !hasPosts}
        >
          {hasChildren || hasPosts ? (
            isExpanded ? (
              <ChevronDown className="h-4 w-4 text-gray-500" />
            ) : (
              <ChevronRight className="h-4 w-4 text-gray-500" />
            )
          ) : (
            <div className="w-4 h-4" />
          )}
        </button>

        {/* 分类图标 */}
        <div className="mr-3">
          {isExpanded ? (
            <FolderOpen className="h-5 w-5 text-blue-500" />
          ) : (
            <Folder className="h-5 w-5 text-gray-500" />
          )}
        </div>

        {/* 分类信息 */}
        <div className="flex-1 min-w-0">
          <h3 className="font-medium text-gray-900 group-hover:text-blue-600 transition-colors truncate">
            {category.name}
          </h3>
          {category.description && (
            <p className="text-sm text-gray-600 mt-1 line-clamp-2">
              {category.description}
            </p>
          )}
        </div>

        {/* 统计信息 */}
        <div className="flex items-center space-x-4 text-sm text-gray-500">
          <span className="flex items-center">
            <FileText className="h-4 w-4 mr-1" />
            {category.count}
          </span>
          {category.children.length > 0 && (
            <span className="flex items-center">
              <Folder className="h-4 w-4 mr-1" />
              {category.children.length}
            </span>
          )}
        </div>
      </div>

      {/* 子分类和文章 */}
      {isExpanded && (
        <div className="mt-2 space-y-2">
          {/* 子分类 */}
          {category.children.map((child) => (
            <CategoryNode
              key={child.id}
              category={child}
              expandedCategories={expandedCategories}
              onToggle={onToggle}
              onCategoryClick={onCategoryClick}
              onPostClick={onPostClick}
              level={level + 1}
              showPosts={showPosts}
            />
          ))}

          {/* 文章列表 */}
          {showPosts && hasPosts && (
            <div className={`space-y-2 ${indentClass} ml-8`}>
              {category.posts!.map((post) => (
                <article
                  key={post.id}
                  className="group cursor-pointer p-3 bg-white border border-gray-200 rounded-lg hover:border-blue-300 hover:shadow-sm transition-all"
                  onClick={() => onPostClick?.(post)}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1 min-w-0">
                      <h4 className="font-medium text-gray-900 group-hover:text-blue-600 line-clamp-2">
                        {post.title}
                      </h4>
                      {post.excerpt && (
                        <p className="text-sm text-gray-600 mt-1 line-clamp-2">
                          {post.excerpt}
                        </p>
                      )}
                      <div className="flex items-center space-x-4 mt-2 text-xs text-gray-500">
                        <span className="flex items-center">
                          <Calendar className="h-3 w-3 mr-1" />
                          {new Date(post.publishedAt).toLocaleDateString()}
                        </span>
                        <span className="flex items-center">
                          <Eye className="h-3 w-3 mr-1" />
                          {post.viewCount}
                        </span>
                        <span>{post.author.displayName}</span>
                      </div>
                    </div>
                    {post.thumbnailUrl && (
                      <img
                        src={post.thumbnailUrl}
                        alt={post.title}
                        className="w-16 h-12 object-cover rounded ml-3 bg-gray-100"
                        loading="lazy"
                      />
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

interface CategoryCardProps {
  category: CategoryTreeNode;
  onCategoryClick?: (category: CategoryTreeNode) => void;
}

function CategoryCard({ category, onCategoryClick }: CategoryCardProps) {
  return (
    <div
      className="group cursor-pointer bg-white border border-gray-200 rounded-lg p-6 hover:border-blue-300 hover:shadow-md transition-all duration-200"
      onClick={() => onCategoryClick?.(category)}
    >
      {/* 分类图标和名称 */}
      <div className="flex items-center space-x-3 mb-4">
        <div className={`p-3 rounded-lg ${category.color ? `bg-${category.color}-100` : 'bg-blue-100'}`}>
          <Folder className={`h-6 w-6 ${category.color ? `text-${category.color}-600` : 'text-blue-600'}`} />
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-gray-900 group-hover:text-blue-600 transition-colors truncate">
            {category.name}
          </h3>
          <p className="text-sm text-gray-600">
            {category.count} 篇文章
          </p>
        </div>
      </div>

      {/* 分类描述 */}
      {category.description && (
        <p className="text-gray-600 text-sm mb-4 line-clamp-3">
          {category.description}
        </p>
      )}

      {/* 统计信息 */}
      <div className="flex items-center justify-between text-sm">
        <div className="flex items-center space-x-4 text-gray-500">
          <span className="flex items-center">
            <FileText className="h-4 w-4 mr-1" />
            {category.count} 文章
          </span>
          {category.children.length > 0 && (
            <span className="flex items-center">
              <Folder className="h-4 w-4 mr-1" />
              {category.children.length} 子分类
            </span>
          )}
        </div>

        {/* 最近更新 */}
        {category.posts && category.posts.length > 0 && (
          <span className="text-gray-500">
            最新: {new Date(category.posts[0].publishedAt).toLocaleDateString()}
          </span>
        )}
      </div>

      {/* 子分类快速链接 */}
      {category.children.length > 0 && (
        <div className="mt-4 pt-4 border-t border-gray-100">
          <div className="flex flex-wrap gap-2">
            {category.children.slice(0, 4).map((child) => (
              <span
                key={child.id}
                className="inline-flex items-center px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded-full hover:bg-gray-200 transition-colors"
              >
                {child.name}
                <span className="ml-1 text-gray-500">({child.count})</span>
              </span>
            ))}
            {category.children.length > 4 && (
              <span className="text-xs text-gray-500">
                +{category.children.length - 4} 更多
              </span>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default function CategoryArchive({
  className = '',
  showSearch = true,
  showStats = true,
  initialExpandedCategories = new Set(),
  onCategoryClick,
  onPostClick,
  viewMode: initialViewMode = 'tree',
}: CategoryArchiveProps) {
  const { categoryTree, loading, error, loadCategoryTree } = useArchiveStore();
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(initialExpandedCategories);
  const [searchTerm, setSearchTerm] = useState('');
  const [viewMode, setViewMode] = useState<'tree' | 'grid' | 'list'>(initialViewMode);
  const [showPosts, setShowPosts] = useState(false);
  const [sortBy, setSortBy] = useState<'name' | 'count' | 'recent'>('name');

  // 加载分类树
  useEffect(() => {
    loadCategoryTree();
  }, [loadCategoryTree]);

  // 过滤和排序分类
  const filteredCategories = useMemo(() => {
    if (!categoryTree) return [];

    let categories = categoryTree.categories;

    // 搜索过滤
    if (searchTerm) {
      const filterCategories = (cats: CategoryTreeNode[]): CategoryTreeNode[] => {
        return cats.filter(cat => {
          const matchesName = cat.name.toLowerCase().includes(searchTerm.toLowerCase());
          const matchesDescription = cat.description?.toLowerCase().includes(searchTerm.toLowerCase());
          const hasMatchingChildren = cat.children.length > 0 && filterCategories(cat.children).length > 0;

          return matchesName || matchesDescription || hasMatchingChildren;
        }).map(cat => ({
          ...cat,
          children: filterCategories(cat.children),
        }));
      };
      categories = filterCategories(categories);
    }

    // 排序
    const sortCategories = (cats: CategoryTreeNode[]): CategoryTreeNode[] => {
      return cats.sort((a, b) => {
        switch (sortBy) {
          case 'count':
            return b.count - a.count;
          case 'recent': {
            const aRecent = a.posts?.[0]?.publishedAt || '';
            const bRecent = b.posts?.[0]?.publishedAt || '';
            return bRecent.localeCompare(aRecent);
          }
          default:
            return a.name.localeCompare(b.name);
        }
      }).map(cat => ({
        ...cat,
        children: sortCategories(cat.children),
      }));
    };

    return sortCategories(categories);
  }, [categoryTree, searchTerm, sortBy]);

  // 统计信息
  const stats = useMemo(() => {
    if (!categoryTree) return { totalCategories: 0, totalPosts: 0, avgPostsPerCategory: 0 };

    const countCategories = (cats: CategoryTreeNode[]): number => {
      return cats.reduce((sum, cat) => sum + 1 + countCategories(cat.children), 0);
    };

    const totalCategories = countCategories(categoryTree.categories);
    const totalPosts = categoryTree.totalCount;
    const avgPostsPerCategory = totalCategories > 0 ? totalPosts / totalCategories : 0;

    return { totalCategories, totalPosts, avgPostsPerCategory };
  }, [categoryTree]);

  // 处理分类展开/折叠
  const handleToggle = (categoryId: string) => {
    setExpandedCategories(prev => {
      const newSet = new Set(prev);
      if (newSet.has(categoryId)) {
        newSet.delete(categoryId);
      } else {
        newSet.add(categoryId);
      }
      return newSet;
    });
  };

  // 展开所有分类
  const expandAll = () => {
    if (!categoryTree) return;

    const getAllIds = (cats: CategoryTreeNode[]): string[] => {
      return cats.reduce((ids: string[], cat) => {
        return [...ids, cat.id, ...getAllIds(cat.children)];
      }, []);
    };

    setExpandedCategories(new Set(getAllIds(categoryTree.categories)));
  };

  // 折叠所有分类
  const collapseAll = () => {
    setExpandedCategories(new Set());
  };

  // 处理分类点击
  const handleCategoryClick = async (category: CategoryTreeNode) => {
    // 如果分类没有加载文章，则加载文章
    if (!category.posts) {
      try {
        const _response = await archiveApi.getCategoryPosts(category.slug);
        // 这里应该更新分类的文章数据
        // 由于状态管理的限制，这里只是示例
      } catch (error) {
        console.error('Failed to load category posts:', error);
      }
    }

    onCategoryClick?.(category);
  };

  if (loading) {
    return (
      <div className={`flex items-center justify-center py-12 ${className}`}>
        <div className="flex items-center space-x-3">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500" />
          <span className="text-gray-600">加载分类数据...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <div className="text-red-500 mb-4">
          <Folder className="h-12 w-12 mx-auto mb-2" />
          <p>加载分类失败</p>
        </div>
        <p className="text-gray-600 mb-4">{error}</p>
        <button
          onClick={() => loadCategoryTree()}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          重试
        </button>
      </div>
    );
  }

  if (!categoryTree || filteredCategories.length === 0) {
    return (
      <div className={`text-center py-12 ${className}`}>
        <Folder className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {searchTerm ? '没有找到匹配的分类' : '暂无分类'}
        </h3>
        <p className="text-gray-600">
          {searchTerm ? '试试调整搜索关键词' : '还没有创建任何分类'}
        </p>
      </div>
    );
  }

  return (
    <div className={`category-archive ${className}`}>
      {/* 控制面板 */}
      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between space-y-4 lg:space-y-0">
          {/* 统计信息 */}
          {showStats && (
            <div className="flex items-center space-x-6 text-sm text-gray-600">
              <span className="flex items-center">
                <Folder className="h-4 w-4 mr-1" />
                {stats.totalCategories} 个分类
              </span>
              <span className="flex items-center">
                <FileText className="h-4 w-4 mr-1" />
                {stats.totalPosts} 篇文章
              </span>
              <span className="flex items-center">
                <BarChart3 className="h-4 w-4 mr-1" />
                平均 {stats.avgPostsPerCategory.toFixed(1)} 篇/分类
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
                  placeholder="搜索分类..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10 pr-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            )}

            {/* 排序选择 */}
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value as 'name' | 'count' | 'recent')}
              className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="name">按名称排序</option>
              <option value="count">按文章数排序</option>
              <option value="recent">按最新排序</option>
            </select>

            {/* 视图切换 */}
            <div className="flex bg-gray-100 rounded-lg p-1">
              <button
                onClick={() => setViewMode('tree')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  viewMode === 'tree'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                <List className="h-4 w-4" />
              </button>
              <button
                onClick={() => setViewMode('grid')}
                className={`px-3 py-1 rounded text-sm transition-colors ${
                  viewMode === 'grid'
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                <Grid className="h-4 w-4" />
              </button>
            </div>

            {/* 显示文章切换 */}
            {viewMode === 'tree' && (
              <label className="flex items-center text-sm text-gray-600">
                <input
                  type="checkbox"
                  checked={showPosts}
                  onChange={(e) => setShowPosts(e.target.checked)}
                  className="mr-2"
                />
                显示文章
              </label>
            )}

            {/* 展开/折叠按钮 */}
            {viewMode === 'tree' && (
              <div className="flex space-x-2">
                <button
                  onClick={expandAll}
                  className="text-sm text-blue-600 hover:text-blue-700 transition-colors"
                >
                  全部展开
                </button>
                <button
                  onClick={collapseAll}
                  className="text-sm text-gray-600 hover:text-gray-700 transition-colors"
                >
                  全部折叠
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* 分类展示 */}
      {viewMode === 'tree' ? (
        <div className="space-y-2">
          {filteredCategories.map((category) => (
            <CategoryNode
              key={category.id}
              category={category}
              expandedCategories={expandedCategories}
              onToggle={handleToggle}
              onCategoryClick={handleCategoryClick}
              onPostClick={onPostClick}
              level={0}
              showPosts={showPosts}
            />
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredCategories.map((category) => (
            <CategoryCard
              key={category.id}
              category={category}
              onCategoryClick={handleCategoryClick}
            />
          ))}
        </div>
      )}
    </div>
  );
}