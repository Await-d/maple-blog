import { ApiService } from './api';
import type {
  PaginatedResponse,
  Post,
  PostListQuery,
  CreatePostInput,
  UpdatePostInput,
  PostStatus,
} from '@/types';

class PostService {
  private baseUrl = '/posts';

  async getPosts(params?: PostListQuery): Promise<PaginatedResponse<Post>> {
    return ApiService.get<PaginatedResponse<Post>>(this.baseUrl, params);
  }

  async getPostById(postId: string): Promise<Post> {
    return ApiService.get<Post>(`${this.baseUrl}/${postId}`);
  }

  async createPost(payload: CreatePostInput): Promise<Post> {
    return ApiService.post<Post>(this.baseUrl, payload);
  }

  async updatePost(postId: string, payload: UpdatePostInput): Promise<Post> {
    return ApiService.put<Post>(`${this.baseUrl}/${postId}`, payload);
  }

  async deletePost(postId: string): Promise<void> {
    await ApiService.delete(`${this.baseUrl}/${postId}`);
  }

  async updatePostStatus(postId: string, status: PostStatus): Promise<Post> {
    return ApiService.patch<Post>(`${this.baseUrl}/${postId}/status`, { status });
  }

  async bulkDelete(postIds: string[]): Promise<void> {
    await ApiService.post(`${this.baseUrl}/bulk-delete`, { postIds });
  }

  async bulkUpdateStatus(postIds: string[], status: PostStatus): Promise<void> {
    await ApiService.post(`${this.baseUrl}/bulk-status`, { postIds, status });
  }
}

export const postService = new PostService();
export default postService;
