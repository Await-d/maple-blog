// @ts-nocheck
/**
 * 首页性能优化工具集
 * 实现代码分割、懒加载、预加载、图片优化等性能优化策略
 */

import { lazy } from 'react';

// Core Web Vitals 目标阈值
export const PERFORMANCE_TARGETS = {
  LCP: 2500, // 最大内容绘制 < 2.5s
  FID: 100,  // 首次输入延迟 < 100ms
  CLS: 0.1,  // 累积布局偏移 < 0.1
  TTFB: 800, // 首字节时间 < 800ms
} as const;

// 懒加载组件映射
export const lazyComponents = {
  // 首页核心组件
  HeroSection: lazy(() => import('../components/home/HeroSection')),
  PopularPosts: lazy(() => import('../components/home/PopularPosts')),
  FeaturedPosts: lazy(() => import('../components/home/FeaturedPosts')),
  CategoryGrid: lazy(() => import('../components/home/CategoryGrid')),
  TagCloud: lazy(() => import('../components/home/TagCloud')),

  // 侧边栏组件
  StatsWidget: lazy(() => import('../components/home/StatsWidget')),
  ActiveAuthors: lazy(() => import('../components/home/ActiveAuthors')),

  // 个性化组件
  PersonalizedFeed: lazy(() =>
    import('../components/home/PersonalizedFeed').then(module => ({
      default: module.PersonalizedFeed
    }))
  ),

  // 搜索组件
  SearchSuggestions: lazy(() => import('../components/search/SearchSuggestions')),

  // 偏好设置
  PreferencesPanel: lazy(() => import('../components/common/PreferencesPanel')),
} as const;

// 关键资源预加载策略
export const preloadStrategies = {
  // 预加载关键CSS
  preloadCriticalCSS: () => {
    const criticalCSS = [
      '/styles/critical.css',
      '/styles/fonts.css',
    ];

    criticalCSS.forEach(href => {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.as = 'style';
      link.href = href;
      document.head.appendChild(link);
    });
  },

  // 预加载关键字体
  preloadFonts: () => {
    const fonts = [
      {
        href: '/fonts/inter-var.woff2',
        type: 'font/woff2',
        crossOrigin: 'anonymous'
      },
    ];

    fonts.forEach(({ href, type, crossOrigin }) => {
      const link = document.createElement('link');
      link.rel = 'preload';
      link.as = 'font';
      link.href = href;
      link.type = type;
      if (crossOrigin) link.crossOrigin = crossOrigin;
      document.head.appendChild(link);
    });
  },

  // 预取下一页内容
  prefetchNextPageContent: () => {
    const nextPageResources = [
      '/api/home/popular-posts',
      '/api/home/categories',
      '/api/home/stats',
    ];

    nextPageResources.forEach(href => {
      const link = document.createElement('link');
      link.rel = 'prefetch';
      link.href = href;
      document.head.appendChild(link);
    });
  },

  // 预连接到外部服务
  preconnectExternalServices: () => {
    const externalServices = [
      'https://cdn.jsdelivr.net',
      'https://fonts.googleapis.com',
      'https://api.github.com',
    ];

    externalServices.forEach(href => {
      const link = document.createElement('link');
      link.rel = 'preconnect';
      link.href = href;
      link.crossOrigin = 'anonymous';
      document.head.appendChild(link);
    });
  }
};

// 图片优化工具
export const imageOptimization = {
  // 生成响应式图片srcset
  generateSrcSet: (baseSrc: string, sizes: number[] = [320, 640, 1024, 1920]) => {
    return sizes.map(size => `${baseSrc}?w=${size}&q=75 ${size}w`).join(', ');
  },

  // 获取优化的图片URL
  getOptimizedImageUrl: (
    src: string,
    { width, height, quality = 75, format = 'webp' }: {
      width?: number;
      height?: number;
      quality?: number;
      format?: 'webp' | 'avif' | 'jpeg' | 'png';
    } = {}
  ) => {
    const params = new URLSearchParams();
    if (width) params.set('w', width.toString());
    if (height) params.set('h', height.toString());
    params.set('q', quality.toString());
    params.set('f', format);

    return `${src}?${params.toString()}`;
  },

  // 懒加载图片的Intersection Observer配置
  lazyLoadConfig: {
    root: null,
    rootMargin: '50px',
    threshold: 0.01
  },

  // 创建响应式图片加载器
  createResponsiveImageLoader: () => {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target as HTMLImageElement;
          const src = img.dataset.src;
          const srcset = img.dataset.srcset;

          if (src) img.src = src;
          if (srcset) img.srcset = srcset;

          img.classList.remove('lazy');
          img.classList.add('loaded');
          observer.unobserve(img);
        }
      });
    }, imageOptimization.lazyLoadConfig);

    return observer;
  }
};

// 代码分割策略
export const codeSplitting = {
  // 路由级别的代码分割
  routeChunks: {
    home: () => import('../pages/HomePage'),
    // blog: () => import('../pages/BlogPage'), // TODO: Create BlogPage
    // post: () => import('../pages/PostDetailPage'), // TODO: Create PostDetailPage
    // archive: () => import('../pages/ArchivePage'), // TODO: Create ArchivePage
    // admin: () => import('../pages/AdminPage'), // TODO: Create AdminPage
  },

  // 功能模块的代码分割
  featureChunks: {
    // auth: () => import('../features/auth'), // TODO: Create auth index
    // search: () => import('../features/search'), // TODO: Create search index
    // admin: () => import('../features/admin'), // TODO: Create admin index
    // personalization: () => import('../features/personalization'), // TODO: Create personalization index
  },

  // 第三方库的代码分割配置 (供Vite使用)
  vendorChunks: {
    react: ['react', 'react-dom'],
    router: ['react-router-dom'],
    ui: ['@radix-ui/react-dialog', '@radix-ui/react-dropdown-menu'],
    query: ['@tanstack/react-query'],
    charts: ['recharts'],
    animation: ['framer-motion'],
    utils: ['date-fns', 'lodash-es'],
  }
};

// 缓存策略配置
export const cacheStrategies = {
  // Service Worker缓存策略
  swCacheConfig: {
    staticAssets: {
      strategy: 'CacheFirst',
      maxEntries: 100,
      maxAgeSeconds: 60 * 60 * 24 * 30 // 30天
    },
    apiData: {
      strategy: 'NetworkFirst',
      maxEntries: 50,
      maxAgeSeconds: 60 * 60 * 24 // 1天
    },
    images: {
      strategy: 'CacheFirst',
      maxEntries: 200,
      maxAgeSeconds: 60 * 60 * 24 * 7 // 7天
    }
  },

  // 浏览器缓存控制
  browserCache: {
    // 设置强缓存
    setStrongCache: (resource: 'css' | 'js' | 'image' | 'font') => {
      const cacheControl = {
        css: 'public, max-age=31536000, immutable',
        js: 'public, max-age=31536000, immutable',
        image: 'public, max-age=2592000',
        font: 'public, max-age=31536000, immutable'
      };
      return cacheControl[resource];
    },

    // 设置协商缓存
    setEtagCache: () => ({
      'Cache-Control': 'public, max-age=0, must-revalidate',
      'ETag': true
    })
  }
};

// 性能监控工具
export const performanceMonitoring = {
  // 测量性能指标
  measurePerformance: () => {
    return new Promise<{
      lcp?: number;
      fid?: number;
      cls?: number;
      ttfb?: number;
    }>((resolve) => {
      const metrics: Record<string, number> = {};

      // 测量 LCP
      new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const lastEntry = entries[entries.length - 1] as PerformancePaintTiming;
        metrics.lcp = lastEntry.startTime;
      }).observe({ entryTypes: ['largest-contentful-paint'] });

      // 测量 FID
      new PerformanceObserver((list) => {
        const entries = list.getEntries() as PerformanceEntry[];
        entries.forEach((entry) => {
          metrics.fid = (entry as any).processingStart - entry.startTime;
        });
      }).observe({ entryTypes: ['first-input'] });

      // 测量 CLS
      let clsValue = 0;
      new PerformanceObserver((list) => {
        for (const entry of list.getEntries() as PerformanceEntry[]) {
          if (!(entry as any).hadRecentInput) {
            clsValue += (entry as any).value;
          }
        }
        metrics.cls = clsValue;
      }).observe({ entryTypes: ['layout-shift'] });

      // 测量 TTFB
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      if (navigation) {
        metrics.ttfb = navigation.responseStart - navigation.requestStart;
      }

      // 延迟解析，等待指标收集
      setTimeout(() => resolve(metrics), 3000);
    });
  },

  // 性能预算检查
  checkPerformanceBudget: (metrics: {
    lcp?: number;
    fid?: number;
    cls?: number;
    ttfb?: number;
  }) => {
    const results = {
      lcp: metrics.lcp ? metrics.lcp <= PERFORMANCE_TARGETS.LCP : false,
      fid: metrics.fid ? metrics.fid <= PERFORMANCE_TARGETS.FID : false,
      cls: metrics.cls ? metrics.cls <= PERFORMANCE_TARGETS.CLS : false,
      ttfb: metrics.ttfb ? metrics.ttfb <= PERFORMANCE_TARGETS.TTFB : false,
    };

    const passed = Object.values(results).every(Boolean);
    return { results, passed };
  },

  // 上报性能数据
  reportPerformanceMetrics: (metrics: Record<string, number>, endpoint = '/api/analytics/performance') => {
    if (navigator.sendBeacon) {
      navigator.sendBeacon(endpoint, JSON.stringify(metrics));
    } else {
      fetch(endpoint, {
        method: 'POST',
        body: JSON.stringify(metrics),
        headers: { 'Content-Type': 'application/json' }
      }).catch(() => {
        // 静默处理错误，不影响用户体验
      });
    }
  }
};

// 初始化性能优化
export const initializePerformanceOptimizations = () => {
  // 预加载关键资源
  preloadStrategies.preloadCriticalCSS();
  preloadStrategies.preloadFonts();
  preloadStrategies.preconnectExternalServices();

  // 预取下一页内容 (延迟执行)
  setTimeout(() => {
    preloadStrategies.prefetchNextPageContent();
  }, 2000);

  // 初始化图片懒加载
  const imageObserver = imageOptimization.createResponsiveImageLoader();
  document.querySelectorAll('img[data-src]').forEach(img => {
    imageObserver.observe(img);
  });

  // 监控性能 (页面加载完成后)
  window.addEventListener('load', async () => {
    // 延迟测量，确保所有指标都能收集到
    setTimeout(async () => {
      const metrics = await performanceMonitoring.measurePerformance();
      const budgetCheck = performanceMonitoring.checkPerformanceBudget(metrics);

      // 上报性能数据
      performanceMonitoring.reportPerformanceMetrics({
        ...metrics,
        budgetPassed: budgetCheck.passed ? 1 : 0,
        url: window.location.href,
        timestamp: Date.now()
      });
    }, 5000);
  });
};

// 导出默认配置
export default {
  lazyComponents,
  preloadStrategies,
  imageOptimization,
  codeSplitting,
  cacheStrategies,
  performanceMonitoring,
  initializePerformanceOptimizations,
  PERFORMANCE_TARGETS
};