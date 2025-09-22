// @ts-nocheck
/**
 * Archive API Service
 * 归档功能相关的API服务
 */

import { apiClient } from '@/services/api/client';
import {
  ArchiveRequest,
  ArchiveResponse,
  TimelineArchive,
  CalendarArchive,
  CategoryTree,
  TagCloud,
  ArchiveStatistics,
  ArchivePost,
  ApiResponse,
  PaginatedResponse,
  DateRange,
  ArchiveType,
  ArchiveGroupBy,
} from '@/types/search';

class ArchiveApiService {
  private readonly baseUrl = '/api/archive';

  /**
   * 获取通用归档数据
   */
  async getArchive(request: ArchiveRequest): Promise<ArchiveResponse> {
    try {
      const response = await apiClient.post<ApiResponse<ArchiveResponse>>(
        `${this.baseUrl}`,
        request
      );

      return response.data.data;
    } catch (error) {
      console.error('Archive request failed:', error);
      throw error;
    }
  }

  /**
   * 获取时间轴归档数据
   */
  async getTimelineArchive(
    year?: number,
    includeContent: boolean = true
  ): Promise<TimelineArchive> {
    try {
      const response = await apiClient.get<ApiResponse<TimelineArchive>>(
        `${this.baseUrl}/timeline`,
        {
          params: {
            year,
            includeContent,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Timeline archive request failed:', error);
      throw error;
    }
  }

  /**
   * 获取日历归档数据
   */
  async getCalendarArchive(
    year: number,
    month?: number
  ): Promise<CalendarArchive> {
    try {
      const response = await apiClient.get<ApiResponse<CalendarArchive>>(
        `${this.baseUrl}/calendar`,
        {
          params: {
            year,
            month,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Calendar archive request failed:', error);
      throw error;
    }
  }

  /**
   * 获取分类树结构
   */
  async getCategoryTree(
    includeEmpty: boolean = false,
    maxDepth?: number
  ): Promise<CategoryTree> {
    try {
      const response = await apiClient.get<ApiResponse<CategoryTree>>(
        `${this.baseUrl}/categories`,
        {
          params: {
            includeEmpty,
            maxDepth,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Category tree request failed:', error);
      throw error;
    }
  }

  /**
   * 获取特定分类的文章
   */
  async getCategoryPosts(
    categorySlug: string,
    page: number = 1,
    pageSize: number = 20,
    sortBy: 'publishedAt' | 'title' | 'viewCount' = 'publishedAt'
  ): Promise<PaginatedResponse<ArchivePost>> {
    try {
      const response = await apiClient.get<
        ApiResponse<PaginatedResponse<ArchivePost>>
      >(`${this.baseUrl}/categories/${categorySlug}/posts`, {
        params: {
          page,
          pageSize,
          sortBy,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error('Category posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取标签云数据
   */
  async getTagCloud(
    limit: number = 100,
    minCount: number = 1
  ): Promise<TagCloud> {
    try {
      const response = await apiClient.get<ApiResponse<TagCloud>>(
        `${this.baseUrl}/tags/cloud`,
        {
          params: {
            limit,
            minCount,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Tag cloud request failed:', error);
      throw error;
    }
  }

  /**
   * 获取特定标签的文章
   */
  async getTagPosts(
    tagSlug: string,
    page: number = 1,
    pageSize: number = 20,
    sortBy: 'publishedAt' | 'title' | 'viewCount' = 'publishedAt'
  ): Promise<PaginatedResponse<ArchivePost>> {
    try {
      const response = await apiClient.get<
        ApiResponse<PaginatedResponse<ArchivePost>>
      >(`${this.baseUrl}/tags/${tagSlug}/posts`, {
        params: {
          page,
          pageSize,
          sortBy,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error('Tag posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取年份归档统计
   */
  async getYearlyStatistics(
    year: number
  ): Promise<{
    year: number;
    totalPosts: number;
    monthlyDistribution: Array<{ month: number; count: number }>;
    categoryDistribution: Array<{ category: string; count: number }>;
    tagDistribution: Array<{ tag: string; count: number }>;
    authorDistribution: Array<{ author: string; count: number }>;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          year: number;
          totalPosts: number;
          monthlyDistribution: Array<{ month: number; count: number }>;
          categoryDistribution: Array<{ category: string; count: number }>;
          tagDistribution: Array<{ tag: string; count: number }>;
          authorDistribution: Array<{ author: string; count: number }>;
        }>
      >(`${this.baseUrl}/years/${year}/statistics`);

      return response.data.data;
    } catch (error) {
      console.error('Yearly statistics request failed:', error);
      throw error;
    }
  }

  /**
   * 获取月份归档数据
   */
  async getMonthlyPosts(
    year: number,
    month: number,
    page: number = 1,
    pageSize: number = 20
  ): Promise<PaginatedResponse<ArchivePost>> {
    try {
      const response = await apiClient.get<
        ApiResponse<PaginatedResponse<ArchivePost>>
      >(`${this.baseUrl}/years/${year}/months/${month}/posts`, {
        params: {
          page,
          pageSize,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error('Monthly posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取归档总体统计信息
   */
  async getArchiveStatistics(
    dateRange?: DateRange
  ): Promise<ArchiveStatistics> {
    try {
      const response = await apiClient.get<ApiResponse<ArchiveStatistics>>(
        `${this.baseUrl}/statistics`,
        {
          params: dateRange,
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Archive statistics request failed:', error);
      throw error;
    }
  }

  /**
   * 获取最近发布的文章
   */
  async getRecentPosts(
    limit: number = 10,
    excludeIds?: string[]
  ): Promise<ArchivePost[]> {
    try {
      const response = await apiClient.get<ApiResponse<ArchivePost[]>>(
        `${this.baseUrl}/recent`,
        {
          params: {
            limit,
            excludeIds: excludeIds?.join(','),
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Recent posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取热门文章
   */
  async getPopularPosts(
    period: 'day' | 'week' | 'month' | 'year' | 'all' = 'month',
    limit: number = 10,
    excludeIds?: string[]
  ): Promise<ArchivePost[]> {
    try {
      const response = await apiClient.get<ApiResponse<ArchivePost[]>>(
        `${this.baseUrl}/popular`,
        {
          params: {
            period,
            limit,
            excludeIds: excludeIds?.join(','),
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Popular posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取随机文章
   */
  async getRandomPosts(
    limit: number = 5,
    excludeIds?: string[],
    categorySlug?: string,
    tagSlug?: string
  ): Promise<ArchivePost[]> {
    try {
      const response = await apiClient.get<ApiResponse<ArchivePost[]>>(
        `${this.baseUrl}/random`,
        {
          params: {
            limit,
            excludeIds: excludeIds?.join(','),
            categorySlug,
            tagSlug,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Random posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取相关文章
   */
  async getRelatedPosts(
    postId: string,
    limit: number = 5,
    method: 'category' | 'tag' | 'content' | 'mixed' = 'mixed'
  ): Promise<ArchivePost[]> {
    try {
      const response = await apiClient.get<ApiResponse<ArchivePost[]>>(
        `${this.baseUrl}/posts/${postId}/related`,
        {
          params: {
            limit,
            method,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Related posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取作者的文章归档
   */
  async getAuthorPosts(
    authorId: string,
    page: number = 1,
    pageSize: number = 20,
    sortBy: 'publishedAt' | 'title' | 'viewCount' = 'publishedAt'
  ): Promise<PaginatedResponse<ArchivePost>> {
    try {
      const response = await apiClient.get<
        ApiResponse<PaginatedResponse<ArchivePost>>
      >(`${this.baseUrl}/authors/${authorId}/posts`, {
        params: {
          page,
          pageSize,
          sortBy,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error('Author posts request failed:', error);
      throw error;
    }
  }

  /**
   * 获取归档导航数据（用于侧边栏等）
   */
  async getArchiveNavigation(): Promise<{
    years: Array<{ year: number; count: number }>;
    categories: Array<{ name: string; slug: string; count: number }>;
    tags: Array<{ name: string; slug: string; count: number }>;
    authors: Array<{ name: string; id: string; count: number }>;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          years: Array<{ year: number; count: number }>;
          categories: Array<{ name: string; slug: string; count: number }>;
          tags: Array<{ name: string; slug: string; count: number }>;
          authors: Array<{ name: string; id: string; count: number }>;
        }>
      >(`${this.baseUrl}/navigation`);

      return response.data.data;
    } catch (error) {
      console.error('Archive navigation request failed:', error);
      throw error;
    }
  }

  /**
   * 搜索归档内容
   */
  async searchArchive(
    query: string,
    type?: ArchiveType,
    filters?: {
      dateFrom?: string;
      dateTo?: string;
      categories?: string[];
      tags?: string[];
    },
    page: number = 1,
    pageSize: number = 20
  ): Promise<PaginatedResponse<ArchivePost>> {
    try {
      const response = await apiClient.post<
        ApiResponse<PaginatedResponse<ArchivePost>>
      >(`${this.baseUrl}/search`, {
        query,
        type,
        filters,
        page,
        pageSize,
      });

      return response.data.data;
    } catch (error) {
      console.error('Archive search failed:', error);
      throw error;
    }
  }

  /**
   * 导出归档数据
   */
  async exportArchive(
    format: 'json' | 'csv' | 'xml',
    filters?: {
      dateFrom?: string;
      dateTo?: string;
      categories?: string[];
      tags?: string[];
      authors?: string[];
    }
  ): Promise<Blob> {
    try {
      const response = await apiClient.post<Blob>(
        `${this.baseUrl}/export`,
        {
          format,
          filters,
        },
        {
          responseType: 'blob',
        }
      );

      return response.data;
    } catch (error) {
      console.error('Archive export failed:', error);
      throw error;
    }
  }

  /**
   * 获取归档缓存状态
   */
  async getCacheStatus(): Promise<{
    cacheHitRatio: number;
    totalQueries: number;
    lastClearTime: string;
    memoryUsage: number;
    itemsInCache: number;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          cacheHitRatio: number;
          totalQueries: number;
          lastClearTime: string;
          memoryUsage: number;
          itemsInCache: number;
        }>
      >(`${this.baseUrl}/cache/status`);

      return response.data.data;
    } catch (error) {
      console.error('Cache status request failed:', error);
      throw error;
    }
  }

  /**
   * 清除归档缓存（管理员功能）
   */
  async clearCache(
    type?: 'timeline' | 'calendar' | 'categories' | 'tags' | 'all'
  ): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/cache/clear`, { type });
    } catch (error) {
      console.error('Clear cache failed:', error);
      throw error;
    }
  }

  /**
   * 获取归档性能指标
   */
  async getPerformanceMetrics(): Promise<{
    averageResponseTime: number;
    slowestQueries: Array<{ query: string; time: number }>;
    mostPopularArchives: Array<{ type: string; count: number }>;
    errorRate: number;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          averageResponseTime: number;
          slowestQueries: Array<{ query: string; time: number }>;
          mostPopularArchives: Array<{ type: string; count: number }>;
          errorRate: number;
        }>
      >(`${this.baseUrl}/metrics`);

      return response.data.data;
    } catch (error) {
      console.error('Performance metrics request failed:', error);
      throw error;
    }
  }
}

// 创建单例实例
export const archiveApi = new ArchiveApiService();

// 导出类型
export type { ArchiveApiService };