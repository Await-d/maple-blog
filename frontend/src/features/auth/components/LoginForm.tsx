/**
 * LoginForm Component - Form for user authentication
 * Integrates with useAuth hook and provides validation
 */

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { useAuth } from '../../../hooks/useAuth';
import { LoginFormData } from '../../../types/auth';
import { cn } from '../../../utils/cn';

// Form validation rules
const validationRules = {
  emailOrUsername: {
    required: 'Email or username is required',
    minLength: {
      value: 3,
      message: 'Email or username must be at least 3 characters',
    },
    maxLength: {
      value: 254,
      message: 'Email or username cannot exceed 254 characters',
    },
  },
  password: {
    required: 'Password is required',
    minLength: {
      value: 8,
      message: 'Password must be at least 8 characters',
    },
    maxLength: {
      value: 128,
      message: 'Password cannot exceed 128 characters',
    },
  },
};

// LoginForm props
export interface LoginFormProps {
  onSuccess?: () => void;
  redirectTo?: string;
  className?: string;
  showTitle?: boolean;
  showRegisterLink?: boolean;
  showForgotPasswordLink?: boolean;
}

// LoginForm component
export const LoginForm: React.FC<LoginFormProps> = ({
  onSuccess,
  className,
  showTitle = true,
  showRegisterLink = true,
  showForgotPasswordLink = true,
}) => {
  const { login, isLoading, error } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);

  // React Hook Form setup
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
    setError: setFormError,
  } = useForm<LoginFormData>({
    defaultValues: {
      emailOrUsername: '',
      password: '',
      rememberMe: false,
    },
    mode: 'onBlur',
  });

  // Handle form submission
  const onSubmit = async (data: LoginFormData) => {
    setServerError(null);

    try {
      const result = await login({
        emailOrUsername: data.emailOrUsername.trim(),
        password: data.password,
        rememberMe: data.rememberMe,
      });

      if (result.success) {
        reset(); // Clear form on success
        onSuccess?.();
      } else {
        // Handle specific error cases
        if (result.requiresEmailVerification) {
          setServerError('Please verify your email before logging in.');
        } else if (result.isLockedOut) {
          setServerError(
            result.errorMessage ||
            'Account is temporarily locked due to multiple failed login attempts.'
          );
        } else if (result.requiresTwoFactor) {
          setServerError('Two-factor authentication is required.');
        } else {
          // Handle validation errors
          if (result.errors && result.errors.length > 0) {
            const errorMessage = result.errors[0];

            // Try to map specific errors to form fields
            if (errorMessage.toLowerCase().includes('email') ||
                errorMessage.toLowerCase().includes('username')) {
              setFormError('emailOrUsername', { message: errorMessage });
            } else if (errorMessage.toLowerCase().includes('password')) {
              setFormError('password', { message: errorMessage });
            } else {
              setServerError(errorMessage);
            }
          } else {
            setServerError(result.errorMessage || 'Login failed. Please try again.');
          }
        }
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'An unexpected error occurred';
      setServerError(errorMessage);
    }
  };

  // Loading state
  const isFormLoading = isLoading || isSubmitting;

  return (
    <div className={cn('w-full max-w-md mx-auto', className)}>
      {/* Title */}
      {showTitle && (
        <div className="text-center mb-8">
          <h2 className="text-3xl font-bold text-gray-900 mb-2">
            Welcome back
          </h2>
          <p className="text-gray-600">
            Sign in to your account to continue
          </p>
        </div>
      )}

      {/* Form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
        {/* Server Error Display */}
        {(serverError || error) && (
          <div
            className="p-4 bg-red-50 border border-red-200 rounded-md"
            role="alert"
            aria-live="polite"
          >
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg
                  className="w-5 h-5 text-red-400"
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
              <div className="ml-3">
                <p className="text-sm text-red-800">
                  {serverError || error}
                </p>
              </div>
            </div>
          </div>
        )}

        {/* Email/Username Field */}
        <div>
          <Input
            {...register('emailOrUsername', validationRules.emailOrUsername)}
            type="text"
            label="Email or Username"
            placeholder="Enter your email or username"
            errorMessage={errors.emailOrUsername?.message}
            disabled={isFormLoading}
            autoComplete="username"
            autoFocus
          />
        </div>

        {/* Password Field */}
        <div>
          <Input
            {...register('password', validationRules.password)}
            type="password"
            label="Password"
            placeholder="Enter your password"
            errorMessage={errors.password?.message}
            disabled={isFormLoading}
            showPasswordToggle
            autoComplete="current-password"
          />
        </div>

        {/* Remember Me & Forgot Password */}
        <div className="flex items-center justify-between">
          {/* Remember Me Checkbox */}
          <div className="flex items-center">
            <input
              {...register('rememberMe')}
              id="remember-me"
              type="checkbox"
              disabled={isFormLoading}
              className="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2 disabled:opacity-50"
            />
            <label
              htmlFor="remember-me"
              className="ml-2 text-sm text-gray-700 select-none"
            >
              Remember me
            </label>
          </div>

          {/* Forgot Password Link */}
          {showForgotPasswordLink && (
            <Link
              to="/forgot-password"
              className="text-sm text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
              tabIndex={isFormLoading ? -1 : 0}
            >
              Forgot password?
            </Link>
          )}
        </div>

        {/* Submit Button */}
        <Button
          type="submit"
          variant="primary"
          size="lg"
          fullWidth
          loading={isFormLoading}
          disabled={isFormLoading}
        >
          {isFormLoading ? 'Signing in...' : 'Sign in'}
        </Button>

        {/* Register Link */}
        {showRegisterLink && (
          <div className="text-center mt-6">
            <p className="text-sm text-gray-600">
              Don&apos;t have an account?{' '}
              <Link
                to="/register"
                className="font-medium text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
                tabIndex={isFormLoading ? -1 : 0}
              >
                Sign up here
              </Link>
            </p>
          </div>
        )}
      </form>

      {/* Accessibility Enhancement */}
      <div className="sr-only" aria-live="polite" role="status">
        {isFormLoading && 'Form is being submitted'}
      </div>
    </div>
  );
};

// Compact LoginForm variant for modals or smaller spaces
export interface CompactLoginFormProps extends Omit<LoginFormProps, 'showTitle' | 'showRegisterLink'> {
  onCancel?: () => void;
}

export const CompactLoginForm: React.FC<CompactLoginFormProps> = ({
  onSuccess,
  onCancel,
  className,
  showForgotPasswordLink = true,
}) => {
  return (
    <div className={cn('w-full', className)}>
      <LoginForm
        onSuccess={onSuccess}
        className="max-w-none"
        showTitle={false}
        showRegisterLink={false}
        showForgotPasswordLink={showForgotPasswordLink}
      />

      {/* Cancel button for modal usage */}
      {onCancel && (
        <div className="mt-4 text-center">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onCancel}
          >
            Cancel
          </Button>
        </div>
      )}
    </div>
  );
};

export default LoginForm;