// @ts-nocheck
/**
 * ResetPasswordPage - Password reset page with form and layout
 * Handles both password reset request and confirmation flows
 */

import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Helmet } from 'react-helmet-async';
import { useForm } from 'react-hook-form';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useAuth } from '../../hooks/useAuth';
import { PasswordResetFormData, PasswordResetConfirmFormData } from '../../types/auth';
import { cn } from '../../utils/cn';

// Email validation regex
const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

// Password validation patterns
const passwordPatterns = {
  minLength: /.{8,}/,
  hasUppercase: /[A-Z]/,
  hasLowercase: /[a-z]/,
  hasNumber: /\d/,
  hasSpecialChar: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/,
};

// Custom password validation function
const validatePassword = (password: string): string | true => {
  if (!passwordPatterns.minLength.test(password)) {
    return 'Password must be at least 8 characters long';
  }
  if (!passwordPatterns.hasUppercase.test(password)) {
    return 'Password must contain at least one uppercase letter';
  }
  if (!passwordPatterns.hasLowercase.test(password)) {
    return 'Password must contain at least one lowercase letter';
  }
  if (!passwordPatterns.hasNumber.test(password)) {
    return 'Password must contain at least one number';
  }
  if (!passwordPatterns.hasSpecialChar.test(password)) {
    return 'Password must contain at least one special character';
  }
  return true;
};

// Page props interface
interface ResetPasswordPageProps {
  className?: string;
}

// Success message component
const SuccessMessage: React.FC<{ message: string; actionText?: string; onAction?: () => void }> = ({
  message,
  actionText = 'Continue to Login',
  onAction,
}) => (
  <div className="text-center">
    <div className="mx-auto flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-6">
      <svg
        className="w-8 h-8 text-green-600"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
        aria-hidden="true"
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          strokeWidth={2}
          d="M5 13l4 4L19 7"
        />
      </svg>
    </div>
    <h2 className="text-2xl font-bold text-gray-900 mb-4">Success!</h2>
    <p className="text-gray-600 mb-8">{message}</p>
    <Button onClick={onAction} variant="primary" size="lg">
      {actionText}
    </Button>
  </div>
);

// Password reset request form component
const PasswordResetRequestForm: React.FC<{ onSuccess: (email: string) => void }> = ({ onSuccess }) => {
  const { requestPasswordReset } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<PasswordResetFormData>({
    defaultValues: { email: '' },
    mode: 'onBlur',
  });

  const onSubmit = async (data: PasswordResetFormData) => {
    setServerError(null);

    try {
      const result = await requestPasswordReset(data.email.trim().toLowerCase());

      if (result.success) {
        onSuccess(data.email);
      } else {
        setServerError(result.message || 'Failed to send password reset email. Please try again.');
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';
      setServerError(errorMessage);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
      {/* Server Error Display */}
      {serverError && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-md" role="alert">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.5 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-red-800">{serverError}</p>
            </div>
          </div>
        </div>
      )}

      {/* Email Field */}
      <Input
        {...register('email', {
          required: 'Email is required',
          pattern: {
            value: emailPattern,
            message: 'Please enter a valid email address',
          },
        })}
        type="email"
        label="Email Address"
        placeholder="Enter your email address"
        errorMessage={errors.email?.message}
        disabled={isSubmitting}
        autoComplete="email"
        autoFocus
      />

      {/* Submit Button */}
      <Button
        type="submit"
        variant="primary"
        size="lg"
        fullWidth
        loading={isSubmitting}
        disabled={isSubmitting}
      >
        {isSubmitting ? 'Sending...' : 'Send Reset Email'}
      </Button>
    </form>
  );
};

// Password reset confirmation form component
const PasswordResetConfirmForm: React.FC<{ token: string; onSuccess: () => void }> = ({ token, onSuccess }) => {
  const { confirmPasswordReset } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    watch,
  } = useForm<PasswordResetConfirmFormData>({
    defaultValues: {
      token,
      password: '',
      confirmPassword: '',
    },
    mode: 'onBlur',
  });

  const watchedPassword = watch('password', '');

  const onSubmit = async (data: PasswordResetConfirmFormData) => {
    setServerError(null);

    if (data.password !== data.confirmPassword) {
      setServerError('Passwords do not match');
      return;
    }

    try {
      const result = await confirmPasswordReset({
        token: token,
        password: data.password,
        confirmPassword: data.confirmPassword,
      });

      if (result.success) {
        onSuccess();
      } else {
        setServerError(result.message || 'Failed to reset password. Please try again.');
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';
      setServerError(errorMessage);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
      {/* Server Error Display */}
      {serverError && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-md" role="alert">
          <div className="flex items-center">
            <div className="flex-shrink-0">
              <svg className="w-5 h-5 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.732-.833-2.5 0L4.268 18.5c-.77.833.192 2.5 1.732 2.5z" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-red-800">{serverError}</p>
            </div>
          </div>
        </div>
      )}

      {/* Hidden Token Field */}
      <input type="hidden" {...register('token')} />

      {/* New Password Field */}
      <Input
        {...register('password', {
          required: 'New password is required',
          validate: validatePassword,
          maxLength: {
            value: 128,
            message: 'Password cannot exceed 128 characters',
          },
        })}
        type="password"
        label="New Password"
        placeholder="Enter your new password"
        errorMessage={errors.password?.message}
        disabled={isSubmitting}
        showPasswordToggle
        autoComplete="new-password"
        autoFocus
      />

      {/* Confirm Password Field */}
      <Input
        {...register('confirmPassword', {
          required: 'Password confirmation is required',
          validate: (value) => value === watchedPassword || 'Passwords do not match',
        })}
        type="password"
        label="Confirm New Password"
        placeholder="Confirm your new password"
        errorMessage={errors.confirmPassword?.message}
        disabled={isSubmitting}
        showPasswordToggle
        autoComplete="new-password"
      />

      {/* Submit Button */}
      <Button
        type="submit"
        variant="primary"
        size="lg"
        fullWidth
        loading={isSubmitting}
        disabled={isSubmitting}
      >
        {isSubmitting ? 'Resetting...' : 'Reset Password'}
      </Button>
    </form>
  );
};

// Main ResetPasswordPage component
export const ResetPasswordPage: React.FC<ResetPasswordPageProps> = ({ className }) => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAuthenticated } = useAuth();

  const [step, setStep] = useState<'request' | 'sent' | 'confirm' | 'success'>('request');
  const [email, setEmail] = useState('');

  const token = searchParams.get('token');

  // Determine initial step based on URL parameters
  useEffect(() => {
    if (token) {
      setStep('confirm');
    }
  }, [token]);

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate('/dashboard', { replace: true });
    }
  }, [isAuthenticated, navigate]);

  // Handle request success
  const handleRequestSuccess = (emailAddress: string) => {
    setEmail(emailAddress);
    setStep('sent');
  };

  // Handle confirmation success
  const handleConfirmSuccess = () => {
    setStep('success');
  };

  // Handle navigation to login
  const handleGoToLogin = () => {
    navigate('/login', {
      replace: true,
      state: {
        message: 'Password reset successfully! You can now log in with your new password.',
      },
    });
  };

  // Handle back to request
  const handleBackToRequest = () => {
    setStep('request');
    setEmail('');
  };

  // Don't render if already authenticated
  if (isAuthenticated) {
    return null;
  }

  // Determine page title and description based on step
  const getPageMeta = () => {
    switch (step) {
      case 'request':
        return {
          title: 'Reset Password - Maple Blog',
          description: 'Reset your Maple Blog password by entering your email address. We\'ll send you a link to create a new password.',
        };
      case 'sent':
        return {
          title: 'Check Your Email - Maple Blog',
          description: 'We\'ve sent a password reset link to your email. Please check your inbox and follow the instructions.',
        };
      case 'confirm':
        return {
          title: 'Set New Password - Maple Blog',
          description: 'Create a new password for your Maple Blog account using the reset link from your email.',
        };
      case 'success':
        return {
          title: 'Password Reset Complete - Maple Blog',
          description: 'Your password has been successfully reset. You can now log in with your new password.',
        };
      default:
        return {
          title: 'Reset Password - Maple Blog',
          description: 'Reset your Maple Blog password.',
        };
    }
  };

  const { title, description } = getPageMeta();

  return (
    <>
      {/* SEO Meta Tags */}
      <Helmet>
        <title>{title}</title>
        <meta name="description" content={description} />
        <meta name="robots" content="noindex, nofollow" />
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
              Reset your password
            </p>
          </div>

          {/* Main Card */}
          <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
            {/* Request Step */}
            {step === 'request' && (
              <div>
                <div className="text-center mb-6">
                  <h2 className="text-2xl font-bold text-gray-900 mb-2">
                    Forgot your password?
                  </h2>
                  <p className="text-gray-600">
                    Enter your email address and we'll send you a link to reset your password.
                  </p>
                </div>
                <PasswordResetRequestForm onSuccess={handleRequestSuccess} />
              </div>
            )}

            {/* Email Sent Step */}
            {step === 'sent' && (
              <div className="text-center">
                <div className="mx-auto flex items-center justify-center w-16 h-16 bg-blue-100 rounded-full mb-6">
                  <svg
                    className="w-8 h-8 text-blue-600"
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
                <h2 className="text-2xl font-bold text-gray-900 mb-4">
                  Check your email
                </h2>
                <div className="text-gray-600 mb-8">
                  <p className="mb-2">
                    We've sent a password reset link to:
                  </p>
                  <p className="font-medium text-gray-900">{email}</p>
                  <p className="mt-4">
                    Click the link in the email to reset your password. If you don't see the email,
                    check your spam folder.
                  </p>
                </div>
                <div className="space-y-3">
                  <Button
                    onClick={handleBackToRequest}
                    variant="outline"
                    size="lg"
                    fullWidth
                  >
                    Use a different email
                  </Button>
                </div>
              </div>
            )}

            {/* Confirm Step */}
            {step === 'confirm' && token && (
              <div>
                <div className="text-center mb-6">
                  <h2 className="text-2xl font-bold text-gray-900 mb-2">
                    Set new password
                  </h2>
                  <p className="text-gray-600">
                    Choose a strong password for your account.
                  </p>
                </div>
                <PasswordResetConfirmForm token={token} onSuccess={handleConfirmSuccess} />
              </div>
            )}

            {/* Success Step */}
            {step === 'success' && (
              <SuccessMessage
                message="Your password has been successfully reset. You can now log in with your new password."
                actionText="Continue to Login"
                onAction={handleGoToLogin}
              />
            )}
          </div>

          {/* Additional Links */}
          <div className="mt-8 text-center">
            <div className="text-sm text-gray-600 space-x-4">
              <a
                href="/login"
                className="font-medium text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
              >
                Back to Sign In
              </a>
              <a
                href="/register"
                className="font-medium text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded transition-colors"
              >
                Create Account
              </a>
            </div>
          </div>
        </div>

        {/* Footer */}
        <footer className="mt-8 text-center text-sm text-gray-500">
          <div className="space-x-4">
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
              Contact Support
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

export default ResetPasswordPage;