import { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { message } from 'antd';
import { debounce, throttle, storageUtils } from '@/utils';
import type { QueryParams, PaginatedResponse, ApiError } from '@/types';

// 本地存储Hook
export const useLocalStorage = <T>(key: string, defaultValue: T) => {
  const [value, setValue] = useState<T>(() => {
    const stored = storageUtils.get(key);
    return stored !== null ? stored : defaultValue;
  });

  const setStoredValue = useCallback((newValue: T | ((prev: T) => T)) => {
    setValue(prevValue => {
      const valueToStore = typeof newValue === 'function'
        ? (newValue as (prev: T) => T)(prevValue)
        : newValue;

      storageUtils.set(key, valueToStore);
      return valueToStore;
    });
  }, [key]);

  const removeValue = useCallback(() => {
    storageUtils.remove(key);
    setValue(defaultValue);
  }, [key, defaultValue]);

  return [value, setStoredValue, removeValue] as const;
};

// 防抖Hook
export const useDebounce = <T>(value: T, delay: number): T => {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
};

// 防抖回调Hook
export const useDebouncedCallback = <T extends (...args: unknown[]) => unknown>(
  callback: T,
  delay: number
): T => {
  const callbackRef = useRef(callback);
  callbackRef.current = callback;

  return useMemo(
    () => debounce((...args: unknown[]) => callbackRef.current(...args), delay) as T,
    [delay]
  );
};

// 节流Hook
export const useThrottle = <T>(value: T, delay: number): T => {
  const [throttledValue, setThrottledValue] = useState<T>(value);
  const lastUpdated = useRef<number>(0);

  useEffect(() => {
    const now = Date.now();
    if (now - lastUpdated.current >= delay) {
      setThrottledValue(value);
      lastUpdated.current = now;
    } else {
      const timer = setTimeout(() => {
        setThrottledValue(value);
        lastUpdated.current = Date.now();
      }, delay - (now - lastUpdated.current));

      return () => clearTimeout(timer);
    }
  }, [value, delay]);

  return throttledValue;
};

// 节流回调Hook
export const useThrottledCallback = <T extends (...args: unknown[]) => unknown>(
  callback: T,
  delay: number
): T => {
  const callbackRef = useRef(callback);
  callbackRef.current = callback;

  return useMemo(
    () => throttle((...args: unknown[]) => callbackRef.current(...args), delay) as T,
    [delay]
  );
};

// 分页Hook
export const usePagination = (
  initialPage: number = 1,
  initialPageSize: number = 10
) => {
  const [current, setCurrent] = useState(initialPage);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const reset = useCallback(() => {
    setCurrent(initialPage);
    setPageSize(initialPageSize);
  }, [initialPage, initialPageSize]);

  const onChange = useCallback((page: number, size?: number) => {
    setCurrent(page);
    if (size && size !== pageSize) {
      setPageSize(size);
      setCurrent(1); // 重置到第一页
    }
  }, [pageSize]);

  return {
    current,
    pageSize,
    onChange,
    reset,
    setCurrent,
    setPageSize,
  };
};

// 表格Hook
export const useTable = <T = Record<string, unknown>>(
  queryFn: (params: QueryParams) => Promise<PaginatedResponse<T>>,
  options?: {
    defaultPageSize?: number;
    defaultSorter?: { field: string; order: 'asc' | 'desc' };
    defaultFilters?: Record<string, unknown>;
  }
) => {
  const [filters, setFilters] = useState(options?.defaultFilters || {});
  const [sorter, setSorter] = useState(options?.defaultSorter);
  const [searchText, setSearchText] = useState('');

  const pagination = usePagination(1, options?.defaultPageSize || 10);
  const debouncedSearchText = useDebounce(searchText, 300);

  const queryParams = useMemo(() => ({
    page: pagination.current,
    pageSize: pagination.pageSize,
    search: debouncedSearchText,
    sortBy: sorter?.field,
    sortOrder: sorter?.order,
    ...filters,
  }), [pagination, debouncedSearchText, sorter, filters]);

  const {
    data,
    isLoading,
    error,
    refetch,
    isFetching,
  } = useQuery({
    queryKey: ['table', queryParams],
    queryFn: () => queryFn(queryParams),
    keepPreviousData: true,
  });

  const handleTableChange = useCallback((paginationConfig: Record<string, unknown>, filtersConfig: Record<string, unknown>, sorterConfig: Record<string, unknown>) => {
    // 处理分页
    if (paginationConfig) {
      pagination.onChange(paginationConfig.current, paginationConfig.pageSize);
    }

    // 处理筛选
    if (filtersConfig) {
      setFilters(filtersConfig);
    }

    // 处理排序
    if (sorterConfig) {
      if (sorterConfig.field && sorterConfig.order) {
        setSorter({
          field: sorterConfig.field,
          order: sorterConfig.order === 'ascend' ? 'asc' : 'desc',
        });
      } else {
        setSorter(undefined);
      }
    }
  }, [pagination]);

  const resetFilters = useCallback(() => {
    setFilters(options?.defaultFilters || {});
    setSorter(options?.defaultSorter);
    setSearchText('');
    pagination.reset();
  }, [options?.defaultFilters, options?.defaultSorter, pagination]);

  return {
    // 数据
    dataSource: data?.data || [],
    pagination: {
      current: pagination.current,
      pageSize: pagination.pageSize,
      total: data?.pagination.total || 0,
      showSizeChanger: true,
      showQuickJumper: true,
      showTotal: (total: number, range: [number, number]) =>
        `第 ${range[0]}-${range[1]} 条，共 ${total} 条`,
      onChange: pagination.onChange,
    },
    loading: isLoading || isFetching,
    error,

    // 搜索
    searchText,
    setSearchText,

    // 筛选和排序
    filters,
    setFilters,
    sorter,
    setSorter,

    // 事件处理
    handleTableChange,
    resetFilters,
    refresh: refetch,
  };
};

// 通用请求Hook
export const useRequest = <T = unknown, P = unknown>(
  requestFn: (params?: P) => Promise<T>,
  options?: {
    manual?: boolean;
    onSuccess?: (data: T) => void;
    onError?: (error: ApiError) => void;
    loadingMessage?: string;
    successMessage?: string;
  }
) => {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<ApiError | null>(null);

  const run = useCallback(async (params?: P) => {
    try {
      setLoading(true);
      setError(null);

      if (options?.loadingMessage) {
        message.loading(options.loadingMessage);
      }

      const result = await requestFn(params);
      setData(result);

      if (options?.successMessage) {
        message.success(options.successMessage);
      }

      options?.onSuccess?.(result);
      return result;
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError);
      options?.onError?.(apiError);
      throw apiError;
    } finally {
      setLoading(false);
      message.destroy();
    }
  }, [requestFn, options]);

  // 自动执行
  useEffect(() => {
    if (!options?.manual) {
      run();
    }
  }, [options?.manual, run]);

  return {
    data,
    loading,
    error,
    run,
  };
};

// 表单状态Hook
export const useFormState = <T extends Record<string, unknown>>(initialValues: T) => {
  const [values, setValues] = useState<T>(initialValues);
  const [errors, setErrors] = useState<Partial<Record<keyof T, string>>>({});
  const [touched, setTouched] = useState<Partial<Record<keyof T, boolean>>>({});

  const setValue = useCallback((name: keyof T, value: unknown) => {
    setValues(prev => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: undefined }));
    }
  }, [errors]);

  const setError = useCallback((name: keyof T, error: string) => {
    setErrors(prev => ({ ...prev, [name]: error }));
  }, []);

  const setFieldTouched = useCallback((name: keyof T, isTouched: boolean = true) => {
    setTouched(prev => ({ ...prev, [name]: isTouched }));
  }, []);

  const reset = useCallback((newValues?: Partial<T>) => {
    setValues(newValues ? { ...initialValues, ...newValues } : initialValues);
    setErrors({});
    setTouched({});
  }, [initialValues]);

  const isValid = useMemo(() => {
    return Object.keys(errors).length === 0;
  }, [errors]);

  const isDirty = useMemo(() => {
    return JSON.stringify(values) !== JSON.stringify(initialValues);
  }, [values, initialValues]);

  return {
    values,
    errors,
    touched,
    setValue,
    setError,
    setTouched: setFieldTouched,
    reset,
    isValid,
    isDirty,
  };
};

// 权限Hook
export const usePermissions = (permissions: string[]) => {
  const userPermissions = useState<string[]>([]); // 从全局状态获取

  return useMemo(() => {
    if (!permissions.length) return true;
    return permissions.some(permission => userPermissions.includes(permission));
  }, [permissions, userPermissions]);
};

// 响应式Hook
export const useResponsive = () => {
  const [windowSize, setWindowSize] = useState({
    width: window.innerWidth,
    height: window.innerHeight,
  });

  useEffect(() => {
    const handleResize = () => {
      setWindowSize({
        width: window.innerWidth,
        height: window.innerHeight,
      });
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  const isMobile = windowSize.width < 768;
  const isTablet = windowSize.width >= 768 && windowSize.width < 1024;
  const isDesktop = windowSize.width >= 1024;

  return {
    windowSize,
    isMobile,
    isTablet,
    isDesktop,
  };
};

// 复制到剪贴板Hook
export const useClipboard = () => {
  const [copied, setCopied] = useState(false);

  const copy = useCallback(async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      message.success('已复制到剪贴板');
      setTimeout(() => setCopied(false), 2000);
    } catch {
      message.error('复制失败');
    }
  }, []);

  return { copied, copy };
};

// 计时器Hook
export const useTimer = (initialTime: number, options?: {
  onEnd?: () => void;
  autoStart?: boolean;
}) => {
  const [time, setTime] = useState(initialTime);
  const [isRunning, setIsRunning] = useState(options?.autoStart || false);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const start = useCallback(() => {
    if (!isRunning) {
      setIsRunning(true);
    }
  }, [isRunning]);

  const pause = useCallback(() => {
    setIsRunning(false);
  }, []);

  const reset = useCallback(() => {
    setTime(initialTime);
    setIsRunning(false);
  }, [initialTime]);

  useEffect(() => {
    if (isRunning && time > 0) {
      intervalRef.current = setInterval(() => {
        setTime(prevTime => {
          if (prevTime <= 1) {
            setIsRunning(false);
            options?.onEnd?.();
            return 0;
          }
          return prevTime - 1;
        });
      }, 1000);
    } else {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [isRunning, time, options?.onEnd, options]);

  return {
    time,
    isRunning,
    start,
    pause,
    reset,
  };
};

// Export additional specialized hooks
export { useWebSocket } from './useWebSocket';
export type { WebSocketMessage, UseWebSocketOptions, UseWebSocketReturn } from './useWebSocket';

export { default as usePermissions, PermissionLevel } from './usePermissions';
export type { Permission, PermissionCheckOptions } from './usePermissions';

export { default as useDashboard } from './useDashboard';
export type {
  DashboardStats,
  SystemMetrics,
  RecentActivity,
  DashboardFilters,
} from './useDashboard';

export { default as useDataTable } from './useDataTable';
export type {
  DataTableOptions,
  FilterConfig,
  SortConfig,
  PaginationConfig,
  UseDataTableReturn,
} from './useDataTable';