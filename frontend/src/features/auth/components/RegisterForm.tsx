// @ts-nocheck
/**
 * RegisterForm Component - Form for user registration
 * Integrates with useAuth hook and provides comprehensive validation
 */

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { Button } from '../../../components/ui/Button';
import { Input } from '../../../components/ui/Input';
import { useAuth } from '../../../hooks/useAuth';
import { RegisterFormData } from '../../../types/auth';
import { cn } from '../../../utils/cn';

// Password validation regex patterns
const passwordPatterns = {
  minLength: /.{8,}/,
  hasUppercase: /[A-Z]/,
  hasLowercase: /[a-z]/,
  hasNumber: /\d/,
  hasSpecialChar: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/,
};

// Username validation regex
const usernamePattern = /^[a-zA-Z0-9._-]+$/;

// Email validation regex (more comprehensive than HTML5)
const emailPattern = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

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

// Form validation rules
const validationRules = {
  email: {
    required: 'Email is required',
    pattern: {
      value: emailPattern,
      message: 'Please enter a valid email address',
    },
    maxLength: {
      value: 254,
      message: 'Email cannot exceed 254 characters',
    },
  },
  userName: {
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
      value: usernamePattern,
      message: 'Username can only contain letters, numbers, dots, underscores, and hyphens',
    },
  },
  password: {
    required: 'Password is required',
    validate: validatePassword,
    maxLength: {
      value: 128,
      message: 'Password cannot exceed 128 characters',
    },
  },
  confirmPassword: {
    required: 'Password confirmation is required',
  },
  firstName: {
    required: 'First name is required',
    minLength: {
      value: 1,
      message: 'First name is required',
    },
    maxLength: {
      value: 100,
      message: 'First name cannot exceed 100 characters',
    },
  },
  lastName: {
    required: 'Last name is required',
    minLength: {
      value: 1,
      message: 'Last name is required',
    },
    maxLength: {
      value: 100,
      message: 'Last name cannot exceed 100 characters',
    },
  },
  agreeToTerms: {
    required: 'You must agree to the terms of service',
  },
};

// RegisterForm props
export interface RegisterFormProps {
  onSuccess?: () => void;
  className?: string;
  showTitle?: boolean;
  showLoginLink?: boolean;
}

// Password strength indicator component
const PasswordStrengthIndicator: React.FC<{ password: string }> = ({ password }) => {
  const checks = [
    { test: passwordPatterns.minLength, label: 'At least 8 characters' },
    { test: passwordPatterns.hasUppercase, label: 'One uppercase letter' },
    { test: passwordPatterns.hasLowercase, label: 'One lowercase letter' },
    { test: passwordPatterns.hasNumber, label: 'One number' },
    { test: passwordPatterns.hasSpecialChar, label: 'One special character' },
  ];

  const passedChecks = checks.filter(check => check.test.test(password)).length;
  const strength = passedChecks / checks.length;

  const getStrengthColor = () => {
    if (strength === 0) return 'bg-gray-200';
    if (strength < 0.4) return 'bg-red-500';
    if (strength < 0.7) return 'bg-yellow-500';
    if (strength < 1) return 'bg-blue-500';
    return 'bg-green-500';
  };

  const getStrengthText = () => {
    if (strength === 0) return 'Enter a password';
    if (strength < 0.4) return 'Weak';
    if (strength < 0.7) return 'Fair';
    if (strength < 1) return 'Good';
    return 'Strong';
  };

  return (
    <div className="mt-2">
      {/* Strength bar */}
      <div className="flex items-center space-x-2">
        <div className="flex-1 bg-gray-200 rounded-full h-2">
          <div
            className={cn('h-2 rounded-full transition-all duration-300', getStrengthColor())}
            style={{ width: `${strength * 100}%` }}
          />
        </div>
        <span className="text-xs text-gray-600 min-w-0">{getStrengthText()}</span>
      </div>

      {/* Requirements checklist */}
      {password && (
        <ul className="mt-2 text-xs space-y-1">
          {checks.map((check, index) => (
            <li
              key={index}
              className={cn(
                'flex items-center space-x-2',
                check.test.test(password) ? 'text-green-600' : 'text-gray-500'
              )}
            >
              <span className="text-lg">
                {check.test.test(password) ? '✓' : '○'}
              </span>
              <span>{check.label}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

// RegisterForm component
export const RegisterForm: React.FC<RegisterFormProps> = ({
  onSuccess,
  className,
  showTitle = true,
  showLoginLink = true,
}) => {
  const { register: registerUser, isLoading, error } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [showPasswordRequirements, setShowPasswordRequirements] = useState(false);

  // React Hook Form setup
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
    setError: setFormError,
    watch,
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
    mode: 'onBlur',
  });

  // Watch password for real-time validation
  const watchedPassword = watch('password', '');
  const watchedConfirmPassword = watch('confirmPassword', '');

  // Handle form submission
  const onSubmit = async (data: RegisterFormData) => {
    setServerError(null);

    // Validate password confirmation
    if (data.password !== data.confirmPassword) {
      setFormError('confirmPassword', { message: 'Passwords do not match' });
      return;
    }

    try {
      const result = await registerUser({
        email: data.email.trim().toLowerCase(),
        userName: data.userName.trim(),
        password: data.password,
        confirmPassword: data.confirmPassword,
        firstName: data.firstName.trim(),
        lastName: data.lastName.trim(),
        agreeToTerms: data.agreeToTerms,
      });

      if (result.success) {
        reset(); // Clear form on success
        onSuccess?.();
      } else {
        // Handle validation errors
        if (result.errors && result.errors.length > 0) {
          // Try to map specific errors to form fields
          result.errors.forEach(errorMessage => {
            const lowerError = errorMessage.toLowerCase();

            if (lowerError.includes('email')) {
              setFormError('email', { message: errorMessage });
            } else if (lowerError.includes('username')) {
              setFormError('userName', { message: errorMessage });
            } else if (lowerError.includes('password')) {
              setFormError('password', { message: errorMessage });
            } else if (lowerError.includes('first name')) {
              setFormError('firstName', { message: errorMessage });
            } else if (lowerError.includes('last name')) {
              setFormError('lastName', { message: errorMessage });
            } else {
              setServerError(errorMessage);
            }
          });

          // If no field-specific errors were set, show the first error as server error
          if (!Object.keys(errors).length) {
            setServerError(result.errors[0]);
          }
        } else {
          setServerError(result.errorMessage || 'Registration failed. Please try again.');
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
            Create an account
          </h2>
          <p className="text-gray-600">
            Join us to start your blogging journey
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

        {/* Name Fields */}
        <div className="grid grid-cols-2 gap-4">
          <Input
            {...register('firstName', validationRules.firstName)}
            type="text"
            label="First Name"
            placeholder="John"
            errorMessage={errors.firstName?.message}
            disabled={isFormLoading}
            autoComplete="given-name"
            autoFocus
          />
          <Input
            {...register('lastName', validationRules.lastName)}
            type="text"
            label="Last Name"
            placeholder="Doe"
            errorMessage={errors.lastName?.message}
            disabled={isFormLoading}
            autoComplete="family-name"
          />
        </div>

        {/* Email Field */}
        <Input
          {...register('email', validationRules.email)}
          type="email"
          label="Email Address"
          placeholder="john@example.com"
          errorMessage={errors.email?.message}
          disabled={isFormLoading}
          autoComplete="email"
        />

        {/* Username Field */}
        <Input
          {...register('userName', validationRules.userName)}
          type="text"
          label="Username"
          placeholder="johndoe"
          errorMessage={errors.userName?.message}
          disabled={isFormLoading}
          autoComplete="username"
          helperText="Only letters, numbers, dots, underscores, and hyphens allowed"
        />

        {/* Password Field */}
        <div>
          <Input
            {...register('password', validationRules.password)}
            type="password"
            label="Password"
            placeholder="Create a strong password"
            errorMessage={errors.password?.message}
            disabled={isFormLoading}
            showPasswordToggle
            autoComplete="new-password"
            onFocus={() => setShowPasswordRequirements(true)}
          />
          {showPasswordRequirements && (
            <PasswordStrengthIndicator password={watchedPassword} />
          )}
        </div>

        {/* Confirm Password Field */}
        <Input
          {...register('confirmPassword', {
            ...validationRules.confirmPassword,
            validate: (value) =>
              value === watchedPassword || 'Passwords do not match',
          })}
          type="password"
          label="Confirm Password"
          placeholder="Confirm your password"
          errorMessage={errors.confirmPassword?.message}
          disabled={isFormLoading}
          showPasswordToggle
          autoComplete="new-password"
        />

        {/* Terms Agreement */}
        <div className="flex items-start">
          <div className="flex items-center h-5">
            <input
              {...register('agreeToTerms', validationRules.agreeToTerms)}
              id="agree-terms"
              type="checkbox"
              disabled={isFormLoading}
              className="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2 disabled:opacity-50"
            />
          </div>
          <div className="ml-3">
            <label htmlFor="agree-terms" className="text-sm text-gray-700 select-none">
              I agree to the{' '}
              <Link
                to="/terms"
                target="_blank"
                rel="noopener noreferrer"
                className="text-blue-600 hover:text-blue-700 underline focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
                tabIndex={isFormLoading ? -1 : 0}
              >
                Terms of Service
              </Link>{' '}
              and{' '}
              <Link
                to="/privacy"
                target="_blank"
                rel="noopener noreferrer"
                className="text-blue-600 hover:text-blue-700 underline focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
                tabIndex={isFormLoading ? -1 : 0}
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
          loading={isFormLoading}
          disabled={isFormLoading}
        >
          {isFormLoading ? 'Creating account...' : 'Create account'}
        </Button>

        {/* Login Link */}
        {showLoginLink && (
          <div className="text-center mt-6">
            <p className="text-sm text-gray-600">
              Already have an account?{' '}
              <Link
                to="/login"
                className="font-medium text-blue-600 hover:text-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 rounded"
                tabIndex={isFormLoading ? -1 : 0}
              >
                Sign in here
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

export default RegisterForm;