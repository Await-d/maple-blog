/**
 * Blog Content Management State using Zustand
 * Manages global blog state, posts, categories, tags, and editor state
 */

import { create } from 'zustand';
import { persist, subscribeWithSelector } from 'zustand/middleware';
import {
  Post,
  Category,
  Tag,
  BlogState,
  PostSearchParams,
  CategorySearchParams,
  TagSearchParams,
  AutoSaveDraft,
  ContentRevision,
  PostStatistics,
  CategoryStatistics,
  TagStatistics,
  EditorConfig,
  BulkOperation,
  PostStatus,
  PostFormData,
  DEFAULT_PAGE_SIZE,
  AUTO_SAVE_INTERVAL,
} from '../types/blog';

// Blog loading states for different operations
interface BlogLoadingStates {
  loadingPosts: boolean;
  loadingCategories: boolean;
  loadingTags: boolean;
  savingPost: boolean;
  deletingPost: boolean;
  uploadingFile: boolean;
  bulkOperation: boolean;
  autoSaving: boolean;
  loadingStatistics: boolean;
  exportingContent: boolean;
  importingContent: boolean;
}

// Extended blog store interface
interface BlogStore extends BlogState {
  // Loading states
  loadingStates: BlogLoadingStates;

  // Cache timestamps for invalidation
  gcTimestamps: {
    posts: Date | null;
    categories: Date | null;
    tags: Date | null;
    statistics: Date | null;
  };

  // Bulk operations
  bulkOperation: BulkOperation | null;

  // Content revisions
  revisions: ContentRevision[];

  // Actions for posts
  setPosts: (posts: Post[]) => void;
  addPost: (post: Post) => void;
  updatePost: (post: Post) => void;
  removePost: (postId: string) => void;
  setCurrentPost: (post: Post | null) => void;
  duplicatePost: (postId: string) => Promise<Post | null>;

  // Actions for categories
  setCategories: (categories: Category[]) => void;
  addCategory: (category: Category) => void;
  updateCategory: (category: Category) => void;
  removeCategory: (categoryId: string) => void;
  sortCategories: (categoryIds: string[]) => void;

  // Actions for tags
  setTags: (tags: Tag[]) => void;
  addTag: (tag: Tag) => void;
  updateTag: (tag: Tag) => void;
  removeTag: (tagId: string) => void;
  getOrCreateTag: (name: string) => Tag;

  // Search and filtering actions
  setSearchParams: (params: Partial<PostSearchParams>) => void;
  setCategorySearchParams: (params: Partial<CategorySearchParams>) => void;
  setTagSearchParams: (params: Partial<TagSearchParams>) => void;
  clearFilters: () => void;

  // Pagination actions
  setCurrentPage: (page: number) => void;
  setTotalPages: (total: number) => void;
  setTotalCount: (count: number) => void;

  // Selection actions for bulk operations
  togglePostSelection: (postId: string) => void;
  selectAllPosts: () => void;
  clearSelection: () => void;
  getSelectedPosts: () => Post[];

  // Editor actions
  setEditorMode: (mode: 'markdown' | 'visual') => void;
  togglePreview: () => void;
  toggleFullscreen: () => void;
  setHasUnsavedChanges: (hasChanges: boolean) => void;

  // Auto-save actions
  setCurrentDraft: (draft: AutoSaveDraft | null) => void;
  saveDraft: (formData: Partial<PostFormData>) => void;
  loadDraft: (draftId: string) => AutoSaveDraft | null;
  deleteDraft: (draftId: string) => void;
  setAutoSaveEnabled: (enabled: boolean) => void;
  markSaved: () => void;

  // Loading state actions
  setLoadingState: (operation: keyof BlogLoadingStates, loading: boolean) => void;

  // Error handling
  setError: (error: string | null) => void;
  clearError: () => void;

  // Cache management
  setCacheTimestamp: (key: keyof BlogStore['gcTimestamps'], timestamp: Date) => void;
  isCacheValid: (key: keyof BlogStore['gcTimestamps'], maxAge: number) => boolean;
  invalidateCache: (key?: keyof BlogStore['gcTimestamps']) => void;

  // Statistics actions
  setStatistics: (stats: PostStatistics) => void;
  setCategoryStatistics: (stats: CategoryStatistics) => void;
  setTagStatistics: (stats: TagStatistics) => void;

  // Bulk operations
  setBulkOperation: (operation: BulkOperation | null) => void;

  // Content revisions
  addRevision: (revision: ContentRevision) => void;
  getRevisions: (postId: string) => ContentRevision[];

  // Editor configuration
  setEditorConfig: (config: Partial<EditorConfig>) => void;
  getEditorConfig: () => EditorConfig;

  // Search suggestions
  getTagSuggestions: (query: string) => Tag[];
  getCategorySuggestions: (query: string) => Category[];

  // Utility actions
  reset: () => void;
  initialize: () => void;
}

// Initial state
const initialState = {
  // Data
  posts: [],
  categories: [],
  tags: [],

  // Current editing state
  currentPost: null,
  currentDraft: null,

  // UI state
  isLoading: false,
  isSaving: false,
  isUploading: false,

  // Search and filtering state
  searchParams: {
    query: '',
    status: [PostStatus.Draft, PostStatus.Published],
    sortBy: 'updatedAt',
    sortOrder: 'desc',
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  },
  categorySearchParams: {
    query: '',
    sortBy: 'name',
    sortOrder: 'asc',
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  },
  tagSearchParams: {
    query: '',
    sortBy: 'name',
    sortOrder: 'asc',
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  },

  // Pagination state
  currentPage: 1,
  totalPages: 0,
  totalCount: 0,

  // Selection state
  selectedPostIds: [],

  // Error handling
  error: null,

  // Editor state
  editorMode: 'markdown',
  showPreview: true,
  isFullscreen: false,

  // Auto-save state
  lastSavedAt: null,
  hasUnsavedChanges: false,
  autoSaveEnabled: true,

  // Statistics cache
  statistics: null,
  categoryStatistics: null,
  tagStatistics: null,

  // Loading states
  loadingStates: {
    loadingPosts: false,
    loadingCategories: false,
    loadingTags: false,
    savingPost: false,
    deletingPost: false,
    uploadingFile: false,
    bulkOperation: false,
    autoSaving: false,
    loadingStatistics: false,
    exportingContent: false,
    importingContent: false,
  },

  // Cache timestamps
  gcTimestamps: {
    posts: null,
    categories: null,
    tags: null,
    statistics: null,
  },

  // Bulk operations
  bulkOperation: null,

  // Content revisions
  revisions: [] as ContentRevision[],
};

// Default editor configuration
const defaultEditorConfig: EditorConfig = {
  theme: 'light',
  fontSize: 14,
  lineHeight: 1.6,
  wordWrap: true,
  autoSave: true,
  autoSaveInterval: AUTO_SAVE_INTERVAL,
  spellCheck: true,
  showLineNumbers: true,
  showMinimap: false,
  toolbar: [
    'bold', 'italic', 'strikethrough', '|',
    'heading', 'quote', 'code', '|',
    'unordered-list', 'ordered-list', 'table', '|',
    'link', 'image', 'horizontal-rule', '|',
    'preview', 'fullscreen', 'help'
  ],
  shortcuts: {
    'Ctrl+B': 'bold',
    'Ctrl+I': 'italic',
    'Ctrl+K': 'link',
    'Ctrl+Shift+P': 'preview',
    'F11': 'fullscreen',
    'Ctrl+S': 'save',
  },
};

// Blog actions function
const createBlogActions = (set: (partial: BlogStore | ((state: BlogStore) => BlogStore)) => void, get: () => BlogStore) => ({
  // Posts actions
  setPosts: (posts: Post[]) => {
    set((state: BlogStore) => ({
      ...state,
      posts,
      error: null,
    }));
  },

  addPost: (post: Post) => {
    set((state: BlogStore) => ({
      ...state,
      posts: [post, ...state.posts],
      totalCount: state.totalCount + 1,
    }));
  },

  updatePost: (post: Post) => {
    set((state: BlogStore) => ({
      ...state,
      posts: state.posts.map(p => p.id === post.id ? post : p),
      currentPost: state.currentPost?.id === post.id ? post : state.currentPost,
    }));
  },

  removePost: (postId: string) => {
    set((state: BlogStore) => ({
      ...state,
      posts: state.posts.filter(p => p.id !== postId),
      selectedPostIds: state.selectedPostIds.filter(id => id !== postId),
      totalCount: Math.max(0, state.totalCount - 1),
      currentPost: state.currentPost?.id === postId ? null : state.currentPost,
    }));
  },

  setCurrentPost: (post: Post | null) => {
    set((state: BlogStore) => ({
      ...state,
      currentPost: post,
      hasUnsavedChanges: false,
    }));
  },

  duplicatePost: async (postId: string): Promise<Post | null> => {
    const { posts } = get();
    const originalPost = posts.find((p: Post) => p.id === postId);

    if (!originalPost) return null;

    // Create a duplicate with modified title and reset fields
    const duplicatedPost: Post = {
      ...originalPost,
      id: `draft_${Date.now()}`, // Temporary ID
      title: `${originalPost.title} (Copy)`,
      slug: `${originalPost.slug}-copy`,
      status: PostStatus.Draft,
      publishedAt: undefined,
      scheduledAt: undefined,
      viewCount: 0,
      likeCount: 0,
      commentCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      version: 1,
    };

    get().addPost(duplicatedPost);
    return duplicatedPost;
  },

  // Categories actions
  setCategories: (categories: Category[]) => {
    set((state: BlogStore) => ({
      ...state,
      categories,
    }));
  },

  addCategory: (category: Category) => {
    set((state: BlogStore) => ({
      ...state,
      categories: [...state.categories, category].sort((a, b) => a.sortOrder - b.sortOrder),
    }));
  },

  updateCategory: (category: Category) => {
    set((state: BlogStore) => ({
      ...state,
      categories: state.categories
        .map(c => c.id === category.id ? category : c)
        .sort((a, b) => a.sortOrder - b.sortOrder),
    }));
  },

  removeCategory: (categoryId: string) => {
    set((state: BlogStore) => ({
      ...state,
      categories: state.categories.filter(c => c.id !== categoryId),
      // Update posts that used this category
      posts: state.posts.map(p =>
        p.categoryId === categoryId
          ? { ...p, categoryId: undefined, category: undefined }
          : p
      ),
    }));
  },

  sortCategories: (categoryIds: string[]) => {
    set((state: BlogStore) => {
      const sortedCategories = categoryIds.map((id, index) => {
        const category = state.categories.find(c => c.id === id);
        return category ? { ...category, sortOrder: index } : null;
      }).filter(Boolean) as Category[];

      return {
        ...state,
        categories: sortedCategories,
      };
    });
  },

  // Tags actions
  setTags: (tags: Tag[]) => {
    set((state: BlogStore) => ({
      ...state,
      tags,
    }));
  },

  addTag: (tag: Tag) => {
    set((state: BlogStore) => ({
      ...state,
      tags: [...state.tags, tag].sort((a, b) => a.name.localeCompare(b.name)),
    }));
  },

  updateTag: (tag: Tag) => {
    set((state: BlogStore) => ({
      ...state,
      tags: state.tags
        .map(t => t.id === tag.id ? tag : t)
        .sort((a, b) => a.name.localeCompare(b.name)),
    }));
  },

  removeTag: (tagId: string) => {
    set((state: BlogStore) => ({
      ...state,
      tags: state.tags.filter(t => t.id !== tagId),
      // Update posts that used this tag
      posts: state.posts.map(p => ({
        ...p,
        tags: p.tags.filter(t => t.id !== tagId),
      })),
    }));
  },

  getOrCreateTag: (name: string): Tag => {
    const { tags } = get();
    const existingTag = tags.find((t: Tag) => t.name.toLowerCase() === name.toLowerCase());

    if (existingTag) {
      return existingTag;
    }

    // Create new tag (this would typically call an API)
    const newTag: Tag = {
      id: `temp_${Date.now()}`, // Temporary ID
      name,
      slug: name.toLowerCase().replace(/\s+/g, '-').replace(/[^\w-]/g, ''),
      postCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    get().addTag(newTag);
    return newTag;
  },

  // Search and filtering actions
  setSearchParams: (params: Partial<PostSearchParams>) => {
    set((state: BlogStore) => ({
      ...state,
      searchParams: { ...state.searchParams, ...params },
      currentPage: params.page || 1,
    }));
  },

  setCategorySearchParams: (params: Partial<CategorySearchParams>) => {
    set((state: BlogStore) => ({
      ...state,
      categorySearchParams: { ...state.categorySearchParams, ...params },
    }));
  },

  setTagSearchParams: (params: Partial<TagSearchParams>) => {
    set((state: BlogStore) => ({
      ...state,
      tagSearchParams: { ...state.tagSearchParams, ...params },
    }));
  },

  clearFilters: () => {
    set((state: BlogStore) => ({
      ...state,
      searchParams: {
        ...state.searchParams,
        query: '',
        status: [PostStatus.Draft, PostStatus.Published],
        categoryId: undefined,
        tagIds: [],
        authorId: undefined,
        dateFrom: undefined,
        dateTo: undefined,
      },
    }));
  },

  // Pagination actions
  setCurrentPage: (page: number) => {
    set((state: BlogStore) => ({
      ...state,
      currentPage: page,
      searchParams: { ...state.searchParams, page },
    }));
  },

  setTotalPages: (total: number) => {
    set((state: BlogStore) => ({
      ...state,
      totalPages: total,
    }));
  },

  setTotalCount: (count: number) => {
    set((state: BlogStore) => ({
      ...state,
      totalCount: count,
    }));
  },

  // Selection actions
  togglePostSelection: (postId: string) => {
    set((state: BlogStore) => {
      const isSelected = state.selectedPostIds.includes(postId);
      return {
        ...state,
        selectedPostIds: isSelected
          ? state.selectedPostIds.filter(id => id !== postId)
          : [...state.selectedPostIds, postId],
      };
    });
  },

  selectAllPosts: () => {
    set((state: BlogStore) => ({
      ...state,
      selectedPostIds: state.posts.map(p => p.id),
    }));
  },

  clearSelection: () => {
    set((state: BlogStore) => ({
      ...state,
      selectedPostIds: [],
    }));
  },

  getSelectedPosts: (): Post[] => {
    const { posts, selectedPostIds } = get();
    return posts.filter((p: Post) => selectedPostIds.includes(p.id));
  },

  // Editor actions
  setEditorMode: (mode: 'markdown' | 'visual') => {
    set((state: BlogStore) => ({
      ...state,
      editorMode: mode,
    }));
  },

  togglePreview: () => {
    set((state: BlogStore) => ({
      ...state,
      showPreview: !state.showPreview,
    }));
  },

  toggleFullscreen: () => {
    set((state: BlogStore) => ({
      ...state,
      isFullscreen: !state.isFullscreen,
    }));
  },

  setHasUnsavedChanges: (hasChanges: boolean) => {
    set((state: BlogStore) => ({
      ...state,
      hasUnsavedChanges: hasChanges,
    }));
  },

  // Auto-save actions
  setCurrentDraft: (draft: AutoSaveDraft | null) => {
    set((state: BlogStore) => ({
      ...state,
      currentDraft: draft,
    }));
  },

  saveDraft: (formData: Partial<PostFormData>) => {
    const draft: AutoSaveDraft = {
      id: `draft_${Date.now()}`,
      postId: get().currentPost?.id,
      title: formData.title || '',
      content: formData.content || '',
      excerpt: formData.excerpt,
      categoryId: formData.categoryId,
      tagIds: formData.tagIds || [],
      savedAt: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(), // 7 days
    };

    set((state: BlogStore) => ({
      ...state,
      currentDraft: draft,
    }));

    // Store in localStorage as backup
    const drafts = JSON.parse(localStorage.getItem('maple-blog-drafts') || '[]');
    const updatedDrafts = drafts.filter((d: AutoSaveDraft) => d.postId !== draft.postId);
    updatedDrafts.push(draft);
    localStorage.setItem('maple-blog-drafts', JSON.stringify(updatedDrafts));
  },

  loadDraft: (draftId: string): AutoSaveDraft | null => {
    const drafts = JSON.parse(localStorage.getItem('maple-blog-drafts') || '[]');
    return drafts.find((d: AutoSaveDraft) => d.id === draftId) || null;
  },

  deleteDraft: (draftId: string) => {
    const drafts = JSON.parse(localStorage.getItem('maple-blog-drafts') || '[]');
    const updatedDrafts = drafts.filter((d: AutoSaveDraft) => d.id !== draftId);
    localStorage.setItem('maple-blog-drafts', JSON.stringify(updatedDrafts));

    set((state: BlogStore) => ({
      ...state,
      currentDraft: state.currentDraft?.id === draftId ? null : state.currentDraft,
    }));
  },

  setAutoSaveEnabled: (enabled: boolean) => {
    set((state: BlogStore) => ({
      ...state,
      autoSaveEnabled: enabled,
    }));
  },

  markSaved: () => {
    set((state: BlogStore) => ({
      ...state,
      lastSavedAt: new Date(),
      hasUnsavedChanges: false,
    }));
  },

  // Loading state actions
  setLoadingState: (operation: keyof BlogLoadingStates, loading: boolean) => {
    set((state: BlogStore) => ({
      ...state,
      loadingStates: {
        ...state.loadingStates,
        [operation]: loading,
      },
    }));
  },

  // Error handling
  setError: (error: string | null) => {
    set((state: BlogStore) => ({
      ...state,
      error,
    }));
  },

  clearError: () => {
    set((state: BlogStore) => ({
      ...state,
      error: null,
    }));
  },

  // Cache management
  setCacheTimestamp: (key: keyof BlogStore['gcTimestamps'], timestamp: Date) => {
    set((state: BlogStore) => ({
      ...state,
      gcTimestamps: {
        ...state.gcTimestamps,
        [key]: timestamp,
      },
    }));
  },

  isCacheValid: (key: keyof BlogStore['gcTimestamps'], maxAge: number): boolean => {
    const { gcTimestamps } = get();
    const timestamp = gcTimestamps[key];
    if (!timestamp) return false;
    return Date.now() - timestamp.getTime() < maxAge;
  },

  invalidateCache: (key?: keyof BlogStore['gcTimestamps']) => {
    set((state: BlogStore) => ({
      ...state,
      gcTimestamps: key
        ? { ...state.gcTimestamps, [key]: null }
        : { posts: null, categories: null, tags: null, statistics: null },
    }));
  },

  // Statistics actions
  setStatistics: (stats: PostStatistics) => {
    set((state: BlogStore) => ({
      ...state,
      statistics: stats,
    }));
  },

  setCategoryStatistics: (stats: CategoryStatistics) => {
    set((state: BlogStore) => ({
      ...state,
      categoryStatistics: stats,
    }));
  },

  setTagStatistics: (stats: TagStatistics) => {
    set((state: BlogStore) => ({
      ...state,
      tagStatistics: stats,
    }));
  },

  // Search suggestions
  getTagSuggestions: (query: string): Tag[] => {
    const { tags } = get();
    if (!query.trim()) return tags.slice(0, 10);

    return tags
      .filter((tag: Tag) => tag.name.toLowerCase().includes(query.toLowerCase()))
      .sort((a: Tag, b: Tag) => b.postCount - a.postCount)
      .slice(0, 10);
  },

  getCategorySuggestions: (query: string): Category[] => {
    const { categories } = get();
    if (!query.trim()) return categories.slice(0, 10);

    return categories
      .filter((category: Category) =>
        category.name.toLowerCase().includes(query.toLowerCase()) ||
        category.description?.toLowerCase().includes(query.toLowerCase())
      )
      .sort((a: Category, b: Category) => b.postCount - a.postCount)
      .slice(0, 10);
  },

  // Editor configuration
  setEditorConfig: (config: Partial<EditorConfig>) => {
    const currentConfig = get().getEditorConfig();
    const newConfig = { ...currentConfig, ...config };

    // Persist to localStorage
    localStorage.setItem('maple-blog-editor-config', JSON.stringify(newConfig));
  },

  getEditorConfig: (): EditorConfig => {
    const stored = localStorage.getItem('maple-blog-editor-config');
    if (stored) {
      try {
        return { ...defaultEditorConfig, ...JSON.parse(stored) };
      } catch (error) {
        console.warn('Failed to parse editor config from localStorage');
      }
    }
    return defaultEditorConfig;
  },

  // Bulk operations
  setBulkOperation: (operation: BulkOperation | null) => {
    set((state: BlogStore) => ({
      ...state,
      bulkOperation: operation,
    }));
  },

  // Content revisions
  addRevision: (revision: ContentRevision) => {
    set((state: BlogStore) => ({
      ...state,
      revisions: [...(state.revisions || []), revision],
    }));
  },

  getRevisions: (postId: string) => {
    return get().revisions?.filter((r: ContentRevision) => r.postId === postId) || [];
  },

  // Utility actions
  reset: () => {
    set(() => ({ ...initialState } as unknown as BlogStore));
  },

  initialize: () => {
    // Load any persisted drafts
    const drafts = JSON.parse(localStorage.getItem('maple-blog-drafts') || '[]');
    const validDrafts = drafts.filter((d: AutoSaveDraft) =>
      new Date(d.expiresAt) > new Date()
    );
    localStorage.setItem('maple-blog-drafts', JSON.stringify(validDrafts));

    // Set initial cache timestamp
    get().setCacheTimestamp('posts', new Date());
  },
});

// Create the blog store with persistence
export const useBlogStore = create<BlogStore>()(
  subscribeWithSelector(
    persist(
      (set, get) => ({
        ...initialState,
        ...createBlogActions(set, get),
      } as BlogStore),
      {
        name: 'maple-blog-content', // localStorage key
        // Only persist essential UI state, not content data
        partialize: (state) => ({
          editorMode: state.editorMode,
          showPreview: state.showPreview,
          autoSaveEnabled: state.autoSaveEnabled,
          searchParams: state.searchParams,
          categorySearchParams: state.categorySearchParams,
          tagSearchParams: state.tagSearchParams,
        }),
        // Initialize on hydration
        onRehydrateStorage: () => (state) => {
          if (state) {
            state.initialize();
          }
        },
      }
    )
  )
);

// Auto-save setup
let autoSaveInterval: NodeJS.Timeout | null = null;

// Setup automatic draft saving
export const setupAutoSave = () => {
  if (autoSaveInterval) {
    clearInterval(autoSaveInterval);
  }

  autoSaveInterval = setInterval(() => {
    const store = useBlogStore.getState();

    if (store.autoSaveEnabled && store.hasUnsavedChanges && store.currentPost) {
      // This would be implemented in the API service layer
      // Auto-saving draft...
    }
  }, AUTO_SAVE_INTERVAL * 1000);
};

// Clean up auto-save
export const cleanupAutoSave = () => {
  if (autoSaveInterval) {
    clearInterval(autoSaveInterval);
    autoSaveInterval = null;
  }
};

// Selectors for better performance
export const blogSelectors = {
  posts: (state: BlogStore) => state.posts,
  currentPost: (state: BlogStore) => state.currentPost,
  categories: (state: BlogStore) => state.categories,
  tags: (state: BlogStore) => state.tags,
  isLoading: (state: BlogStore) => state.isLoading,
  isSaving: (state: BlogStore) => state.isSaving,
  hasUnsavedChanges: (state: BlogStore) => state.hasUnsavedChanges,
  selectedPostIds: (state: BlogStore) => state.selectedPostIds,
  searchParams: (state: BlogStore) => state.searchParams,
  editorMode: (state: BlogStore) => state.editorMode,
  showPreview: (state: BlogStore) => state.showPreview,
  isFullscreen: (state: BlogStore) => state.isFullscreen,
  error: (state: BlogStore) => state.error,

  // Computed selectors
  publishedPosts: (state: BlogStore) =>
    state.posts.filter(p => p.status === PostStatus.Published),
  draftPosts: (state: BlogStore) =>
    state.posts.filter(p => p.status === PostStatus.Draft),
  selectedPostsCount: (state: BlogStore) => state.selectedPostIds.length,
  hasSelectedPosts: (state: BlogStore) => state.selectedPostIds.length > 0,
  canBulkEdit: (state: BlogStore) => state.selectedPostIds.length > 0,
  filteredPosts: (state: BlogStore) => {
    let filtered = state.posts;
    const params = state.searchParams;

    // Apply text search
    if (params.query?.trim()) {
      const query = params.query.toLowerCase();
      filtered = filtered.filter(post =>
        post.title.toLowerCase().includes(query) ||
        post.content.toLowerCase().includes(query) ||
        post.excerpt?.toLowerCase().includes(query)
      );
    }

    // Apply status filter
    if (params.status && params.status.length > 0) {
      filtered = filtered.filter(post => params.status!.includes(post.status));
    }

    // Apply category filter
    if (params.categoryId) {
      filtered = filtered.filter(post => post.categoryId === params.categoryId);
    }

    // Apply tag filter
    if (params.tagIds && params.tagIds.length > 0) {
      filtered = filtered.filter(post =>
        params.tagIds!.some(tagId => post.tags.some(tag => tag.id === tagId))
      );
    }

    // Apply author filter
    if (params.authorId) {
      filtered = filtered.filter(post => post.authorId === params.authorId);
    }

    // Apply date range filter
    if (params.dateFrom) {
      filtered = filtered.filter(post =>
        new Date(post.createdAt) >= new Date(params.dateFrom!)
      );
    }

    if (params.dateTo) {
      filtered = filtered.filter(post =>
        new Date(post.createdAt) <= new Date(params.dateTo!)
      );
    }

    // Apply sticky filter
    if (params.isSticky !== undefined) {
      filtered = filtered.filter(post => post.isSticky === params.isSticky);
    }

    // Apply featured filter
    if (params.isFeatured !== undefined) {
      filtered = filtered.filter(post => post.isFeatured === params.isFeatured);
    }

    // Apply sorting
    if (params.sortBy) {
      filtered = [...filtered].sort((a, b) => {
        let aVal: string | number | Date;
        let bVal: string | number | Date;

        switch (params.sortBy) {
          case 'title':
            aVal = a.title;
            bVal = b.title;
            break;
          case 'publishedAt':
            aVal = a.publishedAt ? new Date(a.publishedAt) : new Date(0);
            bVal = b.publishedAt ? new Date(b.publishedAt) : new Date(0);
            break;
          case 'createdAt':
            aVal = new Date(a.createdAt);
            bVal = new Date(b.createdAt);
            break;
          case 'updatedAt':
            aVal = new Date(a.updatedAt);
            bVal = new Date(b.updatedAt);
            break;
          case 'viewCount':
            aVal = a.viewCount;
            bVal = b.viewCount;
            break;
          case 'likeCount':
            aVal = a.likeCount;
            bVal = b.likeCount;
            break;
          default:
            return 0;
        }

        if (typeof aVal === 'string' && typeof bVal === 'string') {
          return params.sortOrder === 'desc'
            ? (bVal as string).localeCompare(aVal as string)
            : (aVal as string).localeCompare(bVal as string);
        }

        if (aVal instanceof Date && bVal instanceof Date) {
          return params.sortOrder === 'desc'
            ? (bVal as Date).getTime() - (aVal as Date).getTime()
            : (aVal as Date).getTime() - (bVal as Date).getTime();
        }

        if (typeof aVal === 'number' && typeof bVal === 'number') {
          return params.sortOrder === 'desc' ? (bVal as number) - (aVal as number) : (aVal as number) - (bVal as number);
        }

        return 0;
      });
    }

    return filtered;
  },
};

// Export type for external use
export type { BlogStore, BlogLoadingStates };

export default useBlogStore;