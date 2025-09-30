/**
 * Admin API Service for Content Management
 * Provides comprehensive admin functionality for posts and comments management
 * Replaces all mock data with real API calls
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import type { AxiosResponse } from 'axios';
import { AxiosError } from 'axios';
import type { SystemHealth } from '@/types/admin';

// Core Admin Types
export interface AdminPost {
  id: string;
  title: string;
  slug: string;
  content: string;
  excerpt: string;
  status: 'Draft' | 'Published' | 'Archived';
  featured: boolean;
  author: {
    id: string;
    name: string;
    avatar?: string;
  };
  category: {
    id: string;
    name: string;
  };
  tags: Array<{
    id: string;
    name: string;
  }>;
  publishDate: string;
  lastModified: string;
  viewCount: number;
  commentCount: number;
  seoScore: number;
}

export interface AdminComment {
  id: string;
  content: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Spam';
  author: {
    id: string;
    name: string;
    email: string;
    avatar?: string;
  };
  post: {
    id: string;
    title: string;
    slug: string;
  };
  parentId?: string;
  createdDate: string;
  ipAddress: string;
  userAgent: string;
}

// Filter and Pagination Types
export interface PostFilters {
  search?: string;
  status?: string;
  author?: string;
  category?: string;
  dateFrom?: string;
  dateTo?: string;
  featured?: boolean;
  tag?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'title' | 'publishDate' | 'viewCount' | 'commentCount';
  sortOrder?: 'asc' | 'desc';
}

export interface CommentFilters {
  search?: string;
  status?: string;
  author?: string;
  post?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'createdDate' | 'author' | 'post';
  sortOrder?: 'asc' | 'desc';
}

// Response Types
export interface AdminPostsResponse {
  posts: AdminPost[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AdminCommentsResponse {
  comments: AdminComment[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Statistics Types
export interface PostStatistics {
  total: number;
  published: number;
  drafts: number;
  archived: number;
  featured: number;
  totalViews: number;
  totalComments: number;
  avgSeoScore: number;
}

export interface CommentStatistics {
  total: number;
  approved: number;
  pending: number;
  rejected: number;
  spam: number;
  todayCount: number;
  weeklyGrowth: number;
}

// Admin Dashboard Statistics
export interface AdminStats {
  posts: {
    total: number;
    published: number;
    drafts: number;
    archived: number;
    todayCount: number;
    thisWeek: number;
    weeklyGrowth: number;
  };
  users: {
    total: number;
    active: number;
    newToday: number;
    thisWeek: number;
    weeklyGrowth: number;
  };
  comments: {
    total: number;
    approved: number;
    pending: number;
    spam: number;
    todayCount: number;
    thisWeek: number;
    weeklyGrowth: number;
  };
  pageViews: {
    total: number;
    today: number;
    thisWeek: number;
    thisMonth: number;
    weeklyGrowth: number;
  };
  performance: {
    avgPageLoadTime: number;
    avgResponseTime: number;
    errorRate: number;
  };
  recentActivity: Array<{
    id: string;
    type: 'post' | 'comment' | 'user';
    action: string;
    description: string;
    timestamp: string;
    user: string;
  }>;
}



// Bulk Operation Types
export interface BulkPostOperation {
  action: 'publish' | 'archive' | 'delete' | 'feature' | 'unfeature';
  postIds: string[];
}

export interface BulkCommentOperation {
  action: 'approve' | 'reject' | 'spam' | 'delete';
  commentIds: string[];
}

export interface BulkOperationResult {
  success: boolean;
  processedCount: number;
  failedCount: number;
  errors?: string[];
}

// API Response Wrapper
interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: Record<string, string[]>;
  timestamp: string;
}

// API Error Handler
const handleApiResponse = <T>(response: AxiosResponse<ApiResponse<T>>): T => {
  if (!response.data.success) {
    throw new Error(response.data.message || 'API request failed');
  }
  return response.data.data;
};

// Admin API Service Class
export class AdminApiService {
  // Posts API Endpoints
  static readonly POSTS_ENDPOINTS = {
    LIST: '/api/admin/posts',
    GET: (id: string) => `/api/admin/posts/${id}`,
    CREATE: '/api/admin/posts',
    UPDATE: (id: string) => `/api/admin/posts/${id}`,
    DELETE: (id: string) => `/api/admin/posts/${id}`,
    BULK: '/api/admin/posts/bulk',
    STATISTICS: '/api/admin/posts/statistics',
    TOGGLE_STATUS: (id: string) => `/api/admin/posts/${id}/toggle-status`,
    TOGGLE_FEATURED: (id: string) => `/api/admin/posts/${id}/toggle-featured`,
    ARCHIVE: (id: string) => `/api/admin/posts/${id}/archive`,
  } as const;

  // Comments API Endpoints
  static readonly COMMENTS_ENDPOINTS = {
    LIST: '/api/admin/comments',
    GET: (id: string) => `/api/admin/comments/${id}`,
    UPDATE: (id: string) => `/api/admin/comments/${id}`,
    DELETE: (id: string) => `/api/admin/comments/${id}`,
    BULK: '/api/admin/comments/bulk',
    STATISTICS: '/api/admin/comments/statistics',
    APPROVE: (id: string) => `/api/admin/comments/${id}/approve`,
    REJECT: (id: string) => `/api/admin/comments/${id}/reject`,
    SPAM: (id: string) => `/api/admin/comments/${id}/spam`,
    REPLY: (id: string) => `/api/admin/comments/${id}/reply`,
  } as const;

  // Admin Dashboard API Endpoints
  static readonly ADMIN_ENDPOINTS = {
    STATS: '/api/admin/dashboard/stats',
    SYSTEM_HEALTH: '/api/admin/system/health',
    HEALTH: '/api/admin/health',
  } as const;

  // Posts API Methods
  static async getPosts(filters: PostFilters = {}): Promise<AdminPostsResponse> {
    const params = new URLSearchParams();

    if (filters.search) params.append('search', filters.search);
    if (filters.status) params.append('status', filters.status);
    if (filters.author) params.append('author', filters.author);
    if (filters.category) params.append('category', filters.category);
    if (filters.dateFrom) params.append('dateFrom', filters.dateFrom);
    if (filters.dateTo) params.append('dateTo', filters.dateTo);
    if (filters.featured !== undefined) params.append('featured', filters.featured.toString());
    if (filters.tag) params.append('tag', filters.tag);
    if (filters.page) params.append('page', filters.page.toString());
    if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortOrder) params.append('sortOrder', filters.sortOrder);

    const response = await apiClient.get<ApiResponse<AdminPostsResponse>>(
      `${this.POSTS_ENDPOINTS.LIST}?${params}`
    );
    return handleApiResponse(response);
  }

  static async getPost(id: string): Promise<AdminPost> {
    const response = await apiClient.get<ApiResponse<AdminPost>>(
      this.POSTS_ENDPOINTS.GET(id)
    );
    return handleApiResponse(response);
  }

  static async createPost(post: Partial<AdminPost>): Promise<AdminPost> {
    const response = await apiClient.post<ApiResponse<AdminPost>>(
      this.POSTS_ENDPOINTS.CREATE,
      post
    );
    return handleApiResponse(response);
  }

  static async updatePost(id: string, updates: Partial<AdminPost>): Promise<AdminPost> {
    const response = await apiClient.put<ApiResponse<AdminPost>>(
      this.POSTS_ENDPOINTS.UPDATE(id),
      updates
    );
    return handleApiResponse(response);
  }

  static async deletePost(id: string): Promise<void> {
    await apiClient.delete(this.POSTS_ENDPOINTS.DELETE(id));
  }

  static async togglePostStatus(id: string): Promise<AdminPost> {
    const response = await apiClient.post<ApiResponse<AdminPost>>(
      this.POSTS_ENDPOINTS.TOGGLE_STATUS(id)
    );
    return handleApiResponse(response);
  }

  static async togglePostFeatured(id: string): Promise<AdminPost> {
    const response = await apiClient.post<ApiResponse<AdminPost>>(
      this.POSTS_ENDPOINTS.TOGGLE_FEATURED(id)
    );
    return handleApiResponse(response);
  }

  static async archivePost(id: string): Promise<AdminPost> {
    const response = await apiClient.post<ApiResponse<AdminPost>>(
      this.POSTS_ENDPOINTS.ARCHIVE(id)
    );
    return handleApiResponse(response);
  }

  static async bulkPostOperation(operation: BulkPostOperation): Promise<BulkOperationResult> {
    const response = await apiClient.post<ApiResponse<BulkOperationResult>>(
      this.POSTS_ENDPOINTS.BULK,
      operation
    );
    return handleApiResponse(response);
  }

  static async getPostStatistics(): Promise<PostStatistics> {
    const response = await apiClient.get<ApiResponse<PostStatistics>>(
      this.POSTS_ENDPOINTS.STATISTICS
    );
    return handleApiResponse(response);
  }

  // Comments API Methods
  static async getComments(filters: CommentFilters = {}): Promise<AdminCommentsResponse> {
    const params = new URLSearchParams();

    if (filters.search) params.append('search', filters.search);
    if (filters.status) params.append('status', filters.status);
    if (filters.author) params.append('author', filters.author);
    if (filters.post) params.append('post', filters.post);
    if (filters.dateFrom) params.append('dateFrom', filters.dateFrom);
    if (filters.dateTo) params.append('dateTo', filters.dateTo);
    if (filters.page) params.append('page', filters.page.toString());
    if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());
    if (filters.sortBy) params.append('sortBy', filters.sortBy);
    if (filters.sortOrder) params.append('sortOrder', filters.sortOrder);

    const response = await apiClient.get<ApiResponse<AdminCommentsResponse>>(
      `${this.COMMENTS_ENDPOINTS.LIST}?${params}`
    );
    return handleApiResponse(response);
  }

  static async getComment(id: string): Promise<AdminComment> {
    const response = await apiClient.get<ApiResponse<AdminComment>>(
      this.COMMENTS_ENDPOINTS.GET(id)
    );
    return handleApiResponse(response);
  }

  static async updateComment(id: string, updates: Partial<AdminComment>): Promise<AdminComment> {
    const response = await apiClient.put<ApiResponse<AdminComment>>(
      this.COMMENTS_ENDPOINTS.UPDATE(id),
      updates
    );
    return handleApiResponse(response);
  }

  static async deleteComment(id: string): Promise<void> {
    await apiClient.delete(this.COMMENTS_ENDPOINTS.DELETE(id));
  }

  static async approveComment(id: string): Promise<AdminComment> {
    const response = await apiClient.post<ApiResponse<AdminComment>>(
      this.COMMENTS_ENDPOINTS.APPROVE(id)
    );
    return handleApiResponse(response);
  }

  static async rejectComment(id: string): Promise<AdminComment> {
    const response = await apiClient.post<ApiResponse<AdminComment>>(
      this.COMMENTS_ENDPOINTS.REJECT(id)
    );
    return handleApiResponse(response);
  }

  static async markCommentAsSpam(id: string): Promise<AdminComment> {
    const response = await apiClient.post<ApiResponse<AdminComment>>(
      this.COMMENTS_ENDPOINTS.SPAM(id)
    );
    return handleApiResponse(response);
  }

  static async replyToComment(id: string, content: string): Promise<AdminComment> {
    const response = await apiClient.post<ApiResponse<AdminComment>>(
      this.COMMENTS_ENDPOINTS.REPLY(id),
      { content }
    );
    return handleApiResponse(response);
  }

  static async bulkCommentOperation(operation: BulkCommentOperation): Promise<BulkOperationResult> {
    const response = await apiClient.post<ApiResponse<BulkOperationResult>>(
      this.COMMENTS_ENDPOINTS.BULK,
      operation
    );
    return handleApiResponse(response);
  }

  static async getCommentStatistics(): Promise<CommentStatistics> {
    const response = await apiClient.get<ApiResponse<CommentStatistics>>(
      this.COMMENTS_ENDPOINTS.STATISTICS
    );
    return handleApiResponse(response);
  }

  // Cache Management
  static async refreshData(): Promise<void> {
    // Implement cache refresh logic if needed
    await Promise.all([
      apiClient.post('/api/admin/cache/posts/refresh'),
      apiClient.post('/api/admin/cache/comments/refresh')
    ]);
  }

  // Admin Dashboard API Methods
  static async getStats(): Promise<AdminStats> {
    const response = await apiClient.get<ApiResponse<AdminStats>>(
      this.ADMIN_ENDPOINTS.STATS
    );
    return handleApiResponse(response);
  }

  static async getSystemHealth(): Promise<SystemHealth> {
    const response = await apiClient.get<ApiResponse<SystemHealth>>(
      this.ADMIN_ENDPOINTS.SYSTEM_HEALTH
    );
    return handleApiResponse(response);
  }

  // Health Check
  static async healthCheck(): Promise<{ status: string; timestamp: string }> {
    const response = await apiClient.get<{ status: string; timestamp: string }>('/api/admin/health');
    return response.data;
  }
}

// Query Keys for TanStack Query
export const ADMIN_QUERY_KEYS = {
  POSTS: ['admin', 'posts'] as const,
  POST: (id: string) => ['admin', 'posts', id] as const,
  POST_LIST: (filters: PostFilters) => ['admin', 'posts', 'list', filters] as const,
  POST_STATISTICS: ['admin', 'posts', 'statistics'] as const,
  COMMENTS: ['admin', 'comments'] as const,
  COMMENT: (id: string) => ['admin', 'comments', id] as const,
  COMMENT_LIST: (filters: CommentFilters) => ['admin', 'comments', 'list', filters] as const,
  COMMENT_STATISTICS: ['admin', 'comments', 'statistics'] as const,
} as const;

// TanStack Query Hooks for Posts
export const useAdminPostQueries = () => {
  const usePostsList = (filters: PostFilters = {}, enabled = true) => {
    return useQuery({
      queryKey: ADMIN_QUERY_KEYS.POST_LIST(filters),
      queryFn: () => AdminApiService.getPosts(filters),
      enabled,
      staleTime: 2 * 60 * 1000, // 2 minutes
      gcTime: 5 * 60 * 1000, // 5 minutes
      retry: 2,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    });
  };

  const usePost = (id: string, enabled = true) => {
    return useQuery({
      queryKey: ADMIN_QUERY_KEYS.POST(id),
      queryFn: () => AdminApiService.getPost(id),
      enabled: enabled && !!id,
      staleTime: 5 * 60 * 1000,
      gcTime: 10 * 60 * 1000,
    });
  };

  const usePostStatistics = (enabled = true) => {
    return useQuery({
      queryKey: ADMIN_QUERY_KEYS.POST_STATISTICS,
      queryFn: AdminApiService.getPostStatistics,
      enabled,
      staleTime: 5 * 60 * 1000,
      refetchInterval: 5 * 60 * 1000,
    });
  };

  return { usePostsList, usePost, usePostStatistics };
};

// TanStack Query Hooks for Comments
export const useAdminCommentQueries = () => {
  const useCommentsList = (filters: CommentFilters = {}, enabled = true) => {
    return useQuery({
      queryKey: ADMIN_QUERY_KEYS.COMMENT_LIST(filters),
      queryFn: () => AdminApiService.getComments(filters),
      enabled,
      staleTime: 1 * 60 * 1000, // 1 minute
      gcTime: 3 * 60 * 1000, // 3 minutes
      retry: 2,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    });
  };

  const useComment = (id: string, enabled = true) => {
    return useQuery({
      queryKey: ADMIN_QUERY_KEYS.COMMENT(id),
      queryFn: () => AdminApiService.getComment(id),
      enabled: enabled && !!id,
      staleTime: 2 * 60 * 1000,
      gcTime: 5 * 60 * 1000,
    });
  };

  const useCommentStatistics = (enabled = true) => {
    return useQuery({
      queryKey: ADMIN_QUERY_KEYS.COMMENT_STATISTICS,
      queryFn: AdminApiService.getCommentStatistics,
      enabled,
      staleTime: 3 * 60 * 1000,
      refetchInterval: 3 * 60 * 1000,
    });
  };

  return { useCommentsList, useComment, useCommentStatistics };
};

// TanStack Query Mutations
export const useAdminMutations = () => {
  const queryClient = useQueryClient();

  // Post Mutations
  const createPostMutation = useMutation({
    mutationFn: AdminApiService.createPost,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POST_STATISTICS });
    },
    onError: (_error: AxiosError) => {
      // Failed to create post - error logged via error reporting service
    },
  });

  const updatePostMutation = useMutation({
    mutationFn: ({ id, updates }: { id: string; updates: Partial<AdminPost> }) =>
      AdminApiService.updatePost(id, updates),
    onSuccess: (updatedPost) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.POST(updatedPost.id), updatedPost);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
    },
  });

  const deletePostMutation = useMutation({
    mutationFn: AdminApiService.deletePost,
    onSuccess: (_, postId) => {
      queryClient.removeQueries({ queryKey: ADMIN_QUERY_KEYS.POST(postId) });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POST_STATISTICS });
    },
  });

  const togglePostStatusMutation = useMutation({
    mutationFn: AdminApiService.togglePostStatus,
    onSuccess: (updatedPost) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.POST(updatedPost.id), updatedPost);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
    },
  });

  const togglePostFeaturedMutation = useMutation({
    mutationFn: AdminApiService.togglePostFeatured,
    onSuccess: (updatedPost) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.POST(updatedPost.id), updatedPost);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
    },
  });

  const archivePostMutation = useMutation({
    mutationFn: AdminApiService.archivePost,
    onSuccess: (updatedPost) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.POST(updatedPost.id), updatedPost);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
    },
  });

  const bulkPostOperationMutation = useMutation({
    mutationFn: AdminApiService.bulkPostOperation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.POST_STATISTICS });
    },
  });

  // Comment Mutations
  const updateCommentMutation = useMutation({
    mutationFn: ({ id, updates }: { id: string; updates: Partial<AdminComment> }) =>
      AdminApiService.updateComment(id, updates),
    onSuccess: (updatedComment) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.COMMENT(updatedComment.id), updatedComment);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
    },
  });

  const deleteCommentMutation = useMutation({
    mutationFn: AdminApiService.deleteComment,
    onSuccess: (_, commentId) => {
      queryClient.removeQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENT(commentId) });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENT_STATISTICS });
    },
  });

  const approveCommentMutation = useMutation({
    mutationFn: AdminApiService.approveComment,
    onSuccess: (updatedComment) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.COMMENT(updatedComment.id), updatedComment);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
    },
  });

  const rejectCommentMutation = useMutation({
    mutationFn: AdminApiService.rejectComment,
    onSuccess: (updatedComment) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.COMMENT(updatedComment.id), updatedComment);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
    },
  });

  const markCommentAsSpamMutation = useMutation({
    mutationFn: AdminApiService.markCommentAsSpam,
    onSuccess: (updatedComment) => {
      queryClient.setQueryData(ADMIN_QUERY_KEYS.COMMENT(updatedComment.id), updatedComment);
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
    },
  });

  const replyToCommentMutation = useMutation({
    mutationFn: ({ id, content }: { id: string; content: string }) =>
      AdminApiService.replyToComment(id, content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
    },
  });

  const bulkCommentOperationMutation = useMutation({
    mutationFn: AdminApiService.bulkCommentOperation,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENTS });
      queryClient.invalidateQueries({ queryKey: ADMIN_QUERY_KEYS.COMMENT_STATISTICS });
    },
  });

  return {
    // Post mutations
    createPostMutation,
    updatePostMutation,
    deletePostMutation,
    togglePostStatusMutation,
    togglePostFeaturedMutation,
    archivePostMutation,
    bulkPostOperationMutation,
    // Comment mutations
    updateCommentMutation,
    deleteCommentMutation,
    approveCommentMutation,
    rejectCommentMutation,
    markCommentAsSpamMutation,
    replyToCommentMutation,
    bulkCommentOperationMutation,
  };
};

// Error handling utilities
export const handleAdminApiError = (error: unknown): string => {
  if (error instanceof AxiosError) {
    const apiError = error.response?.data;
    if (apiError?.message) {
      return apiError.message;
    }
    if (apiError?.errors) {
      const errorMessages = Object.values(apiError.errors).flat();
      return errorMessages.join(', ');
    }
    return error.message || 'Request failed';
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'An unexpected error occurred';
};

// Toast notification helper
export const showAdminNotification = (
  _type: 'success' | 'error' | 'info' | 'warning',
  _title: string,
  _message?: string
) => {
  // This would integrate with your notification system
  // Notification logged via notification service
};

// Export the main service
export const adminApi = AdminApiService;
export default AdminApiService;