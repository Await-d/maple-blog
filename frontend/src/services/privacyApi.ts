/**
 * Privacy API Service
 * Comprehensive privacy settings and data protection management
 */

import { apiClient } from './api/client';
import type {
  PrivacySettings,
  PrivacyAuditLog,
  PrivacySettingsResponse,
  PrivacyAuditResponse,
  UpdatePrivacySettingsRequest,
  DataExportRequest,
  DataExportResponse,
  AccountDeletionRequest
} from '../types/privacy';

/**
 * Privacy API service
 */
export const privacyApi = {
  /**
   * Get current user's privacy settings
   */
  async getSettings(): Promise<PrivacySettings> {
    const response = await apiClient.get<PrivacySettingsResponse>('/users/me/privacy/settings');
    return response.data.settings;
  },

  /**
   * Update privacy settings
   */
  async updateSettings(settings: Partial<PrivacySettings>, reason?: string): Promise<PrivacySettings> {
    const response = await apiClient.put<PrivacySettingsResponse>(
      '/users/me/privacy/settings',
      { settings, reason } as UpdatePrivacySettingsRequest
    );
    return response.data.settings;
  },

  /**
   * Reset privacy settings to defaults
   */
  async resetToDefaults(reason?: string): Promise<PrivacySettings> {
    const response = await apiClient.post<PrivacySettingsResponse>('/users/me/privacy/settings/reset', { reason });
    return response.data.settings;
  },

  /**
   * Get privacy audit logs
   */
  async getAuditLogs(params?: {
    page?: number;
    pageSize?: number;
    action?: string;
    section?: string;
    startDate?: string;
    endDate?: string;
  }): Promise<{
    logs: PrivacyAuditLog[];
    totalCount: number;
    pagination: {
      page: number;
      pageSize: number;
      totalPages: number;
    };
  }> {
    const response = await apiClient.get<PrivacyAuditResponse>('/users/me/privacy/audit', { params });
    return {
      logs: response.data.logs,
      totalCount: response.data.totalCount,
      pagination: response.data.pagination
    };
  },

  /**
   * Request data export
   */
  async requestDataExport(request: DataExportRequest): Promise<{ exportId: string }> {
    const response = await apiClient.post<{ exportId: string }>('/users/me/privacy/data-export', request);
    return response.data;
  },

  /**
   * Get data export status
   */
  async getDataExportStatus(exportId: string): Promise<DataExportResponse> {
    const response = await apiClient.get<DataExportResponse>(`/users/me/privacy/data-export/${exportId}`);
    return response.data;
  },

  /**
   * Download data export
   */
  async downloadDataExport(exportId: string): Promise<Blob> {
    const response = await apiClient.get(`/users/me/privacy/data-export/${exportId}/download`, {
      responseType: 'blob'
    });
    return response.data as Blob;
  },

  /**
   * Delete data export
   */
  async deleteDataExport(exportId: string): Promise<void> {
    await apiClient.delete(`/users/me/privacy/data-export/${exportId}`);
  },

  /**
   * Get active data exports
   */
  async getActiveDataExports(): Promise<DataExportResponse[]> {
    const response = await apiClient.get<{ exports: DataExportResponse[] }>('/users/me/privacy/data-exports');
    return response.data.exports;
  },

  /**
   * Request account deletion
   */
  async requestAccountDeletion(request: AccountDeletionRequest): Promise<{ deletionId: string; scheduledDate?: string }> {
    const response = await apiClient.post<{ deletionId: string; scheduledDate?: string }>('/users/me/account/delete', request);
    return response.data;
  },

  /**
   * Cancel account deletion
   */
  async cancelAccountDeletion(deletionId: string, password: string): Promise<void> {
    await apiClient.post(`/users/me/account/delete/${deletionId}/cancel`, { password });
  },

  /**
   * Get account deletion status
   */
  async getAccountDeletionStatus(): Promise<{
    isDeletionScheduled: boolean;
    deletionId?: string;
    scheduledDate?: string;
    method?: string;
    canCancel: boolean;
  }> {
    const response = await apiClient.get('/users/me/account/delete/status');
    return response.data as {
      isDeletionScheduled: boolean;
      deletionId?: string;
      scheduledDate?: string;
      method?: string;
      canCancel: boolean;
    };
  },

  /**
   * Withdraw GDPR consent
   */
  async withdrawGdprConsent(reason: string): Promise<void> {
    await apiClient.post('/users/me/privacy/gdpr/withdraw-consent', { reason });
  },

  /**
   * Give GDPR consent
   */
  async giveGdprConsent(version: string): Promise<void> {
    await apiClient.post('/users/me/privacy/gdpr/give-consent', { version });
  },

  /**
   * Get current active sessions
   */
  async getActiveSessions(): Promise<Array<{
    id: string;
    deviceName: string;
    deviceType: string;
    browser: string;
    os: string;
    ipAddress: string;
    location: string;
    isCurrent: boolean;
    lastActivity: string;
    createdAt: string;
  }>> {
    interface SessionsResponse {
      sessions: Array<{
        id: string;
        deviceName: string;
        deviceType: string;
        browser: string;
        os: string;
        ipAddress: string;
        location: string;
        isCurrent: boolean;
        lastActivity: string;
        createdAt: string;
      }>;
    }
    const response = await apiClient.get<SessionsResponse>('/users/me/sessions');
    return response.data.sessions;
  },

  /**
   * Revoke session
   */
  async revokeSession(sessionId: string): Promise<void> {
    await apiClient.delete(`/users/me/sessions/${sessionId}`);
  },

  /**
   * Revoke all sessions except current
   */
  async revokeAllSessions(): Promise<void> {
    await apiClient.post('/users/me/sessions/revoke-all');
  },

  /**
   * Get connected third-party applications
   */
  async getConnectedApps(): Promise<Array<{
    id: string;
    name: string;
    description: string;
    website: string;
    permissions: string[];
    connectedAt: string;
    lastUsed: string;
    isActive: boolean;
  }>> {
    interface ConnectedAppsResponse {
      apps: Array<{
        id: string;
        name: string;
        description: string;
        website: string;
        permissions: string[];
        connectedAt: string;
        lastUsed: string;
        isActive: boolean;
      }>;
    }
    const response = await apiClient.get<ConnectedAppsResponse>('/users/me/connected-apps');
    return response.data.apps;
  },

  /**
   * Revoke third-party application access
   */
  async revokeAppAccess(appId: string): Promise<void> {
    await apiClient.delete(`/users/me/connected-apps/${appId}`);
  },

  /**
   * Get data processing activities
   */
  async getDataProcessingActivities(): Promise<Array<{
    activity: string;
    purpose: string;
    dataTypes: string[];
    legalBasis: string;
    retentionPeriod: string;
    thirdParties: string[];
    canOptOut: boolean;
    isActive: boolean;
  }>> {
    interface DataProcessingResponse {
      activities: Array<{
        activity: string;
        purpose: string;
        dataTypes: string[];
        legalBasis: string;
        retentionPeriod: string;
        thirdParties: string[];
        canOptOut: boolean;
        isActive: boolean;
      }>;
    }
    const response = await apiClient.get<DataProcessingResponse>('/users/me/privacy/data-processing');
    return response.data.activities;
  },

  /**
   * Opt out of data processing activity
   */
  async optOutOfProcessing(activityId: string, reason?: string): Promise<void> {
    await apiClient.post(`/users/me/privacy/data-processing/${activityId}/opt-out`, { reason });
  },

  /**
   * Opt into data processing activity
   */
  async optIntoProcessing(activityId: string): Promise<void> {
    await apiClient.post(`/users/me/privacy/data-processing/${activityId}/opt-in`);
  },

  /**
   * Clear browsing history
   */
  async clearBrowsingHistory(olderThanDays?: number): Promise<void> {
    await apiClient.post('/users/me/privacy/clear-history', { olderThanDays });
  },

  /**
   * Clear search history
   */
  async clearSearchHistory(olderThanDays?: number): Promise<void> {
    await apiClient.post('/users/me/privacy/clear-search-history', { olderThanDays });
  },

  /**
   * Clear interaction data
   */
  async clearInteractionData(types: string[], olderThanDays?: number): Promise<void> {
    await apiClient.post('/users/me/privacy/clear-interactions', { types, olderThanDays });
  },

  /**
   * Update GDPR settings
   */
  async updateGdprSettings(settings: {
    lawfulBasis: string;
    dataProcessingAgreement: boolean;
  }): Promise<void> {
    await apiClient.put('/users/me/privacy/gdpr/settings', settings);
  },

  /**
   * Get privacy policy version
   */
  async getPrivacyPolicyVersion(): Promise<{ version: string; updatedAt: string }> {
    const response = await apiClient.get('/privacy-policy/version');
    return response.data as { version: string; updatedAt: string };
  },

  /**
   * Acknowledge privacy policy
   */
  async acknowledgePrivacyPolicy(version: string): Promise<void> {
    await apiClient.post('/users/me/privacy/acknowledge-policy', { version });
  },

  /**
   * Get blocked users list
   */
  async getBlockedUsers(): Promise<Array<{
    id: string;
    username: string;
    displayName: string;
    avatar?: string;
    blockedAt: string;
    reason?: string;
  }>> {
    interface BlockedUsersResponse {
      users: Array<{
        id: string;
        username: string;
        displayName: string;
        avatar?: string;
        blockedAt: string;
        reason?: string;
      }>;
    }
    const response = await apiClient.get<BlockedUsersResponse>('/users/me/blocked-users');
    return response.data.users;
  },

  /**
   * Block user
   */
  async blockUser(userId: string, reason?: string): Promise<void> {
    await apiClient.post(`/users/${userId}/block`, { reason });
  },

  /**
   * Unblock user
   */
  async unblockUser(userId: string): Promise<void> {
    await apiClient.delete(`/users/${userId}/block`);
  },

  /**
   * Get reported content
   */
  async getReportedContent(): Promise<Array<{
    id: string;
    type: 'post' | 'comment' | 'user';
    contentId: string;
    reason: string;
    reportedAt: string;
    status: 'pending' | 'reviewed' | 'resolved';
  }>> {
    interface ReportedContentResponse {
      reports: Array<{
        id: string;
        type: 'post' | 'comment' | 'user';
        contentId: string;
        reason: string;
        reportedAt: string;
        status: 'pending' | 'reviewed' | 'resolved';
      }>;
    }
    const response = await apiClient.get<ReportedContentResponse>('/users/me/reports');
    return response.data.reports;
  },

  /**
   * Report content
   */
  async reportContent(contentType: 'post' | 'comment' | 'user', contentId: string, reason: string, details?: string): Promise<void> {
    await apiClient.post('/reports', {
      contentType,
      contentId,
      reason,
      details
    });
  }
};

/**
 * Privacy utilities
 */
export const privacyUtils = {
  /**
   * Format file size for display
   */
  formatFileSize(bytes: number): string {
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    if (bytes === 0) return '0 Bytes';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
  },

  /**
   * Validate password strength
   */
  validatePassword(password: string): {
    isValid: boolean;
    strength: 'weak' | 'medium' | 'strong';
    issues: string[];
  } {
    const issues: string[] = [];
    let score = 0;

    if (password.length < 8) {
      issues.push('At least 8 characters required');
    } else {
      score += 1;
    }

    if (!/[a-z]/.test(password)) {
      issues.push('At least one lowercase letter required');
    } else {
      score += 1;
    }

    if (!/[A-Z]/.test(password)) {
      issues.push('At least one uppercase letter required');
    } else {
      score += 1;
    }

    if (!/\d/.test(password)) {
      issues.push('At least one number required');
    } else {
      score += 1;
    }

    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
      issues.push('At least one special character required');
    } else {
      score += 1;
    }

    let strength: 'weak' | 'medium' | 'strong';
    if (score < 3) {
      strength = 'weak';
    } else if (score < 5) {
      strength = 'medium';
    } else {
      strength = 'strong';
    }

    return {
      isValid: issues.length === 0,
      strength,
      issues
    };
  },

  /**
   * Generate secure backup codes
   */
  generateBackupCodes(count: number = 10): string[] {
    const codes: string[] = [];
    const charset = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ';

    for (let i = 0; i < count; i++) {
      let code = '';
      for (let j = 0; j < 8; j++) {
        if (j === 4) code += '-'; // Add separator
        code += charset.charAt(Math.floor(Math.random() * charset.length));
      }
      codes.push(code);
    }

    return codes;
  },

  /**
   * Mask sensitive information
   */
  maskEmail(email: string): string {
    const [local, domain] = email.split('@');
    if (local.length <= 2) return email;

    const masked = local.charAt(0) + '*'.repeat(local.length - 2) + local.charAt(local.length - 1);
    return `${masked}@${domain}`;
  },

  /**
   * Mask phone number
   */
  maskPhone(phone: string): string {
    if (phone.length <= 4) return phone;
    const cleaned = phone.replace(/\D/g, '');
    const masked = '*'.repeat(cleaned.length - 4) + cleaned.slice(-4);
    return masked;
  },

  /**
   * Calculate privacy score based on settings
   */
  calculatePrivacyScore(settings: PrivacySettings): {
    score: number;
    maxScore: number;
    percentage: number;
    recommendations: string[];
  } {
    let score = 0;
    const maxScore = 20;
    const recommendations: string[] = [];

    // Profile privacy (4 points)
    if (settings.profile.visibility !== 'public') score += 1;
    else recommendations.push('Consider making your profile less public');

    if (!settings.profile.showInSearch) score += 1;
    else recommendations.push('Consider hiding your profile from search results');

    if (!settings.profile.showActivity) score += 1;
    else recommendations.push('Consider hiding your activity from others');

    if (!settings.profile.contactInfo.showEmail) score += 1;
    else recommendations.push('Consider hiding your email address');

    // Content sharing (4 points)
    if (settings.contentSharing.whoCanComment !== 'public') score += 1;
    else recommendations.push('Consider restricting who can comment on your posts');

    if (settings.contentSharing.requireCommentApproval) score += 1;
    else recommendations.push('Consider requiring approval for comments');

    if (!settings.contentSharing.allowAnonymousInteractions) score += 1;
    else recommendations.push('Consider disabling anonymous interactions');

    if (!settings.contentSharing.allowContentEmbedding) score += 1;
    else recommendations.push('Consider disabling content embedding');

    // Data collection (4 points)
    if (settings.dataCollection.collectionLevel === 'minimal') score += 2;
    else if (settings.dataCollection.collectionLevel === 'essential') score += 1;
    else recommendations.push('Consider reducing data collection level');

    if (!settings.dataCollection.marketing.enabled) score += 1;
    else recommendations.push('Consider disabling marketing data collection');

    if (!settings.dataCollection.personalization.shareDataWithPartners) score += 1;
    else recommendations.push('Consider not sharing data with partners');

    // Security (4 points)
    if (settings.security.twoFactorAuth.enabled) score += 2;
    else recommendations.push('Enable two-factor authentication for better security');

    if (settings.security.loginSecurity.enableLoginNotifications) score += 1;
    else recommendations.push('Enable login notifications');

    if (!settings.security.apiAccess.allowThirdPartyApps) score += 1;
    else recommendations.push('Consider restricting third-party app access');

    // Communication (4 points)
    if (settings.communication.whoCanContact !== 'public') score += 1;
    else recommendations.push('Consider restricting who can contact you');

    if (settings.communication.messageFiltering.enabled) score += 1;
    else recommendations.push('Enable message filtering');

    if (settings.communication.limits.maxMessagesPerDay < 50) score += 1;
    else recommendations.push('Consider limiting daily messages');

    if (settings.gdprCompliance.enabled) score += 1;
    else recommendations.push('Consider enabling GDPR compliance features');

    const percentage = Math.round((score / maxScore) * 100);

    return {
      score,
      maxScore,
      percentage,
      recommendations: recommendations.slice(0, 5) // Limit to top 5 recommendations
    };
  },

  /**
   * Get privacy level description
   */
  getPrivacyLevelDescription(percentage: number): {
    level: 'low' | 'medium' | 'high' | 'maximum';
    description: string;
    color: string;
  } {
    if (percentage >= 80) {
      return {
        level: 'maximum',
        description: 'Your privacy settings are configured for maximum protection.',
        color: 'green'
      };
    } else if (percentage >= 60) {
      return {
        level: 'high',
        description: 'Your privacy settings provide good protection with room for improvement.',
        color: 'blue'
      };
    } else if (percentage >= 40) {
      return {
        level: 'medium',
        description: 'Your privacy settings provide basic protection but could be improved.',
        color: 'yellow'
      };
    } else {
      return {
        level: 'low',
        description: 'Your privacy settings need attention to better protect your data.',
        color: 'red'
      };
    }
  }
};

export default privacyApi;