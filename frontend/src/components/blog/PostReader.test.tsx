import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '../../../test/utils';
import userEvent from '@testing-library/user-event';
import { PostReader } from './PostReader';
import { createMockPost } from '../../../test/utils';

// Mock react-router-dom
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    Link: ({ children, to, ...props }: React.ComponentProps<'a'> & { to: string }) => <a href={to} {...props}>{children}</a>
  };
});

// Mock react-markdown
vi.mock('react-markdown', () => ({
  default: ({ children }: { children: string }) => (
    <div data-testid="markdown-content">{children}</div>
  )
}));

// Mock syntax highlighter
vi.mock('react-syntax-highlighter', () => ({
  Prism: ({ children }: { children: string }) => (
    <pre data-testid="syntax-highlighter">{children}</pre>
  )
}));

// Mock date-fns
vi.mock('date-fns', () => ({
  formatDistanceToNow: vi.fn(() => '2 days ago'),
  format: vi.fn(() => 'January 15, 2023')
}));

// Mock heroicons
vi.mock('@heroicons/react/24/outline', () => ({
  EyeIcon: ({ className }: { className?: string }) => <div className={className} data-testid="eye-icon" />,
  HeartIcon: ({ className }: { className?: string }) => <div className={className} data-testid="heart-icon" />,
  ChatBubbleLeftIcon: ({ className }: { className?: string }) => <div className={className} data-testid="chat-icon" />,
  ClockIcon: ({ className }: { className?: string }) => <div className={className} data-testid="clock-icon" />,
  TagIcon: ({ className }: { className?: string }) => <div className={className} data-testid="tag-icon" />,
  FolderIcon: ({ className }: { className?: string }) => <div className={className} data-testid="folder-icon" />,
  ShareIcon: ({ className }: { className?: string }) => <div className={className} data-testid="share-icon" />,
  BookmarkIcon: ({ className }: { className?: string }) => <div className={className} data-testid="bookmark-icon" />,
  PrinterIcon: ({ className }: { className?: string }) => <div className={className} data-testid="printer-icon" />,
  ArrowLeftIcon: ({ className }: { className?: string }) => <div className={className} data-testid="arrow-left-icon" />,
  CalendarDaysIcon: ({ className }: { className?: string }) => <div className={className} data-testid="calendar-icon" />
}));

vi.mock('@heroicons/react/24/solid', () => ({
  HeartIcon: ({ className }: { className?: string }) => <div className={className} data-testid="heart-solid-icon" />,
  BookmarkIcon: ({ className }: { className?: string }) => <div className={className} data-testid="bookmark-solid-icon" />
}));

describe('PostReader', () => {
  const mockOnLike = vi.fn();
  const mockOnBookmark = vi.fn();
  const mockOnShare = vi.fn();
  const mockOnPrint = vi.fn();

  const mockPost = createMockPost({
    id: '1',
    title: 'Test Post Title',
    content: '# Introduction\n\nThis is a test post with some content.\n\n## Section 1\n\nMore content here.',
    summary: 'Test post summary',
    authorName: 'John Doe',
    categoryName: 'Technology',
    tags: ['react', 'testing'],
    viewCount: 150,
    likeCount: 25,
    commentCount: 8,
    readingTime: 5,
    publishedAt: '2023-01-15T00:00:00Z'
  });

  const defaultProps = {
    post: mockPost,
    onLike: mockOnLike,
    onBookmark: mockOnBookmark,
    onShare: mockOnShare,
    onPrint: mockOnPrint
  };

  beforeEach(() => {
    vi.clearAllMocks();
    // Mock IntersectionObserver
    global.IntersectionObserver = vi.fn().mockImplementation(() => ({
      observe: vi.fn(),
      unobserve: vi.fn(),
      disconnect: vi.fn()
    }));
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('Basic Rendering', () => {
    it('renders post content correctly', () => {
      render(<PostReader {...defaultProps} />);

      expect(screen.getByTestId('post-reader')).toBeInTheDocument();
      expect(screen.getByTestId('post-title')).toBeInTheDocument();
      expect(screen.getByTestId('post-content')).toBeInTheDocument();
      expect(screen.getByText('Test Post Title')).toBeInTheDocument();
    });

    it('displays loading state', () => {
      render(<PostReader {...defaultProps} loading={true} />);

      expect(screen.getByTestId('post-reader-loading')).toBeInTheDocument();
      expect(screen.queryByTestId('post-title')).not.toBeInTheDocument();
    });

    it('displays error state', () => {
      const errorMessage = 'Failed to load post';
      render(<PostReader {...defaultProps} error={errorMessage} />);

      expect(screen.getByTestId('post-reader-error')).toBeInTheDocument();
      expect(screen.getByText(`Error loading post: ${errorMessage}`)).toBeInTheDocument();
    });

    it('shows back button in error state when enabled', () => {
      render(<PostReader {...defaultProps} error="Error" showBackButton={true} />);

      expect(screen.getByText('Back to posts')).toBeInTheDocument();
    });
  });

  describe('Post Metadata Display', () => {
    it('displays author information when showMetadata is true', () => {
      render(<PostReader {...defaultProps} showMetadata={true} />);

      expect(screen.getByTestId('post-author')).toBeInTheDocument();
      expect(screen.getByText('by John Doe')).toBeInTheDocument();
    });

    it('displays published date information', () => {
      render(<PostReader {...defaultProps} showMetadata={true} />);

      expect(screen.getByTestId('post-published-date')).toBeInTheDocument();
      expect(screen.getByTestId('post-relative-date')).toBeInTheDocument();
      expect(screen.getByText('January 15, 2023')).toBeInTheDocument();
      expect(screen.getByText('2 days ago')).toBeInTheDocument();
    });

    it('displays reading time when available', () => {
      render(<PostReader {...defaultProps} showMetadata={true} />);

      expect(screen.getByTestId('reading-time')).toBeInTheDocument();
      expect(screen.getByText('5 min read')).toBeInTheDocument();
    });

    it('displays category link when category exists', () => {
      render(<PostReader {...defaultProps} showMetadata={true} />);

      expect(screen.getByTestId('post-category')).toBeInTheDocument();
      expect(screen.getByText('Technology')).toBeInTheDocument();
    });

    it('displays tags when available', () => {
      render(<PostReader {...defaultProps} showMetadata={true} />);

      expect(screen.getByTestId('post-tags')).toBeInTheDocument();
      expect(screen.getByText('react')).toBeInTheDocument();
      expect(screen.getByText('testing')).toBeInTheDocument();
    });

    it('hides metadata when showMetadata is false', () => {
      render(<PostReader {...defaultProps} showMetadata={false} />);

      expect(screen.queryByTestId('post-author')).not.toBeInTheDocument();
      expect(screen.queryByTestId('post-published-date')).not.toBeInTheDocument();
      expect(screen.queryByTestId('post-category')).not.toBeInTheDocument();
      expect(screen.queryByTestId('post-tags')).not.toBeInTheDocument();
    });
  });

  describe('Statistics Display', () => {
    it('displays post statistics when showStats is true', () => {
      render(<PostReader {...defaultProps} showStats={true} showMetadata={true} />);

      expect(screen.getByTestId('view-count')).toBeInTheDocument();
      expect(screen.getByTestId('like-count')).toBeInTheDocument();
      expect(screen.getByTestId('comment-count')).toBeInTheDocument();
      expect(screen.getByText('150 views')).toBeInTheDocument();
      expect(screen.getByText('25 likes')).toBeInTheDocument();
      expect(screen.getByText('8 comments')).toBeInTheDocument();
    });

    it('hides statistics when showStats is false', () => {
      render(<PostReader {...defaultProps} showStats={false} showMetadata={true} />);

      expect(screen.queryByTestId('view-count')).not.toBeInTheDocument();
      expect(screen.queryByTestId('like-count')).not.toBeInTheDocument();
      expect(screen.queryByTestId('comment-count')).not.toBeInTheDocument();
    });
  });

  describe('Navigation', () => {
    it('displays back button when showBackButton is true', () => {
      render(<PostReader {...defaultProps} showBackButton={true} />);

      expect(screen.getByTestId('back-button')).toBeInTheDocument();
      expect(screen.getByText('Back to posts')).toBeInTheDocument();
    });

    it('hides back button when showBackButton is false', () => {
      render(<PostReader {...defaultProps} showBackButton={false} />);

      expect(screen.queryByTestId('back-button')).not.toBeInTheDocument();
    });
  });

  describe('Table of Contents', () => {
    it('displays table of contents when showTableOfContents is true', () => {
      render(<PostReader {...defaultProps} showTableOfContents={true} />);

      expect(screen.getByTestId('table-of-contents')).toBeInTheDocument();
      expect(screen.getByText('On this page')).toBeInTheDocument();
    });

    it('hides table of contents when showTableOfContents is false', () => {
      render(<PostReader {...defaultProps} showTableOfContents={false} />);

      expect(screen.queryByTestId('table-of-contents')).not.toBeInTheDocument();
    });

    it('generates TOC links from markdown headings', () => {
      render(<PostReader {...defaultProps} showTableOfContents={true} />);

      // Should have TOC links based on the markdown content
      expect(screen.getByTestId('table-of-contents')).toBeInTheDocument();
    });
  });

  describe('Action Buttons', () => {
    it('displays action buttons when showActions is true', () => {
      render(<PostReader {...defaultProps} showActions={true} />);

      expect(screen.getByTestId('like-post-button')).toBeInTheDocument();
      expect(screen.getByTestId('bookmark-post-button')).toBeInTheDocument();
      expect(screen.getByTestId('share-post-button')).toBeInTheDocument();
      expect(screen.getByTestId('print-post-button')).toBeInTheDocument();
    });

    it('hides action buttons when showActions is false', () => {
      render(<PostReader {...defaultProps} showActions={false} />);

      expect(screen.queryByTestId('like-post-button')).not.toBeInTheDocument();
      expect(screen.queryByTestId('bookmark-post-button')).not.toBeInTheDocument();
      expect(screen.queryByTestId('share-post-button')).not.toBeInTheDocument();
      expect(screen.queryByTestId('print-post-button')).not.toBeInTheDocument();
    });

    it('calls onLike when like button is clicked', async () => {
      const user = userEvent.setup();
      render(<PostReader {...defaultProps} showActions={true} />);

      const likeButton = screen.getByTestId('like-post-button');
      await user.click(likeButton);

      expect(mockOnLike).toHaveBeenCalledWith('1');
    });

    it('calls onBookmark when bookmark button is clicked', async () => {
      const user = userEvent.setup();
      render(<PostReader {...defaultProps} showActions={true} />);

      const bookmarkButton = screen.getByTestId('bookmark-post-button');
      await user.click(bookmarkButton);

      expect(mockOnBookmark).toHaveBeenCalledWith('1');
    });

    it('calls onShare when share button is clicked', async () => {
      const user = userEvent.setup();
      render(<PostReader {...defaultProps} showActions={true} />);

      const shareButton = screen.getByTestId('share-post-button');
      await user.click(shareButton);

      expect(mockOnShare).toHaveBeenCalledWith(mockPost);
    });

    it('calls onPrint when print button is clicked', async () => {
      const user = userEvent.setup();
      render(<PostReader {...defaultProps} showActions={true} />);

      const printButton = screen.getByTestId('print-post-button');
      await user.click(printButton);

      expect(mockOnPrint).toHaveBeenCalled();
    });

    it('toggles like state visually', async () => {
      const user = userEvent.setup();
      render(<PostReader {...defaultProps} showActions={true} />);

      const likeButton = screen.getByTestId('like-post-button');

      // Initially not liked
      expect(screen.getByTestId('heart-icon')).toBeInTheDocument();
      expect(screen.getByText('Like (25)')).toBeInTheDocument();

      await user.click(likeButton);

      // After clicking, should be liked
      expect(screen.getByTestId('heart-solid-icon')).toBeInTheDocument();
      expect(screen.getByText('Like (26)')).toBeInTheDocument();
    });

    it('toggles bookmark state visually', async () => {
      const user = userEvent.setup();
      render(<PostReader {...defaultProps} showActions={true} />);

      const bookmarkButton = screen.getByTestId('bookmark-post-button');

      // Initially not bookmarked
      expect(screen.getByTestId('bookmark-icon')).toBeInTheDocument();

      await user.click(bookmarkButton);

      // After clicking, should be bookmarked
      expect(screen.getByTestId('bookmark-solid-icon')).toBeInTheDocument();
    });
  });

  describe('Reading Progress', () => {
    it('displays reading progress bar', () => {
      render(<PostReader {...defaultProps} />);

      expect(screen.getByTestId('reading-progress')).toBeInTheDocument();
    });

    it('updates reading progress on scroll', async () => {
      render(<PostReader {...defaultProps} />);

      const progressBar = screen.getByTestId('reading-progress');
      expect(progressBar).toHaveStyle({ width: '0%' });

      // Mock scroll event
      Object.defineProperty(window, 'scrollY', { value: 500, writable: true });
      Object.defineProperty(document.documentElement, 'scrollHeight', { value: 2000, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 1000, writable: true });

      fireEvent.scroll(window);

      await waitFor(() => {
        // Should update progress (500 / (2000 - 1000) * 100 = 50%)
        expect(progressBar).toHaveStyle({ width: '50%' });
      });
    });
  });

  describe('Content Rendering', () => {
    it('renders markdown content', () => {
      render(<PostReader {...defaultProps} />);

      expect(screen.getByTestId('post-content')).toBeInTheDocument();
      expect(screen.getByTestId('markdown-content')).toBeInTheDocument();
    });

    it('applies prose styling to content', () => {
      render(<PostReader {...defaultProps} />);

      const contentContainer = screen.getByTestId('post-content');
      expect(contentContainer).toHaveClass('prose', 'prose-lg', 'max-w-none');
    });
  });

  describe('Default Behaviors', () => {
    it('uses window.print as fallback for print action', async () => {
      const user = userEvent.setup();
      const printSpy = vi.spyOn(window, 'print').mockImplementation(() => { /* Mock implementation */ });

      render(<PostReader post={mockPost} showActions={true} />);

      const printButton = screen.getByTestId('print-post-button');
      await user.click(printButton);

      expect(printSpy).toHaveBeenCalled();
      printSpy.mockRestore();
    });

    it('handles missing callback functions gracefully', async () => {
      const user = userEvent.setup();
      render(<PostReader post={mockPost} showActions={true} />);

      const likeButton = screen.getByTestId('like-post-button');
      const bookmarkButton = screen.getByTestId('bookmark-post-button');
      const shareButton = screen.getByTestId('share-post-button');

      // Should not throw errors when callbacks are not provided
      await user.click(likeButton);
      await user.click(bookmarkButton);
      await user.click(shareButton);

      // Visual state should still update
      expect(screen.getByTestId('heart-solid-icon')).toBeInTheDocument();
      expect(screen.getByTestId('bookmark-solid-icon')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('handles post without category', () => {
      const postWithoutCategory = createMockPost({
        id: '1',
        categoryName: undefined,
        categoryId: undefined
      });
      render(<PostReader post={postWithoutCategory} showMetadata={true} />);

      expect(screen.queryByTestId('post-category')).not.toBeInTheDocument();
    });

    it('handles post without tags', () => {
      const postWithoutTags = createMockPost({ id: '1', tags: [] });
      render(<PostReader post={postWithoutTags} showMetadata={true} />);

      expect(screen.queryByTestId('post-tags')).not.toBeInTheDocument();
    });

    it('handles post without reading time', () => {
      const postWithoutReadingTime = createMockPost({ id: '1', readingTime: undefined });
      render(<PostReader post={postWithoutReadingTime} showMetadata={true} />);

      expect(screen.queryByTestId('reading-time')).not.toBeInTheDocument();
    });

    it('uses createdAt when publishedAt is not available', () => {
      const postWithoutPublishedAt = createMockPost({
        id: '1',
        publishedAt: undefined,
        createdAt: '2023-01-10T00:00:00Z'
      });
      render(<PostReader post={postWithoutPublishedAt} showMetadata={true} />);

      expect(screen.getByTestId('post-published-date')).toBeInTheDocument();
      expect(screen.getByTestId('post-relative-date')).toBeInTheDocument();
    });

    it('handles empty content gracefully', () => {
      const postWithEmptyContent = createMockPost({ id: '1', content: '' });
      render(<PostReader post={postWithEmptyContent} />);

      expect(screen.getByTestId('post-content')).toBeInTheDocument();
      expect(screen.getByTestId('markdown-content')).toBeInTheDocument();
    });
  });

  describe('Custom Styling and Props', () => {
    it('applies custom className', () => {
      render(<PostReader {...defaultProps} className="custom-reader-class" />);

      const reader = screen.getByTestId('post-reader');
      expect(reader.parentElement).toHaveClass('custom-reader-class');
    });

    it('accepts custom data-testid', () => {
      render(<PostReader {...defaultProps} data-testid="custom-post-reader" />);

      expect(screen.getByTestId('custom-post-reader')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('has proper heading hierarchy', () => {
      render(<PostReader {...defaultProps} />);

      const title = screen.getByTestId('post-title');
      expect(title.tagName).toBe('H1');
    });

    it('provides proper button accessibility', () => {
      render(<PostReader {...defaultProps} showActions={true} />);

      const buttons = [
        screen.getByTestId('like-post-button'),
        screen.getByTestId('bookmark-post-button'),
        screen.getByTestId('share-post-button'),
        screen.getByTestId('print-post-button')
      ];

      buttons.forEach(button => {
        expect(button).toBeInstanceOf(HTMLButtonElement);
      });
    });

    it('provides proper link accessibility', () => {
      render(<PostReader {...defaultProps} showMetadata={true} showBackButton={true} />);

      const backLink = screen.getByTestId('back-button');
      expect(backLink).toHaveAttribute('href', '/posts');

      if (mockPost.categoryId) {
        const categoryLink = screen.getByTestId('post-category');
        expect(categoryLink).toHaveAttribute('href', `/categories/${mockPost.categoryId}`);
      }
    });
  });

  describe('Responsive Behavior', () => {
    it('has proper grid layout for content and sidebar', () => {
      render(<PostReader {...defaultProps} showTableOfContents={true} />);

      const article = screen.getByTestId('post-reader');
      const articleContainer = article.querySelector('.grid');
      expect(articleContainer).toHaveClass('grid-cols-1', 'lg:grid-cols-4');
    });

    it('collapses to single column on mobile', () => {
      render(<PostReader {...defaultProps} showTableOfContents={true} />);

      const article = screen.getByTestId('post-reader');
      const articleContainer = article.querySelector('.grid');
      expect(articleContainer).toHaveClass('grid-cols-1');
    });
  });
});