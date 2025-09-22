// @ts-nocheck
/**
 * Blog Categories API Service using TanStack Query
 * Handles all category-related API communications with the backend
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError, AxiosResponse } from 'axios';
import {
  Category,
  CategoryListResponse,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CategorySearchParams,
  CategoryStatistics,
  ApiResponse,
  OperationResult,
  DEFAULT_PAGE_SIZE,
} from '../../types/blog';
import { apiClient } from '../api/client';
import { useBlogStore } from '../../stores/blogStore';

// API endpoints
const API_ENDPOINTS = {
  CATEGORIES: {
    LIST: '/api/blog/categories',
    CREATE: '/api/blog/categories',
    GET: (id: string) => `/api/blog/categories/${id}`,
    UPDATE: (id: string) => `/api/blog/categories/${id}`,
    DELETE: (id: string) => `/api/blog/categories/${id}`,
    TREE: '/api/blog/categories/tree',
    REORDER: '/api/blog/categories/reorder',
    STATISTICS: '/api/blog/categories/statistics',
    MERGE: '/api/blog/categories/merge',
    BULK_DELETE: '/api/blog/categories/bulk-delete',
  },
} as const;

// Query keys for TanStack Query caching
export const CATEGORY_QUERY_KEYS = {
  CATEGORIES: ['blog', 'categories'] as const,
  CATEGORY: (id: string) => ['blog', 'categories', id] as const,
  CATEGORY_LIST: (params: CategorySearchParams) => ['blog', 'categories', 'list', params] as const,
  CATEGORY_TREE: ['blog', 'categories', 'tree'] as const,
  CATEGORY_STATISTICS: ['blog', 'categories', 'statistics'] as const,
} as const;

// API response handler
const handleApiResponse = <T>(response: AxiosResponse<ApiResponse<T>>): T => {
  if (!response.data.success) {
    throw new Error(response.data.message || 'API request failed');
  }
  return response.data.data!;
};

// Categories API functions
export const categoriesApi = {
  // Get paginated list of categories
  getCategories: async (params: CategorySearchParams = {}): Promise<CategoryListResponse> => {
    const searchParams = new URLSearchParams();

    // Add pagination parameters
    searchParams.append('page', (params.page || 1).toString());
    searchParams.append('pageSize', (params.pageSize || DEFAULT_PAGE_SIZE).toString());

    // Add search and filtering parameters
    if (params.query) searchParams.append('query', params.query);
    if (params.parentId) searchParams.append('parentId', params.parentId);
    if (params.isActive !== undefined) searchParams.append('isActive', params.isActive.toString());
    if (params.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params.sortOrder) searchParams.append('sortOrder', params.sortOrder);

    const response = await apiClient.get<ApiResponse<CategoryListResponse>>(
      `${API_ENDPOINTS.CATEGORIES.LIST}?${searchParams}`
    );
    return handleApiResponse(response);
  },

  // Get all categories as a hierarchical tree
  getCategoryTree: async (): Promise<Category[]> => {
    const response = await apiClient.get<ApiResponse<Category[]>>(API_ENDPOINTS.CATEGORIES.TREE);
    return handleApiResponse(response);
  },

  // Get single category by ID
  getCategory: async (id: string): Promise<Category> => {
    const response = await apiClient.get<ApiResponse<Category>>(API_ENDPOINTS.CATEGORIES.GET(id));
    return handleApiResponse(response);
  },

  // Create new category
  createCategory: async (data: CreateCategoryRequest): Promise<Category> => {
    const response = await apiClient.post<ApiResponse<Category>>(API_ENDPOINTS.CATEGORIES.CREATE, data);
    return handleApiResponse(response);
  },

  // Update existing category
  updateCategory: async (data: UpdateCategoryRequest): Promise<Category> => {
    const response = await apiClient.put<ApiResponse<Category>>(
      API_ENDPOINTS.CATEGORIES.UPDATE(data.id),
      data
    );
    return handleApiResponse(response);
  },

  // Delete category
  deleteCategory: async (id: string, movePostsToId?: string): Promise<void> => {
    const params = movePostsToId ? `?movePostsTo=${movePostsToId}` : '';
    await apiClient.delete(`${API_ENDPOINTS.CATEGORIES.DELETE(id)}${params}`);
  },

  // Bulk delete categories
  bulkDeleteCategories: async (categoryIds: string[], movePostsToId?: string): Promise<OperationResult> => {
    const response = await apiClient.post<ApiResponse<OperationResult>>(
      API_ENDPOINTS.CATEGORIES.BULK_DELETE,
      { categoryIds, movePostsToId }
    );
    return handleApiResponse(response);
  },

  // Reorder categories (for hierarchical sorting)
  reorderCategories: async (categoryOrders: Array<{ id: string; sortOrder: number; parentId?: string }>): Promise<void> => {
    await apiClient.post(API_ENDPOINTS.CATEGORIES.REORDER, { categoryOrders });
  },

  // Get category statistics
  getCategoryStatistics: async (): Promise<CategoryStatistics> => {
    const response = await apiClient.get<ApiResponse<CategoryStatistics>>(API_ENDPOINTS.CATEGORIES.STATISTICS);
    return handleApiResponse(response);
  },

  // Merge categories
  mergeCategories: async (sourceIds: string[], targetId: string): Promise<Category> => {
    const response = await apiClient.post<ApiResponse<Category>>(API_ENDPOINTS.CATEGORIES.MERGE, {
      sourceIds,
      targetId,
    });
    return handleApiResponse(response);
  },
};

// TanStack Query hooks for categories
export const useCategoryQueries = () => {
  // Query for paginated categories list
  const useCategoriesList = (params: CategorySearchParams = {}, enabled = true) => {
    return useQuery({
      queryKey: CATEGORY_QUERY_KEYS.CATEGORY_LIST(params),
      queryFn: () => categoriesApi.getCategories(params),
      enabled,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    });
  };

  // Query for category tree (hierarchical structure)
  const useCategoryTree = (enabled = true) => {
    return useQuery({
      queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE,
      queryFn: categoriesApi.getCategoryTree,
      enabled,
      staleTime: 10 * 60 * 1000, // 10 minutes
      gcTime: 15 * 60 * 1000, // 15 minutes
      select: (data) => {
        // Transform flat list to hierarchical structure if needed
        const buildTree = (categories: Category[], parentId?: string): Category[] => {
          return categories
            .filter(cat => cat.parentId === parentId)
            .map(cat => ({
              ...cat,
              children: buildTree(categories, cat.id),
            }))
            .sort((a, b) => a.sortOrder - b.sortOrder);
        };

        return buildTree(data);
      },
    });
  };

  // Query for single category
  const useCategory = (id: string, enabled = true) => {
    return useQuery({
      queryKey: CATEGORY_QUERY_KEYS.CATEGORY(id),
      queryFn: () => categoriesApi.getCategory(id),
      enabled: enabled && !!id,
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
    });
  };

  // Query for category statistics
  const useCategoryStatistics = (enabled = true) => {
    return useQuery({
      queryKey: CATEGORY_QUERY_KEYS.CATEGORY_STATISTICS,
      queryFn: categoriesApi.getCategoryStatistics,
      enabled,
      staleTime: 5 * 60 * 1000, // 5 minutes
      refetchInterval: 5 * 60 * 1000, // Auto-refetch every 5 minutes
    });
  };

  return {
    useCategoriesList,
    useCategoryTree,
    useCategory,
    useCategoryStatistics,
  };
};

// TanStack Query mutations for categories
export const useCategoryMutations = () => {
  const queryClient = useQueryClient();
  const blogStore = useBlogStore();

  // Create category mutation
  const createCategoryMutation = useMutation({
    mutationFn: categoriesApi.createCategory,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
    },
    onSuccess: (createdCategory) => {
      blogStore.addCategory(createdCategory);
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_STATISTICS });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to create category');
    },
  });

  // Update category mutation
  const updateCategoryMutation = useMutation({
    mutationFn: categoriesApi.updateCategory,
    onMutate: async (updatedCategory) => {
      await queryClient.cancelQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY(updatedCategory.id) });

      // Optimistic update
      const previousCategory = queryClient.getQueryData(CATEGORY_QUERY_KEYS.CATEGORY(updatedCategory.id));
      if (previousCategory) {
        queryClient.setQueryData(CATEGORY_QUERY_KEYS.CATEGORY(updatedCategory.id), {
          ...previousCategory,
          ...updatedCategory,
          updatedAt: new Date().toISOString(),
        });
      }

      return { previousCategory };
    },
    onSuccess: (updatedCategory) => {
      blogStore.updateCategory(updatedCategory);
      queryClient.setQueryData(CATEGORY_QUERY_KEYS.CATEGORY(updatedCategory.id), updatedCategory);
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
    },
    onError: (error, variables, context) => {
      // Revert optimistic update
      if (context?.previousCategory) {
        queryClient.setQueryData(CATEGORY_QUERY_KEYS.CATEGORY(variables.id), context.previousCategory);
      }
      blogStore.setError(error instanceof Error ? error.message : 'Failed to update category');
    },
  });

  // Delete category mutation
  const deleteCategoryMutation = useMutation({
    mutationFn: ({ id, movePostsToId }: { id: string; movePostsToId?: string }) =>
      categoriesApi.deleteCategory(id, movePostsToId),
    onSuccess: (_, { id }) => {
      blogStore.removeCategory(id);
      queryClient.removeQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY(id) });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_STATISTICS });
      // Also invalidate posts queries as posts may have been moved
      queryClient.invalidateQueries({ queryKey: ['blog', 'posts'] });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to delete category');
    },
  });

  // Bulk delete categories mutation
  const bulkDeleteCategoriesMutation = useMutation({
    mutationFn: ({ categoryIds, movePostsToId }: { categoryIds: string[]; movePostsToId?: string }) =>
      categoriesApi.bulkDeleteCategories(categoryIds, movePostsToId),
    onSuccess: (_, { categoryIds }) => {
      // Remove deleted categories from store
      categoryIds.forEach(id => {
        blogStore.removeCategory(id);
        queryClient.removeQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY(id) });
      });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_STATISTICS });
      queryClient.invalidateQueries({ queryKey: ['blog', 'posts'] });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to bulk delete categories');
    },
  });

  // Reorder categories mutation
  const reorderCategoriesMutation = useMutation({
    mutationFn: categoriesApi.reorderCategories,
    onMutate: async (categoryOrders) => {
      await queryClient.cancelQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });

      // Update local store immediately for better UX
      const orderedIds = categoryOrders
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map(item => item.id);
      blogStore.sortCategories(orderedIds);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
    },
    onError: (error, variables) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to reorder categories');
      // Invalidate to revert optimistic update
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
    },
  });

  // Merge categories mutation
  const mergeCategoriesMutation = useMutation({
    mutationFn: ({ sourceIds, targetId }: { sourceIds: string[]; targetId: string }) =>
      categoriesApi.mergeCategories(sourceIds, targetId),
    onSuccess: (mergedCategory, { sourceIds }) => {
      // Remove source categories and update target category
      sourceIds.forEach(id => {
        blogStore.removeCategory(id);
        queryClient.removeQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY(id) });
      });
      blogStore.updateCategory(mergedCategory);
      queryClient.setQueryData(CATEGORY_QUERY_KEYS.CATEGORY(mergedCategory.id), mergedCategory);
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORIES });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_TREE });
      queryClient.invalidateQueries({ queryKey: CATEGORY_QUERY_KEYS.CATEGORY_STATISTICS });
      queryClient.invalidateQueries({ queryKey: ['blog', 'posts'] });
    },
    onError: (error) => {
      blogStore.setError(error instanceof Error ? error.message : 'Failed to merge categories');
    },
  });

  return {
    createCategoryMutation,
    updateCategoryMutation,
    deleteCategoryMutation,
    bulkDeleteCategoriesMutation,
    reorderCategoriesMutation,
    mergeCategoriesMutation,
  };
};

// Utility hooks for category operations
export const useCategoryUtils = () => {
  const { data: categoryTree } = useCategoryQueries().useCategoryTree();

  // Get category path (breadcrumb) for a given category
  const getCategoryPath = (categoryId: string): Category[] => {
    if (!categoryTree) return [];

    const findPath = (categories: Category[], targetId: string, path: Category[] = []): Category[] | null => {
      for (const category of categories) {
        const currentPath = [...path, category];
        if (category.id === targetId) {
          return currentPath;
        }
        if (category.children.length > 0) {
          const foundPath = findPath(category.children, targetId, currentPath);
          if (foundPath) return foundPath;
        }
      }
      return null;
    };

    return findPath(categoryTree, categoryId) || [];
  };

  // Get all subcategories for a given category
  const getSubcategories = (categoryId: string): Category[] => {
    if (!categoryTree) return [];

    const findCategory = (categories: Category[], id: string): Category | null => {
      for (const category of categories) {
        if (category.id === id) return category;
        if (category.children.length > 0) {
          const found = findCategory(category.children, id);
          if (found) return found;
        }
      }
      return null;
    };

    const getAllSubcategories = (category: Category): Category[] => {
      let result: Category[] = [];
      for (const child of category.children) {
        result.push(child);
        result = result.concat(getAllSubcategories(child));
      }
      return result;
    };

    const category = findCategory(categoryTree, categoryId);
    return category ? getAllSubcategories(category) : [];
  };

  // Get flattened list of all categories
  const getFlatCategoryList = (): Category[] => {
    if (!categoryTree) return [];

    const flatten = (categories: Category[]): Category[] => {
      let result: Category[] = [];
      for (const category of categories) {
        result.push(category);
        if (category.children.length > 0) {
          result = result.concat(flatten(category.children));
        }
      }
      return result;
    };

    return flatten(categoryTree);
  };

  // Check if a category can be moved to another parent (prevent circular references)
  const canMoveCategory = (categoryId: string, newParentId: string): boolean => {
    const subcategories = getSubcategories(categoryId);
    return !subcategories.some(sub => sub.id === newParentId);
  };

  return {
    getCategoryPath,
    getSubcategories,
    getFlatCategoryList,
    canMoveCategory,
  };
};

export default categoriesApi;