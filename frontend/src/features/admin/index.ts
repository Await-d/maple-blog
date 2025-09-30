/**
 * Admin Feature Index
 * Central exports for administrative functionality
 */

// Components
export { ErrorBoundary } from '../../components/admin/ErrorBoundary';
export { ToastNotifications } from '../../components/admin/ToastNotifications';
export { AuditLogSystem } from '../../components/admin/AuditLogSystem';
export { PermissionManagement } from '../../components/admin/PermissionManagement';
export { AccessibilityReport } from '../../components/admin/AccessibilityReport';

// Services and API
export { adminApi } from '../../services/admin/adminApi';
export { notificationService } from '../../services/admin/notificationService';

// Types (re-export from global types)
export type {
  AdminStats,
  SystemHealth,
  AuditLog,
  Permission,
  UserRole,
  AdminSettings,
  SystemMetrics,
  BackupInfo,
  SecurityReport
} from '../../types/admin';

// Admin feature configuration
export const adminFeature = {
  name: 'admin',
  version: '1.0.0',
  description: 'Administrative tools and system management',
  components: {
    ErrorBoundary: () => import('../../components/admin/ErrorBoundary').then(m => ({ default: m.ErrorBoundary })),
    ToastNotifications: () => import('../../components/admin/ToastNotifications').then(m => ({ default: m.ToastNotifications })),
    AuditLogSystem: () => import('../../components/admin/AuditLogSystem').then(m => ({ default: m.AuditLogSystem })),
    PermissionManagement: () => import('../../components/admin/PermissionManagement').then(m => ({ default: m.PermissionManagement })),
    AccessibilityReport: () => import('../../components/admin/AccessibilityReport').then(m => ({ default: m.AccessibilityReport }))
  },
  services: {
    adminApi: () => import('../../services/admin/adminApi').then(m => m.adminApi),
    notificationService: () => import('../../services/admin/notificationService').then(m => m.notificationService)
  }
} as const;

// Admin configuration types
interface AdminRole {
  id: string;
  name: string;
  description: string;
  color: string;
  permissions: string[];
}

interface AdminPermission {
  id: string;
  name: string;
  category: string;
}

interface NotificationType {
  id: string;
  name: string;
  description: string;
  defaultChannels: string[];
  priority: string;
}

interface AdminConfigType {
  monitoring: {
    healthCheckInterval: number;
    metricsUpdateInterval: number;
    alertThresholds: {
      cpu: number;
      memory: number;
      disk: number;
      responseTime: number;
      errorRate: number;
    };
    retentionPeriod: {
      logs: number;
      metrics: number;
      backups: number;
    };
  };
  userManagement: {
    roles: AdminRole[];
    permissions: AdminPermission[];
  };
  contentManagement: {
    postStatuses: string[];
    bulkOperations: string[];
    moderationStatuses: string[];
    backupSchedule: {
      enabled: boolean;
      frequency: string;
      time: string;
      retention: number;
    };
  };
  security: {
    sessionTimeout: number;
    passwordPolicy: {
      minLength: number;
      requireUppercase: boolean;
      requireLowercase: boolean;
      requireNumbers: boolean;
      requireSpecialChars: boolean;
      preventReuse: number;
    };
    rateLimiting: {
      enabled: boolean;
      windowMs: number;
      maxRequests: number;
      skipSuccessfulRequests: boolean;
    };
    auditLogging: {
      enabled: boolean;
      events: string[];
    };
  };
  notifications: {
    channels: string[];
    types: NotificationType[];
  };
  analytics: {
    dashboardMetrics: string[];
    reportSchedule: {
      daily: string[];
      weekly: string[];
      monthly: string[];
    };
    exportFormats: string[];
  };
}

// Admin configuration
export const adminConfig: AdminConfigType = {
  // System monitoring
  monitoring: {
    healthCheckInterval: 30000, // 30 seconds
    metricsUpdateInterval: 60000, // 1 minute
    alertThresholds: {
      cpu: 80, // percent
      memory: 85, // percent
      disk: 90, // percent
      responseTime: 2000, // milliseconds
      errorRate: 5 // percent
    },
    retentionPeriod: {
      logs: 30, // days
      metrics: 90, // days
      backups: 7 // days
    }
  },
  
  // User management
  userManagement: {
    roles: [
      {
        id: 'admin',
        name: 'Administrator',
        description: 'Full system access',
        color: 'red',
        permissions: ['*']
      },
      {
        id: 'editor',
        name: 'Editor',
        description: 'Content management access',
        color: 'blue',
        permissions: [
          'posts:read', 'posts:write', 'posts:publish', 'posts:delete',
          'categories:read', 'categories:write',
          'tags:read', 'tags:write',
          'comments:read', 'comments:moderate',
          'media:read', 'media:upload'
        ]
      },
      {
        id: 'moderator',
        name: 'Moderator',
        description: 'Comment and user moderation',
        color: 'green',
        permissions: [
          'posts:read',
          'comments:read', 'comments:moderate', 'comments:delete',
          'users:read', 'users:moderate'
        ]
      },
      {
        id: 'author',
        name: 'Author',
        description: 'Can write and publish posts',
        color: 'purple',
        permissions: [
          'posts:read', 'posts:write', 'posts:publish',
          'comments:read',
          'media:read', 'media:upload'
        ]
      },
      {
        id: 'user',
        name: 'User',
        description: 'Basic user permissions',
        color: 'gray',
        permissions: [
          'posts:read',
          'comments:read', 'comments:write',
          'profile:read', 'profile:write'
        ]
      }
    ],
    
    permissions: [
      { id: 'posts:read', name: 'Read Posts', category: 'content' },
      { id: 'posts:write', name: 'Write Posts', category: 'content' },
      { id: 'posts:publish', name: 'Publish Posts', category: 'content' },
      { id: 'posts:delete', name: 'Delete Posts', category: 'content' },
      { id: 'categories:read', name: 'Read Categories', category: 'content' },
      { id: 'categories:write', name: 'Manage Categories', category: 'content' },
      { id: 'tags:read', name: 'Read Tags', category: 'content' },
      { id: 'tags:write', name: 'Manage Tags', category: 'content' },
      { id: 'comments:read', name: 'Read Comments', category: 'interaction' },
      { id: 'comments:write', name: 'Write Comments', category: 'interaction' },
      { id: 'comments:moderate', name: 'Moderate Comments', category: 'interaction' },
      { id: 'comments:delete', name: 'Delete Comments', category: 'interaction' },
      { id: 'users:read', name: 'Read Users', category: 'administration' },
      { id: 'users:write', name: 'Manage Users', category: 'administration' },
      { id: 'users:moderate', name: 'Moderate Users', category: 'administration' },
      { id: 'users:delete', name: 'Delete Users', category: 'administration' },
      { id: 'media:read', name: 'Read Media', category: 'media' },
      { id: 'media:upload', name: 'Upload Media', category: 'media' },
      { id: 'media:delete', name: 'Delete Media', category: 'media' },
      { id: 'profile:read', name: 'Read Profile', category: 'profile' },
      { id: 'profile:write', name: 'Edit Profile', category: 'profile' },
      { id: 'system:read', name: 'Read System Info', category: 'system' },
      { id: 'system:write', name: 'Manage System', category: 'system' }
    ]
  },
  
  // Content management
  contentManagement: {
    postStatuses: ['draft', 'published', 'archived', 'deleted'],
    bulkOperations: ['publish', 'unpublish', 'delete', 'archive', 'export'],
    moderationStatuses: ['pending', 'approved', 'rejected', 'spam'],
    backupSchedule: {
      enabled: true,
      frequency: 'daily', // daily, weekly, monthly
      time: '02:00', // UTC time
      retention: 7 // days
    }
  },
  
  // Security settings
  security: {
    sessionTimeout: 8 * 60 * 60 * 1000, // 8 hours for admin users
    passwordPolicy: {
      minLength: 12,
      requireUppercase: true,
      requireLowercase: true,
      requireNumbers: true,
      requireSpecialChars: true,
      preventReuse: 5 // last 5 passwords
    },
    rateLimiting: {
      enabled: true,
      windowMs: 15 * 60 * 1000, // 15 minutes
      maxRequests: 100,
      skipSuccessfulRequests: false
    },
    auditLogging: {
      enabled: true,
      events: [
        'user_login',
        'user_logout',
        'password_change',
        'permission_change',
        'post_create',
        'post_update',
        'post_delete',
        'user_create',
        'user_update',
        'user_delete',
        'system_setting_change'
      ]
    }
  },
  
  // Notification settings
  notifications: {
    channels: ['email', 'web', 'webhook'],
    types: [
      {
        id: 'system_alert',
        name: 'System Alert',
        description: 'Critical system issues',
        defaultChannels: ['email', 'web'],
        priority: 'high'
      },
      {
        id: 'security_alert',
        name: 'Security Alert',
        description: 'Security-related events',
        defaultChannels: ['email', 'web'],
        priority: 'high'
      },
      {
        id: 'user_activity',
        name: 'User Activity',
        description: 'User registration, login anomalies',
        defaultChannels: ['web'],
        priority: 'medium'
      },
      {
        id: 'content_moderation',
        name: 'Content Moderation',
        description: 'Comments awaiting moderation',
        defaultChannels: ['web'],
        priority: 'medium'
      },
      {
        id: 'system_maintenance',
        name: 'System Maintenance',
        description: 'Scheduled maintenance notifications',
        defaultChannels: ['email'],
        priority: 'low'
      }
    ]
  },
  
  // Analytics and reporting
  analytics: {
    dashboardMetrics: [
      'total_users',
      'total_posts',
      'total_comments',
      'page_views',
      'unique_visitors',
      'bounce_rate',
      'avg_session_duration',
      'top_posts',
      'top_categories',
      'user_growth',
      'content_growth'
    ],
    reportSchedule: {
      daily: ['page_views', 'user_activity', 'system_health'],
      weekly: ['user_growth', 'content_performance', 'security_summary'],
      monthly: ['comprehensive_analytics', 'performance_review', 'capacity_planning']
    },
    exportFormats: ['pdf', 'csv', 'json', 'xlsx']
  }
} as const;

// Admin utilities
export const adminUtils = {
  /**
   * Check if user has required permission
   */
  hasPermission: (userRole: string, requiredPermission: string): boolean => {
    const role = adminConfig.userManagement.roles.find(r => r.id === userRole);
    if (!role) return false;
    
    // Admin has all permissions
    if (role.permissions.includes('*')) return true;
    
    return role.permissions.includes(requiredPermission);
  },
  
  /**
   * Get user role information
   */
  getRoleInfo: (roleId: string) => {
    return adminConfig.userManagement.roles.find(role => role.id === roleId);
  },
  
  /**
   * Format system metrics
   */
  formatMetric: (value: number, type: 'percentage' | 'bytes' | 'duration' | 'count'): string => {
    switch (type) {
      case 'percentage':
        return `${value.toFixed(1)}%`;
      case 'bytes': {
        const units = ['B', 'KB', 'MB', 'GB', 'TB'];
        let size = value;
        let unitIndex = 0;
        while (size >= 1024 && unitIndex < units.length - 1) {
          size /= 1024;
          unitIndex++;
        }
        return `${size.toFixed(1)} ${units[unitIndex]}`;
      }
      case 'duration':
        if (value < 1000) return `${value}ms`;
        if (value < 60000) return `${(value / 1000).toFixed(1)}s`;
        return `${(value / 60000).toFixed(1)}m`;
      case 'count':
        return value.toLocaleString();
      default:
        return value.toString();
    }
  },
  
  /**
   * Generate audit log entry
   */
  createAuditLog: (action: string, resource: string, details: Record<string, unknown> = {}) => {
    return {
      id: crypto.randomUUID(),
      timestamp: new Date().toISOString(),
      action,
      resource,
      details,
      ip: 'client-ip', // Should be replaced with actual IP
      userAgent: navigator.userAgent
    };
  },

  /**
   * Validate admin configuration
   */
  validateConfig: (config: Record<string, unknown>) => {
    const errors: string[] = [];
    const warnings: string[] = [];

    // Validate required fields
    const requiredFields = ['siteName', 'adminEmail', 'timezone'];
    requiredFields.forEach(field => {
      if (!config[field]) {
        errors.push(`Missing required field: ${field}`);
      }
    });

    // Validate email format
    if (config.adminEmail && typeof config.adminEmail === 'string' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(config.adminEmail)) {
      errors.push('Invalid admin email format');
    }

    // Validate numeric ranges
    if (config.sessionTimeout && typeof config.sessionTimeout === 'number' && (config.sessionTimeout < 300000 || config.sessionTimeout > 86400000)) {
      warnings.push('Session timeout should be between 5 minutes and 24 hours');
    }

    return { errors, warnings, isValid: errors.length === 0 };
  }
};

export default adminFeature;