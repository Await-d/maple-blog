/**
 * Input Component - Reusable input field with validation and accessibility
 * Supports different types, states, and form integration
 */

import React, { forwardRef, useState } from 'react';
import { cn } from '../../utils/cn';

// Input variant types
export type InputVariant = 'default' | 'error' | 'success';

// Input size types
export type InputSize = 'sm' | 'md' | 'lg';

// Input props interface
export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  variant?: InputVariant;
  inputSize?: InputSize;
  label?: string;
  helperText?: string;
  errorMessage?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  leftAddon?: React.ReactNode;
  rightAddon?: React.ReactNode;
  fullWidth?: boolean;
  showPasswordToggle?: boolean;
}

// Input variant styles
const inputVariants = {
  default: 'border-gray-300 focus:border-blue-500 focus:ring-blue-500',
  error: 'border-red-300 focus:border-red-500 focus:ring-red-500 bg-red-50',
  success: 'border-green-300 focus:border-green-500 focus:ring-green-500 bg-green-50',
};

// Input size styles
const inputSizes = {
  sm: 'px-3 py-2 text-sm',
  md: 'px-4 py-2.5 text-sm',
  lg: 'px-4 py-3 text-base',
};

// Label size styles
const labelSizes = {
  sm: 'text-sm',
  md: 'text-sm',
  lg: 'text-base',
};

// Base input styles
const baseInputStyles = `
  block w-full rounded-md border
  shadow-sm transition-all duration-200
  focus:ring-2 focus:ring-offset-0 focus:outline-none
  disabled:bg-gray-50 disabled:text-gray-500 disabled:cursor-not-allowed
  placeholder:text-gray-400
`;

// Eye icon for password toggle
const EyeIcon: React.FC<{ isVisible: boolean }> = ({ isVisible }) => (
  <svg
    className="w-5 h-5"
    fill="none"
    stroke="currentColor"
    viewBox="0 0 24 24"
    aria-hidden="true"
  >
    {isVisible ? (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth={2}
        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L6.111 6.111M14.12 14.12l3.767 3.768M14.12 14.12L6.111 6.111m7.12 8.009a3 3 0 01-4.243-4.243"
      />
    ) : (
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth={2}
        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
      />
    )}
  </svg>
);

// Input component with forwardRef
export const Input = forwardRef<HTMLInputElement, InputProps>(
  (
    {
      variant = 'default',
      inputSize = 'md',
      label,
      helperText,
      errorMessage,
      leftIcon,
      rightIcon,
      leftAddon,
      rightAddon,
      fullWidth = true,
      showPasswordToggle = false,
      className,
      type = 'text',
      id,
      disabled = false,
      ...props
    },
    ref
  ) => {
    const [showPassword, setShowPassword] = useState(false);
    const [_isFocused, setIsFocused] = useState(false);

    // Generate unique ID if not provided
    const inputId = id || `input-${Math.random().toString(36).substr(2, 9)}`;

    // Determine actual input type
    const actualType = showPasswordToggle && type === 'password'
      ? (showPassword ? 'text' : 'password')
      : type;

    // Determine variant based on error
    const actualVariant = errorMessage ? 'error' : variant;

    // Build input classes
    const inputClasses = cn(
      baseInputStyles,
      inputVariants[actualVariant],
      inputSizes[inputSize],
      leftIcon || leftAddon ? 'pl-10' : '',
      rightIcon || rightAddon || showPasswordToggle ? 'pr-10' : '',
      !fullWidth && 'w-auto',
      className
    );

    // Handle password toggle
    const togglePassword = () => {
      setShowPassword(!showPassword);
    };

    return (
      <div className={cn('relative', fullWidth ? 'w-full' : 'inline-block')}>
        {/* Label */}
        {label && (
          <label
            htmlFor={inputId}
            className={cn(
              'block font-medium text-gray-700 mb-2',
              labelSizes[inputSize],
              disabled && 'text-gray-500'
            )}
          >
            {label}
            {props.required && (
              <span className="text-red-500 ml-1" aria-label="required">
                *
              </span>
            )}
          </label>
        )}

        {/* Input container */}
        <div className="relative">
          {/* Left addon */}
          {leftAddon && (
            <div className="absolute inset-y-0 left-0 flex items-center">
              <span className="px-3 py-2 bg-gray-50 border-r border-gray-300 text-gray-500 text-sm">
                {leftAddon}
              </span>
            </div>
          )}

          {/* Left icon */}
          {leftIcon && !leftAddon && (
            <div className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none">
              <span className="text-gray-400" aria-hidden="true">
                {leftIcon}
              </span>
            </div>
          )}

          {/* Input field */}
          <input
            ref={ref}
            type={actualType}
            id={inputId}
            disabled={disabled}
            className={inputClasses}
            onFocus={(e) => {
              setIsFocused(true);
              props.onFocus?.(e);
            }}
            onBlur={(e) => {
              setIsFocused(false);
              props.onBlur?.(e);
            }}
            aria-invalid={!!errorMessage}
            aria-describedby={
              errorMessage
                ? `${inputId}-error`
                : helperText
                ? `${inputId}-helper`
                : undefined
            }
            {...props}
          />

          {/* Right addon */}
          {rightAddon && (
            <div className="absolute inset-y-0 right-0 flex items-center">
              <span className="px-3 py-2 bg-gray-50 border-l border-gray-300 text-gray-500 text-sm">
                {rightAddon}
              </span>
            </div>
          )}

          {/* Right icon or password toggle */}
          {(rightIcon || showPasswordToggle) && !rightAddon && (
            <div className="absolute inset-y-0 right-0 flex items-center pr-3">
              {showPasswordToggle ? (
                <button
                  type="button"
                  onClick={togglePassword}
                  className="text-gray-400 hover:text-gray-600 focus:outline-none focus:text-gray-600 transition-colors"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                  tabIndex={-1}
                >
                  <EyeIcon isVisible={!showPassword} />
                </button>
              ) : rightIcon ? (
                <span className="text-gray-400 pointer-events-none" aria-hidden="true">
                  {rightIcon}
                </span>
              ) : null}
            </div>
          )}
        </div>

        {/* Helper text or error message */}
        {(helperText || errorMessage) && (
          <div className="mt-2">
            {errorMessage ? (
              <p
                id={`${inputId}-error`}
                className="text-sm text-red-600"
                role="alert"
                aria-live="polite"
              >
                {errorMessage}
              </p>
            ) : helperText ? (
              <p
                id={`${inputId}-helper`}
                className="text-sm text-gray-500"
              >
                {helperText}
              </p>
            ) : null}
          </div>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

// Textarea component with similar styling
export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  variant?: InputVariant;
  inputSize?: InputSize;
  label?: string;
  helperText?: string;
  errorMessage?: string;
  fullWidth?: boolean;
  resize?: 'none' | 'vertical' | 'horizontal' | 'both';
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
  (
    {
      variant = 'default',
      inputSize = 'md',
      label,
      helperText,
      errorMessage,
      fullWidth = true,
      resize = 'vertical',
      className,
      id,
      disabled = false,
      ...props
    },
    ref
  ) => {
    // Generate unique ID if not provided
    const textareaId = id || `textarea-${Math.random().toString(36).substr(2, 9)}`;

    // Determine variant based on error
    const actualVariant = errorMessage ? 'error' : variant;

    // Build textarea classes
    const textareaClasses = cn(
      baseInputStyles,
      inputVariants[actualVariant],
      inputSizes[inputSize],
      !fullWidth && 'w-auto',
      resize === 'none' && 'resize-none',
      resize === 'vertical' && 'resize-y',
      resize === 'horizontal' && 'resize-x',
      resize === 'both' && 'resize',
      className
    );

    return (
      <div className={cn('relative', fullWidth ? 'w-full' : 'inline-block')}>
        {/* Label */}
        {label && (
          <label
            htmlFor={textareaId}
            className={cn(
              'block font-medium text-gray-700 mb-2',
              labelSizes[inputSize],
              disabled && 'text-gray-500'
            )}
          >
            {label}
            {props.required && (
              <span className="text-red-500 ml-1" aria-label="required">
                *
              </span>
            )}
          </label>
        )}

        {/* Textarea field */}
        <textarea
          ref={ref}
          id={textareaId}
          disabled={disabled}
          className={textareaClasses}
          aria-invalid={!!errorMessage}
          aria-describedby={
            errorMessage
              ? `${textareaId}-error`
              : helperText
              ? `${textareaId}-helper`
              : undefined
          }
          {...props}
        />

        {/* Helper text or error message */}
        {(helperText || errorMessage) && (
          <div className="mt-2">
            {errorMessage ? (
              <p
                id={`${textareaId}-error`}
                className="text-sm text-red-600"
                role="alert"
                aria-live="polite"
              >
                {errorMessage}
              </p>
            ) : helperText ? (
              <p
                id={`${textareaId}-helper`}
                className="text-sm text-gray-500"
              >
                {helperText}
              </p>
            ) : null}
          </div>
        )}
      </div>
    );
  }
);

Textarea.displayName = 'Textarea';

// Example usage components (for documentation)
export const InputExamples: React.FC = () => (
  <div className="space-y-8 p-6 max-w-md">
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Basic Inputs</h3>
      <Input label="Email" type="email" placeholder="Enter your email" />
      <Input
        label="Password"
        type="password"
        placeholder="Enter your password"
        showPasswordToggle
      />
      <Input label="Required Field" required placeholder="This field is required" />
    </div>

    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Input States</h3>
      <Input label="Default" placeholder="Default input" />
      <Input
        label="With Error"
        placeholder="Input with error"
        errorMessage="This field is required"
      />
      <Input
        label="Success"
        variant="success"
        placeholder="Success input"
        helperText="This field is valid"
      />
      <Input label="Disabled" placeholder="Disabled input" disabled />
    </div>

    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Input Sizes</h3>
      <Input inputSize="sm" placeholder="Small input" />
      <Input inputSize="md" placeholder="Medium input" />
      <Input inputSize="lg" placeholder="Large input" />
    </div>

    <div className="space-y-4">
      <h3 className="text-lg font-semibold">With Icons and Addons</h3>
      <Input
        placeholder="Search..."
        leftIcon={<span>🔍</span>}
      />
      <Input
        placeholder="Amount"
        leftAddon="$"
        rightAddon=".00"
        type="number"
      />
    </div>

    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Textarea</h3>
      <Textarea
        label="Message"
        placeholder="Enter your message..."
        rows={4}
        helperText="Please provide detailed information"
      />
    </div>
  </div>
);

export default Input;