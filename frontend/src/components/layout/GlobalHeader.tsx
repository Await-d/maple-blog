/**
 * GlobalHeader - 全局头部组件包装器
 * 对现有Header组件的重新导出和适配
 */

import React from 'react';
import { Header } from './Header';

interface GlobalHeaderProps {
  theme?: 'light' | 'dark' | 'system';
  onThemeToggle?: () => void;
  className?: string;
}

export const GlobalHeader: React.FC<GlobalHeaderProps> = ({
  theme: _theme,
  onThemeToggle: _onThemeToggle,
  className,
}) => {
  return (
    <Header
      className={className}
      transparent={false}
      fixed={true}
    />
  );
};

export default GlobalHeader;