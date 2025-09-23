// @ts-nocheck
/**
 * SEO优化组件
 * 实现动态meta标签、结构化数据、OpenGraph和Twitter Cards支持
 */

import React, { useEffect as _useEffect } from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { generateStructuredData as _generateStructuredData, generateMetaTags, generateOpenGraphTags, generateTwitterCards } from '../../utils/seoHelpers';

// SEO数据接口
export interface SEOData {
  // 基础信息
  title: string;
  description: string;
  keywords?: string[];
  canonical?: string;
  robots?: string;

  // Open Graph
  ogTitle?: string;
  ogDescription?: string;
  ogImage?: string;
  ogImageAlt?: string;
  ogType?: 'website' | 'article' | 'blog';
  ogLocale?: string;

  // Twitter Cards
  twitterCard?: 'summary' | 'summary_large_image' | 'app' | 'player';
  twitterSite?: string;
  twitterCreator?: string;
  twitterImage?: string;
  twitterImageAlt?: string;

  // 结构化数据
  structuredData?: {
    type: 'WebSite' | 'Blog' | 'Article' | 'Organization' | 'BreadcrumbList';
    data: Record<string, unknown>;
  }[];

  // 博客特定
  author?: string;
  publishedTime?: string;
  modifiedTime?: string;
  section?: string;
  tags?: string[];

  // 网站信息
  siteName?: string;
  siteUrl?: string;
  logoUrl?: string;
  language?: string;
}

// 首页SEO配置
export interface HomePageSEOProps {
  // 网站基础信息
  siteName?: string;
  siteDescription?: string;
  siteUrl?: string;
  logoUrl?: string;

  // 热门文章
  featuredPosts?: Array<{
    title: string;
    description: string;
    url: string;
    imageUrl?: string;
    author: string;
    publishedAt: string;
  }>;

  // 分类信息
  categories?: Array<{
    name: string;
    description: string;
    url: string;
    postCount: number;
  }>;

  // 统计信息
  stats?: {
    totalPosts: number;
    totalAuthors: number;
    totalCategories: number;
    monthlyVisitors?: number;
  };

  // 自定义SEO数据
  customSEO?: Partial<SEOData>;
}

/**
 * 首页SEO优化组件
 */
export const HomePageSEO: React.FC<HomePageSEOProps> = ({
  siteName = 'Maple Blog',
  siteDescription = '专业的技术博客平台，分享最新的编程技术、开发经验和行业见解',
  siteUrl = 'https://maple-blog.com',
  logoUrl = '/logo.png',
  featuredPosts = [],
  categories = [],
  stats = {
    totalPosts: 0,
    totalAuthors: 0,
    totalCategories: 0
  },
  customSEO = {}
}) => {
  // 生成首页SEO数据
  const generateHomePageSEO = (): SEOData => {
    const keywords = [
      '技术博客',
      '编程教程',
      '开发经验',
      'React',
      'TypeScript',
      '.NET',
      'Web开发',
      '前端开发',
      '后端开发',
      '全栈开发'
    ];

    // 添加分类关键词
    categories.forEach(category => {
      keywords.push(category.name);
    });

    // 基础SEO数据
    const baseSEO: SEOData = {
      title: `${siteName} - ${siteDescription}`,
      description: `${siteDescription}。已发布 ${stats.totalPosts} 篇文章，涵盖 ${stats.totalCategories} 个技术分类，由 ${stats.totalAuthors} 位优秀作者共同维护。`,
      keywords,
      canonical: siteUrl,
      robots: 'index,follow,max-snippet:-1,max-image-preview:large,max-video-preview:-1',

      // Open Graph
      ogTitle: siteName,
      ogDescription: siteDescription,
      ogImage: `${siteUrl}${logoUrl}`,
      ogImageAlt: `${siteName} Logo`,
      ogType: 'website',
      ogLocale: 'zh_CN',

      // Twitter Cards
      twitterCard: 'summary_large_image',
      twitterSite: '@maple_blog',
      twitterImage: `${siteUrl}${logoUrl}`,
      twitterImageAlt: `${siteName} Logo`,

      // 网站信息
      siteName,
      siteUrl,
      logoUrl: `${siteUrl}${logoUrl}`,
      language: 'zh-CN'
    };

    return { ...baseSEO, ...customSEO };
  };

  // 生成首页结构化数据
  const generateHomePageStructuredData = () => {
    const structuredData = [];

    // 网站基本信息
    structuredData.push({
      type: 'WebSite' as const,
      data: {
        '@context': 'https://schema.org',
        '@type': 'WebSite',
        name: siteName,
        description: siteDescription,
        url: siteUrl,
        logo: {
          '@type': 'ImageObject',
          url: `${siteUrl}${logoUrl}`,
          width: '800',
          height: '800'
        },
        sameAs: [
          // 社交媒体链接
        ],
        potentialAction: {
          '@type': 'SearchAction',
          target: {
            '@type': 'EntryPoint',
            urlTemplate: `${siteUrl}/search?q={search_term_string}`
          },
          'query-input': 'required name=search_term_string'
        }
      }
    });

    // 组织信息
    structuredData.push({
      type: 'Organization' as const,
      data: {
        '@context': 'https://schema.org',
        '@type': 'Organization',
        name: siteName,
        url: siteUrl,
        logo: `${siteUrl}${logoUrl}`,
        description: siteDescription,
        foundingDate: '2024',
        contactPoint: {
          '@type': 'ContactPoint',
          contactType: 'customer service',
          url: `${siteUrl}/contact`
        }
      }
    });

    // 博客信息
    structuredData.push({
      type: 'Blog' as const,
      data: {
        '@context': 'https://schema.org',
        '@type': 'Blog',
        name: siteName,
        description: siteDescription,
        url: siteUrl,
        publisher: {
          '@type': 'Organization',
          name: siteName,
          logo: `${siteUrl}${logoUrl}`
        },
        blogPost: featuredPosts.slice(0, 5).map(post => ({
          '@type': 'BlogPosting',
          headline: post.title,
          description: post.description,
          url: `${siteUrl}${post.url}`,
          image: post.imageUrl ? `${siteUrl}${post.imageUrl}` : `${siteUrl}${logoUrl}`,
          author: {
            '@type': 'Person',
            name: post.author
          },
          publisher: {
            '@type': 'Organization',
            name: siteName,
            logo: `${siteUrl}${logoUrl}`
          },
          datePublished: post.publishedAt
        }))
      }
    });

    // 面包屑导航
    structuredData.push({
      type: 'BreadcrumbList' as const,
      data: {
        '@context': 'https://schema.org',
        '@type': 'BreadcrumbList',
        itemListElement: [
          {
            '@type': 'ListItem',
            position: 1,
            name: '首页',
            item: siteUrl
          }
        ]
      }
    });

    return structuredData;
  };

  const seoData = generateHomePageSEO();
  const structuredData = generateHomePageStructuredData();

  // 生成meta标签
  const metaTags = generateMetaTags(seoData);
  const ogTags = generateOpenGraphTags(seoData);
  const twitterTags = generateTwitterCards(seoData);

  return (
    <Helmet>
      {/* 基础meta标签 */}
      {metaTags.map((tag, index) => (
        <meta key={`meta-${index}`} {...tag} />
      ))}

      {/* Open Graph标签 */}
      {ogTags.map((tag, index) => (
        <meta key={`og-${index}`} {...tag} />
      ))}

      {/* Twitter Cards标签 */}
      {twitterTags.map((tag, index) => (
        <meta key={`twitter-${index}`} {...tag} />
      ))}

      {/* 额外的SEO标签 */}
      <link rel="canonical" href={seoData.canonical} />

      {/* 语言设置 */}
      <html lang={seoData.language || 'zh-CN'} />

      {/* 结构化数据 */}
      {structuredData.map((data, index) => (
        <script
          key={`structured-data-${index}`}
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify(data.data)
          }}
        />
      ))}

      {/* PWA相关 */}
      <meta name="theme-color" content="#3b82f6" />
      <meta name="mobile-web-app-capable" content="yes" />
      <meta name="apple-mobile-web-app-capable" content="yes" />
      <meta name="apple-mobile-web-app-status-bar-style" content="default" />

      {/* DNS预解析 */}
      <link rel="dns-prefetch" href="//fonts.googleapis.com" />
      <link rel="dns-prefetch" href="//cdn.jsdelivr.net" />
      <link rel="dns-prefetch" href="//api.github.com" />

      {/* 预加载关键资源 */}
      <link rel="preload" href="/fonts/inter-var.woff2" as="font" type="font/woff2" crossOrigin="" />

      {/* 网站图标 */}
      <link rel="icon" type="image/x-icon" href="/favicon.ico" />
      <link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png" />
      <link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png" />
      <link rel="apple-touch-icon" href="/apple-touch-icon.png" />
      <link rel="manifest" href="/site.webmanifest" />

      {/* RSS源 */}
      <link rel="alternate" type="application/rss+xml" title={`${siteName} RSS Feed`} href="/feed.xml" />
      <link rel="alternate" type="application/atom+xml" title={`${siteName} Atom Feed`} href="/atom.xml" />
    </Helmet>
  );
};

// 文章页面SEO组件
export interface ArticlePageSEOProps {
  article: {
    title: string;
    description: string;
    content?: string;
    url: string;
    imageUrl?: string;
    author: {
      name: string;
      avatar?: string;
      bio?: string;
    };
    publishedAt: string;
    modifiedAt?: string;
    category: {
      name: string;
      url: string;
    };
    tags: string[];
    readingTime?: number;
    wordCount?: number;
  };
  siteName?: string;
  siteUrl?: string;
  customSEO?: Partial<SEOData>;
}

export const ArticlePageSEO: React.FC<ArticlePageSEOProps> = ({
  article,
  siteName = 'Maple Blog',
  siteUrl = 'https://maple-blog.com',
  customSEO = {}
}) => {
  const keywords = [
    ...article.tags,
    article.category.name,
    article.author.name,
    '技术文章',
    '编程教程'
  ];

  const seoData: SEOData = {
    title: `${article.title} - ${siteName}`,
    description: article.description,
    keywords,
    canonical: `${siteUrl}${article.url}`,
    robots: 'index,follow,max-snippet:-1,max-image-preview:large',

    // Open Graph
    ogTitle: article.title,
    ogDescription: article.description,
    ogImage: article.imageUrl ? `${siteUrl}${article.imageUrl}` : undefined,
    ogImageAlt: article.title,
    ogType: 'article',
    ogLocale: 'zh_CN',

    // Twitter Cards
    twitterCard: 'summary_large_image',
    twitterImage: article.imageUrl ? `${siteUrl}${article.imageUrl}` : undefined,
    twitterImageAlt: article.title,

    // 文章特定
    author: article.author.name,
    publishedTime: article.publishedAt,
    modifiedTime: article.modifiedAt,
    section: article.category.name,
    tags: article.tags,

    siteName,
    siteUrl,
    language: 'zh-CN',

    ...customSEO
  };

  // 文章结构化数据
  const articleStructuredData = {
    '@context': 'https://schema.org',
    '@type': 'BlogPosting',
    headline: article.title,
    description: article.description,
    image: article.imageUrl ? `${siteUrl}${article.imageUrl}` : undefined,
    url: `${siteUrl}${article.url}`,
    author: {
      '@type': 'Person',
      name: article.author.name,
      image: article.author.avatar ? `${siteUrl}${article.author.avatar}` : undefined,
      description: article.author.bio
    },
    publisher: {
      '@type': 'Organization',
      name: siteName,
      logo: `${siteUrl}/logo.png`
    },
    datePublished: article.publishedAt,
    dateModified: article.modifiedAt || article.publishedAt,
    articleSection: article.category.name,
    keywords: keywords.join(', '),
    wordCount: article.wordCount,
    timeRequired: article.readingTime ? `PT${article.readingTime}M` : undefined,
    inLanguage: 'zh-CN'
  };

  const metaTags = generateMetaTags(seoData);
  const ogTags = generateOpenGraphTags(seoData);
  const twitterTags = generateTwitterCards(seoData);

  return (
    <Helmet>
      {metaTags.map((tag, index) => (
        <meta key={`meta-${index}`} {...tag} />
      ))}
      {ogTags.map((tag, index) => (
        <meta key={`og-${index}`} {...tag} />
      ))}
      {twitterTags.map((tag, index) => (
        <meta key={`twitter-${index}`} {...tag} />
      ))}

      <link rel="canonical" href={seoData.canonical} />
      <html lang="zh-CN" />

      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(articleStructuredData)
        }}
      />

      {/* 文章特定meta标签 */}
      <meta property="article:published_time" content={article.publishedAt} />
      {article.modifiedAt && (
        <meta property="article:modified_time" content={article.modifiedAt} />
      )}
      <meta property="article:author" content={article.author.name} />
      <meta property="article:section" content={article.category.name} />
      {article.tags.map(tag => (
        <meta key={tag} property="article:tag" content={tag} />
      ))}
    </Helmet>
  );
};

export default HomePageSEO;