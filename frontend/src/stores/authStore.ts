/**
 * Authentication State Management using Zustand
 * Manages global authentication state, JWT tokens, and user session
 */

import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import {
  User,
  AuthState,
  AuthResult,
  LoginRequest,
  JwtPayload,
  AuthLoadingStates,
  UserRole,
} from '../types/auth';

// JWT token parsing utility
const parseJwtToken = (token: string): JwtPayload | null => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (error) {
    console.error('Error parsing JWT token:', error);
    return null;
  }
};

// Check if token is expired
const isTokenExpired = (token: string): boolean => {
  const payload = parseJwtToken(token);
  if (!payload) return true;

  const currentTime = Math.floor(Date.now() / 1000);
  return payload.exp < currentTime;
};

// Extract user from token
const extractUserFromToken = (token: string): User | null => {
  const payload = parseJwtToken(token);
  if (!payload) return null;

  return {
    id: payload.sub,
    email: payload.email,
    userName: payload.userName,
    firstName: '',
    lastName: '',
    fullName: '',
    displayName: payload.userName,
    role: UserRole[payload.role as keyof typeof UserRole] || UserRole.User,
    isEmailVerified: true,
    isActive: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
};

// Auth store interface extending the basic AuthState
interface AuthStore extends AuthState {
  // Loading states for different operations
  loadingStates: AuthLoadingStates;

  // Actions
  login: (credentials: LoginRequest) => Promise<AuthResult>;
  logout: () => void;
  setUser: (user: User | null) => void;
  setTokens: (accessToken: string, refreshToken: string, expiresAt?: Date) => void;
  clearAuth: () => void;
  refreshAuthToken: () => Promise<boolean>;
  updateLoadingState: (operation: keyof AuthLoadingStates, loading: boolean) => void;
  setError: (error: string | null) => void;
  initialize: () => void;

  // Token management
  getAccessToken: () => string | null;
  getRefreshToken: () => string | null;
  isTokenValid: () => boolean;
  shouldRefreshToken: () => boolean;

  // User operations
  updateUser: (userData: Partial<User>) => void;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: UserRole[]) => boolean;
}

// Initial state
const initialState: AuthState & { loadingStates: AuthLoadingStates } = {
  user: null,
  accessToken: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: false,
  expiresAt: null,
  error: null,
  rememberMe: false,
  loadingStates: {
    login: false,
    register: false,
    logout: false,
    refresh: false,
    updateProfile: false,
    changeEmail: false,
    changePassword: false,
    resetPassword: false,
    verifyEmail: false,
    resendVerification: false,
  },
};

// Create the auth store with persistence
export const useAuthStore = create<AuthStore>()(
  persist(
    (set, get) => ({
      ...initialState,

      // Login action
      login: async (_credentials: LoginRequest): Promise<AuthResult> => {
        set((state) => ({
          ...state,
          loadingStates: { ...state.loadingStates, login: true },
          error: null,
        }));

        try {
          // This will be implemented in the API service layer
          // For now, return a placeholder
          return {
            success: false,
            errors: ['Login implementation pending'],
            tokenType: 'Bearer',
            requiresEmailVerification: false,
            requiresTwoFactor: false,
            isLockedOut: false,
          };
        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : 'Login failed';
          set((state) => ({ ...state, error: errorMessage }));
          return {
            success: false,
            errors: [errorMessage],
            tokenType: 'Bearer',
            requiresEmailVerification: false,
            requiresTwoFactor: false,
            isLockedOut: false,
          };
        } finally {
          set((state) => ({
            ...state,
            loadingStates: { ...state.loadingStates, login: false },
          }));
        }
      },

      // Logout action
      logout: () => {
        set((state) => ({
          ...state,
          loadingStates: { ...state.loadingStates, logout: true },
        }));

        // Clear local auth state
        set({
          ...initialState,
        });
      },

      // Set user
      setUser: (user: User | null) => {
        set((state) => ({
          ...state,
          user,
          isAuthenticated: !!user,
          error: null,
        }));
      },

      // Set tokens
      setTokens: (accessToken: string, refreshToken: string, expiresAt?: Date) => {
        const user = extractUserFromToken(accessToken);
        const tokenExpiry = expiresAt || new Date(Date.now() + 60 * 60 * 1000);

        set((state) => ({
          ...state,
          accessToken,
          refreshToken,
          expiresAt: tokenExpiry,
          user: user || state.user,
          isAuthenticated: true,
          error: null,
        }));
      },

      // Clear all authentication data
      clearAuth: () => {
        set({
          ...initialState,
        });
      },

      // Refresh token action
      refreshAuthToken: async (): Promise<boolean> => {
        const state = get();
        if (!state.refreshToken) {
          get().logout();
          return false;
        }

        set((currentState) => ({
          ...currentState,
          loadingStates: { ...currentState.loadingStates, refresh: true },
        }));

        try {
          // This will be implemented in the API service layer
          // For now, return false
          get().logout();
          return false;
        } catch (error) {
          console.error('Token refresh failed:', error);
          get().logout();
          return false;
        } finally {
          set((currentState) => ({
            ...currentState,
            loadingStates: { ...currentState.loadingStates, refresh: false },
          }));
        }
      },

      // Update loading state for specific operation
      updateLoadingState: (operation: keyof AuthLoadingStates, loading: boolean) => {
        set((state) => ({
          ...state,
          loadingStates: {
            ...state.loadingStates,
            [operation]: loading,
          },
        }));
      },

      // Set error message
      setError: (error: string | null) => {
        set((state) => ({
          ...state,
          error,
        }));
      },

      // Initialize auth state (check stored tokens)
      initialize: () => {
        const state = get();
        if (state.accessToken && !isTokenExpired(state.accessToken)) {
          const user = extractUserFromToken(state.accessToken);
          if (user) {
            set((currentState) => ({
              ...currentState,
              user,
              isAuthenticated: true,
              error: null,
            }));
          }
        } else {
          set({
            ...initialState,
          });
        }
      },

      // Get current access token
      getAccessToken: () => get().accessToken,

      // Get current refresh token
      getRefreshToken: () => get().refreshToken,

      // Check if current token is valid
      isTokenValid: () => {
        const token = get().accessToken;
        return token ? !isTokenExpired(token) : false;
      },

      // Check if token should be refreshed (within 5 minutes of expiry)
      shouldRefreshToken: () => {
        const token = get().accessToken;
        if (!token) return false;
        const payload = parseJwtToken(token);
        if (!payload) return false;
        const currentTime = Math.floor(Date.now() / 1000);
        const timeToExpiry = payload.exp - currentTime;
        return timeToExpiry < 300; // 5 minutes
      },

      // Update user information
      updateUser: (userData: Partial<User>) => {
        set((state) => ({
          ...state,
          user: state.user ? { ...state.user, ...userData } : null,
        }));
      },

      // Check if user has specific role
      hasRole: (role: UserRole) => {
        const user = get().user;
        return user ? user.role >= role : false;
      },

      // Check if user has any of the specified roles
      hasAnyRole: (roles: UserRole[]) => {
        const user = get().user;
        return user ? roles.includes(user.role) : false;
      },
    }),
    {
      name: 'maple-blog-auth', // localStorage key
      // Only persist essential data
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        expiresAt: state.expiresAt,
        rememberMe: state.rememberMe,
        user: state.user,
      }),
      // Use new storage API
      storage: createJSONStorage(() => ({
        getItem: (key) => {
          const item = localStorage.getItem(key);
          if (!item) return null;
          
          try {
            const parsed = JSON.parse(item);
            // Handle Date deserialization
            if (parsed?.state?.expiresAt) {
              parsed.state.expiresAt = new Date(parsed.state.expiresAt);
            }
            return item;
          } catch {
            return item;
          }
        },
        setItem: (key, value) => {
          try {
            const parsed = JSON.parse(value);
            // Handle Date serialization
            if (parsed?.state?.expiresAt instanceof Date) {
              parsed.state.expiresAt = parsed.state.expiresAt.toISOString();
              value = JSON.stringify(parsed);
            }
            localStorage.setItem(key, value);
          } catch {
            localStorage.setItem(key, value);
          }
        },
        removeItem: (key) => localStorage.removeItem(key),
      })),
      // Initialize on hydration
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.initialize();
        }
      },
    }
  )
);

// Auto-refresh token setup
let refreshInterval: NodeJS.Timeout | null = null;

// Setup automatic token refresh
export const setupTokenRefresh = () => {
  if (refreshInterval) {
    clearInterval(refreshInterval);
  }

  refreshInterval = setInterval(() => {
    const store = useAuthStore.getState();

    if (store.isAuthenticated && store.shouldRefreshToken()) {
      store.refreshAuthToken();
    }
  }, 60 * 1000); // Check every minute
};

// Clean up token refresh
export const cleanupTokenRefresh = () => {
  if (refreshInterval) {
    clearInterval(refreshInterval);
    refreshInterval = null;
  }
};

// Selectors for better performance
export const authSelectors = {
  user: (state: AuthStore) => state.user,
  isAuthenticated: (state: AuthStore) => state.isAuthenticated,
  isLoading: (state: AuthStore) => state.isLoading,
  error: (state: AuthStore) => state.error,
  accessToken: (state: AuthStore) => state.accessToken,
  loadingStates: (state: AuthStore) => state.loadingStates,

  // Computed selectors
  isAdmin: (state: AuthStore) => state.user?.role === UserRole.Admin,
  isAuthor: (state: AuthStore) => state.user?.role === UserRole.Author,
  canManagePosts: (state: AuthStore) =>
    state.user?.role === UserRole.Admin || state.user?.role === UserRole.Author,
  canManageUsers: (state: AuthStore) => state.user?.role === UserRole.Admin,
  userDisplayName: (state: AuthStore) =>
    state.user?.displayName || state.user?.fullName || state.user?.userName || 'User',
};

// Export type for external use
export type { AuthStore };

// Export alias for backward compatibility
export { useAuthStore as authStore };

export default useAuthStore;