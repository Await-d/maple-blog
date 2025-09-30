/**
 * Notification Settings Types
 * Comprehensive notification management system
 */

export type NotificationFrequency = 'immediate' | 'hourly' | 'daily' | 'weekly' | 'never';
export type NotificationChannel = 'email' | 'push' | 'in_app' | 'sms';
export type NotificationCategory = 'comments' | 'likes' | 'follows' | 'mentions' | 'posts' | 'system' | 'security';

/**
 * Individual notification preference
 */
export interface NotificationPreference {
  enabled: boolean;
  channels: NotificationChannel[];
  frequency: NotificationFrequency;
}

/**
 * Comprehensive notification settings
 */
export interface NotificationSettings {
  /** Email notification preferences */
  email: {
    /** New comments on user's posts */
    commentOnPost: NotificationPreference;
    /** Replies to user's comments */
    replyToComment: NotificationPreference;
    /** Likes on user's posts/comments */
    likes: NotificationPreference;
    /** New followers */
    newFollower: NotificationPreference;
    /** Mentions in comments */
    mentions: NotificationPreference;
    /** New posts from followed users */
    followedUserPost: NotificationPreference;
    /** System updates and maintenance */
    systemUpdates: NotificationPreference;
    /** Security alerts */
    securityAlerts: NotificationPreference;
    /** Marketing and newsletter */
    marketing: NotificationPreference;
    /** Digest emails */
    digest: {
      enabled: boolean;
      frequency: 'daily' | 'weekly' | 'monthly';
      includeTopPosts: boolean;
      includePopularComments: boolean;
    };
  };

  /** Push notification preferences */
  push: {
    enabled: boolean;
    /** Browser push notifications */
    browser: {
      enabled: boolean;
      permission: NotificationPermission;
    };
    /** Mobile app notifications (if applicable) */
    mobile: {
      enabled: boolean;
      sound: boolean;
      vibration: boolean;
      badge: boolean;
    };
    /** Same categories as email */
    commentOnPost: NotificationPreference;
    replyToComment: NotificationPreference;
    likes: NotificationPreference;
    newFollower: NotificationPreference;
    mentions: NotificationPreference;
    followedUserPost: NotificationPreference;
    systemUpdates: NotificationPreference;
    securityAlerts: NotificationPreference;
  };

  /** In-app notification preferences */
  inApp: {
    enabled: boolean;
    sound: {
      enabled: boolean;
      volume: number; // 0-1
      soundFile: string;
    };
    visual: {
      showNotificationDot: boolean;
      showToast: boolean;
      toastDuration: number; // in seconds
      animationsEnabled: boolean;
    };
    /** Notification categories */
    commentOnPost: boolean;
    replyToComment: boolean;
    likes: boolean;
    newFollower: boolean;
    mentions: boolean;
    followedUserPost: boolean;
    systemUpdates: boolean;
  };

  /** Do not disturb settings */
  doNotDisturb: {
    enabled: boolean;
    schedules: Array<{
      id: string;
      name: string;
      enabled: boolean;
      startTime: string; // HH:mm format
      endTime: string; // HH:mm format
      days: number[]; // 0-6, Sunday = 0
      timezone: string;
      allowUrgent: boolean;
    }>;
    urgentCategories: NotificationCategory[];
  };

  /** Global notification settings */
  global: {
    enabled: boolean;
    maxNotificationsPerHour: number;
    enableGrouping: boolean;
    enableBatchProcessing: boolean;
    timezone: string;
    language: string;
  };

  /** Last updated timestamp */
  updatedAt: string;
}

/**
 * Notification statistics and analytics
 */
export interface NotificationStats {
  /** Total notifications sent in last 30 days */
  totalSent: number;
  /** Notifications by channel */
  byChannel: Record<NotificationChannel, number>;
  /** Notifications by category */
  byCategory: Record<NotificationCategory, number>;
  /** Open rates */
  openRates: {
    email: number;
    push: number;
    inApp: number;
  };
  /** Click-through rates */
  clickRates: {
    email: number;
    push: number;
    inApp: number;
  };
  /** Unsubscribe rate */
  unsubscribeRate: number;
  /** Most active hours */
  activeHours: Array<{
    hour: number;
    count: number;
  }>;
}

/**
 * Notification template for customization
 */
export interface NotificationTemplate {
  id: string;
  category: NotificationCategory;
  name: string;
  subject: string;
  body: string;
  variables: string[];
  isCustomizable: boolean;
  isActive: boolean;
}

/**
 * API request types
 */
export interface UpdateNotificationSettingsRequest {
  settings: Partial<NotificationSettings>;
}

export interface TestNotificationRequest {
  channel: NotificationChannel;
  category: NotificationCategory;
  message?: string;
}

/**
 * API response types
 */
export interface NotificationSettingsResponse {
  settings: NotificationSettings;
  success: boolean;
  message?: string;
  timestamp: string;
}

export interface NotificationStatsResponse {
  stats: NotificationStats;
  success: boolean;
  timestamp: string;
}

/**
 * Default notification settings
 */
export const DEFAULT_NOTIFICATION_SETTINGS: NotificationSettings = {
  email: {
    commentOnPost: {
      enabled: true,
      channels: ['email'],
      frequency: 'immediate'
    },
    replyToComment: {
      enabled: true,
      channels: ['email'],
      frequency: 'immediate'
    },
    likes: {
      enabled: false,
      channels: ['email'],
      frequency: 'daily'
    },
    newFollower: {
      enabled: true,
      channels: ['email'],
      frequency: 'immediate'
    },
    mentions: {
      enabled: true,
      channels: ['email'],
      frequency: 'immediate'
    },
    followedUserPost: {
      enabled: true,
      channels: ['email'],
      frequency: 'daily'
    },
    systemUpdates: {
      enabled: true,
      channels: ['email'],
      frequency: 'immediate'
    },
    securityAlerts: {
      enabled: true,
      channels: ['email'],
      frequency: 'immediate'
    },
    marketing: {
      enabled: false,
      channels: ['email'],
      frequency: 'weekly'
    },
    digest: {
      enabled: true,
      frequency: 'weekly',
      includeTopPosts: true,
      includePopularComments: true
    }
  },
  push: {
    enabled: true,
    browser: {
      enabled: false,
      permission: 'default'
    },
    mobile: {
      enabled: true,
      sound: true,
      vibration: true,
      badge: true
    },
    commentOnPost: {
      enabled: true,
      channels: ['push'],
      frequency: 'immediate'
    },
    replyToComment: {
      enabled: true,
      channels: ['push'],
      frequency: 'immediate'
    },
    likes: {
      enabled: false,
      channels: ['push'],
      frequency: 'never'
    },
    newFollower: {
      enabled: true,
      channels: ['push'],
      frequency: 'immediate'
    },
    mentions: {
      enabled: true,
      channels: ['push'],
      frequency: 'immediate'
    },
    followedUserPost: {
      enabled: false,
      channels: ['push'],
      frequency: 'never'
    },
    systemUpdates: {
      enabled: true,
      channels: ['push'],
      frequency: 'immediate'
    },
    securityAlerts: {
      enabled: true,
      channels: ['push'],
      frequency: 'immediate'
    }
  },
  inApp: {
    enabled: true,
    sound: {
      enabled: true,
      volume: 0.5,
      soundFile: '/sounds/notification.mp3'
    },
    visual: {
      showNotificationDot: true,
      showToast: true,
      toastDuration: 5,
      animationsEnabled: true
    },
    commentOnPost: true,
    replyToComment: true,
    likes: true,
    newFollower: true,
    mentions: true,
    followedUserPost: true,
    systemUpdates: true
  },
  doNotDisturb: {
    enabled: false,
    schedules: [],
    urgentCategories: ['security', 'system']
  },
  global: {
    enabled: true,
    maxNotificationsPerHour: 10,
    enableGrouping: true,
    enableBatchProcessing: false,
    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    language: 'en'
  },
  updatedAt: new Date().toISOString()
};