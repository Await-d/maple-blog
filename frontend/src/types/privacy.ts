/**
 * Privacy Settings Types
 * Comprehensive privacy and data protection management
 */

export type ProfileVisibility = 'public' | 'friends_only' | 'private';
export type ContentSharingLevel = 'public' | 'registered_users' | 'followers_only' | 'private';
export type DataCollectionLevel = 'all' | 'essential' | 'minimal' | 'none';

/**
 * Profile privacy settings
 */
export interface ProfilePrivacySettings {
  /** Overall profile visibility */
  visibility: ProfileVisibility;

  /** Show profile in search results */
  showInSearch: boolean;

  /** Allow others to see your followers list */
  showFollowersList: boolean;

  /** Allow others to see who you're following */
  showFollowingList: boolean;

  /** Show your activity (likes, comments) to others */
  showActivity: boolean;

  /** Show your reading history to others */
  showReadingHistory: boolean;

  /** Allow others to see your online status */
  showOnlineStatus: boolean;

  /** Show join date on profile */
  showJoinDate: boolean;

  /** Show post statistics */
  showPostStats: boolean;

  /** Contact information visibility */
  contactInfo: {
    showEmail: boolean;
    showPhone: boolean;
    showSocialLinks: boolean;
    showLocation: boolean;
  };
}

/**
 * Content sharing and interaction settings
 */
export interface ContentSharingSettings {
  /** Who can comment on your posts */
  whoCanComment: ContentSharingLevel;

  /** Who can like your posts */
  whoCanLike: ContentSharingLevel;

  /** Who can share your posts */
  whoCanShare: ContentSharingLevel;

  /** Who can mention you in comments */
  whoCanMention: ContentSharingLevel;

  /** Who can send you direct messages */
  whoCanMessage: ContentSharingLevel;

  /** Who can see your posts */
  postVisibility: ContentSharingLevel;

  /** Automatically approve followers */
  autoApproveFollowers: boolean;

  /** Require approval for comments */
  requireCommentApproval: boolean;

  /** Allow anonymous interactions */
  allowAnonymousInteractions: boolean;

  /** Enable content embedding on other sites */
  allowContentEmbedding: boolean;
}

/**
 * Data collection and tracking preferences
 */
export interface DataCollectionSettings {
  /** Overall data collection level */
  collectionLevel: DataCollectionLevel;

  /** Analytics and usage tracking */
  analytics: {
    enabled: boolean;
    includePageViews: boolean;
    includeClickTracking: boolean;
    includeScrollTracking: boolean;
    includeTimeSpent: boolean;
    includeDeviceInfo: boolean;
    includeBrowserInfo: boolean;
    includeLocationData: boolean;
  };

  /** Personalization and recommendations */
  personalization: {
    enabled: boolean;
    collectReadingHistory: boolean;
    collectInteractionData: boolean;
    collectSearchHistory: boolean;
    enableContentRecommendations: boolean;
    enableUserRecommendations: boolean;
    shareDataWithPartners: boolean;
  };

  /** Marketing and advertising */
  marketing: {
    enabled: boolean;
    personalizedAds: boolean;
    crossSiteTracking: boolean;
    emailMarketing: boolean;
    behavioralTargeting: boolean;
    socialMediaIntegration: boolean;
  };

  /** Third-party integrations */
  thirdParty: {
    allowGoogleAnalytics: boolean;
    allowSocialLoginTracking: boolean;
    allowExternalEmbeds: boolean;
    allowCdnTracking: boolean;
    shareWithSocialPlatforms: boolean;
  };
}

/**
 * Data rights and control settings
 */
export interface DataRightsSettings {
  /** Data export settings */
  dataExport: {
    includeProfiles: boolean;
    includePosts: boolean;
    includeComments: boolean;
    includeInteractions: boolean;
    includePrivateData: boolean;
    format: 'json' | 'csv' | 'xml';
  };

  /** Data retention preferences */
  dataRetention: {
    deleteInactiveData: boolean;
    inactivityPeriod: number; // days
    deleteOldInteractions: boolean;
    interactionRetentionDays: number;
    deleteOldNotifications: boolean;
    notificationRetentionDays: number;
  };

  /** Account deletion preferences */
  accountDeletion: {
    deleteMethod: 'immediate' | 'delayed' | 'anonymize';
    delayPeriod: number; // days for delayed deletion
    preserveContent: boolean;
    anonymizeContent: boolean;
    notifyContacts: boolean;
  };
}

/**
 * Security and access control settings
 */
export interface SecuritySettings {
  /** Two-factor authentication */
  twoFactorAuth: {
    enabled: boolean;
    method: 'sms' | 'email' | 'app' | 'backup_codes';
    backupCodes: string[];
    lastUpdated: string;
  };

  /** Login security */
  loginSecurity: {
    requireStrongPassword: boolean;
    enableLoginNotifications: boolean;
    logUnusualActivity: boolean;
    sessionTimeout: number; // minutes
    maxActiveSessions: number;
    logoutOnPasswordChange: boolean;
  };

  /** Device and location tracking */
  deviceSecurity: {
    rememberDevices: boolean;
    deviceRetentionDays: number;
    logDeviceChanges: boolean;
    requireApprovalForNewDevices: boolean;
    trackLocation: boolean;
    logLocationChanges: boolean;
  };

  /** API and third-party access */
  apiAccess: {
    enableApiAccess: boolean;
    allowThirdPartyApps: boolean;
    requireAppApproval: boolean;
    revokeUnusedTokens: boolean;
    tokenExpirationDays: number;
  };
}

/**
 * Communication preferences
 */
export interface CommunicationSettings {
  /** Who can contact you */
  whoCanContact: ContentSharingLevel;

  /** Message filtering */
  messageFiltering: {
    enabled: boolean;
    filterSpam: boolean;
    filterProfanity: boolean;
    requireKeywords: string[];
    blockKeywords: string[];
    blockDomains: string[];
  };

  /** Auto-responses */
  autoResponses: {
    enabled: boolean;
    message: string;
    conditions: Array<{
      condition: 'all' | 'followers_only' | 'new_users' | 'keywords';
      value?: string;
    }>;
  };

  /** Communication limits */
  limits: {
    maxMessagesPerDay: number;
    maxMessagesPerUser: number;
    cooldownBetweenMessages: number; // minutes
  };
}

/**
 * Comprehensive privacy settings
 */
export interface PrivacySettings {
  profile: ProfilePrivacySettings;
  contentSharing: ContentSharingSettings;
  dataCollection: DataCollectionSettings;
  dataRights: DataRightsSettings;
  security: SecuritySettings;
  communication: CommunicationSettings;

  /** Privacy policy acknowledgment */
  policyAcknowledgment: {
    version: string;
    acknowledgedAt: string;
    ipAddress: string;
  };

  /** GDPR compliance */
  gdprCompliance: {
    enabled: boolean;
    lawfulBasis: 'consent' | 'contract' | 'legal_obligation' | 'vital_interests' | 'public_task' | 'legitimate_interests';
    consentWithdrawn: boolean;
    consentWithdrawnAt?: string;
    dataProcessingAgreement: boolean;
  };

  /** Last updated */
  updatedAt: string;
}

/**
 * Privacy audit log entry
 */
/**
 * Type representing any serializable privacy value
 */
export type PrivacyValue =
  | string
  | number
  | boolean
  | null
  | PrivacyValue[]
  | { [key: string]: PrivacyValue };

export interface PrivacyAuditLog {
  id: string;
  action: 'view' | 'update' | 'delete' | 'export' | 'consent_given' | 'consent_withdrawn';
  section: keyof PrivacySettings;
  field?: string;
  oldValue?: PrivacyValue;
  newValue?: PrivacyValue;
  reason?: string;
  timestamp: string;
  ipAddress: string;
  userAgent: string;
}

/**
 * API request types
 */
export interface UpdatePrivacySettingsRequest {
  settings: Partial<PrivacySettings>;
  reason?: string;
}

export interface DataExportRequest {
  includeProfiles: boolean;
  includePosts: boolean;
  includeComments: boolean;
  includeInteractions: boolean;
  includePrivateData: boolean;
  format: 'json' | 'csv' | 'xml';
}

export interface AccountDeletionRequest {
  password: string;
  reason: string;
  deleteMethod: 'immediate' | 'delayed' | 'anonymize';
  preserveContent: boolean;
}

/**
 * API response types
 */
export interface PrivacySettingsResponse {
  settings: PrivacySettings;
  success: boolean;
  message?: string;
  timestamp: string;
}

export interface DataExportResponse {
  exportId: string;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  downloadUrl?: string;
  expiresAt?: string;
  fileSize?: number;
  success: boolean;
  message?: string;
}

export interface PrivacyAuditResponse {
  logs: PrivacyAuditLog[];
  totalCount: number;
  pagination: {
    page: number;
    pageSize: number;
    totalPages: number;
  };
  success: boolean;
}

/**
 * Default privacy settings
 */
export const DEFAULT_PRIVACY_SETTINGS: PrivacySettings = {
  profile: {
    visibility: 'public',
    showInSearch: true,
    showFollowersList: true,
    showFollowingList: true,
    showActivity: true,
    showReadingHistory: false,
    showOnlineStatus: true,
    showJoinDate: true,
    showPostStats: true,
    contactInfo: {
      showEmail: false,
      showPhone: false,
      showSocialLinks: true,
      showLocation: false
    }
  },
  contentSharing: {
    whoCanComment: 'public',
    whoCanLike: 'public',
    whoCanShare: 'public',
    whoCanMention: 'registered_users',
    whoCanMessage: 'registered_users',
    postVisibility: 'public',
    autoApproveFollowers: true,
    requireCommentApproval: false,
    allowAnonymousInteractions: true,
    allowContentEmbedding: true
  },
  dataCollection: {
    collectionLevel: 'essential',
    analytics: {
      enabled: true,
      includePageViews: true,
      includeClickTracking: false,
      includeScrollTracking: false,
      includeTimeSpent: true,
      includeDeviceInfo: false,
      includeBrowserInfo: false,
      includeLocationData: false
    },
    personalization: {
      enabled: true,
      collectReadingHistory: true,
      collectInteractionData: true,
      collectSearchHistory: true,
      enableContentRecommendations: true,
      enableUserRecommendations: true,
      shareDataWithPartners: false
    },
    marketing: {
      enabled: false,
      personalizedAds: false,
      crossSiteTracking: false,
      emailMarketing: false,
      behavioralTargeting: false,
      socialMediaIntegration: false
    },
    thirdParty: {
      allowGoogleAnalytics: false,
      allowSocialLoginTracking: false,
      allowExternalEmbeds: true,
      allowCdnTracking: true,
      shareWithSocialPlatforms: false
    }
  },
  dataRights: {
    dataExport: {
      includeProfiles: true,
      includePosts: true,
      includeComments: true,
      includeInteractions: true,
      includePrivateData: false,
      format: 'json'
    },
    dataRetention: {
      deleteInactiveData: false,
      inactivityPeriod: 365,
      deleteOldInteractions: false,
      interactionRetentionDays: 365,
      deleteOldNotifications: true,
      notificationRetentionDays: 90
    },
    accountDeletion: {
      deleteMethod: 'delayed',
      delayPeriod: 30,
      preserveContent: false,
      anonymizeContent: true,
      notifyContacts: true
    }
  },
  security: {
    twoFactorAuth: {
      enabled: false,
      method: 'email',
      backupCodes: [],
      lastUpdated: new Date().toISOString()
    },
    loginSecurity: {
      requireStrongPassword: true,
      enableLoginNotifications: true,
      logUnusualActivity: true,
      sessionTimeout: 60,
      maxActiveSessions: 5,
      logoutOnPasswordChange: true
    },
    deviceSecurity: {
      rememberDevices: true,
      deviceRetentionDays: 90,
      logDeviceChanges: true,
      requireApprovalForNewDevices: false,
      trackLocation: false,
      logLocationChanges: false
    },
    apiAccess: {
      enableApiAccess: false,
      allowThirdPartyApps: false,
      requireAppApproval: true,
      revokeUnusedTokens: true,
      tokenExpirationDays: 30
    }
  },
  communication: {
    whoCanContact: 'registered_users',
    messageFiltering: {
      enabled: true,
      filterSpam: true,
      filterProfanity: true,
      requireKeywords: [],
      blockKeywords: [],
      blockDomains: []
    },
    autoResponses: {
      enabled: false,
      message: '',
      conditions: []
    },
    limits: {
      maxMessagesPerDay: 100,
      maxMessagesPerUser: 10,
      cooldownBetweenMessages: 1
    }
  },
  policyAcknowledgment: {
    version: '1.0',
    acknowledgedAt: new Date().toISOString(),
    ipAddress: ''
  },
  gdprCompliance: {
    enabled: false,
    lawfulBasis: 'consent',
    consentWithdrawn: false,
    dataProcessingAgreement: false
  },
  updatedAt: new Date().toISOString()
};