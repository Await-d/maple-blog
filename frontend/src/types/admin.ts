/**
 * Admin-specific TypeScript type definitions
 * These interfaces define the data structures for permission management and audit logs
 */

import { BaseEntity, PaginatedResponse } from './common';
import { UserRole, User } from './auth';

// Re-export commonly used types for convenience
export type { User };
export { UserRole };

// Permission-related types
export enum PermissionAction {
  Create = 'create',
  Read = 'read',
  Update = 'update',
  Delete = 'delete',
  Manage = 'manage',
  Moderate = 'moderate',
  Execute = 'execute'
}

export enum ResourceType {
  Post = 'post',
  Comment = 'comment',
  User = 'user',
  Category = 'category',
  Tag = 'tag',
  Media = 'media',
  System = 'system',
  Analytics = 'analytics',
  Settings = 'settings'
}

export interface Permission extends BaseEntity {
  name: string;
  displayName: string;
  description: string;
  action: PermissionAction;
  resource: ResourceType;
  isSystemPermission: boolean;
  category: string;
}

export interface Role extends BaseEntity {
  name: string;
  displayName: string;
  description: string;
  isSystemRole: boolean;
  isDefault: boolean;
  priority: number;
  permissions: Permission[];
  userCount: number;
  parentRoleId?: string;
  parentRole?: Role;
  childRoles?: Role[];
}

export interface UserRoleAssignment extends BaseEntity {
  userId: string;
  roleId: string;
  assignedById: string;
  assignedBy: User;
  user: User;
  role: Role;
  expiresAt?: string;
  isActive: boolean;
}

// Permission Management interfaces
export interface PermissionMatrix {
  roles: Role[];
  permissions: Permission[];
  assignments: Record<string, string[]>; // roleId -> permissionIds
}

export interface RoleCreateRequest {
  name: string;
  displayName: string;
  description: string;
  parentRoleId?: string;
  permissionIds: string[];
}

export interface RoleUpdateRequest {
  displayName: string;
  description: string;
  parentRoleId?: string;
  permissionIds: string[];
}

export interface UserRoleUpdateRequest {
  userId: string;
  roleIds: string[];
  expiresAt?: string;
}

// Audit Log types
export enum AuditAction {
  Create = 'create',
  Read = 'read',
  Update = 'update',
  Delete = 'delete',
  Login = 'login',
  Logout = 'logout',
  PasswordChange = 'password_change',
  RoleAssignment = 'role_assignment',
  PermissionGrant = 'permission_grant',
  PermissionRevoke = 'permission_revoke',
  SystemConfig = 'system_config',
  DataExport = 'data_export',
  DataImport = 'data_import',
  SecurityEvent = 'security_event'
}

export enum AuditSeverity {
  Low = 'low',
  Medium = 'medium',
  High = 'high',
  Critical = 'critical'
}

export interface AuditLogEntry extends BaseEntity {
  action: AuditAction;
  resourceType: string;
  resourceId?: string;
  userId?: string;
  user?: User;
  ipAddress: string;
  userAgent: string;
  sessionId: string;
  description: string;
  details: Record<string, unknown>;
  severity: AuditSeverity;
  success: boolean;
  errorMessage?: string;
  duration?: number; // milliseconds
  metadata: {
    endpoint?: string;
    method?: string;
    requestId?: string;
    correlationId?: string;
    environment?: string;
    version?: string;
  };
}

// Audit Log filtering and search
export interface AuditLogFilters {
  search?: string;
  userId?: string;
  action?: AuditAction[];
  resourceType?: string[];
  severity?: AuditSeverity[];
  success?: boolean;
  dateFrom?: Date;
  dateTo?: Date;
  ipAddress?: string;
  sessionId?: string;
  sortBy?: 'createdAt' | 'action' | 'severity' | 'userId';
  sortOrder?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface AuditLogStats {
  totalEntries: number;
  todayEntries: number;
  weekEntries: number;
  monthEntries: number;
  actionStats: Record<AuditAction, number>;
  severityStats: Record<AuditSeverity, number>;
  topUsers: Array<{
    userId: string;
    username: string;
    entryCount: number;
  }>;
  topResources: Array<{
    resourceType: string;
    entryCount: number;
  }>;
  failureRate: number;
  averageDuration: number;
}

// Real-time audit log events
export interface AuditLogEvent {
  type: 'new_entry' | 'bulk_insert' | 'retention_cleanup';
  entry?: AuditLogEntry;
  entries?: AuditLogEntry[];
  timestamp: string;
  count?: number;
}

// Export options for audit logs
export enum ExportFormat {
  CSV = 'csv',
  JSON = 'json',
  Excel = 'xlsx',
  PDF = 'pdf'
}

export interface AuditLogExportRequest {
  format: ExportFormat;
  filters: AuditLogFilters;
  includeDetails: boolean;
  includeMetadata: boolean;
  filename?: string;
}

export interface ExportJob extends BaseEntity {
  jobId: string;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  format: ExportFormat;
  totalRecords: number;
  processedRecords: number;
  filePath?: string;
  downloadUrl?: string;
  errorMessage?: string;
  expiresAt: string;
  createdById: string;
  createdBy: User;
}

// System health and monitoring
export interface SystemHealthMetrics {
  timestamp: string;
  auditLogHealth: {
    totalEntries: number;
    entriesLast24h: number;
    avgProcessingTime: number;
    failureRate: number;
    storageUsed: number;
    storageCapacity: number;
  };
  permissionHealth: {
    totalRoles: number;
    totalPermissions: number;
    activeUsers: number;
    roleConflicts: number;
    orphanedPermissions: number;
  };
  systemHealth: {
    cpu: number;
    memory: number;
    disk: number;
    network: number;
    uptime: number;
  };
}

// API response types
export interface PermissionManagementResponse {
  matrix: PermissionMatrix;
  userRoleAssignments: UserRoleAssignment[];
  stats: {
    totalRoles: number;
    totalPermissions: number;
    totalUsers: number;
    customRoles: number;
  };
}

export interface AuditLogResponse extends PaginatedResponse<AuditLogEntry> {
  stats: AuditLogStats;
  filters: AuditLogFilters;
}

// WebSocket event types for real-time updates
export interface AdminWebSocketMessage {
  type: 'audit_log_entry' | 'permission_change' | 'role_assignment' | 'system_health' | 'export_progress';
  data: AuditLogEvent | PermissionMatrix | SystemHealthMetrics | ExportJob;
  timestamp: string;
}

// Form validation schemas
export interface RoleFormData {
  name: string;
  displayName: string;
  description: string;
  parentRoleId: string;
  permissionIds: string[];
}

export interface PermissionFormData {
  name: string;
  displayName: string;
  description: string;
  action: PermissionAction;
  resource: ResourceType;
  category: string;
}

// UI state types
export interface PermissionManagementState {
  matrix: PermissionMatrix | null;
  selectedRole: Role | null;
  selectedPermissions: string[];
  isLoading: boolean;
  error: string | null;
  isDirty: boolean;
}

export interface AuditLogState {
  entries: AuditLogEntry[];
  stats: AuditLogStats | null;
  filters: AuditLogFilters;
  selectedEntry: AuditLogEntry | null;
  isLoading: boolean;
  error: string | null;
  realTimeEnabled: boolean;
  exportJob: ExportJob | null;
}

// Component props interfaces
export interface PermissionMatrixProps {
  matrix: PermissionMatrix;
  selectedRole: Role | null;
  onRoleSelect: (role: Role | null) => void;
  onPermissionToggle: (roleId: string, permissionId: string) => void;
  readonly?: boolean;
}

export interface AuditLogTableProps {
  entries: AuditLogEntry[];
  isLoading: boolean;
  onEntrySelect: (entry: AuditLogEntry) => void;
  onRefresh: () => void;
  onExport: (format: ExportFormat) => void;
}

export interface AuditLogFiltersProps {
  filters: AuditLogFilters;
  onFiltersChange: (filters: AuditLogFilters) => void;
  onReset: () => void;
}

// Utility types
export type PermissionCheck = (permission: string, resourceType?: string, resourceId?: string) => boolean;
export type RoleCheck = (role: UserRole | string) => boolean;

// Constants for permission categories
export const PERMISSION_CATEGORIES = [
  'Content Management',
  'User Management',
  'System Administration',
  'Analytics & Reporting',
  'Security & Moderation',
  'Media Management',
  'Settings & Configuration'
] as const;

export type PermissionCategory = typeof PERMISSION_CATEGORIES[number];

// Predefined system permissions
export const SYSTEM_PERMISSIONS = {
  MANAGE_USERS: 'manage:users',
  MANAGE_CONTENT: 'manage:content',
  MANAGE_SYSTEM: 'manage:system',
  VIEW_ANALYTICS: 'read:analytics',
  MODERATE_COMMENTS: 'moderate:comments',
  MANAGE_MEDIA: 'manage:media',
  EXPORT_DATA: 'execute:export',
  AUDIT_LOGS: 'read:audit_logs'
} as const;

export type SystemPermission = typeof SYSTEM_PERMISSIONS[keyof typeof SYSTEM_PERMISSIONS];

// Export aliases for commonly used types (to fix compilation errors)
export type AdminStats = AuditLogStats;
export type SystemHealth = SystemHealthMetrics;
export type AuditLog = AuditLogEntry;
export type AdminSettings = PermissionManagementState;
export type SystemMetrics = SystemHealthMetrics;
export type BackupInfo = ExportJob;
export type SecurityReport = AuditLogStats;