/**
 * Authentication Feature Index
 * Central exports for authentication-related functionality
 */

// Components - Named exports
export { LoginForm } from './components/LoginForm';
export { RegisterForm } from './components/RegisterForm';
// Component default exports
export { default as LoginFormDefault } from './components/LoginForm';
export { default as RegisterFormDefault } from './components/RegisterForm';

// Re-export from existing auth services and stores
export { authApi } from '@/services/auth/authApi';
export { useAuth } from '@/hooks/useAuth';
export { useAuthStore } from '@/stores/authStore';
// Export with alias for backward compatibility
export { useAuthStore as authStore } from '@/stores/authStore';

// Types (re-export from global types)
export type {
  User,
  AuthState,
  LoginCredentials,
  RegisterData,
  AuthError,
  UserRole,
  UserPermission
} from '@/types/auth';

// Authentication utilities
export const authFeature = {
  name: 'authentication',
  version: '1.0.0',
  description: 'User authentication and authorization system',
  components: {
    LoginForm: () => import('./components/LoginForm').then(m => ({ default: m.LoginForm })),
    RegisterForm: () => import('./components/RegisterForm').then(m => ({ default: m.RegisterForm }))
  },
  services: {
    api: () => import('@/services/auth/authApi').then(m => m.authApi),
    store: () => import('@/stores/authStore').then(m => m.useAuthStore)
  },
  hooks: {
    useAuth: () => import('@/hooks/useAuth').then(m => m.useAuth)
  }
} as const;

// Feature configuration
export const authConfig = {
  routes: [
    {
      path: '/login',
      component: 'LoginForm',
      public: true
    },
    {
      path: '/register',
      component: 'RegisterForm',
      public: true
    },
    {
      path: '/reset-password',
      component: 'ResetPasswordForm',
      public: true
    }
  ],
  permissions: {
    admin: ['read', 'write', 'delete', 'manage_users', 'system_admin'],
    editor: ['read', 'write', 'publish_posts', 'manage_content'],
    moderator: ['read', 'moderate_comments', 'manage_users_limited'],
    user: ['read', 'comment', 'like', 'bookmark']
  },
  settings: {
    sessionTimeout: 24 * 60 * 60 * 1000, // 24 hours
    refreshThreshold: 5 * 60 * 1000, // 5 minutes
    maxLoginAttempts: 5,
    lockoutDuration: 15 * 60 * 1000, // 15 minutes
    passwordMinLength: 8,
    requireEmailVerification: true,
    allowSocialLogin: true
  }
} as const;

export default authFeature;