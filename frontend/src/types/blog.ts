/**
 * Blog content management related TypeScript type definitions
 * These interfaces match the backend DTOs for type safety
 */

import { User, UserRole } from './auth';
export type { ApiResponse, OperationResult, OperationResultWithData } from './auth';

// PostStatus enum matching backend PostStatus
export enum PostStatus {
  Draft = 0,
  Published = 1,
  Private = 2,
  Scheduled = 3,
  Archived = 4,
}

// Post interface matching backend PostDto
export interface Post {
  id: string;
  title: string;
  content: string;
  excerpt: string;
  slug: string;
  status: PostStatus;
  featuredImage?: string;
  publishedAt?: string;
  scheduledAt?: string;
  author: User;
  authorId: string;
  authorName: string; // For display purposes
  category?: Category;
  categoryId?: string;
  tags: Tag[];
  viewCount: number;
  likeCount: number;
  commentCount: number;
  readTimeMinutes: number;
  readingTime: number; // Alias for readTimeMinutes for compatibility
  isSticky: boolean;
  isFeatured: boolean;
  isLiked?: boolean; // User-specific like status
  isBookmarked?: boolean; // User-specific bookmark status
  allowComments: boolean;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
  createdAt: string;
  updatedAt: string;
  lastModifiedBy?: User;
  lastModifiedById?: string;
  version: number;
  relatedPosts?: PostSummary[]; // Related posts for recommendations
}

// Post summary interface for lists and related posts
export interface PostSummary {
  id: string;
  title: string;
  excerpt: string;
  slug: string;
  status: PostStatus;
  featuredImage?: string;
  publishedAt?: string;
  author: User;
  authorId: string;
  authorName: string;
  category?: Category;
  categoryId?: string;
  tags: Tag[];
  viewCount: number;
  likeCount: number;
  commentCount: number;
  readTimeMinutes: number;
  readingTime: number;
  isSticky: boolean;
  isFeatured: boolean;
  createdAt: string;
  updatedAt: string;
}

// Category interface matching backend CategoryDto
export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  icon?: string;
  parentId?: string;
  parent?: Category;
  children: Category[];
  postCount: number;
  sortOrder: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

// Tag interface matching backend TagDto
export interface Tag {
  id: string;
  name: string;
  slug: string;
  description?: string;
  color?: string;
  postCount: number;
  createdAt: string;
  updatedAt: string;
}

// PostTag junction entity for many-to-many relationship
export interface PostTag {
  postId: string;
  tagId: string;
  post: Post;
  tag: Tag;
  createdAt: string;
}

// Create post request interface matching backend CreatePostRequest
export interface CreatePostRequest {
  title: string;
  content: string;
  excerpt?: string;
  slug?: string;
  status: PostStatus;
  featuredImage?: string;
  publishedAt?: string;
  scheduledAt?: string;
  categoryId?: string;
  tagIds: string[];
  isSticky?: boolean;
  isFeatured?: boolean;
  allowComments?: boolean;
  metaTitle?: string;
  metaDescription?: string;
  metaKeywords?: string;
}

// Update post request interface matching backend UpdatePostRequest
export interface UpdatePostRequest extends CreatePostRequest {
  id: string;
  version: number;
}

// Create category request interface matching backend CreateCategoryRequest
export interface CreateCategoryRequest {
  name: string;
  slug?: string;
  description?: string;
  color?: string;
  icon?: string;
  parentId?: string;
  sortOrder?: number;
  isActive?: boolean;
}

// Update category request interface matching backend UpdateCategoryRequest
export interface UpdateCategoryRequest extends CreateCategoryRequest {
  id: string;
}

// Create tag request interface matching backend CreateTagRequest
export interface CreateTagRequest {
  name: string;
  slug?: string;
  description?: string;
  color?: string;
}

// Update tag request interface matching backend UpdateTagRequest
export interface UpdateTagRequest extends CreateTagRequest {
  id: string;
}

// Post search and filtering parameters
export interface PostSearchParams {
  query?: string;
  status?: PostStatus[];
  categoryId?: string;
  tagIds?: string[];
  authorId?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: 'title' | 'publishedAt' | 'createdAt' | 'updatedAt' | 'viewCount' | 'likeCount';
  sortOrder?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
  isSticky?: boolean;
  isFeatured?: boolean;
  allowComments?: boolean;
}

// Category search and filtering parameters
export interface CategorySearchParams {
  query?: string;
  parentId?: string;
  isActive?: boolean;
  sortBy?: 'name' | 'sortOrder' | 'postCount' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

// Tag search and filtering parameters
export interface TagSearchParams {
  query?: string;
  sortBy?: 'name' | 'postCount' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

// Paginated response interface matching backend PagedResponse
export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Post list response
export type PostListResponse = PagedResponse<Post>;

// Category list response
export type CategoryListResponse = PagedResponse<Category>;

// Tag list response
export type TagListResponse = PagedResponse<Tag>;

// Post statistics interface
export interface PostStatistics {
  totalPosts: number;
  publishedPosts: number;
  draftPosts: number;
  scheduledPosts: number;
  totalViews: number;
  totalLikes: number;
  totalComments: number;
  averageReadTime: number;
}

// Category statistics interface
export interface CategoryStatistics {
  totalCategories: number;
  categoriesWithPosts: number;
  averagePostsPerCategory: number;
  topCategories: Array<{
    category: Category;
    postCount: number;
  }>;
}

// Tag statistics interface
export interface TagStatistics {
  totalTags: number;
  tagsWithPosts: number;
  averagePostsPerTag: number;
  topTags: Array<{
    tag: Tag;
    postCount: number;
  }>;
}

// File upload interface
export interface FileUploadRequest {
  file: File;
  alt?: string;
  caption?: string;
}

// File upload response interface
export interface FileUploadResponse {
  url: string;
  fileName: string;
  originalFileName: string;
  contentType: string;
  size: number;
  alt?: string;
  caption?: string;
  thumbnailUrl?: string;
  createdAt: string;
}

// Bulk operations interface
export interface BulkOperation {
  action: 'delete' | 'publish' | 'unpublish' | 'archive' | 'changeCategory' | 'addTags' | 'removeTags';
  postIds: string[];
  targetCategoryId?: string;
  targetTagIds?: string[];
}

// Bulk operation result interface
export interface BulkOperationResult {
  success: boolean;
  processedCount: number;
  errorCount: number;
  errors: Array<{
    postId: string;
    error: string;
  }>;
}

// Auto-save draft interface
export interface AutoSaveDraft {
  id: string;
  postId?: string; // null for new posts
  title: string;
  content: string;
  excerpt?: string;
  categoryId?: string;
  tagIds: string[];
  savedAt: string;
  expiresAt: string;
}

// Content revision interface for version history
export interface ContentRevision {
  id: string;
  postId: string;
  title: string;
  content: string;
  excerpt?: string;
  changeNote?: string;
  authorId: string;
  author: User;
  createdAt: string;
  version: number;
}

// SEO analysis result interface
export interface SEOAnalysis {
  score: number;
  issues: Array<{
    type: 'error' | 'warning' | 'suggestion';
    message: string;
    field?: string;
  }>;
  suggestions: string[];
  readabilityScore: number;
  keywordDensity: number;
}

// Table of contents item interface
export interface TOCItem {
  id: string;
  text: string;
  level: number;
  anchor: string;
  children?: TOCItem[];
}

// Reading progress interface
export interface ReadingProgress {
  postId: string;
  userId?: string;
  progress: number; // 0-100 percentage
  lastReadAt: string;
  timeSpent: number; // seconds
  sessionId: string;
}

// Social sharing interface
export interface SocialShare {
  platform: 'twitter' | 'facebook' | 'linkedin' | 'reddit' | 'copy';
  url: string;
  title: string;
  description?: string;
  image?: string;
}

// Comment interface (for future comment system integration)
export interface Comment {
  id: string;
  postId: string;
  authorId?: string;
  authorName: string;
  authorEmail: string;
  authorWebsite?: string;
  content: string;
  parentId?: string;
  parent?: Comment;
  children: Comment[];
  status: 'pending' | 'approved' | 'spam' | 'rejected';
  isAuthorReply: boolean;
  likeCount: number;
  createdAt: string;
  updatedAt: string;
}

// Blog state interface for global state management
export interface BlogState {
  // Current data
  posts: Post[];
  categories: Category[];
  tags: Tag[];

  // Current editing state
  currentPost: Post | null;
  currentDraft: AutoSaveDraft | null;

  // UI state
  isLoading: boolean;
  isSaving: boolean;
  isUploading: boolean;

  // Search and filtering state
  searchParams: PostSearchParams;
  categorySearchParams: CategorySearchParams;
  tagSearchParams: TagSearchParams;

  // Pagination state
  currentPage: number;
  totalPages: number;
  totalCount: number;

  // Selected items for bulk operations
  selectedPostIds: string[];

  // Error handling
  error: string | null;

  // Editor state
  editorMode: 'markdown' | 'visual';
  showPreview: boolean;
  isFullscreen: boolean;

  // Auto-save state
  lastSavedAt: Date | null;
  hasUnsavedChanges: boolean;
  autoSaveEnabled: boolean;

  // Statistics cache
  statistics: PostStatistics | null;
  categoryStatistics: CategoryStatistics | null;
  tagStatistics: TagStatistics | null;
}

// Blog form data interfaces
export interface PostFormData {
  title: string;
  content: string;
  excerpt: string;
  slug: string;
  status: PostStatus;
  featuredImage: string;
  publishedAt: string;
  scheduledAt: string;
  categoryId: string;
  tagIds: string[];
  isSticky: boolean;
  isFeatured: boolean;
  allowComments: boolean;
  metaTitle: string;
  metaDescription: string;
  metaKeywords: string;
}

export interface CategoryFormData {
  name: string;
  slug: string;
  description: string;
  color: string;
  icon: string;
  parentId: string;
  sortOrder: number;
  isActive: boolean;
}

export interface TagFormData {
  name: string;
  slug: string;
  description: string;
  color: string;
}

// Form validation errors
export interface BlogFormErrors {
  title?: string[];
  content?: string[];
  excerpt?: string[];
  slug?: string[];
  status?: string[];
  categoryId?: string[];
  tagIds?: string[];
  metaTitle?: string[];
  metaDescription?: string[];
  featuredImage?: string[];
  publishedAt?: string[];
  scheduledAt?: string[];
}

export interface CategoryFormErrors {
  name?: string[];
  slug?: string[];
  description?: string[];
  parentId?: string[];
  sortOrder?: string[];
}

export interface TagFormErrors {
  name?: string[];
  slug?: string[];
  description?: string[];
}

// Editor configuration interface
export interface EditorConfig {
  theme: 'light' | 'dark';
  fontSize: number;
  lineHeight: number;
  wordWrap: boolean;
  autoSave: boolean;
  autoSaveInterval: number; // seconds
  spellCheck: boolean;
  showLineNumbers: boolean;
  showMinimap: boolean;
  toolbar: string[];
  shortcuts: Record<string, string>;
}

// Content export interface
export interface ContentExport {
  format: 'markdown' | 'html' | 'pdf' | 'docx';
  includeMetadata: boolean;
  includeImages: boolean;
  posts: string[]; // post IDs
}

// Content import interface
export interface ContentImport {
  source: 'wordpress' | 'medium' | 'markdown' | 'json';
  file: File;
  options: {
    preserveUrls: boolean;
    downloadImages: boolean;
    defaultAuthor: string;
    defaultCategory?: string;
    defaultStatus: PostStatus;
  };
}

// Plugin/extension interface for future extensibility
export interface BlogPlugin {
  id: string;
  name: string;
  version: string;
  description: string;
  author: string;
  enabled: boolean;
  settings: Record<string, unknown>;
  hooks: {
    beforeSave?: (post: Post) => Promise<Post>;
    afterSave?: (post: Post) => Promise<void>;
    beforeRender?: (content: string) => Promise<string>;
    afterRender?: (html: string) => Promise<string>;
  };
}

// Analytics/insights interface
export interface BlogAnalytics {
  period: 'week' | 'month' | 'quarter' | 'year';
  pageViews: Array<{
    date: string;
    views: number;
  }>;
  popularPosts: Array<{
    post: Post;
    views: number;
    change: number;
  }>;
  topCategories: Array<{
    category: Category;
    views: number;
    posts: number;
  }>;
  topTags: Array<{
    tag: Tag;
    views: number;
    posts: number;
  }>;
  trafficSources: Array<{
    source: string;
    visitors: number;
    percentage: number;
  }>;
  deviceBreakdown: {
    desktop: number;
    mobile: number;
    tablet: number;
  };
}

// Collaboration/multi-author interfaces
export interface CollaborationInvite {
  id: string;
  postId: string;
  inviterId: string;
  inviterName: string;
  inviteeEmail: string;
  role: 'viewer' | 'editor' | 'author';
  status: 'pending' | 'accepted' | 'declined' | 'expired';
  message?: string;
  expiresAt: string;
  createdAt: string;
}

export interface CollaborationSession {
  id: string;
  postId: string;
  participants: Array<{
    userId: string;
    userName: string;
    avatar?: string;
    role: 'viewer' | 'editor' | 'author';
    isActive: boolean;
    cursor?: {
      line: number;
      column: number;
    };
  }>;
  createdAt: string;
  lastActivity: string;
}

// Webhook interface for integrations
export interface BlogWebhook {
  id: string;
  name: string;
  url: string;
  events: Array<'post.created' | 'post.updated' | 'post.published' | 'post.deleted'>;
  secret?: string;
  isActive: boolean;
  headers?: Record<string, string>;
  createdAt: string;
  lastTriggered?: string;
  deliveryStatus: 'success' | 'failed' | 'pending';
}

// Permission types for blog content
export type BlogPermission =
  | 'create:posts'
  | 'read:posts'
  | 'update:posts'
  | 'delete:posts'
  | 'publish:posts'
  | 'manage:categories'
  | 'manage:tags'
  | 'upload:files'
  | 'manage:comments'
  | 'view:analytics'
  | 'manage:settings';

// User permissions mapping for blog
export const BLOG_USER_PERMISSIONS: Record<UserRole, BlogPermission[]> = {
  [UserRole.User]: ['read:posts'],
  [UserRole.Author]: [
    'create:posts',
    'read:posts',
    'update:posts',
    'publish:posts',
    'upload:files',
    'manage:tags'
  ],
  [UserRole.Admin]: [
    'create:posts',
    'read:posts',
    'update:posts',
    'delete:posts',
    'publish:posts',
    'manage:categories',
    'manage:tags',
    'upload:files',
    'manage:comments',
    'view:analytics',
    'manage:settings'
  ],
};

// Default values and constants
export const DEFAULT_POST_STATUS = PostStatus.Draft;
export const DEFAULT_PAGE_SIZE = 20;
export const MAX_PAGE_SIZE = 100;
export const AUTO_SAVE_INTERVAL = 30; // seconds
export const DRAFT_EXPIRY_DAYS = 7;
export const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB
export const SUPPORTED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
export const SUPPORTED_DOCUMENT_TYPES = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
export const MAX_TITLE_LENGTH = 200;
export const MAX_EXCERPT_LENGTH = 500;
export const MIN_CONTENT_LENGTH = 50;
export const MAX_TAG_COUNT = 10;
export const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

// Export aliases for compatibility with imports that expect different names
export type BlogPost = Post;
export type BlogCategory = Category;
export type BlogTag = Tag;

export default {
  PostStatus,
  BLOG_USER_PERMISSIONS,
  DEFAULT_POST_STATUS,
  DEFAULT_PAGE_SIZE,
  MAX_PAGE_SIZE,
  AUTO_SAVE_INTERVAL,
  DRAFT_EXPIRY_DAYS,
  MAX_FILE_SIZE,
  SUPPORTED_IMAGE_TYPES,
  SUPPORTED_DOCUMENT_TYPES,
  MAX_TITLE_LENGTH,
  MAX_EXCERPT_LENGTH,
  MIN_CONTENT_LENGTH,
  MAX_TAG_COUNT,
  SLUG_PATTERN,
};