// @ts-nocheck
/**
 * 响应式设计Hook
 * 检测设备类型、屏幕尺寸、触摸支持等
 */

import { useState, useEffect, useCallback } from 'react';

export interface ResponsiveState {
  // 屏幕尺寸
  isMobile: boolean;
  isTablet: boolean;
  isDesktop: boolean;
  isLargeScreen: boolean;

  // 设备特性
  isTouchDevice: boolean;
  isRetina: boolean;
  orientation: 'portrait' | 'landscape';

  // 屏幕尺寸数值
  screenWidth: number;
  screenHeight: number;
  viewportWidth: number;
  viewportHeight: number;

  // 断点
  breakpoint: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | '2xl';
}

// 断点定义 (基于 Tailwind CSS)
const breakpoints = {
  xs: 0,    // < 640px
  sm: 640,  // 640px - 767px
  md: 768,  // 768px - 1023px
  lg: 1024, // 1024px - 1279px
  xl: 1280, // 1280px - 1535px
  '2xl': 1536 // >= 1536px
} as const;

export const useResponsive = () => {
  const [state, setState] = useState<ResponsiveState>({
    isMobile: false,
    isTablet: false,
    isDesktop: true,
    isLargeScreen: false,
    isTouchDevice: false,
    isRetina: false,
    orientation: 'landscape',
    screenWidth: typeof window !== 'undefined' ? window.screen.width : 1920,
    screenHeight: typeof window !== 'undefined' ? window.screen.height : 1080,
    viewportWidth: typeof window !== 'undefined' ? window.innerWidth : 1920,
    viewportHeight: typeof window !== 'undefined' ? window.innerHeight : 1080,
    breakpoint: 'xl'
  });

  const updateState = useCallback(() => {
    if (typeof window === 'undefined') return;

    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const screenWidth = window.screen.width;
    const screenHeight = window.screen.height;

    // 检测设备类型
    const isMobile = viewportWidth < breakpoints.md;
    const isTablet = viewportWidth >= breakpoints.md && viewportWidth < breakpoints.lg;
    const isDesktop = viewportWidth >= breakpoints.lg;
    const isLargeScreen = viewportWidth >= breakpoints.xl;

    // 检测触摸设备
    const isTouchDevice = 'ontouchstart' in window ||
                         navigator.maxTouchPoints > 0 ||
                         /Mobi|Android/i.test(navigator.userAgent);

    // 检测高分辨率屏幕
    const isRetina = window.devicePixelRatio > 1;

    // 检测方向
    const orientation = viewportWidth > viewportHeight ? 'landscape' : 'portrait';

    // 确定当前断点
    let breakpoint: ResponsiveState['breakpoint'] = 'xs';
    if (viewportWidth >= breakpoints['2xl']) breakpoint = '2xl';
    else if (viewportWidth >= breakpoints.xl) breakpoint = 'xl';
    else if (viewportWidth >= breakpoints.lg) breakpoint = 'lg';
    else if (viewportWidth >= breakpoints.md) breakpoint = 'md';
    else if (viewportWidth >= breakpoints.sm) breakpoint = 'sm';

    setState({
      isMobile,
      isTablet,
      isDesktop,
      isLargeScreen,
      isTouchDevice,
      isRetina,
      orientation,
      screenWidth,
      screenHeight,
      viewportWidth,
      viewportHeight,
      breakpoint
    });
  }, []);

  useEffect(() => {
    // 初始化
    updateState();

    // 监听窗口大小变化
    const handleResize = () => {
      updateState();
    };

    // 监听设备方向变化
    const handleOrientationChange = () => {
      // 延迟更新，确保尺寸已经更新
      setTimeout(updateState, 100);
    };

    window.addEventListener('resize', handleResize);
    window.addEventListener('orientationchange', handleOrientationChange);

    return () => {
      window.removeEventListener('resize', handleResize);
      window.removeEventListener('orientationchange', handleOrientationChange);
    };
  }, [updateState]);

  return {
    ...state,

    // 工具方法
    isBreakpoint: (bp: keyof typeof breakpoints) => {
      return state.breakpoint === bp;
    },

    isBreakpointUp: (bp: keyof typeof breakpoints) => {
      const bpValue = breakpoints[bp];
      const currentValue = breakpoints[state.breakpoint];
      return currentValue >= bpValue;
    },

    isBreakpointDown: (bp: keyof typeof breakpoints) => {
      const bpValue = breakpoints[bp];
      const currentValue = breakpoints[state.breakpoint];
      return currentValue < bpValue;
    },

    // 获取适合的组件配置
    getCommentConfig: () => ({
      // 评论列表配置
      enableVirtualScroll: state.isDesktop && state.viewportHeight > 800,
      pageSize: state.isMobile ? 10 : state.isTablet ? 15 : 20,
      maxDepth: state.isMobile ? 2 : 3,
      showAvatars: !state.isMobile || state.viewportWidth > 480,
      showStats: state.isDesktop,
      compact: state.isMobile,

      // 表单配置
      useMobileForm: state.isMobile && state.isTouchDevice,
      autoFocus: !state.isTouchDevice, // 触摸设备避免自动聚焦弹出键盘
      showToolbar: true,
      compactToolbar: state.isMobile,

      // 通知配置
      position: state.isMobile ? 'left' as const : 'right' as const,
      maxNotifications: state.isMobile ? 5 : 10
    }),

    // 获取首页布局配置
    getHomeLayoutConfig: () => ({
      // 栅格配置
      gridCols: state.isMobile ? 1 : state.isTablet ? 2 : state.isLargeScreen ? 4 : 3,
      gap: state.isMobile ? 4 : state.isTablet ? 6 : 8,

      // 侧边栏配置
      showSidebar: state.isDesktop,
      sidebarWidth: state.isLargeScreen ? 320 : 280,
      collapsibleSidebar: state.isTablet,

      // 内容区域
      contentPadding: state.isMobile ? 16 : state.isTablet ? 24 : 32,
      maxContentWidth: state.isMobile ? '100%' : state.isTablet ? '768px' : state.isLargeScreen ? '1600px' : '1400px',

      // 组件显示
      showHeroSection: true,
      heroHeight: state.isMobile ? 300 : state.isTablet ? 400 : 500,
      showFeaturedPosts: state.viewportWidth > 640,
      postsPerRow: state.isMobile ? 1 : state.isTablet ? 2 : state.isLargeScreen ? 4 : 3,

      // 交互优化
      enableTouchGestures: state.isTouchDevice,
      enableHoverEffects: !state.isTouchDevice,
      lazyLoadImages: true,
      virtualScrolling: state.isDesktop && state.viewportHeight > 800
    }),

    // 获取组件尺寸配置
    getComponentSizes: () => ({
      // 按钮尺寸
      buttonSize: state.isMobile ? 'sm' : 'md',
      iconSize: state.isMobile ? 16 : 20,

      // 字体尺寸
      titleSize: state.isMobile ? 'text-xl' : state.isTablet ? 'text-2xl' : 'text-3xl',
      bodySize: state.isMobile ? 'text-sm' : 'text-base',

      // 间距
      spacing: {
        xs: state.isMobile ? 2 : 4,
        sm: state.isMobile ? 4 : 6,
        md: state.isMobile ? 6 : 8,
        lg: state.isMobile ? 8 : 12,
        xl: state.isMobile ? 12 : 16
      },

      // 容器
      containerPadding: state.isMobile ? 'px-4' : state.isTablet ? 'px-6' : 'px-8',
      maxWidth: state.isMobile ? 'max-w-full' : state.isTablet ? 'max-w-4xl' : 'max-w-7xl'
    }),

    // 检查网络连接质量
    getNetworkOptimizations: () => ({
      // 基于连接类型的优化
      enableImageLazyLoad: true,
      imageQuality: (navigator as any)?.connection?.effectiveType === '4g' ? 80 : 60,
      enableVideoPreload: (navigator as any)?.connection?.effectiveType === '4g',
      prefetchContent: !state.isMobile && (navigator as any)?.connection?.effectiveType === '4g',

      // 数据使用优化
      compressImages: state.isMobile || (navigator as any)?.connection?.saveData,
      reducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)').matches,
      preloadCriticalResources: state.isDesktop
    })
  };
};

export default useResponsive;