/**
 * Sitemap Generator Utility
 * Generates XML sitemap for SEO optimization
 */

interface SitemapEntry {
  url: string;
  lastModified?: Date;
  changeFrequency?: 'always' | 'hourly' | 'daily' | 'weekly' | 'monthly' | 'yearly' | 'never';
  priority?: number;
}

export class SitemapGenerator {
  private baseUrl: string;
  private entries: SitemapEntry[] = [];

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/$/, ''); // Remove trailing slash
  }

  addEntry(entry: SitemapEntry): void {
    this.entries.push({
      ...entry,
      url: this.normalizeUrl(entry.url),
    });
  }

  addEntries(entries: SitemapEntry[]): void {
    entries.forEach(entry => this.addEntry(entry));
  }

  private normalizeUrl(url: string): string {
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    return `${this.baseUrl}${url.startsWith('/') ? url : '/' + url}`;
  }

  generate(): string {
    const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${this.entries
  .map(entry => this.generateUrlElement(entry))
  .join('\n')}
</urlset>`;

    return xml;
  }

  private generateUrlElement(entry: SitemapEntry): string {
    const elements = ['  <url>', `    <loc>${this.escapeXml(entry.url)}</loc>`];

    if (entry.lastModified) {
      elements.push(`    <lastmod>${entry.lastModified.toISOString()}</lastmod>`);
    }

    if (entry.changeFrequency) {
      elements.push(`    <changefreq>${entry.changeFrequency}</changefreq>`);
    }

    if (entry.priority !== undefined) {
      elements.push(`    <priority>${entry.priority.toFixed(1)}</priority>`);
    }

    elements.push('  </url>');
    return elements.join('\n');
  }

  private escapeXml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  }

  generateRobotsTxt(sitemapUrl?: string): string {
    const lines = [
      'User-agent: *',
      'Allow: /',
      'Disallow: /admin',
      'Disallow: /api/',
      'Disallow: /auth/',
      '',
    ];

    if (sitemapUrl) {
      lines.push(`Sitemap: ${sitemapUrl}`);
    }

    return lines.join('\n');
  }
}

// Helper function to generate sitemap for blog
export async function generateBlogSitemap(
  baseUrl: string,
  posts: Array<{
    slug: string;
    updatedAt: string;
    priority?: number;
  }>,
  categories?: Array<{
    slug: string;
    updatedAt?: string;
  }>,
  tags?: Array<{
    slug: string;
    updatedAt?: string;
  }>
): Promise<string> {
  const generator = new SitemapGenerator(baseUrl);

  // Add static pages
  generator.addEntries([
    {
      url: '/',
      changeFrequency: 'daily',
      priority: 1.0,
      lastModified: new Date(),
    },
    {
      url: '/blog',
      changeFrequency: 'daily',
      priority: 0.9,
      lastModified: new Date(),
    },
    {
      url: '/archive',
      changeFrequency: 'weekly',
      priority: 0.7,
      lastModified: new Date(),
    },
    {
      url: '/about',
      changeFrequency: 'monthly',
      priority: 0.6,
      lastModified: new Date(),
    },
  ]);

  // Add blog posts
  posts.forEach(post => {
    generator.addEntry({
      url: `/blog/${post.slug}`,
      lastModified: new Date(post.updatedAt),
      changeFrequency: 'weekly',
      priority: post.priority || 0.8,
    });
  });

  // Add category pages
  categories?.forEach(category => {
    generator.addEntry({
      url: `/category/${category.slug}`,
      lastModified: category.updatedAt ? new Date(category.updatedAt) : new Date(),
      changeFrequency: 'weekly',
      priority: 0.6,
    });
  });

  // Add tag pages
  tags?.forEach(tag => {
    generator.addEntry({
      url: `/tag/${tag.slug}`,
      lastModified: tag.updatedAt ? new Date(tag.updatedAt) : new Date(),
      changeFrequency: 'weekly',
      priority: 0.5,
    });
  });

  return generator.generate();
}

// Helper function to generate RSS feed
export function generateRSSFeed(
  baseUrl: string,
  title: string,
  description: string,
  posts: Array<{
    title: string;
    description: string;
    url: string;
    author: string;
    publishedAt: string;
    content?: string;
    categories?: string[];
  }>
): string {
  const escapeXml = (text: string): string => {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  };

  const rss = `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom" xmlns:content="http://purl.org/rss/1.0/modules/content/">
  <channel>
    <title>${escapeXml(title)}</title>
    <link>${baseUrl}</link>
    <description>${escapeXml(description)}</description>
    <language>zh-CN</language>
    <lastBuildDate>${new Date().toUTCString()}</lastBuildDate>
    <atom:link href="${baseUrl}/rss.xml" rel="self" type="application/rss+xml"/>
${posts
  .map(post => `    <item>
      <title>${escapeXml(post.title)}</title>
      <link>${baseUrl}${post.url}</link>
      <guid isPermaLink="true">${baseUrl}${post.url}</guid>
      <description>${escapeXml(post.description)}</description>
      ${post.content ? `<content:encoded><![CDATA[${post.content}]]></content:encoded>` : ''}
      <author>${escapeXml(post.author)}</author>
      <pubDate>${new Date(post.publishedAt).toUTCString()}</pubDate>
      ${post.categories ? post.categories.map(cat => `<category>${escapeXml(cat)}</category>`).join('\n      ') : ''}
    </item>`)
  .join('\n')}
  </channel>
</rss>`;

  return rss;
}