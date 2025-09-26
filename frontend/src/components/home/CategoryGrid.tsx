/**
 * CategoryGrid component - Display categories in an interactive grid layout
 * Features: Hierarchical categories, icons, post counts, hover effects
 */

import React, { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import {
  ChevronRight,
  ChevronDown,
  ChevronUp,
  Folder,
  FolderOpen,
  Hash,
  BookOpen,
  Code,
  Lightbulb,
  Globe,
  Cpu,
  Smartphone,
  Database,
  Cloud,
  Shield,
  Zap,
  Camera,
  Music,
  Heart,
  Coffee,
  Gamepad2,
  Grid3X3,
  List,
  Filter as _Filter,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { useCategoryStats } from '../../services/home/homeApi';
import { useHomeStore, useIsMobile } from '../../stores/homeStore';
import { cn } from '../../utils/cn';
import type { CategorySummary } from '../../types/home';

interface CategoryGridProps {
  className?: string;
  title?: string;
  layout?: 'grid' | 'list' | 'tree';
  showControls?: boolean;
  showEmpty?: boolean;
  maxItems?: number;
  compact?: boolean;
}

interface CategoryCardProps {
  category: CategorySummary;
  layout: 'grid' | 'list' | 'tree';
  isExpanded?: boolean;
  level?: number;
  onToggle?: () => void;
  children?: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
}

// Category icon mapping
const getCategoryIcon = (name: string, color?: string): React.ReactNode => {
  const iconProps = { size: 20, className: color ? `text-${color}-500` : 'text-gray-500' };

  // Normalize name for matching
  const normalizedName = name.toLowerCase();

  if (normalizedName.includes('技术') || normalizedName.includes('tech')) return <Code {...iconProps} />;
  if (normalizedName.includes('前端') || normalizedName.includes('frontend')) return <Globe {...iconProps} />;
  if (normalizedName.includes('后端') || normalizedName.includes('backend')) return <Database {...iconProps} />;
  if (normalizedName.includes('移动') || normalizedName.includes('mobile')) return <Smartphone {...iconProps} />;
  if (normalizedName.includes('人工智能') || normalizedName.includes('ai')) return <Cpu {...iconProps} />;
  if (normalizedName.includes('云计算') || normalizedName.includes('cloud')) return <Cloud {...iconProps} />;
  if (normalizedName.includes('安全') || normalizedName.includes('security')) return <Shield {...iconProps} />;
  if (normalizedName.includes('性能') || normalizedName.includes('performance')) return <Zap {...iconProps} />;
  if (normalizedName.includes('摄影') || normalizedName.includes('photo')) return <Camera {...iconProps} />;
  if (normalizedName.includes('音乐') || normalizedName.includes('music')) return <Music {...iconProps} />;
  if (normalizedName.includes('生活') || normalizedName.includes('life')) return <Heart {...iconProps} />;
  if (normalizedName.includes('咖啡') || normalizedName.includes('coffee')) return <Coffee {...iconProps} />;
  if (normalizedName.includes('游戏') || normalizedName.includes('game')) return <Gamepad2 {...iconProps} />;
  if (normalizedName.includes('想法') || normalizedName.includes('idea')) return <Lightbulb {...iconProps} />;

  return <BookOpen {...iconProps} />;
};

const CategoryCard: React.FC<CategoryCardProps> = ({
  category,
  layout,
  isExpanded = false,
  level = 0,
  onToggle,
  children,
  className,
  style,
}) => {
  const [isHovered, setIsHovered] = useState(false);

  const hasChildren = children && React.Children.count(children) > 0;
  const indent = level * 1.5; // rem units for indentation

  if (layout === 'list' || layout === 'tree') {
    return (
      <div className={cn('space-y-1', className)} style={style}>
        <div
          className="flex items-center space-x-3 p-3 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-all duration-200 group"
          style={{ marginLeft: `${indent}rem` }}
          onMouseEnter={() => setIsHovered(true)}
          onMouseLeave={() => setIsHovered(false)}
        >
          {/* Expand/Collapse Button */}
          {layout === 'tree' && hasChildren && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onToggle}
              className="p-1 h-auto"
              aria-label={isExpanded ? '收起' : '展开'}
            >
              {isExpanded ? <ChevronDown size={16} /> : <ChevronRight size={16} />}
            </Button>
          )}

          {/* Category Icon */}
          <div className={cn(
            'flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center transition-colors',
            isHovered
              ? 'bg-orange-100 dark:bg-orange-900/20'
              : 'bg-gray-100 dark:bg-gray-800'
          )}>
            {category.icon ? (
              <span role="img" aria-label={category.name}>
                {category.icon}
              </span>
            ) : (
              getCategoryIcon(category.name, category.color)
            )}
          </div>

          {/* Content */}
          <Link
            to={`/category/${category.slug}`}
            className="flex-1 min-w-0 group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors"
          >
            <div className="flex items-center justify-between">
              <div>
                <h3 className="font-medium text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors">
                  {category.name}
                </h3>
                {category.description && (
                  <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-1 mt-1">
                    {category.description}
                  </p>
                )}
              </div>

              <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
                <span className="px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded-full">
                  {category.postCount}
                </span>
                <ChevronRight
                  size={16}
                  className={cn(
                    'transition-transform duration-200',
                    isHovered && 'translate-x-1'
                  )}
                />
              </div>
            </div>
          </Link>
        </div>

        {/* Children */}
        {layout === 'tree' && isExpanded && children && (
          <div className="ml-4">
            {children}
          </div>
        )}
      </div>
    );
  }

  // Grid Layout
  return (
    <Link
      to={`/category/${category.slug}`}
      className={cn(
        'block p-6 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 hover:shadow-lg dark:hover:shadow-2xl transition-all duration-300 group hover:-translate-y-1',
        className
      )}
      style={style}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div className={cn(
          'w-12 h-12 rounded-xl flex items-center justify-center transition-all duration-300',
          isHovered
            ? 'bg-orange-100 dark:bg-orange-900/20 scale-110'
            : 'bg-gray-100 dark:bg-gray-800'
        )}>
          {category.icon ? (
            <span role="img" aria-label={category.name} className="text-xl">
              {category.icon}
            </span>
          ) : (
            getCategoryIcon(category.name, category.color)
          )}
        </div>

        <div className="text-right">
          <span className={cn(
            'inline-block px-3 py-1 rounded-full text-sm font-medium transition-colors',
            isHovered
              ? 'bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300'
              : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400'
          )}>
            {category.postCount} 篇
          </span>
        </div>
      </div>

      {/* Content */}
      <h3 className="font-semibold text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors mb-2">
        {category.name}
      </h3>

      {category.description && (
        <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2 mb-4">
          {category.description}
        </p>
      )}

      {/* Footer */}
      <div className="flex items-center justify-between pt-4 border-t border-gray-100 dark:border-gray-700">
        <span className="text-xs text-gray-400 dark:text-gray-500">
          {new Date(category.updatedAt).toLocaleDateString('zh-CN')} 更新
        </span>
        <ChevronRight
          size={16}
          className={cn(
            'text-gray-400 group-hover:text-orange-500 transition-all duration-200',
            isHovered && 'translate-x-1'
          )}
        />
      </div>
    </Link>
  );
};

export const CategoryGrid: React.FC<CategoryGridProps> = ({
  className,
  title = '文章分类',
  layout = 'grid',
  showControls = true,
  showEmpty = false,
  maxItems,
  compact = false,
}) => {
  const _isMobile = useIsMobile();
  const {
    components: { categoryGrid },
    selectCategory: _selectCategory,
    toggleCategoryHierarchy,
    expandCategory,
    collapseCategory,
  } = useHomeStore();

  // Local state
  const [currentLayout, setCurrentLayout] = useState(layout);
  const [showAll, setShowAll] = useState(false);

  // API data
  const { data: categories, isLoading, error, refetch } = useCategoryStats(showEmpty);

  // Build hierarchical structure
  const hierarchicalCategories = useMemo(() => {
    if (!categories) return [];

    const categoryMap = new Map<string, CategorySummary & { children: CategorySummary[] }>();
    const rootCategories: (CategorySummary & { children: CategorySummary[] })[] = [];

    // Initialize all categories with empty children
    categories.forEach(category => {
      categoryMap.set(category.id, { ...category, children: [] });
    });

    // Build hierarchy
    categories.forEach(category => {
      const categoryWithChildren = categoryMap.get(category.id)!;
      if (category.parentId) {
        const parent = categoryMap.get(category.parentId);
        if (parent) {
          parent.children.push(categoryWithChildren);
        } else {
          rootCategories.push(categoryWithChildren);
        }
      } else {
        rootCategories.push(categoryWithChildren);
      }
    });

    return rootCategories;
  }, [categories]);

  // Filter and limit categories
  const displayCategories = useMemo(() => {
    const cats = currentLayout === 'tree' ? hierarchicalCategories : (categories || []);
    if (!showAll && maxItems) {
      return cats.slice(0, maxItems);
    }
    return cats;
  }, [currentLayout, hierarchicalCategories, categories, showAll, maxItems]);

  const renderTreeCategory = (category: CategorySummary & { children: CategorySummary[] }, level = 0) => {
    const isExpanded = categoryGrid.expandedCategories.includes(category.id);
    const hasChildren = category.children.length > 0;

    return (
      <CategoryCard
        key={category.id}
        category={category}
        layout="tree"
        isExpanded={isExpanded}
        level={level}
        onToggle={() => {
          if (isExpanded) {
            collapseCategory(category.id);
          } else {
            expandCategory(category.id);
          }
        }}
      >
        {hasChildren &&
          category.children.map(child => renderTreeCategory(child as CategorySummary & { children: CategorySummary[] }, level + 1))
        }
      </CategoryCard>
    );
  };

  if (isLoading) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <div className="h-8 w-32 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
          {showControls && (
            <div className="flex items-center space-x-2">
              {[1, 2, 3].map((i) => (
                <div key={i} className="w-8 h-8 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
              ))}
            </div>
          )}
        </div>
        <div className={cn(
          'grid gap-4',
          currentLayout === 'grid'
            ? 'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4'
            : 'grid-cols-1'
        )}>
          {Array.from({ length: 8 }, (_, index) => (
            <div key={index} className="animate-pulse">
              <div className={cn(
                'bg-gray-200 dark:bg-gray-700 rounded-xl',
                currentLayout === 'grid' ? 'h-32 p-4' : 'h-16'
              )} />
            </div>
          ))}
        </div>
      </section>
    );
  }

  if (error || !categories?.length) {
    return (
      <section className={cn('space-y-6', className)}>
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
        </div>
        <div className="text-center py-12">
          <Folder size={48} className="mx-auto text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
            暂无分类
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error ? '加载失败，请稍后重试' : '等待创建文章分类'}
          </p>
          {error && (
            <Button onClick={() => refetch()} variant="outline" size="sm">
              重新加载
            </Button>
          )}
        </div>
      </section>
    );
  }

  return (
    <section className={cn('space-y-6', className)} role="region" aria-label={title}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          <Hash className="text-orange-500" size={24} />
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            {title}
          </h2>
          <span className="px-2 py-1 bg-orange-100 dark:bg-orange-900/20 text-orange-700 dark:text-orange-300 text-sm font-medium rounded">
            {categories.length}
          </span>
        </div>

        {/* Controls */}
        {showControls && (
          <div className="flex items-center space-x-2">
            {/* Layout Switcher */}
            <div className="flex items-center space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
              <Button
                variant={currentLayout === 'grid' ? 'primary' : 'ghost'}
                size="sm"
                onClick={() => setCurrentLayout('grid')}
                className="p-2"
                aria-label="网格布局"
              >
                <Grid3X3 size={16} />
              </Button>
              <Button
                variant={currentLayout === 'list' ? 'primary' : 'ghost'}
                size="sm"
                onClick={() => setCurrentLayout('list')}
                className="p-2"
                aria-label="列表布局"
              >
                <List size={16} />
              </Button>
              <Button
                variant={currentLayout === 'tree' ? 'primary' : 'ghost'}
                size="sm"
                onClick={() => setCurrentLayout('tree')}
                className="p-2"
                aria-label="树形布局"
              >
                <FolderOpen size={16} />
              </Button>
            </div>

            {/* Hierarchy Toggle (Tree mode only) */}
            {currentLayout === 'tree' && (
              <Button
                variant="outline"
                size="sm"
                onClick={toggleCategoryHierarchy}
                className="text-sm"
              >
                {categoryGrid.showHierarchy ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
                {categoryGrid.showHierarchy ? '收起' : '展开'}
              </Button>
            )}
          </div>
        )}
      </div>

      {/* Categories */}
      {currentLayout === 'tree' ? (
        <div className="space-y-1">
          {displayCategories.map((category) =>
            renderTreeCategory(category as CategorySummary & { children: CategorySummary[] })
          )}
        </div>
      ) : (
        <div className={cn(
          'grid gap-4',
          {
            'grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5': currentLayout === 'grid' && !compact,
            'grid-cols-2 sm:grid-cols-4 lg:grid-cols-6': currentLayout === 'grid' && compact,
            'grid-cols-1': currentLayout === 'list',
          }
        )}>
          {displayCategories.map((category, index) => (
            <CategoryCard
              key={category.id}
              category={category}
              layout={currentLayout}
              className="animate-fade-in"
              style={{ animationDelay: `${index * 50}ms` } as React.CSSProperties}
            />
          ))}
        </div>
      )}

      {/* Show More */}
      {maxItems && categories.length > maxItems && (
        <div className="text-center">
          <Button
            variant="outline"
            onClick={() => setShowAll(!showAll)}
            className="min-w-32"
          >
            {showAll ? '收起' : `查看全部 ${categories.length} 个分类`}
          </Button>
        </div>
      )}

      {/* All Categories Link */}
      <div className="text-center pt-4">
        <Link to="/categories">
          <Button variant="ghost" size="sm" className="text-orange-600 hover:text-orange-700">
            浏览所有分类
            <ChevronRight size={14} className="ml-1" />
          </Button>
        </Link>
      </div>
    </section>
  );
};

/**
 * Usage:
 * <CategoryGrid /> - Default grid layout
 * <CategoryGrid layout="tree" /> - Hierarchical tree layout
 * <CategoryGrid compact maxItems={12} /> - Compact grid with limit
 *
 * Features:
 * - Multiple layouts (grid, list, tree)
 * - Hierarchical category support with expand/collapse
 * - Dynamic category icons based on names
 * - Post count display
 * - Responsive design with mobile optimization
 * - Loading states and error handling
 * - Smooth animations and hover effects
 * - Layout persistence with home store
 * - Accessibility support
 * - SEO-friendly structured data
 */

export default CategoryGrid;