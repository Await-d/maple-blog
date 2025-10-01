// 基础类型定义
export interface BaseResponse<T = unknown> {
  success: boolean;
  data: T;
  message?: string;
  code?: number;
}

export interface PaginatedResponse<T = unknown> extends BaseResponse<T[]> {
  pagination: {
    current: number;
    pageSize: number;
    total: number;
    totalPages: number;
  };
}

export interface QueryParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  search?: string;
  [key: string]: unknown;
}

// 用户相关类型
export interface User {
  id: string;
  username: string;
  email: string;
  displayName?: string;
  avatar?: string;
  roles: Role[];
  status: UserStatus;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  permissions: Permission[];
  level: number;
  isBuiltIn: boolean;
}

export interface Permission {
  id: string;
  name: string;
  code: string;
  description?: string;
  category: string;
  isActive: boolean;
}

export enum UserStatus {
  Active = 'active',
  Inactive = 'inactive',
  Banned = 'banned',
  Pending = 'pending'
}

export interface UserListQuery extends QueryParams {
  status?: UserStatus;
  roleId?: string;
  startDate?: string;
  endDate?: string;
}

export interface CreateUserInput {
  username: string;
  email: string;
  password: string;
  displayName?: string;
  avatar?: string;
  status?: UserStatus;
  roleIds?: string[];
}

export interface UpdateUserInput {
  username?: string;
  email?: string;
  password?: string;
  displayName?: string;
  avatar?: string;
  status?: UserStatus;
  roleIds?: string[];
}

export interface UserActivityLog {
  id: string;
  userId: string;
  type: 'login' | 'logout' | 'update' | 'permission_change' | 'security';
  description: string;
  createdAt: string;
  ip?: string;
  userAgent?: string;
  metadata?: Record<string, unknown>;
}

export interface UserSession {
  id: string;
  ip: string;
  location?: string;
  device?: string;
  createdAt: string;
}

export interface CreateRoleInput {
  name: string;
  description?: string;
  level: number;
  permissionIds: string[];
  isBuiltIn?: boolean;
}

export interface UpdateRoleInput {
  name?: string;
  description?: string;
  level?: number;
  permissionIds?: string[];
}

// 内容相关类型
export interface Post {
  id: string;
  title: string;
  slug: string;
  content: string;
  excerpt?: string;
  featuredImage?: string;
  featured?: boolean;
  status: PostStatus;
  publishedAt?: string;
  authorId: string;
  author: User;
  categoryId?: string;
  category?: Category;
  tags: Tag[];
  createdAt: string;
  updatedAt: string;
  viewCount: number;
  commentCount: number;
  likeCount: number;
}

export interface PostListQuery extends QueryParams {
  status?: PostStatus;
  categoryId?: string;
  tagIds?: string[];
  startDate?: string;
  endDate?: string;
  featured?: boolean;
}

export interface CreatePostInput {
  title: string;
  slug: string;
  excerpt?: string;
  content: string;
  status: PostStatus;
  categoryId: string;
  tagIds: string[];
  featuredImage?: string;
  featured?: boolean;
  seoTitle?: string;
  seoDescription?: string;
}

export interface UpdatePostInput {
  title?: string;
  slug?: string;
  excerpt?: string;
  content?: string;
  status?: PostStatus;
  categoryId?: string;
  tagIds?: string[];
  featuredImage?: string;
  featured?: boolean;
  seoTitle?: string;
  seoDescription?: string;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  parentId?: string;
  parent?: Category;
  children?: Category[];
  postCount: number;
  sortOrder: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCategoryInput {
  name: string;
  slug: string;
  description?: string;
  parentId?: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface UpdateCategoryInput {
  name?: string;
  slug?: string;
  description?: string;
  parentId?: string;
  sortOrder?: number;
  isActive?: boolean;
}

export interface Tag {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  postCount: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateTagInput {
  name: string;
  slug: string;
  description?: string;
  color?: string;
  isActive?: boolean;
}

export interface UpdateTagInput {
  name?: string;
  slug?: string;
  description?: string;
  color?: string;
  isActive?: boolean;
}

export enum PostStatus {
  Draft = 'draft',
  Published = 'published',
  Scheduled = 'scheduled',
  Archived = 'archived'
}

// 系统相关类型
export interface SystemConfig {
  id: string;
  key: string;
  value: string;
  type: ConfigType;
  category: string;
  description?: string;
  isEditable: boolean;
  isPublic: boolean;
  updatedAt: string;
  updatedBy: string;
}

export enum ConfigType {
  String = 'string',
  Number = 'number',
  Boolean = 'boolean',
  JSON = 'json'
}

// 统计相关类型
export interface DashboardStats {
  userStats: {
    total: number;
    active: number;
    newToday: number;
    trend: number;
  };
  contentStats: {
    totalPosts: number;
    publishedPosts: number;
    drafts: number;
    postsToday: number;
    trend: number;
  };
  systemStats: {
    viewsToday: number;
    viewsTotal: number;
    commentsToday: number;
    commentsTotal: number;
    performanceScore: number;
  };
  recentActivities: Activity[];
}

export interface Activity {
  id: string;
  type: ActivityType;
  description: string;
  userId: string;
  user: User;
  entityType?: string;
  entityId?: string;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

export enum ActivityType {
  UserLogin = 'user_login',
  UserRegister = 'user_register',
  PostCreate = 'post_create',
  PostUpdate = 'post_update',
  PostDelete = 'post_delete',
  CommentCreate = 'comment_create',
  SystemUpdate = 'system_update'
}

// 审计日志类型
export interface AuditLog {
  id: string;
  action: string;
  entityType: string;
  entityId: string;
  userId: string;
  user: User;
  ipAddress: string;
  userAgent: string;
  changes?: Record<string, unknown>;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

// 监控相关类型
export interface SystemMetrics {
  cpu: {
    usage: number;
    cores: number;
  };
  memory: {
    used: number;
    total: number;
    usage: number;
  };
  disk: {
    used: number;
    total: number;
    usage: number;
  };
  network: {
    bytesIn: number;
    bytesOut: number;
  };
  application: {
    uptime: number;
    requestCount: number;
    errorCount: number;
    responseTime: number;
  };
}

export interface HealthCheck {
  status: 'healthy' | 'degraded' | 'unhealthy';
  checks: {
    [key: string]: {
      status: 'pass' | 'fail' | 'warn';
      output?: string;
      duration?: number;
    };
  };
  timestamp: string;
}

// 表格相关类型
export interface TableColumn<T = Record<string, unknown>> {
  key: string;
  title: string;
  dataIndex?: string;
  width?: number;
  fixed?: 'left' | 'right';
  sortable?: boolean;
  filterable?: boolean;
  render?: (value: unknown, record: T, index: number) => React.ReactNode;
}

export interface TableProps<T = Record<string, unknown>> {
  columns: TableColumn<T>[];
  dataSource: T[];
  loading?: boolean;
  pagination?: {
    current: number;
    pageSize: number;
    total: number;
    onChange: (page: number, pageSize: number) => void;
  };
  rowSelection?: {
    selectedRowKeys: string[];
    onChange: (selectedRowKeys: string[], selectedRows: T[]) => void;
  };
  onRow?: (record: T) => Record<string, unknown>;
}

// 路由相关类型
export interface RouteConfig {
  path: string;
  component: React.ComponentType;
  exact?: boolean;
  meta?: {
    title?: string;
    requiresAuth?: boolean;
    permissions?: string[];
    icon?: React.ReactNode;
    hideInMenu?: boolean;
  };
  children?: RouteConfig[];
}

// 全局状态类型
export interface GlobalState {
  user: User | null;
  permissions: string[];
  collapsed: boolean;
  theme: 'light' | 'dark';
  loading: boolean;
  notifications: Notification[];
}

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  description?: string;
  duration?: number;
  action?: {
    text: string;
    onClick: () => void;
  };
  createdAt: string;
}

// API相关类型
export interface ApiError {
  message: string;
  code?: string;
  status?: number;
  details?: Record<string, unknown>;
}

export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresIn: number;
}

// 表单相关类型
export interface FormField {
  name: string;
  label: string;
  type: 'text' | 'email' | 'password' | 'number' | 'select' | 'checkbox' | 'radio' | 'textarea' | 'date' | 'file';
  required?: boolean;
  placeholder?: string;
  options?: { label: string; value: unknown }[];
  validation?: Record<string, unknown>;
  disabled?: boolean;
  tooltip?: string;
}

export interface FormProps {
  fields: FormField[];
  initialValues?: Record<string, unknown>;
  onSubmit: (values: Record<string, unknown>) => void;
  loading?: boolean;
  submitText?: string;
  cancelText?: string;
  onCancel?: () => void;
}

// 图表相关类型
export interface ChartConfig {
  type: 'line' | 'bar' | 'pie' | 'scatter' | 'heatmap';
  title?: string;
  data: Record<string, unknown>[];
  xAxis?: string;
  yAxis?: string;
  series?: string[];
  colors?: string[];
  options?: Record<string, unknown>;
}

// 导出系统配置相关类型
export * from './systemConfig';
export * from './analytics';
