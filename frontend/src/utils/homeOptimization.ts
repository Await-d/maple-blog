/**
 * 首页性能优化工具集
 * 实现代码分割、懒加载、预加载、图片优化等性能优化策略
 */

import { lazy } from 'react';

// Performance API type extensions
interface LayoutShift extends PerformanceEntry {
  value: number;
  hadRecentInput: boolean;
}

interface PerformanceEventTiming extends PerformanceEntry {
  processingStart: number;
}

// Network Information API types
interface NetworkInformation {
  readonly effectiveType: 'slow-2g' | '2g' | '3g' | '4g';
  readonly downlink: number;
  readonly rtt: number;
  readonly saveData: boolean;
  addEventListener(type: string, listener: EventListener): void;
  removeEventListener(type: string, listener: EventListener): void;
}

interface NavigatorWithConnection extends Navigator {
  readonly connection?: NetworkInformation;
}

// Memory API types (Chrome-specific)
interface MemoryInfo {
  readonly usedJSHeapSize: number;
  readonly totalJSHeapSize: number;
  readonly jsHeapSizeLimit: number;
}

interface PerformanceWithMemory extends Performance {
  readonly memory?: MemoryInfo;
}

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
    blog: () => import('../pages/BlogPage'),
    post: () => import('../pages/PostDetailPage'),
    archive: () => import('../pages/archive/ArchivePage'),
    admin: () => import('../pages/AdminPage'),
  },

  // 功能模块的代码分割
  featureChunks: {
    auth: () => import('../features/auth'),
    search: () => import('../features/search'),
    admin: () => import('../features/admin'),
    personalization: () => import('../features/personalization'),
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
      fcp?: number;
      navigation?: PerformanceNavigationTiming;
      resources?: PerformanceResourceTiming[];
    }>((resolve, _reject) => {
      const metrics: Record<string, number> = {};
      const observers: PerformanceObserver[] = [];
      let observersCreated = 0;
      let observersCompleted = 0;
      const timeout = 10000; // 10 seconds timeout
      
      const cleanup = () => {
        observers.forEach(observer => {
          try {
            observer.disconnect();
          } catch (error) {
            // Observer might already be disconnected
          }
        });
      };

      const completeWithResults = () => {
        cleanup();
        
        // Add navigation and resource timing data
        try {
          const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
          if (navigation) {
            metrics.ttfb = navigation.responseStart - navigation.requestStart;
            metrics.domContentLoaded = navigation.domContentLoadedEventEnd - navigation.fetchStart;
            metrics.loadComplete = navigation.loadEventEnd - navigation.fetchStart;
          }

          // First Contentful Paint from paint timing API
          const paintEntries = performance.getEntriesByType('paint');
          const fcpEntry = paintEntries.find(entry => entry.name === 'first-contentful-paint');
          if (fcpEntry) {
            metrics.fcp = fcpEntry.startTime;
          }

          // Resource timing summary
          const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[];
          if (resources.length > 0) {
            metrics.resourceCount = resources.length;
            metrics.totalResourceSize = resources.reduce((total, resource) => 
              total + (resource.transferSize || 0), 0
            );
            metrics.avgResourceLoadTime = resources.reduce((total, resource) => 
              total + (resource.responseEnd - resource.startTime), 0
            ) / resources.length;
          }
        } catch (error) {
          // Don't fail the entire measurement due to navigation timing issues
        }

        resolve(metrics);
      };

      const createObserver = async (entryTypes: string[], handler: (list: PerformanceObserverEntryList) => void) => {
        try {
          if (!('PerformanceObserver' in window)) {
            throw new Error('PerformanceObserver not supported');
          }

          const observer = new PerformanceObserver((list) => {
            try {
              handler(list);
              observersCompleted++;
              
              // Complete when all observers have fired or timeout
              if (observersCompleted >= observersCreated || Date.now() - startTime > timeout) {
                completeWithResults();
              }
            } catch (error) {
              const logger = import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
              logger.then(l => l.warn('Performance observer handler error', 'observer_error', {
                entry_types: entryTypes,
                error_message: (error as Error).message
              }, error as Error));
            }
          });

          observer.observe({ entryTypes });
          observers.push(observer);
          observersCreated++;
          
        } catch (error) {
          const logger = await import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
          logger.warn('Failed to create performance observer', 'observer_creation_error', {
            entry_types: entryTypes,
            error_message: (error as Error).message,
            browser_support: 'PerformanceObserver' in window
          }, error as Error);
        }
      };

      const startTime = Date.now();

      // Set overall timeout
      const timeoutId = setTimeout(() => {
        const logger = import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
        logger.then(l => l.warn('Performance measurement timed out', 'measurement_timeout', {
          timeout_ms: timeout,
          observers_created: observersCreated,
          observers_completed: observersCompleted,
          partial_metrics: Object.keys(metrics)
        }));
        completeWithResults();
      }, timeout);

      // Setup observers with error handling
      Promise.all([
        // LCP Observer
        createObserver(['largest-contentful-paint'], (list) => {
          const entries = list.getEntries();
          const lastEntry = entries[entries.length - 1] as PerformancePaintTiming;
          if (lastEntry) {
            metrics.lcp = lastEntry.startTime;
          }
        }),

        // FID Observer  
        createObserver(['first-input'], (list) => {
          const entries = list.getEntries() as PerformanceEntry[];
          entries.forEach((entry) => {
            metrics.fid = (entry as PerformanceEventTiming).processingStart - entry.startTime;
          });
        }),

        // CLS Observer
        createObserver(['layout-shift'], (list) => {
          for (const entry of list.getEntries() as PerformanceEntry[]) {
            if (!(entry as LayoutShift).hadRecentInput) {
              metrics.cls = (metrics.cls || 0) + (entry as LayoutShift).value;
            }
          }
        })
      ]).then(() => {
        // If no observers were created successfully, resolve immediately
        if (observersCreated === 0) {
          clearTimeout(timeoutId);
          completeWithResults();
        }
      }).catch((error) => {
        clearTimeout(timeoutId);
        const logger = import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
        logger.then(l => l.error('Failed to setup performance observers', 'observer_setup_error', {
          error_message: (error as Error).message
        }, error as Error));
        
        // Still resolve with partial metrics rather than reject
        completeWithResults();
      });

      // Fallback: resolve after minimum wait time if no observers trigger
      setTimeout(() => {
        if (observersCompleted === 0) {
          completeWithResults();
        }
      }, 5000); // 5 second minimum wait
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
  reportPerformanceMetrics: async (metrics: Record<string, number>, endpoint = '/api/analytics/performance') => {
    const maxRetries = 3;
    const baseDelay = 1000;
    
    const enrichedMetrics = {
      ...metrics,
      timestamp: Date.now(),
      page_url: window.location.href,
      user_agent: navigator.userAgent,
      connection_type: (navigator as NavigatorWithConnection)?.connection?.effectiveType || 'unknown',
      memory_info: (performance as PerformanceWithMemory)?.memory ? {
        used_js_heap_size: (performance as PerformanceWithMemory).memory!.usedJSHeapSize,
        total_js_heap_size: (performance as PerformanceWithMemory).memory!.totalJSHeapSize,
        js_heap_size_limit: (performance as PerformanceWithMemory).memory!.jsHeapSizeLimit
      } : null,
      session_id: sessionStorage.getItem('session_id') || 'unknown'
    };

    for (let attempt = 0; attempt < maxRetries; attempt++) {
      try {
        // Try sendBeacon first (more reliable for page unload scenarios)
        if ((attempt === maxRetries - 1 || document.visibilityState === 'hidden') && navigator.sendBeacon) {
          const sent = navigator.sendBeacon(endpoint, JSON.stringify(enrichedMetrics));
          if (sent) {
            const logger = await import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
            logger.debug('Performance metrics sent via sendBeacon', 'metrics_transmission', {
              metrics_count: Object.keys(metrics).length,
              endpoint,
              attempt: attempt + 1
            });
            return;
          }
        }

        // Fallback to fetch with enhanced error handling
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 8000); // 8 second timeout

        const response = await fetch(endpoint, {
          method: 'POST',
          body: JSON.stringify(enrichedMetrics),
          headers: { 
            'Content-Type': 'application/json',
            'X-Client-Version': '1.0.0',
            'X-Request-ID': crypto.randomUUID()
          },
          signal: controller.signal
        });

        clearTimeout(timeoutId);

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const logger = await import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
        logger.debug('Performance metrics sent successfully', 'metrics_transmission', {
          metrics_count: Object.keys(metrics).length,
          endpoint,
          attempt: attempt + 1,
          response_status: response.status
        });
        
        return; // Success

      } catch (error) {
        const logger = await import('./logger').then(m => m.createLogger('PerformanceMonitoring'));
        
        if (attempt === maxRetries - 1) {
          // Final attempt failed - log but don't throw (silent failure)
          logger.warn('Failed to send performance metrics after all retries', 'metrics_transmission_failed', {
            metrics_count: Object.keys(metrics).length,
            endpoint,
            total_attempts: maxRetries,
            final_error: (error as Error).message,
            is_offline: !navigator.onLine
          }, error as Error);
          return; // Silent failure to not impact user experience
        } else {
          // Retry with exponential backoff
          const delay = baseDelay * Math.pow(2, attempt) + Math.random() * 500;
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }
  }
};

// 初始化性能优化
export const initializePerformanceOptimizations = async () => {
  const logger = await import('./logger').then(m => m.createLogger('HomeOptimization'));
  
  try {
    logger.info('Initializing performance optimizations', 'initialization_start', {
      page_url: window.location.href,
      user_agent: navigator.userAgent,
      connection_type: (navigator as NavigatorWithConnection)?.connection?.effectiveType || 'unknown'
    });

    // 预加载关键资源 (with error handling for each)
    const initializationTasks = [
      { name: 'critical_css', fn: preloadStrategies.preloadCriticalCSS },
      { name: 'fonts', fn: preloadStrategies.preloadFonts },
      { name: 'external_services', fn: preloadStrategies.preconnectExternalServices }
    ];

    await Promise.allSettled(
      initializationTasks.map(async (task) => {
        try {
          task.fn();
          logger.debug(`${task.name} preload completed`, 'preload_success', {
            task_name: task.name
          });
        } catch (error) {
          logger.warn(`${task.name} preload failed`, 'preload_error', {
            task_name: task.name,
            error_message: (error as Error).message
          }, error as Error);
        }
      })
    );

    // 预取下一页内容 (延迟执行，非阻塞)
    setTimeout(async () => {
      try {
        preloadStrategies.prefetchNextPageContent();
        logger.debug('Next page content prefetch initiated', 'prefetch_initiated');
      } catch (error) {
        logger.warn('Next page content prefetch failed', 'prefetch_error', {
          error_message: (error as Error).message
        }, error as Error);
      }
    }, 2000);

    // 初始化图片懒加载 (with error recovery)
    try {
      if ('IntersectionObserver' in window) {
        const imageObserver = imageOptimization.createResponsiveImageLoader();
        const lazyImages = document.querySelectorAll('img[data-src]');
        
        lazyImages.forEach(img => {
          try {
            imageObserver.observe(img);
          } catch (error) {
            logger.warn('Failed to observe image for lazy loading', 'lazy_load_error', {
              image_src: (img as HTMLImageElement).dataset.src,
              error_message: (error as Error).message
            }, error as Error);
            
            // Fallback: load image immediately
            const imgElement = img as HTMLImageElement;
            if (imgElement.dataset.src) {
              imgElement.src = imgElement.dataset.src;
            }
          }
        });

        logger.info('Image lazy loading initialized', 'lazy_load_init', {
          image_count: lazyImages.length
        });
      } else {
        logger.warn('IntersectionObserver not supported, loading all images immediately', 'fallback_load');
        // Fallback for browsers without IntersectionObserver
        document.querySelectorAll('img[data-src]').forEach(img => {
          const imgElement = img as HTMLImageElement;
          if (imgElement.dataset.src) {
            imgElement.src = imgElement.dataset.src;
          }
        });
      }
    } catch (error) {
      logger.error('Image lazy loading initialization failed', 'lazy_load_init_error', {
        error_message: (error as Error).message
      }, error as Error);
    }

    // 监控性能 (页面加载完成后，带超时保护)
    const setupPerformanceMonitoring = async () => {
      try {
        logger.info('Setting up performance monitoring', 'perf_monitoring_setup');
        
        // 设置超时保护，确保性能监控不会无限等待
        const monitoringTimeout = setTimeout(() => {
          logger.warn('Performance monitoring timed out', 'perf_monitoring_timeout');
        }, 30000); // 30 seconds max

        const metrics = await performanceMonitoring.measurePerformance();
        const budgetCheck = performanceMonitoring.checkPerformanceBudget(metrics);

        clearTimeout(monitoringTimeout);

        logger.info('Performance metrics collected', 'perf_metrics_collected', {
          metrics_count: Object.keys(metrics).length,
          budget_passed: budgetCheck.passed,
          failed_budgets: Object.entries(budgetCheck.results)
            .filter(([_, passed]) => !passed)
            .map(([metric]) => metric)
        });

        // 上报性能数据 (with retry logic built into reportPerformanceMetrics)
        const { navigation, resources, ...numericMetrics } = metrics;
        await performanceMonitoring.reportPerformanceMetrics({
          ...numericMetrics,
          budgetPassed: budgetCheck.passed ? 1 : 0,
          timestamp: Date.now(),
          page_load_complete: 1
        });

      } catch (error) {
        logger.error('Performance monitoring failed', 'perf_monitoring_error', {
          error_message: (error as Error).message,
          page_fully_loaded: document.readyState === 'complete'
        }, error as Error);
      }
    };

    // Set up performance monitoring with multiple trigger points
    if (document.readyState === 'complete') {
      // Page already loaded
      setTimeout(setupPerformanceMonitoring, 2000);
    } else {
      // Wait for page load
      window.addEventListener('load', () => {
        setTimeout(setupPerformanceMonitoring, 5000);
      });
      
      // Fallback: also trigger on DOM content loaded + delay
      document.addEventListener('DOMContentLoaded', () => {
        setTimeout(setupPerformanceMonitoring, 8000);
      });
    }

    logger.info('Performance optimizations initialization completed', 'initialization_complete');
    
    // Track initialization completion
    const analytics = await import('./analyticsTracking').then(m => m.analytics);
    analytics.track('performance_optimization_initialized', {
      initialization_time: performance.now(),
      page_url: window.location.href
    });

  } catch (error) {
    logger.error('Performance optimizations initialization failed', 'initialization_failed', {
      error_message: (error as Error).message,
      page_ready_state: document.readyState
    }, error as Error);
    
    // Even if initialization fails, we don't want to break the page
    // Continue with basic functionality
  }
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