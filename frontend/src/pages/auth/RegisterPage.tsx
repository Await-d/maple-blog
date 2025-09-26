/**
 * RegisterPage - Registration page with form and layout
 * Provides complete registration experience with navigation and SEO
 */

import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Helmet } from '@/components/common/DocumentHead';
import { RegisterForm } from '../../features/auth/components/RegisterForm';
import { useAuth } from '../../hooks/useAuth';
import { cn } from '../../utils/cn';

// Page props interface
interface RegisterPageProps {
  className?: string;
}

// RegisterPage component
export const RegisterPage: React.FC<RegisterPageProps> = ({ className }) => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate('/dashboard', { replace: true });
    }
  }, [isAuthenticated, navigate]);

  // Handle successful registration
  const handleRegistrationSuccess = () => {
    // Redirect to a success page or dashboard
    navigate('/login', {
      replace: true,
      state: {
        message: 'Registration successful! Please check your email to verify your account.',
      },
    });
  };

  // Don't render if already authenticated (prevents flash)
  if (isAuthenticated) {
    return null;
  }

  return (
    <>
      {/* SEO Meta Tags */}
      <Helmet>
        <title>Sign Up - Maple Blog</title>
        <meta
          name="description"
          content="Create your Maple Blog account to start sharing your thoughts, stories, and ideas with the world. Join our community of writers today."
        />
        <meta name="robots" content="noindex, nofollow" />
        <link rel="canonical" href={`${window.location.origin}/register`} />

        {/* Open Graph */}
        <meta property="og:title" content="Join Maple Blog - Share Your Story" />
        <meta
          property="og:description"
          content="Start your blogging journey with Maple Blog. Create an account to share your thoughts and connect with writers worldwide."
        />
        <meta property="og:type" content="website" />
        <meta property="og:url" content={`${window.location.origin}/register`} />

        {/* Twitter Card */}
        <meta name="twitter:card" content="summary" />
        <meta name="twitter:title" content="Join Maple Blog - Share Your Story" />
        <meta
          name="twitter:description"
          content="Start your blogging journey with Maple Blog. Create an account today."
        />
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
              Join our community of writers
            </p>
          </div>

          {/* Benefits Section */}
          <div className="mb-8 bg-white rounded-lg shadow-sm p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 text-center">
              Why join Maple Blog?
            </h2>
            <ul className="space-y-3 text-sm text-gray-600">
              <li className="flex items-center">
                <div className="flex-shrink-0 w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mr-3">
                  <svg className="w-3 h-3 text-green-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                </div>
                Share your thoughts and stories with the world
              </li>
              <li className="flex items-center">
                <div className="flex-shrink-0 w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mr-3">
                  <svg className="w-3 h-3 text-green-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                </div>
                Connect with like-minded writers and readers
              </li>
              <li className="flex items-center">
                <div className="flex-shrink-0 w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mr-3">
                  <svg className="w-3 h-3 text-green-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                </div>
                Get feedback and grow your writing skills
              </li>
              <li className="flex items-center">
                <div className="flex-shrink-0 w-5 h-5 bg-green-100 rounded-full flex items-center justify-center mr-3">
                  <svg className="w-3 h-3 text-green-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                  </svg>
                </div>
                Access powerful writing and publishing tools
              </li>
            </ul>
          </div>

          {/* Registration Form Card */}
          <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
            <RegisterForm
              onSuccess={handleRegistrationSuccess}
              className="max-w-none"
              showTitle={false}
              showLoginLink={true}
            />
          </div>

          {/* Additional Information */}
          <div className="mt-8 text-center">
            <div className="text-sm text-gray-600 space-y-2">
              <p>
                By creating an account, you&apos;re joining a community of passionate writers
                and readers from around the world.
              </p>
              <a
                href="/"
                className="font-medium text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
              >
                ← Back to Home
              </a>
            </div>
          </div>

          {/* Security Notice */}
          <div className="mt-8 p-4 bg-blue-50 border border-blue-200 rounded-md">
            <div className="flex items-start">
              <div className="flex-shrink-0">
                <svg
                  className="w-5 h-5 text-blue-400 mt-0.5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                  />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-blue-800">
                  Your Privacy Matters
                </h3>
                <p className="mt-1 text-sm text-blue-700">
                  We take your privacy seriously. Your personal information is encrypted
                  and never shared with third parties. You can delete your account at any time.
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <footer className="mt-12 text-center text-sm text-gray-500">
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
            <a
              href="/contact"
              className="hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
            >
              Contact
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

export default RegisterPage;