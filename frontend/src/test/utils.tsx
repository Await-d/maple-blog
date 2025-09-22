// @ts-nocheck
import React from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Create a custom render function that includes providers
function AllTheProviders({ children }: { children: React.ReactNode }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  });

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {children}
      </BrowserRouter>
    </QueryClientProvider>
  );
}

const customRender = (
  ui: React.ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) => render(ui, { wrapper: AllTheProviders, ...options });

export * from '@testing-library/react';
export { customRender as render };

// Helper function to create a mock post
export const createMockPost = (overrides = {}) => ({
  id: '1',
  title: 'Test Post',
  slug: 'test-post',
  content: '# Test Content\n\nThis is a test post.',
  summary: 'Test summary',
  status: 'Published',
  authorId: 'author-1',
  authorName: 'Test Author',
  categoryId: 'cat-1',
  categoryName: 'Technology',
  tags: ['react', 'testing'],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
  publishedAt: '2023-01-01T00:00:00Z',
  viewCount: 10,
  likeCount: 5,
  commentCount: 2,
  readingTime: 3,
  wordCount: 500,
  allowComments: true,
  isFeatured: false,
  isSticky: false,
  ...overrides
});

// Helper function to create a mock category
export const createMockCategory = (overrides = {}) => ({
  id: 'cat-1',
  name: 'Technology',
  slug: 'technology',
  description: 'Tech posts',
  postCount: 5,
  ...overrides
});

// Helper function to create a mock tag
export const createMockTag = (overrides = {}) => ({
  id: 'tag-1',
  name: 'React',
  slug: 'react',
  usageCount: 3,
  ...overrides
});