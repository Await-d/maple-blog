/**
 * PostDetailPage - Individual blog post detail view
 * Implements comprehensive post viewing experience with engagement features
 */

import React, { useCallback, useEffect, useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Helmet } from 'react-helmet-async';
import { useLogger } from '@/utils/logger';
import blogApi from '@/services/blog/blogApi';
import { analytics } from '@/services/analytics';
import { PostReader } from '@/components/blog/PostReader';
import { CommentSystem } from '@/features/comments';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { useAuth } from '@/hooks/useAuth';
import { useSEO } from '@/hooks/useSEO';
import type { BlogPost, BlogTag } from '@/types/blog';

interface PostDetailPageProps {
  className?: string;
}

export const PostDetailPage: React.FC<PostDetailPageProps> = ({ className = '' }) => {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const logger = useLogger('PostDetailPage');
  const { user } = useAuth();
  const contentRef = useRef<HTMLDivElement>(null);
  
  // Tracking refs for scroll and reading time
  const readingStartTime = useRef<number>(0);
  const lastScrollPosition = useRef(0);
  const maxScrollDepth = useRef(0);

  // Fetch post data
  const {
    data: post,
    isLoading: postLoading,
    error: postError,
    refetch: refetchPost
  } = useQuery({
    queryKey: ['blog-post', slug],
    queryFn: async () => {
      if (!slug) {
        throw new Error('Invalid post slug');
      }
      logger.startTimer('fetch_post');
      
      try {
        const result = await blogApi.getPost(slug);
        
        logger.endTimer('fetch_post', 'api_success', { 
          post_id: result.id,
          post_title: result.title,
          post_length: result.content?.length || 0
        });

        return result;
      } catch (error) {
        logger.logApiError('GET', `/api/posts/${slug}`, error as Error);
        throw error;
      }
    },
    enabled: Boolean(slug), // Only run query if slug exists
    staleTime: 10 * 60 * 1000, // 10 minutes
    gcTime: 30 * 60 * 1000, // 30 minutes
    retry: (failureCount, error) => {
      // Don't retry on 404
      const httpError = error as Error & { status?: number };
      if (httpError?.status === 404) return false;
      return failureCount < 3;
    }
  });

  // Update SEO metadata
  useSEO({
    title: post ? `${post.title} | Maple Blog` : 'Loading Post | Maple Blog',
    description: post?.excerpt || 'Loading blog post...',
    keywords: post?.tags?.map(tag => tag.name).join(', '),
    image: post?.featuredImage,
    type: 'article',
    publishedTime: post?.publishedAt,
    modifiedTime: post?.updatedAt,
    author: post?.author?.displayName,
    section: post?.category?.name
  });

  // Like/Unlike mutation
  const likeMutation = useMutation({
    mutationFn: async (_liked: boolean) => {
      if (!post || !user) throw new Error('User must be logged in to like posts');

      // Note: likePost/unlikePost methods need to be implemented in blogApi
      throw new Error('Like functionality not yet implemented');
    },
    onSuccess: (_, liked) => {
      if (post) {
        // Update cache optimistically
        queryClient.setQueryData(['blog-post', slug], (oldData: BlogPost | undefined) => {
          if (!oldData) return oldData;
          return {
            ...oldData,
            isLiked: liked,
            likeCount: liked ? oldData.likeCount + 1 : oldData.likeCount - 1
          };
        });

        logger.logUserAction(liked ? 'like_post' : 'unlike_post', 'post_engagement');

        analytics.track(liked ? 'post_liked' : 'post_unliked', 'PostDetailInterface');
      }
    },
    onError: (error) => {
      logger.logError('Failed to update post like status', 'like_error', {
        post_id: post?.id,
        error_message: (error as Error).message
      });
    }
  });

  // Bookmark mutation
  const bookmarkMutation = useMutation({
    mutationFn: async (_bookmarked: boolean) => {
      if (!post || !user) throw new Error('User must be logged in to bookmark posts');

      // Note: bookmarkPost/unbookmarkPost methods need to be implemented in blogApi
      throw new Error('Bookmark functionality not yet implemented');
    },
    onSuccess: (_, bookmarked) => {
      if (post) {
        // Update cache optimistically
        queryClient.setQueryData(['blog-post', slug], (oldData: BlogPost | undefined) => {
          if (!oldData) return oldData;
          return {
            ...oldData,
            isBookmarked: bookmarked
          };
        });

        logger.logUserAction(bookmarked ? 'bookmark_post' : 'unbookmark_post', 'post_engagement');

        analytics.track(bookmarked ? 'post_bookmarked' : 'post_unbookmarked', 'PostDetailInterface');
      }
    },
    onError: (error) => {
      logger.logError('Failed to update post bookmark status', 'bookmark_error', {
        post_id: post?.id,
        error_message: (error as Error).message
      });
    }
  });

  // Share functionality
  const handleShare = useCallback(async (method: 'native' | 'clipboard' | 'twitter' | 'linkedin' | 'facebook') => {
    if (!post) return;

    const url = window.location.href;
    const title = post.title;
    const text = post.excerpt || title;

    logger.logUserAction('share_post', 'post_engagement');

    analytics.track('post_shared', 'PostDetailInterface');

    try {
      switch (method) {
        case 'native':
          if (navigator.share) {
            await navigator.share({ title, text, url });
          } else {
            // Fallback to clipboard
            await navigator.clipboard.writeText(url);
          }
          break;
          
        case 'clipboard':
          await navigator.clipboard.writeText(url);
          break;
          
        case 'twitter':
          window.open(`https://twitter.com/intent/tweet?text=${encodeURIComponent(text)}&url=${encodeURIComponent(url)}`, '_blank');
          break;
          
        case 'linkedin':
          window.open(`https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(url)}`, '_blank');
          break;
          
        case 'facebook':
          window.open(`https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(url)}`, '_blank');
          break;
      }
    } catch (error) {
      logger.logError('Share failed', 'share_error', {
        post_id: post.id,
        share_method: method,
        error_message: (error as Error).message
      });
    }
  }, [post, logger]);

  // Handle like toggle
  const handleLikeToggle = useCallback(() => {
    if (!post) return;
    likeMutation.mutate(!post.isLiked);
  }, [post, likeMutation]);

  // Handle bookmark toggle
  const handleBookmarkToggle = useCallback(() => {
    if (!post) return;
    bookmarkMutation.mutate(!post.isBookmarked);
  }, [post, bookmarkMutation]);

  // Handle retry
  const handleRetry = useCallback(() => {
    logger.logUserAction('retry_fetch', 'error_recovery');
    refetchPost();
  }, [logger, refetchPost]);

  // Track reading behavior
  useEffect(() => {
    if (!post) return;

    // Track page view and reading start
    readingStartTime.current = Date.now();
    
    logger.logUserAction('post_view_start', 'reading_tracking');

    analytics.track('post_view_detailed', 'PostDetailInterface');

    // Scroll tracking
    const handleScroll = () => {
      const scrollTop = window.scrollY;
      const documentHeight = document.documentElement.scrollHeight - window.innerHeight;
      const scrollPercent = Math.min(100, (scrollTop / documentHeight) * 100);

      if (scrollPercent > maxScrollDepth.current) {
        maxScrollDepth.current = scrollPercent;
      }

      lastScrollPosition.current = scrollPercent;
    };

    window.addEventListener('scroll', handleScroll, { passive: true });

    // Track reading completion on unmount
    return () => {
      window.removeEventListener('scroll', handleScroll);

      if (readingStartTime.current) {
        logger.logUserAction('post_view_end', 'reading_tracking');

        analytics.track('post_reading_completed', 'PostDetailInterface');
      }
    };
  }, [post, logger]);

  // Handle 404 error
  if (postError && (postError as Error & { status?: number })?.status === 404) {
    return (
      <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
        <Helmet>
          <title>Post Not Found | Maple Blog</title>
          <meta name="robots" content="noindex, nofollow" />
        </Helmet>
        <div className="container mx-auto px-4 py-8">
          <div className="flex flex-col items-center justify-center min-h-[50vh] text-center">
            <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
              Post Not Found
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mb-8 max-w-md">
              The blog post you&apos;re looking for doesn&apos;t exist or may have been moved.
            </p>
            <div className="space-x-4">
              <Button onClick={() => navigate('/blog')}>
                Browse Blog
              </Button>
              <Button variant="outline" onClick={() => navigate('/')}>
                Go Home
              </Button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Loading state
  if (postLoading) {
    return (
      <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
        <Helmet>
          <title>Loading Post | Maple Blog</title>
        </Helmet>
        <div className="container mx-auto px-4 py-8">
          <div className="flex items-center justify-center min-h-[50vh]">
            <LoadingSpinner size="lg" />
          </div>
        </div>
      </div>
    );
  }

  // Error state
  if (postError && !post) {
    return (
      <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`}>
        <Helmet>
          <title>Error Loading Post | Maple Blog</title>
        </Helmet>
        <div className="container mx-auto px-4 py-8">
          <div className="flex flex-col items-center justify-center min-h-[50vh] text-center">
            <Alert className="mb-6 max-w-md">
              <h3 className="font-semibold mb-2">Unable to Load Post</h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
                We&apos;re having trouble loading this blog post. This might be a temporary issue.
              </p>
              <div className="space-x-2">
                <Button onClick={handleRetry}>
                  Try Again
                </Button>
                <Button variant="outline" onClick={() => navigate('/blog')}>
                  Browse Blog
                </Button>
              </div>
            </Alert>
          </div>
        </div>
      </div>
    );
  }

  // Handle missing slug parameter
  if (!slug) {
    logger.error('PostDetailPage rendered without slug parameter', 'routing_error');
    navigate('/blog', { replace: true });
    return null;
  }

  if (!post) return null;

  return (
    <div className={`min-h-screen bg-gray-50 dark:bg-gray-900 ${className}`} ref={contentRef}>
      <Helmet>
        <title>{post.title} | Maple Blog</title>
        <meta name="description" content={post.excerpt} />
        <meta name="keywords" content={post.tags?.map(tag => tag.name).join(', ')} />
        
        {/* Open Graph */}
        <meta property="og:title" content={post.title} />
        <meta property="og:description" content={post.excerpt} />
        <meta property="og:type" content="article" />
        <meta property="og:url" content={window.location.href} />
        {post.featuredImage && <meta property="og:image" content={post.featuredImage} />}
        
        {/* Article specific */}
        <meta property="article:published_time" content={post.publishedAt} />
        <meta property="article:modified_time" content={post.updatedAt} />
        <meta property="article:author" content={post.author.displayName} />
        <meta property="article:section" content={post.category?.name} />
        {post.tags?.map((tag: BlogTag) => (
          <meta key={tag.id} property="article:tag" content={tag.name} />
        ))}

        {/* JSON-LD Structured Data */}
        <script type="application/ld+json">
          {JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'BlogPosting',
            'headline': post.title,
            'description': post.excerpt,
            'image': post.featuredImage,
            'author': {
              '@type': 'Person',
              'name': post.author.displayName,
              'url': `/authors/${post.author.userName}`
            },
            'publisher': {
              '@type': 'Organization',
              'name': 'Maple Blog',
              'logo': {
                '@type': 'ImageObject',
                'url': '/logo.png'
              }
            },
            'datePublished': post.publishedAt,
            'dateModified': post.updatedAt,
            'mainEntityOfPage': {
              '@type': 'WebPage',
              '@id': window.location.href
            }
          })}
        </script>
      </Helmet>

      <article className="container mx-auto px-4 py-8 max-w-4xl">
        {/* Breadcrumb */}
        <nav className="mb-8 text-sm text-gray-500 dark:text-gray-400">
          <Link to="/" className="hover:text-gray-700 dark:hover:text-gray-200">
            Home
          </Link>
          <span className="mx-2">/</span>
          <Link to="/blog" className="hover:text-gray-700 dark:hover:text-gray-200">
            Blog
          </Link>
          {post.category && (
            <>
              <span className="mx-2">/</span>
              <Link 
                to={`/blog?category=${post.category.slug}`}
                className="hover:text-gray-700 dark:hover:text-gray-200"
              >
                {post.category.name}
              </Link>
            </>
          )}
          <span className="mx-2">/</span>
          <span className="text-gray-700 dark:text-gray-300">{post.title}</span>
        </nav>

        {/* Post Header */}
        <header className="mb-8">
          <div className="flex flex-wrap gap-2 mb-4">
            {post.category && (
              <Badge variant="secondary">
                {post.category.name}
              </Badge>
            )}
            {post.tags?.map((tag: BlogTag) => (
              <Badge key={tag.id} variant="outline">
                {tag.name}
              </Badge>
            ))}
          </div>

          <h1 className="text-3xl md:text-4xl lg:text-5xl font-bold text-gray-900 dark:text-white mb-4 leading-tight">
            {post.title}
          </h1>

          <p className="text-lg text-gray-600 dark:text-gray-400 mb-6 leading-relaxed">
            {post.excerpt}
          </p>

          <div className="flex flex-wrap items-center gap-4 text-sm text-gray-500 dark:text-gray-400">
            <div className="flex items-center gap-2">
              <img
                src={post.author.avatar || '/default-avatar.png'}
                alt={post.author.displayName}
                className="w-8 h-8 rounded-full"
              />
              <span>By {post.author.displayName}</span>
            </div>
            
            <span>•</span>
            
            <time dateTime={post.publishedAt}>
              {new Date(post.publishedAt || post.createdAt).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'long',
                day: 'numeric'
              })}
            </time>
            
            {post.readingTime && (
              <>
                <span>•</span>
                <span>{post.readingTime} min read</span>
              </>
            )}

            <span>•</span>
            
            <span>{post.viewCount} views</span>
          </div>
        </header>

        {/* Post Content */}
        <PostReader
          post={{
            ...post,
            status: post.status.toString(),
            tags: post.tags.map(tag => tag.name),
            authorName: post.author.displayName || post.author.userName,
            categoryName: post.category?.name
          }}
          onLike={handleLikeToggle}
          onBookmark={handleBookmarkToggle}
          onShare={() => handleShare('native')}
        />

        {/* Related Posts */}
        {post.relatedPosts && post.relatedPosts.length > 0 && (
          <section className="mt-12">
            <Separator className="mb-8" />
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">
              Related Posts
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {post.relatedPosts.map(relatedPost => (
                <Link
                  key={relatedPost.id}
                  to={`/posts/${relatedPost.slug}`}
                  className="group block bg-white dark:bg-gray-800 rounded-lg shadow-sm hover:shadow-md transition-shadow p-4"
                >
                  {relatedPost.featuredImage && (
                    <img
                      src={relatedPost.featuredImage}
                      alt={relatedPost.title}
                      className="w-full h-32 object-cover rounded-lg mb-3"
                    />
                  )}
                  <h3 className="font-semibold text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 mb-2 line-clamp-2">
                    {relatedPost.title}
                  </h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                    {relatedPost.excerpt}
                  </p>
                </Link>
              ))}
            </div>
          </section>
        )}

        {/* Comments Section */}
        <section className="mt-12">
          <Separator className="mb-8" />
          <CommentSystem
            postId={post.id}
          />
        </section>
      </article>
    </div>
  );
};

export default PostDetailPage;
