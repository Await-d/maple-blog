// @ts-nocheck
/**
 * Search API Service
 * 搜索功能相关的API服务
 */

import { apiClient } from '@/services/api/client';
import {
  SearchRequest,
  SearchResponse,
  AutoCompleteSuggestion,
  SearchHistory,
  SearchAnalytics,
  ApiResponse,
  PaginatedResponse,
} from '@/types/search';

class SearchApiService {
  private readonly baseUrl = '/api/search';

  /**
   * 执行搜索
   */
  async search(request: SearchRequest): Promise<SearchResponse> {
    try {
      const response = await apiClient.post<ApiResponse<SearchResponse>>(
        `${this.baseUrl}`,
        request
      );

      return response.data.data;
    } catch (error) {
      console.error('Search failed:', error);
      throw error;
    }
  }

  /**
   * 获取搜索建议（自动完成）
   */
  async getAutoComplete(
    query: string,
    limit: number = 10
  ): Promise<AutoCompleteSuggestion[]> {
    try {
      const response = await apiClient.get<ApiResponse<AutoCompleteSuggestion[]>>(
        `${this.baseUrl}/suggestions`,
        {
          params: {
            q: query,
            limit,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Auto-complete failed:', error);
      return [];
    }
  }

  /**
   * 获取热门搜索关键词
   */
  async getPopularQueries(limit: number = 10): Promise<string[]> {
    try {
      const response = await apiClient.get<ApiResponse<string[]>>(
        `${this.baseUrl}/popular`,
        {
          params: { limit },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Failed to get popular queries:', error);
      return [];
    }
  }

  /**
   * 获取搜索历史
   */
  async getSearchHistory(
    page: number = 1,
    pageSize: number = 20
  ): Promise<PaginatedResponse<SearchHistory>> {
    try {
      const response = await apiClient.get<ApiResponse<PaginatedResponse<SearchHistory>>>(
        `${this.baseUrl}/history`,
        {
          params: { page, pageSize },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Failed to get search history:', error);
      throw error;
    }
  }

  /**
   * 删除搜索历史记录
   */
  async deleteSearchHistory(historyId?: string): Promise<void> {
    try {
      const url = historyId
        ? `${this.baseUrl}/history/${historyId}`
        : `${this.baseUrl}/history`;

      await apiClient.delete(url);
    } catch (error) {
      console.error('Failed to delete search history:', error);
      throw error;
    }
  }

  /**
   * 记录搜索分析数据
   */
  async recordSearchAnalytics(
    query: string,
    resultCount: number,
    clickedResultId?: string,
    clickPosition?: number
  ): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/analytics`, {
        query,
        resultCount,
        clickedResultId,
        clickPosition,
        timestamp: new Date().toISOString(),
      });
    } catch (error) {
      // 搜索分析失败不应该影响用户体验，只记录错误
      console.warn('Search analytics recording failed:', error);
    }
  }

  /**
   * 获取搜索分析数据（管理员功能）
   */
  async getSearchAnalytics(
    dateFrom?: string,
    dateTo?: string,
    limit: number = 100
  ): Promise<SearchAnalytics> {
    try {
      const response = await apiClient.get<ApiResponse<SearchAnalytics>>(
        `${this.baseUrl}/analytics`,
        {
          params: {
            dateFrom,
            dateTo,
            limit,
          },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Failed to get search analytics:', error);
      throw error;
    }
  }

  /**
   * 相似内容推荐
   */
  async getSimilarContent(
    contentId: string,
    limit: number = 5
  ): Promise<SearchResponse> {
    try {
      const response = await apiClient.get<ApiResponse<SearchResponse>>(
        `${this.baseUrl}/similar/${contentId}`,
        {
          params: { limit },
        }
      );

      return response.data.data;
    } catch (error) {
      console.error('Failed to get similar content:', error);
      throw error;
    }
  }

  /**
   * 全文搜索高亮预览
   */
  async getContentPreview(
    contentId: string,
    query: string
  ): Promise<{ content: string; highlights: string[] }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{ content: string; highlights: string[] }>
      >(`${this.baseUrl}/preview/${contentId}`, {
        params: { q: query },
      });

      return response.data.data;
    } catch (error) {
      console.error('Failed to get content preview:', error);
      throw error;
    }
  }

  /**
   * 搜索过滤器数据
   */
  async getFilterData(): Promise<{
    categories: Array<{ name: string; count: number }>;
    tags: Array<{ name: string; count: number }>;
    authors: Array<{ name: string; count: number }>;
    years: Array<{ year: number; count: number }>;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          categories: Array<{ name: string; count: number }>;
          tags: Array<{ name: string; count: number }>;
          authors: Array<{ name: string; count: number }>;
          years: Array<{ year: number; count: number }>;
        }>
      >(`${this.baseUrl}/filters`);

      return response.data.data;
    } catch (error) {
      console.error('Failed to get filter data:', error);
      throw error;
    }
  }

  /**
   * 检查搜索索引状态
   */
  async getIndexStatus(): Promise<{
    isHealthy: boolean;
    totalDocuments: number;
    lastUpdated: string;
    engine: 'elasticsearch' | 'database';
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          isHealthy: boolean;
          totalDocuments: number;
          lastUpdated: string;
          engine: 'elasticsearch' | 'database';
        }>
      >(`${this.baseUrl}/status`);

      return response.data.data;
    } catch (error) {
      console.error('Failed to get index status:', error);
      throw error;
    }
  }

  /**
   * 重新索引内容（管理员功能）
   */
  async reindexContent(): Promise<{ taskId: string }> {
    try {
      const response = await apiClient.post<ApiResponse<{ taskId: string }>>(
        `${this.baseUrl}/reindex`
      );

      return response.data.data;
    } catch (error) {
      console.error('Failed to start reindex:', error);
      throw error;
    }
  }

  /**
   * 获取重新索引状态
   */
  async getReindexStatus(taskId: string): Promise<{
    status: 'pending' | 'running' | 'completed' | 'failed';
    progress: number;
    message?: string;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          status: 'pending' | 'running' | 'completed' | 'failed';
          progress: number;
          message?: string;
        }>
      >(`${this.baseUrl}/reindex/${taskId}`);

      return response.data.data;
    } catch (error) {
      console.error('Failed to get reindex status:', error);
      throw error;
    }
  }

  /**
   * 获取搜索建议的详细信息（用于显示更丰富的建议）
   */
  async getEnhancedSuggestions(
    query: string,
    includeContent: boolean = true
  ): Promise<{
    queries: AutoCompleteSuggestion[];
    categories: AutoCompleteSuggestion[];
    tags: AutoCompleteSuggestion[];
    authors: AutoCompleteSuggestion[];
    posts: AutoCompleteSuggestion[];
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          queries: AutoCompleteSuggestion[];
          categories: AutoCompleteSuggestion[];
          tags: AutoCompleteSuggestion[];
          authors: AutoCompleteSuggestion[];
          posts: AutoCompleteSuggestion[];
        }>
      >(`${this.baseUrl}/suggestions/enhanced`, {
        params: {
          q: query,
          includeContent,
        },
      });

      return response.data.data;
    } catch (error) {
      console.error('Enhanced suggestions failed:', error);
      return {
        queries: [],
        categories: [],
        tags: [],
        authors: [],
        posts: [],
      };
    }
  }

  /**
   * 语音搜索转文本（如果支持）
   */
  async speechToText(audioBlob: Blob): Promise<{ text: string; confidence: number }> {
    try {
      const formData = new FormData();
      formData.append('audio', audioBlob, 'speech.wav');

      const response = await apiClient.post<
        ApiResponse<{ text: string; confidence: number }>
      >(`${this.baseUrl}/speech-to-text`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      return response.data.data;
    } catch (error) {
      console.error('Speech to text failed:', error);
      throw error;
    }
  }

  /**
   * 获取搜索趋势数据
   */
  async getSearchTrends(
    period: 'day' | 'week' | 'month' | 'year' = 'week'
  ): Promise<{
    trending: Array<{ query: string; count: number; growth: number }>;
    timeline: Array<{ date: string; count: number }>;
    categories: Array<{ category: string; count: number }>;
  }> {
    try {
      const response = await apiClient.get<
        ApiResponse<{
          trending: Array<{ query: string; count: number; growth: number }>;
          timeline: Array<{ date: string; count: number }>;
          categories: Array<{ category: string; count: number }>;
        }>
      >(`${this.baseUrl}/trends`, {
        params: { period },
      });

      return response.data.data;
    } catch (error) {
      console.error('Failed to get search trends:', error);
      throw error;
    }
  }
}

// 创建单例实例
export const searchApi = new SearchApiService();

// 导出类型
export type { SearchApiService };