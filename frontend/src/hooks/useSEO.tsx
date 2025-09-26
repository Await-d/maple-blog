/**
 * SEO Hook for managing document head metadata
 * Compatible with React 19 without external dependencies
 */

import { useEffect } from 'react';

interface SEOConfig {
  title?: string;
  description?: string;
  keywords?: string;
  author?: string;
  image?: string;
  url?: string;
  type?: 'website' | 'article' | 'profile';
  publishedTime?: string;
  modifiedTime?: string;
  section?: string;
  tags?: string[];
  locale?: string;
  siteName?: string;
  twitterCard?: 'summary' | 'summary_large_image' | 'app' | 'player';
  twitterSite?: string;
  twitterCreator?: string;
  canonical?: string;
  robots?: string;
  structuredData?: Record<string, unknown>;
}

const DEFAULT_SEO: SEOConfig = {
  siteName: 'Maple Blog',
  locale: 'zh_CN',
  type: 'website',
  twitterCard: 'summary_large_image',
};

export function useSEO(config: SEOConfig) {
  useEffect(() => {
    const mergedConfig = { ...DEFAULT_SEO, ...config };

    // Update document title
    if (mergedConfig.title) {
      const fullTitle = mergedConfig.siteName
        ? `${mergedConfig.title} | ${mergedConfig.siteName}`
        : mergedConfig.title;
      document.title = fullTitle;
    }

    // Helper functions to manage meta tags
    const setMetaTag = (name: string, content: string | undefined, attribute = 'name') => {
      if (!content) return;

      let meta = document.querySelector(`meta[${attribute}="${name}"]`) as HTMLMetaElement;
      if (!meta) {
        meta = document.createElement('meta');
        meta.setAttribute(attribute, name);
        document.head.appendChild(meta);
      }
      meta.content = content;
    };

    const removeMetaTag = (name: string, attribute = 'name') => {
      const meta = document.querySelector(`meta[${attribute}="${name}"]`);
      if (meta) {
        meta.remove();
      }
    };

    // Basic meta tags
    setMetaTag('description', mergedConfig.description);
    setMetaTag('keywords', mergedConfig.keywords);
    setMetaTag('author', mergedConfig.author);
    setMetaTag('robots', mergedConfig.robots || 'index, follow');

    // Open Graph tags
    setMetaTag('og:title', mergedConfig.title, 'property');
    setMetaTag('og:description', mergedConfig.description, 'property');
    setMetaTag('og:image', mergedConfig.image, 'property');
    setMetaTag('og:url', mergedConfig.url, 'property');
    setMetaTag('og:type', mergedConfig.type, 'property');
    setMetaTag('og:site_name', mergedConfig.siteName, 'property');
    setMetaTag('og:locale', mergedConfig.locale, 'property');

    // Article specific Open Graph tags
    if (mergedConfig.type === 'article') {
      setMetaTag('article:published_time', mergedConfig.publishedTime, 'property');
      setMetaTag('article:modified_time', mergedConfig.modifiedTime, 'property');
      setMetaTag('article:section', mergedConfig.section, 'property');

      // Article tags
      mergedConfig.tags?.forEach((tag, _index) => {
        setMetaTag('article:tag', tag, 'property');
      });
    }

    // Twitter Card tags
    setMetaTag('twitter:card', mergedConfig.twitterCard);
    setMetaTag('twitter:title', mergedConfig.title);
    setMetaTag('twitter:description', mergedConfig.description);
    setMetaTag('twitter:image', mergedConfig.image);
    setMetaTag('twitter:site', mergedConfig.twitterSite);
    setMetaTag('twitter:creator', mergedConfig.twitterCreator);

    // Canonical URL
    if (mergedConfig.canonical || mergedConfig.url) {
      let link = document.querySelector('link[rel="canonical"]') as HTMLLinkElement;
      if (!link) {
        link = document.createElement('link');
        link.rel = 'canonical';
        document.head.appendChild(link);
      }
      link.href = mergedConfig.canonical || mergedConfig.url || '';
    }

    // Structured Data (JSON-LD)
    if (mergedConfig.structuredData) {
      let script = document.querySelector('script[type="application/ld+json"]') as HTMLScriptElement;
      if (!script) {
        script = document.createElement('script');
        script.type = 'application/ld+json';
        document.head.appendChild(script);
      }
      script.textContent = JSON.stringify(mergedConfig.structuredData);
    }

    // Cleanup function
    return () => {
      // Restore default title
      document.title = DEFAULT_SEO.siteName || 'Maple Blog';

      // Clean up meta tags that were added
      const metaTagsToRemove = [
        'description',
        'keywords',
        'author',
        'robots',
        'twitter:card',
        'twitter:title',
        'twitter:description',
        'twitter:image',
        'twitter:site',
        'twitter:creator',
      ];

      const propertyTagsToRemove = [
        'og:title',
        'og:description',
        'og:image',
        'og:url',
        'og:type',
        'og:site_name',
        'og:locale',
        'article:published_time',
        'article:modified_time',
        'article:section',
        'article:tag',
      ];

      metaTagsToRemove.forEach(tag => removeMetaTag(tag, 'name'));
      propertyTagsToRemove.forEach(tag => removeMetaTag(tag, 'property'));

      // Remove structured data
      const script = document.querySelector('script[type="application/ld+json"]');
      if (script) {
        script.remove();
      }
    };
  }, [config]);
}

// Helper function to generate structured data for blog posts
export function generateBlogPostStructuredData(post: {
  title: string;
  description: string;
  author: string;
  publishedAt: string;
  updatedAt?: string;
  image?: string;
  url: string;
  tags?: string[];
}): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'BlogPosting',
    headline: post.title,
    description: post.description,
    author: {
      '@type': 'Person',
      name: post.author,
    },
    datePublished: post.publishedAt,
    dateModified: post.updatedAt || post.publishedAt,
    image: post.image,
    url: post.url,
    keywords: post.tags?.join(', '),
    mainEntityOfPage: {
      '@type': 'WebPage',
      '@id': post.url,
    },
    publisher: {
      '@type': 'Organization',
      name: 'Maple Blog',
      logo: {
        '@type': 'ImageObject',
        url: '/logo.png',
      },
    },
  };
}

// Helper function to generate structured data for the website
export function generateWebsiteStructuredData(): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: 'Maple Blog',
    url: window.location.origin,
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: `${window.location.origin}/search?q={search_term_string}`,
      },
      'query-input': 'required name=search_term_string',
    },
  };
}

// Helper function to generate structured data for breadcrumbs
export function generateBreadcrumbStructuredData(breadcrumbs: Array<{
  name: string;
  url: string;
}>): Record<string, unknown> {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: breadcrumbs.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      item: item.url,
    })),
  };
}