// @ts-nocheck
/**
 * ProtectedRoute Component - Route protection with role-based access control
 * Handles authentication and authorization for protected pages
 */

import React, { useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth, useAuthLoading } from '../../hooks/useAuth';
import { ProtectedRouteProps, UserRole } from '../../types/auth';
import { cn } from '../../utils/cn';

// Loading spinner component
const LoadingSpinner: React.FC<{ size?: 'sm' | 'md' | 'lg' }> = ({ size = 'md' }) => {
  const sizeClasses = {
    sm: 'w-6 h-6',
    md: 'w-8 h-8',
    lg: 'w-12 h-12',
  };

  return (
    <div className="flex items-center justify-center min-h-[200px]">
      <div className="flex flex-col items-center space-y-4">
        <svg
          className={cn('animate-spin text-blue-600', sizeClasses[size])}
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle
            className="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            strokeWidth="4"
          />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
        <p className="text-sm text-gray-600">Loading...</p>
      </div>
    </div>
  );
};

// Unauthorized access component
const UnauthorizedAccess: React.FC<{
  message?: string;
  showLoginButton?: boolean;
  onLogin?: () => void;
}> = ({
  message = "You don't have permission to access this page.",
  showLoginButton = true,
  onLogin,
}) => {
  return (
    <div className="flex items-center justify-center min-h-[400px] bg-gray-50">
      <div className="max-w-md mx-auto text-center px-4">
        <div className="mx-auto flex items-center justify-center w-16 h-16 bg-red-100 rounded-full mb-6">
          <svg
            className="w-8 h-8 text-red-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.5 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z"
            />
          </svg>
        </div>

        <h1 className="text-2xl font-bold text-gray-900 mb-4">
          Access Denied
        </h1>

        <p className="text-gray-600 mb-6">
          {message}
        </p>

        {showLoginButton && (
          <div className="space-y-3">
            {onLogin ? (
              <button
                onClick={onLogin}
                className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors"
              >
                Sign In
              </button>
            ) : (
              <a
                href="/login"
                className="inline-block w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors text-center"
              >
                Sign In
              </a>
            )}

            <a
              href="/"
              className="inline-block w-full px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors text-center"
            >
              Go Home
            </a>
          </div>
        )}
      </div>
    </div>
  );
};

// Email verification required component
const EmailVerificationRequired: React.FC<{
  onResendVerification?: () => void;
  isResending?: boolean;
}> = ({ onResendVerification, isResending = false }) => {
  const [resendCooldown, setResendCooldown] = useState(0);

  useEffect(() => {
    if (resendCooldown > 0) {
      const timer = setInterval(() => {
        setResendCooldown(prev => prev - 1);
      }, 1000);

      return () => clearInterval(timer);
    }
  }, [resendCooldown]);

  const handleResend = () => {
    if (onResendVerification && resendCooldown === 0) {
      onResendVerification();
      setResendCooldown(60); // 60 second cooldown
    }
  };

  return (
    <div className="flex items-center justify-center min-h-[400px] bg-yellow-50">
      <div className="max-w-md mx-auto text-center px-4">
        <div className="mx-auto flex items-center justify-center w-16 h-16 bg-yellow-100 rounded-full mb-6">
          <svg
            className="w-8 h-8 text-yellow-600"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M3 8l7.89 4.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
            />
          </svg>
        </div>

        <h1 className="text-2xl font-bold text-gray-900 mb-4">
          Email Verification Required
        </h1>

        <p className="text-gray-600 mb-6">
          Please verify your email address to access this page. Check your inbox for a verification email.
        </p>

        {onResendVerification && (
          <div className="space-y-3">
            <button
              onClick={handleResend}
              disabled={isResending || resendCooldown > 0}
              className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isResending
                ? 'Sending...'
                : resendCooldown > 0
                ? `Resend in ${resendCooldown}s`
                : 'Resend Verification Email'
              }
            </button>

            <a
              href="/"
              className="inline-block w-full px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors text-center"
            >
              Go Home
            </a>
          </div>
        )}
      </div>
    </div>
  );
};

// Main ProtectedRoute component
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  roles,
  requireEmailVerification = false,
  fallback,
  redirectTo = '/login',
}) => {
  const location = useLocation();
  const { isAuthenticated, user, resendVerificationEmail } = useAuth();
  const { isLoading } = useAuthLoading();

  const [isResendingVerification, setIsResendingVerification] = useState(false);

  // Handle email verification resend
  const handleResendVerification = async () => {
    setIsResendingVerification(true);
    try {
      await resendVerificationEmail();
    } catch (error) {
      console.error('Failed to resend verification email:', error);
    } finally {
      setIsResendingVerification(false);
    }
  };

  // Show loading spinner while auth is being determined
  if (isLoading) {
    return fallback || <LoadingSpinner />;
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated || !user) {
    const returnUrl = `${location.pathname}${location.search}`;
    const loginUrl = `${redirectTo}?returnUrl=${encodeURIComponent(returnUrl)}`;
    return <Navigate to={loginUrl} replace />;
  }

  // Check email verification requirement
  if (requireEmailVerification && !user.isEmailVerified) {
    return (
      <EmailVerificationRequired
        onResendVerification={handleResendVerification}
        isResending={isResendingVerification}
      />
    );
  }

  // Check role-based access
  if (roles && roles.length > 0) {
    const hasRequiredRole = roles.includes(user.role);

    if (!hasRequiredRole) {
      const roleNames = roles.map(role => UserRole[role]).join(', ');
      return (
        <UnauthorizedAccess
          message={`This page requires one of the following roles: ${roleNames}.`}
          showLoginButton={false}
        />
      );
    }
  }

  // User is authenticated and authorized
  return <>{children}</>;
};

// Higher-order component version
export const withProtectedRoute = <P extends object>(
  Component: React.ComponentType<P>,
  routeProps: Omit<ProtectedRouteProps, 'children'>
) => {
  const WrappedComponent = (props: P) => (
    <ProtectedRoute {...routeProps}>
      <Component {...props} />
    </ProtectedRoute>
  );
  WrappedComponent.displayName = `withProtectedRoute(${Component.displayName || Component.name})`;
  return WrappedComponent;
};

// Role-specific route components for convenience
export const AdminRoute: React.FC<Pick<ProtectedRouteProps, 'children' | 'fallback' | 'redirectTo'>> = (props) => (
  <ProtectedRoute roles={[UserRole.Admin]} {...props} />
);

export const AuthorRoute: React.FC<Pick<ProtectedRouteProps, 'children' | 'fallback' | 'redirectTo'>> = (props) => (
  <ProtectedRoute roles={[UserRole.Author, UserRole.Admin]} {...props} />
);

export const VerifiedUserRoute: React.FC<Pick<ProtectedRouteProps, 'children' | 'fallback' | 'redirectTo'>> = (props) => (
  <ProtectedRoute requireEmailVerification {...props} />
);

// Hook for programmatic route protection checks
export const useRouteProtection = () => {
  const { isAuthenticated, user } = useAuth();
  const { isLoading } = useAuthLoading();

  const checkAccess = (
    roles?: UserRole[],
    requireEmailVerification = false
  ): {
    hasAccess: boolean;
    reason?: 'not_authenticated' | 'insufficient_role' | 'email_not_verified' | 'loading';
  } => {
    if (isLoading) {
      return { hasAccess: false, reason: 'loading' };
    }

    if (!isAuthenticated || !user) {
      return { hasAccess: false, reason: 'not_authenticated' };
    }

    if (requireEmailVerification && !user.isEmailVerified) {
      return { hasAccess: false, reason: 'email_not_verified' };
    }

    if (roles && roles.length > 0 && !roles.includes(user.role)) {
      return { hasAccess: false, reason: 'insufficient_role' };
    }

    return { hasAccess: true };
  };

  const redirectToLogin = (returnUrl?: string) => {
    const url = returnUrl ? `/login?returnUrl=${encodeURIComponent(returnUrl)}` : '/login';
    window.location.href = url;
  };

  return {
    checkAccess,
    redirectToLogin,
    isAuthenticated,
    user,
    isLoading,
  };
};

export default ProtectedRoute;