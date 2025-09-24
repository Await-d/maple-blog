/**
 * System Settings Types and Interfaces
 */

export interface GeneralSettings {
  siteTitle: string;
  siteDescription: string;
  keywords: string[];
  logo: string;
  favicon: string;
  language: string;
  timezone: string;
  dateFormat: string;
  timeFormat: string;
  contactEmail: string;
  contactPhone: string;
  address: string;
}

export interface ContentSettings {
  postsPerPage: number;
  commentModeration: 'auto' | 'manual' | 'disabled';
  editorType: 'markdown' | 'wysiwyg' | 'both';
  allowedImageFormats: string[];
  maxImageSize: number; // in MB
  enableSEO: boolean;
  autoArchiveDays: number;
  enableSocialSharing: boolean;
  defaultPostStatus: 'draft' | 'published';
  enableCategories: boolean;
  enableTags: boolean;
}

export interface SecuritySettings {
  userRegistration: 'open' | 'invite' | 'disabled';
  passwordMinLength: number;
  requireSpecialChars: boolean;
  requireNumbers: boolean;
  requireUppercase: boolean;
  sessionTimeout: number; // in minutes
  enable2FA: boolean;
  maxLoginAttempts: number;
  lockoutDuration: number; // in minutes
  privacyPolicyUrl: string;
  termsOfServiceUrl: string;
}

export interface EmailSettings {
  smtpServer: string;
  smtpPort: number;
  smtpUsername: string;
  smtpPassword: string;
  smtpSecure: boolean;
  senderName: string;
  senderEmail: string;
  notifyNewUser: boolean;
  notifyNewComment: boolean;
  notifyNewPost: boolean;
  emailVerificationRequired: boolean;
}

export interface PerformanceSettings {
  enableCache: boolean;
  cacheType: 'redis' | 'memory' | 'file';
  cacheExpiration: number; // in seconds
  cdnUrl: string;
  enableImageOptimization: boolean;
  enableGzipCompression: boolean;
  maintenanceMode: boolean;
  maintenanceMessage: string;
  backupSchedule: 'daily' | 'weekly' | 'monthly' | 'disabled';
}

export interface AppearanceSettings {
  theme: 'light' | 'dark' | 'auto';
  primaryColor: string;
  accentColor: string;
  fontFamily: string;
  fontSize: number;
  headerStyle: 'minimal' | 'standard' | 'featured';
  sidebarPosition: 'left' | 'right' | 'disabled';
  customCSS: string;
  customHTML: string;
  logoPosition: 'header' | 'sidebar' | 'both';
}

export interface SystemSettings {
  general: GeneralSettings;
  content: ContentSettings;
  security: SecuritySettings;
  email: EmailSettings;
  performance: PerformanceSettings;
  appearance: AppearanceSettings;
}

export interface SettingsFormData {
  [key: string]: unknown;
}

export interface SettingsValidationError {
  field: string;
  message: string;
}

export interface SettingsState {
  settings: SystemSettings;
  loading: boolean;
  saving: boolean;
  isDirty: boolean;
  errors: SettingsValidationError[];
  lastSaved: Date | null;
  autoSaveEnabled: boolean;
}

// Default settings values
export const DEFAULT_SETTINGS: SystemSettings = {
  general: {
    siteTitle: 'Maple Blog',
    siteDescription: 'A modern AI-driven blog system',
    keywords: ['blog', 'ai', 'maple', 'react', 'dotnet'],
    logo: '',
    favicon: '',
    language: 'en',
    timezone: 'UTC',
    dateFormat: 'YYYY-MM-DD',
    timeFormat: 'HH:mm',
    contactEmail: '',
    contactPhone: '',
    address: '',
  },
  content: {
    postsPerPage: 10,
    commentModeration: 'manual',
    editorType: 'markdown',
    allowedImageFormats: ['jpg', 'jpeg', 'png', 'gif', 'webp'],
    maxImageSize: 5,
    enableSEO: true,
    autoArchiveDays: 365,
    enableSocialSharing: true,
    defaultPostStatus: 'draft',
    enableCategories: true,
    enableTags: true,
  },
  security: {
    userRegistration: 'invite',
    passwordMinLength: 8,
    requireSpecialChars: true,
    requireNumbers: true,
    requireUppercase: true,
    sessionTimeout: 30,
    enable2FA: false,
    maxLoginAttempts: 5,
    lockoutDuration: 15,
    privacyPolicyUrl: '',
    termsOfServiceUrl: '',
  },
  email: {
    smtpServer: '',
    smtpPort: 587,
    smtpUsername: '',
    smtpPassword: '',
    smtpSecure: true,
    senderName: 'Maple Blog',
    senderEmail: '',
    notifyNewUser: true,
    notifyNewComment: true,
    notifyNewPost: false,
    emailVerificationRequired: false,
  },
  performance: {
    enableCache: true,
    cacheType: 'memory',
    cacheExpiration: 3600,
    cdnUrl: '',
    enableImageOptimization: true,
    enableGzipCompression: true,
    maintenanceMode: false,
    maintenanceMessage: 'Site is under maintenance. Please check back later.',
    backupSchedule: 'weekly',
  },
  appearance: {
    theme: 'auto',
    primaryColor: '#3b82f6',
    accentColor: '#10b981',
    fontFamily: 'Inter',
    fontSize: 14,
    headerStyle: 'standard',
    sidebarPosition: 'right',
    customCSS: '',
    customHTML: '',
    logoPosition: 'header',
  },
};

// Settings validation rules
export const VALIDATION_RULES = {
  general: {
    siteTitle: { required: true, minLength: 1, maxLength: 100 },
    siteDescription: { maxLength: 500 },
    contactEmail: { email: true },
    contactPhone: { pattern: /^[+]?[\d\s\-()]+$/ },
  },
  content: {
    postsPerPage: { min: 1, max: 100 },
    maxImageSize: { min: 1, max: 100 },
    autoArchiveDays: { min: 1, max: 3650 },
  },
  security: {
    passwordMinLength: { min: 6, max: 64 },
    sessionTimeout: { min: 5, max: 1440 },
    maxLoginAttempts: { min: 1, max: 20 },
    lockoutDuration: { min: 1, max: 1440 },
  },
  email: {
    smtpServer: { required: true },
    smtpPort: { min: 1, max: 65535 },
    smtpUsername: { required: true },
    smtpPassword: { required: true },
    senderEmail: { required: true, email: true },
  },
  performance: {
    cacheExpiration: { min: 60, max: 86400 },
  },
  appearance: {
    fontSize: { min: 10, max: 24 },
    primaryColor: { pattern: /^#[0-9A-Fa-f]{6}$/ },
    accentColor: { pattern: /^#[0-9A-Fa-f]{6}$/ },
  },
};