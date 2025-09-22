// @ts-nocheck
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '../../../test/utils';
import userEvent from '@testing-library/user-event';
import { PostList } from './PostList';
import { createMockPost } from '../../../test/utils';

// Mock react-router-dom
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    Link: ({ children, to, ...props }: any) => <a href={to} {...props}>{children}</a>
  };
});

// Mock date-fns
vi.mock('date-fns', () => ({
  formatDistanceToNow: vi.fn(() => '2 days ago')
}));

// Mock heroicons
vi.mock('@heroicons/react/24/outline', () => ({
  EyeIcon: ({ className }: { className?: string }) => <div className={className} data-testid="eye-icon" />,
  HeartIcon: ({ className }: { className?: string }) => <div className={className} data-testid="heart-icon" />,
  ChatBubbleLeftIcon: ({ className }: { className?: string }) => <div className={className} data-testid="chat-icon" />,
  ClockIcon: ({ className }: { className?: string }) => <div className={className} data-testid="clock-icon" />,
  TagIcon: ({ className }: { className?: string }) => <div className={className} data-testid="tag-icon" />,
  FolderIcon: ({ className }: { className?: string }) => <div className={className} data-testid="folder-icon" />,
  StarIcon: ({ className }: { className?: string }) => <div className={className} data-testid="star-icon" />
}));

vi.mock('@heroicons/react/24/solid', () => ({
  HeartIcon: ({ className }: { className?: string }) => <div className={className} data-testid="heart-solid-icon" />,
  StarIcon: ({ className }: { className?: string }) => <div className={className} data-testid="star-solid-icon" />
}));

describe('PostList', () => {
  const mockOnLike = vi.fn();
  const mockOnBookmark = vi.fn();

  const defaultProps = {
    posts: [],
    onLike: mockOnLike,
    onBookmark: mockOnBookmark
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Rendering', () => {
    it('renders empty state when no posts provided', () => {
      render(<PostList {...defaultProps} />);

      expect(screen.getByTestId('post-list-empty')).toBeInTheDocument();
      expect(screen.getByText('No posts found')).toBeInTheDocument();
    });

    it('renders loading state when loading is true', () => {
      render(<PostList {...defaultProps} loading={true} />);

      expect(screen.getByTestId('post-list-loading')).toBeInTheDocument();
      expect(screen.getAllByRole('generic')).toHaveLength(3); // 3 skeleton items
    });

    it('renders error state when error is provided', () => {
      const errorMessage = 'Failed to load posts';
      render(<PostList {...defaultProps} error={errorMessage} />);

      expect(screen.getByTestId('post-list-error')).toBeInTheDocument();
      expect(screen.getByText(`Error loading posts: ${errorMessage}`)).toBeInTheDocument();
    });

    it('renders post list when posts are provided', () => {
      const posts = [
        createMockPost({ id: '1', title: 'First Post' }),
        createMockPost({ id: '2', title: 'Second Post' })
      ];

      render(<PostList {...defaultProps} posts={posts} />);

      expect(screen.getByTestId('post-list')).toBeInTheDocument();
      expect(screen.getByTestId('post-card-1')).toBeInTheDocument();
      expect(screen.getByTestId('post-card-2')).toBeInTheDocument();
      expect(screen.getByText('First Post')).toBeInTheDocument();
      expect(screen.getByText('Second Post')).toBeInTheDocument();
    });
  });

  describe('Post Card Content', () => {
    const mockPost = createMockPost({
      id: '1',
      title: 'Test Post Title',
      summary: 'This is a test summary',
      authorName: 'John Doe',
      categoryName: 'Technology',
      tags: ['react', 'testing', 'javascript'],
      viewCount: 150,
      likeCount: 25,
      commentCount: 8,
      readingTime: 5,
      isFeatured: true,
      isSticky: false
    });

    it('displays post title as clickable link', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} />);

      const titleLink = screen.getByTestId('post-link-1');
      expect(titleLink).toBeInTheDocument();
      expect(titleLink).toHaveAttribute('href', '/posts/test-post');
      expect(titleLink).toHaveTextContent('Test Post Title');
    });

    it('displays post summary when provided', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} />);

      expect(screen.getByTestId('post-summary-1')).toBeInTheDocument();
      expect(screen.getByText('This is a test summary')).toBeInTheDocument();
    });

    it('displays author name when showAuthor is true', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showAuthor={true} />);

      expect(screen.getByTestId('post-author-1')).toBeInTheDocument();
      expect(screen.getByText('by John Doe')).toBeInTheDocument();
    });

    it('hides author name when showAuthor is false', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showAuthor={false} />);

      expect(screen.queryByTestId('post-author-1')).not.toBeInTheDocument();
    });

    it('displays category when showCategory is true', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showCategory={true} />);

      expect(screen.getByTestId('post-category-1')).toBeInTheDocument();
      expect(screen.getByText('Technology')).toBeInTheDocument();
    });

    it('displays tags when showTags is true', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showTags={true} />);

      expect(screen.getByTestId('post-tags-1')).toBeInTheDocument();
      expect(screen.getByText('react')).toBeInTheDocument();
      expect(screen.getByText('testing')).toBeInTheDocument();
      expect(screen.getByText('javascript')).toBeInTheDocument();
    });

    it('limits displayed tags to 3 and shows overflow count', () => {
      const postWithManyTags = createMockPost({
        id: '1',
        tags: ['tag1', 'tag2', 'tag3', 'tag4', 'tag5']
      });

      render(<PostList {...defaultProps} posts={[postWithManyTags]} showTags={true} />);

      expect(screen.getByText('tag1')).toBeInTheDocument();
      expect(screen.getByText('tag2')).toBeInTheDocument();
      expect(screen.getByText('tag3')).toBeInTheDocument();
      expect(screen.getByText('+2')).toBeInTheDocument();
      expect(screen.queryByText('tag4')).not.toBeInTheDocument();
    });

    it('displays post statistics when showStats is true', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showStats={true} />);

      expect(screen.getByTestId('post-views-1')).toBeInTheDocument();
      expect(screen.getByTestId('post-likes-1')).toBeInTheDocument();
      expect(screen.getByTestId('post-comments-1')).toBeInTheDocument();
      expect(screen.getByText('150')).toBeInTheDocument();
      expect(screen.getByText('25')).toBeInTheDocument();
      expect(screen.getByText('8')).toBeInTheDocument();
    });

    it('displays reading time when available', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showStats={true} />);

      expect(screen.getByTestId('post-reading-time-1')).toBeInTheDocument();
      expect(screen.getByText('5 min read')).toBeInTheDocument();
    });

    it('displays featured badge for featured posts', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} />);

      expect(screen.getByText('Featured')).toBeInTheDocument();
    });

    it('displays pinned badge for sticky posts', () => {
      const stickyPost = createMockPost({ id: '1', isSticky: true, isFeatured: false });
      render(<PostList {...defaultProps} posts={[stickyPost]} />);

      expect(screen.getByText('Pinned')).toBeInTheDocument();
    });
  });

  describe('Layout Options', () => {
    const mockPosts = [
      createMockPost({ id: '1', title: 'Post 1' }),
      createMockPost({ id: '2', title: 'Post 2' })
    ];

    it('renders in list layout by default', () => {
      render(<PostList {...defaultProps} posts={mockPosts} />);

      const container = screen.getByTestId('post-list');
      expect(container).toHaveClass('space-y-4');
    });

    it('renders in grid layout when layout is grid', () => {
      render(<PostList {...defaultProps} posts={mockPosts} layout="grid" />);

      const container = screen.getByTestId('post-list');
      expect(container).toHaveClass('grid', 'gap-6');
    });

    it('renders in compact layout', () => {
      render(<PostList {...defaultProps} posts={mockPosts} layout="compact" />);

      // Compact layout should still show posts but with different styling
      expect(screen.getByTestId('post-card-1')).toBeInTheDocument();
      expect(screen.getByTestId('post-card-2')).toBeInTheDocument();
    });
  });

  describe('Interactive Features', () => {
    const mockPost = createMockPost({ id: '1', title: 'Interactive Post' });

    it('calls onLike when like button is clicked', async () => {
      const user = userEvent.setup();
      render(<PostList {...defaultProps} posts={[mockPost]} showActions={true} />);

      const likeButton = screen.getByTestId('like-button-1');
      await user.click(likeButton);

      expect(mockOnLike).toHaveBeenCalledWith('1');
    });

    it('calls onBookmark when bookmark button is clicked', async () => {
      const user = userEvent.setup();
      render(<PostList {...defaultProps} posts={[mockPost]} showActions={true} />);

      const bookmarkButton = screen.getByTestId('bookmark-button-1');
      await user.click(bookmarkButton);

      expect(mockOnBookmark).toHaveBeenCalledWith('1');
    });

    it('toggles like state visually', async () => {
      const user = userEvent.setup();
      render(<PostList {...defaultProps} posts={[mockPost]} showActions={true} />);

      const likeButton = screen.getByTestId('like-button-1');

      // Initially not liked (outline icon)
      expect(screen.getByTestId('heart-icon')).toBeInTheDocument();

      await user.click(likeButton);

      // After click, should be liked (solid icon)
      expect(screen.getByTestId('heart-solid-icon')).toBeInTheDocument();
    });

    it('toggles bookmark state visually', async () => {
      const user = userEvent.setup();
      render(<PostList {...defaultProps} posts={[mockPost]} showActions={true} />);

      const bookmarkButton = screen.getByTestId('bookmark-button-1');

      // Initially not bookmarked (outline icon)
      expect(screen.getByTestId('star-icon')).toBeInTheDocument();

      await user.click(bookmarkButton);

      // After click, should be bookmarked (solid icon)
      expect(screen.getByTestId('star-solid-icon')).toBeInTheDocument();
    });

    it('prevents event propagation when action buttons are clicked', async () => {
      const user = userEvent.setup();
      const mockCardClick = vi.fn();

      render(
        <div onClick={mockCardClick}>
          <PostList {...defaultProps} posts={[mockPost]} showActions={true} />
        </div>
      );

      const likeButton = screen.getByTestId('like-button-1');
      await user.click(likeButton);

      expect(mockOnLike).toHaveBeenCalledWith('1');
      expect(mockCardClick).not.toHaveBeenCalled();
    });
  });

  describe('Conditional Display Options', () => {
    const mockPost = createMockPost({ id: '1' });

    it('hides actions when showActions is false', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showActions={false} />);

      expect(screen.queryByTestId('like-button-1')).not.toBeInTheDocument();
      expect(screen.queryByTestId('bookmark-button-1')).not.toBeInTheDocument();
    });

    it('hides stats when showStats is false', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showStats={false} />);

      expect(screen.queryByTestId('post-views-1')).not.toBeInTheDocument();
      expect(screen.queryByTestId('post-likes-1')).not.toBeInTheDocument();
      expect(screen.queryByTestId('post-comments-1')).not.toBeInTheDocument();
    });

    it('hides category when showCategory is false', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showCategory={false} />);

      expect(screen.queryByTestId('post-category-1')).not.toBeInTheDocument();
    });

    it('hides tags when showTags is false', () => {
      render(<PostList {...defaultProps} posts={[mockPost]} showTags={false} />);

      expect(screen.queryByTestId('post-tags-1')).not.toBeInTheDocument();
    });
  });

  describe('Date Formatting', () => {
    it('displays formatted date', () => {
      const mockPost = createMockPost({ id: '1' });
      render(<PostList {...defaultProps} posts={[mockPost]} />);

      expect(screen.getByTestId('post-date-1')).toBeInTheDocument();
      expect(screen.getByText('2 days ago')).toBeInTheDocument();
    });

    it('uses publishedAt date when available, otherwise createdAt', () => {
      const mockPost = createMockPost({
        id: '1',
        publishedAt: '2023-01-15T00:00:00Z',
        createdAt: '2023-01-10T00:00:00Z'
      });

      render(<PostList {...defaultProps} posts={[mockPost]} />);

      expect(screen.getByTestId('post-date-1')).toBeInTheDocument();
    });
  });

  describe('Custom Styling', () => {
    it('applies custom className', () => {
      render(<PostList {...defaultProps} posts={[]} className="custom-class" />);

      const container = screen.getByTestId('post-list-empty');
      expect(container).toHaveClass('custom-class');
    });

    it('accepts custom data-testid', () => {
      const mockPosts = [createMockPost({ id: '1' })];
      render(<PostList {...defaultProps} posts={mockPosts} data-testid="custom-post-list" />);

      expect(screen.getByTestId('custom-post-list')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('handles posts without summary gracefully', () => {
      const postWithoutSummary = createMockPost({ id: '1', summary: undefined });
      render(<PostList {...defaultProps} posts={[postWithoutSummary]} />);

      expect(screen.queryByTestId('post-summary-1')).not.toBeInTheDocument();
    });

    it('handles posts without category gracefully', () => {
      const postWithoutCategory = createMockPost({
        id: '1',
        categoryId: undefined,
        categoryName: undefined
      });
      render(<PostList {...defaultProps} posts={[postWithoutCategory]} showCategory={true} />);

      expect(screen.queryByTestId('post-category-1')).not.toBeInTheDocument();
    });

    it('handles posts without tags gracefully', () => {
      const postWithoutTags = createMockPost({ id: '1', tags: [] });
      render(<PostList {...defaultProps} posts={[postWithoutTags]} showTags={true} />);

      expect(screen.queryByTestId('post-tags-1')).not.toBeInTheDocument();
    });

    it('handles posts without reading time gracefully', () => {
      const postWithoutReadingTime = createMockPost({ id: '1', readingTime: undefined });
      render(<PostList {...defaultProps} posts={[postWithoutReadingTime]} showStats={true} />);

      expect(screen.queryByTestId('post-reading-time-1')).not.toBeInTheDocument();
    });

    it('handles missing callback functions gracefully', async () => {
      const user = userEvent.setup();
      const mockPost = createMockPost({ id: '1' });
      render(<PostList posts={[mockPost]} showActions={true} />);

      const likeButton = screen.getByTestId('like-button-1');
      const bookmarkButton = screen.getByTestId('bookmark-button-1');

      // Should not throw errors when callbacks are not provided
      await user.click(likeButton);
      await user.click(bookmarkButton);

      // Visual state should still update
      expect(screen.getByTestId('heart-solid-icon')).toBeInTheDocument();
      expect(screen.getByTestId('star-solid-icon')).toBeInTheDocument();
    });

    it('handles very long post titles gracefully', () => {
      const longTitle = 'This is a very long post title that should be truncated or handled gracefully in the UI to prevent layout issues';
      const postWithLongTitle = createMockPost({ id: '1', title: longTitle });
      render(<PostList {...defaultProps} posts={[postWithLongTitle]} />);

      expect(screen.getByText(longTitle)).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('has proper link accessibility for post titles', () => {
      const mockPost = createMockPost({ id: '1', title: 'Accessible Post', slug: 'accessible-post' });
      render(<PostList {...defaultProps} posts={[mockPost]} />);

      const link = screen.getByTestId('post-link-1');
      expect(link).toHaveAttribute('href', '/posts/accessible-post');
    });

    it('has proper button accessibility for actions', () => {
      const mockPost = createMockPost({ id: '1' });
      render(<PostList {...defaultProps} posts={[mockPost]} showActions={true} />);

      const likeButton = screen.getByTestId('like-button-1');
      const bookmarkButton = screen.getByTestId('bookmark-button-1');

      expect(likeButton).toBeInstanceOf(HTMLButtonElement);
      expect(bookmarkButton).toBeInstanceOf(HTMLButtonElement);
    });
  });
});