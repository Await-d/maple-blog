/**
 * 首页性能优化 Hook
 * 提供性能监控、懒加载控制、资源预加载等功能
 */

import { useState, useEffect, useCallback, useRef } from 'react';
import {
  performanceMonitoring,
  preloadStrategies,
  imageOptimization,
  PERFORMANCE_TARGETS
} from '../utils/homeOptimization';

// 扩展性能接口
interface PerformanceEntryWithInput extends PerformanceEventTiming {
  processingStart: number;
}

interface PerformanceEntryWithLayoutShift extends PerformanceEntry {
  hadRecentInput?: boolean;
  value?: number;
}

// 性能指标类型
export interface PerformanceMetrics {
  lcp?: number;
  fid?: number;
  cls?: number;
  ttfb?: number;
  loadTime?: number;
  domContentLoaded?: number;
}

// 性能状态类型
export interface PerformanceState {
  metrics: PerformanceMetrics;
  isLoading: boolean;
  budgetPassed: boolean;
  errors: string[];
  optimizationLevel: 'low' | 'medium' | 'high';
}

// Hook配置选项
export interface UseHomePerformanceOptions {
  enableRealtimeMonitoring?: boolean;
  enablePerformanceBudget?: boolean;
  reportToAnalytics?: boolean;
  optimizationLevel?: 'low' | 'medium' | 'high';
}

/**
 * 首页性能优化 Hook
 */
export const useHomePerformance = (options: UseHomePerformanceOptions = {}) => {
  const {
    enableRealtimeMonitoring = true,
    enablePerformanceBudget = true,
    reportToAnalytics = true,
    optimizationLevel = 'high'
  } = options;

  // 状态管理
  const [performanceState, setPerformanceState] = useState<PerformanceState>({
    metrics: {},
    isLoading: true,
    budgetPassed: false,
    errors: [],
    optimizationLevel
  });

  // 引用
  const imageObserverRef = useRef<IntersectionObserver | null>(null);
  const performanceObserversRef = useRef<PerformanceObserver[]>([]);
  const metricsRef = useRef<PerformanceMetrics>({});

  // 初始化性能监控
  const initializePerformanceMonitoring = useCallback(() => {
    if (!enableRealtimeMonitoring) return;

    const observers: PerformanceObserver[] = [];

    try {
      // LCP监控
      const lcpObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const lastEntry = entries[entries.length - 1] as PerformanceEntry;
        if (lastEntry) {
          metricsRef.current.lcp = lastEntry.startTime;
          setPerformanceState(prev => ({
            ...prev,
            metrics: { ...prev.metrics, lcp: lastEntry.startTime }
          }));
        }
      });
      lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });
      observers.push(lcpObserver);

      // FID监控
      const fidObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries() as PerformanceEntryWithInput[];
        entries.forEach((entry) => {
          const fidValue = entry.processingStart - entry.startTime;
          metricsRef.current.fid = fidValue;
          setPerformanceState(prev => ({
            ...prev,
            metrics: { ...prev.metrics, fid: fidValue }
          }));
        });
      });
      fidObserver.observe({ entryTypes: ['first-input'] });
      observers.push(fidObserver);

      // CLS监控
      let clsValue = 0;
      const clsObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries() as PerformanceEntryWithLayoutShift[];
        for (const entry of entries) {
          if (!entry.hadRecentInput) {
            clsValue += entry.value || 0;
          }
        }
        metricsRef.current.cls = clsValue;
        setPerformanceState(prev => ({
          ...prev,
          metrics: { ...prev.metrics, cls: clsValue }
        }));
      });
      clsObserver.observe({ entryTypes: ['layout-shift'] });
      observers.push(clsObserver);

      // Navigation Timing监控
      const navigationObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries() as PerformanceNavigationTiming[];
        entries.forEach((entry) => {
          const ttfb = entry.responseStart - entry.requestStart;
          const loadTime = entry.loadEventEnd - entry.fetchStart;
          const domContentLoaded = entry.domContentLoadedEventEnd - entry.fetchStart;

          metricsRef.current = {
            ...metricsRef.current,
            ttfb,
            loadTime,
            domContentLoaded
          };

          setPerformanceState(prev => ({
            ...prev,
            metrics: {
              ...prev.metrics,
              ttfb,
              loadTime,
              domContentLoaded
            }
          }));
        });
      });
      navigationObserver.observe({ entryTypes: ['navigation'] });
      observers.push(navigationObserver);

      performanceObserversRef.current = observers;

    } catch (error) {
      console.warn('Performance monitoring setup failed:', error);
      setPerformanceState(prev => ({
        ...prev,
        errors: [...prev.errors, 'Performance monitoring setup failed']
      }));
    }
  }, [enableRealtimeMonitoring]);

  // 初始化图片懒加载
  const initializeImageLazyLoading = useCallback(() => {
    if (imageObserverRef.current) {
      imageObserverRef.current.disconnect();
    }

    imageObserverRef.current = imageOptimization.createResponsiveImageLoader();

    // 观察现有的懒加载图片
    const lazyImages = document.querySelectorAll('img[data-src]');
    lazyImages.forEach(img => {
      if (imageObserverRef.current) {
        imageObserverRef.current.observe(img);
      }
    });

    return imageObserverRef.current;
  }, []);

  // 预加载关键资源
  const preloadCriticalResources = useCallback(() => {
    if (optimizationLevel === 'low') return;

    try {
      preloadStrategies.preloadCriticalCSS();
      preloadStrategies.preloadFonts();
      preloadStrategies.preconnectExternalServices();

      // 高优化级别才预取下一页内容
      if (optimizationLevel === 'high') {
        setTimeout(() => {
          preloadStrategies.prefetchNextPageContent();
        }, 2000);
      }
    } catch (error) {
      console.warn('Resource preloading failed:', error);
      setPerformanceState(prev => ({
        ...prev,
        errors: [...prev.errors, 'Resource preloading failed']
      }));
    }
  }, [optimizationLevel]);

  // 检查性能预算
  const checkPerformanceBudget = useCallback(() => {
    if (!enablePerformanceBudget) return false;

    const budgetCheck = performanceMonitoring.checkPerformanceBudget(metricsRef.current);

    setPerformanceState(prev => ({
      ...prev,
      budgetPassed: budgetCheck.passed
    }));

    return budgetCheck.passed;
  }, [enablePerformanceBudget]);

  // 上报性能数据
  const reportPerformanceData = useCallback(() => {
    if (!reportToAnalytics) return;

    const metrics = metricsRef.current;
    const budgetPassed = checkPerformanceBudget();

    performanceMonitoring.reportPerformanceMetrics({
      ...metrics,
      budgetPassed: budgetPassed ? 1 : 0,
      optimizationLevel: optimizationLevel === 'high' ? 3 : optimizationLevel === 'medium' ? 2 : 1,
      timestamp: Date.now()
    });
  }, [reportToAnalytics, optimizationLevel, checkPerformanceBudget]);

  // 获取性能评分
  const getPerformanceScore = useCallback(() => {
    const metrics = metricsRef.current;
    let score = 100;

    // LCP评分 (0-40分)
    if (metrics.lcp) {
      if (metrics.lcp > 4000) score -= 40;
      else if (metrics.lcp > PERFORMANCE_TARGETS.LCP) score -= 20;
    }

    // FID评分 (0-25分)
    if (metrics.fid) {
      if (metrics.fid > 300) score -= 25;
      else if (metrics.fid > PERFORMANCE_TARGETS.FID) score -= 10;
    }

    // CLS评分 (0-25分)
    if (metrics.cls) {
      if (metrics.cls > 0.25) score -= 25;
      else if (metrics.cls > PERFORMANCE_TARGETS.CLS) score -= 10;
    }

    // TTFB评分 (0-10分)
    if (metrics.ttfb) {
      if (metrics.ttfb > 1800) score -= 10;
      else if (metrics.ttfb > PERFORMANCE_TARGETS.TTFB) score -= 5;
    }

    return Math.max(0, score);
  }, []);

  // 获取优化建议
  const getOptimizationSuggestions = useCallback(() => {
    const metrics = metricsRef.current;
    const suggestions: string[] = [];

    if (metrics.lcp && metrics.lcp > PERFORMANCE_TARGETS.LCP) {
      suggestions.push('Optimize largest contentful paint by reducing image sizes or implementing better caching');
    }

    if (metrics.fid && metrics.fid > PERFORMANCE_TARGETS.FID) {
      suggestions.push('Reduce first input delay by optimizing JavaScript execution and using code splitting');
    }

    if (metrics.cls && metrics.cls > PERFORMANCE_TARGETS.CLS) {
      suggestions.push('Fix cumulative layout shift by reserving space for images and avoiding dynamic content insertion');
    }

    if (metrics.ttfb && metrics.ttfb > PERFORMANCE_TARGETS.TTFB) {
      suggestions.push('Improve time to first byte by optimizing server response time and using CDN');
    }

    return suggestions;
  }, []);

  // 重新观察新的懒加载图片
  const observeLazyImage = useCallback((img: HTMLImageElement) => {
    if (imageObserverRef.current) {
      imageObserverRef.current.observe(img);
    }
  }, []);

  // 停止观察图片
  const unobserveLazyImage = useCallback((img: HTMLImageElement) => {
    if (imageObserverRef.current) {
      imageObserverRef.current.unobserve(img);
    }
  }, []);

  // 手动触发性能测量
  const measurePerformance = useCallback(async () => {
    setPerformanceState(prev => ({ ...prev, isLoading: true }));

    try {
      const metrics = await performanceMonitoring.measurePerformance();
      metricsRef.current = { ...metricsRef.current, ...metrics };

      setPerformanceState(prev => ({
        ...prev,
        metrics: { ...prev.metrics, ...metrics },
        isLoading: false
      }));

      return metrics;
    } catch (error) {
      console.error('Performance measurement failed:', error);
      setPerformanceState(prev => ({
        ...prev,
        isLoading: false,
        errors: [...prev.errors, 'Performance measurement failed']
      }));
      throw error;
    }
  }, []);

  // 初始化效果
  useEffect(() => {
    initializePerformanceMonitoring();
    initializeImageLazyLoading();
    preloadCriticalResources();

    // 页面加载完成后进行完整测量
    const handleLoad = () => {
      setTimeout(() => {
        measurePerformance().then(() => {
          checkPerformanceBudget();
          reportPerformanceData();
        });
      }, 3000);
    };

    if (document.readyState === 'complete') {
      handleLoad();
    } else {
      window.addEventListener('load', handleLoad);
    }

    // 页面卸载时清理
    const handleBeforeUnload = () => {
      reportPerformanceData();
    };
    window.addEventListener('beforeunload', handleBeforeUnload);

    return () => {
      window.removeEventListener('load', handleLoad);
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [
    initializePerformanceMonitoring,
    initializeImageLazyLoading,
    preloadCriticalResources,
    measurePerformance,
    checkPerformanceBudget,
    reportPerformanceData
  ]);

  // 清理效果
  useEffect(() => {
    return () => {
      // 清理性能观察者
      performanceObserversRef.current.forEach(observer => {
        try {
          observer.disconnect();
        } catch (error) {
          console.warn('Failed to disconnect performance observer:', error);
        }
      });

      // 清理图片观察者
      if (imageObserverRef.current) {
        imageObserverRef.current.disconnect();
      }
    };
  }, []);

  // 状态完成标记
  useEffect(() => {
    if (performanceState.isLoading && Object.keys(performanceState.metrics).length > 0) {
      setPerformanceState(prev => ({ ...prev, isLoading: false }));
    }
  }, [performanceState.metrics, performanceState.isLoading]);

  return {
    // 状态
    metrics: performanceState.metrics,
    isLoading: performanceState.isLoading,
    budgetPassed: performanceState.budgetPassed,
    errors: performanceState.errors,
    optimizationLevel: performanceState.optimizationLevel,

    // 方法
    measurePerformance,
    checkPerformanceBudget,
    reportPerformanceData,
    observeLazyImage,
    unobserveLazyImage,
    getPerformanceScore,
    getOptimizationSuggestions,

    // 工具方法
    preloadCriticalResources,
    initializeImageLazyLoading,
  };
};