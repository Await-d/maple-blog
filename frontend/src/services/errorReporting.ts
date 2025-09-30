/**
 * Production-grade error reporting and tracking service
 * Provides comprehensive error monitoring, user feedback collection,
 * and integration with error tracking services like Sentry, Bugsnag, etc.
 */

import React, { createElement } from 'react';
import { logger, LogLevel } from './loggingService';

// Type for extra error data that can be any JSON-serializable value
export type ErrorExtraValue = string | number | boolean | null | undefined | ErrorExtraValue[] | { [key: string]: ErrorExtraValue };

export interface ErrorContext {
  errorId?: string;
  userAgent?: string;
  url?: string;
  userId?: string;
  userEmail?: string;
  component?: string;
  action?: string;
  correlationId?: string;
  sessionId?: string;
  timestamp?: string;
  buildVersion?: string;
  breadcrumbs?: Breadcrumb[];
  tags?: Record<string, string>;
  extra?: Record<string, ErrorExtraValue>;
  fingerprint?: string[];
  release?: string;
  environment?: string;
  screenshot?: string;
  handled?: boolean;
}

export interface Breadcrumb {
  timestamp: string;
  message: string;
  category: string;
  level: 'debug' | 'info' | 'warning' | 'error';
  data?: Record<string, ErrorExtraValue>;
}

export interface ErrorReport {
  id: string;
  message: string;
  stack?: string;
  context: ErrorContext;
  timestamp: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  handled: boolean;
  fingerprint: string[];
}

export interface UserFeedback {
  errorId: string;
  userEmail?: string;
  userName?: string;
  comments: string;
  timestamp: string;
  context?: Record<string, ErrorExtraValue>;
}

export interface ErrorConfig {
  enabled: boolean;
  enableUserFeedback: boolean;
  enableScreenshots: boolean;
  enableRecording: boolean;
  autoSubmit: boolean;
  submitThreshold: number; // Minimum error level to auto-submit
  sentryDsn?: string;
  apiKey?: string;
  environment: string;
  release?: string;
  maxBreadcrumbs: number;
  beforeSend?: (error: ErrorReport) => ErrorReport | null;
  onError?: (error: ErrorReport) => void;
  userId?: string;
  userEmail?: string;
  tags?: Record<string, string>;
  extra?: Record<string, ErrorExtraValue>;
}

class ErrorReportingService {
  private config: ErrorConfig;
  private breadcrumbs: Breadcrumb[] = [];
  private errorQueue: ErrorReport[] = [];
  private userFeedbackQueue: UserFeedback[] = [];
  private unhandledErrorListener?: (event: ErrorEvent) => void;
  private unhandledRejectionListener?: (event: PromiseRejectionEvent) => void;
  private globalErrorHandler?: (error: Error, errorInfo?: React.ErrorInfo) => void;

  constructor(config?: Partial<ErrorConfig>) {
    const isProduction = process.env.NODE_ENV === 'production';
    
    this.config = {
      enabled: isProduction,
      enableUserFeedback: true,
      enableScreenshots: false, // Requires additional permissions
      enableRecording: false, // Heavy on performance
      autoSubmit: isProduction,
      submitThreshold: 2, // Warn level and above
      environment: process.env.NODE_ENV || 'development',
      release: process.env.REACT_APP_VERSION,
      maxBreadcrumbs: 100,
      sentryDsn: process.env.REACT_APP_SENTRY_DSN,
      apiKey: process.env.REACT_APP_ERROR_REPORTING_API_KEY,
      ...config
    };

    if (this.config.enabled) {
      this.initialize();
    }
  }

  private initialize(): void {
    this.setupGlobalErrorHandling();
    this.addBreadcrumb('Error Reporting Service initialized', 'system', 'info');
  }

  private setupGlobalErrorHandling(): void {
    // Handle unhandled JavaScript errors
    this.unhandledErrorListener = (event: ErrorEvent) => {
      const error = new Error(event.message);
      error.stack = `${event.filename}:${event.lineno}:${event.colno}`;
      
      this.captureError(error, {
        component: 'Global',
        action: 'unhandledError',
        handled: false,
        extra: {
          filename: event.filename,
          lineno: event.lineno,
          colno: event.colno
        }
      });
    };

    // Handle unhandled promise rejections
    this.unhandledRejectionListener = (event: PromiseRejectionEvent) => {
      const error = event.reason instanceof Error 
        ? event.reason 
        : new Error(String(event.reason));
      
      this.captureError(error, {
        component: 'Global',
        action: 'unhandledRejection',
        handled: false,
        extra: {
          reason: event.reason
        }
      });
    };

    window.addEventListener('error', this.unhandledErrorListener);
    window.addEventListener('unhandledrejection', this.unhandledRejectionListener);
  }

  private generateErrorId(): string {
    return `error_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private generateFingerprint(error: Error, context: ErrorContext): string[] {
    // Create fingerprint for grouping similar errors
    const fingerprint = [
      error.name || 'Error',
      error.message?.split('\n')[0] || 'Unknown error',
      context.component || 'Unknown',
      context.action || 'Unknown'
    ];
    
    if (context.fingerprint) {
      return context.fingerprint;
    }
    
    return fingerprint;
  }

  private determineSeverity(error: Error, context: ErrorContext): 'low' | 'medium' | 'high' | 'critical' {
    // Determine severity based on error characteristics
    if (context.component === 'Auth' || error.message?.includes('authentication')) {
      return 'critical';
    }
    
    if (error.message?.includes('network') || error.message?.includes('fetch')) {
      return 'medium';
    }
    
    if (error.name === 'TypeError' || error.name === 'ReferenceError') {
      return 'high';
    }
    
    if (context.action === 'userInteraction') {
      return 'medium';
    }
    
    return 'low';
  }

  private async captureScreenshot(): Promise<string | undefined> {
    if (!this.config.enableScreenshots) return;

    try {
      // Use html2canvas or similar library to capture screenshot
      // This is a placeholder - actual implementation would depend on chosen library
      const canvas = document.createElement('canvas');
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
      
      const ctx = canvas.getContext('2d');
      if (ctx) {
        ctx.fillStyle = '#f0f0f0';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#666';
        ctx.font = '16px Arial';
        ctx.fillText('Screenshot capture not implemented', 20, 50);
      }
      
      return canvas.toDataURL();
    } catch (error) {
      logger.warn('Failed to capture screenshot', {
        component: 'ErrorReporting',
        action: 'screenshot'
      }, error as Error);
      return;
    }
  }

  private async sendToRemote(reports: ErrorReport[]): Promise<boolean> {
    if (!this.config.enabled) return false;

    // Try Sentry first if configured
    if (this.config.sentryDsn) {
      return this.sendToSentry(reports);
    }

    // Fallback to custom endpoint
    try {
      const response = await fetch('/api/errors', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(this.config.apiKey && { 'Authorization': `Bearer ${this.config.apiKey}` })
        },
        body: JSON.stringify({
          errors: reports,
          metadata: {
            service: 'maple-blog-frontend',
            environment: this.config.environment,
            release: this.config.release
          }
        })
      });

      return response.ok;
    } catch (error) {
      logger.error('Failed to send error reports', {
        component: 'ErrorReporting',
        action: 'sendToRemote'
      }, error as Error);
      return false;
    }
  }

  private async sendToSentry(reports: ErrorReport[]): Promise<boolean> {
    // This is a simplified Sentry implementation
    // In production, you'd use @sentry/browser
    try {
      for (const report of reports) {
        const sentryPayload = {
          message: report.message,
          level: this.mapSeverityToSentryLevel(report.severity),
          fingerprint: report.fingerprint,
          tags: report.context.tags || {},
          extra: report.context.extra || {},
          user: {
            id: report.context.userId,
            email: report.context.userEmail
          },
          contexts: {
            browser: {
              name: navigator.appName,
              version: navigator.appVersion,
              url: window.location.href
            }
          },
          breadcrumbs: this.breadcrumbs.map(b => ({
            timestamp: new Date(b.timestamp).getTime() / 1000,
            message: b.message,
            category: b.category,
            level: b.level,
            data: b.data
          }))
        };

        // Send to Sentry endpoint (simplified)
        await fetch(`https://sentry.io/api/projects/${this.extractProjectId()}/store/`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Sentry-Auth': this.generateSentryAuth()
          },
          body: JSON.stringify(sentryPayload)
        });
      }
      return true;
    } catch (error) {
      logger.error('Failed to send to Sentry', {
        component: 'ErrorReporting',
        action: 'sendToSentry'
      }, error as Error);
      return false;
    }
  }

  private mapSeverityToSentryLevel(severity: string): string {
    const mapping = {
      low: 'info',
      medium: 'warning',
      high: 'error',
      critical: 'fatal'
    };
    return mapping[severity as keyof typeof mapping] || 'error';
  }

  private extractProjectId(): string {
    // Extract project ID from Sentry DSN
    if (!this.config.sentryDsn) return '';
    const match = this.config.sentryDsn.match(/\/(\d+)$/);
    return match ? match[1] : '';
  }

  private generateSentryAuth(): string {
    // Generate Sentry auth header (simplified)
    const timestamp = Math.floor(Date.now() / 1000);
    return `Sentry sentry_version=7,sentry_timestamp=${timestamp},sentry_key=${this.config.apiKey}`;
  }

  // Public methods
  public addBreadcrumb(message: string, category: string, level: 'debug' | 'info' | 'warning' | 'error', data?: Record<string, ErrorExtraValue>): void {
    if (!this.config.enabled) return;

    const breadcrumb: Breadcrumb = {
      timestamp: new Date().toISOString(),
      message,
      category,
      level,
      data
    };

    this.breadcrumbs.push(breadcrumb);

    // Maintain max breadcrumbs limit
    if (this.breadcrumbs.length > this.config.maxBreadcrumbs) {
      this.breadcrumbs = this.breadcrumbs.slice(-this.config.maxBreadcrumbs);
    }

    // Also log as debug
    logger.debug(`Breadcrumb: ${message}`, {
      component: 'ErrorReporting',
      action: 'breadcrumb',
      category,
      level
    });
  }

  public async captureError(error: Error, context: ErrorContext = {}): Promise<string> {
    if (!this.config.enabled) {
      // Still log to local logger in development
      logger.error(error.message, context, error);
      return '';
    }

    const errorId = this.generateErrorId();
    const severity = this.determineSeverity(error, context);
    const fingerprint = this.generateFingerprint(error, context);

    // Capture screenshot if enabled
    let screenshot: string | undefined;
    if (this.config.enableScreenshots && severity === 'critical') {
      screenshot = await this.captureScreenshot();
    }

    const report: ErrorReport = {
      id: errorId,
      message: error.message,
      stack: error.stack,
      context: {
        ...context,
        errorId,
        userAgent: navigator.userAgent,
        url: window.location.href,
        breadcrumbs: [...this.breadcrumbs],
        timestamp: new Date().toISOString(),
        screenshot,
        tags: {
          ...context.tags,
          severity,
          handled: String(context.handled !== false)
        },
        extra: {
          ...context.extra,
          errorName: error.name,
          errorConstructor: error.constructor.name
        }
      },
      timestamp: new Date().toISOString(),
      severity,
      handled: context.handled !== false,
      fingerprint
    };

    // Apply beforeSend hook if configured
    let processedReport = report;
    if (this.config.beforeSend) {
      const result = this.config.beforeSend(report);
      if (!result) {
        return errorId; // Skip sending if beforeSend returns null
      }
      processedReport = result;
    }

    // Add to queue
    this.errorQueue.push(processedReport);

    // Log locally
    logger.error(`Error captured: ${error.message}`, {
      ...context,
      errorId,
      severity
    }, error);

    // Auto-submit if configured and meets threshold
    if (this.config.autoSubmit && LogLevel[severity.toUpperCase() as keyof typeof LogLevel] >= this.config.submitThreshold) {
      await this.submitPendingReports();
    }

    // Call onError hook if configured
    if (this.config.onError) {
      this.config.onError(processedReport);
    }

    // Add breadcrumb for this error
    this.addBreadcrumb(
      `Error: ${error.message}`,
      'error',
      'error',
      { errorId, severity }
    );

    return errorId;
  }

  public async captureMessage(message: string, _level: 'debug' | 'info' | 'warning' | 'error' = 'info', context: ErrorContext = {}): Promise<string> {
    const error = new Error(message);
    return this.captureError(error, { ...context, handled: true });
  }

  public async submitUserFeedback(errorId: string, feedback: Omit<UserFeedback, 'errorId' | 'timestamp'>): Promise<void> {
    const userFeedback: UserFeedback = {
      ...feedback,
      errorId,
      timestamp: new Date().toISOString()
    };

    this.userFeedbackQueue.push(userFeedback);

    logger.info('User feedback submitted', {
      component: 'ErrorReporting',
      action: 'userFeedback',
      errorId
    });

    // Send feedback to remote service
    try {
      await fetch('/api/error-feedback', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(this.config.apiKey && { 'Authorization': `Bearer ${this.config.apiKey}` })
        },
        body: JSON.stringify(userFeedback)
      });
    } catch (error) {
      logger.error('Failed to send user feedback', {
        component: 'ErrorReporting',
        action: 'submitUserFeedback'
      }, error as Error);
    }
  }

  public async submitPendingReports(): Promise<void> {
    if (this.errorQueue.length === 0) return;

    const reports = [...this.errorQueue];
    this.errorQueue = [];

    const success = await this.sendToRemote(reports);
    
    if (!success) {
      // Add back to queue for retry
      this.errorQueue.unshift(...reports);
      logger.error('Failed to submit error reports', {
        component: 'ErrorReporting',
        action: 'submitPendingReports',
        count: reports.length
      });
    } else {
      logger.info(`Submitted ${reports.length} error reports`, {
        component: 'ErrorReporting',
        action: 'submitPendingReports',
        count: reports.length
      });
    }
  }

  public setUser(userId: string, userEmail?: string, userName?: string): void {
    this.addBreadcrumb(
      `User identified: ${userId}`,
      'auth',
      'info',
      { userId, userEmail, userName }
    );
    
    // Update context for future errors
    this.config = {
      ...this.config,
      userId,
      userEmail
    };
  }

  public setTags(tags: Record<string, string>): void {
    // Update tags for future errors
    this.config = {
      ...this.config,
      tags: {
        ...this.config.tags,
        ...tags
      }
    };
  }

  public setExtra(key: string, value: ErrorExtraValue): void {
    // Update extra data for future errors
    this.config = {
      ...this.config,
      extra: {
        ...this.config.extra,
        [key]: value
      }
    };
  }

  public clearBreadcrumbs(): void {
    this.breadcrumbs = [];
  }

  public getBreadcrumbs(): Breadcrumb[] {
    return [...this.breadcrumbs];
  }

  public getPendingReports(): ErrorReport[] {
    return [...this.errorQueue];
  }

  public showUserFeedbackDialog(errorId: string): void {
    if (!this.config.enableUserFeedback) return;

    // Create a simple feedback modal
    const modal = document.createElement('div');
    modal.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 10000;
    `;

    const form = document.createElement('div');
    form.style.cssText = `
      background: white;
      padding: 2rem;
      border-radius: 8px;
      max-width: 500px;
      width: 90%;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
    `;

    form.innerHTML = `
      <h3 style="margin: 0 0 1rem 0; color: #333;">Something went wrong</h3>
      <p style="color: #666; margin-bottom: 1rem;">
        We're sorry for the inconvenience. Would you like to provide feedback about what happened?
      </p>
      <form id="error-feedback-form">
        <div style="margin-bottom: 1rem;">
          <label style="display: block; margin-bottom: 0.5rem; color: #333;">
            Email (optional):
          </label>
          <input
            type="email"
            id="user-email"
            style="width: 100%; padding: 0.5rem; border: 1px solid #ddd; border-radius: 4px;"
            placeholder="your.email@example.com"
          />
        </div>
        <div style="margin-bottom: 1rem;">
          <label style="display: block; margin-bottom: 0.5rem; color: #333;">
            What were you doing when this happened?
          </label>
          <textarea
            id="user-comments"
            rows="4"
            style="width: 100%; padding: 0.5rem; border: 1px solid #ddd; border-radius: 4px; resize: vertical;"
            placeholder="Describe what you were trying to do..."
            required
          ></textarea>
        </div>
        <div style="display: flex; gap: 1rem; justify-content: flex-end;">
          <button
            type="button"
            id="cancel-feedback"
            style="padding: 0.5rem 1rem; border: 1px solid #ddd; background: white; border-radius: 4px; cursor: pointer;"
          >
            Cancel
          </button>
          <button
            type="submit"
            style="padding: 0.5rem 1rem; border: none; background: #007bff; color: white; border-radius: 4px; cursor: pointer;"
          >
            Send Feedback
          </button>
        </div>
      </form>
    `;

    modal.appendChild(form);
    document.body.appendChild(modal);

    // Handle form submission
    const feedbackForm = form.querySelector('#error-feedback-form') as HTMLFormElement;
    const cancelButton = form.querySelector('#cancel-feedback') as HTMLButtonElement;
    const emailInput = form.querySelector('#user-email') as HTMLInputElement;
    const commentsInput = form.querySelector('#user-comments') as HTMLTextAreaElement;

    feedbackForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      
      await this.submitUserFeedback(errorId, {
        userEmail: emailInput.value || undefined,
        comments: commentsInput.value,
        context: {
          url: window.location.href,
          userAgent: navigator.userAgent
        }
      });

      document.body.removeChild(modal);
    });

    cancelButton.addEventListener('click', () => {
      document.body.removeChild(modal);
    });

    // Close on background click
    modal.addEventListener('click', (e) => {
      if (e.target === modal) {
        document.body.removeChild(modal);
      }
    });
  }

  public destroy(): void {
    // Clean up event listeners
    if (this.unhandledErrorListener) {
      window.removeEventListener('error', this.unhandledErrorListener);
    }
    if (this.unhandledRejectionListener) {
      window.removeEventListener('unhandledrejection', this.unhandledRejectionListener);
    }

    // Submit any pending reports
    this.submitPendingReports();
  }
}

// Create singleton instance
export const errorReporter = new ErrorReportingService();

// React Error Boundary helper
export const withErrorBoundary = <P extends Record<string, unknown>>(Component: React.ComponentType<P>) => {
  return class ErrorBoundary extends React.Component<P, { hasError: boolean; errorId?: string }> {
    constructor(props: P) {
      super(props);
      this.state = { hasError: false };
    }

    static getDerivedStateFromError(_error: Error) {
      return { hasError: true };
    }

    async componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
      const errorId = await errorReporter.captureError(error, {
        component: Component.displayName || Component.name || 'Unknown',
        action: 'render',
        extra: {
          errorInfo: {
            componentStack: errorInfo.componentStack
          },
          props: this.props as ErrorExtraValue
        }
      });

      this.setState({ errorId });

      // Show user feedback dialog for critical errors
      if (errorReporter.getPendingReports().some(r => r.severity === 'critical')) {
        errorReporter.showUserFeedbackDialog(errorId);
      }
    }

    render() {
      if (this.state.hasError) {
        return createElement('div', {
          style: { padding: '2rem', textAlign: 'center', background: '#f8f9fa', border: '1px solid #dee2e6', borderRadius: '0.25rem' }
        }, [
          createElement('h3', { key: 'title' }, 'Something went wrong'),
          createElement('p', { key: 'message' }, "We've been notified about this error and are working to fix it."),
          this.state.errorId && createElement('p', {
            key: 'errorId',
            style: { fontSize: '0.875rem', color: '#6c757d' }
          }, `Error ID: ${this.state.errorId}`),
          createElement('button', {
            key: 'retry',
            onClick: () => this.setState({ hasError: false }),
            style: { padding: '0.5rem 1rem', marginTop: '1rem', border: 'none', background: '#007bff', color: 'white', borderRadius: '0.25rem', cursor: 'pointer' }
          }, 'Try Again')
        ].filter(Boolean));
      }

      return createElement(Component, this.props);
    }
  };
};