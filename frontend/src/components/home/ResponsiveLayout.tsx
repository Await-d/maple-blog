// @ts-nocheck
/**
 * 响应式布局组件
 * 实现移动优先的响应式布局系统，支持断点管理和触摸友好的移动交互
 */

import React, { ReactNode, useState, useCallback, useEffect } from 'react';
import { useResponsive } from '../../hooks/useResponsive';
import { useTouchGestures } from '../../hooks/useTouchGestures';
import { cn } from '../../utils/cn';

// 布局配置接口
export interface LayoutConfig {
  // 主要区域
  header: {
    height: number;
    sticky: boolean;
    collapsed?: boolean;
  };
  sidebar: {
    width: number;
    collapsible: boolean;
    collapsed?: boolean;
    position: 'left' | 'right';
  };
  content: {
    maxWidth?: number;
    padding: number;
    centered?: boolean;
  };
  footer: {
    height: number;
    sticky?: boolean;
  };
}

// 响应式断点配置
export interface ResponsiveLayoutConfig {
  xs: LayoutConfig;
  sm: LayoutConfig;
  md: LayoutConfig;
  lg: LayoutConfig;
  xl: LayoutConfig;
  '2xl': LayoutConfig;
}

// 组件属性
export interface ResponsiveLayoutProps {
  children?: ReactNode;
  header?: ReactNode;
  sidebar?: ReactNode;
  footer?: ReactNode;
  config?: Partial<ResponsiveLayoutConfig>;
  className?: string;
  enableTouchGestures?: boolean;
  onSidebarToggle?: (collapsed: boolean) => void;
  onLayoutChange?: (breakpoint: string) => void;
}

// 默认配置
const defaultLayoutConfig: ResponsiveLayoutConfig = {
  xs: {
    header: { height: 56, sticky: true, collapsed: false },
    sidebar: { width: 0, collapsible: false, collapsed: true, position: 'left' },
    content: { padding: 16, centered: true },
    footer: { height: 60, sticky: false }
  },
  sm: {
    header: { height: 60, sticky: true, collapsed: false },
    sidebar: { width: 0, collapsible: false, collapsed: true, position: 'left' },
    content: { padding: 20, centered: true },
    footer: { height: 64, sticky: false }
  },
  md: {
    header: { height: 64, sticky: true, collapsed: false },
    sidebar: { width: 240, collapsible: true, collapsed: false, position: 'left' },
    content: { padding: 24, centered: true, maxWidth: 1200 },
    footer: { height: 80, sticky: false }
  },
  lg: {
    header: { height: 68, sticky: true, collapsed: false },
    sidebar: { width: 280, collapsible: true, collapsed: false, position: 'left' },
    content: { padding: 32, centered: true, maxWidth: 1400 },
    footer: { height: 100, sticky: false }
  },
  xl: {
    header: { height: 72, sticky: true, collapsed: false },
    sidebar: { width: 320, collapsible: true, collapsed: false, position: 'left' },
    content: { padding: 40, centered: true, maxWidth: 1600 },
    footer: { height: 120, sticky: false }
  },
  '2xl': {
    header: { height: 80, sticky: true, collapsed: false },
    sidebar: { width: 360, collapsible: true, collapsed: false, position: 'left' },
    content: { padding: 48, centered: true, maxWidth: 1800 },
    footer: { height: 140, sticky: false }
  }
};

// 侧边栏移动手势组件
const SidebarGestureArea: React.FC<{
  onSwipeOpen: () => void;
  onSwipeClose: () => void;
  className?: string;
  children?: ReactNode;
}> = ({ onSwipeOpen, onSwipeClose, className, children }) => {
  const { touchHandlers } = useTouchGestures(
    {
      swipeThreshold: 50
    },
    {
      onSwipeLeft: onSwipeClose,
      onSwipeRight: onSwipeOpen
    },
    {} // DOM callbacks - 空对象因为我们使用React接口
  );

  return (
    <div {...touchHandlers} className={className}>
      {children}
    </div>
  );
};

// 移动端侧边栏遮罩
const SidebarOverlay: React.FC<{
  visible: boolean;
  onClose: () => void;
}> = ({ visible, onClose }) => {
  if (!visible) return null;

  return (
    <div
      className={cn(
        'fixed inset-0 bg-black/50 z-40 transition-opacity',
        'backdrop-blur-sm md:hidden'
      )}
      onClick={onClose}
      aria-label="关闭侧边栏"
    />
  );
};

/**
 * 响应式布局主组件
 */
export const ResponsiveLayout: React.FC<ResponsiveLayoutProps> = ({
  children,
  header,
  sidebar,
  footer,
  config = {},
  className,
  enableTouchGestures = true,
  onSidebarToggle,
  onLayoutChange
}) => {
  const {
    breakpoint,
    isMobile,
    isTablet,
    isDesktop: _isDesktop,
    isTouchDevice: _isTouchDevice,
    viewportWidth,
    viewportHeight: _viewportHeight
  } = useResponsive();

  // 获取当前断点配置
  const currentConfig: LayoutConfig = {
    ...defaultLayoutConfig[breakpoint],
    ...config[breakpoint]
  };

  // 侧边栏状态管理
  const [sidebarCollapsed, setSidebarCollapsed] = useState(
    currentConfig.sidebar.collapsed ?? false
  );
  const [sidebarAnimating, setSidebarAnimating] = useState(false);

  // 布局变化回调
  useEffect(() => {
    onLayoutChange?.(breakpoint);
  }, [breakpoint, onLayoutChange]);

  // 响应断点变化自动调整侧边栏
  useEffect(() => {
    const newCollapsed = currentConfig.sidebar.collapsed ?? false;
    setSidebarCollapsed(newCollapsed);
  }, [currentConfig.sidebar.collapsed]);

  // 侧边栏切换
  const toggleSidebar = useCallback(() => {
    setSidebarAnimating(true);
    const newCollapsed = !sidebarCollapsed;
    setSidebarCollapsed(newCollapsed);
    onSidebarToggle?.(newCollapsed);

    // 动画完成后重置状态
    setTimeout(() => {
      setSidebarAnimating(false);
    }, 300);
  }, [sidebarCollapsed, onSidebarToggle]);

  // 手势控制侧边栏
  const handleSwipeOpen = useCallback(() => {
    if (isMobile && sidebarCollapsed) {
      toggleSidebar();
    }
  }, [isMobile, sidebarCollapsed, toggleSidebar]);

  const handleSwipeClose = useCallback(() => {
    if (isMobile && !sidebarCollapsed) {
      toggleSidebar();
    }
  }, [isMobile, sidebarCollapsed, toggleSidebar]);

  // 计算布局样式
  const layoutStyles = {
    '--header-height': `${currentConfig.header.height}px`,
    '--sidebar-width': `${currentConfig.sidebar.width}px`,
    '--content-padding': `${currentConfig.content.padding}px`,
    '--footer-height': `${currentConfig.footer.height}px`,
    '--content-max-width': currentConfig.content.maxWidth
      ? `${currentConfig.content.maxWidth}px`
      : 'none'
  } as React.CSSProperties;

  // 渲染内容区域
  const renderContent = () => {
    const contentClasses = cn(
      'flex-1 transition-all duration-300 ease-in-out',
      'min-h-0', // 防止flex子项溢出

      // 基础样式
      currentConfig.content.centered ? 'mx-auto' : '',

      // 响应式间距
      isMobile ? 'px-4' : isTablet ? 'px-6' : 'px-8',

      // 侧边栏适配
      !isMobile && sidebar && !sidebarCollapsed
        ? currentConfig.sidebar.position === 'left'
          ? 'ml-0'
          : 'mr-0'
        : '',

      // 最大宽度
      currentConfig.content.maxWidth ? `max-w-[${currentConfig.content.maxWidth}px]` : ''
    );

    const contentStyle = {
      paddingTop: `${currentConfig.content.padding}px`,
      paddingBottom: `${currentConfig.content.padding}px`,
      marginLeft: !isMobile && sidebar && !sidebarCollapsed && currentConfig.sidebar.position === 'left'
        ? `${currentConfig.sidebar.width}px`
        : '0',
      marginRight: !isMobile && sidebar && !sidebarCollapsed && currentConfig.sidebar.position === 'right'
        ? `${currentConfig.sidebar.width}px`
        : '0'
    };

    return (
      <main className={contentClasses} style={contentStyle}>
        {enableTouchGestures && isMobile ? (
          <SidebarGestureArea
            onSwipeOpen={handleSwipeOpen}
            onSwipeClose={handleSwipeClose}
            className="min-h-full"
          >
            {children}
          </SidebarGestureArea>
        ) : (
          children
        )}
      </main>
    );
  };

  // 渲染侧边栏
  const renderSidebar = () => {
    if (!sidebar || currentConfig.sidebar.width === 0) return null;

    const sidebarClasses = cn(
      'fixed top-0 z-50 transition-all duration-300 ease-in-out',
      'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700',
      'overflow-y-auto overflow-x-hidden',

      // 位置
      currentConfig.sidebar.position === 'left' ? 'left-0' : 'right-0',
      currentConfig.sidebar.position === 'left'
        ? 'border-r'
        : 'border-l',

      // 响应式显示
      isMobile
        ? [
            'h-full shadow-xl',
            sidebarCollapsed
              ? currentConfig.sidebar.position === 'left' ? '-translate-x-full' : 'translate-x-full'
              : 'translate-x-0'
          ]
        : [
            'h-screen',
            sidebarCollapsed ? 'w-0' : `w-[${currentConfig.sidebar.width}px]`
          ],

      // 动画状态
      sidebarAnimating ? 'transition-transform' : ''
    );

    const sidebarStyle = {
      width: isMobile
        ? `${Math.min(currentConfig.sidebar.width, viewportWidth * 0.85)}px`
        : sidebarCollapsed
          ? '0px'
          : `${currentConfig.sidebar.width}px`,
      top: currentConfig.header.sticky ? `${currentConfig.header.height}px` : '0',
      height: currentConfig.header.sticky
        ? `calc(100vh - ${currentConfig.header.height}px)`
        : '100vh'
    };

    return (
      <>
        <aside
          className={sidebarClasses}
          style={sidebarStyle}
          aria-expanded={!sidebarCollapsed}
        >
          <div className={cn(
            'p-4',
            sidebarCollapsed && !isMobile ? 'hidden' : 'block'
          )}>
            {sidebar}
          </div>
        </aside>

        {/* 移动端遮罩 */}
        {isMobile && (
          <SidebarOverlay
            visible={!sidebarCollapsed}
            onClose={toggleSidebar}
          />
        )}
      </>
    );
  };

  // 渲染头部
  const renderHeader = () => {
    if (!header) return null;

    const headerClasses = cn(
      'w-full z-40 transition-all duration-300',
      'bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm',
      'border-b border-gray-200 dark:border-gray-700',

      // 粘性定位
      currentConfig.header.sticky ? 'sticky top-0' : 'relative'
    );

    const headerStyle = {
      height: `${currentConfig.header.height}px`,
      minHeight: `${currentConfig.header.height}px`
    };

    return (
      <header className={headerClasses} style={headerStyle}>
        <div className="flex items-center h-full px-4 lg:px-6">
          {/* 侧边栏切换按钮 */}
          {sidebar && currentConfig.sidebar.collapsible && (
            <button
              onClick={toggleSidebar}
              className={cn(
                'inline-flex items-center justify-center',
                'w-10 h-10 rounded-md',
                'text-gray-500 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white',
                'hover:bg-gray-100 dark:hover:bg-gray-800',
                'transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500',
                isMobile ? 'mr-2' : 'mr-4'
              )}
              aria-label={sidebarCollapsed ? '展开侧边栏' : '收起侧边栏'}
            >
              <svg
                className="w-6 h-6"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d={sidebarCollapsed
                    ? 'M4 6h16M4 12h16M4 18h16'
                    : 'M6 18L18 6M6 6l12 12'
                  }
                />
              </svg>
            </button>
          )}

          <div className="flex-1">
            {header}
          </div>
        </div>
      </header>
    );
  };

  // 渲染页脚
  const renderFooter = () => {
    if (!footer) return null;

    const footerClasses = cn(
      'w-full z-30 transition-all duration-300',
      'bg-white dark:bg-gray-900',
      'border-t border-gray-200 dark:border-gray-700',

      // 粘性定位
      currentConfig.footer.sticky ? 'sticky bottom-0' : 'relative'
    );

    const footerStyle = {
      height: `${currentConfig.footer.height}px`,
      minHeight: `${currentConfig.footer.height}px`
    };

    return (
      <footer className={footerClasses} style={footerStyle}>
        <div className="flex items-center h-full px-4 lg:px-6">
          {footer}
        </div>
      </footer>
    );
  };

  return (
    <div
      className={cn(
        'min-h-screen flex flex-col',
        'bg-gray-50 dark:bg-gray-950',
        'transition-colors duration-200',
        className
      )}
      style={layoutStyles}
    >
      {renderHeader()}

      <div className="flex flex-1 relative">
        {renderSidebar()}
        {renderContent()}
      </div>

      {renderFooter()}
    </div>
  );
};

// 布局上下文 Hook
export const useLayout = () => {
  const responsive = useResponsive();

  return {
    ...responsive,

    // 获取当前布局配置
    getCurrentConfig: (config: Partial<ResponsiveLayoutConfig> = {}) => {
      return {
        ...defaultLayoutConfig[responsive.breakpoint],
        ...config[responsive.breakpoint]
      };
    },

    // 检查是否需要折叠侧边栏
    shouldCollapseSidebar: () => {
      return responsive.isMobile || responsive.isTablet;
    },

    // 获取内容区域最大宽度
    getContentMaxWidth: () => {
      const config = defaultLayoutConfig[responsive.breakpoint];
      return config.content.maxWidth || 'none';
    },

    // 获取适合当前设备的组件尺寸
    getComponentSize: () => {
      if (responsive.isMobile) return 'sm';
      if (responsive.isTablet) return 'md';
      if (responsive.isLargeScreen) return 'xl';
      return 'lg';
    }
  };
};

export default ResponsiveLayout;