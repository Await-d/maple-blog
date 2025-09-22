// @ts-nocheck
import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError, AxiosDefaults } from 'axios';
import type { ApiErrorResponse } from '../../types/home';
import { ENV_CONFIG, isDevelopment } from '../../types/env';

// API response data types
interface ApiResponseData {
  message?: string;
  errors?: Record<string, string[]> | string[];
  data?: any;
  success?: boolean;
}

// Enhanced Axios error with proper typing
interface TypedAxiosError extends AxiosError {
  response?: AxiosResponse<ApiResponseData>;
}

// API configuration
const API_CONFIG: AxiosRequestConfig = {
  baseURL: ENV_CONFIG.API_URL,
  timeout: ENV_CONFIG.API_TIMEOUT,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
};

/**
 * Enhanced axios instance with authentication and error handling
 */
class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create(API_CONFIG);
    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor for authentication
    this.client.interceptors.request.use(
      (config) => {
        // Add auth token if available
        const token = this.getAuthToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }

        // Add request ID for tracing
        config.headers['X-Request-ID'] = this.generateRequestId();

        // Add timestamp
        config.headers['X-Request-Time'] = new Date().toISOString();

        return config;
      },
      (error: AxiosError) => {
        console.error('Request interceptor error:', error);
        return Promise.reject(error);
      }
    );

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => {
        // Log successful responses in development
        if (isDevelopment()) {
          console.debug(`API Success: ${response.config.method?.toUpperCase()} ${response.config.url}`, {
            status: response.status,
            data: response.data,
          });
        }
        return response;
      },
      (error: TypedAxiosError) => {
        return this.handleResponseError(error);
      }
    );
  }

  private getAuthToken(): string | null {
    // Get token from localStorage or your auth store
    return localStorage.getItem('authToken');
  }

  private generateRequestId(): string {
    return `req_${Date.now()}_${Math.random().toString(36).substring(2)}`;
  }

  private handleResponseError(error: TypedAxiosError): Promise<never> {
    const { response, request, message } = error;

    // Network error (no response received)
    if (!response && request) {
      console.error('Network error:', error);
      const networkError: ApiErrorResponse = {
        success: false,
        message: 'Network error. Please check your connection and try again.',
        timestamp: new Date().toISOString(),
      };
      return Promise.reject(networkError);
    }

    // Request setup error
    if (!response && !request) {
      console.error('Request setup error:', error);
      const setupError: ApiErrorResponse = {
        success: false,
        message: 'Request configuration error. Please try again.',
        timestamp: new Date().toISOString(),
      };
      return Promise.reject(setupError);
    }

    // HTTP error response received
    if (response) {
      console.error(`API Error: ${response.status} ${response.statusText}`, {
        url: response.config.url,
        method: response.config.method,
        data: response.data,
      });

      // Handle specific status codes
      switch (response.status) {
        case 401:
          this.handleUnauthorized();
          break;
        case 403:
          console.warn('Access forbidden:', response.config.url);
          break;
        case 429:
          console.warn('Rate limit exceeded:', response.config.url);
          break;
        case 500:
          console.error('Server error:', response.config.url);
          break;
      }

      // Return structured error response
      const responseData = response.data as ApiResponseData | undefined;

      // Handle errors field type conversion
      let errors: Record<string, string[]> | undefined;
      if (responseData?.errors) {
        if (Array.isArray(responseData.errors)) {
          // Convert string array to record format
          errors = { general: responseData.errors };
        } else {
          // Already in record format
          errors = responseData.errors;
        }
      }

      const apiError: ApiErrorResponse = {
        success: false,
        message: responseData?.message || this.getStatusMessage(response.status),
        errors,
        timestamp: new Date().toISOString(),
      };
      return Promise.reject(apiError);
    }

    // Fallback error
    const fallbackError: ApiErrorResponse = {
      success: false,
      message: message || 'An unexpected error occurred',
      timestamp: new Date().toISOString(),
    };
    return Promise.reject(fallbackError);
  }

  private handleUnauthorized(): void {
    // Clear auth token
    localStorage.removeItem('authToken');

    // Redirect to login if not already there
    if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
      window.location.href = '/login?expired=true';
    }
  }

  private getStatusMessage(status: number): string {
    const statusMessages: Record<number, string> = {
      400: 'Bad request. Please check your input and try again.',
      401: 'Authentication required. Please log in.',
      403: 'Access denied. You do not have permission for this action.',
      404: 'Resource not found.',
      405: 'Method not allowed.',
      409: 'Conflict. The resource already exists or is in use.',
      422: 'Validation failed. Please check your input.',
      429: 'Too many requests. Please try again later.',
      500: 'Internal server error. Please try again later.',
      502: 'Bad gateway. Please try again later.',
      503: 'Service unavailable. Please try again later.',
      504: 'Gateway timeout. Please try again later.',
    };

    return statusMessages[status] || 'An unexpected error occurred.';
  }

  // HTTP methods with proper typing
  async get<T = any>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.client.get<T>(url, config);
  }

  async post<T = any, D = any>(url: string, data?: D, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.client.post<T>(url, data, config);
  }

  async put<T = any, D = any>(url: string, data?: D, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.client.put<T>(url, data, config);
  }

  async patch<T = any, D = any>(url: string, data?: D, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.client.patch<T>(url, data, config);
  }

  async delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.client.delete<T>(url, config);
  }

  // Upload file with progress tracking
  async uploadFile<T = any>(
    url: string,
    file: File,
    onProgress?: (progressEvent: { loaded: number; total?: number; progress?: number }) => void,
    config?: AxiosRequestConfig
  ): Promise<AxiosResponse<T>> {
    const formData = new FormData();
    formData.append('file', file);

    return this.client.post<T>(url, formData, {
      ...config,
      headers: {
        ...config?.headers,
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: onProgress,
    });
  }

  // Download file
  async downloadFile(
    url: string,
    filename?: string,
    config?: AxiosRequestConfig
  ): Promise<void> {
    const response = await this.client.get(url, {
      ...config,
      responseType: 'blob',
    });

    const blob = new Blob([response.data]);
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = filename || 'download';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
  }

  // Cancel request using AbortController (modern approach)
  createCancelToken(): AbortController {
    return new AbortController();
  }

  // Legacy cancel token support
  createLegacyCancelToken() {
    return axios.CancelToken.source();
  }

  // Check if request was cancelled
  isCancel(error: any): boolean {
    return axios.isCancel(error);
  }

  // Update base URL
  setBaseURL(baseURL: string): void {
    this.client.defaults.baseURL = baseURL;
  }

  // Update auth token
  setAuthToken(token: string): void {
    localStorage.setItem('authToken', token);
  }

  // Clear auth token
  clearAuthToken(): void {
    localStorage.removeItem('authToken');
  }

  // Get current config
  getConfig(): AxiosRequestConfig {
    const defaults = this.client.defaults as AxiosDefaults;
    return {
      baseURL: defaults.baseURL,
      timeout: defaults.timeout,
      headers: defaults.headers ? Object.assign({}, defaults.headers) : undefined,
      withCredentials: defaults.withCredentials,
      responseType: defaults.responseType,
      maxRedirects: defaults.maxRedirects,
      validateStatus: defaults.validateStatus,
    } as AxiosRequestConfig;
  }
}

// Create singleton instance
export const apiClient = new ApiClient();

// Export for testing or custom instances
export { ApiClient };

// Export types for use in other files
export type { AxiosRequestConfig, AxiosResponse, AxiosError };