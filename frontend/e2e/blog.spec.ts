import { test, expect } from '@playwright/test';

/**
 * End-to-End Tests for Blog Content Management System
 * Tests complete workflows from article creation to publication and reading
 */

test.describe('Blog Content Management E2E Tests', () => {

  test.beforeEach(async ({ page }) => {
    // Mock API responses to avoid dependencies on backend
    await page.route('**/api/**', async (route) => {
      const url = route.request().url();
      const method = route.request().method();

      // Mock authentication check
      if (url.includes('/api/auth/me')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'user-1',
            username: 'testuser',
            email: 'test@example.com',
            role: 'Author'
          })
        });
        return;
      }

      // Mock posts API
      if (url.includes('/api/posts') && method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
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
                allowComments: true
              }
            ],
            totalCount: 1,
            currentPage: 1,
            totalPages: 1,
            pageSize: 10,
            hasNext: false,
            hasPrevious: false
          })
        });
        return;
      }

      // Mock single post API
      if (url.includes('/api/posts/') && method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '1',
            title: 'Test Post 1',
            slug: 'test-post-1',
            content: '# Test Content\n\nThis is a test post with some detailed content for testing the reader.\n\n## Section 1\n\nMore content here.\n\n## Section 2\n\nEven more content to test scrolling and table of contents.',
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
            allowComments: true
          })
        });
        return;
      }

      // Mock post creation
      if (url.includes('/api/posts') && method === 'POST') {
        const postData = await route.request().postDataJSON();
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'new-post-id',
            title: postData.title,
            slug: postData.title.toLowerCase().replace(/\s+/g, '-'),
            content: postData.content,
            summary: postData.summary,
            status: 'Draft',
            authorId: 'author-1',
            authorName: 'Test Author',
            categoryId: postData.categoryId,
            categoryName: 'Technology',
            tags: postData.tags || [],
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            viewCount: 0,
            likeCount: 0,
            commentCount: 0,
            allowComments: true
          })
        });
        return;
      }

      // Mock post update
      if (url.includes('/api/posts/') && method === 'PUT') {
        const postData = await route.request().postDataJSON();
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: 'new-post-id',
            title: postData.title,
            content: postData.content,
            summary: postData.summary,
            status: 'Draft',
            authorId: 'author-1',
            authorName: 'Test Author',
            updatedAt: new Date().toISOString()
          })
        });
        return;
      }

      // Mock post publish
      if (url.includes('/publish') && method === 'POST') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Post published successfully' })
        });
        return;
      }

      // Mock categories
      if (url.includes('/api/categories')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: 'cat-1', name: 'Technology', slug: 'technology' },
            { id: 'cat-2', name: 'Programming', slug: 'programming' }
          ])
        });
        return;
      }

      // Mock tags
      if (url.includes('/api/tags')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            { id: 'tag-1', name: 'React', slug: 'react' },
            { id: 'tag-2', name: 'Testing', slug: 'testing' }
          ])
        });
        return;
      }

      // Mock file upload
      if (url.includes('/api/files/upload')) {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            url: '/uploads/test-image.jpg',
            fileName: 'test-image.jpg',
            size: 12345
          })
        });
        return;
      }

      // Default fallback
      await route.fulfill({ status: 404, body: 'Not Found' });
    });
  });

  test.describe('Article Creation Workflow', () => {
    test('should complete full article creation process', async ({ page }) => {
      // Navigate to create post page
      await page.goto('/admin/posts/new');

      // Wait for the page to load
      await expect(page.locator('[data-testid="markdown-editor"]')).toBeVisible();

      // Fill in post details
      await page.fill('[data-testid="post-title-input"]', 'My New Blog Post');
      await page.fill('[data-testid="post-summary-input"]', 'This is a summary of my new blog post.');

      // Select category
      await page.selectOption('[data-testid="category-select"]', 'cat-1');

      // Add content using markdown editor
      const editor = page.locator('[data-testid="markdown-textarea"]');
      await editor.fill(`# Introduction

This is my new blog post about testing and development.

## Key Points

1. Testing is important
2. End-to-end tests verify complete workflows
3. User experience matters

## Code Example

\`\`\`javascript
function greet(name) {
  return \`Hello, \${name}!\`;
}
\`\`\`

## Conclusion

This post demonstrates the blog creation workflow.`);

      // Test markdown toolbar functionality
      await editor.click();
      await editor.selectText({ start: 0, end: 12 }); // Select "# Introduction"
      await page.click('[data-testid="toolbar-bold"]');

      // Verify bold markdown was applied
      await expect(editor).toContainText('**# Introduction**');

      // Test preview functionality
      await page.click('[data-testid="preview-toggle"]');
      await expect(page.locator('[data-testid="markdown-preview"]')).toBeVisible();
      await expect(page.locator('[data-testid="markdown-textarea"]')).not.toBeVisible();

      // Switch back to edit mode
      await page.click('[data-testid="preview-toggle"]');
      await expect(page.locator('[data-testid="markdown-textarea"]')).toBeVisible();

      // Save as draft
      await page.click('[data-testid="save-draft-button"]');

      // Verify success message
      await expect(page.locator('[data-testid="success-message"]')).toContainText('Post saved successfully');
    });

    test('should handle image upload in markdown editor', async ({ page }) => {
      await page.goto('/admin/posts/new');

      await expect(page.locator('[data-testid="markdown-editor"]')).toBeVisible();

      // Create a test file
      const fileBuffer = Buffer.from('test image data');

      // Set up file chooser handler
      page.on('filechooser', async (fileChooser) => {
        await fileChooser.setFiles({
          name: 'test-image.jpg',
          mimeType: 'image/jpeg',
          buffer: fileBuffer
        });
      });

      // Click image upload button
      await page.click('[data-testid="toolbar-image"]');

      // Verify image markdown was inserted
      const editor = page.locator('[data-testid="markdown-textarea"]');
      await expect(editor).toContainText('![test-image.jpg](/uploads/test-image.jpg)');
    });

    test('should validate required fields', async ({ page }) => {
      await page.goto('/admin/posts/new');

      // Try to save without title
      await page.click('[data-testid="save-draft-button"]');

      // Verify validation error
      await expect(page.locator('[data-testid="title-error"]')).toContainText('Title is required');

      // Fill title but leave content empty
      await page.fill('[data-testid="post-title-input"]', 'Test Post');
      await page.click('[data-testid="save-draft-button"]');

      // Verify content validation error
      await expect(page.locator('[data-testid="content-error"]')).toContainText('Content is required');
    });
  });

  test.describe('Article Publishing Workflow', () => {
    test('should publish a draft post', async ({ page }) => {
      await page.goto('/admin/posts/draft/new-post-id');

      // Verify post loads in edit mode
      await expect(page.locator('[data-testid="post-title-input"]')).toBeVisible();

      // Click publish button
      await page.click('[data-testid="publish-button"]');

      // Confirm publication in modal
      await expect(page.locator('[data-testid="publish-modal"]')).toBeVisible();
      await page.click('[data-testid="confirm-publish-button"]');

      // Verify success message
      await expect(page.locator('[data-testid="success-message"]')).toContainText('Post published successfully');

      // Verify redirect to published post
      await expect(page).toHaveURL(/\/posts\//);
    });

    test('should schedule a post for future publication', async ({ page }) => {
      await page.goto('/admin/posts/draft/new-post-id');

      // Click schedule button
      await page.click('[data-testid="schedule-button"]');

      // Set future date and time
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 1);

      await page.fill('[data-testid="schedule-date-input"]', futureDate.toISOString().split('T')[0]);
      await page.fill('[data-testid="schedule-time-input"]', '10:00');

      await page.click('[data-testid="confirm-schedule-button"]');

      // Verify success message
      await expect(page.locator('[data-testid="success-message"]')).toContainText('Post scheduled successfully');
    });
  });

  test.describe('Article Reading Experience', () => {
    test('should display post content correctly', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Verify post content loads
      await expect(page.locator('[data-testid="post-reader"]')).toBeVisible();
      await expect(page.locator('[data-testid="post-title"]')).toContainText('Test Post 1');
      await expect(page.locator('[data-testid="post-content"]')).toBeVisible();

      // Verify metadata
      await expect(page.locator('[data-testid="post-author"]')).toContainText('Test Author');
      await expect(page.locator('[data-testid="reading-time"]')).toContainText('3 min read');
      await expect(page.locator('[data-testid="post-category"]')).toContainText('Technology');

      // Verify stats
      await expect(page.locator('[data-testid="view-count"]')).toContainText('10 views');
      await expect(page.locator('[data-testid="like-count"]')).toContainText('5 likes');
      await expect(page.locator('[data-testid="comment-count"]')).toContainText('2 comments');
    });

    test('should show table of contents for long posts', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Verify table of contents appears
      await expect(page.locator('[data-testid="table-of-contents"]')).toBeVisible();
      await expect(page.locator('[data-testid="table-of-contents"]')).toContainText('On this page');

      // Verify TOC links work
      const tocLink = page.locator('[data-testid="toc-link-heading-0"]');
      await tocLink.click();

      // Should scroll to the heading (we can't easily test scroll position, but the click should work)
      await expect(tocLink).toBeVisible();
    });

    test('should track reading progress', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Verify progress bar exists
      await expect(page.locator('[data-testid="reading-progress"]')).toBeVisible();

      // Initial progress should be 0
      const progressBar = page.locator('[data-testid="reading-progress"]');
      await expect(progressBar).toHaveCSS('width', '0px');

      // Scroll down the page
      await page.evaluate(() => window.scrollTo(0, 500));

      // Progress should update (we can't easily test exact width, but it should change)
      await page.waitForTimeout(100); // Allow for scroll event handling
    });

    test('should handle like and bookmark interactions', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Test like functionality
      const likeButton = page.locator('[data-testid="like-post-button"]');
      await expect(likeButton).toContainText('Like (5)');

      await likeButton.click();
      await expect(likeButton).toContainText('Like (6)');

      // Test bookmark functionality
      const bookmarkButton = page.locator('[data-testid="bookmark-post-button"]');
      await bookmarkButton.click();

      // Verify visual state change (bookmark should become active)
      await expect(bookmarkButton).toHaveClass(/text-yellow-600/);
    });

    test('should handle share functionality', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Test share button
      await page.click('[data-testid="share-post-button"]');

      // Should trigger share handler (in real app might open share modal or use Web Share API)
      // For this test, we just verify the button is clickable
      await expect(page.locator('[data-testid="share-post-button"]')).toBeVisible();
    });
  });

  test.describe('Post List and Navigation', () => {
    test('should display post list correctly', async ({ page }) => {
      await page.goto('/posts');

      // Verify post list loads
      await expect(page.locator('[data-testid="post-list"]')).toBeVisible();
      await expect(page.locator('[data-testid="post-card-1"]')).toBeVisible();

      // Verify post card content
      await expect(page.locator('[data-testid="post-card-1"]')).toContainText('Test Post 1');
      await expect(page.locator('[data-testid="post-author-1"]')).toContainText('Test Author');
      await expect(page.locator('[data-testid="post-reading-time-1"]')).toContainText('3 min read');

      // Test post navigation
      await page.click('[data-testid="post-link-1"]');
      await expect(page).toHaveURL('/posts/test-post-1');
    });

    test('should handle empty post list', async ({ page }) => {
      // Mock empty response
      await page.route('**/api/posts*', async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [],
            totalCount: 0,
            currentPage: 1,
            totalPages: 0,
            pageSize: 10,
            hasNext: false,
            hasPrevious: false
          })
        });
      });

      await page.goto('/posts');

      // Verify empty state
      await expect(page.locator('[data-testid="post-list-empty"]')).toBeVisible();
      await expect(page.locator('[data-testid="post-list-empty"]')).toContainText('No posts found');
    });

    test('should handle loading and error states', async ({ page }) => {
      // Mock slow response for loading state
      await page.route('**/api/posts*', async (route) => {
        await page.waitForTimeout(1000); // Delay response
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ items: [], totalCount: 0 })
        });
      });

      await page.goto('/posts');

      // Should show loading state initially
      await expect(page.locator('[data-testid="post-list-loading"]')).toBeVisible();

      // Mock error response
      await page.route('**/api/posts*', async (route) => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Server error' })
        });
      });

      await page.reload();

      // Should show error state
      await expect(page.locator('[data-testid="post-list-error"]')).toBeVisible();
      await expect(page.locator('[data-testid="post-list-error"]')).toContainText('Error loading posts');
    });
  });

  test.describe('Mobile Responsiveness', () => {
    test('should work correctly on mobile devices', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 }); // iPhone SE size

      await page.goto('/posts/test-post-1');

      // Verify mobile layout
      await expect(page.locator('[data-testid="post-reader"]')).toBeVisible();
      await expect(page.locator('[data-testid="post-title"]')).toBeVisible();

      // Table of contents should be hidden or collapsed on mobile
      const toc = page.locator('[data-testid="table-of-contents"]');
      const isVisible = await toc.isVisible();

      if (isVisible) {
        // If visible, should be responsive
        const tocBox = await toc.boundingBox();
        expect(tocBox?.width).toBeLessThan(375); // Should fit in mobile viewport
      }
    });

    test('should handle touch interactions', async ({ page }) => {
      await page.setViewportSize({ width: 375, height: 667 });

      await page.goto('/posts/test-post-1');

      // Test touch interactions on action buttons
      await page.tap('[data-testid="like-post-button"]');
      await expect(page.locator('[data-testid="like-post-button"]')).toContainText('Like (6)');

      await page.tap('[data-testid="bookmark-post-button"]');
      await expect(page.locator('[data-testid="bookmark-post-button"]')).toHaveClass(/text-yellow-600/);
    });
  });

  test.describe('Performance and Accessibility', () => {
    test('should meet performance expectations', async ({ page }) => {
      // Start timing
      const startTime = Date.now();

      await page.goto('/posts/test-post-1');

      // Wait for content to load
      await expect(page.locator('[data-testid="post-content"]')).toBeVisible();

      const loadTime = Date.now() - startTime;

      // Should load within reasonable time (adjust threshold as needed)
      expect(loadTime).toBeLessThan(3000); // 3 seconds
    });

    test('should have proper heading hierarchy', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Verify H1 exists and is unique
      const h1Elements = await page.locator('h1').count();
      expect(h1Elements).toBe(1);

      // Verify proper heading structure
      await expect(page.locator('h1')).toContainText('Test Post 1');
    });

    test('should have accessible navigation', async ({ page }) => {
      await page.goto('/posts/test-post-1');

      // Test keyboard navigation
      await page.keyboard.press('Tab'); // Should focus on back button or first interactive element
      await page.keyboard.press('Enter'); // Should activate focused element

      // Verify links have proper accessibility attributes
      const backButton = page.locator('[data-testid="back-button"]');
      if (await backButton.isVisible()) {
        await expect(backButton).toHaveAttribute('href');
      }
    });
  });

  test.describe('Search and Filtering', () => {
    test('should handle search functionality', async ({ page }) => {
      await page.goto('/posts');

      // Mock search response
      await page.route('**/api/posts*search=test*', async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [{
              id: '1',
              title: 'Test Search Result',
              slug: 'test-search-result',
              summary: 'This matches the search term',
              authorName: 'Test Author',
              tags: ['test'],
              readingTime: 2
            }],
            totalCount: 1
          })
        });
      });

      // Perform search
      const searchInput = page.locator('[data-testid="search-input"]');
      if (await searchInput.isVisible()) {
        await searchInput.fill('test');
        await searchInput.press('Enter');

        // Verify search results
        await expect(page.locator('[data-testid="post-list"]')).toBeVisible();
        await expect(page.locator('[data-testid="post-list"]')).toContainText('Test Search Result');
      }
    });
  });

  test.describe('Error Handling and Recovery', () => {
    test('should handle network failures gracefully', async ({ page }) => {
      await page.goto('/posts');

      // Simulate network failure
      await page.route('**/api/posts*', route => route.abort());

      await page.reload();

      // Should show error state
      await expect(page.locator('[data-testid="post-list-error"]')).toBeVisible();
    });

    test('should recover from errors', async ({ page }) => {
      await page.goto('/posts');

      // First, cause an error
      await page.route('**/api/posts*', route => route.abort());
      await page.reload();
      await expect(page.locator('[data-testid="post-list-error"]')).toBeVisible();

      // Then restore normal response
      await page.route('**/api/posts*', async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [{ id: '1', title: 'Recovered Post', slug: 'recovered-post' }],
            totalCount: 1
          })
        });
      });

      // Retry (could be via retry button or page refresh)
      await page.reload();

      // Should show content again
      await expect(page.locator('[data-testid="post-list"]')).toBeVisible();
    });
  });
});