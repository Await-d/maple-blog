/**
 * ErrorBoundaryUtils - Utility functions and hooks for error boundaries
 * Separated to maintain Fast Refresh compatibility
 */

import React from 'react';
import { AdminErrorBoundary } from './ErrorBoundary';
import type { ErrorBoundaryProps } from './ErrorBoundary';

// Simple functional error boundary wrapper
export const withErrorBoundary = <P extends object>(
  Component: React.ComponentType<P>,
  errorBoundaryProps?: Omit<ErrorBoundaryProps, 'children'>
) => {
  const WrappedComponent = (props: P) => (
    <AdminErrorBoundary {...errorBoundaryProps}>
      <Component {...props} />
    </AdminErrorBoundary>
  );

  WrappedComponent.displayName = `withErrorBoundary(${Component.displayName || Component.name})`;
  return WrappedComponent;
};

// Hook to trigger error boundary from functional components
export const useErrorBoundary = () => {
  const [error, setError] = React.useState<Error | null>(null);

  const captureError = React.useCallback((error: Error | string) => {
    const errorObj = typeof error === 'string' ? new Error(error) : error;
    setError(errorObj);
  }, []);

  React.useEffect(() => {
    if (error) {
      throw error;
    }
  }, [error]);

  return captureError;
};