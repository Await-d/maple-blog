import React, { useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { formatDistanceToNow } from 'date-fns';
import {
  EyeIcon,
  HeartIcon,
  ChatBubbleLeftIcon,
  ClockIcon,
  TagIcon,
  FolderIcon,
  StarIcon
} from '@heroicons/react/24/outline';
import { HeartIcon as HeartSolidIcon, StarIcon as StarSolidIcon } from '@heroicons/react/24/solid';
import type { Post } from '@/types/blog';

interface PostListProps {
  posts: Post[]
  loading?: boolean
  error?: string | null
  layout?: 'list' | 'grid' | 'compact'
  showAuthor?: boolean
  showCategory?: boolean
  showTags?: boolean
  showStats?: boolean
  showActions?: boolean
  onLike?: (postId: string) => void
  onBookmark?: (postId: string) => void
  onPostClick?: (post: Post) => void
  onRetry?: () => void
  currentPage?: number
  totalPages?: number
  onPageChange?: (page: number) => void
  emptyStateMessage?: string
  className?: string
  'data-testid'?: string
}

const PostCard: React.FC<{
  post: Post
  layout: 'list' | 'grid' | 'compact'
  showAuthor: boolean
  showCategory: boolean
  showTags: boolean
  showStats: boolean
  showActions: boolean
  onLike?: (postId: string) => void
  onBookmark?: (postId: string) => void
}> = ({
  post,
  layout,
  showAuthor,
  showCategory,
  showTags,
  showStats,
  showActions,
  onLike,
  onBookmark
}) => {
  const [isLiked, setIsLiked] = useState(false);
  const [isBookmarked, setIsBookmarked] = useState(false);

  const handleLike = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsLiked(!isLiked);
    onLike?.(post.id);
  }, [isLiked, onLike, post.id]);

  const handleBookmark = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsBookmarked(!isBookmarked);
    onBookmark?.(post.id);
  }, [isBookmarked, onBookmark, post.id]);

  const formatDate = (dateString: string) => {
    return formatDistanceToNow(new Date(dateString), { addSuffix: true });
  };

  const baseCardClass = `
    bg-white rounded-lg border border-gray-200 hover:border-gray-300
    transition-all duration-200 hover:shadow-md group
  `;

  const compactCard = (
    <div className={`${baseCardClass} p-4`} data-testid={`post-card-${post.id}`}>
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <div className="flex items-center space-x-2 mb-2">
            {post.isSticky && (
              <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800">
                <StarSolidIcon className="w-3 h-3 mr-1" />
                Pinned
              </span>
            )}
            {post.isFeatured && (
              <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">
                Featured
              </span>
            )}
          </div>

          <h3 className="text-lg font-semibold text-gray-900 group-hover:text-blue-600 line-clamp-2">
            <Link to={`/posts/${post.slug}`} data-testid={`post-link-${post.id}`}>
              {post.title}
            </Link>
          </h3>

          <div className="flex items-center space-x-4 mt-2 text-sm text-gray-500">
            {showAuthor && (
              <span data-testid={`post-author-${post.id}`}>by {post.authorName}</span>
            )}
            <span data-testid={`post-date-${post.id}`}>
              {formatDate(post.publishedAt || post.createdAt)}
            </span>
            {showStats && post.readingTime && (
              <span className="flex items-center" data-testid={`post-reading-time-${post.id}`}>
                <ClockIcon className="w-4 h-4 mr-1" />
                {post.readingTime} min read
              </span>
            )}
          </div>
        </div>

        {showActions && (
          <div className="flex items-center space-x-2 ml-4">
            <button
              onClick={handleLike}
              className="p-1 rounded hover:bg-gray-100"
              data-testid={`like-button-${post.id}`}
            >
              {isLiked ? (
                <HeartSolidIcon className="w-5 h-5 text-red-500" />
              ) : (
                <HeartIcon className="w-5 h-5 text-gray-400" />
              )}
            </button>
            <button
              onClick={handleBookmark}
              className="p-1 rounded hover:bg-gray-100"
              data-testid={`bookmark-button-${post.id}`}
            >
              {isBookmarked ? (
                <StarSolidIcon className="w-5 h-5 text-yellow-500" />
              ) : (
                <StarIcon className="w-5 h-5 text-gray-400" />
              )}
            </button>
          </div>
        )}
      </div>
    </div>
  );

  const fullCard = (
    <div
      className={`${baseCardClass} ${layout === 'grid' ? 'h-full flex flex-col' : ''}`}
      data-testid={`post-card-${post.id}`}
    >
      <div className={`p-6 ${layout === 'grid' ? 'flex flex-col flex-1' : ''}`}>
        {/* Post badges */}
        <div className="flex items-center space-x-2 mb-3">
          {post.isSticky && (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800">
              <StarSolidIcon className="w-3 h-3 mr-1" />
              Pinned
            </span>
          )}
          {post.isFeatured && (
            <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">
              Featured
            </span>
          )}
        </div>

        {/* Title */}
        <h2 className="text-xl font-bold text-gray-900 mb-3 group-hover:text-blue-600 line-clamp-2">
          <Link to={`/posts/${post.slug}`} data-testid={`post-link-${post.id}`}>
            {post.title}
          </Link>
        </h2>

        {/* Summary */}
        {post.excerpt && (
          <p
            className="text-gray-600 mb-4 line-clamp-3"
            data-testid={`post-excerpt-${post.id}`}
          >
            {post.excerpt}
          </p>
        )}

        {/* Meta information */}
        <div className={`space-y-3 ${layout === 'grid' ? 'mt-auto' : ''}`}>
          {/* Author and date */}
          <div className="flex items-center justify-between text-sm text-gray-500">
            <div className="flex items-center space-x-4">
              {showAuthor && (
                <span data-testid={`post-author-${post.id}`}>by {post.authorName}</span>
              )}
              <span data-testid={`post-date-${post.id}`}>
                {formatDate(post.publishedAt || post.createdAt)}
              </span>
            </div>
            {post.readingTime && (
              <span
                className="flex items-center"
                data-testid={`post-reading-time-${post.id}`}
              >
                <ClockIcon className="w-4 h-4 mr-1" />
                {post.readingTime} min read
              </span>
            )}
          </div>

          {/* Category and tags */}
          {(showCategory || showTags) && (
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-4">
                {showCategory && post.category && (
                  <Link
                    to={`/categories/${post.category.slug}`}
                    className="flex items-center text-sm text-blue-600 hover:text-blue-800"
                    data-testid={`post-category-${post.id}`}
                  >
                    <FolderIcon className="w-4 h-4 mr-1" />
                    {post.category.name}
                  </Link>
                )}
                {showTags && post.tags.length > 0 && (
                  <div className="flex items-center space-x-2">
                    <TagIcon className="w-4 h-4 text-gray-400" />
                    <div
                      className="flex flex-wrap gap-2"
                      data-testid={`post-tags-${post.id}`}
                    >
                      {post.tags.slice(0, 3).map((tag, index) => (
                        <span
                          key={index}
                          className="inline-block bg-gray-100 text-gray-600 text-xs px-2 py-1 rounded"
                        >
                          {typeof tag === 'string' ? tag : tag.name}
                        </span>
                      ))}
                      {post.tags.length > 3 && (
                        <span className="text-xs text-gray-500">+{post.tags.length - 3}</span>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Stats and actions */}
          <div className="flex items-center justify-between pt-3 border-t border-gray-100">
            {showStats && (
              <div className="flex items-center space-x-4 text-sm text-gray-500">
                <span
                  className="flex items-center"
                  data-testid={`post-views-${post.id}`}
                >
                  <EyeIcon className="w-4 h-4 mr-1" />
                  {post.viewCount}
                </span>
                <span
                  className="flex items-center"
                  data-testid={`post-likes-${post.id}`}
                >
                  <HeartIcon className="w-4 h-4 mr-1" />
                  {post.likeCount}
                </span>
                <span
                  className="flex items-center"
                  data-testid={`post-comments-${post.id}`}
                >
                  <ChatBubbleLeftIcon className="w-4 h-4 mr-1" />
                  {post.commentCount}
                </span>
              </div>
            )}

            {showActions && (
              <div className="flex items-center space-x-2">
                <button
                  onClick={handleLike}
                  className="flex items-center space-x-1 px-3 py-1 rounded-full text-sm transition-colors hover:bg-gray-100"
                  data-testid={`like-button-${post.id}`}
                >
                  {isLiked ? (
                    <HeartSolidIcon className="w-4 h-4 text-red-500" />
                  ) : (
                    <HeartIcon className="w-4 h-4 text-gray-400" />
                  )}
                  <span>Like</span>
                </button>
                <button
                  onClick={handleBookmark}
                  className="p-2 rounded-full transition-colors hover:bg-gray-100"
                  data-testid={`bookmark-button-${post.id}`}
                >
                  {isBookmarked ? (
                    <StarSolidIcon className="w-5 h-5 text-yellow-500" />
                  ) : (
                    <StarIcon className="w-5 h-5 text-gray-400" />
                  )}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );

  return layout === 'compact' ? compactCard : fullCard;
};

export const PostList: React.FC<PostListProps> = ({
  posts,
  loading = false,
  error = null,
  layout = 'list',
  showAuthor = true,
  showCategory = true,
  showTags = true,
  showStats = true,
  showActions = true,
  onLike,
  onBookmark,
  className = '',
  'data-testid': testId = 'post-list'
}) => {
  if (loading) {
    return (
      <div className={`space-y-4 ${className}`} data-testid={`${testId}-loading`}>
        {Array.from({ length: 3 }).map((_, index) => (
          <div
            key={index}
            className="bg-white rounded-lg border border-gray-200 p-6 animate-pulse"
          >
            <div className="h-6 bg-gray-200 rounded mb-3"></div>
            <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
            <div className="h-4 bg-gray-200 rounded w-1/2"></div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div
        className={`bg-red-50 border border-red-200 rounded-lg p-6 text-center ${className}`}
        data-testid={`${testId}-error`}
      >
        <p className="text-red-600">Error loading posts: {error}</p>
      </div>
    );
  }

  if (posts.length === 0) {
    return (
      <div
        className={`bg-gray-50 border border-gray-200 rounded-lg p-12 text-center ${className}`}
        data-testid={`${testId}-empty`}
      >
        <p className="text-gray-500 text-lg">No posts found</p>
      </div>
    );
  }

  // Enhanced responsive grid layout
  const getContainerClass = () => {
    if (layout === 'grid') {
      return `grid gap-6 grid-cols-1 md:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-4 ${className}`;
    }
    return `space-y-4 ${className}`;
  };

  return (
    <div className={getContainerClass()} data-testid={testId}>
      {posts.map((post) => (
        <PostCard
          key={post.id}
          post={post}
          layout={layout}
          showAuthor={showAuthor}
          showCategory={showCategory}
          showTags={showTags}
          showStats={showStats}
          showActions={showActions}
          onLike={onLike}
          onBookmark={onBookmark}
        />
      ))}
    </div>
  );
};

export default PostList;