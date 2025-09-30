/**
 * Production-grade logging service
 * Provides structured logging with multiple levels, environment awareness,
 * and remote logging capabilities for comprehensive application monitoring
 */

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
  FATAL = 4
}

// Type for log context values that can be any JSON-serializable value
export type LogContextValue = string | number | boolean | null | undefined | LogContextValue[] | { [key: string]: LogContextValue } | Record<string, unknown>;

export interface LogContext {
  userId?: string;
  sessionId?: string;
  userAgent?: string;
  url?: string;
  component?: string;
  action?: string;
  correlationId?: string;
  timestamp?: string;
  buildVersion?: string;
  [key: string]: LogContextValue;
}

export interface LogEntry {
  level: LogLevel;
  message: string;
  context: LogContext;
  timestamp: string;
  stack?: string;
  fingerprint?: string;
}

export interface LogConfig {
  level: LogLevel;
  enableConsole: boolean;
  enableRemote: boolean;
  enableLocalStorage: boolean;
  maxLocalEntries: number;
  remoteEndpoint?: string;
  apiKey?: string;
  bufferSize: number;
  flushInterval: number;
  retryAttempts: number;
  userId?: string;
  enablePerformanceLogging: boolean;
}

class LoggingService {
  private config: LogConfig;
  private buffer: LogEntry[] = [];
  private flushTimer: NodeJS.Timeout | null = null;
  private sessionId: string;
  private correlationIdCounter = 0;
  private performanceObserver?: PerformanceObserver;

  constructor(config?: Partial<LogConfig>) {
    this.sessionId = this.generateSessionId();
    
    const isDevelopment = process.env.NODE_ENV === 'development';
    const isProduction = process.env.NODE_ENV === 'production';

    this.config = {
      level: isDevelopment ? LogLevel.DEBUG : LogLevel.INFO,
      enableConsole: isDevelopment,
      enableRemote: isProduction,
      enableLocalStorage: true,
      maxLocalEntries: 1000,
      remoteEndpoint: process.env.REACT_APP_LOGGING_ENDPOINT,
      apiKey: process.env.REACT_APP_LOGGING_API_KEY,
      bufferSize: 50,
      flushInterval: 30000, // 30 seconds
      retryAttempts: 3,
      enablePerformanceLogging: isProduction,
      ...config
    };

    this.initializeService();
  }

  private initializeService(): void {
    // Set up periodic buffer flushing
    if (this.config.enableRemote) {
      this.startPeriodicFlush();
    }

    // Set up performance monitoring
    if (this.config.enablePerformanceLogging && 'PerformanceObserver' in window) {
      this.initializePerformanceLogging();
    }

    // Handle page unload to flush remaining logs
    window.addEventListener('beforeunload', () => {
      this.flush(true); // Force synchronous flush
    });

    // Handle visibility change to flush logs when tab becomes hidden
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') {
        this.flush();
      }
    });
  }

  private generateSessionId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private generateCorrelationId(): string {
    return `${this.sessionId}-${++this.correlationIdCounter}`;
  }

  private getBaseContext(): LogContext {
    return {
      sessionId: this.sessionId,
      userAgent: navigator.userAgent,
      url: window.location.href,
      timestamp: new Date().toISOString(),
      buildVersion: process.env.REACT_APP_VERSION || 'unknown'
    };
  }

  private shouldLog(level: LogLevel): boolean {
    return level >= this.config.level;
  }

  private formatLogEntry(level: LogLevel, message: string, context: LogContext = {}, stack?: string): LogEntry {
    const correlationId = context.correlationId || this.generateCorrelationId();
    
    return {
      level,
      message,
      context: {
        ...this.getBaseContext(),
        ...context,
        correlationId
      },
      timestamp: new Date().toISOString(),
      stack,
      fingerprint: this.generateFingerprint(message, context)
    };
  }

  private generateFingerprint(message: string, context: LogContext): string {
    // Create a fingerprint for grouping similar log entries
    const fingerprintData = `${message}:${context.component || 'unknown'}:${context.action || 'unknown'}`;
    return btoa(fingerprintData).substring(0, 16);
  }

  private async persistToLocalStorage(entry: LogEntry): Promise<void> {
    if (!this.config.enableLocalStorage) return;

    try {
      const stored = localStorage.getItem('app_logs');
      const logs: LogEntry[] = stored ? JSON.parse(stored) : [];
      
      logs.push(entry);
      
      // Maintain max entries limit
      if (logs.length > this.config.maxLocalEntries) {
        logs.splice(0, logs.length - this.config.maxLocalEntries);
      }
      
      localStorage.setItem('app_logs', JSON.stringify(logs));
    } catch (error) {
      // If localStorage fails, at least try to console.error in development
      if (this.config.enableConsole) {
        console.error('Failed to persist log to localStorage:', error);
      }
    }
  }

  private outputToConsole(entry: LogEntry): void {
    if (!this.config.enableConsole) return;

    const style = this.getConsoleStyle(entry.level);
    const prefix = `[${LogLevel[entry.level]}] ${entry.timestamp}`;
    
    switch (entry.level) {
      case LogLevel.DEBUG:
        console.debug(`%c${prefix}`, style, entry.message, entry.context);
        break;
      case LogLevel.INFO:
        // Development mode logging - INFO level
        break;
      case LogLevel.WARN:
        console.warn(`%c${prefix}`, style, entry.message, entry.context);
        if (entry.stack) console.warn('Stack:', entry.stack);
        break;
      case LogLevel.ERROR:
      case LogLevel.FATAL:
        console.error(`%c${prefix}`, style, entry.message, entry.context);
        if (entry.stack) console.error('Stack:', entry.stack);
        break;
    }
  }

  private getConsoleStyle(level: LogLevel): string {
    const styles = {
      [LogLevel.DEBUG]: 'color: #6B7280; background: #F3F4F6;',
      [LogLevel.INFO]: 'color: #3B82F6; background: #EBF8FF;',
      [LogLevel.WARN]: 'color: #D97706; background: #FEF3C7;',
      [LogLevel.ERROR]: 'color: #DC2626; background: #FEE2E2;',
      [LogLevel.FATAL]: 'color: #FFFFFF; background: #DC2626;'
    };
    return styles[level] || '';
  }

  private async sendToRemote(entries: LogEntry[]): Promise<boolean> {
    if (!this.config.enableRemote || !this.config.remoteEndpoint) {
      return false;
    }

    try {
      const response = await fetch(this.config.remoteEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(this.config.apiKey && { 'Authorization': `Bearer ${this.config.apiKey}` })
        },
        body: JSON.stringify({
          logs: entries,
          metadata: {
            service: 'maple-blog-frontend',
            version: process.env.REACT_APP_VERSION,
            environment: process.env.NODE_ENV
          }
        })
      });

      return response.ok;
    } catch (error) {
      // Fallback to console in development if remote logging fails
      if (this.config.enableConsole) {
        console.error('Failed to send logs to remote endpoint:', error);
      }
      return false;
    }
  }

  private addToBuffer(entry: LogEntry): void {
    this.buffer.push(entry);
    
    if (this.buffer.length >= this.config.bufferSize) {
      this.flush();
    }
  }

  private startPeriodicFlush(): void {
    this.flushTimer = setInterval(() => {
      if (this.buffer.length > 0) {
        this.flush();
      }
    }, this.config.flushInterval);
  }

  private async flush(synchronous: boolean = false): Promise<void> {
    if (this.buffer.length === 0) return;

    const entries = [...this.buffer];
    this.buffer = [];

    if (synchronous) {
      // Use sendBeacon for synchronous sending during page unload
      if (navigator.sendBeacon && this.config.remoteEndpoint) {
        const payload = JSON.stringify({
          logs: entries,
          metadata: {
            service: 'maple-blog-frontend',
            version: process.env.REACT_APP_VERSION,
            environment: process.env.NODE_ENV
          }
        });
        navigator.sendBeacon(this.config.remoteEndpoint, payload);
      }
    } else {
      let attempts = 0;
      let success = false;

      while (attempts < this.config.retryAttempts && !success) {
        success = await this.sendToRemote(entries);
        if (!success) {
          attempts++;
          // Exponential backoff
          await new Promise(resolve => setTimeout(resolve, Math.pow(2, attempts) * 1000));
        }
      }

      if (!success && this.config.enableLocalStorage) {
        // If remote logging fails, at least store locally
        entries.forEach(entry => this.persistToLocalStorage(entry));
      }
    }
  }

  private initializePerformanceLogging(): void {
    this.performanceObserver = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      
      entries.forEach((entry) => {
        if (entry.entryType === 'navigation') {
          const navEntry = entry as PerformanceNavigationTiming;
          this.info('Page navigation performance', {
            component: 'Performance',
            action: 'navigation',
            domContentLoaded: navEntry.domContentLoadedEventEnd - navEntry.startTime,
            loadComplete: navEntry.loadEventEnd - navEntry.startTime,
            firstContentfulPaint: navEntry.domContentLoadedEventEnd - navEntry.startTime
          });
        } else if (entry.entryType === 'measure') {
          this.info('Performance measurement', {
            component: 'Performance',
            action: 'measure',
            name: entry.name,
            duration: entry.duration
          });
        }
      });
    });

    this.performanceObserver.observe({ entryTypes: ['navigation', 'measure'] });
  }

  // Public logging methods
  public debug(message: string, context: LogContext = {}): void {
    if (!this.shouldLog(LogLevel.DEBUG)) return;

    const entry = this.formatLogEntry(LogLevel.DEBUG, message, context);
    this.outputToConsole(entry);
    this.persistToLocalStorage(entry);
    this.addToBuffer(entry);
  }

  public info(message: string, context: LogContext = {}): void {
    if (!this.shouldLog(LogLevel.INFO)) return;

    const entry = this.formatLogEntry(LogLevel.INFO, message, context);
    this.outputToConsole(entry);
    this.persistToLocalStorage(entry);
    this.addToBuffer(entry);
  }

  public warn(message: string, context: LogContext = {}, error?: Error): void {
    if (!this.shouldLog(LogLevel.WARN)) return;

    const entry = this.formatLogEntry(LogLevel.WARN, message, context, error?.stack);
    this.outputToConsole(entry);
    this.persistToLocalStorage(entry);
    this.addToBuffer(entry);
  }

  public error(message: string, context: LogContext = {}, error?: Error): void {
    if (!this.shouldLog(LogLevel.ERROR)) return;

    const entry = this.formatLogEntry(LogLevel.ERROR, message, context, error?.stack);
    this.outputToConsole(entry);
    this.persistToLocalStorage(entry);
    this.addToBuffer(entry);
  }

  public fatal(message: string, context: LogContext = {}, error?: Error): void {
    if (!this.shouldLog(LogLevel.FATAL)) return;

    const entry = this.formatLogEntry(LogLevel.FATAL, message, context, error?.stack);
    this.outputToConsole(entry);
    this.persistToLocalStorage(entry);
    this.addToBuffer(entry);
    
    // Force immediate flush for fatal errors
    this.flush();
  }

  // Performance logging methods
  public startPerformanceTimer(name: string): void {
    if (this.config.enablePerformanceLogging) {
      performance.mark(`${name}-start`);
    }
  }

  public endPerformanceTimer(name: string, context: LogContext = {}): void {
    if (this.config.enablePerformanceLogging) {
      performance.mark(`${name}-end`);
      performance.measure(name, `${name}-start`, `${name}-end`);
      
      const measure = performance.getEntriesByName(name, 'measure')[0];
      if (measure) {
        this.info(`Performance: ${name}`, {
          ...context,
          component: 'Performance',
          action: 'timing',
          duration: measure.duration
        });
      }
    }
  }

  // Utility methods
  public setUserId(userId: string): void {
    this.config = {
      ...this.config,
      userId
    };
  }

  public setLogLevel(level: LogLevel): void {
    this.config = {
      ...this.config,
      level
    };
  }

  public getStoredLogs(): LogEntry[] {
    try {
      const stored = localStorage.getItem('app_logs');
      return stored ? JSON.parse(stored) : [];
    } catch {
      return [];
    }
  }

  public clearStoredLogs(): void {
    try {
      localStorage.removeItem('app_logs');
    } catch (error) {
      if (this.config.enableConsole) {
        console.error('Failed to clear stored logs:', error);
      }
    }
  }

  public exportLogs(format: 'json' | 'csv' = 'json'): string {
    const logs = this.getStoredLogs();
    
    if (format === 'csv') {
      const headers = ['timestamp', 'level', 'message', 'component', 'action', 'userId', 'sessionId'];
      const csvRows = [headers.join(',')];
      
      logs.forEach(log => {
        const row = [
          log.timestamp,
          LogLevel[log.level],
          `"${log.message.replace(/"/g, '""')}"`,
          log.context.component || '',
          log.context.action || '',
          log.context.userId || '',
          log.context.sessionId || ''
        ];
        csvRows.push(row.join(','));
      });
      
      return csvRows.join('\n');
    }
    
    return JSON.stringify(logs, null, 2);
  }

  // Cleanup method
  public destroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }

    if (this.performanceObserver) {
      this.performanceObserver.disconnect();
      this.performanceObserver = undefined;
    }

    // Final flush
    this.flush(true);
  }
}

// Create singleton instance
export const logger = new LoggingService();

// Export for testing or custom configurations
export { LoggingService };