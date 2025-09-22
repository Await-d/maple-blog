// @ts-nocheck
/**
 * Blog Posts API Service using TanStack Query
 * Handles all blog post-related API communications with the backend
 */

import { useMutation, useQuery, useQueryClient, useInfiniteQuery } from '@tanstack/react-query';
import { AxiosError, AxiosResponse, AxiosProgressEvent } from 'axios';
import {
  Post,
  PostListResponse,
  CreatePostRequest,
  UpdatePostRequest,
  PostSearchParams,
  PostStatistics,
  BulkOperation,
  BulkOperationResult,
  ContentRevision,
  AutoSaveDraft,
  FileUploadRequest,
  FileUploadResponse,
  ContentExport,
  ContentImport,
  BlogAnalytics,
  ApiResponse,
  OperationResult,
  OperationResultWithData,
  DEFAULT_PAGE_SIZE,
} from '../../types/blog';
import { apiClient } from '../api/client';
import { useBlogStore } from '../../stores/blogStore';

// API endpoints
const API_ENDPOINTS = {
  POSTS: {
    LIST: '/api/blog/posts',
    CREATE: '/api/blog/posts',
    GET: (id: string) => `/api/blog/posts/${id}`,
    UPDATE: (id: string) => `/api/blog/posts/${id}`,
    DELETE: (id: string) => `/api/blog/posts/${id}`,
    PUBLISH: (id: string) => `/api/blog/posts/${id}/publish`,
    UNPUBLISH: (id: string) => `/api/blog/posts/${id}/unpublish`,
    ARCHIVE: (id: string) => `/api/blog/posts/${id}/archive`,
    DUPLICATE: (id: string) => `/api/blog/posts/${id}/duplicate`,
    BULK: '/api/blog/posts/bulk',
    SEARCH: '/api/blog/posts/search',
    STATISTICS: '/api/blog/posts/statistics',
    REVISIONS: (id: string) => `/api/blog/posts/${id}/revisions`,
    RESTORE_REVISION: (id: string, revisionId: string) => `/api/blog/posts/${id}/revisions/${revisionId}/restore`,
  },
  DRAFTS: {
    SAVE: '/api/blog/drafts',
    LIST: '/api/blog/drafts',
    GET: (id: string) => `/api/blog/drafts/${id}`,
    DELETE: (id: string) => `/api/blog/drafts/${id}`,
  },
  FILES: {
    UPLOAD: '/api/blog/files/upload',
    DELETE: (id: string) => `/api/blog/files/${id}`,
    OPTIMIZE: '/api/blog/files/optimize',
  },
  EXPORT: {
    POSTS: '/api/blog/export',
  },
  IMPORT: {
    POSTS: '/api/blog/import',
  },
  ANALYTICS: {
    OVERVIEW: '/api/blog/analytics/overview',
    POSTS: '/api/blog/analytics/posts',
  },
} as const;

// Query keys for TanStack Query caching
export const BLOG_QUERY_KEYS = {
  POSTS: ['blog', 'posts'] as const,
  POST: (id: string) => ['blog', 'posts', id] as const,
  POST_LIST: (params: PostSearchParams) => ['blog', 'posts', 'list', params] as const,
  POST_SEARCH: (query: string, params: PostSearchParams) => ['blog', 'posts', 'search', query, params] as const,
  POST_STATISTICS: ['blog', 'posts', 'statistics'] as const,
  POST_REVISIONS: (id: string) => ['blog', 'posts', id, 'revisions'] as const,
  DRAFTS: ['blog', 'drafts'] as const,
  DRAFT: (id: string) => ['blog', 'drafts', id] as const,
  ANALYTICS: ['blog', 'analytics'] as const,
  ANALYTICS_POSTS: (period: string) => ['blog', 'analytics', 'posts', period] as const,
} as const;

// API response handler
const handleApiResponse = <T>(response: AxiosResponse<ApiResponse<T>>): T => {
  if (!response.data.success) {
    throw new Error(response.data.message || 'API request failed');
  }
  return response.data.data!;
};

// Blog posts API functions
export const blogPostsApi = {
  // Get paginated list of posts
  getPosts: async (params: PostSearchParams = {}): Promise<PostListResponse> => {
    const searchParams = new URLSearchParams();

    // Add pagination parameters
    searchParams.append('page', (params.page || 1).toString());
    searchParams.append('pageSize', (params.pageSize || DEFAULT_PAGE_SIZE).toString());

    // Add search and filtering parameters
    if (params.query) searchParams.append('query', params.query);
    if (params.status && params.status.length > 0) {
      params.status.forEach(status => searchParams.append('status', status.toString()));
    }
    if (params.categoryId) searchParams.append('categoryId', params.categoryId);
    if (params.tagIds && params.tagIds.length > 0) {
      params.tagIds.forEach(tagId => searchParams.append('tagIds', tagId));
    }
    if (params.authorId) searchParams.append('authorId', params.authorId);
    if (params.dateFrom) searchParams.append('dateFrom', params.dateFrom);
    if (params.dateTo) searchParams.append('dateTo', params.dateTo);
    if (params.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params.sortOrder) searchParams.append('sortOrder', params.sortOrder);
    if (params.isSticky !== undefined) searchParams.append('isSticky', params.isSticky.toString());
    if (params.isFeatured !== undefined) searchParams.append('isFeatured', params.isFeatured.toString());
    if (params.allowComments !== undefined) searchParams.append('allowComments', params.allowComments.toString());

    const response = await apiClient.get<ApiResponse<PostListResponse>>(
      `${API_ENDPOINTS.POSTS.LIST}?${searchParams}`
    );
    return handleApiResponse(response);
  },

  // Get single post by ID
  getPost: async (id: string): Promise<Post> => {
    const response = await apiClient.get<ApiResponse<Post>>(API_ENDPOINTS.POSTS.GET(id));
    return handleApiResponse(response);
  },

  // Create new post
  createPost: async (data: CreatePostRequest): Promise<Post> => {
    const response = await apiClient.post<ApiResponse<Post>>(API_ENDPOINTS.POSTS.CREATE, data);
    return handleApiResponse(response);
  },

  // Update existing post
  updatePost: async (data: UpdatePostRequest): Promise<Post> => {
    const response = await apiClient.put<ApiResponse<Post>>(API_ENDPOINTS.POSTS.UPDATE(data.id), data);
    return handleApiResponse(response);
  },

  // Delete post
  deletePost: async (id: string): Promise<void> => {
    await apiClient.delete(API_ENDPOINTS.POSTS.DELETE(id));
  },

  // Publish post
  publishPost: async (id: string, publishedAt?: string): Promise<Post> => {
    const response = await apiClient.post<ApiResponse<Post>>(
      API_ENDPOINTS.POSTS.PUBLISH(id),
      publishedAt ? { publishedAt } : undefined
    );
    return handleApiResponse(response);
  },

  // Unpublish post
  unpublishPost: async (id: string): Promise<Post> => {
    const response = await apiClient.post<ApiResponse<Post>>(API_ENDPOINTS.POSTS.UNPUBLISH(id));
    return handleApiResponse(response);
  },

  // Archive post
  archivePost: async (id: string): Promise<Post> => {
    const response = await apiClient.post<ApiResponse<Post>>(API_ENDPOINTS.POSTS.ARCHIVE(id));
    return handleApiResponse(response);
  },

  // Duplicate post
  duplicatePost: async (id: string): Promise<Post> => {
    const response = await apiClient.post<ApiResponse<Post>>(API_ENDPOINTS.POSTS.DUPLICATE(id));
    return handleApiResponse(response);
  },

  // Bulk operations
  bulkOperation: async (operation: BulkOperation): Promise<BulkOperationResult> => {
    const response = await apiClient.post<ApiResponse<BulkOperationResult>>(
      API_ENDPOINTS.POSTS.BULK,
      operation
    );
    return handleApiResponse(response);
  },

  // Search posts
  searchPosts: async (query: string, params: PostSearchParams = {}): Promise<PostListResponse> => {
    const searchParams = new URLSearchParams();
    searchParams.append('q', query);
    searchParams.append('page', (params.page || 1).toString());
    searchParams.append('pageSize', (params.pageSize || DEFAULT_PAGE_SIZE).toString());

    if (params.status && params.status.length > 0) {
      params.status.forEach(status => searchParams.append('status', status.toString()));
    }
    if (params.categoryId) searchParams.append('categoryId', params.categoryId);
    if (params.tagIds && params.tagIds.length > 0) {
      params.tagIds.forEach(tagId => searchParams.append('tagIds', tagId));
    }
    if (params.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params.sortOrder) searchParams.append('sortOrder', params.sortOrder);

    const response = await apiClient.get<ApiResponse<PostListResponse>>(
      `${API_ENDPOINTS.POSTS.SEARCH}?${searchParams}`
    );
    return handleApiResponse(response);
  },

  // Get post statistics
  getPostStatistics: async (): Promise<PostStatistics> => {
    const response = await apiClient.get<ApiResponse<PostStatistics>>(API_ENDPOINTS.POSTS.STATISTICS);
    return handleApiResponse(response);
  },

  // Get post revisions
  getPostRevisions: async (id: string): Promise<ContentRevision[]> => {
    const response = await apiClient.get<ApiResponse<ContentRevision[]>>(API_ENDPOINTS.POSTS.REVISIONS(id));
    return handleApiResponse(response);
  },

  // Restore post revision
  restoreRevision: async (postId: string, revisionId: string): Promise<Post> => {
    const response = await apiClient.post<ApiResponse<Post>>(
      API_ENDPOINTS.POSTS.RESTORE_REVISION(postId, revisionId)
    );
    return handleApiResponse(response);
  },

  // Draft management
  saveDraft: async (draft: Partial<CreatePostRequest>): Promise<AutoSaveDraft> => {
    const response = await apiClient.post<ApiResponse<AutoSaveDraft>>(API_ENDPOINTS.DRAFTS.SAVE, draft);
    return handleApiResponse(response);
  },

  getDrafts: async (): Promise<AutoSaveDraft[]> => {
    const response = await apiClient.get<ApiResponse<AutoSaveDraft[]>>(API_ENDPOINTS.DRAFTS.LIST);
    return handleApiResponse(response);
  },

  getDraft: async (id: string): Promise<AutoSaveDraft> => {
    const response = await apiClient.get<ApiResponse<AutoSaveDraft>>(API_ENDPOINTS.DRAFTS.GET(id));
    return handleApiResponse(response);
  },

  deleteDraft: async (id: string): Promise<void> => {
    await apiClient.delete(API_ENDPOINTS.DRAFTS.DELETE(id));
  },

  // File management
  uploadFile: async (
    file: File,
    onProgress?: (progressEvent: AxiosProgressEvent) => void
  ): Promise<FileUploadResponse> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<ApiResponse<FileUploadResponse>>(
      API_ENDPOINTS.FILES.UPLOAD,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: onProgress,
      }
    );
    return handleApiResponse(response);
  },

  deleteFile: async (fileId: string): Promise<void> => {
    await apiClient.delete(API_ENDPOINTS.FILES.DELETE(fileId));
  },

  optimizeImages: async (imageIds: string[]): Promise<void> => {
    await apiClient.post(API_ENDPOINTS.FILES.OPTIMIZE, { imageIds });
  },

  // Export/Import
  exportContent: async (exportConfig: ContentExport): Promise<Blob> => {
    const response = await apiClient.post(API_ENDPOINTS.EXPORT.POSTS, exportConfig, {
      responseType: 'blob',
    });
    return response.data;
  },

  importContent: async (importConfig: ContentImport): Promise<OperationResult> => {
    const formData = new FormData();
    formData.append('file', importConfig.file);
    formData.append('source', importConfig.source);
    formData.append('options', JSON.stringify(importConfig.options));

    const response = await apiClient.post<ApiResponse<OperationResult>>(
      API_ENDPOINTS.IMPORT.POSTS,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }
    );
    return handleApiResponse(response);
  },

  // Analytics
  getAnalytics: async (period: 'week' | 'month' | 'quarter' | 'year' = 'month'): Promise<BlogAnalytics> => {
    const response = await apiClient.get<ApiResponse<BlogAnalytics>>(
      `${API_ENDPOINTS.ANALYTICS.OVERVIEW}?period=${period}`
    );
    return handleApiResponse(response);
  },

  getPostAnalytics: async (postId: string, period: string = 'month'): Promise<any> => {
    const response = await apiClient.get<ApiResponse<any>>(
      `${API_ENDPOINTS.ANALYTICS.POSTS}/${postId}?period=${period}`
    );
    return handleApiResponse(response);
  },
};

// TanStack Query hooks for blog posts
export const useBlogPostQueries = () => {
  // Query for paginated posts list
  const usePostsList = (params: PostSearchParams = {}, enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.POST_LIST(params),
      queryFn: () => blogPostsApi.getPosts(params),
      enabled,
      staleTime: 2 * 60 * 1000, // 2 minutes
      gcTime: 5 * 60 * 1000, // 5 minutes
    });
  };

  // Infinite query for posts (for infinite scroll)
  const usePostsInfinite = (params: PostSearchParams = {}) => {
    return useInfiniteQuery({
      queryKey: BLOG_QUERY_KEYS.POST_LIST(params),
      queryFn: ({ pageParam = 1 }) =>
        blogPostsApi.getPosts({ ...params, page: pageParam }),
      getNextPageParam: (lastPage) =>
        lastPage.hasNextPage ? lastPage.pageNumber + 1 : undefined,
      initialPageParam: 1,
      staleTime: 2 * 60 * 1000,
    });
  };

  // Query for single post
  const usePost = (id: string, enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.POST(id),
      queryFn: () => blogPostsApi.getPost(id),
      enabled: enabled && !!id,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    });
  };

  // Query for post search results
  const usePostsSearch = (query: string, params: PostSearchParams = {}, enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.POST_SEARCH(query, params),
      queryFn: () => blogPostsApi.searchPosts(query, params),
      enabled: enabled && !!query.trim(),
      staleTime: 1 * 60 * 1000, // 1 minute for search results
    });
  };

  // Query for post statistics
  const usePostStatistics = (enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.POST_STATISTICS,
      queryFn: blogPostsApi.getPostStatistics,
      enabled,
      staleTime: 5 * 60 * 1000, // 5 minutes
      refetchInterval: 5 * 60 * 1000, // Auto-refetch every 5 minutes
    });
  };

  // Query for post revisions
  const usePostRevisions = (postId: string, enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.POST_REVISIONS(postId),
      queryFn: () => blogPostsApi.getPostRevisions(postId),
      enabled: enabled && !!postId,
      staleTime: 1 * 60 * 1000,
    });
  };

  // Query for drafts
  const useDrafts = (enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.DRAFTS,
      queryFn: blogPostsApi.getDrafts,
      enabled,
      staleTime: 30 * 1000, // 30 seconds for drafts
    });
  };

  // Query for single draft
  const useDraft = (id: string, enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.DRAFT(id),
      queryFn: () => blogPostsApi.getDraft(id),
      enabled: enabled && !!id,
      staleTime: 30 * 1000,
    });
  };

  // Query for analytics
  const useAnalytics = (period: 'week' | 'month' | 'quarter' | 'year' = 'month', enabled = true) => {
    return useQuery({
      queryKey: BLOG_QUERY_KEYS.ANALYTICS_POSTS(period),
      queryFn: () => blogPostsApi.getAnalytics(period),
      enabled,
      staleTime: 10 * 60 * 1000, // 10 minutes for analytics
      refetchInterval: 10 * 60 * 1000,
    });
  };

  return {
    usePostsList,
    usePostsInfinite,
    usePost,
    usePostsSearch,
    usePostStatistics,
    usePostRevisions,
    useDrafts,
    useDraft,
    useAnalytics,
  };
};

// TanStack Query mutations for blog posts
export const useBlogPostMutations = () => {
  const queryClient = useQueryClient();
  const blogStore = useBlogStore();

  // Create post mutation
  const createPostMutation = useMutation({
    mutationFn: blogPostsApi.createPost,
    onMutate: async (newPost) => {
      await queryClient.cancelQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
      blogStore.setLoadingState('savingPost', true);
    },
    onSuccess: (createdPost) => {
      blogStore.addPost(createdPost);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POST_STATISTICS });
      blogStore.markSaved();
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to create post');
    },
    onSettled: () => {
      blogStore.setLoadingState('savingPost', false);
    },
  });

  // Update post mutation
  const updatePostMutation = useMutation({
    mutationFn: blogPostsApi.updatePost,
    onMutate: async (updatedPost) => {
      await queryClient.cancelQueries({ queryKey: BLOG_QUERY_KEYS.POST(updatedPost.id) });
      blogStore.setLoadingState('savingPost', true);

      // Optimistic update
      const previousPost = queryClient.getQueryData(BLOG_QUERY_KEYS.POST(updatedPost.id));
      if (previousPost) {
        queryClient.setQueryData(BLOG_QUERY_KEYS.POST(updatedPost.id), {
          ...previousPost,
          ...updatedPost,
          updatedAt: new Date().toISOString(),
        });
      }

      return { previousPost };
    },
    onSuccess: (updatedPost) => {
      blogStore.updatePost(updatedPost);
      queryClient.setQueryData(BLOG_QUERY_KEYS.POST(updatedPost.id), updatedPost);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
      blogStore.markSaved();
    },
    onError: (error, variables, context) => {
      // Revert optimistic update
      if (context?.previousPost) {
        queryClient.setQueryData(BLOG_QUERY_KEYS.POST(variables.id), context.previousPost);
      }
      blogStore.setError(error instanceof Error ? error.message : 'Failed to update post');
    },
    onSettled: () => {
      blogStore.setLoadingState('savingPost', false);
    },
  });

  // Delete post mutation
  const deletePostMutation = useMutation({
    mutationFn: blogPostsApi.deletePost,
    onMutate: async (postId) => {
      blogStore.setLoadingState('deletingPost', true);
    },
    onSuccess: (_, postId) => {
      blogStore.removePost(postId);
      queryClient.removeQueries({ queryKey: BLOG_QUERY_KEYS.POST(postId) });
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POST_STATISTICS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to delete post');
    },
    onSettled: () => {
      blogStore.setLoadingState('deletingPost', false);
    },
  });

  // Publish post mutation
  const publishPostMutation = useMutation({
    mutationFn: ({ id, publishedAt }: { id: string; publishedAt?: string }) =>
      blogPostsApi.publishPost(id, publishedAt),
    onSuccess: (publishedPost) => {
      blogStore.updatePost(publishedPost);
      queryClient.setQueryData(BLOG_QUERY_KEYS.POST(publishedPost.id), publishedPost);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to publish post');
    },
  });

  // Unpublish post mutation
  const unpublishPostMutation = useMutation({
    mutationFn: blogPostsApi.unpublishPost,
    onSuccess: (unpublishedPost) => {
      blogStore.updatePost(unpublishedPost);
      queryClient.setQueryData(BLOG_QUERY_KEYS.POST(unpublishedPost.id), unpublishedPost);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to unpublish post');
    },
  });

  // Archive post mutation
  const archivePostMutation = useMutation({
    mutationFn: blogPostsApi.archivePost,
    onSuccess: (archivedPost) => {
      blogStore.updatePost(archivedPost);
      queryClient.setQueryData(BLOG_QUERY_KEYS.POST(archivedPost.id), archivedPost);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to archive post');
    },
  });

  // Duplicate post mutation
  const duplicatePostMutation = useMutation({
    mutationFn: blogPostsApi.duplicatePost,
    onSuccess: (duplicatedPost) => {
      blogStore.addPost(duplicatedPost);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to duplicate post');
    },
  });

  // Bulk operation mutation
  const bulkOperationMutation = useMutation({
    mutationFn: blogPostsApi.bulkOperation,
    onMutate: () => {
      blogStore.setLoadingState('bulkOperation', true);
    },
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POST_STATISTICS });
      blogStore.clearSelection();

      if (variables.action === 'delete') {
        // Remove deleted posts from store
        variables.postIds.forEach(postId => {
          queryClient.removeQueries({ queryKey: BLOG_QUERY_KEYS.POST(postId) });
        });
      }
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Bulk operation failed');
    },
    onSettled: () => {
      blogStore.setLoadingState('bulkOperation', false);
    },
  });

  // Save draft mutation
  const saveDraftMutation = useMutation({
    mutationFn: blogPostsApi.saveDraft,
    onMutate: () => {
      blogStore.setLoadingState('autoSaving', true);
    },
    onSuccess: (draft) => {
      blogStore.setCurrentDraft(draft);
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.DRAFTS });
    },
    onError: (error) => {
      console.warn('Failed to save draft:', error);
    },
    onSettled: () => {
      blogStore.setLoadingState('autoSaving', false);
    },
  });

  // File upload mutation
  const uploadFileMutation = useMutation({
    mutationFn: ({ file, onProgress }: { file: File; onProgress?: (event: AxiosProgressEvent) => void }) =>
      blogPostsApi.uploadFile(file, onProgress),
    onMutate: () => {
      blogStore.setLoadingState('uploadingFile', true);
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'File upload failed');
    },
    onSettled: () => {
      blogStore.setLoadingState('uploadingFile', false);
    },
  });

  // Export content mutation
  const exportContentMutation = useMutation({
    mutationFn: blogPostsApi.exportContent,
    onMutate: () => {
      blogStore.setLoadingState('exportingContent', true);
    },
    onSuccess: (blob, variables) => {
      // Create download link
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `blog-export-${variables.format}-${new Date().toISOString().split('T')[0]}.${variables.format}`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Export failed');
    },
    onSettled: () => {
      blogStore.setLoadingState('exportingContent', false);
    },
  });

  // Import content mutation
  const importContentMutation = useMutation({
    mutationFn: blogPostsApi.importContent,
    onMutate: () => {
      blogStore.setLoadingState('importingContent', true);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POSTS });
      queryClient.invalidateQueries({ queryKey: BLOG_QUERY_KEYS.POST_STATISTICS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Import failed');
    },
    onSettled: () => {
      blogStore.setLoadingState('importingContent', false);
    },
  });

  return {
    createPostMutation,
    updatePostMutation,
    deletePostMutation,
    publishPostMutation,
    unpublishPostMutation,
    archivePostMutation,
    duplicatePostMutation,
    bulkOperationMutation,
    saveDraftMutation,
    uploadFileMutation,
    exportContentMutation,
    importContentMutation,
  };
};

// Export the API object as default
const blogApi = blogPostsApi;
export default blogApi;