/**
 * Enhanced Blog Post Page with Complete SEO Implementation
 * Demonstrates the full SEO solution for React 19
 */

import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useSEO, generateBlogPostStructuredData } from '@/hooks/useSEO';

interface BlogPost {
  id: string;
  title: string;
  content: string;
  excerpt: string;
  author: {
    name: string;
    avatar?: string;
  };
  publishedAt: string;
  updatedAt?: string;
  coverImage?: string;
  tags: string[];
  category: string;
  slug: string;
  readingTime: number;
  views?: number;
}

export function BlogPostPageEnhanced() {
  const { slug } = useParams<{ slug: string }>();
  const [post, setPost] = useState<BlogPost | null>(null);
  const [loading, setLoading] = useState(true);

  // Fetch post data (simulated)
  useEffect(() => {
    const fetchPost = async () => {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 500));

      // Example post data
      const mockPost: BlogPost = {
        id: '1',
        slug: slug || 'example-post',
        title: '深入理解 React 19 的新特性',
        excerpt: '探索 React 19 带来的革命性变化，包括新的并发特性、改进的性能和更好的开发体验。',
        content: `
          # 深入理解 React 19 的新特性

          React 19 带来了许多令人兴奋的新特性和改进...

          ## 主要更新

          ### 1. 改进的并发渲染
          React 19 进一步优化了并发渲染机制...

          ### 2. 自动批处理
          现在所有的状态更新都会自动批处理...

          ### 3. 新的 Hook
          引入了几个新的实用 Hook...

          ## 性能优化
          React 19 在性能方面做了大量优化...

          ## 迁移指南
          从 React 18 升级到 React 19 相对简单...
        `,
        author: {
          name: 'Maple Blog Team',
          avatar: '/images/authors/team.jpg',
        },
        publishedAt: '2025-01-22T12:00:00Z',
        updatedAt: '2025-01-23T08:00:00Z',
        coverImage: '/images/blog/react-19.jpg',
        tags: ['React', 'JavaScript', 'Web Development', 'Frontend'],
        category: '技术',
        readingTime: 5,
        views: 1234,
      };

      setPost(mockPost);
      setLoading(false);
    };

    fetchPost();
  }, [slug]);

  // Set up SEO when post data is loaded
  useSEO({
    title: post?.title,
    description: post?.excerpt,
    keywords: post?.tags.join(', '),
    author: post?.author.name,
    image: post?.coverImage ? `${window.location.origin}${post.coverImage}` : undefined,
    url: post ? `${window.location.origin}/blog/${post.slug}` : undefined,
    type: 'article',
    publishedTime: post?.publishedAt,
    modifiedTime: post?.updatedAt,
    section: post?.category,
    tags: post?.tags,
    canonical: post ? `${window.location.origin}/blog/${post.slug}` : undefined,
    structuredData: post ? generateBlogPostStructuredData({
      title: post.title,
      description: post.excerpt,
      author: post.author.name,
      publishedAt: post.publishedAt,
      updatedAt: post.updatedAt,
      image: post.coverImage ? `${window.location.origin}${post.coverImage}` : undefined,
      url: `${window.location.origin}/blog/${post.slug}`,
      tags: post.tags,
    }) : undefined,
  });

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!post) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900">文章未找到</h1>
          <p className="mt-2 text-gray-600">请检查链接是否正确</p>
        </div>
      </div>
    );
  }

  return (
    <article className="max-w-4xl mx-auto px-4 py-8">
      {/* Article Header */}
      <header className="mb-8">
        {/* Category */}
        <div className="mb-4">
          <span className="inline-block px-3 py-1 text-sm font-medium text-blue-600 bg-blue-100 rounded-full">
            {post.category}
          </span>
        </div>

        {/* Title */}
        <h1 className="text-4xl font-bold text-gray-900 mb-4">{post.title}</h1>

        {/* Excerpt */}
        <p className="text-xl text-gray-600 mb-6">{post.excerpt}</p>

        {/* Meta Information */}
        <div className="flex items-center space-x-4 text-sm text-gray-500">
          <div className="flex items-center">
            {post.author.avatar && (
              <img
                src={post.author.avatar}
                alt={post.author.name}
                className="w-10 h-10 rounded-full mr-2"
              />
            )}
            <span className="font-medium">{post.author.name}</span>
          </div>
          <span>•</span>
          <time dateTime={post.publishedAt}>
            {new Date(post.publishedAt).toLocaleDateString('zh-CN', {
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </time>
          <span>•</span>
          <span>{post.readingTime} 分钟阅读</span>
          {post.views && (
            <>
              <span>•</span>
              <span>{post.views.toLocaleString()} 次浏览</span>
            </>
          )}
        </div>
      </header>

      {/* Cover Image */}
      {post.coverImage && (
        <figure className="mb-8">
          <img
            src={post.coverImage}
            alt={post.title}
            className="w-full h-auto rounded-lg shadow-lg"
          />
        </figure>
      )}

      {/* Article Content */}
      <div
        className="prose prose-lg max-w-none"
        dangerouslySetInnerHTML={{ __html: post.content }}
      />

      {/* Tags */}
      {post.tags.length > 0 && (
        <footer className="mt-8 pt-8 border-t border-gray-200">
          <div className="flex items-center flex-wrap gap-2">
            <span className="text-sm font-medium text-gray-700">标签：</span>
            {post.tags.map(tag => (
              <a
                key={tag}
                href={`/tag/${tag.toLowerCase()}`}
                className="inline-block px-3 py-1 text-sm text-gray-600 bg-gray-100 rounded-full hover:bg-gray-200 transition-colors"
              >
                #{tag}
              </a>
            ))}
          </div>
        </footer>
      )}

      {/* Social Sharing */}
      <div className="mt-8 flex items-center space-x-4">
        <span className="text-sm font-medium text-gray-700">分享：</span>
        <button
          onClick={() => {
            if (navigator.share) {
              navigator.share({
                title: post.title,
                text: post.excerpt,
                url: window.location.href,
              });
            }
          }}
          className="p-2 text-gray-600 hover:text-blue-600 transition-colors"
          aria-label="分享文章"
        >
          <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path d="M15 8a3 3 0 10-2.977-2.63l-4.94 2.47a3 3 0 100 4.319l4.94 2.47a3 3 0 10.895-1.789l-4.94-2.47a3.027 3.027 0 000-.74l4.94-2.47C13.456 7.68 14.19 8 15 8z" />
          </svg>
        </button>
      </div>
    </article>
  );
}