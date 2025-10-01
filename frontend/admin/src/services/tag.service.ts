import { ApiService } from './api';
import type {
  Tag,
  CreateTagInput,
  UpdateTagInput,
  QueryParams,
} from '@/types';

class TagService {
  private baseUrl = '/tags';

  async getTags(params?: QueryParams): Promise<Tag[]> {
    return ApiService.get<Tag[]>(this.baseUrl, params);
  }

  async getTagById(tagId: string): Promise<Tag> {
    return ApiService.get<Tag>(`${this.baseUrl}/${tagId}`);
  }

  async createTag(payload: CreateTagInput): Promise<Tag> {
    return ApiService.post<Tag>(this.baseUrl, payload);
  }

  async updateTag(tagId: string, payload: UpdateTagInput): Promise<Tag> {
    return ApiService.put<Tag>(`${this.baseUrl}/${tagId}`, payload);
  }

  async deleteTag(tagId: string): Promise<void> {
    await ApiService.delete(`${this.baseUrl}/${tagId}`);
  }
}

export const tagService = new TagService();
export default tagService;
