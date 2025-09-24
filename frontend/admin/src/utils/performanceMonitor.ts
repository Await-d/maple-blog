/**
 * Frontend Performance Monitor for Admin Dashboard
 * Provides comprehensive performance monitoring, analysis, and optimization insights
 */

import React from 'react';

// Performance Metrics Types
export interface PerformanceMetrics {
  pageLoad: PageLoadMetrics;
  runtime: RuntimeMetrics;
  api: ApiMetrics;
  memory: MemoryMetrics;
  vitals: WebVitalsMetrics;
}

export interface PageLoadMetrics {
  domContentLoaded: number;
  firstContentfulPaint: number;
  largestContentfulPaint: number;
  firstInputDelay: number;
  cumulativeLayoutShift: number;
  timeToInteractive: number;
  totalBlockingTime: number;
}

export interface RuntimeMetrics {
  componentRenderTimes: Map<string, ComponentRenderMetrics>;
  reactRenderCount: number;
  virtualDomUpdates: number;
  memoryLeaks: MemoryLeakDetection[];
  javascriptErrors: ErrorMetrics[];
}

export interface ComponentRenderMetrics {
  componentName: string;
  renderTime: number;
  reRenderCount: number;
  propsChangeCount: number;
  lastRenderTimestamp: number;
  averageRenderTime: number;
}

export interface ApiMetrics {
  requestTimes: Map<string, ApiRequestMetrics>;
  totalRequests: number;
  failedRequests: number;
  averageResponseTime: number;
  slowestEndpoints: Array<{ endpoint: string; time: number }>;
}

export interface ApiRequestMetrics {
  endpoint: string;
  method: string;
  responseTime: number;
  status: number;
  timestamp: number;
  payload: number; // Size in bytes
}

export interface MemoryMetrics {
  heapUsed: number;
  heapTotal: number;
  heapLimit: number;
  memoryUsage: number; // Percentage
  gcCount: number;
  memoryLeaks: MemoryLeakDetection[];
}

export interface MemoryLeakDetection {
  objectType: string;
  count: number;
  size: number;
  timestamp: number;
  stack?: string;
}

export interface WebVitalsMetrics {
  lcp: number; // Largest Contentful Paint
  fid: number; // First Input Delay
  cls: number; // Cumulative Layout Shift
  fcp: number; // First Contentful Paint
  ttfb: number; // Time to First Byte
}

export interface ErrorMetrics {
  message: string;
  stack: string;
  timestamp: number;
  url: string;
  component?: string;
}

export interface PerformanceThresholds {
  pageLoad: {
    excellent: number;
    good: number;
    poor: number;
  };
  api: {
    excellent: number;
    good: number;
    poor: number;
  };
  memory: {
    warningThreshold: number;
    criticalThreshold: number;
  };
}

export interface PerformanceAlert {
  type: 'warning' | 'critical' | 'info';
  metric: string;
  value: number;
  threshold: number;
  message: string;
  timestamp: number;
  suggestion?: string;
}

export interface PerformanceInsight {
  category: 'performance' | 'memory' | 'api' | 'rendering';
  severity: 'low' | 'medium' | 'high';
  title: string;
  description: string;
  impact: string;
  recommendations: string[];
  metrics: Record<string, number>;
}

interface PerformanceEntryWithProcessing extends PerformanceEntry {
  processingStart: number;
}

interface LayoutShiftEntry extends PerformanceEntry {
  value: number;
  hadRecentInput: boolean;
}

interface PerformanceMemory {
  usedJSHeapSize: number;
  totalJSHeapSize: number;
  jsHeapSizeLimit: number;
}

interface ExtendedPerformance extends Performance {
  memory?: PerformanceMemory;
}

// Performance Monitor Class
class PerformanceMonitor {
  private metrics: PerformanceMetrics;
  private observers: Map<string, PerformanceObserver> = new Map();
  private thresholds: PerformanceThresholds;
  private alerts: PerformanceAlert[] = [];
  private insights: PerformanceInsight[] = [];
  private isMonitoring = false;
  private reportingInterval: number | null = null;
  private baselineMetrics: PerformanceMetrics | null = null;

  constructor() {
    this.metrics = this.initializeMetrics();
    this.thresholds = this.getDefaultThresholds();
    this.setupErrorHandling();
  }

  // Initialize Monitoring
  public startMonitoring(config?: {
    reportingInterval?: number;
    enableRealTimeAlerts?: boolean;
    enableBaseline?: boolean;
  }): void {
    if (this.isMonitoring) return;

    this.isMonitoring = true;
    this.setupPerformanceObservers();
    this.setupApiMonitoring();
    this.setupMemoryMonitoring();
    this.setupComponentMonitoring();

    if (config?.reportingInterval) {
      this.startPeriodicReporting(config.reportingInterval);
    }

    if (config?.enableBaseline) {
      this.recordBaseline();
    }

    console.log('🚀 Performance Monitor started');
  }

  public stopMonitoring(): void {
    this.isMonitoring = false;
    this.observers.forEach(observer => observer.disconnect());
    this.observers.clear();

    if (this.reportingInterval) {
      clearInterval(this.reportingInterval);
      this.reportingInterval = null;
    }

    console.log('🛑 Performance Monitor stopped');
  }

  // Core Metrics Collection
  private initializeMetrics(): PerformanceMetrics {
    return {
      pageLoad: {
        domContentLoaded: 0,
        firstContentfulPaint: 0,
        largestContentfulPaint: 0,
        firstInputDelay: 0,
        cumulativeLayoutShift: 0,
        timeToInteractive: 0,
        totalBlockingTime: 0,
      },
      runtime: {
        componentRenderTimes: new Map(),
        reactRenderCount: 0,
        virtualDomUpdates: 0,
        memoryLeaks: [],
        javascriptErrors: [],
      },
      api: {
        requestTimes: new Map(),
        totalRequests: 0,
        failedRequests: 0,
        averageResponseTime: 0,
        slowestEndpoints: [],
      },
      memory: {
        heapUsed: 0,
        heapTotal: 0,
        heapLimit: 0,
        memoryUsage: 0,
        gcCount: 0,
        memoryLeaks: [],
      },
      vitals: {
        lcp: 0,
        fid: 0,
        cls: 0,
        fcp: 0,
        ttfb: 0,
      },
    };
  }

  private getDefaultThresholds(): PerformanceThresholds {
    return {
      pageLoad: {
        excellent: 1500,
        good: 2500,
        poor: 4000,
      },
      api: {
        excellent: 200,
        good: 500,
        poor: 1000,
      },
      memory: {
        warningThreshold: 70,
        criticalThreshold: 90,
      },
    };
  }

  // Performance Observers Setup
  private setupPerformanceObservers(): void {
    // Web Vitals Observer
    if ('PerformanceObserver' in window) {
      // LCP Observer
      const lcpObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const lastEntry = entries[entries.length - 1] as PerformanceEntry;
        this.metrics.vitals.lcp = lastEntry.startTime;
        this.metrics.pageLoad.largestContentfulPaint = lastEntry.startTime;
      });
      lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });
      this.observers.set('lcp', lcpObserver);

      // FID Observer
      const fidObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        entries.forEach((entry) => {
          const fidEntry = entry as PerformanceEntryWithProcessing;
          this.metrics.vitals.fid = fidEntry.processingStart - fidEntry.startTime;
          this.metrics.pageLoad.firstInputDelay = fidEntry.processingStart - fidEntry.startTime;
        });
      });
      fidObserver.observe({ entryTypes: ['first-input'] });
      this.observers.set('fid', fidObserver);

      // CLS Observer
      const clsObserver = new PerformanceObserver((list) => {
        let clsValue = 0;
        const entries = list.getEntries();
        entries.forEach((entry) => {
          const clsEntry = entry as LayoutShiftEntry;
          if (!clsEntry.hadRecentInput) {
            clsValue += clsEntry.value;
          }
        });
        this.metrics.vitals.cls = clsValue;
        this.metrics.pageLoad.cumulativeLayoutShift = clsValue;
      });
      clsObserver.observe({ entryTypes: ['layout-shift'] });
      this.observers.set('cls', clsObserver);

      // Paint Observer
      const paintObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        entries.forEach((entry) => {
          if (entry.name === 'first-contentful-paint') {
            this.metrics.vitals.fcp = entry.startTime;
            this.metrics.pageLoad.firstContentfulPaint = entry.startTime;
          }
        });
      });
      paintObserver.observe({ entryTypes: ['paint'] });
      this.observers.set('paint', paintObserver);
    }

    // Navigation Timing
    this.collectNavigationTiming();
  }

  private collectNavigationTiming(): void {
    if (performance.timing) {
      const timing = performance.timing;
      this.metrics.pageLoad.domContentLoaded = timing.domContentLoadedEventEnd - timing.navigationStart;
      this.metrics.vitals.ttfb = timing.responseStart - timing.navigationStart;
    }
  }

  // API Monitoring
  private setupApiMonitoring(): void {
    const originalFetch = window.fetch;

    window.fetch = async (...args) => {
      const startTime = performance.now();
      const url = typeof args[0] === 'string' ? args[0] : (args[0] instanceof Request ? args[0].url : String(args[0]));
      const method = args[1]?.method || 'GET';

      try {
        const response = await originalFetch(...args);
        const endTime = performance.now();
        const responseTime = endTime - startTime;

        this.recordApiMetrics({
          endpoint: url,
          method,
          responseTime,
          status: response.status,
          timestamp: Date.now(),
          payload: 0, // Would need response.size if available
        });

        return response;
      } catch (error) {
        const endTime = performance.now();
        const responseTime = endTime - startTime;

        this.recordApiMetrics({
          endpoint: url,
          method,
          responseTime,
          status: 0,
          timestamp: Date.now(),
          payload: 0,
        });

        this.metrics.api.failedRequests++;
        throw error;
      }
    };
  }

  private recordApiMetrics(metrics: ApiRequestMetrics): void {
    this.metrics.api.requestTimes.set(
      `${metrics.method}:${metrics.endpoint}`,
      metrics
    );
    this.metrics.api.totalRequests++;

    // Update average response time
    const totalTime = Array.from(this.metrics.api.requestTimes.values())
      .reduce((sum, req) => sum + req.responseTime, 0);
    this.metrics.api.averageResponseTime = totalTime / this.metrics.api.totalRequests;

    // Update slowest endpoints
    this.updateSlowestEndpoints(metrics);

    // Check for alerts
    this.checkApiPerformanceAlerts(metrics);
  }

  private updateSlowestEndpoints(metrics: ApiRequestMetrics): void {
    const slowest = this.metrics.api.slowestEndpoints;
    slowest.push({ endpoint: metrics.endpoint, time: metrics.responseTime });
    slowest.sort((a, b) => b.time - a.time);
    this.metrics.api.slowestEndpoints = slowest.slice(0, 10);
  }

  // Memory Monitoring
  private setupMemoryMonitoring(): void {
    setInterval(() => {
      const extendedPerformance = performance as ExtendedPerformance;
      if ('memory' in extendedPerformance && extendedPerformance.memory) {
        const memory = extendedPerformance.memory;
        this.metrics.memory.heapUsed = memory.usedJSHeapSize;
        this.metrics.memory.heapTotal = memory.totalJSHeapSize;
        this.metrics.memory.heapLimit = memory.jsHeapSizeLimit;
        this.metrics.memory.memoryUsage = (memory.usedJSHeapSize / memory.jsHeapSizeLimit) * 100;

        this.checkMemoryAlerts();
      }
    }, 5000);
  }

  // Component Monitoring
  private setupComponentMonitoring(): void {
    // This would integrate with React DevTools or custom hooks
    // For now, providing interface for external integration
  }

  public recordComponentRender(componentName: string, renderTime: number): void {
    const existing = this.metrics.runtime.componentRenderTimes.get(componentName);

    if (existing) {
      existing.reRenderCount++;
      existing.renderTime = renderTime;
      existing.lastRenderTimestamp = Date.now();
      existing.averageRenderTime = (existing.averageRenderTime + renderTime) / existing.reRenderCount;
    } else {
      this.metrics.runtime.componentRenderTimes.set(componentName, {
        componentName,
        renderTime,
        reRenderCount: 1,
        propsChangeCount: 0,
        lastRenderTimestamp: Date.now(),
        averageRenderTime: renderTime,
      });
    }

    this.metrics.runtime.reactRenderCount++;
  }

  // Error Handling
  private setupErrorHandling(): void {
    window.addEventListener('error', (event) => {
      this.metrics.runtime.javascriptErrors.push({
        message: event.message,
        stack: event.error?.stack || '',
        timestamp: Date.now(),
        url: event.filename,
      });
    });

    window.addEventListener('unhandledrejection', (event) => {
      this.metrics.runtime.javascriptErrors.push({
        message: event.reason?.message || 'Unhandled Promise Rejection',
        stack: event.reason?.stack || '',
        timestamp: Date.now(),
        url: window.location.href,
      });
    });
  }

  // Alert System
  private checkApiPerformanceAlerts(metrics: ApiRequestMetrics): void {
    if (metrics.responseTime > this.thresholds.api.poor) {
      this.addAlert({
        type: 'critical',
        metric: 'API Response Time',
        value: metrics.responseTime,
        threshold: this.thresholds.api.poor,
        message: `Slow API response: ${metrics.endpoint}`,
        timestamp: Date.now(),
        suggestion: 'Consider optimizing the API endpoint or implementing caching',
      });
    }
  }

  private checkMemoryAlerts(): void {
    if (this.metrics.memory.memoryUsage > this.thresholds.memory.criticalThreshold) {
      this.addAlert({
        type: 'critical',
        metric: 'Memory Usage',
        value: this.metrics.memory.memoryUsage,
        threshold: this.thresholds.memory.criticalThreshold,
        message: 'Critical memory usage detected',
        timestamp: Date.now(),
        suggestion: 'Check for memory leaks and optimize component lifecycle',
      });
    }
  }

  private addAlert(alert: PerformanceAlert): void {
    this.alerts.push(alert);

    // Keep only recent alerts
    const oneHourAgo = Date.now() - 60 * 60 * 1000;
    this.alerts = this.alerts.filter(a => a.timestamp > oneHourAgo);

    // Emit alert event
    this.emitAlertEvent(alert);
  }

  private emitAlertEvent(alert: PerformanceAlert): void {
    window.dispatchEvent(new CustomEvent('performanceAlert', { detail: alert }));
  }

  // Performance Analysis
  public analyzePerformance(): PerformanceInsight[] {
    const insights: PerformanceInsight[] = [];

    // Page Load Analysis
    if (this.metrics.pageLoad.largestContentfulPaint > this.thresholds.pageLoad.poor) {
      insights.push({
        category: 'performance',
        severity: 'high',
        title: 'Slow Page Load Performance',
        description: 'Page load times are above acceptable thresholds',
        impact: 'Users may experience poor loading performance',
        recommendations: [
          'Optimize images and assets',
          'Implement code splitting',
          'Use lazy loading for components',
          'Optimize bundle size',
        ],
        metrics: {
          lcp: this.metrics.pageLoad.largestContentfulPaint,
          fcp: this.metrics.pageLoad.firstContentfulPaint,
        },
      });
    }

    // Memory Analysis
    if (this.metrics.memory.memoryUsage > this.thresholds.memory.warningThreshold) {
      insights.push({
        category: 'memory',
        severity: 'medium',
        title: 'High Memory Usage',
        description: 'Application is using significant memory resources',
        impact: 'May cause performance degradation on low-memory devices',
        recommendations: [
          'Review component lifecycle methods',
          'Check for memory leaks',
          'Optimize data structures',
          'Implement proper cleanup in useEffect',
        ],
        metrics: {
          memoryUsage: this.metrics.memory.memoryUsage,
          heapUsed: this.metrics.memory.heapUsed,
        },
      });
    }

    // API Performance Analysis
    if (this.metrics.api.averageResponseTime > this.thresholds.api.good) {
      insights.push({
        category: 'api',
        severity: 'medium',
        title: 'Slow API Performance',
        description: 'API responses are slower than optimal',
        impact: 'Reduced user experience due to slow data loading',
        recommendations: [
          'Implement API response caching',
          'Optimize database queries',
          'Use pagination for large datasets',
          'Consider API endpoint consolidation',
        ],
        metrics: {
          averageResponseTime: this.metrics.api.averageResponseTime,
          totalRequests: this.metrics.api.totalRequests,
        },
      });
    }

    this.insights = insights;
    return insights;
  }

  // Baseline and Regression Detection
  private recordBaseline(): void {
    setTimeout(() => {
      this.baselineMetrics = JSON.parse(JSON.stringify(this.metrics));
      console.log('📊 Performance baseline recorded');
    }, 30000); // Wait 30 seconds for stable metrics
  }

  public compareToBaseline(): Record<string, number> {
    if (!this.baselineMetrics) return {};

    return {
      pageLoadDelta: this.metrics.pageLoad.largestContentfulPaint - this.baselineMetrics.pageLoad.largestContentfulPaint,
      apiResponseDelta: this.metrics.api.averageResponseTime - this.baselineMetrics.api.averageResponseTime,
      memoryDelta: this.metrics.memory.memoryUsage - this.baselineMetrics.memory.memoryUsage,
    };
  }

  // Reporting
  private startPeriodicReporting(interval: number): void {
    this.reportingInterval = window.setInterval(() => {
      this.generatePerformanceReport();
    }, interval);
  }

  public generatePerformanceReport(): PerformanceReport {
    const insights = this.analyzePerformance();
    const regressions = this.compareToBaseline();

    return {
      timestamp: Date.now(),
      metrics: this.metrics,
      alerts: this.alerts,
      insights,
      regressions,
      recommendations: this.generateRecommendations(),
    };
  }

  private generateRecommendations(): string[] {
    const recommendations: string[] = [];
    const insights = this.analyzePerformance();

    insights.forEach(insight => {
      recommendations.push(...insight.recommendations);
    });

    return [...new Set(recommendations)]; // Remove duplicates
  }

  // Public API
  public getMetrics(): PerformanceMetrics {
    return this.metrics;
  }

  public getAlerts(): PerformanceAlert[] {
    return this.alerts;
  }

  public getInsights(): PerformanceInsight[] {
    return this.insights;
  }

  public clearAlerts(): void {
    this.alerts = [];
  }

  public setThresholds(thresholds: Partial<PerformanceThresholds>): void {
    this.thresholds = { ...this.thresholds, ...thresholds };
  }

  public exportMetrics(): string {
    return JSON.stringify({
      metrics: this.metrics,
      alerts: this.alerts,
      insights: this.insights,
      timestamp: Date.now(),
    }, null, 2);
  }
}

// Performance Report Interface
export interface PerformanceReport {
  timestamp: number;
  metrics: PerformanceMetrics;
  alerts: PerformanceAlert[];
  insights: PerformanceInsight[];
  regressions: Record<string, number>;
  recommendations: string[];
}

// React Hooks for Performance Monitoring
export function usePerformanceMonitor() {
  const monitor = React.useRef<PerformanceMonitor | null>(null);

  React.useEffect(() => {
    monitor.current = new PerformanceMonitor();
    monitor.current.startMonitoring({
      reportingInterval: 60000, // Report every minute
      enableRealTimeAlerts: true,
      enableBaseline: true,
    });

    return () => {
      monitor.current?.stopMonitoring();
    };
  }, []);

  return monitor.current;
}

export function useComponentPerformance(componentName: string) {
  const monitor = usePerformanceMonitor();

  React.useEffect(() => {
    const startTime = performance.now();

    return () => {
      const endTime = performance.now();
      monitor?.recordComponentRender(componentName, endTime - startTime);
    };
  });
}

// Create singleton instance
const performanceMonitor = new PerformanceMonitor();

export default performanceMonitor;

// Auto-start monitoring in development
if (process.env.NODE_ENV === 'development') {
  performanceMonitor.startMonitoring({
    reportingInterval: 30000,
    enableRealTimeAlerts: true,
    enableBaseline: true,
  });
}