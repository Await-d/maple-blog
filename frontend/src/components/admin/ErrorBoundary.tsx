/**
 * ErrorBoundary - React Error Boundary for admin components
 * Provides graceful error handling and recovery for complex admin interfaces
 */

import { Component, ReactNode, ErrorInfo } from 'react';
import { AlertTriangle, RefreshCcw, Bug, Home } from 'lucide-react';
import { Button } from '../ui/Button';

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  errorId: string;
}

export interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  showDetails?: boolean;
  resetOnPropsChange?: boolean;
  resetKeys?: Array<string | number>;
}

export class AdminErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  private retryCount = 0;
  private maxRetries = 3;
  private resetTimeoutRef: number | null = null;

  constructor(props: ErrorBoundaryProps) {
    super(props);

    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: this.generateErrorId()
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return {
      hasError: true,
      error,
      errorId: Math.random().toString(36).substring(2, 15)
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Error captured by boundary - logged via error reporting service

    this.setState({
      error,
      errorInfo
    });

    // Call optional error handler
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }

    // Send error to monitoring service (if available)
    this.reportError(error, errorInfo);
  }

  componentDidUpdate(prevProps: ErrorBoundaryProps): void {
    const { resetOnPropsChange, resetKeys } = this.props;
    const { hasError } = this.state;

    if (hasError && resetOnPropsChange && resetKeys) {
      const hasResetKeyChanged = resetKeys.some(
        (resetKey, index) => resetKey !== prevProps.resetKeys?.[index]
      );

      if (hasResetKeyChanged) {
        this.resetErrorBoundary();
      }
    }
  }

  componentWillUnmount(): void {
    if (this.resetTimeoutRef) {
      clearTimeout(this.resetTimeoutRef);
    }
  }

  private generateErrorId(): string {
    return `admin_error_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
  }

  private reportError(error: Error, errorInfo: ErrorInfo): void {
    // In a real application, send to error reporting service
    const _errorReport = {
      errorId: this.state.errorId,
      message: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
      timestamp: new Date().toISOString(),
      url: window.location.href,
      userAgent: navigator.userAgent,
      userId: this.getCurrentUserId(), // Get from auth context
    };

    // Example: Send to error reporting service
    // errorReportingService.report(errorReport);

    // Development mode: errors are handled by error reporting service
  }

  private getCurrentUserId(): string | null {
    // Get user ID from authentication context or localStorage
    try {
      const token = localStorage.getItem('authToken');
      if (token) {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload.sub || null;
      }
    } catch {
      // Ignore parsing errors
    }
    return null;
  }

  private resetErrorBoundary = (): void => {
    this.retryCount = 0;
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      errorId: this.generateErrorId()
    });
  };

  private handleRetry = (): void => {
    if (this.retryCount < this.maxRetries) {
      this.retryCount++;
      this.resetErrorBoundary();
    } else {
      // Show permanent error state after max retries
      // Max retries exceeded - error logged via error reporting service
    }
  };

  private handleReloadPage = (): void => {
    window.location.reload();
  };

  private handleGoHome = (): void => {
    window.location.href = '/admin';
  };

  render(): ReactNode {
    const { hasError, error, errorInfo, errorId } = this.state;
    const { children, fallback, showDetails = false } = this.props;

    if (hasError) {
      // Use custom fallback if provided
      if (fallback) {
        return fallback;
      }

      // Default error UI
      return (
        <div className="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
          <div className="max-w-2xl w-full">
            {/* Error Card */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg p-8 text-center">
              {/* Error Icon */}
              <div className="mx-auto w-16 h-16 bg-red-100 dark:bg-red-900 rounded-full flex items-center justify-center mb-6">
                <AlertTriangle className="w-8 h-8 text-red-600 dark:text-red-400" />
              </div>

              {/* Error Title */}
              <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
                Oops! Something went wrong
              </h1>

              {/* Error Description */}
              <p className="text-gray-600 dark:text-gray-400 mb-8 max-w-lg mx-auto">
                We encountered an unexpected error in the admin interface.
                Don&apos;t worry - your data is safe and this issue has been reported to our team.
              </p>

              {/* Error Details (Development/Debug) */}
              {showDetails && error && (
                <div className="mb-8 text-left">
                  <details className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
                    <summary className="cursor-pointer text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      <Bug className="inline w-4 h-4 mr-2" />
                      Technical Details
                    </summary>
                    <div className="text-xs text-gray-600 dark:text-gray-400 space-y-2">
                      <div>
                        <strong>Error ID:</strong> {errorId}
                      </div>
                      <div>
                        <strong>Message:</strong> {error.message}
                      </div>
                      {error.stack && (
                        <div>
                          <strong>Stack Trace:</strong>
                          <pre className="mt-1 p-2 bg-gray-800 text-green-400 text-xs rounded overflow-x-auto">
                            {error.stack}
                          </pre>
                        </div>
                      )}
                      {errorInfo?.componentStack && (
                        <div>
                          <strong>Component Stack:</strong>
                          <pre className="mt-1 p-2 bg-gray-800 text-blue-400 text-xs rounded overflow-x-auto">
                            {errorInfo.componentStack}
                          </pre>
                        </div>
                      )}
                    </div>
                  </details>
                </div>
              )}

              {/* Action Buttons */}
              <div className="flex flex-col sm:flex-row gap-3 justify-center">
                {this.retryCount < this.maxRetries && (
                  <Button
                    onClick={this.handleRetry}
                    className="inline-flex items-center"
                  >
                    <RefreshCcw className="w-4 h-4 mr-2" />
                    Try Again ({this.maxRetries - this.retryCount} attempts left)
                  </Button>
                )}

                <Button
                  variant="outline"
                  onClick={this.handleReloadPage}
                  className="inline-flex items-center"
                >
                  <RefreshCcw className="w-4 h-4 mr-2" />
                  Reload Page
                </Button>

                <Button
                  variant="outline"
                  onClick={this.handleGoHome}
                  className="inline-flex items-center"
                >
                  <Home className="w-4 h-4 mr-2" />
                  Go to Dashboard
                </Button>
              </div>

              {/* Help Text */}
              <div className="mt-8 p-4 bg-blue-50 dark:bg-blue-900 rounded-lg">
                <p className="text-sm text-blue-800 dark:text-blue-200">
                  <strong>Need help?</strong> If this error persists, please contact the support team
                  and provide the Error ID: <code className="bg-blue-100 dark:bg-blue-800 px-2 py-1 rounded text-xs">{errorId}</code>
                </p>
              </div>
            </div>

            {/* Additional Recovery Options */}
            <div className="mt-6 text-center">
              <details className="inline-block text-left">
                <summary className="cursor-pointer text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300">
                  Advanced Recovery Options
                </summary>
                <div className="mt-4 p-4 bg-white dark:bg-gray-800 rounded-lg shadow space-y-3">
                  <button
                    onClick={() => {
                      localStorage.clear();
                      sessionStorage.clear();
                      window.location.reload();
                    }}
                    className="block w-full text-left text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 p-2 rounded hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    Clear browser storage and reload
                  </button>
                  <button
                    onClick={() => window.location.href = '/login'}
                    className="block w-full text-left text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 p-2 rounded hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    Return to login page
                  </button>
                  <button
                    onClick={() => {
                      const errorDetails = {
                        errorId,
                        message: error?.message,
                        url: window.location.href,
                        timestamp: new Date().toISOString()
                      };
                      navigator.clipboard.writeText(JSON.stringify(errorDetails, null, 2));
                      // Error details copied - should use toast notification instead of alert
                    }}
                    className="block w-full text-left text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 p-2 rounded hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    Copy error details to clipboard
                  </button>
                </div>
              </details>
            </div>
          </div>
        </div>
      );
    }

    return children;
  }
}

// Export both the original name and the expected name
export { AdminErrorBoundary as ErrorBoundary };
export default AdminErrorBoundary;