// @ts-nocheck
/**
 * Common types used across the application
 */

export interface BaseEntity {
  id: string;
  createdAt: string;
  updatedAt: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

export interface ErrorResponse {
  success: false;
  message: string;
  errors?: string[];
  statusCode?: number;
}

export type Theme = 'light' | 'dark' | 'auto';

export type Language = 'en' | 'zh' | 'zh-cn';

export type SortOrder = 'asc' | 'desc';

export interface FilterOptions {
  search?: string;
  category?: string;
  tags?: string[];
  dateFrom?: Date;
  dateTo?: Date;
  sortBy?: string;
  sortOrder?: SortOrder;
}

export interface User {
  id: string;
  username: string;
  email: string;
  avatar?: string;
  role: 'User' | 'Author' | 'Admin';
  createdAt: string;
  updatedAt: string;
}

export interface Comment extends BaseEntity {
  postId: string;
  authorId: string;
  author: User;
  content: string;
  parentId?: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  likeCount: number;
  replyCount: number;
  isEdited: boolean;
  editedAt?: string;
}

export interface TypingUser {
  id: string;
  username: string;
  avatar?: string;
}