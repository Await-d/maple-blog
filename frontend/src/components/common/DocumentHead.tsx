import { useEffect } from 'react';

interface DocumentHeadProps {
  title?: string;
  description?: string;
  keywords?: string;
  ogTitle?: string;
  ogDescription?: string;
  ogImage?: string;
  ogUrl?: string;
  children?: React.ReactNode;
}

export function DocumentHead({
  title,
  description,
  keywords,
  ogTitle,
  ogDescription,
  ogImage,
  ogUrl,
}: DocumentHeadProps) {
  useEffect(() => {
    if (title) {
      document.title = title;
    }

    // Update meta tags
    const updateMetaTag = (name: string, content: string | undefined) => {
      if (!content) return;

      let meta = document.querySelector(`meta[name="${name}"]`) as HTMLMetaElement;
      if (!meta) {
        meta = document.createElement('meta');
        meta.name = name;
        document.head.appendChild(meta);
      }
      meta.content = content;
    };

    const updatePropertyTag = (property: string, content: string | undefined) => {
      if (!content) return;

      let meta = document.querySelector(`meta[property="${property}"]`) as HTMLMetaElement;
      if (!meta) {
        meta = document.createElement('meta');
        meta.setAttribute('property', property);
        document.head.appendChild(meta);
      }
      meta.content = content;
    };

    updateMetaTag('description', description);
    updateMetaTag('keywords', keywords);
    updatePropertyTag('og:title', ogTitle || title);
    updatePropertyTag('og:description', ogDescription || description);
    updatePropertyTag('og:image', ogImage);
    updatePropertyTag('og:url', ogUrl);

    // Cleanup function to restore original title
    return () => {
      if (title) {
        document.title = 'Maple Blog';
      }
    };
  }, [title, description, keywords, ogTitle, ogDescription, ogImage, ogUrl]);

  return null;
}

// Compatibility exports for react-helmet-async replacement
export const Helmet = DocumentHead;
export const HelmetProvider = ({ children }: { children: React.ReactNode }) => <>{children}</>;