/**
 * Blog API Services Export Module
 * Centralized exports for all blog-related API services
 */

// Blog Posts API
export {
  default as blogPostsApi,
  useBlogPostQueries,
  useBlogPostMutations,
  BLOG_QUERY_KEYS,
} from './blogApi';

import { BLOG_QUERY_KEYS, useBlogPostQueries, useBlogPostMutations } from './blogApi';

// Re-export types for convenience
export * from '../../types/blog';

// Combined query keys for cache management
export const ALL_BLOG_QUERY_KEYS = {
  ...BLOG_QUERY_KEYS,
} as const;

// Combined hooks for convenience
export const useBlogQueries = () => {
  const postQueries = useBlogPostQueries();

  return {
    ...postQueries,
  };
};

export const useBlogMutations = () => {
  const postMutations = useBlogPostMutations();

  return {
    ...postMutations,
  };
};