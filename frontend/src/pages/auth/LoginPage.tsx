/**
 * LoginPage - Login page with form and layout
 * Provides complete login experience with navigation and SEO
 */

import React, { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Helmet } from '@/components/common/DocumentHead';
import { LoginForm } from '../../features/auth/components/LoginForm';
import { useAuth } from '../../hooks/useAuth';
import { cn } from '../../utils/cn';

// Page props interface
interface LoginPageProps {
  className?: string;
}

// LoginPage component
export const LoginPage: React.FC<LoginPageProps> = ({ className }) => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAuthenticated } = useAuth();

  const returnUrl = searchParams.get('returnUrl');
  const message = searchParams.get('message');

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      if (returnUrl && returnUrl.startsWith('/')) {
        navigate(returnUrl, { replace: true });
      } else {
        navigate('/dashboard', { replace: true });
      }
    }
  }, [isAuthenticated, navigate, returnUrl]);

  // Handle successful login
  const handleLoginSuccess = () => {
    if (returnUrl && returnUrl.startsWith('/')) {
      navigate(returnUrl, { replace: true });
    } else {
      navigate('/dashboard', { replace: true });
    }
  };

  // Don't render if already authenticated (prevents flash)
  if (isAuthenticated) {
    return null;
  }

  return (
    <>
      {/* SEO Meta Tags */}
      <Helmet>
        <title>Sign In - Maple Blog</title>
        <meta
          name="description"
          content="Sign in to your Maple Blog account to access your dashboard, manage your posts, and connect with the community."
        />
        <meta name="robots" content="noindex, nofollow" />
        <link rel="canonical" href={`${window.location.origin}/login`} />
      </Helmet>

      {/* Page Container */}
      <div className={cn('min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8', className)}>
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          {/* Logo/Brand */}
          <div className="text-center mb-8">
            <h1 className="text-4xl font-bold text-gray-900 mb-2">
              🍁 Maple Blog
            </h1>
            <p className="text-gray-600">
              Share your thoughts with the world
            </p>
          </div>

          {/* Info Message Display */}
          {message && (
            <div className="mb-6">
              <div className="p-4 bg-blue-50 border border-blue-200 rounded-md">
                <div className="flex items-center">
                  <div className="flex-shrink-0">
                    <svg
                      className="w-5 h-5 text-blue-400"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      aria-hidden="true"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <p className="text-sm text-blue-800">
                      {decodeURIComponent(message)}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Login Form Card */}
          <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
            <LoginForm
              onSuccess={handleLoginSuccess}
              className="max-w-none"
              showTitle={false}
              showRegisterLink={true}
              showForgotPasswordLink={true}
            />
          </div>

          {/* Additional Links */}
          <div className="mt-8 text-center">
            <div className="text-sm text-gray-600">
              <a
                href="/"
                className="font-medium text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
              >
                ← Back to Home
              </a>
            </div>
          </div>

          {/* Demo Credentials (for development) */}
          {process.env.NODE_ENV === 'development' && (
            <div className="mt-8 p-4 bg-yellow-50 border border-yellow-200 rounded-md">
              <h3 className="text-sm font-medium text-yellow-800 mb-2">
                Demo Credentials (Development Only)
              </h3>
              <div className="text-xs text-yellow-700 space-y-1">
                <div><strong>Admin:</strong> admin@example.com / password123</div>
                <div><strong>Author:</strong> author@example.com / password123</div>
                <div><strong>User:</strong> user@example.com / password123</div>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <footer className="mt-8 text-center text-sm text-gray-500">
          <div className="space-x-4">
            <a
              href="/terms"
              className="hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
            >
              Terms of Service
            </a>
            <a
              href="/privacy"
              className="hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
            >
              Privacy Policy
            </a>
            <a
              href="/help"
              className="hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
            >
              Help
            </a>
          </div>
          <div className="mt-2">
            © 2024 Maple Blog. All rights reserved.
          </div>
        </footer>
      </div>
    </>
  );
};

export default LoginPage;