// @ts-nocheck
/**
 * Authentication API Service using TanStack Query
 * Handles all authentication-related API communications with the backend
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import axios, { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { ENV_CONFIG } from '../../types/env';
import {
  AuthResult,
  TokenRefreshResult,
  OperationResult,
  OperationResultWithData,
  LoginRequest,
  RegisterRequest,
  PasswordResetRequest,
  PasswordResetConfirmRequest,
  UpdateUserProfileRequest,
  ChangeEmailRequest,
  ChangePasswordRequest,
  EmailVerificationRequest,
  RefreshTokenRequest,
  User,
  ApiResponse,
} from '../../types/auth';
import { useAuthStore } from '../../stores/authStore';

// Extended request config to include retry flag
interface ExtendedAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

// API base URL from environment variables
const API_BASE_URL = `${ENV_CONFIG.API_URL}/api`;

// Create axios instance with default configuration
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add authentication token
apiClient.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle token refresh and errors
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as ExtendedAxiosRequestConfig;

    // Handle 401 Unauthorized - try to refresh token
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;

      const authStore = useAuthStore.getState();

      try {
        // Call refreshAuthToken method on the store
        const refreshSuccess = await authStore.refreshAuthToken();

        if (refreshSuccess) {
          // Retry the original request with new token
          const newToken = authStore.getAccessToken();
          if (newToken && originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
            return apiClient(originalRequest);
          }
        }
      } catch (refreshError) {
        console.error('Token refresh failed:', refreshError);
      }

      // This block is now handled above

      // If refresh failed, logout and redirect
      authStore.logout();
      window.location.href = '/login';
    }

    return Promise.reject(error);
  }
);

// API response wrapper to handle standard response format
const handleApiResponse = <T>(response: AxiosResponse<ApiResponse<T>>): T => {
  if (!response.data.success) {
    throw new Error(response.data.message || 'API request failed');
  }
  return response.data.data!;
};

// API endpoints
const API_ENDPOINTS = {
  AUTH: {
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
    LOGOUT: '/auth/logout',
    REFRESH: '/auth/refresh',
    ME: '/auth/me',
  },
  PASSWORD: {
    RESET_REQUEST: '/auth/forgot-password',
    RESET_CONFIRM: '/auth/reset-password',
    CHANGE: '/auth/change-password',
  },
  EMAIL: {
    VERIFY: '/auth/verify-email',
    RESEND_VERIFICATION: '/auth/resend-verification',
    CHANGE: '/auth/change-email',
  },
  PROFILE: {
    UPDATE: '/auth/profile',
  },
} as const;

// Query keys for TanStack Query caching
export const AUTH_QUERY_KEYS = {
  AUTH: ['auth'] as const,
  USER: ['auth', 'user'] as const,
  PROFILE: ['auth', 'profile'] as const,
} as const;

// Authentication API functions
export const authApi = {
  // Login user
  login: async (credentials: LoginRequest): Promise<AuthResult> => {
    try {
      const response = await apiClient.post<ApiResponse<AuthResult>>(
        API_ENDPOINTS.AUTH.LOGIN,
        credentials
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          errors: apiError.errors || [apiError.message || 'Login failed'],
          errorMessage: apiError.message,
          tokenType: 'Bearer',
          requiresEmailVerification: false,
          requiresTwoFactor: false,
          isLockedOut: false,
        };
      }
      throw error;
    }
  },

  // Register new user
  register: async (userData: RegisterRequest): Promise<AuthResult> => {
    try {
      const response = await apiClient.post<ApiResponse<AuthResult>>(
        API_ENDPOINTS.AUTH.REGISTER,
        userData
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          errors: apiError.errors || [apiError.message || 'Registration failed'],
          errorMessage: apiError.message,
          tokenType: 'Bearer',
          requiresEmailVerification: false,
          requiresTwoFactor: false,
          isLockedOut: false,
        };
      }
      throw error;
    }
  },

  // Logout user
  logout: async (): Promise<void> => {
    try {
      await apiClient.post(API_ENDPOINTS.AUTH.LOGOUT);
    } catch (error) {
      // Even if logout fails on server, we should clear local state
      console.warn('Logout request failed:', error);
    }
  },

  // Refresh authentication token
  refreshToken: async (refreshToken: string): Promise<TokenRefreshResult> => {
    try {
      const response = await apiClient.post<ApiResponse<TokenRefreshResult>>(
        API_ENDPOINTS.AUTH.REFRESH,
        { refreshToken } as RefreshTokenRequest
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          errorMessage: apiError.message || 'Token refresh failed',
          tokenType: 'Bearer',
        };
      }
      throw error;
    }
  },

  // Get current user information
  getCurrentUser: async (): Promise<User> => {
    try {
      const response = await apiClient.get<ApiResponse<User>>(API_ENDPOINTS.AUTH.ME);
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 401) {
        useAuthStore.getState().logout();
      }
      throw error;
    }
  },

  // Request password reset
  requestPasswordReset: async (data: PasswordResetRequest): Promise<OperationResult> => {
    try {
      const response = await apiClient.post<ApiResponse<OperationResult>>(
        API_ENDPOINTS.PASSWORD.RESET_REQUEST,
        data
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Password reset request failed',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },

  // Confirm password reset
  resetPassword: async (data: PasswordResetConfirmRequest): Promise<OperationResult> => {
    try {
      const response = await apiClient.post<ApiResponse<OperationResult>>(
        API_ENDPOINTS.PASSWORD.RESET_CONFIRM,
        data
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Password reset failed',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },

  // Change password
  changePassword: async (data: ChangePasswordRequest): Promise<OperationResult> => {
    try {
      const response = await apiClient.post<ApiResponse<OperationResult>>(
        API_ENDPOINTS.PASSWORD.CHANGE,
        data
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Password change failed',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },

  // Verify email
  verifyEmail: async (data: EmailVerificationRequest): Promise<OperationResult> => {
    try {
      const response = await apiClient.post<ApiResponse<OperationResult>>(
        API_ENDPOINTS.EMAIL.VERIFY,
        data
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Email verification failed',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },

  // Resend email verification
  resendEmailVerification: async (): Promise<OperationResult> => {
    try {
      const response = await apiClient.post<ApiResponse<OperationResult>>(
        API_ENDPOINTS.EMAIL.RESEND_VERIFICATION
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Failed to resend verification email',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },

  // Change email
  changeEmail: async (data: ChangeEmailRequest): Promise<OperationResult> => {
    try {
      const response = await apiClient.post<ApiResponse<OperationResult>>(
        API_ENDPOINTS.EMAIL.CHANGE,
        data
      );
      return handleApiResponse(response);
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Email change failed',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },

  // Update user profile
  updateProfile: async (data: UpdateUserProfileRequest): Promise<OperationResultWithData<User>> => {
    try {
      const response = await apiClient.put<ApiResponse<User>>(
        API_ENDPOINTS.PROFILE.UPDATE,
        data
      );
      const updatedUser = handleApiResponse(response);
      return {
        success: true,
        data: updatedUser,
        message: 'Profile updated successfully',
        errors: [],
      };
    } catch (error) {
      if (error instanceof AxiosError && error.response?.data) {
        const apiError = error.response.data as ApiResponse;
        return {
          success: false,
          message: apiError.message || 'Profile update failed',
          errors: apiError.errors || [],
        };
      }
      throw error;
    }
  },
};

// TanStack Query hooks for authentication
export const useAuthMutations = () => {
  const queryClient = useQueryClient();
  const authStore = useAuthStore();

  // Login mutation
  const loginMutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: (data: AuthResult) => {
      if (data.success && data.accessToken && data.refreshToken && data.user) {
        const expiresAt = data.expiresAt ? new Date(data.expiresAt) : undefined;
        authStore.setTokens(data.accessToken, data.refreshToken, expiresAt);
        authStore.setUser(data.user);
        queryClient.setQueryData(AUTH_QUERY_KEYS.USER, data.user);
      }
    },
    onError: (error) => {
      authStore.setError(error instanceof Error ? error.message : 'Login failed');
    },
  });

  // Register mutation
  const registerMutation = useMutation({
    mutationFn: authApi.register,
    onSuccess: (data: AuthResult) => {
      if (data.success && data.accessToken && data.refreshToken && data.user) {
        const expiresAt = data.expiresAt ? new Date(data.expiresAt) : undefined;
        authStore.setTokens(data.accessToken, data.refreshToken, expiresAt);
        authStore.setUser(data.user);
        queryClient.setQueryData(AUTH_QUERY_KEYS.USER, data.user);
      }
    },
    onError: (error) => {
      authStore.setError(error instanceof Error ? error.message : 'Registration failed');
    },
  });

  // Logout mutation
  const logoutMutation = useMutation({
    mutationFn: authApi.logout,
    onSettled: () => {
      authStore.logout();
      queryClient.clear();
    },
  });

  // Password reset request mutation
  const passwordResetRequestMutation = useMutation({
    mutationFn: authApi.requestPasswordReset,
  });

  // Password reset confirmation mutation
  const passwordResetMutation = useMutation({
    mutationFn: authApi.resetPassword,
  });

  // Change password mutation
  const changePasswordMutation = useMutation({
    mutationFn: authApi.changePassword,
  });

  // Email verification mutation
  const verifyEmailMutation = useMutation({
    mutationFn: authApi.verifyEmail,
    onSuccess: (result) => {
      if (result.success) {
        // Update user verification status in store
        authStore.updateUser({ isEmailVerified: true });
        queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.USER });
      }
    },
  });

  // Resend verification email mutation
  const resendVerificationMutation = useMutation({
    mutationFn: authApi.resendEmailVerification,
  });

  // Change email mutation
  const changeEmailMutation = useMutation({
    mutationFn: authApi.changeEmail,
    onSuccess: (result) => {
      if (result.success) {
        queryClient.invalidateQueries({ queryKey: AUTH_QUERY_KEYS.USER });
      }
    },
  });

  // Update profile mutation
  const updateProfileMutation = useMutation({
    mutationFn: authApi.updateProfile,
    onSuccess: (result) => {
      if (result.success && result.data) {
        authStore.updateUser(result.data);
        queryClient.setQueryData(AUTH_QUERY_KEYS.USER, result.data);
      }
    },
  });

  return {
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
  };
};

// Query hook for current user
export const useCurrentUser = () => {
  const authStore = useAuthStore();

  return useQuery({
    queryKey: AUTH_QUERY_KEYS.USER,
    queryFn: authApi.getCurrentUser,
    enabled: authStore.isAuthenticated && !!authStore.getAccessToken(),
    retry: (failureCount, error) => {
      // Don't retry on authentication errors
      if (error instanceof AxiosError && error.response?.status === 401) {
        return false;
      }
      return failureCount < 3;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes (was gcTime)
  });
};

// Enhanced authApi with integrated store updates
export const createAuthApiWithStore = () => {
  const authStore = useAuthStore.getState();

  return {
    ...authApi,

    // Enhanced login with store integration
    login: async (credentials: LoginRequest): Promise<AuthResult> => {
      authStore.updateLoadingState('login', true);
      try {
        const result = await authApi.login(credentials);
        if (result.success && result.accessToken && result.refreshToken && result.user) {
          const expiresAt = result.expiresAt ? new Date(result.expiresAt) : undefined;
          authStore.setTokens(result.accessToken, result.refreshToken, expiresAt);
          authStore.setUser(result.user);
        }
        return result;
      } finally {
        authStore.updateLoadingState('login', false);
      }
    },

    // Enhanced register with store integration
    register: async (userData: RegisterRequest): Promise<AuthResult> => {
      authStore.updateLoadingState('register', true);
      try {
        const result = await authApi.register(userData);
        if (result.success && result.accessToken && result.refreshToken && result.user) {
          const expiresAt = result.expiresAt ? new Date(result.expiresAt) : undefined;
          authStore.setTokens(result.accessToken, result.refreshToken, expiresAt);
          authStore.setUser(result.user);
        }
        return result;
      } finally {
        authStore.updateLoadingState('register', false);
      }
    },

    // Enhanced refresh token with store integration
    refreshToken: async (): Promise<boolean> => {
      const refreshTokenValue = authStore.getRefreshToken();
      if (!refreshTokenValue) return false;

      authStore.updateLoadingState('refresh', true);
      try {
        const result = await authApi.refreshToken(refreshTokenValue);
        if (result.success && result.accessToken && result.refreshToken) {
          const expiresAt = result.expiresAt ? new Date(result.expiresAt) : undefined;
          authStore.setTokens(result.accessToken, result.refreshToken, expiresAt);
          return true;
        }
        return false;
      } catch {
        return false;
      } finally {
        authStore.updateLoadingState('refresh', false);
      }
    },
  };
};

export default authApi;