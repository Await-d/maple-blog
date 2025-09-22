// @ts-nocheck
/**
 * Blog Tags API Service using TanStack Query
 * Handles all tag-related API communications with the backend
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError, AxiosResponse } from 'axios';
import {
  Tag,
  TagListResponse,
  CreateTagRequest,
  UpdateTagRequest,
  TagSearchParams,
  TagStatistics,
  ApiResponse,
  OperationResult,
  DEFAULT_PAGE_SIZE,
} from '../../types/blog';
import { apiClient } from '../api/client';
import { useBlogStore } from '../../stores/blogStore';

// API endpoints
const API_ENDPOINTS = {
  TAGS: {
    LIST: '/api/blog/tags',
    CREATE: '/api/blog/tags',
    GET: (id: string) => `/api/blog/tags/${id}`,
    UPDATE: (id: string) => `/api/blog/tags/${id}`,
    DELETE: (id: string) => `/api/blog/tags/${id}`,
    BULK_DELETE: '/api/blog/tags/bulk-delete',
    MERGE: '/api/blog/tags/merge',
    STATISTICS: '/api/blog/tags/statistics',
    SUGGESTIONS: '/api/blog/tags/suggestions',
    POPULAR: '/api/blog/tags/popular',
    UNUSED: '/api/blog/tags/unused',
    SEARCH: '/api/blog/tags/search',
  },
} as const;

// Query keys for TanStack Query caching
export const TAG_QUERY_KEYS = {
  TAGS: ['blog', 'tags'] as const,
  TAG: (id: string) => ['blog', 'tags', id] as const,
  TAG_LIST: (params: TagSearchParams) => ['blog', 'tags', 'list', params] as const,
  TAG_SEARCH: (query: string) => ['blog', 'tags', 'search', query] as const,
  TAG_SUGGESTIONS: (query: string) => ['blog', 'tags', 'suggestions', query] as const,
  TAG_POPULAR: ['blog', 'tags', 'popular'] as const,
  TAG_UNUSED: ['blog', 'tags', 'unused'] as const,
  TAG_STATISTICS: ['blog', 'tags', 'statistics'] as const,
} as const;

// API response handler
const handleApiResponse = <T>(response: AxiosResponse<ApiResponse<T>>): T => {
  if (!response.data.success) {
    throw new Error(response.data.message || 'API request failed');
  }
  return response.data.data!;
};

// Tags API functions
export const tagsApi = {
  // Get paginated list of tags
  getTags: async (params: TagSearchParams = {}): Promise<TagListResponse> => {
    const searchParams = new URLSearchParams();

    // Add pagination parameters
    searchParams.append('page', (params.page || 1).toString());
    searchParams.append('pageSize', (params.pageSize || DEFAULT_PAGE_SIZE).toString());

    // Add search and filtering parameters
    if (params.query) searchParams.append('query', params.query);
    if (params.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params.sortOrder) searchParams.append('sortOrder', params.sortOrder);

    const response = await apiClient.get<ApiResponse<TagListResponse>>(
      `${API_ENDPOINTS.TAGS.LIST}?${searchParams}`
    );
    return handleApiResponse(response);
  },

  // Get single tag by ID
  getTag: async (id: string): Promise<Tag> => {
    const response = await apiClient.get<ApiResponse<Tag>>(API_ENDPOINTS.TAGS.GET(id));
    return handleApiResponse(response);
  },

  // Create new tag
  createTag: async (data: CreateTagRequest): Promise<Tag> => {
    const response = await apiClient.post<ApiResponse<Tag>>(API_ENDPOINTS.TAGS.CREATE, data);
    return handleApiResponse(response);
  },

  // Update existing tag
  updateTag: async (data: UpdateTagRequest): Promise<Tag> => {
    const response = await apiClient.put<ApiResponse<Tag>>(API_ENDPOINTS.TAGS.UPDATE(data.id), data);
    return handleApiResponse(response);
  },

  // Delete tag
  deleteTag: async (id: string): Promise<void> => {
    await apiClient.delete(API_ENDPOINTS.TAGS.DELETE(id));
  },

  // Bulk delete tags
  bulkDeleteTags: async (tagIds: string[]): Promise<OperationResult> => {
    const response = await apiClient.post<ApiResponse<OperationResult>>(
      API_ENDPOINTS.TAGS.BULK_DELETE,
      { tagIds }
    );
    return handleApiResponse(response);
  },

  // Merge tags
  mergeTags: async (sourceIds: string[], targetId: string): Promise<Tag> => {
    const response = await apiClient.post<ApiResponse<Tag>>(API_ENDPOINTS.TAGS.MERGE, {
      sourceIds,
      targetId,
    });
    return handleApiResponse(response);
  },

  // Search tags by name
  searchTags: async (query: string): Promise<Tag[]> => {
    if (!query.trim()) return [];

    const response = await apiClient.get<ApiResponse<Tag[]>>(
      `${API_ENDPOINTS.TAGS.SEARCH}?q=${encodeURIComponent(query)}`
    );
    return handleApiResponse(response);
  },

  // Get tag suggestions based on partial input
  getTagSuggestions: async (query: string): Promise<Tag[]> => {
    if (!query.trim()) return [];

    const response = await apiClient.get<ApiResponse<Tag[]>>(
      `${API_ENDPOINTS.TAGS.SUGGESTIONS}?q=${encodeURIComponent(query)}`
    );
    return handleApiResponse(response);
  },

  // Get popular tags
  getPopularTags: async (limit: number = 20): Promise<Tag[]> => {
    const response = await apiClient.get<ApiResponse<Tag[]>>(
      `${API_ENDPOINTS.TAGS.POPULAR}?limit=${limit}`
    );
    return handleApiResponse(response);
  },

  // Get unused tags (tags with no posts)
  getUnusedTags: async (): Promise<Tag[]> => {
    const response = await apiClient.get<ApiResponse<Tag[]>>(API_ENDPOINTS.TAGS.UNUSED);
    return handleApiResponse(response);
  },

  // Get tag statistics
  getTagStatistics: async (): Promise<TagStatistics> => {
    const response = await apiClient.get<ApiResponse<TagStatistics>>(API_ENDPOINTS.TAGS.STATISTICS);
    return handleApiResponse(response);
  },

  // Create tag if it doesn't exist (for auto-tagging)
  getOrCreateTag: async (name: string): Promise<Tag> => {
    try {
      // First try to search for existing tag
      const searchResults = await tagsApi.searchTags(name);
      const existingTag = searchResults.find(tag => tag.name.toLowerCase() === name.toLowerCase());

      if (existingTag) {
        return existingTag;
      }

      // Create new tag if it doesn't exist
      return await tagsApi.createTag({
        name,
        slug: name.toLowerCase().replace(/\s+/g, '-').replace(/[^\w-]/g, ''),
      });
    } catch (error) {
      throw new Error(`Failed to get or create tag: ${name}`);
    }
  },
};

// TanStack Query hooks for tags
export const useTagQueries = () => {
  // Query for paginated tags list
  const useTagsList = (params: TagSearchParams = {}, enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG_LIST(params),
      queryFn: () => tagsApi.getTags(params),
      enabled,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    });
  };

  // Query for single tag
  const useTag = (id: string, enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG(id),
      queryFn: () => tagsApi.getTag(id),
      enabled: enabled && !!id,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    });
  };

  // Query for tag search results
  const useTagsSearch = (query: string, enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG_SEARCH(query),
      queryFn: () => tagsApi.searchTags(query),
      enabled: enabled && !!query.trim(),
      staleTime: 2 * 60 * 1000, // 2 minutes for search results
    });
  };

  // Query for tag suggestions (for autocomplete)
  const useTagSuggestions = (query: string, enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG_SUGGESTIONS(query),
      queryFn: () => tagsApi.getTagSuggestions(query),
      enabled: enabled && query.trim().length >= 2, // Only start suggesting after 2+ characters
      staleTime: 1 * 60 * 1000, // 1 minute for suggestions
      gcTime: 5 * 60 * 1000,
    });
  };

  // Query for popular tags
  const usePopularTags = (limit: number = 20, enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG_POPULAR,
      queryFn: () => tagsApi.getPopularTags(limit),
      enabled,
      staleTime: 10 * 60 * 1000, // 10 minutes
      gcTime: 15 * 60 * 1000, // 15 minutes
    });
  };

  // Query for unused tags
  const useUnusedTags = (enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG_UNUSED,
      queryFn: tagsApi.getUnusedTags,
      enabled,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    });
  };

  // Query for tag statistics
  const useTagStatistics = (enabled = true) => {
    return useQuery({
      queryKey: TAG_QUERY_KEYS.TAG_STATISTICS,
      queryFn: tagsApi.getTagStatistics,
      enabled,
      staleTime: 5 * 60 * 1000, // 5 minutes
      refetchInterval: 5 * 60 * 1000, // Auto-refetch every 5 minutes
    });
  };

  return {
    useTagsList,
    useTag,
    useTagsSearch,
    useTagSuggestions,
    usePopularTags,
    useUnusedTags,
    useTagStatistics,
  };
};

// TanStack Query mutations for tags
export const useTagMutations = () => {
  const queryClient = useQueryClient();
  const blogStore = useBlogStore();

  // Create tag mutation
  const createTagMutation = useMutation({
    mutationFn: tagsApi.createTag,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
    },
    onSuccess: (createdTag) => {
      blogStore.addTag(createdTag);
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_STATISTICS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_POPULAR });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to create tag');
    },
  });

  // Update tag mutation
  const updateTagMutation = useMutation({
    mutationFn: tagsApi.updateTag,
    onMutate: async (updatedTag) => {
      await queryClient.cancelQueries({ queryKey: TAG_QUERY_KEYS.TAG(updatedTag.id) });

      // Optimistic update
      const previousTag = queryClient.getQueryData(TAG_QUERY_KEYS.TAG(updatedTag.id));
      if (previousTag) {
        queryClient.setQueryData(TAG_QUERY_KEYS.TAG(updatedTag.id), {
          ...previousTag,
          ...updatedTag,
          updatedAt: new Date().toISOString(),
        });
      }

      return { previousTag };
    },
    onSuccess: (updatedTag) => {
      blogStore.updateTag(updatedTag);
      queryClient.setQueryData(TAG_QUERY_KEYS.TAG(updatedTag.id), updatedTag);
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
    },
    onError: (error, variables, context) => {
      // Revert optimistic update
      if (context?.previousTag) {
        queryClient.setQueryData(TAG_QUERY_KEYS.TAG(variables.id), context.previousTag);
      }
      blogStore.setError(error instanceof Error ? error.message : 'Failed to update tag');
    },
  });

  // Delete tag mutation
  const deleteTagMutation = useMutation({
    mutationFn: tagsApi.deleteTag,
    onSuccess: (_, tagId) => {
      blogStore.removeTag(tagId);
      queryClient.removeQueries({ queryKey: TAG_QUERY_KEYS.TAG(tagId) });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_STATISTICS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_POPULAR });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_UNUSED });
      // Also invalidate posts queries as posts may have been updated
      queryClient.invalidateQueries({ queryKey: ['blog', 'posts'] });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to delete tag');
    },
  });

  // Bulk delete tags mutation
  const bulkDeleteTagsMutation = useMutation({
    mutationFn: tagsApi.bulkDeleteTags,
    onSuccess: (_, tagIds) => {
      // Remove deleted tags from store
      tagIds.forEach(id => {
        blogStore.removeTag(id);
        queryClient.removeQueries({ queryKey: TAG_QUERY_KEYS.TAG(id) });
      });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_STATISTICS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_POPULAR });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_UNUSED });
      queryClient.invalidateQueries({ queryKey: ['blog', 'posts'] });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to bulk delete tags');
    },
  });

  // Merge tags mutation
  const mergeTagsMutation = useMutation({
    mutationFn: ({ sourceIds, targetId }: { sourceIds: string[]; targetId: string }) =>
      tagsApi.mergeTags(sourceIds, targetId),
    onSuccess: (mergedTag, { sourceIds }) => {
      // Remove source tags and update target tag
      sourceIds.forEach(id => {
        blogStore.removeTag(id);
        queryClient.removeQueries({ queryKey: TAG_QUERY_KEYS.TAG(id) });
      });
      blogStore.updateTag(mergedTag);
      queryClient.setQueryData(TAG_QUERY_KEYS.TAG(mergedTag.id), mergedTag);
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_STATISTICS });
      queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAG_POPULAR });
      queryClient.invalidateQueries({ queryKey: ['blog', 'posts'] });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to merge tags');
    },
  });

  // Get or create tag mutation (for auto-tagging)
  const getOrCreateTagMutation = useMutation({
    mutationFn: tagsApi.getOrCreateTag,
    onSuccess: (tag, tagName) => {
      // Only add to store if it's a new tag (has recent createdAt)
      const isNewTag = new Date(tag.createdAt) > new Date(Date.now() - 5000);
      if (isNewTag) {
        blogStore.addTag(tag);
        queryClient.invalidateQueries({ queryKey: TAG_QUERY_KEYS.TAGS });
      }
    },
    onError: (error) => {
      console.warn('Failed to get or create tag:', error);
    },
  });

  return {
    createTagMutation,
    updateTagMutation,
    deleteTagMutation,
    bulkDeleteTagsMutation,
    mergeTagsMutation,
    getOrCreateTagMutation,
  };
};

// Utility hooks for tag operations
export const useTagUtils = () => {
  const { data: popularTags } = useTagQueries().usePopularTags();
  const { getOrCreateTagMutation } = useTagMutations();

  // Process tag input and create tags as needed
  const processTagInput = async (tagNames: string[]): Promise<Tag[]> => {
    const processedTags: Tag[] = [];

    for (const tagName of tagNames) {
      if (tagName.trim()) {
        try {
          const tag = await getOrCreateTagMutation.mutateAsync(tagName.trim());
          processedTags.push(tag);
        } catch (error) {
          console.warn(`Failed to process tag: ${tagName}`, error);
        }
      }
    }

    return processedTags;
  };

  // Get tag color based on usage/popularity
  const getTagColor = (tag: Tag): string => {
    if (!popularTags) return '#6B7280'; // default gray

    const popularityRank = popularTags.findIndex(popular => popular.id === tag.id);

    if (popularityRank === -1) return '#6B7280'; // not in popular tags
    if (popularityRank < 5) return '#DC2626'; // top 5 - red
    if (popularityRank < 10) return '#EA580C'; // top 10 - orange
    if (popularityRank < 15) return '#CA8A04'; // top 15 - amber
    return '#059669'; // others - green
  };

  // Validate tag name
  const validateTagName = (name: string): { isValid: boolean; error?: string } => {
    if (!name.trim()) {
      return { isValid: false, error: 'Tag name cannot be empty' };
    }

    if (name.length < 2) {
      return { isValid: false, error: 'Tag name must be at least 2 characters long' };
    }

    if (name.length > 50) {
      return { isValid: false, error: 'Tag name cannot exceed 50 characters' };
    }

    if (!/^[\w\s-]+$/i.test(name)) {
      return { isValid: false, error: 'Tag name can only contain letters, numbers, spaces, and hyphens' };
    }

    return { isValid: true };
  };

  // Generate tag slug from name
  const generateTagSlug = (name: string): string => {
    return name
      .toLowerCase()
      .trim()
      .replace(/\s+/g, '-')
      .replace(/[^\w-]/g, '')
      .replace(/--+/g, '-')
      .replace(/^-|-$/g, '');
  };

  return {
    processTagInput,
    getTagColor,
    validateTagName,
    generateTagSlug,
  };
};

// Hook for tag input/autocomplete functionality
export const useTagInput = () => {
  const { useTagSuggestions } = useTagQueries();
  const { processTagInput } = useTagUtils();

  const getTagSuggestionsForInput = (query: string) => {
    return useTagSuggestions(query);
  };

  return {
    getTagSuggestionsForInput,
    processTagInput,
  };
};

export default tagsApi;