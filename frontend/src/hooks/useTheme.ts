// @ts-nocheck
/**
 * useTheme - 主题管理Hook
 * 提供主题切换、持久化和系统主题检测功能
 */

import { useState, useEffect, useCallback } from 'react';

export type Theme = 'light' | 'dark' | 'system';

interface UseThemeReturn {
  theme: Theme;
  actualTheme: 'light' | 'dark';
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
  isSystemTheme: boolean;
}

const THEME_STORAGE_KEY = 'maple-blog-theme';

// 获取系统主题偏好
const getSystemTheme = (): 'light' | 'dark' => {
  if (typeof window === 'undefined') return 'light';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
};

// 从localStorage获取保存的主题
const getStoredTheme = (): Theme => {
  if (typeof window === 'undefined') return 'system';

  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    if (stored && ['light', 'dark', 'system'].includes(stored)) {
      return stored as Theme;
    }
  } catch (error) {
    console.warn('Failed to read theme from localStorage:', error);
  }

  return 'system';
};

// 保存主题到localStorage
const storeTheme = (theme: Theme): void => {
  if (typeof window === 'undefined') return;

  try {
    localStorage.setItem(THEME_STORAGE_KEY, theme);
  } catch (error) {
    console.warn('Failed to save theme to localStorage:', error);
  }
};

// 应用主题到DOM
const applyTheme = (actualTheme: 'light' | 'dark'): void => {
  if (typeof window === 'undefined') return;

  const root = document.documentElement;

  if (actualTheme === 'dark') {
    root.classList.add('dark');
    root.setAttribute('data-theme', 'dark');
  } else {
    root.classList.remove('dark');
    root.setAttribute('data-theme', 'light');
  }

  // 更新meta标签中的主题色
  const metaThemeColor = document.querySelector('meta[name="theme-color"]');
  if (metaThemeColor) {
    metaThemeColor.setAttribute(
      'content',
      actualTheme === 'dark' ? '#0f172a' : '#ffffff'
    );
  }
};

export const useTheme = (): UseThemeReturn => {
  const [theme, setThemeState] = useState<Theme>(() => getStoredTheme());
  const [systemTheme, setSystemTheme] = useState<'light' | 'dark'>(() => getSystemTheme());

  // 计算实际应用的主题
  const actualTheme = theme === 'system' ? systemTheme : theme;

  // 监听系统主题变化
  useEffect(() => {
    if (typeof window === 'undefined') return;

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    const handleChange = (e: MediaQueryListEvent) => {
      setSystemTheme(e.matches ? 'dark' : 'light');
    };

    // 现代浏览器使用addEventListener
    if (mediaQuery.addEventListener) {
      mediaQuery.addEventListener('change', handleChange);
      return () => mediaQuery.removeEventListener('change', handleChange);
    }
    // 兼容旧浏览器
    else if (mediaQuery.addListener) {
      mediaQuery.addListener(handleChange);
      return () => mediaQuery.removeListener(handleChange);
    }
  }, []);

  // 应用主题变化
  useEffect(() => {
    applyTheme(actualTheme);
  }, [actualTheme]);

  // 设置主题
  const setTheme = useCallback((newTheme: Theme) => {
    setThemeState(newTheme);
    storeTheme(newTheme);

    // 发送自定义事件，其他组件可以监听
    if (typeof window !== 'undefined') {
      window.dispatchEvent(new CustomEvent('theme-change', {
        detail: { theme: newTheme, actualTheme: newTheme === 'system' ? systemTheme : newTheme }
      }));
    }
  }, [systemTheme]);

  // 切换主题（在light和dark之间切换）
  const toggleTheme = useCallback(() => {
    if (theme === 'system') {
      // 如果当前是system，切换到与系统相反的主题
      setTheme(systemTheme === 'dark' ? 'light' : 'dark');
    } else {
      // 在light和dark之间切换
      setTheme(theme === 'dark' ? 'light' : 'dark');
    }
  }, [theme, systemTheme, setTheme]);

  return {
    theme,
    actualTheme,
    setTheme,
    toggleTheme,
    isSystemTheme: theme === 'system',
  };
};

// 主题上下文Hook（可选）
export const useThemeEffect = (callback: (actualTheme: 'light' | 'dark') => void) => {
  const { actualTheme } = useTheme();

  useEffect(() => {
    callback(actualTheme);
  }, [actualTheme, callback]);
};

// 检测是否为暗色主题
export const useIsDark = (): boolean => {
  const { actualTheme } = useTheme();
  return actualTheme === 'dark';
};

export default useTheme;