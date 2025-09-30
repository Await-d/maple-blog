/**
 * Production-grade analytics and event tracking service
 * Provides comprehensive user interaction tracking, A/B testing,
 * performance monitoring, and privacy-compliant analytics
 */

import { logger, LogContext } from './loggingService';

// Performance API type extensions
interface LayoutShiftAttribution {
  node?: Node;
  currentRect?: DOMRectReadOnly;
  previousRect?: DOMRectReadOnly;
}

interface LayoutShift extends PerformanceEntry {
  value: number;
  hadRecentInput: boolean;
  sources?: LayoutShiftAttribution[];
}

// Base type for event property values
export type EventPropertyValue = string | number | boolean | null | undefined | Date | EventPropertyValue[] | { [key: string]: EventPropertyValue } | Record<string, unknown>;

// Analytics event properties with strict typing
export interface EventProperties {
  [key: string]: EventPropertyValue;
}

export interface AnalyticsEvent {
  name: string;
  category: string;
  properties: EventProperties;
  timestamp: string;
  sessionId: string;
  userId?: string;
  deviceId?: string;
  correlationId?: string;
}

export interface UserProperties {
  userId?: string;
  email?: string;
  name?: string;
  role?: string;
  plan?: string;
  signupDate?: string;
  lastLoginDate?: string;
  preferences?: EventProperties;
  [key: string]: EventPropertyValue;
}

export interface PageViewEvent {
  url: string;
  title: string;
  referrer?: string;
  timestamp: string;
  sessionId: string;
  userId?: string;
  duration?: number;
  scrollDepth?: number;
  exitIntent?: boolean;
}

export interface ConversionEvent {
  goalId: string;
  goalName: string;
  value?: number;
  currency?: string;
  timestamp: string;
  userId?: string;
  sessionId: string;
  conversionPath?: string[];
}

export interface PerformanceMetrics {
  pageLoadTime: number;
  domContentLoaded: number;
  firstContentfulPaint: number;
  largestContentfulPaint?: number;
  cumulativeLayoutShift?: number;
  firstInputDelay?: number;
  url: string;
  timestamp: string;
  deviceType: 'desktop' | 'tablet' | 'mobile';
  connectionType?: string;
}

export interface ABTestEvent {
  experimentId: string;
  experimentName: string;
  variantId: string;
  variantName: string;
  userId?: string;
  sessionId: string;
  timestamp: string;
  converted?: boolean;
  conversionValue?: number;
}

export interface AnalyticsConfig {
  enabled: boolean;
  enablePerformanceTracking: boolean;
  enableScrollTracking: boolean;
  enableClickTracking: boolean;
  enableFormTracking: boolean;
  enableErrorTracking: boolean;
  enableABTesting: boolean;
  respectDoNotTrack: boolean;
  requireConsent: boolean;
  batchSize: number;
  flushInterval: number;
  retryAttempts: number;
  googleAnalyticsId?: string;
  mixpanelToken?: string;
  segmentWriteKey?: string;
  customEndpoint?: string;
  apiKey?: string;
  debug: boolean;
}

class AnalyticsService {
  private config: AnalyticsConfig;
  private eventQueue: AnalyticsEvent[] = [];
  private pageViewQueue: PageViewEvent[] = [];
  private conversionQueue: ConversionEvent[] = [];
  private performanceQueue: PerformanceMetrics[] = [];
  private abTestQueue: ABTestEvent[] = [];
  
  private sessionId: string;
  private deviceId: string;
  private userProperties: UserProperties = {};
  private hasConsent: boolean = false;
  private currentPageView?: PageViewEvent;
  private performanceObserver?: PerformanceObserver;
  private intersectionObserver?: IntersectionObserver;
  private flushTimer?: NodeJS.Timeout;
  private scrollDepthTracked = new Set<number>();

  constructor(config?: Partial<AnalyticsConfig>) {
    const isProduction = process.env.NODE_ENV === 'production';
    const isDevelopment = process.env.NODE_ENV === 'development';

    this.config = {
      enabled: isProduction,
      enablePerformanceTracking: true,
      enableScrollTracking: true,
      enableClickTracking: true,
      enableFormTracking: true,
      enableErrorTracking: true,
      enableABTesting: false,
      respectDoNotTrack: true,
      requireConsent: true,
      batchSize: 20,
      flushInterval: 10000, // 10 seconds
      retryAttempts: 3,
      googleAnalyticsId: process.env.REACT_APP_GA_ID,
      mixpanelToken: process.env.REACT_APP_MIXPANEL_TOKEN,
      segmentWriteKey: process.env.REACT_APP_SEGMENT_KEY,
      customEndpoint: process.env.REACT_APP_ANALYTICS_ENDPOINT,
      apiKey: process.env.REACT_APP_ANALYTICS_API_KEY,
      debug: isDevelopment,
      ...config
    };

    this.sessionId = this.generateSessionId();
    this.deviceId = this.getOrCreateDeviceId();

    if (this.shouldInitialize()) {
      this.initialize();
    }
  }

  private shouldInitialize(): boolean {
    if (!this.config.enabled) return false;
    
    if (this.config.respectDoNotTrack && navigator.doNotTrack === '1') {
      logger.info('Analytics disabled due to Do Not Track header', {
        component: 'Analytics',
        action: 'initialize'
      });
      return false;
    }

    if (this.config.requireConsent && !this.hasConsent) {
      logger.info('Analytics waiting for user consent', {
        component: 'Analytics',
        action: 'initialize'
      });
      return false;
    }

    return true;
  }

  private initialize(): void {
    logger.info('Analytics service initialized', {
      component: 'Analytics',
      action: 'initialize',
      sessionId: this.sessionId,
      deviceId: this.deviceId
    });

    this.setupPerformanceTracking();
    this.setupScrollTracking();
    this.setupClickTracking();
    this.setupFormTracking();
    this.setupPageVisibilityTracking();
    this.startPeriodicFlush();
    this.initializeThirdPartyServices();
  }

  private generateSessionId(): string {
    return `session_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private getOrCreateDeviceId(): string {
    const stored = localStorage.getItem('analytics_device_id');
    if (stored) {
      return stored;
    }

    const deviceId = `device_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    try {
      localStorage.setItem('analytics_device_id', deviceId);
    } catch (error) {
      logger.warn('Failed to store device ID', {
        component: 'Analytics',
        action: 'getOrCreateDeviceId'
      }, error as Error);
    }

    return deviceId;
  }

  private setupPerformanceTracking(): void {
    if (!this.config.enablePerformanceTracking || typeof PerformanceObserver === 'undefined') {
      return;
    }

    try {
      this.performanceObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        
        entries.forEach((entry) => {
          if (entry.entryType === 'navigation') {
            this.trackPerformanceNavigation(entry as PerformanceNavigationTiming);
          } else if (entry.entryType === 'paint') {
            this.trackPerformancePaint(entry as PerformancePaintTiming);
          } else if (entry.entryType === 'layout-shift') {
            this.trackLayoutShift(entry as unknown as LayoutShift);
          } else if (entry.entryType === 'first-input') {
            this.trackFirstInputDelay(entry as PerformanceEventTiming);
          }
        });
      });

      this.performanceObserver.observe({
        entryTypes: ['navigation', 'paint', 'layout-shift', 'first-input', 'largest-contentful-paint']
      });
    } catch (error) {
      logger.warn('Failed to set up performance tracking', {
        component: 'Analytics',
        action: 'setupPerformanceTracking'
      }, error as Error);
    }
  }

  private trackPerformanceNavigation(entry: PerformanceNavigationTiming): void {
    const metrics: PerformanceMetrics = {
      pageLoadTime: entry.loadEventEnd - entry.fetchStart,
      domContentLoaded: entry.domContentLoadedEventEnd - entry.fetchStart,
      firstContentfulPaint: 0, // Will be updated by paint entries
      url: window.location.href,
      timestamp: new Date().toISOString(),
      deviceType: this.getDeviceType(),
      connectionType: this.getConnectionType()
    };

    this.performanceQueue.push(metrics);
    this.flushIfNeeded();
  }

  private trackPerformancePaint(entry: PerformancePaintTiming): void {
    if (entry.name === 'first-contentful-paint') {
      this.track('performance_paint', 'Performance', {
        paintType: entry.name,
        startTime: entry.startTime,
        url: window.location.href
      });
    }
  }

  private trackLayoutShift(entry: LayoutShift): void {
    if (!entry.hadRecentInput) {
      this.track('layout_shift', 'Performance', {
        value: entry.value,
        url: window.location.href,
        sources: entry.sources?.map((source: LayoutShiftAttribution) => ({
          node: (source.node as Element)?.tagName || 'unknown',
          currentRectX: source.currentRect?.x,
          currentRectY: source.currentRect?.y,
          currentRectWidth: source.currentRect?.width,
          currentRectHeight: source.currentRect?.height,
          previousRectX: source.previousRect?.x,
          previousRectY: source.previousRect?.y,
          previousRectWidth: source.previousRect?.width,
          previousRectHeight: source.previousRect?.height
        }))
      });
    }
  }

  private trackFirstInputDelay(entry: PerformanceEventTiming): void {
    this.track('first_input_delay', 'Performance', {
      delay: entry.processingStart - entry.startTime,
      duration: entry.duration,
      eventType: entry.name,
      url: window.location.href
    });
  }

  private getDeviceType(): 'desktop' | 'tablet' | 'mobile' {
    const userAgent = navigator.userAgent;
    if (/tablet|ipad|playbook|silk/i.test(userAgent)) {
      return 'tablet';
    }
    if (/mobile|iphone|ipod|android|blackberry|opera|mini|windows\sce|palm|smartphone|iemobile/i.test(userAgent)) {
      return 'mobile';
    }
    return 'desktop';
  }

  private getConnectionType(): string | undefined {
    interface NetworkInformation {
      effectiveType?: string;
      downlink?: number;
      rtt?: number;
      saveData?: boolean;
    }

    interface NavigatorWithConnection extends Navigator {
      connection?: NetworkInformation;
      mozConnection?: NetworkInformation;
      webkitConnection?: NetworkInformation;
    }

    const nav = navigator as NavigatorWithConnection;
    const connection = nav.connection || nav.mozConnection || nav.webkitConnection;
    return connection?.effectiveType;
  }

  private setupScrollTracking(): void {
    if (!this.config.enableScrollTracking) return;

    let ticking = false;

    const handleScroll = () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          this.trackScrollDepth();
          ticking = false;
        });
        ticking = true;
      }
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
  }

  private trackScrollDepth(): void {
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    const windowHeight = window.innerHeight;
    const documentHeight = document.documentElement.scrollHeight;
    const scrollPercent = Math.round((scrollTop + windowHeight) / documentHeight * 100);

    // Track at 25%, 50%, 75%, and 100%
    const milestones = [25, 50, 75, 100];
    for (const milestone of milestones) {
      if (scrollPercent >= milestone && !this.scrollDepthTracked.has(milestone)) {
        this.scrollDepthTracked.add(milestone);
        this.track('scroll_depth', 'Engagement', {
          depth: milestone,
          url: window.location.href,
          timestamp: new Date().toISOString()
        });
      }
    }
  }

  private setupClickTracking(): void {
    if (!this.config.enableClickTracking) return;

    document.addEventListener('click', (event) => {
      const target = event.target as HTMLElement;
      if (!target) return;

      // Track button clicks
      if (target.tagName === 'BUTTON' || target.getAttribute('role') === 'button') {
        this.track('button_click', 'Interaction', {
          buttonText: target.textContent?.trim() || '',
          buttonId: target.id || '',
          buttonClass: target.className || '',
          url: window.location.href
        });
      }

      // Track link clicks
      if (target.tagName === 'A' || target.closest('a')) {
        const link = target.tagName === 'A' ? target as HTMLAnchorElement : target.closest('a') as HTMLAnchorElement;
        this.track('link_click', 'Navigation', {
          linkText: link.textContent?.trim() || '',
          linkHref: link.href || '',
          isExternal: link.hostname !== window.location.hostname,
          url: window.location.href
        });
      }

      // Track clicks on elements with data-track attributes
      const trackingElement = target.closest('[data-track]') as HTMLElement;
      if (trackingElement) {
        const trackingData = trackingElement.dataset.track;
        const trackingCategory = trackingElement.dataset.trackCategory || 'Custom';
        const trackingProperties = trackingElement.dataset.trackProperties;

        this.track(trackingData || 'custom_click', trackingCategory, {
          elementText: target.textContent?.trim() || '',
          elementId: target.id || '',
          elementClass: target.className || '',
          url: window.location.href,
          ...(trackingProperties ? JSON.parse(trackingProperties) : {})
        });
      }
    }, true);
  }

  private setupFormTracking(): void {
    if (!this.config.enableFormTracking) return;

    // Track form submissions
    document.addEventListener('submit', (event) => {
      const form = event.target as HTMLFormElement;
      if (!form) return;

      const formData = new FormData(form);
      const formFields = Array.from(formData.keys()).filter(key => 
        !['password', 'token', 'csrf'].includes(key.toLowerCase())
      );

      this.track('form_submit', 'Conversion', {
        formId: form.id || '',
        formClass: form.className || '',
        formAction: form.action || '',
        formMethod: form.method || 'GET',
        fieldCount: formFields.length,
        fields: formFields,
        url: window.location.href
      });
    });

    // Track form abandonment
    const formElements = new Map<HTMLFormElement, number>();
    
    document.addEventListener('focusin', (event) => {
      const target = event.target as HTMLElement;
      const form = target.closest('form') as HTMLFormElement;
      if (form && ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) {
        if (!formElements.has(form)) {
          formElements.set(form, 0);
        }
        formElements.set(form, formElements.get(form)! + 1);
      }
    });

    window.addEventListener('beforeunload', () => {
      formElements.forEach((interactionCount, form) => {
        if (interactionCount > 2) { // Only track if user actually interacted
          this.track('form_abandon', 'Engagement', {
            formId: form.id || '',
            formClass: form.className || '',
            interactionCount,
            url: window.location.href
          });
        }
      });
    });
  }

  private setupPageVisibilityTracking(): void {
    let pageVisible = !document.hidden;
    let visibilityStart = Date.now();

    const handleVisibilityChange = () => {
      const now = Date.now();
      
      if (document.hidden && pageVisible) {
        // Page became hidden
        const visibleDuration = now - visibilityStart;
        this.track('page_visibility_hidden', 'Engagement', {
          visibleDuration,
          url: window.location.href
        });
        pageVisible = false;
      } else if (!document.hidden && !pageVisible) {
        // Page became visible
        this.track('page_visibility_shown', 'Engagement', {
          url: window.location.href
        });
        pageVisible = true;
        visibilityStart = now;
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
  }

  private initializeThirdPartyServices(): void {
    if (this.config.googleAnalyticsId) {
      this.initializeGoogleAnalytics();
    }

    if (this.config.mixpanelToken) {
      this.initializeMixpanel();
    }

    if (this.config.segmentWriteKey) {
      this.initializeSegment();
    }
  }

  private initializeGoogleAnalytics(): void {
    interface GtagFunction {
      (command: 'js', date: Date): void;
      (command: 'config', targetId: string, config?: { anonymize_ip?: boolean; respect_dnt?: boolean }): void;
      (command: 'event', eventName: string, eventParams?: Record<string, unknown>): void;
    }

    interface WindowWithGtag extends Window {
      dataLayer: unknown[];
      gtag: GtagFunction;
    }

    // Load Google Analytics
    const script = document.createElement('script');
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${this.config.googleAnalyticsId}`;
    document.head.appendChild(script);

    // Initialize gtag
    const win = window as unknown as WindowWithGtag;
    win.dataLayer = win.dataLayer || [];
    win.gtag = function(...args: unknown[]) {
      win.dataLayer.push(args);
    } as GtagFunction;

    win.gtag('js', new Date());
    win.gtag('config', this.config.googleAnalyticsId || '', {
      anonymize_ip: true,
      respect_dnt: this.config.respectDoNotTrack
    });
  }

  private initializeMixpanel(): void {
    interface MixpanelClient {
      track: (eventName: string, properties: EventProperties) => void;
    }

    interface WindowWithMixpanel extends Window {
      mixpanel: MixpanelClient;
    }

    // Simplified Mixpanel initialization
    // In production, use the official Mixpanel SDK
    const win = window as unknown as WindowWithMixpanel;
    win.mixpanel = {
      track: (eventName: string, properties: EventProperties) => {
        logger.debug(`Mixpanel event: ${eventName}`, {
          component: 'Analytics',
          action: 'mixpanel',
          properties
        });
      }
    };
  }

  private initializeSegment(): void {
    interface SegmentClient {
      track: (eventName: string, properties: EventProperties) => void;
    }

    interface WindowWithSegment extends Window {
      analytics: SegmentClient;
    }

    // Simplified Segment initialization
    // In production, use the official Segment SDK
    const win = window as unknown as WindowWithSegment;
    win.analytics = {
      track: (eventName: string, properties: EventProperties) => {
        logger.debug(`Segment event: ${eventName}`, {
          component: 'Analytics',
          action: 'segment',
          properties
        });
      }
    };
  }

  private async sendToThirdParty(events: AnalyticsEvent[]): Promise<void> {
    interface WindowWithAnalytics extends Window {
      gtag?: (command: string, ...args: unknown[]) => void;
      mixpanel?: { track: (name: string, props: Record<string, unknown>) => void };
      analytics?: { track: (name: string, props: Record<string, unknown>) => void };
    }

    const win = window as unknown as WindowWithAnalytics;

    // Send to Google Analytics
    if (this.config.googleAnalyticsId && win.gtag) {
      events.forEach(event => {
        win.gtag?.('event', event.name, {
          event_category: event.category,
          ...event.properties
        });
      });
    }

    // Send to Mixpanel
    if (this.config.mixpanelToken && win.mixpanel) {
      events.forEach(event => {
        win.mixpanel?.track(event.name, {
          category: event.category,
          ...event.properties
        });
      });
    }

    // Send to Segment
    if (this.config.segmentWriteKey && win.analytics) {
      events.forEach(event => {
        win.analytics?.track(event.name, {
          category: event.category,
          ...event.properties
        });
      });
    }
  }

  private async sendToCustomEndpoint(events: AnalyticsEvent[]): Promise<boolean> {
    if (!this.config.customEndpoint) return false;

    try {
      const response = await fetch(this.config.customEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(this.config.apiKey && { 'Authorization': `Bearer ${this.config.apiKey}` })
        },
        body: JSON.stringify({
          events,
          session: {
            sessionId: this.sessionId,
            deviceId: this.deviceId,
            userProperties: this.userProperties
          },
          metadata: {
            service: 'maple-blog-frontend',
            timestamp: new Date().toISOString(),
            version: process.env.REACT_APP_VERSION
          }
        })
      });

      return response.ok;
    } catch (error) {
      logger.error('Failed to send analytics events', {
        component: 'Analytics',
        action: 'sendToCustomEndpoint',
        eventCount: events.length
      }, error as Error);
      return false;
    }
  }

  private startPeriodicFlush(): void {
    this.flushTimer = setInterval(() => {
      this.flush();
    }, this.config.flushInterval);
  }

  private flushIfNeeded(): void {
    const totalEvents = this.eventQueue.length + this.pageViewQueue.length + 
                       this.conversionQueue.length + this.performanceQueue.length;
    
    if (totalEvents >= this.config.batchSize) {
      this.flush();
    }
  }

  // Public methods
  public grantConsent(): void {
    this.hasConsent = true;
    
    if (this.config.enabled && !this.flushTimer) {
      this.initialize();
    }

    logger.info('Analytics consent granted', {
      component: 'Analytics',
      action: 'grantConsent'
    });
  }

  public revokeConsent(): void {
    this.hasConsent = false;
    
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = undefined;
    }

    // Clear all queues
    this.eventQueue = [];
    this.pageViewQueue = [];
    this.conversionQueue = [];
    this.performanceQueue = [];
    this.abTestQueue = [];

    logger.info('Analytics consent revoked', {
      component: 'Analytics',
      action: 'revokeConsent'
    });
  }

  public identify(userId: string, properties?: UserProperties): void {
    this.userProperties = {
      ...this.userProperties,
      userId,
      ...properties
    };

    this.track('user_identified', 'Auth', {
      userId,
      ...(properties as EventProperties)
    });

    logger.info('User identified', {
      component: 'Analytics',
      action: 'identify',
      userId
    });
  }

  public track(eventName: string, category: string, properties: EventProperties = {}): void {
    if (!this.shouldTrack()) return;

    const event: AnalyticsEvent = {
      name: eventName,
      category,
      properties: {
        ...properties,
        deviceId: this.deviceId,
        deviceType: this.getDeviceType(),
        url: window.location.href,
        referrer: document.referrer,
        userAgent: navigator.userAgent
      },
      timestamp: new Date().toISOString(),
      sessionId: this.sessionId,
      userId: this.userProperties.userId,
      deviceId: this.deviceId,
      correlationId: typeof properties.correlationId === 'string' ? properties.correlationId : undefined
    };

    this.eventQueue.push(event);

    if (this.config.debug) {
      logger.debug(`Analytics event: ${eventName}`, {
        component: 'Analytics',
        action: 'track',
        category,
        properties
      });
    }

    this.flushIfNeeded();
  }

  public trackPageView(url?: string, title?: string): void {
    if (!this.shouldTrack()) return;

    const pageView: PageViewEvent = {
      url: url || window.location.href,
      title: title || document.title,
      referrer: document.referrer,
      timestamp: new Date().toISOString(),
      sessionId: this.sessionId,
      userId: this.userProperties.userId
    };

    // End previous page view if exists
    if (this.currentPageView) {
      const duration = Date.now() - new Date(this.currentPageView.timestamp).getTime();
      this.currentPageView.duration = duration;
      this.pageViewQueue.push(this.currentPageView);
    }

    this.currentPageView = pageView;
    this.scrollDepthTracked.clear(); // Reset scroll tracking for new page

    if (this.config.debug) {
      logger.debug('Page view tracked', {
        component: 'Analytics',
        action: 'trackPageView',
        url: pageView.url,
        title: pageView.title
      });
    }

    this.flushIfNeeded();
  }

  public trackConversion(goalId: string, goalName: string, value?: number, currency?: string): void {
    if (!this.shouldTrack()) return;

    const conversion: ConversionEvent = {
      goalId,
      goalName,
      value,
      currency,
      timestamp: new Date().toISOString(),
      userId: this.userProperties.userId,
      sessionId: this.sessionId
    };

    this.conversionQueue.push(conversion);

    this.track('conversion', 'Conversion', {
      goalId,
      goalName,
      value,
      currency
    });

    logger.info('Conversion tracked', {
      component: 'Analytics',
      action: 'trackConversion',
      goalId,
      goalName,
      value
    });

    this.flushIfNeeded();
  }

  public trackError(error: Error, context: LogContext = {}): void {
    if (!this.config.enableErrorTracking || !this.shouldTrack()) return;

    this.track('javascript_error', 'Error', {
      errorMessage: error.message,
      errorName: error.name,
      errorStack: error.stack,
      ...context
    });
  }

  public trackABTest(experimentId: string, experimentName: string, variantId: string, variantName: string): void {
    if (!this.config.enableABTesting || !this.shouldTrack()) return;

    const abTest: ABTestEvent = {
      experimentId,
      experimentName,
      variantId,
      variantName,
      userId: this.userProperties.userId,
      sessionId: this.sessionId,
      timestamp: new Date().toISOString()
    };

    this.abTestQueue.push(abTest);

    this.track('ab_test_viewed', 'Experiment', {
      experimentId,
      experimentName,
      variantId,
      variantName
    });

    logger.info('A/B test tracked', {
      component: 'Analytics',
      action: 'trackABTest',
      experimentId,
      variantId
    });

    this.flushIfNeeded();
  }

  public trackABTestConversion(experimentId: string, conversionValue?: number): void {
    if (!this.config.enableABTesting || !this.shouldTrack()) return;

    // Find the experiment in queue and mark as converted
    const experiment = this.abTestQueue.find(exp => exp.experimentId === experimentId);
    if (experiment) {
      experiment.converted = true;
      experiment.conversionValue = conversionValue;
    }

    this.track('ab_test_conversion', 'Experiment', {
      experimentId,
      conversionValue
    });
  }

  private shouldTrack(): boolean {
    return this.config.enabled && this.hasConsent && 
           !(this.config.respectDoNotTrack && navigator.doNotTrack === '1');
  }

  public async flush(): Promise<void> {
    if (!this.shouldTrack()) return;

    const allEvents: AnalyticsEvent[] = [
      ...this.eventQueue,
      ...this.pageViewQueue.map(pv => ({
        name: 'page_view',
        category: 'Navigation',
        properties: pv as unknown as EventProperties,
        timestamp: pv.timestamp,
        sessionId: pv.sessionId,
        userId: pv.userId,
        deviceId: this.deviceId
      })),
      ...this.conversionQueue.map(conv => ({
        name: 'conversion',
        category: 'Conversion',
        properties: conv as unknown as EventProperties,
        timestamp: conv.timestamp,
        sessionId: conv.sessionId,
        userId: conv.userId,
        deviceId: this.deviceId
      })),
      ...this.performanceQueue.map(perf => ({
        name: 'performance_metrics',
        category: 'Performance',
        properties: perf as unknown as EventProperties,
        timestamp: perf.timestamp,
        sessionId: this.sessionId,
        userId: this.userProperties.userId,
        deviceId: this.deviceId
      })),
      ...this.abTestQueue.map(ab => ({
        name: ab.converted ? 'ab_test_conversion' : 'ab_test_exposure',
        category: 'Experiment',
        properties: ab as unknown as EventProperties,
        timestamp: ab.timestamp,
        sessionId: ab.sessionId,
        userId: ab.userId,
        deviceId: this.deviceId
      }))
    ];

    if (allEvents.length === 0) return;

    // Clear queues
    this.eventQueue = [];
    this.pageViewQueue = [];
    this.conversionQueue = [];
    this.performanceQueue = [];
    this.abTestQueue = [];

    // Send to third-party services
    await this.sendToThirdParty(allEvents);

    // Send to custom endpoint
    let attempts = 0;
    let success = false;

    while (attempts < this.config.retryAttempts && !success) {
      success = await this.sendToCustomEndpoint(allEvents);
      if (!success) {
        attempts++;
        await new Promise(resolve => setTimeout(resolve, Math.pow(2, attempts) * 1000));
      }
    }

    if (success) {
      logger.debug(`Flushed ${allEvents.length} analytics events`, {
        component: 'Analytics',
        action: 'flush',
        eventCount: allEvents.length
      });
    } else {
      logger.error(`Failed to flush ${allEvents.length} analytics events`, {
        component: 'Analytics',
        action: 'flush',
        eventCount: allEvents.length
      });
      
      // Re-add events to queue for retry (only keep the most recent events)
      this.eventQueue.unshift(...allEvents.slice(-this.config.batchSize));
    }
  }

  public getQueueStats(): { events: number; pageViews: number; conversions: number; performance: number; abTests: number } {
    return {
      events: this.eventQueue.length,
      pageViews: this.pageViewQueue.length,
      conversions: this.conversionQueue.length,
      performance: this.performanceQueue.length,
      abTests: this.abTestQueue.length
    };
  }

  public destroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = undefined;
    }

    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
      this.performanceObserver = undefined;
    }

    if (this.intersectionObserver) {
      this.intersectionObserver.disconnect();
      this.intersectionObserver = undefined;
    }

    // Final flush
    this.flush();
  }
}

// Create singleton instance
export const analytics = new AnalyticsService();

// React hook for analytics
export const useAnalytics = () => {
  return {
    track: analytics.track.bind(analytics),
    trackPageView: analytics.trackPageView.bind(analytics),
    trackConversion: analytics.trackConversion.bind(analytics),
    trackError: analytics.trackError.bind(analytics),
    identify: analytics.identify.bind(analytics),
    grantConsent: analytics.grantConsent.bind(analytics),
    revokeConsent: analytics.revokeConsent.bind(analytics)
  };
};

// Export for testing or custom configurations
export { AnalyticsService };