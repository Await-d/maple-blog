/**
 * SEO Provider Component
 * Provides a context for managing SEO metadata across the application
 */

import React, { createContext, useContext, useState, useEffect } from 'react';
import { useSEO } from '@/hooks/useSEO';

interface SEOContextType {
  updateSEO: (config: SEOConfig) => void;
  resetSEO: () => void;
}

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
  structuredData?: Record<string, any>;
}

const SEOContext = createContext<SEOContextType | undefined>(undefined);

interface SEOProviderProps {
  children: React.ReactNode;
  defaultConfig?: SEOConfig;
}

export function SEOProvider({ children, defaultConfig }: SEOProviderProps) {
  const [seoConfig, setSEOConfig] = useState<SEOConfig>(defaultConfig || {
    siteName: 'Maple Blog',
    locale: 'zh_CN',
    type: 'website',
    twitterCard: 'summary_large_image',
  });

  // Use the SEO hook with current config
  useSEO(seoConfig);

  const updateSEO = (config: SEOConfig) => {
    setSEOConfig(prev => ({ ...prev, ...config }));
  };

  const resetSEO = () => {
    setSEOConfig(defaultConfig || {
      siteName: 'Maple Blog',
      locale: 'zh_CN',
      type: 'website',
      twitterCard: 'summary_large_image',
    });
  };

  return (
    <SEOContext.Provider value={{ updateSEO, resetSEO }}>
      {children}
    </SEOContext.Provider>
  );
}

export function usePageSEO() {
  const context = useContext(SEOContext);
  if (!context) {
    throw new Error('usePageSEO must be used within SEOProvider');
  }
  return context;
}

// Enhanced DocumentHead component for backward compatibility
interface DocumentHeadProps {
  title?: string;
  description?: string;
  keywords?: string;
  children?: React.ReactNode;
}

export function DocumentHead({ title, description, keywords }: DocumentHeadProps) {
  const context = useContext(SEOContext);

  useEffect(() => {
    if (context) {
      context.updateSEO({ title, description, keywords });
    } else {
      // Fallback if not in provider
      useSEO({ title, description, keywords });
    }

    return () => {
      if (context) {
        context.resetSEO();
      }
    };
  }, [title, description, keywords, context]);

  return null;
}

// Export for compatibility with existing code
export const Helmet = DocumentHead;
export const HelmetProvider = SEOProvider;