// @ts-nocheck
/**
 * SEO优化辅助工具
 * 提供meta标签生成、结构化数据创建、SEO分析等功能
 */

import type { SEOData } from '../components/home/SEOOptimization';

// Meta标签接口
export interface MetaTag {
  name?: string;
  property?: string;
  content: string;
  httpEquiv?: string;
}

// 结构化数据类型
export type StructuredDataType =
  | 'WebSite'
  | 'Organization'
  | 'Person'
  | 'Article'
  | 'BlogPosting'
  | 'BreadcrumbList'
  | 'FAQPage'
  | 'Product'
  | 'Review';

// SEO配置选项
export interface SEOConfig {
  siteName: string;
  siteUrl: string;
  defaultImage: string;
  twitterHandle?: string;
  facebookAppId?: string;
  googleSiteVerification?: string;
  bingVerification?: string;
  yandexVerification?: string;
}

// 默认SEO配置
const defaultSEOConfig: SEOConfig = {
  siteName: 'Maple Blog',
  siteUrl: 'https://maple-blog.com',
  defaultImage: '/og-image.png',
  twitterHandle: '@maple_blog'
};

let seoConfig: SEOConfig = defaultSEOConfig;

/**
 * 设置全局SEO配置
 */
export const setSEOConfig = (config: Partial<SEOConfig>) => {
  seoConfig = { ...seoConfig, ...config };
};

/**
 * 获取当前SEO配置
 */
export const getSEOConfig = () => seoConfig;

/**
 * 生成基础meta标签
 */
export const generateMetaTags = (data: SEOData): MetaTag[] => {
  const tags: MetaTag[] = [];

  // 基础标签
  if (data.description) {
    tags.push({ name: 'description', content: data.description });
  }

  if (data.keywords && data.keywords.length > 0) {
    tags.push({ name: 'keywords', content: data.keywords.join(', ') });
  }

  if (data.author) {
    tags.push({ name: 'author', content: data.author });
  }

  if (data.robots) {
    tags.push({ name: 'robots', content: data.robots });
  }

  // 语言标签
  if (data.language) {
    tags.push({ httpEquiv: 'Content-Language', content: data.language });
  }

  // 网站验证标签
  if (seoConfig.googleSiteVerification) {
    tags.push({ name: 'google-site-verification', content: seoConfig.googleSiteVerification });
  }

  if (seoConfig.bingVerification) {
    tags.push({ name: 'msvalidate.01', content: seoConfig.bingVerification });
  }

  if (seoConfig.yandexVerification) {
    tags.push({ name: 'yandex-verification', content: seoConfig.yandexVerification });
  }

  // 移动设备优化
  tags.push(
    { name: 'viewport', content: 'width=device-width, initial-scale=1' },
    { name: 'format-detection', content: 'telephone=no' },
    { name: 'mobile-web-app-capable', content: 'yes' },
    { name: 'apple-mobile-web-app-capable', content: 'yes' },
    { name: 'apple-mobile-web-app-status-bar-style', content: 'default' }
  );

  return tags;
};

/**
 * 生成Open Graph标签
 */
export const generateOpenGraphTags = (data: SEOData): MetaTag[] => {
  const tags: MetaTag[] = [];

  // 基础OG标签
  tags.push(
    { property: 'og:title', content: data.ogTitle || data.title },
    { property: 'og:description', content: data.ogDescription || data.description },
    { property: 'og:type', content: data.ogType || 'website' },
    { property: 'og:url', content: data.canonical || data.siteUrl || seoConfig.siteUrl },
    { property: 'og:site_name', content: data.siteName || seoConfig.siteName }
  );

  // 图片标签
  const ogImage = data.ogImage || data.siteUrl + seoConfig.defaultImage;
  if (ogImage) {
    tags.push(
      { property: 'og:image', content: ogImage },
      { property: 'og:image:width', content: '1200' },
      { property: 'og:image:height', content: '630' },
      { property: 'og:image:type', content: 'image/png' }
    );

    if (data.ogImageAlt) {
      tags.push({ property: 'og:image:alt', content: data.ogImageAlt });
    }
  }

  // 本地化
  if (data.ogLocale) {
    tags.push({ property: 'og:locale', content: data.ogLocale });
  }

  // 文章特定标签
  if (data.ogType === 'article') {
    if (data.publishedTime) {
      tags.push({ property: 'article:published_time', content: data.publishedTime });
    }

    if (data.modifiedTime) {
      tags.push({ property: 'article:modified_time', content: data.modifiedTime });
    }

    if (data.author) {
      tags.push({ property: 'article:author', content: data.author });
    }

    if (data.section) {
      tags.push({ property: 'article:section', content: data.section });
    }

    if (data.tags) {
      data.tags.forEach(tag => {
        tags.push({ property: 'article:tag', content: tag });
      });
    }
  }

  // Facebook App ID
  if (seoConfig.facebookAppId) {
    tags.push({ property: 'fb:app_id', content: seoConfig.facebookAppId });
  }

  return tags;
};

/**
 * 生成Twitter Cards标签
 */
export const generateTwitterCards = (data: SEOData): MetaTag[] => {
  const tags: MetaTag[] = [];

  // 基础Twitter标签
  tags.push(
    { name: 'twitter:card', content: data.twitterCard || 'summary_large_image' },
    { name: 'twitter:title', content: data.ogTitle || data.title },
    { name: 'twitter:description', content: data.ogDescription || data.description }
  );

  // Twitter账号
  const twitterSiteHandle = data.twitterSite || seoConfig.twitterHandle;
  if (twitterSiteHandle) {
    tags.push({ name: 'twitter:site', content: twitterSiteHandle });
  }

  if (data.twitterCreator) {
    tags.push({ name: 'twitter:creator', content: data.twitterCreator });
  }

  // 图片
  const twitterImage = data.twitterImage || data.ogImage || data.siteUrl + seoConfig.defaultImage;
  if (twitterImage) {
    tags.push({ name: 'twitter:image', content: twitterImage });

    const imageAlt = data.twitterImageAlt || data.ogImageAlt;
    if (imageAlt) {
      tags.push({ name: 'twitter:image:alt', content: imageAlt });
    }
  }

  return tags;
};

/**
 * 生成结构化数据
 */
export const generateStructuredData = (
  type: StructuredDataType,
  data: Record<string, unknown>
): Record<string, unknown> => {
  const baseStructuredData = {
    '@context': 'https://schema.org',
    '@type': type,
    ...data
  };

  // 根据类型添加特定字段
  switch (type) {
    case 'WebSite':
      return {
        ...baseStructuredData,
        url: data.url || seoConfig.siteUrl,
        name: data.name || seoConfig.siteName,
        potentialAction: {
          '@type': 'SearchAction',
          target: {
            '@type': 'EntryPoint',
            urlTemplate: `${data.url || seoConfig.siteUrl}/search?q={search_term_string}`
          },
          'query-input': 'required name=search_term_string'
        }
      };

    case 'Organization':
      return {
        ...baseStructuredData,
        name: data.name || seoConfig.siteName,
        url: data.url || seoConfig.siteUrl,
        logo: data.logo || seoConfig.siteUrl + seoConfig.defaultImage
      };

    case 'Article':
    case 'BlogPosting':
      return {
        ...baseStructuredData,
        publisher: {
          '@type': 'Organization',
          name: seoConfig.siteName,
          logo: {
            '@type': 'ImageObject',
            url: seoConfig.siteUrl + seoConfig.defaultImage
          }
        },
        mainEntityOfPage: {
          '@type': 'WebPage',
          '@id': data.url
        }
      };

    case 'BreadcrumbList':
      return {
        ...baseStructuredData,
        itemListElement: (data.items as any[] || []).map((item: Record<string, unknown>, index: number) => ({
          '@type': 'ListItem',
          position: index + 1,
          name: item.name,
          item: item.url
        }))
      };

    default:
      return baseStructuredData;
  }
};

/**
 * 验证SEO数据完整性
 */
export const validateSEOData = (data: SEOData): {
  isValid: boolean;
  warnings: string[];
  errors: string[]
} => {
  const warnings: string[] = [];
  const errors: string[] = [];

  // 必需字段检查
  if (!data.title) {
    errors.push('标题是必需的');
  } else if (data.title.length > 60) {
    warnings.push('标题长度超过60个字符，可能在搜索结果中被截断');
  }

  if (!data.description) {
    errors.push('描述是必需的');
  } else if (data.description.length > 160) {
    warnings.push('描述长度超过160个字符，可能在搜索结果中被截断');
  }

  // 图片检查
  if (data.ogImage) {
    if (!data.ogImageAlt) {
      warnings.push('建议为Open Graph图片添加alt文本');
    }
  } else {
    warnings.push('建议添加Open Graph图片以提高社交媒体分享效果');
  }

  // 关键词检查
  if (!data.keywords || data.keywords.length === 0) {
    warnings.push('建议添加关键词以提高SEO效果');
  } else if (data.keywords.length > 10) {
    warnings.push('关键词数量较多，建议控制在10个以内');
  }

  // 链接检查
  if (data.canonical && !isValidURL(data.canonical)) {
    errors.push('canonical URL格式不正确');
  }

  return {
    isValid: errors.length === 0,
    warnings,
    errors
  };
};

/**
 * URL有效性检查
 */
const isValidURL = (url: string): boolean => {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
};

/**
 * 生成sitemap条目
 */
export const generateSitemapEntry = (
  url: string,
  lastmod?: string,
  changefreq?: 'always' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'yearly' | 'never',
  priority?: number
) => ({
  url: url.startsWith('http') ? url : `${seoConfig.siteUrl}${url}`,
  lastmod: lastmod || new Date().toISOString().split('T')[0],
  changefreq: changefreq || 'weekly',
  priority: priority || 0.5
});

/**
 * 生成robots.txt内容
 */
export const generateRobotsTxt = (
  customRules: string[] = [],
  sitemap?: string
): string => {
  const rules = [
    'User-agent: *',
    'Allow: /',
    'Disallow: /admin/',
    'Disallow: /api/',
    'Disallow: /private/',
    'Disallow: /*?*utm_*',
    'Disallow: /*?*session*',
    ...customRules
  ];

  if (sitemap) {
    rules.push('', `Sitemap: ${sitemap}`);
  } else {
    rules.push('', `Sitemap: ${seoConfig.siteUrl}/sitemap.xml`);
  }

  return rules.join('\n');
};

/**
 * 分析页面SEO得分
 */
export const analyzeSEOScore = (data: SEOData): {
  score: number;
  factors: Array<{ factor: string; score: number; weight: number; suggestion?: string }>;
} => {
  const factors = [
    {
      factor: '页面标题',
      score: data.title ? (data.title.length <= 60 ? 100 : 70) : 0,
      weight: 20,
      suggestion: !data.title
        ? '添加页面标题'
        : data.title.length > 60
        ? '标题长度控制在60字符以内'
        : undefined
    },
    {
      factor: '页面描述',
      score: data.description ? (data.description.length <= 160 ? 100 : 70) : 0,
      weight: 15,
      suggestion: !data.description
        ? '添加页面描述'
        : data.description.length > 160
        ? '描述长度控制在160字符以内'
        : undefined
    },
    {
      factor: '关键词',
      score: data.keywords && data.keywords.length > 0 ?
        (data.keywords.length <= 10 ? 100 : 80) : 50,
      weight: 10,
      suggestion: !data.keywords || data.keywords.length === 0
        ? '添加相关关键词'
        : data.keywords.length > 10
        ? '关键词数量控制在10个以内'
        : undefined
    },
    {
      factor: 'Open Graph',
      score: data.ogTitle && data.ogDescription ?
        (data.ogImage ? 100 : 80) : 40,
      weight: 15,
      suggestion: !data.ogTitle || !data.ogDescription
        ? '完善Open Graph标签'
        : !data.ogImage
        ? '添加Open Graph图片'
        : undefined
    },
    {
      factor: 'Twitter Cards',
      score: data.twitterCard ? 100 : 60,
      weight: 10,
      suggestion: !data.twitterCard ? '添加Twitter Cards支持' : undefined
    },
    {
      factor: 'Canonical URL',
      score: data.canonical ? 100 : 70,
      weight: 10,
      suggestion: !data.canonical ? '添加canonical URL防止重复内容' : undefined
    },
    {
      factor: '结构化数据',
      score: data.structuredData && data.structuredData.length > 0 ? 100 : 60,
      weight: 15,
      suggestion: !data.structuredData || data.structuredData.length === 0
        ? '添加结构化数据'
        : undefined
    },
    {
      factor: '图片Alt文本',
      score: data.ogImageAlt ? 100 : 80,
      weight: 5,
      suggestion: !data.ogImageAlt ? '为图片添加Alt文本' : undefined
    }
  ];

  const totalScore = factors.reduce((sum, factor) => {
    return sum + (factor.score * factor.weight / 100);
  }, 0);

  return {
    score: Math.round(totalScore),
    factors: factors.filter(f => f.suggestion) // 只返回有建议的因子
  };
};

/**
 * 生成面包屑导航结构化数据
 */
export const generateBreadcrumbStructuredData = (
  breadcrumbs: Array<{ name: string; url: string }>
) => {
  return generateStructuredData('BreadcrumbList', {
    items: breadcrumbs
  });
};

/**
 * 优化图片SEO
 */
export const optimizeImageSEO = (
  src: string,
  alt: string,
  title?: string,
  caption?: string
) => ({
  src,
  alt,
  title,
  caption,
  loading: 'lazy' as const,
  decoding: 'async' as const,
  // 生成结构化数据
  structuredData: {
    '@context': 'https://schema.org',
    '@type': 'ImageObject',
    url: src,
    description: alt,
    caption: caption
  }
});

export default {
  setSEOConfig,
  getSEOConfig,
  generateMetaTags,
  generateOpenGraphTags,
  generateTwitterCards,
  generateStructuredData,
  validateSEOData,
  generateSitemapEntry,
  generateRobotsTxt,
  analyzeSEOScore,
  generateBreadcrumbStructuredData,
  optimizeImageSEO
};