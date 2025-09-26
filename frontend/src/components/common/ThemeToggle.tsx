/* eslint-disable react-refresh/only-export-components */
/**
 * ThemeToggle component - Theme switching with system preference detection
 * Features: Light/dark/auto modes, smooth transitions, accessibility support
 */

import React, { useEffect } from 'react';
import { Sun, Moon, Monitor } from 'lucide-react';
import { Button } from '../ui/Button';
import { useHomeStore, useCurrentTheme } from '../../stores/homeStore';
import { cn } from '../../utils/cn';

interface ThemeToggleProps {
  className?: string;
  size?: 'sm' | 'md' | 'lg';
  showLabel?: boolean;
  variant?: 'button' | 'dropdown';
}

export const ThemeToggle: React.FC<ThemeToggleProps> = ({
  className,
  size = 'md',
  showLabel = false,
  variant = 'button',
}) => {
  const currentTheme = useCurrentTheme();
  const { toggleTheme, setTheme } = useHomeStore();

  // Apply theme to document
  useEffect(() => {
    const root = document.documentElement;
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';

    let appliedTheme: string;
    if (currentTheme === 'auto') {
      appliedTheme = systemTheme;
    } else {
      appliedTheme = currentTheme;
    }

    // Remove existing theme classes
    root.classList.remove('light', 'dark');
    // Add new theme class
    root.classList.add(appliedTheme);

    // Update meta theme-color for mobile browsers
    const metaThemeColor = document.querySelector('meta[name="theme-color"]');
    if (metaThemeColor) {
      metaThemeColor.setAttribute('content', appliedTheme === 'dark' ? '#111827' : '#ffffff');
    }
  }, [currentTheme]);

  // Listen for system theme changes when in auto mode
  useEffect(() => {
    if (currentTheme !== 'auto') return;

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handleChange = () => {
      const root = document.documentElement;
      const systemTheme = mediaQuery.matches ? 'dark' : 'light';
      root.classList.remove('light', 'dark');
      root.classList.add(systemTheme);
    };

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, [currentTheme]);

  const getThemeIcon = (theme: string) => {
    switch (theme) {
      case 'light':
        return <Sun size={size === 'sm' ? 16 : size === 'lg' ? 24 : 20} />;
      case 'dark':
        return <Moon size={size === 'sm' ? 16 : size === 'lg' ? 24 : 20} />;
      case 'auto':
        return <Monitor size={size === 'sm' ? 16 : size === 'lg' ? 24 : 20} />;
      default:
        return <Sun size={size === 'sm' ? 16 : size === 'lg' ? 24 : 20} />;
    }
  };

  const getThemeLabel = (theme: string) => {
    switch (theme) {
      case 'light':
        return '浅色模式';
      case 'dark':
        return '深色模式';
      case 'auto':
        return '跟随系统';
      default:
        return '浅色模式';
    }
  };

  const handleThemeChange = (theme: 'light' | 'dark' | 'auto') => {
    setTheme(theme);
  };

  if (variant === 'dropdown') {
    const themes: Array<'light' | 'dark' | 'auto'> = ['light', 'dark', 'auto'];

    return (
      <div className={cn('space-y-1', className)}>
        <label className="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
          主题设置
        </label>
        {themes.map((theme) => (
          <button
            key={theme}
            onClick={() => handleThemeChange(theme)}
            className={cn(
              'flex items-center space-x-3 w-full px-3 py-2 rounded-lg text-sm transition-colors',
              currentTheme === theme
                ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/20 dark:text-orange-300'
                : 'text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800'
            )}
          >
            {getThemeIcon(theme)}
            <span>{getThemeLabel(theme)}</span>
            {currentTheme === theme && (
              <div className="ml-auto w-2 h-2 bg-orange-500 rounded-full"></div>
            )}
          </button>
        ))}
      </div>
    );
  }

  return (
    <Button
      variant="ghost"
      size={size}
      onClick={toggleTheme}
      className={cn(
        'transition-all duration-200 hover:scale-105',
        showLabel ? 'space-x-2' : 'p-2',
        className
      )}
      aria-label={`当前主题: ${getThemeLabel(currentTheme)}，点击切换主题`}
      title={`当前: ${getThemeLabel(currentTheme)}`}
    >
      <span className="transition-transform duration-200">
        {getThemeIcon(currentTheme)}
      </span>
      {showLabel && (
        <span className="text-sm font-medium">
          {getThemeLabel(currentTheme)}
        </span>
      )}
    </Button>
  );
};

/**
 * Theme preference detection hook
 */
export const useThemeDetection = () => {
  const currentTheme = useCurrentTheme();

  const getEffectiveTheme = (): 'light' | 'dark' => {
    if (currentTheme === 'auto') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    return currentTheme as 'light' | 'dark';
  };

  const isDark = getEffectiveTheme() === 'dark';
  const isLight = getEffectiveTheme() === 'light';
  const isAuto = currentTheme === 'auto';

  return {
    currentTheme,
    effectiveTheme: getEffectiveTheme(),
    isDark,
    isLight,
    isAuto,
  };
};

/**
 * Usage:
 * <ThemeToggle /> - Simple icon button
 * <ThemeToggle showLabel /> - Button with label
 * <ThemeToggle variant="dropdown" /> - Dropdown menu with all options
 * <ThemeToggle size="lg" /> - Larger button
 *
 * Features:
 * - Light, dark, and auto (system) theme modes
 * - Smooth transitions between themes
 * - System preference detection and auto-switching
 * - Accessible with proper ARIA labels
 * - Multiple display variants (button/dropdown)
 * - Persistent theme preference storage via Zustand
 * - Meta theme-color updates for mobile browsers
 * - Automatic DOM class management
 */