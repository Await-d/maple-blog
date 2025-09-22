// @ts-nocheck
/**
 * 评论API服务
 * 封装所有评论相关的HTTP请求
 */

import { apiClient } from './api/client';
import type {
  Comment,
  CommentCreateRequest,
  CommentUpdateRequest,
  CommentQuery,
  CommentPagedResult,
  CommentStats,
  UserCommentStats,
  CommentReportRequest,
  CommentSearchResult,
  ApiResponse
} from '../types/comment';

const COMMENT_BASE_URL = '/api/comments';

export class CommentApiService {
  /**
   * 创建评论
   */
  static async createComment(request: CommentCreateRequest): Promise<Comment> {
    const response = await apiClient.post<Comment>(COMMENT_BASE_URL, request);
    return response.data;
  }

  /**
   * 更新评论
   */
  static async updateComment(commentId: string, request: CommentUpdateRequest): Promise<Comment> {
    const response = await apiClient.put<Comment>(`${COMMENT_BASE_URL}/${commentId}`, request);
    return response.data;
  }

  /**
   * 删除评论
   */
  static async deleteComment(commentId: string): Promise<void> {
    await apiClient.delete(`${COMMENT_BASE_URL}/${commentId}`);
  }

  /**
   * 获取单个评论
   */
  static async getComment(commentId: string): Promise<Comment> {
    const response = await apiClient.get<Comment>(`${COMMENT_BASE_URL}/${commentId}`);
    return response.data;
  }

  /**
   * 获取评论列表
   */
  static async getComments(query: CommentQuery): Promise<CommentPagedResult> {
    const params = new URLSearchParams();

    params.append('postId', query.postId);
    if (query.parentId) params.append('parentId', query.parentId);
    if (query.sortOrder) params.append('sortOrder', query.sortOrder);
    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.rootOnly) params.append('rootOnly', query.rootOnly.toString());
    if (query.includeStatus) {
      query.includeStatus.forEach(status => params.append('includeStatus', status));
    }

    const response = await apiClient.get<CommentPagedResult>(`${COMMENT_BASE_URL}?${params}`);
    return response.data;
  }

  /**
   * 获取评论树结构
   */
  static async getCommentTree(postId: string, maxDepth: number = 5): Promise<Comment[]> {
    const response = await apiClient.get<Comment[]>(`${COMMENT_BASE_URL}/tree/${postId}?maxDepth=${maxDepth}`);
    return response.data;
  }

  /**
   * 获取用户评论列表
   */
  static async getUserComments(
    userId: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<CommentPagedResult> {
    const response = await apiClient.get<CommentPagedResult>(
      `${COMMENT_BASE_URL}/user/${userId}?page=${page}&pageSize=${pageSize}`
    );
    return response.data;
  }

  /**
   * 搜索评论
   */
  static async searchComments(
    keyword: string,
    postId?: string,
    authorId?: string,
    page: number = 1,
    pageSize: number = 20
  ): Promise<CommentPagedResult> {
    const params = new URLSearchParams();
    params.append('keyword', keyword);
    if (postId) params.append('postId', postId);
    if (authorId) params.append('authorId', authorId);
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    const response = await apiClient.get<CommentPagedResult>(
      `${COMMENT_BASE_URL}/search?${params}`
    );
    return response.data;
  }

  /**
   * 点赞评论
   */
  static async likeComment(commentId: string): Promise<void> {
    await apiClient.post(`${COMMENT_BASE_URL}/${commentId}/like`);
  }

  /**
   * 取消点赞评论
   */
  static async unlikeComment(commentId: string): Promise<void> {
    await apiClient.delete(`${COMMENT_BASE_URL}/${commentId}/like`);
  }

  /**
   * 举报评论
   */
  static async reportComment(commentId: string, request: CommentReportRequest): Promise<void> {
    await apiClient.post(`${COMMENT_BASE_URL}/${commentId}/report`, request);
  }

  /**
   * 获取文章评论统计
   */
  static async getCommentStats(postId: string): Promise<CommentStats> {
    const response = await apiClient.get<CommentStats>(`${COMMENT_BASE_URL}/stats/post/${postId}`);
    return response.data;
  }

  /**
   * 获取用户评论统计
   */
  static async getUserCommentStats(userId: string): Promise<UserCommentStats> {
    const response = await apiClient.get<UserCommentStats>(`${COMMENT_BASE_URL}/stats/user/${userId}`);
    return response.data;
  }

  /**
   * 获取热门评论
   */
  static async getPopularComments(
    postId?: string,
    timeRange: number = 7,
    limit: number = 10
  ): Promise<Comment[]> {
    const params = new URLSearchParams();
    if (postId) params.append('postId', postId);
    params.append('timeRange', timeRange.toString());
    params.append('limit', limit.toString());

    const response = await apiClient.get<Comment[]>(`${COMMENT_BASE_URL}/popular?${params}`);
    return response.data;
  }

  /**
   * 批量获取评论
   */
  static async getCommentsBatch(commentIds: string[]): Promise<Comment[]> {
    if (commentIds.length === 0) return [];

    const promises = commentIds.map(id => this.getComment(id));
    const results = await Promise.allSettled(promises);

    return results
      .filter((result): result is PromiseFulfilledResult<Comment> => result.status === 'fulfilled')
      .map(result => result.value);
  }

  /**
   * 预加载评论回复
   */
  static async preloadReplies(commentId: string, depth: number = 2): Promise<Comment[]> {
    const query: CommentQuery = {
      postId: '', // 将由具体调用时填入
      parentId: commentId,
      pageSize: 50
    };

    const result = await this.getComments(query);

    if (depth > 1) {
      // 递归预加载下一级回复
      const nestedPromises = result.comments.map(async (comment) => {
        const nestedReplies = await this.preloadReplies(comment.id, depth - 1);
        return { ...comment, replies: nestedReplies };
      });

      return await Promise.all(nestedPromises);
    }

    return result.comments;
  }

  /**
   * 获取评论上下文（父评论和相邻评论）
   */
  static async getCommentContext(commentId: string): Promise<{
    parent?: Comment;
    siblings: Comment[];
    position: number;
  }> {
    const comment = await this.getComment(commentId);

    if (!comment.parentId) {
      // 根评论，获取同级评论
      const query: CommentQuery = {
        postId: comment.postId,
        rootOnly: true,
        pageSize: 100
      };
      const result = await this.getComments(query);
      const position = result.comments.findIndex(c => c.id === commentId);

      return {
        siblings: result.comments,
        position
      };
    }

    // 子评论，获取父评论和兄弟评论
    const [parent, siblingsResult] = await Promise.all([
      this.getComment(comment.parentId),
      this.getComments({
        postId: comment.postId,
        parentId: comment.parentId,
        pageSize: 100
      })
    ]);

    const position = siblingsResult.comments.findIndex(c => c.id === commentId);

    return {
      parent,
      siblings: siblingsResult.comments,
      position
    };
  }
}

// 导出默认实例
export const commentApi = CommentApiService;

// 错误处理包装器
export const withErrorHandling = <T extends any[], R>(
  fn: (...args: T) => Promise<R>
) => {
  return async (...args: T): Promise<ApiResponse<R>> => {
    try {
      const data = await fn(...args);
      return {
        data,
        success: true
      };
    } catch (error: any) {
      console.error('Comment API Error:', error);

      return {
        success: false,
        message: error.response?.data?.message || error.message || '请求失败',
        errors: error.response?.data?.errors || []
      };
    }
  };
};

// 带错误处理的API方法
export const safeCommentApi = {
  createComment: withErrorHandling(commentApi.createComment),
  updateComment: withErrorHandling(commentApi.updateComment),
  deleteComment: withErrorHandling(commentApi.deleteComment),
  getComment: withErrorHandling(commentApi.getComment),
  getComments: withErrorHandling(commentApi.getComments),
  getCommentTree: withErrorHandling(commentApi.getCommentTree),
  getUserComments: withErrorHandling(commentApi.getUserComments),
  searchComments: withErrorHandling(commentApi.searchComments),
  likeComment: withErrorHandling(commentApi.likeComment),
  unlikeComment: withErrorHandling(commentApi.unlikeComment),
  reportComment: withErrorHandling(commentApi.reportComment),
  getCommentStats: withErrorHandling(commentApi.getCommentStats),
  getUserCommentStats: withErrorHandling(commentApi.getUserCommentStats),
  getPopularComments: withErrorHandling(commentApi.getPopularComments)
};