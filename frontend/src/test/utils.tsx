/* eslint-disable react-refresh/only-export-components */
import { render, RenderOptions } from '@testing-library/react';
import { AllTheProviders } from './test-providers';

// Re-export everything from testing-library
export * from '@testing-library/react';

// Custom render with providers
const customRender = (
  ui: React.ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) => render(ui, { wrapper: AllTheProviders, ...options });

export { customRender as render };

// Helper function to create mock data
export const createMockUser = () => ({
  id: '1',
  username: 'testuser',
  email: 'test@example.com',
  firstName: 'Test',
  lastName: 'User',
  role: 'user' as const,
  isActive: true,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
});

export const createMockPost = () => ({
  id: '1',
  title: 'Test Post',
  content: 'This is a test post content',
  excerpt: 'Test excerpt',
  slug: 'test-post',
  status: 'published' as const,
  authorId: '1',
  categoryId: '1',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
});