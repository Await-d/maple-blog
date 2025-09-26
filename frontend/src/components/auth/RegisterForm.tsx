/**
 * RegisterForm Component
 * Handles user registration with comprehensive validation
 * Includes password strength validation and terms acceptance
 */

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { useAuth } from '../../hooks/useAuth';
import type { RegisterFormData, AuthResult } from '../../types/auth';
import { cn } from '../../utils/cn';

export interface RegisterFormProps {
  onSuccess?: (result: AuthResult) => void;
  onError?: (error: string) => void;
  redirectPath?: string;
  showLoginLink?: boolean;
  className?: string;
}

// Password strength checker
const checkPasswordStrength = (password: string): { strength: number; feedback: string[] } => {
  const feedback: string[] = [];
  let strength = 0;

  if (password.length >= 8) strength += 1;
  else feedback.push('At least 8 characters');

  if (/[a-z]/.test(password)) strength += 1;
  else feedback.push('One lowercase letter');

  if (/[A-Z]/.test(password)) strength += 1;
  else feedback.push('One uppercase letter');

  if (/\d/.test(password)) strength += 1;
  else feedback.push('One number');

  if (/[^A-Za-z0-9]/.test(password)) strength += 1;
  else feedback.push('One special character');

  return { strength, feedback };
};

const getPasswordStrengthColor = (strength: number): string => {
  switch (strength) {
    case 0:
    case 1:
      return 'bg-red-500';
    case 2:
      return 'bg-orange-500';
    case 3:
      return 'bg-yellow-500';
    case 4:
      return 'bg-blue-500';
    case 5:
      return 'bg-green-500';
    default:
      return 'bg-gray-300';
  }
};

const getPasswordStrengthText = (strength: number): string => {
  switch (strength) {
    case 0:
    case 1:
      return 'Very Weak';
    case 2:
      return 'Weak';
    case 3:
      return 'Fair';
    case 4:
      return 'Good';
    case 5:
      return 'Strong';
    default:
      return '';
  }
};

export const RegisterForm: React.FC<RegisterFormProps> = ({
  onSuccess,
  onError,
  redirectPath: _redirectPath = '/',
  showLoginLink = true,
  className,
}) => {
  const { register: registerUser, isLoading, error, clearError } = useAuth();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [passwordStrength, setPasswordStrength] = useState({ strength: 0, feedback: [] as string[] });

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    watch,
    trigger: _trigger,
  } = useForm<RegisterFormData>({
    defaultValues: {
      email: '',
      userName: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
      agreeToTerms: false,
    },
    mode: 'onChange',
  });

  const watchedPassword = watch('password');
  const _watchedConfirmPassword = watch('confirmPassword');

  // Update password strength when password changes
  React.useEffect(() => {
    if (watchedPassword) {
      setPasswordStrength(checkPasswordStrength(watchedPassword));
    } else {
      setPasswordStrength({ strength: 0, feedback: [] });
    }
  }, [watchedPassword]);

  const onSubmit = async (data: RegisterFormData) => {
    try {
      setSubmitError(null);
      clearError();

      const result = await registerUser(data);

      if (result.success) {
        onSuccess?.(result);
        // Navigation will be handled by the parent component or useAuth
      } else {
        const errorMessage = result.errorMessage || 'Registration failed';
        setSubmitError(errorMessage);
        onError?.(errorMessage);

        // Handle specific error cases
        if (result.errors && result.errors.length > 0) {
          setSubmitError(result.errors.join('. '));
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
            Create your account
          </h2>
          <p className="text-gray-600">
            Join us today! Please fill in your details.
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

        {/* Registration Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
          {/* Name Fields Row */}
          <div className="grid grid-cols-2 gap-4">
            {/* First Name */}
            <Input
              label="First Name"
              type="text"
              autoComplete="given-name"
              required
              errorMessage={errors.firstName?.message}
              inputSize="md"
              {...register('firstName', {
                required: 'First name is required',
                minLength: {
                  value: 1,
                  message: 'First name must be at least 1 character',
                },
                maxLength: {
                  value: 100,
                  message: 'First name cannot exceed 100 characters',
                },
                pattern: {
                  value: /^[a-zA-Z\s]+$/,
                  message: 'First name can only contain letters and spaces',
                },
              })}
            />

            {/* Last Name */}
            <Input
              label="Last Name"
              type="text"
              autoComplete="family-name"
              required
              errorMessage={errors.lastName?.message}
              inputSize="md"
              {...register('lastName', {
                required: 'Last name is required',
                minLength: {
                  value: 1,
                  message: 'Last name must be at least 1 character',
                },
                maxLength: {
                  value: 100,
                  message: 'Last name cannot exceed 100 characters',
                },
                pattern: {
                  value: /^[a-zA-Z\s]+$/,
                  message: 'Last name can only contain letters and spaces',
                },
              })}
            />
          </div>

          {/* Email Field */}
          <Input
            label="Email Address"
            type="email"
            autoComplete="email"
            required
            errorMessage={errors.email?.message}
            leftIcon={
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.207"
                />
              </svg>
            }
            {...register('email', {
              required: 'Email address is required',
              pattern: {
                value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                message: 'Please enter a valid email address',
              },
              maxLength: {
                value: 254,
                message: 'Email cannot exceed 254 characters',
              },
            })}
          />

          {/* Username Field */}
          <Input
            label="Username"
            type="text"
            autoComplete="username"
            required
            errorMessage={errors.userName?.message}
            helperText="3-50 characters. Letters, numbers, dots, underscores, and hyphens only."
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
            {...register('userName', {
              required: 'Username is required',
              minLength: {
                value: 3,
                message: 'Username must be at least 3 characters',
              },
              maxLength: {
                value: 50,
                message: 'Username cannot exceed 50 characters',
              },
              pattern: {
                value: /^[a-zA-Z0-9._-]+$/,
                message: 'Username can only contain letters, numbers, dots, underscores, and hyphens',
              },
            })}
          />

          {/* Password Field with Strength Indicator */}
          <div>
            <Input
              label="Password"
              type="password"
              autoComplete="new-password"
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
                validate: (value) => {
                  const { strength } = checkPasswordStrength(value);
                  return strength >= 3 || 'Password is too weak';
                },
              })}
            />

            {/* Password Strength Indicator */}
            {watchedPassword && (
              <div className="mt-2">
                <div className="flex justify-between text-sm mb-1">
                  <span className="text-gray-600">Password strength:</span>
                  <span
                    className={cn(
                      'font-medium',
                      passwordStrength.strength >= 4 ? 'text-green-600' :
                      passwordStrength.strength >= 3 ? 'text-blue-600' :
                      passwordStrength.strength >= 2 ? 'text-yellow-600' :
                      'text-red-600'
                    )}
                  >
                    {getPasswordStrengthText(passwordStrength.strength)}
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2 mb-2">
                  <div
                    className={cn(
                      'h-2 rounded-full transition-all duration-300',
                      getPasswordStrengthColor(passwordStrength.strength)
                    )}
                    style={{ width: `${(passwordStrength.strength / 5) * 100}%` }}
                  />
                </div>
                {passwordStrength.feedback.length > 0 && (
                  <div className="text-xs text-gray-600">
                    <span>Requirements: </span>
                    <span className="text-red-600">{passwordStrength.feedback.join(', ')}</span>
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Confirm Password Field */}
          <Input
            label="Confirm Password"
            type="password"
            autoComplete="new-password"
            required
            showPasswordToggle
            errorMessage={errors.confirmPassword?.message}
            leftIcon={
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            }
            {...register('confirmPassword', {
              required: 'Please confirm your password',
              validate: (value) => {
                return value === watchedPassword || 'Passwords do not match';
              },
            })}
          />

          {/* Terms Agreement Checkbox */}
          <div className="flex items-start">
            <div className="flex items-center h-5">
              <input
                type="checkbox"
                id="agreeToTerms"
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                {...register('agreeToTerms', {
                  required: 'You must agree to the terms and conditions',
                })}
              />
            </div>
            <div className="ml-3 text-sm">
              <label
                htmlFor="agreeToTerms"
                className="font-medium text-gray-700 select-none cursor-pointer"
              >
                I agree to the{' '}
                <Link
                  to="/terms"
                  className="text-blue-600 hover:text-blue-500 underline"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Terms of Service
                </Link>
                {' '}and{' '}
                <Link
                  to="/privacy"
                  className="text-blue-600 hover:text-blue-500 underline"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  Privacy Policy
                </Link>
              </label>
              {errors.agreeToTerms && (
                <p className="mt-1 text-sm text-red-600" role="alert">
                  {errors.agreeToTerms.message}
                </p>
              )}
            </div>
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
            {isLoading || isSubmitting ? 'Creating account...' : 'Create account'}
          </Button>

          {/* Login Link */}
          {showLoginLink && (
            <div className="text-center">
              <p className="text-sm text-gray-600">
                Already have an account?{' '}
                <Link
                  to="/login"
                  className="font-medium text-blue-600 hover:text-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
                >
                  Sign in here
                </Link>
              </p>
            </div>
          )}
        </form>
      </div>
    </div>
  );
};

// Demo Component for testing
export const RegisterFormDemo: React.FC = () => {
  const handleSuccess = (_result: AuthResult) => {
    // Registration successful - data available in result parameter
    // Replace with actual success handling logic
  };

  const handleError = (error: string) => {
    console.error('Registration error:', error);
  };

  return (
    <div className="min-h-screen bg-gray-50 py-12">
      <div className="container mx-auto px-4">
        <h1 className="text-3xl font-bold text-center mb-8">Register Form Demo</h1>
        <RegisterForm
          onSuccess={handleSuccess}
          onError={handleError}
          showLoginLink
        />
      </div>
    </div>
  );
};

export default RegisterForm;