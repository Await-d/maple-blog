// @ts-nocheck
/**
 * 客户端缓存工具
 * 实现浏览器端缓存策略和智能管理
 */

import { QueryClient } from '@tanstack/react-query';

// 缓存配置接口
export interface CacheConfig {
  // 默认过期时间（毫秒）
  defaultTTL: number;
  // 最大缓存大小（字节）
  maxSize: number;
  // 是否启用持久化
  enablePersistence: boolean;
  // 压缩阈值（字节）
  compressionThreshold: number;
  // 清理间隔（毫秒）
  cleanupInterval: number;
}

// 缓存条目接口
interface CacheEntry<T = unknown> {
  data: T;
  timestamp: number;
  ttl: number;
  size: number;
  compressed?: boolean;
  accessCount: number;
  lastAccessed: number;
}

// 缓存统计信息
export interface CacheStats {
  totalEntries: number;
  totalSize: number;
  hitRate: number;
  missCount: number;
  hitCount: number;
  evictionCount: number;
  compressionRatio: number;
}

// 默认配置
const DEFAULT_CONFIG: CacheConfig = {
  defaultTTL: 15 * 60 * 1000, // 15分钟
  maxSize: 50 * 1024 * 1024, // 50MB
  enablePersistence: true,
  compressionThreshold: 1024, // 1KB
  cleanupInterval: 5 * 60 * 1000 // 5分钟
};

// 缓存键前缀
const CACHE_PREFIX = 'maple_cache_';
const STATS_KEY = 'maple_cache_stats';

/**
 * 高级客户端缓存管理器
 */
export class ClientCacheManager {
  private cache = new Map<string, CacheEntry>();
  private config: CacheConfig;
  private stats: CacheStats;
  private cleanupTimer?: NodeJS.Timeout;
  private storageAvailable: boolean;

  constructor(config: Partial<CacheConfig> = {}) {
    this.config = { ...DEFAULT_CONFIG, ...config };
    this.stats = {
      totalEntries: 0,
      totalSize: 0,
      hitRate: 0,
      missCount: 0,
      hitCount: 0,
      evictionCount: 0,
      compressionRatio: 1
    };

    // 检测存储可用性
    this.storageAvailable = this.checkStorageAvailability();

    // 从持久化存储加载缓存
    if (this.config.enablePersistence) {
      this.loadFromStorage();
    }

    // 启动清理定时器
    this.startCleanupTimer();

    // 监听存储空间变化
    if (typeof window !== 'undefined') {
      window.addEventListener('storage', this.handleStorageChange.bind(this));
      window.addEventListener('beforeunload', this.saveToStorage.bind(this));
    }
  }

  /**
   * 获取缓存数据
   */
  get<T>(key: string): T | null {
    const entry = this.cache.get(key);

    if (!entry) {
      this.stats.missCount++;
      this.updateHitRate();
      return null;
    }

    // 检查是否过期
    if (this.isExpired(entry)) {
      this.cache.delete(key);
      this.stats.totalEntries--;
      this.stats.totalSize -= entry.size;
      this.stats.missCount++;
      this.updateHitRate();
      return null;
    }

    // 更新访问统计
    entry.accessCount++;
    entry.lastAccessed = Date.now();
    this.stats.hitCount++;
    this.updateHitRate();

    // 解压缩数据（如果需要）
    let data = entry.data;
    if (entry.compressed) {
      data = this.decompress(entry.data as string);
    }

    return data as T;
  }

  /**
   * 设置缓存数据
   */
  set<T>(key: string, data: T, ttl?: number): void {
    const entryTTL = ttl || this.config.defaultTTL;
    const serializedData = JSON.stringify(data);
    const dataSize = this.estimateSize(serializedData);

    // 检查是否需要压缩
    let finalData: unknown = data;
    let compressed = false;

    if (dataSize > this.config.compressionThreshold) {
      finalData = this.compress(serializedData);
      compressed = true;
    }

    const entry: CacheEntry<T> = {
      data: finalData as T,
      timestamp: Date.now(),
      ttl: entryTTL,
      size: dataSize,
      compressed,
      accessCount: 0,
      lastAccessed: Date.now()
    };

    // 检查缓存大小限制
    if (this.stats.totalSize + dataSize > this.config.maxSize) {
      this.evictEntries(dataSize);
    }

    // 更新现有条目或添加新条目
    const existingEntry = this.cache.get(key);
    if (existingEntry) {
      this.stats.totalSize -= existingEntry.size;
    } else {
      this.stats.totalEntries++;
    }

    this.cache.set(key, entry);
    this.stats.totalSize += dataSize;
  }

  /**
   * 删除缓存条目
   */
  delete(key: string): boolean {
    const entry = this.cache.get(key);
    if (entry) {
      this.cache.delete(key);
      this.stats.totalEntries--;
      this.stats.totalSize -= entry.size;
      return true;
    }
    return false;
  }

  /**
   * 清空所有缓存
   */
  clear(): void {
    this.cache.clear();
    this.stats.totalEntries = 0;
    this.stats.totalSize = 0;
    this.stats.evictionCount = 0;

    if (this.config.enablePersistence && this.storageAvailable) {
      this.clearStorage();
    }
  }

  /**
   * 获取缓存统计信息
   */
  getStats(): CacheStats {
    return { ...this.stats };
  }

  /**
   * 手动触发清理
   */
  cleanup(): void {
    const _now = Date.now();
    let cleanedCount = 0;
    let cleanedSize = 0;

    for (const [key, entry] of this.cache.entries()) {
      if (this.isExpired(entry)) {
        cleanedSize += entry.size;
        this.cache.delete(key);
        cleanedCount++;
      }
    }

    this.stats.totalEntries -= cleanedCount;
    this.stats.totalSize -= cleanedSize;
  }

  /**
   * 预加载数据到缓存
   */
  async preload(entries: Array<{ key: string; fetcher: () => Promise<unknown>; ttl?: number }>): Promise<void> {
    const promises = entries.map(async ({ key, fetcher, ttl }) => {
      try {
        if (!this.cache.has(key)) {
          const data = await fetcher();
          this.set(key, data, ttl);
        }
      } catch (error) {
        console.warn(`Failed to preload cache entry: ${key}`, error);
      }
    });

    await Promise.allSettled(promises);
  }

  /**
   * 销毁缓存管理器
   */
  destroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
    }

    if (this.config.enablePersistence) {
      this.saveToStorage();
    }

    this.cache.clear();
  }

  // 私有方法

  private isExpired(entry: CacheEntry): boolean {
    return Date.now() - entry.timestamp > entry.ttl;
  }

  private evictEntries(neededSize: number): void {
    // LRU + Size based eviction
    const entries = Array.from(this.cache.entries())
      .sort(([, a], [, b]) => {
        // 优先删除过期的条目
        const aExpired = this.isExpired(a);
        const bExpired = this.isExpired(b);

        if (aExpired && !bExpired) return -1;
        if (!aExpired && bExpired) return 1;

        // 然后按最后访问时间排序
        return a.lastAccessed - b.lastAccessed;
      });

    let freedSize = 0;
    let evictedCount = 0;

    for (const [key, entry] of entries) {
      if (freedSize >= neededSize) break;

      this.cache.delete(key);
      freedSize += entry.size;
      evictedCount++;
    }

    this.stats.totalEntries -= evictedCount;
    this.stats.totalSize -= freedSize;
    this.stats.evictionCount += evictedCount;
  }

  private updateHitRate(): void {
    const total = this.stats.hitCount + this.stats.missCount;
    this.stats.hitRate = total > 0 ? this.stats.hitCount / total : 0;
  }

  private estimateSize(data: string): number {
    return new Blob([data]).size;
  }

  private compress(data: string): string {
    // 简单的压缩实现（实际项目中可使用更好的压缩算法）
    try {
      return btoa(encodeURIComponent(data));
    } catch {
      return data;
    }
  }

  private decompress(data: string): unknown {
    try {
      const decompressed = decodeURIComponent(atob(data));
      return JSON.parse(decompressed);
    } catch {
      return data;
    }
  }

  private checkStorageAvailability(): boolean {
    try {
      const test = '__cache_test__';
      localStorage.setItem(test, test);
      localStorage.removeItem(test);
      return true;
    } catch {
      return false;
    }
  }

  private loadFromStorage(): void {
    if (!this.storageAvailable) return;

    try {
      // 加载缓存条目
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key?.startsWith(CACHE_PREFIX)) {
          const item = localStorage.getItem(key);
          if (item) {
            const entry = JSON.parse(item) as CacheEntry;
            const cacheKey = key.replace(CACHE_PREFIX, '');

            if (!this.isExpired(entry)) {
              this.cache.set(cacheKey, entry);
              this.stats.totalEntries++;
              this.stats.totalSize += entry.size;
            } else {
              localStorage.removeItem(key);
            }
          }
        }
      }

      // 加载统计信息
      const statsItem = localStorage.getItem(STATS_KEY);
      if (statsItem) {
        const storedStats = JSON.parse(statsItem) as Partial<CacheStats>;
        this.stats = { ...this.stats, ...storedStats };
      }
    } catch (error) {
      console.warn('Failed to load cache from storage:', error);
    }
  }

  private saveToStorage(): void {
    if (!this.storageAvailable) return;

    try {
      // 保存缓存条目
      for (const [key, entry] of this.cache.entries()) {
        const storageKey = `${CACHE_PREFIX}${key}`;
        localStorage.setItem(storageKey, JSON.stringify(entry));
      }

      // 保存统计信息
      localStorage.setItem(STATS_KEY, JSON.stringify(this.stats));
    } catch (error) {
      console.warn('Failed to save cache to storage:', error);
      // 如果存储空间不足，尝试清理一些条目
      this.cleanup();
    }
  }

  private clearStorage(): void {
    if (!this.storageAvailable) return;

    try {
      const keysToRemove: string[] = [];

      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key?.startsWith(CACHE_PREFIX)) {
          keysToRemove.push(key);
        }
      }

      keysToRemove.forEach(key => localStorage.removeItem(key));
      localStorage.removeItem(STATS_KEY);
    } catch (error) {
      console.warn('Failed to clear storage:', error);
    }
  }

  private startCleanupTimer(): void {
    this.cleanupTimer = setInterval(() => {
      this.cleanup();
      if (this.config.enablePersistence) {
        this.saveToStorage();
      }
    }, this.config.cleanupInterval);
  }

  private handleStorageChange(event: StorageEvent): void {
    // 处理其他标签页的存储变化
    if (event.key?.startsWith(CACHE_PREFIX)) {
      // 重新加载受影响的缓存条目
      const cacheKey = event.key.replace(CACHE_PREFIX, '');
      if (event.newValue) {
        try {
          const entry = JSON.parse(event.newValue) as CacheEntry;
          this.cache.set(cacheKey, entry);
        } catch (error) {
          console.warn('Failed to sync cache entry from storage:', error);
        }
      } else {
        this.cache.delete(cacheKey);
      }
    }
  }
}

// 创建全局缓存实例
export const clientCache = new ClientCacheManager();

/**
 * React Query 缓存配置
 */
export const queryClientConfig = {
  defaultOptions: {
    queries: {
      // 缓存时间 15 分钟
      staleTime: 15 * 60 * 1000,
      // 垃圾回收时间 30 分钟
      gcTime: 30 * 60 * 1000,
      // 重试配置
      retry: (failureCount: number, error: Error) => {
        // 客户端错误不重试
        const httpError = error as Error & { status?: number };
        if (httpError?.status >= 400 && httpError?.status < 500) {
          return false;
        }
        return failureCount < 3;
      },
      // 重试延迟
      retryDelay: (attemptIndex: number) => Math.min(1000 * 2 ** attemptIndex, 30000),
    },
    mutations: {
      retry: 1,
    },
  },
};

/**
 * 创建配置好的 QueryClient
 */
export const createQueryClient = () => new QueryClient(queryClientConfig);

/**
 * 首页特定的缓存键
 */
export const HOME_CACHE_KEYS = {
  popularPosts: (timeRange: string, sortBy: string, page: number) =>
    ['popular-posts', timeRange, sortBy, page],
  featuredPosts: () => ['featured-posts'],
  categories: () => ['categories'],
  tags: () => ['tags'],
  siteStats: () => ['site-stats'],
  personalizedPosts: (userId: string) => ['personalized-posts', userId],
  homePageData: (userId?: string) => userId ? ['home-page-data', userId] : ['home-page-data'],
} as const;

/**
 * 缓存预加载策略
 */
export class CachePreloader {
  private static instance: CachePreloader;

  private constructor(private cache: ClientCacheManager) {}

  static getInstance(cache: ClientCacheManager = clientCache): CachePreloader {
    if (!CachePreloader.instance) {
      CachePreloader.instance = new CachePreloader(cache);
    }
    return CachePreloader.instance;
  }

  /**
   * 预加载首页关键数据
   */
  async preloadHomePage(): Promise<void> {
    const criticalData = [
      {
        key: 'popular-posts:week:views:1',
        fetcher: () => this.fetchPopularPosts('week', 'views', 1),
        ttl: 30 * 60 * 1000 // 30分钟
      },
      {
        key: 'site-stats',
        fetcher: () => this.fetchSiteStats(),
        ttl: 60 * 60 * 1000 // 1小时
      },
      {
        key: 'categories',
        fetcher: () => this.fetchCategories(),
        ttl: 4 * 60 * 60 * 1000 // 4小时
      }
    ];

    await this.cache.preload(criticalData);
  }

  /**
   * 预加载用户个性化数据
   */
  async preloadUserData(userId: string): Promise<void> {
    const userData = [
      {
        key: `personalized-posts:${userId}`,
        fetcher: () => this.fetchPersonalizedPosts(userId),
        ttl: 20 * 60 * 1000 // 20分钟
      },
      {
        key: `home-page-data:${userId}`,
        fetcher: () => this.fetchUserHomeData(userId),
        ttl: 10 * 60 * 1000 // 10分钟
      }
    ];

    await this.cache.preload(userData);
  }

  // 模拟API调用方法
  private async fetchPopularPosts(timeRange: string, sortBy: string, page: number): Promise<unknown> {
    // 实际实现中应该调用真实的API
    const response = await fetch(`/api/posts/popular?timeRange=${timeRange}&sortBy=${sortBy}&page=${page}`);
    return response.json();
  }

  private async fetchSiteStats(): Promise<unknown> {
    const response = await fetch('/api/stats');
    return response.json();
  }

  private async fetchCategories(): Promise<unknown> {
    const response = await fetch('/api/categories');
    return response.json();
  }

  private async fetchPersonalizedPosts(userId: string): Promise<unknown> {
    const response = await fetch(`/api/users/${userId}/personalized-posts`);
    return response.json();
  }

  private async fetchUserHomeData(userId: string): Promise<unknown> {
    const response = await fetch(`/api/users/${userId}/home-data`);
    return response.json();
  }
}

// 导出预加载器实例
export const cachePreloader = CachePreloader.getInstance();

// 导出工具函数
export const cacheUtils = {
  /**
   * 清理过期的查询缓存
   */
  clearExpiredQueries: (queryClient: QueryClient) => {
    queryClient.getQueryCache().clear();
  },

  /**
   * 预取关键数据
   */
  prefetchCriticalData: async (queryClient: QueryClient) => {
    await Promise.allSettled([
      queryClient.prefetchQuery({
        queryKey: HOME_CACHE_KEYS.popularPosts('week', 'views', 1),
        queryFn: () => fetch('/api/posts/popular?timeRange=week&sortBy=views&page=1').then(r => r.json()),
        staleTime: 30 * 60 * 1000
      }),
      queryClient.prefetchQuery({
        queryKey: HOME_CACHE_KEYS.siteStats(),
        queryFn: () => fetch('/api/stats').then(r => r.json()),
        staleTime: 60 * 60 * 1000
      })
    ]);
  },

  /**
   * 获取缓存使用报告
   */
  getCacheReport: () => {
    const stats = clientCache.getStats();
    return {
      ...stats,
      sizeMB: (stats.totalSize / 1024 / 1024).toFixed(2),
      hitRatePercent: (stats.hitRate * 100).toFixed(1)
    };
  }
};

export default clientCache;