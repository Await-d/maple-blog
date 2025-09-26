import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { tomorrow } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { formatDistanceToNow, format } from 'date-fns';
import {
  EyeIcon,
  HeartIcon,
  ChatBubbleLeftIcon,
  ClockIcon,
  TagIcon,
  FolderIcon,
  ShareIcon,
  BookmarkIcon,
  PrinterIcon,
  ArrowLeftIcon,
  CalendarDaysIcon
} from '@heroicons/react/24/outline';
import {
  HeartIcon as HeartSolidIcon,
  BookmarkIcon as BookmarkSolidIcon
} from '@heroicons/react/24/solid';

interface Post {
  id: string
  title: string
  slug: string
  content: string
  summary?: string
  status: string
  authorId: string
  authorName: string
  categoryId?: string
  categoryName?: string
  tags: string[]
  createdAt: string
  updatedAt: string
  publishedAt?: string
  viewCount: number
  likeCount: number
  commentCount: number
  readingTime?: number
  wordCount?: number
  allowComments: boolean
  isFeatured: boolean
  isSticky: boolean
  metaTitle?: string
  metaDescription?: string
  ogImageUrl?: string
}

interface PostReaderProps {
  post: Post
  loading?: boolean
  error?: string | null
  showBackButton?: boolean
  showMetadata?: boolean
  showActions?: boolean
  showStats?: boolean
  showTableOfContents?: boolean
  onLike?: (postId: string) => void
  onBookmark?: (postId: string) => void
  onShare?: (post: Post) => void
  onPrint?: () => void
  className?: string
  'data-testid'?: string
}

interface TableOfContentsProps {
  content: string
}

const TableOfContents: React.FC<TableOfContentsProps> = ({ content }) => {
  const [headings, setHeadings] = useState<Array<{ id: string; text: string; level: number }>>([]);
  const [activeId, setActiveId] = useState<string>('');

  useEffect(() => {
    // Extract headings from markdown content
    const headingRegex = /^(#{1,6})\s+(.+)$/gm;
    const matches = Array.from(content.matchAll(headingRegex));

    const extractedHeadings = matches.map((match, index) => ({
      id: `heading-${index}`,
      text: match[2].trim(),
      level: match[1].length
    }));

    setHeadings(extractedHeadings);
  }, [content]);

  useEffect(() => {
    // Observe headings for active state
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setActiveId(entry.target.id);
          }
        });
      },
      { rootMargin: '-20% 0% -35% 0%' }
    );

    const headingElements = headings.map(h => document.getElementById(h.id)).filter(Boolean);
    headingElements.forEach(el => el && observer.observe(el));

    return () => observer.disconnect();
  }, [headings]);

  if (headings.length === 0) return null;

  return (
    <div className="sticky top-8 space-y-2" data-testid="table-of-contents">
      <h4 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">
        On this page
      </h4>
      <nav>
        <ul className="space-y-2">
          {headings.map((heading) => (
            <li key={heading.id}>
              <a
                href={`#${heading.id}`}
                className={`
                  block text-sm transition-colors duration-150 hover:text-blue-600
                  ${activeId === heading.id ? 'text-blue-600 font-medium' : 'text-gray-600'}
                  ${heading.level > 2 ? 'ml-4' : ''}
                  ${heading.level > 3 ? 'ml-8' : ''}
                `}
                style={{ paddingLeft: `${(heading.level - 1) * 12}px` }}
                data-testid={`toc-link-${heading.id}`}
              >
                {heading.text}
              </a>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  );
};

const CodeBlock: React.FC<{ children: string; className?: string }> = ({ children, className }) => {
  const match = /language-(\w+)/.exec(className || '');
  const language = match ? match[1] : '';

  return (
    <SyntaxHighlighter
      style={tomorrow}
      language={language}
      PreTag="div"
      className="rounded-md"
    >
      {String(children).replace(/\n$/, '')}
    </SyntaxHighlighter>
  );
};

export const PostReader: React.FC<PostReaderProps> = ({
  post,
  loading = false,
  error = null,
  showBackButton = true,
  showMetadata = true,
  showActions = true,
  showStats = true,
  showTableOfContents = true,
  onLike,
  onBookmark,
  onShare,
  onPrint,
  className = '',
  'data-testid': testId = 'post-reader'
}) => {
  const [isLiked, setIsLiked] = useState(false);
  const [isBookmarked, setIsBookmarked] = useState(false);
  const [readingProgress, setReadingProgress] = useState(0);

  // Calculate reading progress
  useEffect(() => {
    const updateProgress = () => {
      const scrollTop = window.scrollY;
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const progress = Math.min(100, Math.max(0, (scrollTop / docHeight) * 100));
      setReadingProgress(progress);
    };

    window.addEventListener('scroll', updateProgress);
    return () => window.removeEventListener('scroll', updateProgress);
  }, []);

  const handleLike = () => {
    setIsLiked(!isLiked);
    onLike?.(post.id);
  };

  const handleBookmark = () => {
    setIsBookmarked(!isBookmarked);
    onBookmark?.(post.id);
  };

  const handleShare = () => {
    onShare?.(post);
  };

  const handlePrint = () => {
    if (onPrint) {
      onPrint();
    } else {
      window.print();
    }
  };

  if (loading) {
    return (
      <div className={`max-w-4xl mx-auto px-4 ${className}`} data-testid={`${testId}-loading`}>
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 rounded w-3/4 mb-4"></div>
          <div className="h-4 bg-gray-200 rounded w-1/2 mb-8"></div>
          <div className="space-y-4">
            <div className="h-4 bg-gray-200 rounded"></div>
            <div className="h-4 bg-gray-200 rounded w-5/6"></div>
            <div className="h-4 bg-gray-200 rounded w-4/6"></div>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div
        className={`max-w-4xl mx-auto px-4 ${className}`}
        data-testid={`${testId}-error`}
      >
        <div className="bg-red-50 border border-red-200 rounded-lg p-8 text-center">
          <p className="text-red-600 text-lg">Error loading post: {error}</p>
          {showBackButton && (
            <Link
              to="/posts"
              className="inline-flex items-center mt-4 text-blue-600 hover:text-blue-800"
            >
              <ArrowLeftIcon className="w-4 h-4 mr-2" />
              Back to posts
            </Link>
          )}
        </div>
      </div>
    );
  }

  return (
    <>
      {/* Reading progress bar */}
      <div className="fixed top-0 left-0 right-0 h-1 bg-gray-200 z-50">
        <div
          className="h-full bg-blue-600 transition-all duration-150 ease-out"
          style={{ width: `${readingProgress}%` }}
          data-testid="reading-progress"
        />
      </div>

      <article className={`max-w-4xl mx-auto px-4 ${className}`} data-testid={testId}>
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Main content */}
          <div className="lg:col-span-3">
            {/* Back button */}
            {showBackButton && (
              <div className="mb-8">
                <Link
                  to="/posts"
                  className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900"
                  data-testid="back-button"
                >
                  <ArrowLeftIcon className="w-4 h-4 mr-2" />
                  Back to posts
                </Link>
              </div>
            )}

            {/* Post header */}
            <header className="mb-8">
              <h1
                className="text-4xl font-bold text-gray-900 mb-4 leading-tight"
                data-testid="post-title"
              >
                {post.title}
              </h1>

              {showMetadata && (
                <div className="space-y-4">
                  {/* Author and date */}
                  <div className="flex items-center space-x-4 text-gray-600">
                    <span data-testid="post-author">by {post.authorName}</span>
                    <span className="w-1 h-1 bg-gray-400 rounded-full"></span>
                    <span
                      className="flex items-center"
                      data-testid="post-published-date"
                    >
                      <CalendarDaysIcon className="w-4 h-4 mr-1" />
                      {format(new Date(post.publishedAt || post.createdAt), 'MMMM d, yyyy')}
                    </span>
                    <span className="w-1 h-1 bg-gray-400 rounded-full"></span>
                    <span
                      data-testid="post-relative-date"
                    >
                      {formatDistanceToNow(new Date(post.publishedAt || post.createdAt), {
                        addSuffix: true
                      })}
                    </span>
                  </div>

                  {/* Post meta */}
                  <div className="flex items-center space-x-6 text-sm text-gray-500">
                    {post.readingTime && (
                      <span
                        className="flex items-center"
                        data-testid="reading-time"
                      >
                        <ClockIcon className="w-4 h-4 mr-1" />
                        {post.readingTime} min read
                      </span>
                    )}
                    {showStats && (
                      <>
                        <span
                          className="flex items-center"
                          data-testid="view-count"
                        >
                          <EyeIcon className="w-4 h-4 mr-1" />
                          {post.viewCount} views
                        </span>
                        <span
                          className="flex items-center"
                          data-testid="like-count"
                        >
                          <HeartIcon className="w-4 h-4 mr-1" />
                          {post.likeCount} likes
                        </span>
                        <span
                          className="flex items-center"
                          data-testid="comment-count"
                        >
                          <ChatBubbleLeftIcon className="w-4 h-4 mr-1" />
                          {post.commentCount} comments
                        </span>
                      </>
                    )}
                  </div>

                  {/* Category and tags */}
                  <div className="flex items-center space-x-6">
                    {post.categoryName && (
                      <Link
                        to={`/categories/${post.categoryId}`}
                        className="flex items-center text-blue-600 hover:text-blue-800"
                        data-testid="post-category"
                      >
                        <FolderIcon className="w-4 h-4 mr-1" />
                        {post.categoryName}
                      </Link>
                    )}
                    {post.tags.length > 0 && (
                      <div className="flex items-center space-x-2">
                        <TagIcon className="w-4 h-4 text-gray-400" />
                        <div className="flex flex-wrap gap-2" data-testid="post-tags">
                          {post.tags.map((tag, index) => (
                            <Link
                              key={index}
                              to={`/tags/${tag}`}
                              className="inline-block bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm px-3 py-1 rounded-full transition-colors"
                            >
                              {tag}
                            </Link>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              )}
            </header>

            {/* Post content */}
            <div className="prose prose-lg max-w-none" data-testid="post-content">
              <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                components={{
                  code: ({ node, className, children, ...props }) => {
                    const isInline = !className;
                    if (isInline) {
                      return (
                        <code
                          className="bg-gray-100 text-gray-800 px-1 py-0.5 rounded text-sm"
                          {...props}
                        >
                          {children}
                        </code>
                      );
                    }
                    return (
                      <CodeBlock className={className}>
                        {String(children)}
                      </CodeBlock>
                    );
                  },
                  h1: ({ children, ...props }) => {
                    const id = `heading-${children?.toString().toLowerCase().replace(/\s+/g, '-')}`;
                    return <h1 id={id} {...props}>{children}</h1>;
                  },
                  h2: ({ children, ...props }) => {
                    const id = `heading-${children?.toString().toLowerCase().replace(/\s+/g, '-')}`;
                    return <h2 id={id} {...props}>{children}</h2>;
                  },
                  h3: ({ children, ...props }) => {
                    const id = `heading-${children?.toString().toLowerCase().replace(/\s+/g, '-')}`;
                    return <h3 id={id} {...props}>{children}</h3>;
                  },
                }}
              >
                {post.content}
              </ReactMarkdown>
            </div>

            {/* Action buttons */}
            {showActions && (
              <div className="mt-12 pt-8 border-t border-gray-200">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4">
                    <button
                      onClick={handleLike}
                      className={`
                        flex items-center space-x-2 px-4 py-2 rounded-lg transition-colors
                        ${isLiked
                          ? 'bg-red-50 text-red-600 border border-red-200'
                          : 'bg-gray-50 text-gray-600 hover:bg-gray-100 border border-gray-200'
                        }
                      `}
                      data-testid="like-post-button"
                    >
                      {isLiked ? (
                        <HeartSolidIcon className="w-5 h-5" />
                      ) : (
                        <HeartIcon className="w-5 h-5" />
                      )}
                      <span>Like ({post.likeCount + (isLiked ? 1 : 0)})</span>
                    </button>

                    <button
                      onClick={handleBookmark}
                      className={`
                        p-2 rounded-lg transition-colors border
                        ${isBookmarked
                          ? 'bg-yellow-50 text-yellow-600 border-yellow-200'
                          : 'bg-gray-50 text-gray-600 hover:bg-gray-100 border-gray-200'
                        }
                      `}
                      data-testid="bookmark-post-button"
                    >
                      {isBookmarked ? (
                        <BookmarkSolidIcon className="w-5 h-5" />
                      ) : (
                        <BookmarkIcon className="w-5 h-5" />
                      )}
                    </button>
                  </div>

                  <div className="flex items-center space-x-2">
                    <button
                      onClick={handleShare}
                      className="flex items-center space-x-2 px-4 py-2 bg-gray-50 text-gray-600 rounded-lg hover:bg-gray-100 border border-gray-200 transition-colors"
                      data-testid="share-post-button"
                    >
                      <ShareIcon className="w-5 h-5" />
                      <span>Share</span>
                    </button>

                    <button
                      onClick={handlePrint}
                      className="p-2 bg-gray-50 text-gray-600 rounded-lg hover:bg-gray-100 border border-gray-200 transition-colors"
                      data-testid="print-post-button"
                    >
                      <PrinterIcon className="w-5 h-5" />
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Sidebar */}
          {showTableOfContents && (
            <aside className="lg:col-span-1">
              <TableOfContents content={post.content} />
            </aside>
          )}
        </div>
      </article>
    </>
  );
};

export default PostReader;