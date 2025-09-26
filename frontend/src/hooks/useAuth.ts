/**
 * Authentication Hook
 * Provides a unified interface for authentication operations
 * Integrates Zustand store with TanStack Query mutations
 */

import { useCallback, useEffect, useMemo } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore, authSelectors } from '../stores/authStore';
import { useAuthMutations, createAuthApiWithStore, AUTH_QUERY_KEYS as _AUTH_QUERY_KEYS } from '../services/auth/authApi';
import type {
  LoginRequest,
  RegisterRequest,
  PasswordResetRequest as _PasswordResetRequest,
  PasswordResetConfirmRequest,
  UpdateUserProfileRequest,
  ChangeEmailRequest,
  ChangePasswordRequest,
  EmailVerificationRequest as _EmailVerificationRequest,
  AuthResult,
  OperationResult,
  UseAuthReturn,
  User as _User,
} from '../types/auth';
import { UserRole } from '../types/auth';

/**
 * Main authentication hook
 * Provides all authentication functionality in a single hook
 */
export const useAuth = (): UseAuthReturn => {
  const queryClient = useQueryClient();

  // Zustand store selectors
  const user = useAuthStore(authSelectors.user);
  const isAuthenticated = useAuthStore(authSelectors.isAuthenticated);
  const isLoading = useAuthStore(authSelectors.isLoading);
  const error = useAuthStore(authSelectors.error);
  const accessToken = useAuthStore(authSelectors.accessToken);

  // Store actions
  const setError = useAuthStore((state) => state.setError);
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const updateUser = useAuthStore((state) => state.updateUser);
  const hasRole = useAuthStore((state) => state.hasRole);
  const _hasAnyRole = useAuthStore((state) => state.hasAnyRole);
  const isTokenValid = useAuthStore((state) => state.isTokenValid);
  const shouldRefreshToken = useAuthStore((state) => state.shouldRefreshToken);
  const _getTimeToExpiry = useAuthStore((state) => state.shouldRefreshToken);

  // TanStack Query mutations
  const {
    loginMutation,
    registerMutation,
    logoutMutation,
    passwordResetRequestMutation,
    passwordResetMutation,
    changePasswordMutation,
    verifyEmailMutation,
    resendVerificationMutation,
    changeEmailMutation,
    updateProfileMutation,
  } = useAuthMutations();

  // Create enhanced API with store integration
  const authApi = useMemo(() => createAuthApiWithStore(), []);

  // Initialize authentication state on mount
  useEffect(() => {
    const store = useAuthStore.getState();
    store.initialize();
  }, []);

  // Utility functions
  const clearError = useCallback(() => {
    setError(null);
  }, [setError]);

  // Authentication actions
  const login = useCallback(
    async (credentials: LoginRequest): Promise<AuthResult> => {
      try {
        clearError();
        const result = await authApi.login(credentials);

        if (!result.success) {
          setError(result.errorMessage || 'Login failed');
        }

        return result;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Login failed';
        setError(errorMessage);
        return {
          success: false,
          errorMessage,
          errors: [errorMessage],
          tokenType: 'Bearer',
          requiresEmailVerification: false,
          requiresTwoFactor: false,
          isLockedOut: false,
        };
      }
    },
    [authApi, setError, clearError]
  );

  const register = useCallback(
    async (userData: RegisterRequest): Promise<AuthResult> => {
      try {
        clearError();
        const result = await authApi.register(userData);

        if (!result.success) {
          setError(result.errorMessage || 'Registration failed');
        }

        return result;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Registration failed';
        setError(errorMessage);
        return {
          success: false,
          errorMessage,
          errors: [errorMessage],
          tokenType: 'Bearer',
          requiresEmailVerification: false,
          requiresTwoFactor: false,
          isLockedOut: false,
        };
      }
    },
    [authApi, setError, clearError]
  );

  const logout = useCallback(() => {
    // Clear all authentication data
    clearAuth();

    // Clear all cached queries
    queryClient.clear();

    // Call logout API
    logoutMutation.mutate(undefined, {
      onError: (error) => {
        console.warn('Logout API call failed:', error);
      },
    });
  }, [clearAuth, queryClient, logoutMutation]);

  const refreshToken = useCallback(async (): Promise<boolean> => {
    try {
      const result = await authApi.refreshToken();
      return result;
    } catch (error) {
      console.error('Token refresh failed:', error);
      logout();
      return false;
    }
  }, [authApi, logout]);

  // Profile management
  const updateProfile = useCallback(
    async (data: UpdateUserProfileRequest): Promise<OperationResult> => {
      return new Promise((resolve) => {
        updateProfileMutation.mutate(data, {
          onSuccess: (result) => {
            resolve(result);
          },
          onError: (error) => {
            const errorMessage = error instanceof Error ? error.message : 'Profile update failed';
            resolve({
              success: false,
              message: errorMessage,
              errors: [errorMessage],
            });
          },
        });
      });
    },
    [updateProfileMutation]
  );

  const changePassword = useCallback(
    async (data: ChangePasswordRequest): Promise<OperationResult> => {
      return new Promise((resolve) => {
        changePasswordMutation.mutate(data, {
          onSuccess: (result) => {
            resolve(result);
          },
          onError: (error) => {
            const errorMessage = error instanceof Error ? error.message : 'Password change failed';
            resolve({
              success: false,
              message: errorMessage,
              errors: [errorMessage],
            });
          },
        });
      });
    },
    [changePasswordMutation]
  );

  const changeEmail = useCallback(
    async (data: ChangeEmailRequest): Promise<OperationResult> => {
      return new Promise((resolve) => {
        changeEmailMutation.mutate(data, {
          onSuccess: (result) => {
            resolve(result);
          },
          onError: (error) => {
            const errorMessage = error instanceof Error ? error.message : 'Email change failed';
            resolve({
              success: false,
              message: errorMessage,
              errors: [errorMessage],
            });
          },
        });
      });
    },
    [changeEmailMutation]
  );

  // Password reset
  const requestPasswordReset = useCallback(
    async (email: string): Promise<OperationResult> => {
      return new Promise((resolve) => {
        passwordResetRequestMutation.mutate(
          { email },
          {
            onSuccess: (result) => {
              resolve(result);
            },
            onError: (error) => {
              const errorMessage = error instanceof Error ? error.message : 'Password reset request failed';
              resolve({
                success: false,
                message: errorMessage,
                errors: [errorMessage],
              });
            },
          }
        );
      });
    },
    [passwordResetRequestMutation]
  );

  const confirmPasswordReset = useCallback(
    async (data: PasswordResetConfirmRequest): Promise<OperationResult> => {
      return new Promise((resolve) => {
        passwordResetMutation.mutate(data, {
          onSuccess: (result) => {
            resolve(result);
          },
          onError: (error) => {
            const errorMessage = error instanceof Error ? error.message : 'Password reset failed';
            resolve({
              success: false,
              message: errorMessage,
              errors: [errorMessage],
            });
          },
        });
      });
    },
    [passwordResetMutation]
  );

  // Email verification
  const verifyEmail = useCallback(
    async (token: string): Promise<OperationResult> => {
      return new Promise((resolve) => {
        verifyEmailMutation.mutate(
          { token },
          {
            onSuccess: (result) => {
              if (result.success && user) {
                updateUser({ isEmailVerified: true });
              }
              resolve(result);
            },
            onError: (error) => {
              const errorMessage = error instanceof Error ? error.message : 'Email verification failed';
              resolve({
                success: false,
                message: errorMessage,
                errors: [errorMessage],
              });
            },
          }
        );
      });
    },
    [verifyEmailMutation, user, updateUser]
  );

  const resendVerificationEmail = useCallback(
    async (): Promise<OperationResult> => {
      return new Promise((resolve) => {
        resendVerificationMutation.mutate(undefined, {
          onSuccess: (result) => {
            resolve(result);
          },
          onError: (error) => {
            const errorMessage = error instanceof Error ? error.message : 'Failed to resend verification email';
            resolve({
              success: false,
              message: errorMessage,
              errors: [errorMessage],
            });
          },
        });
      });
    },
    [resendVerificationMutation]
  );

  const hasRoleWrapper = useCallback(
    (role: UserRole): boolean => {
      return hasRole(role);
    },
    [hasRole]
  );

  const isTokenExpired = useCallback((): boolean => {
    return !isTokenValid();
  }, [isTokenValid]);

  const getTimeToExpiryWrapper = useCallback((): number => {
    if (!accessToken) return 0;

    try {
      const payload = JSON.parse(atob(accessToken.split('.')[1]));
      const expiryTime = payload.exp * 1000;
      const currentTime = Date.now();
      return Math.max(0, expiryTime - currentTime);
    } catch {
      return 0;
    }
  }, [accessToken]);

  // Setup automatic token refresh
  useEffect(() => {
    if (!isAuthenticated || !accessToken) return;

    const checkTokenExpiry = () => {
      if (shouldRefreshToken()) {
        refreshToken();
      }
    };

    // Check immediately
    checkTokenExpiry();

    // Set up interval to check every minute
    const interval = setInterval(checkTokenExpiry, 60 * 1000);

    return () => clearInterval(interval);
  }, [isAuthenticated, accessToken, shouldRefreshToken, refreshToken]);

  // Combined loading state
  const combinedIsLoading = useMemo(() => {
    return (
      isLoading ||
      loginMutation.isPending ||
      registerMutation.isPending ||
      logoutMutation.isPending ||
      updateProfileMutation.isPending ||
      changePasswordMutation.isPending ||
      changeEmailMutation.isPending ||
      passwordResetRequestMutation.isPending ||
      passwordResetMutation.isPending ||
      verifyEmailMutation.isPending ||
      resendVerificationMutation.isPending
    );
  }, [
    isLoading,
    loginMutation.isPending,
    registerMutation.isPending,
    logoutMutation.isPending,
    updateProfileMutation.isPending,
    changePasswordMutation.isPending,
    changeEmailMutation.isPending,
    passwordResetRequestMutation.isPending,
    passwordResetMutation.isPending,
    verifyEmailMutation.isPending,
    resendVerificationMutation.isPending,
  ]);

  // Initialize function
  const initialize = useCallback(() => {
    const store = useAuthStore.getState();
    store.initialize();
  }, []);

  return {
    // State
    user,
    isAuthenticated,
    isLoading: combinedIsLoading,
    loading: combinedIsLoading, // Alias for backward compatibility
    error,

    // Authentication actions
    login,
    register,
    logout,
    refreshToken,
    clearError,
    initialize,

    // Profile management
    updateProfile,
    changePassword,
    changeEmail,

    // Password reset
    requestPasswordReset,
    confirmPasswordReset,

    // Email verification
    verifyEmail,
    resendVerificationEmail,

    // Utility functions
    hasRole: hasRoleWrapper,
    isTokenExpired,
    getTimeToExpiry: getTimeToExpiryWrapper,
  };
};

/**
 * Hook for checking user permissions
 */
export const usePermissions = () => {
  const user = useAuthStore(authSelectors.user);
  const hasRole = useAuthStore((state) => state.hasRole);
  const hasAnyRole = useAuthStore((state) => state.hasAnyRole);

  const permissions = useMemo(() => {
    if (!user) return [];

    switch (user.role) {
      case UserRole.Admin:
        return [
          'read:posts',
          'write:posts',
          'manage:posts',
          'manage:users',
          'manage:system',
          'moderate:comments',
        ];
      case UserRole.Author:
        return ['read:posts', 'write:posts', 'manage:posts'];
      case UserRole.User:
      default:
        return ['read:posts'];
    }
  }, [user]);

  const hasPermission = useCallback(
    (permission: string): boolean => {
      return permissions.includes(permission);
    },
    [permissions]
  );

  return {
    permissions,
    hasPermission,
    hasRole,
    hasAnyRole,
    isAdmin: user?.role === UserRole.Admin,
    isAuthor: user?.role === UserRole.Author,
    isUser: user?.role === UserRole.User,
    canManagePosts: hasAnyRole([UserRole.Admin, UserRole.Author]),
    canManageUsers: hasRole(UserRole.Admin),
    canModerate: hasRole(UserRole.Admin),
  };
};

/**
 * Hook for authentication loading states
 */
export const useAuthLoading = () => {
  const loadingStates = useAuthStore(authSelectors.loadingStates);
  const isLoading = useAuthStore(authSelectors.isLoading);

  return {
    ...loadingStates,
    isLoading,
    isAnyLoading: Object.values(loadingStates).some(Boolean) || isLoading,
  };
};

/**
 * Hook for user profile information
 */
export const useUserProfile = () => {
  const user = useAuthStore(authSelectors.user);
  const displayName = useAuthStore(authSelectors.userDisplayName);

  return {
    user,
    displayName,
    avatar: user?.avatar,
    fullName: user?.fullName,
    email: user?.email,
    userName: user?.userName,
    isEmailVerified: user?.isEmailVerified ?? false,
    role: user?.role,
    createdAt: user?.createdAt,
    lastLoginAt: user?.lastLoginAt,
  };
};

export default useAuth;