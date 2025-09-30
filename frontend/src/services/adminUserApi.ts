/**
 * Admin User Management API Service
 * 
 * Production-ready enterprise-grade user management API service that provides
 * comprehensive CRUD operations, security features, and audit capabilities.
 */

import { apiClient } from './api/client';
import type { AxiosRequestConfig } from './api/client';

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

/**
 * Extended user interface for admin management with comprehensive fields
 */
export interface AdminUser {
  id: string;
  username: string;
  email: string;
  displayName: string;
  firstName: string;
  lastName: string;
  avatar?: string;
  status: 'Active' | 'Inactive' | 'Suspended' | 'Banned';
  roles: string[];
  primaryRole: 'Admin' | 'Author' | 'User';
  lastLogin?: string;
  registrationDate: string;
  postCount: number;
  commentCount: number;
  emailVerified: boolean;
  twoFactorEnabled: boolean;
  isOnline: boolean;
  lastActivity: string;
  profileCompletion: number;
  accountAge: string;
  loginCount: number;
  failedLoginAttempts: number;
  lockoutEnd?: string;
  ipAddress?: string;
  location?: string;
  createdBy?: string;
  notes?: string;
  // Security and audit fields
  securityAlerts?: SecurityAlert[];
  permissions?: UserPermission[];
  sessionCount: number;
  lastPasswordChange?: string;
  phoneNumber?: string;
  countryCode?: string;
  timezone?: string;
  languagePreference?: string;
  dataProcessingConsent: boolean;
  marketingConsent: boolean;
  gdprCompliant: boolean;
}

/**
 * Security alert interface for tracking suspicious activities
 */
export interface SecurityAlert {
  id: string;
  userId: string;
  alertType: 'suspicious_login' | 'password_breach' | 'unusual_activity' | 'failed_login_attempts';
  severity: 'low' | 'medium' | 'high' | 'critical';
  description: string;
  details: Record<string, unknown>;
  resolved: boolean;
  resolvedBy?: string;
  resolvedAt?: string;
  createdAt: string;
}

/**
 * User permission interface for granular access control
 */
export interface UserPermission {
  id: string;
  name: string;
  description: string;
  module: string;
  actions: string[];
  grantedBy: string;
  grantedAt: string;
  expiresAt?: string;
}

/**
 * User creation request interface
 */
export interface CreateUserRequest {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  primaryRole: 'Admin' | 'Author' | 'User';
  roles?: string[];
  emailVerified?: boolean;
  sendWelcomeEmail?: boolean;
  temporaryPassword?: boolean;
  notes?: string;
  permissions?: string[];
  phoneNumber?: string;
  timezone?: string;
  languagePreference?: string;
}

/**
 * User update request interface
 */
export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  displayName?: string;
  email?: string;
  primaryRole?: 'Admin' | 'Author' | 'User';
  roles?: string[];
  status?: 'Active' | 'Inactive' | 'Suspended' | 'Banned';
  emailVerified?: boolean;
  twoFactorEnabled?: boolean;
  notes?: string;
  permissions?: string[];
  phoneNumber?: string;
  timezone?: string;
  languagePreference?: string;
  dataProcessingConsent?: boolean;
  marketingConsent?: boolean;
}

/**
 * User filters for advanced searching and filtering
 */
export interface UserFilters {
  search?: string;
  role?: string;
  status?: string;
  emailVerified?: boolean;
  twoFactorEnabled?: boolean;
  isOnline?: boolean;
  registrationDateFrom?: string;
  registrationDateTo?: string;
  lastLoginFrom?: string;
  lastLoginTo?: string;
  permissions?: string[];
  location?: string;
  hasSecurityAlerts?: boolean;
  gdprCompliant?: boolean;
}

/**
 * Sort configuration for user lists
 */
export interface UserSortConfig {
  field: keyof AdminUser;
  direction: 'asc' | 'desc';
}

/**
 * Pagination configuration
 */
export interface PaginationConfig {
  page: number;
  pageSize: number;
  total?: number;
  totalPages?: number;
}

/**
 * Paginated response wrapper
 */
export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
  filters?: Record<string, unknown>;
  sort?: UserSortConfig;
  timestamp: string;
}

/**
 * Bulk operation types for user management
 */
export type BulkOperation = 
  | 'delete' 
  | 'activate' 
  | 'deactivate' 
  | 'suspend' 
  | 'unsuspend' 
  | 'verify-email' 
  | 'reset-password'
  | 'revoke-sessions'
  | 'enable-2fa'
  | 'disable-2fa'
  | 'grant-permission'
  | 'revoke-permission'
  | 'export-data'
  | 'anonymize-data';

/**
 * Bulk operation request
 */
export interface BulkOperationRequest {
  userIds: string[];
  operation: BulkOperation;
  parameters?: Record<string, unknown>;
  reason?: string;
  notifyUsers?: boolean;
}

/**
 * Bulk operation result
 */
export interface BulkOperationResult {
  operation: BulkOperation;
  totalRequested: number;
  successCount: number;
  failureCount: number;
  results: Array<{
    userId: string;
    success: boolean;
    error?: string;
  }>;
  executedBy: string;
  executedAt: string;
  auditLogId: string;
}

/**
 * User activity log entry
 */
export interface UserActivityLog {
  id: string;
  userId: string;
  action: string;
  details: Record<string, unknown>;
  ipAddress: string;
  userAgent: string;
  performedBy?: string;
  timestamp: string;
  sessionId?: string;
}

/**
 * Password reset request
 */
export interface PasswordResetRequest {
  userId: string;
  temporaryPassword?: boolean;
  requireChangeOnLogin?: boolean;
  sendEmail?: boolean;
  expiryHours?: number;
}

/**
 * User statistics interface
 */
export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  suspendedUsers: number;
  bannedUsers: number;
  onlineUsers: number;
  verifiedUsers: number;
  unverifiedUsers: number;
  twoFactorEnabledUsers: number;
  usersWithSecurityAlerts: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  newUsersThisMonth: number;
  averageSessionDuration: number;
  mostActiveUsers: Array<{ userId: string; username: string; activityCount: number }>;
  roleDistribution: Record<string, number>;
  locationDistribution: Array<{ location: string; count: number }>;
  calculatedAt: string;
}

// ============================================================================
// ERROR HANDLING TYPES
// ============================================================================

export interface UserApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
  field?: string;
}

export interface ValidationErrors {
  [field: string]: string[];
}

// ============================================================================
// API SERVICE CLASS
// ============================================================================

/**
 * Admin User Management API Service
 * 
 * Provides comprehensive user management functionality with enterprise-grade
 * security, audit logging, and compliance features.
 */
export class AdminUserApiService {
  private readonly baseUrl = '/api/admin/users';

  // ========================================================================
  // USER CRUD OPERATIONS
  // ========================================================================

  /**
   * Get paginated list of users with advanced filtering and sorting
   */
  async getUsers(
    filters?: UserFilters,
    sort?: UserSortConfig,
    pagination?: PaginationConfig,
    config?: AxiosRequestConfig
  ): Promise<PaginatedResponse<AdminUser>> {
    const params = new URLSearchParams();

    // Add pagination parameters
    if (pagination) {
      params.append('page', pagination.page.toString());
      params.append('pageSize', pagination.pageSize.toString());
    }

    // Add sort parameters
    if (sort) {
      params.append('sortBy', sort.field);
      params.append('sortOrder', sort.direction);
    }

    // Add filter parameters
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          if (Array.isArray(value)) {
            value.forEach(item => params.append(`${key}[]`, item.toString()));
          } else {
            params.append(key, value.toString());
          }
        }
      });
    }

    const response = await apiClient.get<PaginatedResponse<AdminUser>>(
      `${this.baseUrl}?${params.toString()}`,
      config
    );

    return response.data;
  }

  /**
   * Get detailed information about a specific user
   */
  async getUserById(
    userId: string,
    includeActivityLog = false,
    config?: AxiosRequestConfig
  ): Promise<AdminUser> {
    const params = new URLSearchParams();
    if (includeActivityLog) {
      params.append('includeActivityLog', 'true');
    }

    const response = await apiClient.get<AdminUser>(
      `${this.baseUrl}/${userId}?${params.toString()}`,
      config
    );

    return response.data;
  }

  /**
   * Create a new user with comprehensive validation
   */
  async createUser(
    userData: CreateUserRequest,
    config?: AxiosRequestConfig
  ): Promise<AdminUser> {
    const response = await apiClient.post<AdminUser>(
      this.baseUrl,
      userData,
      config
    );

    return response.data;
  }

  /**
   * Update an existing user with optimistic locking
   */
  async updateUser(
    userId: string,
    userData: UpdateUserRequest,
    version?: number,
    config?: AxiosRequestConfig
  ): Promise<AdminUser> {
    const payload = version ? { ...userData, version } : userData;

    const response = await apiClient.put<AdminUser>(
      `${this.baseUrl}/${userId}`,
      payload,
      config
    );

    return response.data;
  }

  /**
   * Delete a user with optional data anonymization
   */
  async deleteUser(
    userId: string,
    options?: {
      anonymizeData?: boolean;
      transferContent?: boolean;
      transferToUserId?: string;
      reason?: string;
    },
    config?: AxiosRequestConfig
  ): Promise<{ success: boolean; message: string; auditLogId: string }> {
    const response = await apiClient.delete<{ success: boolean; message: string; auditLogId: string }>(
      `${this.baseUrl}/${userId}`,
      {
        ...config,
        data: options,
      }
    );

    return response.data;
  }

  // ========================================================================
  // BULK OPERATIONS
  // ========================================================================

  /**
   * Perform bulk operations on multiple users
   */
  async performBulkOperation(
    request: BulkOperationRequest,
    config?: AxiosRequestConfig
  ): Promise<BulkOperationResult> {
    const response = await apiClient.post<BulkOperationResult>(
      `${this.baseUrl}/bulk`,
      request,
      config
    );

    return response.data;
  }

  /**
   * Get status of a bulk operation
   */
  async getBulkOperationStatus(
    operationId: string,
    config?: AxiosRequestConfig
  ): Promise<BulkOperationResult> {
    const response = await apiClient.get<BulkOperationResult>(
      `${this.baseUrl}/bulk/${operationId}`,
      config
    );

    return response.data;
  }

  // ========================================================================
  // SECURITY OPERATIONS
  // ========================================================================

  /**
   * Reset user password with security validation
   */
  async resetUserPassword(
    userId: string,
    request: PasswordResetRequest,
    config?: AxiosRequestConfig
  ): Promise<{ success: boolean; temporaryPassword?: string; expiresAt?: string }> {
    const response = await apiClient.post<{ success: boolean; temporaryPassword?: string; expiresAt?: string }>(
      `${this.baseUrl}/${userId}/reset-password`,
      request,
      config
    );

    return response.data;
  }

  /**
   * Force logout user from all sessions
   */
  async revokeUserSessions(
    userId: string,
    reason?: string,
    config?: AxiosRequestConfig
  ): Promise<{ success: boolean; revokedSessions: number }> {
    const response = await apiClient.post<{ success: boolean; revokedSessions: number }>(
      `${this.baseUrl}/${userId}/revoke-sessions`,
      { reason },
      config
    );

    return response.data;
  }

  /**
   * Get user's security alerts
   */
  async getUserSecurityAlerts(
    userId: string,
    includeResolved = false,
    config?: AxiosRequestConfig
  ): Promise<SecurityAlert[]> {
    const params = new URLSearchParams();
    if (includeResolved) {
      params.append('includeResolved', 'true');
    }

    const response = await apiClient.get<SecurityAlert[]>(
      `${this.baseUrl}/${userId}/security-alerts?${params.toString()}`,
      config
    );

    return response.data;
  }

  /**
   * Resolve a security alert
   */
  async resolveSecurityAlert(
    userId: string,
    alertId: string,
    resolution: string,
    config?: AxiosRequestConfig
  ): Promise<SecurityAlert> {
    const response = await apiClient.patch<SecurityAlert>(
      `${this.baseUrl}/${userId}/security-alerts/${alertId}`,
      { resolved: true, resolution },
      config
    );

    return response.data;
  }

  // ========================================================================
  // PERMISSIONS MANAGEMENT
  // ========================================================================

  /**
   * Get user's permissions
   */
  async getUserPermissions(
    userId: string,
    config?: AxiosRequestConfig
  ): Promise<UserPermission[]> {
    const response = await apiClient.get<UserPermission[]>(
      `${this.baseUrl}/${userId}/permissions`,
      config
    );

    return response.data;
  }

  /**
   * Grant permissions to user
   */
  async grantUserPermissions(
    userId: string,
    permissionIds: string[],
    expiresAt?: string,
    config?: AxiosRequestConfig
  ): Promise<UserPermission[]> {
    const response = await apiClient.post<UserPermission[]>(
      `${this.baseUrl}/${userId}/permissions`,
      { permissionIds, expiresAt },
      config
    );

    return response.data;
  }

  /**
   * Revoke permissions from user
   */
  async revokeUserPermissions(
    userId: string,
    permissionIds: string[],
    reason?: string,
    config?: AxiosRequestConfig
  ): Promise<{ success: boolean; revokedCount: number }> {
    const response = await apiClient.delete<{ success: boolean; revokedCount: number }>(
      `${this.baseUrl}/${userId}/permissions`,
      {
        ...config,
        data: { permissionIds, reason },
      }
    );

    return response.data;
  }

  // ========================================================================
  // ACTIVITY AND AUDIT
  // ========================================================================

  /**
   * Get user activity log with pagination
   */
  async getUserActivityLog(
    userId: string,
    pagination?: PaginationConfig,
    filters?: { action?: string; dateFrom?: string; dateTo?: string },
    config?: AxiosRequestConfig
  ): Promise<PaginatedResponse<UserActivityLog>> {
    const params = new URLSearchParams();

    if (pagination) {
      params.append('page', pagination.page.toString());
      params.append('pageSize', pagination.pageSize.toString());
    }

    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value) params.append(key, value);
      });
    }

    const response = await apiClient.get<PaginatedResponse<UserActivityLog>>(
      `${this.baseUrl}/${userId}/activity?${params.toString()}`,
      config
    );

    return response.data;
  }

  /**
   * Export user data for GDPR compliance
   */
  async exportUserData(
    userId: string,
    includeActivityLog = true,
    format: 'json' | 'csv' = 'json',
    config?: AxiosRequestConfig
  ): Promise<{ downloadUrl: string; expiresAt: string }> {
    const response = await apiClient.post<{ downloadUrl: string; expiresAt: string }>(
      `${this.baseUrl}/${userId}/export`,
      { includeActivityLog, format },
      config
    );

    return response.data;
  }

  // ========================================================================
  // STATISTICS AND ANALYTICS
  // ========================================================================

  /**
   * Get user management statistics
   */
  async getUserStatistics(
    dateRange?: { from: string; to: string },
    config?: AxiosRequestConfig
  ): Promise<UserStatistics> {
    const params = new URLSearchParams();
    if (dateRange) {
      params.append('from', dateRange.from);
      params.append('to', dateRange.to);
    }

    const response = await apiClient.get<UserStatistics>(
      `${this.baseUrl}/statistics?${params.toString()}`,
      config
    );

    return response.data;
  }

  // ========================================================================
  // SEARCH AND AUTOCOMPLETE
  // ========================================================================

  /**
   * Search users with advanced query capabilities
   */
  async searchUsers(
    query: string,
    filters?: UserFilters,
    limit = 20,
    config?: AxiosRequestConfig
  ): Promise<AdminUser[]> {
    const params = new URLSearchParams();
    params.append('q', query);
    params.append('limit', limit.toString());

    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          params.append(key, value.toString());
        }
      });
    }

    const response = await apiClient.get<AdminUser[]>(
      `${this.baseUrl}/search?${params.toString()}`,
      config
    );

    return response.data;
  }

  /**
   * Get user suggestions for autocomplete
   */
  async getUserSuggestions(
    query: string,
    limit = 10,
    config?: AxiosRequestConfig
  ): Promise<Array<{ id: string; username: string; displayName: string; avatar?: string }>> {
    const params = new URLSearchParams();
    params.append('q', query);
    params.append('limit', limit.toString());

    const response = await apiClient.get<Array<{ id: string; username: string; displayName: string; avatar?: string }>>(
      `${this.baseUrl}/suggestions?${params.toString()}`,
      config
    );

    return response.data;
  }

  // ========================================================================
  // FILE OPERATIONS
  // ========================================================================

  /**
   * Upload user avatar with validation
   */
  async uploadUserAvatar(
    userId: string,
    file: File,
    onProgress?: (progress: number) => void,
    config?: AxiosRequestConfig
  ): Promise<{ avatarUrl: string; thumbnailUrl: string }> {
    const formData = new FormData();
    formData.append('avatar', file);

    const response = await apiClient.post<{ avatarUrl: string; thumbnailUrl: string }>(
      `${this.baseUrl}/${userId}/avatar`,
      formData,
      {
        ...config,
        headers: {
          ...config?.headers,
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          if (onProgress && progressEvent.total) {
            const progress = Math.round((progressEvent.loaded / progressEvent.total) * 100);
            onProgress(progress);
          }
        },
      }
    );

    return response.data;
  }

  /**
   * Import users from CSV file
   */
  async importUsers(
    file: File,
    options?: {
      skipDuplicates?: boolean;
      sendWelcomeEmails?: boolean;
      defaultRole?: string;
    },
    onProgress?: (progress: number) => void,
    config?: AxiosRequestConfig
  ): Promise<{ 
    jobId: string; 
    totalRows: number; 
    estimatedDuration: number;
  }> {
    const formData = new FormData();
    formData.append('file', file);
    if (options) {
      formData.append('options', JSON.stringify(options));
    }

    const response = await apiClient.post<{ 
      jobId: string; 
      totalRows: number; 
      estimatedDuration: number;
    }>(
      `${this.baseUrl}/import`,
      formData,
      {
        ...config,
        headers: {
          ...config?.headers,
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (progressEvent) => {
          if (onProgress && progressEvent.total) {
            const progress = Math.round((progressEvent.loaded / progressEvent.total) * 100);
            onProgress(progress);
          }
        },
      }
    );

    return response.data;
  }

  /**
   * Get import job status
   */
  async getImportJobStatus(
    jobId: string,
    config?: AxiosRequestConfig
  ): Promise<{
    jobId: string;
    status: 'pending' | 'processing' | 'completed' | 'failed';
    progress: number;
    processedRows: number;
    totalRows: number;
    successCount: number;
    errorCount: number;
    errors?: Array<{ row: number; field: string; message: string }>;
    completedAt?: string;
  }> {
    const response = await apiClient.get<{
      jobId: string;
      status: 'pending' | 'processing' | 'completed' | 'failed';
      progress: number;
      processedRows: number;
      totalRows: number;
      successCount: number;
      errorCount: number;
      errors?: Array<{ row: number; field: string; message: string }>;
      completedAt?: string;
    }>(`${this.baseUrl}/import/${jobId}`, config);

    return response.data;
  }

  // ========================================================================
  // UTILITY METHODS
  // ========================================================================

  /**
   * Validate email address availability
   */
  async checkEmailAvailability(
    email: string,
    excludeUserId?: string,
    config?: AxiosRequestConfig
  ): Promise<{ available: boolean; suggestions?: string[] }> {
    const params = new URLSearchParams();
    params.append('email', email);
    if (excludeUserId) {
      params.append('excludeUserId', excludeUserId);
    }

    const response = await apiClient.get<{ available: boolean; suggestions?: string[] }>(
      `${this.baseUrl}/check-email?${params.toString()}`,
      config
    );

    return response.data;
  }

  /**
   * Validate username availability
   */
  async checkUsernameAvailability(
    username: string,
    excludeUserId?: string,
    config?: AxiosRequestConfig
  ): Promise<{ available: boolean; suggestions?: string[] }> {
    const params = new URLSearchParams();
    params.append('username', username);
    if (excludeUserId) {
      params.append('excludeUserId', excludeUserId);
    }

    const response = await apiClient.get<{ available: boolean; suggestions?: string[] }>(
      `${this.baseUrl}/check-username?${params.toString()}`,
      config
    );

    return response.data;
  }
}

// ============================================================================
// SINGLETON INSTANCE
// ============================================================================

/**
 * Singleton instance of the admin user API service
 */
export const adminUserApi = new AdminUserApiService();

// ============================================================================
// EXPORT TYPES FOR EXTERNAL USE
// ============================================================================

// Types are already exported as interfaces above