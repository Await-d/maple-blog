// @ts-nocheck
/**
 * Environment variables type definitions for Vite
 * This file provides TypeScript support for import.meta.env
 */

interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_APP_NAME: string;
  readonly VITE_APP_VERSION: string;
  readonly VITE_NODE_ENV: 'development' | 'production' | 'test';
  readonly VITE_ENABLE_ANALYTICS: string;
  readonly VITE_ANALYTICS_ID?: string;
  readonly VITE_SENTRY_DSN?: string;
  readonly VITE_REDIS_URL?: string;
  readonly VITE_WEBSOCKET_URL?: string;
  readonly VITE_CDN_URL?: string;
  readonly VITE_UPLOAD_MAX_SIZE?: string;
  readonly VITE_ENABLE_PWA: string;
  readonly VITE_ENABLE_OFFLINE: string;
  readonly VITE_DEFAULT_LOCALE: string;
  readonly VITE_SUPPORTED_LOCALES: string;
  readonly VITE_ENABLE_DEBUG: string;
  readonly VITE_API_TIMEOUT?: string;
  readonly VITE_PAGINATION_PAGE_SIZE?: string;
  readonly VITE_SEARCH_DEBOUNCE_MS?: string;
  // Add more environment variables as needed
}

interface _ImportMeta {
  readonly env: ImportMetaEnv;
  readonly hot?: {
    readonly accept: (dep: string, cb?: () => void) => void;
    readonly dispose: (cb: () => void) => void;
    readonly decline: () => void;
    readonly invalidate: () => void;
  };
}

// Extend global process.env for Node.js compatibility
declare namespace _NodeJS {
  interface ProcessEnv extends ImportMetaEnv {
    NODE_ENV: 'development' | 'production' | 'test';
  }
}

// Export environment configuration with defaults
export const ENV_CONFIG = {
  API_URL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  APP_NAME: import.meta.env.VITE_APP_NAME || 'Maple Blog',
  APP_VERSION: import.meta.env.VITE_APP_VERSION || '1.0.0',
  NODE_ENV: import.meta.env.VITE_NODE_ENV || 'development',
  ENABLE_ANALYTICS: import.meta.env.VITE_ENABLE_ANALYTICS === 'true',
  ANALYTICS_ID: import.meta.env.VITE_ANALYTICS_ID,
  SENTRY_DSN: import.meta.env.VITE_SENTRY_DSN,
  REDIS_URL: import.meta.env.VITE_REDIS_URL,
  WEBSOCKET_URL: import.meta.env.VITE_WEBSOCKET_URL || 'ws://localhost:5000',
  CDN_URL: import.meta.env.VITE_CDN_URL,
  UPLOAD_MAX_SIZE: Number(import.meta.env.VITE_UPLOAD_MAX_SIZE) || 10485760, // 10MB
  ENABLE_PWA: import.meta.env.VITE_ENABLE_PWA === 'true',
  ENABLE_OFFLINE: import.meta.env.VITE_ENABLE_OFFLINE === 'true',
  DEFAULT_LOCALE: import.meta.env.VITE_DEFAULT_LOCALE || 'zh-CN',
  SUPPORTED_LOCALES: import.meta.env.VITE_SUPPORTED_LOCALES?.split(',') || ['zh-CN', 'en-US'],
  ENABLE_DEBUG: import.meta.env.VITE_ENABLE_DEBUG === 'true' || import.meta.env.VITE_NODE_ENV === 'development',
  API_TIMEOUT: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  PAGINATION_PAGE_SIZE: Number(import.meta.env.VITE_PAGINATION_PAGE_SIZE) || 20,
  SEARCH_DEBOUNCE_MS: Number(import.meta.env.VITE_SEARCH_DEBOUNCE_MS) || 300,
} as const;

// Type-safe environment helper functions
export const isProduction = () => ENV_CONFIG.NODE_ENV === 'production';
export const isDevelopment = () => ENV_CONFIG.NODE_ENV === 'development';
export const isTest = () => ENV_CONFIG.NODE_ENV === 'test';

// Environment validation
export const validateEnvironment = () => {
  const required = ['VITE_API_URL'] as const;
  const missing = required.filter(key => !import.meta.env[key]);

  if (missing.length > 0) {
    throw new Error(`Missing required environment variables: ${missing.join(', ')}`);
  }
};