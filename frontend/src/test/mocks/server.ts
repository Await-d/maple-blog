// @ts-nocheck
import { setupServer } from 'msw/node';
import { rest } from 'msw';

// Mock data
const mockPosts = [
  {
    id: '1',
    title: 'Test Post 1',
    slug: 'test-post-1',
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
    isSticky: false
  },
  {
    id: '2',
    title: 'Test Post 2',
    slug: 'test-post-2',
    content: '# Another Test\n\nAnother test post with different content.',
    summary: 'Another test summary',
    status: 'Published',
    authorId: 'author-2',
    authorName: 'Another Author',
    categoryId: 'cat-2',
    categoryName: 'Programming',
    tags: ['javascript', 'node'],
    createdAt: '2023-01-02T00:00:00Z',
    updatedAt: '2023-01-02T00:00:00Z',
    publishedAt: '2023-01-02T00:00:00Z',
    viewCount: 20,
    likeCount: 8,
    commentCount: 3,
    readingTime: 5,
    wordCount: 800,
    allowComments: true,
    isFeatured: true,
    isSticky: false
  }
];

const mockCategories = [
  { id: 'cat-1', name: 'Technology', slug: 'technology', description: 'Tech posts', postCount: 5 },
  { id: 'cat-2', name: 'Programming', slug: 'programming', description: 'Programming posts', postCount: 8 }
];

const mockTags = [
  { id: 'tag-1', name: 'React', slug: 'react', usageCount: 3 },
  { id: 'tag-2', name: 'Testing', slug: 'testing', usageCount: 2 },
  { id: 'tag-3', name: 'JavaScript', slug: 'javascript', usageCount: 5 },
  { id: 'tag-4', name: 'Node.js', slug: 'nodejs', usageCount: 4 }
];

export const handlers = [
  // Posts endpoints
  rest.get('/api/posts', (req, res, ctx) => {
    const page = Number(req.url.searchParams.get('page')) || 1;
    const pageSize = Number(req.url.searchParams.get('pageSize')) || 10;
    const search = req.url.searchParams.get('search');

    let filteredPosts = mockPosts;

    if (search) {
      filteredPosts = mockPosts.filter(post =>
        post.title.toLowerCase().includes(search.toLowerCase()) ||
        post.content.toLowerCase().includes(search.toLowerCase())
      );
    }

    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const paginatedPosts = filteredPosts.slice(startIndex, endIndex);

    return res(
      ctx.json({
        items: paginatedPosts,
        totalCount: filteredPosts.length,
        currentPage: page,
        totalPages: Math.ceil(filteredPosts.length / pageSize),
        pageSize,
        hasNext: endIndex < filteredPosts.length,
        hasPrevious: page > 1
      })
    );
  }),

  rest.get('/api/posts/:id', (req, res, ctx) => {
    const { id } = req.params;
    const post = mockPosts.find(p => p.id === id);

    if (!post) {
      return res(ctx.status(404), ctx.json({ message: 'Post not found' }));
    }

    return res(ctx.json(post));
  }),

  rest.get('/api/posts/slug/:slug', (req, res, ctx) => {
    const { slug } = req.params;
    const post = mockPosts.find(p => p.slug === slug);

    if (!post) {
      return res(ctx.status(404), ctx.json({ message: 'Post not found' }));
    }

    return res(ctx.json(post));
  }),

  rest.post('/api/posts', (req, res, ctx) => {
    return res(ctx.status(201), ctx.json({
      ...mockPosts[0],
      id: 'new-post-id',
      title: 'New Test Post'
    }));
  }),

  rest.put('/api/posts/:id', (req, res, ctx) => {
    const { id } = req.params;
    const post = mockPosts.find(p => p.id === id);

    if (!post) {
      return res(ctx.status(404), ctx.json({ message: 'Post not found' }));
    }

    return res(ctx.json({ ...post, title: 'Updated Post' }));
  }),

  rest.delete('/api/posts/:id', (req, res, ctx) => {
    const { id } = req.params;
    const post = mockPosts.find(p => p.id === id);

    if (!post) {
      return res(ctx.status(404), ctx.json({ message: 'Post not found' }));
    }

    return res(ctx.json({ message: 'Post deleted successfully' }));
  }),

  // Categories endpoints
  rest.get('/api/categories', (req, res, ctx) => {
    return res(ctx.json(mockCategories));
  }),

  rest.get('/api/categories/:id', (req, res, ctx) => {
    const { id } = req.params;
    const category = mockCategories.find(c => c.id === id);

    if (!category) {
      return res(ctx.status(404), ctx.json({ message: 'Category not found' }));
    }

    return res(ctx.json(category));
  }),

  // Tags endpoints
  rest.get('/api/tags', (req, res, ctx) => {
    return res(ctx.json(mockTags));
  }),

  // File upload endpoint
  rest.post('/api/files/upload', (req, res, ctx) => {
    return res(ctx.json({
      url: '/uploads/test-image.jpg',
      fileName: 'test-image.jpg',
      size: 12345
    }));
  })
];

export const server = setupServer(...handlers);