/**
 * SearchFilters Component
 * 搜索筛选器组件 - 提供各种筛选选项
 */

import React, { useState, useMemo } from 'react';
import {
  Calendar,
  User,
  Folder,
  Hash,
  FileText,
  CheckCircle,
  Clock as _Clock,
  Search,
  ChevronDown,
  ChevronUp,
} from 'lucide-react';
import type { SearchFilters, ContentType, PostStatus } from '@/types/search';

interface SearchFiltersProps {
  filters: SearchFilters;
  filterOptions: {
    categories: Array<{ name: string; count: number }>;
    tags: Array<{ name: string; count: number }>;
    authors: Array<{ name: string; count: number }>;
    years: Array<{ year: number; count: number }>;
  };
  onChange: (key: keyof SearchFilters, value: unknown) => void;
  className?: string;
}

interface FilterSectionProps {
  title: string;
  icon: React.ComponentType<{ className?: string }>;
  children: React.ReactNode;
  defaultExpanded?: boolean;
  count?: number;
}

function FilterSection({
  title,
  icon: Icon,
  children,
  defaultExpanded = false,
  count,
}: FilterSectionProps) {
  const [expanded, setExpanded] = useState(defaultExpanded);

  return (
    <div className="border border-gray-200 rounded-lg">
      <button
        onClick={() => setExpanded(!expanded)}
        className="w-full flex items-center justify-between p-3 text-left hover:bg-gray-50 transition-colors"
      >
        <div className="flex items-center space-x-2">
          <Icon className="h-4 w-4 text-gray-500" />
          <span className="font-medium text-gray-900">{title}</span>
          {count !== undefined && count > 0 && (
            <span className="bg-blue-100 text-blue-800 text-xs font-medium px-2 py-1 rounded-full">
              {count}
            </span>
          )}
        </div>
        {expanded ? (
          <ChevronUp className="h-4 w-4 text-gray-400" />
        ) : (
          <ChevronDown className="h-4 w-4 text-gray-400" />
        )}
      </button>
      {expanded && (
        <div className="border-t border-gray-200 p-3 space-y-2">
          {children}
        </div>
      )}
    </div>
  );
}

interface MultiSelectProps {
  options: Array<{ name: string; count: number; value?: string }>;
  values: string[];
  onChange: (values: string[]) => void;
  placeholder?: string;
  searchable?: boolean;
  maxHeight?: string;
}

function MultiSelect({
  options,
  values,
  onChange,
  placeholder = '选择选项...',
  searchable = true,
  maxHeight = 'max-h-48',
}: MultiSelectProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [showOptions, setShowOptions] = useState(false);

  const filteredOptions = useMemo(() => {
    if (!searchTerm) return options;
    return options.filter(option =>
      option.name.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }, [options, searchTerm]);

  const handleToggle = (value: string) => {
    const newValues = values.includes(value)
      ? values.filter(v => v !== value)
      : [...values, value];
    onChange(newValues);
  };

  const selectedLabels = values
    .map(value => options.find(opt => (opt.value || opt.name) === value)?.name)
    .filter(Boolean);

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setShowOptions(!showOptions)}
        className="w-full text-left px-3 py-2 border border-gray-300 rounded-md bg-white text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
      >
        {selectedLabels.length > 0 ? (
          <div className="flex flex-wrap gap-1">
            {selectedLabels.slice(0, 3).map((label) => (
              <span
                key={label}
                className="inline-block bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded-full"
              >
                {label}
              </span>
            ))}
            {selectedLabels.length > 3 && (
              <span className="inline-block bg-gray-100 text-gray-600 text-xs px-2 py-1 rounded-full">
                +{selectedLabels.length - 3} 更多
              </span>
            )}
          </div>
        ) : (
          <span className="text-gray-500">{placeholder}</span>
        )}
      </button>

      {showOptions && (
        <div className="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg">
          {searchable && (
            <div className="p-2 border-b border-gray-200">
              <div className="relative">
                <Search className="absolute left-2 top-2 h-4 w-4 text-gray-400" />
                <input
                  type="text"
                  placeholder="搜索..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="w-full pl-8 pr-3 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                />
              </div>
            </div>
          )}

          <div className={`overflow-y-auto ${maxHeight}`}>
            {filteredOptions.length > 0 ? (
              filteredOptions.map((option) => {
                const value = option.value || option.name;
                const isSelected = values.includes(value);
                return (
                  <label
                    key={value}
                    className="flex items-center px-3 py-2 hover:bg-gray-50 cursor-pointer"
                  >
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => handleToggle(value)}
                      className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <span className="ml-3 text-sm text-gray-900 flex-1">
                      {option.name}
                    </span>
                    <span className="text-xs text-gray-500 ml-2">
                      {option.count}
                    </span>
                  </label>
                );
              })
            ) : (
              <div className="px-3 py-2 text-sm text-gray-500">
                没有找到匹配的选项
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default function SearchFilters({
  filters,
  filterOptions,
  onChange,
  className = '',
}: SearchFiltersProps) {
  // 内容类型选项
  const contentTypeOptions: Array<{ value: ContentType; label: string }> = [
    { value: 'post', label: '文章' },
    { value: 'page', label: '页面' },
    { value: 'draft', label: '草稿' },
  ];

  // 状态选项
  const statusOptions: Array<{ value: PostStatus; label: string }> = [
    { value: 'published', label: '已发布' },
    { value: 'draft', label: '草稿' },
    { value: 'archived', label: '已归档' },
  ];

  // 处理日期范围变化
  const handleDateChange = (field: 'dateFrom' | 'dateTo', value: string) => {
    onChange(field, value || undefined);
  };

  // 处理多选变化
  const handleMultiSelectChange = (field: keyof SearchFilters, values: string[]) => {
    onChange(field, values.length > 0 ? values : undefined);
  };

  return (
    <div className={`space-y-4 ${className}`}>
      {/* 分类筛选 */}
      <FilterSection
        title="分类"
        icon={Folder}
        count={filters.categories?.length}
        defaultExpanded={!!filters.categories?.length}
      >
        <MultiSelect
          options={filterOptions.categories.map(cat => ({
            name: cat.name,
            count: cat.count,
          }))}
          values={filters.categories || []}
          onChange={(values) => handleMultiSelectChange('categories', values)}
          placeholder="选择分类..."
        />
      </FilterSection>

      {/* 标签筛选 */}
      <FilterSection
        title="标签"
        icon={Hash}
        count={filters.tags?.length}
        defaultExpanded={!!filters.tags?.length}
      >
        <MultiSelect
          options={filterOptions.tags.map(tag => ({
            name: tag.name,
            count: tag.count,
          }))}
          values={filters.tags || []}
          onChange={(values) => handleMultiSelectChange('tags', values)}
          placeholder="选择标签..."
        />
      </FilterSection>

      {/* 作者筛选 */}
      <FilterSection
        title="作者"
        icon={User}
        count={filters.authors?.length}
        defaultExpanded={!!filters.authors?.length}
      >
        <MultiSelect
          options={filterOptions.authors.map(author => ({
            name: author.name,
            count: author.count,
          }))}
          values={filters.authors || []}
          onChange={(values) => handleMultiSelectChange('authors', values)}
          placeholder="选择作者..."
        />
      </FilterSection>

      {/* 时间范围筛选 */}
      <FilterSection
        title="时间范围"
        icon={Calendar}
        count={filters.dateFrom || filters.dateTo ? 1 : 0}
        defaultExpanded={!!(filters.dateFrom || filters.dateTo)}
      >
        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              开始日期
            </label>
            <input
              type="date"
              value={filters.dateFrom || ''}
              onChange={(e) => handleDateChange('dateFrom', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              结束日期
            </label>
            <input
              type="date"
              value={filters.dateTo || ''}
              onChange={(e) => handleDateChange('dateTo', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* 快捷时间选项 */}
          <div className="grid grid-cols-2 gap-2 pt-2 border-t border-gray-200">
            <button
              type="button"
              onClick={() => {
                const today = new Date();
                const lastWeek = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);
                handleDateChange('dateFrom', lastWeek.toISOString().split('T')[0]);
                handleDateChange('dateTo', today.toISOString().split('T')[0]);
              }}
              className="px-3 py-2 text-xs bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
            >
              最近一周
            </button>
            <button
              type="button"
              onClick={() => {
                const today = new Date();
                const lastMonth = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
                handleDateChange('dateFrom', lastMonth.toISOString().split('T')[0]);
                handleDateChange('dateTo', today.toISOString().split('T')[0]);
              }}
              className="px-3 py-2 text-xs bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
            >
              最近一月
            </button>
            <button
              type="button"
              onClick={() => {
                const today = new Date();
                const lastYear = new Date(today.getTime() - 365 * 24 * 60 * 60 * 1000);
                handleDateChange('dateFrom', lastYear.toISOString().split('T')[0]);
                handleDateChange('dateTo', today.toISOString().split('T')[0]);
              }}
              className="px-3 py-2 text-xs bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
            >
              最近一年
            </button>
            <button
              type="button"
              onClick={() => {
                handleDateChange('dateFrom', '');
                handleDateChange('dateTo', '');
              }}
              className="px-3 py-2 text-xs bg-red-100 hover:bg-red-200 text-red-700 rounded-md transition-colors"
            >
              清除
            </button>
          </div>
        </div>
      </FilterSection>

      {/* 内容类型筛选 */}
      <FilterSection
        title="内容类型"
        icon={FileText}
        count={filters.contentType?.length}
        defaultExpanded={!!filters.contentType?.length}
      >
        <div className="space-y-2">
          {contentTypeOptions.map((option) => (
            <label key={option.value} className="flex items-center">
              <input
                type="checkbox"
                checked={filters.contentType?.includes(option.value) || false}
                onChange={(e) => {
                  const currentTypes = filters.contentType || [];
                  const newTypes = e.target.checked
                    ? [...currentTypes, option.value]
                    : currentTypes.filter(type => type !== option.value);
                  handleMultiSelectChange('contentType', newTypes);
                }}
                className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="ml-3 text-sm text-gray-900">{option.label}</span>
            </label>
          ))}
        </div>
      </FilterSection>

      {/* 发布状态筛选 */}
      <FilterSection
        title="发布状态"
        icon={CheckCircle}
        count={filters.status?.length}
        defaultExpanded={!!filters.status?.length}
      >
        <div className="space-y-2">
          {statusOptions.map((option) => (
            <label key={option.value} className="flex items-center">
              <input
                type="checkbox"
                checked={filters.status?.includes(option.value) || false}
                onChange={(e) => {
                  const currentStatuses = filters.status || [];
                  const newStatuses = e.target.checked
                    ? [...currentStatuses, option.value]
                    : currentStatuses.filter(status => status !== option.value);
                  handleMultiSelectChange('status', newStatuses);
                }}
                className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
              />
              <span className="ml-3 text-sm text-gray-900">{option.label}</span>
            </label>
          ))}
        </div>
      </FilterSection>
    </div>
  );
}