// System Configuration Types

export interface SystemConfiguration {
  id: string;
  version: string;
  environment: 'development' | 'staging' | 'production';
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  modifiedBy: string;
  description?: string;
  size?: number;

  // General Settings
  siteName: string;
  siteDescription?: string;
  siteUrl: string;
  language: string;
  timezone: string;

  // Feature Switches
  enableComments: boolean;
  enableRegistration: boolean;
  enableSearch: boolean;
  enableAnalytics: boolean;
  enableCache: boolean;
  maintenanceMode: boolean;

  // Content Settings
  postsPerPage: number;
  allowedFileTypes: string[];
  maxFileSize: number;
  enableAutoSave: boolean;
  autoSaveInterval: number;

  // Security Settings
  sessionTimeout: number;
  enableTwoFactor: boolean;
  passwordMinLength: number;
  enableCaptcha: boolean;
  maxLoginAttempts: number;

  // Third-party Integrations
  googleAnalyticsId?: string;
  emailProvider: 'smtp' | 'sendgrid' | 'mailgun';
  smtpHost?: string;
  smtpPort?: number;
  smtpUsername?: string;
  smtpPassword?: string;
  sendgridApiKey?: string;
  mailgunApiKey?: string;
  cdnEnabled: boolean;
  cdnUrl?: string;

  // Database Configuration
  databaseProvider?: 'sqlite' | 'postgresql' | 'mysql' | 'sqlserver' | 'oracle';
  connectionString?: string;
  connectionPoolSize?: number;
  commandTimeout?: number;

  // Caching Configuration
  redisConnectionString?: string;
  cacheDefaultExpiration?: number;
  enableDistributedCache?: boolean;

  // Logging Configuration
  logLevel: 'trace' | 'debug' | 'info' | 'warn' | 'error' | 'fatal';
  enableFileLogging: boolean;
  enableDatabaseLogging: boolean;
  logRetentionDays: number;

  // Performance Settings
  enableCompression: boolean;
  enableMinification: boolean;
  enableBundling: boolean;
  maxRequestSize: number;
  requestTimeout: number;

  // API Configuration
  apiRateLimit: number;
  enableApiDocumentation: boolean;
  apiVersioning: boolean;
  corsOrigins: string[];

  // Backup Configuration
  enableAutomaticBackup: boolean;
  backupSchedule?: string;
  backupRetentionDays: number;
  backupStorageProvider?: 'local' | 's3' | 'azure' | 'gcp';

  // Monitoring Configuration
  enableHealthChecks: boolean;
  healthCheckInterval: number;
  enableMetrics: boolean;
  metricsEndpoint?: string;

  // Custom Configuration
  customSettings?: Record<string, unknown>;
}

export interface ConfigurationField {
  key: string;
  label: string;
  type: 'input' | 'textarea' | 'number' | 'switch' | 'select' | 'tags' | 'password';
  required?: boolean;
  defaultValue?: unknown;
  options?: Array<{ value: unknown; label: string }>;
  validation?: {
    min?: number;
    max?: number;
    pattern?: RegExp;
    custom?: (value: unknown) => boolean | string;
  };
  tooltip?: string;
  dependsOn?: string;
  dependsOnValue?: unknown;
  warning?: boolean;
  span?: number;
  group?: string;
}

export interface ConfigurationSection {
  key: string;
  title: string;
  description: string;
  icon: string;
  fields: ConfigurationField[];
  order?: number;
  collapsed?: boolean;
}

export interface ConfigurationTemplate {
  id: string;
  name: string;
  description: string;
  category: string;
  config: Partial<SystemConfiguration>;
  tags?: string[];
  isDefault?: boolean;
  isPublic?: boolean;
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
  usageCount?: number;
}

export interface ConfigurationValidationError {
  field: string;
  message: string;
  severity: 'error' | 'warning' | 'info';
  code?: string;
  value?: unknown;
}

export interface ConfigurationValidationResult {
  isValid: boolean;
  errors: ConfigurationValidationError[];
  warnings: ConfigurationValidationError[];
  suggestions?: ConfigurationValidationError[];
}

export interface ConfigurationDiff {
  fromVersion: string;
  toVersion: string;
  changes: {
    added: Record<string, unknown>;
    modified: Record<string, { from: unknown; to: unknown }>;
    removed: Record<string, unknown>;
  };
  summary: {
    addedCount: number;
    modifiedCount: number;
    removedCount: number;
    totalChanges: number;
  };
}

export interface ConfigurationBackup {
  id: string;
  configId: string;
  version: string;
  description: string;
  createdAt: string;
  createdBy: string;
  size: number;
  checksum: string;
  tags?: string[];
}

export interface ConfigurationConflict {
  id: string;
  configId: string;
  field: string;
  conflictType: 'concurrent_modification' | 'validation_failure' | 'dependency_conflict';
  currentValue: unknown;
  conflictingValue: unknown;
  timestamp: string;
  userId: string;
  isResolved: boolean;
  resolution?: ConflictResolution;
}

export interface ConflictResolution {
  action: 'keep_current' | 'use_conflicting' | 'merge' | 'custom';
  value?: unknown;
  resolvedBy: string;
  resolvedAt: string;
  reason?: string;
}

export interface ConfigurationApproval {
  id: string;
  configId: string;
  requestedBy: string;
  requestedAt: string;
  changes: Partial<SystemConfiguration>;
  reason: string;
  status: 'pending' | 'approved' | 'rejected' | 'expired';
  approvedBy?: string;
  approvedAt?: string;
  rejectionReason?: string;
  expiresAt: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
}

export interface ConfigurationAudit {
  id: string;
  action: 'CREATE' | 'UPDATE' | 'DELETE' | 'ROLLBACK' | 'BACKUP' | 'RESTORE' | 'APPLY_TEMPLATE';
  configId: string;
  userId: string;
  userName: string;
  timestamp: string;
  changes: Record<string, { from?: unknown; to?: unknown }>;
  metadata?: {
    ip: string;
    userAgent: string;
    sessionId: string;
    reason?: string;
    approvalId?: string;
    templateId?: string;
  };
  ip: string;
  userAgent: string;
}

export interface ConfigurationImpactAnalysis {
  configId: string;
  affectedServices: string[];
  performanceImpact: number; // 0-100 percentage
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
  requiresRestart: boolean;
  estimatedDowntime?: number; // minutes
  warnings: string[];
  recommendations: string[];
  dependencies: Array<{
    service: string;
    impact: 'none' | 'minor' | 'major' | 'breaking';
    description: string;
  }>;
}

export interface ConfigurationDeployment {
  id: string;
  configId: string;
  environment: string;
  status: 'pending' | 'deploying' | 'deployed' | 'failed' | 'rolled_back';
  deployedBy: string;
  deployedAt?: string;
  rollbackReason?: string;
  deploymentLog: Array<{
    timestamp: string;
    level: 'info' | 'warn' | 'error';
    message: string;
    service?: string;
  }>;
}

export interface ConfigurationMetrics {
  configId: string;
  timestamp: string;
  metrics: {
    responseTime: number;
    errorRate: number;
    throughput: number;
    memoryUsage: number;
    cpuUsage: number;
    diskUsage: number;
    activeConnections: number;
    cacheHitRate?: number;
  };
}

export interface ConfigurationSchema {
  version: string;
  sections: ConfigurationSection[];
  validationRules: Array<{
    field: string;
    rule: string;
    message: string;
    severity: 'error' | 'warning';
  }>;
  dependencies: Array<{
    field: string;
    dependsOn: string;
    condition: unknown;
  }>;
  migrations?: Array<{
    fromVersion: string;
    toVersion: string;
    script: string;
  }>;
}

// API Response Types
export interface SystemConfigResponse {
  config: SystemConfiguration;
  schema: ConfigurationSchema;
  validationResult: ConfigurationValidationResult;
  lastModified: string;
}

export interface ConfigurationListResponse {
  configs: SystemConfiguration[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ConfigurationHistoryResponse {
  history: SystemConfiguration[];
  backups: ConfigurationBackup[];
  total: number;
}

export interface ConfigurationTemplateResponse {
  templates: ConfigurationTemplate[];
  categories: string[];
  total: number;
}

// Hook State Types
export interface SystemConfigState {
  configs: SystemConfiguration[];
  currentConfig: SystemConfiguration | null;
  configHistory: SystemConfiguration[];
  templates: ConfigurationTemplate[];
  validationErrors: ConfigurationValidationError[];
  conflicts: ConfigurationConflict[];
  approvals: ConfigurationApproval[];
  isLoading: boolean;
  isSaving: boolean;
  isValidating: boolean;
  lastError: string | null;
}

// Event Types
export interface ConfigurationEvent {
  type: 'CONFIG_CHANGED' | 'VALIDATION_FAILED' | 'CONFLICT_DETECTED' | 'APPROVAL_REQUIRED';
  configId: string;
  timestamp: string;
  data: Record<string, unknown>;
}

export interface ConfigurationChangeEvent extends ConfigurationEvent {
  type: 'CONFIG_CHANGED';
  data: {
    changes: Partial<SystemConfiguration>;
    version: string;
    userId: string;
  };
}

export interface ValidationFailedEvent extends ConfigurationEvent {
  type: 'VALIDATION_FAILED';
  data: {
    errors: ConfigurationValidationError[];
    config: Partial<SystemConfiguration>;
  };
}

export interface ConflictDetectedEvent extends ConfigurationEvent {
  type: 'CONFLICT_DETECTED';
  data: {
    conflict: ConfigurationConflict;
  };
}

export interface ApprovalRequiredEvent extends ConfigurationEvent {
  type: 'APPROVAL_REQUIRED';
  data: {
    approval: ConfigurationApproval;
    changes: Partial<SystemConfiguration>;
  };
}