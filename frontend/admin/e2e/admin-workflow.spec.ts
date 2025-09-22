import { test, expect, Page } from '@playwright/test';

// Test configuration
const ADMIN_BASE_URL = 'http://localhost:3001'; // Admin panel runs on different port
const API_BASE_URL = 'http://localhost:5000';

// Test data
const TEST_ADMIN_USER = {
  username: 'admin@test.com',
  password: 'TestPassword123!',
  role: 'admin',
};

const TEST_MODERATOR_USER = {
  username: 'moderator@test.com', 
  password: 'TestPassword123!',
  role: 'moderator',
};

const TEST_POST_DATA = {
  title: 'E2E Test Post',
  content: 'This is a test post created during E2E testing.',
  category: 'Technology',
  tags: ['testing', 'e2e', 'automation'],
  status: 'published',
};

const TEST_USER_DATA = {
  username: 'testuser@example.com',
  displayName: 'Test User',
  role: 'user',
  status: 'active',
};

// Helper functions
async function loginAsAdmin(page: Page) {
  await page.goto(`${ADMIN_BASE_URL}/login`);
  
  // Fill login form
  await page.fill('[data-testid="username-input"]', TEST_ADMIN_USER.username);
  await page.fill('[data-testid="password-input"]', TEST_ADMIN_USER.password);
  
  // Submit form
  await page.click('[data-testid="login-button"]');
  
  // Wait for redirect to dashboard
  await page.waitForURL(`${ADMIN_BASE_URL}/dashboard`);
  
  // Verify successful login
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
  await expect(page.locator('text=管理仪表盘')).toBeVisible();
}

async function loginAsModerator(page: Page) {
  await page.goto(`${ADMIN_BASE_URL}/login`);
  
  await page.fill('[data-testid="username-input"]', TEST_MODERATOR_USER.username);
  await page.fill('[data-testid="password-input"]', TEST_MODERATOR_USER.password);
  await page.click('[data-testid="login-button"]');
  
  await page.waitForURL(`${ADMIN_BASE_URL}/dashboard`);
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
}

async function navigateToSection(page: Page, sectionName: string) {
  // Click on sidebar menu item
  await page.click(`[data-testid="sidebar-${sectionName}"]`);
  
  // Wait for navigation
  await page.waitForURL(`${ADMIN_BASE_URL}/${sectionName}**`);
  
  // Verify page loaded
  await page.waitForLoadState('networkidle');
}

async function createTestPost(page: Page) {
  await navigateToSection(page, 'content');
  
  // Click create new post button
  await page.click('[data-testid="create-post-button"]');
  
  // Fill post form
  await page.fill('[data-testid="post-title-input"]', TEST_POST_DATA.title);
  await page.fill('[data-testid="post-content-textarea"]', TEST_POST_DATA.content);
  
  // Select category
  await page.click('[data-testid="category-select"]');
  await page.click(`text=${TEST_POST_DATA.category}`);
  
  // Add tags
  for (const tag of TEST_POST_DATA.tags) {
    await page.fill('[data-testid="tag-input"]', tag);
    await page.press('[data-testid="tag-input"]', 'Enter');
  }
  
  // Set status
  await page.selectOption('[data-testid="status-select"]', TEST_POST_DATA.status);
  
  // Save post
  await page.click('[data-testid="save-post-button"]');
  
  // Wait for success message
  await expect(page.locator('.ant-message-success')).toBeVisible();
  
  return TEST_POST_DATA.title;
}

async function createTestUser(page: Page) {
  await navigateToSection(page, 'users');
  
  // Click create user button
  await page.click('[data-testid="create-user-button"]');
  
  // Fill user form
  await page.fill('[data-testid="user-email-input"]', TEST_USER_DATA.username);
  await page.fill('[data-testid="user-name-input"]', TEST_USER_DATA.displayName);
  await page.selectOption('[data-testid="user-role-select"]', TEST_USER_DATA.role);
  await page.selectOption('[data-testid="user-status-select"]', TEST_USER_DATA.status);
  
  // Save user
  await page.click('[data-testid="save-user-button"]');
  
  // Wait for success message
  await expect(page.locator('.ant-message-success')).toBeVisible();
  
  return TEST_USER_DATA.username;
}

// Test suite
test.describe('Admin Dashboard Workflow', () => {
  test.beforeEach(async ({ page }) => {
    // Set up test data or clean state if needed
    await page.route(`${API_BASE_URL}/**`, (route) => {
      // Intercept API calls if needed for mocking
      route.continue();
    });
  });

  test.describe('Authentication & Authorization', () => {
    test('should login as admin successfully', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Verify admin dashboard is accessible
      await expect(page.locator('text=管理仪表盘')).toBeVisible();
      await expect(page.locator('[data-testid="admin-stats"]')).toBeVisible();
    });

    test('should login as moderator with limited access', async ({ page }) => {
      await loginAsModerator(page);
      
      // Verify limited access - some admin features should be hidden
      await expect(page.locator('text=管理仪表盘')).toBeVisible();
      
      // Check that certain admin-only features are not visible
      await expect(page.locator('[data-testid="system-settings"]')).not.toBeVisible();
    });

    test('should handle invalid login credentials', async ({ page }) => {
      await page.goto(`${ADMIN_BASE_URL}/login`);
      
      await page.fill('[data-testid="username-input"]', 'invalid@email.com');
      await page.fill('[data-testid="password-input"]', 'wrongpassword');
      await page.click('[data-testid="login-button"]');
      
      // Should show error message
      await expect(page.locator('.ant-message-error')).toBeVisible();
      
      // Should stay on login page
      await expect(page.url()).toContain('/login');
    });

    test('should logout successfully', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Click user menu
      await page.click('[data-testid="user-menu"]');
      
      // Click logout
      await page.click('[data-testid="logout-button"]');
      
      // Should redirect to login page
      await page.waitForURL(`${ADMIN_BASE_URL}/login`);
      await expect(page.locator('[data-testid="login-form"]')).toBeVisible();
    });

    test('should redirect unauthenticated users to login', async ({ page }) => {
      // Try to access dashboard without login
      await page.goto(`${ADMIN_BASE_URL}/dashboard`);
      
      // Should redirect to login
      await page.waitForURL(`${ADMIN_BASE_URL}/login`);
      await expect(page.locator('[data-testid="login-form"]')).toBeVisible();
    });
  });

  test.describe('Dashboard Functionality', () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test('should display dashboard statistics correctly', async ({ page }) => {
      // Verify stat cards are present
      await expect(page.locator('[data-testid="user-stats-card"]')).toBeVisible();
      await expect(page.locator('[data-testid="content-stats-card"]')).toBeVisible();
      await expect(page.locator('[data-testid="system-stats-card"]')).toBeVisible();
      await expect(page.locator('[data-testid="performance-stats-card"]')).toBeVisible();
      
      // Verify charts are rendered
      await expect(page.locator('[data-testid="traffic-chart"]')).toBeVisible();
      await expect(page.locator('[data-testid="performance-chart"]')).toBeVisible();
      await expect(page.locator('[data-testid="users-chart"]')).toBeVisible();
    });

    test('should refresh dashboard data', async ({ page }) => {
      // Click refresh button
      await page.click('[data-testid="refresh-dashboard"]');
      
      // Should show loading state
      await expect(page.locator('.ant-spin')).toBeVisible();
      
      // Wait for refresh to complete
      await page.waitForLoadState('networkidle');
      
      // Verify data is updated
      await expect(page.locator('[data-testid="last-updated"]')).toBeVisible();
    });

    test('should toggle dashboard layout', async ({ page }) => {
      // Open settings dropdown
      await page.click('[data-testid="dashboard-settings"]');
      
      // Click compact layout
      await page.click('[data-testid="compact-layout"]');
      
      // Verify layout changed
      await expect(page.locator('.dashboard-compact')).toBeVisible();
      
      // Switch back to default
      await page.click('[data-testid="dashboard-settings"]');
      await page.click('[data-testid="default-layout"]');
      
      await expect(page.locator('.dashboard-default')).toBeVisible();
    });

    test('should export dashboard data', async ({ page }) => {
      // Open settings dropdown
      await page.click('[data-testid="dashboard-settings"]');
      
      // Start download
      const downloadPromise = page.waitForEvent('download');
      await page.click('[data-testid="export-json"]');
      
      const download = await downloadPromise;
      
      // Verify download started
      expect(download.suggestedFilename()).toContain('dashboard-data');
    });

    test('should show system health status', async ({ page }) => {
      // Verify health status is displayed
      await expect(page.locator('[data-testid="health-status"]')).toBeVisible();
      
      // Check individual service statuses
      await expect(page.locator('[data-testid="database-status"]')).toBeVisible();
      await expect(page.locator('[data-testid="cache-status"]')).toBeVisible();
      await expect(page.locator('[data-testid="api-status"]')).toBeVisible();
    });
  });

  test.describe('Content Management', () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test('should create a new blog post', async ({ page }) => {
      const postTitle = await createTestPost(page);
      
      // Verify post appears in list
      await expect(page.locator(`text=${postTitle}`)).toBeVisible();
      
      // Verify post can be viewed
      await page.click(`[data-testid="view-post-${postTitle}"]`);
      await expect(page.locator(`text=${TEST_POST_DATA.content}`)).toBeVisible();
    });

    test('should edit existing blog post', async ({ page }) => {
      // First create a post
      const postTitle = await createTestPost(page);
      
      // Edit the post
      await page.click(`[data-testid="edit-post-${postTitle}"]`);
      
      const updatedTitle = `${postTitle} - Updated`;
      await page.fill('[data-testid="post-title-input"]', updatedTitle);
      
      await page.click('[data-testid="save-post-button"]');
      
      // Verify update
      await expect(page.locator(`text=${updatedTitle}`)).toBeVisible();
    });

    test('should delete blog post', async ({ page }) => {
      // Create a post
      const postTitle = await createTestPost(page);
      
      // Delete the post
      await page.click(`[data-testid="delete-post-${postTitle}"]`);
      
      // Confirm deletion
      await page.click('[data-testid="confirm-delete"]');
      
      // Verify post is removed
      await expect(page.locator(`text=${postTitle}`)).not.toBeVisible();
    });

    test('should manage categories', async ({ page }) => {
      await navigateToSection(page, 'content');
      await page.click('[data-testid="categories-tab"]');
      
      // Create new category
      await page.click('[data-testid="create-category-button"]');
      await page.fill('[data-testid="category-name-input"]', 'E2E Test Category');
      await page.fill('[data-testid="category-description-input"]', 'Category for E2E testing');
      await page.click('[data-testid="save-category-button"]');
      
      // Verify category appears
      await expect(page.locator('text=E2E Test Category')).toBeVisible();
    });

    test('should manage tags', async ({ page }) => {
      await navigateToSection(page, 'content');
      await page.click('[data-testid="tags-tab"]');
      
      // Create new tag
      await page.click('[data-testid="create-tag-button"]');
      await page.fill('[data-testid="tag-name-input"]', 'e2e-test-tag');
      await page.click('[data-testid="save-tag-button"]');
      
      // Verify tag appears
      await expect(page.locator('text=e2e-test-tag')).toBeVisible();
    });

    test('should bulk manage posts', async ({ page }) => {
      await navigateToSection(page, 'content');
      
      // Select multiple posts
      await page.check('[data-testid="select-post-1"]');
      await page.check('[data-testid="select-post-2"]');
      
      // Perform bulk action
      await page.click('[data-testid="bulk-actions-dropdown"]');
      await page.click('[data-testid="bulk-publish"]');
      
      // Confirm action
      await page.click('[data-testid="confirm-bulk-action"]');
      
      // Verify success message
      await expect(page.locator('.ant-message-success')).toBeVisible();
    });
  });

  test.describe('User Management', () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test('should create a new user', async ({ page }) => {
      const userEmail = await createTestUser(page);
      
      // Verify user appears in list
      await expect(page.locator(`text=${userEmail}`)).toBeVisible();
      
      // Verify user details
      await page.click(`[data-testid="view-user-${userEmail}"]`);
      await expect(page.locator(`text=${TEST_USER_DATA.displayName}`)).toBeVisible();
    });

    test('should edit user details', async ({ page }) => {
      // Create a user first
      const userEmail = await createTestUser(page);
      
      // Edit the user
      await page.click(`[data-testid="edit-user-${userEmail}"]`);
      
      const updatedName = `${TEST_USER_DATA.displayName} - Updated`;
      await page.fill('[data-testid="user-name-input"]', updatedName);
      
      await page.click('[data-testid="save-user-button"]');
      
      // Verify update
      await expect(page.locator(`text=${updatedName}`)).toBeVisible();
    });

    test('should change user role', async ({ page }) => {
      const userEmail = await createTestUser(page);
      
      // Edit user role
      await page.click(`[data-testid="edit-user-${userEmail}"]`);
      await page.selectOption('[data-testid="user-role-select"]', 'moderator');
      await page.click('[data-testid="save-user-button"]');
      
      // Verify role change
      await expect(page.locator('[data-testid="user-role-moderator"]')).toBeVisible();
    });

    test('should suspend and activate user', async ({ page }) => {
      const userEmail = await createTestUser(page);
      
      // Suspend user
      await page.click(`[data-testid="suspend-user-${userEmail}"]`);
      await page.click('[data-testid="confirm-suspend"]');
      
      // Verify suspended status
      await expect(page.locator('[data-testid="user-status-suspended"]')).toBeVisible();
      
      // Activate user
      await page.click(`[data-testid="activate-user-${userEmail}"]`);
      await page.click('[data-testid="confirm-activate"]');
      
      // Verify active status
      await expect(page.locator('[data-testid="user-status-active"]')).toBeVisible();
    });

    test('should search and filter users', async ({ page }) => {
      await navigateToSection(page, 'users');
      
      // Test search functionality
      await page.fill('[data-testid="user-search-input"]', 'test');
      await page.press('[data-testid="user-search-input"]', 'Enter');
      
      // Wait for search results
      await page.waitForTimeout(500);
      
      // Test role filter
      await page.click('[data-testid="role-filter"]');
      await page.click('[data-testid="filter-admin"]');
      
      // Verify filtered results
      await expect(page.locator('[data-testid="admin-user-row"]')).toBeVisible();
    });
  });

  test.describe('Data Table Interactions', () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test('should sort data table columns', async ({ page }) => {
      await navigateToSection(page, 'users');
      
      // Click sort on name column
      await page.click('[data-testid="sort-name-column"]');
      
      // Verify sorting indicator
      await expect(page.locator('.ant-table-column-sorter-up.active')).toBeVisible();
      
      // Click again for reverse sort
      await page.click('[data-testid="sort-name-column"]');
      await expect(page.locator('.ant-table-column-sorter-down.active')).toBeVisible();
    });

    test('should filter data table columns', async ({ page }) => {
      await navigateToSection(page, 'users');
      
      // Open filter dropdown
      await page.click('[data-testid="filter-role-column"]');
      
      // Select filter option
      await page.check('[data-testid="filter-admin-option"]');
      await page.click('[data-testid="apply-filter"]');
      
      // Verify filtered results
      await expect(page.locator('[data-testid="admin-users-only"]')).toBeVisible();
    });

    test('should configure visible columns', async ({ page }) => {
      await navigateToSection(page, 'users');
      
      // Open column settings
      await page.click('[data-testid="column-settings"]');
      
      // Hide email column
      await page.uncheck('[data-testid="show-email-column"]');
      await page.click('[data-testid="apply-column-settings"]');
      
      // Verify email column is hidden
      await expect(page.locator('[data-testid="email-column-header"]')).not.toBeVisible();
    });

    test('should export table data', async ({ page }) => {
      await navigateToSection(page, 'users');
      
      // Click export button
      const downloadPromise = page.waitForEvent('download');
      await page.click('[data-testid="export-table-data"]');
      
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toContain('users');
    });

    test('should handle pagination', async ({ page }) => {
      await navigateToSection(page, 'users');
      
      // Go to next page
      await page.click('[data-testid="next-page"]');
      
      // Verify page change
      await expect(page.locator('[data-testid="current-page-2"]')).toBeVisible();
      
      // Change page size
      await page.click('[data-testid="page-size-selector"]');
      await page.click('[data-testid="page-size-50"]');
      
      // Verify page size change
      await expect(page.locator('[data-testid="showing-50-items"]')).toBeVisible();
    });
  });

  test.describe('Analytics and Reporting', () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test('should view analytics overview', async ({ page }) => {
      await navigateToSection(page, 'analytics');
      
      // Verify analytics charts are loaded
      await expect(page.locator('[data-testid="traffic-analytics-chart"]')).toBeVisible();
      await expect(page.locator('[data-testid="user-analytics-chart"]')).toBeVisible();
      await expect(page.locator('[data-testid="content-analytics-chart"]')).toBeVisible();
    });

    test('should filter analytics by date range', async ({ page }) => {
      await navigateToSection(page, 'analytics');
      
      // Open date picker
      await page.click('[data-testid="date-range-picker"]');
      
      // Select last 7 days
      await page.click('[data-testid="last-7-days"]');
      
      // Verify charts update
      await page.waitForLoadState('networkidle');
      await expect(page.locator('[data-testid="analytics-loading"]')).not.toBeVisible();
    });

    test('should export analytics report', async ({ page }) => {
      await navigateToSection(page, 'analytics');
      
      // Click export button
      const downloadPromise = page.waitForEvent('download');
      await page.click('[data-testid="export-analytics-report"]');
      
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toContain('analytics-report');
    });
  });

  test.describe('System Settings', () => {
    test.beforeEach(async ({ page }) => {
      await loginAsAdmin(page);
    });

    test('should update site configuration', async ({ page }) => {
      await navigateToSection(page, 'settings');
      
      // Update site title
      await page.fill('[data-testid="site-title-input"]', 'Updated Site Title');
      
      // Update site description
      await page.fill('[data-testid="site-description-input"]', 'Updated description');
      
      // Save settings
      await page.click('[data-testid="save-settings-button"]');
      
      // Verify success message
      await expect(page.locator('.ant-message-success')).toBeVisible();
    });

    test('should manage system integrations', async ({ page }) => {
      await navigateToSection(page, 'settings');
      await page.click('[data-testid="integrations-tab"]');
      
      // Configure email settings
      await page.fill('[data-testid="smtp-host-input"]', 'smtp.example.com');
      await page.fill('[data-testid="smtp-port-input"]', '587');
      
      // Test connection
      await page.click('[data-testid="test-smtp-connection"]');
      
      // Verify test result
      await expect(page.locator('[data-testid="smtp-test-result"]')).toBeVisible();
    });

    test('should view audit logs', async ({ page }) => {
      await navigateToSection(page, 'settings');
      await page.click('[data-testid="audit-logs-tab"]');
      
      // Verify audit log entries
      await expect(page.locator('[data-testid="audit-log-entries"]')).toBeVisible();
      
      // Filter by action type
      await page.selectOption('[data-testid="audit-action-filter"]', 'user_create');
      
      // Verify filtered results
      await expect(page.locator('[data-testid="user-create-logs"]')).toBeVisible();
    });
  });

  test.describe('Responsive Design', () => {
    test('should work correctly on mobile devices', async ({ page }) => {
      // Set mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      
      await loginAsAdmin(page);
      
      // Verify mobile layout
      await expect(page.locator('[data-testid="mobile-sidebar-toggle"]')).toBeVisible();
      
      // Open mobile sidebar
      await page.click('[data-testid="mobile-sidebar-toggle"]');
      await expect(page.locator('[data-testid="mobile-sidebar"]')).toBeVisible();
      
      // Navigate on mobile
      await page.click('[data-testid="mobile-nav-users"]');
      await expect(page.url()).toContain('/users');
    });

    test('should work correctly on tablet devices', async ({ page }) => {
      // Set tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 });
      
      await loginAsAdmin(page);
      
      // Verify tablet layout adaptations
      await expect(page.locator('[data-testid="collapsed-sidebar"]')).toBeVisible();
      
      // Test data table responsiveness
      await navigateToSection(page, 'users');
      await expect(page.locator('[data-testid="responsive-table"]')).toBeVisible();
    });
  });

  test.describe('Accessibility', () => {
    test('should support keyboard navigation', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Test tab navigation
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');
      await page.keyboard.press('Enter');
      
      // Verify navigation with keyboard
      await expect(page.locator(':focus')).toBeVisible();
    });

    test('should have proper ARIA labels', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Check for aria-labels on interactive elements
      const buttons = page.locator('button[aria-label]');
      await expect(buttons.first()).toHaveAttribute('aria-label');
      
      // Check for proper heading structure
      const headings = page.locator('h1, h2, h3, h4, h5, h6');
      await expect(headings.first()).toBeVisible();
    });

    test('should work with screen readers', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Check for proper roles
      await expect(page.locator('[role="main"]')).toBeVisible();
      await expect(page.locator('[role="navigation"]')).toBeVisible();
      
      // Check for announcements
      await expect(page.locator('[aria-live="polite"]')).toBeVisible();
    });
  });

  test.describe('Performance', () => {
    test('should load dashboard within acceptable time', async ({ page }) => {
      const startTime = Date.now();
      
      await loginAsAdmin(page);
      
      // Wait for all content to load
      await page.waitForLoadState('networkidle');
      
      const loadTime = Date.now() - startTime;
      
      // Should load within 5 seconds
      expect(loadTime).toBeLessThan(5000);
    });

    test('should handle large data sets efficiently', async ({ page }) => {
      await loginAsAdmin(page);
      await navigateToSection(page, 'users');
      
      // Test virtual scrolling with large dataset
      await page.evaluate(() => {
        // Simulate large dataset
        window.scrollTo(0, document.body.scrollHeight);
      });
      
      // Should remain responsive
      await expect(page.locator('[data-testid="virtual-table"]')).toBeVisible();
    });
  });

  test.describe('Error Handling', () => {
    test('should handle network errors gracefully', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Simulate network error
      await page.route(`${API_BASE_URL}/**`, (route) => {
        route.abort('internetdisconnected');
      });
      
      // Try to refresh data
      await page.click('[data-testid="refresh-dashboard"]');
      
      // Should show error message
      await expect(page.locator('[data-testid="network-error"]')).toBeVisible();
      
      // Should provide retry option
      await expect(page.locator('[data-testid="retry-button"]')).toBeVisible();
    });

    test('should handle server errors gracefully', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Simulate server error
      await page.route(`${API_BASE_URL}/**`, (route) => {
        route.fulfill({ status: 500, body: 'Internal Server Error' });
      });
      
      // Try to load data
      await navigateToSection(page, 'users');
      
      // Should show error state
      await expect(page.locator('[data-testid="server-error"]')).toBeVisible();
    });

    test('should handle session expiration', async ({ page }) => {
      await loginAsAdmin(page);
      
      // Simulate session expiration
      await page.route(`${API_BASE_URL}/**`, (route) => {
        route.fulfill({ status: 401, body: 'Unauthorized' });
      });
      
      // Try to access protected resource
      await page.click('[data-testid="refresh-dashboard"]');
      
      // Should redirect to login
      await page.waitForURL(`${ADMIN_BASE_URL}/login`);
      await expect(page.locator('[data-testid="session-expired-message"]')).toBeVisible();
    });
  });
});