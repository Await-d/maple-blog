/**
 * Authentication related TypeScript type definitions
 * These interfaces match the backend DTOs for type safety
 */

// User Role enum matching backend UserRole
export enum UserRole {
  User = 0,
  Author = 1,
  Admin = 2,
}

// User interface matching backend UserDto
export interface User {
  id: string;
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  fullName: string;
  displayName: string;
  avatar?: string;
  role: UserRole;
  isEmailVerified: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  updatedAt: string;
}

// Extended User Profile interface for comprehensive profile management
export interface UserProfile {
  id: string;
  username: string;
  email: string;
  displayName: string;
  bio?: string;
  avatar?: string;
  location?: string;
  website?: string;
  socialLinks: {
    twitter?: string;
    github?: string;
    linkedin?: string;
  };
  birthday?: string;
  timezone: string;
  preferences: {
    language: string;
    theme: 'light' | 'dark' | 'auto';
    emailNotifications: boolean;
    marketingEmails: boolean;
    profileVisibility: 'public' | 'private';
    showEmail: boolean;
  };
  stats: {
    postsCount: number;
    commentsCount: number;
    joinDate: string;
    lastLoginDate: string;
    totalViews: number;
  };
  security: {
    twoFactorEnabled: boolean;
    lastPasswordChange: string;
    activeSessions: number;
    loginHistory: Array<{
      date: string;
      ip: string;
      device: string;
      location: string;
    }>;
  };
}

// Login request interface matching backend LoginRequest
export interface LoginRequest {
  emailOrUsername: string;
  password: string;
  rememberMe?: boolean;
}

// Registration request interface matching backend RegisterRequest
export interface RegisterRequest {
  email: string;
  userName: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  agreeToTerms: boolean;
}

// Password reset request interface matching backend PasswordResetRequest
export interface PasswordResetRequest {
  email: string;
}

// Password reset confirmation interface matching backend PasswordResetConfirmRequest
export interface PasswordResetConfirmRequest {
  token: string;
  password: string;
  confirmPassword: string;
}

// Update user profile interface matching backend UpdateUserProfileRequest
export interface UpdateUserProfileRequest {
  firstName: string;
  lastName: string;
  avatar?: string;
}

// Change email interface matching backend ChangeEmailRequest
export interface ChangeEmailRequest {
  email: string;
  password: string;
}

// Change password interface matching backend ChangePasswordRequest
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// Email verification interface matching backend EmailVerificationRequest
export interface EmailVerificationRequest {
  token: string;
}

// Refresh token interface matching backend RefreshTokenRequest
export interface RefreshTokenRequest {
  refreshToken: string;
}

// Authentication result interface matching backend AuthResult
export interface AuthResult {
  success: boolean;
  errorMessage?: string;
  errors: string[];
  accessToken?: string;
  refreshToken?: string;
  tokenType: string;
  expiresAt?: string;
  user?: User;
  requiresEmailVerification: boolean;
  requiresTwoFactor: boolean;
  isLockedOut: boolean;
  lockoutEnd?: string;
}

// Token refresh result interface matching backend TokenRefreshResult
export interface TokenRefreshResult {
  success: boolean;
  errorMessage?: string;
  accessToken?: string;
  refreshToken?: string;
  tokenType: string;
  expiresAt?: string;
}

// Operation result interface matching backend OperationResult
export interface OperationResult {
  success: boolean;
  message?: string;
  errors: string[];
}

// Generic operation result with data
export interface OperationResultWithData<T> extends OperationResult {
  data?: T;
}

// Authentication state for global state management
export interface AuthState {
  // Current authenticated user
  user: User | null;
  // Authentication tokens
  accessToken: string | null;
  refreshToken: string | null;
  // Authentication status
  isAuthenticated: boolean;
  isLoading: boolean;
  // Token expiration
  expiresAt: Date | null;
  // Error handling
  error: string | null;
  // Remember me preference
  rememberMe: boolean;
}

// Form validation errors
export interface FormErrors {
  [key: string]: string[];
}

// Login form data
export interface LoginFormData {
  emailOrUsername: string;
  password: string;
  rememberMe: boolean;
}

// Registration form data
export interface RegisterFormData {
  email: string;
  userName: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  agreeToTerms: boolean;
}

// Password reset form data
export interface PasswordResetFormData {
  email: string;
}

// Password reset confirmation form data
export interface PasswordResetConfirmFormData {
  token: string;
  password: string;
  confirmPassword: string;
}

// User profile update form data
export interface UserProfileFormData {
  firstName: string;
  lastName: string;
  avatar?: string;
}

// Change email form data
export interface ChangeEmailFormData {
  email: string;
  password: string;
}

// Change password form data
export interface ChangePasswordFormData {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// Authentication context type
export interface AuthContextType {
  // State
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // Actions
  login: (credentials: LoginRequest) => Promise<AuthResult>;
  register: (userData: RegisterRequest) => Promise<AuthResult>;
  logout: () => void;
  refreshToken: () => Promise<boolean>;
  updateProfile: (data: UpdateUserProfileRequest) => Promise<OperationResult>;
  changeEmail: (data: ChangeEmailRequest) => Promise<OperationResult>;
  changePassword: (data: ChangePasswordRequest) => Promise<OperationResult>;
  requestPasswordReset: (data: PasswordResetRequest) => Promise<OperationResult>;
  resetPassword: (data: PasswordResetConfirmRequest) => Promise<OperationResult>;
  verifyEmail: (data: EmailVerificationRequest) => Promise<OperationResult>;
  resendVerificationEmail: () => Promise<OperationResult>;
}

// API response wrapper
export interface ApiResponse<T = unknown> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
  statusCode?: number;
}

// JWT Token payload interface (for token parsing)
export interface JwtPayload {
  sub: string; // User ID
  email: string;
  userName: string;
  role: string;
  iat: number; // Issued at
  exp: number; // Expires at
  aud: string; // Audience
  iss: string; // Issuer
}

// Route protection props
export interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: UserRole[];
  requireEmailVerification?: boolean;
  fallback?: React.ReactNode;
  redirectTo?: string;
}

// Permission checking utility type
export type Permission =
  | 'read:posts'
  | 'write:posts'
  | 'manage:posts'
  | 'manage:users'
  | 'manage:system'
  | 'moderate:comments';

// User permissions mapping
export const USER_PERMISSIONS: Record<UserRole, Permission[]> = {
  [UserRole.User]: ['read:posts'],
  [UserRole.Author]: ['read:posts', 'write:posts', 'manage:posts'],
  [UserRole.Admin]: [
    'read:posts',
    'write:posts',
    'manage:posts',
    'manage:users',
    'manage:system',
    'moderate:comments'
  ],
};

// Authentication loading states
export interface AuthLoadingStates {
  login: boolean;
  register: boolean;
  logout: boolean;
  refresh: boolean;
  updateProfile: boolean;
  changeEmail: boolean;
  changePassword: boolean;
  resetPassword: boolean;
  verifyEmail: boolean;
  resendVerification: boolean;
}

// Form field configuration for dynamic form generation
export interface FormFieldConfig {
  name: string;
  type: 'text' | 'email' | 'password' | 'checkbox' | 'select';
  label: string;
  placeholder?: string;
  required?: boolean;
  validation?: {
    minLength?: number;
    maxLength?: number;
    pattern?: RegExp;
    custom?: (value: unknown) => string | null;
  };
  options?: Array<{ value: string; label: string }>; // For select fields
}

// Authentication events for analytics/tracking
export interface AuthEvent {
  type: 'login_attempt' | 'login_success' | 'login_failure' | 'registration' | 'logout' | 'token_refresh';
  timestamp: Date;
  userId?: string;
  metadata?: Record<string, unknown>;
}

// Security settings
export interface SecuritySettings {
  passwordMinLength: number;
  passwordRequireUppercase: boolean;
  passwordRequireLowercase: boolean;
  passwordRequireNumbers: boolean;
  passwordRequireSpecialChars: boolean;
  sessionTimeout: number; // minutes
  maxLoginAttempts: number;
  lockoutDuration: number; // minutes
  tokenRefreshThreshold: number; // minutes before expiry
}

// UseAuth hook return type
export interface UseAuthReturn {
  // State
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  loading: boolean; // Alias for isLoading for backward compatibility
  error: string | null;

  // Authentication actions
  login: (credentials: LoginRequest) => Promise<AuthResult>;
  register: (userData: RegisterRequest) => Promise<AuthResult>;
  logout: () => void;
  refreshToken: () => Promise<boolean>;
  clearError: () => void;
  initialize: () => void; // Initialize auth state

  // Profile management
  updateProfile: (data: UpdateUserProfileRequest) => Promise<OperationResult>;
  changePassword: (data: ChangePasswordRequest) => Promise<OperationResult>;
  changeEmail: (data: ChangeEmailRequest) => Promise<OperationResult>;

  // Password reset
  requestPasswordReset: (email: string) => Promise<OperationResult>;
  confirmPasswordReset: (data: PasswordResetConfirmRequest) => Promise<OperationResult>;

  // Email verification
  verifyEmail: (token: string) => Promise<OperationResult>;
  resendVerificationEmail: () => Promise<OperationResult>;

  // Utility functions
  hasRole: (role: UserRole) => boolean;
  isTokenExpired: () => boolean;
  getTimeToExpiry: () => number;
}

export default {
  UserRole,
  USER_PERMISSIONS,
};