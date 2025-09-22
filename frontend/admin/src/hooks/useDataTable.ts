// @ts-nocheck
import { useMemo, useCallback, useRef, useEffect, useState } from 'react';
import { message } from 'antd';
import { useDebouncedCallback } from 'use-debounce';
import type { DataTableColumn } from '../components/tables/DataTable';

// 排序配置
export interface SortConfig {
  field: string;
  order: 'asc' | 'desc';
}

// 过滤配置
export interface FilterConfig {
  [key: string]: any;
}

// 选择配置
export interface SelectionConfig {
  selectedRowKeys: React.Key[];
  onChange: (selectedRowKeys: React.Key[], selectedRows: any[]) => void;
}

// Hook 参数
export interface UseDataTableOptions<T = any> {
  data: T[];
  columns: DataTableColumn<T>[];
  searchValue?: string;
  filters?: FilterConfig;
  sortConfig?: SortConfig | null;
  rowSelection?: SelectionConfig;
  
  // 性能优化选项
  enableVirtualization?: boolean;
  virtualizationThreshold?: number;
  debounceMs?: number;
  
  // 缓存选项
  enableCache?: boolean;
  cacheKey?: string;
  
  // 导出选项
  exportFormat?: 'csv' | 'xlsx' | 'json';
  exportFileName?: string;
}

// Hook 返回值
export interface UseDataTableResult<T = any> {
  // 处理后的数据
  processedData: T[];
  originalData: T[];
  
  // 列配置
  visibleColumns: DataTableColumn<T>[];
  allColumns: DataTableColumn<T>[];
  
  // 选择状态
  selectedRowKeys: React.Key[];
  selectedRows: T[];
  
  // 状态标识
  isProcessing: boolean;
  isEmpty: boolean;
  
  // 统计信息
  totalCount: number;
  filteredCount: number;
  selectedCount: number;
  
  // 数据操作方法
  searchData: (value: string) => void;
  filterData: (filters: FilterConfig) => void;
  sortData: (field: string, order: 'asc' | 'desc' | null) => void;
  
  // 列操作方法
  toggleColumnVisibility: (columnKey: string) => void;
  resetColumns: () => void;
  reorderColumns: (fromIndex: number, toIndex: number) => void;
  
  // 选择操作方法
  selectRow: (rowKey: React.Key) => void;
  selectRows: (rowKeys: React.Key[]) => void;
  selectAll: () => void;
  clearSelection: () => void;
  toggleSelection: (rowKey: React.Key) => void;
  
  // 数据导出方法
  exportData: (data?: T[], columns?: DataTableColumn<T>[]) => void;
  exportSelected: () => void;
  exportFiltered: () => void;
  exportAll: () => void;
  
  // 实用工具方法
  getRowByKey: (key: React.Key) => T | undefined;
  getSelectedData: () => T[];
  refreshData: () => void;
  
  // 性能监控
  performance: {
    searchTime: number;
    filterTime: number;
    sortTime: number;
    renderTime: number;
  };
}

// 数据处理工具函数
const DataProcessingUtils = {
  // 搜索过滤
  searchFilter: <T>(data: T[], searchValue: string, columns: DataTableColumn<T>[]): T[] => {
    if (!searchValue.trim()) return data;
    
    const searchLower = searchValue.toLowerCase();
    const searchableColumns = columns.filter(col => col.searchable !== false);
    
    return data.filter(item => {
      return searchableColumns.some(col => {
        const value = item[col.dataIndex || col.key];
        if (value == null) return false;
        
        return String(value).toLowerCase().includes(searchLower);
      });
    });
  },

  // 高级过滤
  advancedFilter: <T>(data: T[], filters: FilterConfig): T[] => {
    if (!filters || Object.keys(filters).length === 0) return data;
    
    return data.filter(item => {
      return Object.entries(filters).every(([field, filterValue]) => {
        if (filterValue == null || filterValue === '') return true;
        
        const itemValue = item[field];
        
        // 数组类型过滤（多选）
        if (Array.isArray(filterValue)) {
          return filterValue.some(val => {
            if (Array.isArray(itemValue)) {
              return itemValue.includes(val);
            }
            return itemValue === val;
          });
        }
        
        // 对象类型过滤（范围、日期等）
        if (typeof filterValue === 'object') {
          const { min, max, start, end } = filterValue;
          
          // 数值范围过滤
          if (min != null || max != null) {
            const numValue = Number(itemValue);
            if (min != null && numValue < min) return false;
            if (max != null && numValue > max) return false;
            return true;
          }
          
          // 日期范围过滤
          if (start || end) {
            const dateValue = new Date(itemValue);
            if (start && dateValue < new Date(start)) return false;
            if (end && dateValue > new Date(end)) return false;
            return true;
          }
        }
        
        // 字符串匹配
        if (typeof filterValue === 'string') {
          return String(itemValue).toLowerCase().includes(filterValue.toLowerCase());
        }
        
        // 精确匹配
        return itemValue === filterValue;
      });
    });
  },

  // 排序
  sort: <T>(data: T[], sortConfig: SortConfig | null): T[] => {
    if (!sortConfig) return data;
    
    const { field, order } = sortConfig;
    
    return [...data].sort((a, b) => {
      const aVal = a[field];
      const bVal = b[field];
      
      // 处理 null/undefined 值
      if (aVal == null && bVal == null) return 0;
      if (aVal == null) return order === 'asc' ? -1 : 1;
      if (bVal == null) return order === 'asc' ? 1 : -1;
      
      // 数值比较
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return order === 'asc' ? aVal - bVal : bVal - aVal;
      }
      
      // 日期比较
      if (aVal instanceof Date && bVal instanceof Date) {
        return order === 'asc' 
          ? aVal.getTime() - bVal.getTime()
          : bVal.getTime() - aVal.getTime();
      }
      
      // 字符串比较
      const aStr = String(aVal).toLowerCase();
      const bStr = String(bVal).toLowerCase();
      
      if (aStr < bStr) return order === 'asc' ? -1 : 1;
      if (aStr > bStr) return order === 'asc' ? 1 : -1;
      return 0;
    });
  },
};

// 导出工具函数
const ExportUtils = {
  // 导出为 CSV
  exportToCsv: <T>(data: T[], columns: DataTableColumn<T>[], filename: string) => {
    const exportColumns = columns.filter(col => col.exportable !== false);
    
    // 创建 CSV 头部
    const headers = exportColumns.map(col => col.title).join(',');
    
    // 创建 CSV 行
    const rows = data.map(item => {
      return exportColumns.map(col => {
        const value = item[col.dataIndex || col.key];
        // 处理包含逗号或换行的值
        if (typeof value === 'string' && (value.includes(',') || value.includes('\n'))) {
          return `"${value.replace(/"/g, '""')}"`;
        }
        return value ?? '';
      }).join(',');
    });
    
    const csvContent = [headers, ...rows].join('\n');
    
    // 下载文件
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', `${filename}.csv`);
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  },

  // 导出为 JSON
  exportToJson: <T>(data: T[], columns: DataTableColumn<T>[], filename: string) => {
    const exportColumns = columns.filter(col => col.exportable !== false);
    const exportData = data.map(item => {
      const exportItem: any = {};
      exportColumns.forEach(col => {
        exportItem[col.dataIndex || col.key] = item[col.dataIndex || col.key];
      });
      return exportItem;
    });
    
    const jsonContent = JSON.stringify(exportData, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    
    link.setAttribute('href', url);
    link.setAttribute('download', `${filename}.json`);
    link.style.visibility = 'hidden';
    
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  },
};

// 性能监控工具
class PerformanceMonitor {
  private times: { [key: string]: number } = {};
  
  start(operation: string) {
    this.times[operation] = performance.now();
  }
  
  end(operation: string): number {
    const startTime = this.times[operation];
    if (!startTime) return 0;
    
    const duration = performance.now() - startTime;
    delete this.times[operation];
    return Math.round(duration * 100) / 100; // 保留两位小数
  }
}

export const useDataTable = <T extends Record<string, any>>(
  options: UseDataTableOptions<T>
): UseDataTableResult<T> => {
  const {
    data = [],
    columns = [],
    searchValue = '',
    filters = {},
    sortConfig = null,
    rowSelection,
    enableVirtualization = true,
    virtualizationThreshold = 1000,
    debounceMs = 300,
    enableCache = true,
    cacheKey = 'default',
    exportFormat = 'csv',
    exportFileName = 'table-data',
  } = options;

  // 状态管理
  const [internalColumns, setInternalColumns] = useState<DataTableColumn<T>[]>(columns);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);
  const [performance, setPerformance] = useState({
    searchTime: 0,
    filterTime: 0,
    sortTime: 0,
    renderTime: 0,
  });

  // 性能监控器
  const performanceMonitor = useRef(new PerformanceMonitor());
  
  // 缓存引用
  const cache = useRef(new Map());
  const lastCacheKey = useRef('');

  // 防抖处理
  const debouncedProcess = useDebouncedCallback(
    (searchVal: string, filterVal: FilterConfig, sortVal: SortConfig | null) => {
      processData(searchVal, filterVal, sortVal);
    },
    debounceMs
  );

  // 数据处理核心函数
  const processData = useCallback((
    searchVal: string,
    filterVal: FilterConfig,
    sortVal: SortConfig | null
  ) => {
    const monitor = performanceMonitor.current;
    setIsProcessing(true);

    try {
      // 生成缓存键
      const currentCacheKey = `${cacheKey}-${JSON.stringify({ searchVal, filterVal, sortVal })}`;
      
      // 检查缓存
      if (enableCache && cache.current.has(currentCacheKey)) {
        const cachedResult = cache.current.get(currentCacheKey);
        setIsProcessing(false);
        return cachedResult;
      }

      let result = [...data];

      // 搜索过滤
      monitor.start('search');
      if (searchVal) {
        result = DataProcessingUtils.searchFilter(result, searchVal, internalColumns);
      }
      const searchTime = monitor.end('search');

      // 高级过滤
      monitor.start('filter');
      if (filterVal && Object.keys(filterVal).length > 0) {
        result = DataProcessingUtils.advancedFilter(result, filterVal);
      }
      const filterTime = monitor.end('filter');

      // 排序
      monitor.start('sort');
      if (sortVal) {
        result = DataProcessingUtils.sort(result, sortVal);
      }
      const sortTime = monitor.end('sort');

      // 更新性能统计
      setPerformance(prev => ({
        ...prev,
        searchTime,
        filterTime,
        sortTime,
      }));

      // 缓存结果
      if (enableCache) {
        cache.current.set(currentCacheKey, result);
        // 限制缓存大小
        if (cache.current.size > 50) {
          const firstKey = cache.current.keys().next().value;
          cache.current.delete(firstKey);
        }
      }

      return result;
    } finally {
      setIsProcessing(false);
    }
  }, [data, internalColumns, cacheKey, enableCache]);

  // 处理后的数据
  const processedData = useMemo(() => {
    return processData(searchValue, filters, sortConfig);
  }, [data, searchValue, filters, sortConfig, processData]);

  // 可见列
  const visibleColumns = useMemo(() => {
    return internalColumns.filter(col => !col.hidden);
  }, [internalColumns]);

  // 选中的行数据
  const selectedRows = useMemo(() => {
    return processedData.filter(item => {
      const key = typeof options.rowSelection?.onChange === 'function' 
        ? item.id || item.key
        : item.id || item.key;
      return selectedRowKeys.includes(key);
    });
  }, [processedData, selectedRowKeys]);

  // 数据操作方法
  const searchData = useCallback((value: string) => {
    debouncedProcess(value, filters, sortConfig);
  }, [filters, sortConfig, debouncedProcess]);

  const filterData = useCallback((newFilters: FilterConfig) => {
    debouncedProcess(searchValue, newFilters, sortConfig);
  }, [searchValue, sortConfig, debouncedProcess]);

  const sortData = useCallback((field: string, order: 'asc' | 'desc' | null) => {
    const newSortConfig = order ? { field, order } : null;
    debouncedProcess(searchValue, filters, newSortConfig);
  }, [searchValue, filters, debouncedProcess]);

  // 列操作方法
  const toggleColumnVisibility = useCallback((columnKey: string) => {
    setInternalColumns(prev => 
      prev.map(col => 
        col.key === columnKey ? { ...col, hidden: !col.hidden } : col
      )
    );
  }, []);

  const resetColumns = useCallback(() => {
    setInternalColumns(columns);
  }, [columns]);

  const reorderColumns = useCallback((fromIndex: number, toIndex: number) => {
    setInternalColumns(prev => {
      const newColumns = [...prev];
      const [movedColumn] = newColumns.splice(fromIndex, 1);
      newColumns.splice(toIndex, 0, movedColumn);
      return newColumns;
    });
  }, []);

  // 选择操作方法
  const selectRow = useCallback((rowKey: React.Key) => {
    const newKeys = [...selectedRowKeys, rowKey];
    setSelectedRowKeys(newKeys);
    rowSelection?.onChange(newKeys, selectedRows);
  }, [selectedRowKeys, selectedRows, rowSelection]);

  const selectRows = useCallback((rowKeys: React.Key[]) => {
    setSelectedRowKeys(rowKeys);
    rowSelection?.onChange(rowKeys, selectedRows);
  }, [selectedRows, rowSelection]);

  const selectAll = useCallback(() => {
    const allKeys = processedData.map(item => item.id || item.key);
    setSelectedRowKeys(allKeys);
    rowSelection?.onChange(allKeys, processedData);
  }, [processedData, rowSelection]);

  const clearSelection = useCallback(() => {
    setSelectedRowKeys([]);
    rowSelection?.onChange([], []);
  }, [rowSelection]);

  const toggleSelection = useCallback((rowKey: React.Key) => {
    const newKeys = selectedRowKeys.includes(rowKey)
      ? selectedRowKeys.filter(key => key !== rowKey)
      : [...selectedRowKeys, rowKey];
    setSelectedRowKeys(newKeys);
    rowSelection?.onChange(newKeys, selectedRows);
  }, [selectedRowKeys, selectedRows, rowSelection]);

  // 导出方法
  const exportData = useCallback((
    exportData: T[] = processedData,
    exportColumns: DataTableColumn<T>[] = visibleColumns
  ) => {
    try {
      if (exportFormat === 'csv') {
        ExportUtils.exportToCsv(exportData, exportColumns, exportFileName);
      } else if (exportFormat === 'json') {
        ExportUtils.exportToJson(exportData, exportColumns, exportFileName);
      }
      message.success(`Successfully exported ${exportData.length} records`);
    } catch (error) {
      console.error('Export error:', error);
      message.error('Failed to export data');
    }
  }, [processedData, visibleColumns, exportFormat, exportFileName]);

  const exportSelected = useCallback(() => {
    exportData(selectedRows);
  }, [exportData, selectedRows]);

  const exportFiltered = useCallback(() => {
    exportData(processedData);
  }, [exportData, processedData]);

  const exportAll = useCallback(() => {
    exportData(data);
  }, [exportData, data]);

  // 实用工具方法
  const getRowByKey = useCallback((key: React.Key) => {
    return processedData.find(item => (item.id || item.key) === key);
  }, [processedData]);

  const getSelectedData = useCallback(() => {
    return selectedRows;
  }, [selectedRows]);

  const refreshData = useCallback(() => {
    cache.current.clear();
    processData(searchValue, filters, sortConfig);
  }, [searchValue, filters, sortConfig, processData]);

  // 同步外部选择状态
  useEffect(() => {
    if (rowSelection?.selectedRowKeys) {
      setSelectedRowKeys(rowSelection.selectedRowKeys);
    }
  }, [rowSelection?.selectedRowKeys]);

  // 同步外部列配置
  useEffect(() => {
    setInternalColumns(columns);
  }, [columns]);

  return {
    // 处理后的数据
    processedData,
    originalData: data,
    
    // 列配置
    visibleColumns,
    allColumns: internalColumns,
    
    // 选择状态
    selectedRowKeys,
    selectedRows,
    
    // 状态标识
    isProcessing,
    isEmpty: data.length === 0,
    
    // 统计信息
    totalCount: data.length,
    filteredCount: processedData.length,
    selectedCount: selectedRowKeys.length,
    
    // 数据操作方法
    searchData,
    filterData,
    sortData,
    
    // 列操作方法
    toggleColumnVisibility,
    resetColumns,
    reorderColumns,
    
    // 选择操作方法
    selectRow,
    selectRows,
    selectAll,
    clearSelection,
    toggleSelection,
    
    // 数据导出方法
    exportData,
    exportSelected,
    exportFiltered,
    exportAll,
    
    // 实用工具方法
    getRowByKey,
    getSelectedData,
    refreshData,
    
    // 性能监控
    performance,
  };
};

export default useDataTable;