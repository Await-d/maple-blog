// @ts-nocheck
/**
 * LoginForm Component
 * Handles user login with validation and error handling
 * Integrates with useAuth hook for authentication logic
 */

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { useAuth } from '../../hooks/useAuth';
import type { LoginFormData, AuthResult } from '../../types/auth';
import { cn } from '../../utils/cn';

export interface LoginFormProps {
  onSuccess?: (result: AuthResult) => void;
  onError?: (error: string) => void;
  redirectPath?: string;
  showRegisterLink?: boolean;
  showForgotPasswordLink?: boolean;
  className?: string;
}

export const LoginForm: React.FC<LoginFormProps> = ({
  onSuccess,
  onError,
  redirectPath = '/',
  showRegisterLink = true,
  showForgotPasswordLink = true,
  className,
}) => {
  const { login, isLoading, error, clearError } = useAuth();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setValue,
    watch,
  } = useForm<LoginFormData>({
    defaultValues: {
      emailOrUsername: '',
      password: '',
      rememberMe: false,
    },
    mode: 'onChange',
  });

  const _watchedRememberMe = watch('rememberMe');

  const onSubmit = async (data: LoginFormData) => {
    try {
      setSubmitError(null);
      clearError();

      const result = await login(data);

      if (result.success) {
        onSuccess?.(result);
        // Navigation will be handled by the parent component or useAuth
      } else {
        const errorMessage = result.errorMessage || 'Login failed';
        setSubmitError(errorMessage);
        onError?.(errorMessage);

        // Handle specific error cases
        if (result.requiresEmailVerification) {
          setSubmitError('Please verify your email address before logging in.');
        } else if (result.isLockedOut) {
          setSubmitError(
            result.lockoutEnd
              ? `Account is locked until ${new Date(result.lockoutEnd).toLocaleString()}`
              : 'Account is temporarily locked due to multiple failed attempts.'
          );
        } else if (result.requiresTwoFactor) {
          setSubmitError('Two-factor authentication is required.');
        }
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'An unexpected error occurred';
      setSubmitError(errorMessage);
      onError?.(errorMessage);
    }
  };

  const displayError = submitError || error;

  return (
    <div className={cn('w-full max-w-md mx-auto', className)}>
      <div className="bg-white shadow-lg rounded-lg p-8">
        {/* Header */}
        <div className="text-center mb-8">
          <h2 className="text-2xl font-bold text-gray-900 mb-2">
            Sign in to your account
          </h2>
          <p className="text-gray-600">
            Welcome back! Please enter your details.
          </p>
        </div>

        {/* Error Message */}
        {displayError && (
          <div
            className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg"
            role="alert"
            aria-live="polite"
          >
            <div className="flex items-center">
              <div className="flex-shrink-0">
                <svg
                  className="h-5 w-5 text-red-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"
                  />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-red-700">{displayError}</p>
              </div>
            </div>
          </div>
        )}

        {/* Login Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
          {/* Email or Username Field */}
          <Input
            label="Email or Username"
            type="text"
            autoComplete="username"
            required
            errorMessage={errors.emailOrUsername?.message}
            leftIcon={
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            }
            {...register('emailOrUsername', {
              required: 'Email or username is required',
              minLength: {
                value: 3,
                message: 'Email or username must be at least 3 characters',
              },
              maxLength: {
                value: 254,
                message: 'Email or username cannot exceed 254 characters',
              },
            })}
          />

          {/* Password Field */}
          <Input
            label="Password"
            type="password"
            autoComplete="current-password"
            required
            showPasswordToggle
            errorMessage={errors.password?.message}
            leftIcon={
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                />
              </svg>
            }
            {...register('password', {
              required: 'Password is required',
              minLength: {
                value: 8,
                message: 'Password must be at least 8 characters',
              },
              maxLength: {
                value: 128,
                message: 'Password cannot exceed 128 characters',
              },
            })}
          />

          {/* Remember Me Checkbox */}
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <input
                type="checkbox"
                id="rememberMe"
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                {...register('rememberMe')}
              />
              <label
                htmlFor="rememberMe"
                className="ml-2 block text-sm text-gray-900 select-none cursor-pointer"
              >
                Remember me
              </label>
            </div>

            {showForgotPasswordLink && (
              <Link
                to="/reset-password"
                className="text-sm font-medium text-blue-600 hover:text-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
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
            loading={isLoading || isSubmitting}
            disabled={isLoading || isSubmitting}
          >
            {isLoading || isSubmitting ? 'Signing in...' : 'Sign in'}
          </Button>

          {/* Register Link */}
          {showRegisterLink && (
            <div className="text-center">
              <p className="text-sm text-gray-600">
                Don&apos;t have an account?{' '}
                <Link
                  to="/register"
                  className="font-medium text-blue-600 hover:text-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
                >
                  Create one here
                </Link>
              </p>
            </div>
          )}
        </form>

        {/* Additional Info */}
        <div className="mt-8 pt-6 border-t border-gray-200">
          <div className="text-center">
            <p className="text-xs text-gray-500">
              By signing in, you agree to our{' '}
              <Link
                to="/terms"
                className="text-blue-600 hover:text-blue-500 underline"
              >
                Terms of Service
              </Link>
              {' '}and{' '}
              <Link
                to="/privacy"
                className="text-blue-600 hover:text-blue-500 underline"
              >
                Privacy Policy
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

// Demo Component for testing
export const LoginFormDemo: React.FC = () => {
  const handleSuccess = (_result: AuthResult) => {
    // Login successful - data available in result parameter
    // Replace with actual success handling logic
  };

  const handleError = (error: string) => {
    console.error('Login error:', error);
  };

  return (
    <div className="min-h-screen bg-gray-50 py-12">
      <div className="container mx-auto px-4">
        <h1 className="text-3xl font-bold text-center mb-8">Login Form Demo</h1>
        <LoginForm
          onSuccess={handleSuccess}
          onError={handleError}
          showRegisterLink
          showForgotPasswordLink
        />
      </div>
    </div>
  );
};

export default LoginForm;