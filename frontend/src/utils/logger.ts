/**
 * Logger utility functions and convenient wrappers
 * Provides easy-to-use logging functions with component-aware context
 * and performance monitoring capabilities
 */

import { logger, LogContext, LogLevel } from '@/services/loggingService';
import { errorReporter, ErrorContext, ErrorExtraValue } from '@/services/errorReporting';
import { analytics, EventProperties } from '@/services/analytics';

// Type for JSON-serializable values used in logging utilities
export type JsonValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | Date
  | Error
  | JsonValue[]
  | { [key: string]: JsonValue };

// Type for the replacer function in JSON.stringify
export type JsonReplacer = (key: string, value: JsonValue) => JsonValue | undefined;

// Type for objects that can be masked (only objects and arrays)
export type MaskableValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | MaskableValue[]
  | { [key: string]: MaskableValue };

// Component-aware logger that automatically includes component context
export class ComponentLogger {
  private componentName: string;
  private baseContext: LogContext;

  constructor(componentName: string, additionalContext: LogContext = {}) {
    this.componentName = componentName;
    this.baseContext = {
      component: componentName,
      ...additionalContext
    };
  }

  private mergeContext(context: LogContext = {}): LogContext {
    return {
      ...this.baseContext,
      ...context,
      component: this.componentName // Ensure component name is not overridden
    };
  }

  debug(message: string, action?: string, context: LogContext = {}): void {
    logger.debug(message, this.mergeContext({
      action,
      ...context
    }));
  }

  info(message: string, action?: string, context: LogContext = {}): void {
    logger.info(message, this.mergeContext({
      action,
      ...context
    }));
  }

  warn(message: string, action?: string, context: LogContext = {}, error?: Error): void {
    logger.warn(message, this.mergeContext({
      action,
      ...context
    }), error);
  }

  error(message: string, action?: string, context: LogContext = {}, error?: Error): void {
    logger.error(message, this.mergeContext({
      action,
      ...context
    }), error);

    // Also report to error reporting service
    if (error) {
      errorReporter.captureError(error, {
        ...this.mergeContext(context),
        action
      } as ErrorContext);
    }

    // Track error in analytics
    if (error) {
      analytics.trackError(error, this.mergeContext({
        action,
        ...context
      }));
    }
  }

  fatal(message: string, action?: string, context: LogContext = {}, error?: Error): void {
    logger.fatal(message, this.mergeContext({
      action,
      ...context
    }), error);

    // Always report fatal errors
    if (error) {
      errorReporter.captureError(error, {
        ...this.mergeContext(context),
        action,
        handled: false
      } as ErrorContext);
    }
  }

  // Performance logging methods
  startTimer(timerName: string, action?: string): void {
    logger.startPerformanceTimer(`${this.componentName}-${timerName}`);
    this.debug(`Started timer: ${timerName}`, action || 'performance');
  }

  endTimer(timerName: string, action?: string, context: LogContext = {}): void {
    logger.endPerformanceTimer(`${this.componentName}-${timerName}`, this.mergeContext({
      action: action || 'performance',
      ...context
    }));
    this.debug(`Ended timer: ${timerName}`, action || 'performance');
  }

  // API request logging
  logApiRequest(method: string, url: string, context: LogContext = {}): void {
    this.info(`API ${method.toUpperCase()}: ${url}`, 'apiRequest', {
      httpMethod: method,
      requestUrl: url,
      ...context
    });
  }

  logApiResponse(method: string, url: string, status: number, duration: number, context: LogContext = {}): void {
    const level = status >= 400 ? 'error' : status >= 300 ? 'warn' : 'info';
    
    this[level](`API ${method.toUpperCase()}: ${url} - ${status} (${duration}ms)`, 'apiResponse', {
      httpMethod: method,
      requestUrl: url,
      responseStatus: status,
      responseDuration: duration,
      ...context
    });
  }

  logApiError(method: string, url: string, error: Error, context: LogContext = {}): void {
    this.error(`API ${method.toUpperCase()}: ${url} failed`, 'apiError', {
      httpMethod: method,
      requestUrl: url,
      errorMessage: error.message,
      ...context
    }, error);
  }

  // User interaction logging
  logUserAction(action: string, target?: string, context: LogContext = {}): void {
    this.info(`User action: ${action}${target ? ` on ${target}` : ''}`, 'userAction', {
      userAction: action,
      actionTarget: target,
      ...context
    });

    // Also track in analytics
    analytics.track(action, 'UserInteraction', {
      component: this.componentName,
      target,
      ...context
    });
  }

  logUserError(action: string, errorMessage: string, context: LogContext = {}): void {
    this.warn(`User error during ${action}: ${errorMessage}`, 'userError', {
      userAction: action,
      userErrorMessage: errorMessage,
      ...context
    });
  }

  // Form logging
  logFormSubmission(formName: string, success: boolean, context: LogContext = {}): void {
    const message = `Form ${formName} ${success ? 'submitted successfully' : 'submission failed'}`;
    const level = success ? 'info' : 'warn';
    
    this[level](message, 'formSubmission', {
      formName,
      formSuccess: success,
      ...context
    });

    // Track form submission in analytics
    analytics.track(`form_${success ? 'success' : 'error'}`, 'Form', {
      component: this.componentName,
      formName,
      ...context
    });
  }

  logFormValidation(formName: string, fieldErrors: Record<string, string[]>, context: LogContext = {}): void {
    this.warn(`Form ${formName} validation errors`, 'formValidation', {
      formName,
      fieldErrors,
      errorCount: Object.keys(fieldErrors).length,
      ...context
    });
  }

  // Data loading logging
  logDataLoading(operation: string, context: LogContext = {}): void {
    this.info(`Loading ${operation}`, 'dataLoading', {
      operation,
      ...context
    });
  }

  logDataSuccess(operation: string, duration: number, recordCount?: number, context: LogContext = {}): void {
    this.info(`Loaded ${operation} successfully (${duration}ms)${recordCount ? ` - ${recordCount} records` : ''}`, 'dataSuccess', {
      operation,
      duration,
      recordCount,
      ...context
    });
  }

  logDataError(operation: string, error: Error, context: LogContext = {}): void {
    this.error(`Failed to load ${operation}`, 'dataError', {
      operation,
      errorMessage: error.message,
      ...context
    }, error);
  }

  // State change logging
  logStateChange(from: string, to: string, context: LogContext = {}): void {
    this.debug(`State changed: ${from} → ${to}`, 'stateChange', {
      previousState: from,
      newState: to,
      ...context
    });
  }

  logError(error: Error | string, action?: string, context: LogContext = {}, reportError = true): void {
    const errorObj = typeof error === 'string' ? new Error(error) : error;
    
    this.error(errorObj.message, action, context, errorObj);
    
    if (reportError) {
      errorReporter.captureError(errorObj, {
        ...this.mergeContext(context),
        action
      } as ErrorContext);
    }
  }
}

// Convenient factory function
export const createLogger = (componentName: string, additionalContext?: LogContext): ComponentLogger => {
  return new ComponentLogger(componentName, additionalContext);
};

// Global utility functions for quick logging without component context
export const logDebug = (message: string, context: LogContext = {}): void => {
  logger.debug(message, context);
};

export const logInfo = (message: string, context: LogContext = {}): void => {
  logger.info(message, context);
};

export const logWarn = (message: string, context: LogContext = {}, error?: Error): void => {
  logger.warn(message, context, error);
};

export const logError = (message: string, context: LogContext = {}, error?: Error): void => {
  logger.error(message, context, error);
  
  if (error) {
    errorReporter.captureError(error, context as ErrorContext);
    analytics.trackError(error, context);
  }
};

export const logFatal = (message: string, context: LogContext = {}, error?: Error): void => {
  logger.fatal(message, context, error);
  
  if (error) {
    errorReporter.captureError(error, { 
      ...context, 
      handled: false 
    } as ErrorContext);
  }
};

// Performance measurement utilities
export const measurePerformance = async <T>(
  operation: string,
  fn: () => Promise<T>,
  context: LogContext = {}
): Promise<T> => {
  const startTime = performance.now();
  logger.startPerformanceTimer(operation);
  
  try {
    const result = await fn();
    const duration = performance.now() - startTime;
    
    logger.endPerformanceTimer(operation, {
      ...context,
      duration,
      success: true
    });
    
    logInfo(`Operation ${operation} completed successfully in ${duration.toFixed(2)}ms`, {
      ...context,
      operation,
      duration,
      success: true
    });
    
    return result;
  } catch (error) {
    const duration = performance.now() - startTime;
    
    logger.endPerformanceTimer(operation, {
      ...context,
      duration,
      success: false,
      error: (error as Error).message
    });
    
    logError(`Operation ${operation} failed after ${duration.toFixed(2)}ms`, {
      ...context,
      operation,
      duration,
      success: false
    }, error as Error);
    
    throw error;
  }
};

// Synchronous performance measurement
export const measurePerformanceSync = <T>(
  operation: string,
  fn: () => T,
  context: LogContext = {}
): T => {
  const startTime = performance.now();
  logger.startPerformanceTimer(operation);
  
  try {
    const result = fn();
    const duration = performance.now() - startTime;
    
    logger.endPerformanceTimer(operation, {
      ...context,
      duration,
      success: true
    });
    
    logDebug(`Sync operation ${operation} completed in ${duration.toFixed(2)}ms`, {
      ...context,
      operation,
      duration,
      success: true
    });
    
    return result;
  } catch (error) {
    const duration = performance.now() - startTime;
    
    logger.endPerformanceTimer(operation, {
      ...context,
      duration,
      success: false,
      error: (error as Error).message
    });
    
    logError(`Sync operation ${operation} failed after ${duration.toFixed(2)}ms`, {
      ...context,
      operation,
      duration,
      success: false
    }, error as Error);
    
    throw error;
  }
};

// API request wrapper with automatic logging
export const loggedFetch = async (
  url: string,
  options: RequestInit = {},
  context: LogContext = {}
): Promise<Response> => {
  const method = options.method || 'GET';
  const startTime = performance.now();
  
  logInfo(`API ${method.toUpperCase()}: ${url}`, {
    ...context,
    httpMethod: method,
    requestUrl: url,
    action: 'apiRequest'
  });
  
  try {
    const response = await fetch(url, options);
    const duration = performance.now() - startTime;
    
    if (response.ok) {
      logInfo(`API ${method.toUpperCase()}: ${url} - ${response.status} (${duration.toFixed(2)}ms)`, {
        ...context,
        httpMethod: method,
        requestUrl: url,
        responseStatus: response.status,
        responseDuration: duration,
        action: 'apiResponse'
      });
    } else {
      logWarn(`API ${method.toUpperCase()}: ${url} - ${response.status} (${duration.toFixed(2)}ms)`, {
        ...context,
        httpMethod: method,
        requestUrl: url,
        responseStatus: response.status,
        responseDuration: duration,
        action: 'apiResponse'
      });
    }
    
    return response;
  } catch (error) {
    const duration = performance.now() - startTime;
    
    logError(`API ${method.toUpperCase()}: ${url} failed after ${duration.toFixed(2)}ms`, {
      ...context,
      httpMethod: method,
      requestUrl: url,
      responseDuration: duration,
      action: 'apiError'
    }, error as Error);
    
    throw error;
  }
};

// React Hook for component logging
export const useLogger = (componentName: string, additionalContext?: LogContext): ComponentLogger => {
  return new ComponentLogger(componentName, additionalContext);
};

// Debugging utilities
export const logWithStack = (message: string, level: 'debug' | 'info' | 'warn' | 'error' = 'debug', context: LogContext = {}): void => {
  const stack = new Error().stack;
  const logFn = {
    debug: logDebug,
    info: logInfo,
    warn: logWarn,
    error: logError
  }[level];
  
  logFn(message, {
    ...context,
    stack,
    callerInfo: stack?.split('\n')[2]?.trim() || 'unknown'
  });
};

// Breadcrumb helper
export const addBreadcrumb = (message: string, category: string, level: 'debug' | 'info' | 'warning' | 'error' = 'info', data?: Record<string, ErrorExtraValue>): void => {
  errorReporter.addBreadcrumb(message, category, level, data);
};

// Development only logging
export const logDev = (message: string, context: LogContext = {}): void => {
  if (process.env.NODE_ENV === 'development') {
    logDebug(`[DEV] ${message}`, context);
  }
};

// Production error reporting
export const reportError = (error: Error, context: ErrorContext = {}): Promise<string> => {
  return errorReporter.captureError(error, context);
};

// Analytics helpers
export const trackEvent = (eventName: string, category: string, properties: EventProperties = {}): void => {
  analytics.track(eventName, category, properties);
};

export const trackUserAction = (action: string, component?: string, properties: EventProperties = {}): void => {
  analytics.track(action, 'UserInteraction', {
    component,
    ...properties
  });
};

export const trackError = (error: Error, context: LogContext = {}): void => {
  analytics.trackError(error, context);
};

export const trackPageView = (url?: string, title?: string): void => {
  analytics.trackPageView(url, title);
};

// Utility function to safely stringify objects for logging
export const safeStringify = (obj: JsonValue, replacer?: JsonReplacer, space?: string | number): string => {
  try {
    return JSON.stringify(obj, replacer as ((key: string, value: unknown) => unknown) | undefined, space);
  } catch (error) {
    return `[Unable to stringify: ${(error as Error).message}]`;
  }
};

// Mask sensitive data in logs
export const maskSensitiveData = <T extends MaskableValue>(obj: T): MaskableValue => {
  if (typeof obj !== 'object' || obj === null) {
    return obj;
  }

  if (Array.isArray(obj)) {
    return obj.map(item => maskSensitiveData(item)) as MaskableValue[];
  }

  const masked: Record<string, MaskableValue> = { ...(obj as Record<string, MaskableValue>) };
  const sensitiveKeys = [
    'password', 'token', 'apiKey', 'secret', 'auth', 'authorization',
    'creditCard', 'ssn', 'email', 'phone', 'address'
  ];

  for (const key in masked) {
    if (sensitiveKeys.some(sensitiveKey => key.toLowerCase().includes(sensitiveKey))) {
      masked[key] = '***MASKED***';
    } else if (typeof masked[key] === 'object' && masked[key] !== null) {
      masked[key] = maskSensitiveData(masked[key] as MaskableValue);
    }
  }

  return masked;
};

// Log level configuration
export const setLogLevel = (level: LogLevel): void => {
  logger.setLogLevel(level);
};

export const getStoredLogs = () => {
  return logger.getStoredLogs();
};

export const clearStoredLogs = (): void => {
  logger.clearStoredLogs();
};

export const exportLogs = (format: 'json' | 'csv' = 'json'): string => {
  return logger.exportLogs(format);
};

// Default export for convenience
export default {
  createLogger,
  logDebug,
  logInfo,
  logWarn,
  logError,
  logFatal,
  measurePerformance,
  measurePerformanceSync,
  loggedFetch,
  useLogger,
  logWithStack,
  addBreadcrumb,
  logDev,
  reportError,
  trackEvent,
  trackUserAction,
  trackError,
  trackPageView,
  safeStringify,
  maskSensitiveData,
  setLogLevel,
  getStoredLogs,
  clearStoredLogs,
  exportLogs
};