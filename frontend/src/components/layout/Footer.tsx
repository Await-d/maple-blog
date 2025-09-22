// @ts-nocheck
/**
 * Footer component - Site footer with links, social media, and site information
 * Features: Multi-column layout, responsive design, social links, newsletter signup
 */

import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Mail,
  Github,
  Twitter,
  Linkedin,
  Rss,
  Heart,
  ArrowUp,
  MapPin,
  Phone,
  Send,
  ExternalLink,
} from 'lucide-react';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { useSiteStats } from '../../services/home/homeApi';
import { cn } from '../../utils/cn';

interface FooterProps {
  className?: string;
  minimal?: boolean;
}

interface FooterLink {
  label: string;
  href: string;
  external?: boolean;
}

interface FooterSection {
  title: string;
  links: FooterLink[];
}

interface SocialLink {
  name: string;
  href: string;
  icon: React.ReactNode;
  color: string;
}

export const Footer: React.FC<FooterProps> = ({
  className,
  minimal = false,
}) => {
  const { data: siteStats } = useSiteStats();
  const [newsletterEmail, setNewsletterEmail] = useState('');
  const [isSubscribing, setIsSubscribing] = useState(false);
  const [subscribeSuccess, setSubscribeSuccess] = useState(false);

  // Footer navigation sections
  const footerSections: FooterSection[] = [
    {
      title: '内容导航',
      links: [
        { label: '最新文章', href: '/latest' },
        { label: '热门文章', href: '/popular' },
        { label: '文章归档', href: '/archive' },
        { label: '分类浏览', href: '/categories' },
        { label: '标签云', href: '/tags' },
      ],
    },
    {
      title: '网站信息',
      links: [
        { label: '关于我们', href: '/about' },
        { label: '联系我们', href: '/contact' },
        { label: '加入我们', href: '/join' },
        { label: '友情链接', href: '/links' },
        { label: '网站地图', href: '/sitemap', external: true },
      ],
    },
    {
      title: '帮助支持',
      links: [
        { label: '使用帮助', href: '/help' },
        { label: '隐私政策', href: '/privacy' },
        { label: '服务条款', href: '/terms' },
        { label: 'RSS订阅', href: '/rss.xml', external: true },
        { label: 'API文档', href: '/api-docs', external: true },
      ],
    },
  ];

  // Social media links
  const socialLinks: SocialLink[] = [
    {
      name: 'GitHub',
      href: 'https://github.com/maple-blog',
      icon: <Github size={20} />,
      color: 'hover:text-gray-900 dark:hover:text-white',
    },
    {
      name: 'Twitter',
      href: 'https://twitter.com/maple-blog',
      icon: <Twitter size={20} />,
      color: 'hover:text-blue-500',
    },
    {
      name: 'LinkedIn',
      href: 'https://linkedin.com/company/maple-blog',
      icon: <Linkedin size={20} />,
      color: 'hover:text-blue-600',
    },
    {
      name: 'RSS',
      href: '/rss.xml',
      icon: <Rss size={20} />,
      color: 'hover:text-orange-500',
    },
    {
      name: 'Email',
      href: 'mailto:contact@maple-blog.com',
      icon: <Mail size={20} />,
      color: 'hover:text-green-500',
    },
  ];

  const handleNewsletterSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newsletterEmail.trim()) return;

    setIsSubscribing(true);
    try {
      // TODO: Implement newsletter subscription API
      await new Promise(resolve => setTimeout(resolve, 1000)); // Mock API call
      setSubscribeSuccess(true);
      setNewsletterEmail('');
      setTimeout(() => setSubscribeSuccess(false), 3000);
    } catch (error) {
      console.error('Newsletter subscription failed:', error);
    } finally {
      setIsSubscribing(false);
    }
  };

  const scrollToTop = () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  if (minimal) {
    return (
      <footer className={cn('border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900', className)}>
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex flex-col sm:flex-row items-center justify-between space-y-4 sm:space-y-0">
            <div className="flex items-center space-x-2 text-sm text-gray-600 dark:text-gray-400">
              <span>© 2024 Maple Blog</span>
              <span>•</span>
              <span>由</span>
              <Heart size={14} className="text-red-500 mx-1" />
              <span>驱动</span>
            </div>
            <div className="flex items-center space-x-4">
              {socialLinks.slice(0, 3).map((social) => (
                <a
                  key={social.name}
                  href={social.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={cn(
                    'text-gray-500 dark:text-gray-400 transition-colors',
                    social.color
                  )}
                  aria-label={social.name}
                >
                  {social.icon}
                </a>
              ))}
            </div>
          </div>
        </div>
      </footer>
    );
  }

  return (
    <footer className={cn('border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900', className)}>
      {/* Main Footer Content */}
      <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
          {/* Brand and Description */}
          <div className="lg:col-span-1">
            <div className="flex items-center space-x-2 mb-4">
              <div className="w-8 h-8 bg-gradient-to-br from-orange-500 to-red-600 rounded-lg flex items-center justify-center text-white font-bold text-sm">
                M
              </div>
              <span className="text-xl font-bold text-gray-900 dark:text-white">
                Maple Blog
              </span>
            </div>
            <p className="text-gray-600 dark:text-gray-400 text-sm mb-6 leading-relaxed">
              分享技术见解，记录成长历程。Maple Blog 致力于为开发者提供高质量的技术内容和交流平台。
            </p>

            {/* Site Statistics */}
            {siteStats && (
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <div className="font-semibold text-gray-900 dark:text-white">
                    {siteStats.totalPosts.toLocaleString()}
                  </div>
                  <div className="text-gray-500 dark:text-gray-400">文章</div>
                </div>
                <div>
                  <div className="font-semibold text-gray-900 dark:text-white">
                    {siteStats.totalViews.toLocaleString()}
                  </div>
                  <div className="text-gray-500 dark:text-gray-400">访问</div>
                </div>
                <div>
                  <div className="font-semibold text-gray-900 dark:text-white">
                    {siteStats.totalAuthors.toLocaleString()}
                  </div>
                  <div className="text-gray-500 dark:text-gray-400">作者</div>
                </div>
                <div>
                  <div className="font-semibold text-gray-900 dark:text-white">
                    {siteStats.totalCategories.toLocaleString()}
                  </div>
                  <div className="text-gray-500 dark:text-gray-400">分类</div>
                </div>
              </div>
            )}
          </div>

          {/* Navigation Sections */}
          {footerSections.map((section) => (
            <div key={section.title}>
              <h3 className="font-semibold text-gray-900 dark:text-white mb-4">
                {section.title}
              </h3>
              <ul className="space-y-3">
                {section.links.map((link) => (
                  <li key={link.href}>
                    {link.external ? (
                      <a
                        href={link.href}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-sm text-gray-600 dark:text-gray-400 hover:text-orange-600 dark:hover:text-orange-400 transition-colors flex items-center space-x-1"
                      >
                        <span>{link.label}</span>
                        <ExternalLink size={12} />
                      </a>
                    ) : (
                      <Link
                        to={link.href}
                        className="text-sm text-gray-600 dark:text-gray-400 hover:text-orange-600 dark:hover:text-orange-400 transition-colors"
                      >
                        {link.label}
                      </Link>
                    )}
                  </li>
                ))}
              </ul>
            </div>
          ))}

          {/* Newsletter Signup */}
          <div>
            <h3 className="font-semibold text-gray-900 dark:text-white mb-4">
              订阅更新
            </h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
              订阅我们的邮件列表，获取最新文章和网站更新。
            </p>

            <form onSubmit={handleNewsletterSubmit} className="space-y-3">
              <Input
                type="email"
                value={newsletterEmail}
                onChange={(e) => setNewsletterEmail(e.target.value)}
                placeholder="请输入您的邮箱"
                inputSize="sm"
                className="w-full"
                disabled={isSubscribing}
              />
              <Button
                type="submit"
                size="sm"
                className="w-full"
                disabled={isSubscribing || !newsletterEmail.trim()}
              >
                {isSubscribing ? (
                  <div className="flex items-center space-x-2">
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                    <span>订阅中...</span>
                  </div>
                ) : (
                  <div className="flex items-center space-x-2">
                    <Send size={14} />
                    <span>订阅</span>
                  </div>
                )}
              </Button>
            </form>

            {subscribeSuccess && (
              <div className="mt-3 p-3 bg-green-100 dark:bg-green-900/20 text-green-700 dark:text-green-400 text-sm rounded-lg">
                订阅成功！感谢您的关注。
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Bottom Bar */}
      <div className="border-t border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-800">
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex flex-col sm:flex-row items-center justify-between space-y-4 sm:space-y-0">
            {/* Copyright */}
            <div className="flex items-center space-x-4 text-sm text-gray-600 dark:text-gray-400">
              <span>© 2024 Maple Blog. All rights reserved.</span>
              <span className="hidden sm:inline">•</span>
              <span className="hidden sm:inline flex items-center space-x-1">
                <span>Made with</span>
                <Heart size={14} className="text-red-500 mx-1" />
                <span>in China</span>
              </span>
            </div>

            {/* Social Links and Back to Top */}
            <div className="flex items-center space-x-4">
              {socialLinks.map((social) => (
                <a
                  key={social.name}
                  href={social.href}
                  target={social.href.startsWith('http') ? '_blank' : undefined}
                  rel={social.href.startsWith('http') ? 'noopener noreferrer' : undefined}
                  className={cn(
                    'text-gray-500 dark:text-gray-400 transition-colors',
                    social.color
                  )}
                  aria-label={social.name}
                  title={social.name}
                >
                  {social.icon}
                </a>
              ))}

              {/* Back to Top Button */}
              <Button
                variant="ghost"
                size="sm"
                onClick={scrollToTop}
                className="p-2"
                aria-label="回到顶部"
                title="回到顶部"
              >
                <ArrowUp size={16} />
              </Button>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
};

/**
 * Usage:
 * <Footer /> - Full footer with all sections
 * <Footer minimal /> - Simplified footer for certain pages
 *
 * Features:
 * - Multi-column responsive layout
 * - Site statistics integration
 * - Newsletter subscription form
 * - Social media links with hover effects
 * - Comprehensive navigation links
 * - Back to top functionality
 * - External link indicators
 * - Dark theme support
 * - Mobile-friendly responsive design
 * - Accessibility support with proper ARIA labels
 */