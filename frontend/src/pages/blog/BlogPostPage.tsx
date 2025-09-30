/**
 * BlogPostPage - Complete Blog Post Detail Page
 * Full-featured article detail page with all modern blog functionality
 */

import React, { useState, useEffect, useMemo, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { DocumentHead } from '@/components/common/DocumentHead';
import RichTextRenderer from '@/components/common/RichTextRenderer';
import { useAuth, usePermissions } from '@/hooks/useAuth';
import { useBlogPostQueries } from '@/services/blog/blogApi';
import { HomeApiService } from '@/services/home/homeApi';
import { toastService } from '@/services/toastService';
import { errorReporter } from '@/services/errorReporting';
import { Post, TOCItem, SocialShare } from '@/types/blog';
import { Comment } from '@/types/comment';
import {
  Heart,
  Bookmark,
  Share2,
  MessageCircle,
  Clock,
  Calendar,
  User,
  Tag,
  ChevronUp,
  ChevronDown,
  Send,
  ThumbsUp,
  Reply,
  Eye,
  ExternalLink,
  Copy,
  Facebook,
  Twitter,
  Linkedin,
  Menu,
  X,
} from 'lucide-react';

// Table of Contents Generator
const generateTOC = (content: string): TOCItem[] => {
  const headingRegex = /^(#{1,6})\\s+(.+)$/gm;
  const toc: TOCItem[] = [];
  let match;

  while ((match = headingRegex.exec(content)) !== null) {
    const level = match[1].length;
    const text = match[2].trim();
    const anchor = text
      .toLowerCase()
      .replace(/[^a-z0-9\\s-]/g, '')
      .replace(/\\s+/g, '-')
      .replace(/-+/g, '-')
      .trim();

    toc.push({
      id: `heading-${toc.length}`,
      text,
      level,
      anchor,
    });
  }

  return toc;
};

// Social sharing utilities
const generateSocialShareUrls = (post: Post, currentUrl: string): SocialShare[] => [
  {
    platform: 'twitter',
    url: `https://twitter.com/intent/tweet?text=${encodeURIComponent(post.title)}&url=${encodeURIComponent(currentUrl)}`,
    title: 'Share on Twitter',
  },
  {
    platform: 'facebook',
    url: `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(currentUrl)}`,
    title: 'Share on Facebook',
  },
  {
    platform: 'linkedin',
    url: `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(currentUrl)}`,
    title: 'Share on LinkedIn',
  },
  {
    platform: 'reddit',
    url: `https://reddit.com/submit?url=${encodeURIComponent(currentUrl)}&title=${encodeURIComponent(post.title)}`,
    title: 'Share on Reddit',
  },
];

// Reading progress hook
const useReadingProgress = () => {
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    const updateProgress = () => {
      const scrollTop = window.pageYOffset;
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const progress = (scrollTop / docHeight) * 100;
      setProgress(Math.min(100, Math.max(0, progress)));
    };

    window.addEventListener('scroll', updateProgress);
    return () => window.removeEventListener('scroll', updateProgress);
  }, []);

  return progress;
};

// Image lazy loading hook
const useImageLazyLoading = () => {
  useEffect(() => {
    const images = document.querySelectorAll('img[data-src]');
    
    const imageObserver = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const img = entry.target as HTMLImageElement;
          img.src = img.dataset.src || '';
          img.classList.add('loaded');
          imageObserver.unobserve(img);
        }
      });
    }, {
      rootMargin: '50px'
    });

    images.forEach(img => imageObserver.observe(img));

    return () => {
      images.forEach(img => imageObserver.unobserve(img));
    };
  }, []);
};

// Main component
export const BlogPostPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { user, isAuthenticated } = useAuth();
  const { canManagePosts } = usePermissions();
  const contentRef = useRef<HTMLDivElement>(null);

  // State
  const [isLiked, setIsLiked] = useState(false);
  const [likeCount, setLikeCount] = useState(0);
  const [isBookmarked, setIsBookmarked] = useState(false);
  const [showTOC, setShowTOC] = useState(false);
  const [showShareModal, setShowShareModal] = useState(false);
  const [activeHeading, setActiveHeading] = useState<string>('');
  const [commentContent, setCommentContent] = useState('');
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  const [showComments, setShowComments] = useState(true);

  // API hooks
  const { usePost } = useBlogPostQueries();
  const { data: post, isLoading, error } = usePost(id!, !!id);

  // Comment state (mock implementation since full API structure isn't available)
  const [comments, setComments] = useState<Comment[]>([]);

  // Custom hooks
  const readingProgress = useReadingProgress();
  useImageLazyLoading();

  // Generate table of contents
  const toc = useMemo(() => {
    if (!post?.content) return [];
    return generateTOC(post.content);
  }, [post?.content]);

  // Social sharing URLs
  const socialUrls = useMemo(() => {
    if (!post) return [];
    return generateSocialShareUrls(post, window.location.href);
  }, [post]);

  // Track reading progress and active heading
  useEffect(() => {
    const handleScroll = () => {
      const headings = document.querySelectorAll('h1[id], h2[id], h3[id], h4[id], h5[id], h6[id]');
      let activeId = '';

      for (let i = headings.length - 1; i >= 0; i--) {
        const heading = headings[i];
        const rect = heading.getBoundingClientRect();
        if (rect.top <= 100) {
          activeId = heading.id;
          break;
        }
      }

      setActiveHeading(activeId);
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  // Handle comment submission
  useEffect(() => {
    if (post) {
      setIsLiked(Boolean(post.isLiked));
      setIsBookmarked(Boolean(post.isBookmarked));
      setLikeCount(post.likeCount ?? 0);
    }
  }, [post]);

  const handleCommentSubmit = async () => {
    if (!commentContent.trim() || !user || !id) return;

    // Mock comment creation - replace with real API call when available
    const newComment: Comment = {
      id: Date.now().toString(),
      postId: id,
      authorId: user.id,
      author: {
        id: user.id,
        username: user.userName,
        displayName: user.fullName || user.userName,
        avatarUrl: user.avatar,
        role: user.role.toString(),
        isVip: false,
      },
      parentId: replyingTo || undefined,
      content: commentContent,
      renderedContent: commentContent,
      status: 'Approved' as 'Approved' | 'Pending' | 'Rejected',
      depth: replyingTo ? 1 : 0,
      threadPath: '',
      likeCount: 0,
      replyCount: 0,
      isLiked: false,
      canEdit: true,
      canDelete: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      replies: [],
    };

    setComments(prev => [newComment, ...prev]);
    setCommentContent('');
    setReplyingTo(null);
  };

  // Handle social sharing
  const handleShare = (platform: string, url?: string) => {
    if (platform === 'copy') {
      navigator.clipboard.writeText(window.location.href);
      return;
    }

    if (url) {
      window.open(url, '_blank', 'noopener,noreferrer');
    }
  };

  // Generate JSON-LD structured data when post is available
  const structuredData = useMemo(() => {
    if (!post) return null;
    return {
      '@context': 'https://schema.org',
      '@type': 'BlogPosting',
      'headline': post.title,
      'description': post.excerpt,
      'image': post.featuredImage,
      'datePublished': post.publishedAt || post.createdAt,
      'dateModified': post.updatedAt,
      'author': {
        '@type': 'Person',
        'name': post.author.fullName || post.author.userName,
        'url': `${window.location.origin}/author/${post.author.userName}`
      },
      'publisher': {
        '@type': 'Organization',
        'name': 'Maple Blog',
        'logo': {
          '@type': 'ImageObject',
          'url': `${window.location.origin}/logo.png`
        }
      },
      'mainEntityOfPage': {
        '@type': 'WebPage',
        '@id': window.location.href
      }
    };
  }, [post]);

  // Handle like/bookmark actions
  const handleLike = async () => {
    if (!post || !isAuthenticated) {
      toastService.warning('请登录后点赞文章');
      return;
    }

    const nextLiked = !isLiked;
    setIsLiked(nextLiked);
    setLikeCount((prev) => Math.max(0, prev + (nextLiked ? 1 : -1)));

    try {
      if (nextLiked) {
        await HomeApiService.recordInteraction({
          postId: post.id,
          interactionType: 'like',
          timestamp: new Date().toISOString(),
        });
        toastService.success('已点赞');
      }
    } catch (error) {
      setIsLiked(!nextLiked);
      setLikeCount((prev) => Math.max(0, prev - (nextLiked ? 1 : -1)));

      const err = error instanceof Error ? error : new Error(String(error));
      toastService.error(err.message || '点赞失败，请稍后重试');
      errorReporter.captureError(err, {
        component: 'BlogPostPage',
        action: 'likePost',
        handled: true,
        extra: { postId: post.id },
      });
    }
  };

  const handleBookmark = async () => {
    if (!post || !isAuthenticated) {
      toastService.warning('请登录后收藏文章');
      return;
    }

    const nextBookmarked = !isBookmarked;
    setIsBookmarked(nextBookmarked);

    try {
      if (nextBookmarked) {
        await HomeApiService.recordInteraction({
          postId: post.id,
          interactionType: 'bookmark',
          timestamp: new Date().toISOString(),
        });
        toastService.success('已加入书签');
      }
    } catch (error) {
      setIsBookmarked(!nextBookmarked);
      const err = error instanceof Error ? error : new Error(String(error));
      toastService.error(err.message || '收藏失败，请稍后重试');
      errorReporter.captureError(err, {
        component: 'BlogPostPage',
        action: 'bookmarkPost',
        handled: true,
        extra: { postId: post.id },
      });
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto px-4 py-8">
          <div className="max-w-4xl mx-auto">
            <div className="animate-pulse">
              <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded mb-4"></div>
              <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded mb-2"></div>
              <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded mb-8"></div>
              <div className="h-64 bg-gray-200 dark:bg-gray-700 rounded"></div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !post) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            Article Not Found
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mb-6">
            The article you&apos;re looking for doesn&apos;t exist or has been removed.
          </p>
          <Link
            to="/blog"
            className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Back to Blog
          </Link>
        </div>
      </div>
    );
  }


  return (
    <>
      <DocumentHead
        title={post.metaTitle || post.title}
        description={post.metaDescription || post.excerpt}
        keywords={post.metaKeywords}
        ogTitle={post.title}
        ogDescription={post.excerpt}
        ogImage={post.featuredImage}
        ogUrl={window.location.href}
      />

      {/* JSON-LD Structured Data */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(structuredData)
        }}
      />

      {/* Reading Progress Bar */}
      <div className="fixed top-0 left-0 right-0 z-50 h-1 bg-gray-200 dark:bg-gray-700">
        <div
          className="h-full bg-blue-600 transition-all duration-150 ease-out"
          style={{ width: `${readingProgress}%` }}
        />
      </div>

      {/* Floating Action Buttons */}
      <div className="fixed right-4 bottom-4 flex flex-col gap-2 z-40">
        <button
          onClick={() => setShowTOC(!showTOC)}
          className="p-3 bg-white dark:bg-gray-800 rounded-full shadow-lg border border-gray-200 dark:border-gray-700 hover:shadow-xl transition-all"
          title="Table of Contents"
        >
          <Menu className="w-5 h-5 text-gray-600 dark:text-gray-400" />
        </button>
        <button
          onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
          className="p-3 bg-white dark:bg-gray-800 rounded-full shadow-lg border border-gray-200 dark:border-gray-700 hover:shadow-xl transition-all"
          title="Back to Top"
        >
          <ChevronUp className="w-5 h-5 text-gray-600 dark:text-gray-400" />
        </button>
      </div>

      {/* Table of Contents Sidebar */}
      {showTOC && toc.length > 0 && (
        <div className="fixed right-4 top-20 w-64 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-30 max-h-96 overflow-y-auto">
          <div className="p-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
            <h3 className="font-semibold text-gray-900 dark:text-white">Contents</h3>
            <button
              onClick={() => setShowTOC(false)}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
          <nav className="p-2">
            {toc.map((item, index) => (
              <a
                key={index}
                href={`#${item.anchor}`}
                className={`block px-2 py-1 text-sm rounded transition-colors ${
                  activeHeading === item.anchor
                    ? 'bg-blue-50 text-blue-600 dark:bg-blue-900 dark:text-blue-400'
                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
                style={{ paddingLeft: `${(item.level - 1) * 12 + 8}px` }}
                onClick={() => setShowTOC(false)}
              >
                {item.text}
              </a>
            ))}
          </nav>
        </div>
      )}

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container mx-auto px-4 py-8">
          <article className="max-w-4xl mx-auto">
            {/* Article Header */}
            <header className="mb-8">
              {/* Category Badge */}
              {post.category && (
                <Link
                  to={`/blog/category/${post.category.slug}`}
                  className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium mb-4"
                  style={{
                    backgroundColor: post.category.color + '20',
                    color: post.category.color,
                  }}
                >
                  {post.category.icon && <span className="mr-1">{post.category.icon}</span>}
                  {post.category.name}
                </Link>
              )}

              {/* Title */}
              <h1 className="text-3xl md:text-4xl lg:text-5xl font-bold text-gray-900 dark:text-white leading-tight mb-6">
                {post.title}
              </h1>

              {/* Meta Information */}
              <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 dark:text-gray-400 mb-6">
                <div className="flex items-center gap-2">
                  <User className="w-4 h-4" />
                  <Link
                    to={`/author/${post.author.userName}`}
                    className="hover:text-blue-600 dark:hover:text-blue-400"
                  >
                    {post.author.fullName || post.author.userName}
                  </Link>
                </div>
                <div className="flex items-center gap-2">
                  <Calendar className="w-4 h-4" />
                  <time dateTime={post.publishedAt || post.createdAt}>
                    {new Date(post.publishedAt || post.createdAt).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                    })}
                  </time>
                </div>
                <div className="flex items-center gap-2">
                  <Clock className="w-4 h-4" />
                  <span>{post.readTimeMinutes} min read</span>
                </div>
                <div className="flex items-center gap-2">
                  <Eye className="w-4 h-4" />
                  <span>{post.viewCount.toLocaleString()} views</span>
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center gap-4 mb-8">
                <button
                  onClick={handleLike}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg border transition-colors ${
                    isLiked
                      ? 'bg-red-50 text-red-600 border-red-200 dark:bg-red-900 dark:text-red-400'
                      : 'bg-white text-gray-600 border-gray-200 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700'
                  }`}
                >
                  <Heart className={`w-4 h-4 ${isLiked ? 'fill-current' : ''}`} />
                  <span>{likeCount}</span>
                </button>

                <button
                  onClick={handleBookmark}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg border transition-colors ${
                    isBookmarked
                      ? 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-900 dark:text-blue-400'
                      : 'bg-white text-gray-600 border-gray-200 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700'
                  }`}
                >
                  <Bookmark className={`w-4 h-4 ${isBookmarked ? 'fill-current' : ''}`} />
                  <span>Bookmark</span>
                </button>

                <button
                  onClick={() => setShowShareModal(true)}
                  className="flex items-center gap-2 px-4 py-2 rounded-lg border bg-white text-gray-600 border-gray-200 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700 transition-colors"
                >
                  <Share2 className="w-4 h-4" />
                  <span>Share</span>
                </button>

                {canManagePosts && (
                  <Link
                    to={`/admin/posts/${post.id}/edit`}
                    className="flex items-center gap-2 px-4 py-2 rounded-lg border bg-white text-gray-600 border-gray-200 hover:bg-gray-50 dark:bg-gray-800 dark:text-gray-400 dark:border-gray-700 transition-colors"
                  >
                    <ExternalLink className="w-4 h-4" />
                    <span>Edit</span>
                  </Link>
                )}
              </div>

              {/* Featured Image */}
              {post.featuredImage && (
                <div className="mb-8 rounded-lg overflow-hidden">
                  <img
                    src={post.featuredImage}
                    alt={post.title}
                    className="w-full h-64 md:h-96 object-cover"
                    loading="lazy"
                  />
                </div>
              )}

              {/* Excerpt */}
              {post.excerpt && (
                <div className="bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 p-4 rounded-r-lg mb-8">
                  <p className="text-gray-700 dark:text-gray-300 italic text-lg">
                    {post.excerpt}
                  </p>
                </div>
              )}
            </header>

            {/* Article Content */}
            <div ref={contentRef} className="mb-12">
              <RichTextRenderer
                content={post.content}
                className="prose prose-lg max-w-none dark:prose-invert prose-headings:scroll-mt-20"
                enableSyntaxHighlight={true}
                enableTables={true}
                enableTaskLists={true}
              />
            </div>

            {/* Tags */}
            {post.tags.length > 0 && (
              <div className="mb-8">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Tags
                </h3>
                <div className="flex flex-wrap gap-2">
                  {post.tags.map((tag) => (
                    <Link
                      key={tag.id}
                      to={`/blog/tag/${tag.slug}`}
                      className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-800 dark:text-gray-300 dark:hover:bg-gray-700 transition-colors"
                    >
                      <Tag className="w-3 h-3 mr-1" />
                      {tag.name}
                    </Link>
                  ))}
                </div>
              </div>
            )}

            {/* Author Info */}
            <div className="bg-white dark:bg-gray-800 rounded-lg p-6 mb-8 border border-gray-200 dark:border-gray-700">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                About the Author
              </h3>
              <div className="flex items-start gap-4">
                {post.author.avatar && (
                  <img
                    src={post.author.avatar}
                    alt={post.author.fullName || post.author.userName}
                    className="w-16 h-16 rounded-full object-cover"
                  />
                )}
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white">
                    {post.author.fullName || post.author.userName}
                  </h4>
                  {(post.author as typeof post.author & { bio?: string }).bio && (
                    <p className="text-gray-600 dark:text-gray-400 mt-1">
                      {(post.author as typeof post.author & { bio?: string }).bio}
                    </p>
                  )}
                  <Link
                    to={`/author/${post.author.userName}`}
                    className="inline-flex items-center text-blue-600 dark:text-blue-400 hover:underline mt-2 text-sm"
                  >
                    View all posts
                    <ExternalLink className="w-3 h-3 ml-1" />
                  </Link>
                </div>
              </div>
            </div>

            {/* Related Articles */}
            <RelatedArticles currentPostId={post.id} tags={post.tags} category={post.category} />

            {/* Comments Section */}
            <section className="mb-8">
              <div className="flex items-center justify-between mb-6">
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                  <MessageCircle className="w-5 h-5" />
                  Comments ({post.commentCount + comments.length})
                </h3>
                <button
                  onClick={() => setShowComments(!showComments)}
                  className="text-blue-600 dark:text-blue-400 hover:underline text-sm"
                >
                  {showComments ? 'Hide Comments' : 'Show Comments'}
                </button>
              </div>

              {showComments && (
                <>
                  {/* Comment Form */}
                  {isAuthenticated && (
                    <div className="bg-white dark:bg-gray-800 rounded-lg p-4 mb-6 border border-gray-200 dark:border-gray-700">
                      <div className="flex gap-3">
                        {user?.avatar && (
                          <img
                            src={user.avatar}
                            alt={user.fullName || user.userName}
                            className="w-10 h-10 rounded-full object-cover"
                          />
                        )}
                        <div className="flex-1">
                          <textarea
                            value={commentContent}
                            onChange={(e) => setCommentContent(e.target.value)}
                            placeholder={replyingTo ? 'Write a reply...' : 'Write a comment...'}
                            className="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                            rows={3}
                          />
                          <div className="flex items-center justify-between mt-2">
                            <div className="text-xs text-gray-500 dark:text-gray-400">
                              {replyingTo && (
                                <button
                                  onClick={() => setReplyingTo(null)}
                                  className="text-red-500 hover:text-red-600 mr-4"
                                >
                                  Cancel Reply
                                </button>
                              )}
                              Markdown supported
                            </div>
                            <button
                              onClick={handleCommentSubmit}
                              disabled={!commentContent.trim()}
                              className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                            >
                              <Send className="w-4 h-4" />
                              Post Comment
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}

                  {/* Comments List */}
                  <div className="space-y-4">
                    {commentsLoading ? (
                      <div className="text-center py-8">
                        <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                        <p className="text-gray-500 dark:text-gray-400 mt-2">Loading comments...</p>
                      </div>
                    ) : comments.length === 0 ? (
                      <div className="text-center py-8">
                        <MessageCircle className="w-12 h-12 text-gray-400 dark:text-gray-600 mx-auto mb-4" />
                        <p className="text-gray-500 dark:text-gray-400">
                          No comments yet. Be the first to comment!
                        </p>
                      </div>
                    ) : (
                      comments.map((comment) => (
                        <CommentItem
                          key={comment.id}
                          comment={comment}
                          onReply={(commentId) => setReplyingTo(commentId)}
                          currentUser={user}
                        />
                      ))
                    )}
                  </div>
                </>
              )}
            </section>
          </article>
        </div>
      </div>

      {/* Share Modal */}
      {showShareModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Share Article
              </h3>
              <button
                onClick={() => setShowShareModal(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            
            <div className="space-y-3">
              {socialUrls.map((share) => (
                <button
                  key={share.platform}
                  onClick={() => handleShare(share.platform, share.url)}
                  className="w-full flex items-center gap-3 px-4 py-3 rounded-lg border border-gray-200 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                >
                  {share.platform === 'twitter' && <Twitter className="w-5 h-5 text-blue-400" />}
                  {share.platform === 'facebook' && <Facebook className="w-5 h-5 text-blue-600" />}
                  {share.platform === 'linkedin' && <Linkedin className="w-5 h-5 text-blue-700" />}
                  {share.platform === 'reddit' && <MessageCircle className="w-5 h-5 text-orange-500" />}
                  <span className="text-gray-900 dark:text-white capitalize">
                    Share on {share.platform}
                  </span>
                </button>
              ))}
              
              <button
                onClick={() => handleShare('copy')}
                className="w-full flex items-center gap-3 px-4 py-3 rounded-lg border border-gray-200 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                <Copy className="w-5 h-5 text-gray-500" />
                <span className="text-gray-900 dark:text-white">Copy Link</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

// Comment Component
const CommentItem: React.FC<{
  comment: Comment;
  onReply: (commentId: string) => void;
  currentUser: { id: string; userName: string; fullName?: string; avatar?: string } | null;
}> = ({ comment, onReply }) => {
  const [isLiked, setIsLiked] = useState(comment.isLiked);
  const [showReplies, setShowReplies] = useState(false);

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
      <div className="flex gap-3">
        {comment.author?.avatarUrl && (
          <img
            src={comment.author.avatarUrl}
            alt={comment.author.displayName}
            className="w-10 h-10 rounded-full object-cover"
          />
        )}
        <div className="flex-1">
          <div className="flex items-center gap-2 mb-2">
            <span className="font-semibold text-gray-900 dark:text-white">
              {comment.author?.displayName || 'Anonymous'}
            </span>
            {comment.author?.isVip && (
              <span className="px-2 py-1 bg-yellow-100 text-yellow-800 text-xs rounded-full">
                VIP
              </span>
            )}
            <time className="text-xs text-gray-500 dark:text-gray-400">
              {new Date(comment.createdAt).toLocaleDateString()}
            </time>
          </div>
          
          <div className="text-gray-700 dark:text-gray-300 mb-3">
            <RichTextRenderer
              content={comment.renderedContent}
              className="prose prose-sm max-w-none dark:prose-invert"
              enableSyntaxHighlight={false}
            />
          </div>
          
          <div className="flex items-center gap-4 text-sm">
            <button
              onClick={() => setIsLiked(!isLiked)}
              className={`flex items-center gap-1 transition-colors ${
                isLiked ? 'text-red-500' : 'text-gray-500 hover:text-red-500'
              }`}
            >
              <ThumbsUp className={`w-4 h-4 ${isLiked ? 'fill-current' : ''}`} />
              <span>{comment.likeCount}</span>
            </button>
            
            <button
              onClick={() => onReply(comment.id)}
              className="flex items-center gap-1 text-gray-500 hover:text-blue-500 transition-colors"
            >
              <Reply className="w-4 h-4" />
              <span>Reply</span>
            </button>
            
            {comment.replyCount > 0 && (
              <button
                onClick={() => setShowReplies(!showReplies)}
                className="flex items-center gap-1 text-blue-500 hover:text-blue-600 transition-colors"
              >
                <ChevronDown className={`w-4 h-4 transition-transform ${showReplies ? 'rotate-180' : ''}`} />
                <span>{comment.replyCount} replies</span>
              </button>
            )}
          </div>
          
          {showReplies && comment.replies?.length > 0 && (
            <div className="mt-4 pl-4 border-l-2 border-gray-200 dark:border-gray-600 space-y-4">
              {comment.replies.map((reply) => (
                <CommentItem
                  key={reply.id}
                  comment={reply}
                  onReply={onReply}
                  currentUser={null}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

// Related Articles Component
const RelatedArticles: React.FC<{
  currentPostId: string;
  tags: Array<{ id: string; name: string; slug: string }>;
  category: { id: string; name: string; color: string } | null;
}> = ({ currentPostId, tags, category }) => {
  const { usePostsList } = useBlogPostQueries();
  
  // Fetch related posts based on tags and category
  const { data: relatedPosts } = usePostsList({
    categoryId: category?.id,
    tagIds: tags.map(tag => tag.id),
    pageSize: 6,
  });

  // Filter out current post and limit to 4 related articles
  const filteredPosts = relatedPosts?.items
    .filter(post => post.id !== currentPostId)
    .slice(0, 4) || [];

  if (filteredPosts.length === 0) {
    return null;
  }

  return (
    <div className="mb-8">
      <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-6">
        Related Articles
      </h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {filteredPosts.map((post) => (
          <Link
            key={post.id}
            to={`/blog/${post.slug || post.id}`}
            className="group bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden hover:shadow-lg transition-all duration-300"
          >
            {post.featuredImage && (
              <div className="aspect-video overflow-hidden">
                <img
                  src={post.featuredImage}
                  alt={post.title}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  loading="lazy"
                />
              </div>
            )}
            <div className="p-4">
              {post.category && (
                <span
                  className="inline-block px-2 py-1 text-xs font-medium rounded-full mb-2"
                  style={{
                    backgroundColor: post.category.color + '20',
                    color: post.category.color,
                  }}
                >
                  {post.category.name}
                </span>
              )}
              <h4 className="font-semibold text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors line-clamp-2 mb-2">
                {post.title}
              </h4>
              <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2 mb-3">
                {post.excerpt}
              </p>
              <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
                <div className="flex items-center gap-1">
                  <Calendar className="w-3 h-3" />
                  <time dateTime={post.publishedAt || post.createdAt}>
                    {new Date(post.publishedAt || post.createdAt).toLocaleDateString('en-US', {
                      month: 'short',
                      day: 'numeric',
                    })}
                  </time>
                </div>
                <div className="flex items-center gap-1">
                  <Clock className="w-3 h-3" />
                  <span>{post.readTimeMinutes} min</span>
                </div>
                <div className="flex items-center gap-1">
                  <Eye className="w-3 h-3" />
                  <span>{post.viewCount}</span>
                </div>
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
};

export default BlogPostPage;
