import dayjs from 'dayjs';
import 'dayjs/locale/zh-cn';
import relativeTime from 'dayjs/plugin/relativeTime';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';

// 配置 dayjs
dayjs.locale('zh-cn');
dayjs.extend(relativeTime);
dayjs.extend(utc);
dayjs.extend(timezone);

interface StorageItem {
  value: unknown;
  expiry: number | null;
}

interface PaginationResult<T> {
  data: T[];
  pagination: {
    current: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
}

// 格式化工具
export const formatUtils = {
  // 格式化日期
  formatDate: (date: string | Date, format = 'YYYY-MM-DD HH:mm:ss') => {
    return dayjs(date).format(format);
  },

  // 相对时间
  formatRelative: (date: string | Date) => {
    return dayjs(date).fromNow();
  },

  // 格式化文件大小
  formatFileSize: (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  },

  // 格式化数字
  formatNumber: (num: number, separator = ',') => {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, separator);
  },

  // 格式化百分比
  formatPercent: (num: number, decimals = 2) => {
    return (num * 100).toFixed(decimals) + '%';
  },

  // 格式化货币
  formatCurrency: (amount: number, currency = '¥') => {
    return currency + formatUtils.formatNumber(amount);
  },
};

// 验证工具
export const validationUtils = {
  // 邮箱验证
  isEmail: (email: string) => {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
  },

  // 手机号验证
  isPhone: (phone: string) => {
    const regex = /^1[3-9]\d{9}$/;
    return regex.test(phone);
  },

  // 密码强度验证
  isStrongPassword: (password: string) => {
    // 至少8位，包含大小写字母、数字和特殊字符
    const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
    return regex.test(password);
  },

  // URL验证
  isUrl: (url: string) => {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  },
};

// 字符串工具
export const stringUtils = {
  // 生成随机字符串
  random: (length = 8) => {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let result = '';
    for (let i = 0; i < length; i++) {
      result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
  },

  // 截断字符串
  truncate: (str: string, length = 50, suffix = '...') => {
    if (str.length <= length) return str;
    return str.substring(0, length) + suffix;
  },

  // 转换为驼峰命名
  toCamelCase: (str: string) => {
    return str.replace(/-(.)/g, (_, char) => char.toUpperCase());
  },

  // 转换为短横线命名
  toKebabCase: (str: string) => {
    return str.replace(/([A-Z])/g, '-$1').toLowerCase().replace(/^-/, '');
  },

  // 移除HTML标签
  stripHtml: (html: string) => {
    const doc = new DOMParser().parseFromString(html, 'text/html');
    return doc.body.textContent || '';
  },
};

// 数组工具
export const arrayUtils = {
  // 数组去重
  unique: <T>(arr: T[]) => {
    return Array.from(new Set(arr));
  },

  // 数组分组
  groupBy: <T>(arr: T[], key: keyof T) => {
    return arr.reduce((groups, item) => {
      const group = String(item[key]);
      groups[group] = groups[group] || [];
      groups[group].push(item);
      return groups;
    }, {} as Record<string, T[]>);
  },

  // 数组排序
  sortBy: <T>(arr: T[], key: keyof T, order: 'asc' | 'desc' = 'asc') => {
    return [...arr].sort((a, b) => {
      const aVal = a[key];
      const bVal = b[key];
      if (aVal < bVal) return order === 'asc' ? -1 : 1;
      if (aVal > bVal) return order === 'asc' ? 1 : -1;
      return 0;
    });
  },

  // 数组分页
  paginate: <T>(arr: T[], page: number, pageSize: number): PaginationResult<T> => {
    const start = (page - 1) * pageSize;
    const end = start + pageSize;
    return {
      data: arr.slice(start, end),
      pagination: {
        current: page,
        pageSize,
        total: arr.length,
        totalPages: Math.ceil(arr.length / pageSize),
      },
    };
  },
};

// 对象工具
export const objectUtils = {
  // 深度合并对象
  deepMerge: (target: Record<string, unknown>, source: Record<string, unknown>): Record<string, unknown> => {
    const result = { ...target };
    for (const key in source) {
      if (source[key] && typeof source[key] === 'object' && !Array.isArray(source[key])) {
        result[key] = objectUtils.deepMerge(
          (result[key] as Record<string, unknown>) || {}, 
          source[key] as Record<string, unknown>
        );
      } else {
        result[key] = source[key];
      }
    }
    return result;
  },

  // 深拷贝
  deepClone: <T>(obj: T): T => {
    return JSON.parse(JSON.stringify(obj));
  },

  // 移除空值
  removeEmpty: (obj: Record<string, unknown>) => {
    const result: Record<string, unknown> = {};
    for (const key in obj) {
      const value = obj[key];
      if (value !== null && value !== undefined && value !== '') {
        result[key] = value;
      }
    }
    return result;
  },

  // 获取嵌套属性
  get: (obj: Record<string, unknown>, path: string, defaultValue?: unknown) => {
    const keys = path.split('.');
    let result: unknown = obj;
    for (const key of keys) {
      if (result == null) return defaultValue;
      result = (result as Record<string, unknown>)[key];
    }
    return result ?? defaultValue;
  },
};

// 颜色工具
export const colorUtils = {
  // 生成随机颜色
  random: () => {
    return '#' + Math.floor(Math.random() * 16777215).toString(16);
  },

  // 颜色透明度
  opacity: (color: string, alpha: number) => {
    const hex = color.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  },

  // 预定义颜色
  primary: '#1890ff',
  success: '#52c41a',
  warning: '#faad14',
  error: '#ff4d4f',
  info: '#1890ff',
  text: '#333333',
  textSecondary: '#666666',
  border: '#d9d9d9',
  background: '#f0f2f5',
};

// 本地存储工具
export const storageUtils = {
  // 设置本地存储
  set: (key: string, value: unknown, expiry?: number) => {
    const item: StorageItem = {
      value,
      expiry: expiry ? Date.now() + expiry : null,
    };
    localStorage.setItem(key, JSON.stringify(item));
  },

  // 获取本地存储
  get: (key: string) => {
    try {
      const item = localStorage.getItem(key);
      if (!item) return null;

      const parsed = JSON.parse(item) as StorageItem;
      if (parsed.expiry && Date.now() > parsed.expiry) {
        localStorage.removeItem(key);
        return null;
      }

      return parsed.value;
    } catch {
      return null;
    }
  },

  // 移除本地存储
  remove: (key: string) => {
    localStorage.removeItem(key);
  },

  // 清空本地存储
  clear: () => {
    localStorage.clear();
  },
};

// 权限工具
export const permissionUtils = {
  // 检查权限
  hasPermission: (userPermissions: string[], requiredPermissions: string[]) => {
    if (!requiredPermissions.length) return true;
    return requiredPermissions.some(permission => userPermissions.includes(permission));
  },

  // 检查所有权限
  hasAllPermissions: (userPermissions: string[], requiredPermissions: string[]) => {
    if (!requiredPermissions.length) return true;
    return requiredPermissions.every(permission => userPermissions.includes(permission));
  },

  // 检查角色
  hasRole: (userRoles: string[], requiredRoles: string[]) => {
    if (!requiredRoles.length) return true;
    return requiredRoles.some(role => userRoles.includes(role));
  },
};

// 防抖和节流
export const debounce = <T extends (...args: unknown[]) => unknown>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let timeout: NodeJS.Timeout;
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
};

export const throttle = <T extends (...args: unknown[]) => unknown>(
  func: T,
  wait: number
): ((...args: Parameters<T>) => void) => {
  let lastTime = 0;
  return (...args: Parameters<T>) => {
    const now = Date.now();
    if (now - lastTime >= wait) {
      lastTime = now;
      func(...args);
    }
  };
};

// 错误处理
export const errorUtils = {
  // 格式化错误消息
  format: (error: unknown): string => {
    if (typeof error === 'string') return error;
    if (error && typeof error === 'object') {
      const errorObj = error as Record<string, unknown>;
      if (errorObj.message && typeof errorObj.message === 'string') return errorObj.message;
      if (errorObj.response && typeof errorObj.response === 'object') {
        const response = errorObj.response as Record<string, unknown>;
        if (response.data && typeof response.data === 'object') {
          const data = response.data as Record<string, unknown>;
          if (data.message && typeof data.message === 'string') return data.message;
        }
      }
    }
    return '未知错误';
  },

  // 错误日志
  log: (error: unknown, context?: string) => {
    console.error(`[${context || 'Error'}]:`, error);
  },
};

// 环境变量
export const env = {
  isDev: import.meta.env.DEV,
  isProd: import.meta.env.PROD,
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  apiPrefix: import.meta.env.VITE_API_PREFIX || '/api',
  appTitle: import.meta.env.VITE_APP_TITLE || 'Maple Blog 管理后台',
  appVersion: import.meta.env.VITE_APP_VERSION || '1.0.0',
};