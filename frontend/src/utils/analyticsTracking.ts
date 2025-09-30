/**
 * 分析追踪工具
 * 实现用户行为追踪、性能监控、A/B测试等分析功能
 */

// 事件类型定义
export interface AnalyticsEvent {
  name: string;
  properties?: Record<string, string | number | boolean | null>;
  timestamp?: number;
  userId?: string;
  sessionId?: string;
  pageUrl?: string;
  referrer?: string;
}

// 用户属性
export interface UserProperties {
  userId?: string;
  isAuthenticated: boolean;
  userRole?: string;
  deviceType: string;
  browserName: string;
  operatingSystem: string;
  screenResolution: string;
  language: string;
  timezone: string;
  firstVisit: boolean;
  sessionCount: number;
}

// 页面性能指标
export interface PerformanceMetrics {
  // Core Web Vitals
  lcp?: number; // Largest Contentful Paint
  fid?: number; // First Input Delay
  cls?: number; // Cumulative Layout Shift

  // 其他性能指标
  ttfb?: number; // Time to First Byte
  fcp?: number;  // First Contentful Paint
  loadTime?: number;
  domContentLoaded?: number;

  // 资源加载
  resourceCount?: number;
  totalResourceSize?: number;
  criticalResourceTime?: number;

  // 自定义指标
  homePageRenderTime?: number;
  componentLoadTimes?: Record<string, number>;
}

// A/B测试配置
export interface ABTestConfig {
  testId: string;
  variant: string;
  isActive: boolean;
  startDate: string;
  endDate: string;
  trafficAllocation: number; // 0-1 之间
}

// 错误信息
export interface ErrorInfo {
  message: string;
  stack?: string;
  componentStack?: string;
  url: string;
  lineNumber?: number;
  columnNumber?: number;
  userAgent: string;
  timestamp: number;
  userId?: string;
  sessionId: string;
  breadcrumbs?: string[];
}

/**
 * 分析追踪管理器
 */
export class AnalyticsTracker {
  private static instance: AnalyticsTracker;
  private sessionId: string;
  private userId?: string;
  private userProperties: Partial<UserProperties> = {};
  private eventQueue: AnalyticsEvent[] = [];
  private performanceObservers: PerformanceObserver[] = [];
  private abTests: Map<string, ABTestConfig> = new Map();
  private isInitialized = false;
  private flushInterval?: NodeJS.Timeout;
  private apiEndpoint = '/api/analytics/events';

  private constructor() {
    this.sessionId = this.generateSessionId();
    this.collectUserProperties();
    // Setup performance observers asynchronously
    this.setupPerformanceObservers().catch(error => {
      console.warn('Failed to setup performance observers:', error);
    });
    this.startEventFlushTimer();
  }

  static getInstance(): AnalyticsTracker {
    if (!AnalyticsTracker.instance) {
      AnalyticsTracker.instance = new AnalyticsTracker();
    }
    return AnalyticsTracker.instance;
  }

  /**
   * 初始化分析追踪
   */
  async initialize(config: {
    apiEndpoint?: string;
    userId?: string;
    enablePerformanceTracking?: boolean;
    enableErrorTracking?: boolean;
    flushInterval?: number;
  } = {}) {
    if (this.isInitialized) {
      const logger = await import('./logger').then(m => m.createLogger('AnalyticsTracker'));
      logger.warn('Analytics tracker already initialized', 'initialization_warning', {
        attempted_config: config
      });
      return;
    }

    this.apiEndpoint = config.apiEndpoint || this.apiEndpoint;
    this.userId = config.userId;

    if (config.enablePerformanceTracking !== false) {
      this.enablePerformanceTracking();
    }

    if (config.enableErrorTracking !== false) {
      this.enableErrorTracking();
    }

    if (config.flushInterval) {
      this.stopEventFlushTimer();
      this.startEventFlushTimer(config.flushInterval);
    }

    // 追踪页面加载
    this.trackPageLoad();

    this.isInitialized = true;
  }

  /**
   * 追踪自定义事件
   */
  track(eventName: string, properties: Record<string, string | number | boolean | null> = {}) {
    const event: AnalyticsEvent = {
      name: eventName,
      properties: {
        ...properties,
        ...this.getContextProperties()
      },
      timestamp: Date.now(),
      userId: this.userId,
      sessionId: this.sessionId,
      pageUrl: window.location.href,
      referrer: document.referrer || undefined
    };

    this.eventQueue.push(event);

    // 立即发送高优先级事件
    if (this.isHighPriorityEvent(eventName)) {
      this.flush();
    }
  }

  /**
   * 追踪页面浏览
   */
  trackPageView(pageName?: string, properties: Record<string, string | number | boolean | null> = {}) {
    this.track('page_view', {
      page_name: pageName || this.getPageName(),
      page_title: document.title,
      page_url: window.location.href,
      page_path: window.location.pathname,
      page_search: window.location.search,
      page_hash: window.location.hash,
      ...properties
    });
  }

  /**
   * 追踪用户交互
   */
  trackInteraction(element: string, action: string, properties: Record<string, string | number | boolean | null> = {}) {
    this.track('user_interaction', {
      element,
      action,
      ...properties
    });
  }

  /**
   * 追踪转化事件
   */
  trackConversion(goalName: string, value?: number, properties: Record<string, string | number | boolean | null> = {}) {
    this.track('conversion', {
      goal_name: goalName,
      goal_value: value || null,
      ...properties
    });
  }

  /**
   * 追踪搜索事件
   */
  trackSearch(query: string, results: number, filters: Record<string, string | number | boolean | null> = {}) {
    this.track('search', {
      search_query: query,
      search_results: results,
      search_filters: JSON.stringify(filters)
    });
  }

  /**
   * 追踪内容互动
   */
  trackContentEngagement(contentType: string, contentId: string, action: string, properties: Record<string, string | number | boolean | null> = {}) {
    this.track('content_engagement', {
      content_type: contentType,
      content_id: contentId,
      engagement_action: action,
      ...properties
    });
  }

  /**
   * 追踪性能指标
   */
  trackPerformance(metrics: PerformanceMetrics) {
    this.track('performance_metrics', {
      metrics: JSON.stringify(metrics),
      page_url: window.location.href,
      user_agent: navigator.userAgent,
      connection_type: (navigator as Navigator & { connection?: { effectiveType?: string } })?.connection?.effectiveType || 'unknown'
    });
  }

  /**
   * 追踪错误
   */
  trackError(error: ErrorInfo) {
    this.track('error', {
      error_message: error.message,
      error_stack: error.stack || null,
      error_url: error.url || null,
      error_line: error.lineNumber || null,
      error_column: error.columnNumber || null,
      component_stack: error.componentStack || null,
      breadcrumbs: error.breadcrumbs ? JSON.stringify(error.breadcrumbs) : null,
      user_agent: error.userAgent || null
    });

    // 错误事件立即发送
    this.flush();
  }

  /**
   * 设置用户属性
   */
  setUserProperties(properties: Partial<UserProperties>) {
    this.userProperties = { ...this.userProperties, ...properties };
    this.userId = properties.userId || this.userId;
  }

  /**
   * 设置用户ID
   */
  setUserId(userId: string) {
    this.userId = userId;
    this.userProperties.userId = userId;
    this.userProperties.isAuthenticated = true;
  }

  /**
   * 获取当前用户ID
   */
  getUserId(): string | undefined {
    return this.userId;
  }

  /**
   * A/B测试相关方法
   */

  // 注册A/B测试
  registerABTest(config: ABTestConfig) {
    this.abTests.set(config.testId, config);
  }

  // 获取A/B测试变体
  getABTestVariant(testId: string): string | null {
    const test = this.abTests.get(testId);
    if (!test || !test.isActive) {
      return null;
    }

    // 基于用户ID或会话ID的一致性哈希
    const identifier = this.userId || this.sessionId;
    const hash = this.hashString(identifier + testId);
    const bucket = hash % 100;

    return bucket < test.trafficAllocation * 100 ? test.variant : null;
  }

  // 追踪A/B测试曝光
  trackABTestExposure(testId: string, variant: string) {
    this.track('ab_test_exposure', {
      test_id: testId,
      variant,
      user_id: this.userId || null,
      session_id: this.sessionId
    });
  }

  /**
   * 立即发送所有队列中的事件
   */
  async flush() {
    if (this.eventQueue.length === 0) {
      return;
    }

    const events = [...this.eventQueue];
    this.eventQueue = [];

    try {
      await this.sendEvents(events);
    } catch (error) {
      const logger = await import('./logger').then(m => m.createLogger('AnalyticsTracker'));
      logger.error('Failed to send analytics events', 'event_transmission_error', {
        event_count: events.length,
        queue_size_before: this.eventQueue.length,
        error_message: (error as Error).message,
        endpoint: this.apiEndpoint,
        retry_count: events.length > 0 ? (events[0] as AnalyticsEvent & { retryCount?: number }).retryCount || 0 : 0
      }, error as Error);

      // 发送失败时重新入队（但限制重试次数）
      const eventsWithRetry = events.slice(0, 50).map(event => ({
        ...event,
        retryCount: ((event as AnalyticsEvent & { retryCount?: number }).retryCount || 0) + 1,
        lastFailedAt: Date.now()
      })).filter(event => (event as AnalyticsEvent & { retryCount?: number }).retryCount! <= 3); // 最多重试3次
      
      this.eventQueue.unshift(...eventsWithRetry);
    }
  }

  /**
   * 销毁追踪器
   */
  destroy() {
    this.stopEventFlushTimer();
    this.disconnectPerformanceObservers();
    this.flush(); // 发送剩余事件
    this.eventQueue = [];
  }

  // 私有方法

  private generateSessionId(): string {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  }

  private collectUserProperties() {
    const now = new Date();

    this.userProperties = {
      deviceType: this.getDeviceType(),
      browserName: this.getBrowserName(),
      operatingSystem: this.getOperatingSystem(),
      screenResolution: `${window.screen.width}x${window.screen.height}`,
      language: navigator.language,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      firstVisit: !localStorage.getItem('maple_blog_visitor'),
      sessionCount: this.getSessionCount(),
      isAuthenticated: false
    };

    // 标记访问
    if (this.userProperties.firstVisit) {
      localStorage.setItem('maple_blog_visitor', now.toISOString());
    }
  }

  private getContextProperties() {
    return {
      timestamp: Date.now(),
      page_url: window.location.href,
      page_title: document.title,
      referrer: document.referrer || null,
      user_agent: navigator.userAgent,
      viewport_size: `${window.innerWidth}x${window.innerHeight}`,
      screen_size: `${window.screen.width}x${window.screen.height}`,
      device_pixel_ratio: window.devicePixelRatio,
      connection_type: (navigator as Navigator & { connection?: { effectiveType?: string } })?.connection?.effectiveType || 'unknown',
      language: navigator.language,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      ...this.userProperties
    };
  }

  private getPageName(): string {
    const path = window.location.pathname;
    const segments = path.split('/').filter(Boolean);

    if (segments.length === 0) return 'home';
    if (segments[0] === 'blog') return 'blog_list';
    if (segments[0] === 'posts') return 'post_detail';
    if (segments[0] === 'categories') return 'category_page';
    if (segments[0] === 'tags') return 'tag_page';
    if (segments[0] === 'archive') return 'archive_page';
    if (segments[0] === 'about') return 'about_page';

    return segments[0] || 'unknown';
  }

  private async setupPerformanceObservers() {
    if (typeof PerformanceObserver === 'undefined') {
      return;
    }

    try {
      // Largest Contentful Paint
      const lcpObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const lastEntry = entries[entries.length - 1] as PerformancePaintTiming;
        if (lastEntry) {
          this.trackPerformance({ lcp: lastEntry.startTime });
        }
      });
      lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });
      this.performanceObservers.push(lcpObserver);

      // First Input Delay
      const fidObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries() as PerformanceEventTiming[];
        entries.forEach((entry) => {
          const fid = entry.processingStart - entry.startTime;
          this.trackPerformance({ fid });
        });
      });
      fidObserver.observe({ entryTypes: ['first-input'] });
      this.performanceObservers.push(fidObserver);

      // Cumulative Layout Shift
      let clsValue = 0;
      const clsObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries() as (PerformanceEntry & {
          hadRecentInput?: boolean;
          value?: number;
        })[];
        entries.forEach((entry) => {
          if (!entry.hadRecentInput) {
            clsValue += entry.value || 0;
          }
        });
        this.trackPerformance({ cls: clsValue });
      });
      clsObserver.observe({ entryTypes: ['layout-shift'] });
      this.performanceObservers.push(clsObserver);

      // Navigation Timing
      const navigationObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries() as PerformanceNavigationTiming[];
        entries.forEach((entry) => {
          this.trackPerformance({
            ttfb: entry.responseStart - entry.requestStart,
            fcp: entry.responseEnd - entry.requestStart,
            loadTime: entry.loadEventEnd - entry.fetchStart,
            domContentLoaded: entry.domContentLoadedEventEnd - entry.fetchStart
          });
        });
      });
      navigationObserver.observe({ entryTypes: ['navigation'] });
      this.performanceObservers.push(navigationObserver);

    } catch (error) {
      const logger = await import('./logger').then(m => m.createLogger('AnalyticsTracker'));
      logger.warn('Failed to setup performance observers', 'performance_observer_error', {
        browser_support: typeof PerformanceObserver !== 'undefined',
        error_message: (error as Error).message,
        user_agent: navigator.userAgent
      }, error as Error);
      
      // Continue without performance monitoring if setup fails
      // This is a graceful degradation - analytics will still work without performance metrics
    }
  }

  private enablePerformanceTracking() {
    // 监控资源加载
    window.addEventListener('load', () => {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;
      const resources = performance.getEntriesByType('resource');

      this.trackPerformance({
        resourceCount: resources.length,
        totalResourceSize: resources.reduce((total, r) => total + ((r as PerformanceResourceTiming).transferSize || 0), 0),
        loadTime: navigation.loadEventEnd - navigation.fetchStart
      });
    });
  }

  private enableErrorTracking() {
    // 全局错误处理
    window.addEventListener('error', (event) => {
      this.trackError({
        message: event.message,
        stack: event.error?.stack,
        url: event.filename || window.location.href,
        lineNumber: event.lineno,
        columnNumber: event.colno,
        userAgent: navigator.userAgent,
        timestamp: Date.now(),
        sessionId: this.sessionId,
        userId: this.userId
      });
    });

    // Promise rejection 处理
    window.addEventListener('unhandledrejection', (event) => {
      this.trackError({
        message: `Unhandled Promise Rejection: ${event.reason}`,
        stack: event.reason?.stack,
        url: window.location.href,
        userAgent: navigator.userAgent,
        timestamp: Date.now(),
        sessionId: this.sessionId,
        userId: this.userId
      });
    });
  }

  private trackPageLoad() {
    window.addEventListener('load', () => {
      this.trackPageView();
    });
  }

  private startEventFlushTimer(interval: number = 10000) {
    this.flushInterval = setInterval(() => {
      this.flush();
    }, interval);
  }

  private stopEventFlushTimer() {
    if (this.flushInterval) {
      clearInterval(this.flushInterval);
      this.flushInterval = undefined;
    }
  }

  private disconnectPerformanceObservers() {
    this.performanceObservers.forEach(observer => {
      observer.disconnect();
    });
    this.performanceObservers = [];
  }

  private isHighPriorityEvent(eventName: string): boolean {
    const highPriorityEvents = ['error', 'conversion', 'page_view'];
    return highPriorityEvents.includes(eventName);
  }

  private async sendEvents(events: AnalyticsEvent[]) {
    const payload = {
      events,
      session_id: this.sessionId,
      user_id: this.userId,
      timestamp: Date.now(),
      client_info: {
        user_agent: navigator.userAgent,
        screen_resolution: `${window.screen.width}x${window.screen.height}`,
        viewport_size: `${window.innerWidth}x${window.innerHeight}`,
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        language: navigator.language,
        platform: navigator.platform
      }
    };

    const maxRetries = 3;
    const baseDelay = 1000; // 1 second
    
    for (let attempt = 0; attempt < maxRetries; attempt++) {
      try {
        // 尝试使用 sendBeacon (更可靠) - 但只在最后一次尝试或页面卸载时
        if ((attempt === maxRetries - 1 || document.visibilityState === 'hidden') && navigator.sendBeacon) {
          const sent = navigator.sendBeacon(
            this.apiEndpoint,
            JSON.stringify(payload)
          );
          if (sent) {
            const logger = await import('./logger').then(m => m.createLogger('AnalyticsTracker'));
            logger.debug('Events sent via sendBeacon', 'event_transmission', {
              event_count: events.length,
              attempt: attempt + 1,
              method: 'sendBeacon'
            });
            return;
          }
        }

        // 回退到 fetch with enhanced error handling
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 second timeout
        
        const response = await fetch(this.apiEndpoint, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Client-Version': '1.0.0',
            'X-Request-ID': crypto.randomUUID(),
          },
          body: JSON.stringify(payload),
          keepalive: true,
          signal: controller.signal
        });

        clearTimeout(timeoutId);

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();
        
        const logger = await import('./logger').then(m => m.createLogger('AnalyticsTracker'));
        logger.debug('Events sent successfully', 'event_transmission', {
          event_count: events.length,
          attempt: attempt + 1,
          method: 'fetch',
          response_status: response.status,
          server_timestamp: result.timestamp
        });
        
        return; // Success, exit retry loop

      } catch (error) {
        const logger = await import('./logger').then(m => m.createLogger('AnalyticsTracker'));
        
        if (attempt === maxRetries - 1) {
          // Last attempt failed
          logger.error('All attempts to send events failed', 'event_transmission_failed', {
            event_count: events.length,
            total_attempts: maxRetries,
            final_error: (error as Error).message,
            is_offline: !navigator.onLine,
            connection_type: (navigator as Navigator & { connection?: { effectiveType?: string } })?.connection?.effectiveType || 'unknown'
          }, error as Error);
          throw error;
        } else {
          // Retry with exponential backoff
          const delay = baseDelay * Math.pow(2, attempt) + Math.random() * 1000;
          
          logger.warn('Event transmission attempt failed, retrying', 'event_transmission_retry', {
            attempt: attempt + 1,
            max_attempts: maxRetries,
            retry_delay_ms: delay,
            error_message: (error as Error).message,
            is_offline: !navigator.onLine
          }, error as Error);
          
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      }
    }
  }

  private hashString(str: string): number {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return Math.abs(hash);
  }

  private getDeviceType(): string {
    const ua = navigator.userAgent;
    if (/tablet|ipad|playbook|silk/i.test(ua)) return 'tablet';
    if (/mobile|iphone|ipod|android|blackberry|opera|mini|windows\sce|palm|smartphone|iemobile/i.test(ua)) return 'mobile';
    return 'desktop';
  }

  private getBrowserName(): string {
    const ua = navigator.userAgent;
    if (ua.includes('Firefox')) return 'Firefox';
    if (ua.includes('Chrome')) return 'Chrome';
    if (ua.includes('Safari')) return 'Safari';
    if (ua.includes('Edge')) return 'Edge';
    return 'Unknown';
  }

  private getOperatingSystem(): string {
    const ua = navigator.userAgent;
    if (ua.includes('Windows')) return 'Windows';
    if (ua.includes('Mac')) return 'macOS';
    if (ua.includes('Linux')) return 'Linux';
    if (ua.includes('Android')) return 'Android';
    if (ua.includes('iOS')) return 'iOS';
    return 'Unknown';
  }

  private getSessionCount(): number {
    const key = 'maple_blog_session_count';
    const count = parseInt(localStorage.getItem(key) || '0', 10);
    const newCount = count + 1;
    localStorage.setItem(key, newCount.toString());
    return newCount;
  }
}

// 创建全局实例
export const analytics = AnalyticsTracker.getInstance();

// 便捷方法
export const trackEvent = (name: string, properties?: Record<string, string | number | boolean | null>) => {
  analytics.track(name, properties);
};

export const trackPageView = (pageName?: string, properties?: Record<string, string | number | boolean | null>) => {
  analytics.trackPageView(pageName, properties);
};

export const trackClick = (elementName: string, properties?: Record<string, string | number | boolean | null>) => {
  analytics.trackInteraction(elementName, 'click', properties);
};

export const trackFormSubmit = (formName: string, properties?: Record<string, string | number | boolean | null>) => {
  analytics.trackInteraction(formName, 'submit', properties);
};

export const trackSearchQuery = (query: string, results: number, filters?: Record<string, string | number | boolean | null>) => {
  analytics.trackSearch(query, results, filters);
};

export const trackPostView = (postId: string, postTitle: string, properties?: Record<string, string | number | boolean | null>) => {
  analytics.trackContentEngagement('post', postId, 'view', {
    post_title: postTitle,
    ...properties
  });
};

export const trackPostLike = (postId: string, postTitle: string) => {
  analytics.trackContentEngagement('post', postId, 'like', {
    post_title: postTitle
  });
};

export const trackPostShare = (postId: string, postTitle: string, shareMethod: string) => {
  analytics.trackContentEngagement('post', postId, 'share', {
    post_title: postTitle,
    share_method: shareMethod
  });
};

// 首页特定追踪方法
export const homePageAnalytics = {
  // 追踪首页加载完成
  trackHomePageLoad: (loadTime: number) => {
    trackEvent('homepage_loaded', {
      load_time: loadTime,
      page_type: 'homepage'
    });
  },

  // 追踪英雄区域交互
  trackHeroInteraction: (action: string, element: string) => {
    analytics.trackInteraction('hero_section', action, { element });
  },

  // 追踪文章列表交互
  trackPostListInteraction: (action: string, listType: string, postId?: string) => {
    analytics.trackInteraction('post_list', action, {
      list_type: listType,
      post_id: postId || null
    });
  },

  // 追踪搜索使用
  trackSearchUsage: (query: string, source: 'header' | 'hero' | 'sidebar') => {
    trackSearchQuery(query, -1, { search_source: source });
  },

  // 追踪分类导航
  trackCategoryNavigation: (categoryName: string, source: 'grid' | 'sidebar' | 'dropdown') => {
    trackEvent('category_navigation', {
      category_name: categoryName,
      navigation_source: source
    });
  },

  // 追踪个性化功能使用
  trackPersonalizationUsage: (feature: string, enabled: boolean) => {
    trackEvent('personalization_usage', {
      feature,
      enabled,
      user_id: analytics.getUserId() || null
    });
  }
};

export default analytics;